#!/usr/bin/env bash
# tools/unity-ci/check-binaries.sh
#
# Created: July 22, 2026
# Purpose: PR gate against un-LFS'd large binaries. Any tracked BINARY file larger
#   than the binary threshold MUST be routed to Git LFS by .gitattributes
#   (filter=lfs). This keeps the git history from bloating with binary game assets
#   (textures, models, audio, prebuilt libraries) while leaving small intentional
#   binaries (e.g. the ~26 KB src/deterministic-sim/native/td_mxcsr.dll plugin)
#   alone.
#
# Enforces "large ⇒ LFS-designated", NOT "must already be an LFS pointer", so it
# does not force-migrate pre-existing small blobs and runs without git-lfs
# installed (it trusts the .gitattributes routing).
#
# TEXT files are held to a separate, higher ceiling and are NEVER asked to go to
# LFS. This project's append-only records — `spec-error-log.md`,
# `CHANGELOG.md`, `CHANGELOG-src.md`, `file-manifest.md` — grow past 1 MiB in
# the ordinary course of landing work, and `MatchEngine.cs` is over half a
# megabyte of source. Routing any of them to LFS would break every tool in
# `tools/` that greps them (`doc-consistency-check.py` reads them directly) and
# would destroy the line-level diff that makes an append-only chain auditable at
# all. The higher text ceiling still catches a genuinely runaway generated file.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

THRESHOLD_BYTES=${TD_BINARY_THRESHOLD_BYTES:-1048576}        # 1 MiB default
TEXT_THRESHOLD_BYTES=${TD_TEXT_THRESHOLD_BYTES:-4194304}     # 4 MiB default
fail=0
text_fail=0

# Git's own heuristic: a NUL byte in the first 8000 bytes means binary.
is_binary() {
  local head_bytes nul_stripped
  head_bytes=$(head -c 8000 -- "$1" | wc -c)
  nul_stripped=$(head -c 8000 -- "$1" | tr -d '\000' | wc -c)
  [ "$head_bytes" -ne "$nul_stripped" ]
}

while IFS= read -r f; do
  [ -f "$f" ] || continue
  size=$(wc -c < "$f")

  if ! is_binary "$f"; then
    # Text: LFS is the wrong home for it; only guard against runaway size.
    if [ "$size" -gt "$TEXT_THRESHOLD_BYTES" ]; then
      printf 'OVERSIZED TEXT FILE: %s (%s bytes)\n' "$f" "$size"
      text_fail=1
    fi
    continue
  fi

  [ "$size" -le "$THRESHOLD_BYTES" ] && continue
  filter=$(git check-attr filter -- "$f" | awk -F': ' '{print $NF}')
  if [ "$filter" != "lfs" ]; then
    printf 'LARGE NON-LFS FILE: %s (%s bytes) — filter=%s\n' "$f" "$size" "$filter"
    fail=1
  fi
done < <(git ls-files)

if [ "$fail" -ne 0 ]; then
  echo "::error::One or more large binary files (> $THRESHOLD_BYTES bytes) are not routed to Git LFS."
  echo "Add the extension to .gitattributes (filter=lfs diff=lfs merge=lfs -text) and 'git lfs track' it,"
  echo "or reduce the file. Override the threshold with TD_BINARY_THRESHOLD_BYTES if intentional."
fi
if [ "$text_fail" -ne 0 ]; then
  echo "::error::One or more text files exceed the text ceiling (> $TEXT_THRESHOLD_BYTES bytes)."
  echo "Do NOT route text to LFS — it breaks the grep-based tools in tools/ and the line-level diff."
  echo "Split the document instead, or override with TD_TEXT_THRESHOLD_BYTES if intentional."
fi
if [ "$fail" -ne 0 ] || [ "$text_fail" -ne 0 ]; then
  exit 1
fi

echo "Binary guard OK: no un-LFS'd binaries over $THRESHOLD_BYTES bytes,"
echo "no text files over $TEXT_THRESHOLD_BYTES bytes."
