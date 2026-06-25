using Content.Server._ClawCommand.Station.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.Station.Components;

/// <summary>
/// Denotes a station has no captain and holds data for automatic ACO systems
/// </summary>
[RegisterComponent, Access(typeof(EmergencyAccessMedbayStateSystem), typeof(StationSystem))]
public sealed partial class EmergencyAccessMedbayStateComponent : Component
{
    /// <summary>
    /// Denotes wether the entity has a captain or not
    /// </summary>
    /// <remarks>
    /// Assume no captain unless specified
    /// </remarks>
    [DataField]
    public int DoctorCount = 0;

    /// <summary>
    /// The localization ID used for announcing the cancellation of ACO requests
    /// </summary>
    [DataField]
    public LocId RevokeACOMessage = "doctors-arrived-revoke-aco-announcement";

    /// <summary>
    /// Used to denote that AA has been brought into the round either from captain or safe.
    /// </summary>
    [DataField]
    public bool IsAAInPlay;

    /// <summary>
    /// The localization ID for announcing that AA has been unlocked for ACO
    /// </summary>
    [DataField]
    public LocId AAUnlockedMessage = "no-doctors-aa-unlocked-announcement";

}
