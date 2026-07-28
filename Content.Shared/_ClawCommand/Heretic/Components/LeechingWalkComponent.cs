// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Heretic.Components;

[RegisterComponent]
public sealed partial class LeechingWalkComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public FixedPoint2 BoneHeal = -2.5; // Claw Command: halved rust-tile healing rate

    [DataField]
    public DamageSpecifier ToHeal = new()
    {
        // Claw Command: halved rust-tile healing rate (was -1 each).
        DamageDict =
        {
            {"Blunt", -0.5},
            {"Slash", -0.5},
            {"Piercing", -0.5},
            {"Heat", -0.5},
            {"Cold", -0.5},
            {"Shock", -0.5},
            {"Asphyxiation", -0.5},
            {"Bloodloss", -0.5},
            {"Caustic", -0.5},
            {"Poison", -0.5},
            {"Radiation", -0.5},
            {"Cellular", -0.5},
            {"Holy", -0.5},
        },
    };

    [DataField]
    public float StaminaHeal = 2.5f; // Claw Command: halved rust-tile healing rate

    [DataField]
    public float ChemPurgeRate = 3f;

    [DataField]
    public ProtoId<ReagentPrototype> ExcludedReagent = "EldritchEssence";

    [DataField]
    public FixedPoint2 BloodHeal = 2.5f; // Claw Command: halved rust-tile healing rate

    [DataField]
    public TimeSpan StunReduction = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public float TargetTemperature = 310f;
}
