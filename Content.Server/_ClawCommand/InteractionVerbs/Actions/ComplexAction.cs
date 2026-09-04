using System.Linq;
using Content.Shared._ClawCommand.InteractionVerbs;
using Robust.Shared.Serialization;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     An action that combines multiple other actions.
/// </summary>
[Serializable]
public sealed partial class ComplexAction : InteractionAction
{
    [DataField]
    public List<InteractionAction> Actions = new();

    /// <summary>
    ///     If true, when it comes to execution of this action, the entire action will exit early if: <br/>
    ///     * The action has RequireAll = false and at least one action succeeds; <br/>
    ///     * Or if the action has RequireAll = true and at least one action fails.
    /// </summary>
    [DataField]
    public bool Lazy = false;

    /// <summary>
    ///     If true, all actions must pass the IsAllowed and CanPerform checks,
    ///     and all must successfully perform for this action to succeed (boolean and).
    ///     Otherwise, at least one must pass the checks and successfully perform (boolean or).
    /// </summary>
    /// <remarks>If this is false, all actions will be performed if at least one of their CanPerform checks succeeds.</remarks>
    [DataField]
    public bool RequireAll = false;

    private bool RunDelegate(Func<InteractionAction, bool> fn)
    {
        if (Lazy)
            return RequireAll ? Actions.All(fn) : Actions.Any(fn);

        var outcome = RequireAll;
        if (RequireAll)
            foreach (var act in Actions)
                outcome &= fn(act);
        else
            foreach (var act in Actions)
                outcome |= fn(act);

        return outcome;
    }

    public override bool IsAllowed(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return RunDelegate(act => act.IsAllowed(ctx, proto, deps));
    }

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return RunDelegate(act => act.CanPerform(ctx, proto, beforeDelay, deps));
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return RunDelegate(act => act.Perform(ctx, proto, deps));
    }
}
