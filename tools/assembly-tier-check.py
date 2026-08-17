#!/usr/bin/env python3
# File: tools/assembly-tier-check.py
# Created: August 17, 2026
# Purpose: Mechanical guard for the Spec #20 §3.5.2 ten-tier assembly order
#          (FR-CS-046 / FR-CS-046a). Parses the tier table OUT OF the spec —
#          docs/specs/code-standards/section-3.md §3.5.2 — rather than carrying
#          its own copy (a hard-coded duplicate would be a second surface to
#          keep in sync, the recurring defect class this repo keeps filing),
#          enumerates every production src/<folder>/<name>.asmdef, and fails on:
#            * any production folder absent from the table,
#            * any table entry naming a folder that does not exist,
#            * any folder seated in more than one tier,
#            * any upward reference (FR-CS-046),
#            * any ordered-tier reference into an out-of-band Infrastructure
#              assembly ("no tier may reference them at runtime"),
#            * any cycle in the production reference graph (FR-CS-046a).
#          Prints the recomputed reference breakdown (downward / intra-tier /
#          upward / Infrastructure-sourced) so §3.5.2's adoption verification
#          re-runs on every invocation instead of being a one-off hand check.
#
# Usage:  python3 tools/assembly-tier-check.py --repo .
# Exit:   0 on pass, 1 on any failure, 2 on inability to parse inputs.
#
# Plain Python 3, standard library only.

import argparse
import json
import re
import sys
from pathlib import Path

SPEC_PATH = Path("docs") / "specs" / "code-standards" / "section-3.md"
SECTION_HEADING = "### 3.5.2"


def parse_tier_table(spec_text):
    """Parse the §3.5.2 tier table.

    Returns (tier_of_folder, ordered_tiers, infra_folders, errors) where
    tier_of_folder maps folder name -> int tier or the string "infra".
    """
    errors = []
    idx = spec_text.find(SECTION_HEADING)
    if idx < 0:
        return {}, [], [], ["cannot find heading '%s' in %s" % (SECTION_HEADING, SPEC_PATH)]
    # Slice to the next same-or-higher-level heading so we only see §3.5.2.
    m = re.search(r"\n#{2,3} (?!3\.5\.2)", spec_text[idx + len(SECTION_HEADING):])
    section = spec_text[idx: idx + len(SECTION_HEADING) + (m.start() if m else len(spec_text))]

    tier_of = {}
    duplicate_errors = []
    in_table = False
    saw_table = False
    for line in section.splitlines():
        stripped = line.strip()
        if not stripped.startswith("|"):
            if in_table:
                break  # table ended
            continue
        cells = [c.strip() for c in stripped.strip("|").split("|")]
        if len(cells) < 3:
            continue
        if cells[0] == "Tier" and cells[1] == "Assemblies":
            in_table = True
            saw_table = True
            continue
        if not in_table:
            continue
        if set(cells[0]) <= set("-: "):
            continue  # separator row
        tier_cell, asm_cell = cells[0], cells[1]
        num = re.match(r"^(\d+)\b", tier_cell)
        if num:
            tier = int(num.group(1))
        elif tier_cell.startswith(("—", "--", "-")):
            tier = "infra"
        else:
            errors.append("unparseable tier cell in §3.5.2 table: %r" % tier_cell)
            continue
        folders = re.findall(r"`([^`]+)`", asm_cell)
        if not folders:
            errors.append("tier row %r names no backticked assembly folders" % tier_cell)
        for folder in folders:
            if folder in tier_of:
                duplicate_errors.append(
                    "folder '%s' seated in more than one tier (%s and %s)"
                    % (folder, tier_of[folder], tier))
            else:
                tier_of[folder] = tier
    if not saw_table:
        errors.append("no '| Tier | Assemblies |' table found under %s" % SECTION_HEADING)
    errors.extend(duplicate_errors)
    ordered = sorted({t for t in tier_of.values() if t != "infra"})
    infra = sorted(f for f, t in tier_of.items() if t == "infra")
    return tier_of, ordered, infra, errors


def load_production_asmdefs(repo):
    """Enumerate production asmdefs: src/<folder>/<name>.asmdef (tests are deeper).

    Returns (folder_of_name, name_of_folder, refs) where refs maps
    assembly name -> list of referenced assembly names.
    """
    src = repo / "src"
    folder_of_name = {}
    name_of_folder = {}
    refs = {}
    errors = []
    for asmdef in sorted(src.glob("*/*.asmdef")):
        folder = asmdef.parent.name
        try:
            data = json.loads(asmdef.read_text(encoding="utf-8"))
        except (OSError, ValueError) as exc:
            errors.append("cannot parse %s: %s" % (asmdef, exc))
            continue
        name = data.get("name")
        if not name:
            errors.append("%s has no 'name' field" % asmdef)
            continue
        if folder in name_of_folder:
            errors.append("folder src/%s/ holds more than one production .asmdef" % folder)
            continue
        folder_of_name[name] = folder
        name_of_folder[folder] = name
        refs[name] = [r for r in data.get("references", [])]
    return folder_of_name, name_of_folder, refs, errors


