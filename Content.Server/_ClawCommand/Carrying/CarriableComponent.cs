using System.Threading;

namespace Content.Server._ClawCommand.Carrying
{
    /// <summary>
    ///     Marks an entity as something that can be picked up and carried by another entity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CarriableComponent : Component
    {
        /// <summary>
        ///     Number of free hands required to carry the entity.
        /// </summary>
        [DataField]
        public int FreeHandsRequired = 2;

        public CancellationTokenSource? CancelToken;

        /// <summary>
        ///     The base duration (in seconds) of how long it should take to pick up this entity,
        ///     before mass and stamina scaling are considered.
        /// </summary>
        [DataField]
        public float PickupDuration = 3;

        /// <summary>
        ///     Claw Command - How far away the carry verb can be used from, by a carrier of the tallest
        ///     height their species allows.
        /// </summary>
        /// <remarks>
        ///     Unlike the other social interactions, carrying is not an Interaction prototype and so has no
        ///     per-verb range of its own. It used to ride on the verb system's generic accessibility check,
        ///     which is fixed at SharedInteractionSystem.InteractionRange (1.5) and shared by every
        ///     interaction in the game - so it could not be extended without extending all of them.
        ///     This gives carrying its own range, still line-of-sight and container checked.
        /// </remarks>
        [DataField]
        public float CarryRange = 2.25f;

        /// <summary>
        ///     Claw Command - Carry range for the shortest carrier, and for anything without a humanoid
        ///     profile. Matches the old global interaction range, so short characters reach exactly as
        ///     far as everyone did before reach was tied to height.
        /// </summary>
        [DataField]
        public float MinHeightCarryRange = 1.5f;
    }
}
