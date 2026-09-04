using Content.Shared._ClawCommand.InteractionVerbs;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     An action that raises an event on the target or the user. Made for interop with systems that rely on events.
/// </summary>
[Serializable]
public sealed partial class RaiseEventAction : InteractionAction
{
    /// <summary>
    ///     The event to raise. Must be serializable because it will be copied before being raised.
    /// </summary>
    /// <remarks>
    ///     If this is a handled event, the result of the action is whether the event was handled.
    ///     Likewise, if it's cancellable, the result is whether it was not cancelled.
    /// </remarks>
    [DataField("event", required: true)]
    public EntityEventArgs? EventData;

    [DataField]
    public bool Broadcast = false;

    /// <summary>
    ///     If true, the event will be raised on the user. Otherwise, it will be raised on the target.
    /// </summary>
    [DataField]
    public bool OnUser = false;

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return true;
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        if (EventData is null)
            return false;

        var raisedEvent = deps.Serialization.CreateCopy(EventData, notNullableOverride: true);
        deps.EntMan.EventBus.RaiseLocalEvent(OnUser ? ctx.User : ctx.Target, raisedEvent, Broadcast);

        if (raisedEvent is HandledEntityEventArgs handledEv)
            return handledEv.Handled;
        if (raisedEvent is CancellableEntityEventArgs cancelEv)
            return !cancelEv.Cancelled;

        return true;
    }
}
