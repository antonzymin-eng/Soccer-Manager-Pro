#!/usr/bin/env bash
# tools/dotnet-ci/run-gate.sh
# Created: 2026-06-12
# Purpose: Non-certifying Linux compile/test gate. Generates plain .NET projects
#          from the Unity asmdefs, builds the entire src/ tree, and runs every
#          NUnit suite, excluding only the quarantined tests in
#          known-failures.txt (tracked in docs/tracking/dotnet-ci-quarantine.md).
#          NOT a determinism certification — that remains the pinned
#          Windows/Unity tuple in docs/tracking/certification-platform.md.
# Usage:   bash tools/dotnet-ci/run-gate.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SLN="$ROOT/tools/dotnet-ci/TacticalDirector.gen.sln"
QUARANTINE="$ROOT/tools/dotnet-ci/known-failures.txt"

echo "── Generate csproj/sln from asmdefs ────────────────────────────────"
python3 "$ROOT/tools/dotnet-ci/generate_projects.py"

echo "── Restore ─────────────────────────────────────────────────────────"
dotnet restore "$SLN"

echo "── Build (full tree; any compile error fails the gate) ─────────────"
dotnet build "$SLN" --no-restore -clp:ErrorsOnly -m

# Build the exclusion filter from the quarantine ledger.
FILTER="$(grep -v '^\s*#' "$QUARANTINE" | grep -v '^\s*$' \
          | sed 's/^/FullyQualifiedName!~/' | paste -sd'&' -)"

echo "── Test (blocking; quarantined tests excluded) ─────────────────────"
dotnet test "$SLN" --no-build --filter "$FILTER"

echo "── Quarantined tests (report-only; failure expected, not blocking) ─"
INCLUDE="$(grep -v '^\s*#' "$QUARANTINE" | grep -v '^\s*$' \
           | sed 's/^/FullyQualifiedName~/' | paste -sd'|' -)"
dotnet test "$SLN" --no-build --filter "$INCLUDE" || true

echo "── Gate PASSED ─────────────────────────────────────────────────────"
