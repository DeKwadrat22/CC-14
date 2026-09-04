using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RampingStationEventSchedulerSystem))]
public sealed partial class RampingStationEventSchedulerComponent : Component
{
    /// <summary>
    ///     Average ending chaos modifier for the ramping event scheduler. Higher means faster.
    ///     Max chaos chosen for a round will deviate from this
    /// </summary>
    [DataField]
    public float AverageChaos = 12f;

    /// <summary>
    ///     Average time (in minutes) for when the ramping event scheduler should stop increasing the chaos modifier.
    ///     Close to how long you expect a round to last, so you'll probably have to tweak this on downstreams.
    /// </summary>
    [DataField]
    public float AverageEndTime = 90f;

    [DataField]
    public float EndTime;

    [DataField]
    public float MaxChaos;

    [DataField]
    public float StartingChaos;

    /// <summary>
    ///     Claw Command - if set, the chaos modifier the round opens on, instead of deriving it from
    ///     the rolled max chaos. The vanilla derivation is MaxChaos / 10, which forces a 10:1 ratio
    ///     between the opening and closing event rate; that is far too steep for calmer presets.
    /// </summary>
    [DataField]
    public float? InitialChaos;

    /// <summary>
    ///     Claw Command - shortest possible gap between events, in seconds, before the chaos modifier
    ///     is applied. The actual gap is a random value between this and <see cref="MaxEventTime"/>,
    ///     each divided by the current chaos modifier.
    /// </summary>
    [DataField]
    public float MinEventTime = 240f;

    /// <summary>
    ///     Claw Command - longest possible gap between events, in seconds, before the chaos modifier
    ///     is applied. See <see cref="MinEventTime"/>.
    /// </summary>
    [DataField]
    public float MaxEventTime = 720f;

    [DataField]
    public float TimeUntilNextEvent;

    /// <summary>
    /// The gamerules that the scheduler can choose from
    /// </summary>
    /// Reminder that though we could do all selection via the EntityTableSelector, we also need to consider various <see cref="StationEventComponent"/> restrictions.
    /// As such, we want to pass a list of acceptable game rules, which are then parsed for restrictions by the <see cref="EventManagerSystem"/>.
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
