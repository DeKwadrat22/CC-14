namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Marks a shuttle coordinate disk that should auto-resolve its Destination
/// to the currently-loaded CentComm map at MapInit. The server-side
/// CentcommCoordinateDiskSystem does the resolution; the marker itself carries no state.
/// </summary>
[RegisterComponent]
public sealed partial class CentcommCoordinateDiskComponent : Component;
