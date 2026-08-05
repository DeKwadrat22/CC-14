using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Movement.DiveLeap;

/// <summary>
///     Claw Command - Blocks the sprinting dive-leap outright.
///
///     Added by traits whose whole premise rules the move out: someone who needs a cane to walk is
///     not launching into a dive, and someone with brittle bones has no business throwing themselves
///     at the floor. Those characters still lie down normally with the same key - only the leap is
///     taken away, so the trait removes an option rather than breaking a control.
///
///     Kept as its own marker rather than a check against a list of trait IDs so anything else that
///     ought to prevent diving - a future trait, a status effect, a piece of gear - can opt in by
///     adding this and needs no change here.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoDiveLeapComponent : Component;
