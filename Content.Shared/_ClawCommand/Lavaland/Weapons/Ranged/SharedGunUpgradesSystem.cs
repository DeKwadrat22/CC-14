using Content.Shared._Goobstation.Weapons.Ranged;
using Content.Shared._ClawCommand.Lavaland.ItemUpgrades.Components;
using Content.Shared._ClawCommand.Lavaland.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._ClawCommand.Lavaland.Weapons.Ranged;

public abstract partial class SharedGunUpgradesSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunUpgradeComponentsComponent, EntGotInsertedIntoContainerMessage>(OnCompsUpgradeInsert);
        SubscribeLocalEvent<GunUpgradeComponentsComponent, EntGotRemovedFromContainerMessage>(OnCompsUpgradeEject);

        // _ClawCommand: GunUpgradeFireRateComponent / GunUpgradeSpeedComponent GunRefreshModifiersEvent
        // are already handled by fork's vanilla GunUpgradeSystem. Skip re-subscription.
        SubscribeLocalEvent<GunUpgradeFireRateComponent, RechargeBasicEntityAmmoGetCooldownModifiersEvent>(OnFireRateRefreshRecharge);

        SubscribeLocalEvent<GunUpgradeProjectileComponentsComponent, GunShotEvent>(OnDamageGunShotComps);

        SubscribeLocalEvent<GunUpgradeVampirismComponent, GunShotEvent>(OnVampirismGunShot);
        SubscribeLocalEvent<ProjectileVampirismComponent, ProjectileHitEvent>(OnVampirismProjectileHit);

        SubscribeLocalEvent<GunUpgradeBayonetComponent, GetRelayMeleeWeaponEvent>(OnGetMeleeRelay);
    }

    private void OnFireRateRefresh(Entity<GunUpgradeFireRateComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.FireRate *= ent.Comp.Coefficient;
        // _ClawCommand Lavaland: fork's GunRefreshModifiersEvent has no BurstFireRate/BurstCooldown fields.
    }

    private void OnFireRateRefreshRecharge(Entity<GunUpgradeFireRateComponent> ent, ref RechargeBasicEntityAmmoGetCooldownModifiersEvent args)
    {
        args.Multiplier /= ent.Comp.Coefficient;
    }

    private void OnCompsUpgradeInsert(Entity<GunUpgradeComponentsComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_timing.ApplyingState && HasComp<ItemUpgradeableComponent>(args.Container.Owner))
            EntityManager.AddComponents(args.Container.Owner, ent.Comp.Components);
    }

    private void OnCompsUpgradeEject(Entity<GunUpgradeComponentsComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!_timing.ApplyingState && HasComp<ItemUpgradeableComponent>(args.Container.Owner))
            EntityManager.RemoveComponents(args.Container.Owner, ent.Comp.Components);
    }

    private void OnSpeedRefresh(Entity<GunUpgradeSpeedComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.ProjectileSpeed *= ent.Comp.Coefficient;
    }

    private void OnDamageGunShotComps(Entity<GunUpgradeProjectileComponentsComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (HasComp<ProjectileComponent>(ammo))
                EntityManager.AddComponents(ammo.Value, ent.Comp.Components);
        }
    }

    private void OnVampirismGunShot(Entity<GunUpgradeVampirismComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (!HasComp<ProjectileComponent>(ammo))
                continue;

            var comp = EnsureComp<ProjectileVampirismComponent>(ammo.Value);
            comp.DamageOnHit = ent.Comp.DamageOnHit;
        }
    }

    private void OnVampirismProjectileHit(Entity<ProjectileVampirismComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        // _ClawCommand Lavaland: fork's TryChangeDamage takes Entity<DamageableComponent?>.
        if (args.Shooter is { } shooter)
            _damage.TryChangeDamage(shooter, ent.Comp.DamageOnHit);
    }

    private void OnGetMeleeRelay(Entity<GunUpgradeBayonetComponent> ent, ref GetRelayMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        args.Found = ent.Owner;
        args.Handled = true;
    }
}
