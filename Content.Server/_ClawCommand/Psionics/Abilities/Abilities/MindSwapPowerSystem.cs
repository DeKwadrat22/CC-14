using Content.Shared.Actions;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Speech;
using Content.Shared.Stealth.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Damage; using Content.Shared.Damage.Systems; using Content.Shared.Damage.Components; // Claw Command - damage split into Systems/Components
using Content.Server.Mind;
using Content.Server.Ghost; // Claw Command - GhostAttemptHandleEvent lives here
using Content.Shared.Mobs.Systems;
using Content.Server.Popups;
using Content.Server.Psionics;
using Content.Server.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Actions.Events;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class MindSwapPowerSystem : EntitySystem
    {
        [Dependency] private SharedActionsSystem _actions = default!;
        [Dependency] private MobStateSystem _mobStateSystem = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private MindSystem _mindSystem = default!;
        [Dependency] private MetaDataSystem _metaDataSystem = default!;
        [Dependency] private DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MindSwapPowerComponent, MindSwapPowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<PsionicComponent, MindSwapPowerDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<MindSwappedComponent, MindSwapPowerReturnActionEvent>(OnPowerReturned);
            SubscribeLocalEvent<MindSwappedComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<MindSwappedComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
            //
            SubscribeLocalEvent<MindSwappedComponent, ComponentInit>(OnSwapInit);
        }

        private void OnPowerUsed(EntityUid uid, MindSwapPowerComponent component, MindSwapPowerActionEvent args)
        {
            // Claw Command - the damage container moved from DamageableComponent to InjurableComponent.
            if (!_psionics.OnAttemptPowerUse(args.Performer, "mind swap")
                || !(TryComp<InjurableComponent>(args.Target, out var injurable) && injurable.DamageContainer == "Biological"))
                return;

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, component.UseDelay, new MindSwapPowerDoAfterEvent(), args.Performer, target: args.Target)
            {
                Hidden = true,
                BreakOnDamage = true,
                BreakOnMove = true
            }, out var doAfterId);

            if (TryComp<PsionicComponent>(uid, out var magic))
                magic.DoAfter = doAfterId;

            _psionics.LogPowerUsed(args.Performer, "mind swap");
            args.Handled = true;
        }

        private void OnDoAfter(EntityUid uid, PsionicComponent component, MindSwapPowerDoAfterEvent args)
        {
            if (component is null)
                return;
            component.DoAfter = null;

            if (args.Target is null
                || args.Cancelled)
                return;

            Swap(uid, args.Target.Value);
        }

        private void OnPowerReturned(EntityUid uid, MindSwappedComponent component, MindSwapPowerReturnActionEvent args)
        {
            if (HasComp<PsionicInsulationComponent>(component.OriginalEntity) || HasComp<PsionicInsulationComponent>(uid))
                return;

            if (HasComp<MobStateComponent>(uid) && !_mobStateSystem.IsAlive(uid))
                return;

            // How do we get trapped?
            // 1. Original target doesn't exist
            if (!component.OriginalEntity.IsValid() || Deleted(component.OriginalEntity))
            {
                GetTrapped(uid);
                return;
            }
            // 1. Original target is no longer mindswapped
            if (!TryComp<MindSwappedComponent>(component.OriginalEntity, out var targetMindSwap))
            {
                GetTrapped(uid);
                return;
            }

            // 2. Target has undergone a different mind swap
            if (targetMindSwap.OriginalEntity != uid)
            {
                GetTrapped(uid);
                return;
            }

            // 3. Target is dead
            if (HasComp<MobStateComponent>(component.OriginalEntity) && _mobStateSystem.IsDead(component.OriginalEntity))
            {
                GetTrapped(uid);
                return;
            }

            Swap(uid, component.OriginalEntity, true);
        }

        private void OnDispelled(EntityUid uid, MindSwappedComponent component, DispelledEvent args)
        {
            Swap(uid, component.OriginalEntity, true);
            args.Handled = true;
        }

        private void OnMobStateChanged(EntityUid uid, MindSwappedComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
                RemComp<MindSwappedComponent>(uid);
        }

        private void OnGhostAttempt(GhostAttemptHandleEvent args)
        {
            if (args.Handled)
                return;

            if (!HasComp<MindSwappedComponent>(args.Mind.CurrentEntity))
                return;

            //No idea where the viaCommand went. It's on the internal OnGhostAttempt, but not this layer. Maybe unnecessary.
            /*if (!args.viaCommand)
                return;*/

            args.Result = false;
            args.Handled = true;
        }

        private void OnSwapInit(EntityUid uid, MindSwappedComponent component, ComponentInit args)
        {
            _actions.AddAction(uid, ref component.MindSwapReturnActionEntity, component.MindSwapReturnActionId);
            // Claw Command - TryGetActionData is gone; StartUseDelay already no-ops when the action has no
            // UseDelay, so the guard it replaced is now redundant.
            _actions.StartUseDelay(component.MindSwapReturnActionEntity);
        }

        public void Swap(EntityUid performer, EntityUid target, bool end = false)
        {
            if (end && (!HasComp<MindSwappedComponent>(performer) || !HasComp<MindSwappedComponent>(target)))
                return;

            // Get the minds first. On transfer, they'll be gone.
            MindComponent? performerMind = null;
            MindComponent? targetMind = null;

            // This is here to prevent missing MindContainerComponent Resolve errors.
            if (!_mindSystem.TryGetMind(performer, out var performerMindId, out performerMind))
            {
                performerMind = null;
            };

            if (!_mindSystem.TryGetMind(target, out var targetMindId, out targetMind))
            {
                targetMind = null;
            };
            //This is a terrible way to 'unattach' minds. I wanted to use UnVisit but in TransferTo's code they say
            //To unnatch the minds, do it like this.
            //Have to unnattach the minds before we reattach them via transfer. Still feels weird, but seems to work well.
            _mindSystem.TransferTo(performerMindId, null);
            _mindSystem.TransferTo(targetMindId, null);
            // Do the transfer.
            if (performerMind != null)
                _mindSystem.TransferTo(performerMindId, target, ghostCheckOverride: true, false, performerMind);

            if (targetMind != null)
                _mindSystem.TransferTo(targetMindId, performer, ghostCheckOverride: true, false, targetMind);

            if (end)
            {
                var performerMindPowerComp = Comp<MindSwappedComponent>(performer);
                var targetMindPowerComp = Comp<MindSwappedComponent>(target);
                _actions.RemoveAction(performer, performerMindPowerComp.MindSwapReturnActionEntity);
                _actions.RemoveAction(target, targetMindPowerComp.MindSwapReturnActionEntity);

                RemComp<MindSwappedComponent>(performer);
                RemComp<MindSwappedComponent>(target);
                return;
            }

            var perfComp = EnsureComp<MindSwappedComponent>(performer);
            var targetComp = EnsureComp<MindSwappedComponent>(target);

            perfComp.OriginalEntity = target;
            targetComp.OriginalEntity = performer;
        }

        public void GetTrapped(EntityUid uid)
        {

            _popupSystem.PopupEntity(Loc.GetString("mindswap-trapped"), uid, uid, Shared.Popups.PopupType.LargeCaution);
            var perfComp = EnsureComp<MindSwappedComponent>(uid);
            // Claw Command - RemoveAction no longer takes a trailing ActionsComponent argument.
            _actions.RemoveAction(uid, perfComp.MindSwapReturnActionEntity);

            if (HasComp<TelegnosticProjectionComponent>(uid))
            {
                RemComp<PsionicallyInvisibleComponent>(uid);
                RemComp<StealthComponent>(uid);
                EnsureComp<SpeechComponent>(uid);
                EnsureComp<DispellableComponent>(uid);
                _metaDataSystem.SetEntityName(uid, Loc.GetString("telegnostic-trapped-entity-name"));
                _metaDataSystem.SetEntityDescription(uid, Loc.GetString("telegnostic-trapped-entity-desc"));
            }
        }
    }
}
