using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.InteractionVerbs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionVerbsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<InteractionVerbPrototype>> AllowedVerbs = new();
}
