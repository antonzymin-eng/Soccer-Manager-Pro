#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash "$ROOT/tools/run-tests-local.sh" --install-hook
bash "$ROOT/tools/run-tests-local.sh" --verify-hook

# The hook's 60-second contract assumes an incremental developer path, not a
# zero-cache clone. Bootstrap performs the one cold staged-index restore/build
# outside that acceptance budget and leaves bin/obj/generated outputs in the
# persistent .git/testing-strategy/precommit-snapshot for subsequent commits.
TD_PRECOMMIT_PREPARE=1 bash "$ROOT/.githooks/pre-commit"

printf 'Developer bootstrap complete; pre-commit cache prepared.\n'
