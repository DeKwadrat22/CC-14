using Robust.Shared.Random;
using Content.Server.Abilities.Psionics;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Psionics;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Systems;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Zombies;

namespace Content.Server.StationEvents.Events;

internal sealed partial class NoosphericStormRule : StationEventSystem<NoosphericStormRuleComponent>
{
    [Dependency] private PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
    [Dependency] private MobStateSystem _mobStateSystem = default!;
    [Dependency] private GlimmerSystem _glimmerSystem = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;

    protected override void Started(EntityUid uid, NoosphericStormRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<EntityUid> validList = new();

        var query = EntityQueryEnumerator<PsionicComponent>();
        while (query.MoveNext(out var Psionic, out var PsionicComponent))
        {
            if (_mobStateSystem.IsDead(Psionic)
                || HasComp<PsionicInsulationComponent>(Psionic))
                continue;

            validList.Add(Psionic);
        }

        // Give some targets psionic abilities.
        RobustRandom.Shuffle(validList);

        var toAwaken = RobustRandom.Next(1, component.MaxAwaken);

        foreach (var target in validList)
        {
            if (toAwaken-- == 0)
                break;

            _psionicAbilitiesSystem.AddPsionics(target);
        }

        // Increase glimmer.
        var baseGlimmerAdd = _robustRandom.Next(component.BaseGlimmerAddMin, component.BaseGlimmerAddMax);
        //var glimmerSeverityMod = 1 + (component.GlimmerSeverityCoefficient * (GetSeverityModifier() - 1f));
        var glimmerAdded = (int) baseGlimmerAdd; // Math.Round(baseGlimmerAdd * glimmerSeverityMod);

        _glimmerSystem.Glimmer += glimmerAdded;
    }
}
