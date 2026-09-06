from pathlib import Path

root = Path('work')

manifest = root / 'docs/tracking/file-manifest.md'
text = manifest.read_text(encoding='utf-8')
old = 'Modified: `tools/unity-ci/check-meta-integrity.sh`, `tools/unity-ci/generate-missing-metas.sh`, `tools/unity-ci/README.md`, `docs/tracking/CHANGELOG.md`, and this manifest.'
new = 'Modified: `tools/unity-ci/check-meta-integrity.sh`, `tools/unity-ci/generate-missing-metas.sh`, `tools/unity-ci/README.md`, `.github/workflows/ci.yml` (comment-only contract description), `docs/tracking/CHANGELOG.md`, and this manifest.'
assert old in text
text = text.replace(old, new, 1)
needle = 'No production asset, C# runtime behavior, simulation/save schema, RNG stream/domain/draw site/draw order, `SPEC_INDEX.md` row, or spec status changed.'
replacement = 'The `Unity .meta integrity` workflow job behavior/name is unchanged; only its stale `src/`-only explanatory comment is synchronized to the AP-01 managed-root/project-wide-GUID contract. No production asset, C# runtime behavior, simulation/save schema, RNG stream/domain/draw site/draw order, `SPEC_INDEX.md` row, or spec status changed.'
assert needle in text
text = text.replace(needle, replacement, 1)
manifest.write_text(text, encoding='utf-8')

changelog = root / 'docs/tracking/CHANGELOG.md'
text = changelog.read_text(encoding='utf-8')
anchor = '> **Repository shape remains deliberately sparse.** AP-01 creates only `art-source/_templates/` because it has an immediate consumer. It does not reserve the full source tree and leaves `Assets/GameArt/` absent; AP-03 owns the first real source→export→Unity import and importer-bearing production file meta.\n'
addition = anchor + '> **CI description synchronized:** `.github/workflows/ci.yml` changes only the explanatory comment above the existing `Unity .meta integrity` step so it describes managed `src/` + `Assets/GameArt/` missing/orphan enforcement and project-wide tracked `Assets/` + `src/` duplicate-GUID enforcement. The job name, trigger, command, and behavior are unchanged.\n'
assert anchor in text
text = text.replace(anchor, addition, 1)
changelog.write_text(text, encoding='utf-8')
