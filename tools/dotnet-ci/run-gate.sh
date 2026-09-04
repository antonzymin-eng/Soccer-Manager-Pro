#!/usr/bin/env bash
# tools/dotnet-ci/run-gate.sh
# Non-certifying Linux compile/test gate. Generates plain .NET projects from
# Unity asmdefs, builds the full host-free src/ tree, and executes NUnit suites.
# Platform determinism certification is a separate pinned Windows/Unity job.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SLN="$ROOT/tools/dotnet-ci/TacticalDirector.gen.sln"
QUARANTINE="$ROOT/tools/dotnet-ci/known-failures.txt"
OWNER_HELD_RED="$ROOT/tools/dotnet-ci/owner-held-red.txt"
COVERAGE_SETTINGS="$ROOT/tools/dotnet-ci/coverage.runsettings"
REQUESTED_FILTER="${TD_GATE_TEST_FILTER:-}"
FILTER_SOURCE="${TD_GATE_FILTER_SOURCE:-}"
OWNER_MODE="${TD_OWNER_HELD_RED_MODE:-}"
COLLECT_COVERAGE="${TD_COLLECT_COVERAGE:-}"

ledger_names() {
    local ledger="$1"
    grep -v '^[[:space:]]*#' "$ledger" \
        | grep -v '^[[:space:]]*$' \
        | cut -d'|' -f1 \
        | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' || true
}

exclusion_filter() {
    local ledger="$1"
    ledger_names "$ledger" | sed 's/^/FullyQualifiedName!~/' | paste -sd'&' - || true
}

include_filter() {
    local ledger="$1"
    ledger_names "$ledger" | sed 's/^/FullyQualifiedName~/' | paste -sd'|' - || true
}

if [ -n "$REQUESTED_FILTER" ] && [ "$FILTER_SOURCE" != "testing-strategy-runner" ]; then
    echo "ERROR: TD_GATE_TEST_FILTER is set without the trusted testing-strategy runner marker." >&2
    echo "Call tools/run-tests-local.sh instead of injecting a filter into run-gate.sh." >&2
    exit 2
fi
if [ -n "$FILTER_SOURCE" ] && [ -z "$REQUESTED_FILTER" ]; then
    echo "ERROR: TD_GATE_FILTER_SOURCE is set without TD_GATE_TEST_FILTER." >&2
    exit 2
fi

case "$OWNER_MODE" in
  ""|report-only) ;;
  *)
    echo "ERROR: unsupported TD_OWNER_HELD_RED_MODE=$OWNER_MODE" >&2
    exit 2
    ;;
esac

QUARANTINE_FILTER="$(exclusion_filter "$QUARANTINE")"
OWNER_EXCLUSION=""
OWNER_INCLUDE=""
if [ "$OWNER_MODE" = "report-only" ]; then
    OWNER_EXCLUSION="$(exclusion_filter "$OWNER_HELD_RED")"
    OWNER_INCLUDE="$(include_filter "$OWNER_HELD_RED")"
fi

FILTER_PARTS=()
[ -n "$REQUESTED_FILTER" ] && FILTER_PARTS+=("$REQUESTED_FILTER")
[ -n "$QUARANTINE_FILTER" ] && FILTER_PARTS+=("$QUARANTINE_FILTER")
[ -n "$OWNER_EXCLUSION" ] && FILTER_PARTS+=("$OWNER_EXCLUSION")
FILTER=""
if [ "${#FILTER_PARTS[@]}" -gt 0 ]; then
    FILTER="$(IFS='&'; echo "${FILTER_PARTS[*]}")"
fi

if [ "${TD_GATE_DRY_RUN:-}" = "1" ]; then
    printf 'DRY-RUN blocking_filter=%s\n' "${FILTER:-<none>}"
    printf 'DRY-RUN quarantine_include=%s\n' "$(include_filter "$QUARANTINE")"
    printf 'DRY-RUN owner_held_include=%s\n' "${OWNER_INCLUDE:-<none>}"
    printf 'DRY-RUN coverage=%s\n' "${COLLECT_COVERAGE:-0}"
    exit 0
fi

echo "── Unity .meta integrity (blocking; mirrors CI) ─────────────────────"
bash "$ROOT/tools/unity-ci/check-meta-integrity.sh"

echo "── Generate csproj/sln from asmdefs ──────────────────────────────────"
python3 "$ROOT/tools/dotnet-ci/generate_projects.py"

echo "── Restore ───────────────────────────────────────────────────────────"
dotnet restore "$SLN"

echo "── Build (full tree; any compile error fails) ────────────────────────"
dotnet build "$SLN" --no-restore -clp:ErrorsOnly -m

TEST_ARGS=(dotnet test "$SLN" --no-build)
if [ -n "$FILTER" ]; then
    TEST_ARGS+=(--filter "$FILTER")
fi
if [ -n "$COLLECT_COVERAGE" ]; then
    COVERAGE_DIR="$ROOT/artifacts/coverage"
    mkdir -p "$COVERAGE_DIR"
    TEST_ARGS+=(--collect "XPlat Code Coverage" --settings "$COVERAGE_SETTINGS" --results-directory "$COVERAGE_DIR")
fi

echo "── Test (blocking; explicit exclusions applied) ──────────────────────"
if [ -n "$FILTER" ]; then
    printf 'VSTest filter: %s\n' "$FILTER"
fi
"${TEST_ARGS[@]}"

QUARANTINE_INCLUDE="$(include_filter "$QUARANTINE")"
if [ -n "$QUARANTINE_INCLUDE" ] && [ -z "$REQUESTED_FILTER" ]; then
    echo "── Quarantined tests (report-only; flake ledger only) ────────────────"
    dotnet test "$SLN" --no-build --filter "$QUARANTINE_INCLUDE" || true
elif [ -n "$QUARANTINE_INCLUDE" ]; then
    echo "── Quarantined report-only run skipped for bounded filtered caller ──"
else
    echo "── Quarantine empty ─────────────────────────────────────────────────"
fi

if [ "$OWNER_MODE" = "report-only" ] && [ -n "$OWNER_INCLUDE" ]; then
    OWNER_RESULTS="$ROOT/artifacts/owner-held-red"
    rm -rf "$OWNER_RESULTS"
    mkdir -p "$OWNER_RESULTS"
    echo "── Owner-held RED (execute separately; exact diagnostics verified) ──"
    set +e
    dotnet test "$SLN" --no-build --filter "$OWNER_INCLUDE" \
        --logger trx --results-directory "$OWNER_RESULTS"
    owner_dotnet_exit=$?
    set -e
    python3 "$ROOT/tools/dotnet-ci/verify-owner-held-red.py" \
        --ledger "$OWNER_HELD_RED" \
        --results "$OWNER_RESULTS" \
        --dotnet-exit "$owner_dotnet_exit"
fi

echo "── Gate PASSED ──────────────────────────────────────────────────────"
