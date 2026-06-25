namespace Content.Shared._ClawCommand.Lavaland.ItemUpgrades.Events;

[ByRefEvent]
public record struct BeforeItemUpgradeRelayEvent(bool Cancelled = false);
