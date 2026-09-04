// claw command - IPC
using Content.Server.Lightning;
using Content.Server.Popups;
using Content.Shared.PowerCell;
using Content.Server.Silicon.Charge;
using Content.Shared.Silicon.DeadStartupButton;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Electrocution;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Silicon.DeadStartupButtonSystem;

public sealed partial class DeadStartupButtonSystem : SharedDeadStartupButtonSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private SiliconChargeSystem _siliconChargeSystem = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeadStartupButtonComponent, OnDoAfterButtonPressedEvent>(OnDoAfter);
        SubscribeLocalEvent<DeadStartupButtonComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<DeadStartupButtonComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDoAfter(EntityUid uid, DeadStartupButtonComponent comp, OnDoAfterButtonPressedEvent args)
    {
        if (args.Handled || args.Cancelled
            || !TryComp<MobStateComponent>(uid, out var mobStateComponent)
            || !_mobState.IsDead(uid, mobStateComponent)
            || !TryComp<MobThresholdsComponent>(uid, out var mobThresholdsComponent)
            || !TryComp<DamageableComponent>(uid, out var damageable)
            || !_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, mobThresholdsComponent))
            return;

        if (_damageable.GetPositiveDamage((uid, damageable)).GetTotal() < criticalThreshold)
            _mobState.ChangeMobState(uid, MobState.Alive, mobStateComponent);
        else
        {
            _audio.PlayPvs(comp.BuzzSound, uid, AudioHelpers.WithVariation(0.05f, _robustRandom));
            _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-failed", ("target", MetaData(uid).EntityName)), uid);
            Spawn("EffectSparks", Transform(uid).Coordinates);
        }
    }

    private void OnElectrocuted(EntityUid uid, DeadStartupButtonComponent comp, ElectrocutedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobStateComponent)
            || !_mobState.IsDead(uid, mobStateComponent)
            || !_siliconChargeSystem.TryGetSiliconBattery(uid, out var bateria))
            return;

        var charge = _battery.GetCharge(bateria.Value.AsNullable());
        if (charge <= 0)
            return;

        _lightning.ShootRandomLightnings(uid, 2, 4);
        _powerCell.TryUseCharge(uid, charge);
    }

    private void OnMobStateChanged(EntityUid uid, DeadStartupButtonComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        _popup.PopupEntity(Loc.GetString("dead-startup-system-reboot-success", ("target", MetaData(uid).EntityName)), uid);
        _audio.PlayPvs(comp.Sound, uid);
    }
}
