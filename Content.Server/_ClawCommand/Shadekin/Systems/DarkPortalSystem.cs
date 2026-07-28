using Content.Shared._ClawCommand.Shadekin;
using Content.Shared._ClawCommand.Shadekin.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._ClawCommand.Shadekin.Systems;

public sealed partial class DarkPortalSystem : EntitySystem
{
    [Dependency] private LinkedEntitySystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DarkPortalComponent, ComponentStartup>(OnPortalStartup);
        SubscribeLocalEvent<DarkHubComponent, ComponentStartup>(OnHubStartup);
    }

    private void OnPortalStartup(EntityUid uid, DarkPortalComponent component, ComponentStartup args)
        => RelinkAll();

    private void OnHubStartup(EntityUid uid, DarkHubComponent component, ComponentStartup args)
        => RelinkAll();

    /// <summary>
    ///     Rebuilds every dark portal ↔ hub link from scratch so the pairing is always symmetric.
    ///
    ///     The Dark is generated off the station entity's MapInit, which can land before or after the station grid's
    ///     own mapped portals have started up. Mapped station portals also ship a stale serialized LinkedEntity whose
    ///     entries deserialise to EntityUid.Invalid. The old approach used LinkedEntitySystem.TryLink, but TryLink
    ///     short-circuits ("firstLink.Add(x) &amp;&amp; secondLink.Add(y)") — if the portal already contained the hub (stale
    ///     link, or linked earlier in the ordering), the reverse (hub → portal) add was skipped, leaving the hub with no
    ///     back-link, so travelling Dark → station silently did nothing until the portal was re-placed.
    ///
    ///     Here every portal/hub's link set is wiped and then relinked in BOTH directions explicitly via two OneWayLink
    ///     calls, so the result is deterministic and always symmetric regardless of start-up order or what the map saved.
    /// </summary>
    private void RelinkAll()
    {
        var portals = new List<EntityUid>();
        var portalQuery = EntityQueryEnumerator<DarkPortalComponent>();
        while (portalQuery.MoveNext(out var portal, out _))
            portals.Add(portal);

        var hubs = new List<EntityUid>();
        var hubQuery = EntityQueryEnumerator<DarkHubComponent>();
        while (hubQuery.MoveNext(out var hub, out _))
            hubs.Add(hub);

        // Nothing to pair yet (e.g. a station portal came up before the Dark was generated).
        if (portals.Count == 0 || hubs.Count == 0)
            return;

        // Drop any stale/serialized links so we start from a clean slate.
        foreach (var portal in portals)
            RemComp<LinkedEntityComponent>(portal);
        foreach (var hub in hubs)
            RemComp<LinkedEntityComponent>(hub);

        // Link every portal to every hub in both directions. Two OneWayLink calls guarantee the symmetric
        // pairing that TryLink's short-circuit could drop.
        foreach (var portal in portals)
        {
            foreach (var hub in hubs)
            {
                _link.OneWayLink(portal, hub);
                _link.OneWayLink(hub, portal);
            }
        }
    }
}
