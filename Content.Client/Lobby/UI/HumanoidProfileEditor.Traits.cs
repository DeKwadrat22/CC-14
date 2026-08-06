using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    /// <summary>
    /// Refreshes traits selector
    /// </summary>
    public void RefreshTraits()
    {
        TraitsList.RemoveAllChildren();

        // Claw Command - gateway traits first, then by point value rather than name: the traits that hand
        // you points (negative Cost) first, then free ones, then the ones you spend those points on. Ties
        // fall back to name so the order is stable within a price bracket.
        //
        // The gateway pass exists because sorting on cost alone buried Latent Psychic (4 points) underneath
        // every psionic power it unlocks, several of which cost less or refund points. Reading top to bottom
        // you met a wall of locked traits before ever reaching the one that unlocks them.
        var gateways = HumanoidCharacterProfile.GetGatewayTraits(_prototypeManager);
        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>()
            .OrderBy(t => gateways.Contains(t.ID) ? 0 : 1)
            .ThenBy(t => t.Cost)
            .ThenBy(t => Loc.GetString(t.Name))
            .ToList();
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (traits.Count < 1)
        {
            TraitsList.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-no-traits"),
                FontColorOverride = Color.Gray,
            });
            return;
        }

        // Setup model
        Dictionary<string, List<string>> traitGroups = new();
        List<string> defaultTraits = new();
        traitGroups.Add(TraitCategoryPrototype.Default, defaultTraits);

        foreach (var trait in traits)
        {
            if (trait.Category == null)
            {
                defaultTraits.Add(trait.ID);
                continue;
            }

            if (!_prototypeManager.HasIndex(trait.Category))
                continue;

            var group = traitGroups.GetOrNew(trait.Category);
            group.Add(trait.ID);
        }

        // Claw Command - budget is tracked per BudgetPool, not per category. Physical, Psychological,
        // Disabilities and Quirks all share SharedTraitBudget, so points spent in one of them have to
        // be visible from the others. This mirrors what HumanoidCharacterProfile actually enforces;
        // before this the UI counted each category on its own and the three categories without their
        // own MaxTraitPoints showed no budget at all.
        // Claw Command - the counter reads [remaining / total]. A trait with a negative Cost hands you
        // points, so it raises BOTH numbers: the cap goes up and those points are immediately yours to
        // spend. A trait with a positive Cost only pulls the left number down. Taking Blindness on a
        // cap of 10 should read [20/20], not [-10/10].
        //
        // This is display only: spent <= cap + granted is the same inequality as sum(cost) <= cap,
        // which is what HumanoidCharacterProfile enforces. Nothing about validity changes here.
        var poolSpent = new Dictionary<string, int>();
        var poolGranted = new Dictionary<string, int>();
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            var poolKey = GetTraitPoolKey(categoryId);

            foreach (var traitId in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitId);
                if (Profile?.TraitPreferences.Contains(trait.ID) != true)
                    continue;

                if (trait.Cost >= 0)
                    poolSpent[poolKey] = poolSpent.GetValueOrDefault(poolKey) + trait.Cost;
                else
                    poolGranted[poolKey] = poolGranted.GetValueOrDefault(poolKey) - trait.Cost;
            }
        }

        // Create UI view from model
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            TraitCategoryPrototype? category = null;

            if (categoryId != TraitCategoryPrototype.Default)
            {
                category = _prototypeManager.Index<TraitCategoryPrototype>(categoryId);
                // Label
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString(category.Name),
                    Margin = new Thickness(0, 10, 0, 0),
                    StyleClasses = { StyleClass.LabelHeading },
                });
            }

            // Claw Command - spend and limit both come from the shared pool this category belongs to.
            var poolKey = GetTraitPoolKey(categoryId);
            var spent = poolSpent.GetValueOrDefault(poolKey);
            var granted = poolGranted.GetValueOrDefault(poolKey);
            var baseLimit = HumanoidCharacterProfile.GetBudgetPoolLimit(poolKey, _prototypeManager)
                            ?? category?.MaxTraitPoints;

            // Points handed to you by negative-cost traits raise the ceiling rather than lowering the spend.
            var poolLimit = baseLimit + granted;

            List<TraitPreferenceSelector?> selectors = new();

            foreach (var traitProto in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitProto);
                var selector = new TraitPreferenceSelector(trait);

                selector.Preference = Profile?.TraitPreferences.Contains(trait.ID) == true;

                selector.PreferenceChanged += preference =>
                {
                    if (preference)
                    {
                        Profile = Profile?.WithTraitPreference(trait.ID, _prototypeManager);
                    }
                    else
                    {
                        Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                    }

                    SetDirty();
                    RefreshTraits(); // If too many traits are selected, they will be reset to the real value.
                };

                // Claw Command - lock it if the character does not meet its prerequisites. RefreshTraits runs
                // again after every toggle above, so ticking a gateway trait re-evaluates this immediately.
                ApplyTraitAvailability(selector, trait);

                selectors.Add(selector);
            }

            // Selection counter
            if (poolLimit is >= 0)
            {
                TraitsList.AddChild(new Label
                {
                    // The string is "Points available: [{current}/{max}]", so current is what is LEFT,
                    // not what has been used. Granting traits push both numbers up together; spending
                    // traits only pull the left one down.
                    Text = Loc.GetString("humanoid-profile-editor-trait-count-hint", ("current", poolLimit.Value - spent), ("max", poolLimit.Value)),
                    FontColorOverride = Color.Gray
                });
            }

            foreach (var selector in selectors)
            {
                if (selector == null)
                    continue;

                // A negative-cost trait only ever raises the ceiling, so it can never be unaffordable.
                // Claw Command - a trait that is locked outright stays grey; painting it red would blame the
                // point budget for something the player cannot buy at any price.
                if (poolLimit is >= 0 && selector.Cost > 0 && spent + selector.Cost > poolLimit.Value
                    && !selector.Checkbox.Disabled)
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }

                TraitsList.AddChild(selector);
            }
        }
    }

    /// <summary>
    ///     Claw Command - greys out traits whose prerequisites the character does not meet, so a gated trait
    ///     reads as locked rather than as a checkbox that refuses to stay ticked.
    /// </summary>
    /// <remarks>
    ///     Already-selected traits are always left interactive, otherwise a pick that later became invalid
    ///     (say the player enabled a job that forbids it) would be impossible to remove.
    /// </remarks>
    private void ApplyTraitAvailability(TraitPreferenceSelector selector, TraitPrototype trait)
    {
        if (Profile == null || selector.Preference)
            return;

        if (Profile.TraitPrerequisitesMet(trait, _prototypeManager, Profile.TraitPreferences))
            return;

        selector.SetUnavailable(GetTraitLockReason(trait));
    }

    /// <summary>
    ///     Claw Command - names what would unlock a trait, when the thing blocking it is the
    ///     "one of these traits or one of these jobs" gate. Anything else - species, a forbidden job, a
    ///     mutually exclusive pick - falls back to a generic line, because those are already visible to the
    ///     player elsewhere in the editor.
    /// </summary>
    private string GetTraitLockReason(TraitPrototype trait)
    {
        var conflict = Loc.GetString("humanoid-profile-editor-trait-locked-conflict");

        if (Profile == null)
            return conflict;

        // A mutually exclusive pick, or a job that bars the trait outright, is not something the
        // requires-one-of line can describe - listing prerequisites there would send the player looking for
        // a trait to buy when the actual fix is to drop something they already have.
        foreach (var excluded in trait.Excludes)
        {
            if (Profile.TraitPreferences.Contains(excluded))
                return conflict;
        }

        foreach (var job in trait.ForbiddenJobs)
        {
            if (Profile.JobPriorities.TryGetValue(job, out var pri) && pri > JobPriority.Never)
                return conflict;
        }

        var options = new List<string>();

        foreach (var required in trait.RequiresAnyTrait)
        {
            if (_prototypeManager.TryIndex(required, out var requiredProto))
                options.Add(Loc.GetString(requiredProto.Name));
        }

        foreach (var job in trait.RequiresAnyJob)
        {
            if (_prototypeManager.TryIndex(job, out var jobProto))
                options.Add(jobProto.LocalizedName);
        }

        return options.Count == 0
            ? conflict
            : Loc.GetString("humanoid-profile-editor-trait-locked-requires",
                ("options", string.Join(", ", options)));
    }

    /// <summary>
    /// Claw Command - the key a category's trait points are counted against. Categories that declare a
    /// BudgetPool all count against that one key; anything else is its own budget, keyed by category ID.
    /// </summary>
    private string GetTraitPoolKey(string categoryId)
    {
        if (categoryId == TraitCategoryPrototype.Default)
            return categoryId;

        return _prototypeManager.TryIndex<TraitCategoryPrototype>(categoryId, out var category)
            ? category.BudgetPool ?? categoryId
            : categoryId;
    }
}
