// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._ClawCommand.Lavaland.Weather;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Lavaland.Weather;

/// <summary>
/// Server-side damage tick for weather status effects that carry <see cref="WeatherDamageComponent"/>.
/// Ports Goob's WeatherEffectsSystem (Heat 4 on Ashfall) onto the fork's entity-based weather:
///  - Ticks once per <c>WeatherDamageComponent.UpdateDelay</c> per active weather entity.
///  - Skips damage during the 15s startup/shutdown crossfade (only damages at full intensity), matching
///    Goob's "only when WeatherState.Running" gate via <see cref="SharedWeatherSystem.GetWeatherPercent"/>.
///  - Applies damage only to <see cref="MobStateComponent"/> entities on the weather's map that stand on
///    a sky-exposed weather-eligible tile (via <see cref="SharedWeatherSystem.CanWeatherAffect"/>).
///  - Excludes entities matching the component's blacklist (<see cref="WeatherImmuneComponent"/>-bearers).
/// </summary>
public sealed partial class WeatherDamageSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _map = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherDamageComponent, StatusEffectComponent>();
        while (query.MoveNext(out var weatherUid, out var damage, out var status))
        {
            if (now < damage.NextUpdate)
                continue;

            damage.NextUpdate = now + damage.UpdateDelay;

            // Only damage when the weather is at full intensity. During the 15s startup/shutdown
            // crossfade GetWeatherPercent < 1, which mirrors Goob's "WeatherState.Running" gate.
            if (_weather.GetWeatherPercent((weatherUid, status)) < 1f)
                continue;

            // Status effect's AppliedTo is the map entity that hosts the weather.
            if (status.AppliedTo is not { } mapUid)
                continue;

            ApplyDamageToMap(mapUid, damage);
        }
    }

    private void ApplyDamageToMap(EntityUid mapUid, WeatherDamageComponent damage)
    {
        var mobQuery = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            // Blacklist match (e.g. WeatherImmune) skips damage.
            if (damage.DamageBlacklist != null && _whitelist.IsValid(damage.DamageBlacklist, mobUid))
                continue;

            // Need a grid + tile to consult roof/tile-weather rules.
            if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
                continue;

            var tile = _map.GetTileRef(gridUid, grid, xform.Coordinates);
            if (!_weather.CanWeatherAffect((gridUid, grid, null), tile))
                continue;

            _damageable.TryChangeDamage(mobUid, damage.Damage, interruptsDoAfters: false);
        }
    }
}
