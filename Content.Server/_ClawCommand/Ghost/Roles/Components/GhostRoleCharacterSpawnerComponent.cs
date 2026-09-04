using Content.Server.Ghost.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Claw Command - Allows a ghost to take this role, spawning their selected character.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(GhostRoleSystem))]
    public sealed partial class GhostRoleCharacterSpawnerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("deleteOnSpawn")]
        public bool DeleteOnSpawn = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        public int AvailableTakeovers = 1;

        [ViewVariables]
        public int CurrentTakeovers = 0;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("outfitPrototype")]
        public string OutfitPrototype = "PassengerGear";

        /// <summary>
        /// NPC factions to add to the spawned character.
        /// </summary>
        [DataField]
        public List<string> Factions = new();

        /// <summary>
        /// Implants to inject into the spawned character.
        /// </summary>
        [DataField]
        public List<EntProtoId> Implants = new();
    }
}
