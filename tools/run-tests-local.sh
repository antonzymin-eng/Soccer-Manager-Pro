#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:---pr}"

usage() {
  cat <<'USAGE'
Usage: bash tools/run-tests-local.sh [--pre-commit|--pr|--nightly|--install-hook]

Stable local/CI entry point for Testing Strategy #19 FR-TS-075/079.

  --pre-commit  Local pre-commit pipeline entry point.
  --pr          PR-equivalent local pipeline (default).
  --nightly     Nightly full-suite pipeline entry point.
  --install-hook Configure this clone to use the versioned .githooks directory.

The repository does not yet have reliable NUnit taxonomy categories for selecting
FR-TS-075 tiers independently. Until that split exists, all runnable modes execute
the existing whole-tree non-certifying dotnet gate as a conservative superset.
The runner must not be described as platform determinism certification.
USAGE
}

case "$MODE" in
  --install-hook)
    git -C "$ROOT" config core.hooksPath .githooks
    chmod +x "$ROOT/.githooks/pre-commit"
    printf 'Configured core.hooksPath=.githooks for %s\n' "$ROOT"
    exit 0
    ;;
  --pre-commit)
    PIPELINE_NAME="pre-commit"
    ;;
  --pr)
    PIPELINE_NAME="PR"
    ;;
  --nightly)
    PIPELINE_NAME="nightly"
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

printf '== Testing Strategy %s pipeline ==\n' "$PIPELINE_NAME"
printf 'Runner: tools/dotnet-ci/run-gate.sh (whole-tree, non-certifying)\n'
exec bash "$ROOT/tools/dotnet-ci/run-gate.sh"
