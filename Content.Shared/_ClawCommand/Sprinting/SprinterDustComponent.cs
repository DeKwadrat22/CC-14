// Claw Command: lightweight sprint-dust visual marker. Goob's full Sprinting system
// (sprint key + stamina drain + speed boost + collision knockdown) depends on systems we don't
// ship (Sandevistan, EinsteinEngines Flight, FixedPoint shims), and our stamina drain API is a
// no-op stub anyway. Instead this component is a pure visual hook: when an entity moves fast
// enough on the ground, the client periodically spawns a dust cloud beneath it.

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Sprinting;

[RegisterComponent, NetworkedComponent]
public sealed partial class SprinterDustComponent : Component
{
    /// <summary>
    /// Minimum seconds between dust spawns while sprinting.
    /// </summary>
    [DataField]
    public TimeSpan StepInterval = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// Entity prototype spawned beneath the sprinter each step.
    /// </summary>
    [DataField]
    public EntProtoId StepAnimation = "SprintDustSmall";

    /// <summary>
    /// One-shot puff sound played when the entity transitions from not-sprinting to sprinting.
    /// Ported from Goob's SprinterComponent.SprintStartupSound.
    /// </summary>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_ClawCommand/Effects/Sprinting/sprint_puff.ogg");

    /// <summary>
    /// Tracks the last step time. Client-only state — not networked.
    /// </summary>
    public TimeSpan LastStep = TimeSpan.Zero;

    /// <summary>
    /// Tracks whether the entity was sprinting on the previous frame, so the system can detect
    /// the not-sprinting → sprinting transition and play the start sound exactly once. Client-only.
    /// </summary>
    public bool WasSprinting;
}
