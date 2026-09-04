// Stub definitions for Goob-Station types that heretic references but whose
// implementing systems we are intentionally NOT porting. These stubs let heretic
// compile; the underlying gameplay hooks (martial arts riposte, conversion immunity,
// Goob-extended speech, etc.) will simply never fire because no upstream system
// raises these events.

using Content.Shared.Chat.Prototypes;
using Content.Shared.Magic;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Speech;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

// --- PullingSystem adapter (Goob's StopAllPulls → upstream TryStopPull pair) ---

namespace Content.Shared._ClawCommand.HereticAdapters
{
    public static class PullingSystemHereticExtensions
    {
        /// <summary>
        /// Adapter for Goob's PullingSystem.StopAllPulls(uid). Stops the entity from
        /// being pulled AND from pulling anything, using upstream's TryStopPull pair.
        /// </summary>
        public static void StopAllPulls(this PullingSystem system, EntityUid uid, bool stopPuller = true)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (stopPuller && entMan.TryGetComponent(uid, out PullableComponent? pullableMe))
                system.TryStopPull(uid, pullableMe);
            if (entMan.TryGetComponent(uid, out PullerComponent? puller)
                && puller.Pulling is { } pulledEnt
                && entMan.TryGetComponent(pulledEnt, out PullableComponent? pullableOther))
                system.TryStopPull(pulledEnt, pullableOther);
        }
    }

    public static class StunSystemHereticExtensions
    {
        /// <summary>
        /// Adapter for Goob's SharedStunSystem.KnockdownOrStun. Tries to knock down,
        /// falls back to stun if the entity can't be knocked down.
        /// </summary>
        public static void KnockdownOrStun(this SharedStunSystem system, EntityUid uid, TimeSpan time, bool refresh = true, bool drop = true)
        {
            if (!system.TryKnockdown(uid, time, refresh))
                system.TryUpdateStunDuration(uid, time);
        }
    }

    public static class StaminaSystemHereticExtensions
    {
        /// <summary>
        /// Adapter for Goob's stamina-drain toggle. No-op upstream — heretic Realignment
        /// effect just loses its regen-during-cast feature, but still grants the other
        /// benefits (stand up, exit stamcrit, pacify, etc.).
        /// </summary>
        public static void ToggleStaminaDrain(this SharedStaminaSystem _, EntityUid uid, float rate, bool enabled, bool refresh = false, string? key = null, EntityUid? source = null) { }

        /// <summary>
        /// Adapter for Goob's TakeOvertimeStaminaDamage. Routes to immediate stamina damage.
        /// </summary>
        public static void TakeOvertimeStaminaDamage(this SharedStaminaSystem system, EntityUid uid, float value)
        {
            system.TakeStaminaDamage(uid, value);
        }
    }

    public static class MagicSystemHereticExtensions
    {
        /// <summary>
        /// Adapter for Goob's IsTouchSpellDenied. Upstream has no equivalent; default
        /// "not denied". The Goob hook was used to let antimagic gear block touch spells.
        /// </summary>
        public static bool IsTouchSpellDenied(this SharedMagicSystem _, EntityUid user, EntityUid target = default) => false;
    }

    /// <summary>
    /// Adapter for Goob's SharedChatSystem.UpdateFontSize, which wraps both `message` and
    /// `loc` in a [font size=X]…[/font] tag so heretic flavor text appears large/spooky.
    /// </summary>
    public static class ChatSystemHereticExtensions
    {
        public static void UpdateFontSize(int size, ref string message, ref string loc)
        {
            message = $"[font size={size}]{message}[/font]";
            loc = $"[font size={size}]{loc}[/font]";
        }
    }

    public static class StaminaSystemExitStamCritExtensions
    {
        /// <summary>
        /// Goob has ExitStamCrit; upstream's stamina system clears stamCrit by zeroing damage.
        /// </summary>
        public static void ExitStamCrit(this Content.Shared.Damage.Systems.SharedStaminaSystem system, EntityUid uid, Content.Shared.Damage.Components.StaminaComponent? stam = null)
        {
            system.TakeStaminaDamage(uid, -10000f, stam, visual: false);
        }
    }

    public static class ReflectSystemHereticExtensions
    {
        /// <summary>
        /// Stubs for Goob's reflect-projectile and reflect-hitscan helpers.
        /// Without Goob's reflect API, heretic Protective-Blade reflection is a no-op
        /// (always returns false, no reflection happens).
        /// </summary>
        public static bool TryReflectProjectile(this Content.Shared.Weapons.Reflect.ReflectSystem _, EntityUid user, EntityUid reflector, EntityUid projectile)
            => false;

        public static bool TryReflectHitscan(this Content.Shared.Weapons.Reflect.ReflectSystem _, EntityUid user, EntityUid reflector, EntityUid shooter, EntityUid? sourceItem, System.Numerics.Vector2 direction, out System.Numerics.Vector2 newDirection)
        {
            newDirection = direction;
            return false;
        }
    }

    public static class BodySystemHereticExtensions
    {
        /// <summary>No-op restore: upstream BodySystem has no RestoreBody. Heretic Rust ascension just doesn't regrow parts.</summary>
        public static void RestoreBody(this Content.Shared.Body.BodySystem _, EntityUid uid) { }

        /// <summary>Goob's iteration helper for organs. Upstream BodySystem has GetBodyOrgans; route through it and lift to Entity tuples.</summary>
        public static IEnumerable<(EntityUid Owner, T Comp1, Content.Shared.Body.OrganComponent Comp2)> GetBodyOrganEntityComps<T>(this Content.Shared.Body.BodySystem system, Entity<Content.Shared.Body.BodyComponent?> body) where T : IComponent
        {
            yield break;
        }

        /// <summary>Goob's enum-typed body-part iterator. Upstream BodySystem has GetBodyChildren; just yield nothing for the heretic Flesh path.</summary>
        public static IEnumerable<(EntityUid Owner, IComponent Comp)> GetBodyChildrenOfType(this Content.Shared.Body.BodySystem system, EntityUid uid, object partType)
        {
            yield break;
        }
    }

}

