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


def parse_report(report: str) -> dict:
    rows: list[dict[str, int | str]] = []
    aggregate: dict[str, int | float | str] = {"scope": "aggregate"}
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
                aggregate["foulsPer90"] = float(rate_match.group(1))
                aggregate["yellowsPer90"] = float(rate_match.group(2))
                aggregate["straightRedsPer90"] = float(rate_match.group(3))
                aggregate["secondYellowDismissalsPer90"] = float(rate_match.group(4))
                aggregate["totalDismissalsPer90"] = float(rate_match.group(5))

    if current is not None:
        rows.append(current)
    if len(rows) != 6:
        raise ValueError(f"expected 6 seed rows, found {len(rows)}")
    for row in rows:
        missing = [field for field in FIELDS if field not in row]
        if missing:
            raise ValueError(f"seed {row.get('seed')} missing fields: {missing}")
    missing_aggregate = [field for field in FIELDS if field not in aggregate]
    if missing_aggregate:
        raise ValueError(f"aggregate missing fields: {missing_aggregate}")

    # Frozen structural identities from #435/#436. Fail closed if the retained report is inconsistent.
    if aggregate["fromBehindCandidates"] != (
        aggregate["foulCooldownSuppressionsFromBehind"]
        + aggregate["candidateDisplacedByDecided"]
        + aggregate["fromBehindCandidatesDroppedByStrongerSameTick"]
        + aggregate["fromBehindCandidatesShadowedBySentOffWinner"]
        + aggregate["fromBehindPricedCandidates"]
    ):
        raise ValueError("aggregate collision funnel does not reconcile")
    if aggregate["fromBehindCalled"] + aggregate["slideTackleCalled"] != aggregate["totalFouls"]:
        raise ValueError("aggregate foul-source identity does not reconcile")
    if aggregate["straightReds"] + aggregate["secondYellowDismissals"] != aggregate["totalDismissals"]:
        raise ValueError("aggregate dismissal identity does not reconcile")
    if aggregate["pricedCandidateIdentityMismatches"] != 0:
        raise ValueError("priced candidate identity mismatch is non-zero")

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
    args.json_out.write_text(json.dumps(parsed, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
