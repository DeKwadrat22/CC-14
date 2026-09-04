using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Desaturates the world for the entity that has it. Applied by the mood system when an entity's mood
/// drops to Meh or below, to make a bad mood legible without a HUD readout.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SaturationScaleOverlayComponent : Component
{
    /// <summary>
    /// How much color to keep. 0 is fully greyscale, 1 is unchanged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Saturation = 0.5f;
}
