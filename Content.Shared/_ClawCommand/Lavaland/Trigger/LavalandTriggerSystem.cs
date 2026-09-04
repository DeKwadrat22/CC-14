// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.Trigger;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._ClawCommand.Lavaland.Trigger;

public sealed partial class LavalandTriggerSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnTableOnTriggerComponent, TriggerEvent>(OnSpawnerTrigger);
        SubscribeLocalEvent<TriggerCounterComponent, TriggerEvent>(OnTriggerCounter);
        SubscribeLocalEvent<TriggerCounterLimitComponent, AttemptTriggerEvent>(OnTriggerLimitCounter);
    }

    private void OnSpawnerTrigger(Entity<SpawnTableOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target == null)
            return;

        var xform = Transform(target.Value);
        var spawns = _entityTable.GetSpawns(ent.Comp.Table, _random).ToList();

        if (ent.Comp.UseMapCoords)
        {
            var mapCoords = _transform.GetMapCoordinates(target.Value, xform);
            if (ent.Comp.Predicted)
            {
                foreach (var spawn in spawns)
                {
                    EntityManager.PredictedSpawn(spawn, mapCoords);
                }
            }
            else if (_net.IsServer)
            {
                foreach (var spawn in spawns)
                {
                    Spawn(spawn, mapCoords);
                }
            }
        }
        else
        {
            var coords = xform.Coordinates;
            if (!coords.IsValid(EntityManager))
                return;

            if (ent.Comp.Predicted)
            {
                foreach (var spawn in spawns)
                {
                    PredictedSpawnAttachedTo(spawn, coords);
                }
            }
            else if (_net.IsServer)
            {
                foreach (var spawn in spawns)
                {
                    SpawnAttachedTo(spawn, coords);
                }
            }
        }
    }

    private void OnTriggerCounter(Entity<TriggerCounterComponent> ent, ref TriggerEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Count++;
    }

    private void OnTriggerLimitCounter(Entity<TriggerCounterLimitComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!TryComp(ent.Owner, out TriggerCounterComponent? comp))
            return;

        if (comp.Count >= ent.Comp.MaxCount)
            args.Cancelled = true;
    }
}
