using Content.Server._ClawCommand.Lavaland.Weapons.Melee.Components;
using Content.Shared._ClawCommand.Lavaland.Weapons.Melee;
using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._ClawCommand.Lavaland.Weapons.Melee;

public sealed partial class MeleeUpgradesSystem : SharedMeleeUpgradesSystem
{
    // _ClawCommand Lavaland: Goob uses its own SharedEntityEffectSystem with a Reagent-style
    // EntityEffectBaseArgs(hit, EntityManager). The fork's equivalent is SharedEntityEffectsSystem,
    // which exposes ApplyEffects(target, effects, scale, user).
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponUpgradeEffectsComponent, MeleeHitEvent>(OnEffectsUpgradeHit);
    }

    private void OnEffectsUpgradeHit(Entity<WeaponUpgradeEffectsComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.Effects.Count == 0)
            return;

        var effects = ent.Comp.Effects.ToArray();
        foreach (var hit in args.HitEntities)
            _entityEffects.ApplyEffects(hit, effects, user: args.User);
    }
}
