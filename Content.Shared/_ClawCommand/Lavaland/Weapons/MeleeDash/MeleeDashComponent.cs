// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Lavaland.Weapons.MeleeDash;

[RegisterComponent, NetworkedComponent]
public sealed partial class MeleeDashComponent : Component
{
    [DataField]
    public string? EmoteOnDash { get; set; } = "Flip";

    // Claw Command - throwhard.ogg never came across with the port and does not exist in the `space`
    // or Goob-Station trees either, so the default pointed at a missing file. thudswoosh is the
    // fork's own heavy-swing whoosh and is the closest thing it ships.
    [DataField]
    public SoundSpecifier? DashSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField("force")]
    public float DashForce { get; set; } = 15f;

    [DataField("length")]
    public float MaxDashLength { get; set; } = 4f;
}

[Serializable, NetSerializable]
public sealed class MeleeDashEvent(NetEntity weapon, Vector2 direction) : EntityEventArgs
{
    public readonly NetEntity Weapon = weapon;
    public readonly Vector2 Direction = direction;
}
