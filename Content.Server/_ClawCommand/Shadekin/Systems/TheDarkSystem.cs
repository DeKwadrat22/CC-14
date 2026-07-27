using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._ClawCommand.Shadekin;

public sealed partial class TheDarkSystem : EntitySystem
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private MapLoaderSystem _loader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HideoutGeneratorComponent, MapInitEvent>(SetupTheDark);
        SubscribeLocalEvent<HideoutGeneratorComponent, ComponentShutdown>(DestroyTheDark);
    }

    private void SetupTheDark(EntityUid uid, HideoutGeneratorComponent component, MapInitEvent args)
    {
        #if DEBUG
        // The dark map spawns in every single integration test case, slowing the test suite and
        // causing random failures. If you want to test the dark, compile the server in the "Tools"
        // configuration. - Mnemotechnician (Floofstation)
        return;
        #endif

        var opts = DeserializationOptions.Default with { InitializeMaps = true };
        if (!_loader.TryLoadMap(new ResPath("/Maps/_ClawCommand/hideout.yml"), out var map, out var grids, opts))
            return;

        foreach (var grid in grids)
            EnsureComp<PreventPilotComponent>(grid.Owner);

        component.Generated.Add(map.Value.Comp.MapId);
    }

    private void DestroyTheDark(EntityUid uid, HideoutGeneratorComponent component, ComponentShutdown args)
    {
        foreach (var mapId in component.Generated)
        {
            if (!_map.MapExists(mapId))
                continue;

            _map.DeleteMap(mapId);
        }
    }
}
