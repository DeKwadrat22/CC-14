using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Permanently strips a psion of their powers. This is what Mindbreaker Toxin does.
/// </summary>
/// <remarks>
/// Claw Command - the effect DATA lives in Content.Shared even though the namespace says Server, because
/// reagent and reaction prototypes are parsed by the client as well (the guidebook reads them). A
/// server-only effect type makes those prototypes fail to load client-side, which stops rounds starting.
/// The behaviour still lives server-side, in the matching EntityEffectSystem.
/// </remarks>
[UsedImplicitly]
public sealed partial class ChemRemovePsionic : EntityEffectBase<ChemRemovePsionic>
{
    /// <summary>
    ///     Mindbreaking is all-or-nothing, so a partial dose must not partially mindbreak anyone.
    /// </summary>
    public override bool Scaling => false;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-remove-psionic", ("chance", Probability));
}

/// <summary>
/// Grants the drinker one extra roll at obtaining psionic powers. This is what Lotophagoi Oil does.
/// </summary>
/// <remarks>Claw Command - see the note on <see cref="ChemRemovePsionic"/> for why this is in Shared.</remarks>
[UsedImplicitly]
public sealed partial class ChemRerollPsionic : EntityEffectBase<ChemRerollPsionic>
{
    /// <summary>
    /// Reroll multiplier.
    /// </summary>
    [DataField("bonusMultiplier")]
    public float BonusMuliplier = 1f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-reroll-psionic", ("chance", Probability));
}
