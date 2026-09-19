#!/usr/bin/env python3
"""Mechanical Step 5 verification for PR #416 evidence-ref cleanup.

Gate A requires the live evidence refs and proves full-history deletion completeness.
Gate B uses only the durable mainline archive and proves archive integrity and
interpretive reconstruction. The two gates deliberately prove different things.
"""

from __future__ import annotations

import argparse
import csv
import io
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath
from typing import Iterable

ARCHIVE_ROOT = "docs/tracking/evidence/pr416-ref-archive"
MANIFEST_PATH = f"{ARCHIVE_ROOT}/MANIFEST.tsv"
DISPOSITION_PATH = f"{ARCHIVE_ROOT}/ref-disposition.tsv"
RUN_HEADS_PATH = f"{ARCHIVE_ROOT}/run-heads.tsv"
README_PATH = f"{ARCHIVE_ROOT}/README.md"
PROVENANCE_DOC = "docs/tracking/pr416-evidence-provenance.md"
DIAGNOSIS_DOC = "docs/tracking/w6-elevated-stationary-ball-fix.md"

EXPECTED_MANIFEST_FIELDS = [
    "kind",
    "source_ref",
    "source_head",
    "commit_subject",
    "commit_date",
    "run_id",
    "original_path",
    "archive_path",
    "blob_sha",
]
EXPECTED_DISPOSITION_FIELDS = [
    "ref",
    "current_head",
    "archived_current_files",
    "archived_history_files",
    "disposition",
    "delete_now",
    "precondition_or_reason",
]
EXPECTED_RUN_HEAD_FIELDS = [
    "run_id",
    "head_branch",
    "head_sha",
    "commit_subject",
    "commit_date",
    "event",
    "conclusion",
    "workflow_name",
]
EXPECTED_KIND_COUNTS = {"current": 72, "run": 13, "history": 15}
EXPECTED_DISPOSITION_COUNTS = {"deletable": 21, "retain": 0, "policy-retained": 3}
EXPECTED_POLICY_REFS = {
    "evidence/pr416-close-chance-retirement",
    "evidence/pr416-narrow-rolling-candidate",
    "evidence/pr416-state-only-preforce-candidate",
}
EXPECTED_MANIFEST_ROWS = 100
EXPECTED_DISPOSITION_ROWS = 24
EXPECTED_RUN_ROWS = 31
RUN_ID_RE = re.compile(r"\b\d{11}\b")


class CheckError(RuntimeError):
    pass


@dataclass(frozen=True)
class BlobInstance:
    ref: str
    commit: str
    path: str
    blob: str


def _run(repo: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    completed = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
    )
    if check and completed.returncode != 0:
        raise CheckError(
            f"git {' '.join(args)} failed ({completed.returncode}): "
            f"{completed.stderr.strip()}"
        )
    return completed


def git_text(repo: Path, *args: str) -> str:
    return _run(repo, *args).stdout


def git_object_exists(repo: Path, spec: str) -> bool:
    return _run(repo, "cat-file", "-e", spec, check=False).returncode == 0


def show_text(repo: Path, ref: str, path: str) -> str:
    return git_text(repo, "show", f"{ref}:{path}")


def parse_tsv(text: str, expected_fields: list[str], label: str) -> list[dict[str, str]]:
    reader = csv.DictReader(io.StringIO(text), delimiter="\t")
    if reader.fieldnames != expected_fields:
        raise CheckError(f"{label}: fields {reader.fieldnames!r} != expected {expected_fields!r}")
    rows = list(reader)
    for index, row in enumerate(rows, start=2):
        if None in row:
            raise CheckError(f"{label}:{index}: extra TSV fields")
        missing = [field for field in expected_fields if row.get(field) is None]
        if missing:
            raise CheckError(f"{label}:{index}: missing fields {missing}")
    return rows


