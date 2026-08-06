using Content.Shared.Abilities.Psionics;
using Content.Shared.Popups;
using Content.Shared.Actions.Events;

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class MetapsionicPowerSystem : EntitySystem
    {
        [Dependency] private EntityLookupSystem _lookup = default!;
        [Dependency] private SharedPopupSystem _popups = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private SharedTransformSystem _transform = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MetapsionicPowerComponent, MetapsionicPowerActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(EntityUid uid, MetapsionicPowerComponent component, MetapsionicPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "metapsionic pulse"))
                return;

            foreach (var entity in _lookup.GetEntitiesInRange(uid, component.Range))
            {
                if (!IsPsychicPresence(uid, entity))
                    continue;

                _popups.PopupEntity(Loc.GetString("metapsionic-pulse-success"), uid, uid, PopupType.LargeCaution);
                args.Handled = true;
                return;
            }

            _popups.PopupEntity(Loc.GetString("metapsionic-pulse-failure"), uid, uid, PopupType.Large);
            _psionics.LogPowerUsed(uid, "metapsionic pulse", 2, 4);

            args.Handled = true;
        }

        /// <summary>
        ///     Whether a pulse from <paramref name="user"/> should count <paramref name="entity"/> as a psychic
        ///     presence worth reporting.
        /// </summary>
        /// <remarks>
        ///     Claw Command - upstream only excluded psionic-granting clothing whose <c>ParentUid</c> was the caster
        ///     themselves. That misses two cases, and both make the power report the caster's own belongings back at
        ///     them, which is exactly the failure mode that makes a detection power useless:
        ///
        ///     1. The lookup runs with <see cref="LookupFlags.All"/>, which includes contained entities, so anything
        ///        one container deeper than a worn slot - a psionic item in a backpack, a satchel, a belt pouch -
        ///        is parented to the bag rather than to the caster and slips past the shallow check.
        ///     2. Only <see cref="ClothingGrantPsionicPowerComponent"/> was excluded, so any *other* psionic entity
        ///        the caster happened to be carrying still tripped the pulse.
        ///
        ///     Walking the whole transform ancestry covers both: if the caster is anywhere above the entity, it is
        ///     the caster's own and is not news to them.
        ///
        ///     Psionic-granting garments are now skipped outright rather than only when carried. A pair of strange
        ///     spectacles sitting in a locker is not a psychic - and while one is being worn its wearer holds a
        ///     PsionicComponent of their own, so the person is still detected. Nothing is lost by ignoring the cloth.
        /// </remarks>
        private bool IsPsychicPresence(EntityUid user, EntityUid entity)
        {
            if (entity == user
                || !HasComp<PsionicComponent>(entity)
                || HasComp<PsionicInsulationComponent>(entity)
                || HasComp<ClothingGrantPsionicPowerComponent>(entity))
                return false;

            return !_transform.ContainsEntity(user, entity);
        }
    }
}
