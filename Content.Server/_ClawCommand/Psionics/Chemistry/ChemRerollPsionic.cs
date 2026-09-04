using Content.Server.Psionics;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    ///     Applies <see cref="ChemRerollPsionic"/>. The effect declaration itself lives in Content.Shared
    ///     (PsionicEntityEffects.cs) because reagent prototypes are parsed client-side too.
    ///     Gated on <see cref="MobStateComponent"/> rather than PsionicComponent, because the whole point is
    ///     to roll for powers on someone who is not psionic yet.
    /// </summary>
    public sealed partial class ChemRerollPsionicEntityEffectSystem : EntityEffectSystem<MobStateComponent, ChemRerollPsionic>
    {
        [Dependency] private PsionicsSystem _psionics = default!;

        protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<ChemRerollPsionic> args)
        {
            _psionics.RerollPsionics(entity.Owner, bonusMuliplier: args.Effect.BonusMuliplier);
        }
    }
}
