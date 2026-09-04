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
#      correction in CLAUDE.md itself records a stale "14 active" claim). The
#      claim now lives in docs/agent-guides/project-reference.md, not root
#      CLAUDE.md; this check read the old location until September 4, 2026 and
#      printed UNPARSED on every run while the claim drifted to 15/46 against a
#      true 21/51. A missing surface is now reported as BROKEN, not shrugged at.
#
# EXIT-CODE CONTRACT. This script reports; it does not decide whether drift is
# acceptable to land on top of, or write anything. Classes 2 and 3 above are
# therefore advisory and never affect the status — that judgment belongs to
# whoever is about to write the entry.
#
# The no-chain guard is the one exception, and it exits 1. A header chain
# reintroduced into a file whose chain was split out is a contract violation,
# not a judgment call, so a caller reading the status must not see a pass. The
# guard sets chain_violation and the script exits AFTER printing the whole
# report, so a run still shows every section rather than stopping at the first
# violation. (Added September 3, 2026: the guard previously printed FAIL and
# still exited 0, so automation and chained close-out commands treated a
# forbidden chain as a successful check.)
set -euo pipefail
chain_violation=0
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
    chain_violation=1
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
# The claim lives in docs/agent-guides/project-reference.md's OPEN ISSUES section.
# It used to live in root CLAUDE.md; the compact restructure moved it and this check
# was not repointed, so from then until September 4, 2026 it read a file with no such
# section and printed UNPARSED on every run — checking nothing, while the real claim
# drifted to 15/46 against a true 21/51. A check that cannot find its surface is a
# broken check, not a clean result, so the two cases are now reported differently.
claim_file="docs/agent-guides/project-reference.md"
if [[ ! -f "$claim_file" ]]; then
  echo "BROKEN: $claim_file does not exist — this check has no surface to read; repoint it."
else
  claim="$(sed -nE 's/^\*\*([0-9]+) active\*\* \/ ([0-9]+) resolved.*/\1 \2/p' "$claim_file")"
  n_claims="$(printf '%s\n' "$claim" | grep -c . || true)"
  if [[ "$n_claims" -eq 0 ]]; then
    echo "BROKEN: $claim_file carries no '**N active** / M resolved' line — this check is currently checking NOTHING. Find where the claim moved and repoint it; do not read this as a pass."
  elif [[ "$n_claims" -ne 1 ]]; then
    echo "UNPARSED: $claim_file matched '**N active** / M resolved' $n_claims times, expected exactly once — compare by hand."
  else
    read -r claimed_active claimed_resolved <<< "$claim"
    if [[ "$claimed_active" == "$active" && "$claimed_resolved" == "$resolved" ]]; then
      echo "OK: $claim_file claims $claimed_active active / $claimed_resolved resolved — matches."
    else
      echo "FAIL: $claim_file claims $claimed_active active / $claimed_resolved resolved — counted $active / $resolved."
    fi
  fi
fi

# The one status-affecting condition — see the EXIT-CODE CONTRACT at the top.
if [[ "$chain_violation" -ne 0 ]]; then
  echo
  echo "check_drift.sh: FAILED — a forbidden 'Last Updated' header chain is present (see above)."
  exit 1
fi
