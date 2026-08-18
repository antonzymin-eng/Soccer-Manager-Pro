#!/usr/bin/env python3
# File: tools/assembly-tier-check.py
# Created: August 17, 2026
# Modified: August 18, 2026
# Purpose: Mechanical guard for the Spec #20 §3.5.2 ten-tier assembly order
#          (FR-CS-046 / FR-CS-046a / FR-CS-046b). Parses the tier table OUT OF
#          the spec — docs/specs/code-standards/section-3.md §3.5.2 — rather
#          than carrying its own copy (a hard-coded duplicate would be a second
#          surface to keep in sync, the recurring defect class this repo keeps
#          filing), enumerates every production src/<folder>/<name>.asmdef, and
#          fails on:
#            * any production folder absent from the table,
#            * any table entry naming a folder that does not exist,
#            * any folder seated in more than one tier,
#            * any tier row with an empty "Why this tier" cell (the §3.5.2
#              placement rule requires the seating be justified there; this
#              tool checks presence, not adequacy),
#            * a top-level src/<folder>/ holding more than one production
#              .asmdef (the table is folder-keyed; there is no seat for two),
#            * the table's out-of-band Infrastructure set differing BY NAME
#              from the pair FR-CS-046b binds (parsed from the FR-CS-046b row
#              of docs/specs/code-standards/section-2.md, the authority, so
#              reseating that row into the ordered tiers cannot silently
#              disable the FR-CS-046b checks),
#            * any upward reference (FR-CS-046),
#            * any ordered-tier reference into an out-of-band Infrastructure
#              assembly ("no tier may reference them at runtime", FR-CS-046b
#              clause 1),
#            * any Infrastructure-sourced reference whose target is not tier 0
#              (Foundation) and not the other Infrastructure assembly
#              (FR-CS-046b clause 2),
#            * any cycle in the production reference graph (FR-CS-046a).
#          Prints the recomputed reference breakdown (downward / intra-tier /
#          upward / Infrastructure-sourced) so §3.5.2's adoption verification
#          re-runs on every invocation instead of being a one-off hand check.
#          Wired into CI: the `Spec hygiene checks` job in
#          .github/workflows/ci.yml runs this on every push and pull request.
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
SECTION2_PATH = Path("docs") / "specs" / "code-standards" / "section-2.md"
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
        if not cells[2]:
            errors.append(
                "tier row %r has an empty 'Why this tier' cell — the §3.5.2 "
                "placement rule requires the seating be justified there "
                "(this check verifies presence only; adequacy is review)" % tier_cell)
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


def parse_infra_binding(section2_text):
    """Extract the Infrastructure assembly names FR-CS-046b binds.

    FR-CS-046 and FR-CS-046b bind the two out-of-band Infrastructure
    assemblies BY NAME, while the §3.5.2 table marks Infrastructure
    membership only by its tier cell's leading glyph ("—"). One character of
    drift in that cell (e.g. rewriting the row as tier 0 or tier 10) would
    otherwise empty the infra set silently and disable every FR-CS-046b
    check while the run still reports PASS. So the names are read out of the
    authority — the FR-CS-046b row of section-2.md §2.2.5 — rather than
    hard-coded here (no second copy to drift), and main() asserts the
    table's out-of-band set equals them exactly.

    Returns (sorted_names, error) — error is a string on any parse failure.
    """
    m = re.search(r"^\|\s*FR-CS-046b\s*\|(.*)$", section2_text, re.MULTILINE)
    if not m:
        return [], "cannot find the FR-CS-046b row in %s" % SECTION2_PATH
    paren = re.search(
        r"out-of-band \*\*Infrastructure\*\* assembly \(([^)]*)\)", m.group(1))
    if not paren:
        return [], (
            "the FR-CS-046b row in %s no longer names its Infrastructure "
            "assemblies in the expected shape "
            "(\"out-of-band **Infrastructure** assembly (`…`, `…`)\")"
            % SECTION2_PATH)
    names = re.findall(r"`([^`]+)`", paren.group(1))
    if not names:
        return [], (
            "the FR-CS-046b parenthesis in %s contains no backticked "
            "assembly names" % SECTION2_PATH)
    return sorted(set(names)), None


