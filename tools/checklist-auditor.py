#!/usr/bin/env python3
"""Automated approval-checklist evidence auditor (FR-TS-040..045)."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from testing_strategy_audit import (
    Finding,
    candidate_paths,
    describe,
    has_named_programmatic_check,
    has_resolved_local_section_reference,
    infer_spec_id,
    infer_status,
    is_approved_status,
    is_legacy_survey,
    iter_tables,
    resolve_candidate,
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
        help="Spec directory, absolute or repo-relative, that is currently at an approval transition. Repeatable.",
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


def is_enforced(
    path: Path,
    *,
    survey_only: bool,
    changed_scope: bool,
    enforce_dirs: set[Path],
) -> bool:
    if survey_only:
        return False
    if not changed_scope:
        return True
    resolved = path.resolve()
    return any(resolved == root or root in resolved.parents for root in enforce_dirs)


def audit_file(
    path: Path,
    repo_root: Path,
    *,
    survey_only: bool,
    changed_scope: bool,
    enforce_dirs: set[Path],
) -> tuple[int | None, int, list[Finding]]:
    text = path.read_text(encoding="utf-8")
    spec_id = infer_spec_id(text)
    status = infer_status(text)
    lines = text.splitlines()
    findings: list[Finding] = []
    checked = 0
    blocking_scope = is_enforced(
        path,
        survey_only=survey_only,
        changed_scope=changed_scope,
        enforce_dirs=enforce_dirs,
    )

    # FR-TS-042 blocks an approval transition, not an unrelated repository PR.
    # Routine pipelines therefore use --survey-only. A deliberate approval walk
    # omits that flag and may optionally narrow enforcement with --changed-scope
    # plus one or more --enforce-dir values. Legacy #1-#8 retain KD-4 survey-only
    # treatment even during such a walk.
    def blocks() -> bool:
        return (
            blocking_scope
            and not is_legacy_survey(spec_id)
            and is_approved_status(status)
        )

    for headers, rows in iter_tables(lines):
        evidence_indexes = [
            i for i, header in enumerate(headers)
            if "evidence" in header.lower() or "verification" in header.lower()
        ]
        if not evidence_indexes:
            continue
        idx = next(
            (i for i in evidence_indexes if "evidence" in headers[i].lower()),
            evidence_indexes[0],
        )
        for line_no, cells in rows:
            evidence = cells[idx].strip()
            row_id = cells[0].strip() if cells else f"line {line_no}"
            checked += 1

            if not evidence:
                findings.append(Finding(spec_id, str(path), row_id, "empty evidence cell", blocks()))
                continue

            if any(marker in evidence.lower() for marker in ("<file", "<check", "<path", "<test")):
                findings.append(
                    Finding(
                        spec_id,
                        str(path),
                        row_id,
                        "placeholder evidence token is not executable/resolved evidence",
                        blocks(),
                    )
                )
                continue

            paths = candidate_paths(evidence)
            resolved_paths = [
                token for token in paths
                if resolve_candidate(token, repo_root=repo_root, spec_dir=path.parent)
            ]
            broken_paths = [
                token for token in paths
                if not resolve_candidate(token, repo_root=repo_root, spec_dir=path.parent)
                and not any(ch in token for ch in "<>*{}")
            ]
            named_check = has_named_programmatic_check(
                evidence, repo_root=repo_root, spec_dir=path.parent
            )
            local_section = has_resolved_local_section_reference(
                evidence, spec_dir=path.parent
            )

            if broken_paths and not resolved_paths and not named_check and not local_section:
                findings.append(
                    Finding(
                        spec_id,
                        str(path),
                        row_id,
                        "unresolved evidence path(s): " + ", ".join(broken_paths),
                        blocks(),
                    )
                )
            elif not resolved_paths and not named_check and not local_section:
                findings.append(
                    Finding(
                        spec_id,
                        str(path),
                        row_id,
                        "evidence is prose only; no resolved version-controlled path, section citation, or explicit programmatic command",
                        blocks(),
                    )
                )

    return spec_id, checked, findings


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    repo_root = args.repo_root.resolve()
    enforce_dirs = normalize_dirs(args.enforce_dir, repo_root)
    candidates = sorted({*root.rglob("section-9*.md"), *root.rglob("*approval-checklist*.md")})

    total_rows = 0
    findings: list[Finding] = []
    audited_files = 0
    for path in candidates:
        _, checked, file_findings = audit_file(
            path,
            repo_root,
            survey_only=args.survey_only,
            changed_scope=args.changed_scope,
            enforce_dirs=enforce_dirs,
        )
        if checked:
            audited_files += 1
            total_rows += checked
            findings.extend(file_findings)

    payload = {
        "auditor": "checklist-auditor",
        "files": audited_files,
        "rows": total_rows,
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
        print(
            f"checklist-auditor: {audited_files} file(s), {total_rows} evidence row(s), "
            f"{describe(findings)} [{scope}]"
        )
        for finding in findings:
            if args.quiet_survey and not finding.blocking:
                continue
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: {finding.path}:{finding.row}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
