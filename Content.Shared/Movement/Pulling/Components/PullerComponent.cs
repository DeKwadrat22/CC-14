using Content.Shared.Alert;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being able to pull another entity with <see cref="PullableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(PullingSystem))]
public sealed partial class PullerComponent : Component
{
    // My raiding guild
    /// <summary>
    /// Next time the puller can throw what is being pulled.
    /// Used to avoid spamming it for infinite spin + velocity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextThrow;

    /// <summary>
    /// Minimum time between pull throws.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Pulling;

    /// <summary>
    /// Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsHands = true;

    /// <summary>
    /// The alert shown to the puller indicating that they are pulling something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> PullingAlert = "Pulling";

    #region Grab intent - claw command

    /// <summary>
    /// Alert severity shown on the puller for each grab stage.
    /// </summary>
    [DataField]
    public Dictionary<GrabStage, short> PullingAlertSeverity = new()
    {
        { GrabStage.No, 0 },
        { GrabStage.Soft, 1 },
        { GrabStage.Hard, 2 },
        { GrabStage.Suffocate, 3 },
    };

    [DataField, AutoNetworkedField]
    public GrabStage GrabStage = GrabStage.No;

    [DataField, AutoNetworkedField]
    public GrabStageDirection GrabStageDirection = GrabStageDirection.Increase;

    [AutoNetworkedField]
    public TimeSpan NextStageChange;

    /// <summary>
    /// Cooldown between grab escalations, so the ladder can't be climbed instantly.
    /// </summary>
    [DataField]
    public TimeSpan StageChangeCooldown = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// Base chance the victim has of breaking a grab at each stage, before mass is taken into account.
    /// </summary>
    [DataField]
    public Dictionary<GrabStage, float> EscapeChances = new()
    {
        { GrabStage.No, 1f },
        { GrabStage.Soft, 0.7f },
        { GrabStage.Hard, 0.4f },
        { GrabStage.Suffocate, 0.1f },
    };

    /// <summary>
    /// Stamina damage dealt to the victim each time the choke is squeezed again.
    /// </summary>
    [DataField]
    public float SuffocateGrabStaminaDamage = 10f;

    /// <summary>
    /// Virtual items currently occupying the puller's hands because of the grab.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> GrabVirtualItems = new();

    /// <summary>
    /// How many extra hands each stage costs. Choking needs a second hand on the throat.
    /// </summary>
    [DataField]
    public Dictionary<GrabStage, int> GrabVirtualItemStageCount = new()
    {
        { GrabStage.Suffocate, 1 },
    };

    [DataField]
    public float SoftGrabSpeedModifier = 0.9f;

    [DataField]
    public float HardGrabSpeedModifier = 0.7f;

    [DataField]
    public float ChokeGrabSpeedModifier = 0.4f;

    #endregion

    #region Slamming - claw command

    /// <summary>
    /// Lowest grab stage that lets you slam your victim into things. Soft grabs are just a hold.
    /// </summary>
    [DataField]
    public GrabStage SlamRequiredStage = GrabStage.Hard;

    /// <summary>
    /// How long after a slam - successful or not - before this puller can escalate or slam again.
    /// Shares <see cref="NextStageChange"/> with the grab ladder, so a slam locks both out.
    /// </summary>
    [DataField]
    public TimeSpan SlamCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How far the victim may be from the thing you are slamming them into.
    /// </summary>
    [DataField]
    public float SlamRange = 2f;

    #endregion
}

public sealed partial class StopPullingAlertEvent : BaseAlertEvent;
