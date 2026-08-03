using Content.Shared._ClawCommand.Traits.Components;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
///     Grab intent, ported from the space fork (Goobstation lineage).
///
///     Pressing the pull key on somebody you are already pulling, while in combat mode, walks one step up the
///     ladder: pull -> soft grab -> hard grab -> choke. Each step slows the puller further, makes the victim
///     harder to break free, and choking additionally costs a second hand, mutes the victim and stops them
///     breathing. Pressing it with combat mode off lets go.
///
///     Adapted for this fork: Goobstation's martial-arts grab overrides, combo events and table slam are not
///     ported (none of those systems exist here), throwing a grabbed victim is not included, and ContestsSystem
///     is replaced by the local mass helper at the bottom of this file.
/// </summary>
public sealed partial class PullingSystem
{
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly SoundPathSpecifier GrabSound = new("/Audio/Effects/thudswoosh.ogg");

    /// <summary>
    ///     Mirrors contests.max_percentage from the fork this was ported from.
    /// </summary>
    private const float MassContestMaxPercentage = 0.25f;

    /// <summary>
    ///     Set while we are deliberately adding/removing the grab's own virtual items, so that
    ///     OnVirtualItemDeleted doesn't mistake a de-escalation for the player dropping the pull.
    /// </summary>
    private bool _updatingGrabVirtualItems;

    private void InitializeGrab()
    {
        SubscribeLocalEvent<PullableComponent, UpdateCanMoveEvent>(OnGrabbedMoveAttempt);
        SubscribeLocalEvent<PullableComponent, SpeakAttemptEvent>(OnGrabbedSpeakAttempt);
    }

    /// <summary>
    ///     Being grabbed at all roots you in place. Plain pulling (GrabStage.No) still lets you walk.
    /// </summary>
    private void OnGrabbedMoveAttempt(EntityUid uid, PullableComponent component, UpdateCanMoveEvent args)
    {
        if (component.GrabStage == GrabStage.No)
            return;

        args.Cancel();
    }

    private void OnGrabbedSpeakAttempt(EntityUid uid, PullableComponent component, SpeakAttemptEvent args)
    {
        if (component.GrabStage != GrabStage.Suffocate)
            return;

        _popup.PopupEntity(Loc.GetString("popup-grabbed-cant-speak"), uid, uid, PopupType.MediumCaution);
        args.Cancel();
    }

