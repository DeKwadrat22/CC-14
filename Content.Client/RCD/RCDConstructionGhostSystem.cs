using Content.Client.Hands.Systems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client.RCD;

/// <summary>
/// System for handling structure ghost placement in places where RCD can create objects.
/// </summary>
public sealed partial class RCDConstructionGhostSystem : EntitySystem
{
    private const string PlacementMode = nameof(AlignRCDConstruction);

    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPlacementManager _placementManager = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    private Direction _placementDirection = default;
    private bool _useMirrorPrototype = false;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorFlipObject, InputCmdHandler.FromDelegate(_ => HandleFlip()))
            .Register<RCDConstructionGhostSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<RCDConstructionGhostSystem>();
        base.Shutdown();
    }

    private void HandleFlip()
    {
        if (_placementManager.CurrentPermission?.MobUid is not { } rcdUid)
            return;

        if (!TryComp<RCDComponent>(rcdUid, out var rcd))
            return;

        var prototype = _protoManager.Index(rcd.ProtoId);
        if (string.IsNullOrEmpty(prototype.MirrorPrototype))
            return;

        _useMirrorPrototype = !_useMirrorPrototype;
        var useProto = _useMirrorPrototype ? prototype.MirrorPrototype : prototype.Prototype;
        CreatePlacer(rcdUid, prototype, useProto);
        RaiseNetworkEvent(new RCDConstructionGhostFlipEvent(GetNetEntity(rcdUid), _useMirrorPrototype));
    }

    private void CreatePlacer(EntityUid uid, RCDPrototype prototype, string? proto)
    {
        var newObjInfo = new PlacementInformation
        {
            MobUid = uid,
            PlacementOption = PlacementMode,
            EntityType = proto,
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = (prototype.Mode == RcdMode.ConstructTile),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRCD = HasComp<RCDComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCD (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRCD))
            return;

        // Determine if player is carrying an RCD in their active hand
        if (_playerManager.LocalSession?.AttachedEntity is not { } player)
            return;

        var heldEntity = _hands.GetActiveItem(player);

        // Don't open the placement overlay for client-side RCDs.
        // This may happen when predictively spawning one in your hands.
        if (heldEntity != null && IsClientSide(heldEntity.Value))
            return;

        if (!TryComp<RCDComponent>(heldEntity, out var rcd))
        {
            // If the player was holding an RCD, but is no longer, cancel placement
            if (placerIsRCD)
                _placementManager.Clear();

            return;
        }
        var prototype = ProtoMan.Index(rcd.ProtoId);

        // Update the direction the RCD prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RCDConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        var useProto = (_useMirrorPrototype && !string.IsNullOrEmpty(prototype.MirrorPrototype))
            ? prototype.MirrorPrototype
            : prototype.Prototype;

        // If the placer has not changed, exit
        if (heldEntity == placerEntity && useProto == placerProto)
            return;

        CreatePlacer(heldEntity.Value, prototype, useProto);
    }
}
