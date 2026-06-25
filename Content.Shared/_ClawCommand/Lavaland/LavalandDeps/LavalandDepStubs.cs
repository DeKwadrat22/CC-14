// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Phase 4 (Lavaland megafauna framework) port stubs.
//
// Mirrors the heretic-deps pattern in HereticDeps/GoobStubs.cs: provides
// minimal, inert stand-ins for Goob types referenced by ported framework
// code that have no corresponding fork system yet. These compile but do
// nothing at runtime; the real systems will be re-introduced in later
// phases or as standalone follow-ups if needed.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared._vg.TileMovement
{
    // Goob's tile-by-tile movement add-on. Fork does not have tile-movement.
    // Stub: presence-only marker. Hierophant Beat add/remove ops become no-ops.
    [RegisterComponent, NetworkedComponent]
    public sealed partial class TileMovementComponent : Component
    {
    }
}

namespace Content.Shared._ClawCommand.Lavaland.LavalandDeps
{
    // Inert marker stubs for Goob components referenced by ported lavaland
    // YAML but with no corresponding fork system. The components exist so the
    // prototype loader accepts the YAML; runtime behavior they would have
    // driven is silently absent until a real system is ported.

    // Goob tendril mob spawner — counts triggers, used with TriggerCounterLimit.
    [RegisterComponent]
    public sealed partial class TriggerCounterComponent : Component
    {
    }

    // Cap on TriggerCounter, gates SpawnTableOnTrigger.
    [RegisterComponent]
    public sealed partial class TriggerCounterLimitComponent : Component
    {
        [DataField]
        public int MaxCount = 1;
    }

    // Goob worldgen-radius chunk loader (mining console). Fork lacks worldgen.
    [RegisterComponent]
    public sealed partial class WorldLoaderComponent : Component
    {
        [DataField]
        public float Radius = 256f;
    }

    // _DV.Weather marker — mob immune to ashstorm/etc.
    [RegisterComponent]
    public sealed partial class WeatherImmuneComponent : Component
    {
    }

    // Goob boss music driver (Phase 4 lavaland audio). Inert; SharedBossMusicSystem stub does nothing.
    [RegisterComponent]
    public sealed partial class BossMusicComponent : Component
    {
    }

    // Goob station-events combat power metric. Used by mob YAML for threat scaling.
    [RegisterComponent]
    public sealed partial class CombatPowerComponent : Component
    {
        [DataField]
        public float Power = 0f;
    }

    // Goob trigger spawn-table component (tendril mob spawner). Inert until trigger system is ported.
    [RegisterComponent]
    public sealed partial class SpawnTableOnTriggerComponent : Component
    {
    }

    // Goob crop/decoration harvestable. Inert.
    [RegisterComponent]
    public sealed partial class HarvestableComponent : Component
    {
    }

    // Goob melee dash mechanic (katana). Inert.
    [RegisterComponent]
    public sealed partial class MeleeDashComponent : Component
    {
    }

    // Goob multi-shot gun firing (pistol PKA). Inert.
    [RegisterComponent]
    public sealed partial class MultishotComponent : Component
    {
    }

    // Goob held-grant (Hierophant Club). Inert.
    [RegisterComponent]
    public sealed partial class HeldGrantComponent : Component
    {
    }

    // Goob block-melee delay component (crusher). Inert.
    [RegisterComponent]
    public sealed partial class UseDelayBlockMeleeComponent : Component
    {
    }

    // Goob NPC speech-bark advertise (Colossus boss). Inert.
    [RegisterComponent]
    public sealed partial class AdvertiseComponent : Component
    {
    }

    // Goob crusher upgrade — flips entity on hit. Inert.
    [RegisterComponent]
    public sealed partial class FlipOnHitComponent : Component
    {
    }

    // Goob _NF.Shuttles FTL drive (Lavaland outpost map root). Inert.
    [RegisterComponent]
    public sealed partial class FTLDriveComponent : Component
    {
    }

