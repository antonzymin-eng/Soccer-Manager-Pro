#!/usr/bin/env python3
"""Canonical registry and verifier for System XI env-gated match measurements."""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


@dataclass(frozen=True)
class Instrument:
    instrument_id: str
    env_var: str
    source: str
    project: str
    test_filter: str
    sentinel: str


INSTRUMENTS = {
    "tackle-intent": Instrument(
        "tackle-intent",
        "TD_TACKLE_DIAGNOSTIC",
        "src/match-engine/tests/TackleIntentDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~TackleIntentDiagnostic",
        "=== Tackle-intent census (wiring backlog W2) ===",
    ),
    "gk-rush": Instrument(
        "gk-rush",
        "TD_GK_DIAGNOSTIC",
        "src/match-engine/tests/GkRushDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~GkRushDiagnostic",
        "=== Keeper rush anatomy (wiring backlog W1) ===",
    ),
    "gk-save": Instrument(
        "gk-save",
        "TD_GK_DIAGNOSTIC",
        "src/match-engine/tests/GkSaveDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~GkSaveDiagnostic",
        "=== §5.Z.15 follow-up: does the goalkeeper ever actually make a save? ===",
    ),
    "gk-contact-rate": Instrument(
        "gk-contact-rate",
        "TD_GK_DIAGNOSTIC",
        "src/match-engine/tests/GkContactRateDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~GkContactRateDiagnostic",
        "=== Keeper contact-rate anatomy: WHY does the keeper miss three quarters of on-target shots? ===",
    ),
    "goal-conversion": Instrument(
        "goal-conversion",
        "TD_CONVERSION_DIAGNOSTIC",
        "src/match-engine/tests/GoalConversionDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~GoalConversionDiagnostic",
        "REPORT A — what a keeper contact DOES.",
    ),
    "shot-outcome": Instrument(
        "shot-outcome",
        "TD_SHOT_DIAGNOSTIC",
        "src/match-engine/tests/ShotOutcomeDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~ShotOutcomeDiagnostic",
        "=== §5.Z.17 residual: the shot-outcome distribution ===",
    ),
    "foul-rate": Instrument(
        "foul-rate",
        "TD_FOUL_DIAGNOSTIC",
        "src/match-engine/tests/FoulRateDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~FoulRateDiagnostic",
        "=== §5.Z.9 foul-rate measurement ===",
    ),
    "close-chance": Instrument(
        "close-chance",
        "TD_CREATION_DIAGNOSTIC",
        "src/match-engine/tests/CloseChanceDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~CloseChanceDiagnostic",
        "=== close-chance creation: the final-third -> penalty-area transition ===",
    ),
    "match-balance-scoreline": Instrument(
        "match-balance-scoreline",
        "TD_BALANCE_DIAGNOSTIC",
        "src/match-engine/tests/MatchBalanceDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~MatchBalanceDiagnostic_ReportsScorelineAsymmetry",
        "=== §5.Z.11 home/away asymmetry + goal rate: FULL-MATCH per-team measurement ===",
    ),
    "match-balance-contact": Instrument(
        "match-balance-contact",
        "TD_BALANCE_DIAGNOSTIC",
        "src/match-engine/tests/MatchBalanceDiagnosticTests.cs",
        "src/match-engine/tests/match-engine-tests.gen.csproj",
        "FullyQualifiedName~MatchBalanceDiagnostic_ReportsContactStream",
        "=== §5.Z.9 recorded finding: the CONTACT STREAM itself ===",
    ),
    "engine-scoring": Instrument(
        "engine-scoring",
        "TD_ENGINE_DIAGNOSTIC",
        "src/season-save/tests/EngineScoringDiagnosticTests.cs",
        "src/season-save/tests/season-save-tests.gen.csproj",
        "FullyQualifiedName~EngineScoringDiagnostic",
        "ERR-030-014 [",
    ),
}


class MeasurementError(RuntimeError):
    pass


def get_instrument(instrument_id: str) -> Instrument:
    try:
        return INSTRUMENTS[instrument_id]
    except KeyError as exc:
        available = ", ".join(sorted(INSTRUMENTS))
        raise MeasurementError(
            f"unknown instrument {instrument_id!r}; available: {available}"
        ) from exc


def _filter_probe(test_filter: str) -> str:
    if "~" not in test_filter:
        return test_filter
    return test_filter.rsplit("~", 1)[1]


def _asmdef_for_project(project: str) -> str:
    suffix = ".gen.csproj"
    if not project.endswith(suffix):
        raise MeasurementError(f"generated project path must end with {suffix}: {project}")
    return project[: -len(suffix)] + ".asmdef"


