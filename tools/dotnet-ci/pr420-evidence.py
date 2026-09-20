#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import hashlib
import io
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import sys
import tarfile
import tempfile
import xml.etree.ElementTree as ET
import zipfile
from collections import Counter

SOURCE_ARTIFACT_ID = 10578621101
SOURCE_SIZE = 1_106_934
SOURCE_SHA256 = "8a441096b7e4528d282b6d33388d96c03804560b730c97225c72bfa3fa771570"
EXPECTED_TRX_COUNT = 70
EXPECTED_SCOPE_COUNTS = {"coverage": 35, "owner-held-red": 35}
ARCHIVE_NAME = "pr-functional-executed-tests-10578621101.tar.xz"
MEMBER_MANIFEST_NAME = "TRX-SHA256SUMS"
TSV_NAME = "executed-results.tsv"
PAYLOAD_MANIFEST_NAME = "SHA256SUMS"


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def canonical_member_path(name: str) -> str:
    p = PurePosixPath(name)
    if p.is_absolute() or not p.parts or ".." in p.parts:
        raise ValueError(f"unsafe archive member path: {name!r}")
    normalized = p.as_posix()
    if normalized.startswith("./"):
        normalized = normalized[2:]
    return normalized


def validate_member_set(members: dict[str, bytes]) -> None:
    if len(members) != EXPECTED_TRX_COUNT:
        raise ValueError(f"expected {EXPECTED_TRX_COUNT} TRX members, found {len(members)}")
    scope_counts = Counter()
    for path in members:
        p = PurePosixPath(path)
        if p.suffix.lower() != ".trx":
            raise ValueError(f"non-TRX member present: {path}")
        if len(p.parts) < 2 or p.parts[0] not in EXPECTED_SCOPE_COUNTS:
            raise ValueError(f"unexpected evidence scope/path: {path}")
        scope_counts[p.parts[0]] += 1
    if dict(scope_counts) != EXPECTED_SCOPE_COUNTS:
        raise ValueError(
            f"scope counts differ: expected {EXPECTED_SCOPE_COUNTS}, found {dict(scope_counts)}"
        )


def load_source_zip(path: Path) -> dict[str, bytes]:
    size = path.stat().st_size
    digest = sha256_file(path)
    if size != SOURCE_SIZE or digest != SOURCE_SHA256:
        raise ValueError(
            "source artifact does not match pinned PR #420 evidence: "
            f"artifact_id={SOURCE_ARTIFACT_ID} expected_size={SOURCE_SIZE} actual_size={size} "
            f"expected_sha256={SOURCE_SHA256} actual_sha256={digest}"
        )
    members: dict[str, bytes] = {}
    with zipfile.ZipFile(path) as zf:
        for info in zf.infolist():
            if info.is_dir():
                continue
            rel = canonical_member_path(info.filename)
            if rel in members:
                raise ValueError(f"duplicate source member path: {rel}")
            members[rel] = zf.read(info)
    validate_member_set(members)
    return members


def load_tar_xz(path: Path) -> dict[str, bytes]:
    members: dict[str, bytes] = {}
    with tarfile.open(path, mode="r:xz") as tf:
        for info in tf.getmembers():
            if not info.isfile():
                raise ValueError(f"non-file member present in committed archive: {info.name}")
            rel = canonical_member_path(info.name)
            if rel in members:
                raise ValueError(f"duplicate committed member path: {rel}")
            extracted = tf.extractfile(info)
            if extracted is None:
                raise ValueError(f"could not read committed member: {rel}")
            members[rel] = extracted.read()
    validate_member_set(members)
    return members


def render_member_manifest(members: dict[str, bytes]) -> bytes:
    return "".join(
        f"{sha256_bytes(members[path])}  {path}\n" for path in sorted(members)
    ).encode("utf-8")


def parse_member_manifest(data: bytes) -> dict[str, str]:
    expected: dict[str, str] = {}
    for raw in data.decode("utf-8").splitlines():
        if not raw:
            continue
        digest, sep, path = raw.partition("  ")
        if not sep or len(digest) != 64:
            raise ValueError(f"malformed member-manifest row: {raw!r}")
        rel = canonical_member_path(path)
        if rel in expected:
            raise ValueError(f"duplicate member-manifest path: {rel}")
        expected[rel] = digest
    if len(expected) != EXPECTED_TRX_COUNT:
        raise ValueError(
            f"member manifest must contain {EXPECTED_TRX_COUNT} rows, found {len(expected)}"
        )
    return expected


def verify_member_manifest(members: dict[str, bytes], manifest: bytes) -> None:
    expected = parse_member_manifest(manifest)
    actual_paths = set(members)
    expected_paths = set(expected)
    if actual_paths != expected_paths:
        missing = sorted(expected_paths - actual_paths)
        extra = sorted(actual_paths - expected_paths)
        raise ValueError(f"member path set differs: missing={missing} extra={extra}")
    mismatches = [
        path
        for path in sorted(expected)
        if sha256_bytes(members[path]) != expected[path]
    ]
    if mismatches:
        raise ValueError(f"member SHA-256 mismatch: {mismatches}")


def render_tsv(members: dict[str, bytes]) -> bytes:
    rows: Counter[tuple[str, str, str]] = Counter()
    for path in sorted(members):
        scope = PurePosixPath(path).parts[0]
        root = ET.fromstring(members[path])
        for elem in root.iter():
            if elem.tag.rsplit("}", 1)[-1] != "UnitTestResult":
                continue
            test_name = elem.attrib.get("testName")
            outcome = elem.attrib.get("outcome")
            if not test_name or not outcome:
                raise ValueError(f"UnitTestResult missing testName/outcome in {path}")
            rows[(scope, test_name, outcome)] += 1

    buf = io.StringIO(newline="")
    writer = csv.writer(buf, delimiter="\t", lineterminator="\n")
    writer.writerow(("scope", "test_name", "outcome", "multiplicity"))
    for (scope, test_name, outcome), multiplicity in sorted(rows.items()):
        writer.writerow((scope, test_name, outcome, multiplicity))
    return buf.getvalue().encode("utf-8")


