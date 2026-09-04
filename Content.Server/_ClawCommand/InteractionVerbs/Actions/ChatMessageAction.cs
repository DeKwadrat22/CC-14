using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared._ClawCommand.InteractionVerbs;
using Robust.Shared.Serialization;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     Makes the target or the user to send a chat message. <br/><br/>
///
///     Messages are locale-based, their keys follow the form of "interaction-[verb id]-[message loc prefix]-[index]".
///     The index parameter is a random integer from 1 to <see cref="NumMessages"/>. <br/><br/>
///
///     Similarly to interaction verb locales, {$user}, {$target} amd {$used} arguments are passed to the locales retrieved by this action.
/// </summary>
[Serializable]
public sealed partial class ChatMessageAction : InteractionAction
{
    [DataField]
    public int NumMessages = 1;

    [DataField]
    public string MessageLocPrefix = "message";

    [DataField]
    public InGameICChatType ChatType = InGameICChatType.Speak;

    /// <summary>
    ///     If true, makes the target speak. Otherwise, makes the user speak.
    /// </summary>
    [DataField]
    public bool TargetIsSource = true;

    private EntityUid ResolveSpeaker(InteractionArgs ctx) => TargetIsSource ? ctx.Target : ctx.User;

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return deps.EntMan.System<ActionBlockerSystem>().CanSpeak(ResolveSpeaker(ctx));
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var idx = NumMessages <= 1 ? 1 : deps.Random.Next(1, NumMessages + 1);
        var locKey = $"interaction-{proto.ID}-{MessageLocPrefix}-{idx}";

        var usedEntity = ctx.Used ?? EntityUid.Invalid;
        if (!Loc.TryGetString(locKey, out var text, ("user", ctx.User), ("target", ctx.Target), ("used", usedEntity)))
        {
            Logger.GetSawmill("action.chat_message").Error($"No chat message found for interaction {proto.ID}! Loc string: {locKey}.");
            return false;
        }

        var src = ResolveSpeaker(ctx);
        deps.EntMan.System<ChatSystem>().TrySendInGameICMessage(src, text, ChatType, false);

        return true;
    }
}
