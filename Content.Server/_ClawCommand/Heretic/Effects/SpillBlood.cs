using Content.Shared.FixedPoint;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Heretic.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpillBlood : EntityEffectBase<SpillBlood>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Spills target blood.";
}

public sealed partial class SpillBloodEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, SpillBlood>
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private PuddleSystem _puddle = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<SpillBlood> args)
    {
        if (!_solution.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
            return;

        _puddle.TrySpillAt(entity.Owner, bloodSolution.SplitSolution(args.Effect.Amount), out _);
    }
}
