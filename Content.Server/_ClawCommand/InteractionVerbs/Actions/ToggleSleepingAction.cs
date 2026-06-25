using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Components;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ToggleSleepingAction : InteractionAction
{
    [DataField]
    public bool WakeUp = false, Sleep = false;

    public override bool IsAllowed(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var asleep = deps.EntMan.HasComponent<SleepingComponent>(ctx.Target);
        if (!asleep)
            return Sleep && deps.EntMan.HasComponent<MobStateComponent>(ctx.Target); // Non-mobs cannot sleep

        return WakeUp;
    }

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (isBefore)
            ctx.Blackboard["sleeping"] = deps.EntMan.HasComponent<SleepingComponent>(ctx.Target);

        return true; // We already checked the rest in IsAllowed
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var asleep = deps.EntMan.HasComponent<SleepingComponent>(ctx.Target);
        if (ctx.TryGetBlackboard("sleeping", out bool wasAsleep) && wasAsleep != asleep)
            return false; // The target woke up/went to sleep during the do-after - sus

        if (asleep && WakeUp)
            return deps.EntMan.System<SleepingSystem>().TryWaking(ctx.Target, user: ctx.User);
        else if (Sleep)
            return deps.EntMan.System<SleepingSystem>().TrySleeping(ctx.Target);

        return false;
    }
}
