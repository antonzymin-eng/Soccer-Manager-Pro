#!/usr/bin/env python3
"""Verify PR #439 evidence archival after the atomic evidence-ref deletion."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import lzma
import os
import re
import subprocess
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

ARCHIVE_ROOT = Path("docs/tracking/evidence/pr439-ref-archive")
MANIFEST = ARCHIVE_ROOT / "MANIFEST.tsv"
REF_HEADS = ARCHIVE_ROOT / "ref-heads.tsv"
DISPOSITION = ARCHIVE_ROOT / "ref-disposition.tsv"
RUN_HEADS = ARCHIVE_ROOT / "run-heads.tsv"
DISCUSSION_SNAPSHOT = ARCHIVE_ROOT / "pr439-discussion-snapshot.tsv"
DISCUSSION_META = ARCHIVE_ROOT / "pr439-discussion-snapshot-meta.tsv"
SUPERSEDED_RUNS = ARCHIVE_ROOT / "superseded-runs.tsv"
JOB_LOGS = ARCHIVE_ROOT / "archived-job-logs.tsv"
HISTORICAL_WORKFLOWS = ARCHIVE_ROOT / "historical-workflows.tsv"
PR_NUMBER = 439
EXPECTED_REF_COUNT = 16
EXPECTED_RUN_COUNT = 15
EXPECTED_BLOB_STATE_COUNT = 39
EXPECTED_SUPERSEDED_IDS = {
    "35814523589", "35814675549", "35814874360", "35814941362",
    "35815215062", "35879288048", "35887103114", "35887451692",
}
EXPECTED_JOB_IDS = {
    "107240961327", "107241091685", "107241232506",
    "107241699913", "107243207618", "107243250548",
}
ALTERNATE_ARTIFACT_SHA256 = "981600d85ddcb67cd0d313ed698e4f1a6db5c70cce1e753b80a84600a7497571"


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


def validate_frozen_pr_citations(repo: Path, cited_ids: set[str]) -> list[str]:
    errors: list[str] = []
    rows = read_tsv(repo / DISCUSSION_SNAPSHOT)
    meta_rows = read_tsv(repo / DISCUSSION_META)
    if len(meta_rows) != 1:
        return [f"PR #439 discussion snapshot metadata rows {len(meta_rows)} != 1"]
    meta = meta_rows[0]
    if meta.get("pr_number") != str(PR_NUMBER):
        errors.append("PR #439 discussion snapshot has wrong PR number")
    captured_at = meta.get("captured_at_utc", "")
    if not re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", captured_at):
        errors.append("PR #439 discussion snapshot capture timestamp is malformed")
    if not rows:
        errors.append("PR #439 discussion snapshot is empty")
    if meta.get("source_count") != str(len(rows)):
        errors.append("PR #439 discussion snapshot metadata source count mismatch")

    source_keys: set[tuple[str, str]] = set()
    frozen_ids: set[str] = set()
    canonical_lines: list[str] = []
    fields = ("source_kind", "source_id", "source_url", "body_sha256", "run_ids")
    for row in rows:
        key = (row.get("source_kind", ""), row.get("source_id", ""))
        if key in source_keys:
            errors.append(f"duplicate PR #439 discussion source: {key}")
        source_keys.add(key)
        if row.get("source_kind") not in {"pr_body", "issue_comment", "review_comment", "review"}:
            errors.append(f"unexpected PR #439 discussion source kind: {row.get('source_kind')!r}")
        if not row.get("source_id") or not row.get("source_url"):
            errors.append(f"incomplete PR #439 discussion source: {key}")
        if not re.fullmatch(r"[0-9a-f]{64}", row.get("body_sha256", "")):
            errors.append(f"malformed PR #439 body SHA-256: {key}")

        ids = [value for value in row.get("run_ids", "").split(",") if value]
        if len(ids) != len(set(ids)):
            errors.append(f"duplicate run IDs in PR #439 discussion source: {key}")
        for run_id in ids:
            if not re.fullmatch(r"35\d{9}", run_id):
                errors.append(f"malformed frozen PR #439 run ID: {run_id}")
            frozen_ids.add(run_id)
        canonical_lines.append("\t".join(row.get(field, "") for field in fields))

    if frozen_ids != cited_ids:
        errors.append(
            "frozen PR #439 citation coverage mismatch: "
            f"missing={sorted(cited_ids-frozen_ids)} extras={sorted(frozen_ids-cited_ids)}"
        )
    if meta.get("cited_run_count") != str(len(frozen_ids)):
        errors.append("PR #439 discussion snapshot metadata cited-run count mismatch")
    digest = sha256(("\n".join(canonical_lines) + "\n").encode("utf-8"))
    if digest != meta.get("source_index_sha256"):
        errors.append("PR #439 discussion snapshot aggregate digest mismatch")
    return errors


def file_sha256_matches(path: Path, expected: str) -> bool:
    return sha256(path.read_bytes()) == expected


def validate_post_delete_topology(
    actual_refs: set[str], dispositions: list[dict[str, str]]
) -> list[str]:
    errors: list[str] = []
    if len(dispositions) != EXPECTED_REF_COUNT or not all(
        row.get("delete_now") == "true" for row in dispositions
    ):
        errors.append("post-delete certification requires all 16 delete_now rows to be true")
    if actual_refs:
        errors.append(f"post-delete PR439 evidence refs reappeared: {sorted(actual_refs)}")
    return errors


def resolve(repo: Path, spec: str) -> str | None:
    result = git(repo, "rev-parse", "--verify", spec, check=False)
    return result.stdout.strip() if result.returncode == 0 else None


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def archived_path(repo: Path, raw: str, folder: str) -> Path:
    prefix = (ARCHIVE_ROOT / folder).as_posix() + "/"
    if not raw.startswith(prefix) or ".." in Path(raw).parts or "\\" in raw:
        raise ValueError(f"unsafe {folder} archive path: {raw!r}")
    path = repo / raw
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"missing or symlinked {folder} archive file: {raw}")
    return path


def validate_historical_evidence(repo: Path, cited_ids: set[str]) -> list[str]:
    errors: list[str] = []
    old = read_tsv(repo / SUPERSEDED_RUNS)
    logs = read_tsv(repo / JOB_LOGS)
    workflows = read_tsv(repo / HISTORICAL_WORKFLOWS)
    ids = [row["run_id"] for row in old]
    if len(ids) != len(EXPECTED_SUPERSEDED_IDS) or set(ids) != EXPECTED_SUPERSEDED_IDS:
        errors.append("historical eight-run ledger cardinality or IDs changed")
    if set(ids) & cited_ids:
        errors.append("historical run IDs leaked into the 15-run PR #439 citation ledger")

    artifact_paths: set[str] = set()
    for row in old:
        for field in ("conclusion", "head_branch", "head_sha", "production_base_sha",
                      "workflow_name", "event", "commit_subject", "commit_date",
                      "disposition_reason"):
            if not row.get(field):
                errors.append(f"historical run {row['run_id']}: missing {field}")
        if not re.fullmatch(r"[0-9a-f]{40}", row["head_sha"]) or not re.fullmatch(
            r"[0-9a-f]{40}", row["production_base_sha"]
        ):
            errors.append(f"historical run {row['run_id']}: malformed Git SHA")
        if row["run_id"] == "35814941362":
            if (row["artifact_id"], row["artifact_expires_at"],
                    row["artifact_sha256"], row["artifact_size_bytes"]) != (
                "10731182039", "2026-12-22T03:35:34Z",
                ALTERNATE_ARTIFACT_SHA256, "175296"
            ):
                errors.append("alternate pre-W3 artifact provenance changed")
            artifact_paths.add(row["archive_path"])
            try:
                path = archived_path(repo, row["archive_path"], "historical-artifacts")
                raw = path.read_bytes()
                if not file_sha256_matches(path, row["artifact_sha256"]) or len(raw) != int(row["artifact_size_bytes"]):
                    errors.append("alternate pre-W3 ZIP digest or size mismatch")
                with zipfile.ZipFile(path) as archive:
                    checksums = archive.read("SHA256SUMS").decode("utf-8")
                    members = {}
                    for line in checksums.splitlines():
                        digest, name = line.split("  ", 1)
                        name = name.removeprefix("./")
                        if name in members:
                            errors.append(f"alternate artifact duplicate member: {name}")
                        members[name] = digest
                    if set(members) != set(archive.namelist()) - {"SHA256SUMS"}:
                        errors.append("alternate artifact internal member set mismatch")
                    for name, digest in members.items():
                        if sha256(archive.read(name)) != digest:
                            errors.append(f"alternate artifact member hash mismatch: {name}")
            except (OSError, ValueError, KeyError, zipfile.BadZipFile) as exc:
                errors.append(f"alternate artifact cannot be verified: {exc}")
        elif any(row[field] != "none" for field in (
            "artifact_id", "artifact_expires_at", "artifact_sha256",
            "artifact_size_bytes", "archive_path"
        )):
            errors.append(f"historical run {row['run_id']}: unexpected artifact metadata")

    expected_workflow_paths: set[str] = set()
    if len(workflows) != 2:
        errors.append(f"historical workflow count {len(workflows)} != 2")
    for row in workflows:
        name = row["archive_path"]
        expected_workflow_paths.add(name)
        try:
            path = archived_path(repo, name, "historical-workflows")
            if not file_sha256_matches(path, row["sha256"]):
                errors.append(f"historical workflow SHA-256 mismatch: {name}")
            if resolve(repo, f"HEAD:{name}") != row["git_blob_sha"]:
                errors.append(f"historical workflow Git blob mismatch: {name}")
        except ValueError as exc:
            errors.append(str(exc))

    expected_log_paths: set[str] = set()
    job_ids = [row["job_id"] for row in logs]
    if len(job_ids) != len(EXPECTED_JOB_IDS) or set(job_ids) != EXPECTED_JOB_IDS:
        errors.append("archived job-log cardinality or IDs changed")
    for row in logs:
        name = row["archive_path"]
        expected_log_paths.add(name)
        try:
            path = archived_path(repo, name, "job-logs")
            compressed = path.read_bytes()
            if not file_sha256_matches(path, row["sha256_xz"]):
                errors.append(f"job-log compressed SHA-256 mismatch: {name}")
            decoded = lzma.decompress(compressed)
            if sha256(decoded) != row["sha256_decoded_log"] or len(decoded.splitlines()) != int(row["decoded_line_count"]):
                errors.append(f"job-log decoded identity mismatch: {name}")
            if row["job_id"] == "107241699913":
                if not all(marker in decoded for marker in (
                    b"Expected: 2", b"But was:  1", b"M7 mutation proof succeeded:"
                )):
                    errors.append("M7 decisive mutation evidence missing")
            elif b"W2DIAG SUMMARY won=" not in decoded:
                errors.append(f"W2 diagnostic summary missing: {name}")
        except (OSError, ValueError, lzma.LZMAError) as exc:
            errors.append(f"job log cannot be verified: {name}: {exc}")

    for folder, expected in (
        ("historical-artifacts", artifact_paths),
        ("historical-workflows", expected_workflow_paths),
        ("job-logs", expected_log_paths),
    ):
        actual = set(git(repo, "ls-tree", "-r", "--name-only", "HEAD",
                         (ARCHIVE_ROOT / folder).as_posix()).stdout.splitlines())
        if actual != expected or len(expected) != len(actual):
            errors.append(f"{folder} coverage mismatch: expected={sorted(expected)} actual={sorted(actual)}")
    return errors


def validate_archive(repo: Path) -> list[str]:
    errors: list[str] = []
    required = [MANIFEST, REF_HEADS, DISPOSITION, RUN_HEADS,
                DISCUSSION_SNAPSHOT, DISCUSSION_META,
                SUPERSEDED_RUNS, JOB_LOGS, HISTORICAL_WORKFLOWS]
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
    authorization = {row.get("delete_now") for row in dispositions}
    if authorization not in ({"false"}, {"true"}):
        errors.append("all 16 deletion authorizations must agree and be true or false")
    for row in dispositions:
        ref = row["ref"]
        if row.get("current_head") != head_by_ref.get(ref):
            errors.append(
                f"{ref}: disposition head {row.get('current_head')} != ref ledger {head_by_ref.get(ref)}"
            )
        if row.get("disposition") != "deletable":
            errors.append(f"{ref}: unexpected disposition {row.get('disposition')!r}")

    run_ids = {row["run_id"] for row in runs}
    if len(runs) != EXPECTED_RUN_COUNT or len(run_ids) != len(runs):
        errors.append(
            f"run-head ledger cardinality/uniqueness failure: rows={len(runs)} unique={len(run_ids)}"
        )
    errors.extend(validate_frozen_pr_citations(repo, run_ids))
    errors.extend(validate_historical_evidence(repo, run_ids))

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


def _github_list_all(url: str, token: str) -> list[dict[str, object]]:
    items: list[dict[str, object]] = []
    page = 1
    while True:
        separator = "&" if "?" in url else "?"
        payload = _github_json(f"{url}{separator}per_page=100&page={page}", token)
        if not isinstance(payload, list):
            raise ValueError(f"GitHub list endpoint did not return a list: {url}")
        items.extend(payload)
        if len(payload) < 100:
            return items
        page += 1


def compare_live_discussion_snapshot(
    repo: Path, live_sources: list[dict[str, str]]
) -> list[str]:
    errors: list[str] = []
    frozen_rows = read_tsv(repo / DISCUSSION_SNAPSHOT)
    frozen = {
        (row["source_kind"], row["source_id"]): row
        for row in frozen_rows
    }
    live = {
        (row["source_kind"], row["source_id"]): row
        for row in live_sources
    }

    added = sorted(set(live) - set(frozen))
    removed = sorted(set(frozen) - set(live))
    for key in added:
        errors.append(f"PR #439 discussion source added since snapshot: {key}")
    for key in removed:
        errors.append(f"PR #439 discussion source removed since snapshot: {key}")

    for key in sorted(set(live) & set(frozen)):
        current = live[key]
        expected = frozen[key]
        for field in ("source_url", "body_sha256", "run_ids"):
            if current.get(field, "") != expected.get(field, ""):
                errors.append(
                    f"PR #439 discussion source changed since snapshot: {key} "
                    f"{field}={current.get(field, '')!r} "
                    f"expected={expected.get(field, '')!r}"
                )
    return errors


def verify_live_github_state(repo: Path, runs: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    token = os.environ.get("GITHUB_TOKEN", "")
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
    if not token or not repository:
        return ["live GitHub audit requires GITHUB_TOKEN and GITHUB_REPOSITORY"]

    try:
        pr = _github_json(f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}", token)
        comments = _github_list_all(
            f"{api_url}/repos/{repository}/issues/{PR_NUMBER}/comments", token
        )
        review_comments = _github_list_all(
            f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}/comments", token
        )
        reviews = _github_list_all(
            f"{api_url}/repos/{repository}/pulls/{PR_NUMBER}/reviews", token
        )
    except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError, ValueError) as exc:
        return [f"PR #{PR_NUMBER} discussion lookup failed: {exc}"]

    ledger_ids = {row["run_id"] for row in runs}
    pattern = re.compile(r"\b35\d{9}\b")

    def source(kind: str, source_id: object, url: object, body: object) -> dict[str, str]:
        text = str(body or "")
        ids = sorted(set(pattern.findall(text)) & ledger_ids)
        return {
            "source_kind": kind,
            "source_id": str(source_id),
            "source_url": str(url or ""),
            "body_sha256": sha256(text.encode("utf-8")),
            "run_ids": ",".join(ids),
        }

    live_sources = [
        source("pr_body", PR_NUMBER, pr.get("html_url"), pr.get("body"))
    ]
    live_sources.extend(
        source("issue_comment", item.get("id"), item.get("html_url"), item.get("body"))
        for item in comments
    )
    live_sources.extend(
        source("review_comment", item.get("id"), item.get("html_url"), item.get("body"))
        for item in review_comments
    )
    live_sources.extend(
        source("review", item.get("id"), item.get("html_url"), item.get("body"))
        for item in reviews
    )
    errors.extend(compare_live_discussion_snapshot(repo, live_sources))

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
                    f"run {run_id}: Actions {api_key} {actual!r} != ledger "
                    f"{row.get(ledger_key, '')!r}"
                )
    return errors

def validate_live(repo: Path, verify_api: bool = False) -> list[str]:
    errors = validate_archive(repo)
    if errors:
        return errors

    dispositions = read_tsv(repo / DISPOSITION)
    live = git(
        repo,
        "for-each-ref",
        "--format=%(refname) %(objectname)",
        "refs/remotes/origin/evidence/pr439-*",
    ).stdout.splitlines()
    prefix = "refs/remotes/origin/"
    actual_refs = {
        line.split()[0][len(prefix):]
        for line in live
        if line.strip()
    }
    errors.extend(validate_post_delete_topology(actual_refs, dispositions))

    if verify_api:
        errors.extend(verify_live_github_state(repo, read_tsv(repo / RUN_HEADS)))

    print(
        "PR439 post-delete live audit: "
        f"live_refs={len(actual_refs)} runs={len(read_tsv(repo / RUN_HEADS))}"
    )
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".")
    parser.add_argument("--mode", choices=("archive", "live", "all"), default="archive")
    parser.add_argument("--verify-actions-api", action="store_true")
    args = parser.parse_args(argv)
    repo = Path(args.repo).resolve()
    errors: list[str] = []
    try:
        if args.mode in {"archive", "all"}:
            errors.extend(validate_archive(repo))
        if args.mode in {"live", "all"}:
            errors.extend(validate_live(repo, args.verify_actions_api))
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
