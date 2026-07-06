using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ClawCommand.Feroxi;

[RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class FeroxiComponent : Component
{
    [DataField]
    public bool IsSuffocating;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Asphyxiation", 2.0 },
        },
    };

    /// <summary>
    /// The type of gas this lung needs. Used only for the breathing alerts, not actual metabolism.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "LowOxygen";

    /// <summary>
    /// The time when the hunger will update next.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The time between each update.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);
}
