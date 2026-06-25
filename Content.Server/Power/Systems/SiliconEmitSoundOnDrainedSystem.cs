// claw command - IPC
using Content.Server.Silicon.Death;
using Content.Shared.Silicon.Systems;
using Content.Shared.Sound.Components;
using Content.Server.Sound;
using Content.Shared.Mobs;

namespace Content.Server.Silicon;

public sealed partial class EmitSoundOnCritSystem : EntitySystem
{
    [Dependency] private EmitSoundSystem _emitSound = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, SiliconChargeDeathEvent>(OnDeath);
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, SiliconChargeAliveEvent>(OnAlive);
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, SiliconChargeStateUpdateEvent>(OnChargeStateUpdate);
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, MobStateChangedEvent>(OnStateChange);
    }

    private void OnChargeStateUpdate(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, SiliconChargeStateUpdateEvent args)
    {
        // If the entity has SiliconDownOnDead, the death/alive events handle sounds instead.
        if (HasComp<SiliconDownOnDeadComponent>(uid))
            return;

        if (args.ChargePercent == 0)
            StartSound(uid, component);
        else
            StopSound(uid);
    }

    private void OnDeath(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, SiliconChargeDeathEvent args)
    {
        StartSound(uid, component);
    }

    private void OnAlive(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, SiliconChargeAliveEvent args)
    {
        StopSound(uid);
    }

    public void OnStateChange(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        StopSound(uid);
    }

    private void StartSound(EntityUid uid, SiliconEmitSoundOnDrainedComponent component)
    {
        var spamComp = EnsureComp<SpamEmitSoundComponent>(uid);

        spamComp.MinInterval = component.MinInterval;
        spamComp.MaxInterval = component.MaxInterval;
        spamComp.PopUp = component.PopUp;
        spamComp.Sound = component.Sound;
        _emitSound.SetEnabled((uid, spamComp), true);
    }

    private void StopSound(EntityUid uid)
    {
        RemComp<SpamEmitSoundComponent>(uid);
    }
}
