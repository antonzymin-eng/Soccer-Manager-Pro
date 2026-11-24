#!/usr/bin/env bash
# W13 §10 capture or a descriptive distance/scheme sensitivity run.
set -euo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
mode=${1:?Usage: run-marking-protocol.sh protocol|distances|schemes|sensitivity OUTPUT_DIRECTORY}
output=${2:?Set an output directory for the captures}
mkdir -p "$output"
output=$(cd "$output" && pwd)
cd "$root"
dotnet --info >"$output/toolchain.txt"

project=src/match-engine/tests/match-engine-tests.gen.csproj
python3 tools/dotnet-ci/generate_projects.py
dotnet build --configuration Release --artifacts-path "$output/.build" "$project"

capture() {
    local name=$1 armed=$2 distance=$3 tight_distance=$4 tight=$5 scheme=$6
    local directory="$output/$name"
    mkdir -p "$directory"
    cat >"$directory/gameplay-$armed.ini" <<CONFIG
[match-engine]
MarkMovementEnabled = $armed
[defensive-ai]
MarkStandoffM = $distance
MarkStandoffTightM = $tight_distance
CONFIG
    TD_MARKING_PROTOCOL_OUT="$directory" \
        TD_MARKING_PROTOCOL_CONFIG="$directory/gameplay-$armed.ini" \
        TD_MARKING_PROTOCOL_TIGHT="$tight" \
        TD_MARKING_PROTOCOL_SCHEME="$scheme" \
        dotnet test --configuration Release --no-build --artifacts-path "$output/.build" "$project" \
            --filter 'FullyQualifiedName~MarkingProtocol_Capture' | tee "$directory/capture-$armed.log"
}

capture_distances() {
    export TD_MARKING_MEASURE_CASES="${TD_MARKING_MEASURE_CASES:-0,3,4,7,8}"
    for distance in 1.5 2.0 2.5; do
        capture "normal-$distance" true "$distance" 1.0 false Mixed
    done
    for distance in 0.75 1.0 1.25; do
        capture "tight-$distance" true 2.0 "$distance" true Mixed
    done
}

capture_schemes() {
    capture mixed true 2.0 1.0 false Mixed
    capture man-oriented true 2.0 1.0 false ManOriented
}

case "$mode" in
    protocol)
        capture . false 2.0 1.0 false Mixed
        capture . true 2.0 1.0 false Mixed
        ;;
    distances)
        capture_distances
        ;;
    schemes)
        capture_schemes
        ;;
    sensitivity)
        capture_distances
        capture_schemes
        ;;
    *)
        echo "Unknown capture mode: $mode" >&2
        exit 2
        ;;
esac
