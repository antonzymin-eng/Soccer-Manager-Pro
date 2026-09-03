#!/usr/bin/env bash
# check_drift.sh — deterministic pre-checks before writing a new landing entry.
#
# This mechanizes three failure classes the skill doc calls out by name, all of
# which have happened more than once and cost a dedicated reconciliation pass
# to notice by eye:
#
#   1. The changelog chain (docs/tracking/CHANGELOG.md and CHANGELOG-src.md —
#      root CLAUDE.md itself carries no header chain any more) ends up with
#      more than one bare "**Last Updated:**" label — found and fixed at least
#      three times. The rule is exactly one; every older entry must read
#      "**Last Updated (prior):**".
#   2. A tracking doc's own "Last Updated" claim trails the commits that
#      actually touched it, so a new entry gets layered on a stale base.
#   3. The OPEN ISSUES active/resolved counts drift from the real count — this
#      has happened and been silently wrong before (the August 10, 2026
#      correction in CLAUDE.md itself records a stale "14 active" claim).
#
# This script reports; it does not decide whether drift is acceptable to land
# on top of, or write anything. That's still a judgment call for whoever is
# about to write the entry.
set -euo pipefail
repo_root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$repo_root"

echo "== Duplicate bare '**Last Updated:**' label check (changelog chain) =="
for f in docs/tracking/CHANGELOG.md docs/tracking/CHANGELOG-src.md; do
  [[ -f "$f" ]] || { echo "MISSING: $f"; continue; }
  bare="$(grep -cE '^> \*\*Last Updated:\*\*' "$f" || true)"
  prior="$(grep -cE '^> \*\*Last Updated \(prior\):\*\*' "$f" || true)"
  if [[ "$bare" -ne 1 ]]; then
    echo "FAIL: $f has $bare bare '**Last Updated:**' label(s) — exactly one is required."
    grep -nE '^> \*\*Last Updated' "$f" | cut -c1-160
  else
    echo "OK: $f — 1 bare label, $prior (prior) entries."
  fi
done
# Two root docs have had their chains split out and neither may regrow one.
# Root CLAUDE.md's moved on July 31, 2026 (~52 blockquote-prefixed occurrences
# pre-split); README.md's moved on September 3, 2026 (38 entries, 564 lines,
# 47.6% of the file). Catch either the old blockquote-prefixed form
# ("> **Last Updated:**") or a bare re-introduction — a chain re-added in
# either shape means the split regressed. README is checked here rather than in
# the staleness loop below by design: it no longer declares a date to compare
# against, because it is an orientation document and not a landing ledger.
while IFS='|' read -r f archive; do
  [[ -f "$f" ]] || { echo "MISSING: $f"; continue; }
  stray="$(grep -cE '^(> )?\*\*Last Updated' "$f" || true)"
  if [[ "$stray" -eq 0 ]]; then
    echo "OK: $f carries no header chain (it lives in $archive)."
  else
    echo "FAIL: $f has $stray 'Last Updated' line(s) — the chain moved to $archive."
    grep -nE '^(> )?\*\*Last Updated' "$f" | cut -c1-160
  fi
done <<'CHAINLESS'
CLAUDE.md|docs/tracking/CHANGELOG.md
README.md|docs/tracking/CHANGELOG-readme.md
CHAINLESS
echo

echo "== Tracking-doc staleness (declared date vs. last git-touch) =="
for f in docs/tracking/file-manifest.md docs/tracking/CHANGELOG.md docs/tracking/CHANGELOG-src.md; do
  [[ -f "$f" ]] || continue
  # Anchor to the start of the line (optionally behind the changelogs' "> "
  # blockquote prefix) so this can't match the words "**Last Updated:**"
  # mid-sentence inside a file's own Purpose/convention description — the
  # unanchored version did, in both CHANGELOG.md and CHANGELOG-src.md, whose
  # header paragraphs quote that literal string. Cut at the em dash that
  # opens the prose body ("… August 24, 2026 — **ADVERSARIAL…**") so only the
  # date prints; head -c is a hard backstop for any entry that lacks the dash.
  declared="$(grep -m1 -oE '^(> )?\*\*Last Updated(\s*\(prior\))?:\*\*[^—]*' "$f" | head -c 60 || true)"
  [[ -n "$declared" ]] || declared="(no Last Updated line found)"
  touched="$(git log -1 --format=%cs -- "$f" 2>/dev/null || echo unknown)"
  printf '%-34s declares: %-62s git-touched: %s\n' "$f" "$declared" "$touched"
done
echo

echo "== Open-issues active/resolved count check =="
active="$(grep -c '^- \*\*' docs/tracking/open-issues.md 2>/dev/null || true)"
resolved="$(grep -c '^- \*\*' docs/tracking/open-issues-resolved.md 2>/dev/null || true)"
echo "counted: $active active / $resolved resolved"
claim="$(sed -nE 's/^\*\*([0-9]+) active\*\* \/ ([0-9]+) resolved.*/\1 \2/p' CLAUDE.md)"
if [[ "$(printf '%s\n' "$claim" | grep -c .)" -ne 1 ]]; then
  echo "UNPARSED: root CLAUDE.md's OPEN ISSUES header did not match '**N active** / M resolved' exactly once — compare by hand."
else
  read -r claimed_active claimed_resolved <<< "$claim"
  if [[ "$claimed_active" == "$active" && "$claimed_resolved" == "$resolved" ]]; then
    echo "OK: root CLAUDE.md claims $claimed_active active / $claimed_resolved resolved — matches."
  else
    echo "FAIL: root CLAUDE.md claims $claimed_active active / $claimed_resolved resolved — counted $active / $resolved."
  fi
fi
