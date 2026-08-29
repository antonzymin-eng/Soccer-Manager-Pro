#!/usr/bin/env python3
# File: tools/assembly-tier-check.py
# Created: August 17, 2026
# Modified: August 29, 2026
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
#            * the ordered tier rows failing to be exactly ten, numbered 0..9
#              contiguously and ascending in table order, with each row's
#              (number, name) pair matching FR-CS-046's stated sequence
#              (parsed from the FR-CS-046 row of section-2.md, the authority —
#              previously swapping two tier numbers in the table PASSED),
#            * any production assembly referencing a TEST assembly (§3.5.2's
#              exclusion is test→production; the reverse is enforced here),
#            * any reference naming an assembly that resolves to nothing
#              under src/ (an unresolvable name must fail, not be ignored),
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
#          .github/workflows/ci.yml runs this on pushes to `main` and on pull
#          requests targeting `main` (those are ci.yml's only triggers — a
#          push to any other branch runs no CI, an owner call about CI cost).
#
# Usage:  python3 tools/assembly-tier-check.py --repo .
#         python3 tools/assembly-tier-check.py --repo . --json
# Exit:   0 on pass, 1 on any failure, 2 on inability to parse inputs.
#
# Plain Python 3, standard library only.

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

SPEC_PATH = Path("docs") / "specs" / "code-standards" / "section-3.md"
SECTION2_PATH = Path("docs") / "specs" / "code-standards" / "section-2.md"
SECTION_HEADING = "### 3.5.2"


def parse_tier_table(spec_text):
    """Parse the §3.5.2 tier table.

    Returns (tier_of_folder, ordered_rows, infra_folders, errors) where
    tier_of_folder maps folder name -> int tier or the string "infra", and
    ordered_rows is [(tier_number, tier_name)] for the numbered rows in
    TABLE ORDER (so main() can assert the numbering is 0..9, contiguous,
    ascending, and name-matched against FR-CS-046's stated sequence).
    """
    errors = []
    idx = spec_text.find(SECTION_HEADING)
    if idx < 0:
        return {}, [], [], ["cannot find heading '%s' in %s" % (SECTION_HEADING, SPEC_PATH)]
    # Slice to the next same-or-higher-level heading so we only see §3.5.2.
    m = re.search(r"\n#{2,3} (?!3\.5\.2)", spec_text[idx + len(SECTION_HEADING):])
    section = spec_text[idx: idx + len(SECTION_HEADING) + (m.start() if m else len(spec_text))]

    tier_of = {}
    ordered_rows = []
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
            name_m = re.search(r"\*\*([^*]+)\*\*", tier_cell)
            ordered_rows.append(
                (tier, name_m.group(1).strip() if name_m else ""))
            if not name_m:
                errors.append(
                    "ordered tier row %r carries no bold **name** — the "
                    "(number, name) pair cannot be checked against FR-CS-046"
                    % tier_cell)
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
    infra = sorted(f for f, t in tier_of.items() if t == "infra")
    return tier_of, ordered_rows, infra, errors


def parse_tier_sequence(section2_text):
    """Extract the ten tier names FR-CS-046 states, in order.

    The FR-CS-046 row of section-2.md §2.2.5 spells the order out in a
    parenthesis ("(Foundation → Physics → … → Client)"). Parsing it by name —
    the same technique parse_infra_binding uses for FR-CS-046b — gives the
    §3.5.2 table's numbering an authority to be checked against: previously
    NOTHING checked the tier numbers were 0..9, contiguous, ten in count,
    ascending, or paired with the right names, so swapping two rows' numbers
    PASSED.

    Returns (names, error) — error is a string on any parse failure.
    """
    m = re.search(r"^\|\s*FR-CS-046\s*\|(.*)$", section2_text, re.MULTILINE)
    if not m:
        return [], "cannot find the FR-CS-046 row in %s" % SECTION2_PATH
    paren = re.search(
        r"ten-tier order defined in §3\.5\.2\s*\(([^)]*)\)", m.group(1))
    if not paren:
        return [], (
            "the FR-CS-046 row in %s no longer states its tier sequence in "
            "the expected shape (\"ten-tier order defined in §3.5.2 "
            "(A → B → …)\")" % SECTION2_PATH)
    names = [n.strip() for n in paren.group(1).split("→")]
    names = [n for n in names if n]
    if len(names) != 10:
        return [], (
            "the FR-CS-046 parenthesis in %s names %d tiers; the ten-tier "
            "order requires exactly 10" % (SECTION2_PATH, len(names)))
    return names, None


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


