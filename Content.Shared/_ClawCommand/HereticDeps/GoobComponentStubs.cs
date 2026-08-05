// Stub components for heretic YAML references. Each is registered under its Goob-original
// network name so heretic prototype YAML loads cleanly. None of these have backing systems
// in this fork — the components are markers; gameplay hooks that needed them won't fire.
// Per user direction we do NOT port _Shitmed surgery; all surgery-tool stubs are no-ops.

using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

// --- Goob Enchanting ---
namespace Content.Goobstation.Shared.Enchanting.Components
{
    [RegisterComponent] public sealed partial class CanEnchantComponent : Component;
}

// --- Goob Clothing ---
namespace Content.Goobstation.Shared.Clothing.Components
{
    [RegisterComponent] public sealed partial class MadnessMaskComponent : Component;
}

namespace Content.Goobstation.Client.Clothing.Components
{
    [RegisterComponent]
    public sealed partial class HideClothingLayerClothingComponent : Component
    {
        [DataField] public HashSet<string> HiddenSlots = new();
    }
}

// --- Goob Overlays (NightVision/ThermalVision) ---
// Real implementations live in Content.Shared/_ClawCommand/Overlays/ — stubs removed.

// --- Goob Contraband ---
namespace Content.Goobstation.Shared.Contraband
{
    [RegisterComponent] public sealed partial class HideContrabandContentComponent : Component;
    [RegisterComponent] public sealed partial class UndetectableContrabandComponent : Component;
}

// --- Goob Weapons extensions ---
namespace Content.Goobstation.Shared.Weapons.DelayedKnockdown
{
    [RegisterComponent]
    public sealed partial class ModifyDelayedKnockdownComponent : Component
    {
        [DataField] public bool Cancel;
        [DataField] public float DelayDelta;
        [DataField] public float KnockdownTimeDelta;
    }
}

namespace Content.Goobstation.Shared.Weapons.Multihit
{
    [RegisterComponent]
    public sealed partial class MultihitComponent : Component
    {
        [DataField] public float DamageMultiplier = 0.67f;
        [DataField] public TimeSpan MultihitDelay = TimeSpan.FromSeconds(0.25);
        [DataField] public EntityWhitelist? MultihitWhitelist;
        [DataField] public List<BaseMultihitUserConditionEvent> Conditions = new();
        [DataField] public bool RequireAllConditions;
    }

    [Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
    public abstract partial class BaseMultihitUserConditionEvent : HandledEntityEventArgs
    {
        public EntityUid User = EntityUid.Invalid;
    }

    public sealed partial class MultihitUserWhitelistEvent : BaseMultihitUserConditionEvent
    {
        [DataField(required: true)] public EntityWhitelist Whitelist = default!;
        [DataField] public bool Blacklist;
    }

    public sealed partial class MultihitUserHereticEvent : BaseMultihitUserConditionEvent
    {
        [DataField] public int MinPathStage;
        [DataField] public string? RequiredPath;
    }
}

namespace Content.Goobstation.Shared.Weapons.ThrowableBlocker
{
    [RegisterComponent] public sealed partial class ThrowableBlockedComponent : Component;
}

// --- Goob Body extensions ---
namespace Content.Goobstation.Common.Body.Components
{
    [RegisterComponent] public sealed partial class SpecialBreathingImmunityComponent : Component;
}

// --- Goob Supermatter ---
namespace Content.Goobstation.Shared.Supermatter.Components
{
    [RegisterComponent] public sealed partial class SupermatterImmuneComponent : Component;
}

// --- Goob Flammability ---
namespace Content.Goobstation.Common.Flammability
{
    [RegisterComponent] public sealed partial class FireImmunityComponent : Component;
}

// --- Goob Singularity/Flashbang/Weapons (originally Goobstation.Server) ---
// Heretic YAML references these on shared entities, so we register them as shared markers.
namespace Content.Goobstation.Server.Singularity.EventHorizon
{
    [RegisterComponent] public sealed partial class EventHorizonIgnoreComponent : Component;
}

namespace Content.Goobstation.Server.Flashbang
{
    [RegisterComponent] public sealed partial class FlashSoundSuppressionComponent : Component;
}

namespace Content.Goobstation.Server.Weapons.ChangeTemperatureOnHit
{
    [RegisterComponent]
    public sealed partial class ChangeTemperatureOnHitComponent : Component
    {
        [DataField] public float Heat;
        [DataField] public bool IgnoreResistances = true;
    }
}

namespace Content.Goobstation.Server.ComponentsRegistry
{
    [RegisterComponent]
    public sealed partial class GrantComponentsStatusEffectComponent : Component
    {
        [DataField] public ComponentRegistry Components { get; private set; } = new();
    }
}

// --- _Goobstation.Wizard (Traps / Projectiles / Spellblade / UI / ForceWall / HighFrequencyBlade) ---
namespace Content.Shared._Goobstation.Wizard.Traps
{
    [RegisterComponent]
    public sealed partial class DamageTrapComponent : Component
    {
        [DataField] public DamageSpecifier? Damage;
        [DataField] public EntProtoId? SpawnedEntity;
    }
}

namespace Content.Shared._Goobstation.Wizard.Projectiles
{
    [RegisterComponent] public sealed partial class EntityTrailComponent : Component;

