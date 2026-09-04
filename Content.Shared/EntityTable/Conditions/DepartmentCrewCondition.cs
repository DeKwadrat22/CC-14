// Claw Command - lets scheduled event pools require that a department is actually staffed.
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that passes only if enough crew holding a job in one of the given departments are
/// currently playing. Used to gate events that are unfair (or just unfixable) when the department
/// that is supposed to answer them is not staffed, e.g. meteor swarms without engineers or
/// anomaly spawns without scientists.
/// </summary>
public sealed partial class DepartmentCrewCondition : EntityTableCondition
{
    /// <summary>
    /// The departments to count crew from. A player only counts once, even if multiple are listed.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DepartmentPrototype>> Departments = new();

    /// <summary>
    /// How many crew across those departments are needed for this condition to succeed. Inclusive.
    /// </summary>
    [DataField]
    public int Min = 1;

    /// <summary>
    /// If true, only living crew count. Ghosts, dead and crit players are ignored.
    /// </summary>
    [DataField]
    public bool RequireAlive = true;

    private static ISharedPlayerManager? _playerManager;

    protected override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (Min <= 0)
            return true;

        // Don't resolve this repeatedly
        _playerManager ??= IoCManager.Resolve<ISharedPlayerManager>();

        var jobIds = new HashSet<ProtoId<JobPrototype>>();
        foreach (var departmentId in Departments)
        {
            if (!proto.TryIndex(departmentId, out var department))
                continue;

            jobIds.UnionWith(department.Roles);
        }

        if (jobIds.Count == 0)
            return false;

        var jobs = entMan.System<SharedJobSystem>();
        var found = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } player || !entMan.EntityExists(player))
                continue;

            // A ghost has no MobState, so this also filters out players who left their body.
            if (RequireAlive
                && (!entMan.TryGetComponent<MobStateComponent>(player, out var mobState)
                    || mobState.CurrentState != MobState.Alive))
                continue;

            if (!entMan.TryGetComponent<MindContainerComponent>(player, out var mindContainer)
                || mindContainer.Mind is not { } mind)
                continue;

            if (!jobs.MindTryGetJobId(mind, out var job) || job is not { } jobId)
                continue;

            if (!jobIds.Contains(jobId))
                continue;

            if (++found >= Min)
                return true;
        }

        return false;
    }
}
