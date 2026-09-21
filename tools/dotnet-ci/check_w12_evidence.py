#!/usr/bin/env python3
"""Validate the durable W12 pre/post evidence against the committed comparison doc."""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
import subprocess
import sys
import zipfile
from pathlib import Path

POST_ARCHIVE = Path("docs/tracking/evidence/w12/pr398-post-w12-34847990460.zip")
POST_ARCHIVE_SHA256 = "691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d"
PRE_ARCHIVE = Path("docs/tracking/evidence/w12/w12-corrected-pre398-34844425733.zip")
PRE_ARCHIVE_SHA256 = "0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c"
DOC = Path("docs/tracking/w12-gate-firing-post398-comparison.md")
CENSUS_JSON = Path("docs/tracking/evidence/w12/w12-gate-firing-census.json")
SWEEP_ARCHIVE = Path("docs/tracking/evidence/w12/w12-static-unread-field-sweep-34803051927.zip")
SWEEP_ARCHIVE_SHA256 = "c1e3526987793b8d736fad3f52e983982a8d10d483215a4ebbcb0f44bdef94f6"
CENSUS_START = "<!-- W12_POST_CENSUS_BEGIN -->"
CENSUS_END = "<!-- W12_POST_CENSUS_END -->"
MANIFEST = Path("docs/tracking/evidence/w12/README.md")
CENSUS_CONSUMERS = ["docs/tracking/w6-controlled-ball-preregistration.md"]
EXPECTED_CENSUS_ROOT_KEYS = {
    "consumers",
    "durable_evidence",
    "generated_from",
    "post",
    "pre",
    "provenance",
    "salvaged_from",
    "schema_version",
}
EXPECTED_GENERATED_FROM = (
    "first complete W12 census block in the committed pre/post GitHub artifact ZIPs"
)
EXPECTED_SALVAGED_FROM = {
    "branch": "wiring/w12-evidence-repair",
    "commit": "7dd81a9c842298927ac929d35e8caac12860a365",
}

EXPECTED_SWEEP_MEMBERS = {
    "report.json": {
        "sha256": "c542a5f4b8596bc8c62120e39c709e3d9e766038527092e0cb21e8f9097a4859",
        "size": 53109,
    },
    "report.md": {
        "sha256": "50fafdcd92459cc89408de89401c42345c2fc6c2bb57f82bffda37aec25295bd",
        "size": 48401,
    },
}

EXPECTED_ARCHIVE_MEMBERS = {
    "pre": {
        "instrument-output.txt": "66dfe2604e963b3d5569eed5ff10a14d3216cf9042fe182ca86e2e7c8a8adebc",
        "measurement.txt": "0ad04132db4e707f65332d318c0be12e7220a8a143b3a6530d5bb096fb630f15",
    },
    "post": {
        "instrument-output.txt": "8c3764765968e74d63cfb232815ed6a72b4b3789bed07de9757327adf1cf8655",
        "measurement.txt": "9e8c5bc8ae2698a3989cf6e2b750e489069396872d6c6ba3a95ce40928ef7ab1",
    },
}

EXPECTED_ROWS = {
    "hasLatestPass team-heartbeats": ("latestPass",),
    "BACKWARD_PASS raw": ("raw.BackwardPass",),
    "BACKWARD_PASS committed": ("committed.BackwardPass",),
    "BadTouch raw / committed": ("raw.BadTouch", "committed.BadTouch"),
    "SidelineTrap raw / committed": ("raw.SidelineTrap", "committed.SidelineTrap"),
    "WeakReceiver raw / committed": ("raw.WeakReceiver", "committed.WeakReceiver"),
    "primaryAssigned": ("primaryAssigned",),
    "Active": ("exits.Active",),
    "InvariantRejected": ("exits.InvariantRejected",),
    "NoPrimaryPresser": ("exits.NoPrimaryPresser",),
    "Disengaged": ("exits.Disengaged",),
    "Cooldown": ("exits.Cooldown",),
    "NoCommittedTrigger": ("exits.NoCommittedTrigger",),
}


