// SPDX-FileCopyrightText: 2024 Goobstation Contributors
// SPDX-FileCopyrightText: 2025 ClawCommand Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goobstation for ClawCommand.

namespace Content.Shared._GoobStation.Trigger.Components;

/// <summary>
/// When triggered, deletes the host entity that contains this implant
/// rather than the implant entity itself.
/// Used by the Bluespace Lifeline implant to remove the host body on activation.
/// </summary>
[RegisterComponent]
public sealed partial class DeleteParentOnTriggerComponent : Component;
