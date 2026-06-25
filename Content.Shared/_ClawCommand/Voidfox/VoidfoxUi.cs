using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Voidfox;

[Serializable, NetSerializable]
public enum VoidfoxUiKey : byte
{
    Terminal,
}

[Serializable, NetSerializable]
public enum VoidfoxVisuals : byte
{
    State,
}

/// <summary>
/// Composite visual state. Resolved server-side from the component flags
/// and pushed to AppearanceComponent so the client visualizer can swap sprites.
/// </summary>
[Serializable, NetSerializable]
public enum VoidfoxVisualState : byte
{
    Idle,
    ExhaustBoost,
    OpenLanded,
    OpenLandedNoLadder,
    LandedClosedNoLadder,
}

[Serializable, NetSerializable]
public sealed class VoidfoxBuiState : BoundUserInterfaceState
{
    public bool LadderDeployed;
    public bool CockpitLatchOpen;
    public bool FuelLatchOpen;
    public bool HasOccupant;

    /// <summary>Total moles of all gases currently in the fuel tank.</summary>
    public float FuelTotalMoles;
    /// <summary>Plasma fraction (0..1) of the fuel tank.</summary>
    public float PlasmaFraction;
    /// <summary>Tank pressure in kPa.</summary>
    public float Pressure;
    /// <summary>Tank temperature in Kelvin.</summary>
    public float Temperature;
    /// <summary>Tank volume in liters.</summary>
    public float Volume;
    /// <summary>Minimum plasma fraction for the engine to ignite (0..1).</summary>
    public float MinPlasmaPurity;
}

[Serializable, NetSerializable]
public sealed class VoidfoxToggleLadderMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VoidfoxToggleCockpitMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class VoidfoxToggleFuelLatchMessage : BoundUserInterfaceMessage;

/// <summary>Pilot action: ignite/extinguish the engine.</summary>
public sealed partial class VoidfoxIgniteEvent : InstantActionEvent;

/// <summary>Pilot action: sweep nearby grids/entities and report what's in range.</summary>
public sealed partial class VoidfoxMassScanEvent : InstantActionEvent;