def _normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def _committed_bytes(repo: Path, path: Path, expected_sha256: str) -> bytes:
    """Read the committed Git object, not a filtered/normalized worktree copy."""
    completed = subprocess.run(
        ["git", "-C", str(repo), "show", f"HEAD:{path.as_posix()}"],
        check=False,
        capture_output=True,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            f"could not read committed evidence object {path}: "
            + completed.stderr.decode("utf-8", errors="replace")
        )
    data = completed.stdout
    actual = hashlib.sha256(data).hexdigest()
    if actual != expected_sha256:
        raise ValueError(f"{path}: SHA-256 {actual} != expected {expected_sha256}")
    return data


def _read_member(repo: Path, archive: Path, expected_sha256: str, member: str) -> str:
    data = _committed_bytes(repo, archive, expected_sha256)
    with zipfile.ZipFile(io.BytesIO(data)) as zf:
        return _normalize_newlines(zf.read(member).decode("utf-8"))


def extract_census(text: str) -> str:
    start = text.index("=== W12 gate-firing census ===")
    tail = text[start:]
    match = re.search(r"\n{2,}W12GateFiringDiagnostic_", tail)
    if not match:
        raise ValueError("could not locate the end of the W12 census")
    return tail[: match.start()].rstrip() + "\n"


def extract_doc_census(doc: str) -> str:
    start = doc.index(CENSUS_START) + len(CENSUS_START)
    end = doc.index(CENSUS_END, start)
    body = doc[start:end].strip()
    match = re.fullmatch(r"```text\n(.*)\n```", body, re.DOTALL)
    if not match:
        raise ValueError("machine-checkable census block must be one ```text fence")
    return match.group(1).rstrip() + "\n"


def _pairs(text: str) -> dict[str, int]:
    return {key: int(value) for key, value in re.findall(r"([A-Za-z][A-Za-z0-9]*)=(\d+)", text)}


def parse_census(census: str) -> tuple[list[dict[str, object]], list[str]]:
    lines = census.splitlines()
    records: list[dict[str, object]] = []
    scorelines: list[str] = []
    seed = None
    scoreline = None
    i = 0
    while i < len(lines):
        line = lines[i]
        seed_match = re.fullmatch(r"seed (\S+)\s+final (\d+-\d+)", line)
        if seed_match:
            seed, scoreline = seed_match.groups()
            scorelines.append(scoreline)
            i += 1
            continue

        team_match = re.fullmatch(
            r"\s+team (\d+): samples=(\d+) latestPass=(\d+) active=(\d+) "
            r"primaryAssigned=(\d+) coverShadows=(\d+)",
            line,
        )
        if team_match:
            if seed is None or scoreline is None:
                raise ValueError("team record before seed")
            if i + 4 >= len(lines):
                raise ValueError("truncated team record")
            team, samples, latest, active, primary, shadows = map(int, team_match.groups())
            phase_line, exits_line, raw_line, committed_line = lines[i + 1 : i + 5]
            if not phase_line.lstrip().startswith("phase:"):
                raise ValueError(f"missing phase line after {seed} team {team}")
            if not exits_line.lstrip().startswith("exits:"):
                raise ValueError(f"missing exits line after {seed} team {team}")
            if not raw_line.lstrip().startswith("raw:"):
                raise ValueError(f"missing raw line after {seed} team {team}")
            if not committed_line.lstrip().startswith("committed:"):
                raise ValueError(f"missing committed line after {seed} team {team}")
            records.append(
                {
                    "seed": seed,
                    "scoreline": scoreline,
                    "team": team,
                    "samples": samples,
                    "latestPass": latest,
                    "active": active,
                    "primaryAssigned": primary,
                    "coverShadows": shadows,
                    "phase": _pairs(phase_line),
                    "exits": _pairs(exits_line),
                    "raw": _pairs(raw_line),
                    "committed": _pairs(committed_line),
                }
            )
            i += 5
            continue
        i += 1

    if len(records) != 6 or len(scorelines) != 3:
        raise ValueError(f"expected 6 team records / 3 scorelines, got {len(records)} / {len(scorelines)}")
    return records, scorelines


def _value(record: dict[str, object], path: str) -> int:
    if "." not in path:
        return int(record.get(path, 0))
    group, key = path.split(".", 1)
    values = record.get(group, {})
    assert isinstance(values, dict)
    return int(values.get(key, 0))


