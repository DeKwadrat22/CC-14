using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.SuitSensors;

/// <summary>
///     Tracking device, embedded in almost all uniforms and jumpsuits.
///     If enabled, will report to crew monitoring console owners position and status.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSuitSensorSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SuitSensorComponent : Component
{
    /// <summary>
    ///     Choose a random sensor mode when item is spawned.
    /// </summary>
    [DataField]
    public bool RandomMode = true;

    /// <summary>
    ///     If true user can't change suit sensor mode
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ControlsLocked = false;

    /// <summary>
    ///  How much time it takes to change another player's sensors
    /// </summary>
    [DataField]
    public float SensorsTime = 1.75f;

    /// <summary>
    ///     Current sensor mode. Can be switched by user verbs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SuitSensorMode Mode = SuitSensorMode.SensorOff;

    /// <summary>
    ///     Activate sensor if user wear it in this slot.
    /// </summary>
    [DataField]
    public string ActivationSlot = "jumpsuit";

    /// <summary>
    /// Activate sensor if user has this in a sensor-compatible container.
    /// </summary>
    [DataField]
    public string? ActivationContainer;

    /// <summary>
    ///     How often does sensor update its owners status (in seconds). Limited by the system update rate.
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(2f);

    /// <summary>
    ///     Current user that wears suit sensor. Null if nobody wearing it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User = null;

    /// <summary>
    ///     Next time when sensor updated owners status
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    ///     The station this suit sensor belongs to. If it's null the suit didn't spawn on a station and the sensor doesn't work.
    /// </summary>
    [DataField("station"), AutoNetworkedField]
    public EntityUid? StationId = null;

    /// <summary>
    ///     The server the suit sensor sends it state to.
    ///     The suit sensor will try connecting to a new server when no server is connected.
    ///     It does this by calling the servers entity system for performance reasons.
    /// </summary>
    [DataField("server")]
    public string? ConnectedServer = null;

    /// <summary>
    ///     Claw Command: how often the sensor should force a fresh lookup of its
    ///     <see cref="ConnectedServer"/>, even when the cached address still looks
    ///     valid. Without this we'd keep blasting packets at a singleton that was
    ///     replaced or briefly went down — the existing IsAddressPresent fast-path
    ///     in <c>SuitSensorSystem.Update</c> only catches the case where the address
    ///     literally vanishes from the network, not stale routing after a power
    ///     flicker, station-server swap, or map-load transient.
    /// </summary>
    [DataField]
    public TimeSpan ServerResolveInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Claw Command: timestamp at which the next forced <see cref="ConnectedServer"/>
    ///     re-resolution may happen. Initialised to <c>TimeSpan.Zero</c> so the first
    ///     sensor tick after spawn always re-resolves, then advances by
    ///     <see cref="ServerResolveInterval"/> on each refresh.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextServerResolve = TimeSpan.Zero;

    /// <summary>
    /// The previous mode of the suit. This is used to restore the state when an EMP effect ends.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public SuitSensorMode PreviousMode = SuitSensorMode.SensorOff;

    /// <summary>
    ///  The previous locked status of the controls.  This is used to restore the state when an EMP effect ends.
    ///  This keeps prisoner jumpsuits/internal implants from becoming unlocked after an EMP.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public bool PreviousControlsLocked = false;
}
