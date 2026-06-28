using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed partial class ContainerSpawnPointSystem : EntitySystem
{
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: new []{ typeof(SpawnPointSystem) });
    }

    public void HandlePlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // Claw Command: the Cryosleep SpawnPriorityPreference is intentionally ignored — the lobby
        // UI still shows/saves it (preference round-trips), but here we only handle JobEntity jobs
        // (AI, Borg) that need to be inserted into a specific container. Humanoid players fall
        // through to ArrivalsSystem, which is now subscribed before us so it always wins anyway.
        if (!_proto.Resolve(args.Job, out var jobProto) || jobProto.JobEntity == null)
            return;

        var query = EntityQueryEnumerator<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>();
        var possibleContainers = new List<Entity<ContainerSpawnPointComponent, ContainerManagerComponent, TransformComponent>>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var container, out var xform))
        {
            if (args.Station != null && _station.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Claw Command: require an explicit job match on the container. This keeps AI in the
            // AI core (PlayerStationAi has job: StationAi) and Borg in its pod, while jobless
            // cryo containers (CryogenicSleepUnitSpawner / *LateJoin) are skipped completely —
            // even when a Cryosleep-pref player somehow reaches this code, no container matches.
            if (spawnPoint.Job == null || spawnPoint.Job != args.Job)
                continue;

            if (spawnPoint.SpawnType == SpawnPointType.Unset)
            {
                possibleContainers.Add((uid, spawnPoint, container, xform));
                continue;
            }

            if (_gameTicker.RunLevel == GameRunLevel.InRound && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                possibleContainers.Add((uid, spawnPoint, container, xform));
            }

            if (_gameTicker.RunLevel != GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.Job)
            {
                possibleContainers.Add((uid, spawnPoint, container, xform));
            }
        }

        if (possibleContainers.Count == 0)
            return;
        // we just need some default coords so we can spawn the player entity.
        var baseCoords = possibleContainers[0].Comp3.Coordinates;

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            baseCoords,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);

        _random.Shuffle(possibleContainers);
        foreach (var (uid, spawnPoint, manager, xform) in possibleContainers)
        {
            if (!_container.TryGetContainer(uid, spawnPoint.ContainerId, out var container, manager))
                continue;

            if (!_container.Insert(args.SpawnResult.Value, container, containerXform: xform))
                continue;

            var ev = new ContainerSpawnEvent(args.SpawnResult.Value);
            RaiseLocalEvent(uid, ref ev);

            return;
        }

        Del(args.SpawnResult);
        args.SpawnResult = null;
    }
}

/// <summary>
/// Raised on a container when a player is spawned into it.
/// </summary>
[ByRefEvent]
public record struct ContainerSpawnEvent(EntityUid Player);
