using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Allows the entity to see precise damage values when examining themselves.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SelfAwareComponent : Component
{
    /// <summary>
    ///     Damage types that the entity can see exact values for when examining themselves.
    /// </summary>
    /// <remarks>
    ///     Claw Command - must never be null. The component is also added by copy (cloning's
    ///     TraitsMental list) rather than from YAML, and a bare add leaves datafields unset. The
    ///     generated AutoNetworkedField state handler dereferences these directly, so a null here
    ///     is a client-side NullReferenceException in OnHandleState, not a quiet no-op.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public HashSet<string> AnalyzableTypes = new();

    /// <summary>
    ///     Damage groups that the entity can detect presence/severity of when examining themselves.
    /// </summary>
    /// <remarks>
    ///     Claw Command - must never be null, see <see cref="AnalyzableTypes"/>.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public HashSet<string> DetectableGroups = new();

}
