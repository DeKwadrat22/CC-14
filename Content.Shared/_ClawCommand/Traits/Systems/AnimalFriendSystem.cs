using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared._ClawCommand.Traits.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Traits.Systems;

public sealed partial class AnimalFriendSystem : EntitySystem
{
    [Dependency] private NpcFactionSystem _faction = default!;

    private static readonly ProtoId<NpcFactionPrototype> AnimalFriendFaction = "AnimalFriend";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnimalFriendComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, AnimalFriendComponent comp, ComponentStartup args)
    {
        _faction.AddFaction(uid, AnimalFriendFaction);
    }
}
