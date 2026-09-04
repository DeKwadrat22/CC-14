namespace Content.Shared._ClawCommand.Forensics;

/// <summary>
/// Claw Command: gives a creature a readable forensic signature instead of an ordinary print.
///
/// A fingerprint is just a hex string and DNA is just sixteen base pairs; both are opaque, and both
/// are only useful because a records console can match them to a crew record. Anomalies have no
/// record - they are not crew - so a scanner would otherwise return a print that matches nothing,
/// which reads as "unknown intruder" rather than "a shadekin was here".
///
/// This component replaces both strings with a sentence naming the creature. Everything downstream
/// keeps working untouched: the string is copied onto whatever they handle, carried by their blood,
/// and printed by the scanner exactly as any other print would be.
/// </summary>
[RegisterComponent]
public sealed partial class ForensicSignatureComponent : Component
{
    /// <summary>
    /// Fluent id for the signature. Receives a <c>name</c> argument holding the creature's
    /// current name.
    /// </summary>
    [DataField]
    public LocId Signature = "forensics-signature-shadekin";
}
