using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Ghost;

public sealed partial class GhostReturnToRoundSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private GameTicker _ticker = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<GhostReturnToRoundRequest>(OnGhostReturnToRoundRequest);
    }

    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (uid == null)
            return;

        var connectedClient = args.SenderSession.Channel;
        var userId = args.SenderSession.UserId;

        TryGhostReturnToRound(uid.Value, connectedClient, userId, out var message, out var wrappedMessage);

        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            connectedClient,
            Color.Red);
    }

    private void TryGhostReturnToRound(EntityUid uid, INetChannel connectedClient, NetUserId userId, out string message, out string wrappedMessage)
    {
        var maxPlayers = _cfg.GetCVar(CCVars.GhostRespawnMaxPlayers);
        if (_playerManager.PlayerCount >= maxPlayers)
        {
            message = Loc.GetString("ghost-respawn-max-players", ("players", maxPlayers));
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            return;
        }

        // Vulpstation - who on EE let this exploit through?
        if (!TryComp<GhostComponent>(uid, out var ghost))
        {
            message = wrappedMessage = "sus";
            return;
        }

        var deathTime = ghost.TimeOfDeath;
        var timeUntilRespawn = _cfg.GetCVar(CCVars.GhostRespawnTime);
        // _ClawCommand: TimeOfDeath is stamped with _gameTiming.RealTime (see
        // GhostSystem.OnGhostStartup), so the elapsed comparison must use
        // RealTime too. Using CurTime here meant the displayed wait grew with
        // server uptime — a 10h-old server would tell a fresh ghost to wait
        // "~10h" instead of the 15 min the cvar specifies.
        var timePast = (_gameTiming.RealTime - deathTime).TotalMinutes;
        if (timePast >= timeUntilRespawn)
        {
            _playerManager.TryGetSessionById(userId, out var targetPlayer);

            if (targetPlayer != null)
                _ticker.Respawn(targetPlayer);

            _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", connectedClient.UserName))}");

            message = Loc.GetString("ghost-respawn-window-rules-footer");
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

            return;
        }

        message = Loc.GetString("ghost-respawn-time-left", ("time", (int)(timeUntilRespawn - timePast)));
        wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
    }
}
