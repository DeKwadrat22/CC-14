using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Raised on an entity to apply a moodlet to it.
/// </summary>
[Serializable, NetSerializable]
public sealed class MoodEffectEvent : EntityEventArgs
{
    /// <summary>
    ///     ID of the moodlet prototype to use.
    /// </summary>
    public ProtoId<MoodEffectPrototype> EffectId;

    /// <summary>
    ///     How much should the mood change be multiplied by.
    ///     <br />
    ///     This does nothing if the moodlet has a Category.
    /// </summary>
    public float EffectModifier = 1f;

    /// <summary>
    ///     How much should the mood change be offset by, after multiplication.
    ///     <br />
    ///     This does nothing if the moodlet has a Category.
    /// </summary>
    public float EffectOffset;

    public MoodEffectEvent(ProtoId<MoodEffectPrototype> effectId, float effectModifier = 1f, float effectOffset = 0f)
    {
        EffectId = effectId;
        EffectModifier = effectModifier;
        EffectOffset = effectOffset;
    }
}

/// <summary>
///     Raised on an entity to remove a moodlet from it. If the moodlet defines a
///     <see cref="MoodEffectPrototype.MoodletOnEnd"/>, that replacement is applied as normal.
/// </summary>
[Serializable, NetSerializable]
public sealed class MoodRemoveEffectEvent : EntityEventArgs
{
    public ProtoId<MoodEffectPrototype> EffectId;

    public MoodRemoveEffectEvent(ProtoId<MoodEffectPrototype> effectId)
    {
        EffectId = effectId;
    }
}

/// <summary>
///     This event is raised whenever an entity sets their mood, allowing other systems to modify the end result of mood math.
///     EG: The end result after tallying up all Moodlets comes out to 70, but a trait multiplies it by 0.8 to make it 56.
/// </summary>
[ByRefEvent]
public record struct OnSetMoodEvent(EntityUid Receiver, float MoodChangedAmount, bool Cancelled);

/// <summary>
///     This event is raised on an entity when it receives a mood effect, but before the effects are calculated.
///     Allows for other systems to pick and choose specific events to modify.
/// </summary>
[ByRefEvent]
public record struct OnMoodEffect(EntityUid Receiver, ProtoId<MoodEffectPrototype> EffectId, float EffectModifier = 1f, float EffectOffset = 0f);

/// <summary>
///     Raised on the player when they click their mood alert, listing their current moodlets in chat.
/// </summary>
public sealed partial class ShowMoodEffectsAlertEvent : BaseAlertEvent;