// --- MartialArts ----------------------------------------------------------

namespace Content.Goobstation.Common.MartialArts
{
    public abstract class BaseRiposteCheckEvent : HandledEntityEventArgs
    {
        public EntityUid Attacker;
        public EntityUid Defender;
    }
}

namespace Content.Goobstation.Shared.MartialArts.Components
{
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class MartialArtModifiersComponent : Component
    {
        public List<MartialArtModifierData> Data = new();
        public TimeSpan NextUpdate = TimeSpan.Zero;
        public Dictionary<MartialArtModifierType, System.Numerics.Vector4> MinMaxModifiersMultipliers = new();
    }

    public sealed partial class MartialArtModifierData
    {
        public MartialArtModifierType Type = MartialArtModifierType.AttackRate;
        public float Multiplier = 1f;
        public float Modifier;
        public TimeSpan EndTime = TimeSpan.Zero;
    }

    [Flags]
    public enum MartialArtModifierType : byte
    {
        Invalid = 0,
        AttackRate = 1 << 0,
        Damage = 1 << 1,
        MoveSpeed = 1 << 2,
        Healing = 1 << 3,
        Unarmed = 1 << 4,
        Armed = 1 << 5,
    }
}

// --- Conversion -----------------------------------------------------------

namespace Content.Goobstation.Common.Conversion
{
    [ByRefEvent]
    public record struct BeforeConversionEvent(EntityUid Uid, bool Blocked = false);
}

// --- Grab -----------------------------------------------------------------

namespace Content.Goobstation.Common.Grab
{
    public enum GrabStage : byte
    {
        No = 0,
        Soft = 1,
        Hard = 2,
        Choke = 3,
        Suffocate = 4,
    }
}

// --- Speech ---------------------------------------------------------------

namespace Content.Goobstation.Common.Speech
{
    public sealed class GetBarkSourceEntityEvent : HandledEntityEventArgs
    {
        public EntityUid Ent;
    }

    public sealed class GetSpeechSoundEvent : HandledEntityEventArgs
    {
        public ProtoId<SpeechSoundsPrototype>? SpeechSoundProtoId;
    }

    public sealed class GetEmoteSoundsEvent : HandledEntityEventArgs
    {
        public ProtoId<EmoteSoundsPrototype>? EmoteSoundProtoId;
    }
}

// --- Bible / Religion -----------------------------------------------------

