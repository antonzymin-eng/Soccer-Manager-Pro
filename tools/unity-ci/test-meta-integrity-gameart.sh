#!/usr/bin/env bash
# tools/unity-ci/test-meta-integrity-gameart.sh
#
# AP-01 mutation proof for GameArt .meta enforcement and generator ownership.
# Uses a temporary Git index so tracked-path mutations never touch the caller's
# real staging area. Temporary GameArt working-tree fixtures are removed on exit.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

probe_root="Assets/GameArt"
probe_dir="$probe_root/__ap01_meta_integrity_probe__"
probe_asset="$probe_dir/probe.txt"
probe_root_meta="${probe_root}.meta"
probe_dir_meta="${probe_dir}.meta"
probe_asset_meta="${probe_asset}.meta"

tmpdir=$(mktemp -d)
tmp_index="$tmpdir/index"
created_gameart_dir=0
created_root_meta=0

cleanup() {
  unset GIT_INDEX_FILE || true
  rm -rf "$probe_dir"
  rm -f "$probe_dir_meta"
  if [ "$created_root_meta" = "1" ]; then
    rm -f "$probe_root_meta"
  fi
  if [ "$created_gameart_dir" = "1" ]; then
    rmdir "$probe_root" 2>/dev/null || true
  fi
  rm -rf "$tmpdir"
}
trap cleanup EXIT

if [ -e "$probe_dir" ] || [ -e "$probe_dir_meta" ]; then
  echo "::error::Probe path already exists: $probe_dir"
  exit 1
fi

cp "$(git rev-parse --git-path index)" "$tmp_index"
export GIT_INDEX_FILE="$tmp_index"

if [ ! -d "$probe_root" ]; then
  mkdir -p "$probe_root"
  created_gameart_dir=1
fi
mkdir -p "$probe_dir"
printf 'AP-01 meta integrity probe\n' > "$probe_asset"

# The production-like probe file is tracked first WITHOUT metas. The generator
# must identify exactly the two expected folder gaps while refusing to synthesize
# the file meta itself. Any unrelated generator-owned gap makes the proof fail
# closed before the write-mode helper can touch that path in the working tree.
git add -f "$probe_asset"
if [ ! -e "$probe_root_meta" ]; then
  created_root_meta=1
fi

set +e
generator_check=$(bash tools/unity-ci/generate-missing-metas.sh --check 2>&1)
generator_status=$?
set -e
if [ "$generator_status" -eq 0 ]; then
  echo "::error::Generator --check should report missing GameArt folder metas for the probe"
  exit 1
fi
if grep -Fq 'MISSING GENERATOR-OWNED META:' <<< "$generator_check"; then
  echo "::error::Pre-existing src/ generator-owned meta gap makes the standalone mutation proof unsafe"
  printf '%s\n' "$generator_check"
  exit 1
fi
gameart_gap_count=$(grep -Fc 'MISSING GAMEART FOLDER META:' <<< "$generator_check" || true)
if [ "$gameart_gap_count" -ne 2 ]; then
  echo "::error::Expected exactly two GameArt folder-meta gaps for the isolated probe, found $gameart_gap_count"
  printf '%s\n' "$generator_check"
  exit 1
fi
if ! grep -Fxq "MISSING GAMEART FOLDER META: $probe_root" <<< "$generator_check" \
   || ! grep -Fxq "MISSING GAMEART FOLDER META: $probe_dir" <<< "$generator_check"; then
  echo "::error::Generator did not report both expected GameArt folder-meta gaps"
  printf '%s\n' "$generator_check"
  exit 1
fi
if grep -Fq "$probe_asset_meta" <<< "$generator_check"; then
  echo "::error::Generator --check claimed ownership of a production GameArt file meta"
  printf '%s\n' "$generator_check"
  exit 1
fi

bash tools/unity-ci/generate-missing-metas.sh >/dev/null
if [ ! -e "$probe_root_meta" ] || [ ! -e "$probe_dir_meta" ]; then
  echo "::error::Generator failed to create required GameArt folder metas"
  exit 1
fi
if [ -e "$probe_asset_meta" ]; then
  echo "::error::Generator synthesized a production GameArt file meta"
  exit 1
fi
echo "PASS generator boundary: folder metas created; production file meta not synthesized"

# A minimal file meta is created only as a CI fixture so the checker can start
# from a clean staged state. Production art file metas are Unity-authored AP-03.
printf 'fileFormatVersion: 2\nguid: %s\n' \
  "$(printf '%s' "$probe_asset" | md5sum | cut -c1-32)" > "$probe_asset_meta"
cp "$probe_asset_meta" "$tmpdir/original-probe.meta"
git add -f "$probe_root_meta" "$probe_dir_meta" "$probe_asset_meta"