def validate_repo(repo: Path) -> None:
    errors: list[str] = []
    seen_ids: set[str] = set()
    for key, inst in INSTRUMENTS.items():
        if key != inst.instrument_id:
            errors.append(f"{key}: key does not match instrument_id {inst.instrument_id!r}")
        if inst.instrument_id in seen_ids:
            errors.append(f"{key}: duplicate instrument_id")
        seen_ids.add(inst.instrument_id)
        if not inst.env_var.startswith("TD_"):
            errors.append(f"{key}: env var must start TD_: {inst.env_var}")
        if "\n" in inst.sentinel or "\r" in inst.sentinel:
            errors.append(f"{key}: sentinel must be one line")

        source = repo / inst.source
        if not source.is_file():
            errors.append(f"{key}: missing source {inst.source}")
            continue
        text = source.read_text(encoding="utf-8")
        if inst.env_var not in text:
            errors.append(f"{key}: {inst.env_var} absent from {inst.source}")
        probe = _filter_probe(inst.test_filter)
        if probe not in text:
            errors.append(f"{key}: filter probe {probe!r} absent from {inst.source}")
        if inst.sentinel not in text:
            errors.append(f"{key}: sentinel absent from {inst.source}")

        asmdef = repo / _asmdef_for_project(inst.project)
        if not asmdef.is_file():
            errors.append(
                f"{key}: owning asmdef missing for generated project {inst.project}"
            )

    if errors:
        raise MeasurementError("catalog validation failed:\n  - " + "\n  - ".join(errors))


def write_github_env(inst: Instrument, path: Path) -> None:
    values = {
        "MEASUREMENT_INSTRUMENT": inst.instrument_id,
        "MEASUREMENT_ENV": inst.env_var,
        "MEASUREMENT_FILTER": inst.test_filter,
        "MEASUREMENT_PROJECT": inst.project,
        "MEASUREMENT_SENTINEL": inst.sentinel,
    }
    for name, value in values.items():
        if "\n" in value or "\r" in value:
            raise MeasurementError(f"{name} cannot contain a newline")
    with path.open("a", encoding="utf-8") as handle:
        for name, value in values.items():
            handle.write(f"{name}={value}\n")


def _local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def verify_run(inst: Instrument, trx_dir: Path, output: Path) -> None:
    trx_files = sorted(trx_dir.glob("*.trx"))
    if not trx_files:
        raise MeasurementError(f"no TRX result found under {trx_dir}")

    outcomes: list[str] = []
    for trx in trx_files:
        try:
            root = ET.parse(trx).getroot()
        except (ET.ParseError, OSError) as exc:
            raise MeasurementError(f"cannot parse TRX {trx}: {exc}") from exc
        for elem in root.iter():
            if _local_name(elem.tag) == "UnitTestResult":
                outcomes.append(elem.attrib.get("outcome", ""))

    if not outcomes:
        raise MeasurementError("TRX contains no UnitTestResult records")
    bad = [value for value in outcomes if value not in {"Passed", "NotExecuted"}]
    if bad:
        raise MeasurementError(
            "measurement TRX contains non-passing outcomes: " + ", ".join(bad)
        )
    if "Passed" not in outcomes:
        raise MeasurementError(
            "measurement did not execute a passing test; ignored/not-executed-only runs are not evidence"
        )

    try:
        text = output.read_text(encoding="utf-8")
    except OSError as exc:
        raise MeasurementError(f"cannot read measurement output {output}: {exc}") from exc
    if inst.sentinel not in text:
        raise MeasurementError(
            f"measurement test passed but expected report sentinel was absent: {inst.sentinel!r}"
        )


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("list", help="list canonical instrument ids")

    validate = sub.add_parser("validate", help="validate catalog against tracked sources")
    validate.add_argument("--repo", type=Path, default=Path("."))

    resolve = sub.add_parser("resolve", help="resolve one canonical instrument for GitHub Actions")
    resolve.add_argument("instrument")
    resolve.add_argument("--repo", type=Path, default=Path("."))
    resolve.add_argument("--github-env", type=Path, required=True)

    verify = sub.add_parser("verify-run", help="prove a diagnostic test ran and emitted its report")
    verify.add_argument("instrument")
    verify.add_argument("--trx-dir", type=Path, required=True)
    verify.add_argument("--output", type=Path, required=True)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)
    try:
        if args.command == "list":
            for instrument_id in sorted(INSTRUMENTS):
                print(instrument_id)
        elif args.command == "validate":
            validate_repo(args.repo.resolve())
            print(f"measurement catalog: {len(INSTRUMENTS)} instruments valid")
        elif args.command == "resolve":
            repo = args.repo.resolve()
            validate_repo(repo)
            inst = get_instrument(args.instrument)
            write_github_env(inst, args.github_env)
            print(
                f"resolved {inst.instrument_id}: env={inst.env_var} "
                f"filter={inst.test_filter} project={inst.project}"
            )
        elif args.command == "verify-run":
            inst = get_instrument(args.instrument)
            verify_run(inst, args.trx_dir, args.output)
            print(
                f"measurement verification PASS: {inst.instrument_id} "
                "(passing test + report sentinel)"
            )
        else:
            parser.error(f"unknown command: {args.command}")
    except MeasurementError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
