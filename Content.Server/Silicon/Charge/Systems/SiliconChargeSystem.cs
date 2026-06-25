// claw command - IPC
using Robust.Shared.Random;
using Content.Shared.Silicon.Components;
using Content.Shared.Power.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature.Components;
using Content.Shared.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Silicon.Systems;
using Content.Shared.Movement.Systems;
using Content.Server.Body.Components;
using Robust.Shared.Containers;
using Content.Shared.Mind.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.PowerCell;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using Content.Shared.CCVar;
using Content.Shared.PowerCell.Components;
using Content.Shared.Alert;

namespace Content.Server.Silicon.Charge;

public sealed partial class SiliconChargeSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _moveMod = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BatterySystem _battery = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, ComponentStartup>(OnSiliconStartup);
    }

    public bool TryGetSiliconBattery(EntityUid silicon, [NotNullWhen(true)] out Entity<BatteryComponent>? batteryEntity)
    {
        batteryEntity = null;
        if (!HasComp<SiliconComponent>(silicon))
            return false;

        if (TryComp<BatteryComponent>(silicon, out var batteryComp))
        {
            batteryEntity = new Entity<BatteryComponent>(silicon, batteryComp);
            return true;
        }

        if (_powerCell.TryGetBatteryFromSlot(silicon, out batteryEntity))
            return true;

        return false;
    }

    private void OnSiliconStartup(EntityUid uid, SiliconComponent component, ComponentStartup args)
    {
        if (!HasComp<PowerCellSlotComponent>(uid))
            return;

        if (component.EntityType.GetType() != typeof(SiliconType))
            DebugTools.Assert("SiliconComponent.EntityType is not a SiliconType enum.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SiliconComponent>();
        while (query.MoveNext(out var silicon, out var siliconComp))
        {
            if (_mobState.IsDead(silicon)
                || !siliconComp.BatteryPowered)
                continue;

            if (siliconComp.EntityType.Equals(SiliconType.Npc))
            {
                var updateTime = _config.GetCVar(CCVars.SiliconNpcUpdateTime);
                if (_timing.CurTime - siliconComp.LastDrainTime < TimeSpan.FromSeconds(updateTime))
                    continue;

                siliconComp.LastDrainTime = _timing.CurTime;
            }

            if (!TryGetSiliconBattery(silicon, out var batteryComp))
            {
                UpdateChargeState(silicon, 0, siliconComp);
                if (_alerts.IsShowingAlert(silicon, siliconComp.BatteryAlert))
                {
                    _alerts.ClearAlert(silicon, siliconComp.BatteryAlert);
                    _alerts.ShowAlert(silicon, siliconComp.NoBatteryAlert);
                }
                continue;
            }

            if (TryComp<MindContainerComponent>(silicon, out var mindContComp)
                && !mindContComp.HasMind)
                continue;

            var drainRate = siliconComp.DrainPerSecond;
            var drainRateFinalAddi = 0f;

            if (!siliconComp.EntityType.Equals(SiliconType.Npc))
                drainRateFinalAddi += SiliconHeatEffects(silicon, siliconComp, frameTime) - 1;

            drainRate += Math.Clamp(drainRateFinalAddi, drainRate * -0.9f, batteryComp.Value.Comp.MaxCharge / 240);

            _powerCell.TryUseCharge(silicon, frameTime * drainRate);

            var chargePercent = (short) MathF.Round(_battery.GetCharge(batteryComp.Value.AsNullable()) / batteryComp.Value.Comp.MaxCharge * 10f);

            UpdateChargeState(silicon, chargePercent, siliconComp);
        }
    }

    public void UpdateChargeState(EntityUid uid, short chargePercent, SiliconComponent component)
    {
        component.ChargeState = chargePercent;

        RaiseLocalEvent(uid, new SiliconChargeStateUpdateEvent(chargePercent));

        _moveMod.RefreshMovementSpeedModifiers(uid);

        if (_alerts.IsShowingAlert(uid, component.NoBatteryAlert) && chargePercent != 0)
        {
            _alerts.ClearAlert(uid, component.NoBatteryAlert);
            _alerts.ShowAlert(uid, component.BatteryAlert, chargePercent);
        }
    }

    private float SiliconHeatEffects(EntityUid silicon, SiliconComponent siliconComp, float frameTime)
    {
        if (!TryComp<TemperatureComponent>(silicon, out var temperComp)
            || !TryComp<ThermalRegulatorComponent>(silicon, out var thermalComp))
            return 0;

        var upperThresh = thermalComp.NormalBodyTemperature + thermalComp.ThermalRegulationTemperatureThreshold;
        var upperThreshHalf = thermalComp.NormalBodyTemperature + thermalComp.ThermalRegulationTemperatureThreshold * 0.5f;

        if (temperComp.CurrentTemperature > upperThreshHalf)
        {
            var hotTempMulti = Math.Min(temperComp.CurrentTemperature / upperThreshHalf, 4);

            siliconComp.OverheatAccumulator += frameTime;
            if (!(siliconComp.OverheatAccumulator >= 5))
                return hotTempMulti;

            siliconComp.OverheatAccumulator -= 5;

            if (!TryComp<FlammableComponent>(silicon, out var flamComp)
                || flamComp is { OnFire: true }
                || !TryComp<TemperatureDamageComponent>(silicon, out var tempDmgComp)
                || !(temperComp.CurrentTemperature > tempDmgComp.HeatDamageThreshold))
                return hotTempMulti;

            _popup.PopupEntity(Loc.GetString("silicon-overheating"), silicon, silicon, PopupType.MediumCaution);

            // Fire ignition handled by temperature damage system
            return hotTempMulti;
        }

        if (temperComp.CurrentTemperature < thermalComp.NormalBodyTemperature)
            return 0.5f + temperComp.CurrentTemperature / thermalComp.NormalBodyTemperature * 0.5f;

        return 0;
    }
}