def blob_at(repo: Path, ref: str, path: str) -> str | None:
    completed = _run(repo, "ls-tree", ref, "--", path, check=False)
    if completed.returncode != 0:
        raise CheckError(f"cannot inspect {ref}:{path}: {completed.stderr.strip()}")
    line = completed.stdout.strip()
    if not line:
        return None
    entries = line.splitlines()
    if len(entries) != 1:
        raise CheckError(f"{ref}:{path}: expected one ls-tree row, got {len(entries)}")
    meta, listed_path = entries[0].split("\t", 1)
    mode, obj_type, sha = meta.split()
    del mode
    if listed_path != path:
        raise CheckError(f"{ref}:{path}: ls-tree returned unexpected path {listed_path!r}")
    if obj_type != "blob":
        raise CheckError(f"{ref}:{path}: expected blob, got {obj_type}")
    return sha


def normalize_time(value: str) -> datetime:
    raw = value.strip()
    if raw.endswith("Z"):
        raw = raw[:-1] + "+00:00"
    dt = datetime.fromisoformat(raw)
    if dt.tzinfo is None:
        raise CheckError(f"timestamp lacks timezone: {value!r}")
    return dt.astimezone(timezone.utc)


def commit_subject_date(repo: Path, sha: str) -> tuple[str, datetime]:
    if not git_object_exists(repo, f"{sha}^{{commit}}"):
        raise CheckError(f"missing commit object {sha}")
    subject = git_text(repo, "show", "-s", "--format=%s", sha).rstrip("\n")
    date = git_text(repo, "show", "-s", "--format=%cI", sha).strip()
    return subject, normalize_time(date)


def load_main_ledgers(
    repo: Path, main_ref: str
) -> tuple[list[dict[str, str]], list[dict[str, str]], list[dict[str, str]]]:
    manifest = parse_tsv(
        show_text(repo, main_ref, MANIFEST_PATH), EXPECTED_MANIFEST_FIELDS, MANIFEST_PATH
    )
    disposition = parse_tsv(
        show_text(repo, main_ref, DISPOSITION_PATH),
        EXPECTED_DISPOSITION_FIELDS,
        DISPOSITION_PATH,
    )
    run_heads = parse_tsv(
        show_text(repo, main_ref, RUN_HEADS_PATH),
        EXPECTED_RUN_HEAD_FIELDS,
        RUN_HEADS_PATH,
    )
    return manifest, disposition, run_heads


def _validate_archive_path(path: str) -> None:
    p = PurePosixPath(path)
    if p.is_absolute() or ".." in p.parts:
        raise CheckError(f"unsafe archive path: {path}")
    if not path.startswith(ARCHIVE_ROOT + "/") or not path.endswith(".txt"):
        raise CheckError(f"invalid archive path shape: {path}")


def normalized_disposition_counts(
    rows: list[dict[str, str]],
) -> tuple[dict[str, int], list[str]]:
    counts = Counter(row["disposition"] for row in rows)
    normalized = {
        key: counts.get(key, 0) for key in EXPECTED_DISPOSITION_COUNTS
    }
    unknown = sorted(set(counts) - set(EXPECTED_DISPOSITION_COUNTS))
    return normalized, unknown


def cited_run_ids(repo: Path, main_ref: str) -> set[str]:
    ids: set[str] = set()
    for path in (PROVENANCE_DOC, DIAGNOSIS_DOC):
        text = show_text(repo, main_ref, path)
        ids.update(RUN_ID_RE.findall(text))
    return ids


