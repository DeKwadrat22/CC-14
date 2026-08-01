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
    /// <remarks>
    /// Volume is in decibels, so -6 dB is half the amplitude of the stock sample - the puff fires every
    /// time anyone stops holding Walk, so it gets grating fast at full volume.
    /// </remarks>
    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_ClawCommand/Effects/Sprinting/sprint_puff.ogg")
    {
        Params = AudioParams.Default.WithVolume(-6f),
    };

    /// <summary>
    /// How far away the puff can be heard, in tiles. Beyond this - or through a wall - a listener hears
    /// nothing, so the sound only carries as far as they can actually see the sprinter.
    /// </summary>
    [DataField]
    public float SoundRange = 10f;

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
