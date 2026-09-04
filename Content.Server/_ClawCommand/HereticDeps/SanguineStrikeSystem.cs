using Content.Shared._Goobstation.Wizard.SanguineStrike;

namespace Content.Server._Goobstation.Wizard;

/// <summary>
/// Server-side concrete subclass of the SanguineStrike stub. Without this, the entity
/// system manager can't resolve SharedSanguineStrikeSystem dependencies in heretic code
/// because Shared classes are abstract and need a concrete server implementation.
/// </summary>
public sealed class SanguineStrikeSystem : SharedSanguineStrikeSystem;