run_clean() {
  local label="$1"
  if ! output=$(bash tools/unity-ci/check-meta-integrity.sh 2>&1); then
    echo "::error::$label expected clean status but checker failed"
    printf '%s\n' "$output"
    exit 1
  fi
  echo "PASS clean: $label"
}

expect_fail() {
  local label="$1" expected="$2"
  set +e
  output=$(bash tools/unity-ci/check-meta-integrity.sh 2>&1)
  status=$?
  set -e
  if [ "$status" -eq 0 ]; then
    echo "::error::$label expected checker failure but it passed"
    exit 1
  fi
  if ! grep -Fq "$expected" <<< "$output"; then
    echo "::error::$label failed, but expected evidence was absent: $expected"
    printf '%s\n' "$output"
    exit 1
  fi
  echo "PASS mutation: $label"
}

find_meta_with_guid() {
  local regex="$1" exclude_regex="${2:-^$}"
  local m g
  while IFS= read -r m; do
    [ -z "$m" ] && continue
    [[ "$m" =~ $exclude_regex ]] && continue
    g=$(awk '/^guid: [0-9a-f]{32}$/ {print $2; exit}' "$m")
    if [ -n "$g" ]; then
      printf '%s|%s\n' "$m" "$g"
      return 0
    fi
  done < <(git ls-files | grep -E "$regex" || true)
  return 1
}

set_probe_guid() {
  local guid="$1"
  awk -v guid="$guid" '/^guid: / {$0="guid: " guid} {print}' \
    "$tmpdir/original-probe.meta" > "$probe_asset_meta"
}

run_clean "baseline with staged GameArt fixture"

# Mutation 1: tracked GameArt file without its meta.
mv "$probe_asset_meta" "$tmpdir/missing-probe.meta"
git rm --cached -q "$probe_asset_meta"
expect_fail "missing GameArt meta" "MISSING META: $probe_asset"
mv "$tmpdir/missing-probe.meta" "$probe_asset_meta"
git add -f "$probe_asset_meta"
run_clean "after restoring missing-meta mutation"

# Mutation 2: tracked GameArt meta whose asset is gone.
mv "$probe_asset" "$tmpdir/orphan-probe.txt"
git rm --cached -q "$probe_asset"
expect_fail "orphan GameArt meta" "ORPHAN META (asset missing): $probe_asset_meta"
mv "$tmpdir/orphan-probe.txt" "$probe_asset"
git add -f "$probe_asset"
run_clean "after restoring orphan mutation"

# Mutation 3: GameArt GUID collides with junction-backed src/.
src_pair=$(find_meta_with_guid '^src/.*\.meta$') || {
  echo "::error::No tracked src/ meta with a GUID found for cross-tree mutation"
  exit 1
}
src_meta=${src_pair%%|*}
src_guid=${src_pair#*|}
set_probe_guid "$src_guid"
set +e
output=$(bash tools/unity-ci/check-meta-integrity.sh 2>&1)
status=$?
set -e
if [ "$status" -eq 0 ] || ! grep -Fq "$src_meta" <<< "$output" || ! grep -Fq "$probe_asset_meta" <<< "$output"; then
  echo "::error::src↔GameArt duplicate mutation was not proven across both paths"
  printf '%s\n' "$output"
  exit 1
fi
echo "PASS mutation: GameArt GUID collision with src/"
cp "$tmpdir/original-probe.meta" "$probe_asset_meta"
run_clean "after restoring src duplicate mutation"

# Mutation 4: GameArt GUID collides with another tracked Assets/ meta.
assets_pair=$(find_meta_with_guid '^Assets/.*\.meta$' '^Assets/GameArt(/|\.meta$)') || {
  echo "::error::No tracked non-GameArt Assets/ meta with a GUID found for project-wide mutation"
  exit 1
}
assets_meta=${assets_pair%%|*}
assets_guid=${assets_pair#*|}
set_probe_guid "$assets_guid"
set +e
output=$(bash tools/unity-ci/check-meta-integrity.sh 2>&1)
status=$?
set -e
if [ "$status" -eq 0 ] || ! grep -Fq "$assets_meta" <<< "$output" || ! grep -Fq "$probe_asset_meta" <<< "$output"; then
  echo "::error::Assets↔GameArt duplicate mutation was not proven across both paths"
  printf '%s\n' "$output"
  exit 1
fi
echo "PASS mutation: GameArt GUID collision with other Assets/"
cp "$tmpdir/original-probe.meta" "$probe_asset_meta"
run_clean "after restoring Assets duplicate mutation"

echo "AP-01 GameArt meta proof PASS: generator boundary, missing, orphan, src collision, and project Assets collision all proved and restored cleanly."
