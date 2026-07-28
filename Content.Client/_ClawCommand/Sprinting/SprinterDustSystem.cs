// Claw Command: client-side dust spawner for any entity with SprinterDustComponent.
//
// First version of this used a velocity-magnitude threshold (>= 2.5 m/s) to decide whether to
// spawn dust. That was wrong: DefaultBaseWalkSpeed is *exactly* 2.5 m/s in SS14, so dust kicked
// in for walking too. Even raising the threshold wouldn't be reliable — speed gets modified by
// slow zones, drugs, hyperzine, slipping, stamina drain, etc. The authoritative signal is
// InputMoverComponent.Sprinting, which is true when the player is intentionally running
// (default movement, no Walk button held) — that's what we now key off.
//
// Also plays a one-shot puff sound on the not-sprinting → sprinting transition, ported from
// Goob's SprinterComponent.SprintStartupSound.

using Content.Shared._ClawCommand.Sprinting;
using Content.Shared._ClawCommand.Stealth;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Client._ClawCommand.Sprinting;

public sealed partial class SprinterDustSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StealthModeSystem _stealth = default!;

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
            var grounded = xform.GridUid != null && !_gravity.IsWeightless((uid, null));

            // Player must be sprinting (default movement, NOT holding Walk) and actively pressing
            // a direction. Sprinting-but-stationary doesn't kick up dust either.
            var sprintingNow = grounded && mover.Sprinting && mover.HasDirectionalMovement;

            // Anyone hiding (phased shadekin, cloaked ninja, whatever gets added later) has to stay quiet.
            // WasSprinting is still tracked so that dropping stealth mid-sprint doesn't read as a fresh
            // rising edge and fire the puff sound the moment they become visible again.
            if (_stealth.IsStealthed(uid))
            {
                dust.WasSprinting = sprintingNow;
                continue;
            }

            // Rising edge — play the puff sound exactly once when sprint begins.
            if (sprintingNow && !dust.WasSprinting)
                _audio.PlayPredicted(dust.StartSound, uid, uid);

            dust.WasSprinting = sprintingNow;

            if (!sprintingNow)
                continue;

            if (now - dust.LastStep < dust.StepInterval)
                continue;

            dust.LastStep = now;
            Spawn(dust.StepAnimation, xform.Coordinates);
        }
    }
}
