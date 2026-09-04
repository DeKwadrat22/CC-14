// Claw Command: one central answer to "is this entity trying to stay unseen?".
//
// Effects that would give someone away - the sprint dust cloud and its puff sound, for now - ask this
// instead of hardcoding a list of stealth sources, so adding a new way to hide doesn't mean editing
// every consumer. A new stealth state just subscribes to GetStealthModeEvent.

using Content.Shared._ClawCommand.Shadekin.Components;
using Content.Shared.Stealth.Components;

namespace Content.Shared._ClawCommand.Stealth;

/// <summary>
///     Raised on an entity to ask whether it is currently in stealth. Sources are purely additive: a
///     handler sets <see cref="Stealthed"/> to true, nothing ever sets it back to false, so one source
///     saying "hidden" can't be undone by another that has no opinion.
/// </summary>
[ByRefEvent]
public record struct GetStealthModeEvent(bool Stealthed = false);

/// <summary>
///     Collects the stealth states an entity can be in. Register new sources here, or subscribe to
///     <see cref="GetStealthModeEvent"/> from the system that owns the state.
/// </summary>
public sealed class StealthModeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthComponent, GetStealthModeEvent>(OnCloaked);
        SubscribeLocalEvent<EtherealComponent, GetStealthModeEvent>(OnEthereal);
    }

    /// <summary>
    ///     The space ninja's suit cloak: ToggleClothing drives a ComponentToggler with parent: true, which puts
    ///     StealthComponent on the wearer for as long as the cloak is up. Catches anything else that cloaks the
    ///     same way, which is the usual route.
    /// </summary>
    private void OnCloaked(Entity<StealthComponent> ent, ref GetStealthModeEvent args)
    {
        if (ent.Comp.Enabled)
            args.Stealthed = true;
    }

    /// <summary>
    ///     A shadekin phased out with their skip ability.
    /// </summary>
    private void OnEthereal(Entity<EtherealComponent> ent, ref GetStealthModeEvent args)
    {
        args.Stealthed = true;
    }

    /// <summary>
    ///     Whether the entity is currently hiding, and so shouldn't be producing effects that announce it.
    /// </summary>
    public bool IsStealthed(EntityUid uid)
    {
        var ev = new GetStealthModeEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Stealthed;
    }
}
