using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Pool;
using Content.Server.Spawners.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
public sealed class CreateCrystalsOffsetTest
{
    private const string Beaker = "Beaker";

    [Test]
    public async Task CreateCrystals_Reaction_SpawnsCrystalInWorld()
    {
        var report = new List<string>();
        await using (var pair = await PoolManager.GetServerClient())
        {
            var server = pair.Server;
            var sEntMan = server.EntMan;
            var solnSys = server.EntMan.System<SharedSolutionContainerSystem>();
            var testMap = await pair.CreateTestMap();

            var beakerOnGrid = EntityUid.Invalid;
            var beakerInSpace = EntityUid.Invalid;

            await server.WaitPost(() =>
            {
                beakerOnGrid = sEntMan.SpawnEntity(Beaker, testMap.GridCoords);
                beakerInSpace = sEntMan.SpawnEntity(Beaker, new MapCoordinates(new Vector2(200, 250), testMap.MapId));
            });

            await server.WaitIdleAsync();

            await server.WaitPost(() =>
            {
                Trigger(solnSys, report, beakerOnGrid, "ONGRID");
                Trigger(solnSys, report, beakerInSpace, "SPACE");
            });

            await pair.RunTicksSync(30);

            await server.WaitPost(() =>
            {
                var spawners = sEntMan.EntityQuery<EntityTableSpawnerComponent>().ToList();
                report.Add($"SPAWNERS_LEFT: {spawners.Count}");

                foreach (var (tag, beaker) in new[] { ("ONGRID", beakerOnGrid), ("SPACE", beakerInSpace) })
                {
                    var bmp = sEntMan.GetComponent<TransformComponent>(beaker).MapPosition;
                    report.Add($"{tag} beaker Map={bmp.MapId} X={bmp.Position.X:F2} Y={bmp.Position.Y:F2}");

                    var shards = sEntMan.EntityQuery<MetaDataComponent>()
                        .Where(m => m.EntityPrototype?.ID != null && m.EntityPrototype.ID.StartsWith("ShardCrystal"))
                        .Select(m => m.Owner)
                        .ToList();
                    foreach (var uid in shards)
                    {
                        var mp = sEntMan.GetComponent<TransformComponent>(uid).MapPosition;
                        var proto = sEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID ?? "?";
                        var parentOk = sEntMan.GetComponent<TransformComponent>(uid).ParentUid.IsValid();
                        var nearOk = mp.MapId == bmp.MapId && (mp.Position - bmp.Position).Length() < 3f;
                        report.Add($"  {tag} shard {uid} {proto} parent={sEntMan.GetComponent<TransformComponent>(uid).ParentUid} Map={mp.MapId} X={mp.Position.X:F2} Y={mp.Position.Y:F2} nearBeaker={nearOk}");
                    }
                }

                var allShards = sEntMan.EntityQuery<MetaDataComponent>()
                    .Where(m => m.EntityPrototype?.ID != null && m.EntityPrototype.ID.StartsWith("ShardCrystal"))
                    .ToList();
                bool nullspace = allShards.Any(m =>
                    !sEntMan.GetComponent<TransformComponent>(m.Owner).ParentUid.IsValid() ||
                    sEntMan.GetComponent<TransformComponent>(m.Owner).MapPosition.MapId == MapId.Nullspace);
                report.Add(nullspace ? "VERDICT: HAS NULLSPACE SHARD" : "VERDICT: NO NULLSPACE SHARD");
            });

            await pair.RunTicksSync(20);

            await server.WaitPost(() =>
            {
                var cShards = pair.Client.EntMan.EntityQuery<MetaDataComponent>()
                    .Where(m => m.EntityPrototype?.ID != null && m.EntityPrototype.ID.StartsWith("ShardCrystal"))
                    .ToList();
                report.Add($"CLIENT_SHARDS: {cShards.Count}");
                foreach (var m in cShards)
                {
                    var mp = pair.Client.EntMan.GetComponent<TransformComponent>(m.Owner).MapPosition;
                    report.Add($"  client {m.Owner} {m.EntityPrototype?.ID} Map={mp.MapId} X={mp.Position.X:F2} Y={mp.Position.Y:F2} parent={pair.Client.EntMan.GetComponent<TransformComponent>(m.Owner).ParentUid}");
                }
            });
        }

        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "cc14_reactionfix.txt"), report);
        Assert.That(report.Any(r => r.Contains("HAS NULLSPACE SHARD")), Is.False, string.Join("\n", report));
    }

    private static void Trigger(
        SharedSolutionContainerSystem solnSys,
        List<string> report,
        EntityUid beaker,
        string tag)
    {
        if (!solnSys.TryGetSolution(beaker, "beaker", out var soln, out _))
        {
            report.Add($"{tag}: FAIL can't get solution");
            return;
        }

        var sol = soln!.Value;
        solnSys.SetCanReact(sol, false);
        solnSys.TryAddReagent(sol, "Sugar", 15, out _);
        solnSys.TryAddReagent(sol, "Water", 15, out _);
        solnSys.TryAddReagent(sol, "Ethanol", 5, out _);
        solnSys.SetTemperature(sol, 400f);
        solnSys.SetCanReact(sol, true);
        report.Add($"{tag}: fired, vol after={sol.Comp.Solution.Volume}, temp={sol.Comp.Solution.Temperature}");
    }
}