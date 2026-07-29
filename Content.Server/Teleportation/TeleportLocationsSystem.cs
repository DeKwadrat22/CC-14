using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Content.Shared.Chat;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Warps;
using Content.Shared.Whitelist;

namespace Content.Server.Teleportation;

/// <summary>
/// <inheritdoc cref="SharedTeleportLocationsSystem"/>
/// </summary>
public sealed partial class TeleportLocationsSystem : SharedTeleportLocationsSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleportLocationsComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
    }

    private void OnMapInit(Entity<TeleportLocationsComponent> ent, ref MapInitEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void OnBeforeUiOpen(Entity<TeleportLocationsComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    protected override void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        if (!string.IsNullOrWhiteSpace(ent.Comp.Speech))
        {
            var msg = Loc.GetString(ent.Comp.Speech, ("location", args.PointName));
            _chat.TrySendInGameICMessage(args.Actor, msg, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }

        base.OnTeleportToLocationRequest(ent, ref args);
    }

    // If it's in shared this doesn't populate the points on the UI
    /// <summary>
    ///     Gets the teleport points to send to the BUI
    /// </summary>
    private void UpdateTeleportPoints(Entity<TeleportLocationsComponent> ent)
    {
        ent.Comp.AvailableWarps.Clear();

        // CLAW COMMAND - CentComm is a far-off end-game station; its warp points must never be offered as
        // teleport-scroll destinations (e.g. the wizard scroll should land you on the real station, not CC).
        var centcommMaps = GetCentcommMapUids();

        var allEnts = AllEntityQuery<WarpPointComponent, TransformComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp, out var xform))
        {
            if (_whitelist.IsWhitelistPass(warpPointComp.Blacklist, warpEnt) || string.IsNullOrWhiteSpace(warpPointComp.Location))
                continue;

            if (xform.MapUid is { } map && centcommMaps.Contains(map))
                continue;

            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.Location, GetNetEntity(warpEnt)));
        }

        Dirty(ent);
    }

    // CLAW COMMAND - map entity of every station's CentComm, gathered from StationCentcommComponent, so
    // warp points sitting on a CentComm map can be filtered out of teleport/warp destination lists.
    private HashSet<EntityUid> GetCentcommMapUids()
    {
        var maps = new HashSet<EntityUid>();
        var query = AllEntityQuery<StationCentcommComponent>();
        while (query.MoveNext(out var cc))
        {
            if (cc.MapEntity is { } map)
                maps.Add(map);
        }

        return maps;
    }
}
