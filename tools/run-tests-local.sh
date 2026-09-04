#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:---pr}"
PRECOMMIT_BUDGET_SECONDS=60
PRECOMMIT_FILTER='FullyQualifiedName!~sim_&FullyQualifiedName!~e2e_&FullyQualifiedName!~Integration&FullyQualifiedName!~Scenario&FullyQualifiedName!~TacticalDirector.DeterministicSim.Tests&TestCategory!=Calibration'

usage() {
  cat <<'USAGE'
Usage: bash tools/run-tests-local.sh [--pre-commit|--pr|--nightly|--install-hook|--verify-hook]

Stable local/CI entry point for Testing Strategy #19 FR-TS-075/079.

  --pre-commit   Bounded fast functional gate. Hard timeout: 60 seconds.
  --pr           PR-equivalent non-certifying whole-tree gate.
  --nightly      Full non-certifying functional/simulation gate + full-match soak.
  --install-hook Configure this clone to use the versioned .githooks directory.
  --verify-hook  Fail unless this clone is configured to execute that hook.

The repository still lacks the final §3.1 machine-readable taxonomy and D2 property
framework pin. Pre-commit therefore uses the narrowest executable compatibility
filter available in the current tree and enforces the normative 60-second ceiling
mechanically. This mode is not represented as exact taxonomy proof; the remaining
classification/D2 debt stays visible for A3.4 rather than being hidden by this runner.

PR/nightly use the existing Linux shim gate and are non-certifying. Nightly executes
the full-match ShotOutcomeDiagnosticTests soak and treats the separately documented
owner-held-red acceptance scenario as report-only while still executing it. Platform
determinism certification is a separate certified-host job in nightly.yml.
USAGE
}

verify_hook() {
  local configured
  configured="$(git -C "$ROOT" config --get core.hooksPath || true)"
  if [ "$configured" != ".githooks" ]; then
    printf 'ERROR: core.hooksPath is %q, expected .githooks.\n' "$configured" >&2
    printf 'Run: bash tools/run-tests-local.sh --install-hook\n' >&2
    return 1
  fi
  if [ ! -x "$ROOT/.githooks/pre-commit" ]; then
    printf 'ERROR: .githooks/pre-commit is not executable.\n' >&2
    return 1
  fi
  printf 'Pre-commit hook configuration verified.\n'
}

case "$MODE" in
  --install-hook)
    git -C "$ROOT" config core.hooksPath .githooks
    chmod +x "$ROOT/.githooks/pre-commit"
    verify_hook
    exit 0
    ;;
  --verify-hook)
    verify_hook
    exit $?
    ;;
  --pre-commit)
    PIPELINE_NAME="pre-commit"
    unset TD_SHOT_DIAGNOSTIC || true
    unset TD_OWNER_HELD_RED_MODE || true
    export TD_GATE_TEST_FILTER="$PRECOMMIT_FILTER"
    ;;
  --pr)
    PIPELINE_NAME="PR"
    unset TD_GATE_TEST_FILTER || true
    unset TD_SHOT_DIAGNOSTIC || true
    unset TD_OWNER_HELD_RED_MODE || true
    ;;
  --nightly)
    PIPELINE_NAME="nightly-functional"
    unset TD_GATE_TEST_FILTER || true
    export TD_SHOT_DIAGNOSTIC=1
    export TD_OWNER_HELD_RED_MODE=report-only
    ;;
  -h|--help)
    usage
    exit 0
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

printf '== Testing Strategy %s pipeline ==\n' "$PIPELINE_NAME"
if [ -n "${TD_GATE_TEST_FILTER:-}" ]; then
    printf 'Compatibility filter: %s\n' "$TD_GATE_TEST_FILTER"
fi
if [ -n "${TD_SHOT_DIAGNOSTIC:-}" ]; then
    printf 'Full-match soak driver: ShotOutcomeDiagnosticTests\n'
fi
if [ "${TD_OWNER_HELD_RED_MODE:-}" = "report-only" ]; then
    printf 'Owner-held-red policy: execute separately, report-only\n'
fi

if [ "${TD_PIPELINE_DRY_RUN:-}" = "1" ]; then
    printf 'DRY-RUN gate=%s\n' "$ROOT/tools/dotnet-ci/run-gate.sh"
    if [ "$MODE" = "--pre-commit" ]; then
        printf 'DRY-RUN budget_seconds=%s\n' "$PRECOMMIT_BUDGET_SECONDS"
    fi
    exit 0
fi

if [ "$MODE" = "--pre-commit" ]; then
    exec python3 "$ROOT/tools/run-with-time-budget.py" \
        --seconds "$PRECOMMIT_BUDGET_SECONDS" -- \
        bash "$ROOT/tools/dotnet-ci/run-gate.sh"
fi

printf 'Runner: tools/dotnet-ci/run-gate.sh (non-certifying)\n'
exec bash "$ROOT/tools/dotnet-ci/run-gate.sh"
