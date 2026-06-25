using Content.Server.Heretic.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Store;

namespace Content.Server.Store.Systems;

/// <summary>
/// _ClawCommand: routes <c>productHereticKnowledge</c> listings through HereticSystem so the
/// buyer's path/stage actually advances. Without this the store would deduct points and
/// remove the listing but never grant the knowledge, leaving CurrentPath unset and every
/// other path's starter still visible.
/// </summary>
public sealed partial class StoreSystem
{
    [Dependency] private HereticSystem _heretic = default!;

    private void TryGrantHereticKnowledge(EntityUid buyer, ListingDataWithCostModifiers listing)
    {
        if (listing.ProductHereticKnowledge is not { } knowledge)
            return;

        if (!Mind.TryGetMind(buyer, out var mindId, out var mind))
            return;

        _heretic.TryAddKnowledge(mindId, knowledge, mind.CurrentEntity);
    }
}
