using Content.Shared._ClawCommand.Info;
using Robust.Shared.Network;


namespace Content.Client._ClawCommand.Info;


public sealed partial class NsfwDisclaimerSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    private NsfwDisclaimerWindow? _window;

    public override void Initialize()
    {
        SubscribeNetworkEvent<ShowNsfwPopupDisclaimerMessage>(OnShowPopup);
        _net.Disconnect += (_, _) => _window?.Close();
    }

    private void OnShowPopup(ShowNsfwPopupDisclaimerMessage message)
    {
        _window?.Close();

        _window = new();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
    }
}
