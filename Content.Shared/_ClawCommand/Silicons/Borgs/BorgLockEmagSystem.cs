using Content.Shared.Emag.Systems;
using Content.Shared.Lock;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// CLAW COMMAND - lets a syndicate cryptographic sequencer (Emag / EmagUnlimited) bypass a borg's
/// AccessReader lock and toggle it. Holding the emag and clicking the borg - the standard emag
/// interaction - locks/unlocks it regardless of the borg's access requirement, so a traitor can
/// crack open the access-restricted security / medbay / salvage dogborgs (or any borg chassis).
///
/// The emag is an <see cref="EmagType.Interaction"/> breaker, whereas a lock normally only responds to
/// <see cref="EmagType.Access"/> (via <c>LockSystem.OnEmagged</c>, which also only ever *unlocks*). So we
/// handle the Interaction emag on the chassis ourselves and force-toggle the lock, bypassing access.
/// </summary>
public sealed partial class BorgLockEmagSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<BorgChassisComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!TryComp<LockComponent>(ent, out var lockComp))
            return;

        // LockSystem.Lock/Unlock are the "force" variants - no access check - which is exactly the
        // bypass we want from an emag.
        if (lockComp.Locked)
            _lock.Unlock(ent.Owner, args.UserUid, lockComp);
        else
            _lock.Lock(ent.Owner, args.UserUid, lockComp);

        args.Handled = true;
        // Toggling shouldn't burn an emag charge, and keeping it repeatable lets the same emag re-lock
        // or re-open the borg later.
        args.Repeatable = true;
    }
}
