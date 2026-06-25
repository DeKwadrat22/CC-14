using Content.Server.Discord;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.ServerStatusWebhook;

/// <summary>
///     Claw Command - Sends a Discord role ping via webhook when the server reaches
///     a configured player count threshold. Enforces a cooldown (default 3 hours)
///     to prevent notification spam.
/// </summary>
public sealed partial class PopulationAlertSystem : EntitySystem
{
    [Dependency] private IBaseServer _baseServer = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private string _webhookUrl = string.Empty;
    private bool _enabled;
    private string _roleId = string.Empty;
    private int _threshold = 10;
    private float _cooldownHours = 3f;

    private WebhookIdentifier _webhookIdentifier;
    private bool _webhookInitialized;

    /// <summary>
    ///     The last time we sent a population alert ping.
    /// </summary>
    private TimeSpan _lastAlertTime = TimeSpan.Zero;

    /// <summary>
    ///     Whether we already sent an alert for the current population spike.
    ///     Resets when player count drops below the threshold.
    /// </summary>
    private bool _alertSent;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("discord.popalert");

        _cfg.OnValueChanged(CCVars.DiscordPopAlertWebhook, OnWebhookChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordPopAlertEnabled, OnEnabledChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordPopAlertRoleId, OnRoleIdChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordPopAlertThreshold, OnThresholdChanged, true);
        _cfg.OnValueChanged(CCVars.DiscordPopAlertCooldownHours, OnCooldownChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.DiscordPopAlertWebhook, OnWebhookChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordPopAlertEnabled, OnEnabledChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordPopAlertRoleId, OnRoleIdChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordPopAlertThreshold, OnThresholdChanged);
        _cfg.UnsubValueChanged(CCVars.DiscordPopAlertCooldownHours, OnCooldownChanged);
    }

    private void OnWebhookChanged(string url)
    {
        _webhookUrl = url;
        _webhookInitialized = false;
    }

    private void OnEnabledChanged(bool enabled) => _enabled = enabled;
    private void OnRoleIdChanged(string roleId) => _roleId = roleId;
    private void OnThresholdChanged(int threshold) => _threshold = threshold;
    private void OnCooldownChanged(float hours) => _cooldownHours = hours;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || string.IsNullOrWhiteSpace(_webhookUrl) || string.IsNullOrWhiteSpace(_roleId))
            return;

        var playerCount = _playerManager.PlayerCount;

        // Reset alert flag when population drops below threshold
        if (playerCount < _threshold)
        {
            _alertSent = false;
            return;
        }

        // We're at or above threshold
        if (_alertSent)
            return;

        // Check cooldown
        var now = _gameTiming.RealTime;
        if (now - _lastAlertTime < TimeSpan.FromHours(_cooldownHours))
            return;

        _alertSent = true;
        _lastAlertTime = now;
        SendAlert(playerCount);
    }

    private async void SendAlert(int playerCount)
    {
        try
        {
            if (!_webhookInitialized)
            {
                var webhookData = await _discord.GetWebhook(_webhookUrl);
                if (webhookData == null)
                {
                    _sawmill.Warning("Failed to get webhook data for population alert. Is the URL correct?");
                    return;
                }

                _webhookIdentifier = webhookData.Value.ToIdentifier();
                _webhookInitialized = true;
            }

            var serverName = _baseServer.ServerName;

            var payload = new WebhookPayload
            {
                Content = $"<@&{_roleId}>",
                Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Title = "Server is poppin'!",
                        Description =
                            $"**{serverName}** has reached **{playerCount}** players!\n" +
                            $"Come join the fun!",
                        Color = 0x2ECC71 // Green
                    }
                },
                AllowedMentions = new WebhookMentions()
            };

            payload.AllowedMentions.AllowRoleMentions();

            var response = await _discord.CreateMessage(_webhookIdentifier, payload);
            if (response.IsSuccessStatusCode)
            {
                _sawmill.Info("Sent population alert ping ({0} players)", playerCount);
            }
            else
            {
                _sawmill.Error("Failed to send population alert: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error sending population alert:\n{e}");
        }
    }
}
