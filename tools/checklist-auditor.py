#!/usr/bin/env python3
"""Automated approval-checklist evidence auditor (FR-TS-040..045).

This Stage 0+1 tool checks only mechanically decidable facts: checklist rows
exist, required checkboxes are checked, cited repository paths/sections resolve,
and explicit programmatic invocations have captured successful output. It does
not attempt natural-language entailment between a claim and prose in a cited
file; Testing Strategy §3.5.2 assigns that semantic judgment to the Stage 0
reviewer. Routine repository pipelines remain survey-only.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path

from testing_strategy_audit import (
    Finding,
    candidate_paths,
    canonical_spec_status,
    describe,
    infer_spec_id,
    invalid_programmatic_check_labels,
    is_approved_status,
    is_legacy_survey,
    iter_tables,
    programmatic_check_commands,
)

SECTION_RE = re.compile(r"§\s*(\d+(?:\.\d+)*)")
CHECKBOX_RE = re.compile(r"^\s*[-*]\s*\[([ xX])\]\s*(.+?)\s*$")
TABLE_STATUS_CHECKBOX_RE = re.compile(r"\[\s*([ xX])\s*\]")
HEADING_SECTION_RE = re.compile(r"^#{1,6}\s+(\d+(?:\.\d+)*)\b")
EVIDENCE_MARKER_RE = re.compile(r"\bEvidence\s*:\s*", re.IGNORECASE)


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
        "--captured-check",
        action="append",
        default=[],
        help="Exact explicit command whose successful output was captured externally for this approval walk. Repeatable.",
    )
    parser.add_argument(
        "--execute-checks",
        action="store_true",
        help="Execute explicit backticked programmatic checks without a shell and capture their output. Used by the required approval-transition CI helper.",
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


def owning_spec_dir(path: Path, repo_root: Path) -> Path:
    """Return the direct docs/specs child that owns a nested checklist path."""
    specs_root = (repo_root / "docs" / "specs").resolve()
    resolved = path.resolve()
    try:
        rel = resolved.relative_to(specs_root)
    except ValueError:
        return path.parent.resolve()
    if not rel.parts:
        return path.parent.resolve()
    return (specs_root / rel.parts[0]).resolve()


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


def legacy_exempt(
    spec_id: int | None, *, changed_scope: bool, blocking_scope: bool
) -> bool:
    """Keep #1-#8 survey-only except at an explicit natural-reapproval scope."""
    return is_legacy_survey(spec_id) and not (changed_scope and blocking_scope)


def resolve_path(token: str, *, repo_root: Path, spec_dir: Path) -> Path | None:
    token = token.split("#", 1)[0].strip()
    token = re.sub(r"\s+§.*$", "", token).strip()
    if (
        not token
        or any(ch in token for ch in "<>*{}")
        or token.startswith(("http://", "https://"))
    ):
        return None
    path = Path(token)
    choices = [path] if path.is_absolute() else [repo_root / path, spec_dir / path]
    for candidate in choices:
        if candidate.is_file():
            return candidate.resolve()
    return None


def section_heading_match(line: str, section: str) -> re.Match[str] | None:
    return re.match(
        rf"^(?P<marks>#{{1,6}})\s+{re.escape(section)}(?=$|\s|[:—-])",
        line,
    )


def section_block(text: str, section: str) -> str | None:
    """Return the cited Markdown section through the next peer/parent heading."""
    lines = text.splitlines()
    start = None
    level = None
    for i, line in enumerate(lines):
        match = section_heading_match(line, section)
        if match:
            start = i
            level = len(match.group("marks"))
            break
    if start is None or level is None:
        return None
    end = len(lines)
    for i in range(start + 1, len(lines)):
        heading = re.match(r"^(#{1,6})\s+", lines[i])
        if heading and len(heading.group(1)) <= level:
            end = i
            break
    return "\n".join(lines[start:end])


