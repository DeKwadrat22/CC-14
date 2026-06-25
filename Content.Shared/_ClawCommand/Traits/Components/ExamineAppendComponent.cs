using Robust.Shared.Serialization;

namespace Content.Shared._ClawCommand.Traits.Components;

/// <summary>
///     A single entry to append when examining an entity.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ExamineEntry
{
    [DataField]
    public string Text = "";

    [DataField]
    public string Color = "#ffffff";

    [DataField]
    public int FontSize = 12;

    [DataField]
    public bool NeedsProximity = true;
}

/// <summary>
///     Appends extra description text when an entity is examined.
/// </summary>
[RegisterComponent]
public sealed partial class ExamineAppendComponent : Component
{
    [DataField]
    public List<ExamineEntry> Entries = new();
}
