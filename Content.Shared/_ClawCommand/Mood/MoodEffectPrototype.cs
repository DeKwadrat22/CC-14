using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     A "moodlet" - a single, named contribution to an entity's overall mood level.
/// </summary>
[Prototype]
public sealed partial class MoodEffectPrototype : IPrototype
{
    /// <summary>
    ///     The ID of the moodlet to use.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The in-character thought shown to the player when this moodlet is applied, and in the mood alert readout.
    /// </summary>
    public string Description => Loc.GetString($"mood-effect-{ID}");

    /// <summary>
    ///     A short, out-of-character name for this moodlet. Used by guidebook entries for chemicals that apply it.
    /// </summary>
    public string Name => Loc.GetString($"{SharedMoodSystem.LocMoodEffectNamePrefix}{ID}");

    /// <summary>
    ///     If they already have an effect with the same category, the new one will replace the old one.
    /// </summary>
    [DataField]
    public ProtoId<MoodCategoryPrototype>? Category;

    /// <summary>
    ///     How much should this moodlet modify an entity's Mood.
    /// </summary>
    [DataField(required: true)]
    public float MoodChange;

    /// <summary>
    ///     How long, in seconds, does this moodlet last? If omitted, the moodlet will last until canceled by any system.
    /// </summary>
    [DataField]
    public int Timeout;

    /// <summary>
    ///     Should this moodlet be hidden from the player? EG: No popups or chat messages.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    ///     When not null, this moodlet will replace itself with another Moodlet upon expiration.
    /// </summary>
    [DataField]
    public ProtoId<MoodEffectPrototype>? MoodletOnEnd;
}
