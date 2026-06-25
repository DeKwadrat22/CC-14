using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Dictates what species and age this character "looks like"
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem))]
public sealed partial class HumanoidProfileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender Gender;

    [DataField, AutoNetworkedField]
    public Sex Sex;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidCharacterProfile.DefaultSpecies;

    /// <summary>
    /// Claw Command
    ///     Custom species name that overrides the default species name in examine text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string CustomSpeciesName = string.Empty;

    /// <summary>
    /// Claw Command
    ///     The height of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Height = 1f;

    /// <summary>
    /// Claw Command
    ///     The width of this humanoid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Width = 1f;
}
