using System.Numerics;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed partial class ScaleVisualsSystem : SharedScaleVisualsSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(Entity<ScaleVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            args.Sprite == null) return;

        // save the original scale
        ent.Comp.OriginalScale ??= args.Sprite.Scale;
        ent.Comp.OriginalOffset ??= args.Sprite.Offset; // Claw Command

        var vecScale = (Vector2)scale;
        _sprite.SetScale((ent.Owner, args.Sprite), vecScale);

        ApplyBottomPin(ent, args.Sprite); // Claw Command
    }

    /// <summary>
    /// Claw Command - Offsets the sprite so scaling grows it upward from its base instead of outward
    /// from the entity origin. See <see cref="ScaleVisualsComponent.PinBottom"/> for why this matters.
    /// </summary>
    private void ApplyBottomPin(Entity<ScaleVisualsComponent> ent, SpriteComponent sprite)
    {
        var original = ent.Comp.OriginalOffset ?? Vector2.Zero;

        if (!ent.Comp.PinBottom)
        {
            _sprite.SetOffset((ent.Owner, sprite), original);
            return;
        }

        // GetLocalBounds returns bounds with the scale already applied, and Box2.Scale scales about
        // the origin. So the unscaled bottom edge is just bottom / scaleY, and the correction is the
        // gap between where that edge used to sit and where it sits now.
        var scaleY = sprite.Scale.Y;
        if (!float.IsFinite(scaleY) || MathF.Abs(scaleY) < 0.001f)
        {
            _sprite.SetOffset((ent.Owner, sprite), original);
            return;
        }

        var bounds = _sprite.GetLocalBounds((ent.Owner, sprite));
        var correction = bounds.Bottom / scaleY - bounds.Bottom;

        if (!float.IsFinite(correction))
        {
            _sprite.SetOffset((ent.Owner, sprite), original);
            return;
        }

        // Deliberately not rotated by the sprite's own rotation. The offset is applied after rotation
        // in the sprite's local matrix, so an unrotated correction keeps the sprite pinned in screen
        // space, which is what the standing case needs. A lying or buckled mob already has its whole
        // bounding box rotated by CalculateBounds, so its height no longer extends downward and the
        // leftover nudge is well under a pixel at the heights species allow.
        _sprite.SetOffset((ent.Owner, sprite), original + new Vector2(0f, correction));
    }

    // revert to the original scale
    protected override void ResetScale(Entity<ScaleVisualsComponent> ent)
    {
        base.ResetScale(ent);

        if (ent.Comp.OriginalScale != null)
            _sprite.SetScale(ent.Owner, ent.Comp.OriginalScale.Value);

        // Claw Command - the bottom-pin offset has to go back with the scale that caused it.
        if (ent.Comp.OriginalOffset != null)
            _sprite.SetOffset(ent.Owner, ent.Comp.OriginalOffset.Value);
    }
}
