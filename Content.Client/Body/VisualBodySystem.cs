using System.Linq;
using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Body;

public sealed partial class VisualBodySystem : SharedVisualBodySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<VisualOrganComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<VisualOrganComponent, AfterAutoHandleStateEvent>(OnOrganState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotInsertedEvent>(OnMarkingsGotInserted);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotRemovedEvent>(OnMarkingsGotRemoved);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, AfterAutoHandleStateEvent>(OnMarkingsState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent>>(OnMarkingsChangedVisibility);

        Subs.CVar(_cfg, CCVars.AccessibilityClientCensorNudity, OnCensorshipChanged, true);
        Subs.CVar(_cfg, CCVars.AccessibilityServerCensorNudity, OnCensorshipChanged, true);
    }

    private void OnCensorshipChanged(bool value)
    {
        var query = AllEntityQuery<OrganComponent, VisualOrganMarkingsComponent>();
        while (query.MoveNext(out var ent, out var organComp, out var markingsComp))
        {
            if (organComp.Body is not { } body)
                continue;

            RemoveMarkings((ent, markingsComp), body);
            ApplyMarkings((ent, markingsComp), body);
        }
    }

    private void OnOrganGotInserted(Entity<VisualOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyVisual(ent, args.Target);
    }

    private void OnOrganGotRemoved(Entity<VisualOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveVisual(ent, args.Target);
    }

    private void OnOrganState(Entity<VisualOrganComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private void ApplyVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);
    }

    private void RemoveVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    private void OnMarkingsGotInserted(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyMarkings(ent, args.Target);
    }

    private void OnMarkingsGotRemoved(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveMarkings(ent, args.Target);
    }

    private void OnMarkingsState(Entity<VisualOrganMarkingsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganColor(Entity<VisualOrganComponent> ent, Color color)
    {
        base.SetOrganColor(ent, color);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    protected override void SetOrganMarkings(Entity<VisualOrganMarkingsComponent> ent, Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        base.SetOrganMarkings(ent, markings);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganAppearance(Entity<VisualOrganComponent> ent, PrototypeLayerData data)
    {
        base.SetOrganAppearance(ent, data);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private IEnumerable<Marking> AllMarkings(Entity<VisualOrganMarkingsComponent> ent)
    {
        // CLAW COMMAND 14 - Sort markings by a new layering system. This was simple enough. - Cookie
        var allMarkings = ent.Comp.Markings.Values
        .SelectMany(m => m)
        .OrderBy(m => _marking.TryGetMarking(m, out var proto) ? proto.RenderPriority : 0);

        /*
        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                yield return marking;
            }
        }
        */

        foreach (var marking in allMarkings)
        {
            yield return marking;
        }

        var censorNudity = _cfg.GetCVar(CCVars.AccessibilityClientCensorNudity) || _cfg.GetCVar(CCVars.AccessibilityServerCensorNudity);
        if (!censorNudity)
            yield break;

        var group = _prototype.Index(ent.Comp.MarkingData.Group);
        foreach (var layer in ent.Comp.MarkingData.Layers)
        {
            if (!group.Limits.TryGetValue(layer, out var layerLimits))
                continue;

            if (layerLimits.NudityDefault.Count < 1)
                continue;

            var markings = ent.Comp.Markings.GetValueOrDefault(layer) ?? [];
            if (markings.Any(marking => _marking.TryGetMarking(marking, out var proto) && proto.BodyPart == layer))
                continue;

            foreach (var marking in layerLimits.NudityDefault)
            {
                yield return new(marking, 1);
            }
        }
    }

    private void ApplyMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        var applied = new List<Marking>();
        foreach (var marking in AllMarkings(ent))
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!_sprite.LayerMapTryGet(target, proto.BodyPart, out var index, true))
                continue;

            ent.Comp.MarkingsDisplacement.TryGetValue(proto.BodyPart, out var displacement);

            for (var i = 0; i < proto.Sprites.Count; i++)
            {
                var sprite = proto.Sprites[i];

                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerId = $"{proto.ID}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
                {
                    var spriteLayer = _sprite.AddLayer(target, sprite, index + i + 1);
                    _sprite.LayerMapSet(target, layerId, spriteLayer);
                    _sprite.LayerSetSprite(target, layerId, rsi);
                }

                if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                    _sprite.LayerSetColor(target, layerId, marking.MarkingColors[i]);
                else
                    _sprite.LayerSetColor(target, layerId, Color.White);

                if (displacement != null && proto.CanBeDisplaced)
                    _displacement.TryAddDisplacement(displacement, (target, target.Comp), index + i + 1, layerId, out _);
            }

            applied.Add(marking);
        }
        ent.Comp.AppliedMarkings = applied;
    }
    // CLAW COMMAND 14 - Markings replacement system
    private void ApplyMarkings(Entity<VisualOrganMarkingsComponent> ent, EntityUid target)
    {
        var applied = new List<Marking>();
        var layerOffsets = new Dictionary<HumanoidVisualLayers, int>();
        foreach (var marking in AllMarkings(ent))
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!_sprite.LayerMapTryGet(target, proto.BodyPart, out _, true))
                continue;

            for (var i = 0; i < proto.Sprites.Count; i++)
            {
                var sprite = proto.Sprites[i];

                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                // Determine which layer this sprite should go into
                var layerSlot = proto.BodyPart;
                if (proto.Layering != null &&
                    proto.Layering.TryGetValue(rsi.RsiState, out var layerName) &&
                    Enum.TryParse<HumanoidVisualLayers>(layerName, out var parsedLayer))
                {
                    layerSlot = parsedLayer;
                }

                if (!_sprite.LayerMapTryGet(target, layerSlot, out var index, true))
                    continue;

                var offset = layerOffsets.GetValueOrDefault(layerSlot, 0);
                var layerId = $"{proto.ID}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
                {
                    var targLayerAdj = index + offset + 1;
                    var layer = _sprite.AddLayer(target, sprite, targLayerAdj);
                    _sprite.LayerMapSet(target, layerId, layer);
                    _sprite.LayerSetSprite(target, layerId, rsi);

                    if (ent.Comp.InitiallyHiddenLayers.Contains(proto.BodyPart))
                         _sprite.LayerSetVisible(target, layerId, false);

                    // Claw Command - hide marking if its layer is currently hidden by clothing
                    if (ent.Comp.HideableLayers.Contains(proto.BodyPart) &&
                        TryComp<HideableHumanoidLayersComponent>(target, out var hideComp) &&
                        hideComp.HiddenLayers.ContainsKey(proto.BodyPart))
                        _sprite.LayerSetVisible(target, layerId, false);
                }

                if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                    _sprite.LayerSetColor(target, layerId, marking.MarkingColors[i]);
                else
                    _sprite.LayerSetColor(target, layerId, Color.White);

                layerOffsets[layerSlot] = offset + 1;
            }

            applied.Add(marking);
        }
        ent.Comp.AppliedMarkings = applied;
    }

    private void RemoveMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        foreach (var marking in ent.Comp.AppliedMarkings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            foreach (var sprite in proto.Sprites)
            {
                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerId = $"{proto.ID}-{rsi.RsiState}";

                // If this marking is one that can be displaced, we need to remove the displacement as well; otherwise
                // altering a marking at runtime can lead to the renderer falling over.
                // The Vulps must be shaved.
                // (https://github.com/space-wizards/space-station-14/issues/40135).
                if (proto.CanBeDisplaced)
                    _displacement.EnsureDisplacementIsNotOnSprite((target, target.Comp), layerId);

                if (!_sprite.LayerMapTryGet(target, layerId, out var index, false))
                    continue;

                _sprite.LayerMapRemove(target, layerId);
                _sprite.RemoveLayer(target, index);
            }
        }
    }

    private void OnMarkingsChangedVisibility(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent> args)
    {
        if (!ent.Comp.HideableLayers.Contains(args.Args.Layer))
            return;

        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                if (proto.BodyPart != args.Args.Layer && !(ent.Comp.DependentHidingLayers.TryGetValue(args.Args.Layer, out var dependent) && dependent.Contains(proto.BodyPart)))
                    continue;

                foreach (var sprite in proto.Sprites)
                {
                    DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (!_sprite.LayerMapTryGet(args.Body.Owner, layerId, out var index, true))
                        continue;

                    _sprite.LayerSetVisible(args.Body.Owner, index, args.Args.Visible);
                }
            }
        }
    }
}
