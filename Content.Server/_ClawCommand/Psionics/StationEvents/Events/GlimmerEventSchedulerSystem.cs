using Content.Server.GameTicking.Rules;
using Content.Server.Psionics.Glimmer;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Psionics.Glimmer;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

/// <summary>
///     Claw Command - drives glimmer events off the glimmer level instead of the ordinary event pacing.
///
///     See <see cref="GlimmerEventSchedulerComponent"/> for why. This only decides <em>whether</em> a
///     glimmer event happens; <em>which</em> one is still the usual weighted pick, narrowed by each
///     event's own glimmer window in <see cref="EventManagerSystem.CanRun"/>. So a roll that lands at
///     220 glimmer can only produce the low-end events, while the same roll at 800 has the whole ladder
///     available to it.
/// </summary>
[UsedImplicitly]
public sealed partial class GlimmerEventSchedulerSystem : GameRuleSystem<GlimmerEventSchedulerComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EventManagerSystem _event = default!;
    [Dependency] private GlimmerSystem _glimmer = default!;

    protected override void Started(EntityUid uid,
        GlimmerEventSchedulerComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.TimeUntilNextCheck = (float) component.CheckInterval.TotalSeconds;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<GlimmerEventSchedulerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            scheduler.TimeUntilNextCheck -= frameTime;
            if (scheduler.TimeUntilNextCheck > 0)
                continue;

            scheduler.TimeUntilNextCheck = (float) scheduler.CheckInterval.TotalSeconds;

            var chance = GetCheckChance(scheduler);
            if (chance <= 0f || !_random.Prob(chance))
                continue;

            _event.RunRandomEvent(scheduler.ScheduledGameRules);
        }
    }

    /// <summary>
    ///     Chance of at least one glimmer event across <see cref="GlimmerEventSchedulerComponent.Window"/>
    ///     at the current glimmer level. This is the number the component is tuned in.
    /// </summary>
    public float GetWindowChance(GlimmerEventSchedulerComponent component)
    {
        var glimmer = _glimmer.Glimmer;
        if (glimmer < component.MinimumGlimmer)
            return 0f;

        var steps = (float) (glimmer - component.MinimumGlimmer) / component.GlimmerPerStep;

        return Math.Clamp(component.BaseChance + steps * component.ChancePerStep, 0f, component.MaximumChance);
    }

    /// <summary>
    ///     The windowed chance converted to the odds for one roll, so the component can be tuned in
    ///     "x% per half hour" and stay correct if CheckInterval is ever changed.
    /// </summary>
    /// <remarks>
    ///     P(at least one across the window) = 1 - (1 - perRoll) ^ rolls, solved for perRoll.
    /// </remarks>
    public float GetCheckChance(GlimmerEventSchedulerComponent component)
    {
        var windowChance = GetWindowChance(component);
        if (windowChance <= 0f)
            return 0f;

        var rolls = (float) (component.Window / component.CheckInterval);
        if (rolls <= 1f)
            return windowChance;

        return 1f - MathF.Pow(1f - windowChance, 1f / rolls);
    }
}
