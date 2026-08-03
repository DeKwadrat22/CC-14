// Ported from space/ — _ClawCommand Pirate Punk accent.

using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech.Components;

namespace Content.Server.Speech.Components;

[RegisterComponent]
[Access(typeof(PunkAccentSystem))]
public sealed partial class PunkAccentComponent : BaseAccentComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("yarrChance")]
    public float YarrChance = 0.10f;

    [ViewVariables]
    public readonly List<string> PunkWords = new()
    {
        "accent-punk-prefix-1",
        "accent-punk-prefix-2",
        "accent-punk-prefix-3",
        "accent-punk-prefix-4",
        "accent-punk-prefix-5",
        "accent-punk-prefix-6",
        "accent-punk-prefix-7",
        "accent-punk-prefix-8",
        "accent-punk-prefix-9",
    };
}
