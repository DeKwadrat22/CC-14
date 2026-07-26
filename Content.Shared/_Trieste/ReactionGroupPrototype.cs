using Content.Shared.Chemistry.Reaction;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trieste;

/// <summary>
///     Prototype to group reactions for the guidebook.
/// </summary>
[Prototype]
public sealed partial class ReactionGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<ReactionPrototype> Reaction;

    [DataField]
    public string Group = "Other";
}
