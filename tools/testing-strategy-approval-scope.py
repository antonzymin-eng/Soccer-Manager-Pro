#!/usr/bin/env python3
"""Emit spec directories that transition into APPROVED between two Git trees.

Routine Testing Strategy auditors remain survey-only. This detector gives PR CI
a narrow, mechanical signal for the event where FR-TS-042/052 require those
auditors to become blocking. SPEC_INDEX.md is the repository's canonical status
authority, so the transition is derived from its base/head registry rows rather
than from presentation-specific status text inside individual section files.
Every emitted registry folder is validated as an existing direct child tree of
docs/specs in the head revision before it can become an enforcement scope.
"""

from __future__ import annotations

import argparse
from pathlib import Path
import re
import subprocess

from testing_strategy_audit import is_approved_status, registry_statuses_from_text

INDEX_PATH = "docs/specs/SPEC_INDEX.md"
FOLDER_RE = re.compile(r"^[A-Za-z0-9._-]+$")


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


def registry_at(repo: Path, rev: str) -> dict[str, str]:
    text = blob_text(repo, rev, INDEX_PATH)
    if text is None:
        raise SystemExit(f"cannot read canonical approval registry {rev}:{INDEX_PATH}")
    statuses = registry_statuses_from_text(text)
    if not statuses:
        raise SystemExit(f"cannot parse canonical approval registry {rev}:{INDEX_PATH}")
    return statuses


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


def main() -> int:
    args = parse_args()
    repo = args.repo_root.resolve()

    git(repo, "cat-file", "-e", f"{args.base}^{{commit}}")
    git(repo, "cat-file", "-e", f"{args.head}^{{commit}}")

    base_statuses = registry_at(repo, args.base)
    head_statuses = registry_at(repo, args.head)

    approval_dirs: set[str] = set()
    for folder, head_status in head_statuses.items():
        if not is_approved_status(head_status):
            continue
        if is_approved_status(base_statuses.get(folder)):
            continue
        approval_dirs.add(validated_spec_dir(repo, args.head, folder))

    for spec_dir in sorted(approval_dirs):
        print(spec_dir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
