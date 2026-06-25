// Goob's heretic gamerule uses extra antag-selection fields (chaosScore, jobBlacklist).
// We extend the partial struct here so the YAML loads cleanly; the upstream AntagSelectionSystem
// ignores these new fields, so they have no runtime effect.

using System.Collections.Generic;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

// The two heretic-only fields below have no runtime effect (the upstream selector ignores them),
// so the lack of a defined ordering between this partial and the main struct doesn't matter.
#pragma warning disable CS0282

namespace Content.Server.Antag.Components
{
    public partial struct AntagSelectionDefinition
    {
        [DataField]
        public int ChaosScore;

        [DataField]
        public List<ProtoId<JobPrototype>>? JobBlacklist;
    }
}

#pragma warning restore CS0282

// Note: GhostRoleComponent.Requirements already exists in this fork's upstream
// definition (Content.Server.Ghost.Roles.Components.GhostRoleComponent), so no
// partial extension is needed here. Removed to avoid CS0102 duplicate-member error.
