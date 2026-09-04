using Content.Shared._ClawCommand.InteractionVerbs;
using Content.Shared.Jittering;

namespace Content.Server._ClawCommand.InteractionVerbs.Actions;

[Serializable]
public sealed partial class JitterAction : InteractionAction
{
    [DataField]
    public float Amplitude = 10f, Frequency = 4f;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(1);

    [DataField]
    public bool Refresh = false;

    public override bool CanPerform(InteractionArgs ctx, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        return true;
    }

    public override bool Perform(InteractionArgs ctx, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        deps.EntMan.System<SharedJitteringSystem>().DoJitter(ctx.Target, Time, Refresh, Amplitude, Frequency);
        return true;
    }
}
