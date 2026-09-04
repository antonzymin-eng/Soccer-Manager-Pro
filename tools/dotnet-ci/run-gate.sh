#!/usr/bin/env bash
# tools/dotnet-ci/run-gate.sh
# Non-certifying Linux compile/test gate. Generates plain .NET projects from
# Unity asmdefs, builds host-free code, and executes NUnit suites.
# Platform determinism certification is a separate pinned Windows/Unity job.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SLN="$ROOT/tools/dotnet-ci/TacticalDirector.gen.sln"
QUARANTINE="$ROOT/tools/dotnet-ci/known-failures.txt"
OWNER_HELD_RED="$ROOT/tools/dotnet-ci/owner-held-red.txt"
COVERAGE_SETTINGS="$ROOT/tools/dotnet-ci/coverage.runsettings"
REQUESTED_FILTER=""
OWNER_MODE=""
COLLECT_COVERAGE=0
FAST_MODE=0

usage() {
    cat <<'USAGE'
Usage: bash tools/dotnet-ci/run-gate.sh [options]

Options:
  --test-filter <vstest-filter>    Restrict the blocking test set.
  --owner-held-red report-only     Exclude the owner-held RED from the blocking
                                   pass, then execute and value-verify it separately.
  --coverage                       Collect XPlat Code Coverage.
  --fast                           Skip the whole-tree meta/build pass and run
                                   generated test projects directly. Intended only
                                   for the bounded pre-commit unit/property gate.
  -h, --help                       Show this help.

Call tools/run-tests-local.sh for repository policy modes. This lower-level gate
accepts explicit arguments only; ambient filter/owner/coverage environment
variables are rejected so CI cannot be silently narrowed by inherited state.
USAGE
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --test-filter)
            [ "$#" -ge 2 ] || { echo "ERROR: --test-filter requires a value." >&2; exit 2; }
            REQUESTED_FILTER="$2"
            shift 2
            ;;
        --owner-held-red)
            [ "$#" -ge 2 ] || { echo "ERROR: --owner-held-red requires a value." >&2; exit 2; }
            OWNER_MODE="$2"
            shift 2
            ;;
        --coverage)
            COLLECT_COVERAGE=1
            shift
            ;;
        --fast)
            FAST_MODE=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "ERROR: unknown run-gate option: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

for legacy_var in TD_GATE_TEST_FILTER TD_GATE_FILTER_SOURCE TD_OWNER_HELD_RED_MODE TD_COLLECT_COVERAGE; do
    if [ -n "${!legacy_var:-}" ]; then
        echo "ERROR: $legacy_var is no longer accepted; pass explicit run-gate.sh arguments through tools/run-tests-local.sh." >&2
        exit 2
    fi
done

case "$OWNER_MODE" in
    ""|report-only) ;;
    *)
        echo "ERROR: unsupported owner-held RED mode: $OWNER_MODE" >&2
        exit 2
        ;;
esac

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
    printf 'DRY-RUN coverage=%s\n' "$COLLECT_COVERAGE"
    printf 'DRY-RUN fast=%s\n' "$FAST_MODE"
    exit 0
fi

if [ "$FAST_MODE" -eq 0 ]; then
    echo "── Unity .meta integrity (blocking; mirrors CI) ─────────────────────"
    bash "$ROOT/tools/unity-ci/check-meta-integrity.sh"
fi

echo "── Generate csproj/sln from asmdefs ──────────────────────────────────"
python3 "$ROOT/tools/dotnet-ci/generate_projects.py"

echo "── Restore ───────────────────────────────────────────────────────────"
dotnet restore "$SLN"

if [ "$FAST_MODE" -eq 0 ]; then
    echo "── Build (full tree; any compile error fails) ────────────────────────"
    dotnet build "$SLN" --no-restore -clp:ErrorsOnly -m
fi

COMMON_TEST_ARGS=(--no-restore)
if [ "$FAST_MODE" -eq 0 ]; then
    COMMON_TEST_ARGS+=(--no-build)
fi
if [ -n "$FILTER" ]; then
    COMMON_TEST_ARGS+=(--filter "$FILTER")
fi
if [ "$COLLECT_COVERAGE" -eq 1 ]; then
    COVERAGE_DIR="$ROOT/artifacts/coverage"
    mkdir -p "$COVERAGE_DIR"
    COMMON_TEST_ARGS+=(--collect "XPlat Code Coverage" --settings "$COVERAGE_SETTINGS" --results-directory "$COVERAGE_DIR")
fi

echo "── Test (blocking; explicit exclusions applied) ──────────────────────"
if [ -n "$FILTER" ]; then
    printf 'VSTest filter: %s\n' "$FILTER"
fi

if [ "$FAST_MODE" -eq 1 ]; then
    mapfile -t TEST_PROJECTS < <(find "$ROOT/src" -name '*.Tests.gen.csproj' -type f -print | sort)
    if [ "${#TEST_PROJECTS[@]}" -eq 0 ]; then
        echo "ERROR: generated test-project set is empty." >&2
        exit 1
    fi
    for project in "${TEST_PROJECTS[@]}"; do
        dotnet test "$project" "${COMMON_TEST_ARGS[@]}"
    done
else
    dotnet test "$SLN" "${COMMON_TEST_ARGS[@]}"
fi

QUARANTINE_INCLUDE="$(include_filter "$QUARANTINE")"
if [ -n "$QUARANTINE_INCLUDE" ] && [ -z "$REQUESTED_FILTER" ]; then
    echo "── Quarantined tests (report-only; flake ledger only) ────────────────"
    dotnet test "$SLN" --no-build --no-restore --filter "$QUARANTINE_INCLUDE" || true
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
    dotnet test "$SLN" --no-build --no-restore --filter "$OWNER_INCLUDE" \
        --logger trx --results-directory "$OWNER_RESULTS"
    owner_dotnet_exit=$?
    set -e
    python3 "$ROOT/tools/dotnet-ci/verify-owner-held-red.py" \
        --ledger "$OWNER_HELD_RED" \
        --results "$OWNER_RESULTS" \
        --dotnet-exit "$owner_dotnet_exit"
fi

echo "── Gate PASSED ──────────────────────────────────────────────────────"
