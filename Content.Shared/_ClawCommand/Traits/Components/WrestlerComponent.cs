using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Exempts the holder from a pullable's <see cref="Movement.Pulling.Components.PullableComponent.MaxGrabStage"/>
///     cap, letting them escalate a grab on anything they can pull at all.
///
///     Hostile mobs cap at Soft so a passer-by cannot choke a space carp into submission. A wrestler
///     can. This grants no other advantage: escape chances, stamina drain and the grab ladder itself
///     are untouched, so the mob still fights back exactly as hard.
///
///     Networked because the grab clamp is predicted - the client has to agree the puller is exempt,
///     or it will mispredict a refusal every time.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WrestlerComponent : Component;
