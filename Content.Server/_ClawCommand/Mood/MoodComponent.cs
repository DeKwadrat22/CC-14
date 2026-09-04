using Content.Shared._ClawCommand.Mood;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Server._ClawCommand.Mood;

/// <summary>
///     Tracks every moodlet currently affecting an entity, and the aggregate mood level they produce.
///     Server-only; <see cref="NetMoodComponent"/> carries the parts the client needs.
/// </summary>
[RegisterComponent]
public sealed partial class MoodComponent : Component
{
    [DataField]
    public float CurrentMoodLevel;

    [DataField]
    public MoodThreshold CurrentMoodThreshold;

    [DataField]
    public MoodThreshold LastThreshold;

    /// <summary>
    ///     Moodlets that belong to a category, keyed by that category. Only one moodlet per category may
    ///     be active at a time; applying a second replaces the first.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<MoodCategoryPrototype>, ProtoId<MoodEffectPrototype>> CategorisedEffects = new();

    /// <summary>
    ///     Categoryless moodlets, mapped to the mood change they contribute. The stored value already has
    ///     the event's modifier and offset applied, so it can differ from the prototype's MoodChange.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<MoodEffectPrototype>, float> UncategorisedEffects = new();

    /// <summary>
    ///     When each categorised moodlet expires, keyed by category. Absent means "never".
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<MoodCategoryPrototype>, TimeSpan> CategorisedExpiry = new();

    /// <summary>
    ///     When each categoryless moodlet expires. Absent means "never".
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<ProtoId<MoodEffectPrototype>, TimeSpan> UncategorisedExpiry = new();

    /// <summary>
    ///     The formula for the movement speed modifier is SpeedBonusGrowth ^ (MoodLevel - MoodThreshold.Neutral).
    ///     Change this ONLY BY 0.001 AT A TIME.
    /// </summary>
    [DataField]
    public float SpeedBonusGrowth = 1.003f;

    /// <summary>
    ///     The lowest point that low morale can multiply our movement speed by. Lowering speed follows a linear curve, rather than geometric.
    /// </summary>
    [DataField]
    public float MinimumSpeedModifier = 0.75f;

    /// <summary>
    ///     The maximum amount that high morale can multiply our movement speed by. This follows a significantly slower geometric sequence.
    /// </summary>
    [DataField]
    public float MaximumSpeedModifier = 1.15f;

    /// <summary>
    ///     Multiplier applied to the critical damage threshold while in a good mood.
    ///     Only used when <c>mood.modify_thresholds</c> is enabled.
    /// </summary>
    [DataField]
    public float IncreaseCritThreshold = 1.2f;

    /// <summary>
    ///     Multiplier applied to the critical damage threshold while in a bad mood.
    ///     Only used when <c>mood.modify_thresholds</c> is enabled.
    /// </summary>
    [DataField]
    public float DecreaseCritThreshold = 0.9f;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 CritThresholdBeforeModify;

    [DataField]
    public ProtoId<AlertCategoryPrototype> MoodCategory = "Mood";

    [DataField(customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, float>))]
    public Dictionary<MoodThreshold, float> MoodThresholds = new()
    {
        { MoodThreshold.Perfect, 100f },
        { MoodThreshold.Exceptional, 80f },
        { MoodThreshold.Great, 70f },
        { MoodThreshold.Good, 60f },
        { MoodThreshold.Neutral, 50f },
        { MoodThreshold.Meh, 40f },
        { MoodThreshold.Bad, 30f },
        { MoodThreshold.Terrible, 20f },
        { MoodThreshold.Horrible, 10f },
        { MoodThreshold.Dead, 0f },
    };

    [DataField]
    public Dictionary<MoodThreshold, ProtoId<AlertPrototype>> MoodThresholdsAlerts = new()
    {
        { MoodThreshold.Dead, "MoodDead" },
        { MoodThreshold.Horrible, "MoodHorrible" },
        { MoodThreshold.Terrible, "MoodTerrible" },
        { MoodThreshold.Bad, "MoodBad" },
        { MoodThreshold.Meh, "MoodMeh" },
        { MoodThreshold.Neutral, "MoodNeutral" },
        { MoodThreshold.Good, "MoodGood" },
        { MoodThreshold.Great, "MoodGreat" },
        { MoodThreshold.Exceptional, "MoodExceptional" },
        { MoodThreshold.Perfect, "MoodPerfect" },
        { MoodThreshold.Insane, "MoodInsane" },
    };

    /// <summary>
    ///     These thresholds represent a percentage of Crit-Threshold, 0.8 corresponding with 80%.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MoodEffectPrototype>, float> HealthMoodEffectsThresholds = new()
    {
        { "HealthHeavyDamage", 0.8f },
        { "HealthSevereDamage", 0.5f },
        { "HealthLightDamage", 0.1f },
        { "HealthNoDamage", 0.05f },
    };
}
