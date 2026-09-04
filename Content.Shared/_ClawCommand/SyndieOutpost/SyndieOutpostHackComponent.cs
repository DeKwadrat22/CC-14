using Robust.Shared.GameObjects;

namespace Content.Shared._ClawCommand.SyndieOutpost;

/// <summary>
/// Marks a device on a syndicate outpost for a chance to hack into station systems on spawn.
/// On MapInit, rolls probability - if successful, boosts wireless range and bypasses station network limits.
/// </summary>
[RegisterComponent]
public sealed partial class SyndieOutpostHackComponent : Component
{
    /// <summary>
    /// Probability (0-1) that this device successfully hacks into station systems.
    /// </summary>
    [DataField]
    public float HackChance { get; set; } = 0.5f;

    /// <summary>
    /// Wireless range to set if hack succeeds.
    /// </summary>
    [DataField]
    public int HackedRange { get; set; } = 9999;

    /// <summary>
    /// Whether the hack succeeded (determined once on map init).
    /// </summary>
    [DataField]
    public bool HackSucceeded { get; set; }
}
