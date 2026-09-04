// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Lavaland.Weapons.MeleeDash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DashingComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> HitEntities { get; set; } = new();

    [DataField, AutoNetworkedField]
    public List<string> ChangedFixtures { get; set; } = new();

    [DataField, AutoNetworkedField]
    public EntityUid? Weapon { get; set; }
}
