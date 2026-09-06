#!/usr/bin/env bash
# tools/unity-ci/check-meta-integrity.sh
#
# Created: July 22, 2026
# Purpose: PR gate for Unity .meta integrity across the repository's managed
#          Unity asset roots. Fails on any of:
#   (1) MISSING  — a tracked file/folder under src/ or Assets/GameArt/ has no
#                  committed .meta (Unity would assign a fresh random GUID on
#                  checkout, silently breaking references);
#   (2) ORPHAN   — a committed .meta in those managed roots whose asset is gone;
#   (3) DUP GUID — any two tracked Unity .meta files under Assets/ plus the
#                  junction-backed src/ tree share a GUID. Unity resolves GUIDs
#                  project-wide, so duplicate detection must be one combined
#                  scan rather than separate per-root scans.
#
# Runs on Linux with no Unity install — pure git + coreutils. The src/ root
# itself carries no .meta by convention (it is the asset-mount root).
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

fail=0

# ---- managed tracked-path universe (files + ancestor dirs, minus .meta) ----
# Missing/orphan ownership is intentionally limited to src/ + Assets/GameArt/.
# Other Assets/ content participates in project-wide duplicate-GUID detection,
# but AP-01 does not claim ownership of its missing/orphan lifecycle.
files=$(git ls-files src/ Assets/GameArt/ | grep -v '\.meta$' || true)
dirs=$(printf '%s\n' "$files" | while IFS= read -r f; do
  [ -z "$f" ] && continue
  case "$f" in
    src/*)
      d=$(dirname "$f")
      while [ "$d" != "." ] && [ "$d" != "src" ]; do
        echo "$d"
        d=$(dirname "$d")
      done
      ;;
    Assets/GameArt/*)
      d=$(dirname "$f")
      while [ "$d" != "." ] && [ "$d" != "Assets" ]; do
        echo "$d"
        [ "$d" = "Assets/GameArt" ] && break
        d=$(dirname "$d")
      done
      ;;
  esac
done | sort -u)

# ---- (1) MISSING / UNCOMMITTED ----
# Filesystem existence is not sufficient: an untracked .meta would disappear on
# checkout and Unity would assign a new GUID. Require the meta in the active Git
# index as well as in the working tree. This also makes the local check match CI.
missing=0
while IFS= read -r p; do
  [ -z "$p" ] && continue
  meta="$p.meta"
  if ! git ls-files --error-unmatch -- "$meta" >/dev/null 2>&1 || [ ! -e "$meta" ]; then
    echo "MISSING META: $p"
    missing=$((missing + 1))
  fi
done < <(printf '%s\n%s\n' "$files" "$dirs" | sed '/^$/d' | sort -u)
if [ "$missing" -gt 0 ]; then
  echo "::error::$missing tracked path(s) under src/ or Assets/GameArt/ lack a committed .meta. The generator may repair src/ and GameArt folder metas; production GameArt file metas must come from Unity import."
  fail=1
fi

# ---- (2) ORPHAN ----
orphan=0
managed_metas=$(git ls-files | grep -E '^(src/.*\.meta|Assets/GameArt\.meta|Assets/GameArt/.*\.meta)$' || true)
while IFS= read -r m; do
  [ -z "$m" ] && continue
  asset="${m%.meta}"
  if [ ! -e "$asset" ]; then
    echo "ORPHAN META (asset missing): $m"
    orphan=$((orphan + 1))
  fi
done <<< "$managed_metas"
if [ "$orphan" -gt 0 ]; then
  echo "::error::$orphan orphan .meta file(s) in managed Unity roots — delete them or restore the asset."
  fail=1
fi

# ---- (3) PROJECT-WIDE DUPLICATE GUID ----
# Unity GUIDs are project-wide. Scan every tracked .meta under Assets/ plus the
# junction-backed src/ tree in ONE universe, including GameArt and pre-existing
# Assets content such as scenes/plugins/reference files.
mapfile -t all_meta_files < <(git ls-files | grep -E '^(Assets/.*\.meta|src/.*\.meta)$' || true)
dups=""
if [ "${#all_meta_files[@]}" -gt 0 ]; then
  dups=$(awk '/^guid: [0-9a-f]{32}$/ {print $2}' "${all_meta_files[@]}" \
    | sort | uniq -d || true)
fi
if [ -n "$dups" ]; then
  echo "DUPLICATE GUIDs:"
  while IFS= read -r g; do
    [ -z "$g" ] && continue
    echo "  $g:"
    grep -lE "^guid: ${g}$" "${all_meta_files[@]}" | sed 's/^/    /'
  done <<< "$dups"
  echo "::error::Duplicate Unity .meta GUID(s) detected across tracked Assets/ + src/."
  fail=1
fi

if [ "$fail" -eq 0 ]; then
  echo "Meta integrity OK: no managed-root missing/orphan metas and no duplicate GUIDs across tracked Assets/ + src/."
fi
exit "$fail"
