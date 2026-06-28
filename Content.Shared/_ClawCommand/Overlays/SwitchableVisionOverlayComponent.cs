// Ported from Goobstation under AGPL-3.0-or-later.
// Original authors: Aiden, Aviu00, Misandry, Spatison, gus.

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Overlays;

public abstract partial class SwitchableVisionOverlayComponent : BaseVisionOverlayComponent
{
    [DataField]
    public bool IsActive;

    [DataField]
    public bool DrawOverlay = true;

    [DataField]
    public float OverlayOpacity = 0.5f;

    [DataField]
    public bool IsEquipment;

    [DataField]
    public float PulseTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public float PulseAccumulator;

    [DataField]
    public float FlashDurationMultiplier = 1f;

    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/_White/Items/Goggles/activate.ogg");

    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/_White/Items/Goggles/deactivate.ogg");

    [DataField]
    public virtual EntProtoId? ToggleAction { get; set; }

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

[Serializable, NetSerializable]
public sealed class SwitchableVisionOverlayComponentState : IComponentState
{
    public Color Color;
    public bool IsEquipment;
    public bool IsActive;
    public float FlashDurationMultiplier;
    public SoundSpecifier? ActivateSound;
    public SoundSpecifier? DeactivateSound;
    public EntProtoId? ToggleAction;
    public float LightRadius;
    public string? ThermalShader;
    public bool DrawOverlay;
    public float OverlayOpacity;
}
