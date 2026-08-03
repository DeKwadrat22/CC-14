using Robust.Shared.GameStates;

namespace Content.Shared._ClawCommand.Body;

/// <summary>
///     Claw Command - Carries the weight derived from a character's height and width sliders, and
///     remembers the prototype values it overwrote so it can be recomputed without drifting.
///
///     Added automatically to anything with a humanoid profile. See <see cref="BodyWeightSystem"/>
///     for what the weight actually feeds into.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodyWeightComponent : Component
{
    /// <summary>
    ///     Body weight in kilograms. Display value; the mechanics all key off <see cref="Scale"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Weight;

    /// <summary>
    ///     Standing height in centimetres. Display only.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float HeightCm;

    /// <summary>
    ///     Weight as a multiple of the species baseline. 1.0 is a default-built character, and every
    ///     effect below is expressed relative to it so an untouched slider changes nothing.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Scale = 1f;

    #region Prototype baselines

    // Everything below is captured the first time weight is applied, before anything is overwritten.
    // Recomputing then always works forward from the prototype value rather than compounding on top
    // of the last result, which is what would happen if we just multiplied in place.

    [ViewVariables]
    public bool CapturedBaselines;

    [ViewVariables]
    public float BaseDensity;

    [ViewVariables]
    public float BaseRadius;

    [ViewVariables]
    public float BaseHungerDecay;

    [ViewVariables]
    public float BaseThirstDecay;

    [ViewVariables]
    public Dictionary<Mobs.MobState, Content.Shared.FixedPoint.FixedPoint2> BaseThresholds = new();

    [ViewVariables]
    public Dictionary<EntityUid, Content.Shared.FixedPoint.FixedPoint2> BaseStomachCapacity = new();

    #endregion

    /// <summary>
    ///     How much of the weight difference carries into maximum health. Full strength would let a
    ///     heavyset character shrug off a third again as much damage as a slight one, which is far
    ///     too strong for a cosmetic slider, so only a fraction of it is passed through.
    /// </summary>
    [DataField]
    public float HealthInfluence = 0.35f;

    /// <summary>
    ///     How much of the weight difference carries into hunger and thirst burn rate. Bigger bodies
    ///     need more fuel, but a heavy character should not be tied to the kitchen.
    /// </summary>
    [DataField]
    public float MetabolismInfluence = 0.5f;

    /// <summary>
    ///     How much of the weight difference carries into stomach capacity.
    /// </summary>
    [DataField]
    public float CapacityInfluence = 0.75f;

    /// <summary>
    ///     How much of the weight difference resists alcohol. Body mass is most of what real alcohol
    ///     tolerance is, so this one runs close to full strength.
    /// </summary>
    [DataField]
    public float AlcoholInfluence = 0.9f;

    /// <summary>
    ///     How much of the width difference carries into physical size. Deliberately small: a mob
    ///     fixture wider than half a tile starts catching on doorways and corners, and getting stuck
    ///     on the geometry is a far worse outcome than a big character not feeling quite big enough.
    /// </summary>
    [DataField]
    public float CollisionInfluence = 0.5f;
}
