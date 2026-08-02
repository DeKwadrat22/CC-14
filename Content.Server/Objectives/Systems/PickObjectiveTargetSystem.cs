using Content.Server.Objectives.Components;
using Content.Shared.EntityConditions; // Claw Command
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetObjectiveComponent"/> using different components.
/// These can be combined with condition components for objective completions in order to create a variety of objectives.
/// </summary>
public sealed partial class PickObjectiveTargetSystem : EntitySystem
{
    [Dependency] private TargetObjectiveSystem _target = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    // Claw Command - needed to run a preferred-target pass before the normal random pick.
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private IDependencyCollection _dependency = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickSpecificPersonComponent, ObjectiveAssignedEvent>(OnSpecificPersonAssigned);
        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnRandomPersonAssigned);
    }

    private void OnSpecificPersonAssigned(Entity<PickSpecificPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }

    private void OnRandomPersonAssigned(Entity<PickRandomPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // couldn't find a target :(
        if (PickTarget(ent.Comp, args.MindId) is not {} picked)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent, picked, target);
    }

    /// <summary>
    /// Claw Command - Picks a mind from the objective's pool, giving priority to minds that also pass
    /// <see cref="PickRandomPersonComponent.PreferredConditions"/>. Used by the "Marked Target" trait so
    /// Syndicate kill objectives home in on volunteers first. Falls back to a plain random pick from the
    /// whole valid pool when nobody is preferred, so behaviour is unchanged for objectives that don't set it.
    /// </summary>
    private Entity<MindComponent>? PickTarget(PickRandomPersonComponent comp, EntityUid? exclude)
    {
        if (comp.PreferredConditions.Length == 0)
            return _mind.PickFromPool(comp.Pool, exclude, comp.Conditions);

        var minds = new HashSet<Entity<MindComponent>>();
        comp.Pool.FindMinds(minds, _dependency, exclude, comp.Conditions);

        if (minds.Count == 0)
            return null;

        // Pass the picking mind as the source entity, same as the pool's own condition checks do.
        var preferred = minds.Where(mind => _conditions.TryConditions(mind, comp.PreferredConditions, exclude)).ToList();

        return preferred.Count > 0 ? _random.Pick(preferred) : _random.Pick(minds);
    }
}
