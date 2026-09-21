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
    keys = {
        "samples",
        "latestPass",
        "active",
        "primaryAssigned",
        "coverShadows",
        *{f"phase.{k}" for k in ("InPoss", "OutOfPoss", "TransToAtk", "TransToDef")},
        *{f"exits.{k}" for k in (
            "InPossession", "NoCommittedTrigger", "Active", "NoPrimaryPresser",
            "InvariantRejected", "Disengaged", "Cooldown", "StaleTick"
        )},
        *{f"raw.{k}" for k in ("BadTouch", "BackwardPass", "SidelineTrap", "WeakReceiver")},
        *{f"committed.{k}" for k in ("BadTouch", "BackwardPass", "SidelineTrap", "WeakReceiver")},
    }
    return {key: sum(_value(record, key) for record in records) for key in keys}



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


def _aggregate_for_census(
    records: list[dict[str, object]], scorelines: list[str]
) -> dict[str, object]:
    values = aggregate(records)
    exit_names = (
        "Active",
        "Cooldown",
        "Disengaged",
        "InPossession",
        "InvariantRejected",
        "NoCommittedTrigger",
        "NoPrimaryPresser",
    )
    trigger_names = ("BackwardPass", "BadTouch", "SidelineTrap", "WeakReceiver")
    return {
        "active": values["active"],
        "committed": {name: values[f"committed.{name}"] for name in trigger_names},
        "coverShadows": values["coverShadows"],
        "exits": {name: values[f"exits.{name}"] for name in exit_names},
        "latestPass": values["latestPass"],
        "nonInPossession": values["samples"] - values["exits.InPossession"],
        "primaryAssigned": values["primaryAssigned"],
        "raw": {name: values[f"raw.{name}"] for name in trigger_names},
        "samples": values["samples"],
        "scorelines": scorelines,
    }


def validate_census_json(
    repo: Path,
    pre_records: list[dict[str, object]],
    pre_scores: list[str],
    post_records: list[dict[str, object]],
    post_scores: list[str],
) -> list[str]:
    errors: list[str] = []
    try:
        census = json.loads((repo / CENSUS_JSON).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{CENSUS_JSON}: could not load census JSON: {exc}"]

    if census.get("schema_version") != 2:
        errors.append(f"{CENSUS_JSON}: schema_version must be 2")

    durable = census.get("durable_evidence", {})
    expected_archives = {
        "pre_zip": (PRE_ARCHIVE.as_posix(), PRE_ARCHIVE_SHA256),
        "post_zip": (POST_ARCHIVE.as_posix(), POST_ARCHIVE_SHA256),
        "sweep_zip": (SWEEP_ARCHIVE.as_posix(), SWEEP_ARCHIVE_SHA256),
    }
    for key, (expected_path, expected_sha) in expected_archives.items():
        entry = durable.get(key, {})
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

    expected_lanes = {
        "pre": (pre_records, pre_scores),
        "post": (post_records, post_scores),
    }
    for lane, (records, scorelines) in expected_lanes.items():
        actual = census.get(lane, {})
        expected_rows = [_record_for_census(record) for record in records]
        expected_aggregate = _aggregate_for_census(records, scorelines)
        if actual.get("rows") != expected_rows:
            errors.append(f"{CENSUS_JSON}: {lane}.rows differ from committed archive census")
        if actual.get("aggregate") != expected_aggregate:
            errors.append(
                f"{CENSUS_JSON}: {lane}.aggregate differs from committed archive census"
            )

    provenance = census.get("provenance", {})
    if provenance.get("pre", {}).get("artifact_zip_sha256") != PRE_ARCHIVE_SHA256:
        errors.append(f"{CENSUS_JSON}: pre provenance ZIP hash is stale")
    if provenance.get("post", {}).get("artifact_zip_sha256") != POST_ARCHIVE_SHA256:
        errors.append(f"{CENSUS_JSON}: post provenance ZIP hash is stale")

    try:
        sweep_data = _committed_bytes(repo, SWEEP_ARCHIVE, SWEEP_ARCHIVE_SHA256)
        with zipfile.ZipFile(io.BytesIO(sweep_data)) as zf:
            for member, expected in durable.get("sweep_members", {}).items():
                data = zf.read(member)
                actual_sha = hashlib.sha256(data).hexdigest()
                actual_size = len(data)
                if actual_sha != expected.get("sha256"):
                    errors.append(
                        f"{SWEEP_ARCHIVE}:{member}: SHA-256 {actual_sha} "
                        f"!= {expected.get('sha256')}"
                    )
                if actual_size != expected.get("size"):
                    errors.append(
                        f"{SWEEP_ARCHIVE}:{member}: size {actual_size} "
                        f"!= {expected.get('size')}"
                    )
    except (KeyError, OSError, RuntimeError, ValueError, zipfile.BadZipFile) as exc:
        errors.append(f"{SWEEP_ARCHIVE}: static sweep validation failed: {exc}")

    return errors

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
