using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._ClawCommand.Kitsune;
using Content.Shared.Actions;
using Content.Shared.Body;

namespace Content.Server._ClawCommand.Kitsune;

public sealed partial class KitsuneFoxFormSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitsuneFoxFormComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<KitsuneFoxFormComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<KitsuneFoxFormComponent, KitsuneFoxFormActionEvent>(OnAction);
    }

    private void OnStartup(EntityUid uid, KitsuneFoxFormComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
        EnsureComp<PolymorphableComponent>(uid);
    }

    private void OnShutdown(EntityUid uid, KitsuneFoxFormComponent comp, ComponentShutdown args)
    {
        if (TryComp<PolymorphedEntityComponent>(uid, out _))
            _polymorph.Revert(uid);

        _actions.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnAction(EntityUid uid, KitsuneFoxFormComponent comp, KitsuneFoxFormActionEvent args)
    {
        ToggleFoxForm(uid, comp);
        args.Handled = true;
    }

    private void ToggleFoxForm(EntityUid uid, KitsuneFoxFormComponent comp)
    {
        Log.Info("Toggling Fox Form");

        if (_visualBody.TryGatherMarkingsData(uid, null, out _, out _, out var applied))
        {
            foreach (var layers in applied.Values)
            {
                foreach (var (layer, markings) in layers)
                {
                    foreach (var marking in markings)
                    {
                        if (marking.MarkingId != "KitsuneFox")
                            continue;

                        if (marking.MarkingColors.Count > 0)
                            comp.FoxBodyColor = marking.MarkingColors[0];
                        if (marking.MarkingColors.Count > 1)
                            comp.FoxInnerEarColor = marking.MarkingColors[1];
                    }
                }
            }
        }

        var foxUid = _polymorph.PolymorphEntity(uid, comp.FoxPolymorphId);
        if (foxUid != null)
        {
            _appearance.SetData(foxUid.Value, KitsuneColorVisuals.Body, comp.FoxBodyColor);
            _appearance.SetData(foxUid.Value, KitsuneColorVisuals.Overlay, comp.FoxInnerEarColor);
        }
    }
}
