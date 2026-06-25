using Content.Client.Eui;
using Content.Shared._ClawCommand.Administration;
using Content.Shared.Eui;

namespace Content.Client._ClawCommand.Administration.UI.PlayTimeEditor;

public sealed class PlayTimeEditorEui : BaseEui
{
    public PlayTimeEditorWindow EditorWindow { get; }

    public PlayTimeEditorEui()
    {
        EditorWindow = new PlayTimeEditorWindow();
        EditorWindow.OnSubmit += args => SendMessage(new PlayTimeEditorSubmitMessage(args.targetName, args.adjustments, args.overwrite));
    }

    public override void Opened()
    {
        EditorWindow.OpenCentered();
    }

    public override void Closed()
    {
        EditorWindow.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PlayTimeEditorEuiState cast)
            return;

        EditorWindow.UpdatePermissions(cast.HasPermission);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PlayTimeEditorStatusMessage status)
            return;

        EditorWindow.SetStatus(status.Text, status.StatusColor);
    }
}
