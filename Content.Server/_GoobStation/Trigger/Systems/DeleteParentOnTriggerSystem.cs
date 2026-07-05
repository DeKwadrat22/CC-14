// SPDX-FileCopyrightText: 2024 Goobstation Contributors
// SPDX-FileCopyrightText: 2025 ClawCommand Contributors
// Will Not work in dev mode since CentComm doesn't spawn there.
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goobstation for ClawCommand.
using Content.Shared._GoobStation.Trigger.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Trigger;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._GoobStation.Trigger.Systems;

/// <summary>
/// Server-side handler for <see cref="DeleteParentOnTriggerComponent"/>.
/// When the implant triggers, teleports the host to Central Command medical
/// instead of deleting them. Leaves a bluespace core at the original location.
/// </summary>
public sealed partial class DeleteParentOnTriggerSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    private const string CentcommGridName = "Central Command";
    private const float TargetX = 32f;
    private const float TargetY = -20f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeleteParentOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DeleteParentOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (!TryComp<SubdermalImplantComponent>(ent.Owner, out var implant))
            return;

        if (implant.ImplantedEntity == null || Terminating(implant.ImplantedEntity.Value))
            return;

        var host = implant.ImplantedEntity.Value;

        EntityUid? centcommGrid = null;
        var gridQuery = AllEntityQuery<MapGridComponent, MetaDataComponent>();
        while (gridQuery.MoveNext(out var gridUid, out _, out var meta))
        {
            if (meta.EntityName == CentcommGridName)
            {
                centcommGrid = gridUid;
                break;
            }
        }

        if (centcommGrid == null)
        {
            Log.Warning($"BluespaceLifeline: Could not find grid named '{CentcommGridName}'. Host not teleported.");
            return;
        }

        var targetCoords = new EntityCoordinates(centcommGrid.Value, TargetX, TargetY);
        _transformSystem.SetCoordinates(host, targetCoords);

        // Spawn a bluespace arrival effect at the destination
        Spawn("BluespaceLifeline", targetCoords);

        args.Handled = true;
    }
}
