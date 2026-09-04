using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Undergarment;

[Serializable, NetSerializable]
public sealed partial class UndergarmentDoAfterEvent : SimpleDoAfterEvent;
