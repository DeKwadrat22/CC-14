using System.Linq;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Guidebook;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    private ColorSelectorSliders _rgbSkinColorSelector;
    private List<SpeciesPrototype> _species = new();
    private List<EmoteSoundsPrototype> _voices = new();
    private static readonly ProtoId<GuideEntryPrototype> DefaultSpeciesGuidebook = "Species";

    // CLAW COMMAND //
    // Throttle timer for slider updates
    private bool _sliderUpdatePending;

    public void UpdateSpeciesGuidebookIcon()
    {
        SpeciesInfoButton.StyleClasses.Clear();

        var species = Profile?.Species;
        if (species is null)
            return;

        if (!_prototypeManager.Resolve<SpeciesPrototype>(species, out var speciesProto))
            return;

        // Don't display the info button if no guide entry is found
        if (!_prototypeManager.HasIndex<GuideEntryPrototype>(species))
            return;

        const string style = "SpeciesInfoDefault";
        SpeciesInfoButton.StyleIdentifier = style;
    }

    private void UpdateGenderControls()
    {
        if (Profile == null)
        {
            return;
        }

        PronounsButton.SelectId((int)Profile.Gender);
    }

    private void UpdateAgeEdit()
    {
        AgeEdit.Text = Profile?.Age.ToString() ?? "";
    }

    private void UpdateSexControls()
    {
        if (Profile == null)
            return;

        SexButton.Clear();

        var sexes = new List<Sex>();

        // add species sex options, default to just none if we are in bizzaro world and have no species
        if (_prototypeManager.Resolve(Profile.Species, out var speciesProto))
        {
            foreach (var sex in speciesProto.Sexes)
            {
                sexes.Add(sex);
            }
        }
        else
        {
            sexes.Add(Sex.Unsexed);
        }

        // add button for each sex
        foreach (var sex in sexes)
        {
            SexButton.AddItem(Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLower()}-text"), (int)sex);
        }

        if (sexes.Contains(Profile.Sex))
            SexButton.SelectId((int)Profile.Sex);
        else
            SexButton.SelectId((int)sexes[0]);
    }

    private void UpdateEyePickers()
    {
        if (Profile == null)
        {
            return;
        }

        _markingsModel.SetOrganEyeColor(Profile.Appearance.EyeColor);
        EyeColorPicker.SetData(Profile.Appearance.EyeColor);
    }

    private void UpdateVoiceControls()
    {
        if (Profile == null)
            return;

        VoiceButton.Clear();
        _voices.Clear();

        var speciesPrototype = _prototypeManager.Index(Profile.Species);
        var availableVoices = speciesPrototype.Voices;

        _voices.AddRange(availableVoices.Select(protoId => _prototypeManager.Index(protoId)));

        if (_voices.All(proto => Profile?.Voice != proto.ID))
            SetVoice(speciesPrototype.DefaultSoundsBySex[(int)Profile.Sex]);

        for (var i = 0; i < availableVoices.Count; i++)
        {
            var name = Loc.GetString(_voices[i].VoiceSelectorName);
            VoiceButton.AddItem(name, i);

            if (Profile?.Voice.Equals(_voices[i].ID) == true)
            {
                VoiceButton.SelectId(i);
            }
        }
    }

    private void UpdateSkinColor()
    {
        if (Profile == null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;
        var strategy = _prototypeManager.Index(skin).Strategy;

        switch (strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    if (!Skin.Visible)
                    {
                        Skin.Visible = true;
                        RgbSkinColorContainer.Visible = false;
                    }

                    Skin.Value = strategy.ToUnary(Profile.Appearance.SkinColor);

                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    if (!RgbSkinColorContainer.Visible)
                    {
                        Skin.Visible = false;
                        RgbSkinColorContainer.Visible = true;
                    }

                    _rgbSkinColorSelector.Color = strategy.ClosestSkinColor(Profile.Appearance.SkinColor);

                    break;
                }
        }
    }

    private void UpdateSpawnPriorityControls()
    {
        if (Profile == null)
        {
            return;
        }

        SpawnPriorityButton.SelectId((int)Profile.SpawnPriority);
    }

        private void UpdateHeightWidthSliders()
    {
        if (Profile is null)
            return;

        var species = _species.Find(x => x.ID == Profile?.Species) ?? _species.First();
        var width1 = Profile?.Width ?? DefaultHeight;
        var height1 = Profile?.Height ?? DefaultHeight;

        WidthSlider.MinValue = MinCharWidth;
        WidthSlider.MaxValue = MaxCharWidth;
        WidthSlider.SetValueWithoutEvent(width1);

        HeightSlider.MinValue = MinCharHeight;
        HeightSlider.MaxValue = MaxCharHeight;
        HeightSlider.SetValueWithoutEvent(height1);

        var height = MathF.Round(AverageHeight * HeightSlider.Value);
        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int)height));

        var width = MathF.Round(AverageWidth * WidthSlider.Value);
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("width", (int)width));

        UpdateDimensions(SliderUpdate.Both);
    }
    private enum SliderUpdate
    {
        Height,
        Width,
        Both
    }
    private void UpdateDimensions(SliderUpdate updateType)
    {
        if (Profile == null)
            return;

        var heightValue = Math.Clamp(HeightSlider.Value, MinCharHeight, MaxCharHeight);
        var widthValue = Math.Clamp(WidthSlider.Value, MinCharWidth, MaxCharWidth);
        var sizeRatio = SizeRatio;
        var ratio = heightValue / widthValue;

        if (updateType == SliderUpdate.Height || updateType == SliderUpdate.Both)
        {
            if (ratio < 1 / sizeRatio || ratio > sizeRatio)
                widthValue = heightValue / (ratio < 1 / sizeRatio ? (1 / sizeRatio) : sizeRatio);
        }

        if (updateType == SliderUpdate.Width || updateType == SliderUpdate.Both)
        {
            if (ratio < 1 / sizeRatio || ratio > sizeRatio)
                heightValue = widthValue * (ratio < 1 / sizeRatio ? (1 / sizeRatio) : sizeRatio);
        }

        heightValue = Math.Clamp(heightValue, MinCharHeight, MaxCharHeight);
        widthValue = Math.Clamp(widthValue, MinCharWidth, MaxCharWidth);

        HeightSlider.Value = heightValue;
        WidthSlider.Value = widthValue;

        // Update profile directly to avoid infinite recursion through SetCharacterHeight/SetCharacterWidth → UpdateHeightWidthSliders → UpdateDimensions.
        Profile = Profile?.WithWidthHeight(widthValue, heightValue);
        if (!_sliderUpdatePending)
        {
            _sliderUpdatePending = true;
            UserInterfaceManager.DeferAction(() =>
            {
                _sliderUpdatePending = false;
                ReloadProfilePreview(); // Claw Command - use slim reload for smoother slider dragging
            });
        }

        var height = MathF.Round(AverageHeight * HeightSlider.Value);
        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int)height));

        var width = MathF.Round(AverageWidth * WidthSlider.Value);
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("width", (int)width));

        UpdateWeight();
    }

    private void UpdateWeight()
    {
        if (Profile == null)
            return;

        var species = _species.Find(x => x.ID == Profile.Species) ?? _species.First();
        _prototypeManager.Index(species.Prototype).TryGetComponent<FixturesComponent>(out var fixture);

        if (fixture != null)
        {
            var radius = fixture.Fixtures["fix1"].Shape.Radius;
            var density = fixture.Fixtures["fix1"].Density;
            var avg = (Profile.Width + Profile.Height) / 2;
            var weight = MathF.Round(MathF.PI * MathF.Pow(radius * avg, 2) * density);
            WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", (int)weight));
        }
        else // Whelp, the fixture doesn't exist, guesstimate it instead
            WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", 71));

        SpriteView.InvalidateMeasure();
    }

    /// <summary>
    /// Refreshes the species selector.
    /// </summary>
    public void RefreshSpecies()
    {
        SpeciesButton.Clear();
        _species.Clear();

        _species.AddRange(_prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart));
        _species.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        var speciesIds = _species.Select(o => o.ID).ToList();

        for (var i = 0; i < _species.Count; i++)
        {
            var name = Loc.GetString(_species[i].Name);
            SpeciesButton.AddItem(name, i);

            if (Profile?.Species.Equals(_species[i].ID) == true)
            {
                SpeciesButton.SelectId(i);
            }
        }

        // If our species isn't available then reset it to default.
        if (Profile != null)
        {
            if (!speciesIds.Contains(Profile.Species))
            {
                SetSpecies(HumanoidCharacterProfile.DefaultSpecies);
            }
        }
    }

    private void SetSpecies(string newSpecies)
    {
        Profile = Profile?.WithSpecies(newSpecies);
        OnSkinColorOnValueChanged(); // Species may have special color prefs, make sure to update it.
        _markingsModel.OrganData = _markingManager.GetMarkingData(newSpecies);
        _markingsModel.ValidateMarkings();
        // In case there's job restrictions for the species
        RefreshJobs();
        // In case there's species restrictions for loadouts
        RefreshLoadouts();
        UpdateSexControls(); // update sex for new species
        UpdateVoiceControls();
        UpdateSpeciesGuidebookIcon();
        ReloadPreview();
    }

    private void SetAge(int newAge)
    {
        Profile = Profile?.WithAge(newAge);
        ReloadPreview();
    }

    // CLAW COMMAND: Added.
    private void SetCustomSpeciesName(string name)
    {
        Profile = Profile?.WithCustomSpeciesName(name);
        ReloadPreview();
    }

    // Claw Command station char heights.
    //
    // These now resolve from the current profile's species rather than being fixed, so the editor
    // can never offer a value that HumanoidCharacterProfile.EnsureValid will clamp away on the
    // server. The literals below are only a fallback for when no species is resolved yet, and
    // match SpeciesPrototype's own defaults.
    private SpeciesPrototype? CurrentSpecies =>
        Profile is null ? null : _species.Find(x => x.ID == Profile.Species);

    public float MaxCharWidth => CurrentSpecies?.MaxWidth ?? 1.2f;
    public float MinCharWidth => CurrentSpecies?.MinWidth ?? 0.85f;
    public float MaxCharHeight => CurrentSpecies?.MaxHeight ?? 1.2f;
    public float MinCharHeight => CurrentSpecies?.MinHeight ?? 0.9f;
    public float SizeRatio = 1.2f;
    public float AverageHeight = 176.1f;
    public float AverageWidth = 40f;
    public float DefaultHeight = 1f;
    public float DefaultWidth = 1f;

    /// <summary>
    ///     Set the height of a humanoid mob
    /// </summary>
    /// <param name="uid">The humanoid mob's UID</param>
    /// <param name="height">The height to set the mob to</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetCharacterHeight(float height)
    {
        var clamped = Math.Clamp(height, MinCharHeight, MaxCharHeight);
        Profile = Profile?.WithHeight(clamped);

        UpdateHeightWidthSliders();
        ReloadPreview();
    }

    /// <summary>
    ///     Set the width of a humanoid mob
    /// </summary>
    /// <param name="uid">The humanoid mob's UID</param>
    /// <param name="width">The width to set the mob to</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void SetCharacterWidth(float width)
    {
        var clamped = Math.Clamp(width, MinCharWidth, MaxCharWidth);
        Profile = Profile?.WithWidth(clamped);

        UpdateHeightWidthSliders();
        ReloadPreview();
    }

    private void SetSex(Sex newSex)
    {
        Profile = Profile?.WithSex(newSex);
        // for convenience, default to most common gender when new sex is selected
        switch (newSex)
        {
            case Sex.Male:
                Profile = Profile?.WithGender(Gender.Male);
                break;
            case Sex.Female:
                Profile = Profile?.WithGender(Gender.Female);
                break;
            default:
                Profile = Profile?.WithGender(Gender.Epicene);
                break;
        }

        // this does the same as above but for voice
        if (_prototypeManager.TryIndex(Profile?.Species, out var prototype))
            SetVoice(prototype.DefaultSoundsBySex[(int)newSex]);

        UpdateGenderControls();
        UpdateVoiceControls();
        _markingsModel.SetOrganSexes(newSex);
        ReloadPreview();
    }

    private void SetVoice(ProtoId<EmoteSoundsPrototype> newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        SetDirty();
    }

    private void SetGender(Gender newGender)
    {
        Profile = Profile?.WithGender(newGender);
        ReloadPreview();
    }

    private void SetSpawnPriority(SpawnPriorityPreference newSpawnPriority)
    {
        Profile = Profile?.WithSpawnPriorityPreference(newSpawnPriority);
        SetDirty();
    }

    private void OnSpeciesInfoButtonPressed(BaseButton.ButtonEventArgs args)
    {
        // TODO GUIDEBOOK
        // make the species guide book a field on the species prototype.
        // I.e., do what jobs/antags do.

        var guidebookController = UserInterfaceManager.GetUIController<GuidebookUIController>();
        var species = Profile?.Species ?? HumanoidCharacterProfile.DefaultSpecies;
        var page = DefaultSpeciesGuidebook;
        if (_prototypeManager.HasIndex<GuideEntryPrototype>(species))
            page = new ProtoId<GuideEntryPrototype>(species.Id); // Gross. See above todo comment.

        if (_prototypeManager.Resolve(DefaultSpeciesGuidebook, out var guideRoot))
        {
            var dict = new Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>();
            dict.Add(DefaultSpeciesGuidebook, guideRoot);
            //TODO: Don't close the guidebook if its already open, just go to the correct page
            guidebookController.OpenGuidebook(dict, includeChildren: true, selected: page);
        }
    }

    private void OnSkinColorOnValueChanged()
    {
        if (Profile is null) return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;
        var strategy = _prototypeManager.Index(skin).Strategy;

        switch (strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                {
                    if (!Skin.Visible)
                    {
                        Skin.Visible = true;
                        RgbSkinColorContainer.Visible = false;
                    }

                    var color = strategy.FromUnary(Skin.Value);

                    _markingsModel.SetOrganSkinColor(color);
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));

                    break;
                }
            case SkinColorationStrategyInput.Color:
                {
                    if (!RgbSkinColorContainer.Visible)
                    {
                        Skin.Visible = false;
                        RgbSkinColorContainer.Visible = true;
                    }

                    var color = strategy.ClosestSkinColor(_rgbSkinColorSelector.Color);

                    _markingsModel.SetOrganSkinColor(color);
                    Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));

                    break;
                }
        }

        ReloadProfilePreview();
    }
}
