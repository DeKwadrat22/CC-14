// Claw Command: client-side dust spawner for any entity with SprinterDustComponent.
//
// First version of this used a velocity-magnitude threshold (>= 2.5 m/s) to decide whether to
// spawn dust. That was wrong: DefaultBaseWalkSpeed is *exactly* 2.5 m/s in SS14, so dust kicked
// in for walking too. Even raising the threshold wouldn't be reliable — speed gets modified by
// slow zones, drugs, hyperzine, slipping, stamina drain, etc. The authoritative signal is
// InputMoverComponent.Sprinting, which is true when the player is intentionally running
// (default movement, no Walk button held) — that's what we now key off.

using Content.Shared._ClawCommand.Sprinting;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Robust.Shared.Timing;

namespace Content.Client._ClawCommand.Sprinting;

public sealed partial class SprinterDustSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SprinterDustComponent, InputMoverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dust, out var mover, out var xform))
        {
            // No floor → no dust (no ground to kick up).
            if (xform.GridUid == null || _gravity.IsWeightless((uid, null)))
                continue;

            // Player must be sprinting (default movement, NOT holding Walk) and actively pressing
            // a direction. Sprinting-but-stationary doesn't kick up dust either.
            if (!mover.Sprinting || !mover.HasDirectionalMovement)
                continue;

            if (now - dust.LastStep < dust.StepInterval)
                continue;

            dust.LastStep = now;
            Spawn(dust.StepAnimation, xform.Coordinates);
        }
    }
}
