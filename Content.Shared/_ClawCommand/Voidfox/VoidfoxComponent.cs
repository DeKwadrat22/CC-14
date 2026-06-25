using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Voidfox;

/// <summary>
/// Claw Command - Marker for the voidfox spaceframe. Holds spacecraft-specific state
/// (ladder, cockpit latch, fuel latch) and the on-board fuel reservoir.
/// Intended to live alongside MechComponent which handles pilot entry/exit.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoidfoxComponent : Component
{
    /// <summary>
    /// Whether the boarding ladder is deployed. Required for a pilot to enter.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool LadderDeployed = true;

    /// <summary>
    /// Whether the cockpit canopy latch is open. Required for a pilot to enter.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool CockpitLatchOpen = true;

    /// <summary>
    /// Whether the fuel-fill latch is open. Required to refuel via plasma canister.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool FuelLatchOpen;

    /// <summary>
    /// Whether the spaceframe is currently grounded. Drives the visual base state.
    /// Always true until in-flight is implemented.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Landed = true;

    /// <summary>
    /// Whether the engine is currently boosting (used for the exhaust_boost state).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Boosting;

    /// <summary>
    /// Internal fuel tank. Holds any gas mixture, but the engine will
    /// only ignite at >= 95% plasma purity (enforced elsewhere when added).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasMixture FuelTank = new(VoidfoxFuelTankVolume);

    /// <summary>
    /// Volume of the fuel tank in liters.
    /// </summary>
    public const float VoidfoxFuelTankVolume = 200f;

    /// <summary>
    /// Minimum plasma fraction (by moles) required for the engine to ignite. 0..1.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinPlasmaPurityForIgnition = 0.95f;

    /// <summary>
    /// Detection radius (tiles) for the mass scanner ability.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MassScannerRange = 32f;

    #region Pilot actions
    [DataField]
    public EntProtoId IgniteAction = "ActionVoidfoxIgnite";
    [DataField]
    public EntProtoId MassScanAction = "ActionVoidfoxMassScan";

    [DataField] public EntityUid? IgniteActionEntity;
    [DataField] public EntityUid? MassScanActionEntity;
    #endregion
}
