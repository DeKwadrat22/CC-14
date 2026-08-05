using Content.Server.Abilities.Psionics;
using Content.Server._ClawCommand.Carrying; // Claw Command - carrying lives under _ClawCommand here
using Content.Shared.ActionBlocker;
using Content.Shared.Damage; using Content.Shared.Damage.Systems; using Content.Shared.Damage.Components; // Claw Command - damage split into Systems/Components
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.NPC.Systems;


namespace Content.Server.LifeDrainer;

public sealed partial class LifeDrainerSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LifeDrainerComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LifeDrainerComponent, LifeDrainDoAfterEvent>(OnDrain);
    }

    private void OnGetVerbs(Entity<LifeDrainerComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var target = args.Target;
        if (!args.CanAccess || !args.CanInteract || !CanDrain(ent, target))
            return;

        args.Verbs.Add(new InnateVerb()
        {
            Act = () =>
            {
                TryDrain(ent, target);
            },
            Text = Loc.GetString("verb-life-drain"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Nyanotrasen/Icons/verbiconfangs.png")),
            Priority = 2
        });
    }

    private void OnDrain(Entity<LifeDrainerComponent> ent, ref LifeDrainDoAfterEvent args)
    {
        var (uid, comp) = ent;
        CancelDrain(comp);
        if (args.Handled || args.Args.Target is not {} target)
            return;

        // attack whoever interrupted the draining
        if (args.Cancelled)
        {
            // someone pulled the psionic away
            if (TryComp<PullableComponent>(target, out var pullable) && pullable.Puller is {} puller)
                _faction.AggroEntity(uid, puller);

            // someone pulled me away
            if (TryComp(ent, out pullable) && pullable.Puller is {} selfPuller)
                _faction.AggroEntity(uid, selfPuller);

            // someone carried the psionic away
            if (TryComp<BeingCarriedComponent>(target, out var carried))
                _faction.AggroEntity(uid, carried.Carrier);

            return;
        }

        _popup.PopupEntity(Loc.GetString("life-drain-second-end", ("drainer", uid)), target, target, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("life-drain-third-end", ("drainer", uid), ("target", target)), target, Filter.PvsExcept(target), true, PopupType.LargeCaution);

        var rejuv = new RejuvenateEvent();
        RaiseLocalEvent(uid, rejuv);

        _audio.PlayPvs(comp.FinishSound, uid);

        _damageable.TryChangeDamage(target, comp.Damage, true, origin: uid); // Claw Command - Entity<T> implicit from EntityUid
        _psionicAbilitiesSystem.MindBreak(target);
    }

    public bool CanDrain(Entity<LifeDrainerComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;
        return !IsDraining(comp)
            && uid != target
            && (comp.Whitelist is null || _whitelist.IsValid(comp.Whitelist, target))
            && _mob.IsCritical(target);
    }

    public bool IsDraining(LifeDrainerComponent comp)
    {
        return _doAfter.GetStatus(comp.DoAfter) == DoAfterStatus.Running;
    }

    public bool TryDrain(Entity<LifeDrainerComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;
        if (!CanDrain(ent, target) || !_actionBlocker.CanInteract(uid, target) || !_interaction.InRangeUnobstructed(ent.Owner, target, popup: true))
            return false;

        _popup.PopupEntity(Loc.GetString("life-drain-second-start", ("drainer", uid)), target, target, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("life-drain-third-start", ("drainer", uid), ("target", target)), target, Filter.PvsExcept(target), true, PopupType.LargeCaution);

        if (_audio.PlayPvs(comp.DrainSound, target) is {} stream)
            comp.DrainStream = stream.Item1;

        var ev = new LifeDrainDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, uid, comp.Delay, ev, target: target, eventTarget: uid)
        {
            BreakOnMove = true,
            MovementThreshold = 2f,
            NeedHand = false
        };

        if (!_doAfter.TryStartDoAfter(args, out var id))
            return false;

        comp.DoAfter = id;
        comp.Target = target;
        return true;
    }

    public void CancelDrain(LifeDrainerComponent comp)
    {
        comp.DrainStream = _audio.Stop(comp.DrainStream);
        _doAfter.Cancel(comp.DoAfter);
        comp.DoAfter = null;
        comp.Target = null;
    }
}
