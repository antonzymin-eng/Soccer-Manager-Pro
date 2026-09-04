#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
bash "$ROOT/tools/run-tests-local.sh" --install-hook
bash "$ROOT/tools/run-tests-local.sh" --verify-hook
printf 'Developer bootstrap complete.\n'
