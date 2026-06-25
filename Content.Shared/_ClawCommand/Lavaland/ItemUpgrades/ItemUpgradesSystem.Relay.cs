using Content.Goobstation.Common.Weapons;
using Content.Shared._Goobstation.Weapons.Ranged;
using Content.Shared._ClawCommand.Lavaland.ItemUpgrades.Components;
using Content.Shared._ClawCommand.Lavaland.ItemUpgrades.Events;
using Content.Shared._ClawCommand.Lavaland.Weapons;
using Content.Shared._ClawCommand.Lavaland.Weapons.Ranged.Events;
using Content.Shared.Actions;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._ClawCommand.Lavaland.ItemUpgrades;

public sealed partial class ItemUpgradesSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<ItemUpgradeableComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, RechargeBasicEntityAmmoGetCooldownModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GunShotEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, ProjectileShotEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GetRelayMeleeWeaponEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GetMeleeDamageEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GetLightAttackRangeEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GetMeleeAttackRateEvent>(RelayEvent);
        SubscribeLocalEvent<ItemUpgradeableComponent, GetItemActionsEvent>(RelayGetActionEvent);
    }

    private void RelayEvent<T>(Entity<ItemUpgradeableComponent> ent, ref T args) where T : notnull
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            var beforeEv = new BeforeItemUpgradeRelayEvent();
            RaiseLocalEvent(upgrade, ref beforeEv);
            if (beforeEv.Cancelled)
                continue;

            RaiseLocalEvent(upgrade, ref args);
        }
    }

    // _ClawCommand Lavaland: fork's GetItemActionsEvent has no IsEquipping flag and SharedActionsSystem
    // lacks SaveActions/LoadActions. We simply re-raise the event on each upgrade so they can register
    // their own actions, then forward any returned action IDs into the parent event's Actions set so the
    // fork's normal equip handler grants them to the wearer.
    private void RelayGetActionEvent(Entity<ItemUpgradeableComponent> ent, ref GetItemActionsEvent args)
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            var beforeEv = new BeforeItemUpgradeRelayEvent();
            RaiseLocalEvent(upgrade.Owner, ref beforeEv);
            if (beforeEv.Cancelled)
                continue;

            var ev = new GetItemActionsEvent(_actionContainer, args.User, upgrade.Owner, args.SlotFlags);
            RaiseLocalEvent(upgrade.Owner, ev);

            foreach (var action in ev.Actions)
                args.Actions.Add(action);
        }
    }
}
