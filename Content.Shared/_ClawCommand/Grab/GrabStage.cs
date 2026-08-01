// Grab intent ported from the space fork (Goobstation lineage).
// Lives in the PullingSystem namespace so PullerComponent/PullableComponent can reference it
// exactly as they do upstream.
namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
///     How firmly a puller has hold of their target. Each press of the pull key while in combat mode
///     walks one step up this ladder: pulling -> soft grab -> hard grab -> choke.
/// </summary>
public enum GrabStage
{
    No = 0,
    Soft = 1,
    Hard = 2,
    Suffocate = 3,
}

public enum GrabStageDirection
{
    Increase,
    Decrease,
}
