using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.Events;
using Content.Shared.Atmos.Components; // Claw Command - FlammableComponent moved to Shared
using Robust.Shared.Map;

namespace Content.Server.Abilities.Psionics;

public sealed partial class AnomalyPowerSystem
{
    private void DoPyroclasticAnomalyEffects(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args, bool overcharged = false)
    {
        if (args.Pyroclastic is null)
            return;

        if (overcharged)
            PyroclasticSupercrit(uid, component, args);
        else PyroclasticPulse(uid, component, args);
    }

    private void PyroclasticSupercrit(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        var pyroclastic = args.Pyroclastic!.Value;
        var xform = Transform(uid);
        var ignitionRadius = pyroclastic.SupercritMaximumIgnitionRadius * component.CurrentAmplification;
        IgniteNearby(uid, xform.Coordinates, component.CurrentAmplification, ignitionRadius);
    }

    private void PyroclasticPulse(EntityUid uid, PsionicComponent component, AnomalyPowerActionEvent args)
    {
        var pyroclastic = args.Pyroclastic!.Value;
        var xform = Transform(uid);
        var ignitionRadius = pyroclastic.MaximumIgnitionRadius * component.CurrentAmplification;
        IgniteNearby(uid, xform.Coordinates, component.CurrentAmplification, ignitionRadius);
    }

    private void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius)
    {
        var flammables = new HashSet<Entity<FlammableComponent>>();
        _lookup.GetEntitiesInRange(coordinates, radius, flammables);

        foreach (var flammable in flammables)
        {
            var ent = flammable.Owner;
            var stackAmount = 1 + (int) (severity / 0.15f);
            // Claw Command - these now take the component itself, not the Entity<T> wrapper.
            _flammable.AdjustFireStacks(ent, stackAmount, flammable.Comp);
            _flammable.Ignite(ent, uid, flammable.Comp);
        }
    }
}