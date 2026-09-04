// Ported from Goobstation under AGPL-3.0-or-later.
// Original authors: Aiden, Aviu00, Misandry, Spatison, gus.

using Content.Client.Overlays;
using Content.Goobstation.Shared.Overlays;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client.Overlays;

public sealed partial class NightVisionSystem : EquipmentHudSystem<NightVisionOverlayComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private ILightManager _lightManager = default!;

    private BaseSwitchableOverlay<NightVisionOverlayComponent> _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionOverlayComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _overlay = new BaseSwitchableOverlay<NightVisionOverlayComponent>();
    }

    protected override void OnRefreshComponentHud(Entity<NightVisionOverlayComponent> ent,
        ref RefreshEquipmentHudEvent<NightVisionOverlayComponent> args)
    {
        if (!ent.Comp.IsEquipment)
            base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void OnRefreshEquipmentHud(Entity<NightVisionOverlayComponent> ent,
        ref InventoryRelayedEvent<RefreshEquipmentHudEvent<NightVisionOverlayComponent>> args)
    {
        // Don't route through base.OnRefreshComponentHud — the override there skips on IsEquipment=true,
        // which would silently drop equipment goggles from the refresh list.
        if (!ent.Comp.IsEquipment)
            return;

        args.Args.Active = true;
        args.Args.Components.Add(ent.Comp);
    }

    private void OnToggle(Entity<NightVisionOverlayComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionOverlayComponent> args)
    {
        base.UpdateInternal(args);

        var active = false;
        NightVisionOverlayComponent? nvComp = null;
        foreach (var comp in args.Components)
        {
            if (comp.IsActive || comp.PulseTime > 0f && comp.PulseAccumulator < comp.PulseTime)
                active = true;
            else
                continue;

            if (comp.DrawOverlay)
            {
                if (nvComp == null)
                    nvComp = comp;
                else if (nvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                    nvComp = comp;
            }

            if (active && nvComp is { PulseTime: <= 0 })
                break;
        }

        UpdateNightVision(active);
        UpdateOverlay(nvComp);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        UpdateNightVision(false);
        UpdateOverlay(null);
    }

    private void UpdateNightVision(bool active)
    {
        _lightManager.DrawLighting = !active;
    }

    private void UpdateOverlay(NightVisionOverlayComponent? nvComp)
    {
        _overlay.Comp = nvComp;

        switch (nvComp)
        {
            case not null when !_overlayMan.HasOverlay<BaseSwitchableOverlay<NightVisionOverlayComponent>>():
                _overlayMan.AddOverlay(_overlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_overlay);
                break;
        }

        if (_overlayMan.TryGetOverlay<BaseSwitchableOverlay<ThermalVisionComponent>>(out var overlay))
            overlay.IsActive = nvComp == null;
    }
}
