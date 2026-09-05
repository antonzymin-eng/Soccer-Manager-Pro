#!/usr/bin/env python3
"""Automated approval-checklist evidence auditor (FR-TS-040..045).

Existence is not evidence. At an approval transition, a file/section citation
must carry concrete text or values that support the checklist claim, or a named
programmatic check must have captured-output attestation supplied to this run.
Both table rows and Markdown checkbox rows are checklist rows. Routine repository
pipelines remain survey-only.
"""

from __future__ import annotations

import argparse
import json
import math
import re
from pathlib import Path

from testing_strategy_audit import (
    Finding,
    candidate_paths,
    canonical_spec_status,
    describe,
    has_named_programmatic_check,
    infer_spec_id,
    is_approved_status,
    is_legacy_survey,
    iter_tables,
)

BACKTICK_RE = re.compile(r"`([^`]+)`")
SECTION_RE = re.compile(r"§\s*(\d+(?:\.\d+)*)")
IDENT_RE = re.compile(r"\b(?:FR-[A-Z]+-\d+|ERR-\d+-\d+|KD-\d+|[A-Za-z_][A-Za-z0-9_.]*\([^)]*\))\b")
CHECKBOX_RE = re.compile(r"^\s*[-*]\s*\[([ xX])\]\s*(.+?)\s*$")
HEADING_SECTION_RE = re.compile(r"^#{1,6}\s+(\d+(?:\.\d+)*)\b")
EVIDENCE_MARKER_RE = re.compile(r"\bEvidence\s*:\s*", re.IGNORECASE)
WORD_RE = re.compile(r"[A-Za-z][A-Za-z0-9_.+-]*|[-+]?\d+(?:\.\d+)?%?")
NUMERIC_TERM_RE = re.compile(r"[-+]?\d+(?:\.\d+)?%?")
MANDATORY_VALUE_WORDS = {
    "enabled", "disabled", "true", "false", "present", "absent",
    "on", "off", "yes", "no", "not",
}
STOP_WORDS = {
    "about", "after", "against", "also", "and", "are", "been", "before",
    "being", "between", "check", "checked", "claim", "confirmed", "defined",
    "evidence", "exact", "file", "for", "from", "granted", "has", "have",
    "into", "its", "must", "not", "only", "per", "review", "reviewed",
    "row", "rule", "section", "specified", "the", "their", "this", "through",
    "under", "uses", "using", "verified", "with", "within",
}


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


def local_section_paths(evidence: str, spec_dir: Path) -> list[tuple[str, Path]]:
    """Resolve canonical `§N.x` citations to the owning local section file."""
    out: list[tuple[str, Path]] = []
    for section in SECTION_RE.findall(evidence):
        major = section.split(".", 1)[0]
        matches = sorted(spec_dir.glob(f"section-{major}*.md"))
        for path in matches:
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            if section_block(text, section) is not None:
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
    # Explicit numeric and polarity values are semantic claim literals. They
    # are mandatory rather than optional members of the lexical quorum: `60`
    # cannot be satisfied by `600`, and `disabled` cannot be outvoted by
    # surrounding words in evidence that actually says `enabled`.
    literals.extend(NUMERIC_TERM_RE.findall(claim))
    for token in WORD_RE.findall(claim):
        lowered = token.lower().strip("._+-")
        if lowered in MANDATORY_VALUE_WORDS:
            literals.append(lowered)
    return list(dict.fromkeys(literals))


def claim_terms(claim: str) -> list[str]:
    # Remove Markdown links/paths and section citations before lexical binding;
    # those identify evidence location, not the value the claim says is there.
    text = re.sub(r"\[[^\]]+\]\([^)]+\)", " ", claim)
    text = SECTION_RE.sub(" ", text)
    text = re.sub(r"`[^`]+\.(?:md|py|sh|cs|json|yaml|yml|txt)`", " ", text, flags=re.IGNORECASE)
    terms: list[str] = []
    for token in WORD_RE.findall(text):
        lowered = token.lower().strip("._+-")
        numeric = bool(NUMERIC_TERM_RE.fullmatch(token))
        if ((not numeric and len(lowered) < 3) or lowered in STOP_WORDS):
            continue
        terms.append(lowered)
    return list(dict.fromkeys(terms))


