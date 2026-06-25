using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     When added to an entity, changes their speech sounds to dog barks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DogSpeechComponent : Component;
