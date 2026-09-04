using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.InteractionVerbs.Requirements;

/// <summary>
///     Requires the target to be the user itself.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SelfTargetRequirement : InvertableInteractionRequirement
{
    public override bool IsMet(InteractionArgs ctx, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return (ctx.Target == ctx.User) ^ Inverted;
    }
}

/// <summary>
///     Requires the target to meet a certain whitelist and not meet a blacklist.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EntityWhitelistRequirement : InteractionRequirement
{
    [DataField] public EntityWhitelist? Whitelist, Blacklist;

    public override bool IsMet(InteractionArgs ctx, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps) =>
        !deps.WhitelistSystem.IsWhitelistFail(Whitelist, ctx.Target);
}

/// <summary>
///     Requires the target to be in a specific standing state.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class StandingStateRequirement : InteractionRequirement
{
    [DataField] public bool AllowStanding, AllowLaying, AllowKnockedDown;

    public override bool IsMet(InteractionArgs ctx, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        if (deps.EntMan.HasComponent<KnockedDownComponent>(ctx.Target))
            return AllowKnockedDown;

        if (!deps.EntMan.TryGetComponent<StandingStateComponent>(ctx.Target, out var standComp))
            return false;

        return standComp.Standing && AllowStanding
            || !standComp.Standing && AllowLaying;
    }
}

/// <summary>
///     Requires the mob to be a mob in a certain state. If inverted, requires the mob to not be in that state.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MobStateRequirement : InvertableInteractionRequirement
{
    [DataField] public List<MobState> AllowedStates = new();

    public override bool IsMet(InteractionArgs ctx, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        if (!deps.EntMan.TryGetComponent<MobStateComponent>(ctx.Target, out var mobState))
            return false;

        return AllowedStates.Contains(mobState.CurrentState) ^ Inverted;
    }
}
