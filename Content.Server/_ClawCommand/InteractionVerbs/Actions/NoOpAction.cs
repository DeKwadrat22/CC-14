using Content.Shared._ClawCommand.InteractionVerbs;
using Robust.Shared.Random;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     An action that does nothing on its own, made just to mimic the old "chance to show a popup" interactions.
/// </summary>
[Serializable]
public sealed partial class NoOpAction : InteractionAction
{
    [DataField]
    public float SuccessChance = 1f;

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return true;
    }

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        if (isBefore)
            return true; // so the do-after can happen if there's one

        // Return true if chance >= 1f, false if <= 0f, or a random result if anywhere in-between.
        return SuccessChance > 0f && (SuccessChance >= 1f || deps.Random.Prob(SuccessChance));
    }
}
