// Heretic adds a `productHereticKnowledge` slot to listings, which grants knowledge
// points instead of a regular product entity. We extend ListingData here so the YAML
// loads cleanly; the actual grant logic is handled in HereticSystem's purchase hook.

using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store
{
    public partial class ListingData
    {
        [DataField]
        public ProtoId<HereticKnowledgePrototype>? ProductHereticKnowledge;
    }
}
