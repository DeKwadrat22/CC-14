using Content.Shared.GameTicking;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server._ClawCommand.Silicons;

/// <summary>
/// Claw Command: when a player spawns into a cyborg or dogborg job (round start
/// or late join), copy their character profile name onto the spawned entity.
///
/// Why this exists: BaseBorgChassisNT used to carry a RandomMetadata that stamped
/// a "Borg-XX-YY" name onto every chassis-NT-derived entity at init, overriding
/// whatever the job system would have applied later. Borg jobs run with
/// `applyTraits: false`, so the upstream profile-name path doesn't run either —
/// the player's character name was never honoured. We disabled the RandomMetadata
/// on BaseBorgChassisNT and rely on this system to assign the profile name on
/// PlayerSpawnCompleteEvent, which fires for both round start and late join.
///
/// Ghost-role / admin-spawn / derelict borgs are untouched: their entities don't
/// fire PlayerSpawnCompleteEvent, and the ghost-role derelict variants carry
/// their own RandomMetadata for the no-profile fallback.
/// </summary>
public sealed partial class BorgSpawnNameSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<BorgChassisComponent>(args.Mob))
            return;

        var name = args.Profile.Name;
        if (string.IsNullOrWhiteSpace(name))
            return;

        _metadata.SetEntityName(args.Mob, name);
    }
}
