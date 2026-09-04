using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
///     Component that denotes a piece of clothing that blocks the mouth or otherwise prevents eating & drinking.
/// </summary>
/// <remarks>
///     In the event that more head-wear & mask functionality is added (like identity systems, or raising/lowering of
///     masks), then this component might become redundant.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(IngestionSystem))]
public sealed partial class IngestionBlockerComponent : Component
{
    /// <summary>
    ///     Whether this item currently blocks consuming something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    // Claw Command: ported from Goob-Station — when true and worn in the mask slot,
    // this passively prevents smoke from being ingested into the bloodstream even
    // without internals enabled. Gives "real" gas masks meaningful smoke protection.
    [DataField, AutoNetworkedField]
    public bool BlockSmokeIngestion;
}
