using Content.Server.Abilities.Psionics;
using Content.Shared.Abilities.Psionics;
using Content.Shared.EntityEffects;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    ///     Applies <see cref="ChemRemovePsionic"/>. The effect declaration itself lives in Content.Shared
    ///     (PsionicEntityEffects.cs) because reagent prototypes are parsed client-side too; only the
    ///     behaviour is server-side.
    /// </summary>
    public sealed partial class ChemRemovePsionicEntityEffectSystem : EntityEffectSystem<PsionicComponent, ChemRemovePsionic>
    {
        [Dependency] private PsionicAbilitiesSystem _psionicAbilities = default!;

        protected override void Effect(Entity<PsionicComponent> entity, ref EntityEffectEvent<ChemRemovePsionic> args)
        {
            _psionicAbilities.MindBreak(entity.Owner);
        }
    }
}
