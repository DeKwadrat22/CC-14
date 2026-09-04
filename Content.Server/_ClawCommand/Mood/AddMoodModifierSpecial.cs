using Content.Shared._ClawCommand.Mood;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.Mood;

/// <summary>
///     Scales how strongly moodlets affect an entity, typically from a trait.
/// </summary>
/// <remarks>
///     Merges multiplicatively with modifiers a previous trait already applied, so several mood traits can
///     be taken together without one silently replacing another.
/// </remarks>
[UsedImplicitly]
public sealed partial class AddMoodModifierSpecial : JobSpecial
{
    [DataField]
    public float PositiveMultiplier { get; private set; } = 1f;

    [DataField]
    public float NegativeMultiplier { get; private set; } = 1f;

    [DataField]
    public Dictionary<ProtoId<MoodCategoryPrototype>, float> CategoryMultipliers { get; private set; } = new();

    [DataField]
    public Dictionary<ProtoId<MoodEffectPrototype>, float> EffectMultipliers { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var comp = entMan.EnsureComponent<MoodModifierComponent>(mob);

        comp.PositiveMultiplier *= PositiveMultiplier;
        comp.NegativeMultiplier *= NegativeMultiplier;

        foreach (var (category, multiplier) in CategoryMultipliers)
            comp.CategoryMultipliers[category] = comp.CategoryMultipliers.GetValueOrDefault(category, 1f) * multiplier;

        foreach (var (effect, multiplier) in EffectMultipliers)
            comp.EffectMultipliers[effect] = comp.EffectMultipliers.GetValueOrDefault(effect, 1f) * multiplier;

        // The component's own startup only covers the first trait to add it.
        entMan.System<MoodSystem>().RefreshMood(mob);
    }
}
