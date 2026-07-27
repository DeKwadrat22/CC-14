using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        None = 0,
        Normal = 1 << 0,
        Ghost = 1 << 1, // Observers and revenants.
        Subfloor = 1 << 2, // Pipes, disposal chutes, cables etc. while hidden under tiles. Can be revealed with a t-ray.
        Admin = 1 << 3, // Reserved for admins in stealth mode and admin tools.
        EldritchInfluence = 1 << 4, // Heretic influences visible to heretics only.
        EldritchInfluenceSpent = 1 << 5, // Drained heretic influences.
        Ethereal = 1 << 6, // CLAW COMMAND - Shadekin phased into the dark plane; visible only to other ethereal entities.
    }
}
