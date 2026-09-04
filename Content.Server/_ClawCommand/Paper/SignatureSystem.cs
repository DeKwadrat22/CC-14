using Content.Shared._ClawCommand.Paper;
using Content.Shared.Access.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.Paper;

/// <summary>
///     CLAW COMMAND - lets a player SIGN a paper/document by alt-clicking it while holding any writing
///     implement (an item with the <c>Write</c> tag, e.g. a pen). Ported from space/DeltaV and adapted to
///     this fork's shared <see cref="PaperSystem"/>. A signature is a <see cref="StampDisplayInfo"/> pushed
///     into the paper's stamp list with the signer's identity name and the <c>paper_stamp-signature</c>
///     sprite, so it reuses the whole existing stamp render / examine / UI pipeline (and, like any stamp,
///     locks the paper from further normal-pen edits). The original's Devil-antag coupling is dropped; the
///     generic Sign* extension events are kept so a contract antag can hook in later.
/// </summary>
public sealed partial class SignatureSystem : EntitySystem
{
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly ProtoId<TagPrototype> WriteTag = "Write";
    private const string SignatureStampState = "paper_stamp-signature";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Must be holding a writing implement - the same Write tag the paper's own writing already uses.
        if (args.Using is not { } pen || !_tags.HasTag(pen, WriteTag))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () => TrySignPaper(ent, user, pen),
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10,
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Applies <paramref name="signer"/>'s signature to <paramref name="paper"/> using
    ///     <paramref name="pen"/>. Returns false if a hook vetoes it or the same signer already signed.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        var comp = paper.Comp;

        // Extension hooks: the pen may veto (e.g. a broken pen), then the paper may veto (e.g. a contract
        // that only accepts certain signers).
        var penEv = new SignAttemptEvent(paper, signer);
        RaiseLocalEvent(pen, ref penEv);
        if (penEv.Cancelled)
            return false;

        var paperEv = new BeingSignedAttemptEvent(paper, signer);
        RaiseLocalEvent(paper.Owner, ref paperEv);
        if (paperEv.Cancelled)
            return false;

        var stampInfo = new StampDisplayInfo
        {
            StampedName = DetermineEntitySignature(signer),
            StampedColor = Color.DarkSlateGray, // TODO could be made pen-configurable
        };

        // The same signer can't sign the same paper twice.
        if (comp.StampedBy.Contains(stampInfo))
        {
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper.Owner)), signer, signer, PopupType.SmallCaution);
            return false;
        }

        _paper.TryStamp(paper, stampInfo, SignatureStampState);

        _popup.PopupEntity(Loc.GetString("paper-signed-other", ("user", signer), ("target", paper.Owner)),
            signer, Filter.PvsExcept(signer), true);
        _popup.PopupEntity(Loc.GetString("paper-signed-self", ("target", paper.Owner)), signer, signer);

        _audio.PlayPvs(comp.Sound, signer);

        // Refresh an open paper window so the new signature shows in its stamp list immediately.
        _paper.UpdateUserInterface(paper);

        var successEv = new SignSuccessfulEvent(paper, signer);
        RaiseLocalEvent(paper.Owner, ref successEv);

        return true;
    }

    /// <summary>
    ///     The name a signature is signed with: the signer's ID-card full name if they have one, otherwise
    ///     their (identity) entity name.
    /// </summary>
    private string DetermineEntitySignature(EntityUid uid)
    {
        if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
            return id.Comp.FullName;

        return Name(uid);
    }
}