def find_cycle(graph):
    """Return one cycle as a list of nodes, or None. graph: node -> iterable of nodes."""
    WHITE, GREY, BLACK = 0, 1, 2
    color = {n: WHITE for n in graph}
    stack = []

    def dfs(node):
        color[node] = GREY
        stack.append(node)
        for nxt in graph.get(node, ()):
            if nxt not in color:
                continue
            if color[nxt] == GREY:
                return stack[stack.index(nxt):] + [nxt]
            if color[nxt] == WHITE:
                cyc = dfs(nxt)
                if cyc:
                    return cyc
        stack.pop()
        color[node] = BLACK
        return None

    for n in graph:
        if color[n] == WHITE:
            cyc = dfs(n)
            if cyc:
                return cyc
    return None


def main():
    ap = argparse.ArgumentParser(
        description="Check the Spec #20 §3.5.2 ten-tier assembly order against "
                    "the production .asmdef reference graph.")
    ap.add_argument("--repo", default=".", help="repository root (default: .)")
    args = ap.parse_args()
    repo = Path(args.repo)

    spec_file = repo / SPEC_PATH
    if not spec_file.is_file():
        print("FATAL: %s not found (is --repo the repository root?)" % spec_file)
        return 2

    tier_of, ordered_tiers, infra_folders, parse_errors = parse_tier_table(
        spec_file.read_text(encoding="utf-8"))
    folder_of_name, name_of_folder, refs, asmdef_errors = load_production_asmdefs(repo)

    failures = list(parse_errors) + list(asmdef_errors)
    if not name_of_folder:
        failures.append("no production .asmdef found under %s" % (repo / "src"))

    # 1. Every production folder must appear in the table.
    for folder in sorted(name_of_folder):
        if folder not in tier_of:
            failures.append(
                "production folder 'src/%s/' is ABSENT from the §3.5.2 tier table "
                "(the placement rule requires the same commit that adds the .asmdef "
                "to seat it)" % folder)

    # 2. Every table entry must name an existing production folder.
    for folder in sorted(tier_of):
        if folder not in name_of_folder:
            failures.append(
                "§3.5.2 tier table names '%s' but src/%s/ holds no production "
                ".asmdef" % (folder, folder))

    # 3. Classify every production->production reference.
    downward = intra = upward = infra_sourced = external = 0
    total = 0
    for name in sorted(refs):
        src_folder = folder_of_name[name]
        src_tier = tier_of.get(src_folder)
        for ref in refs[name]:
            if ref not in folder_of_name:
                external += 1  # not a production assembly (e.g. Unity/test) — out of scope
                continue
            total += 1
            dst_folder = folder_of_name[ref]
            dst_tier = tier_of.get(dst_folder)
            if src_tier is None or dst_tier is None:
                continue  # already failed above as absent-from-table
            if src_tier == "infra":
                infra_sourced += 1  # out of band by definition, whatever the target
                continue
            if dst_tier == "infra":
                failures.append(
                    "ordered-tier assembly 'src/%s/' references out-of-band "
                    "Infrastructure assembly 'src/%s/' — §3.5.2: no tier may "
                    "reference Infrastructure at runtime" % (src_folder, dst_folder))
                continue
            if dst_tier < src_tier:
                downward += 1
            elif dst_tier == src_tier:
                intra += 1
            else:
                upward += 1
                failures.append(
                    "UPWARD reference (FR-CS-046): 'src/%s/' (tier %s) references "
                    "'src/%s/' (tier %s)" % (src_folder, src_tier, dst_folder, dst_tier))

    # 4. The whole production reference graph must be acyclic (FR-CS-046a).
    graph = {n: [r for r in rs if r in folder_of_name] for n, rs in refs.items()}
    cycle = find_cycle(graph)
    if cycle:
        failures.append(
            "CYCLE in production reference graph (FR-CS-046a): %s"
            % " -> ".join(folder_of_name[n] for n in cycle))

    placed = sum(1 for f in tier_of if f in name_of_folder)
    print("assembly-tier-check: §3.5.2 table vs production .asmdef graph")
    print("  production assembly folders under src/ : %d" % len(name_of_folder))
    print("  folders placed in the tier table       : %d  (%d ordered tiers + "
          "%d out-of-band Infrastructure)" % (
              placed, sum(1 for f in tier_of
                          if f in name_of_folder and tier_of[f] != "infra"),
              sum(1 for f in infra_folders if f in name_of_folder)))
    print("  production->production references      : %d" % total)
    print("    downward                             : %d" % downward)
    print("    intra-tier                           : %d" % intra)
    print("    upward                               : %d" % upward)
    print("    sourced by out-of-band Infrastructure: %d" % infra_sourced)
    if external:
        print("  references to non-production assemblies (ignored): %d" % external)

    if failures:
        print()
        print("FAIL — %d problem(s):" % len(failures))
        for f in failures:
            print("  * %s" % f)
        return 1
    print("PASS — every folder placed, no upward reference, graph acyclic.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
