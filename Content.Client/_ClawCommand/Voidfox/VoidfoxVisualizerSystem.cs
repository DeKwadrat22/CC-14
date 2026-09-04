using Content.Shared._ClawCommand.Voidfox;
using Robust.Client.GameObjects;

namespace Content.Client._ClawCommand.Voidfox;

/// <summary>
/// Claw Command - Switches the voidfox sprite based on the composite visual state
/// resolved server-side from cockpit/ladder/landed/boost flags.
/// </summary>
public sealed class VoidfoxVisualizerSystem : VisualizerSystem<VoidfoxComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VoidfoxComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<VoidfoxVisualState>(uid, VoidfoxVisuals.State, out var state, args.Component))
            return;

        var rsiState = state switch
        {
            VoidfoxVisualState.Idle => "idle",
            VoidfoxVisualState.ExhaustBoost => "exhaust_boost",
            VoidfoxVisualState.OpenLanded => "open_landed",
            VoidfoxVisualState.OpenLandedNoLadder => "open_landed_no_ladder",
            VoidfoxVisualState.LandedClosedNoLadder => "landed_closed_no_ladder",
            _ => "open_landed",
        };

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, rsiState);
    }
}
