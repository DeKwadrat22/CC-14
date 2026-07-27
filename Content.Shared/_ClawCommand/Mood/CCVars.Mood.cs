using Robust.Shared.Configuration;

// ReSharper disable once CheckNamespace
namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether the mood system is enabled at all. When disabled, no moodlets are tracked and no
    ///     mood-driven effects (speed, alerts, overlays, crit thresholds) are applied.
    /// </summary>
    public static readonly CVarDef<bool> MoodEnabled =
        CVarDef.Create("mood.enabled", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Whether a mood above Neutral grants a movement speed bonus.
    /// </summary>
    public static readonly CVarDef<bool> MoodIncreasesSpeed =
        CVarDef.Create("mood.increases_speed", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Whether a mood below Neutral applies a movement speed penalty.
    /// </summary>
    public static readonly CVarDef<bool> MoodDecreasesSpeed =
        CVarDef.Create("mood.decreases_speed", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Whether mood modifies an entity's critical damage threshold. Off by default; this is a large
    ///     balance lever and interacts poorly with anything else that rewrites mob thresholds.
    /// </summary>
    public static readonly CVarDef<bool> MoodModifiesThresholds =
        CVarDef.Create("mood.modify_thresholds", false, CVar.SERVER);
}
