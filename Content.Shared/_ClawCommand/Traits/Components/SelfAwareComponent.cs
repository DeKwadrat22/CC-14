using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DamageTypePrototype>)), AutoNetworkedField]
    public HashSet<string> AnalyzableTypes = default!;

    /// <summary>
    ///     Damage groups that the entity can detect presence/severity of when examining themselves.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DamageGroupPrototype>)), AutoNetworkedField]
    public HashSet<string> DetectableGroups = default!;

}
