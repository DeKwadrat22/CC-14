using Content.Server.Chat.Systems;
using Content.Server.ClawCommand.Cabinet;
using Content.Server._ClawCommand.Station.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Access.Systems;
using Content.Server.ClawCommand.Cabinet.Events;

namespace Content.Server._ClawCommand.Station.Systems;

public sealed partial class EmergencyAccessMedbayStateSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private AirlockSystem _airlockSystem = default!;
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;

    private bool _eaEnabled = true;
    private TimeSpan _acoDelay = TimeSpan.FromMinutes(10);
    private int _maxDoctorsForEA = 1;
    private ISawmill _log = default!;

    public override void Initialize()
    {

        SubscribeLocalEvent<EmergencyAccessMedbayStateComponent, PlayerJobAddedEvent>(OnPlayerJobAdded);
        SubscribeLocalEvent<EmergencyAccessMedbayStateComponent, PlayerJobsRemovedEvent>(OnPlayerJobsRemoved); _log = Logger.GetSawmill("mapsel");

        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        var currentTime = _ticker.RoundDuration();
        if (currentTime < _acoDelay)
            return;

        var query = EntityQueryEnumerator<EmergencyAccessMedbayStateComponent>();
        while (query.MoveNext(out var station, out var captainState))
        {

            if (captainState.DoctorCount <= _maxDoctorsForEA)
                HandleNoDoctors(station, captainState, currentTime);
            else
            {
                if (captainState.IsAAInPlay)
                {
                    captainState.IsAAInPlay = false;
                    _chat.DispatchStationAnnouncement(station, Loc.GetString(captainState.RevokeACOMessage), colorOverride: Color.Yellow);

                    var qquery = EntityQueryEnumerator<AirlockComponent>();
                    while (qquery.MoveNext(out var airlockID, out var airlockComp))
                    {

                        if (_accessReaderSystem.GetMainAccessReader(airlockID, out var mainReader))
                        {

                            if (mainReader.Value.Comp.AccessLists.Any(list => list.Contains("Medical")))
                            {

                                _airlockSystem.SetEmergencyAccess((airlockID, airlockComp), false);
                            }
                        }

                    }
                }
            }
        }
    }

    private void OnPlayerJobAdded(Entity<EmergencyAccessMedbayStateComponent> ent, ref PlayerJobAddedEvent args)
    {
        if (args.JobPrototypeId == "ChiefMedicalOfficer" ||
         args.JobPrototypeId == "MedicalDoctor" ||
          args.JobPrototypeId == "Chemist" ||
          args.JobPrototypeId == "Paramedic")
        {
            ent.Comp.IsAAInPlay = true;
            ent.Comp.DoctorCount += 1;
        }
    }

    private void OnPlayerJobsRemoved(Entity<EmergencyAccessMedbayStateComponent> ent, ref PlayerJobsRemovedEvent args)
    {
        if (!TryComp<StationJobsComponent>(ent, out var stationJobs))
            return;
        if (!args.PlayerJobs.Contains("ChiefMedicalOfficer") ||
        !args.PlayerJobs.Contains("MedicalDoctor") ||
        !args.PlayerJobs.Contains("Chemist") ||
        !args.PlayerJobs.Contains("Paramedic")) // If the player that left was a captain we need to check if there are any captains left
            return;
        if (stationJobs.PlayerJobs.Any(playerJobs => playerJobs.Value.Contains("ChiefMedicalOfficer")) ||
        stationJobs.PlayerJobs.Any(playerJobs => playerJobs.Value.Contains("MedicalDoctor")) ||
       stationJobs.PlayerJobs.Any(playerJobs => playerJobs.Value.Contains("Chemist")) ||
       stationJobs.PlayerJobs.Any(playerJobs => playerJobs.Value.Contains("Paramedic"))
       ) // We check the PlayerJobs if there are any cpatins left
            return;
        ent.Comp.DoctorCount -= 1;
        if (ent.Comp.DoctorCount < 0)
            ent.Comp.DoctorCount = 0;
    }


    /// <summary>
    /// Handles cases for when there is no captain
    /// </summary>
    /// <param name="station"></param>
    /// <param name="EmergencyAccessMedbayState"></param>
    private void HandleNoDoctors(Entity<EmergencyAccessMedbayStateComponent?> station, EmergencyAccessMedbayStateComponent captainState, TimeSpan currentTime)
    {

        if (CheckUnlockAA(captainState, currentTime))
        {
            captainState.IsAAInPlay = true;
            _chat.DispatchStationAnnouncement(station, Loc.GetString(captainState.AAUnlockedMessage), colorOverride: Color.Yellow);

            // Extend access of spare id lockers to command so they can access emergency AA
            var query = EntityQueryEnumerator<AirlockComponent>();
            while (query.MoveNext(out var airlockID, out var airlockComp))
            {

                if (_accessReaderSystem.GetMainAccessReader(airlockID, out var mainReader))
                {

                    if (mainReader.Value.Comp.AccessLists.Any(list => list.Contains("Medical")))
                    {

                        _airlockSystem.SetEmergencyAccess((airlockID, airlockComp), true);
                    }
                }

            }
        }
    }

    /// <summary>
    /// Checks the conditions for if AA should be unlocked
    /// If time is null its condition is ignored
    /// </summary>
    /// <param name="EmergencyAccessMedbayState"></param>
    /// <returns>True if conditions are met for AA to be unlocked, False otherwise</returns>
    private bool CheckUnlockAA(EmergencyAccessMedbayStateComponent captainState, TimeSpan? currentTime)
    {
        if (captainState.IsAAInPlay || !_eaEnabled)
            return false;
        return currentTime == null || currentTime > _acoDelay;
    }
}
