using Content.Shared._ClawCommand.Shadekin;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.FixedPoint;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._ClawCommand.Shadekin;

/// <summary>
///     Handles the Shadekin energy/light HUD alert being clicked, printing the shadekin's
///     current energy, black-eye status and light exposure into their emote chat channel.
/// </summary>
public sealed partial class ShadekinAlertSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinComponent, CheckShadekinAlertEvent>(OnCheckAlert);
    }

    private void OnCheckAlert(Entity<ShadekinComponent> ent, ref CheckShadekinAlertEvent args)
    {
        if (args.Handled || !_playerManager.TryGetSessionByEntity(ent.Owner, out var session))
            return;

        var shadekin = ent.Comp;

        if (shadekin.Blackeye)
            SendMessage(Loc.GetString("shadekinenergy-alert-blackeye"), session);
        else
            SendMessage(Loc.GetString("shadekinenergy-alert-energy",
                ("energy", FixedPoint2.Min(shadekin.Energy, shadekin.MaxEnergy)),
                ("energyMax", shadekin.MaxEnergy)), session);

        SendMessage(Loc.GetString("shadekinenergy-alert-" + shadekin.LightExposure), session);

        args.Handled = true;
    }

    private void SendMessage(string msg, ICommonSession session)
    {
        _chatManager.ChatMessageToOne(ChatChannel.Emotes,
            msg,
            msg,
            EntityUid.Invalid,
            false,
            session.Channel);
    }
}
