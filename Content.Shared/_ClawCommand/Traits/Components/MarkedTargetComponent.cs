namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     Claw Command - Marks this character as a priority target for Syndicate kill objectives.
///     Granted by the "Marked Target" trait.
/// </summary>
/// <remarks>
///     This does nothing on its own. Objectives opt in via the <c>preferredConditions</c> field on
///     <c>PickRandomPerson</c>, which picks from the marked subset of the pool when one exists and
///     falls back to the whole pool when it doesn't.
/// </remarks>
[RegisterComponent]
public sealed partial class MarkedTargetComponent : Component;
