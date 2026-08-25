#!/usr/bin/env bash
# check_drift.sh — deterministic pre-checks before writing a new landing entry.
#
# This mechanizes two failure classes the skill doc calls out by name, both of
# which have happened more than once and cost a dedicated reconciliation pass
# to notice by eye:
#
#   1. Root CLAUDE.md ends up with more than one bare "**Last Updated:**"
#      label — found and fixed at least three times. The rule is exactly one;
#      every older entry must read "**Last Updated (prior):**".
#   2. A tracking doc's own "Last Updated" claim trails the commits that
#      actually touched it, so a new entry gets layered on a stale base.
#
# It also re-derives the open-issues active count the same way CLAUDE.md's own
# changelog has had to by hand, repeatedly (and gotten wrong at least once —
# see the August 10, 2026 correction in the file itself): `grep -c '^- \*\*'`.
#
# This script reports; it does not decide whether drift is acceptable to land
# on top of, or write anything. That's still a judgment call for whoever is
# about to write the entry.
set -euo pipefail
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

echo "== Duplicate 'Last Updated:' label check (CLAUDE.md) =="
count="$(grep -c '^\*\*Last Updated:\*\*' CLAUDE.md || true)"
if [[ "$count" -gt 1 ]]; then
  echo "FAIL: $count bare '**Last Updated:**' labels found — exactly one is allowed."
  grep -n '^\*\*Last Updated' CLAUDE.md
else
  echo "OK: $count bare label(s) found."
fi
echo

echo "== Tracking-doc staleness (declared date vs. last git-touch) =="
for f in README.md docs/tracking/file-manifest.md src/CLAUDE.md; do
  [[ -f "$f" ]] || continue
  declared="$(grep -m1 -iE '\*\*Last Updated' "$f" || echo "  (no Last Updated line found)")"
  touched="$(git log -1 --format=%cs -- "$f" 2>/dev/null || echo unknown)"
  printf '%-32s declares: %-60s git-touched: %s\n' "$f" "$declared" "$touched"
done
echo

echo "== Open-issues active-count re-derivation =="
if [[ -f docs/tracking/open-issues.md ]]; then
  active="$(grep -c '^- \*\*' docs/tracking/open-issues.md || true)"
  echo "grep -c '^- \\*\\*' docs/tracking/open-issues.md => $active"
  echo "Compare against whatever count the CLAUDE.md OPEN ISSUES header currently states."
fi
if [[ -f docs/tracking/open-issues-resolved.md ]]; then
  resolved="$(grep -c '^- \*\*' docs/tracking/open-issues-resolved.md || true)"
  echo "grep -c '^- \\*\\*' docs/tracking/open-issues-resolved.md => $resolved"
fi
