using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Content.Server._ClawCommand.StationEvents.Components;
using Content.Shared._ClawCommand.SyndieOutpost;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._ClawCommand.StationEvents.Events;

public sealed partial class SyndieOutpostSpawnRule : StationEventSystem<SyndieOutpostSpawnRuleComponent>
{
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private StationSystem _stations = default!;
    [Dependency] private MapSystem _mapSystem = default!;

    protected override void Started(EntityUid uid, SyndieOutpostSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Gather all stations with data
        var stations = new List<(EntityUid Uid, StationDataComponent Data)>();
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var sUid, out var sData))
        {
            // Skip stations on planet surfaces
            if (_stations.GetLargestGrid((sUid, sData)) is { } grid && HasComp<BiomeComponent>(Transform(grid).MapUid))
                continue;

            stations.Add((sUid, sData));
        }

        if (stations.Count <= 0)
            return;

        var (chosenStation, stationData) = _random.Pick(stations);
        if (_stations.GetLargestGrid((chosenStation, stationData)) is not { } targetStation)
            return;

        var targetMapId = Transform(targetStation).MapID;
        if (!_mapSystem.MapExists(targetMapId))
            return;

        var randomOffset = _random.NextVector2(component.MinimumDistance, component.MaximumDistance);
        var spawnPos = _xform.GetWorldPosition(targetStation) + randomOffset;

        if (!_mapLoader.TryLoadGrid(targetMapId, new ResPath(_random.Pick(component.OutpostMapPaths)), out var outpost, offset: spawnPos))
            return;

        // Register outpost grid as part of the target station so device networks,
        // crew monitoring, cameras etc. can communicate with the station's systems.
        _stations.AddGridToStation(chosenStation, outpost.Value);

        // Add marker so pinpointers can find the outpost
        EnsureComp<SyndieOutpostMarkerComponent>(outpost.Value);

        Log.Info($"Syndicate outpost spawned at offset {randomOffset} from station {ToPrettyString(chosenStation)}");

        SpawnDebris(component, outpost.Value, targetMapId);
    }

    private void SpawnDebris(SyndieOutpostSpawnRuleComponent component, EntityUid outpost, MapId mapId)
    {
        if (component.DebrisCount <= 0)
            return;

        var outpostPos = _xform.GetWorldPosition(outpost);

        for (var k = 0; k < component.DebrisCount; k++)
        {
            var debrisRandomOffset = _random.NextVector2(component.MinimumDebrisDistance, component.MaximumDebrisDistance);
            var randomer = _random.NextVector2(component.DebrisMinimumOffset, component.DebrisMaximumOffset);
            var debrisPos = outpostPos + debrisRandomOffset + randomer;

            if (!_mapSystem.MapExists(mapId))
                return;

            var salvPrototypes = _prototypeManager.EnumeratePrototypes<SalvageMapPrototype>().ToList();
            var salvageProto = _random.Pick(salvPrototypes);

            _mapLoader.TryLoadGrid(mapId, new ResPath(salvageProto.MapPath.ToString()), out _, offset: debrisPos);
        }
    }

    protected override void Ended(EntityUid uid, SyndieOutpostSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.AdditionalRule != null)
            GameTicker.EndGameRule(component.AdditionalRule.Value);
    }
}
