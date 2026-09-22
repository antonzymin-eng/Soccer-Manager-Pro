#!/usr/bin/env python3
"""Verify committed evidence-integrity contracts and SHA-256 manifests."""

from __future__ import annotations

import argparse
import hashlib
import re
import subprocess
from pathlib import Path

EVIDENCE_ROOT = Path("docs/tracking/evidence")
FULL_MANIFEST = "SHA256SUMS"
PARTIAL_MANIFEST = "artifact-SHA256SUMS"
MANIFEST_NAMES = {FULL_MANIFEST, PARTIAL_MANIFEST}
LINE_RE = re.compile(r"^([0-9a-f]{64})  (.+)$")

# Every tracked top-level evidence directory must declare one integrity contract.
# "manifest" contracts are verified by this tool. "external" contracts are owned
# by the named repository verifier and are deliberately not reimplemented here.
DIRECTORY_CONTRACTS: dict[str, tuple[str, str]] = {
    "v210-rolling": ("manifest", FULL_MANIFEST),
    "pr420-owner-held-isolation": ("manifest", FULL_MANIFEST),
    "foul-card-six-seed": ("manifest", FULL_MANIFEST),
    "w2-six-seed": ("manifest", PARTIAL_MANIFEST),
    "w2": ("manifest", PARTIAL_MANIFEST),
    "w12": ("external", "tools/dotnet-ci/check_w12_evidence.py"),
    "pr416-ref-archive": (
        "external",
        "tools/dotnet-ci/check_pr416_evidence_refs.py",
    ),
}

# Standalone root metadata is outside a directory-manifest contract but must be
# explicitly registered so new root-level evidence cannot appear silently.
ROOT_FILE_ALLOWLIST = {
    "pr420-owner-held-isolation.md",
}

# This is a sha256sum-format ledger for members inside the committed tar.xz, not
# a filesystem coverage manifest. tools/dotnet-ci/pr420-evidence.py owns it.
AUXILIARY_SHA_MANIFEST_ALLOWLIST = {
    "pr420-owner-held-isolation/TRX-SHA256SUMS",
}


class EvidenceScopeError(RuntimeError):
    pass


def _git(repo: Path, *args: str) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
    )


def _tracked_paths(repo: Path, scope: Path) -> list[Path] | None:
    """Return tracked paths, or None only when repo is genuinely outside Git."""
    probe = _git(repo, "rev-parse", "--is-inside-work-tree")
    if probe.returncode != 0:
        stderr = probe.stderr.decode("utf-8", errors="replace").strip()
        if "not a git repository" in stderr.lower():
            return None
        raise EvidenceScopeError(
            "git worktree probe failed: " + (stderr or f"exit {probe.returncode}")
        )
    if probe.stdout.strip() != b"true":
        raise EvidenceScopeError(
            "repository is not a normal Git worktree; refusing filesystem scope fallback"
        )

    scope_text = scope.as_posix()
    completed = _git(repo, "ls-files", "-z", "--", scope_text)
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise EvidenceScopeError(
            f"git ls-files failed for {scope_text!r}: "
            + (stderr or f"exit {completed.returncode}")
        )
    return [
        Path(raw.decode("utf-8", errors="strict"))
        for raw in completed.stdout.split(b"\0")
        if raw
    ]


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _safe_relative_target(manifest: Path, raw_name: str) -> tuple[Path | None, str | None]:
    candidate = Path(raw_name)
    if candidate.is_absolute() or not raw_name or "\\" in raw_name:
        return None, f"{manifest}: unsafe manifest path {raw_name!r}"
    if any(part in {"", ".", ".."} for part in candidate.parts):
        return None, f"{manifest}: unsafe manifest path {raw_name!r}"
    target = manifest.parent / candidate
    try:
        target.resolve().relative_to(manifest.parent.resolve())
    except ValueError:
        return None, f"{manifest}: path escapes manifest directory: {raw_name!r}"
    return target, None


def _parse_manifest(manifest: Path) -> tuple[dict[str, str], list[str]]:
    errors: list[str] = []
    entries: dict[str, str] = {}
    try:
        lines = manifest.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        return {}, [f"{manifest}: could not read manifest: {exc}"]

    if not lines:
        return {}, [f"{manifest}: manifest is empty"]

    for line_no, line in enumerate(lines, 1):
        if not line.strip():
            errors.append(f"{manifest}:{line_no}: blank lines are not allowed")
            continue
        match = LINE_RE.fullmatch(line)
        if not match:
            errors.append(
                f"{manifest}:{line_no}: expected '<64 lowercase hex><two spaces><relative path>'"
            )
            continue
        digest, name = match.groups()
        if name in entries:
            errors.append(f"{manifest}:{line_no}: duplicate entry {name!r}")
            continue
        target, path_error = _safe_relative_target(manifest, name)
        if path_error:
            errors.append(path_error)
            continue
        assert target is not None
        entries[name] = digest
    return entries, errors


def _full_scope_files(repo: Path, manifest: Path) -> set[str]:
    """Full manifests cover tracked files only; temp-fixture repos fall back to disk."""
    try:
        directory_rel = manifest.parent.relative_to(repo)
    except ValueError:
        directory_rel = Path(".")

    tracked = _tracked_paths(repo, directory_rel)
    if tracked is not None:
        prefix = directory_rel.parts
        names: set[str] = set()
        for path in tracked:
            if path == manifest.relative_to(repo):
                continue
            if path.parts[: len(prefix)] != prefix:
                continue
            relative = Path(*path.parts[len(prefix) :])
            if relative.parts:
                names.add(relative.as_posix())
        return names

    return {
        path.relative_to(manifest.parent).as_posix()
        for path in manifest.parent.rglob("*")
        if path.is_file() and path != manifest
    }


