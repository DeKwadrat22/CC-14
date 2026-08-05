using Content.Shared._ClawCommand.Grab;
using Content.Shared.Actions.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
///     Slamming, the follow-up to a hard grab. Ported from Goob-Station's TableSlamSystem and widened
///     past tables: punching any solid anchored thing while you have somebody in a hard grab drives them
///     into it, whether that is a table you put them through or a wall you introduce them to.
///
///     Adapted for this fork: Goobstation keeps its grab state on its own GrabIntent/Grabbable components
///     and reaches for ContestsSystem and _Shitmed body targeting, none of which exist here. The tuning
///     lives on PullerComponent/PullableComponent alongside the rest of the grab port instead, this is a
///     partial of PullingSystem so it can actually write those (they are [Access]-locked to it), and the
///     mass roll reuses the local MassContest helper in PullingSystem.Grab.cs.
/// </summary>
public sealed partial class PullingSystem
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;

    private static readonly SoundCollectionSpecifier SlamSound = new("MetalThud");

    private void InitializeSlam()
    {
        SubscribeLocalEvent<PullerComponent, MeleeHitEvent>(OnSlamMeleeHit);
        SubscribeLocalEvent<PullableComponent, StartCollideEvent>(OnSlammedCollide);
        SubscribeLocalEvent<PullableComponent, LandEvent>(OnSlammedLand);
        SubscribeLocalEvent<SlammedComponent, DisarmAttemptEvent>(OnSlammedDisarmAttempt);
    }

    /// <summary>
    ///     Expire the post-slam daze. Server only - the component is networked, so letting the client
    ///     remove it just gets it put straight back by the next state.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_netManager.IsServer)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SlammedComponent>();
        while (query.MoveNext(out var uid, out var slammed))
        {
            if (now >= slammed.Until)
                RemCompDeferred<SlammedComponent>(uid);
        }
    }

    /// <summary>
    ///     An unarmed swing at a table, wall or locker while you have somebody in a hard grab slams them
    ///     into it instead of hitting it.
    /// </summary>
    private void OnSlamMeleeHit(Entity<PullerComponent> ent, ref MeleeHitEvent args)
    {
        // A wide swing sweeps an arc and can catch several things - only a deliberate click on one
        // specific thing reads as lining somebody up with it.
        if (args.Direction != null || args.HitEntities.Count != 1)
            return;

        if (ent.Comp.GrabStage < ent.Comp.SlamRequiredStage
            || ent.Comp.Pulling is not { } victim
            || _timing.CurTime < ent.Comp.NextStageChange)
            return;

        var surface = args.HitEntities[0];
        if (surface == victim || !IsSlammable(surface, out var hardSurface))
            return;

        // The swing was spent lining them up, so it doesn't also land as a punch on the furniture -
        // this cancels the melee damage and hit sound that would otherwise follow. Set on both sides
        // so the client doesn't predict a punch the server is never going to send.
        args.Handled = true;

        // The slam itself is server-only: it turns on a mass roll and ends in a throw, and a client
        // that rolls differently would yank the victim somewhere the server never sent them.
        if (!_netManager.IsServer)
            return;

        TrySlam(victim, (ent.Owner, ent.Comp), surface, hardSurface);
    }

    /// <summary>
    ///     Throw a grabbed victim into something solid.
    /// </summary>
    /// <param name="pullable">The victim, who must currently be hard-grabbed by <paramref name="puller"/>.</param>
    /// <param name="puller">The grabber.</param>
    /// <param name="surface">What they are going into.</param>
    /// <param name="hardSurface">
    ///     False for a table, which they land on top of; true for a wall or locker, which they get driven into.
    /// </param>
    public bool TrySlam(Entity<PullableComponent?> pullable, Entity<PullerComponent?> puller, EntityUid surface, bool hardSurface)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp, false)
            || !Resolve(puller.Owner, ref puller.Comp, false))
            return false;

        if (pullable.Comp.Puller != puller.Owner || puller.Comp.Pulling != pullable.Owner)
            return false;

        // You have to have actually walked them over to the thing first.
        if (!_xformSystem.InRange(pullable.Owner.ToCoordinates(), surface.ToCoordinates(), puller.Comp.SlamRange))
            return false;

        // The cooldown is spent either way, so a failed slam is a real opening rather than something
        // you can just mash through.
        puller.Comp.NextStageChange = _timing.CurTime + puller.Comp.SlamCooldown;
        Dirty(puller.Owner, puller.Comp);

        // Unclamped on purpose: picking somebody heavier than you up off the floor should be able to fail.
        if (!_random.Prob(Math.Clamp(MassContest(puller.Owner, pullable.Owner, bypassClamp: true), 0f, 1f)))
        {
            _popup.PopupEntity(Loc.GetString("popup-slam-failed",
                    ("target", Identity.Entity(pullable.Owner, EntityManager))),
                puller.Owner,
                puller.Owner,
                PopupType.SmallCaution);

            return false;
        }

        // Deliberately NOT knocking them down first, which is what Goobstation does here: going prone
        // strips MidImpassable from the victim's collision mask (StandingStateSystem.StandingCollisionLayer),
        // and MidImpassable is the entire CollisionGroup.TableLayer - a prone victim sails straight through
        // the table and the slam never registers. They get knocked down on impact instead.
        TryStopPull(pullable.Owner, pullable.Comp, puller.Owner, ignoreGrab: true);
        _throwing.TryThrow(pullable.Owner,
            surface.ToCoordinates(),
            pullable.Comp.SlamThrowSpeed,
            animated: false,
            doSpin: false);

        // Read back out by OnSlammedCollide when they land, which is where the damage happens.
        pullable.Comp.BeingSlammed = true;
        pullable.Comp.SlamHardSurface = hardSurface;
        Dirty(pullable.Owner, pullable.Comp);

        var key = hardSurface ? "object" : "table";
        var others = Filter.Empty()
            .AddPlayersByPvs(Transform(pullable.Owner).Coordinates)
            .RemovePlayerByAttachedEntity(puller.Owner)
            .RemovePlayerByAttachedEntity(pullable.Owner);

        _popup.PopupEntity(Loc.GetString($"popup-slam-{key}-self",
                ("target", Identity.Entity(pullable.Owner, EntityManager)),
                ("surface", surface)),
            pullable.Owner, puller.Owner, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString($"popup-slam-{key}-target",
                ("puller", Identity.Entity(puller.Owner, EntityManager)),
                ("surface", surface)),
            pullable.Owner, pullable.Owner, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString($"popup-slam-{key}-others",
                ("puller", Identity.Entity(puller.Owner, EntityManager)),
                ("target", Identity.Entity(pullable.Owner, EntityManager)),
                ("surface", surface)),
            pullable.Owner, others, true, PopupType.MediumCaution);

        return true;
    }

    /// <summary>
    ///     The landing. Whatever they hit first while mid-slam is what they get hurt on.
    /// </summary>
    private void OnSlammedCollide(Entity<PullableComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.BeingSlammed || !_netManager.IsServer)
            return;

        // Thrown bodies clip plenty of things on the way; only something solid ends the slam.
        if (!IsSlammable(args.OtherEntity, out _))
            return;

        var hardSurface = ent.Comp.SlamHardSurface;

        ent.Comp.BeingSlammed = false;
        Dirty(ent.Owner, ent.Comp);

        var knockdown = ent.Comp.SlamKnockdownDuration;

        if (TryComp<GlassTableComponent>(args.OtherEntity, out var glass))
        {
            // A glass table gives way rather than stopping you, so the table eats it and you land in
            // the wreckage - same numbers the table already uses for someone climbing onto it.
            _damageable.TryChangeDamage(args.OtherEntity, glass.TableDamage, origin: ent.Owner);
            _damageable.TryChangeDamage(ent.Owner, glass.ClimberDamage, origin: ent.Owner);
            knockdown *= 2;
        }
        else
        {
            var blunt = hardSurface ? ent.Comp.SlamObjectDamage : ent.Comp.SlamTableDamage;
            var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = FixedPoint2.New(blunt) } };

            _damageable.TryChangeDamage(ent.Owner, damage, origin: ent.Owner);

            // Walls shrug it off, but a window or a locker should show where somebody's head went.
            if (hardSurface)
                _damageable.TryChangeDamage(args.OtherEntity, damage, origin: ent.Owner);
        }

        _stamina.TakeStaminaDamage(ent.Owner, ent.Comp.SlamStaminaDamage);
        _stun.TryKnockdown(ent.Owner, knockdown);

        var slammed = EnsureComp<SlammedComponent>(ent.Owner);
        slammed.Until = _timing.CurTime + knockdown;
        Dirty(ent.Owner, slammed);

        _audio.PlayPvs(SlamSound, ent.Owner);
    }

    /// <summary>
    ///     They came down without hitting anything - a missed slam, or the surface stopped existing
    ///     mid-flight. Clear the flag so it isn't still armed the next time they bump a wall.
    /// </summary>
    private void OnSlammedLand(Entity<PullableComponent> ent, ref LandEvent args)
    {
        if (!ent.Comp.BeingSlammed)
            return;

        ent.Comp.BeingSlammed = false;
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///     Shoving somebody who is still picking themselves up off the floor.
    /// </summary>
    private void OnSlammedDisarmAttempt(Entity<SlammedComponent> ent, ref DisarmAttemptEvent args)
    {
        if (_timing.CurTime >= ent.Comp.Until)
            return;

        if (!_netManager.IsServer || !_random.Prob(ent.Comp.ParalyzeChance))
            return;

        _stun.TryUpdateParalyzeDuration(ent.Owner, ent.Comp.ParalyzeDuration);
        RemComp<SlammedComponent>(ent.Owner);
    }

    /// <summary>
    ///     Anything anchored and solid enough to stop a body: tables, walls, windows, lockers, machines.
    /// </summary>
    /// <param name="hardSurface">
    ///     False for tables, which have their own bonk handling and which you land on top of. True for
    ///     everything else, which you go into face first and which hurts more for it.
    /// </param>
    private bool IsSlammable(EntityUid uid, out bool hardSurface)
    {
        hardSurface = false;

        // Checked first: a table is anchored and solid too, but it is the softer of the two cases.
        if (HasComp<BonkableComponent>(uid))
            return true;

        if (Transform(uid) is not { Anchored: true }
            || !TryComp<FixturesComponent>(uid, out var fixtures))
            return false;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            // Has to be something a walking mob would have been stopped by in the first place -
            // otherwise every open grille and disposal pipe on the way counts as a wall.
            if (!fixture.Hard || (fixture.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                continue;

            hardSurface = true;
            return true;
        }

        return false;
    }
}
