#!/usr/bin/env bash
# tools/unity-ci/generate-missing-metas.sh
#
# Created: July 22, 2026
# Purpose: Write CI-safe .meta files for any git-tracked file/folder under src/
#          that is missing one, so the tree stays GUID-consistent across
#          checkouts WITHOUT requiring a Unity editor run.
#
# WHY DETERMINISTIC GUIDs: a .cs without a committed .meta gets a fresh random
# GUID from Unity on every checkout, silently breaking every asset/prefab/scene
# reference to it and diverging GUIDs between machines. Here each GUID is
# md5(repo-relative-path) — stable, unique per path, valid 32-hex Unity form,
# and reproducible by anyone without opening Unity.
#
# IMPORTANT — these are PLACEHOLDER metas. They are format-correct and
# reference-stable, but they were NOT authored by the Unity editor (no
# MonoImporter/importer-settings block beyond the minimal repo convention).
# When the project is next opened in Unity 6 on the pinned host, Unity may
# enrich these metas; the GUIDs must be preserved (do not let Unity reassign
# them). See tools/unity-ci/README.md.
#
# Usage:  bash tools/unity-ci/generate-missing-metas.sh          # write missing metas
#         bash tools/unity-ci/generate-missing-metas.sh --check  # list only, exit 1 if any missing
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

CHECK_ONLY=0
[ "${1:-}" = "--check" ] && CHECK_ONLY=1

guid_for() { printf '%s' "$1" | md5sum | cut -c1-32; }

# Enumerate every tracked file plus every ancestor directory under src/
# (excluding the src/ root). Ancestors are walked so a directory holding only
# subdirectories still gets a folder .meta.
enumerate_paths() {
  local files
  files=$(git ls-files src/ | grep -v '\.meta$')
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

# Every tracked file/dir under src/ needs a .meta EXCEPT the src/ root itself
# (the asset-mount root, which by Unity convention carries no .meta).
missing=0
while IFS= read -r path; do
  [ "$path" = "src" ] && continue
  [ -e "$path.meta" ] && continue
  missing=$((missing + 1))
  if [ "$CHECK_ONLY" = "1" ]; then
    echo "MISSING META: $path"
    continue
  fi

  guid=$(guid_for "$path")
  if [ -d "$path" ]; then
    # Folder asset
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
    # .cs (and any other script) — minimal repo convention, no trailing newline
    printf 'fileFormatVersion: 2\nguid: %s' "$guid" > "$path.meta"
  fi
  echo "wrote $path.meta ($guid)"
done < <(enumerate_paths)

if [ "$CHECK_ONLY" = "1" ]; then
  [ "$missing" -eq 0 ] && { echo "All tracked src/ files and folders have .meta files."; exit 0; }
  echo "::error::$missing tracked path(s) under src/ are missing a .meta file."
  exit 1
fi
echo "Done. $missing meta(s) written."
