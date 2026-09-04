using Content.Shared._ClawCommand.Mood;

namespace Content.Shared.Contests;

/// <summary>
///     Claw Command - a deliberately minimal stand-in for the upstream ContestsSystem.
///
///     The fork this psionics port came from shipped a ~900 line ContestsSystem covering mass, stamina,
///     health, mood and item contests, of which the psionics code only ever calls <see cref="MoodContest"/>.
///     Porting the whole thing would duplicate the mass-contest maths this fork already implements locally
///     inside CarryingSystem and PullingSystem.Grab, so only the mood contest is reproduced here, on top of
///     this fork's own <see cref="NetMoodComponent"/>.
///
///     If a future port needs the other contest types, add them here rather than reintroducing the upstream file.
/// </summary>
public sealed class ContestsSystem : EntitySystem
{
    /// <summary>
    ///     Mirrors upstream contests.max_percentage. Clamped contests may swing results by at most this much.
    /// </summary>
    private const float MaxPercentage = 0.25f;

    /// <summary>
    ///     Returns the ratio of an entity's mood to its neutral threshold.
    ///     Above 1 for a happy entity, below 1 for a miserable one, exactly 1 when mood is unavailable.
    /// </summary>
    /// <param name="performer">The entity whose mood is being measured.</param>
    /// <param name="bypassClamp">
    ///     When true the raw ratio is returned. The psionics systems pass true here because upstream's
    ///     Nyanotrasen-era power maths was balanced against the unclamped value.
    /// </param>
    /// <param name="rangeFactor">Widens or narrows the clamp window when <paramref name="bypassClamp"/> is false.</param>
    public float MoodContest(EntityUid performer, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (!TryComp<NetMoodComponent>(performer, out var mood)
            || mood.NeutralMoodThreshold <= 0f)
            return 1f;

        var ratio = mood.CurrentMoodLevel / mood.NeutralMoodThreshold;

        return bypassClamp
            ? ratio
            : Math.Clamp(ratio, 1 - MaxPercentage * rangeFactor, 1 + MaxPercentage * rangeFactor);
    }

    /// <summary>
    ///     Returns the ratio between two entities' mood contests, used when a power pits caster against target.
    /// </summary>
    public float MoodContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f)
    {
        var theirs = MoodContest(target, bypassClamp, rangeFactor);

        return theirs == 0f
            ? 1f
            : MoodContest(performer, bypassClamp, rangeFactor) / theirs;
    }
}
