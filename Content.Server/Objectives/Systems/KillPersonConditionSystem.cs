using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Components; // Claw Command
using Content.Shared.Mobs; // Claw Command
using Content.Shared.Objectives.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed partial class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged); // Claw Command
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        // Claw Command - a latched kill stays banked, so the dead check is already satisfied.
        var requireDead = comp.RequireDead && !IsLatched(comp, target.Value);

        args.Progress = GetProgress(target.Value, requireDead, comp.RequireMaroon);
    }

    /// <summary>
    /// Claw Command - Banks the kill on any objective targeting the mind of a mob that just died.
    /// Progress is only ever polled on demand, so without this a target who is killed and then
    /// revived before anyone opens their objectives would never register as having died at all.
    /// </summary>
    // By value, not by ref: MobStateChangedEvent already has by-value broadcast subscribers elsewhere,
    // and the bus refuses to mix the two.
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<MindContainerComponent>(args.Target, out var container) || container.Mind is not { } mind)
            return;

        var query = EntityQueryEnumerator<KillPersonConditionComponent, TargetObjectiveComponent>();
        while (query.MoveNext(out _, out var comp, out var target))
        {
            if (comp.LatchOnDeath && target.Target == mind)
                comp.Latched = true;
        }
    }

    /// <summary>
    /// Claw Command - Whether this objective's kill is banked. Also latches on poll, to catch bodies
    /// that left play without a state change to <see cref="MobState.Dead"/> first, such as a gib.
    /// </summary>
    private bool IsLatched(KillPersonConditionComponent comp, EntityUid target)
    {
        if (!comp.LatchOnDeath || comp.Latched)
            return comp.Latched;

        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            comp.Latched = true;

        return comp.Latched;
    }

    private float GetProgress(EntityUid target, bool requireDead, bool requireMaroon)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
            return 1f;

        var targetDead = _mind.IsCharacterDeadIc(mind);
        var targetMarooned = !_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) || _mind.IsCharacterUnrevivableIc(mind);
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled) && requireMaroon)
        {
            requireDead = true;
            requireMaroon = false;
        }

        if (requireDead && !targetDead)
            return 0f;

        // Always failed if the target needs to be marooned and the shuttle hasn't even arrived yet
        if (requireMaroon && !_emergencyShuttle.EmergencyShuttleArrived)
            return 0f;

        // If the shuttle hasn't left, give 50% progress if the target isn't on the shuttle as a "almost there!"
        if (requireMaroon && !_emergencyShuttle.ShuttlesLeft)
            return targetMarooned ? 0.5f : 0f;

        // If the shuttle has already left, and the target isn't on it, 100%
        if (requireMaroon && _emergencyShuttle.ShuttlesLeft)
            return targetMarooned ? 1f : 0f;

        return 1f; // Good job you did it woohoo
    }
}
