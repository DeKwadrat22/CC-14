using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Borgs;

/// <summary>
/// Client side logic for borg type switching. Sets up primarily client-side visual information.
/// </summary>
/// <seealso cref="SharedBorgSwitchableTypeSystem"/>
/// <seealso cref="BorgSwitchableTypeComponent"/>
public sealed partial class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem
{
    [Dependency] private BorgSystem _borgSystem = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableTypeComponent, AfterAutoHandleStateEvent>(AfterStateHandler);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, ComponentStartup>(OnComponentStartup);
        // _ClawCommand: when a dogborg starts/stops moving, swap the Body and
        // LightStatus layers to their "_moving" counterparts so the walk cycle
        // is shown only while actually walking.
        //
        // We subscribe on the BorgSwitchableTypeComponent (not SpriteMovementComponent)
        // because ClientSpriteMovementSystem already owns
        // (SpriteMovementComponent, AfterAutoHandleStateEvent) and
        // SharedSpriteMovementSystem owns (SpriteMovementComponent, SpriteMoveEvent),
        // and Robust forbids a second subscription for the same (component, event) pair.
        //
        // OnSpriteMove pre-writes SpriteMovementComponent.IsMoving from
        // args.IsMoving before reading it, so dispatch order between us and
        // SharedSpriteMovementSystem doesn't matter.
        SubscribeLocalEvent<BorgSwitchableTypeComponent, SpriteMoveEvent>(OnSpriteMove);
    }

    private void OnComponentStartup(Entity<BorgSwitchableTypeComponent> ent, ref ComponentStartup args)
    {
        UpdateEntityAppearance(ent);
    }

    private void AfterStateHandler(Entity<BorgSwitchableTypeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateEntityAppearance(ent);
    }

    private void OnSpriteMove(Entity<BorgSwitchableTypeComponent> ent, ref SpriteMoveEvent args)
    {
        // The bus may invoke us before SharedSpriteMovementSystem because we
        // subscribe on BorgSwitchableTypeComponent and they subscribe on
        // SpriteMovementComponent — different (component, event) slots that
        // aren't globally ordered. Mirror the new value ourselves so the read
        // inside UpdateEntityAppearance via GetMotionState is correct
        // regardless of dispatch order. If shared already ran first, this is
        // a no-op assignment.
        if (TryComp<SpriteMovementComponent>(ent.Owner, out var move))
            move.IsMoving = args.IsMoving;
        UpdateEntityAppearance(ent);
    }

    protected override void UpdateEntityAppearance(
        Entity<BorgSwitchableTypeComponent> entity,
        BorgTypePrototype prototype)
    {
        if (TryComp(entity, out SpriteComponent? sprite))
        {
            // _ClawCommand: route Body/LightStatus through BorgSystem's motion
            // helper so dogborgs (those with SpriteMovementComponent) animate
            // their legs only when actually moving. Regular borgs return the
            // base state unchanged.
            _sprite.LayerSetRsiState((entity, sprite), BorgVisualLayers.Body, _borgSystem.GetMotionState(entity.Owner, prototype.SpriteBodyState));
            _sprite.LayerSetRsiState((entity, sprite), BorgVisualLayers.LightStatus, _borgSystem.GetMotionState(entity.Owner, prototype.SpriteToggleLightState));
        }

        if (TryComp(entity, out BorgChassisComponent? chassis))
        {
            _borgSystem.SetMindStates(
                (entity.Owner, chassis),
                prototype.SpriteHasMindState,
                prototype.SpriteNoMindState);

            if (TryComp(entity, out AppearanceComponent? appearance))
            {
                // Queue update so state changes apply.
                _appearance.QueueUpdate(entity, appearance);
            }
        }

        base.UpdateEntityAppearance(entity, prototype);
    }
}
