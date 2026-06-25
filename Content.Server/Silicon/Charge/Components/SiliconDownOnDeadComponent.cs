// claw command - IPC
namespace Content.Server.Silicon.Death;

/// <summary>
///     Marks a Silicon as becoming incapacitated when they run out of battery charge.
/// </summary>
[RegisterComponent]
public sealed partial class SiliconDownOnDeadComponent : Component
{
    /// <summary>
    ///     Is this Silicon currently dead?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Dead;
}
