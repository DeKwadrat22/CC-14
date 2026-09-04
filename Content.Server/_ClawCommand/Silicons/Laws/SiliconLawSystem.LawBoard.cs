using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// CLAW COMMAND - law board handling for anything that physically holds a board (borgs), as opposed to
/// the AI upload console, which broadcasts a board's laws to every station AI at once.
/// </summary>
public sealed partial class SiliconLawSystem
{
    /// <summary>
    /// Copies a law board's lawset onto <paramref name="target"/> and tells its player the laws changed.
    /// The lawset is cloned so the borg emagging itself later doesn't scribble on the board's own copy.
    /// </summary>
    public void ApplyLawBoard(EntityUid target, Entity<SiliconLawProviderComponent> board)
    {
        var lawset = (board.Comp.Lawset ?? GetLawset(board.Comp.Laws)).Clone();

        var provider = EnsureComp<SiliconLawProviderComponent>(target);
        provider.Laws = board.Comp.Laws;
        provider.Lawset = lawset;
        provider.LawUploadSound = board.Comp.LawUploadSound;

        NotifyLawsChanged((target, provider), board.Comp.LawUploadSound);
    }

    /// <summary>
    /// Strips a silicon of its laws entirely - it keeps its provider component, but the lawset is empty
    /// so it doesn't fall back to the station's lawset.
    /// </summary>
    public void ClearLaws(EntityUid target)
    {
        if (!TryComp<SiliconLawProviderComponent>(target, out var provider))
            return;

        provider.Lawset = new SiliconLawset();

        NotifyLawsChanged((target, provider));
    }
}