def resolve_section_references(
    evidence: str,
    *,
    spec_dir: Path,
    resolved_paths: list[tuple[str, Path]],
) -> tuple[list[tuple[str, Path]], list[str]]:
    """Resolve every explicit §N.x reference to the cited Markdown file when one exists."""
    found: list[tuple[str, Path]] = []
    missing: list[str] = []
    markdown_candidates = [
        path for _, path in resolved_paths if path.suffix.lower() == ".md"
    ]

    for section in SECTION_RE.findall(evidence):
        major = section.split(".", 1)[0]
        # If evidence explicitly cites one or more Markdown files, the section
        # must resolve in those files. Falling back to the owning spec would
        # silently rebind a broken cross-file citation to unrelated local text.
        candidates = (
            list(markdown_candidates)
            if markdown_candidates
            else sorted(spec_dir.glob(f"section-{major}*.md"))
        )
        seen: set[Path] = set()
        matched_path: Path | None = None
        for path in candidates:
            resolved = path.resolve()
            if resolved in seen:
                continue
            seen.add(resolved)
            try:
                text = resolved.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            if section_block(text, section) is not None:
                matched_path = resolved
                break
        if matched_path is None:
            missing.append(f"§{section}")
        else:
            found.append((f"§{section}", matched_path))
    return found, missing


def captured_programmatic_check(label: str, captured_checks: set[str]) -> bool:
    return label in captured_checks


def run_programmatic_check(
    label: str,
    argv: tuple[str, ...],
    *,
    repo_root: Path,
    cache: dict[str, tuple[int, str]],
) -> tuple[int, str]:
    """Execute one previously validated command without a shell and capture output."""
    if label in cache:
        return cache[label]
    try:
        proc = subprocess.run(
            list(argv),
            cwd=repo_root,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=300,
        )
        result = (proc.returncode, proc.stdout or "")
    except subprocess.TimeoutExpired as exc:
        output = exc.stdout if isinstance(exc.stdout, str) else ""
        result = (124, output + "\nERROR: approval check exceeded 300 seconds")
    except OSError as exc:
        result = (127, f"ERROR: could not execute approval check: {exc}")
    cache[label] = result
    return result


def checkbox_rows(lines: list[str]) -> list[tuple[int, str, bool, str, str]]:
    """Return line, row-id, checked, claim, evidence for Markdown checkboxes."""
    rows: list[tuple[int, str, bool, str, str]] = []
    current_section = "§9"
    for line_no, line in enumerate(lines, start=1):
        heading = HEADING_SECTION_RE.match(line)
        if heading:
            current_section = f"§{heading.group(1)}"
        match = CHECKBOX_RE.match(line)
        if not match:
            continue
        checked = match.group(1).lower() == "x"
        body = match.group(2).strip()
        marker = EVIDENCE_MARKER_RE.search(body)
        if marker:
            claim = body[: marker.start()].strip()
            evidence = body[marker.end() :].strip()
        else:
            claim = body
            evidence = body
        rows.append((line_no, f"{current_section} line {line_no}", checked, claim, evidence))
    return rows


def audit_evidence_row(
    *,
    spec_id: int | None,
    path: Path,
    spec_dir: Path,
    row_id: str,
    claim: str,
    evidence: str,
    repo_root: Path,
    captured_checks: set[str],
    execute_checks: bool,
    execution_cache: dict[str, tuple[int, str]],
    blocking: bool,
) -> Finding | None:
    # `claim` remains in the signature because reports identify the checklist
    # claim, but semantic entailment is deliberately a Stage 0 reviewer duty.
    del claim

    if not evidence:
        return Finding(spec_id, str(path), row_id, "empty evidence", blocking)
    if any(marker in evidence.lower() for marker in ("<file", "<check", "<path", "<test")):
        return Finding(
            spec_id,
            str(path),
            row_id,
            "placeholder evidence token is not executable/resolved evidence",
            blocking,
        )

    invalid_commands = invalid_programmatic_check_labels(
        evidence, repo_root=repo_root, spec_dir=spec_dir
    )
    if invalid_commands:
        return Finding(
            spec_id,
            str(path),
            row_id,
            "invalid/unsupported programmatic invocation(s): "
            + ", ".join(f"`{label}`" for label in invalid_commands),
            blocking,
        )

    commands = programmatic_check_commands(
        evidence, repo_root=repo_root, spec_dir=spec_dir
    )
    command_labels = {label for label, _ in commands}

    path_tokens = candidate_paths(evidence)
    resolved: list[tuple[str, Path]] = []
    broken: list[str] = []
    for token in path_tokens:
        if token in command_labels:
            continue
        resolved_path = resolve_path(token, repo_root=repo_root, spec_dir=spec_dir)
        if resolved_path is not None:
            resolved.append((token, resolved_path))
        elif not any(ch in token for ch in "<>*{}"):
            broken.append(token)
    resolved = list(dict.fromkeys(resolved))

    section_paths, missing_sections = resolve_section_references(
        evidence, spec_dir=spec_dir, resolved_paths=resolved
    )
    resolved.extend(section_paths)
    resolved = list(dict.fromkeys(resolved))

    if broken:
        return Finding(
            spec_id,
            str(path),
            row_id,
            "unresolved evidence path(s): " + ", ".join(dict.fromkeys(broken)),
            blocking,
        )
    if missing_sections:
        return Finding(
            spec_id,
            str(path),
            row_id,
            "unresolved evidence section(s): " + ", ".join(dict.fromkeys(missing_sections)),
            blocking,
        )

    for label, argv in commands:
        if execute_checks:
            returncode, output = run_programmatic_check(
                label, argv, repo_root=repo_root, cache=execution_cache
            )
            if returncode != 0:
                tail = output.strip()[-1000:]
                detail = f"; captured output: {tail}" if tail else ""
                return Finding(
                    spec_id,
                    str(path),
                    row_id,
                    f"programmatic check failed with exit {returncode}: `{label}`{detail}",
                    blocking,
                )
        elif not captured_programmatic_check(label, captured_checks):
            return Finding(
                spec_id,
                str(path),
                row_id,
                "programmatic check is named but this approval walk supplied no matching captured output; rerun with --execute-checks or exact --captured-check attestation",
                blocking,
            )

    if resolved or commands:
        return None

    return Finding(
        spec_id,
        str(path),
        row_id,
        "evidence is prose only; no resolved version-controlled path/section or captured programmatic check",
        blocking,
    )


