using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared._ClawCommand.Mood;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

/// <summary>
///     An action that adds a moodlet to the target, or removes one.
/// </summary>
[Serializable]
public sealed partial class MoodAction : InteractionAction
{
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> Effect;

    /// <summary>
    ///     Parameters for the <see cref="MoodEffectEvent"/>. Only used if <see cref="Remove"/> is false.
    /// </summary>
    [DataField]
    public float Modifier = 1f, Offset = 0f;

    /// <summary>
    ///     If true, the moodlet will be removed. Otherwise, it will be added.
    /// </summary>
    [DataField]
    public bool Remove;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool isBefore, VerbDependencies deps)
    {
        return true;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var mood = deps.EntMan.System<SharedMoodSystem>();

        if (Remove)
            mood.RemoveMoodlet(args.Target, Effect);
        else
            mood.AddMoodlet(args.Target, Effect, Modifier, Offset);

        // The mood system silently ignores entities without a mood, so there is nothing meaningful to report here.
        return true;
    }
}
