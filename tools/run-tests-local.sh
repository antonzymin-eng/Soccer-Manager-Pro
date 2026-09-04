#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:---pr}"
PRECOMMIT_BUDGET_SECONDS=60
PRECOMMIT_SETTINGS="$ROOT/tools/dotnet-ci/precommit.runsettings"

# Legacy ambient controls are deliberately not policy inputs. The stable runner
# owns composition and sanitizes them before invoking the lower-level gate.
unset TD_GATE_TEST_FILTER TD_GATE_FILTER_SOURCE TD_OWNER_HELD_RED_MODE TD_COLLECT_COVERAGE || true

usage() {
  cat <<'USAGE'
Usage: bash tools/run-tests-local.sh [--pre-commit|--pr|--nightly|--install-hook|--verify-hook]

Stable local/CI entry point for Testing Strategy #19 FR-TS-075/079.

  --pre-commit   Fast unit/property compatibility gate. Whole composition is hard-bounded to 60 seconds.
  --pr           PR gate: survey auditors + approval-transition blocking + whole-tree functional test superset + coverage.
  --nightly      Survey auditors + full non-certifying simulation/soak gate + coverage.
  --install-hook Configure this clone to use the versioned .githooks directory.
  --verify-hook  Fail unless this clone is configured to execute that hook.

Routine pre-commit/PR/nightly runs SURVEY the documentation corpus. They do not
turn pre-existing Spec #19 schema/checklist debt into an unrelated repository
merge gate. On PR CI, TD_APPROVAL_BASE_REF names the PR base commit; the policy
compares base/head SPEC_INDEX.md, the canonical approval registry. Any spec whose
registry status transitions from non-approved/missing to APPROVED is rerun through
both auditors as blocking. Missing/unparseable registry history fails closed.
This wires FR-TS-042/052 to the approval event without retroactively blocking
unrelated changes on pre-existing corpus debt.

D2 is pinned to FsCheck.NUnit 2.16.6 and D3 is pinned to
coverlet.collector 6.0.4 through Directory.Build.targets. Property tests
participate automatically when present. Pre-commit selection is expressed with
NUnit's test-selection language in tools/dotnet-ci/precommit.runsettings, where
canonical int_/sim_/e2e_ exclusions are anchored to METHOD-name prefixes rather
than unsafe substrings of FullyQualifiedName.

The pre-commit path skips the separate whole-tree meta/build pass and uses one
incremental generated-solution test invocation; the versioned hook preserves a
persistent staged-index snapshot/build cache and refreshes only changed index
paths so unchanged source mtimes remain stable. Snapshot checkout disables LFS
smudging because staged pointer bytes are sufficient for this test gate. The
entire attempted composition remains subject to the 60-second hard limit. Meeting
that limit on the certified developer host is an operational acceptance
measurement, not inferred from the timeout itself.

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
    GATE_ARGS=(--fast --settings "$PRECOMMIT_SETTINGS")
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

AUDITOR_SCOPE_ARGS=(--survey-only --quiet-survey)

printf '== Testing Strategy %s pipeline ==\n' "$PIPELINE_NAME"
printf 'Documentation audit policy: routine survey; canonical registry approval transitions are blocking\n'
if [ "$MODE" = "--pre-commit" ]; then
    printf 'Pre-commit selection: anchored NUnit method/category rules in %s\n' "$PRECOMMIT_SETTINGS"
fi
if [ -n "${TD_SHOT_DIAGNOSTIC:-}" ]; then
    printf 'Full-match soak driver: ShotOutcomeDiagnosticTests\n'
fi
if [ "$MODE" != "--pre-commit" ]; then
    printf 'Owner-held-red policy: execute separately and verify recorded diagnostics\n'
    printf 'Coverage: XPlat Code Coverage (coverlet.collector)\n'
fi
if [ "$MODE" = "--pr" ] && [ -n "${TD_APPROVAL_BASE_REF:-}" ]; then
    printf 'Approval-transition base: %s\n' "$TD_APPROVAL_BASE_REF"
fi

if [ "${TD_PIPELINE_DRY_RUN:-}" = "1" ]; then
    printf 'DRY-RUN checklist_auditor=%s\n' "$ROOT/tools/checklist-auditor.py"
    printf 'DRY-RUN schema_auditor=%s\n' "$ROOT/tools/spec5-schema-auditor.py"
    printf 'DRY-RUN auditor_args='
    printf '%q ' "${AUDITOR_SCOPE_ARGS[@]}"
    printf '\n'
    if [ "$MODE" = "--pr" ]; then
        printf 'DRY-RUN approval_scope_detector=%s\n' "$ROOT/tools/testing-strategy-approval-scope.py"
        printf 'DRY-RUN approval_base=%s\n' "${TD_APPROVAL_BASE_REF:-<none>}"
    fi
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
# gate), not only dotnet test. During bootstrap cache preparation the hook sets
# TD_PRECOMMIT_BUDGET_ACTIVE=1 deliberately so the one-time cold warmup is not
# misrepresented as a normal commit-path measurement.
if [ "$MODE" = "--pre-commit" ] && [ "${TD_PRECOMMIT_BUDGET_ACTIVE:-}" != "1" ]; then
    export TD_PRECOMMIT_BUDGET_ACTIVE=1
    exec python3 "$ROOT/tools/run-with-time-budget.py" \
        --seconds "$PRECOMMIT_BUDGET_SECONDS" -- \
        bash "$ROOT/tools/run-tests-local.sh" --pre-commit
fi

printf 'Auditor: approval-checklist evidence (survey)\n'
python3 "$ROOT/tools/checklist-auditor.py" \
    --root "$ROOT/docs/specs" --repo-root "$ROOT" "${AUDITOR_SCOPE_ARGS[@]}"

printf 'Auditor: per-spec section-5 schema (survey)\n'
python3 "$ROOT/tools/spec5-schema-auditor.py" \
    --root "$ROOT/docs/specs" --repo-root "$ROOT" "${AUDITOR_SCOPE_ARGS[@]}"

# Approval blocking is automatically wired only where the canonical registry
# itself advances a spec into APPROVED. Existing approved specs with historical
# debt stay survey-only unless they undergo a new canonical approval transition.
if [ "$MODE" = "--pr" ] && [ -n "${TD_APPROVAL_BASE_REF:-}" ]; then
    APPROVAL_SCOPE_FILE="$(mktemp)"
    trap 'rm -f "$APPROVAL_SCOPE_FILE"' EXIT INT TERM
    python3 "$ROOT/tools/testing-strategy-approval-scope.py" \
        --repo-root "$ROOT" --base "$TD_APPROVAL_BASE_REF" --head HEAD \
        > "$APPROVAL_SCOPE_FILE"
    if [ -s "$APPROVAL_SCOPE_FILE" ]; then
        APPROVAL_ARGS=(--changed-scope --quiet-survey)
        while IFS= read -r spec_dir; do
            [ -n "$spec_dir" ] || continue
            APPROVAL_ARGS+=(--enforce-dir "$spec_dir")
        done < "$APPROVAL_SCOPE_FILE"
        printf 'Approval-transition blocking scope:\n'
        cat "$APPROVAL_SCOPE_FILE"
        python3 "$ROOT/tools/checklist-auditor.py" \
            --root "$ROOT/docs/specs" --repo-root "$ROOT" "${APPROVAL_ARGS[@]}"
        python3 "$ROOT/tools/spec5-schema-auditor.py" \
            --root "$ROOT/docs/specs" --repo-root "$ROOT" "${APPROVAL_ARGS[@]}"
    else
        printf 'Approval-transition blocking scope: none\n'
    fi
    rm -f "$APPROVAL_SCOPE_FILE"
    trap - EXIT INT TERM
fi

printf 'Runner: tools/dotnet-ci/run-gate.sh (Linux shim, non-certifying)\n'
exec bash "$ROOT/tools/dotnet-ci/run-gate.sh" "${GATE_ARGS[@]}"
