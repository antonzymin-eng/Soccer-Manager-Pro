#!/usr/bin/env python3
"""Validate the durable W12 pre/post evidence against the committed comparison doc."""

from __future__ import annotations

import argparse
import re
import sys
import zipfile
from pathlib import Path

POST_ARCHIVE = Path("docs/tracking/evidence/w12/pr398-post-w12-34847990460.zip")
PRE_ARCHIVE = Path("docs/tracking/evidence/w12/w12-corrected-pre398-34844425733.zip")
DOC = Path("docs/tracking/w12-gate-firing-post398-comparison.md")
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


def _read_member(archive: Path, member: str) -> str:
    with zipfile.ZipFile(archive) as zf:
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
    post_archive = repo / POST_ARCHIVE
    pre_archive = repo / PRE_ARCHIVE
    doc_path = repo / DOC
    errors: list[str] = []

    post_census = extract_census(_read_member(post_archive, "instrument-output.txt"))
    pre_census = extract_census(_read_member(pre_archive, "instrument-output.txt"))
    doc = _normalize_newlines(doc_path.read_text(encoding="utf-8"))
    doc_census = extract_doc_census(doc)

    if doc_census != post_census:
        errors.append("committed post-#398 census block does not exactly match durable artifact")

    post_records, post_scores = parse_census(post_census)
    pre_records, pre_scores = parse_census(pre_census)
    errors.extend(validate_accounting(post_records))
    errors.extend(validate_accounting(pre_records))

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
