using Content.Shared._ClawCommand.Devices.Components;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;

namespace Content.Server._ClawCommand.Devices.Systems;

public sealed partial class ServerPinpointerCrewSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private SharedPinpointerSystem _sharedPinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerCrewComponent, BoundUIOpenedEvent>(OpenUIEvent);
        SubscribeLocalEvent<PinpointerComponent, CrewTrackerSelectCrewMessage>(ChangeTarget);
    }

    private float _timePassed = 0;

    public override void Update(float frameTime)
    {
        _timePassed += frameTime;
        if (_timePassed > 3)
        {
            _timePassed = 0;
            var pointers = EntityQueryEnumerator<PinpointerCrewComponent, PinpointerComponent>();
            while (pointers.MoveNext(out var pointerId, out var pinpointerCrewComponent, out var pinpointerComponent))
            {
                if (pinpointerComponent.IsActive == false)
                    continue;

                var sensors = EntityQueryEnumerator<SuitSensorComponent>();
                var found = false;
                while (sensors.MoveNext(out var sensorId, out var sensorComponent))
                {
                    if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid user)
                    {
                        if (user == pinpointerComponent.Target)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    _sharedPinpointerSystem.SetActive((pointerId, pinpointerComponent), false);
                    _sharedPinpointerSystem.SetTarget((pointerId, pinpointerComponent), null);
                }
            }
        }
    }

    private void ChangeTarget(EntityUid uid, PinpointerComponent pinpointer, CrewTrackerSelectCrewMessage args)
    {
        if (args.ID is not int targetID)
            return;

        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        while (sensors.MoveNext(out var sensorId, out var sensorComponent))
        {
            if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid user)
            {
                _sharedPinpointerSystem.SetTarget((uid, pinpointer), new EntityUid(targetID));
                _sharedPinpointerSystem.SetActive((uid, pinpointer), true);
            }
        }
    }

    private void OpenUIEvent(EntityUid uid, PinpointerCrewComponent component, BoundUIOpenedEvent arg)
    {
        if (!_entityManager.TryGetComponent<PinpointerComponent>(uid, out var pinpointerComponent))
            return;

        PinpointedCrew? target = null;
        if (pinpointerComponent.Target is EntityUid targetEnt && pinpointerComponent.TargetName is String targetName)
        {
            target = new PinpointedCrew();
            target.ID = targetEnt.Id;
            target.Name = targetName;
        }

        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        var crewList = new List<PinpointedCrew>();

        while (sensors.MoveNext(out var sensorId, out var sensorComponent))
        {
            if (sensorComponent.Mode == SuitSensorMode.SensorCords && sensorComponent.User is EntityUid user)
            {
                if (!_entityManager.TryGetComponent<MetaDataComponent>(user, out var metadataComponent))
                    continue;

                var crew = new PinpointedCrew();
                crew.ID = user.Id;
                crew.Name = metadataComponent.EntityName;
                crewList.Add(crew);
            }
        }

        var newState = new PinpointerCrewBoundUserInterfaceState(target, crewList);
        _userInterface.SetUiState(uid, PinpointerCrewUiKey.Key, newState);
    }
}