def aggregate(records: list[dict[str, object]]) -> dict[str, int]:
    scalar_keys = ("samples", "latestPass", "active", "primaryAssigned", "coverShadows")
    result = {key: sum(_value(record, key) for record in records) for key in scalar_keys}
    for group in ("phase", "exits", "raw", "committed"):
        names = sorted(
            {
                name
                for record in records
                for name in (
                    record.get(group, {}).keys()
                    if isinstance(record.get(group, {}), dict)
                    else ()
                )
            }
        )
        for name in names:
            result[f"{group}.{name}"] = sum(_value(record, f"{group}.{name}") for record in records)
    return result


def _record_for_census(record: dict[str, object]) -> dict[str, object]:
    return {
        "seed": record["seed"],
        "final": record["scoreline"],
        "team": record["team"],
        "samples": record["samples"],
        "latestPass": record["latestPass"],
        "active": record["active"],
        "primaryAssigned": record["primaryAssigned"],
        "coverShadows": record["coverShadows"],
        "phase": record["phase"],
        "exits": record["exits"],
        "raw": record["raw"],
        "committed": record["committed"],
    }


def _group_aggregate(
    values: dict[str, int], records: list[dict[str, object]], group: str
) -> dict[str, int]:
    names = sorted(
        {
            name
            for record in records
            for name in (
                record.get(group, {}).keys()
                if isinstance(record.get(group, {}), dict)
                else ()
            )
        }
    )
    return {name: values[f"{group}.{name}"] for name in names}


def _aggregate_for_census(
    records: list[dict[str, object]], scorelines: list[str]
) -> dict[str, object]:
    values = aggregate(records)
    exits = _group_aggregate(values, records, "exits")
    return {
        "active": values["active"],
        "committed": _group_aggregate(values, records, "committed"),
        "coverShadows": values["coverShadows"],
        "exits": exits,
        "latestPass": values["latestPass"],
        "nonInPossession": values["samples"] - exits.get("InPossession", 0),
        "phase": _group_aggregate(values, records, "phase"),
        "primaryAssigned": values["primaryAssigned"],
        "raw": _group_aggregate(values, records, "raw"),
        "samples": values["samples"],
        "scorelines": scorelines,
    }


def _first_difference(
    expected: object, actual: object, path: str = ""
) -> tuple[str, object, object] | None:
    if type(expected) is not type(actual):
        return path or "<root>", expected, actual
    if isinstance(expected, dict):
        assert isinstance(actual, dict)
        expected_keys = set(expected)
        actual_keys = set(actual)
        missing = sorted(expected_keys - actual_keys)
        if missing:
            key = missing[0]
            return f"{path}.{key}".lstrip("."), expected[key], "<missing>"
        extra = sorted(actual_keys - expected_keys)
        if extra:
            key = extra[0]
            return f"{path}.{key}".lstrip("."), "<absent>", actual[key]
        for key in sorted(expected):
            diff = _first_difference(
                expected[key],
                actual[key],
                f"{path}.{key}".lstrip("."),
            )
            if diff is not None:
                return diff
        return None
    if isinstance(expected, list):
        assert isinstance(actual, list)
        if len(expected) != len(actual):
            return f"{path}.length".lstrip("."), len(expected), len(actual)
        for index, expected_item in enumerate(expected):
            diff = _first_difference(
                expected_item,
                actual[index],
                f"{path}[{index}]",
            )
            if diff is not None:
                return diff
        return None
    if expected != actual:
        return path or "<root>", expected, actual
    return None


def _append_difference(
    errors: list[str], label: str, expected: object, actual: object
) -> None:
    diff = _first_difference(expected, actual)
    if diff is None:
        return
    path, expected_value, actual_value = diff
    errors.append(
        f"{CENSUS_JSON}: {label}.{path}: expected {expected_value!r}, got {actual_value!r}"
    )


def _archive_member_hashes(
    repo: Path, archive: Path, expected_archive_sha256: str
) -> tuple[dict[str, str], set[str]]:
    data = _committed_bytes(repo, archive, expected_archive_sha256)
    with zipfile.ZipFile(io.BytesIO(data)) as zf:
        names = set(zf.namelist())
        hashes = {
            member: hashlib.sha256(zf.read(member)).hexdigest()
            for member in names
            if not member.endswith("/")
        }
    return hashes, names


