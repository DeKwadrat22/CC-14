using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class TelegnosisPowerComponent : Component
    {
        [DataField("prototype")]
        public string Prototype = "MobObserverTelegnostic";
        // Claw Command - upstream carried a dead `InstantActionComponent? TelegnosisPowerAction` field here.
        // Nothing ever read or assigned it, and InstantActionComponent has since moved to
        // Content.Shared.Actions.Components, so it is dropped rather than given a using for a field that does nothing.
        [ValidatePrototypeId<EntityPrototype>]
        public const string TelegnosisActionPrototype = "ActionTelegnosis";
        [DataField("telegnosisActionId",
        customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? TelegnosisActionId = "ActionTelegnosis";

        [DataField("telegnosisActionEntity")]
        public EntityUid? TelegnosisActionEntity;
    }
}