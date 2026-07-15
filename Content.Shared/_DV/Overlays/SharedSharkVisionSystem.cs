using Content.Goobstation.Shared.Overlays;
using Content.Shared._DV.Overlays.Components;
using Content.Shared.Actions;

namespace Content.Shared._DV.Overlays;

public sealed class SharedSharkVisionSystem : SwitchableOverlaySystem<SharkVisionComponent, ToggleSharkVisionEvent>;

public sealed partial class ToggleSharkVisionEvent : InstantActionEvent;
