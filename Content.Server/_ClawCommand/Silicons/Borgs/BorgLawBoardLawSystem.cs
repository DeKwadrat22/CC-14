using Content.Server.Silicons.Laws;
using Content.Shared._ClawCommand.Silicons.Borgs;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Silicons.Laws.Components;

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
    [Dependency] private SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        // After ItemSlots so the chassis' starting board actually exists by now, before StartIonStormed so
        // a derelict borg's scrambled laws land on top of the board's rather than being wiped by them.
        SubscribeLocalEvent<BorgLawBoardComponent, MapInitEvent>(OnMapInit,
            after: [typeof(ItemSlotsSystem)],
            before: [typeof(StartIonStormedSystem)]);
        SubscribeLocalEvent<BorgLawBoardComponent, BorgLawBoardChangedEvent>(OnBoardChanged);
    }

    private void OnMapInit(Entity<BorgLawBoardComponent> ent, ref MapInitEvent args)
    {
        if (_lawBoard.GetLawBoard(ent.AsNullable()) is not { } boardUid ||
            !TryComp<SiliconLawProviderComponent>(boardUid, out var board))
            return;

        _siliconLaw.ApplyLawBoard(ent.Owner, (boardUid, board));
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