def verify_manifest(repo: Path, manifest: Path) -> list[str]:
    entries, errors = _parse_manifest(manifest)
    recorded = set(entries)

    if manifest.name == FULL_MANIFEST:
        expected = _full_scope_files(repo, manifest)
        missing = sorted(expected - recorded)
        extra = sorted(recorded - expected)
        if missing:
            errors.append(f"{manifest}: uncovered tracked in-scope files: {missing!r}")
        if extra:
            errors.append(f"{manifest}: entries without tracked in-scope files: {extra!r}")

    for name, expected_digest in entries.items():
        target = manifest.parent / Path(name)
        if target.is_symlink():
            errors.append(f"{manifest}: symlink targets are not allowed: {name}")
            continue
        if not target.is_file():
            errors.append(f"{manifest}: listed file is missing: {name}")
            continue
        actual = _sha256(target)
        if actual != expected_digest:
            errors.append(
                f"{manifest}: SHA-256 mismatch for {name}: "
                f"expected {expected_digest}, got {actual}"
            )
    return errors


def _evidence_paths(repo: Path) -> list[Path]:
    tracked = _tracked_paths(repo, EVIDENCE_ROOT)
    if tracked is not None:
        return tracked
    root = repo / EVIDENCE_ROOT
    if not root.is_dir():
        return []
    return sorted(
        path.relative_to(repo)
        for path in root.rglob("*")
        if path.is_file() or path.is_symlink()
    )


def discover_manifests(repo: Path) -> list[Path]:
    return sorted(
        repo / path
        for path in _evidence_paths(repo)
        if path.name in MANIFEST_NAMES
    )


def _validate_repository_scope(repo: Path, evidence_paths: list[Path]) -> list[str]:
    errors: list[str] = []
    prefix_len = len(EVIDENCE_ROOT.parts)
    directories: set[str] = set()
    root_files: set[str] = set()

    for path in evidence_paths:
        relative_parts = path.parts[prefix_len:]
        if not relative_parts:
            continue
        if len(relative_parts) == 1:
            root_files.add(relative_parts[0])
        else:
            directories.add(relative_parts[0])

        if "SHA256SUMS" in path.name.upper() and path.name not in MANIFEST_NAMES:
            relative = Path(*relative_parts).as_posix()
            if relative not in AUXILIARY_SHA_MANIFEST_ALLOWLIST:
                errors.append(
                    f"{EVIDENCE_ROOT}/{relative}: unrecognized SHA256SUMS-style "
                    "manifest name; register an explicit contract or use a canonical name"
                )

    unknown_dirs = sorted(directories - set(DIRECTORY_CONTRACTS))
    if unknown_dirs:
        errors.append(
            f"{EVIDENCE_ROOT}: tracked evidence directories lack an integrity "
            f"contract: {unknown_dirs!r}"
        )

    unknown_root_files = sorted(root_files - ROOT_FILE_ALLOWLIST)
    if unknown_root_files:
        errors.append(
            f"{EVIDENCE_ROOT}: tracked root evidence files are not registered: "
            f"{unknown_root_files!r}"
        )

    if evidence_paths:
        stale_contracts = sorted(set(DIRECTORY_CONTRACTS) - directories)
        if stale_contracts:
            errors.append(
                f"{EVIDENCE_ROOT}: registered integrity contracts have no tracked "
                f"directory: {stale_contracts!r}"
            )

    for directory in sorted(directories & set(DIRECTORY_CONTRACTS)):
        kind, contract = DIRECTORY_CONTRACTS[directory]
        if kind == "manifest":
            manifest_path = EVIDENCE_ROOT / directory / contract
            if manifest_path not in evidence_paths:
                errors.append(
                    f"{EVIDENCE_ROOT / directory}: required integrity manifest "
                    f"{contract!r} is missing"
                )
        elif kind == "external":
            if not (repo / contract).is_file():
                errors.append(
                    f"{EVIDENCE_ROOT / directory}: external integrity verifier "
                    f"is missing: {contract}"
                )
        else:
            errors.append(
                f"{EVIDENCE_ROOT / directory}: unknown integrity contract kind {kind!r}"
            )
    return errors


def validate(repo: Path) -> tuple[list[Path], list[str]]:
    try:
        evidence_paths = _evidence_paths(repo)
    except EvidenceScopeError as exc:
        return [], [f"{EVIDENCE_ROOT}: {exc}"]

    manifests = sorted(
        repo / path for path in evidence_paths if path.name in MANIFEST_NAMES
    )
    errors = _validate_repository_scope(repo, evidence_paths)
    for manifest in manifests:
        try:
            errors.extend(verify_manifest(repo, manifest))
        except EvidenceScopeError as exc:
            errors.append(f"{manifest}: {exc}")
    return manifests, errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    args = parser.parse_args(argv)
    repo = Path(args.repo).resolve()
    manifests, errors = validate(repo)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    full = sum(path.name == FULL_MANIFEST for path in manifests)
    partial = sum(path.name == PARTIAL_MANIFEST for path in manifests)
    print(
        "Evidence integrity contracts: PASS "
        f"({len(manifests)} SHA-256 manifests; {full} full-coverage, "
        f"{partial} artifact-scoped; {len(DIRECTORY_CONTRACTS)} registered directories)"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
