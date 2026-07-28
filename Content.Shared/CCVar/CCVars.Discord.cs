using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     The role that will get mentioned if a new SOS ahelp comes in.
    /// </summary>
    public static readonly CVarDef<string> DiscordAhelpMention =
        CVarDef.Create("discord.on_call_ping", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     URL of the discord webhook to relay unanswered ahelp messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordOnCallWebhook =
        CVarDef.Create("discord.on_call_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     URL of the Discord webhook which will relay all ahelp messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpWebhook =
        CVarDef.Create("discord.ahelp_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     The server icon to use in the Discord ahelp embed footer.
    ///     Valid values are specified at https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpFooterIcon =
        CVarDef.Create("discord.ahelp_footer_icon", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     The avatar to use for the webhook. Should be an URL.
    /// </summary>
    public static readonly CVarDef<string> DiscordAHelpAvatar =
        CVarDef.Create("discord.ahelp_avatar", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay all custom votes. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordVoteWebhook =
        CVarDef.Create("discord.vote_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay all votekick votes. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordVotekickWebhook =
        CVarDef.Create("discord.votekick_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay event logs. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordEventWebhook =
        CVarDef.Create("discord.event_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay round restart messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundUpdateWebhook =
        CVarDef.Create("discord.round_update_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Role id for the Discord webhook to ping when the round ends.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundEndRoleWebhook =
        CVarDef.Create("discord.round_end_role", string.Empty, CVar.SERVERONLY);


    /// <summary>
    ///     Claw Command - URL of the Discord webhook which will relay ERT notification messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordERTNotificationWebhook =
        CVarDef.Create("discord.ert_notification_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Claw Command - Role id for the Discord webhook to ping when ERT is called.
    /// </summary>
    public static readonly CVarDef<string> DiscordERTNotificationRoleWebhook =
        CVarDef.Create("discord.ert_notification_role", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - URL of the Discord webhook for ERT request notifications from comms console.
    /// </summary>
    public static readonly CVarDef<string> DiscordERTRequestWebhook =
        CVarDef.Create("discord.ert_request_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Claw Command - Role id for the Discord webhook to ping when ERT is requested from comms console.
    /// </summary>
    public static readonly CVarDef<string> DiscordERTRequestRoleWebhook =
        CVarDef.Create("discord.ert_request_role", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     The token used to authenticate with Discord. For the Bot to function set: discord.token, discord.guild_id, and discord.prefix.
    ///     If this is empty, the bot will not connect.
    /// </summary>
    public static readonly CVarDef<string> DiscordToken =
        CVarDef.Create("discord.token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     The Discord guild ID to use for commands as well as for several other features.
    ///     If this is empty, the bot will not connect.
    /// </summary>
    public static readonly CVarDef<string> DiscordGuildId =
        CVarDef.Create("discord.guild_id", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Prefix used for commands for the Discord bot.
    ///     If this is empty, the bot will not connect.
    /// </summary>
    public static readonly CVarDef<string> DiscordPrefix =
        CVarDef.Create("discord.prefix", "!", CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will relay watchlist connection notifications. If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordWatchlistConnectionWebhook =
        CVarDef.Create("discord.watchlist_connection_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Claw Command - URL of the Discord webhook for the live server status message (player count, map, gamemode).
    /// </summary>
    public static readonly CVarDef<string> DiscordServerStatusWebhook =
        CVarDef.Create("discord.server_status_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Claw Command - Whether the live server status webhook is enabled.
    /// </summary>
    public static readonly CVarDef<bool> DiscordServerStatusEnabled =
        CVarDef.Create("discord.server_status_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - Optional persistent Discord message ID to edit instead of creating a new message on each launch.
    ///     If empty, a new message is created on startup. Set this to reuse the same message across restarts.
    /// </summary>
    public static readonly CVarDef<string> DiscordServerStatusMessageId =
        CVarDef.Create("discord.server_status_message_id", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - URL of the Discord webhook for population alert pings.
    /// </summary>
    public static readonly CVarDef<string> DiscordPopAlertWebhook =
        CVarDef.Create("discord.pop_alert_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Claw Command - Whether the population alert system is enabled.
    /// </summary>
    public static readonly CVarDef<bool> DiscordPopAlertEnabled =
        CVarDef.Create("discord.pop_alert_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - The Discord role ID to ping when the player threshold is reached (e.g. "123456789012345678").
    /// </summary>
    public static readonly CVarDef<string> DiscordPopAlertRoleId =
        CVarDef.Create("discord.pop_alert_role_id", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - Minimum number of online players required to trigger a population alert ping.
    /// </summary>
    public static readonly CVarDef<int> DiscordPopAlertThreshold =
        CVarDef.Create("discord.pop_alert_threshold", 10, CVar.SERVERONLY);

    /// <summary>
    ///     Claw Command - Cooldown in hours between population alert pings to avoid spam.
    /// </summary>
    public static readonly CVarDef<float> DiscordPopAlertCooldownHours =
        CVarDef.Create("discord.pop_alert_cooldown_hours", 3f, CVar.SERVERONLY);

    /// <summary>
    ///     How long to buffer watchlist connections for, in seconds.
    ///     All connections within this amount of time from the first one will be batched and sent as a single
    ///     Discord notification. If zero, always sends a separate notification for each connection (not recommended).
    /// </summary>
    public static readonly CVarDef<float> DiscordWatchlistConnectionBufferTime =
        CVarDef.Create("discord.watchlist_connection_buffer_time", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord webhook which will receive station news acticles at the round end.
    ///     If left empty, disables the webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordNewsWebhook =
        CVarDef.Create("discord.news_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     HEX color of station news discord webhook's embed.
    /// </summary>
    public static readonly CVarDef<string> DiscordNewsWebhookEmbedColor =
        CVarDef.Create("discord.news_webhook_embed_color", Color.LawnGreen.ToHex(), CVar.SERVERONLY);

    /// <summary>
    ///     Whether or not articles should be sent mid-round instead of all at once at the round's end
    /// </summary>
    public static readonly CVarDef<bool> DiscordNewsWebhookSendDuringRound =
        CVarDef.Create("discord.news_webhook_send_during_round", false, CVar.SERVERONLY);

}
