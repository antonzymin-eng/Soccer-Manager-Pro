#!/usr/bin/env python3
"""Shared mechanics for Testing Strategy #19 documentation auditors."""

from __future__ import annotations

import re
import shlex
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

SPEC_ID_RE = re.compile(r"(?:Spec(?:ification)?\s*#|#)(\d{1,3})", re.IGNORECASE)
# Individual specs use several presentation forms, including blockquoted and
# backticked values. SPEC_INDEX is canonical when available; this regex is the
# fallback for isolated fixtures and incomplete candidate trees.
STATUS_RE = re.compile(
    r"^\s*>?\s*\*\*Status:\*\*\s*(?P<value>[^\n]+)$",
    re.IGNORECASE | re.MULTILINE,
)
SECTION_REF_RE = re.compile(r"§\s*(\d+)(?:\.\d+)*")
BACKTICK_RE = re.compile(r"`([^`]+)`")
MARKDOWN_LINK_RE = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
PATH_TOKEN_RE = re.compile(
    r"(?<![\w./-])(?:\.{0,2}/)?[\w.-]+(?:/[\w .()#+'-]+)*"
    r"\.(?:md|py|sh|cs|json|jsonc|yaml|yml|txt|asmdef|meta)(?![\w.-])",
    re.IGNORECASE,
)
SHELL_CONTROL_RE = re.compile(r"^[;&|<>]+$")
# Approval checks execute with shell=False. Reject syntax whose meaning would
# otherwise depend on shell expansion instead of silently passing it as a
# literal argument and recording only the prefix command as executed.
UNSUPPORTED_SHELL_EXPANSION_RE = re.compile(
    r"(?:\$\(|\$\{|\$[A-Za-z_][A-Za-z0-9_]*|<\(|>\()"
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
READ_ONLY_GIT_SUBCOMMANDS = {
    "diff",
    "grep",
    "ls-files",
    "rev-parse",
    "show",
    "status",
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


def normalize_status(value: str | None) -> str | None:
    if value is None:
        return None
    # Strip common Markdown and decorative presentation without changing
    # semantic words such as AMENDMENT DRAFT. This accepts established local
    # forms including `APPROVED`, ✅ APPROVED, and blockquoted status lines.
    normalized = re.sub(r"[`*_]", "", value).strip()
    normalized = re.sub(r"^[^A-Za-z0-9]+", "", normalized).strip()
    return normalized or None


def infer_status(text: str) -> str | None:
    head = "\n".join(text.splitlines()[:40])
    match = STATUS_RE.search(head)
    return normalize_status(match.group("value")) if match else None


def is_approved_status(status: str | None) -> bool:
    normalized = normalize_status(status)
    if normalized is None:
        return False
    upper = normalized.upper()
    return upper.startswith("APPROVED") and "AMENDMENT DRAFT" not in upper


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


def registry_statuses_from_text(text: str) -> dict[str, str]:
    """Return canonical `folder -> status` values from SPEC_INDEX registry.

    The parser keys by header names rather than fixed column numbers so the
    historical prose above the registry and future added columns do not matter.
    """
    for headers, rows in iter_tables(text.splitlines()):
        lowered = [header.strip().lower() for header in headers]
        if "folder" not in lowered or "status" not in lowered:
            continue
        folder_i = lowered.index("folder")
        status_i = lowered.index("status")
        out: dict[str, str] = {}
        for _, cells in rows:
            if len(cells) <= max(folder_i, status_i):
                continue
            folder = cells[folder_i].strip().strip("`").strip().rstrip("/")
            status = normalize_status(cells[status_i])
            if folder and status:
                out[folder] = status
        if out:
            return out
    return {}


def registry_statuses(repo_root: Path) -> dict[str, str]:
    index = repo_root / "docs" / "specs" / "SPEC_INDEX.md"
    if not index.is_file():
        return {}
    try:
        return registry_statuses_from_text(index.read_text(encoding="utf-8", errors="replace"))
    except OSError:
        return {}


def canonical_spec_status(
    repo_root: Path,
    spec_dir: Path,
    *,
    fallback_text: str | None = None,
) -> str | None:
    """Resolve status from canonical SPEC_INDEX, falling back to local metadata.

    SPEC_INDEX explicitly states that its approval status overrides individual
    spec files. Isolated auditor fixtures often omit the index, so fallback
    parsing remains useful for unit tests and incomplete authoring trees.
    """
    status = registry_statuses(repo_root).get(spec_dir.name)
    if status is not None:
        return status
    return infer_status(fallback_text or "")


def candidate_paths(evidence: str) -> list[str]:
    found: list[str] = []
    for token in BACKTICK_RE.findall(evidence):
        if "/" in token or "." in token:
            found.append(token.strip())
    found.extend(MARKDOWN_LINK_RE.findall(evidence))
    found.extend(PATH_TOKEN_RE.findall(evidence))
    return list(dict.fromkeys(found))


def repository_file(candidate: Path, *, repo_root: Path) -> Path | None:
    """Return a contained repository file, requiring Git tracking when available."""
    root = repo_root.resolve()
    try:
        resolved = candidate.resolve(strict=True)
        rel = resolved.relative_to(root)
    except (OSError, ValueError):
        return None
    if not resolved.is_file():
        return None

    # Unit fixtures and extracted source trees may intentionally omit .git.
    # In a real checkout, however, approval evidence must name an indexed file.
    if (root / ".git").exists():
        try:
            proc = subprocess.run(
                [
                    "git",
                    "-C",
                    str(root),
                    "ls-files",
                    "--error-unmatch",
                    "--",
                    rel.as_posix(),
                ],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                check=False,
            )
        except OSError:
            return None
        if proc.returncode != 0:
            return None
    return resolved


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
    return any(
        repository_file(candidate, repo_root=repo_root) is not None
        for candidate in options
    )


def has_resolved_local_section_reference(evidence: str, *, spec_dir: Path) -> bool:
    """Resolve canonical `§N.x` citations to the versioned section-N file."""
    refs = SECTION_REF_RE.findall(evidence)
    if not refs:
        return False
    for section in refs:
        if any(spec_dir.glob(f"section-{section}*.md")):
            return True
    if "appendix" in evidence.lower() and (spec_dir / "appendices.md").exists():
        return True
    return False


def _shell_aware_argv(token: str) -> list[str] | None:
    """Split one backticked invocation while exposing unquoted shell controls."""
    try:
        lexer = shlex.shlex(token, posix=True, punctuation_chars=";&|<>")
        lexer.whitespace_split = True
        return list(lexer)
    except ValueError:
        return None


def _inline_programmatic_argv(
    token: str, *, repo_root: Path, spec_dir: Path
) -> tuple[str, ...] | None:
    """Parse an explicit, bounded programmatic invocation from a backtick span.

    Merely citing a source file is never an invocation. In particular, `.cs`
    citations remain file evidence. Commands are executed without a shell.
    Compound commands, redirections, substitutions, and shell expansion are
    rejected rather than partially or differently executed.
    """
    if UNSUPPORTED_SHELL_EXPANSION_RE.search(token):
        return None
    argv = _shell_aware_argv(token)
    if not argv or argv[0] not in PROGRAM_COMMANDS:
        return None
    if any(SHELL_CONTROL_RE.fullmatch(arg) for arg in argv):
        return None

    command = argv[0]
    if command in {"python", "python3", "bash", "sh"}:
        # Inline code/module execution is deliberately excluded. Approval checks
        # must name a version-controlled repository script.
        if any(arg in {"-c", "-m"} for arg in argv[1:]):
            return None
        script_args = [
            arg
            for arg in argv[1:]
            if not arg.startswith("-")
            and ("/" in arg or Path(arg).suffix.lower() in {".py", ".sh"})
        ]
        if not script_args:
            return None
        if not any(
            resolve_candidate(arg, repo_root=repo_root, spec_dir=spec_dir)
            for arg in script_args
        ):
            return None
    elif command == "git":
        if len(argv) < 2 or argv[1] not in READ_ONLY_GIT_SUBCOMMANDS:
            return None
    elif command == "dotnet":
        if len(argv) < 2 or argv[1] != "test":
            return None
    elif command in {"grep", "rg"}:
        if len(argv) < 2:
            return None

    return tuple(argv)


def programmatic_check_commands(
    evidence: str, *, repo_root: Path, spec_dir: Path
) -> list[tuple[str, tuple[str, ...]]]:
    """Return explicit programmatic checks as `(label, argv)` pairs.

    Only backticked invocations are checks. A backticked or plain file path,
    including `.py`, `.sh`, or `.cs`, is a source citation unless it includes
    an explicit command such as `python3 tools/check.py`.
    """
    checks: list[tuple[str, tuple[str, ...]]] = []
    seen: set[str] = set()
    for raw in BACKTICK_RE.findall(evidence):
        label = raw.strip()
        argv = _inline_programmatic_argv(
            label, repo_root=repo_root, spec_dir=spec_dir
        )
        if argv is None or label in seen:
            continue
        seen.add(label)
        checks.append((label, argv))
    return checks


def invalid_programmatic_check_labels(
    evidence: str, *, repo_root: Path, spec_dir: Path
) -> list[str]:
    """Return command-looking backtick spans that cannot be safely executed whole."""
    invalid: list[str] = []
    for raw in BACKTICK_RE.findall(evidence):
        label = raw.strip()
        argv = _shell_aware_argv(label)
        if not argv or argv[0] not in PROGRAM_COMMANDS:
            continue
        if _inline_programmatic_argv(label, repo_root=repo_root, spec_dir=spec_dir) is None:
            invalid.append(label)
    return list(dict.fromkeys(invalid))


def has_named_programmatic_check(evidence: str, *, repo_root: Path, spec_dir: Path) -> bool:
    return bool(
        programmatic_check_commands(
            evidence, repo_root=repo_root, spec_dir=spec_dir
        )
    )


def describe(findings: list[Finding]) -> str:
    blocks = sum(f.blocking for f in findings)
    notes = len(findings) - blocks
    return f"{blocks} blocking, {notes} survey/note"
