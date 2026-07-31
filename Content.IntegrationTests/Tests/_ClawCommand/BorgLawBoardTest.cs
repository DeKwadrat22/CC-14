using Content.IntegrationTests.Fixtures;
using Content.Server.Silicons.Laws;
using Content.Shared._ClawCommand.Silicons.Borgs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._ClawCommand;

/// <summary>
/// CLAW COMMAND - a borg's laws come off the law board in its chassis, and the board it is assembled with
/// is the one its borg type is issued with. This checks a spawned dogborg actually ends up running its
/// department's lawset rather than the chassis' factory Crewsimov.
/// </summary>
[TestFixture]
public sealed class BorgLawBoardTest : GameTest
{
    private static readonly (string Proto, string Board, string Lawset)[] Cases =
    {
        ("PlayerDogborgSecurity", "SiliconPoliceCircuitBoard", "SiliconPolice"),
        ("PlayerDogborgMedical", "MedicalCircuitBoard", "Medical"),
        ("BorgChassisEngineer", "EngineerCircuitBoard", "Engineer"),
    };

    [Test]
    public async Task BorgsSpawnWithTheirDepartmentLawset()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var sysMan = server.ResolveDependency<IEntitySystemManager>();

        await server.WaitAssertion(() =>
        {
            var itemSlots = sysMan.GetEntitySystem<ItemSlotsSystem>();
            var laws = sysMan.GetEntitySystem<SiliconLawSystem>();

            foreach (var (proto, expectedBoard, expectedLawset) in Cases)
            {
                var borg = entMan.SpawnEntity(proto, testMap.GridCoords);

                Assert.That(entMan.TryGetComponent<BorgLawBoardComponent>(borg, out var lawBoard),
                    $"{proto} has no BorgLawBoardComponent.");

                Assert.That(itemSlots.TryGetSlot(borg, lawBoard!.SlotId, out var slot),
                    $"{proto} has no {lawBoard.SlotId} item slot.");

                Assert.That(slot!.Item, Is.Not.Null, $"{proto} spawned with no law board installed.");

                var boardProto = entMan.GetComponent<MetaDataComponent>(slot.Item!.Value).EntityPrototype?.ID;
                Assert.That(boardProto, Is.EqualTo(expectedBoard), $"{proto} has the wrong law board.");

                Assert.That(entMan.TryGetComponent<SiliconLawProviderComponent>(borg, out var provider),
                    $"{proto} has no SiliconLawProviderComponent.");

                Assert.That(provider!.Laws.Id, Is.EqualTo(expectedLawset), $"{proto} advertises the wrong lawset id.");

                var lawset = laws.GetLaws(borg);
                var expected = laws.GetLawset(expectedLawset);
                Assert.That(lawset.Laws.Count, Is.EqualTo(expected.Laws.Count),
                    $"{proto} reports {lawset.Laws.Count} laws, expected {expected.Laws.Count} from {expectedLawset}.");
                Assert.That(lawset.Laws[0].LawString, Is.EqualTo(expected.Laws[0].LawString),
                    $"{proto} first law is '{lawset.Laws[0].LawString}', expected '{expected.Laws[0].LawString}'.");

                entMan.DeleteEntity(borg);
            }
        });
    }
}
