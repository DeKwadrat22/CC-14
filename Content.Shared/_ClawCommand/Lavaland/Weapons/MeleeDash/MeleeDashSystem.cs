// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Physics;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._ClawCommand.Lavaland.Weapons.MeleeDash;

/// <summary>
/// Lavaland katana dash. Adapted from Goob: the on-collide light-attack handler is
/// dropped since fork's <c>SharedMeleeWeaponSystem.DoLightAttack</c> is protected and
/// fork has no AnimatedEmotes component. The dash motion + audio still ports cleanly.
/// </summary>
public sealed partial class MeleeDashSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    private const int DashCollisionLayer = (int) CollisionGroup.MidImpassable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DashingComponent, LandEvent>(OnLand);
        SubscribeAllEvent<MeleeDashEvent>(OnDash);
    }

    private void OnLand(Entity<DashingComponent> ent, ref LandEvent args)
    {
        var (uid, comp) = ent;

        if (TryComp(uid, out FixturesComponent? fixtureComponent))
        {
            foreach (var key in comp.ChangedFixtures)
            {
                if (!fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                    continue;

                _physics.SetCollisionMask(uid,
                    key,
                    fixture,
                    fixture.CollisionMask | DashCollisionLayer,
                    fixtureComponent);
            }
        }

        RemCompDeferred(uid, comp);
    }

    private void OnDash(MeleeDashEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
            return;

        var user = args.SenderSession.AttachedEntity.Value;

        if (_standing.IsDown(user))
            return;

        if (_container.IsEntityInContainer(user))
            return;

        var weapon = GetEntity(msg.Weapon);

        if (!TryComp(weapon, out MeleeDashComponent? dash) ||
            !TryComp(weapon, out UseDelayComponent? delay) || _useDelay.IsDelayed((weapon, delay)))
            return;

        var length = MathF.Min(msg.Direction.Length(), dash.MaxDashLength);
        if (length <= 0f)
            return;
        var dir = msg.Direction.Normalized() * length;

        _useDelay.TryResetDelay((weapon, delay));

        var dashing = EnsureComp<DashingComponent>(user);

        if (TryComp(user, out FixturesComponent? fixtureComponent))
        {
            foreach (var (key, fixture) in fixtureComponent.Fixtures)
            {
                if ((fixture.CollisionMask & DashCollisionLayer) == 0)
                    continue;

                dashing.ChangedFixtures.Add(key);
                _physics.SetCollisionMask(user,
                    key,
                    fixture,
                    fixture.CollisionMask & ~DashCollisionLayer,
                    manager: fixtureComponent);
            }
        }

        dashing.Weapon = weapon;
        Dirty(user, dashing);

        _throwing.TryThrow(user, dir, dash.DashForce, null, 0f, null, false, false, false, false, false);
        _audio.PlayPredicted(dash.DashSound, user, user);
    }
}
