using Content.Shared.Eye;
using Content.Shared._ClawCommand.Shadekin;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using System.Linq;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Server._ClawCommand.Shadekin;

public sealed partial class EtherealSystem : SharedEtherealSystem
{
    [Dependency] private VisibilitySystem _visibilitySystem = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private EyeSystem _eye = default!;
    [Dependency] private NpcFactionSystem _factions = default!;

    public override void OnStartup(EntityUid uid, EtherealComponent component, MapInitEvent args)
    {
        base.OnStartup(uid, component, args);

        var visibility = EnsureComp<VisibilityComponent>(uid);
        _visibilitySystem.RemoveLayer((uid, visibility), (ushort) VisibilityFlags.Normal, false);
        _visibilitySystem.AddLayer((uid, visibility), (ushort) VisibilityFlags.Ethereal, false);
        _visibilitySystem.RefreshVisibility(uid, visibility);

        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.Ethereal), eye);

        if (TryComp<TemperatureComponent>(uid, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0;

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, 0.8f, stealth);

        SuppressFactions(uid, component, true);

        EnsureComp<PressureImmunityComponent>(uid);
        EnsureComp<RespiratorImmuneComponent>(uid);
        EnsureComp<MovementIgnoreGravityComponent>(uid);
    }

    public override void OnShutdown(EntityUid uid, EtherealComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component, args);

        if (TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibilitySystem.AddLayer((uid, visibility), (ushort) VisibilityFlags.Normal, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (ushort) VisibilityFlags.Ethereal, false);
            _visibilitySystem.RefreshVisibility(uid, visibility);
        }

        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetVisibilityMask(uid, (int) VisibilityFlags.Normal, eye);

        if (TryComp<TemperatureComponent>(uid, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0.1f;

        SuppressFactions(uid, component, false);

        RemComp<StealthComponent>(uid);
        RemComp<PressureImmunityComponent>(uid);
        RemComp<RespiratorImmuneComponent>(uid);
        RemComp<MovementIgnoreGravityComponent>(uid);
    }

    public void SuppressFactions(EntityUid uid, EtherealComponent component, bool set)
    {
        if (set)
        {
            if (!TryComp<NpcFactionMemberComponent>(uid, out var factions))
                return;

            component.SuppressedFactions = factions.Factions.ToList();

            foreach (var faction in factions.Factions)
                _factions.RemoveFaction(uid, faction);
        }
        else
        {
            foreach (var faction in component.SuppressedFactions)
                _factions.AddFaction(uid, faction);

            component.SuppressedFactions.Clear();
        }
    }
}
