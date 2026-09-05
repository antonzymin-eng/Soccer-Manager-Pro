#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_REF="${1:-}"

if [ -z "$BASE_REF" ]; then
    echo "ERROR: approval-transition audit requires the PR base commit SHA." >&2
    exit 2
fi

scope_file="$(mktemp)"
cleanup() { rm -f "$scope_file"; }
trap cleanup EXIT INT TERM

python3 "$ROOT/tools/testing-strategy-approval-scope.py" \
    --repo-root "$ROOT" --base "$BASE_REF" --head HEAD > "$scope_file"

if [ ! -s "$scope_file" ]; then
    echo "Testing Strategy approval-transition blocking scope: none"
    exit 0
fi

approval_args=(--changed-scope --quiet-survey)
while IFS= read -r spec_dir; do
    [ -n "$spec_dir" ] || continue
    approval_args+=(--enforce-dir "$spec_dir")
done < "$scope_file"

printf 'Testing Strategy approval-transition blocking scope:\n'
cat "$scope_file"

python3 "$ROOT/tools/checklist-auditor.py" \
    --root "$ROOT/docs/specs" --repo-root "$ROOT" "${approval_args[@]}"
python3 "$ROOT/tools/spec5-schema-auditor.py" \
    --root "$ROOT/docs/specs" --repo-root "$ROOT" "${approval_args[@]}"
