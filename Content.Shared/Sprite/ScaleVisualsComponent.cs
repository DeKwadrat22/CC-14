using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Sprite;

/// <summary>
/// Used to set the <see cref="Robust.Client.GameObjects.SpriteComponent.Scale"/> datafield to a certain value from the server.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScaleVisualsSystem))]
public sealed partial class ScaleVisualsComponent : Component
{
    /// <summary>
    /// The current sprite scale.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables]
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// Claw Command - Whether the sprite should grow upward from its base instead of outward from
    /// its centre.
    /// </summary>
    /// <remarks>
    /// Sprite scale is applied about the entity origin, so scaling a centred 32x32 mob to 1.2 height
    /// pushes its feet ~3px into the tile to the south as well as raising its head. That is visible as
    /// clipping through furniture, and it also skews sprite sorting: the renderer y-sorts on the bottom
    /// of the scaled bounding box, so a taller mob sorts as though it were standing further south and
    /// draws in front of things it is actually behind. Setting this pins the bottom of the bounding box
    /// where it would be at scale 1, which fixes both.
    ///
    /// Leave this off for entities that should scale about their centre, such as free-floating effects.
    /// </remarks>
    [DataField, AutoNetworkedField]
    [ViewVariables]
    public bool PinBottom;

    /// <summary>
    /// The original sprite scale, which we revert to if this component is removed.
    /// Only set on the client.
    /// </summary>
    [DataField]
    [ViewVariables]
    public Vector2? OriginalScale;

    /// <summary>
    /// Claw Command - The original sprite offset, which we revert to if this component is removed
    /// or <see cref="PinBottom"/> is off. Only set on the client.
    /// </summary>
    [DataField]
    [ViewVariables]
    public Vector2? OriginalOffset;
}
