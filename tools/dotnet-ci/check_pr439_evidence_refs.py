#!/usr/bin/env python3
"""Verify PR #439 evidence-ref archival before disposable refs are deleted."""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path

ARCHIVE_ROOT = Path("docs/tracking/evidence/pr439-ref-archive")
MANIFEST = ARCHIVE_ROOT / "MANIFEST.tsv"
REF_HEADS = ARCHIVE_ROOT / "ref-heads.tsv"
DISPOSITION = ARCHIVE_ROOT / "ref-disposition.tsv"
RUN_HEADS = ARCHIVE_ROOT / "run-heads.tsv"
PR_NUMBER = 439
EXPECTED_REF_COUNT = 16
EXPECTED_RUN_COUNT = 15
EXPECTED_BLOB_STATE_COUNT = 39


@dataclass(frozen=True)
class BlobState:
    ref: str
    commit: str
    path: str
    blob: str


def git(repo: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
    )
    if check and result.returncode != 0:
        raise RuntimeError(
            f"git {' '.join(args)} failed ({result.returncode}): {result.stderr.strip()}"
        )
    return result


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def resolve(repo: Path, spec: str) -> str | None:
    result = git(repo, "rev-parse", "--verify", spec, check=False)
    return result.stdout.strip() if result.returncode == 0 else None


def changed_blob_states(repo: Path, ref: str, main_ref: str) -> set[BlobState]:
    remote = f"refs/remotes/origin/{ref}"
    merge_base = git(repo, "merge-base", main_ref, remote).stdout.strip()
    commits = git(repo, "rev-list", "--reverse", f"{merge_base}..{remote}").stdout.splitlines()
    states: set[BlobState] = set()
    for commit in commits:
        paths = git(repo, "diff", "--no-renames", "--name-only", f"{commit}^", commit, "--").stdout.splitlines()
        for path in paths:
            blob = resolve(repo, f"{commit}:{path}")
            if blob:
                states.add(BlobState(ref, commit, path, blob))
    return states


def validate_archive(repo: Path) -> list[str]:
    errors: list[str] = []
    required = [MANIFEST, REF_HEADS, DISPOSITION, RUN_HEADS]
    for rel in required:
        if not (repo / rel).is_file():
            errors.append(f"missing archive ledger: {rel}")
    if errors:
        return errors

    manifest = read_tsv(repo / MANIFEST)
    refs = read_tsv(repo / REF_HEADS)
    dispositions = read_tsv(repo / DISPOSITION)
    runs = read_tsv(repo / RUN_HEADS)

    ref_names = {row["ref"] for row in refs}
    disposition_names = {row["ref"] for row in dispositions}
    if len(refs) != EXPECTED_REF_COUNT or ref_names != disposition_names:
        errors.append(
            f"ref ledger/disposition mismatch: refs={len(refs)} dispositions={len(dispositions)}"
        )
    head_by_ref = {row["ref"]: row["head_sha"] for row in refs}
    for row in dispositions:
        ref = row["ref"]
        if row.get("current_head") != head_by_ref.get(ref):
            errors.append(
                f"{ref}: disposition head {row.get('current_head')} != ref ledger {head_by_ref.get(ref)}"
            )
        if row.get("disposition") != "deletable":
            errors.append(f"{ref}: unexpected disposition {row.get('disposition')!r}")
        if row.get("delete_now") != "false":
            errors.append(f"{ref}: deletion is not authorized in this archive revision")

    run_ids = {row["run_id"] for row in runs}
    if len(runs) != EXPECTED_RUN_COUNT or len(run_ids) != len(runs):
        errors.append(
            f"run-head ledger cardinality/uniqueness failure: rows={len(runs)} unique={len(run_ids)}"
        )

    archive_paths: set[str] = set()
    if len(manifest) != EXPECTED_BLOB_STATE_COUNT:
        errors.append(
            f"manifest blob-state rows {len(manifest)} != {EXPECTED_BLOB_STATE_COUNT}"
        )

    manifest_refs = {row["source_ref"] for row in manifest}
    if manifest_refs != ref_names:
        errors.append(
            f"manifest ref coverage mismatch: missing={sorted(ref_names-manifest_refs)} "
            f"extras={sorted(manifest_refs-ref_names)}"
        )
    for row in manifest:
        path = row["archive_path"]
        if path in archive_paths:
            errors.append(f"duplicate archive path: {path}")
        archive_paths.add(path)
        if not path.endswith(".txt"):
            errors.append(f"snapshot is not quarantined as .txt: {path}")
        actual = resolve(repo, f"HEAD:{path}")
        if actual != row["blob_sha"]:
            errors.append(f"archive blob mismatch {path}: {actual} != {row['blob_sha']}")

    tree_paths = set(
        git(repo, "ls-tree", "-r", "--name-only", "HEAD", (ARCHIVE_ROOT / "history").as_posix())
        .stdout.splitlines()
    )
    snapshot_paths = {path for path in tree_paths if path.endswith(".txt")}
    if snapshot_paths != archive_paths:
        errors.append(
            "archive/manifest path-set mismatch: "
            f"missing={sorted(archive_paths-snapshot_paths)} extras={sorted(snapshot_paths-archive_paths)}"
        )

    return errors


