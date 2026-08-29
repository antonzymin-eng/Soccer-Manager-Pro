#!/usr/bin/env bash
# tools/dotnet-ci/run-gate.sh
# Created: 2026-06-12
# Purpose: Non-certifying Linux compile/test gate. Generates plain .NET projects
#          from the Unity asmdefs, builds the entire src/ tree, and runs every
#          NUnit suite, excluding only the quarantined tests in
#          known-failures.txt (tracked in docs/tracking/dotnet-ci-quarantine.md).
#          NOT a determinism certification — that remains the pinned
#          Windows/Unity tuple in docs/tracking/certification-platform.md.
#
#          Modified 2026-08-15 (#44 AR round 6, H1): this gate now also runs the
#          unity-meta-integrity check, because it did not, and four files went
#          .meta-less for two days while FIVE review rounds and four separate
#          "GATE: RUN TO COMPLETION" landing records all read green. The gate's
#          verdict is quoted in landing entries as though it covered CI; it
#          covered one job of it. A missing .meta makes Unity mint a fresh random
#          GUID on checkout, silently breaking every reference to the file — so
#          this is cheap to check and expensive to miss. Run FIRST: it costs
#          milliseconds and fails before an hour of tests.
# Usage:   bash tools/dotnet-ci/run-gate.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SLN="$ROOT/tools/dotnet-ci/TacticalDirector.gen.sln"
QUARANTINE="$ROOT/tools/dotnet-ci/known-failures.txt"

echo "── Unity .meta integrity (blocking; mirrors the unity-meta-integrity CI job) ─"
bash "$ROOT/tools/unity-ci/check-meta-integrity.sh"

echo "── Generate csproj/sln from asmdefs ────────────────────────────────"
python3 "$ROOT/tools/dotnet-ci/generate_projects.py"

echo "── Restore ─────────────────────────────────────────────────────────"
dotnet restore "$SLN"

echo "── Build (full tree; any compile error fails the gate) ─────────────"
dotnet build "$SLN" --no-restore -clp:ErrorsOnly -m

# Build the exclusion filter from the quarantine ledger (empty when no tests are
# quarantined — the burn-down is complete and the whole suite is enforced).
# `|| true`: grep exits 1 when the quarantine has no test lines (burn-down done);
# under `set -euo pipefail` that would abort the gate, so tolerate empty output.
FILTER="$(grep -v '^\s*#' "$QUARANTINE" | grep -v '^\s*$' \
          | sed 's/^/FullyQualifiedName!~/' | paste -sd'&' - || true)"

echo "── Test (blocking; quarantined tests excluded) ─────────────────────"
if [ -n "$FILTER" ]; then
    dotnet test "$SLN" --no-build --filter "$FILTER"
else
    dotnet test "$SLN" --no-build
fi

INCLUDE="$(grep -v '^\s*#' "$QUARANTINE" | grep -v '^\s*$' \
           | sed 's/^/FullyQualifiedName~/' | paste -sd'|' - || true)"
if [ -n "$INCLUDE" ]; then
    echo "── Quarantined tests (report-only; failure expected, not blocking) ─"
    dotnet test "$SLN" --no-build --filter "$INCLUDE" || true
else
    echo "── Quarantine empty — full suite enforced above; no report-only run ─"
fi

echo "── Gate PASSED ─────────────────────────────────────────────────────"
