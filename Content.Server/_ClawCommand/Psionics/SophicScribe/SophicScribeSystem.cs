using Content.Server.Abilities.Psionics;
using Content.Server.Chat.Systems;
using Content.Shared.Radio.Components; // Claw Command - Radio components moved to Shared
using Content.Server.Radio.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Nyanotrasen.Research.SophicScribe;

public sealed partial class SophicScribeSystem : EntitySystem
{
    // Claw Command - RA0033: IPrototypeManager.Index forbids literals.
    private static readonly ProtoId<RadioChannelPrototype> ScienceChannel = "Science";
    private static readonly ProtoId<RadioChannelPrototype> CommonChannel = "Common";
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private GlimmerSystem _glimmerSystem = default!;
    [Dependency] private RadioSystem _radioSystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_glimmerSystem.Glimmer == 0)
            return; // yes, return. Glimmer value is global.

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<SophicScribeComponent>();
        while (query.MoveNext(out var scribe, out var scribeComponent))
        {
            if (curTime < scribeComponent.NextAnnounceTime)
                continue;

            if (!TryComp<IntrinsicRadioTransmitterComponent>(scribe, out var radio))
                continue;

            var message = Loc.GetString("glimmer-report", ("level", _glimmerSystem.Glimmer));
            var channel = _prototypeManager.Index(ScienceChannel);
            if (_glimmerSystem.Glimmer > 250)
            {
                channel = _prototypeManager.Index(CommonChannel);
            }
            _radioSystem.SendRadioMessage(scribe, message, channel, scribe);

            scribeComponent.NextAnnounceTime = curTime + scribeComponent.AnnounceInterval;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SophicScribeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<GlimmerEventEndedEvent>(OnGlimmerEventEnded);
    }

    private void OnInteractHand(EntityUid uid, SophicScribeComponent component, InteractHandEvent args)
    {
        //TODO: the update function should be removed eventually too.
        if (_timing.CurTime < component.StateTime)
            return;

        component.StateTime = _timing.CurTime + component.StateCD;

        _chat.TrySendInGameICMessage(uid, Loc.GetString("glimmer-report", ("level", _glimmerSystem.Glimmer)), InGameICChatType.Speak, true);
    }

    private void OnGlimmerEventEnded(GlimmerEventEndedEvent args)
    {
        var query = EntityQueryEnumerator<SophicScribeComponent>();
        while (query.MoveNext(out var scribe, out _))
        {
            if (!TryComp<IntrinsicRadioTransmitterComponent>(scribe, out var radio)) return;

            // mind entities when...
            var speaker = scribe;
            if (TryComp<MindSwappedComponent>(scribe, out var swapped))
            {
                speaker = swapped.OriginalEntity;
            }

            var message = Loc.GetString(args.Message, ("decrease", args.GlimmerBurned), ("level", _glimmerSystem.Glimmer));
            var channel = _prototypeManager.Index(CommonChannel);
            _radioSystem.SendRadioMessage(speaker, message, channel, speaker);
        }
    }
}
