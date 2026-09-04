using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Adds the entity to the AnimalFriend faction on startup,
///     making hostile animals treat them as friendly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnimalFriendComponent : Component;
