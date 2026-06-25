namespace Content.Shared.Atmos.Components;

// Unfortunately can't be friends yet due to magboots.
[RegisterComponent]
public sealed partial class MovedByPressureComponent : Component
{
    [DataField]
    public bool Enabled { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public int LastHighPressureMovementAirCycle { get; set; } = 0;

    /// <summary>
    /// Whether this entity is currently being actively moved around by pressure deltas.
    /// While true, the entity is held in the active-pressure tracking set so its physics
    /// state can be reset once it stops being hit by wind.
    /// </summary>
    [DataField]
    public bool Throwing;

    /// <summary>
    /// Time at which the entity should "fall to the ground" again if not hit by another
    /// pressure delta in the meantime.
    /// </summary>
    [DataField]
    public TimeSpan ThrowingCutoffTarget;

    /// <summary>
    /// How long (in seconds) this object can go between being hit by space wind before
    /// it stops being treated as "in-air" by the pressure system.
    /// </summary>
    [DataField]
    public TimeSpan CutoffTime = TimeSpan.FromSeconds(2.0);
}
