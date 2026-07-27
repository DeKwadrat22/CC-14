using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared._ClawCommand.Mood;
using Content.Shared.Alert;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._ClawCommand.Mood;

/// <summary>
///     Authoritative half of the mood system. Tracks moodlets, sums them into a mood level, and applies
///     the consequences: the mood alert, a movement speed modifier, a desaturation overlay, and
///     optionally a shifted critical damage threshold.
/// </summary>
public sealed partial class MoodSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private PopupSystem _popup = default!;

    /// <summary>
    ///     Moodlet expiry doesn't need per-tick resolution, so it's batched onto this interval.
    /// </summary>
    private static readonly TimeSpan ExpiryCheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextExpiryCheck;

    /// <summary>
    ///     Reused between expiry passes to avoid mutating dictionaries while enumerating them.
    /// </summary>
    private readonly List<ProtoId<MoodCategoryPrototype>> _expiredCategories = new();
    private readonly List<ProtoId<MoodEffectPrototype>> _expiredEffects = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<MoodComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MoodComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MoodComponent, MoodEffectEvent>(OnMoodEffect);
        SubscribeLocalEvent<MoodComponent, MoodRemoveEffectEvent>(OnRemoveEffect);
        SubscribeLocalEvent<MoodComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<MoodComponent, ShowMoodEffectsAlertEvent>(OnShowMoodEffects);

        SubscribeLocalEvent<PermanentMoodletsComponent, ComponentStartup>(OnPermanentMoodlets);
        SubscribeLocalEvent<MoodModifierComponent, ComponentStartup>(OnMoodModifier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextExpiryCheck)
            return;

        _nextExpiryCheck = _timing.CurTime + ExpiryCheckInterval;

        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        var query = EntityQueryEnumerator<MoodComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CategorisedExpiry.Count == 0 && comp.UncategorisedExpiry.Count == 0)
                continue;

            ExpireMoodlets(uid, comp);
        }
    }

    private void ExpireMoodlets(EntityUid uid, MoodComponent component)
    {
        _expiredCategories.Clear();
        _expiredEffects.Clear();

        foreach (var (category, expiry) in component.CategorisedExpiry)
        {
            if (expiry <= _timing.CurTime)
                _expiredCategories.Add(category);
        }

        foreach (var (effect, expiry) in component.UncategorisedExpiry)
        {
            if (expiry <= _timing.CurTime)
                _expiredEffects.Add(effect);
        }

        if (_expiredCategories.Count == 0 && _expiredEffects.Count == 0)
            return;

        foreach (var category in _expiredCategories)
        {
            if (component.CategorisedEffects.TryGetValue(category, out var protoId))
                RemoveEffect(uid, component, protoId, category);
        }

        foreach (var effect in _expiredEffects)
            RemoveEffect(uid, component, effect);
    }

    private void OnInit(EntityUid uid, MoodComponent component, ComponentStartup args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (_config.GetCVar(CCVars.MoodModifiesThresholds)
            && TryComp<MobThresholdsComponent>(uid, out var mobThresholdsComponent)
            && _mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var critThreshold, mobThresholdsComponent))
            component.CritThresholdBeforeModify = critThreshold.Value;

        var netMood = EnsureComp<NetMoodComponent>(uid);
        netMood.SpeedBonusGrowth = component.SpeedBonusGrowth;
        netMood.MinimumSpeedModifier = component.MinimumSpeedModifier;
        netMood.MaximumSpeedModifier = component.MaximumSpeedModifier;
        Dirty(uid, netMood);

        RefreshMood(uid, component);
    }

    private void OnShutdown(EntityUid uid, MoodComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, component.MoodCategory);
        RemComp<SaturationScaleOverlayComponent>(uid);
    }

    private void OnPermanentMoodlets(Entity<PermanentMoodletsComponent> ent, ref ComponentStartup args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        foreach (var moodlet in ent.Comp.Moodlets)
            RaiseLocalEvent(ent.Owner, new MoodEffectEvent(moodlet));
    }

    private void OnMoodModifier(Entity<MoodModifierComponent> ent, ref ComponentStartup args)
    {
        // Moodlets that were already applied need to be re-tallied through the new multipliers.
        RefreshMood(ent.Owner);
    }

    #region Applying and removing moodlets

    private void OnMoodEffect(EntityUid uid, MoodComponent component, MoodEffectEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !_proto.TryIndex(args.EffectId, out var prototype))
            return;

        var ev = new OnMoodEffect(uid, args.EffectId, args.EffectModifier, args.EffectOffset);
        RaiseLocalEvent(uid, ref ev);

        ApplyEffect(uid, component, prototype, ev.EffectModifier, ev.EffectOffset);
    }

    private void ApplyEffect(EntityUid uid, MoodComponent component, MoodEffectPrototype prototype, float eventModifier = 1f, float eventOffset = 0f)
    {
        // Applying a moodlet always restarts its timeout, whether or not it was already present.
        if (prototype.Category is { } category)
        {
            // Don't send the moodlet popup if we already have this exact moodlet.
            if (!component.CategorisedEffects.TryGetValue(category, out var oldProtoId) || oldProtoId != prototype.ID)
                SendEffectText(uid, prototype);

            component.CategorisedEffects[category] = prototype.ID;

            if (prototype.Timeout != 0)
                component.CategorisedExpiry[category] = _timing.CurTime + TimeSpan.FromSeconds(prototype.Timeout);
            else
                component.CategorisedExpiry.Remove(category);
        }
        else
        {
            var moodChange = prototype.MoodChange * eventModifier + eventOffset;
            if (moodChange == 0)
                return;

            if (!component.UncategorisedEffects.ContainsKey(prototype.ID))
                SendEffectText(uid, prototype);

            component.UncategorisedEffects[prototype.ID] = moodChange;

            if (prototype.Timeout != 0)
                component.UncategorisedExpiry[prototype.ID] = _timing.CurTime + TimeSpan.FromSeconds(prototype.Timeout);
            else
                component.UncategorisedExpiry.Remove(prototype.ID);
        }

        RefreshMood(uid, component);
    }

    private void OnRemoveEffect(EntityUid uid, MoodComponent component, MoodRemoveEffectEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (component.UncategorisedEffects.ContainsKey(args.EffectId))
        {
            RemoveEffect(uid, component, args.EffectId);
            return;
        }

        foreach (var (category, id) in component.CategorisedEffects)
        {
            if (id != args.EffectId)
                continue;

            RemoveEffect(uid, component, args.EffectId, category);
            return;
        }
    }

    /// <summary>
    ///     Drops a moodlet, applies its replacement moodlet if it has one, and recalculates mood.
    /// </summary>
    private void RemoveEffect(EntityUid uid, MoodComponent component, ProtoId<MoodEffectPrototype> prototypeId, ProtoId<MoodCategoryPrototype>? category = null)
    {
        if (category is { } cat)
        {
            if (!component.CategorisedEffects.TryGetValue(cat, out var currentProtoId)
                || currentProtoId != prototypeId)
                return;

            component.CategorisedEffects.Remove(cat);
            component.CategorisedExpiry.Remove(cat);
        }
        else
        {
            if (!component.UncategorisedEffects.Remove(prototypeId))
                return;

            component.UncategorisedExpiry.Remove(prototypeId);
        }

        ReplaceMood(uid, prototypeId);
        RefreshMood(uid, component);
    }

    /// <summary>
    ///     Some moods specifically create a moodlet upon expiration. This is normally used for "Addiction" type moodlets,
    ///     such as a positive moodlet from an addictive substance that becomes a negative moodlet when a timer ends.
    /// </summary>
    /// <remarks>
    ///     Moodlets that use this should probably also share a category with each other, but this isn't necessarily required.
    ///     Only if you intend that "Re-using the drug" should also remove the negative moodlet.
    /// </remarks>
    private void ReplaceMood(EntityUid uid, ProtoId<MoodEffectPrototype> prototypeId)
    {
        if (!_proto.TryIndex(prototypeId, out var proto)
            || proto.MoodletOnEnd is not { } replacement)
            return;

        RaiseLocalEvent(uid, new MoodEffectEvent(replacement));
    }

    private void SendEffectText(EntityUid uid, MoodEffectPrototype prototype)
    {
        if (prototype.Hidden)
            return;

        _popup.PopupEntity(prototype.Description, uid, uid, prototype.MoodChange > 0 ? PopupType.Medium : PopupType.MediumCaution);
    }

    #endregion

    #region Mood level and thresholds

    /// <summary>
    ///     Recalculate the mood level of an entity by summing up all moodlets.
    /// </summary>
    /// <remarks>
    ///     Call this after changing anything that feeds into mood math without going through a moodlet,
    ///     such as an entity's <see cref="MoodModifierComponent"/>.
    /// </remarks>
    public void RefreshMood(EntityUid uid)
    {
        if (TryComp<MoodComponent>(uid, out var component))
            RefreshMood(uid, component);
    }

    /// <summary>
    ///     Recalculate the mood level of an entity by summing up all moodlets.
    /// </summary>
    private void RefreshMood(EntityUid uid, MoodComponent component)
    {
        var amount = 0f;
        TryComp<MoodModifierComponent>(uid, out var modifier);

        foreach (var (_, protoId) in component.CategorisedEffects)
        {
            if (!_proto.TryIndex(protoId, out var prototype))
                continue;

            amount += ApplyMoodModifier(protoId, prototype.Category, prototype.MoodChange, modifier);
        }

        foreach (var (protoId, value) in component.UncategorisedEffects)
            amount += ApplyMoodModifier(protoId, null, value, modifier);

        SetMood(uid, amount, component, refresh: true);
    }

    /// <summary>
    ///     Scales one moodlet's contribution by the entity's mood modifiers, if it has any. The most specific
    ///     multiplier wins; they don't stack with each other.
    /// </summary>
    private static float ApplyMoodModifier(
        ProtoId<MoodEffectPrototype> protoId,
        ProtoId<MoodCategoryPrototype>? category,
        float moodChange,
        MoodModifierComponent? modifier)
    {
        if (modifier is null || moodChange == 0f)
            return moodChange;

        if (modifier.EffectMultipliers.TryGetValue(protoId, out var effectMultiplier))
            return moodChange * effectMultiplier;

        if (category is { } moodCategory && modifier.CategoryMultipliers.TryGetValue(moodCategory, out var categoryMultiplier))
            return moodChange * categoryMultiplier;

        return moodChange * (moodChange > 0f ? modifier.PositiveMultiplier : modifier.NegativeMultiplier);
    }

    private void SetMood(EntityUid uid, float amount, MoodComponent? component = null, bool force = false, bool refresh = false)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !Resolve(uid, ref component)
            || component.CurrentMoodThreshold == MoodThreshold.Dead && !refresh)
            return;

        var neutral = component.MoodThresholds[MoodThreshold.Neutral];
        var ev = new OnSetMoodEvent(uid, amount, false);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return;

        uid = ev.Receiver;
        amount = ev.MoodChangedAmount;

        var newMoodLevel = amount + neutral;
        if (!force)
            newMoodLevel = Math.Clamp(
                amount + neutral,
                component.MoodThresholds[MoodThreshold.Dead],
                component.MoodThresholds[MoodThreshold.Perfect]);

        component.CurrentMoodLevel = newMoodLevel;

        UpdateCurrentThreshold(uid, component);
        SyncNetMood(uid, component);
    }

    private void SyncNetMood(EntityUid uid, MoodComponent component)
    {
        if (!TryComp<NetMoodComponent>(uid, out var netMood))
            return;

        netMood.CurrentMoodLevel = component.CurrentMoodLevel;
        netMood.NeutralMoodThreshold = component.MoodThresholds.GetValueOrDefault(MoodThreshold.Neutral);
        netMood.CurrentMoodThreshold = component.CurrentMoodThreshold;
        Dirty(uid, netMood);
    }

    private void UpdateCurrentThreshold(EntityUid uid, MoodComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var calculatedThreshold = GetMoodThreshold(component);
        if (calculatedThreshold == component.CurrentMoodThreshold)
            return;

        component.CurrentMoodThreshold = calculatedThreshold;

        DoMoodThresholdsEffects(uid, component);
    }

    private void DoMoodThresholdsEffects(EntityUid uid, MoodComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component)
            || component.CurrentMoodThreshold == component.LastThreshold && !force)
            return;

        var modifier = GetMovementThreshold(component.CurrentMoodThreshold);

        // Modify mob stats
        if (modifier != GetMovementThreshold(component.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            SetCritThreshold(uid, component, modifier);
            RefreshShaders(uid, modifier);
        }

        // Modify interface
        if (component.MoodThresholdsAlerts.TryGetValue(component.CurrentMoodThreshold, out var alertId))
            _alerts.ShowAlert(uid, alertId);
        else
            _alerts.ClearAlertCategory(uid, component.MoodCategory);

        component.LastThreshold = component.CurrentMoodThreshold;
    }

    private void RefreshShaders(EntityUid uid, int modifier)
    {
        if (modifier == -1)
            EnsureComp<SaturationScaleOverlayComponent>(uid);
        else
            RemComp<SaturationScaleOverlayComponent>(uid);
    }

    private void SetCritThreshold(EntityUid uid, MoodComponent component, int modifier)
    {
        if (!_config.GetCVar(CCVars.MoodModifiesThresholds)
            || !TryComp<MobThresholdsComponent>(uid, out var mobThresholds)
            || !_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var key))
            return;

        var newKey = modifier switch
        {
            1 => FixedPoint2.New(key.Value.Float() * component.IncreaseCritThreshold),
            -1 => FixedPoint2.New(key.Value.Float() * component.DecreaseCritThreshold),
            _ => component.CritThresholdBeforeModify,
        };

        component.CritThresholdBeforeModify = key.Value;
        _mobThreshold.SetMobStateThreshold(uid, newKey, MobState.Critical, mobThresholds);
    }

    private MoodThreshold GetMoodThreshold(MoodComponent component, float? moodLevel = null)
    {
        moodLevel ??= component.CurrentMoodLevel;
        var result = MoodThreshold.Dead;
        var value = component.MoodThresholds[MoodThreshold.Perfect];

        foreach (var threshold in component.MoodThresholds)
        {
            if (threshold.Value <= value && threshold.Value >= moodLevel)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }

        return result;
    }

    private int GetMovementThreshold(MoodThreshold threshold) =>
        threshold switch
        {
            >= MoodThreshold.Good => 1,
            <= MoodThreshold.Meh => -1,
            _ => 0,
        };

    #endregion

    #region Reactions to the world

    private void OnMobStateChanged(EntityUid uid, MoodComponent component, MobStateChangedEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (args.NewMobState == MobState.Dead && args.OldMobState != MobState.Dead)
            RaiseLocalEvent(uid, new MoodEffectEvent("Dead"));
        else if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead)
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent("Dead"));

        RefreshMood(uid, component);
    }

    private void OnDamageChange(EntityUid uid, MoodComponent component, DamageChangedEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !_mobThreshold.TryGetPercentageForState(uid, MobState.Critical, _damageable.GetTotalDamage(uid), out var damage))
            return;

        ProtoId<MoodEffectPrototype> protoId = "HealthNoDamage";
        if (!component.HealthMoodEffectsThresholds.TryGetValue(protoId, out var value))
            return;

        foreach (var threshold in component.HealthMoodEffectsThresholds)
        {
            if (threshold.Value <= damage && threshold.Value >= value)
            {
                protoId = threshold.Key;
                value = threshold.Value;
            }
        }

        RaiseLocalEvent(uid, new MoodEffectEvent(protoId));
    }

    #endregion

    #region Alert readout

    private void OnShowMoodEffects(Entity<MoodComponent> ent, ref ShowMoodEffectsAlertEvent args)
    {
        if (args.Handled
            || ent.Comp.CurrentMoodThreshold == MoodThreshold.Dead
            || !_player.TryGetSessionByEntity(ent, out var session))
            return;

        args.Handled = true;

        var msgStart = Loc.GetString("mood-show-effects-start");
        _chat.ChatMessageToOne(ChatChannel.Emotes, msgStart, msgStart, EntityUid.Invalid, false, session.Channel);

        foreach (var (_, protoId) in ent.Comp.CategorisedEffects)
            SendDescToChat(protoId, session.Channel);

        foreach (var (protoId, _) in ent.Comp.UncategorisedEffects)
            SendDescToChat(protoId, session.Channel);
    }

    private void SendDescToChat(ProtoId<MoodEffectPrototype> protoId, Robust.Shared.Network.INetChannel channel)
    {
        if (!_proto.TryIndex(protoId, out var proto) || proto.Hidden)
            return;

        var color = proto.MoodChange > 0 ? "#008000" : "#BA0000";
        var msg = $"[font size=10][color={color}]{proto.Description}[/color][/font]";

        _chat.ChatMessageToOne(ChatChannel.Emotes, msg, msg, EntityUid.Invalid, false, channel);
    }

    #endregion
}
