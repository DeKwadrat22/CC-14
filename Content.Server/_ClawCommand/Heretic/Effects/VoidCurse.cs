using Content.Server._Goobstation.Heretic.EntitySystems.PathSpecific;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Heretic.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class VoidCurse : EntityEffectBase<VoidCurse>
{
    [DataField]
    public int Stacks = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Inflicts void curse.";
}

public sealed partial class VoidCurseEntityEffectSystem : EntityEffectSystem<MobStateComponent, VoidCurse>
{
    [Dependency] private VoidCurseSystem _voidCurse = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<VoidCurse> args)
    {
        _voidCurse.DoCurse(entity.Owner, args.Effect.Stacks);
    }
}
