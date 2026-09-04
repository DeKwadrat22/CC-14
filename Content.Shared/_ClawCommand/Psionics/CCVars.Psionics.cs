using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
///     Claw Command - CVars for the ported psionics / glimmer systems.
///     Kept in a _ClawCommand-pathed partial so the upstream CCVars.cs stays untouched.
/// </summary>
public sealed partial class CCVars
{
    /// <summary>
    ///     Whether glimmer is enabled. When false the glimmer value is pinned at 0, which
    ///     disables every glimmer event and glimmer-reactive structure without unloading them.
    /// </summary>
    public static readonly CVarDef<bool> GlimmerEnabled =
        CVarDef.Create("glimmer.enabled", true, CVar.REPLICATED);

    /// <summary>
    ///     Passive glimmer drain per second.
    ///     Note that this is randomized and this is an average value.
    /// </summary>
    public static readonly CVarDef<float> GlimmerLostPerSecond =
        CVarDef.Create("glimmer.passive_drain_per_second", 0.1f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether random rolls for psionics are allowed.
    ///     Guaranteed psionics (traits, the Mantis job, innate powers) still go through.
    /// </summary>
    public static readonly CVarDef<bool> PsionicRollsEnabled =
        CVarDef.Create("psionics.rolls_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     When mindbroken, permanently eject the player from their own body and turn their character into an NPC.
    ///     Off by default: this is effectively a murder that leaves a walking body behind.
    /// </summary>
    public static readonly CVarDef<bool> ScarierMindbreaking =
        CVarDef.Create("psionics.scarier_mindbreaking", false, CVar.SERVERONLY);
}
