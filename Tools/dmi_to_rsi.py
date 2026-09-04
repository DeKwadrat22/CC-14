"""DMI -> RSI converter, scoped to the Citadel-Station-13 dogborg port.

Reads a BYOND .dmi (PNG with a zTXt chunk containing the state descriptor) and
emits a directory tree of `.rsi` folders (PNG sheets + `meta.json`) compatible
with this fork's loader.

Usage:
    python tools/dmi_to_rsi.py <dmi-path> <out-dir> --state state1,state2,...

The script is intentionally small: it understands the subset of DMI features
the dogborg sheets actually use (4-dir sprites with frame counts > 1 = animation,
single-frame icons). It does NOT support movement states, hotspots, or unusual
loop counts beyond plain loop-forever.
"""
from __future__ import annotations

import argparse
import json
import struct
import sys
import zlib
from pathlib import Path

from PIL import Image  # type: ignore[import-not-found]


def read_dmi_descriptor(path: Path) -> tuple[str, Image.Image]:
    raw = path.read_bytes()
    pos = 8  # skip PNG signature
    descriptor: str | None = None
    while pos < len(raw):
        length = struct.unpack(">I", raw[pos : pos + 4])[0]
        chunk_type = raw[pos + 4 : pos + 8].decode("ascii", errors="replace")
        chunk_data = raw[pos + 8 : pos + 8 + length]
        if chunk_type == "zTXt":
            sep = chunk_data.index(b"\x00")
            descriptor = zlib.decompress(chunk_data[sep + 2 :]).decode("latin-1")
            break
        if chunk_type == "IEND":
            break
        pos += 12 + length
    if descriptor is None:
        raise RuntimeError(f"{path}: no zTXt DMI descriptor chunk found")
    img = Image.open(path).convert("RGBA")
    return descriptor, img


def parse_descriptor(text: str) -> tuple[int, int, list[dict]]:
    """Returns (width, height, [state_dict, ...]).

    Each state_dict has: name, dirs, frames, delay (list or None), loop.
    """
    width = height = 32
    states: list[dict] = []
    current: dict | None = None
    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("version") or line.startswith("# BEGIN") or line.startswith("# END"):
            continue
        if "=" not in line:
            continue
        key, _, value = line.partition("=")
        key = key.strip()
        value = value.strip()
        if key == "width":
            width = int(value)
        elif key == "height":
            height = int(value)
        elif key == "state":
            current = {
                "name": value.strip().strip('"'),
                "dirs": 1,
                "frames": 1,
                "delay": None,
                "loop": 0,
                "rewind": 0,
                "movement": 0,
            }
            states.append(current)
        elif current is not None:
            if key == "dirs":
                current["dirs"] = int(value)
            elif key == "frames":
                current["frames"] = int(value)
            elif key == "delay":
                current["delay"] = [float(v) for v in value.split(",")]
            elif key == "loop":
                current["loop"] = int(value)
            elif key == "rewind":
                current["rewind"] = int(value)
            elif key == "movement":
                current["movement"] = int(value)
    return width, height, states


def slice_sheet(img: Image.Image, width: int, height: int, states: list[dict]) -> list[list[Image.Image]]:
    """Return list-of-list: outer index = state, inner index = cell number.

    Cells are read left-to-right, top-to-bottom from the source sheet. Each cell
    is one (dir, frame) combination; ordering inside a state is frame-major,
    i.e. cells for state S are dir0-frame0, dir1-frame0, ..., dir(n-1)-frame0,
    dir0-frame1, ... — exactly how BYOND lays them out.
    """
    cols = img.width // width
    out: list[list[Image.Image]] = []
    cell_idx = 0
    for state in states:
        n_cells = state["dirs"] * state["frames"]
        cells: list[Image.Image] = []
        for _ in range(n_cells):
            row, col = divmod(cell_idx, cols)
            box = (col * width, row * height, (col + 1) * width, (row + 1) * height)
            cells.append(img.crop(box))
            cell_idx += 1
        out.append(cells)
    return out


