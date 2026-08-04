using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Movement.DiveLeap;

/// <summary>
///     Claw Command - Lets an entity dive-leap by hitting the lie-down key while sprinting.
///
///     Holds the tuning only. The live state of a leap in progress lives on
///     <see cref="DiveLeapingComponent"/>, which is added for the duration and removed on landing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiveLeaperComponent : Component
{
    /// <summary>
    ///     How long the leap lasts. Short on purpose - this is a dive, not flight.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0.55);

    /// <summary>
    ///     Speed along the launch direction, in tiles/second. Sprint speed is about 4.5, so this
    ///     leaves the leap feeling like a committed lunge rather than a faster way to travel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 6.5f;

    /// <summary>
    ///     How hard WASD can push the leap sideways mid-air, in tiles/second of sideways velocity at
    ///     full deflection. Deliberately small next to <see cref="Speed"/>: you committed to a
    ///     direction when you jumped and you only get to nudge it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SteerSpeed = 1.6f;

    /// <summary>
    ///     Hard cap on how far the leap may be bent away from its launch direction, total. Even
    ///     holding a perpendicular key for the whole leap cannot exceed this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle MaxSteerAngle = Angle.FromDegrees(35);

    /// <summary>
    ///     The angle the body lies at during the dive.
    ///
    ///     Defaults to 90, the same value <see cref="Content.Shared.Rotation.RotationVisualsComponent"/>
    ///     uses for an ordinary lie-down, so a dive is posed exactly like lying down and flows into
    ///     the prone landing without a flip. Set it to -90 to lie on the opposite shoulder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle PoseOffset = Angle.FromDegrees(90);

    /// <summary>
    ///     Peak height of the visual arc, in tiles. Client-side sprite offset only - the entity's
    ///     actual position is flat, because the game is top-down and there is no Z axis to move on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ArcHeight = 0.55f;

    /// <summary>
    ///     Minimum gap between leaps, measured from landing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1.6);

    /// <summary>
    ///     Earliest the next leap may start. Networked so the client predicts the refusal instead of
    ///     starting a leap the server will reject.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextLeap;

    /// <summary>
    ///     How recently the entity must have been running for a leap to count as a sprinting leap.
    ///
    ///     This exists because Shift is both the run key and a modifier key. Pressing another bound
    ///     key while Shift is held makes the input system treat Shift as that key's modifier, which
    ///     drops the Walk button state and decelerates the mob to a walk on the very frame the leap
    ///     is requested - the diagnostics caught exactly that, refusing at speed 2.42 when walking
    ///     is 2.5. Sampling sprint at the instant of the keypress is therefore the one moment it is
    ///     guaranteed to read wrong. A short memory of "was running a moment ago" is both more
    ///     robust and closer to what the player means.
    /// </summary>
    /// <remarks>
    ///     Sized from measurement: refusals were logged at 0.27s and 0.28s past the last run, so a
    ///     0.25s window missed them by a hair. Safe to widen now that the run threshold is anchored
    ///     to walk speed - a walking entity never stamps LastSprintTime at all, so this window can only
    ///     ever extend from a genuine run rather than letting a stroll qualify.
    /// </remarks>
    [DataField]
    public TimeSpan SprintGrace = TimeSpan.FromSeconds(0.35);

    /// <summary>
    ///     Last time this entity was observed moving at running speed. Not networked - both sides
    ///     derive it from the same simulated velocity, so they agree without any traffic.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastSprintTime = TimeSpan.MinValue;



    /// <summary>
    ///     Played at launch. Left null so the system falls back to the entity's own footstep sound,
    ///     which is what makes the leap sound like the sprint it came out of.
    /// </summary>
    [DataField]
    public SoundSpecifier? LaunchSound;

    /// <summary>
    ///     Extra volume on the launch sound, on top of the sprint footstep modifier.
    /// </summary>
    [DataField]
    public float LaunchVolume = 2f;
}
