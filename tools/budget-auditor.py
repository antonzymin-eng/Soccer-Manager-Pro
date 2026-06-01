#!/usr/bin/env python3
"""
File:    tools/budget-auditor.py
Spec:    Performance Optimization Strategy #18 §5.3, §5.5, Appendix E, FR-PO-070
Purpose: Stage 0 schema-conformance and loop-tag auditor for docs/specs/ §6 sections.

    --mode schema    Walk every approved spec's §6 file against the Appendix B template.
                     Reports missing sections as ERR-018-NNN candidates.
    --mode loop-tag  Regex pass over every §6 budget table for [LOOP-...] tags.
                     Reports budget numbers lacking a loop-tag as §3.2.2 failures.
    --mode all       Run both modes in sequence (default when --mode is omitted).

Stage 0+1: this script is invoked by tools/run-perf-local.sh and by CI
           (tools/perf-harness/run.sh) per FR-PO-070.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# ── Appendix B required §6 sections ─────────────────────────────────────────
REQUIRED_SECTIONS = [
    "### 6.1",  # Total Per-Tick Budget
    "### 6.2",  # Per-Tick Budget Breakdown
    "### 6.3",  # Worst-Case Input Parameters
    "### 6.4",  # Headroom Multiplier
    "### 6.5",  # Cross-Spec Budget Consumption (optional if no cross-spec budget)
    "### 6.6",  # Version History
]

# §6.5 is listed as required in Appendix B but only when cross-spec budgets exist.
# The auditor flags its absence as an advisory (not a hard failure) per §3.1.2.
ADVISORY_SECTIONS = {"### 6.5"}

# ── §3.2.2 loop-tag strings ──────────────────────────────────────────────────
LOOP_TAG_TACTICAL = "LOOP-TACTICAL-10HZ"
LOOP_TAG_PHYSICS  = "LOOP-PHYSICS-60HZ"
LOOP_TAG_PATTERN  = re.compile(
    r"\[LOOP-(?:TACTICAL-10HZ|PHYSICS-60HZ)\]"
)

# Budget table row: pipe-delimited with a numeric ms value.
# Captures rows like: | SomeRoutine | [LOOP-…] | 0.05 ms | 0 | [GT] |
# Also catches rows missing the loop-tag (which is the §3.2.2 violation).
BUDGET_ROW_PATTERN = re.compile(
    r"^\|\s*(?P<subroutine>[^|]+?)\s*\|"      # subroutine column
    r"(?P<rest>[^|]*(?:\|[^|]*)*)\|\s*$",      # remaining columns
    re.MULTILINE,
)
MS_VALUE_PATTERN = re.compile(r"\b\d+(?:\.\d+)?\s*ms\b", re.IGNORECASE)

# Spec folders that are approved and have §6 sections to audit.
# Updated when SPEC_INDEX.md changes (§3.1.3 roll-up is populated by this list).
APPROVED_SPEC_FOLDERS = [
    "ball-physics",
    "agent-movement",
    "collision-system",
    "first-touch",
    "pass-mechanics",
    "shot-mechanics",
    "perception-system",
    "decision-tree",
    "heading-mechanics",
    "goalkeeper-mechanics",
    "positioning-ai",
    "pressing-ai",
    "defensive-ai",
    "attacking-ai",
    "deterministic-sim",
    "event-system",
    "performance-optimization",
]

# Candidate §6 filenames per spec (Shot Mechanics uses section-4.md §4.5).
SECTION6_CANDIDATES = ["section-6.md", "section-4.md"]


def find_section6_file(spec_dir: Path) -> Path | None:
    """Return the first candidate §6 file that exists under spec_dir."""
    for candidate in SECTION6_CANDIDATES:
        path = spec_dir / candidate
        if path.exists():
            return path
    return None


def audit_schema(docs_root: Path) -> list[str]:
    """
    §5.3 schema-conformance auditor.
    Returns a list of finding strings; empty list means all specs pass.
    """
    findings: list[str] = []

    for folder in APPROVED_SPEC_FOLDERS:
        spec_dir = docs_root / folder
        if not spec_dir.is_dir():
            findings.append(
                f"[MISSING-DIR] {folder}: spec directory not found under {docs_root}"
            )
            continue

        section6 = find_section6_file(spec_dir)
        if section6 is None:
            findings.append(
                f"[MISSING-§6]  {folder}: no section-6.md (or section-4.md) found"
            )
            continue

        text = section6.read_text(encoding="utf-8")
        citation = f"{folder}/{section6.name}"

        for required in REQUIRED_SECTIONS:
            if required not in text:
                severity = "ADVISORY" if required in ADVISORY_SECTIONS else "FAIL"
                findings.append(
                    f"[{severity}]    {citation}: missing '{required}' "
                    f"(Appendix B schema)"
                )

    return findings


def audit_loop_tags(docs_root: Path) -> list[str]:
    """
    §5.5 loop-tag auditor.
    Checks every budget table row with a numeric ms value for a [LOOP-…] tag.
    Returns a list of finding strings.
    """
    findings: list[str] = []

    for folder in APPROVED_SPEC_FOLDERS:
        spec_dir = docs_root / folder
        if not spec_dir.is_dir():
            continue

        section6 = find_section6_file(spec_dir)
        if section6 is None:
            continue

        text   = section6.read_text(encoding="utf-8")
        citation = f"{folder}/{section6.name}"
        lines  = text.splitlines()

        # Scan every line that contains a numeric ms value for the loop-tag.
        for lineno, line in enumerate(lines, start=1):
            if not MS_VALUE_PATTERN.search(line):
                continue
            if LOOP_TAG_PATTERN.search(line):
                continue
            # Line has a numeric ms value but no loop-tag → §3.2.2 violation.
            findings.append(
                f"[LOOP-TAG]    {citation}:{lineno}: ms value without loop-tag: "
                f"{line.strip()[:120]}"
            )

    return findings


def print_report(mode: str, findings: list[str], *, label: str) -> bool:
    """Print findings and return True if any hard FAIL was found."""
    print(f"  {label}: {len(findings)} finding(s)")
    has_fail = False
    for f in findings:
        print(f"    {f}")
        if f.startswith("[FAIL]") or f.startswith("[MISSING") or f.startswith("[LOOP-TAG]"):
            has_fail = True
    if not findings:
        print("    (none — all checks passed)")
    return has_fail


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Spec #18 §5.3/§5.5 budget auditor"
    )
    parser.add_argument(
        "--mode",
        choices=["schema", "loop-tag", "all"],
        default="all",
        help="Audit mode: schema | loop-tag | all (default: all)",
    )
    parser.add_argument(
        "--docs",
        default="docs/specs",
        help="Path to the docs/specs/ root (default: docs/specs)",
    )
    args = parser.parse_args()

    docs_root = Path(args.docs)
    if not docs_root.is_dir():
        print(f"ERROR: docs root not found: {docs_root}", file=sys.stderr)
        return 2

    exit_code = 0

    if args.mode in ("schema", "all"):
        schema_findings = audit_schema(docs_root)
        has_fail = print_report("schema", schema_findings, label="Schema-conformance (Appendix B)")
        if has_fail:
            exit_code = 1

    if args.mode in ("loop-tag", "all"):
        loop_findings = audit_loop_tags(docs_root)
        has_fail = print_report("loop-tag", loop_findings, label="Loop-tag audit (§3.2.2)")
        if has_fail:
            exit_code = 1

    if exit_code != 0:
        print("")
        print("RESULT: FAIL — file ERR-018-NNN rows in docs/tracking/spec-error-log.md")
        print("        for each finding before marking the PR review-ready.")
    else:
        print("")
        print("RESULT: PASS")

    return exit_code


if __name__ == "__main__":
    sys.exit(main())