def load_asmdefs(repo):
    """Enumerate and parse every src/**/*.asmdef once.

    Test classification follows #20 §3.5.2 exactly: a path segment matching
    [Tt]ests below src/<folder>/ marks a test assembly. A stray .asmdef
    directly under src/ remains in the complete graph but is reported as an
    unclassifiable policy failure because §3.5.2 is folder-keyed.

    Returns (records, errors). Each record carries path, folder, name,
    references and is_test.
    """
    src = repo / "src"
    records = []
    errors = []
    for asmdef in sorted(src.glob("**/*.asmdef")):
        rel_parts = asmdef.relative_to(src).parts
        stray = len(rel_parts) < 2
        try:
            data = json.loads(asmdef.read_text(encoding="utf-8"))
        except (OSError, ValueError) as exc:
            errors.append("cannot parse %s: %s" % (asmdef, exc))
            continue
        name = data.get("name")
        refs = data.get("references", [])
        if not name:
            errors.append("%s has no 'name' field" % asmdef)
            continue
        if not isinstance(refs, list) or not all(isinstance(ref, str) for ref in refs):
            errors.append("%s has an invalid 'references' field" % asmdef)
            continue
        is_test = (not stray) and any(
            re.fullmatch(r"[Tt]ests", part)
            for part in rel_parts[1:-1]
        )
        records.append({
            "path": asmdef.relative_to(repo).as_posix(),
            "folder": None if stray else rel_parts[0],
            "name": name,
            "references": sorted(refs),
            "is_test": is_test,
        })
        if stray:
            errors.append(
                "%s is directly under src/ and has no top-level folder; "
                "§3.5.2 is folder-keyed, so the assembly cannot be classified"
                % asmdef)
    return records, errors


def load_production_asmdefs(records):
    """Derive the existing production policy view from parsed asmdef records."""
    folder_of_name = {}
    name_of_folder = {}
    path_of_folder = {}
    refs = {}
    test_names = set()
    errors = []

    for record in records:
        if record["is_test"]:
            test_names.add(record["name"])

    for record in records:
        if record["is_test"]:
            continue
        folder = record["folder"]
        name = record["name"]
        path = record["path"]
        if folder is None:
            continue
        if folder in name_of_folder:
            errors.append(
                "folder src/%s/ holds more than one production .asmdef — the "
                "§3.5.2 tier table is folder-keyed, exactly one is allowed; "
                "kept '%s' (first in path sort order), DROPPED '%s' and all "
                "of its references from the graph"
                % (folder, path_of_folder[folder], path))
            continue
        if name in folder_of_name:
            errors.append(
                "assembly name '%s' is declared by two production .asmdef "
                "files — src/%s/ and %s; names are the graph's node keys, so "
                "the second would silently REPLACE the first's reference list "
                "and drop a whole assembly's edges from the tier and cycle "
                "checks while the folder count still reads complete"
                % (name, folder_of_name[name], path))
            continue
        folder_of_name[name] = folder
        name_of_folder[folder] = name
        path_of_folder[folder] = path
        refs[name] = list(record["references"])
    return folder_of_name, name_of_folder, refs, test_names, errors


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


def find_cycle_components(graph):
    """Return deterministic strongly-connected components that contain cycles."""
    index = 0
    stack = []
    on_stack = set()
    indices = {}
    lowlinks = {}
    components = []

    def strongconnect(node):
        nonlocal index
        indices[node] = index
        lowlinks[node] = index
        index += 1
        stack.append(node)
        on_stack.add(node)

        for nxt in sorted(graph.get(node, ())):
            if nxt not in graph:
                continue
            if nxt not in indices:
                strongconnect(nxt)
                lowlinks[node] = min(lowlinks[node], lowlinks[nxt])
            elif nxt in on_stack:
                lowlinks[node] = min(lowlinks[node], indices[nxt])

        if lowlinks[node] != indices[node]:
            return
        component = []
        while True:
            member = stack.pop()
            on_stack.remove(member)
            component.append(member)
            if member == node:
                break
        component.sort()
        if len(component) > 1 or (
                len(component) == 1 and component[0] in graph.get(component[0], ())):
            components.append(component)

    for node in sorted(graph):
        if node not in indices:
            strongconnect(node)
    components.sort(key=lambda item: tuple(item))
    return components


