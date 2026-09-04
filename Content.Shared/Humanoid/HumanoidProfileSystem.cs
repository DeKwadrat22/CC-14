using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Sprite;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private SharedScaleVisualsSystem _scaleVisuals = default!; // Claw Command
    [Dependency] private _ClawCommand.Body.BodyWeightSystem _bodyWeight = default!; // Claw Command

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HumanoidProfileComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<HumanoidProfileComponent> ent, ref MapInitEvent args)
    {
        ApplyScale(ent, ent.Comp.Width, ent.Comp.Height);
        // Claw Command - driven from here rather than a second MapInit subscription in
        // BodyWeightSystem, which Robust rejects as a duplicate on the same component/event pair.
        _bodyWeight.RefreshWeight(new Entity<HumanoidProfileComponent?>(ent.Owner, ent.Comp));
    }

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Voice = profile.Voice;
        ent.Comp.CustomSpeciesName = profile.CustomSpeciesName; // Claw Command
        ent.Comp.Sex = profile.Sex;
        ent.Comp.Width = profile.Width;
        ent.Comp.Height = profile.Height;
        Dirty(ent);

        ApplyScale(ent, profile.Width, profile.Height);
        _bodyWeight.RefreshWeight((ent, ent.Comp)); // Claw Command - build now decides body weight

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, profile.Voice);
        RaiseLocalEvent(ent, ref voiceChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }
    }

    /// <summary>
    ///     Claw Command - Where this entity sits between the shortest and tallest height its species
    ///     allows, as 0 to 1. Used to scale how far a character can reach for social interactions.
    /// </summary>
    /// <remarks>
    ///     Returns 0 for anything without a humanoid profile - animals, silicons, and so on. Those get
    ///     the shortest reach, which is the range they had before reach was ever tied to height.
    /// </remarks>
    public float GetHeightFraction(Entity<HumanoidProfileComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !ProtoMan.TryIndex(ent.Comp.Species, out var species))
        {
            return 0f;
        }

        var span = species.MaxHeight - species.MinHeight;
        if (span <= 0f)
            return 0f;

        return Math.Clamp((ent.Comp.Height - species.MinHeight) / span, 0f, 1f);
    }

    public void ApplyScale(EntityUid uid, float width, float height)
    {
        // Claw Command - pinBottom: humanoids stand on the floor, so extra height belongs above
        // their feet. Without it the sprite sinks into the tile to the south and sorts in front of
        // things it is standing behind.
        _scaleVisuals.SetSpriteScale(uid, new Vector2(width, height), pinBottom: true);
    }

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        // Claw Command - use custom species name if set, otherwise use default species name
        var species = !string.IsNullOrWhiteSpace(ent.Comp.CustomSpeciesName)
            ? ent.Comp.CustomSpeciesName.ToLower()
            : GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (ProtoMan.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!ProtoMan.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
