using Content.Server._ClawCommand.StationEvents.Events;

namespace Content.Server._ClawCommand.StationEvents.Components;

[RegisterComponent, Access(typeof(SyndieOutpostSpawnRule))]
public sealed partial class SyndieOutpostSpawnRuleComponent : Component
{
    [DataField]
    public List<string> OutpostMapPaths { get; private set; } = new()
    {
        "Maps/_ClawCommand/syndieoutpost.yml",
    };

    [DataField]
    public EntityUid? AdditionalRule;

    [DataField]
    public int DebrisCount { get; set; }

    [DataField]
    public float MinimumDistance { get; set; } = 750f;

    [DataField]
    public float MaximumDistance { get; set; } = 1250f;

    [DataField]
    public float MinimumDebrisDistance { get; set; } = 150f;

    [DataField]
    public float MaximumDebrisDistance { get; set; } = 250f;

    [DataField]
    public float DebrisMinimumOffset { get; set; } = 50f;

    [DataField]
    public float DebrisMaximumOffset { get; set; } = 100f;
}
