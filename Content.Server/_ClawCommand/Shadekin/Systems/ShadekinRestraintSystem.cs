using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Shared._ClawCommand.Shadekin;
using Content.Shared._ClawCommand.Shadekin.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;

namespace Content.Server._ClawCommand.Shadekin.Systems;

/// <summary>
///     CLAW COMMAND - shadekin restraints. Applied in-hand onto a shadekin the same way you apply cuffs:
///     target them with the item and wait out a do-after. On success the shadekin is permanently severed
///     from the Dark (<see cref="ShadekinSystem.RestrainShadekin"/>) and the restraints are then worn on
///     them. The sever is permanent, but the restraints themselves can be stripped back off by the kin or
///     anyone else - taking them off does not restore the powers. Deconverting an actual (awakened) anomaly
///     also plays a brief fire/ash sound and the eerie "the Dark is torn away" flavour.
/// </summary>
public sealed partial class ShadekinRestraintSystem : EntitySystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ShadekinSystem _shadekin = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    /// <summary>First-person lines dropped into the bound shadekin's chat log, in order.</summary>
    private static readonly string[] EerieLines =
    {
        "shadekin-restraint-applied-self-1",
        "shadekin-restraint-applied-self-2",
        "shadekin-restraint-applied-self-3",
    };

    /// <summary>Muted darker-gray the eerie lines print in, so they read cold and distant.</summary>
    private static readonly Color EerieColor = Color.FromHex("#696969");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadekinRestraintComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ShadekinRestraintComponent, ShadekinRestraintDoAfterEvent>(OnRestraintDoAfter);
    }

    private void OnAfterInteract(Entity<ShadekinRestraintComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (!args.CanReach)
        {
            _popup.PopupEntity(Loc.GetString("shadekin-restraint-too-far"), args.User, args.User);
            return;
        }

        // The restraints only mean anything against a shadekin.
        if (!HasComp<ShadekinComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadekin-restraint-not-shadekin"), target, args.User);
            return;
        }

        if (HasComp<ShadekinCuffComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadekin-restraint-already", ("target", target)), target, args.User);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.ApplyTime,
            new ShadekinRestraintDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            DistanceThreshold = 1.5f,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _popup.PopupEntity(Loc.GetString("shadekin-restraint-begin", ("target", target)), target, args.User);
        args.Handled = true;
    }

    private void OnRestraintDoAfter(Entity<ShadekinRestraintComponent> ent, ref ShadekinRestraintDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        // They could have died/despawned or been bound by someone else while the do-after ran.
        if (!TryComp<ShadekinComponent>(target, out var shadekin) || HasComp<ShadekinCuffComponent>(target))
            return;

        args.Handled = true;

        // Was this an actual deconversion (an awakened anomaly) or just binding an already-ordinary kin?
        var wasAnomaly = !shadekin.Blackeye;
        var deconvertSound = ent.Comp.DeconvertSound;

        // The permanent "sever from the Dark" transition: blackeye state, keep darkvision, cuff marker.
        _shadekin.RestrainShadekin(target, shadekin);

        // Put the restraints on so they're visibly worn. They can be stripped back off later by the kin
        // or anyone else - the sever has already happened and does not come back off with them.
        if (!_inventory.TryEquip(args.User, target, ent.Owner, "outerClothing", silent: true, force: true))
            _popup.PopupEntity(Loc.GetString("shadekin-restraint-equip-fail", ("target", target)), target, args.User);

        // Burning an awakened anomaly's bond to the Dark away is the dramatic part: fire/ash + eerie lines.
        if (wasAnomaly)
        {
            _audio.PlayPvs(deconvertSound, target);
            SendEerieMessages(target);
        }
    }

    /// <summary>
    ///     The eerie "the Dark is being torn away, and it hurts" flavour the restraints produce on the kin.
    /// </summary>
    private void SendEerieMessages(EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("shadekin-restraint-applied-others", ("target", target)), target, PopupType.MediumCaution);

        if (_playerManager.TryGetSessionByEntity(target, out var session))
        {
            foreach (var line in EerieLines)
            {
                var msg = Loc.GetString(line);
                _chatManager.ChatMessageToOne(ChatChannel.Emotes, msg, msg, EntityUid.Invalid, false, session.Channel, EerieColor);
            }
        }

        _popup.PopupEntity(Loc.GetString("shadekin-restraint-applied-self-pain"), target, target, PopupType.LargeCaution);
    }
}
