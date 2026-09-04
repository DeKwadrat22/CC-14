// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Lavaland.Trigger;

/// <summary>
/// Allows the trigger to actually activate only when the
/// total amount of triggers is within a certain range.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerCounterLimitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxCount { get; set; } = 1;
}
