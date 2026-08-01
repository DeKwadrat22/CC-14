using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._ClawCommand.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Atmos;

public sealed partial class SpaceWindSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ThrownItemSystem _thrown = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ITileDefinitionManager _tileDefMan = default!;

    private const float MinAtmosForce = 1f;
    private readonly EntProtoId _spaceWindProto = "SpaceWindVisual";
    private readonly HashSet<Entity<MovedByPressureComponent>> _activePressures = new();
    private readonly HashSet<EntityUid> _entSet = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateHighPressure(frameTime);
    }

    private void UpdateHighPressure(float frameTime)
    {
        var toRemove = new List<Entity<MovedByPressureComponent>>();

        foreach (var ent in _activePressures)
        {
            if (!ent.Comp.Throwing
                || _timing.CurTime < ent.Comp.ThrowingCutoffTarget
                || !TryComp(ent.Owner, out PhysicsComponent? physics))
                continue;

            if (TryComp(ent.Owner, out ThrownItemComponent? thrown))
            {
                _thrown.LandComponent(ent.Owner, thrown, physics, true);
                _thrown.StopThrow(ent.Owner, thrown);
            }

            _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(ent.Owner, physics, true);

            ent.Comp.Throwing = false;
            ent.Comp.Accumulator += frameTime;

            if (ent.Comp.Accumulator < 2f)
                continue;

            // Reset it just for VV reasons even though it doesn't matter
            ent.Comp.Accumulator = 0f;
            toRemove.Add(ent);
        }

        foreach (var ent in toRemove)
        {
            _activePressures.Remove(ent);
        }
    }

    /// <summary>
    ///     Per-tile entry point: computes the pressure vector via the Matrix Airflow System, plays SFX/visuals,
    ///     then iterates entities on the tile and applies the throw force to any with a <see cref="MovedByPressureComponent"/>.
    /// </summary>
    public void HighPressureMovements(Entity<GridAtmosphereComponent> gridAtmosphere,
        TileAtmosphere tile,
        EntityQuery<PhysicsComponent> bodies,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<MovedByPressureComponent> pressureQuery,
        EntityQuery<MetaDataComponent> metas,
        EntityQuery<ProjectileComponent> projectileQuery,
        double gravity)
    {
        var windComp = EnsureComp<SpaceWindComponent>(gridAtmosphere.Owner);
        var atmosComp = gridAtmosphere.Comp;
        var oneAtmos = Atmospherics.OneAtmosphere;

        // No atmos yeets - return early.
        // We also check for if the grid is marked as exemt from space wind,
        // as well as pressure differences in a hard vacuum.
        if (!_atmos.SpaceWind || !atmosComp.SpaceWindSimulation || tile.Space)
            return;

        // If the pressure is below 5kPA, it can't throw any BASE items.
        // Or if it's below the cutoff of 1 atm: skip it.
        var pressure = tile.AirArchived?.Pressure;
        if (pressure is null
            || pressure <= atmosComp.PressureCutoff
            || oneAtmos - atmosComp.PressureCutoff <= pressure
            && pressure <= oneAtmos + atmosComp.PressureCutoff
            || !TryComp(gridAtmosphere.Owner, out MapGridComponent? mapGrid)
            || !_map.TryGetTileRef(gridAtmosphere.Owner, mapGrid, tile.GridIndices, out var tileRef))
            return;

        var tileDef = (ContentTileDefinition)_tileDefMan[tileRef.Tile.TypeId];
        if (!tileDef.SimulatedTurf)
            return;

        var partialFrictionComposition = gravity * tileDef.MobFrictionNoInput ?? 0.2f;

        var pressureVector = _atmos.GetPressureVectorFromTile(atmosComp, tile);
        if (!pressureVector.IsValid())
            return;

        // Remember the vector for visuals/debug.
        windComp.LastPressureVector = pressureVector;

        // Apply the strength multiplier BEFORE the small-vector guard so the cvar can scale the deadzone.
        pressureVector *= SpaceWindStrengthMultiplier;

        // Cache magnitude so we don't re-run sqrt per entity.
        var pVecLength = pressureVector.Length();
        if (pVecLength <= MinAtmosForce)
            return;

        if (SpaceWindVisuals && windComp.SpaceWindCooldown == 0)
        {
            var location = _map.GridTileToLocal(gridAtmosphere.Owner, mapGrid, tile.GridIndices);
            var visualEnt = SpawnAtPosition(_spaceWindProto, location);
            _xform.SetLocalRotation(visualEnt, pressureVector.ToAngle() - MathF.PI / 2);
        }

        if (windComp.SpaceWindCooldown++ > windComp.SpaceWindCooldownCycles)
            windComp.SpaceWindCooldown = 0;

        _entSet.Clear();
        _lookup.GetLocalEntitiesIntersecting(tile.GridIndex, tile.GridIndices, _entSet, 0f);

        foreach (var entity in _entSet)
        {
            if (!bodies.TryGetComponent(entity, out var body)
                || !pressureQuery.TryGetComponent(entity, out var pressureComp)
                || !pressureComp.Enabled
                || _containers.IsEntityInContainer(entity, metas.GetComponent(entity))
                || pressureComp.LastHighPressureMovementAirCycle >= atmosComp.UpdateCounter)
                continue;

            ExperiencePressureDifference(
                (entity, pressureComp),
                atmosComp.UpdateCounter,
                pressureVector,
                pVecLength,
                partialFrictionComposition,
                projectileQuery,
                xforms.GetComponent(entity),
                body);
        }
    }

    /// <summary>
    ///     Decides whether and how hard a single entity gets thrown by the local pressure vector.
    ///     Friction is computed as gravity * tileFriction * mass. If the wind force is below static friction
    ///     (and the entity isn't already floating or weightless), nothing happens.
    ///     Humanoids get a separate multiplier and may be knocked down if the torque threshold is exceeded.
    /// </summary>
    public void ExperiencePressureDifference(
        Entity<MovedByPressureComponent> ent,
        int cycle,
        Vector2 pressureVector,
        float pVecLength,
        double partialFrictionComposition,
        EntityQuery<ProjectileComponent> projectileQuery,
        TransformComponent? xform = null,
        PhysicsComponent? physics = null)
    {
        var (uid, component) = ent;
        if (!Resolve(uid, ref physics, false)
            || !Resolve(uid, ref xform)
            || physics.BodyType == BodyType.Static
            || physics.LinearVelocity.Length() >= SpaceWindMaxForce)
            return;

        var alwaysThrow = partialFrictionComposition == 0 || physics.BodyStatus == BodyStatus.InAir;

        // Coefficient of static friction in Newtons (kg * m/s^2). Tripled while prone.
        var coefficientOfFriction = partialFrictionComposition * physics.Mass;
        if (_standing.IsDown(uid))
            coefficientOfFriction *= 3;

        if (TryComp(ent.Owner, out HumanoidProfileComponent? humanoidProfile))
        {
            pressureVector *= HumanoidThrowMultiplier;

            var pVecLength2 = pressureVector.Length();
            if (pVecLength2 <= MinAtmosForce)
                return;

            if (SpaceWindAllowKnockdown)
            {
                // Quick-and-dirty torque threshold: ~1/3 * mass * height^2 for a humanoid (1.75 m default).
                var heightSquared = MathF.Pow(humanoidProfile.Height * 1.75f, 2);
                var knockdownThreshold = heightSquared / 3f;
                if (knockdownThreshold <= pVecLength)
                    _stun.TryKnockdown(uid, TimeSpan.FromSeconds(SpaceWindKnockdownTime));
            }
        }

        if (!alwaysThrow && pVecLength < coefficientOfFriction)
            return;

        // Add the entity's facing as a small bias on top of the wind direction.
        var velocity = _xform.GetWorldRotation(uid).ToWorldVec() + pressureVector;

        _throwing.TryThrow(uid,
            velocity,
            physics,
            xform,
            baseThrowSpeed: 1f,
            doSpin: physics.AngularVelocity < SpaceWindMaxAngularVelocity);

        component.LastHighPressureMovementAirCycle = cycle;
        component.Throwing = true;
        component.ThrowingCutoffTarget = _timing.CurTime + component.CutoffTime;
        _activePressures.Add(ent);
    }
}