def load_production_asmdefs(repo):
    """Enumerate production asmdefs under src/, recursively.

    docs/specs/code-standards/section-3.md §3.5.2 states the exclusion rule
    as: test assemblies live in src/<folder>/[Tt]ests/. So a production
    asmdef is any src/**/*.asmdef whose path (below the top-level src/<folder>/
    it lives in) does not pass through a path segment matching [Tt]ests --
    depth alone is not the rule.

    The §3.5.2 tier table is FOLDER-keyed, so exactly ONE production .asmdef
    may exist per top-level src/<folder>/ (the §3.5.2 placement rule). A
    second one — nested or not, e.g. src/foo/editor/foo-editor.asmdef beside
    src/foo/foo.asmdef — is a HARD FAILURE: there is no rule for seating two
    assemblies under one folder key. Which of the two survives into the graph
    is an accident of path sort order (the first wins), so the failure
    message names both paths and says which one was dropped; the run's other
    figures must not be trusted while that failure stands.

    Returns (folder_of_name, name_of_folder, refs, errors) where refs maps
    assembly name -> list of referenced assembly names and errors is a list
    of parse-failure strings.
    """
    src = repo / "src"
    folder_of_name = {}
    name_of_folder = {}
    path_of_folder = {}
    refs = {}
    errors = []
    for asmdef in sorted(src.glob("**/*.asmdef")):
        rel_parts = asmdef.relative_to(src).parts
        if len(rel_parts) < 2:
            continue  # stray file directly under src/, not inside any folder
        folder = rel_parts[0]
        if any(re.fullmatch(r"[Tt]ests", part) for part in rel_parts[1:-1]):
            continue  # excluded: a [Tt]ests path segment, per §3.5.2
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
            errors.append(
                "folder src/%s/ holds more than one production .asmdef — the "
                "§3.5.2 tier table is folder-keyed, exactly one is allowed; "
                "kept '%s' (first in path sort order), DROPPED '%s' and all "
                "of its references from the graph"
                % (folder, path_of_folder[folder], asmdef))
            continue
        folder_of_name[name] = folder
        name_of_folder[folder] = name
        path_of_folder[folder] = asmdef
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
    section2_file = repo / SECTION2_PATH
    if not section2_file.is_file():
        print("FATAL: %s not found (is --repo the repository root?)" % section2_file)
        return 2

    tier_of, ordered_tiers, infra_folders, parse_errors = parse_tier_table(
        spec_file.read_text(encoding="utf-8"))
    bound_infra, infra_err = parse_infra_binding(
        section2_file.read_text(encoding="utf-8"))
    if infra_err:
        print("FATAL: %s" % infra_err)
        return 2
    folder_of_name, name_of_folder, refs, asmdef_errors = load_production_asmdefs(repo)

    failures = list(parse_errors) + list(asmdef_errors)
    if not name_of_folder:
        failures.append("no production .asmdef found under %s" % (repo / "src"))

    # 0. The table's out-of-band Infrastructure set must be exactly the pair
    #    FR-CS-046b binds BY NAME. Membership above is derived only from the
    #    tier cell's leading glyph, so one character of drift (an ordered
    #    number where "—" stood) would otherwise empty this set silently and
    #    disable every FR-CS-046b check while the run still passes.
    if set(infra_folders) != set(bound_infra):
        failures.append(
            "the §3.5.2 Infrastructure row no longer names the assemblies "
            "FR-CS-046b binds: the table's out-of-band set is {%s} but "
            "FR-CS-046b (%s) binds {%s} — Infrastructure membership is bound "
            "BY NAME, and folding that row into the ordered tiers (or "
            "renaming its assemblies) would silently disable the FR-CS-046b "
            "checks, so it MUST fail here"
            % (", ".join(infra_folders) or "empty",
               SECTION2_PATH, ", ".join(bound_infra)))

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
                infra_sourced += 1
                # FR-CS-046b clause 2: an Infrastructure assembly may reference
                # only tier-0 (Foundation) assemblies and the other
                # Infrastructure assembly — nothing else.
                if dst_tier != "infra" and dst_tier != 0:
                    failures.append(
                        "Infrastructure assembly 'src/%s/' references 'src/%s/' "
                        "(tier %s) — FR-CS-046b: an Infrastructure assembly MUST "
                        "NOT reference any ordered-tier assembly other than "
                        "tier 0 (Foundation)" % (src_folder, dst_folder, dst_tier))
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
    print("  folders placed in the tier table       : %d  (%d in ordered tiers + "
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

# Version History
# | Version | Date       | Author      | Notes                                        |
# | 1.0     | 2026-08-17 | Claude Code | Initial: §3.5.2 tier-table vs production     |
# |         |            |             | .asmdef graph — absent/ghost/duplicate       |
# |         |            |             | seatings, upward references (FR-CS-046),     |
# |         |            |             | ordered-tier->Infrastructure references,     |
# |         |            |             | cycles (FR-CS-046a).                         |
# | 1.1     | 2026-08-18 | Claude Code | AR round 2 (H9): recursive enumeration with  |
# |         |            |             | the [Tt]ests path-segment exclusion replaces |
# |         |            |             | depth-1 glob; FR-CS-046b clause 2 now        |
# |         |            |             | ENFORCED (Infrastructure-sourced references  |
# |         |            |             | were previously counted but never checked —  |
# |         |            |             | a verdict-semantics change).                 |
# | 1.2     | 2026-08-18 | Claude Code | Reviewed-findings pass (M1 + Lows): the      |
# |         |            |             | out-of-band Infrastructure set is asserted   |
# |         |            |             | BY NAME against the pair FR-CS-046b binds,   |
# |         |            |             | parsed from section-2.md's FR-CS-046b row    |
# |         |            |             | (reseating the "—" row to an ordered tier    |
# |         |            |             | previously PASSED and silently disabled both |
# |         |            |             | FR-CS-046b checks — a verdict-semantics      |
# |         |            |             | change); empty "Why this tier" cells now     |
# |         |            |             | fail; the one-production-.asmdef-per-folder  |
# |         |            |             | failure names both paths and which was       |
# |         |            |             | dropped; docstring's nested-asmdef claim     |
# |         |            |             | corrected to the enforced one-per-folder     |
# |         |            |             | rule; header gains Modified + this block.    |
