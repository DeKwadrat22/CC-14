using Content.Shared.Abilities.Psionics;
using Content.Server.Abilities.Psionics;
using Content.Shared.Eye;
using Content.Server.NPC.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;


namespace Content.Server.Psionics
{
    public sealed partial class PsionicInvisibilitySystem : EntitySystem
    {
        // Claw Command - RA0033: the faction parameters forbid literals, so the ids live in fields.
        private static readonly ProtoId<NpcFactionPrototype> PsionicInterloperFaction = "PsionicInterloper";
        private static readonly ProtoId<NpcFactionPrototype> GlimmerMonsterFaction = "GlimmerMonster";

        [Dependency] private VisibilitySystem _visibilitySystem = default!;
        [Dependency] private PsionicInvisibilityPowerSystem _invisSystem = default!;
        [Dependency] private NpcFactionSystem _npcFactonSystem = default!;
        [Dependency] private SharedEyeSystem _eye = default!;
        public override void Initialize()
        {
            base.Initialize();
            /// Masking
            SubscribeLocalEvent<ActorComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicInsulationComponent, ComponentInit>(OnInsulInit);
            SubscribeLocalEvent<PsionicInsulationComponent, ComponentShutdown>(OnInsulShutdown);

            /// Layer
            SubscribeLocalEvent<PsionicallyInvisibleComponent, ComponentStartup>(OnInvisInit);
            SubscribeLocalEvent<PsionicallyInvisibleComponent, ComponentShutdown>(OnInvisShutdown);

            // PVS Stuff
            SubscribeLocalEvent<PsionicallyInvisibleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<PsionicallyInvisibleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnInit(EntityUid uid, ActorComponent component, ComponentInit args)
        {
            if (!HasComp<PsionicInsulationComponent>(uid))
                SetCanSeePsionicInvisiblity(uid, false);
        }

        private void OnInsulInit(EntityUid uid, PsionicInsulationComponent component, ComponentInit args)
        {
            if (HasComp<PsionicInvisibilityUsedComponent>(uid))
                _invisSystem.ToggleInvisibility(uid);

            if (_npcFactonSystem.IsMember(uid, PsionicInterloperFaction))
            {
                component.SuppressedFactions.Add("PsionicInterloper");
                _npcFactonSystem.RemoveFaction(uid, PsionicInterloperFaction);
            }

            if (_npcFactonSystem.IsMember(uid, GlimmerMonsterFaction))
            {
                component.SuppressedFactions.Add("GlimmerMonster");
                _npcFactonSystem.RemoveFaction(uid, GlimmerMonsterFaction);
            }

            SetCanSeePsionicInvisiblity(uid, true);
        }

        private void OnInsulShutdown(EntityUid uid, PsionicInsulationComponent component, ComponentShutdown args)
        {
            SetCanSeePsionicInvisiblity(uid, false);

            if (!HasComp<PsionicComponent>(uid))
            {
                component.SuppressedFactions.Clear();
                return;
            }

            foreach (var faction in component.SuppressedFactions)
            {
                _npcFactonSystem.AddFaction(uid, faction);
            }
            component.SuppressedFactions.Clear();
        }

        private void OnInvisInit(EntityUid uid, PsionicallyInvisibleComponent component, ComponentStartup args)
        {
            var visibility = EnsureComp<VisibilityComponent>(uid);

            _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.PsionicInvisibility, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(uid, visibility);
        }


        private void OnInvisShutdown(EntityUid uid, PsionicallyInvisibleComponent component, ComponentShutdown args)
        {
            if (TryComp<VisibilityComponent>(uid, out var visibility))
            {
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.PsionicInvisibility, false);
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibility);
            }
        }

        private void OnEntInserted(EntityUid uid, PsionicallyInvisibleComponent component, EntInsertedIntoContainerMessage args)
        {
            DirtyEntity(args.Entity);
        }

        private void OnEntRemoved(EntityUid uid, PsionicallyInvisibleComponent component, EntRemovedFromContainerMessage args)
        {
            DirtyEntity(args.Entity);
        }

        public void SetCanSeePsionicInvisiblity(EntityUid uid, bool set)
        {
            if (set == true)
            {
                if (TryComp(uid, out EyeComponent? eye))
                {
                    _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) VisibilityFlags.PsionicInvisibility, eye);
                }
            }
            else
            {
                if (TryComp(uid, out EyeComponent? eye))
                {
                    _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(int) VisibilityFlags.PsionicInvisibility, eye);
                }
            }
        }
    }
}
