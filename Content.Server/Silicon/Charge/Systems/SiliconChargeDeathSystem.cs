// claw command - IPC
using Content.Shared.Power.Components;
using Content.Shared.Silicon.Systems;
using Content.Shared.Bed.Sleep;
using Content.Server.Silicon.Charge;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Silicon.Death;

public sealed partial class SiliconDeathSystem : EntitySystem
{
    [Dependency] private SleepingSystem _sleep = default!;
    [Dependency] private SiliconChargeSystem _silicon = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconDownOnDeadComponent, SiliconChargeStateUpdateEvent>(OnSiliconChargeStateUpdate);
    }

    private void OnSiliconChargeStateUpdate(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, SiliconChargeStateUpdateEvent args)
    {
        if (!_silicon.TryGetSiliconBattery(uid, out var batteryEntity))
        {
            SiliconDead(uid, siliconDeadComp, batteryEntity, uid);
            return;
        }

        if (args.ChargePercent == 0 && siliconDeadComp.Dead)
            return;

        if (args.ChargePercent == 0 && !siliconDeadComp.Dead)
            SiliconDead(uid, siliconDeadComp, batteryEntity, uid);
        else if (args.ChargePercent != 0 && siliconDeadComp.Dead)
            SiliconUnDead(uid, siliconDeadComp, batteryEntity, uid);
    }

    private void SiliconDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<BatteryComponent>? batteryEntity, EntityUid batteryUid)
    {
        var deadEvent = new SiliconChargeDyingEvent(uid, batteryEntity?.Comp, batteryUid);
        RaiseLocalEvent(uid, deadEvent);

        if (deadEvent.Cancelled)
            return;

        EnsureComp<SleepingComponent>(uid);
        _statusEffect.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromDays(1));

        siliconDeadComp.Dead = true;

        RaiseLocalEvent(uid, new SiliconChargeDeathEvent(uid, batteryEntity?.Comp, batteryUid));
    }

    private void SiliconUnDead(EntityUid uid, SiliconDownOnDeadComponent siliconDeadComp, Entity<BatteryComponent>? batteryEntity, EntityUid batteryUid)
    {
        _statusEffect.TryRemoveStatusEffect(uid, SleepingSystem.StatusEffectForcedSleeping);
        _sleep.TryWaking(uid, true);

        siliconDeadComp.Dead = false;

        RaiseLocalEvent(uid, new SiliconChargeAliveEvent(uid, batteryEntity?.Comp, batteryUid));
    }
}

/// <summary>
///     A cancellable event raised when a Silicon is about to go down due to charge.
/// </summary>
public sealed partial class SiliconChargeDyingEvent : CancellableEntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeDyingEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}

/// <summary>
///     An event raised after a Silicon has gone down due to charge.
/// </summary>
public sealed partial class SiliconChargeDeathEvent : EntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeDeathEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}

/// <summary>
///     An event raised after a Silicon has reawoken due to an increase in charge.
/// </summary>
public sealed partial class SiliconChargeAliveEvent : EntityEventArgs
{
    public EntityUid SiliconUid { get; }
    public BatteryComponent? BatteryComp { get; }
    public EntityUid BatteryUid { get; }

    public SiliconChargeAliveEvent(EntityUid siliconUid, BatteryComponent? batteryComp, EntityUid batteryUid)
    {
        SiliconUid = siliconUid;
        BatteryComp = batteryComp;
        BatteryUid = batteryUid;
    }
}
