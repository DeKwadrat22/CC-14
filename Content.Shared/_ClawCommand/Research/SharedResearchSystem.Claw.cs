using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Shared.Research.Systems;

public abstract partial class SharedResearchSystem : EntitySystem
{
    /// <summary>
    /// _ClawCommand: percentage progress toward unlocking the next tier in
    /// this discipline, shown on the fancy research console's side panel.
    ///
    /// The tier-prerequisite gate has been removed (see
    /// <see cref="GetHighestDisciplineTier(TechnologyDatabaseComponent, TechDisciplinePrototype)"/>),
    /// so there is no gate left to progress toward — every tier is always
    /// available. Return 100.
    /// </summary>
    public int GetTierCompletionPercentage(TechnologyDatabaseComponent component, TechDisciplinePrototype techDiscipline)
    {
        return 100;
    }
}
