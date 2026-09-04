using Content.Shared._ClawCommand.Shadekin;
using Content.Shared._ClawCommand.Shadekin.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._ClawCommand.Shadekin.Systems;

/// <summary>
///     Keeps every dark portal on a station wired symmetrically to every dark hub in the Dark.
///
///     Two earlier fixes tried to build these links from ComponentStartup and both stayed fragile for the same
///     reason: a mapped station portal and the hub come up in different load phases (the hub's map is loaded off
///     the station entity's MapInit, via TheDarkSystem), so whichever side came up last had to repair the pairing,
///     and doing that from inside an entity's own ComponentStartup meant mutating LinkedEntityComponent on an
///     entity that was still initialising. Deleting and re-placing the portal by hand appeared to fix it because
///     that spawn happens long after load, clear of both phases.
///
///     This version does not depend on ordering at all. There are only ever a couple of these entities, so each
///     tick it cheaply verifies the wiring and rebuilds it only when it is actually wrong. The links therefore
///     self-heal regardless of spawn order, what the map shipped, hideout regeneration, or admin edits.
/// </summary>
public sealed partial class DarkPortalSystem : EntitySystem
{
    [Dependency] private LinkedEntitySystem _link = default!;

    public override void Update(float frameTime)
    {
        var portals = new List<EntityUid>();
        var portalQuery = EntityQueryEnumerator<DarkPortalComponent>();
        while (portalQuery.MoveNext(out var portal, out _))
            portals.Add(portal);

        var hubs = new List<EntityUid>();
        var hubQuery = EntityQueryEnumerator<DarkHubComponent>();
        while (hubQuery.MoveNext(out var hub, out _))
            hubs.Add(hub);

        // One side is missing - station loaded but the Dark isn't generated yet, or the other way round.
        // Nothing to wire; we'll pick it up on a later tick once both exist.
        if (portals.Count == 0 || hubs.Count == 0)
            return;

        if (IsWiredCorrectly(portals, hubs))
            return;

        Relink(portals, hubs);
    }

    /// <summary>
    ///     Every portal must link to exactly the set of hubs, and every hub to exactly the set of portals.
    ///     Anything else counts as wrong and triggers a rebuild: a stale EntityUid.Invalid deserialised from a
    ///     map, a half-applied pairing left by LinkedEntitySystem.TryLink's short-circuit, or a link pointing at
    ///     a portal that has since been deleted.
    /// </summary>
    private bool IsWiredCorrectly(List<EntityUid> portals, List<EntityUid> hubs)
    {
        foreach (var portal in portals)
        {
            if (!TryComp<LinkedEntityComponent>(portal, out var link) || !SetMatches(link.LinkedEntities, hubs))
                return false;
        }

        foreach (var hub in hubs)
        {
            if (!TryComp<LinkedEntityComponent>(hub, out var link) || !SetMatches(link.LinkedEntities, portals))
                return false;
        }

        return true;
    }

    private bool SetMatches(HashSet<EntityUid> actual, List<EntityUid> expected)
    {
        if (actual.Count != expected.Count)
            return false;

        foreach (var ent in expected)
        {
            if (!actual.Contains(ent))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Wipes every link set and rebuilds it in both directions explicitly.
    ///
    ///     Two OneWayLink calls rather than TryLink, because TryLink short-circuits on
    ///     "firstLink.Add(x) &amp;&amp; secondLink.Add(y)" - if the portal already contained the hub, the reverse
    ///     hub -> portal add never ran. That is precisely the state that left the Dark -> station direction
    ///     silently doing nothing while station -> Dark still worked.
    /// </summary>
    private void Relink(List<EntityUid> portals, List<EntityUid> hubs)
    {
        foreach (var portal in portals)
            RemComp<LinkedEntityComponent>(portal);

        foreach (var hub in hubs)
            RemComp<LinkedEntityComponent>(hub);

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
