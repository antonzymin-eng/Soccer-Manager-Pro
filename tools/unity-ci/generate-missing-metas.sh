#!/usr/bin/env bash
# tools/unity-ci/generate-missing-metas.sh
#
# Created: July 22, 2026
# Purpose: Write CI-safe .meta files for generator-owned paths without requiring
#          a Unity editor run.
#
# Ownership boundary:
#   - src/: preserve the existing behavior — tracked source files and folders may
#           receive deterministic placeholder metas.
#   - Assets/GameArt/: generate FOLDER metas only. Production art files (texture,
#           vector, font, etc.) must receive importer-bearing metas from an actual
#           Unity import; this helper must never synthesize those file metas.
#
# WHY DETERMINISTIC GUIDs: a path without a committed .meta gets a fresh random
# GUID from Unity on checkout. For helper-owned paths, md5(repo-relative-path)
# provides a stable, reproducible 32-hex Unity GUID without opening Unity.
#
# Usage:  bash tools/unity-ci/generate-missing-metas.sh          # write eligible missing metas
#         bash tools/unity-ci/generate-missing-metas.sh --check  # report eligible gaps only
#
# Full integrity is enforced by check-meta-integrity.sh; --check here does NOT
# claim that production GameArt files have valid importer metas.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

CHECK_ONLY=0
[ "${1:-}" = "--check" ] && CHECK_ONLY=1

guid_for() { printf '%s' "$1" | md5sum | cut -c1-32; }

write_folder_meta() {
  local path="$1" guid="$2"
  {
    printf 'fileFormatVersion: 2\n'
    printf 'guid: %s\n' "$guid"
    printf 'folderAsset: yes\n'
    printf 'DefaultImporter:\n'
    printf '  externalObjects: {}\n'
    printf '  userData: \n'
    printf '  assetBundleName: \n'
    printf '  assetBundleVariant: \n'
  } > "$path.meta"
}

write_src_meta() {
  local path="$1" guid="$2"
  if [ -d "$path" ]; then
    write_folder_meta "$path" "$guid"
  elif [[ "$path" == *.asmdef ]]; then
    {
      printf 'fileFormatVersion: 2\n'
      printf 'guid: %s\n' "$guid"
      printf 'AssemblyDefinitionImporter:\n'
      printf '  externalObjects: {}\n'
      printf '  userData: \n'
      printf '  assetBundleName: \n'
      printf '  assetBundleVariant: \n'
    } > "$path.meta"
  else
    # Existing src/ convention for .cs and other source files.
    printf 'fileFormatVersion: 2\nguid: %s' "$guid" > "$path.meta"
  fi
}

# Existing src/ behavior: every tracked non-meta file plus every ancestor folder
# below the src/ root is generator-owned.
enumerate_src_paths() {
  local files
  files=$(git ls-files src/ | grep -v '\.meta$' || true)
  {
    printf '%s\n' "$files"
    printf '%s\n' "$files" | while IFS= read -r f; do
      [ -z "$f" ] && continue
      d=$(dirname "$f")
      while [ "$d" != "." ] && [ "$d" != "src" ]; do
        echo "$d"
        d=$(dirname "$d")
      done
    done
  } | sed '/^$/d' | sort -u
}

# GameArt behavior: derive only ancestor FOLDERS from tracked production paths.
# The Assets/GameArt root is included when GameArt has tracked content. File
# metas are deliberately excluded and must come from Unity import.
enumerate_gameart_dirs() {
  local files
  files=$(git ls-files Assets/GameArt/ | grep -v '\.meta$' || true)
  printf '%s\n' "$files" | while IFS= read -r f; do
    [ -z "$f" ] && continue
    d=$(dirname "$f")
    while [ "$d" != "." ] && [ "$d" != "Assets" ]; do
      echo "$d"
      [ "$d" = "Assets/GameArt" ] && break
      d=$(dirname "$d")
    done
  done | sort -u
}

missing=0

while IFS= read -r path; do
  [ -z "$path" ] && continue
  [ -e "$path.meta" ] && continue
  missing=$((missing + 1))
  if [ "$CHECK_ONLY" = "1" ]; then
    echo "MISSING GENERATOR-OWNED META: $path"
    continue
  fi
  guid=$(guid_for "$path")
  write_src_meta "$path" "$guid"
  echo "wrote $path.meta ($guid)"
done < <(enumerate_src_paths)

while IFS= read -r path; do
  [ -z "$path" ] && continue
  [ -e "$path.meta" ] && continue
  missing=$((missing + 1))
  if [ "$CHECK_ONLY" = "1" ]; then
    echo "MISSING GAMEART FOLDER META: $path"
    continue
  fi
  guid=$(guid_for "$path")
  write_folder_meta "$path" "$guid"
  echo "wrote $path.meta ($guid) [folder only]"
done < <(enumerate_gameart_dirs)

if [ "$CHECK_ONLY" = "1" ]; then
  if [ "$missing" -eq 0 ]; then
    echo "All generator-owned src/ paths and GameArt folders have .meta files."
    exit 0
  fi
  echo "::error::$missing generator-owned path(s) are missing .meta files."
  exit 1
fi

echo "Done. $missing generator-owned meta(s) written. Production GameArt file metas remain Unity-authored."
