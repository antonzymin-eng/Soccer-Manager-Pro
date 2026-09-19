#!/usr/bin/env python3
"""Verify PR #416 evidence-ref archival before disposable refs are deleted."""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from datetime import datetime, timezone
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

ARCHIVE_ROOT = Path("docs/tracking/evidence/pr416-ref-archive")
MANIFEST = ARCHIVE_ROOT / "MANIFEST.tsv"
DISPOSITION = ARCHIVE_ROOT / "ref-disposition.tsv"
RUN_HEADS = ARCHIVE_ROOT / "run-heads.tsv"
CITED_DOCS = (
    Path("docs/tracking/pr416-evidence-provenance.md"),
    Path("docs/tracking/w6-elevated-stationary-ball-fix.md"),
)

EXPECTED_MANIFEST_ROWS = 100
EXPECTED_DISPOSITION_ROWS = 24
EXPECTED_RUN_ROWS = 31
EXPECTED_DISPOSITIONS = {"deletable": 21, "retain": 0, "policy-retained": 3}


@dataclass(frozen=True)
class BlobInstance:
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


def normalize_git_date(value: str) -> str:
    parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    return (
        parsed.astimezone(timezone.utc)
        .isoformat(timespec="seconds")
        .replace("+00:00", "Z")
    )


def commit_meta(repo: Path, sha: str) -> tuple[str, str]:
    output = git(repo, "show", "-s", "--format=%s%x00%cI", sha).stdout.rstrip("\n")
    subject, date = output.split("\x00", 1)
    return subject, normalize_git_date(date)


def resolve(repo: Path, spec: str) -> str | None:
    result = git(repo, "rev-parse", "--verify", spec, check=False)
    return result.stdout.strip() if result.returncode == 0 else None


def remote_ref(branch: str) -> str:
    return f"refs/remotes/origin/{branch}"


def live_evidence_refs(repo: Path) -> set[str]:
    output = git(
        repo,
        "for-each-ref",
        "--format=%(refname)",
        "refs/remotes/origin/evidence/pr416-*",
    ).stdout
    prefix = "refs/remotes/origin/"
    return {
        line[len(prefix) :]
        for line in output.splitlines()
        if line.startswith(prefix)
    }


def changed_blob_instances(repo: Path, ref: str, main_ref: str) -> list[BlobInstance]:
    """Return every historical state of every path changed in the ref-exclusive history.

    This deliberately evaluates the full changed-path set at every exclusive commit,
    not only the path(s) modified by that individual commit. That makes intermediate
    tree states explicit and prevents a later commit from hiding an earlier state.
    """
    remote = remote_ref(ref)
    merge_base = git(repo, "merge-base", main_ref, remote).stdout.strip()
    commits = git(repo, "rev-list", "--reverse", f"{merge_base}..{remote}").stdout.splitlines()
    changed_paths: set[str] = set()

    for commit in commits:
        parent_line = git(repo, "rev-list", "--parents", "-n", "1", commit).stdout.split()
        if len(parent_line) <= 1:
            changed_paths.update(
                git(repo, "ls-tree", "-r", "--name-only", commit).stdout.splitlines()
            )
        else:
            changed_paths.update(
                git(
                    repo,
                    "diff",
                    "--no-renames",
                    "--name-only",
                    parent_line[1],
                    commit,
                ).stdout.splitlines()
            )

    instances: list[BlobInstance] = []
    for commit in commits:
        for path in sorted(changed_paths):
            blob = resolve(repo, f"{commit}:{path}")
            if blob is None:
                continue
            if git(repo, "cat-file", "-t", blob).stdout.strip() == "blob":
                instances.append(BlobInstance(ref, commit, path, blob))

    return instances


