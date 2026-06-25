using System.Net;
using Content.Shared.CCVar;
using Content.Shared._ClawCommand.Info;
using Robust.Shared.Configuration;
using Robust.Shared.Network;


namespace Content.Server._ClawCommand.Info;


public sealed partial class NsfwDisclaimerSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    public override void Initialize()
    {
        _netManager.Connected += OnConnected;
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        // Ignore localhost unless the specified debug cvar is set
        if (IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) && _cfg.GetCVar(CCVars.RulesExemptLocal))
            return;

        var message = new ShowNsfwPopupDisclaimerMessage();
        RaiseNetworkEvent(message, e.Channel);
    }
}
