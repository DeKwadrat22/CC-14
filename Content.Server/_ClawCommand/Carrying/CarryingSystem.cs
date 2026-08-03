using System.Numerics;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Resist;
using Content.Shared._ClawCommand.Carrying;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;

namespace Content.Server._ClawCommand.Carrying
{
    /// <summary>
    ///     Fireman carry. Ported from the space fork's Nyanotrasen-derived carrying system.
    ///     Adapted for this fork: the PseudoItem insert verb and the polymorph hook are dropped
    ///     (neither system exists here), and ContestsSystem is replaced by the local mass/stamina
    ///     helpers at the bottom of this file.
    /// </summary>
    public sealed partial class CarryingSystem : EntitySystem
    {
        [Dependency] private SharedVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private CarryingSlowdownSystem _slowdown = default!;
        [Dependency] private DoAfterSystem _doAfterSystem = default!;
        [Dependency] private StandingStateSystem _standingState = default!;
        [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private PullingSystem _pullingSystem = default!;
        [Dependency] private MobStateSystem _mobStateSystem = default!;
        [Dependency] private EscapeInventorySystem _escapeInventorySystem = default!;
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private SharedHandsSystem _hands = default!;
        [Dependency] private TransformSystem _transform = default!;

        /// <summary>
        ///     Mirrors contests.max_percentage from the fork this was ported from.
        /// </summary>
        private const float MassContestMaxPercentage = 0.25f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
            SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
            SubscribeLocalEvent<BeingCarriedComponent, BuckledEvent>(OnBuckled);
            SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
        }

