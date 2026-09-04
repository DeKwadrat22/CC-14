using Content.Server.Destructible;
using Content.Shared._Shitcode.Heretic.Systems;

namespace Content.Server._Shitcode.Heretic.EntitySystems.PathSpecific;

public sealed partial class RustChargeSystem : SharedRustChargeSystem
{
    [Dependency] private DestructibleSystem _destructible = default!;

    protected override void DestroyStructure(EntityUid uid, EntityUid user)
    {
        base.DestroyStructure(uid, user);

        if (!TryComp(uid, out DestructibleComponent? destructible) || destructible.Thresholds.Count == 0)
        {
            Del(uid);
            return;
        }

        var threshold = destructible.Thresholds[^1];
        RaiseLocalEvent(uid, new DamageThresholdReached(destructible, threshold), true);
        // Upstream DamageThreshold has no public Execute; the DamageThresholdReached event
        // above is what destructible behaviors hook into. Rust path keeps the visual smash.
        foreach (var behavior in threshold.Behaviors)
            behavior.Execute(uid, _destructible, user);
    }
}
