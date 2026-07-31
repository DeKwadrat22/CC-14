using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// CLAW COMMAND - marks a borg chassis whose laws come from a law board slotted into it instead of
/// being hardcoded on the chassis prototype. The board lives in an <c>ItemSlots</c> slot
/// (<see cref="SlotId"/>) and is installed by clicking the chassis with the board in hand, which runs
/// a do-after. Installing / removing a board rewrites the borg's laws (see the server-side
/// <c>BorgLawBoardSystem</c>), and a borg with no board at all can't be activated.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BorgLawBoardComponent : Component
{
    /// <summary>
    /// The <c>ItemSlots</c> slot the law board sits in.
    /// </summary>
    [DataField]
    public string SlotId = "borg_lawboard";

    /// <summary>
    /// How long installing a law board takes.
    /// </summary>
    [DataField]
    public TimeSpan InstallDelay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Whether the maintenance panel has to be unscrewed before the board can be swapped.
    /// </summary>
    [DataField]
    public bool RequirePanelOpen = true;

    /// <summary>
    /// Whether the chassis has to be unlocked before the board can be swapped.
    /// </summary>
    [DataField]
    public bool RequireUnlocked = true;
}
