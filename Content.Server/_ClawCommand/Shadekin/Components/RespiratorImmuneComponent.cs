namespace Content.Server._ClawCommand.Shadekin;

/// <summary>
///     Marks an entity as immune to respiration (suffocation) effects. Applied to ethereal Shadekin
///     so that phasing into the dark plane does not cause them to suffocate.
/// </summary>
[RegisterComponent]
public sealed partial class RespiratorImmuneComponent : Component { }
