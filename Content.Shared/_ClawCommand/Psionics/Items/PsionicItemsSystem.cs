using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.StatusEffect;

namespace Content.Shared.Abilities.Psionics
{
    public sealed partial class PsionicItemsSystem : EntitySystem
    {
        [Dependency] private StatusEffectsSystem _statusEffects = default!;
        [Dependency] private IComponentFactory _componentFactory = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TinfoilHatComponent, GotEquippedEvent>(OnTinfoilEquipped);
            SubscribeLocalEvent<TinfoilHatComponent, GotUnequippedEvent>(OnTinfoilUnequipped);
            SubscribeLocalEvent<ClothingGrantPsionicPowerComponent, GotEquippedEvent>(OnGranterEquipped);
            SubscribeLocalEvent<ClothingGrantPsionicPowerComponent, GotUnequippedEvent>(OnGranterUnequipped);
        }
        private void OnTinfoilEquipped(EntityUid uid, TinfoilHatComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            var insul = EnsureComp<PsionicInsulationComponent>(args.EquipTarget);
            insul.Passthrough = component.Passthrough;
            component.IsActive = true;
        }

        private void OnTinfoilUnequipped(EntityUid uid, TinfoilHatComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            if (!_statusEffects.HasStatusEffect(uid, "PsionicallyInsulated"))
                RemComp<PsionicInsulationComponent>(args.EquipTarget);

            component.IsActive = false;
        }

        private void OnGranterEquipped(EntityUid uid, ClothingGrantPsionicPowerComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            // does the user already has this power?
            // Claw Command - RA0045 requires the EntitySystem proxies rather than EntityManager directly.
            var componentType = _componentFactory.GetRegistration(component.Power).Type;
            if (HasComp(args.EquipTarget, componentType)) return;

            var newComponent = (Component) _componentFactory.GetComponent(componentType);
            AddComp(args.EquipTarget, newComponent);

            component.IsActive = true;
        }

        private void OnGranterUnequipped(EntityUid uid, ClothingGrantPsionicPowerComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            component.IsActive = false;
            // Claw Command - RA0045 requires the EntitySystem proxies rather than EntityManager directly.
            var componentType = _componentFactory.GetRegistration(component.Power).Type;
            if (HasComp(args.EquipTarget, componentType))
            {
                RemComp(args.EquipTarget, componentType);
            }
        }
    }
}
