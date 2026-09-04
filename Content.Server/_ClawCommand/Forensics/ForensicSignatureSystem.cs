using Content.Shared._ClawCommand.Forensics;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;

namespace Content.Server._ClawCommand.Forensics;

/// <summary>
/// Claw Command: keeps <see cref="ForensicSignatureComponent"/> mobs stamped with a readable
/// fingerprint and DNA string rather than a random one.
///
/// Two hooks, because the name is not known when the mob is created:
///   - <see cref="MapInitEvent"/>, ordered after <see cref="ForensicsSystem"/> so it overwrites the
///     random values that system has just generated. At this point the name is still the prototype
///     one, which is the right answer for an unplayed NPC.
///   - <see cref="EntityRenamedEvent"/>, which is what actually gets the character name in. Player
///     spawning renames the mob through MetaDataSystem.SetEntityName, and so does anything else
///     that renames a creature mid-round, so the signature follows the name for the whole shift.
/// </summary>
public sealed class ForensicSignatureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForensicSignatureComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(ForensicsSystem) });
        SubscribeLocalEvent<ForensicSignatureComponent, EntityRenamedEvent>(OnRenamed);
    }

    private void OnMapInit(Entity<ForensicSignatureComponent> ent, ref MapInitEvent args)
    {
        Apply(ent);
    }

    private void OnRenamed(Entity<ForensicSignatureComponent> ent, ref EntityRenamedEvent args)
    {
        Apply(ent);
    }

    /// <summary>
    /// Overwrite the mob's fingerprint and DNA with its signature. Prints and DNA already left on
    /// objects are not rewritten - evidence collected before a rename keeps the name it was
    /// collected under, which is the honest result.
    /// </summary>
    private void Apply(Entity<ForensicSignatureComponent> ent)
    {
        var signature = Loc.GetString(ent.Comp.Signature, ("name", Name(ent)));

        if (TryComp<FingerprintComponent>(ent, out var fingerprint))
        {
            fingerprint.Fingerprint = signature;
            Dirty(ent.Owner, fingerprint);
        }

        if (!TryComp<DnaComponent>(ent, out var dna))
            return;

        dna.DNA = signature;
        Dirty(ent.Owner, dna);

        // Bloodstream caches the DNA string for the solution it hands out, so it has to be told.
        var ev = new GenerateDnaEvent { Owner = ent.Owner, DNA = signature };
        RaiseLocalEvent(ent.Owner, ref ev);
    }
}
