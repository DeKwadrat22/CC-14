using Content.Shared._ClawCommand.Kitsune;
using Robust.Client.GameObjects;

namespace Content.Client._ClawCommand.Kitsune;

public sealed partial class KitsuneFoxFormSystem : VisualizerSystem<KitsuneFoxComponent>
{
    [Dependency]  private SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, KitsuneFoxComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, KitsuneColorVisuals.Body, out var bodyColor, args.Component))
            return;

        if (!AppearanceSystem.TryGetData<Color>(uid, KitsuneColorVisuals.Overlay, out var earColor, args.Component))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), KitsuneColorVisuals.Body, out var bodyLayer, true))
            _sprite.LayerSetColor((uid, sprite), bodyLayer, bodyColor);

        if (_sprite.LayerMapTryGet((uid, sprite), KitsuneColorVisuals.Overlay, out var earLayer, true))
            _sprite.LayerSetColor((uid, sprite), earLayer, earColor);
    }
}
