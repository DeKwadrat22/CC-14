// Ported from Goobstation under AGPL-3.0-or-later.
// Original authors: Aiden, Aviu00, Misandry, Spatison, gus, Armok.
// Fork adaptations: stripped BodyComponent.ThermalVisibility / StealthComponent.ThermalsImmune checks
// (Goob-specific fields not present in this fork); thermals see every BodyComponent and ignore stealth.

using System.Linq;
using System.Numerics;
using Content.Goobstation.Shared.Overlays;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Overlays;

public sealed partial class ThermalVisionOverlay : Overlay
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEyeManager _eyeManager = default!;

    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly ContainerSystem _container;
    private readonly SharedPointLightSystem _light;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<ThermalVisionRenderEntry> _entries = [];

    private EntityUid? _lightEntity;

    public float LightRadius;

    public ThermalVisionComponent? Comp;

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _light = _entity.System<SharedPointLightSystem>();

        ZIndex = -1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye == _eyeManager.CurrentEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || Comp is null)
            return;

        var worldHandle = args.WorldHandle;
        var eye = args.Viewport.Eye;

        if (eye == null)
            return;

        var player = _player.LocalEntity;

        if (!_entity.TryGetComponent(player, out TransformComponent? playerXform))
            return;

        var accumulator = Math.Clamp(Comp.PulseAccumulator, 0f, Comp.PulseTime);
        var alpha = Comp.PulseTime <= 0f ? 1f : float.Lerp(1f, 0f, accumulator / Comp.PulseTime);

        if (LightRadius > 0)
        {
            _lightEntity ??= _entity.SpawnAttachedTo(null, playerXform.Coordinates);
            _transform.SetParent(_lightEntity.Value, player.Value);
            var light = _entity.EnsureComponent<PointLightComponent>(_lightEntity.Value);
            _light.SetRadius(_lightEntity.Value, LightRadius, light);
            _light.SetEnergy(_lightEntity.Value, alpha, light);
            _light.SetColor(_lightEntity.Value, Comp.Color, light);
        }
        else
            ResetLight();

        var mapId = eye.Position.MapId;
        var eyeRot = eye.Rotation;

        _entries.Clear();
        var entities = _entity.EntityQueryEnumerator<BodyComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (!CanSee(sprite))
                continue;

            var entity = uid;

            if (_container.TryGetOuterContainer(uid, xform, out var container))
            {
                var owner = container.Owner;
                if (_entity.TryGetComponent<SpriteComponent>(owner, out var ownerSprite)
                    && _entity.TryGetComponent<TransformComponent>(owner, out var ownerXform))
                {
                    entity = owner;
                    sprite = ownerSprite;
                    xform = ownerXform;
                }
            }

            if (_entries.Any(e => e.Ent.Owner == entity))
                continue;

            _entries.Add(new ThermalVisionRenderEntry((entity, sprite, xform), mapId, eyeRot));
        }

        foreach (var entry in _entries)
        {
            Render(entry.Ent, entry.Map, worldHandle, entry.EyeRot, Comp.Color, Comp.ThermalShader, alpha);
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
    }

    private void Render(Entity<SpriteComponent, TransformComponent> ent,
        MapId? map,
        DrawingHandleWorld handle,
        Angle eyeRot,
        Color color,
        string? shader,
        float alpha)
    {
        var (uid, sprite, xform) = ent;
        if (xform.MapID != map || !CanSee(sprite))
            return;

        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

        var originalColor = sprite.Color;
        Dictionary<int, (ShaderInstance? shader, Color color)> layerData = new();
        if (shader != null)
        {
            var allLayers = sprite.AllLayers.ToList();
            for (var i = 0; i < allLayers.Count; i++)
            {
                if (allLayers[i] is not SpriteComponent.Layer { Visible: true } layer)
                    continue;

                if (layer.ShaderPrototype?.Id is "DisplacedDraw" or "DisplacedStencilDraw")
                    _sprite.LayerSetVisible((uid, sprite), i, false);

                layerData[i] = (layer.Shader, layer.Color);
                layer.Shader = null;
                _sprite.LayerSetColor(layer, Color.White.WithAlpha(layer.Color.A));
            }

            _sprite.SetColor((uid, sprite), Color.White.WithAlpha(alpha));
            handle.UseShader(_protoMan.Index<ShaderPrototype>(shader).Instance());
        }
        else
            _sprite.SetColor((uid, sprite), color.WithAlpha(alpha));
        _sprite.RenderSprite((uid, sprite), handle, eyeRot, rotation, position);
        _sprite.SetColor((uid, sprite), originalColor);
        handle.UseShader(null);
        foreach (var (key, value) in layerData)
        {
            ((SpriteComponent.Layer) sprite[key]).Shader = value.shader;
            _sprite.LayerSetColor((uid, sprite), key, value.color);
            _sprite.LayerSetVisible((uid, sprite), key, true);
        }
    }

    private static bool CanSee(SpriteComponent sprite) => sprite.Visible;

    public void ResetLight(bool checkFirstTimePredicted = true)
    {
        if (_lightEntity == null || checkFirstTimePredicted && !_timing.IsFirstTimePredicted)
            return;

        _entity.DeleteEntity(_lightEntity);
        _lightEntity = null;
    }
}

public record struct ThermalVisionRenderEntry(
    Entity<SpriteComponent, TransformComponent> Ent,
    MapId? Map,
    Angle EyeRot);
