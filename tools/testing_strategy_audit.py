#!/usr/bin/env python3
"""Shared mechanics for Testing Strategy #19 documentation auditors."""

from __future__ import annotations

import re
import shlex
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

SPEC_ID_RE = re.compile(r"(?:Spec(?:ification)?\s*#|#)(\d{1,3})", re.IGNORECASE)
STATUS_RE = re.compile(r"^\*\*Status:\*\*\s*([^\n]+)$", re.IGNORECASE | re.MULTILINE)
SECTION_REF_RE = re.compile(r"§\s*(\d+)(?:\.\d+)*")
BACKTICK_RE = re.compile(r"`([^`]+)`")
MARKDOWN_LINK_RE = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
PATH_TOKEN_RE = re.compile(
    r"(?<![\w./-])(?:\.{0,2}/)?[\w.-]+(?:/[\w .()#+'-]+)*"
    r"\.(?:md|py|sh|cs|json|jsonc|yaml|yml|txt|asmdef|meta)(?![\w.-])",
    re.IGNORECASE,
)

PROGRAM_COMMANDS = {
    "bash",
    "dotnet",
    "git",
    "grep",
    "python",
    "python3",
    "rg",
    "sh",
}


@dataclass(frozen=True)
class Finding:
    spec_id: int | None
    path: str
    row: str
    message: str
    blocking: bool


def infer_spec_id(text: str) -> int | None:
    head = "\n".join(text.splitlines()[:40])
    match = SPEC_ID_RE.search(head)
    return int(match.group(1)) if match else None


def infer_status(text: str) -> str | None:
    head = "\n".join(text.splitlines()[:40])
    match = STATUS_RE.search(head)
    return match.group(1).strip() if match else None


def is_approved_status(status: str | None) -> bool:
    if status is None:
        return False
    normalized = status.upper()
    return normalized.startswith("APPROVED") and "AMENDMENT DRAFT" not in normalized


def is_legacy_survey(spec_id: int | None) -> bool:
    return spec_id is not None and 1 <= spec_id <= 8


def split_markdown_row(line: str) -> list[str]:
    stripped = line.strip()
    if not stripped.startswith("|"):
        return []
    return [cell.strip() for cell in stripped.strip("|").split("|")]


def is_separator_row(cells: list[str]) -> bool:
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", c.replace(" ", "")) for c in cells)


def iter_tables(lines: list[str]) -> Iterable[tuple[list[str], list[tuple[int, list[str]]]]]:
    i = 0
    while i + 1 < len(lines):
        headers = split_markdown_row(lines[i])
        sep = split_markdown_row(lines[i + 1])
        if headers and sep and len(headers) == len(sep) and is_separator_row(sep):
            rows: list[tuple[int, list[str]]] = []
            j = i + 2
            while j < len(lines):
                cells = split_markdown_row(lines[j])
                if not cells:
                    break
                if len(cells) == len(headers):
                    rows.append((j + 1, cells))
                j += 1
            yield headers, rows
            i = j
        else:
            i += 1


def candidate_paths(evidence: str) -> list[str]:
    found: list[str] = []
    for token in BACKTICK_RE.findall(evidence):
        if "/" in token or "." in token:
            found.append(token.strip())
    found.extend(MARKDOWN_LINK_RE.findall(evidence))
    found.extend(PATH_TOKEN_RE.findall(evidence))
    return list(dict.fromkeys(found))


def resolve_candidate(token: str, *, repo_root: Path, spec_dir: Path) -> bool:
    token = token.split("#", 1)[0].strip()
    token = re.sub(r"\s+§.*$", "", token).strip()
    if not token or any(ch in token for ch in "<>*{}"):
        return False
    if token.startswith(("http://", "https://")):
        return False
    p = Path(token)
    options = []
    if p.is_absolute():
        options.append(p)
    else:
        options.extend((repo_root / p, spec_dir / p))
    return any(candidate.exists() for candidate in options)


def has_resolved_local_section_reference(evidence: str, *, spec_dir: Path) -> bool:
    """Resolve canonical `§N.x` citations to the versioned section-N file.

    This is a path-resolution convenience, not a prose escape hatch: at least one
    explicit section symbol must be present and its owning section file must exist.
    """
    refs = SECTION_REF_RE.findall(evidence)
    if not refs:
        return False
    for section in refs:
        if any(spec_dir.glob(f"section-{section}*.md")):
            return True
    if "appendix" in evidence.lower() and (spec_dir / "appendices.md").exists():
        return True
    return False


def _inline_programmatic_command(
    token: str, *, repo_root: Path, spec_dir: Path
) -> bool:
    """Accept only explicit inline commands, never prose that merely says 'test/check'."""
    try:
        argv = shlex.split(token)
    except ValueError:
        return False
    if not argv or argv[0] not in PROGRAM_COMMANDS:
        return False

    # Commands that name a repository script/file must resolve that operand.
    path_like = [
        arg for arg in argv[1:]
        if not arg.startswith("-") and ("/" in arg or Path(arg).suffix)
    ]
    if path_like:
        return any(
            resolve_candidate(arg, repo_root=repo_root, spec_dir=spec_dir)
            for arg in path_like
        )

    # Repository-independent invocations such as `git diff` or `dotnet test`
    # are still explicit programmatic checks if they have a concrete subcommand.
    return len(argv) >= 2 and not argv[1].startswith("-")


def has_named_programmatic_check(evidence: str, *, repo_root: Path, spec_dir: Path) -> bool:
    for token in candidate_paths(evidence):
        if resolve_candidate(token, repo_root=repo_root, spec_dir=spec_dir):
            suffix = Path(token.split("#", 1)[0]).suffix.lower()
            if suffix in {".py", ".sh", ".cs"} or token.startswith("tools/"):
                return True

    return any(
        _inline_programmatic_command(token.strip(), repo_root=repo_root, spec_dir=spec_dir)
        for token in BACKTICK_RE.findall(evidence)
    )


def describe(findings: list[Finding]) -> str:
    blocks = sum(f.blocking for f in findings)
    notes = len(findings) - blocks
    return f"{blocks} blocking, {notes} survey/note"
