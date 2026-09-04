using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.Events;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;
using Content.Shared.Anomaly;
using Robust.Shared.Audio.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage; using Content.Shared.Damage.Systems; using Content.Shared.Damage.Components; // Claw Command - damage split into Systems/Components
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Server.Lightning;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Throwing;
using Content.Server.Chemistry.EntitySystems; // Claw Command - SolutionContainerSystem namespace
using Content.Server.Fluids.EntitySystems;

namespace Content.Server.Abilities.Psionics;

public sealed partial class AnomalyPowerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GlimmerSystem _glimmerSystem = default!;
    [Dependency] private SharedAnomalySystem _anomalySystem = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPsionicAbilitiesSystem _psionics = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private EmpSystem _emp = default!;
    [Dependency] private ExplosionSystem _boom = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsionicComponent, AnomalyPowerActionEvent>(OnPowerUsed);
    }

    private void OnPowerUsed(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        if (!_psionics.OnAttemptPowerUse(args.Performer, args.Settings.PowerName, args.Settings.ManaCost, args.Settings.CheckInsulation))
            return;

        var overcharged = args.Settings.DoSupercritical ? _glimmerSystem.Glimmer * component.CurrentAmplification
            > Math.Min(args.Settings.SupercriticalThreshold * component.CurrentDampening, args.Settings.MaxSupercriticalThreshold)
            : false;

        // Behold the wall of nullable logic gates.
        DoBluespaceAnomalyEffects(uid, component, args, overcharged);
        DoElectricityAnomalyEffects(uid, component, args, overcharged);
        DoEntityAnomalyEffects(uid, component, args, overcharged);
        DoExplosionAnomalyEffects(uid, component, args, overcharged);
        DoGasProducerAnomalyEffects(uid, component, args, overcharged);
        DoGravityAnomalyEffects(uid, component, args, overcharged);
        DoInjectionAnomalyEffects(uid, component, args, overcharged);
        DoPuddleAnomalyEffects(uid, component, args, overcharged);
        DoPyroclasticAnomalyEffects(uid, component, args, overcharged);
        DoAnomalySounds(uid, component, args, overcharged);
        DoGlimmerEffects(uid, component, args, overcharged);

        if (overcharged)
            DoOverchargedEffects(uid, component, args);

        args.Handled = true;
    }

    public void DoAnomalySounds(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args, bool overcharged = false)
    {
        if (overcharged && args.Settings.SupercriticalSound is not null)
        {
            _audio.PlayPvs(args.Settings.SupercriticalSound, uid);
            return;
        }

        if (args.Settings.PulseSound is null
            || _glimmerSystem.Glimmer < args.Settings.GlimmerSoundThreshold * component.CurrentDampening)
            return;

        _audio.PlayEntity(args.Settings.PulseSound, uid, uid);
    }

    public void DoGlimmerEffects(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args, bool overcharged = false)
    {
        var minGlimmer = (int) Math.Round(MathF.MinMagnitude(args.Settings.MinGlimmer, args.Settings.MaxGlimmer)
            * (overcharged ? args.Settings.SupercriticalGlimmerMultiplier : 1)
            * component.CurrentAmplification - component.CurrentDampening);
        var maxGlimmer = (int) Math.Round(MathF.MaxMagnitude(args.Settings.MinGlimmer, args.Settings.MaxGlimmer)
            * (overcharged ? args.Settings.SupercriticalGlimmerMultiplier : 1)
            * component.CurrentAmplification - component.CurrentDampening);

        _psionics.LogPowerUsed(uid, args.Settings.PowerName, minGlimmer, maxGlimmer);
    }

    public void DoOverchargedEffects(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        if (args.Settings.OverchargeFeedback is not null
            && Loc.TryGetString(args.Settings.OverchargeFeedback, out var popup))
            _popup.PopupEntity(popup, uid, uid);

        // Claw Command - TryChangeDamage now takes an Entity<DamageableComponent?>; origin moved up one slot.
        if (args.Settings.OverchargeRecoil is not null
            && TryComp<DamageableComponent>(uid, out var damageable))
            _damageable.TryChangeDamage((uid, damageable), args.Settings.OverchargeRecoil / component.CurrentDampening, true, true, uid);

        if (args.Settings.OverchargeCooldown > 0)
            foreach (var action in component.Actions)
                _actions.SetCooldown(action.Value, TimeSpan.FromSeconds(args.Settings.OverchargeCooldown / component.CurrentDampening));
    }
}
