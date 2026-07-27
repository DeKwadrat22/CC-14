using Content.Shared.Actions;
using Content.Shared.Alert;

namespace Content.Shared._ClawCommand.Shadekin;

public sealed partial class ShadekinPhaseActionEvent : InstantActionEvent
{
}

public sealed partial class CritShadekinEvent : InstantActionEvent
{
}

/// <summary>
///     Raised on a Shadekin when they click their energy/light HUD alert. Prints their status to chat.
/// </summary>
public sealed partial class CheckShadekinAlertEvent : BaseAlertEvent
{
}
