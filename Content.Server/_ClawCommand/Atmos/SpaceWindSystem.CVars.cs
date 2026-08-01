using Content.Shared._ClawCommand.CCVar;

namespace Content.Server._ClawCommand.Atmos;

public sealed partial class SpaceWindSystem
{
    public float SpaceWindStrengthMultiplier { get; private set; }
    public float SpaceWindMaxForce { get; private set; }
    public float SpaceWindMaxAngularVelocity { get; private set; }
    public float SpaceWindKnockdownTime { get; private set; }
    public bool SpaceWindAllowKnockdown { get; private set; }
    public bool SpaceWindVisuals { get; private set; }
    public float HumanoidThrowMultiplier { get; private set; }

    private void InitializeCVars()
    {
        Subs.CVar(_cfg, ClawCCVars.SpaceWindStrengthMultiplier, value => SpaceWindStrengthMultiplier = value, true);
        Subs.CVar(_cfg, ClawCCVars.SpaceWindMaxForce, value => SpaceWindMaxForce = value, true);
        Subs.CVar(_cfg, ClawCCVars.SpaceWindMaxAngularVelocity, value => SpaceWindMaxAngularVelocity = value, true);
        Subs.CVar(_cfg, ClawCCVars.SpaceWindKnockdownTime, value => SpaceWindKnockdownTime = value, true);
        Subs.CVar(_cfg, ClawCCVars.SpaceWindAllowKnockdown, value => SpaceWindAllowKnockdown = value, true);
        Subs.CVar(_cfg, ClawCCVars.SpaceWindVisuals, value => SpaceWindVisuals = value, true);
        Subs.CVar(_cfg, ClawCCVars.AtmosHumanoidThrowMultiplier, value => HumanoidThrowMultiplier = value, true);
    }
}
