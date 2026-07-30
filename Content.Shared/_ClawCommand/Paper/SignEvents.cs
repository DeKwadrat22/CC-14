namespace Content.Shared._ClawCommand.Paper;

/// <summary>
///     CLAW COMMAND (ported from space/DeltaV) - raised on the PEN before a signature is applied.
///     Cancel to veto the signing (e.g. a pen that cannot sign / forgery gating).
/// </summary>
[ByRefEvent]
public record struct SignAttemptEvent(EntityUid Paper, EntityUid Signer, bool Cancelled = false);

/// <summary>
///     CLAW COMMAND - raised on the PAPER before a signature is applied. Cancel to veto the signing
///     (e.g. a contract that only accepts certain signers).
/// </summary>
[ByRefEvent]
public record struct BeingSignedAttemptEvent(EntityUid Paper, EntityUid Signer, bool Cancelled = false);

/// <summary>
///     CLAW COMMAND - raised on the PAPER after a signature was successfully applied.
/// </summary>
[ByRefEvent]
public record struct SignSuccessfulEvent(EntityUid Paper, EntityUid Signer);
