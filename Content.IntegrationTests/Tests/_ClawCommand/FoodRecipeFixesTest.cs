using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Kitchen;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._ClawCommand;

/// <summary>
/// CLAW COMMAND - covers two food fixes that are easy to break again by tweaking numbers:
/// mozzarella and curd cheese have to be separately reachable (they used to share milk + vinegar, so the
/// cheaper one ate the milk first and mozzarella was unmakeable), and carrot fries has to cook on the
/// microwave timer a player would actually press.
/// </summary>
[TestFixture]
public sealed class FoodRecipeFixesTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: ClawTestCheeseBeaker
  components:
  - type: Solution
    id: beaker
    solution:
      maxVol: 300";

    /// <summary>
    /// The buttons the microwave UI actually offers - see MicrowaveMenu, which builds i * 5 for i in 1..6.
    /// </summary>
    private static readonly uint[] TimerButtons = [5, 10, 15, 20, 25, 30];

    /// <summary>
    /// Carrot fries were reported as uncookable. They aren't: a carrot and a salt shaker select carrot
    /// fries and nothing else. What does block them is the microwave's rule that the timer must be an exact
    /// multiple of the recipe's cook time - at 15 seconds that means only the 15s and 30s buttons work,
    /// which is normal here and shared with ~90 other recipes. This pins both halves of that so a future
    /// "fix" doesn't get aimed at the wrong thing again.
    /// </summary>
    [Test]
    [TestOf(typeof(MicrowaveSystem))]
    public async Task CarrotAndSaltOnlyEverCookCarrotFries()
    {
        var pair = Pair;
        var server = pair.Server;
        var recipeManager = server.ResolveDependency<RecipeManager>();

        await server.WaitAssertion(() =>
        {
            // What is actually in the microwave: the carrot (which brings its own juice/vitamin/oculine
            // along) and the salt shaker holding 15u of table salt.
            var solids = new Dictionary<string, int>
            {
                ["FoodCarrot"] = 1,
                ["FoodShakerSalt"] = 1,
            };
            var reagents = new Dictionary<string, FixedPoint2>
            {
                ["JuiceCarrot"] = 5,
                ["Vitamin"] = 7,
                ["Oculine"] = 3,
                ["TableSalt"] = 15,
            };

            var friesCookTime = recipeManager.Recipes.First(r => r.ID == "RecipeCarrotFries").CookTime;

            Assert.Multiple(() =>
            {
                foreach (var timer in TimerButtons)
                {
                    var microwave = new MicrowaveComponent { CurrentCookTimerTime = timer };

                    // Mirrors MicrowaveSystem.Wzhzhzh: first satisfied recipe wins, and RecipeManager has
                    // already sorted them by ingredient count descending.
                    var picked = recipeManager.Recipes
                        .Select(r => MicrowaveSystem.CanSatisfyRecipe(microwave, r, solids, reagents))
                        .FirstOrDefault(r => r.Item2 > 0);

                    if (timer % friesCookTime == 0)
                    {
                        Assert.That(picked.Item1, Is.Not.Null, $"Nothing cooked on the {timer}s button.");
                        Assert.That(picked.Item1.ID,
                            Is.EqualTo("RecipeCarrotFries"),
                            $"A carrot and a salt shaker cooked {picked.Item1.ID} instead of carrot fries on the {timer}s button.");
                    }
                    else
                    {
                        // Not a bug to fix in this recipe - it is how every recipe in the game behaves.
                        Assert.That(picked.Item1,
                            Is.Null,
                            $"The {timer}s button is not a multiple of the {friesCookTime}s cook time, so nothing should cook, but {picked.Item1?.ID} did.");
                    }
                }
            });
        });
    }

    [Test]
    public async Task MozzarellaAndCurdCheeseAreBothReachable()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var testMap = await pair.CreateTestMap();
        var solutionSystem = entMan.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            // Milk plus vinegar: curd cheese, and only curd cheese.
            var vinegarBeaker = entMan.SpawnEntity("ClawTestCheeseBeaker", testMap.GridCoords);
            Assert.That(solutionSystem.TryGetSolution(vinegarBeaker, "beaker", out var vinegarSoln, out _));
            solutionSystem.TryAddReagent(vinegarSoln!.Value, "Vinegar", 10, out _);
            solutionSystem.TryAddReagent(vinegarSoln.Value, "Milk", 60, out _);

            Assert.That(CountOf(entMan, "FoodCurdCheese"), Is.GreaterThan(0), "Milk + vinegar made no curd cheese.");

            // Milk, cream and lemon juice: mozzarella. This is the one that used to be unreachable.
            var lemonBeaker = entMan.SpawnEntity("ClawTestCheeseBeaker", testMap.GridCoords);
            Assert.That(solutionSystem.TryGetSolution(lemonBeaker, "beaker", out var lemonSoln, out _));
            solutionSystem.TryAddReagent(lemonSoln!.Value, "JuiceLemon", 10, out _);
            solutionSystem.TryAddReagent(lemonSoln.Value, "Cream", 20, out _);
            solutionSystem.TryAddReagent(lemonSoln.Value, "Milk", 60, out _);

            Assert.That(CountOf(entMan, "FoodMozzarella"), Is.GreaterThan(0), "Milk + cream + lemon juice made no mozzarella.");
        });
    }

    private static int CountOf(IEntityManager entMan, string protoId)
    {
        var count = 0;
        var query = entMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out _, out var meta))
        {
            if (meta.EntityPrototype?.ID == protoId)
                count++;
        }

        return count;
    }
}
