using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server._ClawCommand.Maps.NameGenerators;

/// <summary>
/// Claw Command - as <see cref="NanotrasenNameGenerator"/>, but the whole prefix is configurable.
/// That generator hardcodes "NT" and only lets the map pick the two or three letters after it, so
/// every station it names necessarily reads as Nanotrasen property. Stations that belong to
/// somebody else need to say so, and inventing a generator per operator does not scale.
/// </summary>
[UsedImplicitly]
public sealed partial class ClawCommandNameGenerator : StationNameGenerator
{
    /// <summary>
    /// The full designation prefix, used verbatim. No operator code is prepended.
    /// </summary>
    [DataField(required: true)]
    public string Prefix = default!;

    /// <summary>
    /// Two letter codes for the tail of the designation. Matches the Nanotrasen set by default.
    /// </summary>
    [DataField]
    public string[] SuffixCodes = { "LV", "NX", "EV", "QT", "PR" };

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, Prefix, $"{random.Pick(SuffixCodes)}-{random.Next(0, 999):D3}");
    }
}
