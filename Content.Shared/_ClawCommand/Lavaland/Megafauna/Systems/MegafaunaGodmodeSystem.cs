using Content.Shared._ClawCommand.Lavaland.Megafauna.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Player;

namespace Content.Shared._ClawCommand.Lavaland.Megafauna.Systems;

public sealed partial class MegafaunaGodmodeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MegafaunaGodmodeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<MegafaunaGodmodeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin == null
            || !HasComp<ActorComponent>(args.Origin))
            args.Cancelled = true;
    }
}
