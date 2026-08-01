using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Clothing.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Ghost;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles
{
    /// <summary>
    /// Claw Command - Partial class extension for character-based ghost role spawning.
    /// Spawns the player's currently selected character instead of a random mob.
    /// Blocks if the player already has a character with the same name in the round.
    /// </summary>
    public sealed partial class GhostRoleSystem
    {
        [Dependency] private IServerPreferencesManager _prefs = default!;
        [Dependency] private OutfitSystem _outfitSystem = default!;
        [Dependency] private IChatManager _chatMan = default!;
        [Dependency] private NpcFactionSystem _factionSystem = default!;
        [Dependency] private SharedSubdermalImplantSystem _implantSystem = default!;

        private void OnSpawnerTakeCharacter(EntityUid uid, GhostRoleCharacterSpawnerComponent component,
            ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                ghostRole.Taken)
            {
                args.TookRole = false;
                return;
            }

            var character = (HumanoidCharacterProfile) _prefs.GetPreferences(args.Player.UserId).SelectedCharacter;

            // Check if this player already has a character with this name in the round
            if (!IsCharacterNameAvailable(args.Player, character.Name))
            {
                // Don't consume the role - just reject this player
                args.TookRole = false;

                _chatMan.ChatMessageToOne(
                    Shared.Chat.ChatChannel.Server,
                    Loc.GetString("ghost-role-character-name-taken"),
                    Loc.GetString("chat-manager-server-wrap-message",
                        ("message", Loc.GetString("ghost-role-character-name-taken"))),
                    default,
                    false,
                    args.Player.Channel,
                    Color.Red);
                return;
            }

            var mob = EntityManager.System<StationSpawningSystem>()
                .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
            _transform.AttachToGridOrMap(mob);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
            RaiseLocalEvent(mob, ref spawnedEvent);

            EnsureComp<MindContainerComponent>(mob);

            GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

            // Apply outfit after spawning
            if (!string.IsNullOrEmpty(component.OutfitPrototype))
                _outfitSystem.SetOutfit(mob, component.OutfitPrototype);

            // Apply factions
            if (component.Factions.Count > 0)
            {
                var factionComp = EnsureComp<NpcFactionMemberComponent>(mob);
                foreach (var faction in component.Factions)
                {
                    _factionSystem.AddFaction((mob, factionComp), faction);
                }
            }

            // Apply implants
            if (component.Implants.Count > 0)
                _implantSystem.AddImplants(mob, component.Implants);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            args.TookRole = true;
        }

        /// <summary>
        /// Checks if the player's selected character name is available (not already used this round).
        /// Uses the same logic as the ghost respawn duplicate character check.
        /// </summary>
        private bool IsCharacterNameAvailable(ICommonSession player, string characterName)
        {
            var allPlayerMinds = EntityQuery<MindComponent>()
                .Where(mind => mind.OriginalOwnerUserId == player.UserId);

            foreach (var mind in allPlayerMinds)
            {
                // Skip minds on ghost entities - ghosts inherit the character name but aren't real mobs
                if (mind.CurrentEntity is { } currentEntity && HasComp<GhostComponent>(currentEntity))
                    continue;

                // Skip minds with no entity (fully ghosted/disconnected)
                if (mind.CurrentEntity == null)
                    continue;

                // Exact name match - blocked
                if (mind.CharacterName == characterName)
                    return false;

                if (mind.CharacterName == null)
                    continue;

                // Similarity check - 85%+ is too similar
                var similarity = CalculateNameSimilarity(mind.CharacterName, characterName);
                if (similarity >= 85f)
                    return false;
            }

            return true;
        }

        private static float CalculateNameSimilarity(string str1, string str2)
        {
            var minLength = Math.Min(str1.Length, str2.Length);
            var matchingCharacters = 0;

            for (var i = 0; i < minLength; i++)
            {
                if (str1[i] == str2[i])
                    matchingCharacters++;
            }

            float maxLength = Math.Max(str1.Length, str2.Length);
            return (matchingCharacters / maxLength) * 100;
        }
    }
}
