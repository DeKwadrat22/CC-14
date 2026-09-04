using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood.EntityEffects;

/// <summary>
/// Returns true if the entity currently has a specific moodlet.
/// Always false for entities without a mood, so combine with <c>inverted</c> accordingly.
/// </summary>
/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MoodletCondition : EntityConditionBase<MoodletCondition>
{
    /// <summary>
    /// The moodlet to test for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodEffectPrototype> Moodlet;

    /// <summary>
    /// Overrides the guidebook description of the moodlet being tested for.
    /// </summary>
    [DataField]
    public LocId? Description;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var name = Description is { } desc
            ? Loc.GetString(desc)
            : Loc.GetString($"{SharedMoodSystem.LocMoodEffectNamePrefix}{Moodlet}");

        return Loc.GetString("entity-condition-guidebook-has-moodlet", ("inverted", Inverted), ("effect", name));
    }
}

/// <summary>
/// Returns true if the entity currently has any moodlet belonging to a specific category.
/// Always false for entities without a mood, so combine with <c>inverted</c> accordingly.
/// </summary>
/// <inheritdoc cref="EntityCondition"/>
public sealed partial class MoodCategoryCondition : EntityConditionBase<MoodCategoryCondition>
{
    /// <summary>
    /// The moodlet category to test for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MoodCategoryPrototype> Category;

    /// <summary>
    /// Overrides the guidebook description of the category being tested for.
    /// </summary>
    [DataField]
    public LocId? Description;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var name = Description is { } desc
            ? Loc.GetString(desc)
            : Loc.GetString($"{SharedMoodSystem.LocMoodCategoryNamePrefix}{Category}");

        return Loc.GetString("entity-condition-guidebook-has-moodlet", ("inverted", Inverted), ("effect", name));
    }
}
