using Content.Shared._Goobstation.Wizard.SanguineStrike;

namespace Content.Client._Goobstation.Wizard;

/// <summary>
/// Client-side concrete subclass of the SanguineStrike stub. Pairs with the server
/// version so heretic systems that depend on SharedSanguineStrikeSystem resolve cleanly
/// on both sides of the network.
/// </summary>
public sealed class SanguineStrikeSystem : SharedSanguineStrikeSystem;
