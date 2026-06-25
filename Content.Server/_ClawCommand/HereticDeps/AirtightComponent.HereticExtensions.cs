// Goob's heretic tile (Mansus Doorway etc.) sets `blockExplosions: false` on AirtightComponent.
// Upstream doesn't expose that field. Add it here so the YAML loads; behavior unchanged.

namespace Content.Server.Atmos.Components
{
    public sealed partial class AirtightComponent
    {
        [DataField]
        public bool BlockExplosions = true;
    }
}
