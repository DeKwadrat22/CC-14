using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Localizations;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._ClawCommand.Roles;

/// <summary>
/// Like <see cref="RoleTimeRequirement"/>, but accepts a list of role
/// trackers and passes when the SUM of playtime across all of them meets
/// or exceeds <see cref="Time"/>. Used to gate Station AI behind any silicon
/// time — regular Borg time and any dogborg time both contribute.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AnyRoleTimeRequirement : Content.Shared.Roles.JobRequirement
{
    /// <summary>
    /// The role trackers whose playtimes get summed.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<PlayTimeTrackerPrototype>> Roles = new();

    /// <summary>
    /// Time threshold that the sum must meet.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        var total = TimeSpan.Zero;
        foreach (var role in Roles)
        {
            if (playTimes.TryGetValue(role.Id, out var t))
                total += t;
        }

        var diffSpan = Time - total;
        var formatted = ContentLocalizationManager.FormatPlaytime(diffSpan);
        var color = Color.Yellow;

        // Use the first role's department for color, like RoleTimeRequirement does.
        if (Roles.Count > 0
            && entManager.EntitySysManager.TryGetEntitySystem(out SharedJobSystem? jobSystem))
        {
            var firstJob = jobSystem.GetJobPrototype(Roles[0].Id);
            if (jobSystem.TryGetDepartment(firstJob, out var dept))
                color = dept.Color;
        }

        if (!Inverted)
        {
            if (diffSpan <= TimeSpan.Zero)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-any-insufficient",
                ("time", formatted),
                ("departmentColor", color.ToHex())));
            return false;
        }

        if (diffSpan <= TimeSpan.Zero)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-any-too-high",
                ("time", formatted),
                ("departmentColor", color.ToHex())));
            return false;
        }

        return true;
    }
}
