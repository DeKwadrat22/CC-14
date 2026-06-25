using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._ClawCommand.Traits.Components;

namespace Content.Shared._ClawCommand.Traits.Systems;

/// <summary>
///     Applies GaitSpeedAdjust modifiers to entity movement speed.
/// </summary>
public sealed partial class GaitSpeedAdjustSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GaitSpeedAdjustComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<GaitSpeedAdjustComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GaitSpeedAdjustComponent comp, ComponentStartup args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var move))
            return;

        _movement.RefreshMovementSpeedModifiers(uid, move);
    }

    private void OnRefresh(EntityUid uid, GaitSpeedAdjustComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.WalkFactor, comp.SprintFactor);
    }
}
