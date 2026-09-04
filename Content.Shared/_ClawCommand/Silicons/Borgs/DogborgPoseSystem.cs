using Content.Shared.Chat;
using Content.Shared.Movement.Events;

namespace Content.Shared._ClawCommand.Silicons.Borgs;

/// <summary>
/// Drives the dogborg pose state (sit / rest / belly-up). Listens to the
/// matching emote and action events, clears the pose the moment the borg
/// starts moving, and lets the client visualiser swap the Body sprite layer
/// to the corresponding Citadel-ported pose state.
/// </summary>
public sealed class DogborgPoseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DogborgPoseComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<DogborgPoseComponent, SpriteMoveEvent>(OnSpriteMove);

        SubscribeLocalEvent<DogborgPoseComponent, DogborgSitActionEvent>(OnSitAction);
        SubscribeLocalEvent<DogborgPoseComponent, DogborgRestActionEvent>(OnRestAction);
        SubscribeLocalEvent<DogborgPoseComponent, DogborgBellyUpActionEvent>(OnBellyUpAction);
    }

    private void OnEmote(Entity<DogborgPoseComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        var pose = args.Emote.ID switch
        {
            "DogborgSit" => DogborgPose.Sit,
            "DogborgRest" => DogborgPose.Rest,
            "DogborgBellyUp" => DogborgPose.BellyUp,
            _ => DogborgPose.None,
        };
        if (pose == DogborgPose.None)
            return;

        TogglePose(ent, pose);
        args.Handled = true;
    }

    private void OnSpriteMove(Entity<DogborgPoseComponent> ent, ref SpriteMoveEvent args)
    {
        // Moving breaks the pose — walking while sitting would look ridiculous.
        if (args.IsMoving && ent.Comp.Pose != DogborgPose.None)
            SetPose(ent, DogborgPose.None);
    }

    private void OnSitAction(Entity<DogborgPoseComponent> ent, ref DogborgSitActionEvent args)
    {
        if (args.Handled)
            return;
        TogglePose(ent, DogborgPose.Sit);
        args.Handled = true;
    }

    private void OnRestAction(Entity<DogborgPoseComponent> ent, ref DogborgRestActionEvent args)
    {
        if (args.Handled)
            return;
        TogglePose(ent, DogborgPose.Rest);
        args.Handled = true;
    }

    private void OnBellyUpAction(Entity<DogborgPoseComponent> ent, ref DogborgBellyUpActionEvent args)
    {
        if (args.Handled)
            return;
        TogglePose(ent, DogborgPose.BellyUp);
        args.Handled = true;
    }

    /// <summary>
    /// Toggle into the requested pose. If already in that pose, clear it
    /// instead — so the same button or emote stands the dogborg back up.
    /// </summary>
    private void TogglePose(Entity<DogborgPoseComponent> ent, DogborgPose pose)
    {
        SetPose(ent, ent.Comp.Pose == pose ? DogborgPose.None : pose);
    }

    public void SetPose(Entity<DogborgPoseComponent> ent, DogborgPose pose)
    {
        if (ent.Comp.Pose == pose)
            return;
        ent.Comp.Pose = pose;
        Dirty(ent);
    }
}
