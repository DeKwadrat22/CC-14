// claw command - IPC
using Robust.Shared.GameStates;
using Content.Shared.Silicon.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared.Silicon.Components;

/// <summary>
///     Component for defining a mob as a robot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public short ChargeState = 10;

    [ViewVariables(VVAccess.ReadOnly)]
    public float OverheatAccumulator = 0.0f;

    /// <summary>
    ///     The last time the Silicon was drained.
    ///     Used for NPC Silicons to avoid over updating.
    /// </summary>
    public TimeSpan LastDrainTime = TimeSpan.Zero;

    /// <summary>
    ///     Is the Silicon currently dead?
    /// </summary>
    public bool Dead;

    /// <summary>
    ///     The type of silicon this is.
    /// </summary>
    [DataField(customTypeSerializer: typeof(EnumSerializer))]
    public Enum EntityType = SiliconType.Npc;

    /// <summary>
    ///     Is this silicon battery powered?
    /// </summary>
    [DataField]
    public bool BatteryPowered;

    /// <summary>
    ///     How much power is drained by this Silicon every second by default.
    /// </summary>
    [DataField]
    public float DrainPerSecond = 50f;

    /// <summary>
    ///     The percentages at which the silicon will enter each state.
    /// </summary>
    [DataField]
    public float? ChargeThresholdMid = 0.5f;

    /// <inheritdoc cref="ChargeThresholdMid"/>
    [DataField]
    public float? ChargeThresholdLow = 0.25f;

    /// <inheritdoc cref="ChargeThresholdMid"/>
    [DataField]
    public float? ChargeThresholdCritical = 0.1f;

    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    /// <summary>
    ///     The amount the Silicon will be slowed at each charge state.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, float> SpeedModifierThresholds = default!;

    [DataField]
    public float FireStackMultiplier = 1f;

    /// <summary>
    ///     Whether or not a Silicon will cancel all sleep events.
    /// </summary>
    [DataField]
    public bool DoSiliconsDreamOfElectricSheep;
}
