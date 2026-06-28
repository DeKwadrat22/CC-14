// Claw Command: client-side dust spawner for any entity with SprinterDustComponent.
// Polls velocities each tick; when a sprinter is on solid ground and moving above the threshold,
// spawns a small dust cloud beneath them at most once per StepInterval. Pure visual — no input
// handling, no stamina drain, no speed boost.

using Content.Shared._ClawCommand.Sprinting;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client._ClawCommand.Sprinting;

public sealed class SprinterDustSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SprinterDustComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dust, out var body, out var xform))
        {
            // Skip if grid-less (i.e. floating in space) or not moving fast enough.
            if (xform.GridUid == null)
                continue;

            if (body.LinearVelocity.LengthSquared() < dust.SpeedThreshold * dust.SpeedThreshold)
                continue;

            if (now - dust.LastStep < dust.StepInterval)
                continue;

            dust.LastStep = now;
            Spawn(dust.StepAnimation, xform.Coordinates);
        }
    }
}
