using Content.Shared._ClawCommand.Mood;
using Content.Shared._ClawCommand.Mood.EntityEffects;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Server._ClawCommand.Mood;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class AddMoodletEntityEffectSystem : EntityEffectSystem<MoodComponent, AddMoodlet>
{
    protected override void Effect(Entity<MoodComponent> entity, ref EntityEffectEvent<AddMoodlet> args)
    {
        RaiseLocalEvent(entity.Owner, new MoodEffectEvent(args.Effect.Moodlet));
    }
}

/// <inheritdoc cref="EntityConditionSystem{T,TCondition}"/>
public sealed class MoodletEntityConditionSystem : EntityConditionSystem<MoodComponent, MoodletCondition>
{
    protected override void Condition(Entity<MoodComponent> entity, ref EntityConditionEvent<MoodletCondition> args)
    {
        var moodlet = args.Condition.Moodlet;

        args.Result = entity.Comp.UncategorisedEffects.ContainsKey(moodlet)
            || entity.Comp.CategorisedEffects.ContainsValue(moodlet);
    }
}

/// <inheritdoc cref="EntityConditionSystem{T,TCondition}"/>
public sealed class MoodCategoryEntityConditionSystem : EntityConditionSystem<MoodComponent, MoodCategoryCondition>
{
    protected override void Condition(Entity<MoodComponent> entity, ref EntityConditionEvent<MoodCategoryCondition> args)
    {
        args.Result = entity.Comp.CategorisedEffects.ContainsKey(args.Condition.Category);
    }
}
