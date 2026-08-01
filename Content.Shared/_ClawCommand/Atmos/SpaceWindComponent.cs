using Content.Shared.Atmos;

namespace Content.Shared._ClawCommand.Atmos;

[ RegisterComponent ]
public sealed partial class SpaceWindComponent : Component
{
    /// <summary>
    ///     Memorizes the last pressure on a tile.
    /// </summary>
    [ViewVariables]
    public System.Numerics.Vector2 LastPressureVector;

    /// <summary>
    /// Whether this grid participates in Space Wind calculations. Disable for planet-side / large bombarded grids
    /// where the per-tile Navier-Stokes solve isn't worth the CPU.
    /// </summary>
    [DataField]
    public bool SpaceWindSimulation = true;

    [DataField]
    public int SpaceWindCooldown;

    [DataField]
    public int SpaceWindCooldownCycles = 75;
}
