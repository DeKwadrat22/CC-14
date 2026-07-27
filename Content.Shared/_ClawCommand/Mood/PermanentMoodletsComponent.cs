using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Applies a fixed set of moodlets to the entity when it gains this component.
///     Typically used by traits for permanent moodlets or pre-existing drug addictions, but works on
///     any entity prototype.
/// </summary>
[RegisterComponent]
public sealed partial class PermanentMoodletsComponent : Component
{
    /// <summary>
    ///     The moodlets to apply. Moodlets with a timeout will still expire normally.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<MoodEffectPrototype>> Moodlets = new();
}
