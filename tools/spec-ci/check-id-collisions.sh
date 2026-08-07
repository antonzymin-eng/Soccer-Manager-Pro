#!/usr/bin/env bash
# tools/spec-ci/check-id-collisions.sh
#
# Created: August 7, 2026
# Purpose: PR gate for identifier collisions across the project's numbered
#   namespaces. Every collision this repo has recorded happened because two
#   workstreams allocated the same id against state that moved underneath them:
#
#     (1) a design supplement proposed an ERR id already filed (3 instances,
#         July 2026 wave; ERR-028-002..004 went stale the same way);
#     (2) BRANCH-vs-MAIN — ERR-030-015 was verified free on a branch, then
#         claimed on main by #30's T3 landing while that branch was still open,
#         and had to be reassigned to ERR-030-025;
#     (3) two branches fixing the SAME ERR both merged — PR #59 + PR #60 each
#         authored an Appendix F.0 schema for ERR-018-005; git kept both, and
#         that one merge produced ERR-018-012..018 (duplicate schema sections,
#         duplicate constant rows, duplicate v0.2 version-history rows in
#         seven files);
#     (4) the same id filed twice from two approvals — ERR-030-007 at #42's and
#         again at #32's, both taking "step 7" in a sequence six approved specs
#         cite by number.
#
#   Case (2) is the one that matters most here: a check run at AUTHORING time
#   cannot see it, because the log moves under an open branch. This script runs
#   on `pull_request`, which is the first moment both sides are visible.
#
#   Blocking checks (1-6) are clean across the tree as of this commit. Checks
#   (7)-(8) carry a baseline of pre-existing hits (see known-id-collisions.txt)
#   so NEW duplicates fail while shipped ones stay preserved verbatim as errata
#   — the root CLAUDE.md rule that a duplicate already in approved text is not
#   silently renumbered. Check (9) is informational only.
#
# Usage:
#   bash tools/spec-ci/check-id-collisions.sh                # gate
#   bash tools/spec-ci/check-id-collisions.sh --emit-baseline # regenerate baseline
#
# Pure grep/awk/coreutils — no Unity, no .NET, no network.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

ERR_LOG="docs/tracking/spec-error-log.md"
DS_CONST="src/deterministic-sim/DeterministicSimConstants.cs"
DS_ORD="src/deterministic-sim/SubsystemOrdinals.cs"
BASELINE="tools/spec-ci/known-id-collisions.txt"

EMIT_BASELINE=0
[ "${1:-}" = "--emit-baseline" ] && EMIT_BASELINE=1

fail=0
findings=""   # accumulated `check|file|token` lines for the baselined checks

note()  { printf '%s\n' "$*"; }
emit()  { findings="${findings}$1|$2|$3"$'\n'; }

# ---------------------------------------------------------------------------
# (1) Duplicate ERR detail entries  —  `## ERR-NNN-NNN: ...`
# ---------------------------------------------------------------------------
if [ -f "$ERR_LOG" ]; then
  dups=$(grep -hoE '^## ERR-[0-9]{3}-[0-9]{3}' "$ERR_LOG" | sed 's/^## //' | sort | uniq -d || true)
  if [ -n "$dups" ]; then
    note "::error::Duplicate ERR detail entries in $ERR_LOG — two workstreams claimed one id."
    printf '  %s\n' $dups
    note "  Precedence: main's claim wins. Reassign the newer entry to the next free id."
    fail=1
  fi

  # ---------------------------------------------------------------------------
  # (2) Duplicate ERR index rows  —  `| ERR-NNN-NNN | ... |`
  # ---------------------------------------------------------------------------
  dups=$(grep -hoE '^\| ERR-[0-9]{3}-[0-9]{3} \|' "$ERR_LOG" | tr -d '| ' | sort | uniq -d || true)
  if [ -n "$dups" ]; then
    note "::error::Duplicate rows in the $ERR_LOG Error Index table."
    printf '  %s\n' $dups
    fail=1
  fi
fi