def write_deterministic_archive(members: dict[str, bytes], output: Path) -> tuple[str, str]:
    tar_bin = shutil.which("tar")
    xz_bin = shutil.which("xz")
    if not tar_bin or not xz_bin:
        raise RuntimeError("bootstrap requires GNU tar and xz")
    tar_version = subprocess.check_output([tar_bin, "--version"], text=True).splitlines()[0]
    xz_version = subprocess.check_output([xz_bin, "--version"], text=True).splitlines()[0]

    with tempfile.TemporaryDirectory() as td:
        root = Path(td)
        for rel, data in members.items():
            dest = root / PurePosixPath(rel)
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
        ordered = [path + "\0" for path in sorted(members)]
        tar_path = root / "payload.tar"
        proc = subprocess.run(
            [
                tar_bin,
                "--null",
                "-T",
                "-",
                "--sort=name",
                "--mtime=UTC 1970-01-01",
                "--owner=0",
                "--group=0",
                "--numeric-owner",
                "-cf",
                str(tar_path),
            ],
            cwd=root,
            input="".join(ordered).encode("utf-8"),
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
        )
        if proc.returncode != 0:
            raise RuntimeError(proc.stdout.decode("utf-8", errors="replace"))
        with output.open("wb") as out:
            proc = subprocess.run(
                [xz_bin, "-9", "-T1", "--stdout", str(tar_path)],
                check=False,
                stdout=out,
                stderr=subprocess.PIPE,
            )
        if proc.returncode != 0:
            raise RuntimeError(proc.stderr.decode("utf-8", errors="replace"))
    return tar_version, xz_version


def render_payload_manifest(output_dir: Path) -> bytes:
    paths = [ARCHIVE_NAME, MEMBER_MANIFEST_NAME, TSV_NAME]
    return "".join(
        f"{sha256_file(output_dir / name)}  {name}\n" for name in paths
    ).encode("utf-8")


def bootstrap(source: Path, output_dir: Path) -> None:
    members = load_source_zip(source)
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / MEMBER_MANIFEST_NAME).write_bytes(render_member_manifest(members))
    (output_dir / TSV_NAME).write_bytes(render_tsv(members))
    tar_version, xz_version = write_deterministic_archive(members, output_dir / ARCHIVE_NAME)
    (output_dir / PAYLOAD_MANIFEST_NAME).write_bytes(render_payload_manifest(output_dir))
    print(
        "BOOTSTRAP OK: "
        f"source_artifact_id={SOURCE_ARTIFACT_ID} members={len(members)} "
        f"archive_sha256={sha256_file(output_dir / ARCHIVE_NAME)}"
    )
    print(f"ARCHIVE TOOLCHAIN: {tar_version}; {xz_version}")


def verify(output_dir: Path) -> None:
    required = [ARCHIVE_NAME, MEMBER_MANIFEST_NAME, TSV_NAME, PAYLOAD_MANIFEST_NAME]
    missing = [name for name in required if not (output_dir / name).is_file()]
    if missing:
        raise ValueError(f"missing evidence files: {missing}")

    payload_lines = (output_dir / PAYLOAD_MANIFEST_NAME).read_text(encoding="utf-8").splitlines()
    expected_payload: dict[str, str] = {}
    for raw in payload_lines:
        if not raw:
            continue
        digest, sep, name = raw.partition("  ")
        if not sep or name not in {ARCHIVE_NAME, MEMBER_MANIFEST_NAME, TSV_NAME}:
            raise ValueError(f"unexpected SHA256SUMS row: {raw!r}")
        expected_payload[name] = digest
    if set(expected_payload) != {ARCHIVE_NAME, MEMBER_MANIFEST_NAME, TSV_NAME}:
        raise ValueError(f"SHA256SUMS coverage differs: {sorted(expected_payload)}")
    for name, expected in expected_payload.items():
        actual = sha256_file(output_dir / name)
        if actual != expected:
            raise ValueError(f"payload SHA-256 mismatch for {name}: expected={expected} actual={actual}")

    members = load_tar_xz(output_dir / ARCHIVE_NAME)
    verify_member_manifest(members, (output_dir / MEMBER_MANIFEST_NAME).read_bytes())
    regenerated = render_tsv(members)
    committed = (output_dir / TSV_NAME).read_bytes()
    if regenerated != committed:
        raise ValueError(
            "executed-results.tsv differs from the member-verified archive; regenerate from the pinned source"
        )
    print(
        "VERIFY OK: "
        f"source_artifact_id={SOURCE_ARTIFACT_ID} members={len(members)} "
        f"ordinary=3730 dedicated=1"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Bootstrap or verify durable PR #420 owner-held evidence.")
    sub = parser.add_subparsers(dest="mode", required=True)
    create = sub.add_parser("bootstrap")
    create.add_argument("--source", type=Path, required=True)
    create.add_argument("--output-dir", type=Path, required=True)
    check = sub.add_parser("verify")
    check.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.mode == "bootstrap":
            bootstrap(args.source, args.output_dir)
        else:
            verify(args.output_dir)
        return 0
    except (OSError, ValueError, RuntimeError, zipfile.BadZipFile, tarfile.TarError, ET.ParseError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
