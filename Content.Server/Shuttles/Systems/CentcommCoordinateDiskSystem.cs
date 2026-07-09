using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Resolves the Destination of coordinate disks marked with
/// <see cref="CentcommCoordinateDiskComponent"/> to the currently-loaded CentComm map.
/// Fires on MapInit — CentComm is set up during station init, so by the time any disk
/// (map-spawned or admin-spawned) initializes, StationCentcomm.MapEntity is already set.
/// </summary>
public sealed class CentcommCoordinateDiskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CentcommCoordinateDiskComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<CentcommCoordinateDiskComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ShuttleDestinationCoordinatesComponent>(ent, out var disk))
        {
            Log.Warning($"CentcommCoordinateDisk {ToPrettyString(ent)} lacks ShuttleDestinationCoordinatesComponent; cannot set destination.");
            return;
        }

        var query = EntityQueryEnumerator<StationCentcommComponent>();
        while (query.MoveNext(out var stationComp))
        {
            if (stationComp.MapEntity is not { } map)
                continue;

            disk.Destination = map;
            Dirty(ent, disk);
            return;
        }

        Log.Warning($"CentcommCoordinateDisk {ToPrettyString(ent)} spawned before CentComm map exists; Destination left unset.");
    }
}
