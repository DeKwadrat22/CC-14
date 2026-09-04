using Content.Shared.Medical.SuitSensor;
using Content.Server._ClawCommand.SyndieOutpost; // Claw Command

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem),
    typeof(SyndieOutpostHackSystem))] // Claw Command - direct data tap
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;
}
