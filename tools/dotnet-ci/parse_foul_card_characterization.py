#!/usr/bin/env python3
"""Parse the frozen #435 foul/card characterization report into TSV/JSON."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

START = "=== #435 §2.1 source-complete foul/card measurement ==="
AGGREGATE = "--- aggregate #435 §2.1 discipline stream ---"
END_PREFIX = "Real-football reference:"
SEED_RE = re.compile(r"^seed (0x[0-9A-F]{16}):$")
KV_RE = re.compile(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)")
RATE_RE = re.compile(
    r"^SHIPPED per-90-min rates: fouls=(\S+) yellows=(\S+) straightReds=(\S+) "
    r"secondYellowDismissals=(\S+) totalDismissals=(\S+)$"
)

FROZEN_SEEDS = (
    "0x0F1E2D3C4B5A6978",
    "0x00000000D1A6D05E",
    "0x0000000000000001",
    "0x00000000ABCDEF12",
    "0x0000000099887766",
    "0x000000005A5A5A5A",
)

FIELDS = [
    "fromBehindCandidates",
    "fromBehindPricedCandidates",
    "fromBehindCalled",
    "fromBehindWavedOn",
    "fromBehindCandidatesDroppedByStrongerSameTick",
    "fromBehindSentOffSlotOccupancies",
    "fromBehindCandidatesShadowedBySentOffWinner",
    "candidateDisplacedByDecided",
    "foulCooldownSuppressionsFromBehind",
    "pricedCandidateIdentityChecks",
    "pricedCandidateIdentityMismatches",
    "slideTackleCandidates",
    "slideTackleCalled",
    "slideTackleCallsDuringFoulCooldown",
    "slideTackleRaisedDuringCooldownApplied",
    "slideTackleCallsDuringCollisionSuppressionWindow",
    "slideTackleRaisedDuringCollisionSuppressionApplied",
    "totalFouls",
    "yellowCards",
    "straightReds",
    "secondYellowDismissals",
    "totalDismissals",
    "playedTicks",
]

RATE_FIELDS = (
    "foulsPer90",
    "yellowsPer90",
    "straightRedsPer90",
    "secondYellowDismissalsPer90",
    "totalDismissalsPer90",
)

RATE_SPECS = (
    ("foulsPer90", "totalFouls", 2),
    ("yellowsPer90", "yellowCards", 2),
    ("straightRedsPer90", "straightReds", 3),
    ("secondYellowDismissalsPer90", "secondYellowDismissals", 3),
    ("totalDismissalsPer90", "totalDismissals", 3),
)


def extract_report(text: str) -> str:
    start = text.find(START)
    if start < 0:
        raise ValueError(f"missing report sentinel: {START}")
    end_line = text.find(END_PREFIX, start)
    if end_line < 0:
        raise ValueError(f"missing report terminator: {END_PREFIX}")
    end = text.find("\n", end_line)
    if end < 0:
        end = len(text)
    else:
        end += 1
    return text[start:end]


def _validate_identities(row: dict[str, int | float | str], label: str) -> None:
    if row["fromBehindCandidates"] != (
        row["foulCooldownSuppressionsFromBehind"]
        + row["candidateDisplacedByDecided"]
        + row["fromBehindCandidatesDroppedByStrongerSameTick"]
        + row["fromBehindCandidatesShadowedBySentOffWinner"]
        + row["fromBehindPricedCandidates"]
    ):
        raise ValueError(f"{label} collision funnel does not reconcile")
    if row["fromBehindPricedCandidates"] != row["fromBehindCalled"] + row["fromBehindWavedOn"]:
        raise ValueError(f"{label} priced-candidate partition does not reconcile")
    if row["fromBehindCalled"] + row["slideTackleCalled"] != row["totalFouls"]:
        raise ValueError(f"{label} foul-source identity does not reconcile")
    if row["straightReds"] + row["secondYellowDismissals"] != row["totalDismissals"]:
        raise ValueError(f"{label} dismissal identity does not reconcile")
    if row["pricedCandidateIdentityMismatches"] != 0:
        raise ValueError(f"{label} priced candidate identity mismatch is non-zero")


def parse_report(report: str) -> dict:
    rows: list[dict[str, int | str]] = []
    aggregate: dict[str, int | float | str] = {"scope": "aggregate"}
    rate_tokens: dict[str, str] = {}
    current: dict[str, int | str] | None = None
    in_aggregate = False

    for raw in report.splitlines():
        line = raw.strip()
        seed_match = SEED_RE.match(line)
        if seed_match:
            if current is not None:
                rows.append(current)
            current = {"scope": "seed", "seed": seed_match.group(1)}
            in_aggregate = False
            continue
        if line == AGGREGATE:
            if current is not None:
                rows.append(current)
                current = None
            in_aggregate = True
            continue

        target = aggregate if in_aggregate else current
        if target is not None:
            for key, value in KV_RE.findall(line):
                if key in FIELDS or (in_aggregate and key == "agentAgentContacts"):
                    try:
                        target[key] = int(value)
                    except ValueError:
                        pass

        if in_aggregate:
            rate_match = RATE_RE.match(line)
            if rate_match:
                rate_tokens = {
                    field: token
                    for field, token in zip(RATE_FIELDS, rate_match.groups())
                }

    if current is not None:
        rows.append(current)

    observed_seeds = tuple(str(row.get("seed", "")) for row in rows)
    if observed_seeds != FROZEN_SEEDS:
        raise ValueError(
            "seed corpus/order does not match frozen #435 corpus: "
            f"expected={FROZEN_SEEDS}, observed={observed_seeds}"
        )

    for row in rows:
        missing = [field for field in FIELDS if field not in row]
        if missing:
            raise ValueError(f"seed {row.get('seed')} missing fields: {missing}")
        _validate_identities(row, f"seed {row['seed']}")

    missing_aggregate = [field for field in FIELDS if field not in aggregate]
    if missing_aggregate:
        raise ValueError(f"aggregate missing fields: {missing_aggregate}")
    missing_rates = [field for field in RATE_FIELDS if field not in rate_tokens]
    if missing_rates:
        raise ValueError(f"aggregate missing shipped per-90 rates: {missing_rates}")

    _validate_identities(aggregate, "aggregate")

    for field in FIELDS:
        seed_sum = sum(int(row[field]) for row in rows)
        if aggregate[field] != seed_sum:
            raise ValueError(
                f"aggregate {field}={aggregate[field]} does not equal seed sum {seed_sum}"
            )

    for rate_field, count_field, decimals in RATE_SPECS:
        expected = f"{int(aggregate[count_field]) / len(FROZEN_SEEDS):.{decimals}f}"
        observed = rate_tokens[rate_field]
        if observed != expected:
            raise ValueError(
                f"aggregate shipped rate {rate_field}={observed} does not match expected {expected}"
            )
        aggregate[rate_field] = float(observed)

    return {"seeds": rows, "aggregate": aggregate}


def write_tsv(parsed: dict, path: Path) -> None:
    columns = ["scope", "seed", *FIELDS]
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write("\t".join(columns) + "\n")
        for row in parsed["seeds"]:
            handle.write("\t".join(str(row.get(col, "")) for col in columns) + "\n")
        agg = parsed["aggregate"]
        agg_row = {**agg, "seed": "ALL"}
        handle.write("\t".join(str(agg_row.get(col, "")) for col in columns) + "\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path, help="instrument-output.txt or extracted verbatim report")
    parser.add_argument("--report-out", type=Path, required=True)
    parser.add_argument("--tsv-out", type=Path, required=True)
    parser.add_argument("--json-out", type=Path, required=True)
    args = parser.parse_args()

    text = args.input.read_text(encoding="utf-8")
    report = extract_report(text) if START in text and "NUnit Adapter" in text else text
    if not report.startswith(START):
        report = extract_report(text)
    parsed = parse_report(report)
    args.report_out.write_text(report, encoding="utf-8", newline="\n")
    write_tsv(parsed, args.tsv_out)
    args.json_out.write_text(
        json.dumps(parsed, indent=2, sort_keys=True, allow_nan=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
