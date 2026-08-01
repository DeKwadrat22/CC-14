using Robust.Shared.Configuration;

namespace Content.Shared._ClawCommand.CCVar;

public sealed partial class ClawCCVars
{
    /// <summary>
    ///     A direct multiplier on how violent space wind is.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindStrengthMultiplier =
        CVarDef.Create("atmos.space_wind_strength_multiplier", 1f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum Force (in Newtons) that may be applied to an object by atmospheric pressure differences.
    ///     Useful to prevent clipping through objects.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindMaxForce =
        CVarDef.Create("atmos.space_wind_max_force", 200f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum angular velocity that space wind can spin objects at while throwing them. This one is mostly for fun.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindMaxAngularVelocity =
        CVarDef.Create("atmos.space_wind_max_angular_velocity", 3f, CVar.SERVERONLY);

    /// <summary>
    ///     The amount of time (in seconds) for space wind to knock down a player character if they are subjected to space wind.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindKnockdownTime =
        CVarDef.Create("atmos.space_wind_knockdown_time", 0.75f, CVar.SERVERONLY);

    /// <summary>
    ///     A multiplier on the amount of force applied to Humanoid entities, as tracked by HumanoidAppearanceComponent.
    ///     Applied after all other checks, and applies to both throwing force and how easy it is for an entity to be thrown.
    /// </summary>
    public static readonly CVarDef<float> AtmosHumanoidThrowMultiplier =
        CVarDef.Create("atmos.humanoid_throw_multiplier", 2f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether Space Wind is allowed to attempt to knock down player characters.
    /// </summary>
    public static readonly CVarDef<bool> SpaceWindAllowKnockdown =
        CVarDef.Create("atmos.space_wind_allow_knockdown", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether Space Wind will create subtle visual indicators for the presence of air currents.
    /// </summary>
    public static readonly CVarDef<bool> SpaceWindVisuals =
        CVarDef.Create("atmos.space_wind_visuals", true, CVar.SERVERONLY);
}
