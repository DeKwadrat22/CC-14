using Content.Shared._ClawCommand.Silicons.Borgs;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!; // Claw Command - dogborg pose state lookup
    [Dependency] private EntityQuery<BorgChassisComponent> _chassisQuery = default!;
    [Dependency] private EntityQuery<PowerCellSlotComponent> _slotQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBattery();

        SubscribeLocalEvent<BorgChassisComponent, AppearanceChangeEvent>(OnBorgAppearanceChanged);
        SubscribeLocalEvent<MMIComponent, AppearanceChangeEvent>(OnMMIAppearanceChanged);
        // _ClawCommand note: motion-aware sprite state swaps are driven by
        // BorgSwitchableTypeSystem's SpriteMovementComponent subscription,
        // which cascades back into UpdateBorgAppearance via the appearance
        // update it queues. Subscribing here too would crash with
        // "Duplicate Subscriptions for comp=SpriteMovementComponent" since
        // Robust forbids two handlers for the same (component, event) pair.

        // _ClawCommand: pose changes (sit / rest / belly-up) come in as
        // DogborgPoseComponent state syncs. Refresh the borg appearance when
        // the pose flips so the Body layer swaps to / from the pose sprite.
        SubscribeLocalEvent<DogborgPoseComponent, AfterAutoHandleStateEvent>(OnPoseStateChanged);
    }

    private void OnPoseStateChanged(Entity<DogborgPoseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<BorgChassisComponent>(ent.Owner, out var chassis))
            return;
        UpdateBorgAppearance((ent.Owner, chassis, null, null));
    }

    /// <summary>
    /// _ClawCommand: returns the input state's "_moving" counterpart when the
    /// entity is currently moving and has a <see cref="SpriteMovementComponent"/>.
    /// Lets dogborgs swap between static and walk-cycle frames cleanly without
    /// duplicating logic. Returns the base state for any entity that isn't
    /// motion-aware (regular borgs, MMIs, etc.).
    /// </summary>
    public string GetMotionState(EntityUid uid, string baseState)
    {
        if (TryComp<SpriteMovementComponent>(uid, out var move) && move.IsMoving)
            return $"{baseState}_moving";
        return baseState;
    }

    public override void UpdateUI(Entity<BorgChassisComponent?> chassis)
    {
        if (_ui.TryGetOpenUi(chassis.Owner, BorgUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnBorgAppearanceChanged(Entity<BorgChassisComponent> chassis, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateBorgAppearance((chassis.Owner, chassis.Comp, args.Component, args.Sprite));
    }

    protected override void OnInserted(Entity<BorgChassisComponent> chassis, ref EntInsertedIntoContainerMessage args)
    {
        if (!chassis.Comp.Initialized)
            return;

        base.OnInserted(chassis, ref args);
        UpdateUI(chassis.AsNullable());
        UpdateBorgAppearance((chassis, chassis.Comp));
        UpdateBatteryAlert((chassis.Owner, chassis.Comp, null));
    }

    protected override void OnRemoved(Entity<BorgChassisComponent> chassis, ref EntRemovedFromContainerMessage args)
    {
        if (!chassis.Comp.Initialized)
            return;

        base.OnRemoved(chassis, ref args);
        UpdateUI(chassis.AsNullable());
        UpdateBorgAppearance((chassis, chassis.Comp));
        UpdateBatteryAlert((chassis.Owner, chassis.Comp, null));
    }

    private void UpdateBorgAppearance(Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, ref ent.Comp3))
            return;

        var bakedBody = HasComp<BorgBakedBodyComponent>(ent.Owner);

        if (_appearance.TryGetData<MobState>(ent.Owner, MobStateVisuals.State, out var state, ent.Comp2))
        {
            if (state != MobState.Alive)
            {
                _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, false);
                if (bakedBody)
                    _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Body, true);
                // _ClawCommand: pin the Body to its dead pose — the wreck sprite if
                // the BorgTypePrototype defines one, otherwise just the static base.
                // Without this a borg killed mid-run keeps trotting on the ground.
                if (TryGetDeadBodyState(ent.Owner, out var deadState))
                    _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), BorgVisualLayers.Body, deadState);
                return;
            }
        }

        // _ClawCommand: pose branch — when the dogborg is voluntarily sitting /
        // resting / belly-up, show only the Body layer with the chosen pose
        // sprite. Light overlay carries the baked-in upright body so leaving it
        // visible would render two bodies on top of each other.
        if (TryComp<DogborgPoseComponent>(ent.Owner, out var poseComp)
            && poseComp.Pose != DogborgPose.None
            && TryGetPoseBodyState(ent.Owner, poseComp.Pose, out var poseState))
        {
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, false);
            // _ClawCommand: also hide the light-strip layer — its RSI only ships
            // the standing pose, so leaving it visible while the body sits/rests
            // renders a floating standing-pose strip above the seated body.
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.LightStatus, false);
            if (bakedBody)
                _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Body, true);
            _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), BorgVisualLayers.Body, poseState);
            return;
        }

        if (!_appearance.TryGetData<bool>(ent.Owner, BorgVisuals.HasPlayer, out var hasPlayer, ent.Comp2))
            hasPlayer = false;

        var lightOn = ent.Comp1.BrainEntity != null || hasPlayer;
        _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, lightOn);
        var lightState = hasPlayer ? ent.Comp1.HasMindState : ent.Comp1.NoMindState;
        _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), BorgVisualLayers.Light, GetMotionState(ent.Owner, lightState));

        // _ClawCommand: when the Light overlay carries a baked-in body (so its
        // animation stays in lockstep with the eye glow), hide the regular Body
        // layer while Light is visible — otherwise two bodies render and visibly
        // drift apart by a frame or two.
        if (bakedBody)
        {
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Body, !lightOn);

            // When the Body becomes the only visible layer (no brain, no player),
            // pin it to the static base so an unoccupied chassis can't trot.
            if (!lightOn && TryGetBaseBodyState(ent.Owner, out var baseBody))
                _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), BorgVisualLayers.Body, baseBody);
        }
    }

    /// <summary>
    /// _ClawCommand: returns the sprite state the Body layer should pin to when
    /// the borg is dead. Prefers <see cref="BorgTypePrototype.SpriteWreckState"/>
    /// when defined, otherwise the static <see cref="BorgTypePrototype.SpriteBodyState"/>.
    /// Returns false for borgs without a BorgSwitchableType (e.g. legacy chassis).
    /// </summary>
    private bool TryGetDeadBodyState(EntityUid uid, out string state)
    {
        if (TryComp<BorgSwitchableTypeComponent>(uid, out var switchable)
            && switchable.SelectedBorgType is { } selected
            && _proto.TryIndex<BorgTypePrototype>(selected, out var bt))
        {
            state = bt.SpriteWreckState ?? bt.SpriteBodyState;
            return true;
        }
        state = string.Empty;
        return false;
    }

    /// <summary>
    /// _ClawCommand: returns the static base body state from the entity's
    /// BorgTypePrototype, used to lock an unoccupied chassis to its idle pose.
    /// </summary>
    private bool TryGetBaseBodyState(EntityUid uid, out string state)
    {
        if (TryComp<BorgSwitchableTypeComponent>(uid, out var switchable)
            && switchable.SelectedBorgType is { } selected
            && _proto.TryIndex<BorgTypePrototype>(selected, out var bt))
        {
            state = bt.SpriteBodyState;
            return true;
        }
        state = string.Empty;
        return false;
    }

    /// <summary>
    /// _ClawCommand: returns the Body sprite state for a dogborg pose
    /// (sit / rest / belly-up) from its BorgTypePrototype. Returns false when
    /// the variant doesn't define the requested pose (e.g. blade is too small).
    /// </summary>
    private bool TryGetPoseBodyState(EntityUid uid, DogborgPose pose, out string state)
    {
        state = string.Empty;
        if (!TryComp<BorgSwitchableTypeComponent>(uid, out var switchable)
            || switchable.SelectedBorgType is not { } selected
            || !_proto.TryIndex<BorgTypePrototype>(selected, out var bt))
            return false;

        var picked = pose switch
        {
            DogborgPose.Sit => bt.SpriteSitState,
            DogborgPose.Rest => bt.SpriteRestState,
            DogborgPose.BellyUp => bt.SpriteBellyUpState,
            _ => null,
        };
        if (picked == null)
            return false;
        state = picked;
        return true;
    }

    private void OnMMIAppearanceChanged(EntityUid uid, MMIComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        var sprite = args.Sprite;

        if (!_appearance.TryGetData(uid, MMIVisuals.BrainPresent, out bool brain))
            brain = false;
        if (!_appearance.TryGetData(uid, MMIVisuals.HasMind, out bool hasMind))
            hasMind = false;

        _sprite.LayerSetVisible((uid, sprite), MMIVisualLayers.Brain, brain);
        if (!brain)
        {
            _sprite.LayerSetRsiState((uid, sprite), MMIVisualLayers.Base, component.NoBrainState);
        }
        else
        {
            var state = hasMind
                ? component.HasMindState
                : component.NoMindState;
            _sprite.LayerSetRsiState((uid, sprite), MMIVisualLayers.Base, state);
        }
    }

    /// <summary>
    /// Sets the sprite states used for the borg "is there a mind or not" indication.
    /// </summary>
    /// <param name="borg">The entity and component to modify.</param>
    /// <param name="hasMindState">The state to use if the borg has a mind.</param>
    /// <param name="noMindState">The state to use if the borg has no mind.</param>
    /// <seealso cref="BorgChassisComponent.HasMindState"/>
    /// <seealso cref="BorgChassisComponent.NoMindState"/>
    public void SetMindStates(Entity<BorgChassisComponent> borg, string hasMindState, string noMindState)
    {
        borg.Comp.HasMindState = hasMindState;
        borg.Comp.NoMindState = noMindState;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateBattery(frameTime);
    }
}
