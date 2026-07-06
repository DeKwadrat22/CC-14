using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Shared._ClawCommand.Feroxi;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Feroxi;

[UsedImplicitly]
public sealed partial class FeroxiSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FeroxiComponent, ThirstComponent>();
        while (query.MoveNext(out var uid, out var feroxi, out var thirst))
        {
            if (!TryComp<MobStateComponent>(uid, out var mobState))
                continue;

            if (!TryComp<RespiratorComponent>(uid, out var respirator))
                continue;

            if (_timing.CurTime < feroxi.NextUpdateTime)
                continue;

            feroxi.NextUpdateTime += feroxi.UpdateRate;

            if (thirst.CurrentThirstThreshold <= ThirstThreshold.Parched
                && mobState.CurrentState is not (MobState.Critical or MobState.Dead))
            {
                _damageable.ChangeDamage(uid, feroxi.Damage, interruptsDoAfters: false, ignoreResistances: true);
                _alerts.ShowAlert(uid, feroxi.Alert);

                if (_timing.CurTime >= respirator.LastGaspEmoteTime + respirator.GaspEmoteCooldown)
                {
                    respirator.LastGaspEmoteTime = _timing.CurTime;
                    _chat.TryEmoteWithChat(uid,
                        respirator.GaspEmote,
                        ChatTransmitRange.HideChat,
                        ignoreActionBlocker: true);
                }
            }
            else
            {
                _alerts.ClearAlert(uid, feroxi.Alert);
            }
        }
    }
}
