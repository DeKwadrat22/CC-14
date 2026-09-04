using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared._ClawCommand.Traits.Components;

namespace Content.Shared._ClawCommand.Traits.Systems;

/// <summary>
///     Handles threshold adjustments from HealthCritAdjust, HealthDeadAdjust,
///     StaminaCapAdjust, and InjurySlowAdjust components at startup.
/// </summary>
public sealed partial class EntityThresholdAdjustSystem : EntitySystem
{
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealthDeadAdjustComponent, ComponentStartup>(OnDeadStartup);
        SubscribeLocalEvent<StaminaCapAdjustComponent, ComponentStartup>(OnStaminaStartup);
        SubscribeLocalEvent<HealthCritAdjustComponent, ComponentStartup>(OnCritStartup);
        SubscribeLocalEvent<InjurySlowAdjustComponent, ComponentStartup>(OnInjurySlowStartup);
        SubscribeLocalEvent<BloodRegenBoostComponent, ComponentStartup>(OnBloodRegenStartup);
    }

    private void OnStaminaStartup(EntityUid uid, StaminaCapAdjustComponent comp, ComponentStartup args)
    {
        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        stamina.CritThreshold += comp.Offset;
    }

    private void OnDeadStartup(EntityUid uid, HealthDeadAdjustComponent comp, ComponentStartup args)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        var current = _threshold.GetThresholdForState(uid, Mobs.MobState.Dead, thresholds);
        if (current != 0)
            _threshold.SetMobStateThreshold(uid, current + comp.Offset, Mobs.MobState.Dead);
    }

    private void OnInjurySlowStartup(EntityUid uid, InjurySlowAdjustComponent comp, ComponentStartup args)
    {
        if (!TryComp<SlowOnDamageComponent>(uid, out var slowComp))
            return;

        var adjusted = new Dictionary<FixedPoint2, float>();
        foreach (var (thresh, mod) in slowComp.SpeedModifierThresholds)
        {
            var shifted = FixedPoint2.Max(FixedPoint2.Zero, thresh + comp.ThresholdShift);
            adjusted[shifted] = mod;
        }

        slowComp.SpeedModifierThresholds = adjusted;
        _speedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnCritStartup(EntityUid uid, HealthCritAdjustComponent comp, ComponentStartup args)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        var current = _threshold.GetThresholdForState(uid, Mobs.MobState.Critical, thresholds);
        if (current != 0)
            _threshold.SetMobStateThreshold(uid, current + comp.Offset, Mobs.MobState.Critical);
    }

    private void OnBloodRegenStartup(EntityUid uid, BloodRegenBoostComponent comp, ComponentStartup args)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        bloodstream.BloodRefreshAmount *= comp.RegenMultiplier;
    }
}
