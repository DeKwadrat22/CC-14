using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Humanoid; // Claw Command
using Content.Shared.Interaction; // Claw Command
using Content.Shared._ClawCommand.InteractionVerbs.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared._ClawCommand.InteractionVerbs.InteractionPopupPrototype.Prefix;
using static Content.Shared._ClawCommand.InteractionVerbs.InteractionVerbPrototype.ContestType;
using static Content.Shared._ClawCommand.InteractionVerbs.InteractionVerbPrototype.EffectTargetSpecifier;

namespace Content.Shared._ClawCommand.InteractionVerbs;

public abstract partial class SharedInteractionVerbsSystem : EntitySystem
{
    private readonly InteractionAction.VerbDependencies _deps = new();
    private List<InteractionVerbPrototype> _globalProtos = default!;

    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAudioSystem _sfx = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSys = default!;
    [Dependency] private INetManager _netMgr = default!;
    [Dependency] private SharedPopupSystem _popupSys = default!;
    [Dependency] private IPrototypeManager _protoMgr = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!; // Claw Command
    [Dependency] private HumanoidProfileSystem _humanoid = default!; // Claw Command

    public override void Initialize()
    {
        IoCManager.InjectDependencies(_deps);

        CacheGlobalVerbs();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(HandlePrototypesReloaded);

        SubscribeLocalEvent<InteractionVerbsComponent, GetVerbsEvent<InteractionVerb>>(HandleGetOthersVerbs);
        SubscribeLocalEvent<OwnInteractionVerbsComponent, GetVerbsEvent<InnateVerb>>(HandleGetOwnVerbs);
        SubscribeLocalEvent<InteractionVerbDoAfterEvent>(HandleDoAfterCompleted);
    }

    private void CacheGlobalVerbs()
    {
        _globalProtos = _protoMgr.EnumeratePrototypes<InteractionVerbPrototype>()
            .Where(v => v is { Global: true, Abstract: false })
            .ToList();
    }

    #region event handling

