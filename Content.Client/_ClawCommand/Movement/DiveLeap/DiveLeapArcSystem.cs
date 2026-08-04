using System.Numerics;
using Content.Shared._ClawCommand.Movement.DiveLeap;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._ClawCommand.Movement.DiveLeap;

/// <summary>
///     Claw Command - Draws the hop in a dive-leap.
///
///     The game is top-down and entities have no Z axis, so "goes up a bit" cannot be real motion -
///     a leaping character travels perfectly flat as far as physics is concerned. The height is
///     therefore a pure sprite offset, nudged up and back down over the life of the leap.
///
///     Client-only and cosmetic by design. It touches nothing the server simulates, so it cannot
///     desync, and a client that somehow misses the leap just sees a flat dive rather than ending
///     up somewhere different from everyone else.
///
///     Deliberately subscribes to no events. SharedDiveLeapSystem already owns
///     (DiveLeapingComponent, ComponentShutdown) and Robust throws on a second subscription to the
///     same component/event pair - which, because the shared system also runs client-side, took down
///     entity manager startup the moment a player joined. Cleanup is therefore done by sweeping our
///     own tracking dictionary each frame instead, which needs no subscription at all and cannot
///     collide with anything the shared system decides to listen for later.
/// </summary>
public sealed partial class DiveLeapArcSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    ///     Sprite offsets we applied, so they can be restored exactly. Storing the original rather
    ///     than subtracting our own contribution means a leap that ends unexpectedly still leaves
    ///     the sprite where it started.
    /// </summary>
    private readonly Dictionary<EntityUid, Vector2> _originalOffsets = new();

    private readonly List<EntityUid> _finished = new();

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        RestoreFinished();

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DiveLeapingComponent, DiveLeaperComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var leaping, out var leaper, out var sprite))
        {
            var total = (leaping.EndTime - leaping.StartTime).TotalSeconds;
            if (total <= 0)
                continue;

            var progress = (float) Math.Clamp((now - leaping.StartTime).TotalSeconds / total, 0d, 1d);

            if (!_originalOffsets.TryGetValue(uid, out var original))
            {
                original = sprite.Offset;
                _originalOffsets[uid] = original;
            }

            // sin is zero at both ends and peaks in the middle, which is exactly the shape of a hop
            // and costs nothing to evaluate.
            var height = MathF.Sin(progress * MathF.PI) * leaper.ArcHeight;

            sprite.Offset = original + new Vector2(0f, height);
        }
    }

    /// <summary>
    ///     Put back the sprite offset for anything that has stopped leaping, been deleted, or lost
    ///     its sprite since we last looked.
    /// </summary>
    private void RestoreFinished()
    {
        if (_originalOffsets.Count == 0)
            return;

        _finished.Clear();

        foreach (var (uid, original) in _originalOffsets)
        {
            if (!TerminatingOrDeleted(uid) && HasComp<DiveLeapingComponent>(uid))
                continue;

            if (TryComp<SpriteComponent>(uid, out var sprite))
                sprite.Offset = original;

            _finished.Add(uid);
        }

        foreach (var uid in _finished)
        {
            _originalOffsets.Remove(uid);
        }
    }
}
