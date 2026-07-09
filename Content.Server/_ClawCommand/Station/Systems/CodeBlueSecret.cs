using Content.Server.ClawCommand.Cabinet.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.AlertLevel;

namespace Content.Server._ClawCommand.Station.Systems;

public sealed partial class CodeBlueSecretSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
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
            if (_alertLevelSystem.GetLevel(station) == "green")
                _alertLevelSystem.SetLevel(station, "blueAuto", true, true);
        }
    }
}