def text_contains_complete_term(text: str, term: str) -> bool:
    """Match a complete claim value/token rather than an arbitrary substring."""
    needle = term.strip()
    if not needle:
        return False
    escaped = re.escape(needle)
    if NUMERIC_TERM_RE.fullmatch(needle):
        # Prevent 60 from matching 600 or 60.0. Percent/sign are part of the
        # numeric value and must not be silently discarded.
        pattern = rf"(?<![0-9.+-]){escaped}(?![0-9.%])"
    else:
        # Hyphen/underscore join lexical tokens; punctuation such as a trailing
        # period remains a boundary.
        pattern = rf"(?<![A-Za-z0-9_-]){escaped}(?![A-Za-z0-9_-])"
    return re.search(pattern, text, re.IGNORECASE) is not None


def path_evidence_supports_claim(claim: str, evidence: str, resolved: list[tuple[str, Path]]) -> bool:
    if not resolved:
        return False

    path_text: dict[Path, str] = {}
    for _, path in resolved:
        if path in path_text:
            continue
        try:
            path_text[path] = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            return False

    evidence_sections = SECTION_RE.findall(evidence)
    chunks: list[str] = []
    if evidence_sections:
        # A section citation constrains the evidence to that section. Merely
        # proving that the heading exists is insufficient; the claim must bind
        # to text/value inside the cited block.
        for section in evidence_sections:
            matching = [block for text in path_text.values() if (block := section_block(text, section)) is not None]
            if not matching:
                return False
            chunks.extend(matching)
    else:
        chunks.extend(path_text.values())

    combined = "\n".join(chunks)
    path_tokens = {token for token, _ in resolved}
    literals = concrete_literals(claim, evidence, path_tokens)
    if literals and not all(text_contains_complete_term(combined, literal) for literal in literals):
        return False

    terms = claim_terms(claim)
    if not terms:
        return bool(literals)
    matched = sum(text_contains_complete_term(combined, term) for term in terms)
    required = 1 if len(terms) == 1 else min(4, max(2, math.ceil(len(terms) * 0.4)))
    return matched >= required


def captured_programmatic_check(evidence: str, captured_checks: set[str]) -> bool:
    return bool(captured_checks) and any(check in evidence for check in captured_checks)


def checkbox_rows(lines: list[str]) -> list[tuple[int, str, str, str]]:
    """Return line, row-id, claim, evidence for Markdown approval checkboxes."""
    rows: list[tuple[int, str, str, str]] = []
    current_section = "§9"
    for line_no, line in enumerate(lines, start=1):
        heading = HEADING_SECTION_RE.match(line)
        if heading:
            current_section = f"§{heading.group(1)}"
        match = CHECKBOX_RE.match(line)
        if not match:
            continue
        body = match.group(2).strip()
        marker = EVIDENCE_MARKER_RE.search(body)
        if marker:
            claim = body[: marker.start()].strip()
            evidence = body[marker.end() :].strip()
        else:
            # Some established checklists put citations inline rather than after
            # an `Evidence:` marker. Use the body as evidence input so paths,
            # sections or named checks can still resolve; prose-only rows fail.
            claim = body
            evidence = body
        rows.append((line_no, f"{current_section} line {line_no}", claim, evidence))
    return rows


