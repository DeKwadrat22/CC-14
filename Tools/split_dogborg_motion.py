"""
Split each dogborg RSI's animated body / eye / light states into a static
"<variant>" state (frame 0 only, no delays) and an animated "<variant>_moving"
state (all frames, original delays).

Combined with the SpriteMovement component on the dogborg chassis entities,
this lets the dogborgs stand still without their legs trotting on the spot.

PNG layout (verified):
  - Rows = 4 directions
  - Cols = N frames per direction
  - Frame size = meta.size

Skips variants whose body PNG already has 1 column (e.g. blade).

Run from repo root:
    python tools/split_dogborg_motion.py
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

RSI_ROOT = Path("Resources/Textures/_ClawCommand/Mobs/Silicon")

# Suffixes of the three motion-correlated states per variant (body / eye / light).
# These all carry the baked body, so they animate together.
OVERLAY_SUFFIXES = ["", "_e", "_l"]


def split_state_png(rsi_dir: Path, base_name: str, fw: int, fh: int) -> bool:
    """Split a multi-frame RSI state PNG into static (frame 0) + _moving copies.

    Returns True if a split happened; False if already single-frame (skipped).
    """
    src = rsi_dir / f"{base_name}.png"
    if not src.exists():
        return False

    img = Image.open(src).convert("RGBA")
    w, h = img.size
    cols = w // fw
    rows = h // fh

    if cols <= 1:
        return False  # already static; nothing to do

    # Static: column 0 of each row -> new single-column image.
    static = Image.new("RGBA", (fw, rows * fh), (0, 0, 0, 0))
    for r in range(rows):
        frame0 = img.crop((0, r * fh, fw, (r + 1) * fh))
        static.paste(frame0, (0, r * fh))

    # Animated: rename the original to <base>_moving.png
    moving_path = rsi_dir / f"{base_name}_moving.png"
    img.save(moving_path)

    # Overwrite static at original path.
    static.save(src)
    return True


def patch_meta(rsi_dir: Path) -> bool:
    """Add <state>_moving states to meta.json mirroring the existing animated
    states' direction count + delays; strip the delays off the original states
    so they're rendered static.

    Returns True if changes were written.

    A `_moving` entry is only added when a matching `<state>_moving.png` exists
    on disk — i.e. `split_state_png` actually produced a separate animated PNG.
    Source DMIs occasionally encode delays on single-frame states (e.g. the
    `-wreck` pose); those must stay as plain static states or the engine throws
    a missing-PNG error at load time.
    """
    meta_path = rsi_dir / "meta.json"
    meta = json.loads(meta_path.read_text())

    states = meta["states"]
    by_name = {s["name"]: s for s in states}

    changed = False
    for s in list(states):
        name = s["name"]
        if "delays" not in s:
            continue
        if name.endswith("_moving"):
            continue
        moving_name = f"{name}_moving"

        # Only emit a moving sibling when the animated PNG actually exists.
        # Otherwise, just demote this state to static.
        moving_png = rsi_dir / f"{moving_name}.png"
        if not moving_png.exists():
            del s["delays"]
            changed = True
            continue

        if moving_name in by_name:
            if "delays" in s:
                del s["delays"]
                changed = True
            continue

        states.append({
            "name": moving_name,
            "directions": s.get("directions", 1),
            "delays": s["delays"],
        })
        by_name[moving_name] = states[-1]
        del s["delays"]
        changed = True

    if changed:
        meta_path.write_text(json.dumps(meta, indent=4))
    return changed


def variant_from_dir(rsi_dir: Path) -> str | None:
    """dogborg_<variant>.rsi -> <variant>; None for non-dogborg dirs."""
    name = rsi_dir.name
    if not (name.startswith("dogborg_") and name.endswith(".rsi")):
        return None
    return name[len("dogborg_") : -len(".rsi")]


def process_variant(rsi_dir: Path) -> None:
    variant = variant_from_dir(rsi_dir)
    if variant is None:
        return

    meta = json.loads((rsi_dir / "meta.json").read_text())
    fw = meta["size"]["x"]
    fh = meta["size"]["y"]

    any_split = False
    for suffix in OVERLAY_SUFFIXES:
        base = f"{variant}{suffix}"
        if split_state_png(rsi_dir, base, fw, fh):
            any_split = True

    patched = patch_meta(rsi_dir)

    if any_split or patched:
        print(f"  {rsi_dir.name}: split={any_split} meta={patched}")
    else:
        print(f"  {rsi_dir.name}: no changes (already static or processed)")


def main() -> None:
    if not RSI_ROOT.is_dir():
        raise SystemExit(f"missing {RSI_ROOT}")

    print(f"Processing dogborg RSIs under {RSI_ROOT} ...")
    for rsi_dir in sorted(RSI_ROOT.iterdir()):
        if not rsi_dir.is_dir():
            continue
        process_variant(rsi_dir)
    print("Done.")


if __name__ == "__main__":
    main()
