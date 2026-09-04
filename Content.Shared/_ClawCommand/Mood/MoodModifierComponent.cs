using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Scales how much moodlets move this entity's mood. Used by traits to make a character more or less
///     sensitive to what happens around them, rather than just shifting their mood by a flat amount.
/// </summary>
/// <remarks>
///     Multipliers are resolved most-specific-first, and do not stack with each other: a multiplier for one
///     specific moodlet wins over one for that moodlet's category, which wins over the positive/negative
///     multiplier. A negative multiplier inverts the moodlet, which is how pain can read as pleasure.
/// </remarks>
[RegisterComponent]
public sealed partial class MoodModifierComponent : Component
{
    /// <summary>
    ///     Multiplier for every moodlet that raises mood.
    /// </summary>
    [DataField]
    public float PositiveMultiplier = 1f;

    /// <summary>
    ///     Multiplier for every moodlet that lowers mood.
    /// </summary>
    [DataField]
    public float NegativeMultiplier = 1f;

    /// <summary>
    ///     Multipliers for every moodlet belonging to a given category, whichever way it moves mood.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MoodCategoryPrototype>, float> CategoryMultipliers = new();

    /// <summary>
    ///     Multipliers for individual moodlets, whichever way they move mood.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MoodEffectPrototype>, float> EffectMultipliers = new();
}