def _validate_manifest_hash_rows(
    repo: Path, archive_hashes: dict[str, dict[str, str]]
) -> list[str]:
    errors: list[str] = []
    try:
        manifest = (repo / MANIFEST).read_text(encoding="utf-8")
    except OSError as exc:
        return [f"{MANIFEST}: could not read manifest: {exc}"]
    for lane, members in archive_hashes.items():
        for member in ("instrument-output.txt", "measurement.txt"):
            digest = members.get(member)
            if digest is None:
                errors.append(f"{lane} archive missing required member {member}")
                continue
            expected_row = f"| `{member}` | `{digest}` |"
            if expected_row not in manifest:
                errors.append(
                    f"{MANIFEST}: missing {lane} extracted-hash row for {member} ({digest})"
                )
    return errors


def _table_rows(text: str) -> dict[str, tuple[str, str]]:
    rows: dict[str, tuple[str, str]] = {}
    for line in text.splitlines():
        if not line.startswith("|"):
            continue
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if len(cells) != 3 or cells[0] in {"Exit / outcome", "---"}:
            continue
        rows[cells[0]] = (cells[1], cells[2])
    return rows


def _validate_prereg_baseline(
    repo: Path, expected_pre_aggregate: dict[str, object]
) -> list[str]:
    errors: list[str] = []
    consumer = Path(CENSUS_CONSUMERS[0])
    consumer_path = repo / consumer
    try:
        text = consumer_path.read_text(encoding="utf-8")
    except OSError as exc:
        return [f"{consumer}: required census consumer is missing/unreadable: {exc}"]

    baseline_line = f"**Baseline source:** `{CENSUS_JSON.as_posix()}`  "
    if baseline_line not in text:
        errors.append(
            f"{consumer}: baseline source must point to {CENSUS_JSON.as_posix()}"
        )

    non_in_possession = int(expected_pre_aggregate["nonInPossession"])
    expected_population = (
        f"Non-`InPossession` population: **{non_in_possession:,} heartbeats**."
    )
    if expected_population not in text:
        errors.append(
            f"{consumer}: locked non-InPossession baseline does not match "
            f"{non_in_possession:,}"
        )

    exits = expected_pre_aggregate.get("exits")
    if not isinstance(exits, dict):
        return errors + ["pre aggregate exits must be an object"]

    combined = sum(
        int(exits.get(name, 0))
        for name in ("InvariantRejected", "Cooldown", "NoPrimaryPresser", "Disengaged")
    )
    expected_rows = {
        "InvariantRejected": int(exits.get("InvariantRejected", 0)),
        "Cooldown (Pressing AI / `DisengageResolver`)": int(exits.get("Cooldown", 0)),
        "Active": int(exits.get("Active", 0)),
        "NoPrimaryPresser": int(exits.get("NoPrimaryPresser", 0)),
        "Disengaged": int(exits.get("Disengaged", 0)),
        "NoCommittedTrigger": int(exits.get("NoCommittedTrigger", 0)),
        "Combined suppression mass¹": combined,
    }
    rows = _table_rows(text)
    for label, count in expected_rows.items():
        expected_share = f"{(count / non_in_possession) * 100:.3f}%"
        actual = rows.get(label)
        expected = (f"{count:,}", expected_share)
        if actual != expected:
            errors.append(
                f"{consumer}: baseline row {label!r} expected {expected!r}, got {actual!r}"
            )
    return errors


