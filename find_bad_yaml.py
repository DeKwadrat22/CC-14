#!/usr/bin/env python3
"""
Bulk YAML validator for SS14-style content repos.
Walks a directory tree, tries to parse every .yml/.yaml file, and reports
any that fail with the exact file + line/col, so you don't have to
hand-grep a huge Resources folder.

Usage:
    python3 find_bad_yaml.py /path/to/repo/Resources
    python3 find_bad_yaml.py /path/to/repo/Resources --filter "Outpost,Lavaland"
"""
import sys
import re
import argparse
from pathlib import Path

import yaml

# SS14 content YAML is full of engine-specific tags like !type:SoundPathSpecifier,
# !type:ConstructionGraphStep, !ResPath, etc. PyYAML's SafeLoader has no idea what
# to do with these and will throw "could not determine a constructor" on nearly
# every file, which drowns out real syntax errors. We register a pass-through
# multi-constructor for tag PREFIXES so any of these are treated as opaque data
# (their contents are still parsed structurally, just not type-resolved) instead
# of raising.
DEFAULT_IGNORE_TAG_PREFIXES = [
    "!type:",   # e.g. !type:SoundPathSpecifier, !type:SoundCollectionSpecifier
    "!ResPath",
    "!Res",
]


class Ss14SafeLoader(yaml.SafeLoader):
    """SafeLoader variant that treats specified custom-tag prefixes as opaque
    passthrough nodes instead of erroring on unknown constructors."""
    pass


def _passthrough_constructor(loader, tag_suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)


def register_ignored_tags(prefixes):
    for prefix in prefixes:
        yaml.add_multi_constructor(prefix, _passthrough_constructor, Loader=Ss14SafeLoader)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("root", help="Directory to scan recursively")
    parser.add_argument(
        "--filter",
        default="",
        help="Comma-separated substrings; matching filenames are checked FIRST",
    )
    parser.add_argument(
        "--ext",
        default="yml,yaml",
        help="Comma-separated extensions to check (default: yml,yaml)",
    )
    parser.add_argument(
        "--blacklist-tags",
        default="",
        help="Comma-separated tag prefixes to IGNORE in ADDITION to the defaults "
             f"({', '.join(DEFAULT_IGNORE_TAG_PREFIXES)}), e.g. '!type:SoundPathSpecifier,!type:ConstructionGraphStep'",
    )
    parser.add_argument(
        "--whitelist-tags",
        default="",
        help="Comma-separated tag prefixes to ignore INSTEAD of the defaults. "
             "Use this if you want everything else (including !type: tags not listed) to still raise errors.",
    )
    args = parser.parse_args()

    if args.whitelist_tags:
        ignore_prefixes = [t.strip() for t in args.whitelist_tags.split(",") if t.strip()]
    else:
        ignore_prefixes = list(DEFAULT_IGNORE_TAG_PREFIXES)
        if args.blacklist_tags:
            ignore_prefixes += [t.strip() for t in args.blacklist_tags.split(",") if t.strip()]

    register_ignored_tags(ignore_prefixes)
    print(f"Ignoring tag prefixes: {ignore_prefixes}\n")

    root = Path(args.root)
    if not root.exists():
        print(f"Path does not exist: {root}")
        sys.exit(1)

    exts = tuple("." + e.strip().lstrip(".") for e in args.ext.split(","))
    filters = [f.strip().lower() for f in args.filter.split(",") if f.strip()]

    all_files = [p for p in root.rglob("*") if p.suffix in exts and p.is_file()]

    # Priority files (matching filter) get checked first
    def is_priority(p: Path) -> bool:
        name = str(p).lower()
        return any(f in name for f in filters)

    priority_files = [p for p in all_files if is_priority(p)]
    other_files = [p for p in all_files if not is_priority(p)]

    print(f"Found {len(all_files)} YAML files under {root}")
    if filters:
        print(f"  -> {len(priority_files)} match filter {filters}, checking those first\n")

    errors = []

    def check(files, label):
        for fp in files:
            try:
                with open(fp, "r", encoding="utf-8") as fh:
                    # load_all handles multi-document files (SS14 maps are usually single-doc,
                    # but some prototype files use multiple documents). Uses Ss14SafeLoader so
                    # !type:X / !ResPath tags don't false-positive as unknown constructors.
                    list(yaml.load_all(fh, Loader=Ss14SafeLoader))
            except yaml.YAMLError as e:
                errors.append((fp, e))
                mark = getattr(e, "problem_mark", None)
                loc = f" (line {mark.line + 1}, col {mark.column + 1})" if mark else ""
                print(f"[BAD YAML]{loc} {fp}")
                print(f"    {str(e).splitlines()[0]}")
            except UnicodeDecodeError as e:
                errors.append((fp, e))
                print(f"[ENCODING ERROR] {fp}: {e}")

    if priority_files:
        check(priority_files, "priority")
    check(other_files, "other")

    print(f"\nDone. {len(errors)} file(s) failed to parse out of {len(all_files)}.")
    if errors:
        sys.exit(1)


if __name__ == "__main__":
    main()