namespace Content.Goobstation.Shared.Bible
{
    // BibleUserComponent + WeakToHolyComponent + UnholyItemComponent live in their own files.
    // No BibleComponent stub here: upstream already has Content.Server.Bible.Components.BibleComponent
    // registered as "Bible", and adding another component with the same name would crash startup
    // with "Bible is already registered". Heretic's chaplain-bible-on-rune feature is now detected
    // via PrayableComponent (shared, upstream) — see CosmicRunesSystem.
    internal sealed class _GoobBibleMarker;
}

namespace Content.Goobstation.Shared.Religion
{
    internal sealed class _ReligionMarker;
}

namespace Content.Goobstation.Common.Religion
{
    // BibleUserComponent removed (conflicted with upstream registration).
    internal sealed class _CommonReligionMarker;
}

namespace Content.Goobstation.Shared.Enchanting.Components
{
    /// <summary>Stub EnchantingTool — heretic items mark themselves as enchantable.
    /// No enchanting system upstream, so this is a marker only.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class EnchantingToolComponent : Component;

    /// <summary>Stub EnchantingTable — heretic ritual runes accept enchanting interactions.
    /// No upstream enchanting; marker only.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class EnchantingTableComponent : Component;
}

namespace Content.Shared._Goobstation.Wizard.ForceWall
{
    /// <summary>Stub SpawnAnimation — heretic polymorph targets play a Goob-specific
    /// spawn animation. Without the system, polymorph still happens but with no animation flair.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class SpawnAnimationComponent : Component
    {
        [DataField] public float AnimationLength;
        [DataField] public bool Spawned;
    }

    [Serializable, Robust.Shared.Serialization.NetSerializable]
    public enum SpawnAnimationVisuals : byte
    {
        Spawned,
    }
}

// --- SecondSkin -----------------------------------------------------------

namespace Content.Goobstation.Common.SecondSkin
{
    // ComplexJointVisualsComponent already ported in HereticDeps. Marker namespace.
}

namespace Content.Goobstation.Shared.SecondSkin
{
    // marker namespace
}

// --- Physics --------------------------------------------------------------

namespace Content.Goobstation.Common.Physics
{
    // marker namespace
}

// --- Bloodstream ----------------------------------------------------------

namespace Content.Goobstation.Common.Bloodstream
{
    [ByRefEvent]
    public record struct GetBloodlossDamageMultiplierEvent(float Multiplier = 1f);
}

// --- White BackStab -------------------------------------------------------

namespace Content.Shared._White.BackStab
{
    /// <summary>
    /// Stub: heretic Mansus Grasp uses TryBackstab to detect attacking-from-behind.
    /// Without the _White system we always return false (no backstab bonus).
    /// </summary>
    public sealed class BackStabSystem : EntitySystem
    {
        public bool TryBackstab(EntityUid target, EntityUid attacker, Angle facingTolerance) => false;
    }
}

// --- _Goobstation.Wizard (Traps, TimeStop, SanguineStrike) ---------------

namespace Content.Shared._Goobstation.Wizard.Traps
{
    public sealed class TrapTriggeredEvent : EntityEventArgs
    {
        public EntityUid User;
        public EntityUid Victim;
    }

    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class WizardTrapComponent : Component
    {
        public HashSet<EntityUid> IgnoredMinds = new();
        [DataField] public Content.Shared.Whitelist.EntityWhitelist? TargetedEntityWhitelist;
        [DataField] public Content.Shared.Whitelist.EntityWhitelist? IgnoredEntityWhitelist;
        [DataField] public TimeSpan TimeBetweenTriggers = TimeSpan.FromSeconds(5);
        [DataField] public int Charges = 1;
        [DataField] public Robust.Shared.Prototypes.EntProtoId? Effect;
        [DataField] public Robust.Shared.Audio.SoundSpecifier? TriggerSound;
        [DataField] public bool CanReveal = true;
        [DataField] public bool Silent;
        [DataField] public TimeSpan StunTime = TimeSpan.FromSeconds(2);
        [DataField] public bool Sparks = true;
        [DataField] public Content.Shared.Damage.DamageSpecifier? Damage;
    }

    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class HomingProjectileComponent : Component
    {
        [DataField] public EntityUid? Target;
        [DataField] public float HomingSpeed = 5f;
    }
}

