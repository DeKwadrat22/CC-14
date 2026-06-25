// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._ClawCommand.Lavaland.Held;

public sealed partial class HeldGrantSystem : EntitySystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeldGrantComponent, GotEquippedHandEvent>(OnCompEquip);
        SubscribeLocalEvent<HeldGrantComponent, GotUnequippedHandEvent>(OnCompUnequip);
    }

    private void OnCompEquip(Entity<HeldGrantComponent> ent, ref GotEquippedHandEvent args)
    {
        foreach (var (name, data) in ent.Comp.Components)
        {
            var newComp = (Component) _componentFactory.GetComponent(name);
            if (HasComp(args.User, newComp.GetType()))
                continue;

            object? temp = newComp;
            _serializationManager.CopyTo(data.Component, ref temp);
            AddComp(args.User, (Component)temp!);

            ent.Comp.Active[name] = true;
        }
    }

    private void OnCompUnequip(Entity<HeldGrantComponent> ent, ref GotUnequippedHandEvent args)
    {
        foreach (var (name, _) in ent.Comp.Components)
        {
            if (!ent.Comp.Active.ContainsKey(name) || !ent.Comp.Active[name])
                continue;

            var newComp = (Component) _componentFactory.GetComponent(name);

            RemComp(args.User, newComp.GetType());
            ent.Comp.Active[name] = false;
        }
    }
}
