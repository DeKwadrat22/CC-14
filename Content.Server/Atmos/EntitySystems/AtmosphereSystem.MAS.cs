using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

// WELCOME TO THE MATRIX AIRFLOW SYSTEM.
public sealed partial class AtmosphereSystem
{
# pragma warning disable IDE1006
    /// <summary>
    ///     The standard issue "Search Pattern" used by the Matrix Airflow System.
    /// </summary>
    private readonly List<(int, int, AtmosDirection)> MASSearchPattern = new()
    {
        (-1,  1, AtmosDirection.SouthEast), (0,  1, AtmosDirection.South), (1,  1, AtmosDirection.SouthWest),
        (-1,  0, AtmosDirection.East),                                      (1,  0, AtmosDirection.West),
        (-1, -1, AtmosDirection.NorthEast), (0, -1, AtmosDirection.North), (1, -1, AtmosDirection.NorthWest),
    };
# pragma warning restore IDE1006

    /// <summary>
    ///     This function solves for the flow of air across a given tile, expressed in the format of (Vector) kg/ms^2.
    ///     Multiply this output against any "Area"(such as a human cross section) in the form of meters squared to get
    ///     the force of air flowing against that object in Newtons.
    ///     From there, you can divide by the object's mass (in kg) to get the object's acceleration in meters per second
    ///     squared. To solve for the object's change in velocity per CPU tick, you then multiply by frameTime to get
    ///     Delta-V.
    /// </summary>
    /// <remarks>
    ///     This function is a direct implementation of the Navier-Stokes system of partial differential equations.
    ///     Simplified since we don't need to account for fluid viscosity (yet) as this is currently only being used
    ///     to handle breathable atmosphere.
    /// </remarks>
    public Vector2 GetPressureVectorFromTile(GridAtmosphereComponent gridAtmos, TileAtmosphere tile)
    {
        if (!HasComp<MapGridComponent>(tile.GridIndex))
            return Vector2.Zero;

        var centerPressure = tile.AirArchived?.Pressure ?? 0f;
        var pressureVector = Vector2.Zero;

        foreach (var (x, y, dir) in MASSearchPattern)
        {
            // Create a new Vector2 using the search pattern, normalize it so its magnitude doesn't bias the result.
            var offsetVector = new Vector2(x, y);
            offsetVector = Vector2.Normalize(offsetVector);

            // If the tile checked doesn't exist or is space, then there's nothing to push back against our center.
            if (!gridAtmos.Tiles.TryGetValue(tile.GridIndices + new Vector2i(x, y), out var tileAtmosphere)
                || tileAtmosphere.Space)
            {
                pressureVector += offsetVector * centerPressure;
                if (!gridAtmos.Tiles.TryGetValue(tile.GridIndices - new Vector2i(x, y), out var opposingTile)
                    || opposingTile.AirArchived is null)
                    continue;
                pressureVector += offsetVector * (opposingTile.AirArchived.Pressure - centerPressure);
                continue;
            }

            // If the tile checked is blocking airflow from this direction, the center tile's air "bounces" off it.
            if (tileAtmosphere.AirtightData.BlockedDirections is AtmosDirection.All
                || tileAtmosphere.AirtightData.BlockedDirections.IsFlagSet(dir)
                || tileAtmosphere.AirArchived is null)
            {
                pressureVector -= offsetVector * centerPressure;
                if (!gridAtmos.Tiles.TryGetValue(tile.GridIndices - new Vector2i(x, y), out var opposingTile)
                    || opposingTile.AirArchived is null)
                    continue;

                pressureVector += offsetVector * (opposingTile.AirArchived.Pressure - centerPressure);
                continue;
            }

            // Center tile transfers its pressure across the target.
            var pressureDiff = centerPressure - tileAtmosphere.AirArchived.Pressure;
            pressureVector += offsetVector * pressureDiff;

            // And the pressure in the target tile resists the original target pressure.
            pressureVector -= offsetVector * tileAtmosphere.AirArchived.Pressure;
        }

        // By this point all possible conditions are checked; for any airtight vessel with a standard atmosphere
        // the final output will be ~(0, 0). Should any holes exist, the air will flow at an exponential rate
        // toward them while deflecting around walls.
        return pressureVector;
    }
}
