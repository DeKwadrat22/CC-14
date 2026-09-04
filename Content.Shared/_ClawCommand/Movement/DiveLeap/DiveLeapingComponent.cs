using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Movement.DiveLeap;

/// <summary>
///     Claw Command - Present only while a dive-leap is actually in the air. Removed on landing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiveLeapingComponent : Component
{
    /// <summary>
    ///     Direction the leap was launched in, normalised. Steering is measured against this, so it
    ///     never drifts with the current velocity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 LaunchDirection;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    /// <summary>
    ///     Accumulated steer, in radians, signed. Clamped against
    ///     <see cref="DiveLeaperComponent.MaxSteerAngle"/> so a whole leap of side input still only
    ///     bends the arc so far.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SteerAngle;

    /// <summary>
    ///     Fixtures we stripped MidImpassable from, so landing can put back exactly what it took.
    ///     Networked rather than recomputed: if the leap ends through an unusual path, the restore
    ///     still knows precisely which fixtures it owns.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> ChangedFixtures = new();

    /// <summary>
    ///     Whether we put the entity into the horizontal pose ourselves. False if it was somehow
    ///     already lying down, in which case landing must not stand it back up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AppliedHorizontal;
}
