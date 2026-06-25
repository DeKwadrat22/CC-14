"""Composite the `_e` and `_l` overlays of each dogborg RSI on top of the
body sprite, so each overlay PNG carries the full body + overlay.

Why: SS14 sprite layers animate independently. With separate Body (animated)
and Light overlay (animated), the two timelines can drift apart over time and
the eye glow visibly desyncs from the walking animation. By baking the body
underneath each overlay, we sidestep the sync problem entirely — whenever the
Light layer is visible it draws the full body+overlay together, hiding the
body layer behind it. When the Light layer is hidden, the bare body shows.

Net effect: the eye glow always animates in lockstep with the body, matching
how Citadel/BYOND renders the same icons (one mob, one timeline).
"""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image  # type: ignore[import-not-found]


def composite_overlay(body: Path, overlay: Path) -> None:
    """Open `body` and `overlay`, write back overlay = body + overlay."""
    img_body = Image.open(body).convert("RGBA")
    img_overlay = Image.open(overlay).convert("RGBA")
    if img_body.size != img_overlay.size:
        print(f"SKIP size mismatch {body}: {img_body.size} vs {overlay}: {img_overlay.size}", file=sys.stderr)
        return
    composite = Image.alpha_composite(img_body, img_overlay)
    composite.save(overlay)
    print(f"baked {overlay}")


def process_rsi(rsi_dir: Path) -> None:
    """For each RSI folder, find body + overlays by naming convention."""
    # Body = the .png whose state name has no underscore-suffix (e.g. `k9`, `medihound`)
    # Overlays = sibling .pngs whose state name = body name + "_e" or "_l"
    body_candidates: list[Path] = []
    for png in rsi_dir.glob("*.png"):
        stem = png.stem
        if "-" in stem:
            continue
        if stem.endswith("_e") or stem.endswith("_l"):
            continue
        body_candidates.append(png)
    if not body_candidates:
        print(f"SKIP {rsi_dir.name}: no body sprite found", file=sys.stderr)
        return
    if len(body_candidates) > 1:
        print(f"WARN {rsi_dir.name}: multiple body candidates {body_candidates}; using first", file=sys.stderr)
    body = body_candidates[0]
    base = body.stem
    for suffix in ("_e", "_l"):
        overlay = rsi_dir / f"{base}{suffix}.png"
        if overlay.exists():
            composite_overlay(body, overlay)


def main() -> int:
    root = Path("Resources/Textures/_ClawCommand/Mobs/Silicon")
    for rsi in sorted(root.glob("dogborg_*.rsi")):
        process_rsi(rsi)
    return 0


if __name__ == "__main__":
    sys.exit(main())
