using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Claw Command - Client sends this to the server to set WalkByDefault on their InputMoverComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetWalkByDefaultEvent : EntityEventArgs
{
    public bool WalkByDefault;

    public SetWalkByDefaultEvent(bool walkByDefault)
    {
        WalkByDefault = walkByDefault;
    }
}
