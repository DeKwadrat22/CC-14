using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._ClawCommand.Traits;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class EggLayerTraitComponent : Component
{
        /// <summary>
    ///     The item that gets laid/spawned, retrieved from animal prototype.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> EggSpawn = new();

    /// <summary>
    ///     Player action.
    /// </summary>
    [DataField]
    public EntProtoId EggLayAction = "ActionAnimalLayEgg";

    [DataField]
    public SoundSpecifier EggLaySound = new SoundPathSpecifier("/Audio/Effects/pop.ogg");

    /// <summary>
    ///     Minimum cooldown used for the automatic egg laying.
    /// </summary>
    [DataField]
    public float EggLayCooldownMin = 60f;

    /// <summary>
    ///     Maximum cooldown used for the automatic egg laying.
    /// </summary>
    [DataField]
    public float EggLayCooldownMax = 120f;

    /// <summary>
    ///     The amount of nutrient consumed on update.
    /// </summary>
    [DataField]
    public float HungerUsage = 60f;

    [DataField] public EntityUid? Action;

    /// <summary>
    ///     When to next try to produce.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextGrowth = TimeSpan.Zero;
}
