#!/usr/bin/env bash
# tools/dotnet-ci/run-gate.sh
# Created: 2026-06-12
# Purpose: Non-certifying Linux compile/test gate. Generates plain .NET projects
#          from the Unity asmdefs, builds the entire src/ tree, and runs NUnit.
#          The flake quarantine remains in known-failures.txt. A separate,
#          explicitly activated owner-held-red ledger may be executed report-only
#          for scheduled diagnostics; it is not quarantine.
#          NOT a determinism certification — that remains the pinned
#          Windows/Unity tuple in docs/tracking/certification-platform.md.
#
#          Modified 2026-09-04 (FR-TS-075/079 conformance): callers MAY set
#          TD_GATE_TEST_FILTER to a VSTest filter expression. The default remains
#          the whole-tree gate. TD_OWNER_HELD_RED_MODE=report-only is an explicit
#          scheduled-run policy: listed owner-held tests are removed from the
#          blocking pass and then executed separately so their known RED does not
#          make every nightly run meaningless.
# Usage:   bash tools/dotnet-ci/run-gate.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SLN="$ROOT/tools/dotnet-ci/TacticalDirector.gen.sln"
QUARANTINE="$ROOT/tools/dotnet-ci/known-failures.txt"
OWNER_HELD_RED="$ROOT/tools/dotnet-ci/owner-held-red.txt"
REQUESTED_FILTER="${TD_GATE_TEST_FILTER:-}"
OWNER_HELD_MODE="${TD_OWNER_HELD_RED_MODE:-}"

case "$OWNER_HELD_MODE" in
  ""|report-only) ;;
  *)
    echo "ERROR: TD_OWNER_HELD_RED_MODE must be empty or 'report-only'." >&2
    exit 2
    ;;
esac

ledger_exclusion() {
  local ledger="$1"
  if [ ! -f "$ledger" ]; then
    return 0
  fi
  grep -v '^[[:space:]]*#' "$ledger" | grep -v '^[[:space:]]*$' \
    | sed 's/^/FullyQualifiedName!~/' | paste -sd'&' - || true
}

ledger_include() {
  local ledger="$1"
  if [ ! -f "$ledger" ]; then
    return 0
  fi
  grep -v '^[[:space:]]*#' "$ledger" | grep -v '^[[:space:]]*$' \
    | sed 's/^/FullyQualifiedName~/' | paste -sd'|' - || true
}

append_filter() {
  local current="$1"
  local extra="$2"
  if [ -z "$extra" ]; then
    printf '%s' "$current"
  elif [ -z "$current" ]; then
    printf '%s' "$extra"
  else
    printf '%s&%s' "$current" "$extra"
  fi
}

QUARANTINE_FILTER="$(ledger_exclusion "$QUARANTINE")"
OWNER_HELD_FILTER=""
if [ "$OWNER_HELD_MODE" = "report-only" ]; then
  OWNER_HELD_FILTER="$(ledger_exclusion "$OWNER_HELD_RED")"
fi

FILTER="$REQUESTED_FILTER"
FILTER="$(append_filter "$FILTER" "$QUARANTINE_FILTER")"
FILTER="$(append_filter "$FILTER" "$OWNER_HELD_FILTER")"

if [ "${TD_GATE_DRY_RUN:-}" = "1" ]; then
  printf 'DRY-RUN blocking_filter=%s\n' "$FILTER"
  if [ "$OWNER_HELD_MODE" = "report-only" ]; then
    printf 'DRY-RUN owner_held_include=%s\n' "$(ledger_include "$OWNER_HELD_RED")"
  fi
  exit 0
fi

echo "── Unity .meta integrity (blocking; mirrors the unity-meta-integrity CI job) ─"
bash "$ROOT/tools/unity-ci/check-meta-integrity.sh"

echo "── Generate csproj/sln from asmdefs ────────────────────────────────"
python3 "$ROOT/tools/dotnet-ci/generate_projects.py"

echo "── Restore ─────────────────────────────────────────────────────────"
dotnet restore "$SLN"

echo "── Build (full tree; any compile error fails the gate) ─────────────"
dotnet build "$SLN" --no-restore -clp:ErrorsOnly -m

echo "── Test (blocking; caller/policy exclusions applied) ───────────────"
if [ -n "$FILTER" ]; then
    printf 'VSTest filter: %s\n' "$FILTER"
    dotnet test "$SLN" --no-build --filter "$FILTER"
else
    dotnet test "$SLN" --no-build
fi

QUARANTINE_INCLUDE="$(ledger_include "$QUARANTINE")"
if [ -n "$QUARANTINE_INCLUDE" ] && [ -z "$REQUESTED_FILTER" ]; then
    echo "── Quarantined tests (report-only; failure expected, not blocking) ─"
    dotnet test "$SLN" --no-build --filter "$QUARANTINE_INCLUDE" || true
elif [ -n "$QUARANTINE_INCLUDE" ]; then
    echo "── Quarantined report-only run skipped for filtered caller ─────────"
else
    echo "── Quarantine empty — no quarantine report-only run ───────────────"
fi

if [ "$OWNER_HELD_MODE" = "report-only" ]; then
    OWNER_HELD_INCLUDE="$(ledger_include "$OWNER_HELD_RED")"
    if [ -z "$OWNER_HELD_INCLUDE" ]; then
        echo "ERROR: owner-held-red report-only mode requested but ledger is empty." >&2
        exit 2
    fi
    echo "── Owner-held RED tests (executed, report-only by recorded decision) ─"
    set +e
    dotnet test "$SLN" --no-build --filter "$OWNER_HELD_INCLUDE"
    owner_rc=$?
    set -e
    if [ "$owner_rc" -eq 0 ]; then
        echo "::warning::Owner-held RED suite is now green; revisit and retire the owner hold."
    else
        echo "Owner-held RED remains red as recorded (non-blocking for this scheduled run)."
    fi
fi

echo "── Gate PASSED ─────────────────────────────────────────────────────"
