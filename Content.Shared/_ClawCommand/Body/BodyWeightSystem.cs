using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Drunk;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._ClawCommand.Body;

/// <summary>
///     Claw Command - Gives the height and width sliders physical consequences.
///
///     Before this the two sliders were purely cosmetic: a 2-metre character and a 1.5-metre one
///     shoved each other around identically, ate the same amount and drank the same amount. This
///     turns the build into a weight (see <see cref="BodyWeight"/>) and feeds that weight into the
///     systems where body mass plausibly matters.
///
///     Most of it goes through the physics fixture, because mass is already the shared input to
///     spacewind throws, the carrying system's mass contest, the grab ladder's escape odds and
///     ordinary shoving. Setting the fixture density correctly gets all of those at once and keeps
///     them consistent with each other, rather than bolting a separate weight multiplier onto each.
///
///     The remaining effects - health, metabolism, stomach capacity and alcohol tolerance - have no
///     common input, so each is applied on its own with its own influence fraction. None of them run
///     at full strength: a cosmetic slider that hands out a third again as much health would stop
///     being cosmetic very quickly. See the Influence fields on <see cref="BodyWeightComponent"/>.
/// </summary>
public sealed partial class BodyWeightSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private Shared.Body.BodySystem _body = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    ///     A mob fixture much past half a tile starts snagging on doorframes and corners, which is a
    ///     far worse experience than a broad character not quite feeling broad enough.
    /// </summary>
    private const float MaxCollisionRadius = 0.45f;

    public override void Initialize()
    {
        base.Initialize();

        // No MapInit subscription here on purpose. HumanoidProfileSystem already owns
        // (HumanoidProfileComponent, MapInitEvent), and Robust throws on a second subscription to
        // the same component/event pair. That system calls RefreshWeight directly instead, which
        // also guarantees weight is applied after the sprite scale rather than racing it.
        SubscribeLocalEvent<BodyWeightComponent, SharedDrunkSystem.DrunkEvent>(OnDrunk);
    }

    /// <summary>
    ///     Recomputes weight from the entity's current height and width and re-applies every effect.
    ///     Safe to call repeatedly - each application works forward from the captured prototype
    ///     baseline rather than compounding on the previous result.
    /// </summary>
    public void RefreshWeight(Entity<HumanoidProfileComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return;

        if (!ProtoMan.TryIndex(ent.Comp.Species, out var species))
            return;

        var weight = EnsureComp<BodyWeightComponent>(ent);

        weight.Scale = BodyWeight.GetScale(ent.Comp.Height, ent.Comp.Width);
        weight.Weight = BodyWeight.GetWeight(species.BaseWeight, ent.Comp.Height, ent.Comp.Width);
        weight.HeightCm = BodyWeight.GetHeightCm(species.BaseHeightCm, ent.Comp.Height);
        Dirty(ent.Owner, weight);

        CaptureBaselines(ent.Owner, weight);

        // Predicted, because mass feeds movement and pressure throws.
        ApplyPhysics(ent.Owner, weight, ent.Comp.Width);

        // The rest is bookkeeping the client has no reason to simulate, and the organ lookups below
        // only resolve properly on the server anyway.
        if (_net.IsClient)
            return;

        ApplyHealth(ent.Owner, weight);
        ApplyMetabolism(ent.Owner, weight);
        ApplyStomachCapacity(ent.Owner, weight);
    }

    /// <summary>
    ///     Multiplier for an effect that only takes <paramref name="influence"/> of the weight
    ///     difference. At influence 1 this is the raw weight scale; at 0 it is always 1.
    /// </summary>
    private static float Influence(float scale, float influence)
    {
        return 1f + (scale - 1f) * influence;
    }

    private void CaptureBaselines(EntityUid uid, BodyWeightComponent weight)
    {
        if (weight.CapturedBaselines)
            return;

        weight.CapturedBaselines = true;

        if (TryComp<FixturesComponent>(uid, out var fixtures)
            && fixtures.Fixtures.TryGetValue("fix1", out var fixture))
        {
            weight.BaseDensity = fixture.Density;
            weight.BaseRadius = fixture.Shape.Radius;
        }

        if (TryComp<HungerComponent>(uid, out var hunger))
            weight.BaseHungerDecay = hunger.BaseDecayRate;

        if (TryComp<ThirstComponent>(uid, out var thirst))
            weight.BaseThirstDecay = thirst.BaseDecayRate;

        if (TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            foreach (var (damage, state) in thresholds.Thresholds)
            {
                weight.BaseThresholds[state] = damage;
            }
        }
    }

    /// <summary>
    ///     Sets the fixture so the mob actually weighs what the sliders say, and widens the hitbox a
    ///     little for broad characters.
    /// </summary>
    private void ApplyPhysics(EntityUid uid, BodyWeightComponent weight, float width)
    {
        if (weight.BaseRadius <= 0f
            || !TryComp<FixturesComponent>(uid, out var fixtures)
            || !fixtures.Fixtures.TryGetValue("fix1", out var fixture)
            || fixture.Shape is not PhysShapeCircle)
        {
            return;
        }

        var radius = Math.Min(weight.BaseRadius * Influence(width, weight.CollisionInfluence), MaxCollisionRadius);

        // Mass is density times area, so widening the circle would inflate the mass on its own.
        // Divide that back out so the final mass is exactly BaseWeight * Scale regardless of what
        // the collision radius ended up being.
        var areaRatio = weight.BaseRadius / radius;
        var density = weight.BaseDensity * weight.Scale * areaRatio * areaRatio;

        _physics.SetRadius(uid, "fix1", fixture, fixture.Shape, radius, fixtures);
        _physics.SetDensity(uid, "fix1", fixture, density, manager: fixtures);
    }

    /// <summary>
    ///     Heavier characters take a little more punishment before dropping.
    /// </summary>
    private void ApplyHealth(EntityUid uid, BodyWeightComponent weight)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        var factor = Influence(weight.Scale, weight.HealthInfluence);

        foreach (var (state, baseDamage) in weight.BaseThresholds)
        {
            _thresholds.SetMobStateThreshold(uid, baseDamage * factor, state, thresholds);
        }
    }

    /// <summary>
    ///     Bigger bodies burn through food and water faster. Both systems recompute ActualDecayRate
    ///     from BaseDecayRate every update, so setting the base is enough.
    /// </summary>
    private void ApplyMetabolism(EntityUid uid, BodyWeightComponent weight)
    {
        var factor = Influence(weight.Scale, weight.MetabolismInfluence);

        if (TryComp<HungerComponent>(uid, out var hunger) && weight.BaseHungerDecay > 0f)
        {
            hunger.BaseDecayRate = weight.BaseHungerDecay * factor;
            Dirty(uid, hunger);
        }

        if (TryComp<ThirstComponent>(uid, out var thirst) && weight.BaseThirstDecay > 0f)
        {
            thirst.BaseDecayRate = weight.BaseThirstDecay * factor;
            Dirty(uid, thirst);
        }
    }

    /// <summary>
    ///     ...and can hold more before they are full. The stomach solution's capacity is what
    ///     actually refuses the next bite, so that is what gets scaled.
    /// </summary>
    private void ApplyStomachCapacity(EntityUid uid, BodyWeightComponent weight)
    {
        if (!TryComp<Shared.Body.BodyComponent>(uid, out var body))
            return;

        if (!_body.TryGetOrgansWithComponent<StomachComponent>((uid, body), out var stomachs))
            return;

        var factor = Influence(weight.Scale, weight.CapacityInfluence);

        foreach (var stomach in stomachs)
        {
            if (stomach.Comp.Solution is not { } solution)
                continue;

            if (!weight.BaseStomachCapacity.TryGetValue(solution.Owner, out var baseCapacity))
            {
                baseCapacity = solution.Comp.Solution.MaxVolume;
                weight.BaseStomachCapacity[solution.Owner] = baseCapacity;
            }

            _solution.SetCapacity(solution, baseCapacity * factor);
        }
    }

    /// <summary>
    ///     Mass is most of what real alcohol tolerance is, so a heavy character sobers up sooner off
    ///     the same drink and a slight one goes down faster.
    /// </summary>
    private void OnDrunk(Entity<BodyWeightComponent> ent, ref SharedDrunkSystem.DrunkEvent args)
    {
        var resistance = Influence(ent.Comp.Scale, ent.Comp.AlcoholInfluence);
        if (resistance <= 0f)
            return;

        args.Duration /= resistance;
    }
}
