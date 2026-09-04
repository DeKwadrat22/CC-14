using Content.Shared._ClawCommand.Voidfox;
using Robust.Client.UserInterface;

namespace Content.Client._ClawCommand.Voidfox;

public sealed class VoidfoxBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VoidfoxWindow? _window;

    public VoidfoxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoidfoxWindow>();
        _window.OnToggleLadder += () => SendMessage(new VoidfoxToggleLadderMessage());
        _window.OnToggleCockpit += () => SendMessage(new VoidfoxToggleCockpitMessage());
        _window.OnToggleFuelLatch += () => SendMessage(new VoidfoxToggleFuelLatchMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is VoidfoxBuiState s)
            _window?.UpdateState(s);
    }
}
