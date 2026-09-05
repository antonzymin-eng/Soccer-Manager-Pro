#!/usr/bin/env python3
"""Emit spec directories that enter or re-enter APPROVED between two Git trees.

Routine Testing Strategy auditors remain survey-only. This detector gives PR CI
a narrow, mechanical signal for the event where FR-TS-042/052 require those
auditors to become blocking. SPEC_INDEX.md is the repository's canonical status
authority. A first approval is a non-approved/missing -> APPROVED status change;
a reapproval is an APPROVED row whose canonical Approved metadata changes while
remaining APPROVED. Every emitted registry folder is validated as an existing
direct child tree of docs/specs in the head revision.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
import re
import subprocess

from testing_strategy_audit import (
    is_approved_status,
    iter_tables,
    normalize_status,
)

INDEX_PATH = "docs/specs/SPEC_INDEX.md"
FOLDER_RE = re.compile(r"^[A-Za-z0-9._-]+$")


@dataclass(frozen=True)
class RegistryRecord:
    status: str
    approved: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--base", required=True)
    parser.add_argument("--head", default="HEAD")
    return parser.parse_args()


def git(repo: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    proc = subprocess.run(
        ["git", "-C", str(repo), *args],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if check and proc.returncode != 0:
        raise SystemExit(
            f"git {' '.join(args)} failed ({proc.returncode}): {proc.stderr.strip()}"
        )
    return proc


def blob_text(repo: Path, rev: str, path: str) -> str | None:
    proc = git(repo, "show", f"{rev}:{path}", check=False)
    if proc.returncode != 0:
        return None
    return proc.stdout


def registry_records_from_text(text: str) -> dict[str, RegistryRecord]:
    """Parse canonical folder/status/Approved metadata from SPEC_INDEX."""
    for headers, rows in iter_tables(text.splitlines()):
        lowered = [header.strip().lower() for header in headers]
        if "folder" not in lowered or "status" not in lowered or "approved" not in lowered:
            continue
        folder_i = lowered.index("folder")
        status_i = lowered.index("status")
        approved_i = lowered.index("approved")
        out: dict[str, RegistryRecord] = {}
        for _, cells in rows:
            if len(cells) <= max(folder_i, status_i, approved_i):
                continue
            folder = cells[folder_i].strip().strip("`").strip().rstrip("/")
            status = normalize_status(cells[status_i])
            approved = cells[approved_i].strip().strip("`").strip()
            if folder and status:
                out[folder] = RegistryRecord(status=status, approved=approved)
        if out:
            return out
    return {}


def registry_at(repo: Path, rev: str) -> dict[str, RegistryRecord]:
    text = blob_text(repo, rev, INDEX_PATH)
    if text is None:
        raise SystemExit(f"cannot read canonical approval registry {rev}:{INDEX_PATH}")
    records = registry_records_from_text(text)
    if not records:
        raise SystemExit(f"cannot parse canonical approval registry {rev}:{INDEX_PATH}")
    return records


def validated_spec_dir(repo: Path, head: str, folder: str) -> str:
    if not FOLDER_RE.fullmatch(folder) or folder in {".", ".."}:
        raise SystemExit(f"invalid canonical spec folder in {INDEX_PATH}: {folder!r}")
    rel = str(Path("docs") / "specs" / folder)
    kind = git(repo, "cat-file", "-t", f"{head}:{rel}", check=False)
    if kind.returncode != 0 or kind.stdout.strip() != "tree":
        raise SystemExit(
            f"canonical APPROVED spec folder does not resolve to a head-tree directory: {rel}"
        )
    return rel


def is_approval_event(
    base: RegistryRecord | None,
    head: RegistryRecord,
) -> bool:
    if not is_approved_status(head.status):
        return False
    if base is None or not is_approved_status(base.status):
        return True
    # A canonical row that remains APPROVED can still represent a fresh
    # amendment approval. The Approved metadata is the registry's explicit
    # sign-off marker; changing it while status remains APPROVED is reapproval.
    return head.approved != base.approved


def main() -> int:
    args = parse_args()
    repo = args.repo_root.resolve()

    git(repo, "cat-file", "-e", f"{args.base}^{{commit}}")
    git(repo, "cat-file", "-e", f"{args.head}^{{commit}}")

    base_records = registry_at(repo, args.base)
    head_records = registry_at(repo, args.head)

    approval_dirs: set[str] = set()
    for folder, head_record in head_records.items():
        if not is_approval_event(base_records.get(folder), head_record):
            continue
        approval_dirs.add(validated_spec_dir(repo, args.head, folder))

    for spec_dir in sorted(approval_dirs):
        print(spec_dir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
