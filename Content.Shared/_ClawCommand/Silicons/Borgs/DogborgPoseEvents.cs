using Content.Shared.Actions;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// Action event raised when the dogborg's "sit" action button is pressed.
/// Toggles the Sit pose via <see cref="DogborgPoseSystem"/>.
/// </summary>
public sealed partial class DogborgSitActionEvent : InstantActionEvent;

/// <summary>
/// Action event raised when the dogborg's "rest" action button is pressed.
/// Toggles the Rest pose via <see cref="DogborgPoseSystem"/>.
/// </summary>
public sealed partial class DogborgRestActionEvent : InstantActionEvent;

/// <summary>
/// Action event raised when the dogborg's "belly-up" action button is pressed.
/// Toggles the BellyUp pose via <see cref="DogborgPoseSystem"/>.
/// </summary>
public sealed partial class DogborgBellyUpActionEvent : InstantActionEvent;
