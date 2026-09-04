using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Undergarment;

[ RegisterComponent, NetworkedComponent ]
public sealed partial class UndergarmentRemovalComponent : Component
{
    [DataField]
    public bool Removed;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> SavedMarkings = new();
}
