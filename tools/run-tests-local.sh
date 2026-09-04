#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:---pr}"

usage() {
  cat <<'USAGE'
Usage: bash tools/run-tests-local.sh [--pre-commit|--pr|--nightly|--install-hook]

Stable local/CI entry point for Testing Strategy #19 FR-TS-075/079.

  --pre-commit  Fast local gate: full compile + non-simulation test subset.
  --pr          PR-equivalent whole-tree gate (default).
  --nightly     Whole-tree gate + existing full-90-minute match soak driver.
  --install-hook Configure this clone to use the versioned .githooks directory.

The repository does not yet expose the §3.1 taxonomy as NUnit categories and D2's
property framework remains unpinned. The pre-commit mode therefore includes every
currently runnable test except names/categories reserved for simulation, e2e and
calibration work; this is a conservative fast approximation that includes all
ordinary unit/property candidates and avoids the known-red long simulation gate.
PR/nightly modes retain the existing whole-tree non-certifying dotnet gate; nightly
also enables ShotOutcomeDiagnosticTests, which executes three full 90-minute matches.
Nothing here is platform determinism certification.
USAGE
}

case "$MODE" in
  --install-hook)
    git -C "$ROOT" config core.hooksPath .githooks
    chmod +x "$ROOT/.githooks/pre-commit"
    printf 'Configured core.hooksPath=.githooks for %s\n' "$ROOT"
    exit 0
    ;;
  --pre-commit)
    PIPELINE_NAME="pre-commit"
    unset TD_SHOT_DIAGNOSTIC || true
    export TD_GATE_TEST_FILTER='FullyQualifiedName!~sim_&FullyQualifiedName!~e2e_&TestCategory!=Calibration'
    ;;
  --pr)
    PIPELINE_NAME="PR"
    unset TD_GATE_TEST_FILTER || true
    unset TD_SHOT_DIAGNOSTIC || true
    ;;
  --nightly)
    PIPELINE_NAME="nightly"
    unset TD_GATE_TEST_FILTER || true
    export TD_SHOT_DIAGNOSTIC=1
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
    printf 'Fast filter: %s\n' "$TD_GATE_TEST_FILTER"
fi
if [ -n "${TD_SHOT_DIAGNOSTIC:-}" ]; then
    printf 'Full-match soak driver: ShotOutcomeDiagnosticTests\n'
fi
printf 'Runner: tools/dotnet-ci/run-gate.sh (non-certifying)\n'
exec bash "$ROOT/tools/dotnet-ci/run-gate.sh"
