using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood.EntityEffects;

/// <summary>
/// Applies a moodlet to the entity. Does nothing to entities without a mood.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddMoodlet : EntityEffectBase<AddMoodlet>
{
    /// <summary>
    /// The moodlet to apply.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> Moodlet;

    /// <summary>
    /// Whether the guidebook should name the moodlet. Useful for addiction moodlets, where "gives you an
    /// amphetamine addiction" is the point, and noise otherwise.
    /// </summary>
    [DataField]
    public bool GuidebookShowEffectName;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!prototype.TryIndex(Moodlet, out var moodlet))
            return null;

        return Loc.GetString("entity-effect-guidebook-add-moodlet",
            ("chance", Probability),
            ("useEffectName", GuidebookShowEffectName),
            ("moodEffect", moodlet.Name),
            ("amount", MathF.Abs(moodlet.MoodChange)),
            ("deltasign", MathF.Sign(moodlet.MoodChange)),
            ("timeout", moodlet.Timeout));
    }
}
