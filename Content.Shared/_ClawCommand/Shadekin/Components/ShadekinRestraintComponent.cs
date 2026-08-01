using Robust.Shared.Audio;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._ClawCommand.Shadekin.Components;

/// <summary>
///     CLAW COMMAND - marks an item as a set of shadekin restraints. Used in-hand on a shadekin the same
///     way cuffs are applied (target them with the item, wait out a do-after). On success the shadekin is
///     permanently severed from the Dark - see the server-side <c>ShadekinRestraintSystem</c> and
///     <c>ShadekinSystem.RestrainShadekin</c> - and the restraints are then worn on them. The sever is
///     permanent even though the restraints themselves can be taken back off by the shadekin or anyone else.
/// </summary>
[RegisterComponent]
public sealed partial class ShadekinRestraintComponent : Component
{
    /// <summary>
    ///     How long the do-after to apply the restraints takes.
    /// </summary>
    [DataField]
    public TimeSpan ApplyTime = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Fire/ash sound played the moment an awakened (anomaly) shadekin is deconverted by the restraints.
    /// </summary>
    [DataField]
    public SoundSpecifier DeconvertSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");
}
