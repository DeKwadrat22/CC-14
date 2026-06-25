using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Devices.Components
{
    [Serializable, NetSerializable]
    public sealed class PinpointerCrewBoundUserInterfaceState : BoundUserInterfaceState
    {
        public PinpointedCrew? SelectedCrew { get; }
        public List<PinpointedCrew> List { get; set; } = new List<PinpointedCrew>();

        public PinpointerCrewBoundUserInterfaceState(PinpointedCrew? selectedCrew, List<PinpointedCrew> list)
        {
            SelectedCrew = selectedCrew;
            List = list;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CrewTrackerSelectCrewMessage : BoundUserInterfaceMessage
    {
        public int? ID { get; }

        public CrewTrackerSelectCrewMessage(int? id)
        {
            ID = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PinpointedCrew
    {
        public int ID;
        public String Name = "";
    }

    [RegisterComponent, NetworkedComponent]
    public sealed partial class PinpointerCrewComponent : Component { }

    [Serializable, NetSerializable]
    public enum PinpointerCrewUiKey : byte
    {
        Key,
    }
}
