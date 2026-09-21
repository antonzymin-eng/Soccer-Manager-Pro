#!/usr/bin/env python3
"""Fail-closed local Git ancestry check for branch cleanup decisions."""

from __future__ import annotations

import argparse
import subprocess
from pathlib import Path


class GitCheckError(RuntimeError):
    pass


def _git(repo: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
    )


def is_shallow(repo: Path) -> bool:
    completed = _git(repo, "rev-parse", "--is-shallow-repository")
    if completed.returncode != 0:
        raise GitCheckError(
            "could not determine repository depth: " + completed.stderr.strip()
        )
    value = completed.stdout.strip().lower()
    if value == "true":
        return True
    if value == "false":
        return False
    raise GitCheckError(f"unexpected --is-shallow-repository output: {value!r}")


def resolve_commit(repo: Path, ref: str) -> str:
    completed = _git(repo, "rev-parse", "--verify", f"{ref}^{{commit}}")
    if completed.returncode != 0:
        raise GitCheckError(f"could not resolve commit ref {ref!r}: {completed.stderr.strip()}")
    return completed.stdout.strip()


def check_ancestry(repo: Path, ancestor: str, descendant: str) -> tuple[bool, str, str]:
    if is_shallow(repo):
        raise GitCheckError(
            "repository history is shallow; local ancestry is not authoritative. "
            "Run 'git fetch --unshallow' (or otherwise obtain full history), or use an "
            "authoritative remote/API compare before making a branch deletion/reachability decision."
        )

    ancestor_sha = resolve_commit(repo, ancestor)
    descendant_sha = resolve_commit(repo, descendant)
    completed = _git(repo, "merge-base", "--is-ancestor", ancestor_sha, descendant_sha)
    if completed.returncode == 0:
        return True, ancestor_sha, descendant_sha
    if completed.returncode == 1:
        return False, ancestor_sha, descendant_sha
    raise GitCheckError(
        "git merge-base --is-ancestor failed: " + completed.stderr.strip()
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    parser.add_argument("--ancestor", required=True, help="candidate branch tip / ancestor ref")
    parser.add_argument("--descendant", required=True, help="target ref, normally main")
    args = parser.parse_args(argv)

    try:
        result, ancestor_sha, descendant_sha = check_ancestry(
            Path(args.repo).resolve(),
            args.ancestor,
            args.descendant,
        )
    except GitCheckError as exc:
        print(f"ERROR: {exc}")
        return 2

    if result:
        print(
            f"ANCESTOR: {args.ancestor} ({ancestor_sha}) is an ancestor of "
            f"{args.descendant} ({descendant_sha})"
        )
        return 0

    print(
        f"NOT_ANCESTOR: {args.ancestor} ({ancestor_sha}) is not an ancestor of "
        f"{args.descendant} ({descendant_sha})"
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
