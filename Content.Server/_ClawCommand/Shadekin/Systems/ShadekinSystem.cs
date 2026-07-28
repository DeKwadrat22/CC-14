using System.Linq;
using Content.Server.Ghost;
using Content.Shared._ClawCommand.Mood;
using Content.Shared._ClawCommand.Shadekin;
using Content.Shared._ClawCommand.Shadekin.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Shadekin.Systems;

public sealed partial class ShadekinSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMoodSystem _mood = default!; // Claw Command
    [Dependency] private IGameTiming _timing = default!; // Claw Command

    public const string ShadekinPhaseActionId = "ShadekinActionPhase";
    public const string ShadekinSleepActionId = "ShadekinActionSleep";

    private sealed class LightCone
    {
        public float Direction { get; set; }
        public float InnerWidth { get; set; }
        public float OuterWidth { get; set; }
    }
    private readonly Dictionary<string, List<LightCone>> _lightMasks = new()
    {
        ["/Textures/Effects/LightMasks/cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 }
    },
        ["/Textures/Effects/LightMasks/double_cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 },
        new LightCone { Direction = 180, InnerWidth = 30, OuterWidth = 60 }
    }
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ShadekinComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ShadekinComponent, ShadekinPhaseActionEvent>(OnPhaseAction);
        SubscribeLocalEvent<ShadekinComponent, CritShadekinEvent>(OnCritShadekinAction);
    }

    private void OnInit(EntityUid uid, ShadekinComponent component, MapInitEvent args)
    {
        if (component.Blackeye)
            ApplyBlackEye(uid, component);
        else
        {
            _actionsSystem.AddAction(uid, ref component.ShadekinPhaseAction, ShadekinPhaseActionId, uid);
            if (TryComp<MobStateActionsComponent>(uid, out var mobstate))
            {
                mobstate.Actions[MobState.Critical].Clear();
                mobstate.Actions[MobState.Critical].Add("ShadekinActionRejuvenate");
            }
        }

        _actionsSystem.AddAction(uid, ref component.ShadekinSleepAction, ShadekinSleepActionId, uid);
        UpdateAlert(uid, component);
    }

    public void ApplyBlackEye(EntityUid uid, ShadekinComponent component)
    {
        // NOTE (CLAW COMMAND): Floof recolored the eyes black here via HumanoidAppearanceComponent.
        // This fork's organ-based body locks VisualOrganComponent behind [Access(SharedVisualBodySystem)],
        // so the black-eye *visual* is omitted; the black-eye state (no powers, drained energy) is intact.

        _actionsSystem.RemoveAction(uid, component.ShadekinPhaseAction);

        if (TryComp<MobStateActionsComponent>(uid, out var mobstate))
        {
            mobstate.Actions[MobState.Critical].Clear();
            mobstate.Actions[MobState.Critical].Add("ActionCritSuccumb");
            mobstate.Actions[MobState.Critical].Add("ActionCritFakeDeath");
            mobstate.Actions[MobState.Critical].Add("ActionCritLastWords");
        }

        component.Energy = 0;

        UpdateAlert(uid, component);
    }

    public void UpdateAlert(EntityUid uid, ShadekinComponent component)
    {
        var lightseverity = component.LightExposure;
        var energyseverity = (short) ContentHelpers.RoundToLevels(component.Energy, component.MaxEnergy, 5);

        if (component.Blackeye)
            energyseverity = 0;

        _alerts.ShowAlert(uid, "Shadekin-" + lightseverity + "-" + energyseverity);
    }

    private void OnRejuvenate(EntityUid uid, ShadekinComponent component, RejuvenateEvent args)
    {
        if (component.Blackeye)
            return;

        component.Energy = component.MaxEnergy;

        _actionsSystem.AddAction(uid, ref component.ShadekinPhaseAction, ShadekinPhaseActionId, uid);

        if (TryComp<MobStateActionsComponent>(uid, out var mobstate))
        {
            mobstate.Actions[MobState.Critical].Clear();
            mobstate.Actions[MobState.Critical].Add("ShadekinActionRejuvenate");
        }

        UpdateAlert(uid, component);
    }

    private void OnPhaseAction(EntityUid uid, ShadekinComponent component, ShadekinPhaseActionEvent args)
    {
        if (component.Blackeye)
        {
            args.Handled = true;
            return;
        }

        if (HasComp<ShadekinCuffComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("phase-fail-generic"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        if (component.LightExposure == 4)
        {
            _popup.PopupEntity(Loc.GetString("shadekin-lightextreme-energy"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        var price = 0;
        switch (component.LightExposure)
        {
            case 3:
                price += 50;
                break;
            case 2:
                price += 30;
                break;
            case 1:
                price += 15;
                break;
        }

        if (HasComp<EtherealComponent>(uid))
        {
            Phase(uid);
            args.Handled = true;
            return;
        }

        price += 100;

        if (component.Energy >= price)
        {
            if (Phase(uid))
            {
                component.Energy -= price;
                UpdateAlert(uid, component);
            }
        }
        else
            _popup.PopupEntity(Loc.GetString("shadekin-no-energy"), uid, uid, PopupType.LargeCaution);

        args.Handled = true;
    }

    public bool Phase(EntityUid uid)
    {
        if (TryComp<EtherealComponent>(uid, out var ethereal))
        {
            var tileref = _turf.GetTileRef(Transform(uid).Coordinates);
            if (tileref != null
            && _physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return false;
            }

            if (HasComp<ShadekinComponent>(uid))
            {
                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                {
                    _ghost.DoGhostBooEvent(light);
                }

                var effect = SpawnAtPosition("ShadekinPhaseInEffect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);

            RemComp(uid, ethereal);
        }
        else
        {
            if (_container.IsEntityInContainer(uid))
            {
                _popup.PopupEntity(Loc.GetString("phase-fail-generic"), uid, uid);
                return false;
            }

            var newEthereal = EnsureComp<EtherealComponent>(uid);
            if (HasComp<ShadekinComponent>(uid))
            {
                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                {
                    _ghost.DoGhostBooEvent(light);
                }

                var effect = SpawnAtPosition("ShadekinPhaseOutEffect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;

                newEthereal.LastEtherealTime = _timing.CurTime;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);
        }
        return true;
    }

    private void OnCritShadekinAction(EntityUid uid, ShadekinComponent component, CritShadekinEvent args)
    {
        _mobState.ChangeMobState(uid, MobState.Dead);
    }

    private void OnMobStateChanged(EntityUid uid, ShadekinComponent component, MobStateChangedEvent args)
    {
        if (component.Blackeye
            || HasComp<ShadekinCuffComponent>(uid)
            || args.NewMobState != MobState.Dead)
            return;

        if (TryComp<InventoryComponent>(uid, out var inventoryComponent) && _inventorySystem.TryGetSlots(uid, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventorySystem.TryUnequip(uid, slot.Name, true, true, false, inventoryComponent);
            }
        }

        SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);

        var spawns = new List<Entity<AnomalyJobSpawnComponent>>();
        var query = EntityQueryEnumerator<AnomalyJobSpawnComponent>();
        while (query.MoveNext(out var spawnUid, out var spawn))
        {
            spawns.Add((spawnUid, spawn));
        }

        _random.Shuffle(spawns);

        foreach (var (spawnUid, spawn) in spawns)
        {
            _joints.RecursiveClearJoints(uid);
            _transform.SetCoordinates(uid, Transform(spawnUid).Coordinates);
            break;
        }

        var effect = SpawnAtPosition("ShadekinPhaseIn2Effect", Transform(uid).Coordinates);
        Transform(effect).LocalRotation = Transform(uid).LocalRotation;

        RaiseLocalEvent(uid, new RejuvenateEvent());
        component.Rejuvenating = true;
        component.Energy = 0;
    }

    private Angle GetAngle(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid)
    {
        var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightUid);
        lightPos += lightRot.RotateVec(lightComp.Offset);

        var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetUid);

        var mapDiff = targetPos - lightPos;

        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        var angle = oppositeMapDiff.ToWorldAngle();

        if (angle == double.NaN && _transform.ContainsEntity(targetUid, lightUid) || _transform.ContainsEntity(lightUid, targetUid))
        {
            angle = 0f;
        }

        return angle;
    }

    /// <summary>
    ///     Claw Command - Shadekin are at home in the dark and increasingly miserable in the light.
    ///     Indexed by the moodlet that a given light exposure level maps to.
    /// </summary>
    private static readonly ProtoId<MoodEffectPrototype>[] LightMoodlets =
    {
        "ShadekinDarkness",
        "ShadekinLightAnnoyed",
        "ShadekinLightHigh",
        "ShadekinLightExtreme",
    };

    /// <summary>
    ///     Claw Command - Applies the one light moodlet that matches the current exposure and clears the rest.
    ///     Exposure level 1 (dim light, or phased out into the Dark) is neutral: no moodlet either way.
    /// </summary>
    private void UpdateLightMood(EntityUid uid, int lightExposure)
    {
        var index = lightExposure switch
        {
            0 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            _ => -1,
        };

        for (var i = 0; i < LightMoodlets.Length; i++)
        {
            if (i == index)
                _mood.AddMoodlet(uid, LightMoodlets[i]);
            else
                _mood.RemoveMoodlet(uid, LightMoodlets[i]);
        }
    }

    public float GetLightExposure(EntityUid uid)
    {
        var illumination = 0f;

        var lightQuery = _lookup.GetEntitiesInRange(uid, 20)
                .Where(x => HasComp<PointLightComponent>(x));

        foreach (var light in lightQuery)
        {
            if (!TryComp<PointLightComponent>(light, out var pointLight))
                continue;

            if (HasComp<DarkLightComponent>(light))
                continue;

            if (!pointLight.Enabled
                || pointLight.Radius < 1
                || pointLight.Energy <= 0)
                continue;

            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(light);
            lightPos += lightRot.RotateVec(pointLight.Offset);

            if (!_examine.InRangeUnOccluded(light, uid, pointLight.Radius, null))
                continue;

            Transform(uid).Coordinates.TryDistance(EntityManager, Transform(light).Coordinates, out var dist);

            var denom = dist / pointLight.Radius;
            var attenuation = 1 - (denom * denom);
            var calculatedLight = 0f;

            if (pointLight.MaskPath is not null)
            {
                var angleToTarget = GetAngle(light, pointLight, uid);
                foreach (var cone in _lightMasks[pointLight.MaskPath])
                {
                    var coneLight = 0f;
                    var angleAttenuation = (float) Math.Min((float) Math.Max(cone.OuterWidth - angleToTarget, 0f), cone.InnerWidth) / cone.OuterWidth;

                    if (angleToTarget.Degrees - cone.Direction > cone.OuterWidth)
                        continue;
                    else if (angleToTarget.Degrees - cone.Direction > cone.InnerWidth
                        && angleToTarget.Degrees - cone.Direction < cone.OuterWidth)
                        coneLight = pointLight.Energy * attenuation * attenuation * angleAttenuation;
                    else
                        coneLight = pointLight.Energy * attenuation * attenuation;

                    calculatedLight = Math.Max(calculatedLight, coneLight);
                }
            }
            else
                calculatedLight = pointLight.Energy * attenuation * attenuation;

            illumination += calculatedLight;
        }

        return illumination;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadekinComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.Accumulator += frameTime;

            if (_mobState.IsDead(uid))
                continue;

            if (component.Accumulator <= 1)
                continue;

            component.Accumulator = 0;
            var ethereal = HasComp<EtherealComponent>(uid);

            var lightExposure = 0f;

            if (!_container.IsEntityInContainer(uid))
                lightExposure = GetLightExposure(uid);

            if (lightExposure >= 20f)
                component.LightExposure = 4;
            else if (lightExposure >= 10f)
                component.LightExposure = 3;
            else if (lightExposure >= 5f)
                component.LightExposure = 2;
            else if (lightExposure >= 0.8f)
                component.LightExposure = 1;
            else
                component.LightExposure = 0;

            UpdateAlert(uid, component);
            UpdateLightMood(uid, ethereal ? 1 : (int) component.LightExposure); // Claw Command

            if (component.Blackeye
                || HasComp<ShadekinCuffComponent>(uid))
                continue;

            if (component.Energy > component.MaxEnergy)
                component.Energy = component.MaxEnergy;

            if (component.Energy < 0)
                component.Energy = 0;

            if (component.Energy < component.MaxEnergy)
            {
                var energyGain = 1f;

                if (!ethereal)
                {
                    if (component.LightExposure == 4)
                        energyGain = 0f;
                    else if (component.LightExposure == 3)
                        energyGain = 0.1f;
                    else if (component.LightExposure == 2)
                        energyGain = 0.4f;
                    else if (component.LightExposure == 1)
                        energyGain = 0.5f;
                }

                if (HasComp<SleepingComponent>(uid))
                    energyGain *= 2;

                energyGain *= component.Energymultiplier;

                component.Energy += energyGain;
            }

            UpdateAlert(uid, component);

            if (component.Rejuvenating && component.Energy >= component.MaxEnergy)
            {
                component.Rejuvenating = false;
                _popup.PopupEntity(Loc.GetString("shadekin-rejuvenate-completed"), uid, uid, PopupType.Large);
            }
        }
    }
}
