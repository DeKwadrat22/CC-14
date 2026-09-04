using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Heretic.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class RemoveAccess : EntityEffectBase<RemoveAccess>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Removes all target access.";
}

public sealed partial class RemoveAccessEntityEffectSystem : EntityEffectSystem<InventoryComponent, RemoveAccess>
{
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedAccessSystem _access = default!;

    protected override void Effect(Entity<InventoryComponent> entity, ref EntityEffectEvent<RemoveAccess> args)
    {
        if (!_idCard.TryFindIdCard(entity.Owner, out var id))
            return;

        _access.TrySetTags(id, new List<ProtoId<AccessLevelPrototype>>());
    }
}
