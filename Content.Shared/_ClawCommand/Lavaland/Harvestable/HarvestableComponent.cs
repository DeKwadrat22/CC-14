// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Lavaland.Harvestable;

/// <summary>
/// Simple component for harvestables. "Click on me to get loot" behavior.
/// </summary>
[RegisterComponent]
public sealed partial class HarvestableComponent : Component
{
    // Harvest loot.
    [DataField(required: true)]
    public EntProtoId? Loot { get; set; }

    // Harvest doAfter delay.
    [DataField]
    public float Delay { get; set; } = 1f;
}
