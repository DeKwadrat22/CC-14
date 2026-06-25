using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.InteractionVerbs.Requirements;

/// <summary>
///     A requirement that combines multiple other requirements.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComplexRequirement : InteractionRequirement
{
    /// <summary>
    ///     If true, all requirements must pass (boolean and). Otherwise, at least one must pass (boolean or).
    /// </summary>
    [DataField]
    public bool RequireAll = true;

    [DataField]
    public List<InteractionRequirement> Requirements = new();

    public override bool IsMet(InteractionArgs ctx, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return RequireAll
            ? Requirements.All(req => req.IsMet(ctx, proto, deps))
            : Requirements.Any(req => req.IsMet(ctx, proto, deps));
    }
}