def _digest(value):
    payload = json.dumps(
        value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def build_machine_report(
        records, tier_of, ordered_rows, infra_folders, bound_infra,
        bound_sequence, folder_of_name, failures, stats):
    """Build the machine-readable A1 evidence view from the checker's facts."""
    tier_names = dict(ordered_rows)
    by_name = {}
    for record in records:
        by_name.setdefault(record["name"], []).append(record)

    nodes = []
    classification_counts = {}
    for record in sorted(records, key=lambda item: item["path"]):
        if record["is_test"]:
            classification = "test"
            normative_category = None
        else:
            tier = tier_of.get(record["folder"])
            if tier is None:
                classification = "unresolved"
                normative_category = None
            elif tier == "infra":
                classification = "out-of-band"
                normative_category = {
                    "kind": "out-of-band",
                    "name": "Infrastructure",
                }
            else:
                classification = "production"
                normative_category = {
                    "kind": "tier",
                    "tier": tier,
                    "name": tier_names.get(tier),
                }
        classification_counts[classification] = (
            classification_counts.get(classification, 0) + 1)
        nodes.append({
            "path": record["path"],
            "folder": record["folder"],
            "name": record["name"],
            "classification": classification,
            "normative_category": normative_category,
            "references": list(record["references"]),
        })

    unique_by_name = {
        name: items[0] for name, items in by_name.items() if len(items) == 1
    }
    duplicate_names = [
        {
            "name": name,
            "paths": sorted(item["path"] for item in items),
        }
        for name, items in sorted(by_name.items())
        if len(items) > 1
    ]

    internal_edges = []
    external_references = []
    ambiguous_references = []
    all_graph = {node["path"]: [] for node in nodes}
    production_paths = {
        node["path"] for node in nodes
        if node["classification"] in ("production", "out-of-band")
    }

    node_by_path = {node["path"]: node for node in nodes}
    for node in nodes:
        for ref in node["references"]:
            target = unique_by_name.get(ref)
            if target is not None:
                edge = {
                    "source": node["path"],
                    "source_name": node["name"],
                    "target": target["path"],
                    "target_name": ref,
                }
                internal_edges.append(edge)
                all_graph[node["path"]].append(target["path"])
            elif ref in by_name:
                ambiguous_references.append({
                    "source": node["path"],
                    "source_name": node["name"],
                    "reference": ref,
                    "candidate_targets": sorted(
                        item["path"] for item in by_name[ref]),
                })
            else:
                external_references.append({
                    "source": node["path"],
                    "source_name": node["name"],
                    "source_classification": node["classification"],
                    "reference": ref,
                })

    internal_edges.sort(
        key=lambda edge: (edge["source"], edge["target"], edge["target_name"]))
    external_references.sort(
        key=lambda item: (item["source"], item["reference"]))
    ambiguous_references.sort(
        key=lambda item: (item["source"], item["reference"]))

    production_graph = {
        path: sorted(
            target for target in all_graph[path] if target in production_paths)
        for path in sorted(production_paths)
    }
    all_cycles = find_cycle_components(all_graph)
    production_cycles = find_cycle_components(production_graph)

    graph_material = {
        "nodes": [
            {
                "path": node["path"],
                "name": node["name"],
                "references": node["references"],
            }
            for node in nodes
        ],
        "internal_edges": internal_edges,
        "external_references": external_references,
        "ambiguous_references": ambiguous_references,
    }
    classification_material = {
        "ordered_tiers": [
            {"tier": tier, "name": name}
            for tier, name in ordered_rows
        ],
        "folder_classifications": [
            {
                "folder": folder,
                "kind": "out-of-band" if tier == "infra" else "tier",
                "tier": None if tier == "infra" else tier,
            }
            for folder, tier in sorted(tier_of.items())
        ],
        "bound_tier_sequence": list(bound_sequence),
        "bound_infrastructure": list(bound_infra),
        "table_infrastructure": list(infra_folders),
    }
    subject_material = {
        "graph": graph_material,
        "classification": classification_material,
    }

    production_unknown = [
        item for item in external_references
        if item["source_classification"] in ("production", "out-of-band")
    ]

    summary = dict(stats)
    summary.update({
        "asmdef_count": len(records),
        "classification_counts": classification_counts,
        "internal_edge_count": len(internal_edges),
        "external_reference_count": len(external_references),
        "production_unknown_reference_count": len(production_unknown),
        "ambiguous_reference_count": len(ambiguous_references),
        "duplicate_name_count": len(duplicate_names),
        "all_cycle_component_count": len(all_cycles),
        "production_cycle_component_count": len(production_cycles),
        "policy_failure_count": len(failures),
    })

    return {
        "report_version": 1,
        "mode": "enforcing-check-with-machine-report",
        "status": "fail" if failures else "pass",
        "source_universe": "src/**/*.asmdef below src/<folder>/",
        "classification_authority": {
            "section_2": SECTION2_PATH.as_posix(),
            "section_3": SPEC_PATH.as_posix(),
            "section": "3.5.2",
        },
        "digests": {
            "graph_sha256": _digest(graph_material),
            "classification_sha256": _digest(classification_material),
            "subject_sha256": _digest(subject_material),
        },
        "summary": summary,
        "assemblies": nodes,
        "internal_edges": internal_edges,
        "external_references": external_references,
        "production_unknown_references": production_unknown,
        "ambiguous_references": ambiguous_references,
        "duplicate_names": duplicate_names,
        "cycles": {
            "all": all_cycles,
            "production": production_cycles,
        },
        "policy_failures": list(failures),
    }


def analyze(repo):
    """Run the existing policy checks and return a machine-readable report."""
    spec_file = repo / SPEC_PATH
    if not spec_file.is_file():
        raise RuntimeError(
            "%s not found (is --repo the repository root?)" % spec_file)
    section2_file = repo / SECTION2_PATH
    if not section2_file.is_file():
        raise RuntimeError(
            "%s not found (is --repo the repository root?)" % section2_file)

    section2_text = section2_file.read_text(encoding="utf-8")
    tier_of, ordered_rows, infra_folders, parse_errors = parse_tier_table(
        spec_file.read_text(encoding="utf-8"))
    bound_infra, infra_err = parse_infra_binding(section2_text)
    if infra_err:
        raise RuntimeError(infra_err)
    bound_sequence, seq_err = parse_tier_sequence(section2_text)
    if seq_err:
        raise RuntimeError(seq_err)

    records, asmdef_parse_errors = load_asmdefs(repo)
    folder_of_name, name_of_folder, refs, test_names, view_errors = (
        load_production_asmdefs(records))

    failures = (
        list(parse_errors) + list(asmdef_parse_errors) + list(view_errors))
    if not name_of_folder:
        failures.append("no production .asmdef found under %s" % (repo / "src"))

    expected = list(enumerate(bound_sequence))
    if ordered_rows != expected:
        failures.append(
            "the §3.5.2 numbered tier rows do not match FR-CS-046's stated "
            "ten-tier sequence: table has [%s] but FR-CS-046 (%s) requires "
            "[%s] — numbering must be 0..9, contiguous, ascending in table "
            "order, with matching names"
            % (", ".join("%s %s" % (t, n) for t, n in ordered_rows),
               SECTION2_PATH,
               ", ".join("%s %s" % (t, n) for t, n in expected)))

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

    for folder in sorted(name_of_folder):
        if folder not in tier_of:
            failures.append(
                "production folder 'src/%s/' is ABSENT from the §3.5.2 tier "
                "table (the placement rule requires the same commit that adds "
                "the .asmdef to seat it)" % folder)

    for folder in sorted(tier_of):
        if folder not in name_of_folder:
            failures.append(
                "§3.5.2 tier table names '%s' but src/%s/ holds no production "
                ".asmdef" % (folder, folder))

    downward = intra = upward = infra_sourced = 0
    total = 0
    for name in sorted(refs):
        src_folder = folder_of_name[name]
        src_tier = tier_of.get(src_folder)
        for ref in refs[name]:
            if ref in test_names:
                failures.append(
                    "production assembly 'src/%s/' references TEST assembly "
                    "'%s' — §3.5.2's exclusion is test→production only; a "
                    "production→test reference is upward out of the order "
                    "entirely" % (src_folder, ref))
                continue
            if ref not in folder_of_name:
                failures.append(
                    "production assembly 'src/%s/' references '%s', which "
                    "resolves to no production assembly under src/ — an "
                    "unresolvable reference name must fail, not be silently "
                    "ignored" % (src_folder, ref))
                continue
            total += 1
            dst_folder = folder_of_name[ref]
            dst_tier = tier_of.get(dst_folder)
            if src_tier is None or dst_tier is None:
                continue
            if src_tier == "infra":
                infra_sourced += 1
                if dst_tier != "infra" and dst_tier != 0:
                    failures.append(
                        "Infrastructure assembly 'src/%s/' references "
                        "'src/%s/' (tier %s) — FR-CS-046b: an Infrastructure "
                        "assembly MUST NOT reference any ordered-tier assembly "
                        "other than tier 0 (Foundation)"
                        % (src_folder, dst_folder, dst_tier))
                continue
            if dst_tier == "infra":
                failures.append(
                    "ordered-tier assembly 'src/%s/' references out-of-band "
                    "Infrastructure assembly 'src/%s/' — §3.5.2: no tier may "
                    "reference Infrastructure at runtime"
                    % (src_folder, dst_folder))
                continue
            if dst_tier < src_tier:
                downward += 1
            elif dst_tier == src_tier:
                intra += 1
            else:
                upward += 1
                failures.append(
                    "UPWARD reference (FR-CS-046): 'src/%s/' (tier %s) "
                    "references 'src/%s/' (tier %s)"
                    % (src_folder, src_tier, dst_folder, dst_tier))

    production_graph = {
        name: [ref for ref in values if ref in folder_of_name]
        for name, values in refs.items()
    }
    cycle = find_cycle(production_graph)
    if cycle:
        failures.append(
            "CYCLE in production reference graph (FR-CS-046a): %s"
            % " -> ".join(folder_of_name[name] for name in cycle))

    placed = sum(1 for folder in tier_of if folder in name_of_folder)
    stats = {
        "production_assembly_folder_count": len(name_of_folder),
        "placed_folder_count": placed,
        "ordered_tier_folder_count": sum(
            1 for folder in tier_of
            if folder in name_of_folder and tier_of[folder] != "infra"),
        "out_of_band_folder_count": sum(
            1 for folder in infra_folders if folder in name_of_folder),
        "production_to_production_reference_count": total,
        "downward_reference_count": downward,
        "intra_tier_reference_count": intra,
        "upward_reference_count": upward,
        "infrastructure_sourced_reference_count": infra_sourced,
        "test_assembly_count": sum(1 for record in records if record["is_test"]),
    }
    return build_machine_report(
        records, tier_of, ordered_rows, infra_folders, bound_infra,
        bound_sequence, folder_of_name, failures, stats)


def print_human_report(report):
    summary = report["summary"]
    print("assembly-tier-check: §3.5.2 table vs production .asmdef graph")
    print("  production assembly folders under src/ : %d"
          % summary["production_assembly_folder_count"])
    print("  folders placed in the tier table       : %d  (%d in ordered tiers + "
          "%d out-of-band Infrastructure)" % (
              summary["placed_folder_count"],
              summary["ordered_tier_folder_count"],
              summary["out_of_band_folder_count"]))
    print("  production->production references      : %d"
          % summary["production_to_production_reference_count"])
    print("    downward                             : %d"
          % summary["downward_reference_count"])
    print("    intra-tier                           : %d"
          % summary["intra_tier_reference_count"])
    print("    upward                               : %d"
          % summary["upward_reference_count"])
    print("    sourced by out-of-band Infrastructure: %d"
          % summary["infrastructure_sourced_reference_count"])
    print("  test assemblies enumerated (outside the order): %d"
          % summary["test_assembly_count"])

    failures = report["policy_failures"]
    if failures:
        print()
        print("FAIL — %d problem(s):" % len(failures))
        for failure in failures:
            print("  * %s" % failure)
        return
    print("PASS — every folder placed, no upward reference, graph acyclic.")


def main(argv=None):
    ap = argparse.ArgumentParser(
        description="Check the Spec #20 §3.5.2 ten-tier assembly order against "
                    "the production .asmdef reference graph.")
    ap.add_argument("--repo", default=".", help="repository root (default: .)")
    ap.add_argument(
        "--json", action="store_true",
        help="emit deterministic machine-readable graph/evidence JSON")
    args = ap.parse_args(argv)
    repo = Path(args.repo)

    try:
        report = analyze(repo)
    except (OSError, UnicodeError, RuntimeError) as exc:
        if args.json:
            print(json.dumps(
                {"report_version": 1, "status": "error", "error": str(exc)},
                ensure_ascii=False, sort_keys=True))
        else:
            print("FATAL: %s" % exc)
        return 2

    if args.json:
        print(json.dumps(
            report, ensure_ascii=False, sort_keys=True, indent=2))
    else:
        print_human_report(report)
    return 1 if report["policy_failures"] else 0


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
# | 1.3     | 2026-08-18 | Claude Code | Reviewed-findings pass (G1 + G2): the ten    |
# |         |            |             | numbered tier rows are now asserted against  |
# |         |            |             | FR-CS-046's stated sequence, parsed from     |
# |         |            |             | section-2.md — 0..9, contiguous, ascending   |
# |         |            |             | in table order, (number, name) pairs matched |
# |         |            |             | (swapping two tier numbers previously        |
# |         |            |             | PASSED); test assemblies are enumerated and  |
# |         |            |             | a production→test reference now FAILS, as    |
# |         |            |             | does any reference naming an assembly that   |
# |         |            |             | resolves to nothing under src/ (both were    |
# |         |            |             | previously "ignored as external");           |
# |         |            |             | docstring's CI claim corrected to ci.yml's   |
# |         |            |             | real triggers (pushes to main + PRs into     |
# |         |            |             | main — NOT "every push").                    |
# | 1.4     | 2026-08-18 | Claude Code | AR round 6 (M6): a DUPLICATE assembly `name` |
# |         |            |             | across two production .asmdef files now      |
# |         |            |             | FAILS. Folder collisions were already        |
# |         |            |             | caught; name collisions were not, and names  |
# |         |            |             | are the graph's node keys — the second       |
# |         |            |             | declaration silently REPLACED the first's    |
# |         |            |             | reference list, dropping a whole assembly's  |
# |         |            |             | edges from the tier and cycle checks while   |
# |         |            |             | the folder count still read complete.        |
# |         |            |             | Mutation-proven: naming ball-physics         |
# |         |            |             | TacticalDirector.AgentMovement now reports   |
# |         |            |             | the collision by name as the first error     |
# |         |            |             | (previously it surfaced only indirectly, as  |
# |         |            |             | downstream unresolvable references — and a   |
# |         |            |             | duplicate naming an unreferenced assembly    |
# |         |            |             | would have PASSED silently).                 |
# | 1.5     | 2026-08-29 | ChatGPT     | A1 consolidation: retain this file as |
# |         |            |             | the single §3.5.2 parser/checker; add |
# |         |            |             | --json complete-graph evidence, graph/ |
# |         |            |             | classification/subject digests, all-  |
# |         |            |             | assembly cycle reporting, and shared  |
# |         |            |             | one-pass asmdef parsing. Verdict-      |
# |         |            |             | semantics change: a stray src/*.asmdef |
# |         |            |             | with no top-level folder was skipped   |
# |         |            |             | by v1.4; v1.5 reports it unresolved    |
# |         |            |             | and fails because §3.5.2 is folder-    |
# |         |            |             | keyed. Current tree is unaffected.     |