def write_rsi(
    state_name: str,
    state: dict,
    cells: list[Image.Image],
    width: int,
    height: int,
    out_dir: Path,
    copyright_notice: str,
) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    # SS14 RSI directions: 1 = single, 4 = south/north/east/west, 8 = sw/se/nw/ne added.
    rsi_dirs = state["dirs"] if state["dirs"] in (1, 4, 8) else 1

    # BYOND ordering is S, N, E, W within a frame. SS14 RSI also uses S, N, E, W
    # for `directions: 4` so the mapping is 1:1.
    frames = state["frames"]
    # Build a sheet: (frames columns, dirs rows)
    sheet = Image.new("RGBA", (width * frames, height * rsi_dirs), (0, 0, 0, 0))
    for f in range(frames):
        for d in range(rsi_dirs):
            cell = cells[f * state["dirs"] + d]
            sheet.paste(cell, (f * width, d * height))
    sheet.save(out_dir / f"{state_name}.png")

    # meta.json
    meta = {
        "version": 1,
        "license": "CC-BY-SA-3.0",
        "copyright": copyright_notice,
        "size": {"x": width, "y": height},
        "states": [
            {
                "name": state_name,
                "directions": rsi_dirs,
            }
        ],
    }
    if frames > 1:
        delays = state["delay"] or [1.0] * frames
        # BYOND delay is in 1/10ths of a second. SS14 RSI expects seconds.
        delays_secs = [round(d / 10.0, 4) for d in delays]
        # Pad or truncate to match frames
        if len(delays_secs) < frames:
            delays_secs += [delays_secs[-1]] * (frames - len(delays_secs))
        delays_secs = delays_secs[:frames]
        meta["states"][0]["delays"] = [delays_secs for _ in range(rsi_dirs)]

    (out_dir / "meta.json").write_text(json.dumps(meta, indent=2))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("dmi", type=Path, help="Source .dmi file")
    ap.add_argument("out", type=Path, help="Destination RSI folder root")
    ap.add_argument(
        "--state",
        action="append",
        default=[],
        help="State name (or comma-separated). Only these will be exported. Repeat for groups going into separate .rsi folders.",
    )
    ap.add_argument(
        "--rsi-name",
        action="append",
        default=[],
        help="Per --state: name for the output .rsi folder. Defaults to the first state in the group.",
    )
    ap.add_argument(
        "--copyright",
        default="Citadel-Station-13 (GPL-3.0). Original sprite work; ported to SS14 RSI format.",
    )
    args = ap.parse_args()

    descriptor, img = read_dmi_descriptor(args.dmi)
    width, height, all_states = parse_descriptor(descriptor)
    by_name = {s["name"]: s for s in all_states}

    if not args.state:
        # List available states and exit
        print(f"width={width} height={height}")
        for s in all_states:
            print(
                f"  state={s['name']!r} dirs={s['dirs']} frames={s['frames']} delay={s['delay']}"
            )
        return 0

    # Slice the sheet once
    sliced = slice_sheet(img, width, height, all_states)
    name_to_cells = dict(zip([s["name"] for s in all_states], sliced))

    for i, group in enumerate(args.state):
        wanted = [n.strip() for n in group.split(",") if n.strip()]
        rsi_name = args.rsi_name[i] if i < len(args.rsi_name) else wanted[0]
        rsi_dir = args.out / f"{rsi_name}.rsi"
        # Build a merged sheet's worth of states
        # We write each state as a separate PNG inside one .rsi folder.
        rsi_dir.mkdir(parents=True, exist_ok=True)

        meta_states = []
        size: dict[str, int] | None = None
        for state_name in wanted:
            if state_name not in by_name:
                print(f"WARN: state {state_name!r} not found in {args.dmi}", file=sys.stderr)
                continue
            state = by_name[state_name]
            cells = name_to_cells[state_name]
            rsi_dirs = state["dirs"] if state["dirs"] in (1, 4, 8) else 1
            frames = state["frames"]
            sheet = Image.new("RGBA", (width * frames, height * rsi_dirs), (0, 0, 0, 0))
            for f in range(frames):
                for d in range(rsi_dirs):
                    cell_index = f * state["dirs"] + d
                    if cell_index >= len(cells):
                        continue
                    sheet.paste(cells[cell_index], (f * width, d * height))
            sheet.save(rsi_dir / f"{state_name}.png")
            entry = {"name": state_name, "directions": rsi_dirs}
            if frames > 1:
                delays = state["delay"] or [1.0] * frames
                # BYOND delay = tenths of a second; SS14 RSI uses seconds.
                delays_secs = [round(d / 10.0, 4) for d in delays]
                if len(delays_secs) < frames:
                    delays_secs += [delays_secs[-1]] * (frames - len(delays_secs))
                delays_secs = delays_secs[:frames]
                entry["delays"] = [delays_secs for _ in range(rsi_dirs)]
            meta_states.append(entry)
            size = {"x": width, "y": height}

        meta = {
            "version": 1,
            "license": "GPL-3.0-or-later",
            "copyright": args.copyright,
            "size": size or {"x": width, "y": height},
            "states": meta_states,
        }
        (rsi_dir / "meta.json").write_text(json.dumps(meta, indent=2))
        print(f"wrote {rsi_dir} with {len(meta_states)} state(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