def gate_b(
    repo: Path, main_ref: str
) -> tuple[list[dict[str, str]], list[dict[str, str]], list[dict[str, str]]]:
    errors: list[str] = []
    try:
        manifest, disposition, run_heads = load_main_ledgers(repo, main_ref)
    except CheckError as exc:
        raise CheckError(f"Gate B ledger load failed: {exc}") from exc

    if len(manifest) != EXPECTED_MANIFEST_ROWS:
        errors.append(f"manifest rows={len(manifest)} expected={EXPECTED_MANIFEST_ROWS}")
    kind_counts = Counter(row["kind"] for row in manifest)
    if dict(kind_counts) != EXPECTED_KIND_COUNTS:
        errors.append(
            f"manifest kind counts={dict(kind_counts)} expected={EXPECTED_KIND_COUNTS}"
        )

    archive_paths = [row["archive_path"] for row in manifest]
    if len(set(archive_paths)) != len(archive_paths):
        errors.append("manifest archive_path values are not unique")

    for row in manifest:
        try:
            _validate_archive_path(row["archive_path"])
            actual = blob_at(repo, main_ref, row["archive_path"])
            if actual != row["blob_sha"]:
                errors.append(
                    f"archive blob mismatch {row['archive_path']}: "
                    f"{actual} != {row['blob_sha']}"
                )
            if not row["commit_subject"] or not row["commit_date"]:
                errors.append(f"manifest metadata missing for {row['archive_path']}")
            else:
                normalize_time(row["commit_date"])
        except (CheckError, ValueError) as exc:
            errors.append(str(exc))

    if len(disposition) != EXPECTED_DISPOSITION_ROWS:
        errors.append(
            f"disposition rows={len(disposition)} expected={EXPECTED_DISPOSITION_ROWS}"
        )
    disposition_counts = Counter(row["disposition"] for row in disposition)
    normalized_disposition_counts, unknown_dispositions = normalized_disposition_counts(
        disposition
    )
    if normalized_disposition_counts != EXPECTED_DISPOSITION_COUNTS or unknown_dispositions:
        errors.append(
            f"disposition counts={normalized_disposition_counts} "
            f"unknown={unknown_dispositions} expected={EXPECTED_DISPOSITION_COUNTS}"
        )
    policy_refs = {
        row["ref"] for row in disposition if row["disposition"] == "policy-retained"
    }
    if policy_refs != EXPECTED_POLICY_REFS:
        errors.append(
            f"policy-retained refs={sorted(policy_refs)} "
            f"expected={sorted(EXPECTED_POLICY_REFS)}"
        )
    for row in disposition:
        if row["delete_now"] != "false":
            errors.append(
                f"{row['ref']}: delete_now must remain false before authorization"
            )
        if row["disposition"] not in EXPECTED_DISPOSITION_COUNTS:
            errors.append(
                f"{row['ref']}: unknown disposition {row['disposition']!r}"
            )

    if len(run_heads) != EXPECTED_RUN_ROWS:
        errors.append(f"run-head rows={len(run_heads)} expected={EXPECTED_RUN_ROWS}")
    run_ids = [row["run_id"] for row in run_heads]
    if len(set(run_ids)) != len(run_ids):
        errors.append("run-head run_id values are not unique")
    for row in run_heads:
        if not row["commit_subject"] or not row["commit_date"]:
            errors.append(f"run {row['run_id']}: missing preserved commit metadata")
        else:
            try:
                normalize_time(row["commit_date"])
            except (CheckError, ValueError) as exc:
                errors.append(f"run {row['run_id']}: {exc}")

    try:
        cited = cited_run_ids(repo, main_ref)
        ledger = set(run_ids)
        if cited != ledger:
            errors.append(
                f"cited run set differs from run-head ledger: "
                f"missing={sorted(cited-ledger)} extra={sorted(ledger-cited)}"
            )
    except CheckError as exc:
        errors.append(str(exc))

    run_by_id = {row["run_id"]: row for row in run_heads}
    for row in manifest:
        if row["kind"] == "run":
            run = run_by_id.get(row["run_id"])
            if run is None:
                errors.append(
                    f"run snapshot {row['archive_path']}: "
                    f"missing run-head row {row['run_id']}"
                )
                continue
            if row["source_head"] != run["head_sha"]:
                errors.append(
                    f"run snapshot {row['archive_path']}: "
                    f"source_head {row['source_head']} != ledger head_sha {run['head_sha']}"
                )
            if row["source_ref"] != run["head_branch"]:
                errors.append(
                    f"run snapshot {row['archive_path']}: "
                    f"source_ref {row['source_ref']} != ledger head_branch {run['head_branch']}"
                )

    for path in (README_PATH, PROVENANCE_DOC, DIAGNOSIS_DOC):
        if blob_at(repo, main_ref, path) is None:
            errors.append(f"durable reconstruction document missing: {path}")

    if errors:
        for error in errors:
            print(f"GATE_B_ERROR: {error}")
        raise CheckError(f"Gate B failed with {len(errors)} violation(s)")

    print(
        "PR416 Step 5 Gate B: PASS "
        f"manifest={len(manifest)} current={kind_counts['current']} "
        f"run={kind_counts['run']} history={kind_counts['history']} disposition="
        f"{disposition_counts['deletable']}/{disposition_counts['retain']}/"
        f"{disposition_counts['policy-retained']} cited_runs={len(run_heads)} "
        "violations=0"
    )
    return manifest, disposition, run_heads