    // Goob bossMusic prototype kind — minimal stub so YAML loads.
    [Prototype]
    public sealed partial class BossMusicPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
    }

    // _DV.Weather scheduler. Inert; ashfall/scheduling won't fire until a real weather scheduler lands.
    // Fields mirror Goob's `Content.Server._DV.Weather.WeatherSchedulerComponent` so the YAML loads.
    [RegisterComponent]
    public sealed partial class WeatherSchedulerComponent : Component
    {
        [DataField(required: true)]
        public List<LavalandWeatherStage> Stages = new();

        [DataField]
        public int Stage;
    }

    [Serializable, DataDefinition]
    public partial struct LavalandWeatherStage
    {
        [DataField(required: true)]
        public Content.Shared.Destructible.Thresholds.MinMax Duration = new(0, 0);

        [DataField]
        public EntProtoId? Weather;

        [DataField]
        public LocId? Message;
    }
}

namespace Content.Shared._ClawCommand.Lavaland.Audio
{
    // Real boss-music system is part of Phase 6/Audio polish. Stub provides
    // the API surface (StartBossMusic/EndAllMusic) so AggressorsSystem compiles.
    public sealed partial class SharedBossMusicSystem : EntitySystem
    {
        public void StartBossMusic(EntityUid source) { }
        public void EndAllMusic() { }
    }
}

namespace Content.Shared._Goobstation.Weapons.Ranged
{
    // Phase 6 (Lavaland ItemUpgrades / GunUpgrades) port stub.
    // Goob raises this on a gun-style entity to let firerate modifiers tweak recharge cooldown.
    // Fork's RechargeBasicEntityAmmoSystem does not raise it; presence here keeps the
    // subscriptions in SharedGunUpgradesSystem / ItemUpgradesSystem.Relay compiling.
    [ByRefEvent]
    public record struct RechargeBasicEntityAmmoGetCooldownModifiersEvent(float Multiplier);
}

namespace Content.Shared._ClawCommand.Lavaland.LavalandDeps
{
    /// <summary>
    /// Extension methods that re-add Goob's helper APIs on the fork's
    /// <see cref="SharedActionsSystem"/>. The fork keeps TryPerformAction
    /// private and lacks TryGetActionById/CanPerformAction; rather than
    /// touch vanilla code (Rule 5), we add equivalents here.
    /// </summary>
    public static class ActionsSystemLavalandExtensions
    {
        // EntitySystem.EntityManager is protected; resolve via IoC for use inside extension methods.
        private static IEntityManager EntMan => IoCManager.Resolve<IEntityManager>();

        public static bool TryGetActionById(
            this SharedActionsSystem actions,
            EntityUid actionContainer,
            EntProtoId actionId,
            [NotNullWhen(true)] out Entity<ActionComponent>? action)
        {
            action = null;
            var entMan = EntMan;
            foreach (var ent in actions.GetActions(actionContainer))
            {
                if (!entMan.TryGetComponent<MetaDataComponent>(ent.Owner, out var meta)
                    || meta.EntityPrototype?.ID is not { } protoId
                    || protoId != actionId)
                    continue;
                action = ent;
                return true;
            }
            return false;
        }

        public static bool CanPerformAction(
            this SharedActionsSystem actions,
            EntityUid user,
            Entity<ActionComponent> action,
            RequestPerformActionEvent ev)
        {
            // Best-effort check using only the fork's public ValidAction (cooldown + enabled).
            // Full Goob validation pipeline (DoActionRequest, ActionAttempt, ActionValidate) is
            // owned by the actions system internally; without modifying vanilla we approximate it.
            _ = ev;
            _ = user;
            return actions.ValidAction(action);
        }

        public static bool TryPerformAction(
            this SharedActionsSystem actions,
            EntityUid user,
            RequestPerformActionEvent ev)
        {
            var entMan = EntMan;
            var actionEnt = entMan.GetEntity(ev.Action);
            if (!entMan.TryGetComponent<ActionComponent>(actionEnt, out var actionComp))
                return false;

            var action = new Entity<ActionComponent>(actionEnt, actionComp);
            if (!actions.ValidAction(action))
                return false;

            actions.PerformAction(user, action);
            return true;
        }
    }
}
