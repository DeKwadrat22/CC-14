using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnPointComponent : Component, ISpawnPoint
{
    /// <summary>
    /// The job this spawn point is valid for.
    /// Null will allow all jobs to spawn here.
    /// </summary>
    [DataField("job_id")]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Extra jobs whose players are also allowed to use this spawn point.
    /// Used by _ClawCommand so e.g. the SpawnPointBorg accepts all dogborg jobs
    /// without needing per-variant spawn point entities on every map.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> AdditionalJobs = new();

    /// <summary>
    /// The type of spawn point.
    /// </summary>
    [DataField("spawn_type"), ViewVariables(VVAccess.ReadWrite)]
    public SpawnPointType SpawnType { get; set; } = SpawnPointType.Unset;

    public override string ToString()
    {
        return $"{Job} {SpawnType}";
    }
}

public enum SpawnPointType
{
    Unset = 0,
    LateJoin,
    Job,
    Observer,
}
