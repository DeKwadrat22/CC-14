namespace Content.Server.StationEvents
{
    /// <summary>
    ///     Marks a spot as a hidden spawn location for mid-round threats. Glimmer creature events fall back to
    ///     these when no glimmer source or vent-critter spawn is available, so the mobs appear somewhere sensible
    ///     (maintenance, unused rooms) rather than nowhere at all.
    /// </summary>
    /// <remarks>
    ///     Claw Command - ported alongside the psionics/glimmer systems. Upstream also used this for the
    ///     MidRoundAntagRule (rat king / paradox anomaly spawner), which is not part of this port; the marker
    ///     itself is still what GlimmerMobRule reads for its hidden-spawn pool.
    /// </remarks>
    [RegisterComponent]
    public sealed partial class MidRoundAntagSpawnLocationComponent : Component
    {

    }
}
