// Reagent effects / conditions / tile reactions used by Goob's heretic chemistry YAML.
// Upstream uses `EntityEffect` + `EntityCondition`; Goob has `EntityEffectCondition` /
// `EntityEffectBaseArgs` which never made it to this fork. We stub the classes Goob's
// YAML references so prototypes load; the actual runtime hooks are no-ops.

using System.Collections.Generic;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Wizard.Chemistry
{
    /// <summary>
    /// Stub: returns true unconditionally. Heretic reagent files gate effects on
    /// "is this person a heretic/ghoul?" via this condition; upstream has no equivalent
    /// raiser, so the gate just opens for everyone.
    /// </summary>
    public sealed partial class HasComponentCondition : EntityCondition
    {
        [DataField] public HashSet<string> Components = new();
        [DataField] public LocId? GuidebookComponentName;
        [DataField] public bool Invert;
        [DataField] public bool CheckMind;

        public override bool RaiseEvent(EntityUid target, IEntityConditionRaiser raiser, EntityUid? sourceEnt) => true;

        public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
    }
}

namespace Content.Shared.EntityEffects.Effects
{
    /// <summary>
    /// Stub: ModifyBleedAmount — registered as an upstream-shaped effect so heretic YAML
    /// loads. No system fires the effect upstream.
    /// </summary>
    public sealed partial class ModifyBleedAmount : EntityEffect
    {
        [DataField] public bool Scaled = false;
        [DataField] public float Amount = -1.0f;

        public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user) { }
    }

    /// <summary>
    /// Stub: ChemCleanBloodstream — same approach as ModifyBleedAmount.
    /// </summary>
    public sealed partial class ChemCleanBloodstream : EntityEffect
    {
        [DataField] public float CleanseRate = 3.0f;

        public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user) { }
    }
}

namespace Content.Goobstation.shared.Chemistry
{
    /// <summary>
    /// Stub: TakeStaminaDamage reagent effect. Used by heretic Eldritch Essence; behavior is
    /// a no-op upstream (no `immediate` overload on SharedStaminaSystem).
    /// </summary>
    public sealed partial class TakeStaminaDamage : EntityEffect
    {
        [DataField] public int Amount = 10;
        [DataField] public bool Immediate;

        public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user) { }
    }
}

namespace Content.Goobstation.Shared.Chemistry
{
    /// <summary>
    /// Stub: ChangeTileReaction. Used by heretic Rust Decoction to corrupt tiles. We register
    /// the tile reaction so YAML loads; no system actually invokes ITileReaction without the
    /// Goob plumbing, so corrupting tiles becomes a no-op.
    /// </summary>
    [DataDefinition]
    public sealed partial class ChangeTileReaction : ITileReaction
    {
        [DataField] public FixedPoint2 ChangeTileCost { get; private set; } = 4.5f;
        [DataField] public string NewTileId = "PlatingRust";
        [DataField] public string? OldTileId;
        [DataField] public EntProtoId? Effect;

        public FixedPoint2 TileReact(TileRef tile,
            ReagentPrototype reagent,
            FixedPoint2 reactVolume,
            IEntityManager entityManager,
            List<ReagentData>? data = null)
        {
            return FixedPoint2.Zero;
        }
    }
}
