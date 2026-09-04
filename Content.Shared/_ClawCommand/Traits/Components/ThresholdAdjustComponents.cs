using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Adjusts the dead damage threshold for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HealthDeadAdjustComponent : Component
{
    [DataField]
    public int Offset { get; private set; } = 0;
}

/// <summary>
///     Shifts SlowOnDamage thresholds up or down.
///     Positive values require more damage to slow, negative less.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InjurySlowAdjustComponent : Component
{
    [DataField]
    public int ThresholdShift;
}

/// <summary>
///     Adjusts the stamina critical threshold for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StaminaCapAdjustComponent : Component
{
    [DataField]
    public int Offset { get; private set; } = 0;
}

/// <summary>
///     Adjusts the critical damage threshold for an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HealthCritAdjustComponent : Component
{
    [DataField]
    public int Offset { get; private set; } = 0;
}

/// <summary>
///     Boosts blood regeneration rate on an entity's BloodstreamComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodRegenBoostComponent : Component
{
    [DataField]
    public float RegenMultiplier = 1.0f;
}
