using Content.Server.StationEvents;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Claw Command - schedules glimmer events at a rate driven by the station's glimmer level.
///
///     The psionics port dropped glimmer events into the ordinary event tables, where the normal
///     scheduler picked them at its own pace and glimmer had no say in how often they came up. This
///     runs them off glimmer instead: silence below <see cref="MinimumGlimmer"/>, and above it a
///     chance that climbs with how bad the noosphere has got.
/// </summary>
[RegisterComponent, Access(typeof(GlimmerEventSchedulerSystem))]
public sealed partial class GlimmerEventSchedulerComponent : Component
{
    /// <summary>
    ///     Nothing fires below this. A raw glimmer value rather than a GlimmerTier on purpose - the
    ///     Moderate tier technically opens at 100, which is too early for anything to be stirring.
    /// </summary>
    [DataField]
    public int MinimumGlimmer = 200;

    /// <summary>
    ///     Chance of at least one glimmer event within <see cref="Window"/> when glimmer sits exactly
    ///     at <see cref="MinimumGlimmer"/>.
    /// </summary>
    [DataField]
    public float BaseChance = 0.15f;

    /// <summary>
    ///     Added to <see cref="BaseChance"/> per <see cref="GlimmerPerStep"/> glimmer above the minimum.
    ///     Interpolated, not stepped, so 250 glimmer sits halfway between 200 and 300.
    /// </summary>
    [DataField]
    public float ChancePerStep = 0.05f;

    /// <inheritdoc cref="ChancePerStep"/>
    [DataField]
    public int GlimmerPerStep = 100;

    /// <summary>
    ///     Ceiling on the windowed chance, so even pinned-at-1000 glimmer isn't a guarantee.
    /// </summary>
    [DataField]
    public float MaximumChance = 0.6f;

    /// <summary>
    ///     The span the chances above are quoted over. 15% at 200 glimmer means 15% per half hour.
    /// </summary>
    [DataField]
    public TimeSpan Window = TimeSpan.FromMinutes(30);

    /// <summary>
    ///     How often a roll is made. The per-roll odds are derived from this, so the windowed chance
    ///     stays what it says on the tin whatever this is set to - shorter just makes it smoother.
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Seconds until the next roll.
    /// </summary>
    [DataField]
    public float TimeUntilNextCheck;

    /// <summary>
    ///     Pool to pick from once a roll succeeds. Which event comes out is still filtered by each
    ///     event's own minimumGlimmer / maximumGlimmer window in <see cref="EventManagerSystem"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
