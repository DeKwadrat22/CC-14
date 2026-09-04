// Ported from Goobstation under AGPL-3.0-or-later.
// Original authors: Aiden, Aviu00, Misandry, Spatison, gus.

using System.Numerics;

namespace Content.Goobstation.Shared.Overlays;

public abstract partial class BaseVisionOverlayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual Vector3 Tint { get; set; } = new(0.3f, 0.3f, 0.3f);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual float Strength { get; set; } = 2f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual float Noise { get; set; } = 0.5f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public virtual Color Color { get; set; } = Color.White;
}
