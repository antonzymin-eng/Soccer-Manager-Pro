#!/usr/bin/env python3
"""Verify owner-held RED results match the exact recorded disposition."""

from __future__ import annotations

import argparse
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ledger", type=Path, required=True)
    parser.add_argument("--results", type=Path, required=True)
    parser.add_argument("--dotnet-exit", type=int, required=True)
    return parser.parse_args()


def normalize(text: str) -> str:
    return text.replace("−", "-").replace("\u2013", "-").replace("\u2014", "-")


def load_ledger(path: Path) -> dict[str, tuple[str, ...]]:
    entries: dict[str, tuple[str, ...]] = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = [part.strip() for part in line.split("|")]
        name, tokens = parts[0], tuple(parts[1:])
        if not name or name in entries:
            raise ValueError(f"invalid/duplicate owner-held RED entry: {line!r}")
        if not tokens:
            raise ValueError(f"owner-held RED entry has no diagnostic tokens: {name}")
        entries[name] = tokens
    return entries


def collect_results(results_dir: Path) -> dict[str, tuple[str, str]]:
    found: dict[str, tuple[str, str]] = {}
    for trx in sorted(results_dir.rglob("*.trx")):
        root = ET.parse(trx).getroot()
        for elem in root.iter():
            if not elem.tag.endswith("UnitTestResult"):
                continue
            name = elem.attrib.get("testName", "")
            outcome = elem.attrib.get("outcome", "")
            body = normalize(" ".join(elem.itertext()))
            if name:
                found[name] = (outcome, body)
    return found


def find_result(expected_name: str, results: dict[str, tuple[str, str]]) -> tuple[str, str] | None:
    exact = results.get(expected_name)
    if exact is not None:
        return exact
    matches = [
        result for name, result in results.items()
        if name.endswith("." + expected_name) or expected_name in name
    ]
    if len(matches) == 1:
        return matches[0]
    return None


def main() -> int:
    args = parse_args()
    try:
        ledger = load_ledger(args.ledger)
    except ValueError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    results = collect_results(args.results)
    if not ledger:
        print("Owner-held RED ledger empty.")
        return 0

    failed_expected = 0
    for name, tokens in ledger.items():
        result = find_result(name, results)
        if result is None:
            print(f"ERROR: owner-held RED result missing or ambiguous: {name}", file=sys.stderr)
            return 1
        outcome, body = result
        if outcome == "Passed":
            print(
                f"ERROR: owner-held RED unexpectedly passed: {name}; remove/review the exception before merge.",
                file=sys.stderr,
            )
            return 1
        if outcome != "Failed":
            print(
                f"ERROR: owner-held RED {name} ended with outcome={outcome!r}, expected Failed.",
                file=sys.stderr,
            )
            return 1
        missing = [token for token in tokens if normalize(token) not in body]
        if missing:
            print(
                f"ERROR: owner-held RED {name} changed diagnostics; missing token(s): "
                + ", ".join(missing),
                file=sys.stderr,
            )
            return 1
        failed_expected += 1
        print(f"OWNER-HELD RED MATCHES RECORDED BASELINE: {name}")

    if failed_expected and args.dotnet_exit == 0:
        print(
            "ERROR: dotnet reported success even though a recorded owner-held test failed.",
            file=sys.stderr,
        )
        return 1
    if args.dotnet_exit != 1:
        print(
            f"ERROR: owner-held RED run exited {args.dotnet_exit}; expected test-failure exit code 1.",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
