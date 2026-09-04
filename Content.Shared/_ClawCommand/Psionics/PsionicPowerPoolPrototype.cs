using Robust.Shared.Prototypes;

namespace Content.Shared.Psionics;

// Claw Command - RA0042: the explicit name matched what the attribute generates anyway, so it is dropped.
[Prototype]
public sealed partial class PsionicPowerPoolPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [DataField]
    public List<string> Powers = new();
}
