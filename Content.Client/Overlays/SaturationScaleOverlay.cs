using System.Numerics;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

/// <summary>
/// Drains the color out of the world while the local player's mood is low.
/// </summary>
public sealed partial class SaturationScaleOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "SaturationScale";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;
    private float _saturation = 0.5f;

    public SaturationScaleOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(Shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.TryGetComponent<SaturationScaleOverlayComponent>(player, out var comp))
            return false;

        _saturation = comp.Saturation;
        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("saturation", _saturation);

        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
