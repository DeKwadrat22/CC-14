using Content.Shared.Actions;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Damage; using Content.Shared.Damage.Systems; using Content.Shared.Damage.Components; // Claw Command - damage split into Systems/Components
using Content.Shared.Stunnable;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Server.Psionics;
using Content.Shared.Actions.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class PsionicInvisibilityPowerSystem : EntitySystem
    {
        [Dependency] private SharedActionsSystem _actions = default!;
        [Dependency] private SharedStunSystem _stunSystem = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private SharedStealthSystem _stealth = default!;
        [Dependency] private SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicInvisibilityPowerComponent, PsionicInvisibilityPowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<RemovePsionicInvisibilityOffPowerActionEvent>(OnPowerOff);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, ComponentInit>(OnStart);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, ComponentShutdown>(OnEnd);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnPowerUsed(EntityUid uid, PsionicInvisibilityPowerComponent component, PsionicInvisibilityPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "psionic invisibility")
                || HasComp<PsionicInvisibilityUsedComponent>(uid))
                return;

            ToggleInvisibility(args.Performer);
            var action = Spawn(PsionicInvisibilityUsedComponent.PsionicInvisibilityUsedActionPrototype);
            _actions.AddAction(uid, action, action);
            // Claw Command - TryGetActionData is gone; StartUseDelay already no-ops when the action has no
            // UseDelay, so the guard it replaced is now redundant.
            _actions.StartUseDelay(action);

            _psionics.LogPowerUsed(uid, "psionic invisibility");
            args.Handled = true;
        }

        private void OnPowerOff(RemovePsionicInvisibilityOffPowerActionEvent args)
        {
            if (!HasComp<PsionicInvisibilityUsedComponent>(args.Performer))
                return;

            ToggleInvisibility(args.Performer);
            args.Handled = true;
        }

        private void OnStart(EntityUid uid, PsionicInvisibilityUsedComponent component, ComponentInit args)
        {
            EnsureComp<PsionicallyInvisibleComponent>(uid);
            EnsureComp<PacifiedComponent>(uid);
            var stealth = EnsureComp<StealthComponent>(uid);
            _stealth.SetVisibility(uid, 0.66f, stealth);
            _audio.PlayPvs("/Audio/Effects/toss.ogg", uid);

        }

        private void OnEnd(EntityUid uid, PsionicInvisibilityUsedComponent component, ComponentShutdown args)
        {
            if (Terminating(uid))
                return;

            RemComp<PsionicallyInvisibleComponent>(uid);
            RemComp<PacifiedComponent>(uid);
            RemComp<StealthComponent>(uid);
            _audio.PlayPvs("/Audio/Effects/toss.ogg", uid);
            //Pretty sure this DOESN'T work as intended.
            _actions.RemoveAction(uid, component.PsionicInvisibilityUsedActionEntity);

            _stunSystem.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(8), false);
            DirtyEntity(uid);
        }

        private void OnDamageChanged(EntityUid uid, PsionicInvisibilityUsedComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased)
                return;

            ToggleInvisibility(uid);
        }

        public void ToggleInvisibility(EntityUid uid)
        {
            if (!HasComp<PsionicInvisibilityUsedComponent>(uid))
            {
                EnsureComp<PsionicInvisibilityUsedComponent>(uid);
            }
            else
            {
                RemComp<PsionicInvisibilityUsedComponent>(uid);
            }
        }
    }
}
