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
EXPECTED_KIND_COUNTS = {"current": 72, "run": 13, "history": 15}
EXPECTED_POLICY_REFS = {
    "evidence/pr416-close-chance-retirement",
    "evidence/pr416-narrow-rolling-candidate",
    "evidence/pr416-state-only-preforce-candidate",
}
EXPECTED_BRANCH_EXCLUSIVE_BLOB_INSTANCES = 266
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
    """Return branch-differing blob states at every ref-exclusive commit.

    Each exclusive commit is compared directly to the branch merge base. A path
    therefore contributes an instance exactly while its blob state differs from
    the merge-base tree. This retains intermediate branch-only states without
    counting paths before they change or after they revert to the base state.
    """
    remote = remote_ref(ref)
    merge_base = git(repo, "merge-base", main_ref, remote).stdout.strip()
    commits = git(repo, "rev-list", "--reverse", f"{merge_base}..{remote}").stdout.splitlines()

    instances: list[BlobInstance] = []
    for commit in commits:
        paths = git(
            repo,
            "diff",
            "--no-renames",
            "--name-only",
            merge_base,
            commit,
            "--",
        ).stdout.splitlines()
        for path in sorted(set(paths)):
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

    kind_counts: dict[str, int] = defaultdict(int)
    for row in manifest:
        kind_counts[row.get("kind", "")] += 1
    for kind, expected in EXPECTED_KIND_COUNTS.items():
        if kind_counts.get(kind, 0) != expected:
            errors.append(
                f"manifest kind {kind}: {kind_counts.get(kind, 0)} != {expected}"
            )
    unexpected_kinds = set(kind_counts) - set(EXPECTED_KIND_COUNTS)
    if unexpected_kinds:
        errors.append(f"unexpected manifest kind values: {sorted(unexpected_kinds)}")

    counts: dict[str, int] = defaultdict(int)
    refs_seen: set[str] = set()
    for row in dispositions:
        ref = row.get("ref", "")
        if not ref or ref in refs_seen:
            errors.append(f"duplicate/empty disposition ref: {ref!r}")
        refs_seen.add(ref)
        disposition = row.get("disposition", "")
        counts[disposition] += 1
        delete_now = row.get("delete_now", "")
        if disposition == "deletable":
            if delete_now not in {"false", "true"}:
                errors.append(
                    f"{ref}: deletable delete_now must be exactly true or false"
                )
        elif delete_now != "false":
            errors.append(
                f"{ref}: only refs classified deletable may be deletion-authorized"
            )

    for disposition, expected in EXPECTED_DISPOSITIONS.items():
        if counts.get(disposition, 0) != expected:
            errors.append(
                f"disposition {disposition}: {counts.get(disposition, 0)} != {expected}"
            )
    unexpected = set(counts) - set(EXPECTED_DISPOSITIONS)
    if unexpected:
        errors.append(f"unexpected disposition values: {sorted(unexpected)}")

    deletable_delete_states = {
        row.get("delete_now", "")
        for row in dispositions
        if row.get("disposition") == "deletable"
    }
    if deletable_delete_states != {"false"} and deletable_delete_states != {"true"}:
        errors.append(
            "deletable refs must transition delete_now atomically; "
            f"states={sorted(deletable_delete_states)}"
        )

    policy_refs = {
        row["ref"]
        for row in dispositions
        if row.get("disposition") == "policy-retained"
    }
    if policy_refs != EXPECTED_POLICY_REFS:
        errors.append(
            f"policy-retained refs {sorted(policy_refs)} != "
            f"{sorted(EXPECTED_POLICY_REFS)}"
        )

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
        else:
            if run_row.get("head_sha") != row.get("source_head"):
                errors.append(
                    f"run snapshot {run_id}: source_head {row.get('source_head')} "
                    f"!= run head {run_row.get('head_sha')}"
                )
            if run_row.get("head_branch") != row.get("source_ref"):
                errors.append(
                    f"run snapshot {run_id}: source_ref {row.get('source_ref')} "
                    f"!= run branch {run_row.get('head_branch')}"
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
    repo: Path,
    main_ref: str,
    verify_actions_api: bool = False,
    require_authorized: bool = False,
    live_state: str = "auto",
) -> list[str]:
    errors, manifest, dispositions, runs = common_checks(repo)
    if errors:
        return errors

    all_refs = {row["ref"] for row in dispositions}
    policy_ref_names = {
        row["ref"] for row in dispositions if row["disposition"] == "policy-retained"
    }
    deletable_rows = [
        row for row in dispositions if row["disposition"] == "deletable"
    ]
    authorized = bool(deletable_rows) and all(
        row.get("delete_now") == "true" for row in deletable_rows
    )
    if require_authorized and not authorized:
        errors.append(
            "deletion authorization required: all 21 deletable refs must have delete_now=true"
        )
        return errors

    actual_refs = live_evidence_refs(repo)
    if live_state == "pre-delete":
        expected_live_refs = all_refs
        resolved_state = "pre-delete"
    elif live_state == "post-delete":
        if not authorized:
            errors.append("post-delete state requires all 21 deletable refs to be authorized")
            return errors
        expected_live_refs = policy_ref_names
        resolved_state = "post-delete"
    elif actual_refs == all_refs:
        expected_live_refs = all_refs
        resolved_state = "pre-delete"
    elif authorized and actual_refs == policy_ref_names:
        expected_live_refs = policy_ref_names
        resolved_state = "post-delete"
    else:
        allowed = [sorted(all_refs)]
        if authorized:
            allowed.append(sorted(policy_ref_names))
        errors.append(
            "live evidence-ref set mismatch: "
            f"actual={sorted(actual_refs)} allowed={allowed}"
        )
        return errors

    if actual_refs != expected_live_refs:
        errors.append(
            f"{resolved_state} evidence-ref set mismatch: "
            f"missing={sorted(expected_live_refs - actual_refs)} "
            f"extras={sorted(actual_refs - expected_live_refs)}"
        )
        return errors

    disposition_by_ref = {row["ref"]: row for row in dispositions}

    for ref in sorted(actual_refs):
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

    if resolved_state == "post-delete":
        if verify_actions_api:
            errors.extend(verify_actions_metadata(runs))
        print(
            "Gate A post-delete state: "
            f"refs={len(actual_refs)} deleted={len(all_refs - actual_refs)} "
            f"policy={len(policy_ref_names)} authorized={authorized}"
        )
        return errors

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
        if branch in all_refs:
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

    policy_refs = [remote_ref(ref) for ref in sorted(policy_ref_names)]
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

    if len(all_instances) != EXPECTED_BRANCH_EXCLUSIVE_BLOB_INSTANCES:
        errors.append(
            "branch-exclusive blob-instance census drift: "
            f"{len(all_instances)} != {EXPECTED_BRANCH_EXCLUSIVE_BLOB_INSTANCES}"
        )

    covered_instances = sum(
        1 for instance in all_instances if instance.blob in durable_blobs
    )
    history_violations = len(uncovered) + len(delete_only_blobs)

    print(
        "Gate A pre-delete live-ref history: "
        f"refs={len(dispositions)} "
        f"candidates={sum(row['disposition'] == 'deletable' for row in dispositions)} "
        f"policy={sum(row['disposition'] == 'policy-retained' for row in dispositions)} "
        f"branch_exclusive_blob_instances={len(all_instances)} "
        f"covered={covered_instances} "
        f"deletion_candidate_blob_instances={len(candidate_instances)} "
        f"delete_only_blob_objects={len(delete_only_blobs)} "
        f"uncovered={len(uncovered)} violations={history_violations}"
    )
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    parser.add_argument("--mode", choices=("live", "archive", "all"), default="all")
    parser.add_argument("--main-ref", default="refs/remotes/origin/main")
    parser.add_argument(
        "--live-state",
        choices=("auto", "pre-delete", "post-delete"),
        default="auto",
        help="expected live evidence-ref topology; auto accepts only complete pre- or post-delete states",
    )
    parser.add_argument(
        "--require-authorized",
        action="store_true",
        help="require all 21 deletable rows to carry delete_now=true",
    )
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
                validate_live(
                    repo,
                    args.main_ref,
                    args.verify_actions_api,
                    args.require_authorized,
                    args.live_state,
                )
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
