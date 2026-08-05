using Content.Shared.Abilities.Psionics;
using Content.Shared.Atmos.Components; // Claw Command - FlammableComponent moved to Shared
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.Nutrition.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Smoking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;

// FloofStation Modified

namespace Content.Server.Abilities.Psionics
{
    public sealed partial class PyrokinesisPowerSystem : EntitySystem
    {
        [Dependency] private SharedCombatModeSystem _combat = default!;
        [Dependency] private FlammableSystem _flammableSystem = default!;
        [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private InventorySystem _inventory = default!;
        [Dependency] private SharedHandsSystem _hands = default!;
        [Dependency] private SmokingSystem _smoking = default!;
        [Dependency] private ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PyrokinesisPowerActionEvent>(OnPowerUsed);
        }
        private void OnPowerUsed(PyrokinesisPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "pyrokinesis"))
                return;

            // Light your (or friend's!) ciggy with style if possible (before considering setting aflame.)
            if (
                !_combat.IsInCombatMode(args.Performer) &&
                _inventory.TryGetSlotEntity(args.Target, "mask", out var targetEntity) &&
                TryComp<SmokableComponent>(targetEntity, out var smokableComponentOnSomeone) &&
                smokableComponentOnSomeone.State == SmokableState.Unlit
            )
            {
                IgniteWornSmokable(args, (targetEntity.Value, smokableComponentOnSomeone));
                args.Handled = true;
            }
            else if (
                TryComp<SmokableComponent>(args.Target, out var smokableComponentWorld) &&
                smokableComponentWorld.State == SmokableState.Unlit
            )
            {
                IgniteSmokable(args, smokableComponentWorld);
                args.Handled = true;
            }
            else if (TryComp<FlammableComponent>(args.Target, out var flammableComponent))
            {
                flammableComponent.FireStacks += 5;
                _flammableSystem.Ignite(args.Target, args.Target);
                _popupSystem.PopupEntity(Loc.GetString("pyrokinesis-power-used", ("target", args.Target)), args.Target, PopupType.LargeCaution);
                args.Handled = true;
            }
            if (args.Handled)
                _psionics.LogPowerUsed(args.Performer, "pyrokinesis");
        }

        private void IgniteSmokable(PyrokinesisPowerActionEvent args, SmokableComponent smokableComponent)
        {
            _smoking.SetSmokableState(args.Target, SmokableState.Lit, smokableComponent);
            _popupSystem.PopupEntity(Loc.GetString("pyrokinesis-power-used-smokable", ("target", args.Target)), args.Target, PopupType.LargeCaution);
        }

        private void IgniteWornSmokable(PyrokinesisPowerActionEvent args, Entity<SmokableComponent> smokable)
        {
            var onSelf = args.Performer == args.Target;
            var handTrick = onSelf && _hands.TryGetEmptyHand(args.Performer, out _);
            var otherLocString = handTrick ?
                "pyrokinesis-power-used-smokable-performance" : "pyrokinesis-power-used-smokable-performance-no-hands";
            var otherLocalizedString = Loc.GetString(otherLocString,
                ("performer", args.Performer),
                ("target", args.Target),
                ("targetEntity", smokable)
            );
            _smoking.SetSmokableState(smokable, SmokableState.Lit, smokable);
            if (handTrick) _chat.TryEmoteWithChat(args.Performer, "Snap");
            if (onSelf)
                _popupSystem.PopupEntity(
                    otherLocalizedString,
                    args.Target,
                    PopupType.Small
                );
            else // Get the target's attention!
            {
                var targetLocalizedString = Loc.GetString("pyrokinesis-power-used-smokable-not-performed-self",
                    ("performer", args.Performer),
                    ("target", args.Target),
                    ("targetEntity", smokable)
                );
                // Claw Command - upstream used a Floofstation-only PopupEntity overload that takes a separate
                // message for the target. That overload does not exist here, so the same effect is produced with
                // two calls: everyone except the target sees the flavour text, the target gets the warning.
                _popupSystem.PopupEntity(
                    otherLocalizedString,
                    args.Target,
                    Filter.PvsExcept(args.Target),
                    true,
                    PopupType.Small
                );
                _popupSystem.PopupEntity(
                    targetLocalizedString,
                    args.Target,
                    args.Target,
                    PopupType.MediumCaution
                );
            }
        }
    }
}
