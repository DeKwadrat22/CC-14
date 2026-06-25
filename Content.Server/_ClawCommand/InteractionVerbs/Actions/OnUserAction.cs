using Content.Shared._ClawCommand.InteractionVerbs;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     A special proxy action that swaps the target and the user for the proxied action.
///     This effectively means that in most cases the proxied action will be applied to the user even if it's meant for target.
/// </summary>
[Serializable]
public sealed partial class OnUserAction : InteractionAction
{
    [DataField(required: true)]
    public InteractionAction Action = default!;

    private InteractionArgs SwapRoles(InteractionArgs ctx)
    {
        return new InteractionArgs(ctx)
        {
            Target = ctx.User,
            User = ctx.Target
        };
    }

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return Action.CanPerform(SwapRoles(ctx), proto, beforeDelay, deps);
    }

    public override bool IsAllowed(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return Action.IsAllowed(SwapRoles(ctx), proto, deps);
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return Action.Perform(SwapRoles(ctx), proto, deps);
    }
}
