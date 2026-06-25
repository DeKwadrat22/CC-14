// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Lavaland.Weather;

/// <summary>
/// Makes an entity not take damage from any weather.
/// Marker component — the fork's weather system is visual-only, so this is
/// presence-only. Damage-applying systems hooked into weather should check
/// for this component before applying damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherImmuneComponent : Component;
