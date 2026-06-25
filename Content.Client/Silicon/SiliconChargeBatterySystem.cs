// Claw Command - IPC battery alert client-side update
using Content.Shared.Alert;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicon.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Silicon;

/// <summary>
///     Handles client-side battery alert updates for Silicon entities (like IPCs)
///     that don't have BorgChassisComponent.
/// </summary>
public sealed partial class SiliconChargeBatterySystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private static readonly TimeSpan AlertUpdateDelay = TimeSpan.FromSeconds(0.5f);
    private TimeSpan _nextAlertUpdate = TimeSpan.Zero;

    private EntityQuery<SiliconComponent> _siliconQuery;
    private EntityQuery<PowerCellSlotComponent> _slotQuery;
    private EntityQuery<BorgChassisComponent> _chassisQuery;

    public override void Initialize()
    {
        base.Initialize();

        _siliconQuery = GetEntityQuery<SiliconComponent>();
        _slotQuery = GetEntityQuery<PowerCellSlotComponent>();
        _chassisQuery = GetEntityQuery<BorgChassisComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } localPlayer)
            return;

        var curTime = _timing.CurTime;
        if (curTime < _nextAlertUpdate)
            return;

        _nextAlertUpdate = curTime + AlertUpdateDelay;

        // Only handle Silicons that are NOT borgs (borgs are handled by BorgSystem.Battery)
        if (!_siliconQuery.TryComp(localPlayer, out var silicon)
            || !_slotQuery.TryComp(localPlayer, out _)
            || _chassisQuery.HasComp(localPlayer))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(localPlayer, out var battery))
        {
            _alerts.ShowAlert(localPlayer, silicon.NoBatteryAlert);
            return;
        }

        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);

        if (chargeLevel == 0 && _powerCell.HasDrawCharge(localPlayer))
            chargeLevel = 1;

        _alerts.ShowAlert(localPlayer, silicon.BatteryAlert, chargeLevel);
    }
}
