using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Heretic.Components;

/// <summary>
/// Marker for an ascended Path of Lock heretic. Grants passive bypass of access readers
/// (doors, lockers, ID-gated machines) — checked in <see cref="Content.Shared.Access.Systems.AccessReaderSystem.IsAllowed"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LockMasterComponent : Component
{
}