    /// <summary>
    ///     Escalate the grab on somebody already being pulled.
    /// </summary>
    /// <param name="pullable">The target being grabbed.</param>
    /// <param name="puller">The grabber.</param>
    /// <param name="ignoreCombatMode">Escalate even with combat mode off.</param>
    public bool TryGrab(Entity<PullableComponent?> pullable, Entity<PullerComponent?> puller, bool ignoreCombatMode = false)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp, false)
            || !Resolve(puller.Owner, ref puller.Comp, false))
            return false;

        // You can't grab anyone while somebody has hold of you.
        if (TryComp(puller.Owner, out PullableComponent? pullerAsPullable) && pullerAsPullable.Puller != null)
            return false;

        if (HasComp<PacifiedComponent>(puller.Owner)
            || pullable.Comp.Puller != puller.Owner
            || puller.Comp.Pulling != pullable.Owner)
            return false;

        // Still on cooldown - swallow the input rather than falling through to "let go".
        if (puller.Comp.NextStageChange > _timing.CurTime)
            return true;

        // You can't choke a crate.
        if (!HasComp<MobStateComponent>(pullable.Owner))
            return false;

        puller.Comp.NextStageChange = _timing.CurTime + puller.Comp.StageChangeCooldown;
        Dirty(puller.Owner, puller.Comp);

        if (!ignoreCombatMode && !_combatMode.IsInCombatMode(puller.Owner))
            return false;

        // Already choking - squeezing again just hurts them instead of escalating past the top of the ladder.
        if (puller.Comp.GrabStage == GrabStage.Suffocate)
        {
            _stamina.TakeStaminaDamage(pullable.Owner, puller.Comp.SuffocateGrabStaminaDamage);
            Dirty(pullable.Owner, pullable.Comp);
            return true;
        }

        var step = puller.Comp.GrabStageDirection switch
        {
            GrabStageDirection.Increase => 1,
            GrabStageDirection.Decrease => -1,
            _ => 1,
        };

        var newStage = puller.Comp.GrabStage + step;

        // Claw Command - dangerous hostile mobs cap at Soft, so combat grabs never escalate on them.
        // Swallow the input instead of returning false: false falls through to "let go", which would
        // make trying to combat-grab a carp drop the pull entirely.
        // The Wrestler trait lifts the cap entirely - see WrestlerComponent. It is checked on the
        // puller, so the exemption travels with the person rather than being baked into each mob,
        // and any hostile mob added later is covered without touching this code again.
        if (step > 0 && newStage > pullable.Comp.MaxGrabStage && !HasComp<WrestlerComponent>(puller.Owner))
        {
            _popup.PopupClient(Loc.GetString("popup-grab-too-dangerous", ("target", pullable.Owner)),
                pullable.Owner,
                puller.Owner,
                PopupType.SmallCaution);
            return true;
        }

        if (!TrySetGrabStages((puller.Owner, puller.Comp), (pullable.Owner, pullable.Comp), newStage))
            return false;

        _color.RaiseEffect(Color.Yellow,
            new List<EntityUid> { pullable.Owner },
            Filter.Pvs(pullable.Owner, entityManager: EntityManager));

        return true;
    }

    /// <summary>
    ///     Step the grab back down a rung, or let go entirely.
    /// </summary>
    /// <param name="ignoreCombatMode">If true, will NOT release the target just because combat mode is on.</param>
    public bool TryLowerGrabStage(Entity<PullableComponent?> pullable, Entity<PullerComponent?> puller, bool ignoreCombatMode = false)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp, false)
            || !Resolve(puller.Owner, ref puller.Comp, false))
            return false;

        if (pullable.Comp.Puller != puller.Owner || puller.Comp.Pulling != pullable.Owner)
            return false;

        if (_timing.CurTime < puller.Comp.NextStageChange)
            return true;

        pullable.Comp.NextEscapeAttempt = _timing.CurTime + TimeSpan.FromSeconds(1f);
        Dirty(pullable.Owner, pullable.Comp);
        Dirty(puller.Owner, puller.Comp);

        if (!ignoreCombatMode && _combatMode.IsInCombatMode(puller.Owner)
            || puller.Comp.GrabStage == GrabStage.No)
        {
            TryStopPull(pullable.Owner, pullable.Comp, ignoreGrab: true);
            return true;
        }

        TrySetGrabStages((puller.Owner, puller.Comp), (pullable.Owner, pullable.Comp), puller.Comp.GrabStage - 1);
        return true;
    }

    private bool TrySetGrabStages(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, GrabStage stage)
    {
        var previous = puller.Comp.GrabStage;

        puller.Comp.GrabStage = stage;
        pullable.Comp.GrabStage = stage;

        if (!TryUpdateGrabVirtualItems(puller, pullable))
        {
            // Not enough hands - roll the whole thing back so we don't leave a half-applied stage.
            puller.Comp.GrabStage = previous;
            pullable.Comp.GrabStage = previous;
            return false;
        }

        // Heavier grabbers are harder to escape.
        var massModifier = MassContest(puller.Owner, pullable.Owner);
        pullable.Comp.GrabEscapeChance = Math.Clamp(puller.Comp.EscapeChances[stage] / massModifier, 0f, 1f);

        _alertsSystem.ShowAlert(puller.Owner, puller.Comp.PullingAlert, puller.Comp.PullingAlertSeverity[stage]);
        _alertsSystem.ShowAlert(pullable.Owner, pullable.Comp.PulledAlert, pullable.Comp.PulledAlertSeverity[stage]);

        _blocker.UpdateCanMove(pullable.Owner);
        _modifierSystem.RefreshMovementSpeedModifiers(puller.Owner);

        Dirty(pullable.Owner, pullable.Comp);
        Dirty(puller.Owner, puller.Comp);

        if (!_netManager.IsServer)
            return true;

        var popupType = stage switch
        {
            GrabStage.Hard => PopupType.MediumCaution,
            GrabStage.Suffocate => PopupType.LargeCaution,
            _ => PopupType.Small,
        };

        var key = stage.ToString().ToLowerInvariant();
        var others = Filter.Empty()
            .AddPlayersByPvs(Transform(puller.Owner).Coordinates)
            .RemovePlayerByAttachedEntity(puller.Owner)
            .RemovePlayerByAttachedEntity(pullable.Owner);

        _popup.PopupEntity(Loc.GetString($"popup-grab-{key}-target",
            ("puller", Identity.Entity(puller.Owner, EntityManager))), pullable.Owner, pullable.Owner, popupType);
        _popup.PopupEntity(Loc.GetString($"popup-grab-{key}-self",
            ("target", Identity.Entity(pullable.Owner, EntityManager))), pullable.Owner, puller.Owner, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString($"popup-grab-{key}-others",
            ("target", Identity.Entity(pullable.Owner, EntityManager)),
            ("puller", Identity.Entity(puller.Owner, EntityManager))), pullable.Owner, others, true, popupType);

        _audio.PlayPvs(GrabSound, pullable.Owner);

        return true;
    }

    /// <summary>
    ///     Keeps the puller's occupied hands in step with the grab stage. Choking needs a second hand free.
    /// </summary>
    private bool TryUpdateGrabVirtualItems(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        var current = puller.Comp.GrabVirtualItems.Count;

        var wanted = 0;
        if (puller.Comp.GrabVirtualItemStageCount.TryGetValue(puller.Comp.GrabStage, out var extra))
            wanted += extra;

        if (current == wanted)
            return true;

        _updatingGrabVirtualItems = true;
        try
        {
            for (var i = current; i < wanted; i++)
            {
                if (!_handsSystem.TryGetEmptyHand(puller.Owner, out _)
                    || !_virtual.TrySpawnVirtualItemInHand(pullable.Owner, puller.Owner, out var item, true))
                {
                    if (_netManager.IsServer)
                        _popup.PopupEntity(Loc.GetString("popup-grab-need-hand"), puller.Owner, puller.Owner, PopupType.Medium);

                    return false;
                }

                puller.Comp.GrabVirtualItems.Add(item.Value);
            }

            for (var i = current; i > wanted; i--)
            {
                var last = puller.Comp.GrabVirtualItems.Count - 1;
                if (last < 0)
                    break;

                var item = puller.Comp.GrabVirtualItems[last];
                puller.Comp.GrabVirtualItems.RemoveAt(last);
                QueueDel(item);
            }
        }
        finally
        {
            _updatingGrabVirtualItems = false;
        }

        return true;
    }

    /// <summary>
    ///     Roll to break free. Failing puts the victim on a short cooldown, so mashing the key doesn't help.
    /// </summary>
    private bool AttemptGrabRelease(Entity<PullableComponent?> pullable)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp, false))
            return false;

        if (pullable.Comp.GrabStage == GrabStage.No)
            return true;

        if (_timing.CurTime < pullable.Comp.NextEscapeAttempt)
            return false;

        if (_random.Prob(pullable.Comp.GrabEscapeChance))
            return true;

        pullable.Comp.NextEscapeAttempt = _timing.CurTime + TimeSpan.FromSeconds(3);
        Dirty(pullable.Owner, pullable.Comp);
        return false;
    }

    /// <summary>
    ///     Wipes grab state. Called when a pull ends for any reason.
    /// </summary>
    private void ClearGrabState(PullableComponent pullable, PullerComponent? puller)
    {
        pullable.GrabStage = GrabStage.No;
        pullable.GrabEscapeChance = 1f;

        if (puller == null)
            return;

        puller.GrabStage = GrabStage.No;

        _updatingGrabVirtualItems = true;
        try
        {
            foreach (var item in puller.GrabVirtualItems)
                QueueDel(item);
        }
        finally
        {
            _updatingGrabVirtualItems = false;
        }

        puller.GrabVirtualItems.Clear();
    }

    /// <summary>
    ///     Extra slowdown on top of the base pulling penalty, by stage.
    /// </summary>
    private float GetGrabSpeedModifier(PullerComponent component)
    {
        return component.GrabStage switch
        {
            GrabStage.Soft => component.SoftGrabSpeedModifier,
            GrabStage.Hard => component.HardGrabSpeedModifier,
            GrabStage.Suffocate => component.ChokeGrabSpeedModifier,
            _ => 1f,
        };
    }

    /// <summary>
    ///     This fork has no ContestsSystem, so the one contest grabbing uses is reimplemented here with the
    ///     upstream defaults (contests.max_percentage 0.25, clamp override on).
    /// </summary>
    private float MassContest(EntityUid performer, EntityUid target)
    {
        if (!TryComp<PhysicsComponent>(performer, out var performerPhysics)
            || !TryComp<PhysicsComponent>(target, out var targetPhysics)
            || performerPhysics.Mass == 0
            || targetPhysics.InvMass == 0)
            return 1f;

        var ratio = Math.Clamp(performerPhysics.Mass * targetPhysics.InvMass,
            1 - MassContestMaxPercentage,
            1 + MassContestMaxPercentage);

        return Math.Clamp(ratio, float.Epsilon, float.MaxValue);
    }
}
