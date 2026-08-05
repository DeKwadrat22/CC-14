// Ported from Goobstation under AGPL-3.0-or-later.
// Original authors: Aiden, Aviu00, Misandry, Spatison, gus.

using System.Numerics;
using Content.Goobstation.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Overlays;

/// <summary>
///     Claw Command - the shader id lives out here rather than on the overlay itself.
///
///     RobustToolbox's prototype-id scanner walks static fields looking for ProtoId values and
///     throws on any it finds inside a generic class, because there is no concrete type argument to
///     resolve them against: "cannot be a static field inside a generic class". That took out the
///     YAML linter and the integration harness entirely - nothing to do with night vision, it just
///     happened to be the one static ProtoId sitting on a generic type. A non-generic holder keeps
///     the field static without tripping the scanner.
/// </summary>
internal static class SwitchableOverlayShaders
{
    public static readonly ProtoId<ShaderPrototype> NightVision = "NightVision";
}

public sealed partial class BaseSwitchableOverlay<TComp> : Overlay where TComp : SwitchableVisionOverlayComponent
{

    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public TComp? Comp = null;

    public bool IsActive = true;

    public bool RestrictToPlayerViewport { get; set; } = false;

    public BaseSwitchableOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototype.Index(SwitchableOverlayShaders.NightVision).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (RestrictToPlayerViewport)
            return args.Viewport.Eye == _eyeManager.CurrentEye;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || Comp is null || !IsActive)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("tint", Comp.Tint);
        _shader.SetParameter("luminance_threshold", Comp.Strength);
        _shader.SetParameter("noise_amount", Comp.Noise);

        var worldHandle = args.WorldHandle;

        var accumulator = Math.Clamp(Comp.PulseAccumulator, 0f, Comp.PulseTime);
        var alpha = Comp.PulseTime <= 0f ? 1f : float.Lerp(1f, 0f, accumulator / Comp.PulseTime);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Comp.Color.WithAlpha(alpha * Comp.OverlayOpacity));
        worldHandle.UseShader(null);
    }
}
