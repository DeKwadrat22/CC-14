using System.Numerics;
using Content.Shared._ClawCommand.Lavaland.Megafauna.Components;
using Content.Shared._ClawCommand.Lavaland.Megafauna.Events;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared._ClawCommand.Lavaland.Megafauna.Systems;

public sealed partial class MegafaunaSystem
{
    public void StartupMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        RaiseLocalEvent(ent, new MegafaunaStartupEvent());
        ent.Comp.Active = true;
    }

    public void ShutdownMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        RaiseLocalEvent(ent, new MegafaunaShutdownEvent());
        ent.Comp.Active = false;
    }

    public void KillMegafauna(Entity<MegafaunaAiComponent> ent)
    {
        RaiseLocalEvent(ent, new MegafaunaKilledEvent());
        ent.Comp.Active = false;
    }

    /// <summary>
    /// Helper method that constructs new <see cref="RequestPerformActionEvent"/> for megafauna AI to use an action.
    /// </summary>
    public RequestPerformActionEvent GetPerformEvent(EntityUid boss, EntityUid action)
    {
        var targetingComp = CompOrNull<MegafaunaAiTargetingComponent>(boss);

        var netAction = GetNetEntity(action);
        var netTarget = HasComp<EntityTargetActionComponent>(action) ? GetNetEntity(targetingComp?.TargetEnt) : null;
        // Fork: RequestPerformActionEvent expects a non-nullable NetCoordinates when an EntityCoordinates ctor is used.
        if (HasComp<WorldTargetActionComponent>(action))
        {
            var coords = GetNetCoordinates(targetingComp?.TargetCoords ?? default);
            return new RequestPerformActionEvent(netAction, netTarget, coords);
        }

        return netTarget.HasValue
            ? new RequestPerformActionEvent(netAction, netTarget.Value)
            : new RequestPerformActionEvent(netAction);
    }

    public void PickRandomPosition(MegafaunaCalculationBaseArgs args, float radius)
    {
        // TODO add an option to not pick any obstructed coordinates

        var uid = args.Entity;
        var mapId = Transform(uid).MapID;

        var randomVector = new Vector2(args.Random.NextFloat(-radius, radius), args.Random.NextFloat(-radius, radius));
        var position = _xform.GetWorldPosition(uid) + randomVector;
        var newMapCoords = new MapCoordinates(position, mapId);
        var coords = _xform.ToCoordinates(newMapCoords);

        var comp = EnsureComp<MegafaunaAiTargetingComponent>(args.Entity);
        comp.TargetEnt = null;
        comp.TargetCoords = coords;
    }
}
