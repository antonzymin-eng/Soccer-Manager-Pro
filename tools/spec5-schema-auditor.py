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
    parser.add_argument(
        "--changed-scope",
        action="store_true",
        help="Survey the whole root but block only non-legacy spec directories named by --enforce-dir.",
    )
    parser.add_argument(
        "--enforce-dir",
        action="append",
        default=[],
        help="Spec directory, absolute or repo-relative, currently under review. Repeatable.",
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


def structured_lines(text: str) -> list[str]:
    """Return headings/list/table lines that can carry an explicit schema surface.

    A prose paragraph that happens to mention all required words is not a §5 schema.
    """
    out: list[str] = []
    for raw in text.splitlines():
        stripped = raw.strip()
        if not stripped:
            continue
        if (
            stripped.startswith("#")
            or stripped.startswith("|")
            or stripped.startswith("- ")
            or stripped.startswith("* ")
            or re.match(r"^\d+[.)]\s+", stripped)
        ):
            out.append(stripped.lower())
    return out


def has_surface(lines: list[str], needles: tuple[str, ...]) -> bool:
    return any(all(needle in line for needle in needles) for line in lines)


def spec_dir_enforced(spec_dir: Path, *, changed_scope: bool, enforce_dirs: set[Path]) -> bool:
    if not changed_scope:
        return True
    resolved = spec_dir.resolve()
    return any(resolved == root or root in resolved.parents for root in enforce_dirs)


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    repo_root = args.repo_root.resolve()
    enforce_dirs = normalize_dirs(args.enforce_dir, repo_root)
    findings: list[Finding] = []
    checked_specs = 0

    for spec_dir in sorted(p for p in root.iterdir() if p.is_dir()):
        files = section5_files(spec_dir)
        if not files:
            continue
        text = "\n".join(p.read_text(encoding="utf-8") for p in files)
        spec_id = infer_spec_id(text)
        enforced = spec_dir_enforced(
            spec_dir,
            changed_scope=args.changed_scope,
            enforce_dirs=enforce_dirs,
        )
        if spec_id is None:
            findings.append(
                Finding(
                    None,
                    ",".join(str(p) for p in files),
                    "§5",
                    "cannot infer spec ID from section-5 header",
                    enforced,
                )
            )
            continue
        checked_specs += 1
        blocking = enforced and not is_legacy_survey(spec_id)
        surfaces = structured_lines(text)

        for label, needles in REQUIREMENTS:
            if not has_surface(surfaces, needles):
                findings.append(
                    Finding(
                        spec_id,
                        ",".join(str(p) for p in files),
                        "§5",
                        f"missing structured {label} surface ({', '.join(needles)})",
                        blocking,
                    )
                )

        manifest_tokens = re.findall(r"`(tests/scenarios/[^`]+)`", text)
        for token in manifest_tokens:
            if any(marker in token for marker in ("<", ">", "*")):
                continue
            if not (repo_root / token).exists():
                findings.append(
                    Finding(
                        spec_id,
                        str(spec_dir),
                        "§5 scenario",
                        f"scenario manifest path does not resolve: {token}",
                        blocking,
                    )
                )

    payload = {
        "auditor": "spec5-schema-auditor",
        "specs": checked_specs,
        "blocking": sum(f.blocking for f in findings),
        "notes": sum(not f.blocking for f in findings),
        "changed_scope": args.changed_scope,
        "enforced_dirs": sorted(str(path) for path in enforce_dirs),
        "findings": [f.__dict__ for f in findings],
    }
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        scope = "changed-spec enforcement" if args.changed_scope else "full enforcement"
        print(
            f"spec5-schema-auditor: {checked_specs} spec(s), {describe(findings)} [{scope}]"
        )
        for finding in findings:
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: spec #{finding.spec_id}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
