using Content.Shared.Interaction.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared._ClawCommand.Shadekin;
using System.Linq;
using Content.Shared.Stacks;
using Content.Server.Damage.Systems;
using Content.Server.Ghost;
using Content.Shared.Light.Components;

namespace Content.Server._ClawCommand.Shadekin;

public sealed partial class EtherealStunItemSystem : EntitySystem
{
    [Dependency] private StaminaSystem _stamina = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedStackSystem _sharedStackSystem = default!;
    [Dependency] private ShadekinSystem _shadekinSystem = default!;
    [Dependency] private GhostSystem _ghost = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EtherealStunItemComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, EtherealStunItemComponent component, UseInHandEvent args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (!TryComp<EtherealComponent>(ent, out var ethereal)
                || !ethereal.CanBeStunned)
                continue;

            RemComp(ent, ethereal);

            if (TryComp<StaminaComponent>(ent, out var stamina))
                _stamina.TakeStaminaDamage(ent, stamina.CritThreshold, stamina, ent);

            if (TryComp<ShadekinComponent>(ent, out var shadekin))
            {
                shadekin.Energy = 0;
                _shadekinSystem.UpdateAlert(ent, shadekin);

                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                    _ghost.DoGhostBooEvent(light);

                var effect = SpawnAtPosition("ShadekinPhaseIn2Effect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);
        }

        if (!component.DeleteOnUse)
            return;

        if (TryComp<StackComponent>(uid, out var stack))
            _sharedStackSystem.TryUse((uid, stack), 1);
        else
            QueueDel(uid);
    }
}