# ---------------------------------------------------------------------------
# (3) Duplicate DOMAIN_TAG values
#     Scoped to `public const byte` DECLARATIONS — the file's version-history
#     comments quote allocated tags and would otherwise self-trip.
# ---------------------------------------------------------------------------
if [ -f "$DS_CONST" ]; then
  dups=$(grep -oE 'public const byte +DOMAIN_TAG_[A-Z0-9_]+ *= *0x[0-9A-Fa-f]+' "$DS_CONST" \
         | sed -E 's/.*= *(0x[0-9A-Fa-f]+)/\1/' | tr 'a-f' 'A-F' | sort | uniq -d || true)
  if [ -n "$dups" ]; then
    note "::error::Two DOMAIN_TAG constants share a value in $DS_CONST."
    for v in $dups; do
      note "  $v:"
      grep -oE "public const byte +DOMAIN_TAG_[A-Z0-9_]+ *= *$v" "$DS_CONST" | sed 's/^/    /'
    done
    note "  A shared domain tag silently merges two hash namespaces — see #16 §3.4."
    fail=1
  fi
fi

# ---------------------------------------------------------------------------
# (4) Duplicate SubsystemOrdinals values
# ---------------------------------------------------------------------------
if [ -f "$DS_ORD" ]; then
  dups=$(grep -oE 'public const [A-Za-z]+ +[A-Za-z0-9_]+ *= *[0-9]+' "$DS_ORD" \
         | sed -E 's/.*= *([0-9]+)/\1/' | sort | uniq -d || true)
  if [ -n "$dups" ]; then
    note "::error::Two SubsystemOrdinals constants share a value in $DS_ORD."
    printf '  ordinal %s\n' $dups
    fail=1
  fi
fi

# ---------------------------------------------------------------------------
# (5) Duplicate FR definitions  —  `- **FR-XX-NNN** — ...` in a section-2 file
#     Covers the specs that follow the current authoring convention; a spec
#     predating it simply contributes no definition lines.
# ---------------------------------------------------------------------------
fr_defs=$(grep -rhoE '^- \*\*FR-[A-Z]{2,3}-[0-9]{3}[a-z]?\*\*' docs/specs/*/section-2*.md 2>/dev/null \
          | sed -E 's/^- \*\*(FR-[A-Z]{2,3}-[0-9]{3}[a-z]?)\*\*/\1/' || true)
if [ -n "$fr_defs" ]; then
  dups=$(printf '%s\n' "$fr_defs" | sort | uniq -d || true)
  if [ -n "$dups" ]; then
    note "::error::The same FR id is defined more than once."
    for id in $dups; do
      note "  $id:"
      grep -rlE "^- \*\*$id\*\*" docs/specs/*/section-2*.md 2>/dev/null | sed 's/^/    /'
    done
    fail=1
  fi
fi

# ---------------------------------------------------------------------------
# (6) FR- prefix defined in more than one spec folder
#     `FR-PR-` was proposed for Positional Rotations and is already Pressing
#     AI's; a prefix must have exactly one owning folder.
# ---------------------------------------------------------------------------
prefix_owners=$(for f in docs/specs/*/section-2*.md; do
  [ -f "$f" ] || continue
  d=$(basename "$(dirname "$f")")
  grep -ohE '^- \*\*FR-[A-Z]{2,3}-[0-9]{3}' "$f" 2>/dev/null \
    | sed -E 's/^- \*\*FR-([A-Z]{2,3})-[0-9]{3}/\1/' | sort -u | sed "s|\$| $d|"
done | sort -u || true)
if [ -n "$prefix_owners" ]; then
  clashes=$(printf '%s\n' "$prefix_owners" | awk '{print $1}' | sort | uniq -d || true)
  if [ -n "$clashes" ]; then
    note "::error::An FR- prefix is defined by more than one spec folder."
    for p in $clashes; do
      note "  FR-$p- : $(printf '%s\n' "$prefix_owners" | awk -v p="$p" '$1==p {printf "%s ", $2}')"
    done
    fail=1
  fi
fi

# ---------------------------------------------------------------------------
# (7) Duplicate version-history rows  —  the PR #59 + PR #60 class
#     A version-history row is `| <ver> | <date> | ...`. Requiring a DATE in
#     column 2 is what separates it from the many numeric data tables in the
#     spec tree (sensitivity tables, lookup tables) whose first column is also
#     a decimal. Both date forms in use are matched.
# ---------------------------------------------------------------------------
VER_ROW='^\|? *v?[0-9]+\.[0-9]+(\.[0-9]+)? *\| *([0-9]{4}-[0-9]{2}-[0-9]{2}|[A-Z][a-z]+ [0-9]{1,2}, [0-9]{4})'
while IFS= read -r f; do
  [ -f "$f" ] || continue
  dups=$(grep -ohE "$VER_ROW" "$f" 2>/dev/null | sed -E 's/^\|? *v?([0-9.]+) *\|.*/\1/' | sort | uniq -d || true)
  for v in $dups; do emit "version-row" "$f" "$v"; done
