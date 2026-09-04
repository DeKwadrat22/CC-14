// Claw Command: Path of Lock handlers. The C# event classes EventHereticBulglarFinesse and
// EventHereticLastRefugee already existed in Heretic.Abilites.cs but had no YAML wiring and no
// subscribers — this file implements them and finishes the Path of Lock progression.

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Heretic;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedEntityStorageSystem _storage = default!;

    private const float BurglarFinesseRange = 5f;
    private const float LastRefugeeRange = 30f;

    private void SubscribePathLock()
    {
        SubscribeLocalEvent<EventHereticBulglarFinesse>(OnBurglarFinesse);
        SubscribeLocalEvent<EventHereticLastRefugee>(OnLastRefugee);
    }

    private void OnBurglarFinesse(EventHereticBulglarFinesse args)
    {
        if (!TryUseAbility(args))
            return;

        var performer = args.Performer;
        var coords = Transform(performer).Coordinates;

        // Doors first — they're the iconic target.
        foreach (var door in _lookup.GetEntitiesInRange<DoorComponent>(coords, BurglarFinesseRange))
        {
            if (door.Comp.State != DoorState.Closed)
                continue;

            // Bolted? Unbolt before opening — Burglar's Finesse defies even the captain's airlocks.
            if (TryComp<DoorBoltComponent>(door, out var bolts) && bolts.BoltsDown)
                _door.SetBoltsDown((door, bolts), false);

            // StartOpening skips the access check; TryOpen would not.
            _door.StartOpening(door);
            Popup.PopupEntity(Loc.GetString("heretic-burglar-finesse-success"), performer, performer);
            args.Handled = true;
            return;
        }

        // No nearby door — try a sealed locker/crate instead.
        foreach (var locker in _lookup.GetEntitiesInRange<EntityStorageComponent>(coords, BurglarFinesseRange))
        {
            if (_storage.IsOpen(locker, locker.Comp))
                continue;

            if (TryComp<LockComponent>(locker, out var lockComp) && lockComp.Locked)
                _lock.Unlock(locker, null, lockComp);

            _storage.OpenStorage((locker.Owner, locker), locker);
            Popup.PopupEntity(Loc.GetString("heretic-burglar-finesse-success"), performer, performer);
            args.Handled = true;
            return;
        }

        Popup.PopupEntity(Loc.GetString("heretic-burglar-finesse-fail"), performer, performer);
    }

    private void OnLastRefugee(EventHereticLastRefugee args)
    {
        if (!TryUseAbility(args))
            return;

        var performer = args.Performer;
        var coords = Transform(performer).Coordinates;

        // Find a sealed locker to vanish into. Range is generous (30 tiles) so an ambushed
        // heretic almost always finds *something* — even if it's a maint locker on the next deck.
        foreach (var locker in _lookup.GetEntitiesInRange<EntityStorageComponent>(coords, LastRefugeeRange))
        {
            if (_storage.IsOpen(locker, locker.Comp))
                continue;

            if (TryComp<LockComponent>(locker, out var lockComp) && lockComp.Locked)
                _lock.Unlock(locker, null, lockComp);

            // Pop the locker, jump in, slam it shut behind us.
            _storage.OpenStorage((locker.Owner, locker), locker);
            _transform.SetCoordinates(performer, Transform(locker).Coordinates);
            _storage.Insert(performer, locker, locker.Comp);
            _storage.CloseStorage((locker.Owner, locker), locker);

            // Re-lock if it was locked when we arrived — extra concealment.
            if (lockComp is { Locked: false } && TryComp<LockComponent>(locker, out var lockCompAfter))
                _lock.Lock(locker, null, lockCompAfter);

            Popup.PopupEntity(Loc.GetString("heretic-last-refugee-success"), performer, performer);
            args.Handled = true;
            return;
        }

        Popup.PopupEntity(Loc.GetString("heretic-last-refugee-fail"), performer, performer);
    }
}
