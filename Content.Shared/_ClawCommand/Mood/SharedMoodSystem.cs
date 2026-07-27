using Content.Shared.CCVar;
using Content.Shared.Movement.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Mood;

/// <summary>
///     Shared half of the mood system. Provides the helpers other shared systems use to raise moodlets,
///     and predicts the movement speed modifier from the networked <see cref="NetMoodComponent"/> so that
///     mood-driven speed changes don't desync the client.
/// </summary>
/// <remarks>
///     The authoritative moodlet bookkeeping lives in the server-only <c>MoodSystem</c>.
/// </remarks>
public sealed partial class SharedMoodSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedJetpackSystem _jetpack = default!;

    /// <summary>
    ///     Prefix for the short, out-of-character name of a moodlet. Used by guidebook text.
    /// </summary>
    public const string LocMoodEffectNamePrefix = "mood-effect-name-";

    /// <summary>
    ///     Prefix for the short, out-of-character name of a moodlet category. Used by guidebook text.
    /// </summary>
    public const string LocMoodCategoryNamePrefix = "mood-category-name-";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetMoodComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
    }

    /// <summary>
    ///     Applies a moodlet to an entity. Does nothing if the entity has no mood.
    /// </summary>
    public void AddMoodlet(EntityUid uid, ProtoId<MoodEffectPrototype> effectId, float modifier = 1f, float offset = 0f)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent(effectId, modifier, offset));
    }

    /// <summary>
    ///     Removes a moodlet from an entity. If the moodlet defines a
    ///     <see cref="MoodEffectPrototype.MoodletOnEnd"/>, that replacement is applied.
    /// </summary>
    public void RemoveMoodlet(EntityUid uid, ProtoId<MoodEffectPrototype> effectId)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent(effectId));
    }

    /// <summary>
    ///     The mood level of an entity, relative to its neutral threshold. 1 is neutral, 2 is a perfect mood.
    ///     Returns 1 for entities without a mood.
    /// </summary>
    public float GetMoodRatio(EntityUid uid)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !TryComp<NetMoodComponent>(uid, out var mood)
            || mood.NeutralMoodThreshold <= 0f)
            return 1f;

        return mood.CurrentMoodLevel / mood.NeutralMoodThreshold;
    }

    private void OnRefreshMoveSpeed(Entity<NetMoodComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var comp = ent.Comp;

        if (!_config.GetCVar(CCVars.MoodEnabled)
            || comp.CurrentMoodThreshold is > MoodThreshold.Meh and < MoodThreshold.Good or MoodThreshold.Dead
            || _jetpack.IsUserFlying(ent))
            return;

        // This ridiculous math serves a purpose: making high mood less impactful on movement speed than low mood.
        // Positive mood follows a slow geometric curve, negative mood follows a linear one.
        float modifier;
        if (comp.CurrentMoodLevel >= comp.NeutralMoodThreshold)
        {
            modifier = _config.GetCVar(CCVars.MoodIncreasesSpeed)
                ? MathF.Pow(comp.SpeedBonusGrowth, comp.CurrentMoodLevel - comp.NeutralMoodThreshold)
                : 1f;
        }
        else
        {
            // A mood level of 0 divides to -Infinity here, which clamps to MinimumSpeedModifier. That is intended.
            modifier = _config.GetCVar(CCVars.MoodDecreasesSpeed)
                ? 2f - comp.NeutralMoodThreshold / comp.CurrentMoodLevel
                : 1f;
        }

        args.ModifySpeed(1f, Math.Clamp(modifier, comp.MinimumSpeedModifier, comp.MaximumSpeedModifier));
    }
}
