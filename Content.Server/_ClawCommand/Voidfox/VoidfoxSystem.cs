using System.Linq;
using System.Text;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._ClawCommand.Voidfox;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.DragDrop;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mind.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._ClawCommand.Voidfox;

/// <summary>
/// Claw Command - Server logic for the Voidfox spaceframe terminal:
/// UI handling, ladder/cockpit/fuel toggles, and pilot-entry gating
/// (a pilot can only enter when both the cockpit latch is open and the ladder is deployed).
/// Pilot insertion itself is delegated to the existing mech system.
/// </summary>
public sealed partial class VoidfoxSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _mapManager = default!;
    [Dependency] private VehicleSystem _vehicleSystem = default!;

    /// <summary>Pipe node id used for the fuel-fill port on the voidfox prototype.</summary>
    public const string FuelPortName = "fuelPort";

    /// <summary>How fast (in liters of pipe gas equivalent) the tank can be filled per atmos tick when conditions are met.</summary>
    private const float FuelFillRate = 200f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoidfoxComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VoidfoxComponent, BoundUIOpenedEvent>(OnUiOpened);

        // Run after the mech system so we can override its CanDrop = true with our own gating.
        SubscribeLocalEvent<VoidfoxComponent, CanDropTargetEvent>(OnCanDragDrop, after: new[] { typeof(SharedMechSystem) });

        SubscribeLocalEvent<VoidfoxComponent, VoidfoxToggleLadderMessage>(OnToggleLadder);
        SubscribeLocalEvent<VoidfoxComponent, VoidfoxToggleCockpitMessage>(OnToggleCockpit);
        SubscribeLocalEvent<VoidfoxComponent, VoidfoxToggleFuelLatchMessage>(OnToggleFuelLatch);

        SubscribeLocalEvent<VoidfoxComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);

        // Pilot enter/exit -> grant/revoke pilot actions.
        SubscribeLocalEvent<VoidfoxComponent, VehicleOperatorSetEvent>(OnVehicleOperatorSet);

        // Pilot action handlers.
        SubscribeLocalEvent<VoidfoxComponent, VoidfoxIgniteEvent>(OnIgnite);
        SubscribeLocalEvent<VoidfoxComponent, VoidfoxMassScanEvent>(OnMassScan);
    }

    private void OnVehicleOperatorSet(Entity<VoidfoxComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldPilot)
        {
            _actions.RemoveProvidedActions(oldPilot, ent.Owner);
            ent.Comp.IgniteActionEntity = null;
            ent.Comp.MassScanActionEntity = null;
        }

        if (args.NewOperator is { } pilot)
        {
            _actions.AddAction(pilot, ref ent.Comp.IgniteActionEntity, ent.Comp.IgniteAction, ent.Owner);
            _actions.AddAction(pilot, ref ent.Comp.MassScanActionEntity, ent.Comp.MassScanAction, ent.Owner);
        }

        UpdateUi(ent);
    }

    private void OnIgnite(Entity<VoidfoxComponent> ent, ref VoidfoxIgniteEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        // If already lit, snuff it.
        if (ent.Comp.Boosting || !ent.Comp.Landed)
        {
            ent.Comp.Boosting = false;
            ent.Comp.Landed = true;
            Dirty(ent);
            UpdateAppearance(ent);
            UpdateUi(ent);
            _popup.PopupEntity(Loc.GetString("voidfox-ignite-off"), ent.Owner, args.Performer);
            return;
        }

        if (ent.Comp.CockpitLatchOpen)
        {
            _popup.PopupEntity(Loc.GetString("voidfox-ignite-need-cockpit-closed"), ent.Owner, args.Performer);
            return;
        }

        var tank = ent.Comp.FuelTank;
        var totalMoles = tank.TotalMoles;
        if (totalMoles <= 0f)
        {
            _popup.PopupEntity(Loc.GetString("voidfox-ignite-no-fuel"), ent.Owner, args.Performer);
            return;
        }

        var plasmaFrac = tank[(int)Gas.Plasma] / totalMoles;
        if (plasmaFrac < ent.Comp.MinPlasmaPurityForIgnition)
        {
            _popup.PopupEntity(Loc.GetString("voidfox-ignite-low-purity",
                ("pct", (plasmaFrac * 100f).ToString("F1")),
                ("min", (ent.Comp.MinPlasmaPurityForIgnition * 100f).ToString("F0"))),
                ent.Owner, args.Performer);
            return;
        }

        ent.Comp.Boosting = true;
        ent.Comp.Landed = false;
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("voidfox-ignite-on"), ent.Owner, args.Performer);
    }

    private void OnMassScan(Entity<VoidfoxComponent> ent, ref VoidfoxMassScanEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        var origin = _transform.GetMapCoordinates(ent.Owner);
        var range = ent.Comp.MassScannerRange;

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("voidfox-scan-header", ("range", range.ToString("F0"))));

        // Grids in range.
        var grids = new List<Entity<Robust.Shared.Map.Components.MapGridComponent>>();
        _mapManager.FindGridsIntersecting(origin.MapId,
            new Box2(origin.Position - new System.Numerics.Vector2(range, range), origin.Position + new System.Numerics.Vector2(range, range)),
            ref grids);

        var contacts = 0;
        foreach (var grid in grids)
        {
            if (grid.Owner == ent.Owner)
                continue;
            var gridPos = _transform.GetMapCoordinates(grid.Owner).Position;
            var dist = (gridPos - origin.Position).Length();
            if (dist > range)
                continue;
            var name = Name(grid.Owner);
            var bearing = BearingFromDelta(gridPos - origin.Position);
            sb.AppendLine(Loc.GetString("voidfox-scan-grid",
                ("name", name),
                ("dist", dist.ToString("F0")),
                ("bearing", bearing)));
            contacts++;
        }

        // Mind-bearing entities (other pilots / crew) in range.
        var minds = new HashSet<Entity<MindContainerComponent>>();
        _lookup.GetEntitiesInRange(origin, range, minds);
        foreach (var mind in minds)
        {
            if (mind.Owner == ent.Owner || mind.Owner == args.Performer)
                continue;
            var pos = _transform.GetMapCoordinates(mind.Owner).Position;
            var dist = (pos - origin.Position).Length();
            var bearing = BearingFromDelta(pos - origin.Position);
            sb.AppendLine(Loc.GetString("voidfox-scan-entry",
                ("name", Name(mind.Owner)),
                ("dist", dist.ToString("F0")),
                ("bearing", bearing)));
            contacts++;
        }

        if (contacts == 0)
            sb.AppendLine(Loc.GetString("voidfox-scan-empty"));

        _popup.PopupEntity(sb.ToString(), args.Performer, args.Performer, PopupType.Medium);
    }

    private static string BearingFromDelta(System.Numerics.Vector2 delta)
    {
        // 0deg = east; rotate so 0deg = north for player intuition.
        var deg = (MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI) - 90f + 360f) % 360f;
        // Flip so it reads clockwise from north
        deg = (450f - deg) % 360f;
        var dirs = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        var idx = (int)MathF.Round(deg / 45f) % 8;
        return dirs[idx];
    }

    private void OnAtmosUpdate(Entity<VoidfoxComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        // Only refuel through the pipe when the fuel latch is open AND the spaceframe is anchored
        // (i.e. wrenched down to the deck so the pipe port can dock).
        if (!ent.Comp.FuelLatchOpen)
            return;

        if (!Transform(ent.Owner).Anchored)
            return;

        if (!TryComp<NodeContainerComponent>(ent.Owner, out var nodes))
            return;

        if (!_nodeContainer.TryGetNode<PipeNode>(nodes, FuelPortName, out var pipe))
            return;

        var pipeAir = pipe.Air;
        var tank = ent.Comp.FuelTank;

        // Equalize pressure: pull gas from pipe into tank up to the pipe's current pressure,
        // capped by an atmos-tick transfer rate so it isn't instant.
        var transferMoles = MathF.Min(
            pipeAir.TotalMoles,
            (pipeAir.Pressure - tank.Pressure) * FuelFillRate / Atmospherics.R / MathF.Max(pipeAir.Temperature, 1f));

        if (transferMoles <= 0f)
            return;

        var removed = pipeAir.Remove(transferMoles);
        _atmos.Merge(tank, removed);
        UpdateUi(ent);
    }

    private void OnStartup(Entity<VoidfoxComponent> ent, ref ComponentStartup args)
    {
        // Make sure the fuel tank's volume matches our spec even if YAML didn't set it.
        if (ent.Comp.FuelTank.Volume <= 0f)
            ent.Comp.FuelTank.Volume = VoidfoxComponent.VoidfoxFuelTankVolume;

        UpdateAppearance(ent);
    }

    private void OnUiOpened(Entity<VoidfoxComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (Equals(args.UiKey, VoidfoxUiKey.Terminal))
            UpdateUi(ent);
    }

    private void OnCanDragDrop(Entity<VoidfoxComponent> ent, ref CanDropTargetEvent args)
    {
        // The mech subscriber may have set CanDrop = true; veto it if the spaceframe isn't open for boarding.
        if (!ent.Comp.LadderDeployed || !ent.Comp.CockpitLatchOpen)
        {
            args.CanDrop = false;
            args.Handled = true;
        }
    }

    private void OnToggleLadder(Entity<VoidfoxComponent> ent, ref VoidfoxToggleLadderMessage args)
    {
        // Don't retract the ladder while a pilot is still inside.
        if (ent.Comp.LadderDeployed && HasOccupant(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("voidfox-cannot-retract-ladder-occupied"), ent.Owner, args.Actor);
            return;
        }

        ent.Comp.LadderDeployed = !ent.Comp.LadderDeployed;
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnToggleCockpit(Entity<VoidfoxComponent> ent, ref VoidfoxToggleCockpitMessage args)
    {
        // Can't seal the cockpit on top of someone with no graceful air handling yet.
        // Allow closing only if empty, but always allow opening.
        if (ent.Comp.CockpitLatchOpen && HasOccupant(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("voidfox-cannot-close-cockpit-occupied"), ent.Owner, args.Actor);
            return;
        }

        ent.Comp.CockpitLatchOpen = !ent.Comp.CockpitLatchOpen;
        Dirty(ent);
        UpdateAppearance(ent);
        UpdateUi(ent);
    }

    private void OnToggleFuelLatch(Entity<VoidfoxComponent> ent, ref VoidfoxToggleFuelLatchMessage args)
    {
        ent.Comp.FuelLatchOpen = !ent.Comp.FuelLatchOpen;
        Dirty(ent);
        UpdateUi(ent);
    }

    private bool HasOccupant(EntityUid uid)
    {
        return _vehicleSystem.HasOperator(uid);
    }

    private void UpdateAppearance(Entity<VoidfoxComponent> ent)
    {
        if (!HasComp<AppearanceComponent>(ent.Owner))
            return;

        VoidfoxVisualState state;
        if (!ent.Comp.Landed)
        {
            state = ent.Comp.Boosting ? VoidfoxVisualState.ExhaustBoost : VoidfoxVisualState.Idle;
        }
        else if (ent.Comp.CockpitLatchOpen && ent.Comp.LadderDeployed)
        {
            state = VoidfoxVisualState.OpenLanded;
        }
        else if (ent.Comp.CockpitLatchOpen)
        {
            state = VoidfoxVisualState.OpenLandedNoLadder;
        }
        else
        {
            // Cockpit closed (with or without ladder out) - we have no "closed + ladder" sprite,
            // so fall back to closed + retracted.
            state = VoidfoxVisualState.LandedClosedNoLadder;
        }

        _appearance.SetData(ent.Owner, VoidfoxVisuals.State, state);
    }

    private void UpdateUi(Entity<VoidfoxComponent> ent)
    {
        var tank = ent.Comp.FuelTank;
        var totalMoles = tank.TotalMoles;
        var plasmaFrac = totalMoles > 0f ? tank[(int)Gas.Plasma] / totalMoles : 0f;

        _ui.SetUiState(ent.Owner, VoidfoxUiKey.Terminal, new VoidfoxBuiState
        {
            LadderDeployed = ent.Comp.LadderDeployed,
            CockpitLatchOpen = ent.Comp.CockpitLatchOpen,
            FuelLatchOpen = ent.Comp.FuelLatchOpen,
            HasOccupant = HasOccupant(ent.Owner),
            FuelTotalMoles = totalMoles,
            PlasmaFraction = plasmaFrac,
            Pressure = tank.Pressure,
            Temperature = tank.Temperature,
            Volume = tank.Volume,
            MinPlasmaPurity = ent.Comp.MinPlasmaPurityForIgnition,
        });
    }
}
