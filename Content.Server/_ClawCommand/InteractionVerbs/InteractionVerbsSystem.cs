using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._ClawCommand.InteractionVerbs;

public sealed partial class InteractionVerbsSystem : SharedInteractionVerbsSystem
{
    [Dependency] private IChatManager _chatMgr = default!;
    [Dependency] private SharedInteractionSystem _interactionSys = default!;

    private EntityQuery<OccluderComponent> _occluderLookup;

    public override void Initialize()
    {
        base.Initialize();
        _occluderLookup = GetEntityQuery<OccluderComponent>();
    }

    private Color DeriveColor(PopupType popupKind) => popupKind switch
    {
        // These are all hardcoded on client-side, so we have to improvise
        PopupType.LargeCaution or PopupType.MediumCaution or PopupType.SmallCaution => Color.Red,
        PopupType.Medium or PopupType.Small => Color.LightGray,
        _ => Color.White
    };

    private bool IsTargetVisible(EntityUid observer, EntityUid observed, float range)
    {
        // TODO: InRangeUnobstructed has a pretty high performance cost and is not intended to be used like that.
        // We should see if we can move this to client side later, aka make the client check if the target is visible for it.
        return _interactionSys.InRangeUnobstructed(
            observer, observed, range,
            CollisionGroup.Opaque,
            uid => !_occluderLookup.TryComp(uid, out var occComp) || !occComp.Enabled, // We ignore all entities that do not occlude light
            false);
    }

    protected override void SendChatLog(string message, EntityUid source, Filter filter, InteractionPopupPrototype popup, bool clip)
    {
        if (filter.Count <= 0)
            return;

        var clr = popup.LogColor ?? DeriveColor(popup.PopupType);
        var formattedMsg = message; // TODO: custom chat wraps maybe?

        // Exclude entities who cannot directly see the target of the popup. TODO this may have a high performance cost - although whispers do the same.
        // We only do this if the popup has to be logged into chat since that has some gameplay implications.
        if (clip && popup.DoClipping)
            filter.RemoveWhereAttachedEntity(ent => !IsTargetVisible(ent, source, popup.VisibilityRange));

        if (filter.Count == 1)
            _chatMgr.ChatMessageToOne(popup.LogChannel, message, formattedMsg, source, false, filter.Recipients.First().Channel, clr);
        else
            _chatMgr.ChatMessageToManyFiltered(filter, popup.LogChannel, message, formattedMsg, source, false, false, clr);
    }
}
