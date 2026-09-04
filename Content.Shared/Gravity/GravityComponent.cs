using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    [RegisterComponent]
    [AutoGenerateComponentState]
    [NetworkedComponent]
    public sealed partial class GravityComponent : Component
    {
        [DataField, AutoNetworkedField]
        public SoundSpecifier GravityShakeSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/alert.ogg");

        [DataField, AutoNetworkedField]
        public bool Enabled;

        /// <summary>
        /// Inherent gravity ensures GravitySystem won't change Enabled according to the gravity generators attached to this entity.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Inherent;

        /// <summary>
        /// Gravitational acceleration in m/s^2. Used by Space Wind to compute the friction force a tile applies
        /// against entities being pushed by airflow (friction = gravity * tileMobFrictionNoInput * mass).
        /// </summary>
        [DataField]
        public double Acceleration = 9.80665;
    }
}