def reachable_blobs(repo: Path, refs: list[str]) -> set[str]:
    object_lines = git(repo, "rev-list", "--objects", *refs).stdout.splitlines()
    object_ids = [line.split(" ", 1)[0] for line in object_lines if line]
    if not object_ids:
        return set()

    result = subprocess.run(
        [
            "git",
            "-C",
            str(repo),
            "cat-file",
            "--batch-check=%(objectname) %(objecttype)",
        ],
        input="\n".join(object_ids) + "\n",
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise RuntimeError(f"git cat-file --batch-check failed: {result.stderr.strip()}")

    return {
        fields[0]
        for line in result.stdout.splitlines()
        if len(fields := line.split()) >= 2 and fields[1] == "blob"
    }



def verify_actions_metadata(runs: list[dict[str, str]]) -> list[str]:
    """Cross-check the durable run ledger against authoritative GitHub Actions metadata."""
    errors: list[str] = []
    token = os.environ.get("GITHUB_TOKEN", "")
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
    if not token or not repository:
        return ["Actions metadata verification requires GITHUB_TOKEN and GITHUB_REPOSITORY"]

    headers = {
        "Accept": "application/vnd.github+json",
        "Authorization": f"Bearer {token}",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "pr416-evidence-ref-gate",
    }
    fields = (
        ("head_branch", "head_branch"),
        ("head_sha", "head_sha"),
        ("event", "event"),
        ("conclusion", "conclusion"),
        ("workflow_name", "name"),
    )

    for row in runs:
        run_id = row["run_id"]
        request = urllib.request.Request(
            f"{api_url}/repos/{repository}/actions/runs/{run_id}",
            headers=headers,
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                payload = json.load(response)
        except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError) as exc:
            errors.append(f"run {run_id}: Actions API lookup failed: {exc}")
            continue

        for ledger_key, api_key in fields:
            expected = row.get(ledger_key, "")
            actual_value = payload.get(api_key)
            actual = "" if actual_value is None else str(actual_value)
            if actual != expected:
                errors.append(
                    f"run {run_id}: Actions {api_key} {actual!r} != ledger {expected!r}"
                )

    return errors

def cited_run_ids(repo: Path) -> set[str]:
    pattern = re.compile(r"\b35\d{9}\b")
    ids: set[str] = set()
    for rel in CITED_DOCS:
        ids.update(pattern.findall((repo / rel).read_text(encoding="utf-8")))
    return ids


def common_checks(
    repo: Path,
) -> tuple[
    list[str],
    list[dict[str, str]],
    list[dict[str, str]],
    list[dict[str, str]],
]:
    errors: list[str] = []
    manifest = read_tsv(repo / MANIFEST)
    dispositions = read_tsv(repo / DISPOSITION)
    runs = read_tsv(repo / RUN_HEADS)

    if len(manifest) != EXPECTED_MANIFEST_ROWS:
        errors.append(f"manifest rows {len(manifest)} != {EXPECTED_MANIFEST_ROWS}")
    if len(dispositions) != EXPECTED_DISPOSITION_ROWS:
        errors.append(f"disposition rows {len(dispositions)} != {EXPECTED_DISPOSITION_ROWS}")
    if len(runs) != EXPECTED_RUN_ROWS:
        errors.append(f"run-head rows {len(runs)} != {EXPECTED_RUN_ROWS}")

    counts: dict[str, int] = defaultdict(int)
    refs_seen: set[str] = set()
    for row in dispositions:
        ref = row.get("ref", "")
        if not ref or ref in refs_seen:
            errors.append(f"duplicate/empty disposition ref: {ref!r}")
        refs_seen.add(ref)
        counts[row.get("disposition", "")] += 1
        if row.get("delete_now") != "false":
            errors.append(f"{ref}: delete_now must remain false before deletion authorization")

    for disposition, expected in EXPECTED_DISPOSITIONS.items():
        if counts.get(disposition, 0) != expected:
            errors.append(
                f"disposition {disposition}: {counts.get(disposition, 0)} != {expected}"
            )
    unexpected = set(counts) - set(EXPECTED_DISPOSITIONS)
    if unexpected:
        errors.append(f"unexpected disposition values: {sorted(unexpected)}")

    archive_paths: set[str] = set()
    for row in manifest:
        archive_path = row.get("archive_path", "")
        blob = row.get("blob_sha", "")
        if not archive_path or archive_path in archive_paths:
            errors.append(f"duplicate/empty manifest archive_path: {archive_path!r}")
        archive_paths.add(archive_path)
        if not archive_path.endswith(".txt"):
            errors.append(f"snapshot path is not quarantined as .txt: {archive_path}")
        committed_blob = resolve(repo, f"HEAD:{archive_path}")
        if committed_blob != blob:
            errors.append(
                f"archive blob mismatch {archive_path}: {committed_blob} != {blob}"
            )
        if not row.get("commit_subject") or not row.get("commit_date"):
            errors.append(
                f"manifest commit metadata missing for {row.get('source_head', '')}"
            )

    tree_paths = git(
        repo, "ls-tree", "-r", "--name-only", "HEAD", ARCHIVE_ROOT.as_posix()
    ).stdout.splitlines()
    snapshot_paths = {
        path
        for path in tree_paths
        if path.endswith(".txt")
        and (
            f"{ARCHIVE_ROOT.as_posix()}/current/" in path
            or f"{ARCHIVE_ROOT.as_posix()}/runs/" in path
            or f"{ARCHIVE_ROOT.as_posix()}/history/" in path
        )
    }
    if snapshot_paths != archive_paths:
        errors.append(
            "archive/manifest path-set mismatch: "
            f"missing={sorted(archive_paths - snapshot_paths)} "
            f"extras={sorted(snapshot_paths - archive_paths)}"
        )

    run_ids = {row.get("run_id", "") for row in runs}
    cited = cited_run_ids(repo)
    if run_ids != cited:
        errors.append(
            f"run-head/citation mismatch: "
            f"missing={sorted(cited - run_ids)} extras={sorted(run_ids - cited)}"
        )

    for row in runs:
        if not row.get("commit_subject") or not row.get("commit_date"):
            errors.append(f"run {row.get('run_id', '')}: commit metadata missing")

    run_by_id = {row.get("run_id", ""): row for row in runs}
    for row in manifest:
        if row.get("kind") != "run":
            continue
        run_id = row.get("run_id", "")
        run_row = run_by_id.get(run_id)
        if run_row is None:
            errors.append(f"run snapshot {run_id}: no run-head row")
        elif run_row.get("head_sha") != row.get("source_head"):
            errors.append(
                f"run snapshot {run_id}: source_head {row.get('source_head')} "
                f"!= run head {run_row.get('head_sha')}"
            )

    return errors, manifest, dispositions, runs


def validate_archive(repo: Path) -> list[str]:
    errors, manifest, dispositions, runs = common_checks(repo)
    print(
        "Gate B archive integrity: "
        f"manifest={len(manifest)} dispositions={len(dispositions)} runs={len(runs)}"
    )
    return errors


def validate_live(
    repo: Path, main_ref: str, verify_actions_api: bool = False
) -> list[str]:
    errors, manifest, dispositions, runs = common_checks(repo)
    if errors:
        return errors

    expected_refs = {row["ref"] for row in dispositions}
    actual_refs = live_evidence_refs(repo)
    if actual_refs != expected_refs:
        errors.append(
            "live evidence-ref set mismatch: "
            f"missing={sorted(expected_refs - actual_refs)} "
            f"extras={sorted(actual_refs - expected_refs)}"
        )
        return errors

    disposition_by_ref = {row["ref"]: row for row in dispositions}

    for ref in sorted(expected_refs):
        remote = remote_ref(ref)
        actual_head = resolve(repo, remote)
        expected_head = disposition_by_ref[ref]["current_head"]
        if actual_head != expected_head:
            errors.append(f"{ref}: live head {actual_head} != recorded {expected_head}")
            continue

        merge_base = git(repo, "merge-base", main_ref, remote).stdout.strip()
        name_status = git(
            repo, "diff", "--name-status", merge_base, remote
        ).stdout.splitlines()

        tip_paths: set[str] = set()
        for line in name_status:
            fields = line.split("\t")
            status = fields[0]
            if status.startswith("D"):
                errors.append(
                    f"{ref}: tip diff contains unarchived deletion {fields[-1]}"
                )
                continue
            tip_paths.add(fields[-1])

        rows = [
            row
            for row in manifest
            if row.get("kind") == "current" and row.get("source_ref") == ref
        ]
        manifest_paths = {row["original_path"] for row in rows}
        if tip_paths != manifest_paths:
            errors.append(
                f"{ref}: tip path mismatch "
                f"missing={sorted(tip_paths - manifest_paths)} "
                f"extras={sorted(manifest_paths - tip_paths)}"
            )

        for row in rows:
            blob = resolve(repo, f"{remote}:{row['original_path']}")
            if blob != row["blob_sha"]:
                errors.append(
                    f"{ref}:{row['original_path']}: "
                    f"tip blob {blob} != archived {row['blob_sha']}"
                )

    for row in manifest:
        source_head = row["source_head"]
        source_blob = resolve(repo, f"{source_head}:{row['original_path']}")
        if source_blob != row["blob_sha"]:
            errors.append(
                f"manifest provenance {source_head}:{row['original_path']} "
                f"-> {source_blob}, expected {row['blob_sha']}"
            )
        try:
            subject, date = commit_meta(repo, source_head)
        except RuntimeError as exc:
            errors.append(str(exc))
            continue
        if subject != row["commit_subject"] or date != row["commit_date"]:
            errors.append(
                f"manifest commit metadata mismatch {source_head}: "
                f"{(subject, date)!r} != "
                f"{(row['commit_subject'], row['commit_date'])!r}"
            )

    for row in runs:
        sha = row["head_sha"]
        try:
            subject, date = commit_meta(repo, sha)
        except RuntimeError as exc:
            errors.append(str(exc))
            continue

        if subject != row["commit_subject"] or date != row["commit_date"]:
            errors.append(
                f"run {row['run_id']}: commit metadata "
                f"{(subject, date)!r} != "
                f"{(row['commit_subject'], row['commit_date'])!r}"
            )

        branch = row["head_branch"]
        if branch in expected_refs:
            ancestor = git(
                repo,
                "merge-base",
                "--is-ancestor",
                sha,
                remote_ref(branch),
                check=False,
            )
            if ancestor.returncode != 0:
                errors.append(
                    f"run {row['run_id']}: {sha} is not an ancestor of live {branch}"
                )
        elif branch == "main":
            ancestor = git(
                repo,
                "merge-base",
                "--is-ancestor",
                sha,
                main_ref,
                check=False,
            )
            if ancestor.returncode != 0:
                errors.append(
                    f"run {row['run_id']}: {sha} is not an ancestor of {main_ref}"
                )

    if verify_actions_api:
        errors.extend(verify_actions_metadata(runs))

    policy_refs = [
        remote_ref(row["ref"])
        for row in dispositions
        if row["disposition"] == "policy-retained"
    ]
    candidate_refs = [
        remote_ref(row["ref"])
        for row in dispositions
        if row["disposition"] == "deletable"
    ]
    durable_blobs = reachable_blobs(repo, [main_ref, *policy_refs])
    durable_blobs.update(row["blob_sha"] for row in manifest)

    candidate_reachable_blobs = reachable_blobs(repo, candidate_refs)
    delete_only_blobs = candidate_reachable_blobs - durable_blobs
    if delete_only_blobs:
        sample = sorted(delete_only_blobs)[:20]
        errors.append(
            "blob objects would become unreachable after deleting the 21 candidate refs: "
            f"count={len(delete_only_blobs)} sample={sample}"
        )

    all_instances: list[BlobInstance] = []
    candidate_instances: list[BlobInstance] = []
    uncovered: list[BlobInstance] = []

    for row in dispositions:
        ref = row["ref"]
        instances = changed_blob_instances(repo, ref, main_ref)
        all_instances.extend(instances)
        if row["disposition"] != "deletable":
            continue
        candidate_instances.extend(instances)
        for instance in instances:
            if instance.blob not in durable_blobs:
                uncovered.append(instance)

    for item in uncovered:
        errors.append(
            "would be lost on deletion: "
            f"{item.ref} {item.commit} {item.path} blob={item.blob}"
        )

    print(
        "Gate A live-ref history: "
        f"refs={len(dispositions)} "
        f"candidates={sum(row['disposition'] == 'deletable' for row in dispositions)} "
        f"policy={sum(row['disposition'] == 'policy-retained' for row in dispositions)} "
        f"branch_exclusive_blob_instances={len(all_instances)} "
        f"deletion_candidate_blob_instances={len(candidate_instances)} "
        f"delete_only_blob_objects={len(delete_only_blobs)} "
        f"uncovered={len(uncovered)}"
    )
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    parser.add_argument("--mode", choices=("live", "archive", "all"), default="all")
    parser.add_argument("--main-ref", default="refs/remotes/origin/main")
    parser.add_argument(
        "--verify-actions-api",
        action="store_true",
        help="cross-check run-heads.tsv against GitHub Actions API metadata",
    )
    args = parser.parse_args(argv)

    repo = Path(args.repo).resolve()
    errors: list[str] = []

    try:
        if args.mode in ("archive", "all"):
            errors.extend(validate_archive(repo))
        if args.mode in ("live", "all"):
            errors.extend(
                validate_live(repo, args.main_ref, args.verify_actions_api)
            )
    except (OSError, RuntimeError, ValueError, KeyError) as exc:
        errors.append(str(exc))

    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print("PR416 evidence-ref reconciliation: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
