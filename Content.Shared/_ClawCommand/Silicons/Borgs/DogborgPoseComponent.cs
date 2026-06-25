using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// Tracks the currently-held pose (sit / rest / belly-up) on a dogborg chassis.
/// Used by <see cref="DogborgPoseSystem"/> together with the matching
/// emote/action prototypes to swap the Body sprite layer to a Citadel-ported
/// pose state for social roleplay.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DogborgPoseComponent : Component
{
    [DataField, AutoNetworkedField]
    public DogborgPose Pose = DogborgPose.None;
}

[Serializable, NetSerializable]
public enum DogborgPose : byte
{
    None = 0,
    Sit,
    Rest,
    BellyUp,
}