    private void HandlePrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<InteractionVerbPrototype>())
            return;

        CacheGlobalVerbs();
    }

    private void HandleGetOthersVerbs(Entity<InteractionVerbsComponent> ent, ref GetVerbsEvent<InteractionVerb> ev)
    {
        // Global verbs are not added here since HandleGetOwnVerbs already adds them
        PopulateVerbs(ent.Comp.AllowedVerbs.Select(_protoMgr.Index), ev, () => new InteractionVerb());
    }

    private void HandleGetOwnVerbs(Entity<OwnInteractionVerbsComponent> ent, ref GetVerbsEvent<InnateVerb> ev)
    {
        var verbList = ent.Comp.AllowedVerbs;

        var getVerbsEv = new GetInteractionVerbsEvent(verbList);
        RaiseLocalEvent(ent, ref getVerbsEv);

        // Global verbs are added here because they should be allowed even on entities that do not define any interactions
        PopulateVerbs(verbList.Select(_protoMgr.Index).Union(_globalProtos), ev, () => new InnateVerb());
    }

    private void HandleDoAfterCompleted(InteractionVerbDoAfterEvent doAfterEv)
    {
        if (doAfterEv.Cancelled || doAfterEv.Handled || !_protoMgr.TryIndex(doAfterEv.VerbPrototype, out var verbProto))
            return;

        PerformVerb(verbProto, doAfterEv.VerbArgs!);
        doAfterEv.Handled = true;
    }

    #endregion

    #region public api

    /// <summary>
    ///     Starts the verb, checking if it can be performed first, unless forced.
    ///     Upon success, this method will either start a do-after, or pass control to <see cref="PerformVerb"/>.
    /// </summary>
    // TODO this function is an active battlefield
    public bool StartVerb(InteractionVerbPrototype proto, InteractionArgs ctx, bool force = false)
    {
        if (!TryComp<OwnInteractionVerbsComponent>(ctx.User, out var ownVerbs)
            || !force && !IsCooldownReady(proto, ctx, out _, ownVerbs))
            return false;

        // If contest advantage wasn't calculated yet, calculate it now and ensure it's in the allowed range
        var advantageOk = true;
        if (ctx.ContestAdvantage is null)
            ComputeAdvantage(proto, ref ctx, out advantageOk);

        if (!_netMgr.IsClient
            && !force
            && (!advantageOk || proto.Action?.CanPerform(ctx, proto, true, _deps) != true))
        {
            EmitVerbEffects(proto.EffectFailure, Fail, proto, ctx);
            return false;
        }

        var attemptEv = new InteractionVerbAttemptEvent(proto, ctx);
        RaiseLocalEvent(ctx.User, ref attemptEv);
        RaiseLocalEvent(ctx.Target, ref attemptEv);

        if (attemptEv.Cancelled)
        {
            EmitVerbEffects(proto.EffectFailure, Fail, proto, ctx);
            return false;
        }
        if (attemptEv.Handled)
            return true;

        var cd = proto.Cooldown;
        var wait = proto.Delay;
        if (proto.ContestDelay)
            wait /= ctx.ContestAdvantage!.Value;
        if (proto.ContestCooldown)
            cd /= ctx.ContestAdvantage!.Value;

        BeginCooldown(proto, ctx, cd, ownVerbs);

        // Delay can become zero if the contest advantage is infinity or just really large...
        if (wait <= TimeSpan.Zero)
        {
            PerformVerb(proto, ctx);
            return true;
        }

        var doAfterCfg = new DoAfterArgs(proto.DoAfter)
        {
            User = ctx.User,
            Target = ctx.Target,
            EventTarget = EntityUid.Invalid, // Raised broadcast
            Broadcast = true,
            BreakOnHandChange = proto.RequiresHands,
            NeedHand = proto.RequiresHands,
            RequireCanInteract = proto.RequiresCanInteract,
            Delay = wait,
            Event = new InteractionVerbDoAfterEvent(proto.ID, ctx),
        };

        var started = _doAfterSys.TryStartDoAfter(doAfterCfg);
        if (started)
            EmitVerbEffects(proto.EffectDelayed, Delayed, proto, ctx);

        return started;
    }

    /// <summary>
    ///     Performs an additional CanPerform check (unless forced) and then actually performs the action of the verb
    ///     and shows a success/failure popup.
    /// </summary>
    /// <remarks>This does nothing on client, as the client has no clue about verb actions. Only the server should ever perform verbs.</remarks>
    public void PerformVerb(InteractionVerbPrototype proto, InteractionArgs ctx, bool force = false)
    {
        if (_netMgr.IsClient)
            return; // this leads to issues

        if (!RunChecks(proto, ref ctx, out _, out _) && !force
            || !proto.Action!.CanPerform(ctx, proto, false, _deps) && !force
            || !proto.Action.Perform(ctx, proto, _deps))
        {
            EmitVerbEffects(proto.EffectFailure, Fail, proto, ctx);
            return;
        }

        EmitVerbEffects(proto.EffectSuccess, Success, proto, ctx);
    }

    #endregion

    #region private api

    /// <summary>
    ///     Creates verbs for all listed prototypes that match their own requirements. Uses the provided factory to create new verb instances.
    /// </summary>
    // Note: using `where T : Verb, new()` here results in a sandbox violation... Yea we peasants don't get OOP in ss14.
    private void PopulateVerbs<T>(IEnumerable<InteractionVerbPrototype> protos, GetVerbsEvent<T> ev, Func<T> factory) where T : Verb
    {
        // Don't add verbs to ghosts. Ghost system will also cancel all verbs by/on non-admin ghosts.
        if (TryComp<GhostComponent>(ev.User, out var ghostComp) && !ghostComp.CanGhostInteract)
            return;

        var ownVerbs = EnsureComp<OwnInteractionVerbsComponent>(ev.User);
        foreach (var proto in protos)
        {
            DebugTools.AssertNotEqual(proto.Abstract, true, "Attempted to add a verb with an abstract prototype.");

            var label = proto.Name;
            if (ev.Verbs.Any(v => v.Text == label))
                continue;

            var ctx = InteractionArgs.From(ev);
            var enabled = RunChecks(proto, ref ctx, out var shouldSkip, out var errLocale);

            if (shouldSkip)
                continue;

            var verb = factory.Invoke();
            ApplyVerbData(proto, verb);
            verb.Act = () => StartVerb(proto, ctx);
            verb.Disabled = !enabled;

            if (!enabled)
                verb.Message = Loc.GetString(errLocale!);

            if (enabled && !IsCooldownReady(proto, ctx, out var timeLeft, ownVerbs))
            {
                verb.Disabled = true;
                verb.Message = Loc.GetString("interaction-verb-cooldown", ("seconds", timeLeft.TotalSeconds));
            }

            ev.Verbs.Add(verb);
        }
    }

    /// <summary>
    ///     Performs all requirement/action checks on the verb. Returns true if the verb can be executed right now.
    ///     The shouldSkip output param indicates whether the caller should skip adding this verb to the verb list, if applicable.
    /// </summary>
    private bool RunChecks(InteractionVerbPrototype proto, ref InteractionArgs ctx, out bool shouldSkip, [NotNullWhen(false)] out string? errLocale)
    {
        if (!proto.AllowSelfInteract && ctx.User == ctx.Target
            || !Transform(ctx.User).Coordinates.TryDistance(EntityManager, Transform(ctx.Target).Coordinates, out var dist))
        {
            shouldSkip = true;
            errLocale = "interaction-verb-invalid-target";
            return false;
        }

        if (proto.Requirement?.IsMet(ctx, proto, _deps) == false)
        {
            shouldSkip = proto.HideByRequirement;
            errLocale = "interaction-verb-invalid";
            return false;
        }

        // TODO: we skip this check since the client is not aware of actions. This should be changed, maybe make actions mixed server/client?
        if (proto.Action?.IsAllowed(ctx, proto, _deps) != true && !_netMgr.IsClient)
        {
            shouldSkip = proto.HideWhenInvalid;
            errLocale = "interaction-verb-invalid";
            return false;
        }

        shouldSkip = false;
        if (proto.RequiresHands && !ctx.HasHands)
        {
            errLocale = "interaction-verb-no-hands";
            return false;
        }

        if (proto.RequiresConsciousness && !_blocker.CanConsciouslyPerformAction(ctx.User))
        {
            errLocale = "interaction-verb-unconscious";
            return false;
        }

        // Claw Command - resolve this verb's reach for this particular user, then check against it.
        var range = proto.Range;
        var accessRange = proto.AccessRange;

        if (proto.MinHeightRange is { } minHeightRange)
        {
            // Taller characters reach further. Interpolate between the short and tall reach, and use
            // the result for the accessibility check too so it never caps the scaled value.
            range.Max = float.Lerp(minHeightRange, range.Max, _humanoid.GetHeightFraction(ctx.User));
            accessRange = range.Max;
        }

        // A verb with its own access range re-runs the accessibility check at that range instead of
        // using ctx.CanAccess, which is fixed at the global interaction range and would otherwise cap
        // Range at 1.5 no matter what the prototype asks for.
        var canAccess = accessRange is { } ar
            ? _interaction.InRangeAndAccessible(ctx.User, ctx.Target, ar)
            : ctx.CanAccess;

        if (proto.RequiresCanInteract && !ctx.CanInteract || proto.RequiresCanAccess && !canAccess || !range.IsInRange(dist))
        {
            errLocale = "interaction-verb-cannot-reach";
            return false;
        }

        // Calculate contest advantage early if required
        if (proto.ContestAdvantageRange is not null)
        {
            ComputeAdvantage(proto, ref ctx, out var allowed);

            if (!allowed)
            {
                errLocale = "interaction-verb-too-" + (ctx.ContestAdvantage > 1f ? "strong" : "weak");
                return false;
            }
        }

        errLocale = null;
        return true;
    }

    /// <summary>
    ///     Calculates the effective contest advantage for the verb and writes their clamped value to <see cref="InteractionArgs.ContestAdvantage"/>.
    /// </summary>
    private void ComputeAdvantage(InteractionVerbPrototype proto, ref InteractionArgs ctx, out bool isAllowed)
    {
        ctx.ContestAdvantage = 1f;
        isAllowed = true;

        var contestFlags = proto.AllowedContests;
        if (contestFlags == None)
            return;

        isAllowed = proto.ContestAdvantageRange?.IsInRange(ctx.ContestAdvantage.Value) ?? true;
        ctx.ContestAdvantage = proto.ContestAdvantageLimit.Clamp(ctx.ContestAdvantage.Value);
    }

    private void ApplyVerbData(InteractionVerbPrototype proto, Verb verb)
    {
        verb.Text = proto.Name;
        verb.Message = proto.Description;
        verb.DoContactInteraction = proto.DoContactInteraction;
        verb.Priority = proto.Priority;
        verb.Icon = proto.Icon;
        verb.Category = new VerbCategory("verb-categories-interaction", null);
    }

    /// <summary>
    ///     Checks if the verb is on cooldown. Returns true if the verb can be used right now.
    /// </summary>
    private bool IsCooldownReady(InteractionVerbPrototype proto, InteractionArgs ctx, out TimeSpan timeLeft, OwnInteractionVerbsComponent? comp = null)
    {
        timeLeft = TimeSpan.Zero;
        if (!Resolve(ctx.User, ref comp))
            return false;

        var cdKey = proto.GlobalCooldown ? EntityUid.Invalid : ctx.Target;
        if (!comp.Cooldowns.TryGetValue((proto.ID, cdKey), out var expiresAt))
            return true;

        timeLeft = expiresAt - _gameTiming.CurTime;
        return timeLeft <= TimeSpan.Zero;
    }

    private void BeginCooldown(InteractionVerbPrototype proto, InteractionArgs ctx, TimeSpan duration, OwnInteractionVerbsComponent? comp = null)
    {
        if (!Resolve(ctx.User, ref comp))
            return;

        var cdKey = proto.GlobalCooldown ? EntityUid.Invalid : ctx.Target;
        comp.Cooldowns[(proto.ID, cdKey)] = _gameTiming.CurTime + duration;

        // We also clean up old cooldowns here to avoid a memory leak... This is probably a bad place to do it.
        // TODO might wanna switch to a list because dict is probably overkill for this task given we clean it up often.
        foreach (var (entry, expiry) in comp.Cooldowns.ToArray())
        {
            if (expiry < _gameTiming.CurTime)
                comp.Cooldowns.Remove(entry);
        }
    }

    private void EmitVerbEffects(InteractionVerbPrototype.EffectSpecifier? spec, InteractionPopupPrototype.Prefix prefix, InteractionVerbPrototype proto, InteractionArgs ctx)
    {
        // Not doing effects on client because it causes issues
        if (spec is null || _netMgr.IsClient)
            return;

        var (performer, tgt, heldItem) = (ctx.User, ctx.Target, ctx.Used);

        // Effect targets for different players
        var userPopupTarget = spec.EffectTarget is User or UserThenTarget or TargetThenUser ? performer : tgt;
        var targetPopupTarget = spec.EffectTarget is Target or UserThenTarget or TargetThenUser ? tgt : performer;
        var othersPopupTarget = spec.EffectTarget is Target or UserThenTarget ? tgt : performer;
        var bystanderFilter = Filter.Pvs(othersPopupTarget).RemoveWhereAttachedEntity(uid => uid == performer || uid == tgt);

        // Popups
        if (_protoMgr.TryIndex(spec.Popup, out var popupProto))
        {
            var locBase = $"interaction-{proto.ID}-{prefix.ToString().ToLower()}";

            (string, object)[] locParams =
            [
                ("user", Identity.Entity(performer, EntityManager)),
                ("target", Identity.Entity(tgt, EntityManager)),
                ("used", heldItem ?? EntityUid.Invalid),
                ("selfTarget", performer == tgt),
                ("hasUsed", heldItem != null)
            ];

            // User popup
            var selfKey = popupProto.SelfSuffix ?? popupProto.OthersSuffix;
            if (selfKey is not null)
                ShowPopup(Loc.GetString($"{locBase}-{selfKey}-popup", locParams), userPopupTarget, Filter.Entities(performer), false, popupProto);

            // Target popup
            var tgtKey = popupProto.TargetSuffix ?? popupProto.OthersSuffix;
            if (tgtKey is not null && performer != tgt)
                ShowPopup(Loc.GetString($"{locBase}-{tgtKey}-popup", locParams), targetPopupTarget, Filter.Entities(tgt), false, popupProto);

            // Others popup
            var othersKey = popupProto.OthersSuffix;
            if (othersKey is not null)
                ShowPopup(Loc.GetString($"{locBase}-{othersKey}-popup", locParams), othersPopupTarget, bystanderFilter, true, popupProto, clip: true);
        }

        // Sounds
        if (spec.Sound is { } snd)
        {
            // TODO we have a choice between having an accurate sound source or saving on an entity spawn...
            _sfx.PlayEntity(snd, Filter.Entities(performer, tgt), tgt, false, spec.SoundParams ?? snd.Params);

            if (spec.SoundPerceivedByOthers)
                _sfx.PlayEntity(snd, bystanderFilter, othersPopupTarget, false, spec.SoundParams ?? snd.Params);
        }
    }

    private void ShowPopup(string msg, EntityUid anchor, Filter recipients, bool recordReplay, InteractionPopupPrototype popupCfg, bool clip = false)
    {
        // Sending a chat message will result in a popup anyway
        // TODO this needs to be fixed probably. Popups and chat messages should be independent.
        if (popupCfg.LogPopup)
            SendChatLog(msg, anchor, recipients, popupCfg, clip);
        else
            _popupSys.PopupEntity(msg, anchor, recipients, recordReplay, popupCfg.PopupType);
    }

    protected virtual void SendChatLog(string message, EntityUid source, Filter filter, InteractionPopupPrototype popup, bool clip)
    {
    }

    #endregion
}
