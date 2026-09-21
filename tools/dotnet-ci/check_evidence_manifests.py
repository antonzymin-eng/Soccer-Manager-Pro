#!/usr/bin/env python3
"""Verify committed SHA-256 evidence manifests and their declared coverage."""

from __future__ import annotations

import argparse
import hashlib
import re
from pathlib import Path

EVIDENCE_ROOT = Path("docs/tracking/evidence")
FULL_MANIFEST = "SHA256SUMS"
PARTIAL_MANIFEST = "artifact-SHA256SUMS"
MANIFEST_NAMES = {FULL_MANIFEST, PARTIAL_MANIFEST}
LINE_RE = re.compile(r"^([0-9a-f]{64})  (.+)$")


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


def _full_scope_files(manifest: Path) -> set[str]:
    return {
        path.relative_to(manifest.parent).as_posix()
        for path in manifest.parent.rglob("*")
        if path.is_file() and path != manifest
    }


def verify_manifest(manifest: Path) -> list[str]:
    entries, errors = _parse_manifest(manifest)
    recorded = set(entries)

    if manifest.name == FULL_MANIFEST:
        expected = _full_scope_files(manifest)
        missing = sorted(expected - recorded)
        extra = sorted(recorded - expected)
        if missing:
            errors.append(f"{manifest}: uncovered in-scope files: {missing!r}")
        if extra:
            errors.append(f"{manifest}: entries without in-scope files: {extra!r}")

    for name, expected_digest in entries.items():
        target, path_error = _safe_relative_target(manifest, name)
        if path_error:
            if path_error not in errors:
                errors.append(path_error)
            continue
        assert target is not None
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


def discover_manifests(repo: Path) -> list[Path]:
    root = repo / EVIDENCE_ROOT
    if not root.is_dir():
        return []
    return sorted(
        path
        for path in root.rglob("*")
        if path.is_file() and path.name in MANIFEST_NAMES
    )


def validate(repo: Path) -> list[str]:
    manifests = discover_manifests(repo)
    if not manifests:
        return [f"{EVIDENCE_ROOT}: no evidence manifests found"]
    errors: list[str] = []
    for manifest in manifests:
        errors.extend(verify_manifest(manifest))
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    args = parser.parse_args(argv)
    repo = Path(args.repo).resolve()
    manifests = discover_manifests(repo)
    errors = validate(repo)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    full = sum(path.name == FULL_MANIFEST for path in manifests)
    partial = sum(path.name == PARTIAL_MANIFEST for path in manifests)
    print(
        "Evidence SHA-256 manifests: PASS "
        f"({len(manifests)} manifests; {full} full-coverage, {partial} artifact-scoped)"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
