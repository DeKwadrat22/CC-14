using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.MouseRotator;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._ClawCommand.Movement.DiveLeap;

/// <summary>
///     Claw Command - The sprinting dive-leap.
///
///     Hitting the lie-down key while sprinting throws the character into a short, shallow,
///     barely-steerable dive instead of dropping them where they stand. They go horizontal in the
///     air, arc up and back down, and land prone with the usual body-fall thud.
///
///     Written against the movement system directly rather than reusing ThrowingSystem, because a
///     throw is a one-shot impulse and this needs per-tick control: the whole feel of the move is
///     that you commit to a direction at launch and can only lean it afterwards. Velocity is
///     re-derived every tick from the launch direction plus a clamped steer angle, which keeps the
///     speed constant, stops nudges compounding into a full turn, and makes the motion perfectly
///     deterministic - so the client predicts it exactly with no reconciliation snap.
///
///     The one thing borrowed from the Lavaland katana dash is the fixture trick: strip
///     MidImpassable for the duration so a dive clears tables and railings, remembering exactly
///     which fixtures were touched so landing restores precisely those.
///
///     Note the horizontal pose is applied through the appearance layer only, never through
///     StandingStateSystem.Down. Down() runs ChangeLayers(), which strips collision masks into its
///     own tracking list - having two systems strip the same masks would restore them incorrectly.
///     We want the pose, not the helplessness, until the leap actually lands.
/// </summary>
public sealed partial class SharedDiveLeapSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedRotationVisualsSystem _rotationVisuals = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private Shared.Throwing.ThrowingSystem _throwing = default!;
    [Dependency] private Shared.Throwing.ThrownItemSystem _thrownItem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private Shared.Gravity.SharedGravitySystem _gravity = default!;

    private const int LeapCollisionLayer = (int) CollisionGroup.MidImpassable;


    /// <summary>
    ///     Scratch list for leaps finishing this tick. See Update for why they cannot land inline.
    /// </summary>
    private readonly List<EntityUid> _landing = new();

    private static readonly SoundSpecifier FallbackLaunchSound =
        new SoundCollectionSpecifier("FootstepFloor");

    public override void Initialize()
    {
        base.Initialize();

        // Safety nets. A leap that ends any way other than landing still has to put the collision
        // mask back, or the entity is left permanently able to walk through tables.
        SubscribeLocalEvent<DiveLeapingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DiveLeapingComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
    }

    /// <summary>
    ///     Try to start a leap. Returns true if one began, in which case the caller should not make
    ///     the entity fall over in the ordinary way.
    /// </summary>
    public bool TryStartLeap(EntityUid uid)
    {
        if (!TryComp<DiveLeaperComponent>(uid, out var leaper))
        {
            return false;
        }

        if (HasComp<DiveLeapingComponent>(uid))
            return false;

        if (_timing.CurTime < leaper.NextLeap)
        {
            return false;
        }

        // Traits and effects that rule the move out entirely.
        if (HasComp<NoDiveLeapComponent>(uid))
            return false;

        // No stamina gate here on purpose - the dive costs stamina instead, charged on landing.
        // See DiveLeaperComponent.StaminaCost and Land().

        // Already on the floor, stunned, cuffed to something - none of those get to launch.
        if (_standing.IsDown(uid) || HasComp<KnockedDownComponent>(uid))
        {
            return false;
        }

        if (_container.IsEntityInContainer(uid))
        {
            return false;
        }

        // Must be an upright mob actively sprinting somewhere.
        if (!TryComp<InputMoverComponent>(uid, out var mover))
        {
            return false;
        }

        if (!TryComp<PhysicsComponent>(uid, out var physics) || physics.BodyType == BodyType.Static)
        {
            return false;
        }

        // Sprinting now, or sprinting a moment ago. The second half carries the check: Shift is both
        // the sprint key and a modifier, so the flag reads false on exactly the frame R is pressed.
        var sprintingNow = mover.Sprinting;
        var sprintedRecently = leaper.LastSprintTime != TimeSpan.MinValue
                               && _timing.CurTime - leaper.LastSprintTime <= leaper.SprintGrace;

        if (!mover.CanMove || (!sprintingNow && !sprintedRecently))
        {
            return false;
        }

        // Nothing to push off means nothing to leap from.
        if (_gravity.IsWeightless(uid) && !CanPushOff(uid, physics))
        {
            return false;
        }

        var direction = GetWishDirection(mover);
        if (direction == Vector2.Zero)
        {
            return false;
        }

        StartLeap((uid, leaper), direction.Normalized(), physics);
        return true;
    }

    /// <summary>
    ///     Is this entity actually running right now?
    ///
    ///     <see cref="InputMoverComponent.Sprinting"/> reads the Walk button flag, which is bound to
    ///     Shift. That flag turned out not to be reliably set at the moment another key is pressed
    ///     while Shift is held - the diagnostics showed buttons=Down with no Walk flag during a
    ///     shift-held R press - so gating purely on it made the leap silently unavailable for anyone
    ///     using walk-by-default.
    ///
    ///     Actual speed is the honest answer to "are they running", is immune to input-flag quirks,
    ///     and is identical on client and server since both simulate the same velocity. The flag is
    ///     still accepted first so a player who has only just started moving, and is still
    ///     accelerating, can leap immediately rather than waiting to reach full speed.
    /// </summary>
    /// <summary>
    ///     Whether a weightless entity has something to shove against.
    ///
    ///     Mirrors exactly what SharedMoverController does for weightless movement, and for the same
    ///     reason: in freefall you cannot change direction out of nothing. You may push off a hull,
    ///     a wall or anything solid you can reach, or move freely if something grants it (a jetpack,
    ///     magboots). Adrift in open space with nothing in arm's reach, you get no leap - the same
    ///     rule that already stops you walking or sprinting there.
    /// </summary>
    private bool CanPushOff(EntityUid uid, PhysicsComponent physics)
    {
        var xform = Transform(uid);

        // Jetpack, magboots, anything else that grants free movement while weightless.
        var ev = new CanWeightlessMoveEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);

        if (ev.CanMove || xform.GridUid != null || HasComp<MapGridComponent>(xform.GridUid))
            return true;

        // Off-grid but close enough to touch a hull or other solid body.
        return TryComp<MobMoverComponent>(uid, out var mobMover)
               && _mover.IsAroundCollider((uid, physics, mobMover, xform));
    }

    /// <summary>
    ///     Records, every tick, whether the entity is in the sprint state.
    ///
    ///     Deliberately the sprint *state* and nothing else. An earlier version compared movement
    ///     speed against a threshold, which is unworkable: traits, gear, injury and slowdown
    ///     modifiers all move both walk and sprint speed, so no threshold can separate them for
    ///     every character. Whether the player is sprinting is a boolean the game already tracks,
    ///     and it is true regardless of how fast that happens to make them.
    ///
    ///     Sampled here rather than at the keypress because pressing another bound key while Shift
    ///     is held makes the input system treat Shift as that key's modifier, dropping the Walk
    ///     button and flipping this flag for exactly that frame. This runs on the ordinary tick, so
    ///     it sees the honest value.
    /// </summary>
    private void TrackSprinting()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DiveLeaperComponent, InputMoverComponent>();

        while (query.MoveNext(out var uid, out var leaper, out var mover))
        {
            if (HasComp<DiveLeapingComponent>(uid))
                continue;

            if (mover.Sprinting)
                leaper.LastSprintTime = now;

        }
    }

    /// <summary>
    ///     The direction the player is currently asking to move in, in world terms. Taken from the
    ///     mover so grid rotation and the diagonal-movement setting are handled exactly the way
    ///     ordinary movement handles them.
    /// </summary>
    /// <summary>
    ///     The movement keys as the player sees them, before the grid's rotation is folded in.
    ///
    ///     <see cref="GetWishDirection"/> returns a world-space heading, which is the right thing for
    ///     moving but the wrong thing for deciding what "right" means on screen: on a rotated grid
    ///     the same keypress produces a completely different world vector. Anything that has to
    ///     match the player's idea of a direction needs this instead.
    /// </summary>
    private Vector2 GetScreenDirection(InputMoverComponent mover)
    {
        var (walk, sprint) = _mover.GetVelocityInput(mover);
        var total = walk + sprint;

        return total.LengthSquared() < 0.001f ? Vector2.Zero : total.Normalized();
    }

    private Vector2 GetWishDirection(InputMoverComponent mover)
    {
        var (walk, sprint) = _mover.GetVelocityInput(mover);
        var total = walk + sprint;

        // GetVelocityInput reports the movement accumulated *so far this tick*, scaled by how much
        // of the tick is left. Press the key late in a tick and that legitimately comes back at or
        // near zero even though the player is plainly running. WishDir is the direction the mover
        // controller last actually moved us in, so it is the reliable answer to "which way are we
        // heading right now" - fall back to it rather than refusing the leap.
        if (total.LengthSquared() < 0.001f)
            total = mover.WishDir;

        if (total.LengthSquared() < 0.001f)
            return Vector2.Zero;

        // WishDir is already in world space; the raw button vector is not. Only rotate the latter.
        if (total == mover.WishDir)
            return total.Normalized();

        return _mover.GetParentGridAngle(mover).RotateVec(total);
    }

    private void StartLeap(Entity<DiveLeaperComponent> ent, Vector2 direction, PhysicsComponent physics)
    {
        var (uid, leaper) = ent;

        var leaping = EnsureComp<DiveLeapingComponent>(uid);
        leaping.LaunchDirection = direction;
        leaping.StartTime = _timing.CurTime;
        leaping.EndTime = _timing.CurTime + leaper.Duration;
        leaping.SteerAngle = 0f;
        leaping.ChangedFixtures.Clear();
        leaping.AppliedHorizontal = true;

        SetHorizontalPose(uid, direction, leaper);

        // Clear tables and railings on the way over.
        if (TryComp<FixturesComponent>(uid, out var fixtures))
        {
            foreach (var (key, fixture) in fixtures.Fixtures)
            {
                if ((fixture.CollisionMask & LeapCollisionLayer) == 0)
                    continue;

                leaping.ChangedFixtures.Add(key);
                _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~LeapCollisionLayer, fixtures);
            }
        }


        // Throw rather than write velocity directly.
        //
        // Player mobs are KinematicController bodies, which do not retain linear velocity the way a
        // dynamic body does - setting it every tick still bled to nothing and produced a 1.18 tile
        // shuffle instead of a 3.58 tile dive. ThrowingSystem is the mechanism this codebase already
        // uses to move exactly these bodies (the Lavaland katana dash rides on it), it explicitly
        // accepts KinematicController, and it keeps the entity airborne for the whole flight.
        //
        // direction's *length* is the distance thrown, and flyTime works out as length/speed, so
        // scaling by Speed * Duration makes the throw last exactly as long as the leap.
        var distance = leaper.Speed * (float) leaper.Duration.TotalSeconds;

        _throwing.TryThrow(
            uid,
            direction * distance,
            physics,
            Transform(uid),
            baseThrowSpeed: leaper.Speed,
            user: uid,
            pushbackRatio: 0f,      // no self-recoil; the dive is the whole motion
            friction: 0f,           // constant speed for the flight, we control the arc
            compensateFriction: false,
            recoil: false,
            animated: false,        // our own horizontal pose and arc do the visuals
            playSound: false,       // launch sound is played below
            doSpin: false);         // spinning would fight the horizontal pose

        leaper.NextLeap = leaping.EndTime + leaper.Cooldown;

        Dirty(uid, leaping);
        Dirty(uid, leaper);

        PlayLaunchSound((uid, leaper));

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        TrackSprinting();

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DiveLeapingComponent, DiveLeaperComponent, PhysicsComponent>();

        // Landing removes DiveLeapingComponent, and mutating a component the query is iterating is
        // not safe, so finished leaps are collected first and landed after the loop.
        _landing.Clear();

        while (query.MoveNext(out var uid, out var leaping, out var leaper, out var physics))
        {
            if (now >= leaping.EndTime)
            {
                _landing.Add(uid);
                continue;
            }

            Steer((uid, leaping, leaper), physics, frameTime);
        }

        foreach (var uid in _landing)
        {
            if (TryComp<DiveLeapingComponent>(uid, out var leaping) && TryComp<PhysicsComponent>(uid, out var physics))
                Land((uid, leaping), physics);
        }
    }

    /// <summary>
    ///     Bend the leap a little toward whatever the player is holding.
    ///
    ///     The steer is stored as a signed angle off the launch heading and clamped, so holding a
    ///     perpendicular key for a whole leap curves it by MaxSteerAngle and no further. Velocity is
    ///     then rebuilt from launch direction rotated by that angle, so speed never changes and the
    ///     dive cannot be steered into a turn.
    /// </summary>
    private void Steer(Entity<DiveLeapingComponent, DiveLeaperComponent> ent, PhysicsComponent physics, float frameTime)
    {
        var (uid, leaping, leaper) = ent;

        if (TryComp<InputMoverComponent>(uid, out var mover))
        {
            var wish = GetWishDirection(mover);
            if (wish != Vector2.Zero)
            {
                wish = wish.Normalized();

                // Cross product sign tells us which side of the launch heading the input is on.
                var launch = leaping.LaunchDirection;
                var cross = launch.X * wish.Y - launch.Y * wish.X;

                var maxSteer = (float) leaper.MaxSteerAngle.Theta;
                var rate = leaper.SteerSpeed / MathF.Max(leaper.Speed, 0.01f);

                leaping.SteerAngle = Math.Clamp(leaping.SteerAngle + cross * rate * frameTime, -maxSteer, maxSteer);
                Dirty(uid, leaping);
            }
        }

        var heading = new Angle(leaping.SteerAngle).RotateVec(leaping.LaunchDirection);
        _physics.SetLinearVelocity(uid, heading * leaper.Speed, body: physics);
    }

    private void Land(Entity<DiveLeapingComponent> ent, PhysicsComponent physics)
    {
        var uid = ent.Owner;


        // End the throw through ThrownItemSystem, never by tearing the component off ourselves.
        //
        // TryThrow adds a throwing fixture as well as the component, and StopThrow is what destroys
        // it, regenerates contacts and restores ground status. Removing the component by hand left
        // that fixture attached to the player after every single leap, quietly mutating their
        // networked FixturesComponent - which is what was corrupting the game state PVS serialises
        // for the player entity and tripping the engine's state assert mid-dive.
        if (TryComp<Shared.Throwing.ThrownItemComponent>(uid, out var thrown))
            _thrownItem.StopThrow(uid, thrown);
        else
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);

        // RemComp fires ComponentShutdown, which restores the fixtures and clears our cosmetic pose.
        // Keeping that in one place means every exit path - landing, gibbing, being stuffed into a
        // locker - runs identical cleanup.
        RemComp<DiveLeapingComponent>(uid);

        // Pay for the dive, and pay for it *here* rather than at launch.
        //
        // Half a stamina pool is enough to drop an already-winded diver straight into stamina crit,
        // and crit paralyses, which runs StandingStateSystem.Down -> ChangeLayers. That strips
        // StandingCollisionLayer, which is the very same MidImpassable bit the leap strips for the
        // flight. Charging at launch would therefore have both systems owning that bit at once:
        // ChangeLayers would find it already gone, record nothing, and landing would hand it back
        // while the player was still lying there paralysed - a mob that body-blocks while prone.
        // The class summary flags this exact overlap as the reason the leap never calls Down().
        //
        // By the time we get here the component is gone and RestoreFixtures has already run, so the
        // fixtures are whole and stamina crit can do its normal thing. The knockdown below then
        // simply refreshes what crit already applied.
        ApplyStaminaCost(uid);

        // Now actually go down. This plays the body-fall sound for us and applies the real prone
        // state, including its own fixture handling, which by now has ours fully out of the way.
        //
        // The duration must be greater than zero: CanKnockdown bails on `time <= TimeSpan.Zero`, so
        // passing Zero here silently did nothing at all and every dive ended standing up, with no
        // landing thud. Mirror the lie-down key and use the crawler's own default.
        var knockdownTime = TryComp<CrawlerComponent>(uid, out var crawler)
            ? crawler.DefaultKnockedDuration
            : TimeSpan.FromSeconds(0.5);

        _stun.TryKnockdown(uid, knockdownTime, refresh: true, autoStand: false, drop: false);
    }

    /// <summary>
    ///     Charge the diver <see cref="DiveLeaperComponent.StaminaCost"/> of their stamina pool.
    ///
    ///     Scaled off CritThreshold rather than being a flat number so the dive costs the same
    ///     proportion of everyone's stamina, whatever traits and gear have done to the size of it.
    ///
    ///     visual: false because the aqua flash is the "you got hit" tell and this is self-inflicted
    ///     exertion; the launch sound and the landing thud already carry the move.
    /// </summary>
    private void ApplyStaminaCost(EntityUid uid)
    {
        if (!TryComp<DiveLeaperComponent>(uid, out var leaper) || leaper.StaminaCost <= 0f)
            return;

        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        _stamina.TakeStaminaDamage(uid, stamina.CritThreshold * leaper.StaminaCost, stamina, visual: false);
    }

    private void OnShutdown(Entity<DiveLeapingComponent> ent, ref ComponentShutdown args)
    {
        RestoreFixtures(ent);

        // Catch-all for leaps that end without landing - gibbed, deleted, admin-yanked. Land() has
        // usually stopped the throw already and this is a no-op, but if it has not then the throwing
        // fixture would otherwise stay attached forever and keep corrupting networked fixture state.
        if (TryComp<Shared.Throwing.ThrownItemComponent>(ent, out var thrown) && !TerminatingOrDeleted(ent))
            _thrownItem.StopThrow(ent, thrown);

        if (ent.Comp.AppliedHorizontal)
            ClearHorizontalPose(ent);
    }

    private void OnInserted(Entity<DiveLeapingComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        // Shoved into a locker or a disposal loop mid-flight. End the leap rather than let it keep
        // driving velocity from inside a container.
        if (TryComp<PhysicsComponent>(ent, out var physics))
            _physics.SetBodyStatus(ent, physics, BodyStatus.OnGround);

        RemComp<DiveLeapingComponent>(ent);
    }

    private void RestoreFixtures(Entity<DiveLeapingComponent> ent)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        foreach (var key in ent.Comp.ChangedFixtures)
        {
            if (!fixtures.Fixtures.TryGetValue(key, out var fixture))
                continue;

            _physics.SetCollisionMask(ent, key, fixture, fixture.CollisionMask | LeapCollisionLayer, fixtures);
        }

        ent.Comp.ChangedFixtures.Clear();
    }

    /// <summary>
    ///     Lay the sprite over for the flight, and decide which way it lies.
    ///
    ///     Out of combat the body lies along the direction of travel, so the dive reads as going
    ///     that way. In combat mode the mouse owns facing, so instead we pick whichever side keeps
    ///     the cursor in front of the character's head - dive left while aiming right and they flip
    ///     over in the air to keep facing the cursor.
    /// </summary>
    private void SetHorizontalPose(EntityUid uid, Vector2 direction, DiveLeaperComponent leaper)
    {
        var lieAngle = Angle.FromDegrees(90);

        if (HasComp<MouseRotatorComponent>(uid) && HasComp<CombatModeComponent>(uid))
        {
            var facing = _transform.GetWorldRotation(uid).ToWorldVec();
            var cross = facing.X * direction.Y - facing.Y * direction.X;
            if (cross < 0f)
                lieAngle = Angle.FromDegrees(-90);
        }
        else
        {
            // A fixed offset, because the sprite is already showing the character facing the way
            // they are running - the rotation only has to tip them over, not aim them.
            //
            // -90 gives head-first when running north, south or west. East is the one exception and
            // needs the opposite sign: the east-facing sprite is the mirror of the west-facing one,
            // and mirroring reverses which way a rotation visually turns, so the same angle drops
            // the head on the opposite end. Flipping the sign for eastward travel cancels that out.
            //
            // Derived from observation rather than theory: -90 was reported correct for left, up and
            // down, and wrong only for right.
            // Use whatever angle this entity normally lies at, unchanged.
            //
            // Aiming the head along the direction of travel was tried repeatedly and every formula
            // broke at least one direction: constants worked for exactly one heading, cancelling the
            // sprite-state quantisation collapsed to a constant, and deriving straight from the world
            // angle left characters standing upright mid-dive. The interaction between the entity
            // rotation, the four-way sprite state and the sprite rotation does not behave the way any
            // of those assumed, and guessing at it kept regressing headings that already looked fine.
            //
            // The default lying angle is the one pose guaranteed to read correctly, because it is the
            // same one the lie-down key produces. It also matches what the landing knockdown settles
            // on, so the dive flows into the prone landing with no snap.
            // Every screen direction poses correctly at the default lying angle except right, which
            // comes out feet-first, so that one case is turned around.
            //
            // Keyed on the *screen* direction - the raw movement keys before the grid rotation is
            // applied - because that is what "right" means to the player. The world-space heading
            // for right is different on every grid: it measured 58 degrees on one station and -4 on
            // another, so world angle, GetCardinalDir and a raw X test each picked out a different
            // screen direction depending on where you were standing. That is why earlier attempts
            // kept fixing one heading and breaking another.
            var screen = TryComp<InputMoverComponent>(uid, out var poseMover)
                ? GetScreenDirection(poseMover)
                : Vector2.Zero;

            if (screen.X > 0f && MathF.Abs(screen.X) >= MathF.Abs(screen.Y))
                lieAngle = leaper.PoseOffset + Angle.FromDegrees(180);
            else
                lieAngle = leaper.PoseOffset;
        }

        _rotationVisuals.SetHorizontalAngle(uid, lieAngle);
        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal);
    }

    private void ClearHorizontalPose(EntityUid uid)
    {
        _rotationVisuals.ResetHorizontalAngle(uid);

        // Only stand the sprite back up if nothing else wants it lying down. Landing knocks the
        // entity prone a moment later, and StandingStateSystem will set Horizontal again itself.
        if (!_standing.IsDown(uid))
            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical);
    }

    /// <summary>
    ///     Launch audio - the footstep again, louder, so the leap sounds like it grew out of the run
    ///     rather than being a separate noise bolted on.
    /// </summary>
    private void PlayLaunchSound(Entity<DiveLeaperComponent> ent)
    {
        var sound = ent.Comp.LaunchSound;

        // Prefer whatever this mob actually steps with, so a lizard's leap sounds like a lizard.
        if (sound == null && TryComp<FootstepModifierComponent>(ent, out var footstep))
            sound = footstep.FootstepSoundCollection;

        sound ??= FallbackLaunchSound;

        var volume = InputMoverComponent.SprintingSoundModifier + ent.Comp.LaunchVolume;
        _audio.PlayPredicted(sound, ent.Owner, ent.Owner, AudioParams.Default.WithVolume(volume));
    }
}
