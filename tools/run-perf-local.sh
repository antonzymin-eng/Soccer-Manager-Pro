#!/usr/bin/env bash
# File:    tools/run-perf-local.sh
# Spec:    Performance Optimization Strategy #18 Appendix E, FR-PO-070
# Purpose: Stage 0 local pre-commit perf-gate harness.
#          Runs schema-conformance and loop-tag auditors against docs/specs/ only.
#          Synthetic-harness invocation for anchor baselines.
#
#          Stage 0:    manual Stopwatch harness; auditor output reviewed by hand (FR-PO-070).
#          Stage 0+1:  this script invokes automated tools/budget-auditor.py; CI calls it too.
#
# Usage:
#   bash tools/run-perf-local.sh                    # full run (schema + loop-tag + harness)
#   bash tools/run-perf-local.sh --schema-only      # schema-conformance pass only
#   bash tools/run-perf-local.sh --loop-tag-only    # loop-tag pass only
#   bash tools/run-perf-local.sh --harness-only     # anchor baseline capture only

set -euo pipefail

DOCS_ROOT="docs/specs"
BASELINES_DIR="docs/specs/performance-optimization/baselines"
SCENARIOS_DIR="tools/perf-harness/scenarios"

RUN_SCHEMA=true
RUN_LOOP_TAG=true
RUN_HARNESS=true

for arg in "$@"; do
    case "$arg" in
        --schema-only)   RUN_LOOP_TAG=false; RUN_HARNESS=false ;;
        --loop-tag-only) RUN_SCHEMA=false;   RUN_HARNESS=false ;;
        --harness-only)  RUN_SCHEMA=false;   RUN_LOOP_TAG=false ;;
    esac
done

echo "=== Spec #18 Stage 0 local perf-gate ==="
echo "Docs root : $DOCS_ROOT"
echo "Baselines : $BASELINES_DIR"
echo ""

# ── 1. Schema-conformance auditor (§5.3 / FR-PO-070) ────────────────────────
# Walks every approved spec's §6 section against the Appendix B template.
# Failures logged as ERR-018-NNN candidates.
if $RUN_SCHEMA; then
    echo "── Step 1: Schema-conformance auditor ──────────────────────────────"
    python3 tools/budget-auditor.py --mode schema --docs "$DOCS_ROOT"
    echo ""
fi

# ── 2. Loop-tag auditor (§5.5 / FR-PO-070) ──────────────────────────────────
# Regex pass over every §6 budget table for [LOOP-TACTICAL-10HZ] / [LOOP-PHYSICS-60HZ].
# Untagged budget numbers are §3.2.2 conformance failures.
if $RUN_LOOP_TAG; then
    echo "── Step 2: Loop-tag auditor ────────────────────────────────────────"
    python3 tools/budget-auditor.py --mode loop-tag --docs "$DOCS_ROOT"
    echo ""
fi

# ── 3. Synthetic-harness invocation (Appendix E / §4.1) ─────────────────────
# Each scenario in tools/perf-harness/scenarios/ is run with a recorded seed.
# Output JSON records land under docs/specs/performance-optimization/baselines/.
# Stage 0: manual Stopwatch harness per §3.3.5.
if $RUN_HARNESS; then
    echo "── Step 3: Synthetic harness (anchor baselines) ────────────────────"
    SEED="$(python3 tools/select-seed.py)"
    echo "  Selected seed: $SEED"

    for SCENARIO in "$SCENARIOS_DIR"/*.manifest.json; do
        if [ ! -f "$SCENARIO" ]; then
            echo "  No scenario manifests found in $SCENARIOS_DIR — skipping harness."
            break
        fi
        echo "  Running: $SCENARIO"
        bash tools/perf-harness/run.sh \
            --scenario "$SCENARIO" \
            --seed "$SEED" \
            --output "$BASELINES_DIR" \
            --mark-anchor
    done
    echo ""
fi

# ── 4. Reviewer action (FR-PO-071) ──────────────────────────────────────────
echo "=== Stage 0 local perf-gate complete ==="
echo ""
echo "ACTION REQUIRED (FR-PO-071):"
echo "  Paste the output above into the PR description under the heading:"
echo "  '## Perf Gate — Stage 0 manual audit'"
echo "  Include: schema-conformance findings, loop-tag findings, seed used."
echo ""
echo "  Any ERR-018-NNN candidates must be filed in docs/tracking/spec-error-log.md"
echo "  before the PR is marked review-ready."
