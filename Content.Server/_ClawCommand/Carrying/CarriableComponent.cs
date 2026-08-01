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
    }
}
