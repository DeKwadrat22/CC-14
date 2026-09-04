using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared._ClawCommand.SyndieOutpost; // Claw Command

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private SuitSensorSystem _sensors = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (servers.MoveNext(out var id, out var server))
        {
            // Claw Command: a syndicate outpost's crew-monitor server sits on a grid that gets
            // added to the real station (SyndieOutpostSpawnRule.AddGridToStation), so it would
            // otherwise count as a second station server and broadcast its own (empty) sensor
            // list over the CrewMonitor frequency, wiping the real station console every tick.
            // The outpost console receives its data through the direct hack tap
            // (SyndieOutpostHackSystem) instead, so this server must never broadcast.
            if (HasComp<SyndieOutpostHackComponent>(id))
                continue;

            if (!_singletonServerSystem.IsActiveServer(id))
                continue;

            UpdateTimeout(id);
            BroadcastSensorStatus(id, server);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for too long.
    /// Two upstream bugs fixed here:
    /// 1) `dif.Seconds` returns only the seconds component (0-59), not the elapsed total — so
    ///    a 70-second gap evaluates to 10 and timed-out sensors were getting kept while
    ///    sensors that had been gone for over a minute looked fresh.
    /// 2) Calling `Dictionary.Remove` from inside `foreach` on the same dictionary throws
    ///    `InvalidOperationException` on the next MoveNext, aborting the broadcast tick.
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        List<string>? toRemove = null;
        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.TotalSeconds > component.SensorTimeout)
                (toRemove ??= new List<string>()).Add(address);
        }

        if (toRemove == null)
            return;

        foreach (var address in toRemove)
            component.SensorStatus.Remove(address);
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    /// <summary>
    /// Clears sensor data on disconnect
    /// </summary>
    private void OnDisconnected(EntityUid uid, CrewMonitoringServerComponent component, ref DeviceNetServerDisconnectedEvent _)
    {
        component.SensorStatus.Clear();
    }
}
