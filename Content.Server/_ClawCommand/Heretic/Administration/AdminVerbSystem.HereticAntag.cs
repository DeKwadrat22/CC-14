using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._ClawCommand.Heretic.Administration;

/// <summary>
/// Adds a "Make Heretic" admin verb alongside the upstream Make-Traitor / Make-Zombie /
/// Make-Wizard etc. verbs. Uses the same ForceMakeAntag pipeline as the others.
/// </summary>
public sealed partial class AdminVerbSystemHereticAntag : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;

    private static readonly EntProtoId DefaultHereticRule = "Heretic";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddHereticVerb);
    }

    private void AddHereticVerb(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        var hereticName = Loc.GetString("admin-verb-text-make-heretic");
        Verb heretic = new()
        {
            Text = hereticName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_ClawCommand/Interface/Misc/job_icons.rsi"), "HereticMaster"),
            Act = () =>
            {
                _antag.ForceMakeAntag<HereticRuleComponent>(targetPlayer, DefaultHereticRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", hereticName, Loc.GetString("admin-verb-make-heretic")),
        };
        args.Verbs.Add(heretic);
    }
}
