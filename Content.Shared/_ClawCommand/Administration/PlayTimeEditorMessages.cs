using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Administration;

[Serializable, NetSerializable]
public sealed class PlayTimeEditorEuiState : EuiStateBase
{
    public bool HasPermission { get; }

    public PlayTimeEditorEuiState(bool hasPermission)
    {
        HasPermission = hasPermission;
    }
}

[Serializable, NetSerializable]
public sealed class PlayTimeEditorSubmitMessage : EuiMessageBase
{
    public string TargetName { get; }
    public List<PlayTimeAdjustment> Adjustments { get; }
    public bool Overwrite { get; }

    public PlayTimeEditorSubmitMessage(string targetName, List<PlayTimeAdjustment> adjustments, bool overwrite)
    {
        TargetName = targetName;
        Adjustments = adjustments;
        Overwrite = overwrite;
    }
}

[Serializable, NetSerializable]
public sealed class PlayTimeEditorStatusMessage : EuiMessageBase
{
    public string Text { get; }
    public Color StatusColor { get; }

    public PlayTimeEditorStatusMessage(string text, Color color)
    {
        Text = text;
        StatusColor = color;
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public partial record struct PlayTimeAdjustment
{
    [DataField]
    public string DurationText { get; init; }

    [DataField]
    public string RoleTracker { get; init; }

    public PlayTimeAdjustment(string roleTracker, string durationText)
    {
        RoleTracker = roleTracker;
        DurationText = durationText;
    }
}
