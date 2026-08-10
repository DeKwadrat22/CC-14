using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Station;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Sprite;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<VisualBodyComponent>(PreviewDummy))
            return;

        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
        EntMan.System<SharedScaleVisualsSystem>().SetSpriteScale(PreviewDummy, new System.Numerics.Vector2(humanoid.Width, humanoid.Height));
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    private void LoadHumanoidEntity(HumanoidCharacterProfile? humanoid, JobPrototype? job, bool jobClothes)
    {
        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes)
        {
            job ??= GetPreferredJob(humanoid);

            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job?.JobEntity;
        }

        if (previewEntity != null)
        {
            // Special type like borg or AI, do not spawn a human just spawn the entity.
            PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
            PreviewDummy = EntMan.SpawnEntity(dummy, MapCoordinates.Nullspace);
            EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
            EntMan.System<SharedScaleVisualsSystem>().SetSpriteScale(PreviewDummy, new System.Numerics.Vector2(humanoid.Width, humanoid.Height));
        }
        else
        {
            PreviewDummy = EntMan.SpawnEntity(_prototypeManager.Index(HumanoidCharacterProfile.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        if (humanoid != null && jobClothes)
        {
            DebugTools.Assert(job != null);

            GiveDummyJobClothes(humanoid, job);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            {
                var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, humanoid.Species, EntMan, _prototypeManager);
                GiveDummyLoadout(loadout);
            }
        }
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? SharedGameTicker.FallbackOverflowJob);
    }

    private void GiveDummyLoadout(RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(PreviewDummy, loadoutProto);
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy, the same way the server dresses a real spawn.
    /// </summary>
    /// <remarks>
    ///     Claw Command - this used to show the opposite of what a player actually spawned wearing.
    ///
    ///     On the server, <c>SpawnPlayerMob</c> equips the role loadout FIRST and the job's starting gear
    ///     SECOND. <c>EquipStartingGear</c> cannot displace an occupied slot - <c>TryEquip</c>'s <c>force</c>
    ///     only skips the CanEquip checks, while the container insert underneath still fails on a full slot -
    ///     so in game the loadout always wins and the job gear only fills what is left over.
    ///
    ///     The preview did it backwards: it applied the loadout, then walked every slot applying the job's
    ///     starting gear with an explicit force-unequip-and-delete first. Any slot both of them wanted showed
    ///     the job item. That is why picking a jumpsuit from a wardrobe group appeared to do nothing for jobs
    ///     whose starting gear also sets a jumpsuit, even though the wardrobe suit is what you spawn in.
    ///
    ///     Loadouts are now applied in the role prototype's group order and claim slots first-come, matching
    ///     <c>EquipRoleLoadout</c>, and the job gear only fills slots nothing has claimed.
    /// </remarks>
    private void GiveDummyJobClothes(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        // Claw Command - strip first, so a species doll that ships with default clothing does not leak into
        // the preview. This is what the old per-slot unequip amounted to, just hoisted out of the passes below.
        foreach (var slot in slots)
        {
            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var stripped, silent: true, force: true, reparent: false))
                EntMan.DeleteEntity(stripped.Value);
        }

        // Claw Command - slots already taken. First source to fill one keeps it, which is what the server's
        // occupied-slot insert failure amounts to.
        var claimed = new HashSet<string>();

        void Apply(IEquipmentLoadout gear)
        {
            foreach (var slot in slots)
            {
                if (claimed.Contains(slot.Name))
                    continue;

                var itemType = gear.GetGear(slot.Name);
                if (string.IsNullOrEmpty(itemType))
                    continue;

                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                claimed.Add(slot.Name);
            }
        }

        // Claw Command - ordered by the role prototype's group list, matching EquipRoleLoadout. The old code
        // walked SelectedLoadouts in dictionary order and let the last write win, which could disagree with
        // the server even before the job gear was involved.
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout)
            && _prototypeManager.TryIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID), out var roleProto))
        {
            foreach (var group in jobLoadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
            {
                foreach (var loadout in group.Value)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    // A loadout may delegate to a StartingGear prototype instead of carrying gear itself.
                    if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        Apply(loadoutGear);
                    else
                        Apply(loadoutProto);
                }
            }
        }

        // Claw Command - job gear last, filling only what no loadout claimed. Previously this ran with a
        // force-unequip per slot and overwrote the loadout, which is what inverted the preview.
        if (_prototypeManager.Resolve(job.StartingGear, out var gear))
            Apply(gear);
    }
}
