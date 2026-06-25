// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Lavaland.Trigger;

/// <summary>
/// Counts the total amount of triggers that this entity had in its entire lifetime.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerCounterComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Count { get; set; }
}
