namespace Content.Shared._ClawCommand.InteractionVerbs.Events;

/// <summary>
///     Raised directly on the performer of the interaction verb and on its target to determine if it should be allowed.
///     Note that this is raised if and only if verb's own CanPerform check returns true.
/// </summary>
[ByRefEvent]
public sealed class InteractionVerbAttemptEvent(InteractionVerbPrototype verbProto, InteractionArgs verbArgs) : CancellableEntityEventArgs
{
    public bool Handled { get; set; } = false;

    public InteractionVerbPrototype Proto => verbProto;
    public InteractionArgs Args => verbArgs;
}
