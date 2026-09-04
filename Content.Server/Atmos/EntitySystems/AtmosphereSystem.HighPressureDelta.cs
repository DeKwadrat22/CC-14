using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private const float MinAtmosForce = 1f;
    private readonly EntProtoId _spaceWindProto = "SpaceWindVisual";
    private readonly HashSet<Entity<MovedByPressureComponent>> _activePressures = new();

    /// <summary>
    /// Per-tick cleanup pass for entities that the airflow system put into the in-air state.
    /// Once an entity's <see cref="MovedByPressureComponent.ThrowingCutoffTarget"/> elapses without
    /// being hit by another pressure delta, we land it, stop the throw and let it sleep again.
    /// </summary>
    private void UpdateHighPressure(float frameTime)
    {
        // Collect first; we mutate _activePressures inside the loop.
        var toRemove = new List<Entity<MovedByPressureComponent>>();

        foreach (var ent in _activePressures)
        {
            if (!ent.Comp.Throwing
                || _gameTiming.CurTime < ent.Comp.ThrowingCutoffTarget
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
            toRemove.Add(ent);
        }

        foreach (var ent in toRemove)
            _activePressures.Remove(ent);
    }

    /// <summary>
    /// Per-tile entry point: computes the pressure vector via the Matrix Airflow System, plays SFX/visuals,
    /// then iterates entities on the tile and applies the throw force to any with a <see cref="MovedByPressureComponent"/>.
    /// </summary>
    private void HighPressureMovements(Entity<GridAtmosphereComponent> gridAtmosphere,
        TileAtmosphere tile,
        EntityQuery<PhysicsComponent> bodies,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<MovedByPressureComponent> pressureQuery,
        EntityQuery<MetaDataComponent> metas,
        EntityQuery<ProjectileComponent> projectileQuery,
        double gravity)
    {
        var atmosComp = gridAtmosphere.Comp;
        var oneAtmos = Atmospherics.OneAtmosphere;

        // No atmos yeets — return early.
        if (!SpaceWind
            || !atmosComp.SpaceWindSimulation     // Grid is marked as exempt from space wind.
            || tile.Space)                         // Pressure differentials can't exist in a hard vacuum.
            return;

        var pressure = tile.AirArchived?.Pressure;
        if (pressure is null
            || pressure <= atmosComp.PressureCutoff                          // Below 5 kPa: can't throw a base item.
            || oneAtmos - atmosComp.PressureCutoff <= pressure
            && pressure <= oneAtmos + atmosComp.PressureCutoff               // Within +-cutoff of 1 atm: skip.
            || !TryComp(gridAtmosphere.Owner, out MapGridComponent? mapGrid)
            || !_mapSystem.TryGetTileRef(gridAtmosphere.Owner, mapGrid, tile.GridIndices, out var tileRef))
            return;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];
        if (!tileDef.SimulatedTurf)
            return;

        // NOTE: This expression matches space/'s precedence quirk exactly. `*` binds tighter than `??`,
        // so when MobFrictionNoInput is null (the default for every tile that doesn't override it) the
        // multiplication produces a nullable double of value null, then `?? 0.2f` makes the whole thing
        // equal to a flat 0.2 — gravity is dropped from the friction equation in that path. Adding parens
        // here ("fixing" the math) makes humans roughly 10x harder to throw than space/'s gameplay was
        // tuned for. Do NOT add parens.
        var partialFrictionComposition = gravity * tileDef.MobFrictionNoInput ?? 0.2f;

        var pressureVector = GetPressureVectorFromTile(atmosComp, tile);
        if (!pressureVector.IsValid())
            return;

        // Remember the vector for visuals/debug.
        tile.LastPressureVector = pressureVector;

        // Apply the strength multiplier BEFORE the small-vector guard so the cvar can scale the deadzone.
        pressureVector *= SpaceWindStrengthMultiplier;

        // Cache magnitude so we don't re-sqrt per-entity.
        var pVecLength = pressureVector.Length();
        if (pVecLength <= MinAtmosForce)
            return;

        if (SpaceWindVisuals && atmosComp.SpaceWindSoundCooldown == 0)
        {
            var location = _mapSystem.GridTileToLocal(gridAtmosphere.Owner, mapGrid, tile.GridIndices);
            var visualEnt = SpawnAtPosition(_spaceWindProto, location);
            XformSystem.SetLocalRotation(visualEnt, pressureVector.ToAngle() - MathF.PI / 2);
        }

        if (pVecLength > 15 && !tile.Hotspot.Valid && atmosComp.SpaceWindSoundCooldown == 0)
        {
            var coordinates = _mapSystem.ToCenterCoordinates(tile.GridIndex, tile.GridIndices);
            var volume = Math.Clamp(pVecLength / atmosComp.SpaceWindSoundDenominator,
                atmosComp.SpaceWindSoundMinVolume,
                atmosComp.SpaceWindSoundMaxVolume);
            _audio.PlayPvs(new SoundPathSpecifier(atmosComp.SpaceWindSound),
                coordinates,
                AudioParams.Default.WithVariation(0.125f).WithVolume(volume));
        }

        if (atmosComp.SpaceWindSoundCooldown++ > atmosComp.SpaceWindSoundCooldownCycles)
            atmosComp.SpaceWindSoundCooldown = 0;

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
    /// Queues a tile for high-pressure-delta processing. The new system doesn't care about direction here —
    /// the Navier-Stokes solver in <see cref="GetPressureVectorFromTile"/> computes flow from neighbour pressures.
    /// </summary>
    private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
    {
        gridAtmosphere.HighPressureDelta.Add(tile);
    }

    /// <summary>
    /// Back-compat overload used by LINDA / Monstermos which still pass a direction + magnitude.
    /// Direction is ignored; the new solver derives it from per-tile pressures.
    /// </summary>
    private void ConsiderPressureDifference(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, AtmosDirection differenceDirection, float difference)
    {
        gridAtmosphere.HighPressureDelta.Add(tile);
    }

    /// <summary>
    /// Decides whether and how hard a single entity gets thrown by the local pressure vector.
    /// Friction is computed as gravity * tileFriction * mass. If the wind force is below static friction
    /// (and the entity isn't already floating or weightless), nothing happens.
    /// Humanoids get a separate multiplier and may be knocked down if the torque threshold is exceeded.
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
        // Claw Command - body weight is derived from the character sliders, so the raw mass would
        // swing footing 0.65x-1.73x purely on build. GetWindResistMass damps that to 1x-1.33x: heavy
        // characters still brace better, light ones are never worse off than default.
        var coefficientOfFriction = partialFrictionComposition * _bodyWeight.GetWindResistMass(uid, physics.Mass);
        if (_standingSystem.IsDown(uid))
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
                    _sharedStunSystem.TryKnockdown(uid, TimeSpan.FromSeconds(SpaceWindKnockdownTime), true);
            }
        }

        if (!alwaysThrow && pVecLength < coefficientOfFriction)
            return;

        // Add the entity's facing as a small bias on top of the wind direction.
        var velocity = XformSystem.GetWorldRotation(uid).ToWorldVec() + pressureVector;

        _throwing.TryThrow(uid, velocity, physics, xform,
            baseThrowSpeed: 1f,
            doSpin: physics.AngularVelocity < SpaceWindMaxAngularVelocity);

        component.LastHighPressureMovementAirCycle = cycle;
        component.Throwing = true;
        component.ThrowingCutoffTarget = _gameTiming.CurTime + component.CutoffTime;
        _activePressures.Add(ent);
    }
}
