using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Scales the delay when climbing over objects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VaultDelayAdjustComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Factor = 1f;
}

/// <summary>
///     Adjusts walk and sprint movement speed via multipliers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GaitSpeedAdjustComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SprintFactor = 1.0f;

    [DataField, AutoNetworkedField]
    public float WalkFactor = 1.0f;

    [DataField, AutoNetworkedField]
    public float TriggeredFactor = 1.0f;
}

/// <summary>
///     Offsets footstep audio volume for walk and sprint.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StepAudioAdjustComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WalkAdjust;

    [DataField, AutoNetworkedField]
    public float SprintAdjust;
}
