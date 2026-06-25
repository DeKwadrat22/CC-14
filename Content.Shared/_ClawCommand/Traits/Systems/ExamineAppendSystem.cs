using Content.Shared.Examine;
using Content.Shared._ClawCommand.Traits.Components;

namespace Content.Shared._ClawCommand.Traits.Systems;

/// <summary>
///     Pushes additional markup from ExamineAppendComponent when an entity is examined.
/// </summary>
public sealed class ExamineAppendSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExamineAppendComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ExamineAppendComponent comp, ExaminedEvent args)
    {
        if (comp.Entries.Count <= 0)
            return;

        foreach (var entry in comp.Entries)
        {
            if (!args.IsInDetailsRange && entry.NeedsProximity)
                continue;

            args.PushMarkup($"[font size={entry.FontSize}][color={entry.Color}]{Loc.GetString(entry.Text, ("entity", uid))}[/color][/font]");
        }
    }
}
