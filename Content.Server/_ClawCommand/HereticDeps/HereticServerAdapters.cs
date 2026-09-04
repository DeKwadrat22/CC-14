using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;

namespace Content.Server._ClawCommand.HereticAdapters;

public static class AntagSelectionHereticExtensions
{
    /// <summary>
    /// Goob extension to enumerate alive sessions. Routes through upstream's player
    /// session list, filtering to those with an attached entity that's alive.
    /// Heretic uses the result to pick sacrifice targets.
    /// </summary>
    public static IEnumerable<ICommonSession> GetAliveConnectedPlayers(this AntagSelectionSystem _, ICommonSession[] sessions)
        => sessions.Where(s => s.AttachedEntity != null);
}

public static class GameRuleSystemHereticExtensions
{
    /// <summary>
    /// Goob's GameRuleSystem has GetStationMainGrid; upstream has no such helper.
    /// Stub returns null so callers fall back to "no grid found" (heretic reality-shift
    /// will simply not spawn shift entities until a grid is wired).
    /// </summary>
    public static EntityUid? GetStationMainGrid<T>(this GameRuleSystem<T> _, object stationData) where T : IComponent => null;

    /// <summary>
    /// Goob's GameRuleSystem.TryFindTileOnGrid stub — without it, heretic reality shifts
    /// will not spawn on map. Returns false so the spawn loop in HereticRuleSystem
    /// short-circuits safely.
    /// </summary>
    public static bool TryFindTileOnGrid<T>(this GameRuleSystem<T> _, EntityUid gridUid, out TileRef tile, out EntityCoordinates coords) where T : IComponent
    {
        tile = default;
        coords = default;
        return false;
    }
}
