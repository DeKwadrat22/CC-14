// Ported from space/ — _ClawCommand Pirate Punk accent system.

using System.Linq;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// Claw Command - inherits RelayAccentSystem like every other accent since upstream #43008.
/// Subscribing to AccentGetEvent directly no longer works: it is a [ByRefEvent] record struct
/// now, so a by-value handler mutates a copy and the accent silently does nothing.
/// </summary>
public sealed partial class PunkAccentSystem : RelayAccentSystem<PunkAccentComponent>
{
    private static readonly Regex FirstWordAllCapsRegex = new(@"^(\S+)");

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    public override string Accentuate(string message, Entity<PunkAccentComponent>? ent = null)
    {
        var msg = _replacement.ApplyReplacements(message, "punk");

        if (!_random.Prob(ent.HasValue ? ent.Value.Comp.YarrChance : 0.1f))
            return msg;

        if (!ent.HasValue)
            return msg;

        var firstWordAllCaps = !FirstWordAllCapsRegex.Match(msg).Value.Any(char.IsLower);

        var pick = _random.Pick(ent.Value.Comp.PunkWords);
        var punkWord = Loc.GetString(pick);
        if (!firstWordAllCaps)
            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
        else
            punkWord = punkWord.ToUpper();

        return punkWord + " " + msg;
    }
}
