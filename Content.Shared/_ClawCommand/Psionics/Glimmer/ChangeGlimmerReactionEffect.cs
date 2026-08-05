using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReactionEffects;

/// <summary>
///     Pushes the station's glimmer value up or down when the reagent is metabolised.
/// </summary>
/// <remarks>
/// Claw Command - the effect DATA lives in Content.Shared even though the namespace says Server, because
/// reaction prototypes are parsed by the client as well. A server-only effect type makes those prototypes
/// fail to load client-side, which stops rounds starting. The behaviour lives server-side, in
/// ChangeGlimmerEntityEffectSystem.
/// </remarks>
public sealed partial class ChangeGlimmerReactionEffect : EntityEffectBase<ChangeGlimmerReactionEffect>
{
    /// <summary>
    ///     Added to glimmer when reaction occurs. Negative values drain glimmer.
    /// </summary>
    [DataField("count")]
    public int Count = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-glimmer-reaction-effect", ("chance", Probability),
            ("count", Count));
}