def audit_file(
    path: Path,
    repo_root: Path,
    *,
    survey_only: bool,
    changed_scope: bool,
    enforce_dirs: set[Path],
    captured_checks: set[str],
    execute_checks: bool,
    execution_cache: dict[str, tuple[int, str]],
) -> tuple[int | None, int, list[Finding]]:
    text = path.read_text(encoding="utf-8")
    spec_dir = owning_spec_dir(path, repo_root)
    spec_id = infer_spec_id(text)
    if spec_id is None:
        for sibling in sorted(spec_dir.glob("section-*.md")):
            try:
                spec_id = infer_spec_id(
                    sibling.read_text(encoding="utf-8", errors="replace")
                )
            except OSError:
                continue
            if spec_id is not None:
                break
    status = canonical_spec_status(repo_root, spec_dir, fallback_text=text)
    lines = text.splitlines()
    findings: list[Finding] = []
    checked = 0
    blocking_scope = is_enforced(
        spec_dir,
        survey_only=survey_only,
        changed_scope=changed_scope,
        enforce_dirs=enforce_dirs,
    )
    blocking = (
        blocking_scope
        and not legacy_exempt(
            spec_id, changed_scope=changed_scope, blocking_scope=blocking_scope
        )
        and is_approved_status(status)
    )

    for headers, rows in iter_tables(lines):
        evidence_indexes = [
            i
            for i, header in enumerate(headers)
            if "evidence" in header.lower() or "verification" in header.lower()
        ]
        if not evidence_indexes:
            continue
        evidence_i = next(
            (i for i in evidence_indexes if "evidence" in headers[i].lower()),
            evidence_indexes[0],
        )
        claim_i = next(
            (i for i, header in enumerate(headers) if "claim" in header.lower()),
            None,
        )
        status_i = next(
            (i for i, header in enumerate(headers) if header.strip().lower() == "status"),
            None,
        )

        for line_no, cells in rows:
            evidence = cells[evidence_i].strip()
            claim = (
                cells[claim_i].strip()
                if claim_i is not None and claim_i < len(cells)
                else ""
            )
            row_id = cells[0].strip() if cells else f"line {line_no}"
            checked += 1

            if status_i is not None and status_i < len(cells):
                status_checkbox = TABLE_STATUS_CHECKBOX_RE.search(cells[status_i])
                if status_checkbox and status_checkbox.group(1).lower() != "x":
                    findings.append(
                        Finding(
                            spec_id,
                            str(path),
                            row_id,
                            "approval checklist status checkbox is not checked",
                            blocking,
                        )
                    )

            finding = audit_evidence_row(
                spec_id=spec_id,
                path=path,
                spec_dir=spec_dir,
                row_id=row_id,
                claim=claim,
                evidence=evidence,
                repo_root=repo_root,
                captured_checks=captured_checks,
                execute_checks=execute_checks,
                execution_cache=execution_cache,
                blocking=blocking,
            )
            if finding is not None:
                findings.append(finding)

    for line_no, row_id, is_checked, claim, evidence in checkbox_rows(lines):
        checked += 1
        if not is_checked:
            findings.append(
                Finding(
                    spec_id,
                    str(path),
                    row_id,
                    "approval checklist checkbox is not checked",
                    blocking,
                )
            )
        finding = audit_evidence_row(
            spec_id=spec_id,
            path=path,
            spec_dir=spec_dir,
            row_id=row_id,
            claim=claim,
            evidence=evidence,
            repo_root=repo_root,
            captured_checks=captured_checks,
            execute_checks=execute_checks,
            execution_cache=execution_cache,
            blocking=blocking,
        )
        if finding is not None:
            findings.append(finding)

    return spec_id, checked, findings


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    repo_root = args.repo_root.resolve()
    enforce_dirs = normalize_dirs(args.enforce_dir, repo_root)
    captured_checks = {value.strip() for value in args.captured_check if value.strip()}
    candidates = sorted(
        {*root.rglob("section-9*.md"), *root.rglob("*approval-checklist*.md")}
    )

    total_rows = 0
    findings: list[Finding] = []
    audited_files = 0
    candidate_dirs: set[Path] = set()
    rows_by_dir: dict[Path, int] = {}
    execution_cache: dict[str, tuple[int, str]] = {}

    for path in candidates:
        spec_dir = owning_spec_dir(path, repo_root)
        candidate_dirs.add(spec_dir)
        _, checked, file_findings = audit_file(
            path,
            repo_root,
            survey_only=args.survey_only,
            changed_scope=args.changed_scope,
            enforce_dirs=enforce_dirs,
            captured_checks=captured_checks,
            execute_checks=args.execute_checks,
            execution_cache=execution_cache,
        )
        rows_by_dir[spec_dir] = rows_by_dir.get(spec_dir, 0) + checked
        if checked:
            audited_files += 1
            total_rows += checked
            findings.extend(file_findings)

    if not args.survey_only and enforce_dirs:
        for enforced_dir in sorted(enforce_dirs):
            matching_dirs = {
                candidate_dir
                for candidate_dir in candidate_dirs
                if candidate_dir == enforced_dir or enforced_dir in candidate_dir.parents
            }
            if not matching_dirs:
                findings.append(
                    Finding(
                        None,
                        str(enforced_dir),
                        "§9",
                        "missing required approval-checklist file",
                        True,
                    )
                )
                continue
            row_count = sum(
                rows_by_dir.get(candidate_dir, 0) for candidate_dir in matching_dirs
            )
            if row_count == 0:
                findings.append(
                    Finding(
                        None,
                        str(enforced_dir),
                        "§9",
                        "approval-checklist contains no auditable evidence rows",
                        True,
                    )
                )

    payload = {
        "auditor": "checklist-auditor",
        "files": audited_files,
        "rows": total_rows,
        "blocking": sum(f.blocking for f in findings),
        "notes": sum(not f.blocking for f in findings),
        "survey_only": args.survey_only,
        "changed_scope": args.changed_scope,
        "enforced_dirs": sorted(str(path) for path in enforce_dirs),
        "captured_checks": sorted(captured_checks),
        "execute_checks": args.execute_checks,
        "executed_checks": {
            label: {"exit": result[0], "output": result[1]}
            for label, result in sorted(execution_cache.items())
        },
        "semantic_entailment": "manual-stage-0-review",
        "findings": [f.__dict__ for f in findings],
    }
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        scope = (
            "survey-only routine pipeline"
            if args.survey_only
            else (
                "approval enforcement (explicit scope)"
                if args.changed_scope
                else "approval-state enforcement"
            )
        )
        print(
            f"checklist-auditor: {audited_files} file(s), {total_rows} evidence row(s), "
            f"{describe(findings)} [{scope}]"
        )
        if execution_cache:
            for label, (returncode, output) in sorted(execution_cache.items()):
                print(f"CAPTURED CHECK exit={returncode}: `{label}`")
                if output.strip():
                    print(output.rstrip())
        for finding in findings:
            if args.quiet_survey and not finding.blocking:
                continue
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: {finding.path}:{finding.row}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
