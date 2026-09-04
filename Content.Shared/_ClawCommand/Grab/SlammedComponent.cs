using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Grab;

/// <summary>
///     Sits on somebody for a short window after they have been slammed into a table, wall or other
///     solid object. While it is on them they are still seeing stars, so a shove has a good chance of
///     dropping them flat instead of just pushing them back.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlammedComponent : Component
{
    /// <summary>
    ///     When the window closes. Set from the knockdown duration of the slam that caused it.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan Until;

    /// <summary>
    ///     Chance that a shove landed inside the window paralyzes instead of doing its usual thing.
    /// </summary>
    [DataField]
    public float ParalyzeChance = 0.35f;

    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(3);
}
