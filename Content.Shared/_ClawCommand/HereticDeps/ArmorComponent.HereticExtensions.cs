// Goob/Shitmed adds coverage + trauma deduction tables to ArmorComponent. We extend it
// here so heretic armor YAML loads; the deductions are never read because the trauma
// system isn't ported.

using System.Collections.Generic;
using Content.Shared.Body.Part;

namespace Content.Shared.Armor
{
    public sealed partial class ArmorComponent
    {
        [DataField("coverage")]
        public List<BodyPartType> ArmorCoverage = new();

        [DataField]
        public Dictionary<string, float> TraumaDeductions = new();
    }
}