def audit_evidence_row(
    *,
    spec_id: int | None,
    path: Path,
    row_id: str,
    claim: str,
    evidence: str,
    repo_root: Path,
    captured_checks: set[str],
    blocking: bool,
) -> Finding | None:
    if not evidence:
        return Finding(spec_id, str(path), row_id, "empty evidence", blocking)
    if any(marker in evidence.lower() for marker in ("<file", "<check", "<path", "<test")):
        return Finding(spec_id, str(path), row_id, "placeholder evidence token is not executable/resolved evidence", blocking)

    path_tokens = candidate_paths(evidence)
    resolved = [
        (token, resolved_path)
        for token in path_tokens
        if (resolved_path := resolve_path(token, repo_root=repo_root, spec_dir=path.parent)) is not None
    ]
    resolved.extend(local_section_paths(evidence, path.parent))
    resolved = list(dict.fromkeys(resolved))
    broken = [
        token
        for token in path_tokens
        if resolve_path(token, repo_root=repo_root, spec_dir=path.parent) is None
        and not any(ch in token for ch in "<>*{}")
    ]
    named_check = has_named_programmatic_check(evidence, repo_root=repo_root, spec_dir=path.parent)

    if broken and not resolved and not named_check:
        return Finding(spec_id, str(path), row_id, "unresolved evidence path(s): " + ", ".join(broken), blocking)

    file_bound = path_evidence_supports_claim(claim, evidence, resolved)
    check_bound = named_check and captured_programmatic_check(evidence, captured_checks)
    if file_bound or check_bound:
        return None

    if named_check and not check_bound:
        message = "programmatic check is named but this approval walk supplied no matching --captured-check output attestation"
    elif resolved:
        message = "evidence path/section resolves but does not contain concrete text or values supporting the claim"
    else:
        message = "evidence is prose only; no claim-bound version-controlled evidence or captured programmatic check"
    return Finding(spec_id, str(path), row_id, message, blocking)


def audit_file(path: Path, repo_root: Path, *, survey_only: bool, changed_scope: bool, enforce_dirs: set[Path], captured_checks: set[str]) -> tuple[int | None, int, list[Finding]]:
    text = path.read_text(encoding="utf-8")
    spec_id = infer_spec_id(text)
    status = canonical_spec_status(repo_root, path.parent, fallback_text=text)
    lines = text.splitlines()
    findings: list[Finding] = []
    checked = 0
    blocking_scope = is_enforced(path, survey_only=survey_only, changed_scope=changed_scope, enforce_dirs=enforce_dirs)
    blocking = blocking_scope and not is_legacy_survey(spec_id) and is_approved_status(status)

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
            finding = audit_evidence_row(
                spec_id=spec_id,
                path=path,
                row_id=row_id,
                claim=claim,
                evidence=evidence,
                repo_root=repo_root,
                captured_checks=captured_checks,
                blocking=blocking,
            )
            if finding is not None:
                findings.append(finding)

    for line_no, row_id, claim, evidence in checkbox_rows(lines):
        checked += 1
        finding = audit_evidence_row(
            spec_id=spec_id,
            path=path,
            row_id=row_id,
            claim=claim,
            evidence=evidence,
            repo_root=repo_root,
            captured_checks=captured_checks,
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
    candidates = sorted({*root.rglob("section-9*.md"), *root.rglob("*approval-checklist*.md")})

    total_rows = 0
    findings: list[Finding] = []
    audited_files = 0
    candidate_dirs: set[Path] = set()
    rows_by_dir: dict[Path, int] = {}
    for path in candidates:
        parent = path.parent.resolve()
        candidate_dirs.add(parent)
        _, checked, file_findings = audit_file(path, repo_root, survey_only=args.survey_only, changed_scope=args.changed_scope, enforce_dirs=enforce_dirs, captured_checks=captured_checks)
        rows_by_dir[parent] = rows_by_dir.get(parent, 0) + checked
        if checked:
            audited_files += 1
            total_rows += checked
            findings.extend(file_findings)

    # Approval enforcement is incomplete if an explicitly enforced spec has no
    # checklist artifact or if its checklist contributes no auditable evidence
    # rows. Treat absence itself as a blocking finding rather than an empty pass.
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
            row_count = sum(rows_by_dir.get(candidate_dir, 0) for candidate_dir in matching_dirs)
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
