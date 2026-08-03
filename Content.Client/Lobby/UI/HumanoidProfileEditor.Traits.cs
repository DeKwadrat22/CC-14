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

        // Claw Command - sorted by point value rather than name: the traits that hand you points
        // (negative Cost) first, then free ones, then the ones you spend those points on. Ties fall
        // back to name so the order is stable within a price bracket.
        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>()
            .OrderBy(t => t.Cost)
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
        // Claw Command - the counter reads [spent / available], not [net cost / cap]. A trait with a
        // negative Cost hands you points, so it raises the right-hand number instead of lowering the
        // left one - picking up Blindness should read as "you now have 10 more points to play with",
        // not as "you have spent -10 points". Traits with a positive Cost are what fill the left side.
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
                selectors.Add(selector);
            }

            // Selection counter
            if (poolLimit is >= 0)
            {
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString("humanoid-profile-editor-trait-count-hint", ("current", spent), ("max", poolLimit.Value)),
                    FontColorOverride = Color.Gray
                });
            }

            foreach (var selector in selectors)
            {
                if (selector == null)
                    continue;

                // A negative-cost trait only ever raises the ceiling, so it can never be unaffordable.
                if (poolLimit is >= 0 && selector.Cost > 0 && spent + selector.Cost > poolLimit.Value)
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }

                TraitsList.AddChild(selector);
            }
        }
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
