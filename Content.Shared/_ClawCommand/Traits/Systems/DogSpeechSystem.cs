using Content.Shared.Speech;
using Content.Shared._ClawCommand.Traits.Components;

namespace Content.Shared._ClawCommand.Traits.Systems;

public sealed class DogSpeechSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DogSpeechComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, DogSpeechComponent comp, ComponentStartup args)
    {
        if (TryComp<SpeechComponent>(uid, out var speech))
        {
            speech.SpeechSounds = "Dog";
        }
    }
}