def remote_ref(branch: str) -> str:
    return f"refs/remotes/origin/{branch}"


def branch_exclusive_commits(repo: Path, ref: str, main_ref: str) -> list[str]:
    text = git_text(repo, "rev-list", "--reverse", ref, "--not", main_ref)
    return [line for line in text.splitlines() if line]


def changed_paths(repo: Path, commit: str) -> list[str]:
    parents = git_text(repo, "show", "-s", "--format=%P", commit).strip().split()
    paths: set[str] = set()
    if not parents:
        out = git_text(
            repo, "diff-tree", "--root", "--no-commit-id", "--name-only", "-r", commit
        )
        paths.update(line for line in out.splitlines() if line)
    else:
        for parent in parents:
            out = git_text(
                repo, "diff", "--name-only", "--no-renames", parent, commit, "--"
            )
            paths.update(line for line in out.splitlines() if line)
    return sorted(paths)


def enumerate_instances(
    repo: Path, branch: str, ref: str, main_ref: str
) -> tuple[str, list[str], list[BlobInstance]]:
    merge_base = git_text(repo, "merge-base", main_ref, ref).strip()
    if not merge_base:
        raise CheckError(f"{branch}: no merge base with {main_ref}")
    commits = branch_exclusive_commits(repo, ref, main_ref)
    instances: list[BlobInstance] = []
    for commit in commits:
        if not git_object_exists(repo, f"{commit}^{{commit}}"):
            raise CheckError(f"{branch}: missing branch-exclusive commit {commit}")
        for path in changed_paths(repo, commit):
            blob = blob_at(repo, commit, path)
            if blob is not None:
                instances.append(BlobInstance(branch, commit, path, blob))
    return merge_base, commits, instances


def reachable_objects(repo: Path, refs: Iterable[str]) -> set[str]:
    refs = list(refs)
    if not refs:
        return set()
    text = git_text(repo, "rev-list", "--objects", *refs)
    return {line.split(" ", 1)[0] for line in text.splitlines() if line}


def validate_manifest_live_sources(
    repo: Path, manifest: list[dict[str, str]]
) -> list[str]:
    errors: list[str] = []
    commit_meta: dict[str, tuple[str, datetime]] = {}
    for row in manifest:
        sha = row["source_head"]
        if not git_object_exists(repo, f"{sha}^{{commit}}"):
            errors.append(
                f"manifest source commit missing: {sha} ({row['archive_path']})"
            )
            continue
        actual_blob = blob_at(repo, sha, row["original_path"])
        if actual_blob != row["blob_sha"]:
            errors.append(
                f"manifest source mismatch {sha}:{row['original_path']}: "
                f"{actual_blob} != {row['blob_sha']}"
            )
        try:
            if sha not in commit_meta:
                commit_meta[sha] = commit_subject_date(repo, sha)
            subject, date = commit_meta[sha]
            if subject != row["commit_subject"]:
                errors.append(
                    f"manifest subject mismatch {sha}: "
                    f"{row['commit_subject']!r} != {subject!r}"
                )
            if normalize_time(row["commit_date"]) != date:
                errors.append(
                    f"manifest commit_date mismatch {sha}: {row['commit_date']}"
                )
        except (CheckError, ValueError) as exc:
            errors.append(str(exc))
    return errors


def validate_run_commit_metadata(
    repo: Path, run_heads: list[dict[str, str]]
) -> list[str]:
    errors: list[str] = []
    cache: dict[str, tuple[str, datetime]] = {}
    for row in run_heads:
        sha = row["head_sha"]
        try:
            if sha not in cache:
                cache[sha] = commit_subject_date(repo, sha)
            subject, date = cache[sha]
            if subject != row["commit_subject"]:
                errors.append(f"run {row['run_id']}: subject mismatch for {sha}")
            if normalize_time(row["commit_date"]) != date:
                errors.append(f"run {row['run_id']}: commit_date mismatch for {sha}")
        except (CheckError, ValueError) as exc:
            errors.append(f"run {row['run_id']}: {exc}")
    return errors


