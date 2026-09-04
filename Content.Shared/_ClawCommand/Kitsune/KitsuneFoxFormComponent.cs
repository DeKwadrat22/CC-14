using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Kitsune;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KitsuneFoxFormComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> FoxPolymorphId = "PolymorphKitsune";

    [DataField]
    public EntProtoId Action = "ActionToggleFoxForm";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public Color? FoxColor;

    [DataField, AutoNetworkedField]
    public Color FoxBodyColor = Color.White;

    [DataField, AutoNetworkedField]
    public Color FoxInnerEarColor = Color.Black;
}

[Serializable, NetSerializable]
public enum KitsuneColorVisuals : byte
{
    Body,
    Overlay
}
