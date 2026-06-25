using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared.Standing;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ChangeStandingStateAction : InteractionAction
{
    [DataField]
    public bool MakeStanding, MakeLaying;

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(ctx.Target, out var standState))
            return false;

        if (isBefore)
            ctx.Blackboard["standing"] = standState.Standing;

        return standState.Standing && MakeLaying
               || !standState.Standing && MakeStanding;
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var standingSys = deps.EntMan.System<StandingStateSystem>();

        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(ctx.Target, out var standState)
            || ctx.TryGetBlackboard("standing", out bool prevStanding) && prevStanding != standState.Standing)
            return false;

        // Note: these will get cancelled if the target is forced to stand/lay, e.g. due to a buckle or stun or something else.
        if (!standState.Standing && MakeStanding)
            return standingSys.Stand(ctx.Target);
        else if (standState.Standing && MakeLaying)
            return standingSys.Down(ctx.Target);

        return false;
    }
}