def _github_json(url: str, token: str) -> object:
    request = urllib.request.Request(
        url,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "pr439-evidence-ref-gate",
        },
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def verify_actions_and_pr_citations(runs: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    token = os.environ.get("GITHUB_TOKEN", "")
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
    if not token or not repository:
        return ["Actions/citation verification requires GITHUB_TOKEN and GITHUB_REPOSITORY"]

    try:
        pr = _github_json(f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}", token)
        comments = _github_json(
            f"{api_url}/repos/{repository}/issues/{PR_NUMBER}/comments?per_page=100",
            token,
        )
        review_comments = _github_json(
            f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}/comments?per_page=100",
            token,
        )
        reviews = _github_json(
            f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}/reviews?per_page=100",
            token,
        )
    except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError) as exc:
        return [f"PR #{PR_NUMBER} citation lookup failed: {exc}"]

    pattern = re.compile(r"\b35\d{9}\b")
    bodies = [str(pr.get("body") or "")]
    bodies.extend(str(item.get("body") or "") for item in comments)
    bodies.extend(str(item.get("body") or "") for item in review_comments)
    bodies.extend(str(item.get("body") or "") for item in reviews)
    cited: set[str] = set()
    for body in bodies:
        cited.update(pattern.findall(body))

    ledger_ids = {row["run_id"] for row in runs}
    if cited != ledger_ids:
        errors.append(
            f"PR #{PR_NUMBER} run citation mismatch: "
            f"missing={sorted(cited-ledger_ids)} extras={sorted(ledger_ids-cited)}"
        )

    fields = (
        ("head_branch", "head_branch"),
        ("head_sha", "head_sha"),
        ("event", "event"),
        ("conclusion", "conclusion"),
        ("workflow_name", "name"),
    )
    for row in runs:
        run_id = row["run_id"]
        try:
            payload = _github_json(
                f"{api_url}/repos/{repository}/actions/runs/{run_id}", token
            )
        except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError) as exc:
            errors.append(f"run {run_id}: Actions lookup failed: {exc}")
            continue
        for ledger_key, api_key in fields:
            actual_value = payload.get(api_key)
            actual = "" if actual_value is None else str(actual_value)
            if actual != row.get(ledger_key, ""):
                errors.append(
                    f"run {run_id}: Actions {api_key} {actual!r} != ledger {row.get(ledger_key, '')!r}"
                )
    return errors


def validate_live(repo: Path, main_ref: str, verify_api: bool = False) -> list[str]:
    errors = validate_archive(repo)
    if errors:
        return errors

    manifest = read_tsv(repo / MANIFEST)
    refs = read_tsv(repo / REF_HEADS)
    expected_heads = {row["ref"]: row["head_sha"] for row in refs}
    manifest_states = {
        (row["source_ref"], row["source_commit"], row["original_path"], row["blob_sha"])
        for row in manifest
    }

    live = git(
        repo,
        "for-each-ref",
        "--format=%(refname) %(objectname)",
        "refs/remotes/origin/evidence/pr439-*",
    ).stdout.splitlines()
    actual: dict[str, str] = {}
    prefix = "refs/remotes/origin/"
    for line in live:
        refname, sha = line.split()
        actual[refname[len(prefix):]] = sha

    if set(actual) != set(expected_heads):
        errors.append(
            "live PR439 evidence-ref set mismatch: "
            f"missing={sorted(set(expected_heads)-set(actual))} "
            f"extras={sorted(set(actual)-set(expected_heads))}"
        )
        return errors

    for ref, expected in expected_heads.items():
        if actual[ref] != expected:
            errors.append(f"{ref}: live head {actual[ref]} != recorded {expected}")

    live_states: set[tuple[str, str, str, str]] = set()
    for ref in sorted(expected_heads):
        for state in changed_blob_states(repo, ref, main_ref):
            live_states.add((state.ref, state.commit, state.path, state.blob))

    missing = live_states - manifest_states
    extras = manifest_states - live_states
    if missing:
        for ref, commit, path, blob in sorted(missing)[:50]:
            errors.append(
                f"unarchived branch-exclusive blob state: {ref} {commit} {path} {blob}"
            )
        if len(missing) > 50:
            errors.append(f"... plus {len(missing)-50} additional missing states")
    if extras:
        for ref, commit, path, blob in sorted(extras)[:50]:
            errors.append(
                f"manifest state is not a live branch-exclusive change: {ref} {commit} {path} {blob}"
            )
        if len(extras) > 50:
            errors.append(f"... plus {len(extras)-50} additional extra states")

    if verify_api:
        errors.extend(verify_actions_and_pr_citations(read_tsv(repo / RUN_HEADS)))

    print(
        "PR439 live evidence census: "
        f"refs={len(expected_heads)} blob_states={len(live_states)} "
        f"archived={len(manifest_states)} runs={len(read_tsv(repo / RUN_HEADS))}"
    )
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".")
    parser.add_argument("--mode", choices=("archive", "live", "all"), default="archive")
    parser.add_argument("--main-ref", default="refs/remotes/origin/main")
    parser.add_argument("--verify-actions-api", action="store_true")
    args = parser.parse_args(argv)
    repo = Path(args.repo).resolve()
    errors: list[str] = []
    try:
        if args.mode in {"archive", "all"}:
            errors.extend(validate_archive(repo))
        if args.mode in {"live", "all"}:
            errors.extend(validate_live(repo, args.main_ref, args.verify_actions_api))
    except (OSError, RuntimeError, KeyError, ValueError) as exc:
        errors.append(str(exc))

    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    print("PR439 evidence-ref reconciliation: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
