using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Gibbing;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// CLAW COMMAND - handles physically installing and removing a silicon law board on a borg chassis.
///
/// Law boards used to only be usable on the AI upload console; this makes the same boards the source of
/// a borg's laws. Clicking an unscrewed and unlocked chassis with a board in hand runs a do-after and
/// slots the board in, and popping the board back out leaves the borg lawless - which also keeps it from
/// being activated at all (see <see cref="BorgActivateAttemptEvent"/>).
///
/// The laws themselves are rewritten server-side by <c>SiliconLawSystem.ApplyLawBoard</c>, since lawsets
/// aren't predicted.
/// </summary>
public sealed partial class BorgLawBoardSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedBorgSystem _borg = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgLawBoardComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BorgLawBoardComponent, BorgLawBoardInstallDoAfterEvent>(OnInstallDoAfter);
        SubscribeLocalEvent<BorgLawBoardComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BorgLawBoardComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
        SubscribeLocalEvent<BorgLawBoardComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BorgLawBoardComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<BorgLawBoardComponent, BorgActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<BorgLawBoardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BorgLawBoardComponent, GibbedBeforeDeletionEvent>(OnBeingGibbed);
    }

    // Drop the board with the rest of the borg's parts instead of deleting it along with the chassis.
    private void OnBeingGibbed(Entity<BorgLawBoardComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        if (_container.TryGetContainer(ent.Owner, ent.Comp.SlotId, out var container))
            _container.EmptyContainer(container);
    }

    // Only visible with the panel open, same as the rest of the borg's guts.
    private void OnExamined(Entity<BorgLawBoardComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.RequirePanelOpen && TryComp<WiresPanelComponent>(ent, out var panel) && !panel.Open)
            return;

        if (GetLawBoard(ent.AsNullable()) is { } board)
            args.PushMarkup(Loc.GetString("clawcommand-borg-lawboard-examine", ("board", Name(board))));
        else
            args.PushMarkup(Loc.GetString("clawcommand-borg-lawboard-examine-empty"));
    }

    /// <summary>
    /// Is there a law board sitting in this borg right now?
    /// </summary>
    public bool HasLawBoard(Entity<BorgLawBoardComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return _itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot) && slot.HasItem;
    }

    /// <summary>
    /// The board currently installed in this borg, if any.
    /// </summary>
    public EntityUid? GetLawBoard(Entity<BorgLawBoardComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        return _itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot) ? slot.Item : null;
    }

    /// <summary>
    /// Checks that the chassis is opened up enough to get at its law board, popping up why it isn't.
    /// </summary>
    private bool CanSwapBoard(Entity<BorgLawBoardComponent> ent, EntityUid? user, bool popup)
    {
        // Borgs can't reach into their own guts to swap their own laws.
        if (user == ent.Owner)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("clawcommand-borg-lawboard-self"), ent, user);
            return false;
        }

        if (ent.Comp.RequirePanelOpen && TryComp<WiresPanelComponent>(ent, out var panel) && !panel.Open)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("borg-panel-not-open"), ent, user);
            return false;
        }

        if (ent.Comp.RequireUnlocked && TryComp<LockComponent>(ent, out var lockComp) && lockComp.Locked)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("clawcommand-borg-lawboard-locked"), ent, user);
            return false;
        }

        return true;
    }

    private void OnInteractUsing(Entity<BorgLawBoardComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // Only law boards, everything else (brains, modules, tools) is somebody else's job.
        if (!HasComp<SiliconLawProviderComponent>(args.Used))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot))
            return;

        args.Handled = true;

        if (!CanSwapBoard(ent, args.User, true))
            return;

        if (slot.HasItem)
        {
            _popup.PopupClient(Loc.GetString("clawcommand-borg-lawboard-occupied"), ent, args.User);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.InstallDelay,
            new BorgLawBoardInstallDoAfterEvent(),
            ent.Owner,
            ent.Owner,
            args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        });
    }

    private void OnInstallDoAfter(Entity<BorgLawBoardComponent> ent, ref BorgLawBoardInstallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } board)
            return;

        args.Handled = true;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot) || slot.HasItem)
            return;

        if (!CanSwapBoard(ent, args.User, true))
            return;

        if (!_itemSlots.TryInsert(ent.Owner, slot, board, args.User))
            return;

        _popup.PopupClient(Loc.GetString("clawcommand-borg-lawboard-installed"), ent, args.User);
    }

    // The panel/lock requirement also covers the eject verb and any other way of getting at the slot.
    private void OnInsertAttempt(Entity<BorgLawBoardComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled || args.Slot.ID != ent.Comp.SlotId)
            return;

        // Starting items are spawned in with no user, don't block those.
        if (args.User == null)
            return;

        if (!CanSwapBoard(ent, args.User, false))
            args.Cancelled = true;
    }

    private void OnEjectAttempt(Entity<BorgLawBoardComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled || args.Slot.ID != ent.Comp.SlotId)
            return;

        if (args.User == null)
            return;

        if (!CanSwapBoard(ent, args.User, true))
            args.Cancelled = true;
    }

    private void OnInserted(Entity<BorgLawBoardComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.SlotId)
            return;

        // Laws are back, so the borg is allowed to run again.
        if (TryComp<BorgChassisComponent>(ent, out var chassis))
            _borg.TryActivate((ent.Owner, chassis));

        var ev = new BorgLawBoardChangedEvent(args.Entity, true);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void OnRemoved(Entity<BorgLawBoardComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.SlotId)
            return;

        // No lawset, no borg.
        if (TryComp<BorgChassisComponent>(ent, out var chassis))
            _borg.SetActive((ent.Owner, chassis), false);

        var ev = new BorgLawBoardChangedEvent(args.Entity, false);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void OnActivateAttempt(Entity<BorgLawBoardComponent> ent, ref BorgActivateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasLawBoard(ent.AsNullable()))
            args.Cancelled = true;
    }
}
