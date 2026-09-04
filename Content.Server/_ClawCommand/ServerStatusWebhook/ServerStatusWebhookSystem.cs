using System.Linq;
using System.Text.Json.Nodes;
using Content.Server.AlertLevel;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared.CCVar;
using Content.Shared.AlertLevel;
using Content.Shared.GameTicking;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.ServerStatusWebhook;

/// <summary>
///     Claw Command - Maintains a live-updating Discord message showing current server status and player count,
///     replacing the external wizard-cogs GameServerStatus bot with an internal implementation.
/// </summary>
public sealed partial class ServerStatusWebhookSystem : EntitySystem
{
    [Dependency] private IBaseServer _baseServer = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IGameMapManager _gameMapManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private string _webhookUrl = string.Empty;
    private bool _enabled;
    private float _updateInterval = 60f;

    private WebhookIdentifier _webhookIdentifier;
    private ulong _messageId;
    private ulong _configuredMessageId;
    private TimeSpan _lastUpdateTime;
    private bool _initialized;
    private int _consecutiveFailures;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("discord.serverstatus");

        _cfg.OnValueChanged(CCVars.DiscordServerStatusWebhook, OnWebhookUrlChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordServerStatusEnabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordServerStatusMessageId, OnMessageIdChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.DiscordServerStatusWebhook, OnWebhookUrlChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordServerStatusEnabled, OnEnabledChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordServerStatusMessageId, OnMessageIdChanged);
    }

    private void OnWebhookUrlChanged(string url)
    {
        if (_webhookUrl == url)
            return;

        _webhookUrl = url;
        // Reset state so we re-initialize with the new webhook
        _initialized = false;
        _messageId = _configuredMessageId;
    }

    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;
    }

    private void OnMessageIdChanged(string messageId)
    {
        if (ulong.TryParse(messageId, out var id))
        {
            _configuredMessageId = id;
            _messageId = id;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        var now = _gameTiming.RealTime;
        if (now - _lastUpdateTime < TimeSpan.FromSeconds(_updateInterval))
            return;

        _lastUpdateTime = now;
        SendUpdate();
    }

    private async void SendUpdate()
    {
        try
        {
            // Initialize webhook identifier if needed
            if (!_initialized)
            {
                var webhookData = await _discord.GetWebhook(_webhookUrl);
                if (webhookData == null)
                {
                    _sawmill.Warning("Failed to get webhook data for server status. Is the URL correct?");
                    return;
                }

                _webhookIdentifier = webhookData.Value.ToIdentifier();
                _initialized = true;
            }

            var payload = BuildPayload();

            if (_messageId == 0)
            {
                // Create initial message
                var response = await _discord.CreateMessage(_webhookIdentifier, payload);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var id = JsonNode.Parse(content)?["id"]?.GetValue<string>();
                    if (id != null)
                        _messageId = ulong.Parse(id);

                    _consecutiveFailures = 0;
                    _sawmill.Debug("Created server status message with ID {0}", _messageId);
                }
                else
                {
                    _sawmill.Error("Failed to create server status message: {0}", response.StatusCode);
                }
            }
            else
            {
                // Edit existing message
                var response = await _discord.EditMessage(_webhookIdentifier, _messageId, payload);
                if (response.IsSuccessStatusCode)
                {
                    _consecutiveFailures = 0;
                }
                else if ((int) response.StatusCode == 404)
                {
                    // Message was actually deleted, recreate
                    _sawmill.Warning("Server status message (ID {0}) was deleted, will recreate.", _messageId);
                    _messageId = 0;
                }
                else
                {
                    // Transient error (rate limit, server error, etc.) - don't reset message ID
                    _consecutiveFailures++;
                    _sawmill.Warning("Failed to edit server status message (ID {0}): {1} (failure {2})", _messageId, response.StatusCode, _consecutiveFailures);
                }
            }
        }
        catch (Exception e)
        {
            _consecutiveFailures++;
            _sawmill.Error($"Error updating server status webhook (failure {_consecutiveFailures}):\n{e}");
        }
    }

    private WebhookPayload BuildPayload()
    {
        var gameTicker = EntityManager.System<GameTicker>();
        var sharedTicker = EntityManager.System<SharedGameTicker>();

        var serverName = _baseServer.ServerName;
        var playerCount = _playerManager.PlayerCount;
        var maxPlayers = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        var runLevel = gameTicker.RunLevel;
        var roundId = sharedTicker.RoundId;
        var mapName = _gameMapManager.GetSelectedMap()?.MapName ?? "Unknown";
        // Use Decoy preset if set (matches what the /status endpoint shows publicly)
        var presetProto = gameTicker.Decoy ?? gameTicker.CurrentPreset ?? gameTicker.Preset;
        var preset = presetProto != null ? Loc.GetString(presetProto.ModeTitle) : "Unknown";

        // Build status string with elapsed time if in-round
        var status = runLevel switch
        {
            GameRunLevel.PreRoundLobby => "Lobby",
            GameRunLevel.InRound => FormatInRoundStatus(sharedTicker),
            GameRunLevel.PostRound => "Ending",
            _ => "Unknown"
        };

        var alertLevel = GetAlertLevelDisplay();

        // Color: green for in-round, blue for lobby, yellow for ending
        var color = runLevel switch
        {
            GameRunLevel.PreRoundLobby => 0x3498DB, // Blue
            GameRunLevel.InRound => 0x2ECC71,       // Green
            GameRunLevel.PostRound => 0xF1C40F,     // Yellow
            _ => 0x95A5A6                            // Gray
        };

        return new WebhookPayload
        {
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = serverName,
                    Description =
                        $"**Players:** {playerCount}/{maxPlayers}\n" +
                        $"**Round Status:** {status}\n" +
                        $"**Alert Level:** {alertLevel}\n" +
                        $"**Map:** {mapName}\n" +
                        $"**Preset:** {preset}",
                    Color = color,
                    Footer = new WebhookEmbedFooter
                    {
                        Text = $"Round ID: {roundId}"
                    }
                }
            }
        };
    }

    private string FormatInRoundStatus(SharedGameTicker sharedTicker)
    {
        var elapsed = _gameTiming.RealTime - sharedTicker.RoundStartTimeSpan;

        if (elapsed.TotalSeconds < 0)
            return "In game";

        // Format elapsed time similar to the Python bot's humanize_timedelta
        var parts = new List<string>();

        if (elapsed.Days > 0)
            parts.Add($"{elapsed.Days} day{(elapsed.Days != 1 ? "s" : "")}");
        if (elapsed.Hours > 0)
            parts.Add($"{elapsed.Hours} hour{(elapsed.Hours != 1 ? "s" : "")}");
        if (elapsed.Minutes > 0)
            parts.Add($"{elapsed.Minutes} minute{(elapsed.Minutes != 1 ? "s" : "")}");

        if (parts.Count == 0)
            return "Just started";

        return string.Join(", ", parts);
    }

    private string GetAlertLevelDisplay()
    {
        var levels = new List<string>();
        var query = EntityQueryEnumerator<AlertLevelComponent>();
        while (query.MoveNext(out _, out var alert))
        {
            if (string.IsNullOrEmpty(alert.CurrentAlertLevel.Id))
                continue;

            // Capitalize first letter for nicer display (e.g. "green" -> "Green").
            var level = alert.CurrentAlertLevel.Id;
            if (level.Length > 0)
                level = char.ToUpperInvariant(level[0]) + level[1..];

            if (!levels.Contains(level))
                levels.Add(level);
        }

        return levels.Count == 0 ? "Unknown" : string.Join(", ", levels);
    }
}
