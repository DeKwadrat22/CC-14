// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 JohnOakman <sremy2012@hotmail.fr>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 github-actions <github-actions@github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Shitcode.Heretic;

public sealed partial class GhoulSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticMinionComponent, GetStatusIconsEvent>(OnGetMinionIcons);
    }

    private void OnGetMinionIcons(Entity<HereticMinionComponent> ent, ref GetStatusIconsEvent args)
    {
        // Upstream GetStatusIconsEvent has no Uid field — handler is per-component, so
        // `ent.Owner` is the target. Heretics see their bound minions' icon; minions see
        // their master.
        if (_player.LocalEntity is not { } player)
            return;

        if (TryComp(player, out HereticMinionComponent? selfMinion) && selfMinion.BoundHeretic == ent.Owner)
            args.StatusIcons.Add(_prototype.Index(selfMinion.MasterIcon));
        else if (ent.Comp.BoundHeretic == player)
            args.StatusIcons.Add(_prototype.Index(ent.Comp.GhoulIcon));
    }
}
