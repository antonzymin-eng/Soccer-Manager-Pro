#!/usr/bin/env python3
"""Verify owner-held RED results match the exact recorded disposition."""

from __future__ import annotations

import argparse
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NUMERIC_TOKEN_RE = re.compile(r"[-+]?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?")
FIELD_NAME_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_.-]*")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ledger", type=Path, required=True)
    parser.add_argument("--results", type=Path, required=True)
    parser.add_argument("--dotnet-exit", type=int, required=True)
    return parser.parse_args()


def normalize(text: str) -> str:
    return text.replace("−", "-").replace("\u2013", "-").replace("\u2014", "-")


def load_ledger(path: Path) -> dict[str, tuple[tuple[str, str], ...]]:
    """Load `test_name|field=value|field=value` owner-held expectations."""
    entries: dict[str, tuple[tuple[str, str], ...]] = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = [part.strip() for part in line.split("|")]
        name = parts[0]
        if not name or name in entries:
            raise ValueError(f"invalid/duplicate owner-held RED entry: {line!r}")
        if len(parts) < 2:
            raise ValueError(f"owner-held RED entry has no diagnostic fields: {name}")

        expectations: list[tuple[str, str]] = []
        seen_fields: set[str] = set()
        for item in parts[1:]:
            if "=" not in item:
                raise ValueError(
                    f"owner-held RED diagnostic must be field=value, got {item!r} for {name}"
                )
            field, expected = (piece.strip() for piece in item.split("=", 1))
            expected = normalize(expected)
            if not FIELD_NAME_RE.fullmatch(field) or field in seen_fields:
                raise ValueError(
                    f"invalid/duplicate owner-held RED diagnostic field {field!r} for {name}"
                )
            if not NUMERIC_TOKEN_RE.fullmatch(expected):
                raise ValueError(
                    f"owner-held RED diagnostic value must be numeric, got {expected!r} for {name}.{field}"
                )
            seen_fields.add(field)
            expectations.append((field, expected))
        entries[name] = tuple(expectations)
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


def diagnostic_field_values(body: str, field: str) -> list[str]:
    """Return complete numeric values assigned to an exact diagnostic field."""
    normalized_body = normalize(body)
    escaped_field = re.escape(field)
    pattern = re.compile(
        rf"(?<![A-Za-z0-9_.-]){escaped_field}\s*=\s*"
        rf"({NUMERIC_TOKEN_RE.pattern})(?![0-9.eE%])"
    )
    return [normalize(match.group(1)) for match in pattern.finditer(normalized_body)]


def diagnostic_field_matches(body: str, field: str, expected: str) -> bool:
    """Require one unambiguous field assignment equal to the recorded value."""
    values = diagnostic_field_values(body, field)
    return len(values) == 1 and values[0] == normalize(expected)


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
    for name, expectations in ledger.items():
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

        mismatched = [
            f"{field}={expected}"
            for field, expected in expectations
            if not diagnostic_field_matches(body, field, expected)
        ]
        if mismatched:
            print(
                f"ERROR: owner-held RED {name} changed diagnostics; missing, changed, or ambiguous field assignment(s): "
                + ", ".join(mismatched),
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