    /// <summary>Stub Trail — referenced by some heretic projectile prototypes; renders a Goob-only trail effect.</summary>
    [RegisterComponent]
    public sealed partial class TrailComponent : Component
    {
        [DataField] public SpriteSpecifier? Sprite;
        [DataField] public float Frequency = 0.2f;
        [DataField] public float Lifetime = 1f;
        [DataField] public float LerpTime = 0.05f;
        [DataField] public float AlphaLerpAmount = 0.3f;
        [DataField] public RenderedEntityRotationStrategy RenderedEntityRotationStrategy;
        [DataField] public Color Color = Color.White;
        [DataField] public float Scale = 1f;
        [DataField] public int ParticleAmount = 1;
        [DataField] public Vector2? SpawnPosition;
    }

    public enum RenderedEntityRotationStrategy : byte
    {
        RenderedEntity = 0,
        Trail,
        Particle,
    }
}

namespace Content.Shared._Goobstation.Wizard.UserInterface
{
    [RegisterComponent]
    public sealed partial class ActivatableUiUserWhitelistComponent : Component
    {
        [DataField] public EntityWhitelist? Whitelist;
        [DataField] public bool CheckMind;
    }
}

namespace Content.Shared._Goobstation.Wizard.Spellblade
{
    [RegisterComponent]
    public sealed partial class SpacetimeSpellbladeEnchantmentComponent : Component
    {
        [DataField] public EntProtoId? Effect;
    }
}

namespace Content.Shared._Goobstation.Wizard.HighFrequencyBlade
{
    [RegisterComponent] public sealed partial class RandomRotationComponent : Component;
}

// --- _Shitmed surgery (deliberately not ported — empty markers only) ---
namespace Content.Shared._Shitmed.Medical.Surgery
{
    [RegisterComponent]
    public sealed partial class SanitizedComponent : Component
    {
        [DataField] public bool WorksInHands;
    }

    [RegisterComponent] public sealed partial class SurgeryIgnoreClothingComponent : Component;
}

namespace Content.Shared._Shitmed.Medical.Surgery.Tools
{
    [RegisterComponent]
    public sealed partial class BoneGelComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class BoneSawComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class BoneSetterComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class CauteryComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class DrillComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class HemostatComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class RetractorComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class ScalpelComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class StitchesComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class SurgeryToolComponent : Component
    {
        [DataField] public bool IgnoreToggle;
        [DataField] public SoundSpecifier? StartSound;
        [DataField] public SoundSpecifier? EndSound;
    }

    [RegisterComponent]
    public sealed partial class TendingComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }

    [RegisterComponent]
    public sealed partial class TweezersComponent : Component
    {
        [DataField] public float Speed { get; set; } = 1f;
    }
}

namespace Content.Shared._Shitmed.Targeting
{
    [RegisterComponent] public sealed partial class TargetingComponent : Component;
}

// --- _White Xenomorphs ---
namespace Content.Server._White.Xenomorphs.FaceHugger
{
    [RegisterComponent] public sealed partial class FaceHuggerBlockerComponent : Component;
}

// --- EinsteinEngines language stack ---
namespace Content.Shared._EinsteinEngines.Language.Components
{
    [RegisterComponent]
    public sealed partial class LanguageSpeakerComponent : Component
    {
        // Einstein Engines' language system sets a starting language on the mob; the stub carries
        // the field so the ported YAML loads even though nothing reads it here.
        [DataField] public string? CurrentLanguage;
    }
    [RegisterComponent] public sealed partial class UniversalLanguageSpeakerComponent : Component;
}

namespace Content.Server._EinsteinEngines.Language
{
    [RegisterComponent]
    public sealed partial class LanguageKnowledgeComponent : Component
    {
        [DataField] public List<string> Speaks = new();
        [DataField] public List<string> Understands = new();
    }

    [RegisterComponent] public sealed partial class TowerOfBabelComponent : Component;
}
