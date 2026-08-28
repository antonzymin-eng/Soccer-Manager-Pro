#!/usr/bin/env bash
# next_err_id.sh — deterministic ERR id allocation for one owning spec.
#
# The allocation itself has no judgment in it: find the highest ERR-<spec>-<seq>
# id used ANYWHERE in docs/ or src/, and propose seq+1. Searching both trees
# (not just spec-error-log.md) matters — a citation can land in an approved
# spec section or a code comment before the log entry exists, and every
# collision this repo has hit was a proposed id that had already been used
# somewhere the log-only search missed.
#
# What this script does NOT do, on purpose: decide the id is safe to use. Ids
# have collided here specifically because they were verified once and then
# claimed by someone else before landing (a branch-vs-main race, in one case).
# Re-run this immediately before you write the entry, and again after any
# rebase — that's a timing/judgment call, not something a single grep settles.
#
# Usage: next_err_id.sh <spec-number>   e.g. next_err_id.sh 011
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: $(basename "$0") <spec-number, e.g. 011 or 30>" >&2
  exit 2
fi

# Accept "11" or "011" — the log always zero-pads to 3 digits.
spec="$(printf '%03d' "$((10#$1))")"
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"

matches="$(grep -rhoE "ERR-${spec}-[0-9]{3}" "$repo_root/docs" "$repo_root/src" 2>/dev/null | sort -u || true)"

if [[ -z "$matches" ]]; then
  next="001"
else
  highest="$(echo "$matches" | sed -E "s/ERR-${spec}-//" | sort -n | tail -1)"
  next="$(printf '%03d' $((10#$highest + 1)))"
fi

echo "Next free id: ERR-${spec}-${next}"
echo
echo "Existing ids found for spec ${spec} (docs/ + src/):"
if [[ -n "$matches" ]]; then echo "$matches" | sed 's/^/  /'; else echo "  (none)"; fi
echo
echo "Not checked by this script — re-verify before you write the entry:"
echo "  - free on the branch you will actually merge into, not just this checkout"
echo "  - free against any design-supplement note proposing this range"
