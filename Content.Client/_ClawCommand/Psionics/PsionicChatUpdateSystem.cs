using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Abilities.Psionics;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Chat
{
    /// <summary>
    ///     Refreshes the chat channel list when the local player gains or loses telepathy, so the Telepathic
    ///     channel appears in the selector the moment a power is granted rather than on the next reconnect.
    /// </summary>
    /// <remarks>
    ///     Claw Command - upstream called IChatManager.UpdatePermissions(), which no longer exists. The channel
    ///     permission set now lives on ChatUIController, so this asks that controller to recompute instead.
    ///     It also keys off TelepathyComponent rather than PsionicComponent: being psionic does not by itself
    ///     grant telepathy, and ChatUIController gates the channel on the same component.
    /// </remarks>
    public sealed partial class PsionicChatUpdateSystem : EntitySystem
    {
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IUserInterfaceManager _ui = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TelepathyComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TelepathyComponent, ComponentRemove>(OnRemove);
        }

        public PsionicComponent? Player => CompOrNull<PsionicComponent>(_playerManager.LocalEntity);
        public bool IsPsionic => Player != null;

        private void OnInit(EntityUid uid, TelepathyComponent component, ComponentInit args)
        {
            RefreshIfLocal(uid);
        }

        private void OnRemove(EntityUid uid, TelepathyComponent component, ComponentRemove args)
        {
            RefreshIfLocal(uid);
        }

        /// <summary>
        ///     Other entities gaining or losing telepathy does not change what this client may send, so the
        ///     recompute is skipped unless it is the local player's own entity.
        /// </summary>
        private void RefreshIfLocal(EntityUid uid)
        {
            if (_playerManager.LocalEntity != uid)
                return;

            _ui.GetUIController<ChatUIController>().UpdateChannelPermissions();
        }
    }
}