def validate_census_payload(
    repo: Path,
    census: object,
    pre_records: list[dict[str, object]],
    pre_scores: list[str],
    post_records: list[dict[str, object]],
    post_scores: list[str],
) -> list[str]:
    errors: list[str] = []
    if not isinstance(census, dict):
        return [f"{CENSUS_JSON}: root must be an object"]

    actual_root_keys = set(census)
    if actual_root_keys != EXPECTED_CENSUS_ROOT_KEYS:
        errors.append(
            f"{CENSUS_JSON}: root keys {sorted(actual_root_keys)!r} "
            f"!= expected {sorted(EXPECTED_CENSUS_ROOT_KEYS)!r}"
        )
    if census.get("schema_version") != 2:
        errors.append(f"{CENSUS_JSON}: schema_version must be 2")
    if census.get("generated_from") != EXPECTED_GENERATED_FROM:
        errors.append(
            f"{CENSUS_JSON}: generated_from {census.get('generated_from')!r} "
            f"!= expected {EXPECTED_GENERATED_FROM!r}"
        )
    if census.get("salvaged_from") != EXPECTED_SALVAGED_FROM:
        errors.append(
            f"{CENSUS_JSON}: salvaged_from {census.get('salvaged_from')!r} "
            f"!= expected {EXPECTED_SALVAGED_FROM!r}"
        )
    if census.get("consumers") != CENSUS_CONSUMERS:
        errors.append(
            f"{CENSUS_JSON}: consumers must equal {CENSUS_CONSUMERS!r}"
        )

    durable = census.get("durable_evidence")
    if not isinstance(durable, dict):
        errors.append(f"{CENSUS_JSON}: durable_evidence must be an object")
        durable = {}

    expected_archives = {
        "pre_zip": (PRE_ARCHIVE.as_posix(), PRE_ARCHIVE_SHA256),
        "post_zip": (POST_ARCHIVE.as_posix(), POST_ARCHIVE_SHA256),
        "sweep_zip": (SWEEP_ARCHIVE.as_posix(), SWEEP_ARCHIVE_SHA256),
    }
    for key, (expected_path, expected_sha) in expected_archives.items():
        entry = durable.get(key)
        if not isinstance(entry, dict):
            errors.append(f"{CENSUS_JSON}: durable_evidence.{key} must be an object")
            continue
        if entry.get("path") != expected_path:
            errors.append(
                f"{CENSUS_JSON}: durable_evidence.{key}.path "
                f"{entry.get('path')!r} != {expected_path!r}"
            )
        if entry.get("sha256") != expected_sha:
            errors.append(
                f"{CENSUS_JSON}: durable_evidence.{key}.sha256 "
                f"{entry.get('sha256')!r} != {expected_sha!r}"
            )

    sweep_members = durable.get("sweep_members")
    if not isinstance(sweep_members, dict):
        errors.append(f"{CENSUS_JSON}: durable_evidence.sweep_members must be an object")
        sweep_members = {}
    recorded_sweep_names = set(sweep_members)
    expected_sweep_names = set(EXPECTED_SWEEP_MEMBERS)
    if recorded_sweep_names != expected_sweep_names:
        errors.append(
            f"{CENSUS_JSON}: durable_evidence.sweep_members names "
            f"{sorted(recorded_sweep_names)!r} != expected {sorted(expected_sweep_names)!r}"
        )
    for member, expected in EXPECTED_SWEEP_MEMBERS.items():
        entry = sweep_members.get(member)
        if not isinstance(entry, dict):
            errors.append(
                f"{CENSUS_JSON}: durable_evidence.sweep_members.{member} must be an object"
            )
            continue
        _append_difference(
            errors,
            f"durable_evidence.sweep_members.{member}",
            expected,
            entry,
        )

    expected_pre_aggregate = _aggregate_for_census(pre_records, pre_scores)
    errors.extend(_validate_prereg_baseline(repo, expected_pre_aggregate))

    expected_lanes = {
        "pre": (pre_records, pre_scores),
        "post": (post_records, post_scores),
    }
    for lane, (records, scorelines) in expected_lanes.items():
        actual = census.get(lane)
        if not isinstance(actual, dict):
            errors.append(f"{CENSUS_JSON}: {lane} must be an object")
            continue
        expected_rows = [_record_for_census(record) for record in records]
        actual_rows = actual.get("rows")
        if not isinstance(actual_rows, list):
            errors.append(f"{CENSUS_JSON}: {lane}.rows must be an array")
        elif len(actual_rows) != len(expected_rows):
            errors.append(
                f"{CENSUS_JSON}: {lane}.rows length: expected {len(expected_rows)}, "
                f"got {len(actual_rows)}"
            )
        else:
            for index, expected_row in enumerate(expected_rows):
                actual_row = actual_rows[index]
                ident = f"{expected_row['seed']} team {expected_row['team']}"
                if not isinstance(actual_row, dict):
                    errors.append(
                        f"{CENSUS_JSON}: {lane}.rows[{index}] {ident}: expected object, "
                        f"got {type(actual_row).__name__}"
                    )
                    continue
                diff = _first_difference(expected_row, actual_row)
                if diff is not None:
                    field, expected_value, actual_value = diff
                    errors.append(
                        f"{CENSUS_JSON}: {lane}.rows[{index}] {ident} {field}: "
                        f"expected {expected_value!r}, got {actual_value!r}"
                    )
                    break

        expected_aggregate = _aggregate_for_census(records, scorelines)
        actual_aggregate = actual.get("aggregate")
        if not isinstance(actual_aggregate, dict):
            errors.append(f"{CENSUS_JSON}: {lane}.aggregate must be an object")
        else:
            _append_difference(
                errors,
                f"{lane}.aggregate",
                expected_aggregate,
                actual_aggregate,
            )

    provenance = census.get("provenance")
    if not isinstance(provenance, dict):
        errors.append(f"{CENSUS_JSON}: provenance must be an object")
        provenance = {}

    archive_specs = {
        "pre": (PRE_ARCHIVE, PRE_ARCHIVE_SHA256),
        "post": (POST_ARCHIVE, POST_ARCHIVE_SHA256),
    }
    archive_hashes: dict[str, dict[str, str]] = {}
    for lane, (archive, archive_sha) in archive_specs.items():
        lane_provenance = provenance.get(lane)
        if not isinstance(lane_provenance, dict):
            errors.append(f"{CENSUS_JSON}: provenance.{lane} must be an object")
            lane_provenance = {}
        if lane_provenance.get("artifact_zip_sha256") != archive_sha:
            errors.append(f"{CENSUS_JSON}: {lane} provenance ZIP hash is stale")
        try:
            member_hashes, _ = _archive_member_hashes(repo, archive, archive_sha)
            archive_hashes[lane] = member_hashes
            for field, member in (
                ("instrument_output_sha256", "instrument-output.txt"),
                ("measurement_sha256", "measurement.txt"),
            ):
                actual_digest = member_hashes.get(member)
                if actual_digest is None:
                    errors.append(f"{archive}: missing required member {member}")
                elif lane_provenance.get(field) != actual_digest:
                    errors.append(
                        f"{CENSUS_JSON}: provenance.{lane}.{field} "
                        f"{lane_provenance.get(field)!r} != archive {actual_digest!r}"
                    )
                expected_digest = EXPECTED_ARCHIVE_MEMBERS[lane][member]
                if actual_digest is not None and actual_digest != expected_digest:
                    errors.append(
                        f"{archive}:{member}: SHA-256 {actual_digest} "
                        f"!= expected {expected_digest}"
                    )
        except (KeyError, OSError, RuntimeError, ValueError, zipfile.BadZipFile) as exc:
            errors.append(f"{archive}: archive validation failed: {exc}")

    errors.extend(_validate_manifest_hash_rows(repo, archive_hashes))

    try:
        sweep_data = _committed_bytes(repo, SWEEP_ARCHIVE, SWEEP_ARCHIVE_SHA256)
        with zipfile.ZipFile(io.BytesIO(sweep_data)) as zf:
            archive_names = {name for name in zf.namelist() if not name.endswith("/")}
            if archive_names != recorded_sweep_names:
                errors.append(
                    f"{SWEEP_ARCHIVE}: member names {sorted(archive_names)!r} "
                    f"!= census {sorted(recorded_sweep_names)!r}"
                )
            if archive_names != expected_sweep_names:
                errors.append(
                    f"{SWEEP_ARCHIVE}: member names {sorted(archive_names)!r} "
                    f"!= expected {sorted(expected_sweep_names)!r}"
                )
            for member in sorted(archive_names & expected_sweep_names):
                data = zf.read(member)
                actual_sha = hashlib.sha256(data).hexdigest()
                actual_size = len(data)
                expected = EXPECTED_SWEEP_MEMBERS[member]
                if actual_sha != expected["sha256"]:
                    errors.append(
                        f"{SWEEP_ARCHIVE}:{member}: SHA-256 {actual_sha} "
                        f"!= expected {expected['sha256']}"
                    )
                if actual_size != expected["size"]:
                    errors.append(
                        f"{SWEEP_ARCHIVE}:{member}: size {actual_size} "
                        f"!= expected {expected['size']}"
                    )
    except (KeyError, OSError, RuntimeError, TypeError, ValueError, zipfile.BadZipFile) as exc:
        errors.append(f"{SWEEP_ARCHIVE}: static sweep validation failed: {exc}")

    return errors


