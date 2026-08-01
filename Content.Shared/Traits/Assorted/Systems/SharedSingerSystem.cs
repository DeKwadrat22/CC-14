using Content.Shared.Actions;
using Content.Shared.Instruments;
using Content.Shared.Traits.Assorted.Components;
using Content.Shared.Traits.Assorted.Prototypes;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits.Assorted.Systems;

public abstract partial class SharedSingerSystem : EntitySystem
{
    [Dependency] protected IPrototypeManager ProtoMan = default!;

    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedInstrumentSystem _instrument = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<SingerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SingerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SingerComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnStartup(Entity<SingerComponent> ent, ref ComponentStartup args)
    {
        if (!ProtoMan.TryIndex(ent.Comp.Proto, out var singer))
            return;

        _actionsSystem.AddAction(ent, ref ent.Comp.MidiAction, ent.Comp.MidiActionId);

        var instrumentComp = EnsureInstrumentComp(ent);
        var defaultData = singer.InstrumentList[singer.DefaultInstrument];
        _instrument.SetInstrumentProgram(ent.Owner, instrumentComp, defaultData.Item1, defaultData.Item2);
        SetUpSwappableInstrument(ent, singer);
    }

    private void OnShutdown(Entity<SingerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.MidiAction);
    }

    private void OnZombified(ref EntityZombifiedEvent args)
    {
        CloseMidiUi(args.Target);
    }

    private void OnPlayerDetached(EntityUid uid, SingerComponent component, PlayerDetachedEvent args)
    {
        CloseMidiUi(uid);
    }

    /// <summary>
    ///     Closes the MIDI UI if it is open. Does nothing on client side.
    /// </summary>
    public virtual void CloseMidiUi(EntityUid uid)
    {
    }

    /// <summary>
    ///     Sets up the swappable instrument on the entity, only on the server.
    /// </summary>
    protected virtual void SetUpSwappableInstrument(EntityUid uid, SingerInstrumentPrototype singer)
    {
    }

    /// <summary>
    ///     Ensures an InstrumentComponent on the entity. Uses client-side comp on client and server-side comp on the server.
    /// </summary>
    protected abstract SharedInstrumentComponent EnsureInstrumentComp(EntityUid uid);
}
