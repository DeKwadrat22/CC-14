// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Lavaland.Weather;

/// <summary>
/// Attached to a weather status-effect entity to make standing in that weather tick damage on
/// unsheltered mobs (sky-exposed tile and no <see cref="WeatherImmuneComponent"/>). Ported from
/// Goob's DeltaV-style WeatherPrototype.Damage/DamageBlacklist fields onto our entity-based
/// weather status effects so the fork's lavaland Ashfall actually burns.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherDamageComponent : Component
{
    /// <summary>
    /// Damage applied to each affected mob per damage tick.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Entities matching this whitelist are EXCLUDED from damage (used as a blacklist).
    /// Mirrors Goob's <c>damageBlacklist</c>. Typically set to <c>components: [WeatherImmune]</c>.
    /// </summary>
    [DataField]
    public EntityWhitelist? DamageBlacklist;

    /// <summary>
    /// Seconds between damage applications.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next time damage will be applied. Managed by the system; not for YAML.
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate;
}