done < <(git ls-files 'docs/specs/*.md' 'docs/tracking/*.md' 'src/**/*.cs' 2>/dev/null)

# ---------------------------------------------------------------------------
# (8) More than one `**Version:**` field in a file
#     spec-error-log.md carried two (1.39 and 1.40), both stale against the
#     entry above them — recorded in that file's own v1.36 housekeeping note.
# ---------------------------------------------------------------------------
while IFS= read -r f; do
  [ -f "$f" ] || continue
  c=$(grep -cE '^\*\*Version:\*\*' "$f" 2>/dev/null || true)
  [ "${c:-0}" -gt 1 ] && emit "multi-version-field" "$f" "$c"
done < <(git ls-files 'docs/specs/*.md' 'docs/tracking/*.md' 2>/dev/null)

# ---- baseline reconciliation for (7) and (8) -------------------------------
findings=$(printf '%s' "$findings" | sed '/^$/d' | sort -u)

if [ "$EMIT_BASELINE" = "1" ]; then
  {
    echo "# tools/spec-ci/known-id-collisions.txt"
    echo "# Regenerated by: bash tools/spec-ci/check-id-collisions.sh --emit-baseline"
    echo "# Format: <check>|<file>|<token>"
    printf '%s\n' "$findings"
  } > "$BASELINE"
  note "Baseline written to $BASELINE ($(printf '%s\n' "$findings" | sed '/^$/d' | wc -l) entries)."
  exit 0
fi

known=""
[ -f "$BASELINE" ] && known=$(grep -vE '^\s*(#|$)' "$BASELINE" | sort -u || true)

new=$(comm -23 <(printf '%s\n' "$findings" | sed '/^$/d') <(printf '%s\n' "$known" | sed '/^$/d') || true)
if [ -n "$new" ]; then
  note "::error::New duplicate version numbering (not in $BASELINE):"
  printf '%s\n' "$new" | while IFS='|' read -r c f t; do
    case "$c" in
      version-row)         note "  $f — version '$t' appears on more than one version-history row" ;;
      multi-version-field) note "  $f — $t '**Version:**' fields; there must be exactly one" ;;
    esac
  done
  note "  Two branches editing one file both wrote the same version. Reconcile by UNION, then"
  note "  renumber the later row — git keeps both sides silently (PR #59 + PR #60 → ERR-018-012..018)."
  fail=1
fi

# a baselined entry that no longer reproduces should be retired
stale=$(comm -13 <(printf '%s\n' "$findings" | sed '/^$/d') <(printf '%s\n' "$known" | sed '/^$/d') || true)
if [ -n "$stale" ]; then
  note "::warning::Baseline entries that no longer reproduce — remove them from $BASELINE:"
  printf '  %s\n' $stale
fi

# ---------------------------------------------------------------------------
# (9) INFORMATIONAL — ERR detail entry with no Error Index row.
#     The log has two surfaces per entry and it is easy to land in the detail
#     section and never scroll back. Non-blocking: 24 pre-existing at the time
#     this gate was written, all predating the two-surface convention.
# ---------------------------------------------------------------------------
if [ -f "$ERR_LOG" ]; then
  orphans=$(comm -23 \
    <(grep -hoE '^## ERR-[0-9]{3}-[0-9]{3}' "$ERR_LOG" | sed 's/^## //' | sort -u) \
    <(grep -hoE '^\| ERR-[0-9]{3}-[0-9]{3} \|' "$ERR_LOG" | tr -d '| ' | sort -u) || true)
  n=$(printf '%s\n' "$orphans" | sed '/^$/d' | wc -l | tr -d ' ')
  [ "$n" -gt 0 ] && note "::warning::$n ERR detail entr(ies) have no Error Index row (informational)."
fi

if [ "$fail" -eq 0 ]; then
  note "id-collision gate: PASSED"
fi
exit "$fail"
