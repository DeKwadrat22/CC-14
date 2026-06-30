using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Station.Components;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.StationRecords;

/// <summary>
/// Claw Command: register cyborg / dogborg players in the station crew manifest.
///
/// Upstream's StationRecordsSystem.OnPlayerSpawn early-returns when the spawned mob
/// has no "id" inventory slot, because it tries to link the new GeneralStationRecord
/// to an ID card via SetIdKey. Borg chassis entities have no inventory at all — their
/// identity lives in BorgChassisComponent — so they silently never get a record,
/// which means they're invisible in:
///   - the Crew Manifest (accessed from Late Join window or any records console)
///   - any GeneralStationRecordConsole listing
///
/// This system runs on PlayerSpawnCompleteEvent (fires for both round start and late
/// join), spots any spawn whose mob has BorgChassisComponent, and creates the record
/// manually with idUid: null. The existing CreateGeneralRecord overload tolerates a
/// null idUid (it just skips the SetIdKey assignment), so this is a purely additive
/// hook with no upstream changes.
///
/// Skipped on purpose:
///   - IPC species: they have an "id" inventory slot and a PDA, so the upstream path
///     already creates their records correctly.
///   - StationAI: spawns via its own pipeline that doesn't raise PlayerSpawnCompleteEvent.
///   - Ghost-role derelict borgs: those don't fire PlayerSpawnCompleteEvent either.
/// </summary>
public sealed partial class SiliconCrewManifestSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<BorgChassisComponent>(args.Mob))
            return;

        if (string.IsNullOrEmpty(args.JobId) || !_prototype.HasIndex<JobPrototype>(args.JobId))
            return;

        if (!TryComp<StationRecordsComponent>(args.Station, out var records))
            return;

        // The borg's display name is whatever BorgSpawnNameSystem stamped onto the
        // mob (the character profile name). Fall back to the profile name directly,
        // then to the entity's current metadata name if both are empty.
        var name = args.Profile.Name;
        if (string.IsNullOrWhiteSpace(name))
            name = Name(args.Mob);

        _records.CreateGeneralRecord(
            args.Station,
            idUid: null,
            name,
            args.Profile.Age,
            args.Profile.Species,
            args.Profile.Gender,
            args.JobId,
            mobFingerprint: null,
            dna: null,
            args.Profile,
            records);
    }
}
