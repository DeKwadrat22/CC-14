using Content.Shared.Humanoid;
using Content.Shared.Damage.Components; // Claw Command - InjurableComponent now carries the damage container
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Speech;
using Content.Shared.Speech.Muting;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.CombatMode;
using Content.Shared.Nutrition.Components;
using Content.Shared.Abilities.Psionics;
using Content.Server.Abilities.Psionics;
using Content.Server.Psionics.Glimmer;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Chat;

/// <summary>
///     Psychognomy: what a psion "sees" when another entity speaks telepathically. Each subscribed component
///     contributes one or more descriptors, and one is picked at random, so the reading is suggestive rather
///     than a clean identification.
/// </summary>
/// <remarks>
///     Claw Command - two upstream descriptor sources were dropped because the systems behind them do not exist
///     in this fork:
///     <list type="bullet">
///     <item>MetempsychosisKarmaComponent ("cyclic") - Nyanotrasen's metempsychotic cloning karma.</item>
///     <item>BloodSuckerComponent ("vampiric") - Nyanotrasen's vampirism system.</item>
///     </list>
///     The "vampiric" descriptor is still reachable through <see cref="DescribeGlimmerSource"/>, so no locale
///     string is orphaned. If either system is ever ported, re-add the subscription here.
/// </remarks>
public sealed partial class TelepathicChatSystem
{
    public string SourceToDescriptor(EntityUid source)
    {
        var ev = new GetPsychognomicDescriptorEvent();
        RaiseLocalEvent(source, ev);

        ev.Descriptors.Add(Loc.GetString("p-descriptor-ignorant"));

        return _random.Pick(ev.Descriptors);
    }
    private void InitializePsychognomy()
    {
        // Claw Command - HumanoidAppearanceComponent was replaced by HumanoidProfileComponent in this fork.
        SubscribeLocalEvent<HumanoidProfileComponent, GetPsychognomicDescriptorEvent>(DescribeHumanoid);
        SubscribeLocalEvent<GrammarComponent, GetPsychognomicDescriptorEvent>(DescribeGrammar);
        // Claw Command - the damage container moved from DamageableComponent to InjurableComponent.
        SubscribeLocalEvent<InjurableComponent, GetPsychognomicDescriptorEvent>(DescribeDamage);
        SubscribeLocalEvent<MobStateComponent, GetPsychognomicDescriptorEvent>(DescribeMobState);
        SubscribeLocalEvent<HungerComponent, GetPsychognomicDescriptorEvent>(DescribeHunger);
        SubscribeLocalEvent<PhysicsComponent, GetPsychognomicDescriptorEvent>(DescribePhysics);
        SubscribeLocalEvent<FixturesComponent, GetPsychognomicDescriptorEvent>(DescribeFixtures);
        SubscribeLocalEvent<GlimmerSourceComponent, GetPsychognomicDescriptorEvent>(DescribeGlimmerSource);
        SubscribeLocalEvent<PsionicComponent, GetPsychognomicDescriptorEvent>(DescribePsion);
        SubscribeLocalEvent<InnatePsionicPowersComponent, GetPsychognomicDescriptorEvent>(DescribeInnatePsionics);
    }

    private void DescribeHumanoid(EntityUid uid, HumanoidProfileComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.Sex != Sex.Unsexed)
            ev.Descriptors.Add(component.Sex == Sex.Male ? Loc.GetString("p-descriptor-male") : Loc.GetString("p-descriptor-female"));

        // Deliberately obfuscating a bit; psionic signatures use different standards
        ev.Descriptors.Add(component.Age >= 100 ? Loc.GetString("p-descriptor-old") : Loc.GetString("p-descriptor-young"));
    }

    private void DescribeGrammar(EntityUid uid, GrammarComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.Gender == Gender.Male || component.Gender == Gender.Female)
            ev.Descriptors.Add(component.Gender == Gender.Male ? Loc.GetString("p-descriptor-masculine") : Loc.GetString("p-descriptor-feminine"));
    }

    private void DescribeDamage(EntityUid uid, InjurableComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.DamageContainer == "ManifestedSpirit")
        {
            ev.Descriptors.Add(Loc.GetString("p-descriptor-liminal"));
            if (!HasComp<HumanoidProfileComponent>(uid))
                ev.Descriptors.Add(Loc.GetString("p-descriptor-old"));
            return;
        }

        ev.Descriptors.Add(Loc.GetString("p-descriptor-hylic"));
    }

    private void DescribeMobState(EntityUid uid, MobStateComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.CurrentState != Shared.Mobs.MobState.Critical)
            return;

        ev.Descriptors.Add(Loc.GetString("p-descriptor-liminal"));
    }

    private void DescribeHunger(EntityUid uid, HungerComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.CurrentThreshold > HungerThreshold.Peckish)
            return;

        ev.Descriptors.Add(Loc.GetString("p-descriptor-hungry"));
    }

    private void DescribeFixtures(EntityUid uid, FixturesComponent component, GetPsychognomicDescriptorEvent ev)
    {
        foreach (var fixture in component.Fixtures.Values)
            if (fixture.CollisionMask == (int) CollisionGroup.GhostImpassable)
            {
                ev.Descriptors.Add(Loc.GetString("p-descriptor-pneumatic"));
                return;
            }
    }

    private void DescribePhysics(EntityUid uid, PhysicsComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.FixturesMass < 45)
            ev.Descriptors.Add(Loc.GetString("p-descriptor-light"));
        else if (component.FixturesMass > 70)
            ev.Descriptors.Add(Loc.GetString("p-descriptor-heavy"));
    }

    private void DescribeGlimmerSource(EntityUid uid, GlimmerSourceComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.AddToGlimmer)
            ev.Descriptors.Add(Loc.GetString("p-descriptor-emanative"));
        else
        {
            ev.Descriptors.Add(Loc.GetString("p-descriptor-vampiric"));
            ev.Descriptors.Add(Loc.GetString("p-descriptor-hungry"));
        }
    }

    // This one's also a bit of a catch-all for "lacks component"
    private void DescribePsion(EntityUid uid, PsionicComponent component, GetPsychognomicDescriptorEvent ev)
    {
        if (component.PsychognomicDescriptors.Count > 0)
        {
            foreach (var descriptor in component.PsychognomicDescriptors)
            {
                ev.Descriptors.Add(Loc.GetString(descriptor));
            }
        }

        if (!HasComp<SpeechComponent>(uid) || HasComp<MutedComponent>(uid))
            ev.Descriptors.Add(Loc.GetString("p-descriptor-dumb"));

        if (!HasComp<CombatModeComponent>(uid) || HasComp<PacifiedComponent>(uid))
            ev.Descriptors.Add(Loc.GetString("p-descriptor-passive"));

        foreach (var power in component.ActivePowers)
        {
            // TODO: Mime counts too and isn't added back to psions yet
            if (power.ID != "PyrokinesisPower" && power.ID != "NoosphericZapPower")
                continue;

            ev.Descriptors.Add(Loc.GetString("p-descriptor-kinetic"));
            return;
        }
    }

    private void DescribeInnatePsionics(EntityUid uid, InnatePsionicPowersComponent component, GetPsychognomicDescriptorEvent ev)
    {
        ev.Descriptors.Add(Loc.GetString("p-descriptor-gnostic"));
    }
}
public sealed class GetPsychognomicDescriptorEvent : EntityEventArgs
{
    public List<String> Descriptors = new List<String>();
}
