using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared._ClawCommand.Traits;
using Content.Shared.Actions.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Traits;

public sealed partial class EggLayerTraitSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggLayerTraitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EggLayerTraitComponent, EggLayInstantActionEvent>(OnEggLayAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EggLayerTraitComponent>();
        while (query.MoveNext(out var uid, out var eggLayer))
        {
            // Players should be using the action.
            if (HasComp<ActorComponent>(uid))
                continue;

            if (_timing.CurTime < eggLayer.NextGrowth)
                continue;

            // Randomize next growth time for more organic egglaying.
            eggLayer.NextGrowth += TimeSpan.FromSeconds(_random.NextFloat(eggLayer.EggLayCooldownMin, eggLayer.EggLayCooldownMax));
        }
    }

    private void OnMapInit(EntityUid uid, EggLayerTraitComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.Action, component.EggLayAction);
        component.NextGrowth = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.EggLayCooldownMin, component.EggLayCooldownMax));
    }

    private void OnEggLayAction(EntityUid uid, EggLayerTraitComponent egglayer, EggLayInstantActionEvent args)
    {
        // Cooldown is handeled by ActionAnimalLayEgg in types.yml.
        args.Handled = TryLayEgg(uid, egglayer);
    }

    public bool TryLayEgg(EntityUid uid, EggLayerTraitComponent? egglayer)
    {
        if (!Resolve(uid, ref egglayer))
            return false;

        if (_mobState.IsDead(uid))
            return false;

        // Allow infinitely laying eggs if they can't get hungry.
        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            if (_hunger.GetHunger(hunger) < egglayer.HungerUsage)
            {
                _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-too-hungry"), uid, uid);
                return false;
            }

            _hunger.ModifyHunger(uid, -egglayer.HungerUsage, hunger);
        }

        foreach (var ent in EntitySpawnCollection.GetSpawns(egglayer.EggSpawn, _random))
        {
            Spawn(ent, Transform(uid).Coordinates);
        }

        // Sound + popups
        _audio.PlayPvs(egglayer.EggLaySound, uid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-user"), uid, uid);
        _popup.PopupEntity(Loc.GetString("action-popup-lay-egg-others", ("entity", uid)), uid, Filter.PvsExcept(uid), true);

        return true;
    }
}