def validate_census_json(
    repo: Path,
    pre_records: list[dict[str, object]],
    pre_scores: list[str],
    post_records: list[dict[str, object]],
    post_scores: list[str],
) -> list[str]:
    try:
        census = json.loads((repo / CENSUS_JSON).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{CENSUS_JSON}: could not load census JSON: {exc}"]
    return validate_census_payload(
        repo,
        census,
        pre_records,
        pre_scores,
        post_records,
        post_scores,
    )


def validate_accounting(records: list[dict[str, object]]) -> list[str]:
    errors: list[str] = []
    for record in records:
        exits = record["exits"]
        assert isinstance(exits, dict)
        samples = int(record["samples"])
        latest = int(record["latestPass"])
        upper = samples - int(exits.get("InPossession", 0)) - int(exits.get("Cooldown", 0)) - int(exits.get("StaleTick", 0))
        ident = f'{record["seed"]} team {record["team"]}'
        if latest > upper:
            errors.append(f"{ident}: latestPass={latest} exceeds eligible upper bound {upper}")
        exit_total = sum(int(v) for v in exits.values())
        if exit_total != samples:
            errors.append(f"{ident}: exit total {exit_total} != samples {samples}")
    return errors


def _clean_cell(cell: str) -> str:
    return cell.replace("**", "").replace("`", "").replace(",", "").strip()


def _format_expected(values: list[int]) -> str:
    return " / ".join(str(v) for v in values)


def validate_table(doc: str, pre: dict[str, int], post: dict[str, int]) -> list[str]:
    errors: list[str] = []
    section = doc.split("## Aggregate comparison", 1)[1].split("## Semantics boundary", 1)[0]
    rows: dict[str, tuple[str, str]] = {}
    for line in section.splitlines():
        if not line.startswith("|") or line.startswith("|---"):
            continue
        cells = [cell.strip() for cell in line.strip("|").split("|")]
        if len(cells) < 3 or cells[0] == "Surface":
            continue
        rows[_clean_cell(cells[0])] = (_clean_cell(cells[1]), _clean_cell(cells[2]))

    for label, paths in EXPECTED_ROWS.items():
        if label not in rows:
            errors.append(f"aggregate table missing row {label!r}")
            continue
        expected_pre = _format_expected([pre.get(path, 0) for path in paths])
        expected_post = _format_expected([post.get(path, 0) for path in paths])
        actual_pre, actual_post = rows[label]
        if actual_pre != expected_pre:
            errors.append(f"{label}: pre table {actual_pre!r} != census {expected_pre!r}")
        if actual_post != expected_post:
            errors.append(f"{label}: post table {actual_post!r} != census {expected_post!r}")
    return errors


def validate(repo: Path) -> list[str]:
    doc_path = repo / DOC
    errors: list[str] = []

    post_census = extract_census(_read_member(repo, POST_ARCHIVE, POST_ARCHIVE_SHA256, "instrument-output.txt"))
    pre_census = extract_census(_read_member(repo, PRE_ARCHIVE, PRE_ARCHIVE_SHA256, "instrument-output.txt"))
    doc = _normalize_newlines(doc_path.read_text(encoding="utf-8"))
    doc_census = extract_doc_census(doc)

    if doc_census != post_census:
        errors.append("committed post-#398 census block does not exactly match durable artifact")

    post_records, post_scores = parse_census(post_census)
    pre_records, pre_scores = parse_census(pre_census)
    errors.extend(validate_accounting(post_records))
    errors.extend(validate_accounting(pre_records))
    errors.extend(validate_census_json(repo, pre_records, pre_scores, post_records, post_scores))

    if pre_scores != post_scores:
        errors.append(f"pre/post scorelines differ: {pre_scores!r} != {post_scores!r}")

    pre_agg = aggregate(pre_records)
    post_agg = aggregate(post_records)
    errors.extend(validate_table(doc, pre_agg, post_agg))

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=".", help="repository root")
    args = parser.parse_args(argv)
    errors = validate(Path(args.repo).resolve())
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1
    print("W12 evidence reconciliation: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
