// claw command - IPC
using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicon.WeldingHealing;

[RegisterComponent]
public sealed partial class WeldingHealingComponent : Component
{
    /// <summary>
    ///     All the damage to change information is stored in this <see cref="DamageSpecifier"/>.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    /// <summary>
    ///     The fuel amount needed to repair physical related damage
    /// </summary>
    [DataField]
    public int FuelCost = 5;

    [DataField]
    public int DoAfterDelay = 3;

    /// <summary>
    ///     A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField]
    public float SelfHealPenalty = 3f;

    /// <summary>
    ///     Whether or not an entity is allowed to repair itself.
    /// </summary>
    [DataField]
    public bool AllowSelfHeal = true;

    [DataField(required: true)]
    public List<string> DamageContainers;
}
