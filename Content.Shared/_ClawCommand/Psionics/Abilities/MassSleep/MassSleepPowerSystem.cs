using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components; // Claw Command - DamageableComponent moved out of Content.Shared.Damage
using Content.Shared.Mobs.Components;
using Content.Shared.Actions.Events;

namespace Content.Shared.Abilities.Psionics
{
    public sealed partial class MassSleepPowerSystem : EntitySystem
    {
        [Dependency] private EntityLookupSystem _lookup = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MassSleepPowerComponent, MassSleepPowerActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(EntityUid uid, MassSleepPowerComponent component, MassSleepPowerActionEvent args)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(args.Target, component.Radius))
            {
                if (HasComp<MobStateComponent>(entity) && entity != uid && !HasComp<PsionicInsulationComponent>(entity))
                {
                    // Claw Command - the damage container moved off DamageableComponent onto InjurableComponent.
                    // Same intent as upstream: only things with biological damage can be put to sleep.
                    if (TryComp<InjurableComponent>(entity, out var injurable) && injurable.DamageContainer == "Biological")
                        EnsureComp<SleepingComponent>(entity);
                }
            }
            _psionics.LogPowerUsed(uid, "mass sleep");
            args.Handled = true;
        }
    }
}
