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
        ///     Claw Command - How far away the carry verb can be used from.
        /// </summary>
        /// <remarks>
        ///     Unlike the other social interactions, carrying is not an Interaction prototype and so has no
        ///     per-verb range of its own. It used to ride on the verb system's generic accessibility check,
        ///     which is fixed at SharedInteractionSystem.InteractionRange (1.5) and shared by every
        ///     interaction in the game - so it could not be extended without extending all of them.
        ///     This gives carrying its own range, still line-of-sight checked, leaving everything else alone.
        /// </remarks>
        [DataField]
        public float CarryRange = 2.25f;
    }
}
