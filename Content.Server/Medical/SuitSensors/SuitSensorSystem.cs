using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed partial class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            // check if sensor is ready to update
            if (curTime < sensor.NextUpdate)
                continue;
            sensor.NextUpdate += sensor.UpdateRate;

            if (!CheckSensorAssignedStation((uid, sensor)))
                continue;

            // get sensor status
            var status = GetSensorState((uid, sensor));
            if (status == null)
                continue;

            // Claw Command: periodically nudge the cached ConnectedServer so it
            // self-heals from stale state — the singleton crew monitor server
            // flickered off and came back under a new address, the station's
            // active server got swapped mid-round, a sensor crossed a station
            // boundary, etc. The block immediately below does the actual lookup
            // when ConnectedServer is null. We never touch the broadcast path,
            // the timeout dict, or the per-sensor NextUpdate cadence — if the
            // refresh lookup happens to fail this tick we just skip the same
            // way an initial lookup would, and try again next tick.
            if (curTime >= sensor.NextServerResolve)
            {
                sensor.ConnectedServer = null;
                sensor.NextServerResolve = curTime + sensor.ServerResolveInterval;
            }

            //Retrieve active server address if the sensor isn't connected to a server
            if (sensor.ConnectedServer == null)
            {
                if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(sensor.StationId!.Value, out var address))
                    continue;

                sensor.ConnectedServer = address;
            }

            // Clear the connected server if its address isn't on the network
            if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, sensor.ConnectedServer))
            {
                sensor.ConnectedServer = null;
                continue;
            }

            var payload = new SuitSensorStatusPayload
            {
                Data = status.Value,
            };
            _deviceNetworkSystem.SendPacket((uid, device), sensor.ConnectedServer, ref payload);
        }
    }
}
