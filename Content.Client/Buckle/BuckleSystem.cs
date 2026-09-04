using Content.Client.Rotation;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Buckle;

internal sealed partial class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private RotationVisualizerSystem _rotationVisualizerSystem = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<StrapComponent, MoveEvent>(OnStrapMoveEvent);
        SubscribeLocalEvent<BuckleComponent, BuckledEvent>(OnBuckledEvent);
        SubscribeLocalEvent<BuckleComponent, UnbuckledEvent>(OnUnbuckledEvent);
        SubscribeLocalEvent<BuckleComponent, AttemptMobCollideEvent>(OnMobCollide);
    }

    private void OnMobCollide(Entity<BuckleComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (ent.Comp.Buckled)
        {
            args.Cancelled = true;
        }
    }

    private void OnStrapMoveEvent(EntityUid uid, StrapComponent component, ref MoveEvent args)
    {
        // I'm moving this to the client-side system, but for the sake of posterity let's keep this comment:
        // > This is mega cursed. Please somebody save me from Mr Buckle's wild ride

        // The nice thing is its still true, this is quite cursed, though maybe not omega cursed anymore.
        // This code is garbage, it doesn't work with rotated viewports. I need to finally get around to reworking
        // sprite rendering for entity layers & direction dependent sorting.

        // Future notes:
        // Right now this doesn't handle: other grids, other grids rotating, the camera rotation changing, and many other fun rotation specific things
        // The entire thing should be a concern of the engine, or something engine helps to implement properly.
        // Give some of the sprite rotations their own drawdepth, maybe as an offset within the rsi, or something like this
        // And we won't ever need to set the draw depth manually

        if (!component.ModifyBuckleDrawDepth)
            return;

        if (args.NewRotation == args.OldRotation)
            return;

        UpdateStrapDrawDepth((uid, component)); // Claw Command
    }

    /// <summary>
    /// Claw Command - Raise the strap over its occupants while it faces screen-north, rather than
    /// sinking the occupants below the strap.
    /// </summary>
    /// <remarks>
    /// Upstream did the latter: buckled mobs were set to <c>strapDrawDepth - 1</c>. For a chair, which
    /// sits at <see cref="Content.Shared.DrawDepth.DrawDepth.Objects"/>, that put the occupant at <see cref="Content.Shared.DrawDepth.DrawDepth.WallTops"/> -
    /// below <see cref="Content.Shared.DrawDepth.DrawDepth.Items"/>, <see cref="Content.Shared.DrawDepth.DrawDepth.SmallObjects"/> and everything else in
    /// that band. So anything lying on or near the chair rendered on top of the person sitting in it.
    ///
    /// There is no single depth that is both under furniture and over items, because furniture sorts
    /// below items by design. Raising the strap instead gets the same "chair back in front of the
    /// occupant" result while leaving the occupant at their normal mob depth.
    ///
    /// Tradeoff: while occupied and facing north, the strap also draws over loose items on its own tile.
    /// That is a far rarer sight than a seated player vanishing behind a dropped screwdriver.
    /// </remarks>
    private void UpdateStrapDrawDepth(Entity<StrapComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var strapSprite))
            return;

        var angle = _xformSystem.GetWorldRotation(ent.Owner) + _eye.CurrentEye.Rotation; // Get true screen position, or close enough
        var raise = angle.GetCardinalDir() == Direction.North && ent.Comp.BuckledEntities.Count > 0;

        if (raise)
        {
            // Only assign if empty, so repeat calls don't overwrite it with the already-raised depth.
            ent.Comp.OriginalDrawDepth ??= strapSprite.DrawDepth;
            _sprite.SetDrawDepth((ent.Owner, strapSprite), (int)Content.Shared.DrawDepth.DrawDepth.OverMobs);
        }
        else if (ent.Comp.OriginalDrawDepth.HasValue)
        {
            _sprite.SetDrawDepth((ent.Owner, strapSprite), ent.Comp.OriginalDrawDepth.Value);
            ent.Comp.OriginalDrawDepth = null;
        }

        // Undo upstream's occupant lowering if anything already applied it, so a mob that was buckled
        // before this ran doesn't stay stuck underneath the item layer.
        foreach (var buckledEntity in ent.Comp.BuckledEntities)
        {
            if (!TryComp<BuckleComponent>(buckledEntity, out var buckle))
                continue;

            RestoreBuckleDrawDepth((buckledEntity, buckle));
        }
    }

    /// <summary>
    /// Claw Command - Puts a buckled entity back on its own drawdepth if something lowered it.
    /// </summary>
    private void RestoreBuckleDrawDepth(Entity<BuckleComponent> ent)
    {
        if (!ent.Comp.OriginalDrawDepth.HasValue)
            return;

        if (TryComp<SpriteComponent>(ent.Owner, out var buckledSprite))
            _sprite.SetDrawDepth((ent.Owner, buckledSprite), ent.Comp.OriginalDrawDepth.Value);

        ent.Comp.OriginalDrawDepth = null;
    }

    /// <summary>
    /// Raise the strap over the buckled entity without needing for the strap entity to rotate/move.
    /// Only do so when the strap is facing screen-local north.
    /// </summary>
    private void OnBuckledEvent(Entity<BuckleComponent> ent, ref BuckledEvent args)
    {
        if (!args.Strap.Comp.ModifyBuckleDrawDepth)
            return;

        UpdateStrapDrawDepth((args.Strap.Owner, args.Strap.Comp)); // Claw Command
    }

    /// <summary>
    /// Was the strap raised over its occupants? Drop it back down once the last one leaves.
    /// </summary>
    private void OnUnbuckledEvent(Entity<BuckleComponent> ent, ref UnbuckledEvent args)
    {
        // Claw Command - always restore the mob, even if the strap opted out of depth changes, in case
        // it was lowered by something else.
        RestoreBuckleDrawDepth(ent);

        if (!args.Strap.Comp.ModifyBuckleDrawDepth)
            return;

        UpdateStrapDrawDepth((args.Strap.Owner, args.Strap.Comp)); // Claw Command
    }

    private void OnAppearanceChange(EntityUid uid, BuckleComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<RotationVisualsComponent>(uid, out var rotVisuals))
            return;

        if (!Appearance.TryGetData<bool>(uid, BuckleVisuals.Buckled, out var buckled, args.Component) ||
            !buckled ||
            args.Sprite == null)
        {
            _rotationVisualizerSystem.SetHorizontalAngle((uid, rotVisuals), rotVisuals.DefaultRotation);
            return;
        }

        // Animate strapping yourself to something at a given angle
        // TODO: Dump this when buckle is better
        _rotationVisualizerSystem.AnimateSpriteRotation(uid, args.Sprite, rotVisuals.HorizontalRotation, 0.125f);
    }
}
