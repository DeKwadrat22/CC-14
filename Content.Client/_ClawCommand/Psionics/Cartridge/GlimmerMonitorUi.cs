using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;

// Claw Command - XamlIL derives the expected class from the FILE PATH, so a XAML file under
// _ClawCommand must carry a matching namespace. This is why it differs from the upstream one.
namespace Content.Client._ClawCommand.Psionics.Cartridge;

public sealed partial class GlimmerMonitorUi : UIFragment
{
    private GlimmerMonitorUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new GlimmerMonitorUiFragment();

        _fragment.OnSync += _ => SendSyncMessage(userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GlimmerMonitorUiState monitorState)
            return;

        _fragment?.UpdateState(monitorState.GlimmerValues);
    }

    private void SendSyncMessage(BoundUserInterface userInterface)
    {
        var syncMessage = new GlimmerMonitorSyncMessageEvent();
        var message = new CartridgeUiMessage(syncMessage);
        userInterface.SendMessage(message);
    }
}
