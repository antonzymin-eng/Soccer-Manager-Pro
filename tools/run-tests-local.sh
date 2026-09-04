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

  --pre-commit   Fast unit/property compatibility gate. Whole composition is hard-bounded to 60 seconds.
  --pr           PR gate: auditors + whole-tree functional test superset + coverage.
  --nightly      Auditors + full non-certifying simulation/soak gate + coverage.
  --install-hook Configure this clone to use the versioned .githooks directory.
  --verify-hook  Fail unless this clone is configured to execute that hook.

D2 is pinned to FsCheck.NUnit 2.16.6 and D3 is pinned to
coverlet.collector 6.0.4 through Directory.Build.targets. Property tests
participate automatically when present. The pre-commit path executes only the
unit/property-compatible test subset and skips the whole-tree compile/meta pass;
it remains subject to the 60-second hard limit. Meeting that limit on the
certified developer host is an operational acceptance measurement, not inferred
from the existence of the timeout.

PR/nightly use the Linux shim gate for non-certifying functional evidence. Nightly
also enables the existing full-match ShotOutcomeDiagnosticTests soak. Platform
determinism certification is a separate certified Windows/Unity job in nightly.yml.
The recorded owner-held RED is executed separately and verified against its pinned
diagnostic values; it is not quarantine and a changed failure or unexpected pass
is blocking.
USAGE
}

verify_hook() {
  local configured
  configured="$(git -C "$ROOT" config --get core.hooksPath || true)"
  if [ "$configured" != ".githooks" ]; then
    printf 'ERROR: core.hooksPath is %q, expected .githooks.\n' "$configured" >&2
    printf 'Run: bash tools/bootstrap-dev.sh\n' >&2
    return 1
  fi
  if [ ! -x "$ROOT/.githooks/pre-commit" ]; then
    printf 'ERROR: .githooks/pre-commit is not executable.\n' >&2
    return 1
  fi
  printf 'Pre-commit hook configuration verified.\n'
}

install_hook() {
  local configured
  configured="$(git -C "$ROOT" config --get core.hooksPath || true)"
  case "$configured" in
    "")
      git -C "$ROOT" config core.hooksPath .githooks
      ;;
    .githooks)
      ;;
    *)
      printf 'ERROR: refusing to overwrite existing core.hooksPath=%q.\n' "$configured" >&2
      printf 'Reconcile the existing hook chain explicitly, then point it at .githooks.\n' >&2
      return 2
      ;;
  esac
  chmod +x "$ROOT/.githooks/pre-commit"
  verify_hook
}

case "$MODE" in
  --install-hook)
    install_hook
    exit $?
    ;;
  --verify-hook)
    verify_hook
    exit $?
    ;;
  --pre-commit)
    PIPELINE_NAME="pre-commit"
    unset TD_SHOT_DIAGNOSTIC || true
    GATE_ARGS=(--fast --test-filter "$PRECOMMIT_FILTER")
    ;;
  --pr)
    PIPELINE_NAME="PR"
    unset TD_SHOT_DIAGNOSTIC || true
    GATE_ARGS=(--owner-held-red report-only --coverage)
    ;;
  --nightly)
    PIPELINE_NAME="nightly-functional"
    export TD_SHOT_DIAGNOSTIC=1
    GATE_ARGS=(--owner-held-red report-only --coverage)
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
if [ "$MODE" = "--pre-commit" ]; then
    printf 'Compatibility filter: %s\n' "$PRECOMMIT_FILTER"
fi
if [ -n "${TD_SHOT_DIAGNOSTIC:-}" ]; then
    printf 'Full-match soak driver: ShotOutcomeDiagnosticTests\n'
fi
if [ "$MODE" != "--pre-commit" ]; then
    printf 'Owner-held-red policy: execute separately and verify recorded diagnostics\n'
    printf 'Coverage: XPlat Code Coverage (coverlet.collector)\n'
fi

if [ "${TD_PIPELINE_DRY_RUN:-}" = "1" ]; then
    printf 'DRY-RUN checklist_auditor=%s\n' "$ROOT/tools/checklist-auditor.py"
    printf 'DRY-RUN schema_auditor=%s\n' "$ROOT/tools/spec5-schema-auditor.py"
    printf 'DRY-RUN gate=%s\n' "$ROOT/tools/dotnet-ci/run-gate.sh"
    printf 'DRY-RUN gate_args='
    printf '%q ' "${GATE_ARGS[@]}"
    printf '\n'
    if [ "$MODE" = "--pre-commit" ]; then
        printf 'DRY-RUN budget_seconds=%s\n' "$PRECOMMIT_BUDGET_SECONDS"
    fi
    exit 0
fi

# The 60-second pre-commit budget covers the ENTIRE composition (auditors + test
# gate), not only dotnet test. Re-exec ourselves once under the process-group
# budget so a slow restore/build/auditor cannot escape the requirement.
if [ "$MODE" = "--pre-commit" ] && [ "${TD_PRECOMMIT_BUDGET_ACTIVE:-}" != "1" ]; then
    export TD_PRECOMMIT_BUDGET_ACTIVE=1
    exec python3 "$ROOT/tools/run-with-time-budget.py" \
        --seconds "$PRECOMMIT_BUDGET_SECONDS" -- \
        bash "$ROOT/tools/run-tests-local.sh" --pre-commit
fi

printf 'Auditor: approval-checklist evidence\n'
python3 "$ROOT/tools/checklist-auditor.py" --root "$ROOT/docs/specs" --repo-root "$ROOT"

printf 'Auditor: per-spec section-5 schema\n'
python3 "$ROOT/tools/spec5-schema-auditor.py" --root "$ROOT/docs/specs" --repo-root "$ROOT"

printf 'Runner: tools/dotnet-ci/run-gate.sh (Linux shim, non-certifying)\n'
exec bash "$ROOT/tools/dotnet-ci/run-gate.sh" "${GATE_ARGS[@]}"
