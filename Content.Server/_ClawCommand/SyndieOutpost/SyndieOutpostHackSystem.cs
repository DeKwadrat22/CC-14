using System.Linq;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared._ClawCommand.SyndieOutpost;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._ClawCommand.SyndieOutpost;

/// <summary>
/// Handles syndicate outpost hack rolls on MapInit.
/// When hack succeeds, periodically copies crew monitoring data from the station's
/// server directly to the outpost's console, bypassing all device network restrictions.
/// Camera hacking is handled directly in SurveillanceCameraMonitorSystem.
/// </summary>
public sealed partial class SyndieOutpostHackSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    private const float UpdateInterval = 3f;
    private float _updateAccumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SyndieOutpostHackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SyndieOutpostHackComponent component, MapInitEvent args)
    {
        component.HackSucceeded = _random.Prob(component.HackChance);
        Log.Debug($"Syndicate outpost hack {(component.HackSucceeded ? "SUCCEEDED" : "FAILED")} for {ToPrettyString(uid)}");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;
        _updateAccumulator -= UpdateInterval;

        // Find station crew monitoring server data (skip outpost servers)
        var serverQuery = EntityQueryEnumerator<CrewMonitoringServerComponent>();
        Dictionary<string, SuitSensorStatus>? stationSensorData = null;

        while (serverQuery.MoveNext(out var serverUid, out var serverComp))
        {
            if (HasComp<SyndieOutpostHackComponent>(serverUid))
                continue;

            if (serverComp.SensorStatus.Count > 0)
            {
                stationSensorData = serverComp.SensorStatus;
                break;
            }
        }

        if (stationSensorData == null)
            return;

        // Copy to hacked outpost consoles
        var consoleQuery = EntityQueryEnumerator<SyndieOutpostHackComponent, CrewMonitoringConsoleComponent>();
        while (consoleQuery.MoveNext(out var consoleUid, out var hack, out var console))
        {
            if (!hack.HackSucceeded)
                continue;

            console.ConnectedSensors = new Dictionary<string, SuitSensorStatus>(stationSensorData);

            if (_uiSystem.IsUiOpen(consoleUid, CrewMonitoringUIKey.Key))
            {
                var allSensors = console.ConnectedSensors.Values.ToList();
                _uiSystem.SetUiState(consoleUid, CrewMonitoringUIKey.Key, new CrewMonitoringState(allSensors));
            }
        }
    }
}
