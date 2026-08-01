using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// CLAW COMMAND - raised on the chassis when someone finishes screwing a law board into it.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BorgLawBoardInstallDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// CLAW COMMAND - raised on a borg chassis to check whether it is allowed to turn on at all.
/// Cancelled by <see cref="BorgLawBoardSystem"/> when the borg has no law board installed, so a
/// lawless borg is dead weight until someone gives it a lawset.
/// </summary>
[ByRefEvent]
public record struct BorgActivateAttemptEvent(bool Cancelled = false);

/// <summary>
/// CLAW COMMAND - raised on a borg chassis when its law board is installed or pulled out. Only one system
/// may subscribe to a given component/event pair, so the shared system owns the container subscriptions
/// and relays this for the server-side law rewrite.
/// </summary>
/// <param name="Board">The board that was installed or removed.</param>
/// <param name="Installed">True when the board went in, false when it came out.</param>
[ByRefEvent]
public record struct BorgLawBoardChangedEvent(EntityUid Board, bool Installed);