        private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !_mobStateSystem.IsAlive(args.User)
                || !CanCarry(args.User, uid, component)
                || HasComp<CarryingComponent>(args.User)
                || HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target)
                || args.User == args.Target)
                return;

            var user = args.User;
            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartCarryDoAfter(user, uid, component);
                },
                Text = Loc.GetString("carry-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     Since the carried entity is stored as virtual items, when those are deleted we want to drop them.
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<CarriableComponent>(args.BlockingEntity))
                return;

            DropCarried(uid, args.BlockingEntity);
        }

        /// <summary>
        ///     Virtual item passthrough, so throwing the virtual item throws the carried person instead.
        /// </summary>
        private void OnThrow(EntityUid uid, CarryingComponent component, ref BeforeThrowEvent args)
        {
            if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem)
                || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
                return;

            args.ItemUid = virtItem.BlockingEntity;

            args.ThrowSpeed *= MassContest(uid, virtItem.BlockingEntity, false, 2f)
                            * StaminaContest(uid, virtItem.BlockingEntity);
        }

        private void OnParentChanged(EntityUid uid, CarryingComponent component, ref EntParentChangedMessage args)
        {
            var xform = Transform(uid);
            if (xform.MapUid != args.OldMapId || xform.ParentUid == xform.GridUid)
                return;

            DropCarried(uid, component.Carried);
        }

        private void OnMobStateChanged(EntityUid uid, CarryingComponent component, MobStateChangedEvent args)
        {
            DropCarried(uid, component.Carried);
        }

        /// <summary>
        ///     Only let the person being carried interact with their carrier and things on their person.
        /// </summary>
        private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, ref InteractionAttemptEvent args)
        {
            if (args.Target == null) // Allow self-interacts
                return;

            if (IsChildOfCarrier(Transform(args.Target.Value), component.Carrier)) // Everything on the carrier
                return;

            var targetParent = Transform(args.Target.Value).ParentUid;
            if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
                args.Cancelled = true;
        }

        private bool IsChildOfCarrier(TransformComponent childXform, EntityUid carrier)
        {
            if (childXform.ParentUid == carrier)
                return true;

            return childXform.ParentUid is { Valid: true } parent && IsChildOfCarrier(Transform(parent), carrier);
        }

        /// <summary>
        ///     Try to escape via the escape inventory system.
        /// </summary>
        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape)
                || !args.HasDirectionalMovement)
                return;

            // Escape time scales with the inverse of a mass contest. Being lighter makes escape harder.
            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
            {
                var disadvantage = MassContest(component.Carrier, uid, false, 2f);
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape, disadvantage);
            }
        }

        private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
        {
            args.Cancel();
        }

        private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnStartClimb(EntityUid uid, BeingCarriedComponent component, ref StartClimbEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnBuckled(EntityUid uid, BeingCarriedComponent component, ref BuckledEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
        {
            component.CancelToken = null;
            if (args.Handled || args.Cancelled
                || !CanCarry(args.Args.User, uid, component))
                return;

            Carry(args.Args.User, uid);
            args.Handled = true;
        }

        public void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            if (!TryComp<PhysicsComponent>(carrier, out var carrierPhysics)
                || !TryComp<PhysicsComponent>(carried, out var carriedPhysics)
                || carriedPhysics.Mass > carrierPhysics.Mass * 2f)
            {
                _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
                return;
            }

            var length = TimeSpan.FromSeconds(component.PickupDuration
                        * MassContest(carriedPhysics, carrierPhysics, false, 4f)
                        * StaminaContest(carrier, carried)
                        * (_standingState.IsDown(carried) ? 0.5f : 1));

            component.CancelToken = new CancellationTokenSource();

            var ev = new CarryDoAfterEvent();
            var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
            {
                BreakOnMove = true,
                NeedHand = true
            };

            _doAfterSystem.TryStartDoAfter(args);

            // Show a popup to the person getting picked up
            _popupSystem.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
        }

        private void Carry(EntityUid carrier, EntityUid carried)
        {
            if (TryComp<PullableComponent>(carried, out var pullable))
                _pullingSystem.TryStopPull(carried, pullable);

            // Knock down first, because some systems can break carrying in response to knockdown.
            EnsureComp<KnockedDownComponent>(carried);
            _transform.AttachToGridOrMap(carrier);
            _transform.AttachToGridOrMap(carried);
            _transform.SetCoordinates(carried, Transform(carrier).Coordinates);
            _transform.SetParent(carried, carrier);

            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            if (TryComp<CarriableComponent>(carried, out var carriableComp) && carriableComp.FreeHandsRequired > 1)
                _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);

            var carryingComp = EnsureComp<CarryingComponent>(carrier);
            ApplyCarrySlowdown(carrier, carried);
            var carriedComp = EnsureComp<BeingCarriedComponent>(carried);

            // Claw Command - being carried has to be escapable. OnMoveInput routes the struggle through
            // EscapeInventorySystem, which requires CanEscapeInventoryComponent, and that component only
            // exists on small critters that get stuffed into bags - never on humanoids. Without this a
            // carried player has no way out at all and the carry is effectively permanent.
            //
            // Granted here rather than on the mob prototypes on purpose: putting it on humanoids
            // permanently would also let them struggle out of bags, lockers and body bags, which is a
            // separate balance question. We track whether we added it so the drop can undo exactly that.
            if (!HasComp<CanEscapeInventoryComponent>(carried))
            {
                EnsureComp<CanEscapeInventoryComponent>(carried);
                carriedComp.GrantedEscape = true;
            }

            carryingComp.Carried = carried;
            carriedComp.Carrier = carrier;

            _actionBlockerSystem.UpdateCanMove(carried);
        }

        public bool TryCarry(EntityUid carrier, EntityUid toCarry, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(toCarry, ref carriedComp, false)
                || !CanCarry(carrier, toCarry, carriedComp)
                || HasComp<BeingCarriedComponent>(carrier)
                || HasComp<ItemComponent>(carrier)
                || TryComp<PhysicsComponent>(carrier, out var carrierPhysics)
                && TryComp<PhysicsComponent>(toCarry, out var toCarryPhysics)
                && carrierPhysics.Mass * 2f < toCarryPhysics.Mass)
                return false;

            Carry(carrier, toCarry);

            return true;
        }

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // Get rid of this first so we don't recursively fire that event.
            RemComp<CarryingSlowdownComponent>(carrier);

            // Claw Command - only strip the escape component if the carry is what granted it.
            if (TryComp<BeingCarriedComponent>(carried, out var beingCarried) && beingCarried.GrantedEscape)
                RemComp<CanEscapeInventoryComponent>(carried);

            RemComp<BeingCarriedComponent>(carried);
            RemComp<KnockedDownComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            _transform.AttachToGridOrMap(carried);
            _standingState.Stand(carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            var massRatio = MassContest(carrier, carried, true);
            var massRatioSq = MathF.Pow(massRatio, 2);
            var modifier = 1 - 0.15f / massRatioSq;
            modifier = Math.Max(0.1f, modifier);

            EnsureComp<CarryingSlowdownComponent>(carrier, out var slowdownComp);
            _slowdown.SetModifier(carrier, modifier, modifier, slowdownComp);
        }

        public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(carried, ref carriedComp, false)
                || carriedComp.CancelToken != null
                || !HasComp<MapGridComponent>(Transform(carrier).ParentUid)
                || HasComp<BeingCarriedComponent>(carrier)
                || HasComp<BeingCarriedComponent>(carried)
                || !TryComp<HandsComponent>(carrier, out var hands)
                || _hands.CountFreeHands((carrier, hands)) < carriedComp.FreeHandsRequired)
                return false;

            return true;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BeingCarriedComponent>();
            while (query.MoveNext(out var carried, out var comp))
            {
                var carrier = comp.Carrier;
                if (carrier is not { Valid: true } || carried is not { Valid: true })
                    continue;

                // Sometimes - disposals, cryosleep chambers - an entity gets re-parented without a proper
                // reparent event. When that happens it needs to be dropped, or behaviour gets weird.
                var xform = Transform(carried);
                if (xform.ParentUid != carrier)
                {
                    DropCarried(carrier, carried);
                    continue;
                }

                // Keep the carried entity centred on the carrier; gravity pulls can otherwise offset it.
                if (!xform.LocalPosition.Equals(Vector2.Zero))
                    _transform.SetLocalPosition(carried, Vector2.Zero, xform);
            }
        }

        #region Contests

        // This fork has no ContestsSystem, so the two contests the carrying system actually uses are
        // reimplemented here with the upstream defaults (contests.max_percentage 0.25, clamp override on).

        private float MassContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f)
        {
            if (!TryComp<PhysicsComponent>(performer, out var performerPhysics)
                || !TryComp<PhysicsComponent>(target, out var targetPhysics))
                return 1f;

            return MassContest(performerPhysics, targetPhysics, bypassClamp, rangeFactor);
        }

        private float MassContest(PhysicsComponent performerPhysics, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f)
        {
            if (performerPhysics.Mass == 0 || targetPhysics.InvMass == 0)
                return 1f;

            var ratio = performerPhysics.Mass * targetPhysics.InvMass;

            if (!bypassClamp)
            {
                ratio = Math.Clamp(ratio,
                    1 - MassContestMaxPercentage * rangeFactor,
                    1 + MassContestMaxPercentage * rangeFactor);
            }

            return Math.Clamp(ratio, float.Epsilon, float.MaxValue);
        }

        private float StaminaContest(EntityUid performer, EntityUid target, float rangeFactor = 1f)
        {
            if (!TryComp<StaminaComponent>(performer, out var performerStamina)
                || !TryComp<StaminaComponent>(target, out var targetStamina))
                return 1f;

            var performerRatio = 1 - Math.Clamp(performerStamina.StaminaDamage / performerStamina.CritThreshold, 0, 0.25f * rangeFactor);
            var targetRatio = 1 - Math.Clamp(targetStamina.StaminaDamage / targetStamina.CritThreshold, 0, 0.25f * rangeFactor);

            if (targetRatio == 0)
                return 1f;

            return Math.Clamp(performerRatio / targetRatio, float.Epsilon, float.MaxValue);
        }

        #endregion
    }
}
