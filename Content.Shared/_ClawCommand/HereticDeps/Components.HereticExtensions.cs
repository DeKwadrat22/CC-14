// Partial-class extensions to upstream components that Goob's heretic YAML configures
// with fields upstream doesn't have. We add the fields here so the prototypes validate;
// the runtime never reads them (no system polls these new fields), so behavior is just
// "the field is ignored".

using System.Collections.Generic;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

// -- ActionComponent --
namespace Content.Shared.Actions.Components
{
    public sealed partial class ActionComponent
    {
        [DataField] public bool Predicted;
    }
}

// -- BloodstreamComponent --
namespace Content.Shared.Body.Components
{
    public sealed partial class BloodstreamComponent
    {
        [DataField] public ProtoId<ReagentPrototype>? BloodReagent;
        [DataField] public FixedPoint2 BloodMaxVolume = 300f;
    }
}

// -- BodyComponent --
namespace Content.Shared.Body
{
    public sealed partial class BodyComponent
    {
        [DataField] public ProtoId<Robust.Shared.Prototypes.EntityPrototype>? Prototype;
        [DataField] public string? BodyType;
    }
}

// -- TemperatureComponent --
namespace Content.Shared.Temperature.Components
{
    public sealed partial class TemperatureComponent
    {
        [DataField] public DamageSpecifier? HeatDamage;
        [DataField] public DamageSpecifier? ColdDamage;
        [DataField] public float DamageCap;
    }
}

// -- MetabolizerComponent --
namespace Content.Shared.Metabolism
{
    public sealed partial class MetabolizerComponent
    {
        [DataField] public List<HereticMetabolizerGroup>? Groups;
    }

    [DataDefinition]
    public sealed partial class HereticMetabolizerGroup
    {
        [DataField] public ProtoId<Content.Shared.Body.Prototypes.MetabolismGroupPrototype> Id;
        [DataField] public float RateModifier = 1f;
    }
}

// -- MeleeWeaponComponent --
namespace Content.Shared.Weapons.Melee
{
    public sealed partial class MeleeWeaponComponent
    {
        [DataField] public bool CanWideSwing = true;
        [DataField] public bool CanHeavyAttack = true;
        [DataField] public Robust.Shared.Maths.Angle AnimationRotation;
    }
}

// -- ProjectileComponent --
namespace Content.Shared.Projectiles
{
    public sealed partial class ProjectileComponent
    {
        [DataField] public bool Penetrate;
    }
}

// -- DamageSpecifier --
namespace Content.Shared.Damage
{
    public sealed partial class DamageSpecifier
    {
        [DataField] public float ArmorPenetration { get; set; }
        [DataField] public Dictionary<string, FixedPoint2> WoundSeverityMultipliers { get; set; } = new();
    }
}

// -- ToggleableClothingComponent --
namespace Content.Shared.Clothing.Components
{
    public sealed partial class ToggleableClothingComponent
    {
        [DataField] public Dictionary<string, EntProtoId>? ClothingPrototypes;
    }
}

// -- Overlays --
namespace Content.Shared.Overlays
{
    public sealed partial class ShowHealthBarsComponent
    {
        [DataField] public bool WorksInHands;
    }

    public sealed partial class ShowHealthIconsComponent
    {
        [DataField] public bool WorksInHands;
    }
}

// -- StepTriggerComponent --
namespace Content.Shared.StepTrigger.Components
{
    public sealed partial class StepTriggerComponent
    {
        [DataField] public HereticStepTriggerGroups? TriggerGroups;
    }

    [DataDefinition]
    public sealed partial class HereticStepTriggerGroups
    {
        [DataField] public HashSet<string>? Types;
        [DataField] public HashSet<string>? Tags;
    }
}

// -- PreventCollideComponent --
namespace Content.Shared.Physics
{
    public sealed partial class PreventCollideComponent
    {
        [DataField] public EntityWhitelist? Whitelist;
    }
}

// -- SpeedModifierContactsComponent --
namespace Content.Shared.Movement.Components
{
    public sealed partial class SpeedModifierContactsComponent
    {
        [DataField] public EntityWhitelist? Whitelist;
    }
}

// -- ContentTileDefinition --
namespace Content.Shared.Maps
{
    public sealed partial class ContentTileDefinition
    {
        [DataField] public float DeconstructTimeMultiplier = 1f;
    }
}

// -- ProjectileSpellEvent --
namespace Content.Shared.Magic.Events
{
    public sealed partial class ProjectileSpellEvent
    {
        [DataField] public float Speed;
    }
}

// -- PolymorphConfiguration --
namespace Content.Shared.Polymorph
{
    public sealed partial record PolymorphConfiguration
    {
        [DataField] public bool AttachToGridOrMap;
        [DataField] public bool ShowPopup = true;
        [DataField] public bool AllowMovement = true;
        [DataField] public bool SkipRevertConfirmation;
        [DataField] public List<HereticPolymorphComponentEntry>? ComponentsToTransfer;
    }

    [DataDefinition]
    public sealed partial class HereticPolymorphComponentEntry
    {
        [DataField] public string Component = string.Empty;
    }
}

// -- CloningSettingsPrototype --
namespace Content.Shared.Cloning
{
    public sealed partial class CloningSettingsPrototype
    {
        [DataField] public bool CopyStorage;
        [DataField] public bool MakeEquipmentUnremoveable;
        [DataField] public bool InternalContentsUnremoveable;
        [DataField] public bool AllowNonHumanoid;
    }
}
