using System.Linq;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchSystem : EntitySystem
{
    /// <summary>
    /// _ClawCommand: percentage progress toward unlocking the next tier in
    /// this discipline, shown on the fancy research console's side panel.
    ///
    /// The old implementation divided every unlocked tech in the discipline
    /// by the discipline's TOTAL tech count (across every tier). That made
    /// the number incompatible with the engine's actual tier-prereq gate
    /// (which uses `unlockedOfTier / totalOfTier` per tier in
    /// <see cref="GetHighestDisciplineTier(TechnologyDatabaseComponent, TechDisciplinePrototype)"/>),
    /// so a player with all of Tier 1 unlocked saw a number well under
    /// 75% and assumed they couldn't go to Tier 2.
    ///
    /// We now report progress within the player's currently-accessible tier
    /// — i.e. the tier whose completion percentage gates access to the next
    /// tier. When the player is already at the highest tier (no further tier
    /// to unlock), we return 100.
    /// </summary>
    public int GetTierCompletionPercentage(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        var currentTier = GetHighestDisciplineTier(component, techDiscipline);

        // If we're already at or beyond the highest tier the discipline
        // defines a prereq for, there's no "next tier" to progress toward.
        var highestPrereqTier = techDiscipline.TierPrerequisites.Keys.Max();
        if (currentTier >= highestPrereqTier)
            return 100;

        var allTierTech = PrototypeManager.EnumeratePrototypes<TechnologyPrototype>()
            .Where(p => p.Discipline == techDiscipline.ID && p.Tier == currentTier && !p.Hidden)
            .ToList();

        if (allTierTech.Count == 0)
            return 100;

        var unlockedTierCount = component.UnlockedTechnologies
            .Count(x => PrototypeManager.TryIndex<TechnologyPrototype>(x, out var proto)
                        && proto.Discipline == techDiscipline.ID
                        && proto.Tier == currentTier);

        var percentage = (float) unlockedTierCount / allTierTech.Count * 100f;
        return (int) Math.Clamp(percentage, 0, 100);
    }
}
