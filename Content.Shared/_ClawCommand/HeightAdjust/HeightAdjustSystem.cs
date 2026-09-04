using System.Numerics;
using Content.Shared.Humanoid;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.HeightAdjust;

public sealed partial class HeightAdjustSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private HumanoidProfileSystem _appearance = default!;


    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided float scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, float scale, bool restricted = true) // Floofstation - added restricted flag, only set false if you know what you're doing!
    {
        return SetScale(uid, new Vector2(scale, scale), restricted: restricted); // Floofstation - added restricted flag
    }

    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided Vector2 scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, Vector2 scale, bool restricted = true) // Floofstation - added restricted flag, only set false if you know what you're doing!
    {

        float width = scale.X;
        float height = scale.Y;
        var adjScale = new Vector2(width, height);

        var succeeded = true;
        var avg = (scale.X + scale.Y) / 2;

        if (TryComp<FixturesComponent>(uid, out var fixtures))
            foreach (var fixture in fixtures.Fixtures)
                _physics.SetRadius(uid, fixture.Key, fixture.Value, fixture.Value.Shape, MathF.MinMagnitude(fixture.Value.Shape.Radius * avg, 0.49f));
        else
            succeeded = false;

        var finalScale = restricted ? scale : adjScale;
        _appearance.ApplyScale(uid, finalScale.X, finalScale.Y);

        RaiseLocalEvent(uid, new HeightAdjustedEvent { NewScale = restricted ? scale : adjScale }); // Floofstation - if restricted is false send the adjusted scale out to properly inform of the new size being set

        return succeeded;
    }
}
