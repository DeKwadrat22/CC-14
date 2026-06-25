using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
/// Traits category with general settings. Allows you to limit the number of taken traits in one category
/// </summary>
[Prototype]
public sealed partial class TraitCategoryPrototype : IPrototype
{
    public const string Default = "Default";

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Name of the trait category displayed in the UI
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    ///     The maximum number of trait points that can be spent in this category.
    ///     If null, no limit is enforced for this category.
    /// </summary>
    [DataField]
    public int? MaxTraitPoints;

    /// <summary>
    ///     Claw Command - Optional shared budget pool. Categories with the same BudgetPool
    ///     share a single trait point allocation while remaining visually separate in the UI.
    ///     The MaxTraitPoints of the pool is taken from the first category that defines it.
    /// </summary>
    [DataField]
    public string? BudgetPool;
}
