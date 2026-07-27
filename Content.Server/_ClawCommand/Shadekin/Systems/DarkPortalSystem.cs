using Content.Shared._ClawCommand.Shadekin;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._ClawCommand.Shadekin;

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
    {
        ClearSerializedLinks(uid);

        var query = EntityQueryEnumerator<DarkHubComponent>();
        while (query.MoveNext(out var hub, out _))
            _link.TryLink(uid, hub);
    }

    /// <summary>
    ///     The Dark is generated off the station entity's MapInit, which can land after the station grid's own
    ///     entities have started up. Linking from the portal side alone would leave a portal that came up first
    ///     without a destination forever, so the hub links backwards as well.
    /// </summary>
    private void OnHubStartup(EntityUid uid, DarkHubComponent component, ComponentStartup args)
    {
        ClearSerializedLinks(uid);

        var query = EntityQueryEnumerator<DarkPortalComponent>();
        while (query.MoveNext(out var portal, out _))
            _link.TryLink(portal, uid);
    }

    /// <summary>
    ///     Station maps ship these portals with a saved LinkedEntity component whose entries deserialise to
    ///     EntityUid.Invalid. A dead link still counts towards LinkedEntities, so SharedPortalSystem rolls it as a
    ///     teleport destination (throwing on Transform()) and CanPredictTeleport bails on the count being != 1.
    ///     Links are rebuilt from scratch below, so drop whatever the map brought with it.
    /// </summary>
    private void ClearSerializedLinks(EntityUid uid)
    {
        RemComp<LinkedEntityComponent>(uid);
    }
}