namespace Content.Goobstation.Maths.FixedPoint
{
    internal sealed class _MathsMarker;
}

namespace Content.Goobstation.Maths.Vectors
{
    /// <summary>
    /// Stub for Goob's GoobVector3. Heretic uses it for some path tile-based math.
    /// </summary>
    [Serializable, Robust.Shared.Serialization.NetSerializable]
    public struct GoobVector3
    {
        public float X;
        public float Y;
        public float Z;
        public GoobVector3(float x = 0f, float y = 0f, float z = 0f) { X = x; Y = y; Z = z; }
        public GoobVector3(System.Numerics.Vector2 v, float z = 0f) { X = v.X; Y = v.Y; Z = z; }
        public static float CalculateAngle(GoobVector3 a, GoobVector3 b)
        {
            // Stub: heretic uses this for inter-mob triangulation. Without Goob's full
            // implementation, return 0f so the trig later produces a no-op distance.
            return 0f;
        }
    }
}

namespace Content.Shared._Goobstation.Wizard.TimeStop
{
    // marker
}

namespace Content.Shared._Goobstation.Wizard.SanguineStrike
{
    public abstract class SharedSanguineStrikeSystem : EntitySystem
    {
        /// <summary>
        /// Stub: Heretic Blade calls this on melee hit to drain target's blood into
        /// the wielder. Without the Goob wizard system, this is a no-op.
        /// Signature matches Goob's: (uid, amount, damageable?, consciousness?).
        /// </summary>
        public void LifeSteal(EntityUid uid, Content.Shared.FixedPoint.FixedPoint2 amount, DamageableComponent? damageable = null, IComponent? consciousness = null) { }
    }
}

// --- Shitmed DoAfter (we said no surgery; this event is stubbed) ---------

namespace Content.Shared._Shitmed.DoAfter
{
    [ByRefEvent]
    public record struct GetDoAfterDelayMultiplierEvent(float Multiplier = 1f);
}

// --- Temperature (Goob added an extra event to upstream Temperature.Events) ---

namespace Content.Shared.Temperature
{
    public sealed class TemperatureChangeAttemptEvent : CancellableEntityEventArgs
    {
        public float LastTemperature;
        public float CurrentTemperature;
    }
}

// --- Body.Prototypes (Goob adds MetabolismGroupPrototype) ----------------

namespace Content.Shared.Body.Prototypes
{
    [Prototype]
    public sealed partial class MetabolismGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
    }
}

// --- More stubs for missing Goob/Wizard/Other-fork types ----------------

namespace Content.Shared.Teleportation
{
    [ByRefEvent]
    public record struct TeleportAttemptEvent(bool Cancelled = false);
}

namespace Content.Goobstation.Common.MartialArts
{
    public sealed class LoadLastAttacksEvent(EntityUid uid = default) : EntityEventArgs { public EntityUid Uid = uid; }
    public sealed class SaveLastAttacksEvent(EntityUid uid = default) : EntityEventArgs { public EntityUid Uid = uid; }
    public sealed class ResetLastAttacksEvent(EntityUid uid = default) : EntityEventArgs { public EntityUid Uid = uid; }
}

namespace Content.Goobstation.Common.Speech
{
    [ByRefEvent]
    public record struct ModifyDisgustEvent(float Multiplier = 1f);
}

namespace Content.Shared._Goobstation.Wizard.Apprentice
{
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class ApprenticeComponent : Component;
}

namespace Content.Shared._Goobstation.Wizard
{
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class WizardComponent : Component;
}

namespace Content.Shared._Goobstation.Wizard.IceCube
{
    /// <summary>Inherits heretic's BaseSpriteOverlayComponent so the client SpriteOverlaySystem can use it.
    /// Key/Sprite are heretic-internal enums; a stub IceCubeOverlayKey is provided.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class IceCubeComponent : Content.Shared._Shitcode.Heretic.SpriteOverlay.BaseSpriteOverlayComponent
    {
        public override Enum Key { get; set; } = IceCubeOverlayKey.IceCube;
        public override Robust.Shared.Utility.SpriteSpecifier? Sprite { get; set; }
    }

    public enum IceCubeOverlayKey { IceCube }

    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class FrozenComponent : Component;
}

