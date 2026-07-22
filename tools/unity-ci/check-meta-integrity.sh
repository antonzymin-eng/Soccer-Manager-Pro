#!/usr/bin/env bash
# tools/unity-ci/check-meta-integrity.sh
#
# Created: July 22, 2026
# Purpose: PR gate for Unity .meta integrity across src/. Fails on any of:
#   (1) MISSING  — a tracked file/folder under src/ has no committed .meta
#                  (Unity would assign it a fresh random GUID on checkout,
#                   silently breaking every reference to it);
#   (2) ORPHAN   — a committed .meta whose asset is gone (dangling importer);
#   (3) DUP GUID — two .meta files sharing a GUID (Unity resolves references by
#                  GUID; a collision makes references non-deterministic).
#
# Runs on Linux with no Unity install — pure git + coreutils. The src/ root
# itself carries no .meta by convention (it is the asset-mount root).
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

fail=0

# ---- tracked-path universe under src/ (files + ancestor dirs, minus .meta) ----
files=$(git ls-files src/ | grep -v '\.meta$' || true)
dirs=$(printf '%s\n' "$files" | while IFS= read -r f; do
  [ -z "$f" ] && continue
  d=$(dirname "$f")
  while [ "$d" != "." ] && [ "$d" != "src" ]; do echo "$d"; d=$(dirname "$d"); done
done | sort -u)

# ---- (1) MISSING ----
missing=0
while IFS= read -r p; do
  [ -z "$p" ] && continue
  if [ ! -e "$p.meta" ]; then echo "MISSING META: $p"; missing=$((missing+1)); fi
done < <(printf '%s\n%s\n' "$files" "$dirs" | sed '/^$/d' | sort -u)
if [ "$missing" -gt 0 ]; then
  echo "::error::$missing tracked path(s) under src/ lack a .meta. Run: bash tools/unity-ci/generate-missing-metas.sh"
  fail=1
fi

# ---- (2) ORPHAN ----
orphan=0
while IFS= read -r m; do
  [ -z "$m" ] && continue
  asset="${m%.meta}"
  if [ ! -e "$asset" ]; then echo "ORPHAN META (asset missing): $m"; orphan=$((orphan+1)); fi
done < <(git ls-files 'src/**.meta' 'src/*.meta')
if [ "$orphan" -gt 0 ]; then
  echo "::error::$orphan orphan .meta file(s) — delete them or restore the asset."
  fail=1
fi

# ---- (3) DUPLICATE GUID ----
# Include committed + working-tree metas so a new PR's metas are checked too.
dups=$(grep -rhoE '^guid: [0-9a-f]{32}' src --include='*.meta' 2>/dev/null \
       | awk '{print $2}' | sort | uniq -d || true)
if [ -n "$dups" ]; then
  echo "DUPLICATE GUIDs:"
  while IFS= read -r g; do
    [ -z "$g" ] && continue
    echo "  $g:"; grep -rl "guid: $g" src --include='*.meta' | sed 's/^/    /'
  done <<< "$dups"
  echo "::error::Duplicate .meta GUID(s) detected."
  fail=1
fi

if [ "$fail" -eq 0 ]; then
  echo "Meta integrity OK: no missing, no orphan, no duplicate GUIDs under src/."
fi
exit "$fail"
