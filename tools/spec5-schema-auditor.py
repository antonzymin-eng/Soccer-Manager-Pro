#!/usr/bin/env python3
"""Per-spec section-5 schema-conformance auditor (FR-TS-046..052).

The authority is Testing Strategy Appendix C. A heading or a bullet containing
words such as "property" or "coverage tier" is not a schema. The approval gate
requires the table shapes and payloads Appendix C actually publishes. An
explicitly enforced approval directory with no section-5 file is itself a
blocking schema finding rather than an empty successful audit.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from testing_strategy_audit import (
    Finding,
    canonical_spec_status,
    describe,
    infer_spec_id,
    is_approved_status,
    is_legacy_survey,
    iter_tables,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--json", action="store_true")
    parser.add_argument(
        "--survey-only",
        action="store_true",
        help="Report findings but never return a blocking verdict. Routine pre-commit/PR/nightly pipelines use this mode; approval transitions do not.",
    )
    parser.add_argument(
        "--changed-scope",
        action="store_true",
        help="Survey the whole root but consider blocking only spec directories named by --enforce-dir.",
    )
    parser.add_argument(
        "--enforce-dir",
        action="append",
        default=[],
        help="Spec directory, absolute or repo-relative, currently at an approval transition. Repeatable.",
    )
    parser.add_argument(
        "--quiet-survey",
        action="store_true",
        help="Print blocking findings plus counts, but omit individual survey findings.",
    )
    return parser.parse_args()


def normalize_dirs(values: list[str], repo_root: Path) -> set[Path]:
    out: set[Path] = set()
    for value in values:
        path = Path(value)
        if not path.is_absolute():
            path = repo_root / path
        out.add(path.resolve())
    return out


def section5_files(spec_dir: Path) -> list[Path]:
    return sorted(p for p in spec_dir.glob("section-5*.md") if p.is_file())


def fallback_spec_text(spec_dir: Path) -> str:
    """Read enough local spec text to infer ID/status when section 5 is absent."""
    chunks: list[str] = []
    for path in sorted(spec_dir.glob("section-*.md")):
        try:
            chunks.append(path.read_text(encoding="utf-8", errors="replace"))
        except OSError:
            continue
        if infer_spec_id("\n".join(chunks)) is not None:
            break
    return "\n".join(chunks)


def spec_dir_enforced(
    spec_dir: Path,
    *,
    survey_only: bool,
    changed_scope: bool,
    enforce_dirs: set[Path],
) -> bool:
    if survey_only:
        return False
    if not changed_scope:
        return True
    resolved = spec_dir.resolve()
    return any(resolved == root or root in resolved.parents for root in enforce_dirs)


def section_block(text: str, subsection: str) -> str | None:
    """Return the `## 5.N ...` block, stopping at the next H2."""
    pattern = re.compile(
        rf"^##\s+{re.escape(subsection)}(?:\s|\b).*?$\n(?P<body>.*?)(?=^##\s+5\.\d+(?:\s|\b)|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    match = pattern.search(text)
    return match.group("body") if match else None


def table_matching(block: str, required_headers: tuple[str, ...]) -> tuple[list[str], list[list[str]]] | None:
    for headers, rows in iter_tables(block.splitlines()):
        lowered = [h.lower().strip() for h in headers]
        if all(any(req in header for header in lowered) for req in required_headers):
            return headers, [cells for _, cells in rows]
    return None


def column_index(headers: list[str], needle: str) -> int | None:
    needle = needle.lower()
    for i, header in enumerate(headers):
        if needle in header.lower():
            return i
    return None


def clean(cell: str) -> str:
    return cell.strip().strip("`").strip()


def placeholder(cell: str) -> bool:
    value = clean(cell).lower()
    return (
        not value
        or "<" in value
        or ">" in value
        or value in {"tbd", "todo", "n/a?", "?"}
    )


def int_cell(cell: str) -> bool:
    return bool(re.fullmatch(r"\s*\d+\s*", clean(cell)))


def int_or_dash_cell(cell: str) -> bool:
    return int_cell(cell) or clean(cell) == "—"


def tier_cell(cell: str) -> bool:
    return clean(cell).upper() in {"A", "B", "C"}


def percent_at_least(cell: str, expected: int) -> bool:
    value = clean(cell).replace(" ", "")
    match = re.search(r"(\d+(?:\.\d+)?)%", value)
    return bool(match and float(match.group(1)) >= expected)


def taxonomy_row(rows: list[list[str]], layer_i: int, prefix: str) -> list[str] | None:
    prefix = prefix.lower()
    return next(
        (row for row in rows if len(row) > layer_i and clean(row[layer_i]).lower().startswith(prefix)),
        None,
    )


def validate_taxonomy(block: str | None) -> list[str]:
    if block is None:
        return ["missing §5.1 Test Count by Taxonomy Layer subsection"]
    table = table_matching(block, ("layer", "count"))
    if table is None:
        return ["§5.1 must contain Appendix-C Layer/Count table"]
    headers, rows = table
    layer_i, count_i = column_index(headers, "layer"), column_index(headers, "count")
    assert layer_i is not None and count_i is not None
    errors: list[str] = []

    # Appendix C has FIVE required taxonomy rows. The determinism count is the
    # one row that permits an em dash because the suite is consumed from #16.
    required = (
        ("unit", "Unit", int_cell, "an integer"),
        ("integration", "Integration", int_cell, "an integer"),
        ("simulation", "Simulation", int_cell, "an integer"),
        ("determinism", "Determinism (consumed from #16 §5)", int_or_dash_cell, "an integer or —"),
        ("end-to-end / soak", "End-to-end / soak", int_cell, "an integer"),
    )
    for prefix, display, validator, payload in required:
        row = taxonomy_row(rows, layer_i, prefix)
        if row is None:
            errors.append(f"§5.1 missing {display} row")
        elif len(row) <= count_i or not validator(row[count_i]):
            errors.append(f"§5.1 {display} Count must be {payload}")
    return errors


def validate_properties(block: str | None) -> list[str]:
    if block is None:
        return ["missing §5.2 Property Test List subsection"]
    table = table_matching(block, ("property", "tier", "owning"))
    if table is None:
        return ["§5.2 must contain Appendix-C Property/Tier/Owning Module table"]
    headers, rows = table
    pi, ti, oi = column_index(headers, "property"), column_index(headers, "tier"), column_index(headers, "owning")
    assert pi is not None and ti is not None and oi is not None
    valid = [r for r in rows if len(r) > max(pi, ti, oi) and not placeholder(r[pi]) and tier_cell(r[ti]) and not placeholder(r[oi])]
    return [] if valid else ["§5.2 has no concrete property row with Property, Tier A/B/C, and Owning Module"]


def validate_scenarios(block: str | None, repo_root: Path) -> list[str]:
    if block is None:
        return ["missing §5.3 Scenario List subsection"]
    table = table_matching(block, ("scenario", "manifest", "tier"))
    if table is None:
        return ["§5.3 must contain Appendix-C Scenario/Manifest Path/Tier table"]
    headers, rows = table
    si, mi, ti = column_index(headers, "scenario"), column_index(headers, "manifest"), column_index(headers, "tier")
    assert si is not None and mi is not None and ti is not None
    errors: list[str] = []
    concrete = 0
    for row in rows:
        if len(row) <= max(si, mi, ti) or placeholder(row[si]) or placeholder(row[mi]) or not tier_cell(row[ti]):
            continue
        manifest = clean(row[mi])
        if not manifest.startswith("tests/scenarios/"):
            errors.append(f"§5.3 manifest path must live under tests/scenarios/: {manifest}")
            continue
        concrete += 1
        if not (repo_root / manifest).exists():
            errors.append(f"§5.3 scenario manifest path does not resolve: {manifest}")
    if concrete == 0:
        errors.insert(0, "§5.3 has no concrete scenario row with manifest path and Tier A/B/C")
    return errors


def validate_coverage(block: str | None) -> list[str]:
    if block is None:
        return ["missing §5.4 Coverage Targets subsection"]
    table = table_matching(block, ("tier", "line", "branch"))
    if table is None:
        return ["§5.4 must contain Appendix-C Tier/Line/Branch coverage table"]
    headers, rows = table
    ti, li, bi = column_index(headers, "tier"), column_index(headers, "line"), column_index(headers, "branch")
    assert ti is not None and li is not None and bi is not None
    by_tier = {clean(r[ti]).upper(): r for r in rows if len(r) > max(ti, li, bi)}
    errors: list[str] = []
    required = {"A": (98, 95), "B": (90, 80)}
    for tier, (line_min, branch_min) in required.items():
        row = by_tier.get(tier)
        if row is None:
            errors.append(f"§5.4 missing Tier {tier} row")
        else:
            if not percent_at_least(row[li], line_min):
                errors.append(f"§5.4 Tier {tier} line target must be at least {line_min}%")
            if not percent_at_least(row[bi], branch_min):
                errors.append(f"§5.4 Tier {tier} branch target must be at least {branch_min}%")
    row_c = by_tier.get("C")
    if row_c is None:
        errors.append("§5.4 missing Tier C row")
    elif "lint" not in clean(row_c[li]).lower():
        errors.append("§5.4 Tier C line policy must be lint-only")
    return errors


def validate_determinism(block: str | None) -> list[str]:
    if block is None:
        return ["missing §5.5 Determinism-Tier Classification subsection"]
    table = table_matching(block, ("field", "tier", "source"))
    if table is None:
        return ["§5.5 must contain Appendix-C Field/Tier/Source table"]
    headers, rows = table
    fi, ti, si = column_index(headers, "field"), column_index(headers, "tier"), column_index(headers, "source")
    assert fi is not None and ti is not None and si is not None
    valid = [
        r for r in rows
        if len(r) > max(fi, ti, si)
        and not placeholder(r[fi])
        and tier_cell(r[ti])
        and "#16" in r[si]
        and "§1.1.1" in r[si]
    ]
    return [] if valid else ["§5.5 has no concrete authoritative Field row with Tier A/B/C and #16 §1.1.1 source"]


def validate_approval_links(block: str | None) -> list[str]:
    if block is None:
        return ["missing §5.6 Approval-Checklist Linkage subsection"]
    table = table_matching(block, ("test id", "verifies"))
    if table is None:
        return ["§5.6 must contain Appendix-C Test ID / Verifies §9 Row table"]
    headers, rows = table
    ti, vi = column_index(headers, "test id"), column_index(headers, "verifies")
    assert ti is not None and vi is not None
    valid = [
        r for r in rows
        if len(r) > max(ti, vi)
        and not placeholder(r[ti])
        and re.search(r"§\s*9\.\d+(?:\.\d+)?", r[vi])
    ]
    return [] if valid else ["§5.6 has no concrete Test ID linked to a §9 checklist row"]


def schema_errors(text: str, repo_root: Path) -> list[str]:
    errors: list[str] = []
    errors.extend(validate_taxonomy(section_block(text, "5.1")))
    errors.extend(validate_properties(section_block(text, "5.2")))
    errors.extend(validate_scenarios(section_block(text, "5.3"), repo_root))
    errors.extend(validate_coverage(section_block(text, "5.4")))
    errors.extend(validate_determinism(section_block(text, "5.5")))
    errors.extend(validate_approval_links(section_block(text, "5.6")))
    if section_block(text, "5.7") is None:
        errors.append("missing §5.7 Version History subsection")
    return errors


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    repo_root = args.repo_root.resolve()
    enforce_dirs = normalize_dirs(args.enforce_dir, repo_root)
    findings: list[Finding] = []
    checked_specs = 0

    for spec_dir in sorted(p for p in root.iterdir() if p.is_dir()):
        files = section5_files(spec_dir)
        fallback_text = "" if files else fallback_spec_text(spec_dir)
        text = "\n".join(p.read_text(encoding="utf-8") for p in files) if files else fallback_text
        spec_id = infer_spec_id(text)
        status = canonical_spec_status(repo_root, spec_dir, fallback_text=text)
        enforced = spec_dir_enforced(
            spec_dir,
            survey_only=args.survey_only,
            changed_scope=args.changed_scope,
            enforce_dirs=enforce_dirs,
        )

        if not files:
            if not fallback_text and not enforced:
                continue
            if spec_id is not None:
                checked_specs += 1
            blocking = enforced and not is_legacy_survey(spec_id) and is_approved_status(status)
            findings.append(
                Finding(
                    spec_id,
                    str(spec_dir),
                    "§5",
                    "missing required section-5 test-plan file",
                    blocking,
                )
            )
            continue

        if spec_id is None:
            findings.append(
                Finding(
                    None,
                    ",".join(str(p) for p in files),
                    "§5",
                    "cannot infer spec ID from section-5 header",
                    enforced and is_approved_status(status),
                )
            )
            continue

        checked_specs += 1
        blocking = enforced and not is_legacy_survey(spec_id) and is_approved_status(status)
        for message in schema_errors(text, repo_root):
            findings.append(
                Finding(
                    spec_id,
                    ",".join(str(p) for p in files),
                    "§5",
                    message,
                    blocking,
                )
            )

    # Fail closed if --enforce-dir points to a registry-approved directory that
    # does not exist beneath the audited root at all.
    if not args.survey_only:
        existing_dirs = {p.resolve() for p in root.iterdir() if p.is_dir()}
        for enforced_dir in sorted(enforce_dirs - existing_dirs):
            findings.append(
                Finding(
                    None,
                    str(enforced_dir),
                    "§5",
                    "enforced approval spec directory does not exist",
                    True,
                )
            )

    payload = {
        "auditor": "spec5-schema-auditor",
        "specs": checked_specs,
        "blocking": sum(f.blocking for f in findings),
        "notes": sum(not f.blocking for f in findings),
        "survey_only": args.survey_only,
        "changed_scope": args.changed_scope,
        "enforced_dirs": sorted(str(path) for path in enforce_dirs),
        "findings": [f.__dict__ for f in findings],
    }
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        if args.survey_only:
            scope = "survey-only routine pipeline"
        elif args.changed_scope:
            scope = "approval enforcement (explicit scope)"
        else:
            scope = "approval-state enforcement"
        print(f"spec5-schema-auditor: {checked_specs} spec(s), {describe(findings)} [{scope}]")
        for finding in findings:
            if args.quiet_survey and not finding.blocking:
                continue
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: spec #{finding.spec_id}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
