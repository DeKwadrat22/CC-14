// Claw Command: lightweight sprint-dust visual marker. Goob's full Sprinting system
// (sprint key + stamina drain + speed boost + collision knockdown) depends on systems we don't
// ship (Sandevistan, EinsteinEngines Flight, FixedPoint shims), and our stamina drain API is a
// no-op stub anyway. Instead this component is a pure visual hook: when an entity moves fast
// enough on the ground, the client periodically spawns a dust cloud beneath it.

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Sprinting;

[RegisterComponent, NetworkedComponent]
public sealed partial class SprinterDustComponent : Component
{
    /// <summary>
    /// Minimum world-units-per-second the entity must be moving for dust to spawn.
    /// Slightly under default run speed (~3.0) so it doesn't kick in on creep movement.
    /// </summary>
    [DataField]
    public float SpeedThreshold = 2.5f;

    /// <summary>
    /// Minimum seconds between dust spawns while moving.
    /// </summary>
    [DataField]
    public TimeSpan StepInterval = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// Entity prototype spawned beneath the sprinter each step.
    /// </summary>
    [DataField]
    public EntProtoId StepAnimation = "SprintDustSmall";

    /// <summary>
    /// Tracks the last step time. Client-only state — not networked.
    /// </summary>
    public TimeSpan LastStep = TimeSpan.Zero;
}
