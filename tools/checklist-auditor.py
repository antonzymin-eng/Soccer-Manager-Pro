#!/usr/bin/env python3
"""Automated approval-checklist evidence auditor (FR-TS-040..045).

Existence is not evidence. At an approval transition, a file citation must bind
the checklist claim to a concrete section or literal carried by that file, or a
programmatic check must have captured-output attestation supplied to this run.
Routine repository pipelines remain survey-only.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from testing_strategy_audit import (
    Finding,
    candidate_paths,
    describe,
    has_named_programmatic_check,
    infer_spec_id,
    infer_status,
    is_approved_status,
    is_legacy_survey,
    iter_tables,
)

BACKTICK_RE = re.compile(r"`([^`]+)`")
SECTION_RE = re.compile(r"§\s*(\d+(?:\.\d+)*)")
IDENT_RE = re.compile(r"\b(?:FR-[A-Z]+-\d+|ERR-\d+-\d+|KD-\d+|[A-Za-z_][A-Za-z0-9_.]*\([^)]*\))\b")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--survey-only", action="store_true", help="Report findings but never return a blocking verdict. Routine pre-commit/PR/nightly pipelines use this mode; approval transitions do not.")
    parser.add_argument("--changed-scope", action="store_true", help="Survey the whole root but consider blocking only spec directories named by --enforce-dir.")
    parser.add_argument("--enforce-dir", action="append", default=[], help="Spec directory, absolute or repo-relative, that is currently at an approval transition. Repeatable.")
    parser.add_argument("--captured-check", action="append", default=[], help="Exact check/command token whose output was captured for this approval walk. Repeatable. A named check is not RESOLVED without this attestation.")
    parser.add_argument("--quiet-survey", action="store_true", help="Print blocking findings plus counts, but omit individual survey findings.")
    return parser.parse_args()


def normalize_dirs(values: list[str], repo_root: Path) -> set[Path]:
    out: set[Path] = set()
    for value in values:
        path = Path(value)
        if not path.is_absolute():
            path = repo_root / path
        out.add(path.resolve())
    return out


def is_enforced(path: Path, *, survey_only: bool, changed_scope: bool, enforce_dirs: set[Path]) -> bool:
    if survey_only:
        return False
    if not changed_scope:
        return True
    resolved = path.resolve()
    return any(resolved == root or root in resolved.parents for root in enforce_dirs)


def resolve_path(token: str, *, repo_root: Path, spec_dir: Path) -> Path | None:
    token = token.split("#", 1)[0].strip()
    token = re.sub(r"\s+§.*$", "", token).strip()
    if not token or any(ch in token for ch in "<>*{}") or token.startswith(("http://", "https://")):
        return None
    path = Path(token)
    choices = [path] if path.is_absolute() else [repo_root / path, spec_dir / path]
    for candidate in choices:
        if candidate.is_file():
            return candidate.resolve()
    return None


def section_is_present(text: str, section: str) -> bool:
    return bool(re.search(rf"^#{{1,6}}\s+{re.escape(section)}(?:\s|\b)", text, re.MULTILINE))


def local_section_paths(evidence: str, spec_dir: Path) -> list[tuple[str, Path]]:
    """Resolve section-only citations like `§3.2` to the owning local section file."""
    out: list[tuple[str, Path]] = []
    for section in SECTION_RE.findall(evidence):
        major = section.split(".", 1)[0]
        matches = sorted(spec_dir.glob(f"section-{major}*.md"))
        for path in matches:
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            if section_is_present(text, section):
                out.append((f"§{section}", path.resolve()))
                break
    return out


def concrete_literals(claim: str, evidence: str, path_tokens: set[str]) -> list[str]:
    literals: list[str] = []
    for source in (claim, evidence):
        for token in BACKTICK_RE.findall(source):
            stripped = token.strip()
            if stripped in path_tokens or not stripped or stripped.startswith(("http://", "https://")):
                continue
            if stripped.split(maxsplit=1)[0] in {"bash", "python", "python3", "git", "grep", "rg", "dotnet", "sh"}:
                continue
            literals.append(stripped)
    literals.extend(IDENT_RE.findall(claim))
    return list(dict.fromkeys(literals))


def path_evidence_supports_claim(claim: str, evidence: str, resolved: list[tuple[str, Path]]) -> bool:
    if not resolved:
        return False
    texts: list[str] = []
    for _, path in resolved:
        try:
            texts.append(path.read_text(encoding="utf-8", errors="replace"))
        except OSError:
            return False
    combined = "\n".join(texts)

    evidence_sections = SECTION_RE.findall(evidence)
    if evidence_sections and all(any(section_is_present(text, section) for text in texts) for section in evidence_sections):
        return True

    path_tokens = {token for token, _ in resolved}
    literals = concrete_literals(claim, evidence, path_tokens)
    if literals and all(literal in combined for literal in literals):
        return True
    return False


def captured_programmatic_check(evidence: str, captured_checks: set[str]) -> bool:
    return bool(captured_checks) and any(check in evidence for check in captured_checks)


def audit_file(path: Path, repo_root: Path, *, survey_only: bool, changed_scope: bool, enforce_dirs: set[Path], captured_checks: set[str]) -> tuple[int | None, int, list[Finding]]:
    text = path.read_text(encoding="utf-8")
    spec_id = infer_spec_id(text)
    status = infer_status(text)
    lines = text.splitlines()
    findings: list[Finding] = []
    checked = 0
    blocking_scope = is_enforced(path, survey_only=survey_only, changed_scope=changed_scope, enforce_dirs=enforce_dirs)

    def blocks() -> bool:
        return blocking_scope and not is_legacy_survey(spec_id) and is_approved_status(status)

    for headers, rows in iter_tables(lines):
        evidence_indexes = [i for i, header in enumerate(headers) if "evidence" in header.lower() or "verification" in header.lower()]
        if not evidence_indexes:
            continue
        evidence_i = next((i for i in evidence_indexes if "evidence" in headers[i].lower()), evidence_indexes[0])
        claim_i = next((i for i, header in enumerate(headers) if "claim" in header.lower()), None)

        for line_no, cells in rows:
            evidence = cells[evidence_i].strip()
            claim = cells[claim_i].strip() if claim_i is not None and claim_i < len(cells) else ""
            row_id = cells[0].strip() if cells else f"line {line_no}"
            checked += 1

            if not evidence:
                findings.append(Finding(spec_id, str(path), row_id, "empty evidence cell", blocks()))
                continue
            if any(marker in evidence.lower() for marker in ("<file", "<check", "<path", "<test")):
                findings.append(Finding(spec_id, str(path), row_id, "placeholder evidence token is not executable/resolved evidence", blocks()))
                continue

            path_tokens = candidate_paths(evidence)
            resolved = [(token, resolved_path) for token in path_tokens if (resolved_path := resolve_path(token, repo_root=repo_root, spec_dir=path.parent)) is not None]
            resolved.extend(local_section_paths(evidence, path.parent))
            # De-duplicate by token+path while preserving order.
            resolved = list(dict.fromkeys(resolved))
            broken = [token for token in path_tokens if resolve_path(token, repo_root=repo_root, spec_dir=path.parent) is None and not any(ch in token for ch in "<>*{}")]
            named_check = has_named_programmatic_check(evidence, repo_root=repo_root, spec_dir=path.parent)

            if broken and not resolved and not named_check:
                findings.append(Finding(spec_id, str(path), row_id, "unresolved evidence path(s): " + ", ".join(broken), blocks()))
                continue

            file_bound = path_evidence_supports_claim(claim, evidence, resolved)
            check_bound = named_check and captured_programmatic_check(evidence, captured_checks)
            if file_bound or check_bound:
                continue

            if named_check and not check_bound:
                message = "programmatic check is named but this approval walk supplied no matching --captured-check output attestation"
            elif resolved:
                message = "evidence path exists but is not bound to the claim by a concrete cited section or literal value"
            else:
                message = "evidence is prose only; no claim-bound version-controlled evidence or captured programmatic check"
            findings.append(Finding(spec_id, str(path), row_id, message, blocks()))

    return spec_id, checked, findings


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    repo_root = args.repo_root.resolve()
    enforce_dirs = normalize_dirs(args.enforce_dir, repo_root)
    captured_checks = {value.strip() for value in args.captured_check if value.strip()}
    candidates = sorted({*root.rglob("section-9*.md"), *root.rglob("*approval-checklist*.md")})

    total_rows = 0
    findings: list[Finding] = []
    audited_files = 0
    for path in candidates:
        _, checked, file_findings = audit_file(path, repo_root, survey_only=args.survey_only, changed_scope=args.changed_scope, enforce_dirs=enforce_dirs, captured_checks=captured_checks)
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
        "captured_checks": sorted(captured_checks),
        "findings": [f.__dict__ for f in findings],
    }
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        scope = "survey-only routine pipeline" if args.survey_only else ("approval enforcement (explicit scope)" if args.changed_scope else "approval-state enforcement")
        print(f"checklist-auditor: {audited_files} file(s), {total_rows} evidence row(s), {describe(findings)} [{scope}]")
        for finding in findings:
            if args.quiet_survey and not finding.blocking:
                continue
            level = "BLOCK" if finding.blocking else "SURVEY"
            print(f"{level}: {finding.path}:{finding.row}: {finding.message}")

    return 1 if payload["blocking"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
