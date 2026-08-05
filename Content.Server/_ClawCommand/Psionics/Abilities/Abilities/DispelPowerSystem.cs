using Content.Shared.StatusEffect;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Damage; using Content.Shared.Damage.Systems; using Content.Shared.Damage.Components; // Claw Command - damage split into Systems/Components
using Content.Shared.Revenant.Components;
using Content.Shared.Guardian; using Content.Shared.Guardian.Components; // Claw Command - Guardian moved to Shared
using Content.Shared.Bible.Components; // Claw Command - Bible components moved to Shared
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Actions.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class DispelPowerSystem : EntitySystem
    {
        [Dependency] private StatusEffectsSystem _statusEffects = default!;
        [Dependency] private DamageableSystem _damageableSystem = default!;
        [Dependency] private GuardianSystem _guardianSystem = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private SharedAudioSystem _audioSystem = default!;
        [Dependency] private PopupSystem _popupSystem = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DispelPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<DispellableComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<DamageOnDispelComponent, DispelledEvent>(OnDmgDispelled);
            // Upstream stuff we're just gonna handle here
            SubscribeLocalEvent<GuardianComponent, DispelledEvent>(OnGuardianDispelled);
            SubscribeLocalEvent<FamiliarComponent, DispelledEvent>(OnFamiliarDispelled);
            SubscribeLocalEvent<RevenantComponent, DispelledEvent>(OnRevenantDispelled);
        }

        private void OnPowerUsed(DispelPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "dispel"))
                return;

            var ev = new DispelledEvent();
            RaiseLocalEvent(args.Target, ev, false);

            if (ev.Handled)
            {
                args.Handled = true;
                _psionics.LogPowerUsed(args.Performer, "dispel");
            }
        }

        private void OnDispelled(EntityUid uid, DispellableComponent component, DispelledEvent args)
        {
            QueueDel(uid);
            Spawn("Ash", Transform(uid).Coordinates);
            _popupSystem.PopupCoordinates(Loc.GetString("psionic-burns-up", ("item", uid)), Transform(uid).Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.MediumCaution);
            _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);
            args.Handled = true;
        }

        private void OnDmgDispelled(EntityUid uid, DamageOnDispelComponent component, DispelledEvent args)
        {
            var damage = component.Damage;
            var modifier = (1 + component.Variance) - (_random.NextFloat(0, component.Variance * 2));

            damage *= modifier;
            DealDispelDamage(uid, damage);
            args.Handled = true;
        }

        private void OnGuardianDispelled(EntityUid uid, GuardianComponent guardian, DispelledEvent args)
        {
            // Claw Command - ToggleGuardian now takes an Entity<GuardianHostComponent> instead of (uid, comp).
            if (TryComp<GuardianHostComponent>(guardian.Host, out var host))
                _guardianSystem.ToggleGuardian((guardian.Host.Value, host));

            DealDispelDamage(uid);
            args.Handled = true;
        }

        private void OnFamiliarDispelled(EntityUid uid, FamiliarComponent component, DispelledEvent args)
        {
            if (component.Source != null)
                EnsureComp<SummonableRespawningComponent>(component.Source.Value);

            args.Handled = true;
        }

        private void OnRevenantDispelled(EntityUid uid, RevenantComponent component, DispelledEvent args)
        {
            DealDispelDamage(uid);
            _statusEffects.TryAddStatusEffect(uid, "Corporeal", TimeSpan.FromSeconds(30), false, "Corporeal");
            args.Handled = true;
        }

        public void DealDispelDamage(EntityUid uid, DamageSpecifier? damage = null)
        {
            if (Deleted(uid))
                return;

            _popupSystem.PopupCoordinates(Loc.GetString("psionic-burn-resist", ("item", uid)), Transform(uid).Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.SmallCaution);
            _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);

            if (damage == null)
            {
                damage = new();
                damage.DamageDict.Add("Blunt", 100);
            }
            _damageableSystem.TryChangeDamage(uid, damage, true, true);
        }
    }
    public sealed partial class DispelledEvent : HandledEntityEventArgs {}
}


