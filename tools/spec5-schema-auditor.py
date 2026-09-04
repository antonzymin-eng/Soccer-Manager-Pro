#!/usr/bin/env python3
"""Per-spec section-5 schema-conformance auditor (FR-TS-046..052)."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from testing_strategy_audit import Finding, describe, infer_spec_id, is_legacy_survey


REQUIREMENTS: tuple[tuple[str, tuple[str, ...]], ...] = (
    ("taxonomy/test-count", ("unit", "integration", "simulation")),
    ("property-test list", ("property",)),
    ("scenario list", ("scenario",)),
    ("coverage targets", ("coverage", "tier")),
    ("determinism classification", ("determin",)),
    ("approval-checklist linkage", ("approval",)),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--json", action="store_true")
    return parser.parse_args()


def section5_files(spec_dir: Path) -> list[Path]:
    return sorted(p for p in spec_dir.glob("section-5*.md") if p.is_file())


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    findings: list[Finding] = []
    checked_specs = 0

    for spec_dir in sorted(p for p in root.iterdir() if p.is_dir()):
        files = section5_files(spec_dir)
        if not files:
            continue
        text = "\n".join(p.read_text(encoding="utf-8") for p in files)
        spec_id = infer_spec_id(text)
        if spec_id is None:
            continue
        checked_specs += 1
        lower = text.lower()
        legacy = is_legacy_survey(spec_id)

        for label, needles in REQUIREMENTS:
            if not all(needle in lower for needle in needles):
                findings.append(
                    Finding(
                        spec_id,
                        ",".join(str(p) for p in files),
                        "§5",
                        f"missing {label} surface ({', '.join(needles)})",
                        not legacy,
                    )
                )

        manifest_tokens = re.findall(r"`(tests/scenarios/[^`]+)`", text)
        for token in manifest_tokens:
            if any(marker in token for marker in ("<", ">", "*")):
                continue
            if not (args.repo_root.resolve() / token).exists():
                findings.append(
                    Finding(
                        spec_id,
                        str(spec_dir),
                        "§5 scenario",
                        f"scenario manifest path does not resolve: {token}",
                        not legacy,
                    )
                )

    payload = {
        "auditor": "spec5-schema-auditor",
        "specs": checked_specs,
        "blocking": sum(f.blocking for f in findings),
        "notes": sum(not f.blocking for f in findings),
        "findings": [f.__dict__ for f in findings],
    }
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print(f"spec5-schema-auditor: {checked_specs} spec(s), {describe(findings)}")
        for finding in findings:
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: spec #{finding.spec_id}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
