namespace Content.Server._ClawCommand.Carrying
{
    /// <summary>
    ///     Stores the carrier of an entity being carried.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BeingCarriedComponent : Component
    {
        public EntityUid Carrier = default!;

        /// <summary>
        ///     Claw Command - true when the carry itself added CanEscapeInventoryComponent, so dropping
        ///     knows to take it away again. Entities that already had one (critters that can wriggle out
        ///     of bags) keep theirs.
        /// </summary>
        public bool GrantedEscape;
    }
}
