using Content.Shared._ClawCommand.Shadekin;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._ClawCommand.Shadekin.Systems;

public sealed partial class DarkPortalSystem : EntitySystem
{
    [Dependency] private LinkedEntitySystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DarkPortalComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, DarkPortalComponent component, MapInitEvent args)
    {
        var query = EntityQueryEnumerator<Shared._ClawCommand.Shadekin.Components.DarkHubComponent>();
        while (query.MoveNext(out var target, out var portal))
        {
            _link.TryLink(uid, target);
        }
    }
}