namespace Content.Shared._EinsteinEngines.Silicon.Components
{
    // No SiliconComponent stub here: upstream already has Content.Shared.Silicon.Components.SiliconComponent
    // registered as "Silicon". Adding another with the same name crashes startup.
    internal sealed class _SiliconStubMarker;
}

namespace Content.Shared._Starlight.CollectiveMind
{
    [Prototype]
    public sealed partial class CollectiveMindPrototype : IPrototype
    {
        [IdDataField] public string ID { get; private set; } = default!;
    }

    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField]
        public HashSet<ProtoId<CollectiveMindPrototype>> Channels = new();

        [DataField]
        public ProtoId<CollectiveMindPrototype>? DefaultChannel;
    }
}

namespace Content.Shared.Gibbing.Events
{
    // Stub: heretic uses this namespace as a using; types referenced by name (if any)
    // need explicit stubs. Most call sites are in server-side ghoul gib effects.
    public enum GibType { Gib, Skip, Drop }
    public enum GibContentsOption { Skip, Drop, Gib }
}

namespace Content.Shared.Body.Part
{
    /// <summary>Stub BodyPartComponent — upstream doesn't have a per-body-part component
    /// for the things heretic needs (limb tracking). Heretic features that reference it
    /// see this empty marker.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class BodyPartComponent : Component;

    /// <summary>Stub BodyPartType enum for heretic's blade-ascend code; upstream lacks the type.</summary>
    [Flags]
    public enum BodyPartType
    {
        Other = 0,
        Torso = 1 << 0,
        Head = 1 << 1,
        Arm = 1 << 2,
        Hand = 1 << 3,
        Leg = 1 << 4,
        Foot = 1 << 5,
        Chest = 1 << 6,
        Groin = 1 << 7,
        Tail = 1 << 8,
    }
}

namespace Content.Server.Roles
{
    /// <summary>Stub HereticRoleComponent — used as a mind-role component for heretic-tier
    /// briefings/objectives. Matches Goob's location (Content.Server.Roles).</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class HereticRoleComponent : Content.Shared.Roles.Components.BaseMindRoleComponent;
}

namespace Content.Shared.Damage.Components
{
    /// <summary>Stub DamageOverTimeComponent — Goob's per-tick damage applicator. With no
    /// upstream tick system raising damage from this component, heretic-applied DoTs
    /// don't actually fire, but the configuration call sites compile.</summary>
    [Robust.Shared.GameObjects.RegisterComponent]
    public sealed partial class DamageOverTimeComponent : Component
    {
        [DataField] public Content.Shared.Damage.DamageSpecifier? Damage;
        [DataField] public float MultiplierIncrease;
        [DataField] public bool IgnoreResistances;
    }
}

namespace Content.Goobstation.Shared.Overlays
{
    internal sealed class _OverlaysMarker;
}

namespace Content.Goobstation.Shared.Clothing.Components
{
    internal sealed class _ClothingMarker;
}

namespace Content.Goobstation.Shared.Teleportation.Components
{
    internal sealed class _TeleportationMarker;
}

namespace Content.Server.IdentityManagement
{
    internal sealed class _IdentityManagementMarker;
}

namespace Content.Server._Goobstation.Objectives.Components
{
    internal sealed class _ObjectivesMarker;
}

namespace Content.Shared.Magic.Events
{
    public sealed class BeforeCastTouchSpellEvent(EntityUid target, bool doEffects = true) : CancellableEntityEventArgs
    {
        public EntityUid Target = target;
        public bool DoEffects = doEffects;
    }
}

namespace Content.Goobstation.Common.Targeting
{
    public sealed class StopTargetingEvent : EntityEventArgs;
}

// --- Weapons.Melee BeforeHarmfulActionEvent (Goob extension) -------------

namespace Content.Shared.Weapons.Melee.Events
{
    public sealed class BeforeHarmfulActionEvent(EntityUid user, HarmfulActionType type) : CancellableEntityEventArgs
    {
        public EntityUid User { get; } = user;
        public HarmfulActionType Type { get; } = type;
    }

    public enum HarmfulActionType : byte
    {
        Harm,
        Disarm,
        Grab,
        MansusGrasp,
    }
}
