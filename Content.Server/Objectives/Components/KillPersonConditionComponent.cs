using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a target dies or, if <see cref="RequireDead"/> is false, is not on the emergency shuttle.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class KillPersonConditionComponent : Component
{
    /// <summary>
    /// Whether the target must be dead
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireDead = false;

    /// <summary>
    /// Whether the target must not be on evac
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireMaroon = false;

    /// <summary>
    /// Claw Command - Whether <see cref="RequireDead"/> should latch the first time the target dies.
    /// Without this, progress is re-read live, so cloning or defibbing the target un-completes the
    /// objective and "kill them" quietly becomes "make sure they're still dead at round end".
    /// Only affects the dead check; <see cref="RequireMaroon"/> is still evaluated normally.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool LatchOnDeath = false;

    /// <summary>
    /// Claw Command - Set once the target has died at least once, if <see cref="LatchOnDeath"/> is set.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Latched = false;
}
