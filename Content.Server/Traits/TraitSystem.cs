using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed partial class TraitSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !ProtoMan.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        // Claw Command - track which traits were applied for exclusion checks.
        var appliedTraits = new HashSet<string>();

        // Claw Command - resolve which departments the player's job belongs to.
        var playerDepts = new HashSet<string>();
        if (args.JobId != null)
        {
            foreach (var dept in ProtoMan.EnumeratePrototypes<DepartmentPrototype>())
            {
                foreach (var role in dept.Roles)
                {
                    if (role.Id == args.JobId)
                    {
                        playerDepts.Add(dept.ID);
                        break;
                    }
                }
            }
        }

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (!ProtoMan.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                continue;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, args.Mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, args.Mob))
                continue;

            // Claw Command - skip if an already-applied trait is mutually exclusive.
            var excluded = false;
            foreach (var ex in traitPrototype.Excludes)
            {
                if (appliedTraits.Contains(ex))
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded)
                continue;

            // Claw Command - skip if the player's job is in a restricted department.
            if (traitPrototype.RestrictedDepts.Count > 0)
            {
                var blocked = false;
                foreach (var dept in traitPrototype.RestrictedDepts)
                {
                    if (playerDepts.Contains(dept))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked)
                    continue;
            }

            // Claw Command - mark trait as applied for exclusion tracking.
            appliedTraits.Add(traitId);

            // Add all components required by the prototype
            // Claw Command - overwrite enabled so trait components can replace existing ones (e.g. Flashable, LightweightDrunk)
            if (traitPrototype.Components.Count > 0)
                EntityManager.AddComponents(args.Mob, traitPrototype.Components, true);

            // Add all JobSpecials required by the prototype
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(args.Mob);
            }

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(args.Mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(args.Mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(args.Mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
}
