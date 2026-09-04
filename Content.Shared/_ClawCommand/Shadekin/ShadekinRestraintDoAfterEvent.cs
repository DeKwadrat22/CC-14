using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Shadekin;

/// <summary>
///     CLAW COMMAND - raised on a <c>ShadekinRestraintComponent</c> item when the do-after to bind a
///     shadekin with it completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShadekinRestraintDoAfterEvent : SimpleDoAfterEvent;
