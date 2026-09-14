using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Body.Components; // Claw Command - BloodstreamComponent moved to Shared
using Content.Shared.Body.Systems;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Psionics.Events;
using Content.Shared.Examine;
using static Content.Shared.Examine.ExamineSystemShared;
using Robust.Shared.Timing;
using Content.Shared.Actions.Events;
using Robust.Server.Audio;

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class PsionicRegenerationPowerSystem : EntitySystem
    {
        // Claw Command - RA0033: Solution.AddReagent forbids literals.
        private static readonly ProtoId<ReagentPrototype> RegenerationEssence = "PsionicRegenerationEssence";
        [Dependency] private BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private AudioSystem _audioSystem = default!;
        [Dependency] private DoAfterSystem _doAfterSystem = default!;
        [Dependency] private SharedPopupSystem _popupSystem = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private ExamineSystemShared _examine = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, PsionicRegenerationPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<PsionicRegenerationPowerComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, PsionicRegenerationDoAfterEvent>(OnDoAfter);
        }


        private void OnPowerUsed(EntityUid uid, PsionicRegenerationPowerComponent component, PsionicRegenerationPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "psionic regeneration"))
                return;

            var ev = new PsionicRegenerationDoAfterEvent(_gameTiming.CurTime);
            var doAfterArgs = new DoAfterArgs(EntityManager, uid, component.UseDelay, ev, uid);

            _doAfterSystem.TryStartDoAfter(doAfterArgs, out var doAfterId);

            component.DoAfter = doAfterId;

            _popupSystem.PopupEntity(Loc.GetString("psionic-regeneration-begin", ("entity", uid)),
                uid,
                // TODO: Use LoS-based Filter when one is available.
                Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !_examine.InRangeUnOccluded(uid, entity, ExamineRange, null)),
                true,
                PopupType.Medium);

            _audioSystem.PlayPvs(component.SoundUse, component.Owner, AudioParams.Default.WithVolume(8f).WithMaxDistance(1.5f).WithRolloffFactor(3.5f));
            _psionics.LogPowerUsed(uid, "psionic regeneration");
            args.Handled = true;
        }

        private void OnDispelled(EntityUid uid, PsionicRegenerationPowerComponent component, DispelledEvent args)
        {
            if (component.DoAfter == null)
                return;

            _doAfterSystem.Cancel(component.DoAfter);
            component.DoAfter = null;

            args.Handled = true;
        }

        private void OnDoAfter(EntityUid uid, PsionicRegenerationPowerComponent component, PsionicRegenerationDoAfterEvent args)
        {
            component.DoAfter = null;

            if (!TryComp<BloodstreamComponent>(uid, out var stream))
                return;

            // DoAfter has no way to run a callback during the process to give
            // small doses of the reagent, so we wait until either the action
            // is cancelled (by being dispelled) or complete to give the
            // appropriate dose. A timestamp delta is used to accomplish this.
            var percentageComplete = Math.Min(1f, (_gameTiming.CurTime - args.StartedAt).TotalSeconds / component.UseDelay);

            var solution = new Solution();
            solution.AddReagent(RegenerationEssence, FixedPoint2.New(component.EssenceAmount * percentageComplete));
            // Claw Command - TryAddToChemicals was renamed to TryAddToBloodstream and now takes an Entity<T>.
            _bloodstreamSystem.TryAddToBloodstream((uid, stream), solution);
        }
    }
}

