using Content.Server.Silicons.Borgs;
using Content.Server.Silicons.Laws;
using Content.Shared._ClawCommand.Silicons.Borgs;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.Silicons.Borgs;

/// <summary>
/// CLAW COMMAND - server half of the borg law board: rewrites a borg's laws when a board is slotted in
/// and wipes them when it is pulled back out. The interaction itself (do-after, panel/lock checks) and the
/// container plumbing live in the shared <see cref="BorgLawBoardSystem"/>, which relays
/// <see cref="BorgLawBoardChangedEvent"/> here.
/// </summary>
public sealed partial class BorgLawBoardLawSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private BorgLawBoardSystem _lawBoard = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        // After ItemSlots so the chassis' starting board actually exists by now, before StartIonStormed so
        // a derelict borg's scrambled laws land on top of the board's rather than being wiped by them.
        // After ItemSlots and the type system so a chassis that pre-picks its type (every dogborg) has its
        // department board in already; this only fills in the factory default when nothing else did.
        SubscribeLocalEvent<BorgLawBoardComponent, MapInitEvent>(OnMapInit,
            after: [typeof(ItemSlotsSystem), typeof(BorgSwitchableTypeSystem)],
            before: [typeof(StartIonStormedSystem)]);
        SubscribeLocalEvent<BorgLawBoardComponent, BorgLawBoardChangedEvent>(OnBoardChanged);
    }

    private void OnMapInit(Entity<BorgLawBoardComponent> ent, ref MapInitEvent args)
    {
        TrySpawnDefaultBoard(ent);

        if (_lawBoard.GetLawBoard(ent.AsNullable()) is not { } boardUid ||
            !TryComp<SiliconLawProviderComponent>(boardUid, out var board))
            return;

        _siliconLaw.ApplyLawBoard(ent.Owner, (boardUid, board));
    }

    /// <summary>
    /// Fits the chassis' factory-default board, unless it was mapped or filled with one already.
    /// </summary>
    private void TrySpawnDefaultBoard(Entity<BorgLawBoardComponent> ent)
    {
        if (ent.Comp.DefaultBoard is not { } proto)
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot) || slot.HasItem)
            return;

        InsertBoard(ent, slot, proto);
    }

    /// <summary>
    /// Swaps in the law board a borg type is issued with, called when a chassis' type is applied.
    /// A board someone deliberately installed - anything other than the chassis' factory board - is left
    /// alone, so this only ever upgrades a borg straight out of assembly.
    /// </summary>
    public void TrySetTypeLawBoard(Entity<BorgLawBoardComponent?> ent, EntProtoId proto)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.SlotId, out var slot))
            return;

        if (slot.Item is { } existing)
        {
            if (Prototype(existing)?.ID != ent.Comp.DefaultBoard)
                return;

            // The factory board is a part of assembly, not loot - don't litter one per borg.
            _itemSlots.TryEject(ent.Owner, slot, user: null, out var ejected, excludeUserAudio: true);
            if (ejected != null)
                QueueDel(ejected.Value);
        }

        InsertBoard((ent.Owner, ent.Comp), slot, proto);
    }

    private void InsertBoard(Entity<BorgLawBoardComponent> ent, ItemSlot slot, EntProtoId proto)
    {
        var board = Spawn(proto, MapCoordinates.Nullspace);

        if (!_itemSlots.TryInsert(ent.Owner, slot, board, user: null, excludeUserAudio: true))
        {
            QueueDel(board);
            return;
        }

        // Board changes after map init apply their laws off BorgLawBoardChangedEvent, but this also runs
        // during map init - where that relay deliberately does nothing - so apply them here too.
        if (TryComp<SiliconLawProviderComponent>(board, out var provider))
            _siliconLaw.ApplyLawBoard(ent.Owner, (board, provider));
    }

    private void OnBoardChanged(Entity<BorgLawBoardComponent> ent, ref BorgLawBoardChangedEvent args)
    {
        if (!TryComp<SiliconLawProviderComponent>(args.Board, out var board))
            return;

        // The board a chassis spawns with is slotted in during map init, which OnMapInit already handles in
        // the right order relative to everything else that touches laws at spawn.
        if (LifeStage(ent.Owner) < EntityLifeStage.MapInitialized)
            return;

        if (args.Installed)
        {
            _siliconLaw.ApplyLawBoard(ent.Owner, (args.Board, board));

            _adminLog.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(ent)} had the lawset {board.Laws} installed via {ToPrettyString(args.Board)}");
        }
        else
        {
            _siliconLaw.ClearLaws(ent.Owner);

            _adminLog.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(ent)} had its law board {ToPrettyString(args.Board)} removed and is now lawless");
        }
    }
}
