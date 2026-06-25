# Dogborg sprite attribution

All `dogborg_*.rsi` folders in this directory are ports of sprite work from the
**Citadel-Station-13** project (a BYOND/DM Space Station 13 server codebase),
specifically from:

    modular_citadel/icons/mob/widerobot.dmi

Source repository: https://github.com/Citadel-Station-13

## License — CC-BY-SA-3.0

Per the upstream Citadel-Station-13 README:

> All assets including icons and sound are under a Creative Commons 3.0 BY-SA
> license (https://creativecommons.org/licenses/by-sa/3.0/) unless otherwise
> indicated.

These sprites are therefore licensed under **CC-BY-SA-3.0**. This license
covers artwork only; it is distinct from the project's *code* license
(Citadel-Station-13's DM code is AGPL-3.0). No Citadel-Station-13 code was
ported into this repository — sprites only.

Each `meta.json` in this directory carries:

    "license": "CC-BY-SA-3.0"
    "copyright": "Citadel-Station-13 (https://github.com/Citadel-Station-13). ..."

Attribution chain: Citadel-Station-13 → /tg/station (Citadel's upstream) →
the original sprite contributors. As Citadel-Station-13 is a long-running
multi-contributor codebase, individual sprite authors are not always tracked
at the asset level; the project name stands in as the collective attribution
per CC-BY-SA-3.0 §4(c).

Cyborg behavior is provided by this fork's existing `BorgChassis*` entities;
the dogborg entities are children of those upstream chassis prototypes and
reuse their components verbatim — only the `sprite:` reference differs.

## Sprite -> RSI conversion

DMI (BYOND PNG + zTXt descriptor) was converted to SS14 RSI format using
`tools/dmi_to_rsi.py` in this repository. The converter:

  - Splits each multi-direction / multi-frame state into individual cells.
  - Re-emits one PNG per state (frames laid out left-to-right, directions
    top-to-bottom).
  - Generates `meta.json` with the SS14 schema (`size`, `states`, `delays`).
  - Converts BYOND animation delays (tenths of a second) into seconds.

The original sheets are 62x32 (wide dogborg sprites that BYOND offsets by
`pixel_x = -16` at draw time); the SS14 sprites preserve the same 62x32 size.

## Variants ported

| Module | Variant | RSI folder |
|---|---|---|
| Security | K9 | dogborg_k9.rsi |
| Security | K9 Dark | dogborg_k9dark.rsi |
| Security | Vale | dogborg_valesec.rsi |
| Medical | Medihound | dogborg_medihound.rsi |
| Medical | Medihound Dark | dogborg_medihounddark.rsi |
| Medical | Vale | dogborg_valemed.rsi |
| Engineering | Pup Dozer | dogborg_pupdozer.rsi |
| Engineering | Vale | dogborg_valeeng.rsi |
| Janitor | Scrubpup | dogborg_scrubpup.rsi |
| Service | Vale | dogborg_valeserv.rsi |
| Miner | Blade | dogborg_blade.rsi |