def github_json(url: str, token: str) -> dict[str, object]:
    request = urllib.request.Request(
        url,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "soccer-manager-pro-pr416-step5-checker",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read().decode("utf-8"))
    except (
        urllib.error.URLError,
        urllib.error.HTTPError,
        TimeoutError,
        json.JSONDecodeError,
    ) as exc:
        raise CheckError(f"GitHub API request failed for {url}: {exc}") from exc


def validate_runs_online(
    run_heads: list[dict[str, str]], repository: str, token: str
) -> list[str]:
    errors: list[str] = []
    for row in run_heads:
        run_id = row["run_id"]
        try:
            data = github_json(
                f"https://api.github.com/repos/{repository}/actions/runs/{run_id}", token
            )
        except CheckError as exc:
            errors.append(str(exc))
            continue
        checks = {
            "head_branch": data.get("head_branch"),
            "head_sha": data.get("head_sha"),
            "event": data.get("event"),
            "conclusion": data.get("conclusion"),
            "workflow_name": data.get("name"),
        }
        for field, actual in checks.items():
            expected = row[field]
            if actual != expected:
                errors.append(
                    f"run {run_id}: {field} API={actual!r} ledger={expected!r}"
                )
    return errors


def gate_a(
    repo: Path,
    main_ref: str,
    manifest: list[dict[str, str]],
    disposition: list[dict[str, str]],
    run_heads: list[dict[str, str]],
    verify_runs_online: bool,
    expected_instances: int | None,
) -> None:
    errors: list[str] = []
    refs: dict[str, str] = {}
    disposition_by_ref = {row["ref"]: row for row in disposition}

    for row in disposition:
        branch = row["ref"]
        ref = remote_ref(branch)
        refs[branch] = ref
        if not git_object_exists(repo, f"{ref}^{{commit}}"):
            errors.append(f"missing live evidence ref {branch} ({ref})")
            continue
        tip = git_text(repo, "rev-parse", ref).strip()
        if tip != row["current_head"]:
            errors.append(
                f"{branch}: tip {tip} != disposition current_head {row['current_head']}"
            )

    errors.extend(validate_manifest_live_sources(repo, manifest))
    errors.extend(validate_run_commit_metadata(repo, run_heads))

    all_instances: list[BlobInstance] = []
    current_rows: dict[str, dict[str, str]] = defaultdict(dict)
    current_source_heads: dict[str, set[str]] = defaultdict(set)
    for row in manifest:
        if row["kind"] != "current":
            continue
        if row["original_path"] in current_rows[row["source_ref"]]:
            errors.append(
                f"duplicate current manifest path "
                f"{row['source_ref']}:{row['original_path']}"
            )
        current_rows[row["source_ref"]][row["original_path"]] = row["blob_sha"]
        current_source_heads[row["source_ref"]].add(row["source_head"])

    for branch, ref in refs.items():
        if not git_object_exists(repo, f"{ref}^{{commit}}"):
            continue
        try:
            merge_base, commits, instances = enumerate_instances(
                repo, branch, ref, main_ref
            )
            all_instances.extend(instances)
        except CheckError as exc:
            errors.append(str(exc))
            continue

        tip = git_text(repo, "rev-parse", ref).strip()
        diff_paths = [
            line
            for line in git_text(
                repo, "diff", "--name-only", merge_base, ref, "--"
            ).splitlines()
            if line
        ]
        tip_map: dict[str, str] = {}
        for path in diff_paths:
            blob = blob_at(repo, ref, path)
            if blob is None:
                errors.append(f"{branch}: tip directional diff contains deletion {path}")
            else:
                tip_map[path] = blob
        manifest_map = current_rows.get(branch, {})
        if tip_map != manifest_map:
            missing = sorted(set(tip_map) - set(manifest_map))
            extra = sorted(set(manifest_map) - set(tip_map))
            changed = sorted(
                path
                for path in set(tip_map) & set(manifest_map)
                if tip_map[path] != manifest_map[path]
            )
            errors.append(
                f"{branch}: current-tip manifest mismatch "
                f"missing={missing} extra={extra} changed={changed}"
            )
        if current_source_heads.get(branch, set()) != {tip}:
            errors.append(
                f"{branch}: current manifest source_head set="
                f"{sorted(current_source_heads.get(branch,set()))} expected={[tip]}"
            )

        row = disposition_by_ref.get(branch)
        if row is not None:
            try:
                declared_current = int(row["archived_current_files"])
                declared_history = int(row["archived_history_files"])
                actual_history = sum(
                    1
                    for item in manifest
                    if item["kind"] == "history" and item["source_ref"] == branch
                )
                if declared_current != len(manifest_map):
                    errors.append(
                        f"{branch}: archived_current_files={declared_current} "
                        f"actual={len(manifest_map)}"
                    )
                if declared_history != actual_history:
                    errors.append(
                        f"{branch}: archived_history_files={declared_history} "
                        f"actual={actual_history}"
                    )
            except ValueError:
                errors.append(f"{branch}: non-integer archived file count")
        if not commits:
            errors.append(
                f"{branch}: no branch-exclusive commits found from merge base {merge_base}"
            )

    policy_refspecs = [
        refs[branch] for branch in sorted(EXPECTED_POLICY_REFS) if branch in refs
    ]
    try:
        durable_objects = reachable_objects(repo, [main_ref, *policy_refspecs])
    except CheckError as exc:
        errors.append(str(exc))
        durable_objects = set()

    covered = 0
    for instance in all_instances:
        if instance.blob in durable_objects:
            covered += 1
        else:
            errors.append(
                f"uncovered branch-exclusive blob {instance.blob} "
                f"at {instance.ref} {instance.commit}:{instance.path}"
            )

    if expected_instances is not None and len(all_instances) != expected_instances:
        errors.append(
            f"branch-exclusive blob instances={len(all_instances)} "
            f"expected={expected_instances}"
        )

    if verify_runs_online:
        token = os.environ.get("GITHUB_TOKEN", "")
        repository = os.environ.get("GITHUB_REPOSITORY", "")
        if not token or not repository:
            errors.append(
                "--verify-runs-online requires GITHUB_TOKEN and GITHUB_REPOSITORY"
            )
        else:
            errors.extend(validate_runs_online(run_heads, repository, token))

    if errors:
        for error in errors:
            print(f"GATE_A_ERROR: {error}")
        raise CheckError(f"Gate A failed with {len(errors)} violation(s)")

    print(
        "PR416 Step 5 Gate A: PASS "
        f"refs={len(refs)} branch_exclusive_blob_instances={len(all_instances)} "
        f"covered={covered} violations=0 runs={len(run_heads)}"
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    parser.add_argument(
        "--main-ref",
        default="refs/remotes/origin/main",
        help="durable main ref used for both gates",
    )
    parser.add_argument(
        "--gate-a", action="store_true", help="run live-ref completeness gate"
    )
    parser.add_argument(
        "--gate-b", action="store_true", help="run mainline archive-integrity gate"
    )
    parser.add_argument(
        "--verify-runs-online",
        action="store_true",
        help="verify run-head rows against the GitHub Actions API (Gate A)",
    )
    parser.add_argument(
        "--expected-instances",
        type=int,
        default=None,
        help="optional expected total branch-exclusive blob-instance count",
    )
    args = parser.parse_args(argv)
    if not args.gate_a and not args.gate_b:
        parser.error("select --gate-a and/or --gate-b")
    if args.verify_runs_online and not args.gate_a:
        parser.error("--verify-runs-online requires --gate-a")

    repo = Path(args.repo).resolve()
    if not git_object_exists(repo, f"{args.main_ref}^{{commit}}"):
        print(f"ERROR: main ref is missing: {args.main_ref}")
        return 2

    try:
        if args.gate_b:
            manifest, disposition, run_heads = gate_b(repo, args.main_ref)
        else:
            manifest, disposition, run_heads = load_main_ledgers(repo, args.main_ref)
        if args.gate_a:
            gate_a(
                repo,
                args.main_ref,
                manifest,
                disposition,
                run_heads,
                args.verify_runs_online,
                args.expected_instances,
            )
    except CheckError as exc:
        print(f"ERROR: {exc}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
