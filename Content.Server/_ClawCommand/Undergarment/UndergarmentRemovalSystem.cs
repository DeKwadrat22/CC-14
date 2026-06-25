using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._ClawCommand.Undergarment;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._ClawCommand.Undergarment;

/// <summary>
///     System handling undergarment removing.
/// </summary>
public sealed partial class UndergarmentRemovalSystem : EntitySystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private MarkingManager _markingManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UndergarmentRemovalComponent, GetVerbsEvent<ExamineVerb>>(OnExaminedEvent);
        SubscribeLocalEvent<UndergarmentRemovalComponent, UndergarmentDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<UndergarmentRemovalComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, UndergarmentRemovalComponent component, ComponentShutdown args)
    {
        if (component.Removed)
            ToggleUndergarment(uid, component);

        component.SavedMarkings.Clear();
    }

    private void OnDoAfter(EntityUid uid, UndergarmentRemovalComponent comp, UndergarmentDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ToggleUndergarment(uid, comp);
        args.Handled = true;
    }

    /// <summary>
    ///     Examine verb method.
    /// </summary>
    /// <param name="uid">UndergarmentRemoval UID</param>
    /// <param name="comp">UndergarmentRemoval Component</param>
    /// <param name="args">Verb Events for Examine</param>
    private void OnExaminedEvent(EntityUid uid, UndergarmentRemovalComponent comp, GetVerbsEvent<ExamineVerb> args)
    {
        // We do some checks first, like if we can access and whether we can interact.
        // We also check the EXAMINE method for whether we're in range.
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_examine.IsInDetailsRange(args.User, args.Target))
            return;

        // Now we make the verb and add it to the examine list.
        var verb = new ExamineVerb
        {
            Message = Loc.GetString("cc-undergarment-system-verb-tooltip"),
            Text = Loc.GetString("cc-undergarment-system-verb-name"),
            Category = VerbCategory.Adjust,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 2f, new UndergarmentDoAfterEvent(), args.Target, args.Target)
                {
                    BreakOnMove = false,
                    BreakOnDamage = true,
                    NeedHand = true,
                });
            }
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Removal of undergarments when activated.
    /// </summary>
    /// <param name="target">Target UID</param>
    /// <param name="comp">UndergarmentRemoval Component</param>
    private void ToggleUndergarment(EntityUid target, UndergarmentRemovalComponent comp)
    {
        if (!_visualBody.TryGatherMarkingsData(target, null, out var profiles, out var markingData, out var applied))
            return;

        if (comp.Removed)
        {
            // Restore saved markings back into the dict
            _popup.PopupEntity(Loc.GetString("cc-undergarments-apply"), target, target, PopupType.Large);

            foreach (var (organ, layers) in comp.SavedMarkings)
            {
                if (!applied.ContainsKey(organ))
                    applied[organ] = new();

                foreach (var (layer, markings) in layers)
                {
                    if (!applied[organ].ContainsKey(layer))
                        applied[organ][layer] = new();

                    applied[organ][layer].AddRange(markings);
                }
            }

            comp.SavedMarkings.Clear();
            comp.Removed = false;
        }
        else
        {
            // Find and strip undergarment markings, saving them
            _popup.PopupEntity(Loc.GetString("cc-undergarments-remove"), target, target, PopupType.Large);
            foreach (var (organ, layers) in applied)
            {
                foreach (var (layer, markings) in layers)
                {
                    for (var i = markings.Count - 1; i >= 0; i--)
                    {
                        if (!_markingManager.TryGetMarking(markings[i], out var proto))
                            continue;

                        if (proto.GroupWhitelist == null)
                            continue;

                        if (proto.BodyPart != HumanoidVisualLayers.UndergarmentTop &&
                            proto.BodyPart != HumanoidVisualLayers.UndergarmentBottom)
                            continue;

                        // Save it
                        if (!comp.SavedMarkings.ContainsKey(organ))
                            comp.SavedMarkings[organ] = new();
                        if (!comp.SavedMarkings[organ].ContainsKey(layer))
                            comp.SavedMarkings[organ][layer] = new();

                        comp.SavedMarkings[organ][layer].Add(markings[i]);
                        markings.RemoveAt(i);
                    }
                }
            }

            comp.Removed = true;
        }

        _visualBody.ApplyMarkings(target, applied);
    }
}
