using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Physics;

/// <summary>
/// Zeroes physics damping when an entity falls asleep so that objects launched into space
/// keep their momentum instead of being slowed to a halt by the physics solver.
/// Part of the Frictionless Space port (#464).
/// </summary>
public sealed partial class FrictionRemoverSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicsComponent, PhysicsSleepEvent>(RemoveDampening);
    }

    private void RemoveDampening(EntityUid uid, PhysicsComponent component, PhysicsSleepEvent args)
    {
        _physics.SetAngularDamping(uid, component, 0f, false);
        _physics.SetLinearDamping(uid, component, 0f);
    }
}
