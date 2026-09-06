from pathlib import Path

root = Path('work')

manifest = root / 'docs/tracking/file-manifest.md'
text = manifest.read_text(encoding='utf-8')
title = '# File Manifest (Post-Migration Baseline)\n\n'
current = title + '**Last Updated:**'
assert text.startswith(current)
text = text.replace(current, title + '**Last Updated (prior):**', 1)
entry = (
    '**Last Updated:** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production GameArt tree or asset landed.**\n'
    'New: `art-source/README.md`, `art-source/_templates/example.art.json`, `docs/design/art/art-asset.schema.json`, and `tools/unity-ci/test-meta-integrity-gameart.sh`. Modified: `tools/unity-ci/check-meta-integrity.sh`, `tools/unity-ci/generate-missing-metas.sh`, `tools/unity-ci/README.md`, `docs/tracking/CHANGELOG.md`, and this manifest. Missing/orphan `.meta` enforcement now covers the managed `src/` + `Assets/GameArt/` roots, while duplicate-GUID detection is one project-wide scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree. The generator retains its existing `src/` behavior but is folder-only for GameArt; production art-file metas remain Unity-authored in AP-03. The dedicated mutation proof uses a temporary Git index and proves the generator boundary, missing GameArt meta, orphan GameArt meta, GameArt↔`src/` GUID collision, and GameArt↔other-`Assets/` GUID collision, restoring to clean after every mutation. Temporary proof run `34057630107` completed green after the exact-assertion review fix, including shell syntax, clean gate, generator-owned-path check, mutation proof, no-premature-GameArt-tree assertion, metadata contract parse, binary/LFS guard, and diff-whitespace check. `Assets/GameArt/` and `Assets/GameArt.meta` are absent from the landed AP-01 tree by design; AP-03 owns the first real Unity import. No production asset, C# runtime behavior, simulation/save schema, RNG stream/domain/draw site/draw order, `SPEC_INDEX.md` row, or spec status changed.\n'
)
text = text.replace(title, title + entry, 1)

marker = '## Design References\n'
assert marker in text
section = '''## Art Pipeline Infrastructure\n\nOperational AP-01 surfaces. These are pipeline/tooling contracts, not production art assets.\n\n| File | Purpose |\n|------|---------|\n| `art-source/README.md` | Editable-source/runtime-export contract, rights/revision policy, allowed/forbidden runtime-format boundary, and Unity `.meta` ownership split |\n| `art-source/_templates/example.art.json` | Non-production example of the authoritative adjacent `.art.json` production-candidate record |\n| `docs/design/art/art-asset.schema.json` | Draft 2020-12 machine schema for initial art-candidate identity, lifecycle, provenance/rights, target/export, and generated-source context |\n| `tools/unity-ci/check-meta-integrity.sh` | Missing/orphan enforcement for managed `src/` + `Assets/GameArt/`; one project-wide duplicate-GUID scan across tracked `Assets/**/*.meta` + `src/**/*.meta` |\n| `tools/unity-ci/generate-missing-metas.sh` | Deterministic helper preserving `src/` behavior while limiting GameArt generation to folder metas/CI fixtures; production art file metas remain Unity-authored |\n| `tools/unity-ci/test-meta-integrity-gameart.sh` | Temporary-index mutation proof for generator boundary, missing/orphan GameArt metas, and GameArt GUID collisions with both `src/` and other tracked `Assets/` metas |\n| `tools/unity-ci/README.md` | Human contract and local commands for the AP-01 Unity asset-integrity tooling |\n\n---\n\n'''
assert '## Art Pipeline Infrastructure\n' not in text
text = text.replace(marker, section + marker, 1)
manifest.write_text(text, encoding='utf-8')

changelog = root / 'docs/tracking/CHANGELOG.md'
text = changelog.read_text(encoding='utf-8')
divider = '---\n\n'
current = divider + '> **Last Updated:**'
assert current in text
text = text.replace(current, divider + '> **Last Updated (prior):**', 1)
lines = [
    '> **Last Updated:** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production art landed.**',
    '> **G0 execution began from the post-#360 main commit `9414b0b6`.** AP-01 adds the editable-source contract (`art-source/README.md`), authoritative initial `.art.json` schema/example, and the executable Unity `.meta` integrity boundary without creating a persistent `Assets/GameArt/` tree.',
    '> **Meta integrity is now explicit and executable.** Missing-meta/ancestor and orphan-meta checks cover the managed `src/` + `Assets/GameArt/` roots. Duplicate GUIDs are checked once across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree, matching Unity\'s project-wide GUID identity. `generate-missing-metas.sh` preserves its `src/` behavior but may create only GameArt folder metas/CI fixtures; real texture/vector/font/other production file metas are reserved for actual Unity import in AP-03.',
    '> **Mutation evidence is positive, negative, and restored-clean.** `tools/unity-ci/test-meta-integrity-gameart.sh` uses a temporary Git index and proves: generator creates both required GameArt folder metas but not the production-like file meta; missing GameArt meta fails; orphan GameArt meta fails; GameArt↔`src/` duplicate GUID fails; GameArt↔another tracked `Assets/` duplicate GUID fails; each mutation restores to a clean checker result. Temporary proof run `34057630107` is green after the exact-match assertion refinement, including shell syntax, baseline/generator checks, mutation proof, no-premature-GameArt-tree assertion, metadata parse, LFS guard, and diff-whitespace check.',
    '> **Repository shape remains deliberately sparse.** AP-01 creates only `art-source/_templates/` because it has an immediate consumer. It does not reserve the full source tree and leaves `Assets/GameArt/` absent; AP-03 owns the first real source→export→Unity import and importer-bearing production file meta.',
    '> **Determinism/runtime declaration:** no production art asset, C# runtime behavior, snapshot/serialization schema, RNG stream/domain/draw site/draw order, assembly graph, `SPEC_INDEX.md` row, or spec status changed. The known owner-held match-engine RED is unrelated to this slice and is not rebaselined here.',
    '>',
    '',
]
text = text.replace(divider, divider + '\n'.join(lines), 1)
changelog.write_text(text, encoding='utf-8')
