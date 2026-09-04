using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being pullable by an entity with <see cref="PullerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Systems.PullingSystem))]
public sealed partial class PullableComponent : Component
{
    /// <summary>
    /// The current entity pulling this component.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Puller;

    /// <summary>
    /// The pull joint.
    /// </summary>
    [AutoNetworkedField, DataField]
    public string? PullJointId;

    public bool BeingPulled => Puller != null;

    /// <summary>
    /// If the physics component has FixedRotation should we keep it upon being pulled
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [ViewVariables(VVAccess.ReadWrite), DataField("fixedRotation")]
    public bool FixedRotationOnPull;

    /// <summary>
    /// What the pullable's fixedrotation was set to before being pulled.
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField, DataField]
    public bool PrevFixedRotation;

    [DataField]
    public ProtoId<AlertPrototype> PulledAlert = "Pulled";

    #region Grab intent - claw command

    /// <summary>
    /// Alert severity shown on the victim for each grab stage.
    /// </summary>
    [DataField]
    public Dictionary<Systems.GrabStage, short> PulledAlertSeverity = new()
    {
        { Systems.GrabStage.No, 0 },
        { Systems.GrabStage.Soft, 1 },
        { Systems.GrabStage.Hard, 2 },
        { Systems.GrabStage.Suffocate, 3 },
    };

    [AutoNetworkedField, DataField]
    public Systems.GrabStage GrabStage = Systems.GrabStage.No;

    /// <summary>
    /// Claw Command - highest grab stage anyone can escalate this entity to. Dangerous hostile mobs
    /// cap at Soft: you can drag a space carp out of the way, but you cannot combat-grab or choke one
    /// into submission the way you would a person. Defaults to Suffocate, i.e. no restriction.
    /// </summary>
    [AutoNetworkedField, DataField]
    public Systems.GrabStage MaxGrabStage = Systems.GrabStage.Suffocate;

    /// <summary>
    /// Resolved chance of breaking free on any one attempt, after mass is factored in.
    /// </summary>
    [AutoNetworkedField, DataField]
    public float GrabEscapeChance = 1f;

    [AutoNetworkedField]
    public TimeSpan NextEscapeAttempt = TimeSpan.Zero;

    #endregion

    #region Slamming - claw command

    /// <summary>
    /// Set while this entity is mid-flight from a slam. The next solid thing they touch is what they
    /// get hurt on; cleared there.
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool BeingSlammed;

    /// <summary>
    /// Whether the in-flight slam is into something solid (wall, locker, window) rather than onto a
    /// table. Decides which damage figure below gets used on landing.
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool SlamHardSurface;

    /// <summary>
    /// How hard they get thrown at the thing.
    /// </summary>
    [DataField]
    public float SlamThrowSpeed = 5f;

    [DataField]
    public float SlamStaminaDamage = 40f;

    /// <summary>
    /// Blunt taken from being put through a table, which at least gives way a little.
    /// </summary>
    [DataField]
    public float SlamTableDamage = 5f;

    /// <summary>
    /// Blunt taken from being driven into a wall, locker or machine, which does not.
    /// </summary>
    [DataField]
    public float SlamObjectDamage = 10f;

    /// <summary>
    /// How long they stay down afterwards. Doubled when the surface was a glass table.
    /// </summary>
    [DataField]
    public TimeSpan SlamKnockdownDuration = TimeSpan.FromSeconds(3);

    #endregion
}

public sealed partial class StopBeingPulledAlertEvent : BaseAlertEvent;
