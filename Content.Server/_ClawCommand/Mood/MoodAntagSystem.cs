using Content.Shared._ClawCommand.Mood;
using Content.Shared.Revolutionary.Components;

namespace Content.Server._ClawCommand.Mood;

/// <summary>
///     Grants a morale bonus to antagonists whose conviction is tracked by a component on the mob itself,
///     and takes it away again when that conviction ends (deconversion, for instance).
/// </summary>
/// <remarks>
///     Antags whose role is only recorded on the mind - traitors, heretics - apply their moodlet from
///     their own rule system instead, since there is no component on the body to hang this off.
/// </remarks>
public sealed partial class MoodAntagSystem : EntitySystem
{
    [Dependency] private SharedMoodSystem _mood = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ComponentStartup is already taken by SharedRevolutionarySystem, and Robust permits only one
        // handler per component/event pair, so hang off ComponentInit instead.
        SubscribeLocalEvent<RevolutionaryComponent, ComponentInit>(OnRevInit);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentShutdown>(OnRevShutdown);
    }

    private void OnRevInit(Entity<RevolutionaryComponent> ent, ref ComponentInit args)
    {
        _mood.AddMoodlet(ent.Owner, "RevolutionFocused");
    }

    private void OnRevShutdown(Entity<RevolutionaryComponent> ent, ref ComponentShutdown args)
    {
        _mood.RemoveMoodlet(ent.Owner, "RevolutionFocused");
    }
}
