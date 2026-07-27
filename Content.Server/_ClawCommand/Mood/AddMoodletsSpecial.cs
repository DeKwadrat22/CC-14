using Content.Shared._ClawCommand.Mood;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._ClawCommand.Mood;

/// <summary>
///     Gives an entity permanent moodlets, typically from a trait.
/// </summary>
/// <remarks>
///     Traits add their components with overwrite enabled, so two traits that both hand out
///     <see cref="PermanentMoodletsComponent"/> would clobber each other. This merges into whatever a
///     previous trait already gave instead.
/// </remarks>
[UsedImplicitly]
public sealed partial class AddMoodletsSpecial : JobSpecial
{
    [DataField(required: true)]
    public List<ProtoId<MoodEffectPrototype>> Moodlets { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();

        // Startup applies whatever is already in the list, so anything added afterwards is applied by hand.
        var comp = entMan.EnsureComponent<PermanentMoodletsComponent>(mob);
        var moodSystem = entMan.System<SharedMoodSystem>();

        foreach (var moodlet in Moodlets)
        {
            if (comp.Moodlets.Contains(moodlet))
                continue;

            comp.Moodlets.Add(moodlet);
            moodSystem.AddMoodlet(mob, moodlet);
        }
    }
}
