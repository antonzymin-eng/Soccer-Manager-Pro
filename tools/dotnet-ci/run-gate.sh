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
SETTINGS_FILE=""
OWNER_MODE=""
COLLECT_COVERAGE=0
FAST_MODE=0

usage() {
    cat <<'USAGE'
Usage: bash tools/dotnet-ci/run-gate.sh [options]

Options:
  --test-filter <vstest-filter>    Restrict the blocking test set with an explicit VSTest filter.
  --settings <runsettings>         Apply an explicit .runsettings file (used by pre-commit for anchored NUnit selection).
  --owner-held-red report-only     Exclude the owner-held RED from the blocking pass, then execute and value-verify it separately.
  --coverage                       Collect XPlat Code Coverage.
  --fast                           Skip the separate whole-tree meta/build pass. `dotnet test` still builds the generated solution once, incrementally.
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
        --settings)
            [ "$#" -ge 2 ] || { echo "ERROR: --settings requires a value." >&2; exit 2; }
            SETTINGS_FILE="$2"
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

if [ "$COLLECT_COVERAGE" -eq 1 ] && [ -n "$SETTINGS_FILE" ]; then
    echo "ERROR: --coverage and --settings cannot be combined; coverage owns its runsettings file." >&2
    exit 2
fi
if [ -n "$SETTINGS_FILE" ] && [ ! -f "$SETTINGS_FILE" ]; then
    echo "ERROR: runsettings file does not exist: $SETTINGS_FILE" >&2
    exit 2
fi

ledger_names() {
    local ledger="$1"
    grep -v '^[[:space:]]*#' "$ledger" \
        | grep -v '^[[:space:]]*$' \
        | cut -d'|' -f1 \
        | sed 's/^[[:space:]]*//;s/[[:space:]]*$//' || true
}

quarantine_exclusion_filter() {
    ledger_names "$1" | sed 's/^/FullyQualifiedName!~/' | paste -sd'&' - || true
}

quarantine_include_filter() {
    ledger_names "$1" | sed 's/^/FullyQualifiedName~/' | paste -sd'|' - || true
}

owner_exclusion_filter() {
    ledger_names "$1" | sed 's/^/Name!=/' | paste -sd'&' - || true
}

owner_include_filter() {
    ledger_names "$1" | sed 's/^/Name=/' | paste -sd'|' - || true
}

append_filter() {
    local part="$1"
    [ -n "$part" ] || return 0
    if [ -z "$FILTER" ]; then
        FILTER="$part"
    else
        FILTER="$FILTER&$part"
    fi
}

QUARANTINE_FILTER="$(quarantine_exclusion_filter "$QUARANTINE")"
OWNER_EXCLUSION=""
OWNER_INCLUDE=""
if [ "$OWNER_MODE" = "report-only" ]; then
    OWNER_EXCLUSION="$(owner_exclusion_filter "$OWNER_HELD_RED")"
    OWNER_INCLUDE="$(owner_include_filter "$OWNER_HELD_RED")"
fi

FILTER=""
append_filter "$REQUESTED_FILTER"
append_filter "$QUARANTINE_FILTER"
append_filter "$OWNER_EXCLUSION"

if [ "${TD_GATE_DRY_RUN:-}" = "1" ]; then
    printf 'DRY-RUN blocking_filter=%s\n' "${FILTER:-<none>}"
    printf 'DRY-RUN settings=%s\n' "${SETTINGS_FILE:-<none>}"
    printf 'DRY-RUN quarantine_include=%s\n' "$(quarantine_include_filter "$QUARANTINE")"
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

TEST_ARGS=(--no-restore)
if [ "$FAST_MODE" -eq 0 ]; then
    TEST_ARGS+=(--no-build)
fi
if [ -n "$FILTER" ]; then
    TEST_ARGS+=(--filter "$FILTER")
fi
if [ -n "$SETTINGS_FILE" ]; then
    TEST_ARGS+=(--settings "$SETTINGS_FILE")
fi
if [ "$COLLECT_COVERAGE" -eq 1 ]; then
    COVERAGE_DIR="$ROOT/artifacts/coverage"
    mkdir -p "$COVERAGE_DIR"
    TEST_ARGS+=(--collect "XPlat Code Coverage" --settings "$COVERAGE_SETTINGS" --results-directory "$COVERAGE_DIR")
fi

echo "── Test (blocking; explicit policy selection applied) ────────────────"
if [ -n "$FILTER" ]; then
    printf 'VSTest filter: %s\n' "$FILTER"
fi
if [ -n "$SETTINGS_FILE" ]; then
    printf 'Runsettings: %s\n' "$SETTINGS_FILE"
fi

# Fast mode intentionally avoids 34 sequential per-project `dotnet test` calls.
# A single solution invocation lets MSBuild schedule the generated project graph
# and reuse incremental outputs preserved by the persistent pre-commit snapshot.
dotnet test "$SLN" "${TEST_ARGS[@]}"

QUARANTINE_INCLUDE="$(quarantine_include_filter "$QUARANTINE")"
if [ -n "$QUARANTINE_INCLUDE" ] && [ "$FAST_MODE" -eq 0 ] && [ -z "$REQUESTED_FILTER" ] && [ -z "$SETTINGS_FILE" ]; then
    echo "── Quarantined tests (report-only; flake ledger only) ────────────────"
    dotnet test "$SLN" --no-build --no-restore --filter "$QUARANTINE_INCLUDE" || true
elif [ -n "$QUARANTINE_INCLUDE" ]; then
    echo "── Quarantined report-only run skipped for bounded/selected caller ──"
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
