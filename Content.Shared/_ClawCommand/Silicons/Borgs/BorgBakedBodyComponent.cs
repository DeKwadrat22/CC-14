using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// Marker on borg chassis whose `Light` overlay sprite already includes the body
/// pixels baked in (e.g. dogborg variants where the `_e`/`_l` overlays carry the
/// full body so the walking animation stays in lockstep with the eye glow).
///
/// When this marker is present, the client BorgSystem hides the `Body` layer
/// whenever the `Light` layer is visible — otherwise the baked body and the
/// regular body render on top of each other and visibly drift apart frame by
/// frame.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BorgBakedBodyComponent : Component
{
}
