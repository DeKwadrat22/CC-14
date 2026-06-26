// Ported from space/ — _ClawCommand Pirate Punk accent system.

using System.Linq;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class PunkAccentSystem : EntitySystem
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PunkAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, PunkAccentComponent component)
    {
        var msg = _replacement.ApplyReplacements(message, "punk");

        if (!_random.Prob(component.YarrChance))
            return msg;

        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);

        var pick = _random.Pick(component.PunkWords);
        var pirateWord = Loc.GetString(pick);
        if (!firstWordAllCaps)
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        else
            pirateWord = pirateWord.ToUpper();
        msg = pirateWord + " " + msg;

        return msg;
    }

    private void OnAccentGet(EntityUid uid, PunkAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
