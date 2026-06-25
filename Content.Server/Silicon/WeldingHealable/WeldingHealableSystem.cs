// claw command - IPC
using Content.Server.Silicon.WeldingHealing;
using Content.Shared.Chemistry.Components;
using Content.Shared.Silicon.WeldingHealing;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Silicon.WeldingHealable;

public sealed partial class WeldingHealableSystem : SharedWeldingHealableSystem
{
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WeldingHealableComponent, InteractUsingEvent>(Repair);
        SubscribeLocalEvent<WeldingHealableComponent, SiliconRepairFinishedEvent>(OnRepairFinished);
    }

    private void OnRepairFinished(EntityUid uid, WeldingHealableComponent healableComponent, SiliconRepairFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null
            || !TryComp<DamageableComponent>(args.Target, out var damageable)
            || !TryComp<WeldingHealingComponent>(args.Used, out var component)
            // _ClawCommand: upstream split DamageContainerID off DamageableComponent into InjurableComponent.DamageContainer (ProtoId<DamageContainerPrototype>?).
            || !TryComp<InjurableComponent>(args.Target, out var injurable)
            || injurable.DamageContainer is null
            || !component.DamageContainers.Contains(injurable.DamageContainer.Value)
            || !HasDamage((args.Target.Value, damageable), component)
            || !TryComp<WelderComponent>(args.Used, out var welder))
            return;

        _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);

        Entity<SolutionComponent>? sol = new();
        // _ClawCommand: upstream's new ResolveSolution takes EntityUid directly (or Entity<SolutionManagerComponent?>); it resolves the component itself.
        if (!_solutionContainer.ResolveSolution((EntityUid)args.Used, welder.FuelSolutionName, ref sol, out _))
            return;
        _solutionContainer.RemoveReagent(sol.Value, welder.FuelReagent, component.FuelCost);

        var str = Loc.GetString("comp-repairable-repair",
            ("target", uid),
            ("tool", args.Used!));
        _popup.PopupEntity(str, uid, args.User);

        if (!args.Used.HasValue)
            return;

        args.Handled = _toolSystem.UseTool
            (args.Used.Value,
            args.User,
            uid,
            args.Delay,
            component.QualityNeeded,
            new SiliconRepairFinishedEvent
            {
                Delay = args.Delay
            });
    }

    private async void Repair(EntityUid uid, WeldingHealableComponent healableComponent, InteractUsingEvent args)
    {
        if (args.Handled
            || !TryComp(args.Used, out WeldingHealingComponent? component)
            || !TryComp(args.Target, out DamageableComponent? damageable)
            // _ClawCommand: upstream split DamageContainerID off DamageableComponent into InjurableComponent.DamageContainer (ProtoId<DamageContainerPrototype>?).
            || !TryComp(args.Target, out InjurableComponent? injurable)
            || injurable.DamageContainer is null
            || !component.DamageContainers.Contains(injurable.DamageContainer.Value)
            || !HasDamage((args.Target, damageable), component)
            || !_toolSystem.HasQuality(args.Used, component.QualityNeeded)
            || args.User == args.Target && !component.AllowSelfHeal)
            return;

        float delay = args.User == args.Target
            ? component.DoAfterDelay * component.SelfHealPenalty
            : component.DoAfterDelay;

        args.Handled = _toolSystem.UseTool(
            args.Used,
            args.User,
            args.Target,
            delay,
            component.QualityNeeded,
            new SiliconRepairFinishedEvent
            {
                Delay = delay,
            });
    }

    private bool HasDamage(Entity<DamageableComponent> damageable, WeldingHealingComponent healable)
    {
        if (healable.Damage.DamageDict is null)
            return false;

        var positiveDamage = _damageableSystem.GetPositiveDamage(damageable);
        foreach (var type in healable.Damage.DamageDict)
            if (positiveDamage.DamageDict.TryGetValue(type.Key, out var value) && value > 0)
                return true;

        return false;
    }
}
