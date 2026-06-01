#!/usr/bin/env bash
# File:    tools/perf-harness/run.sh
# Spec:    Performance Optimization Strategy #18 Appendix E, §3.3.5, §4.1
# Purpose: Stage 0 synthetic harness runner.
#          Executes a scenario manifest against the manual Stopwatch harness and writes
#          a baseline-record JSON projection under the specified output directory.
#
#          Stage 0:    manual Stopwatch-based capture per §3.3.5; no src/ code exercised.
#          Stage 0+1:  replaced by the production harness against tests/perf/<spec>/.
#
# Usage:
#   bash tools/perf-harness/run.sh \
#       --scenario tools/perf-harness/scenarios/anchor-baseline.manifest.json \
#       --seed 11400714819323198485 \
#       --output docs/specs/performance-optimization/baselines \
#       [--mark-anchor]

set -euo pipefail

SCENARIO=""
SEED=""
OUTPUT_DIR=""
MARK_ANCHOR=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --scenario)   SCENARIO="$2";   shift 2 ;;
        --seed)       SEED="$2";       shift 2 ;;
        --output)     OUTPUT_DIR="$2"; shift 2 ;;
        --mark-anchor) MARK_ANCHOR=true; shift ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [[ -z "$SCENARIO" || -z "$SEED" || -z "$OUTPUT_DIR" ]]; then
    echo "Usage: run.sh --scenario MANIFEST --seed SEED --output DIR [--mark-anchor]" >&2
    exit 1
fi

if [[ ! -f "$SCENARIO" ]]; then
    echo "ERROR: scenario manifest not found: $SCENARIO" >&2
    exit 1
fi

# ── Parse manifest ────────────────────────────────────────────────────────────
# Requires python3 + json module (stdlib).
SCENARIO_ID=$(python3 -c "import json,sys; d=json.load(open('$SCENARIO')); print(d['scenario_manifest_id'])")
LOOP_TAG=$(python3    -c "import json,sys; d=json.load(open('$SCENARIO')); print(d.get('loop_tag','LOOP-TACTICAL-10HZ'))")
GIT_SHA=$(git rev-parse HEAD 2>/dev/null || echo "0000000000000000000000000000000000000000")
HARNESS_VERSION="0.1.0"
NOW=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

echo "  Scenario  : $SCENARIO_ID"
echo "  Seed      : $SEED"
echo "  Loop tag  : $LOOP_TAG"
echo "  Git SHA   : $GIT_SHA"

# ── Stage 0 Stopwatch capture ─────────────────────────────────────────────────
# No real game code runs at Stage 0. The harness records synthetic timing stubs
# so the JSON projection schema can be validated end-to-end (FR-PO-069 / §3.3.5).
# Replace this block at Stage 0+1 with the production benchmark invocation.

P50_MS="0.000"
P99_MS="0.000"
ANCHOR_FLAG=""
$MARK_ANCHOR && ANCHOR_FLAG=", \"anchor\": true"

# ── Write baseline-record JSON projection (Appendix A §A.3) ──────────────────
SPEC_SLUG=$(python3 -c "import json,sys; d=json.load(open('$SCENARIO')); print(d.get('spec_id','unknown').lstrip('#'))")
RECORD_DIR="$OUTPUT_DIR/spec-$SPEC_SLUG"
mkdir -p "$RECORD_DIR"

RECORD_FILE="$RECORD_DIR/${SCENARIO_ID}-${SEED}-${GIT_SHA:0:8}.json"

python3 - <<PYEOF
import json, sys
record = {
    "format_note": "Stage 0 JSON projection per Spec #18 Appendix A §A.3",
    "session_manifest": {
        "git_sha":               "$GIT_SHA",
        "seed":                  $SEED,
        "environment_fingerprint": "Stage0Dev (certification-platform.md not yet pinned)",
        "platform_pin":          "TBD — see docs/tracking/certification-platform.md",
        "scenario_manifest_id":  "$SCENARIO_ID",
        "session_start_utc":     "$NOW",
        "session_end_utc":       "$NOW",
        "hardware_counters": {
            "cpu_model":    "unknown (Stage 0 placeholder)",
            "core_count":   0,
            "thermal_state": "unknown"
        },
        "harness_version": "$HARNESS_VERSION"
    },
    "loop_tag":              "$LOOP_TAG",
    "per_tick_ms_p50":      $P50_MS,
    "per_tick_ms_p99":      $P99_MS,
    "per_method_alloc_bytes": {},
    "pass_fail_vs_threshold": "advisory",
    "threshold_cited":       "FR-PO-031 (Stage 0 placeholder; no src/ code runs yet)"
}
with open("$RECORD_FILE", "w") as f:
    json.dump(record, f, indent=2)
print(f"  Written: $RECORD_FILE")
PYEOF
