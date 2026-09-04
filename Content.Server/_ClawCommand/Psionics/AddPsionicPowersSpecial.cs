using Content.Server.Abilities.Psionics;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

/// <summary>
///     Grants specific psionic powers on spawn, making the holder psionic if they were not already.
/// </summary>
/// <remarks>
///     Claw Command - replaces upstream's <c>TraitAddPsionics</c> trait function. That fork's trait system ran
///     arbitrary <c>functions:</c> on a trait; this one applies <see cref="JobSpecial"/>s instead, so the same
///     capability is expressed as a special that both traits and jobs can use.
/// </remarks>
public sealed partial class AddPsionicPowersSpecial : JobSpecial
{
    /// <summary>
    ///     The powers to grant. Each is initialised exactly once, so listing a power the entity already has is a no-op.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<PsionicPowerPrototype>> PsionicPowers = new();

    /// <summary>
    ///     How many power slots the holder gets. Exceeding this by acquiring more powers turns the holder into a
    ///     glimmer source, so roles that are meant to carry several powers should raise it.
    ///     Null leaves whatever the entity already has.
    /// </summary>
    [DataField]
    public int? PowerSlots;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var psionicAbilities = entMan.System<PsionicAbilitiesSystem>();

        var psionic = entMan.EnsureComponent<PsionicComponent>(mob);

        if (PowerSlots is { } slots)
            psionic.PowerSlots = slots;

        foreach (var powerId in PsionicPowers)
        {
            if (!protoMan.TryIndex(powerId, out var power))
                continue;

            psionicAbilities.InitializePsionicPower(mob, power, psionic, false);
        }
    }
}

/// <summary>
///     Makes the holder a Latent Psychic: psionic, but with no powers to start with. They can still roll for
///     powers over the round like any other psion.
/// </summary>
/// <remarks>
///     Claw Command - the Latent Psychic trait upstream simply added a bare PsionicComponent. This exists so that
///     the same thing can be expressed as a special rather than through the obsolete <c>components:</c> field.
/// </remarks>
public sealed partial class MakeLatentPsychicSpecial : JobSpecial
{
    /// <summary>
    ///     Multiplies how quickly this psion accrues Potentia, and so how readily they roll new powers.
    /// </summary>
    [DataField]
    public float PotentiaMultiplier = 1f;

    /// <summary>
    ///     Added to the baseline chance of a power roll succeeding.
    /// </summary>
    [DataField]
    public float PowerRollFlatBonus;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var psionic = entMan.EnsureComponent<PsionicComponent>(mob);

        psionic.PowerRollMultiplier *= PotentiaMultiplier;
        psionic.PowerRollFlatBonus += PowerRollFlatBonus;
    }
}

/// <summary>
///     Removes psionic insulation, letting an otherwise insulated entity become psionic.
/// </summary>
/// <remarks>
///     Claw Command - backs the Anomalous Positronics trait, which is how an IPC opts into psionics.
///     IPCs carry PsionicInsulation by default, and insulation blocks every psionic power.
/// </remarks>
public sealed partial class RemovePsionicInsulationSpecial : JobSpecial
{
    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        entMan.RemoveComponent<PsionicInsulationComponent>(mob);
    }
}
