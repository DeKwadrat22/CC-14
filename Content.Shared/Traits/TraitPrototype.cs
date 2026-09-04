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

    /// <summary>
    ///     Claw Command - Species IDs that are forbidden from taking this trait.
    ///     Unlike <see cref="RestrictedSpecies"/> this is a blacklist, and it can be waived by
    ///     <see cref="SpeciesExemptTraits"/>. Added for the psionics port, where IPCs are shut out of psionic
    ///     traits unless they have taken Anomalous Positronics.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>> ForbiddenSpecies { get; private set; } = new();

    /// <summary>
    ///     Claw Command - taking any one of these traits waives <see cref="ForbiddenSpecies"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<TraitPrototype>> SpeciesExemptTraits { get; private set; } = new();

    /// <summary>
    ///     Claw Command - the character must have at least one of these traits, or at least one job from
    ///     <see cref="RequiresAnyJob"/>, before this trait may be taken. Leaving both lists empty means no gate.
    ///     Used by the psionics port so that psionic powers are only offered to Latent Psychics and to the
    ///     handful of jobs that are psionic by default.
    /// </summary>
    [DataField]
    public List<ProtoId<TraitPrototype>> RequiresAnyTrait { get; private set; } = new();

    /// <summary>
    ///     Claw Command - see <see cref="RequiresAnyTrait"/>. A job counts when the character has it set to any
    ///     priority above Never.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> RequiresAnyJob { get; private set; } = new();

    /// <summary>
    ///     Claw Command - jobs that may not take this trait, typically because the job already grants the same
    ///     thing for free. Finer-grained than <see cref="RestrictedDepts"/>, which blocks a whole department.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ForbiddenJobs { get; private set; } = new();
}
