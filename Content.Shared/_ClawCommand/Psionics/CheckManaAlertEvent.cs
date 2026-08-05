using Content.Shared.Alert;

namespace Content.Shared.Abilities.Psionics;

/// <summary>
///     Raised when a psion clicks their mana alert. Handled server-side to report the exact figure in chat.
/// </summary>
/// <remarks>
///     Claw Command - upstream used <c>onClick: !type:CheckMana</c> with an IAlertClick implementation.
///     Alerts now dispatch a <see cref="BaseAlertEvent"/> instead, so the logic lives in PsionicsSystem.
/// </remarks>
public sealed partial class CheckManaAlertEvent : BaseAlertEvent;
