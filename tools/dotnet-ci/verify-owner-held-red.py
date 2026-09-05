#!/usr/bin/env python3
"""Verify owner-held RED results match the exact recorded disposition."""

from __future__ import annotations

import argparse
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NUMERIC_TOKEN_RE = re.compile(r"[-+]?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?")


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


def collect_results(results_dir: Path) -> list[tuple[str, str, str]]:
    found: list[tuple[str, str, str]] = []
    for trx in sorted(results_dir.rglob("*.trx")):
        root = ET.parse(trx).getroot()
        for elem in root.iter():
            if not elem.tag.endswith("UnitTestResult"):
                continue
            name = elem.attrib.get("testName", "")
            outcome = elem.attrib.get("outcome", "")
            body = normalize(" ".join(elem.itertext()))
            if name:
                found.append((name, outcome, body))
    return found


def method_leaf(test_name: str) -> str:
    """Return the exact method identity from a simple or fully-qualified TRX name."""
    without_args = test_name.split("(", 1)[0]
    return without_args.rsplit(".", 1)[-1]


def find_result(
    expected_name: str,
    results: list[tuple[str, str, str]],
) -> tuple[int, str, str] | None:
    matches = [
        (index, outcome, body)
        for index, (actual_name, outcome, body) in enumerate(results)
        if actual_name == expected_name or method_leaf(actual_name) == expected_name
    ]
    if len(matches) == 1:
        return matches[0]
    return None


def diagnostic_token_present(body: str, token: str) -> bool:
    """Require recorded numeric diagnostics as complete values, never prefixes."""
    normalized_body = normalize(body)
    normalized_token = normalize(token).strip()
    if NUMERIC_TOKEN_RE.fullmatch(normalized_token):
        escaped = re.escape(normalized_token)
        # `-0.165` must not match `-0.1659`; `0.407` must not match
        # `0.4078`. Assignment punctuation around the value remains allowed.
        return re.search(
            rf"(?<![0-9.+-]){escaped}(?![0-9.eE%])",
            normalized_body,
        ) is not None
    return normalized_token in normalized_body


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

    matched_indexes: set[int] = set()
    failed_expected = 0
    for name, tokens in ledger.items():
        result = find_result(name, results)
        if result is None:
            print(f"ERROR: owner-held RED result missing or ambiguous: {name}", file=sys.stderr)
            return 1
        index, outcome, body = result
        matched_indexes.add(index)
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
        missing = [token for token in tokens if not diagnostic_token_present(body, token)]
        if missing:
            print(
                f"ERROR: owner-held RED {name} changed diagnostics; missing token(s): "
                + ", ".join(missing),
                file=sys.stderr,
            )
            return 1
        failed_expected += 1
        print(f"OWNER-HELD RED MATCHES RECORDED BASELINE: {name}")

    unexpected = [results[i][0] for i in range(len(results)) if i not in matched_indexes]
    if unexpected:
        print(
            "ERROR: owner-held RED run returned unexpected additional test result(s): "
            + ", ".join(unexpected),
            file=sys.stderr,
        )
        return 1

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
