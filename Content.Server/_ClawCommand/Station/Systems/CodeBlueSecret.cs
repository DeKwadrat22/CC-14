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
    private bool _ran = false;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _ticker.RoundDuration(); // Caching to reduce redundant calls
        if (_ran || currentTime < _acoDelay) // Avoid timing issues. No need to run before _acoDelay is reached anyways.
            return;
        _ran = true;
        if (_ticker.IsGameRuleAdded<SecretRuleComponent>())
        {

            var query = EntityQueryEnumerator<CaptainStateComponent>();
            while (query.MoveNext(out var station, out var _))
            {


                if (_alertLevelSystem.GetLevel(station) == "green")
                {
                    _alertLevelSystem.SetLevel(station, "blueAuto", true, true);
                }
            }


        }


    }

}
