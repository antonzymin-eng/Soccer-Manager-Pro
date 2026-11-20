"""Verify that the strict and broad #441 reports reached identical end states."""

import argparse
import lzma
from pathlib import Path
import re
import xml.etree.ElementTree as ET


STRICT_TEST = "HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters"
FINAL_TICK = "324000"
FROZEN_SEEDS = {
    "0x0F1E2D3C4B5A6978",
    "0x00000000D1A6D05E",
    "0x0000000000000001",
    "0x00000000ABCDEF12",
    "0x0000000099887766",
    "0x000000005A5A5A5A",
}


def strict_output(path):
    open_report = lzma.open if path.suffix == ".xz" else Path.open
    with open_report(path, "rb") as report:
        root = ET.parse(report)
    results = root.findall("./{*}Results/{*}UnitTestResult")
    if len(results) != 1 or results[0].get("testName") != STRICT_TEST:
        raise ValueError(f"{path}: expected exactly one {STRICT_TEST} result")
    if results[0].get("outcome") != "Passed":
        raise ValueError(f"{path}: strict census did not pass")
    output = results[0].findtext("./{*}Output/{*}StdOut")
    if not output:
        raise ValueError(f"{path}: strict census output is missing")
    return output


def end_digests(output, path):
    digests = {}
    for line in output.splitlines():
        if not line.startswith("seedEnd,"):
            continue
        _, seed, *fields = line.split(",")
        if seed not in FROZEN_SEEDS:
            raise ValueError(f"{path}: unexpected seed {seed}")
        if seed in digests:
            raise ValueError(f"{path}: duplicate seed {seed}")

        values = {}
        for field in fields:
            key, separator, value = field.partition("=")
            if not separator or key in values:
                raise ValueError(f"{path}: malformed end record for {seed}")
            values[key] = value
        digest = values.get("digest", "")
        if values.get("tick") != FINAL_TICK or not re.fullmatch(r"[0-9a-fA-F]{64}", digest):
            raise ValueError(f"{path}: invalid final tick or SHA-256 digest for {seed}")
        digests[seed] = digest.lower()

    missing = FROZEN_SEEDS - digests.keys()
    if missing:
        raise ValueError(f"{path}: missing end records for {', '.join(sorted(missing))}")
    return digests


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("strict_trx", type=Path, help="Strict census .trx or .trx.xz")
    parser.add_argument("broad_report", type=Path, help="Broad census text report")
    args = parser.parse_args()

    try:
        strict = end_digests(strict_output(args.strict_trx), args.strict_trx)
        broad = end_digests(args.broad_report.read_text(encoding="utf-8"), args.broad_report)
        for seed in sorted(FROZEN_SEEDS):
            if strict[seed] != broad[seed]:
                raise ValueError(f"{seed}: strict {strict[seed]} != broad {broad[seed]}")
    except (OSError, EOFError, ET.ParseError, ValueError) as error:
        parser.exit(1, f"Digest verification failed: {error}\n")

    print(f"Verified {len(FROZEN_SEEDS)} final digests at tick {FINAL_TICK}.")


if __name__ == "__main__":
    main()
