using Content.Shared.Humanoid.Prototypes; // Claw Command
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
/// Describes a trait.
/// </summary>
[Prototype]
public sealed partial class TraitPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this trait.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    /// The description of this trait.
    /// </summary>
    [DataField]
    public LocId? Description { get; private set; }

    /// <summary>
    /// Don't apply this trait to entities this whitelist IS NOT valid for.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Don't apply this trait to entities this whitelist IS valid for. (hence, a blacklist)
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The components that get added to the player, when they pick this trait.
    /// NOTE: When implementing a new trait, it's preferable to add it as a status effect instead if possible.
    /// </summary>
    [DataField]
    [Obsolete("Use JobSpecial instead.")]
    public ComponentRegistry Components { get; private set; } = new();

    /// <summary>
    /// Special effects applied to the player who takes this Trait.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<JobSpecial> Specials { get; private set; } = new();

    /// <summary>
    /// Gear that is given to the player, when they pick this trait.
    /// </summary>
    [DataField]
    public EntProtoId? TraitGear;

    /// <summary>
    /// Trait Price. If negative number, points will be added.
    /// </summary>
    [DataField]
    public int Cost = 0;

    /// <summary>
    /// Adds a trait to a category, allowing you to limit the selection of some traits to the settings of that category.
    /// </summary>
    [DataField]
    public ProtoId<TraitCategoryPrototype>? Category;

    /// <summary>
    ///     Claw Command - Trait IDs that are mutually exclusive with this trait.
    ///     If any of these traits are already selected, this trait cannot be taken (and vice versa).
    /// </summary>
    [DataField]
    public List<ProtoId<TraitPrototype>> Excludes { get; private set; } = new();

    /// <summary>
    ///     Claw Command - Department IDs where this trait is forbidden.
    ///     If any of the player's preferred jobs belong to a restricted department, the trait is blocked.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> RestrictedDepts { get; private set; } = new();

    /// <summary>
    ///     Claw Command - Species IDs that are allowed to take this trait.
    ///     If set, only characters of the listed species can see and select this trait.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>> RestrictedSpecies { get; private set; } = new();
}
