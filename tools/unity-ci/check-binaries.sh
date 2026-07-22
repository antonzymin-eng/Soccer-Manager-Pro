#!/usr/bin/env bash
# tools/unity-ci/check-binaries.sh
#
# Created: July 22, 2026
# Purpose: PR gate against un-LFS'd large binaries. Any tracked file larger than
#   the threshold MUST be routed to Git LFS by .gitattributes (filter=lfs).
#   This keeps the git history from bloating with binary game assets (textures,
#   models, audio, prebuilt libraries) while leaving small intentional binaries
#   (e.g. the ~26 KB src/deterministic-sim/native/td_mxcsr.dll plugin) alone.
#
# Enforces "large ⇒ LFS-designated", NOT "must already be an LFS pointer", so it
# does not force-migrate pre-existing small blobs and runs without git-lfs
# installed (it trusts the .gitattributes routing).
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

THRESHOLD_BYTES=${TD_BINARY_THRESHOLD_BYTES:-1048576}   # 1 MiB default
fail=0

while IFS= read -r f; do
  [ -f "$f" ] || continue
  size=$(wc -c < "$f")
  [ "$size" -le "$THRESHOLD_BYTES" ] && continue
  filter=$(git check-attr filter -- "$f" | awk -F': ' '{print $NF}')
  if [ "$filter" != "lfs" ]; then
    printf 'LARGE NON-LFS FILE: %s (%s bytes) — filter=%s\n' "$f" "$size" "$filter"
    fail=1
  fi
done < <(git ls-files)

if [ "$fail" -ne 0 ]; then
  echo "::error::One or more large files (> $THRESHOLD_BYTES bytes) are not routed to Git LFS."
  echo "Add the extension to .gitattributes (filter=lfs diff=lfs merge=lfs -text) and 'git lfs track' it,"
  echo "or reduce the file. Override the threshold with TD_BINARY_THRESHOLD_BYTES if intentional."
  exit 1
fi
echo "Binary guard OK: no un-LFS'd files over $THRESHOLD_BYTES bytes."
