using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Coarse buckets that an entity's mood level falls into. Drives the mood alert, movement speed,
///     the desaturation overlay and (optionally) the critical damage threshold.
/// </summary>
[Serializable, NetSerializable]
public enum MoodThreshold : ushort
{
    Dead = 0,
    Insane = 1,
    Horrible = 2,
    Terrible = 3,
    Bad = 4,
    Meh = 5,
    Neutral = 6,
    Good = 7,
    Great = 8,
    Exceptional = 9,
    Perfect = 10,
}
