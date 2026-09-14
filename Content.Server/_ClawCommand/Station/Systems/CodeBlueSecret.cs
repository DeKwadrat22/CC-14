using Content.Server.ClawCommand.Cabinet.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.AlertLevel;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server._ClawCommand.Station.Systems;

public sealed partial class CodeBlueSecretSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private AlertLevelSystem _alertLevelSystem = default!;

    private TimeSpan _acoDelay = TimeSpan.FromMinutes(5);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (_ticker.RoundDuration() < _acoDelay)
            return;

        if (!_ticker.IsGameRuleAdded<SecretRuleComponent>())
            return;

        var query = EntityQueryEnumerator<CaptainStateComponent>();
        while (query.MoveNext(out var station, out _))
        {
            if (_alertLevelSystem.TryGetLevel(station, out var level) && level == "Green")
            {
                _alertLevelSystem.SetLevel(station, "Blue", playSound: true, announce: false, force: true);
                _chatSystem.DispatchStationAnnouncement(station,
                    "Enemy communications intercepted. Suspected security threat to the station or its crew. Crewmembers are advised to follow commands issued by any relevant authority.",
                    colorOverride: Color.DodgerBlue,
                    sender: "Claw Command",
                    playDefaultSound: false);
            }
        }
    }
}
