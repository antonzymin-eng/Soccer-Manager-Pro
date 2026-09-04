#!/usr/bin/env python3
"""Emit spec directories that transition into APPROVED between two Git trees.

Routine Testing Strategy auditors remain survey-only. This detector gives PR CI
a narrow, mechanical signal for the one event where FR-TS-042/052 require those
auditors to become blocking: a spec section changing from non-approved/missing to
an approved status.
"""

from __future__ import annotations

import argparse
from pathlib import Path
import subprocess

from testing_strategy_audit import infer_status, is_approved_status


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


def main() -> int:
    args = parse_args()
    repo = args.repo_root.resolve()

    git(repo, "cat-file", "-e", f"{args.base}^{{commit}}")
    git(repo, "cat-file", "-e", f"{args.head}^{{commit}}")

    changed = git(
        repo,
        "diff",
        "--name-only",
        "--diff-filter=AM",
        "--no-renames",
        args.base,
        args.head,
        "--",
        "docs/specs",
    ).stdout.splitlines()

    approval_dirs: set[str] = set()
    for path in changed:
        parts = Path(path).parts
        if len(parts) < 4 or parts[0:2] != ("docs", "specs"):
            continue
        if not parts[-1].startswith("section-") or not parts[-1].endswith(".md"):
            continue

        current = blob_text(repo, args.head, path)
        if current is None or not is_approved_status(infer_status(current)):
            continue
        previous = blob_text(repo, args.base, path)
        if previous is not None and is_approved_status(infer_status(previous)):
            continue

        approval_dirs.add(str(Path(*parts[:3])))

    for spec_dir in sorted(approval_dirs):
        print(spec_dir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
