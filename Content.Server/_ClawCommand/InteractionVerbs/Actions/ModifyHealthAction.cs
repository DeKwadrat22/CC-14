using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Serialization;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ModifyHealthAction : InteractionAction
{
    [DataField(required: true)] public DamageSpecifier Damage = default!;
    [DataField] public bool IgnoreResistance = false;

    [DataField] public InteractionVerbPrototype.RangeSpecifier RandomFactor = new() { Min = 0.75f, Max = 1.25f };

    public override bool IsAllowed(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return deps.EntMan.HasComponent<DamageableComponent>(ctx.Target);
    }

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        // TODO: check if container supports this kind of damage?
        return true;
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var scaledDmg = Damage * RandomFactor.Random(deps.Random);
        return deps.EntMan.System<DamageableSystem>()
            .TryChangeDamage(ctx.Target, scaledDmg, IgnoreResistance, origin: ctx.User);
    }
}
