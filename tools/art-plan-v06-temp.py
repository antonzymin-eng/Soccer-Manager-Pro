from pathlib import Path

plan = Path('docs/planning/art-pipeline-foundation.md')
text = plan.read_text(encoding='utf-8')
old_header = '**Last Updated:** September 5, 2026  \n**Version:** 0.5  '
new_header = '**Last Updated:** September 6, 2026  \n**Version:** 0.6  '
assert old_header in text
text = text.replace(old_header, new_header, 1)

old = '''Every tracked file/folder created under `Assets/GameArt/` must receive the appropriate committed Unity `.meta` identity from the moment it is introduced. **Before the first GameArt path is created, AP-01 extends both `tools/unity-ci/check-meta-integrity.sh` and `tools/unity-ci/generate-missing-metas.sh` so their tracked-path universe includes `Assets/GameArt/` as well as `src/`, and proves the check path fails on a missing GameArt `.meta`.** No GameArt path may be committed in the interval between directory creation and that scope change.'''
new = '''Every tracked file/folder created under `Assets/GameArt/` must receive the appropriate committed Unity `.meta` identity from the moment it is introduced. **Before the first GameArt path is created, AP-01 extends `tools/unity-ci/check-meta-integrity.sh` across all three of its current `src/`-scoped enumerations: (1) the missing-meta/ancestor walk, (2) the orphan-meta scan, and (3) duplicate-GUID detection. Missing/orphan coverage must include both `src/` and `Assets/GameArt/`; duplicate-GUID detection must be one combined cross-tree scan so a GUID collision between the two roots cannot pass.** `tools/unity-ci/generate-missing-metas.sh` may be expanded only for GameArt **folders and CI-safety fixtures**; it must not synthesize production `.meta` files for art assets such as textures, vector exports, or font binaries. Those asset metas come from an actual Unity import under AP-03. AP-01 proves the checker fails on a missing GameArt `.meta` before any real GameArt path is committed.'''
assert old in text
text = text.replace(old, new, 1)

old = '''The repo already routes common textures, 3D formats, audio, video, and fonts through Git LFS and has a whole-repository large-binary guard. The existing `.meta` integrity checker and generator currently enumerate only `src/`; **AP-01 extends both to `Assets/GameArt/` before creating any tracked GameArt path.** AP-06 may harden that baseline from H2 evidence, but it is not the first line of defense.'''
new = '''The repo already routes common textures, 3D formats, audio, video, and fonts through Git LFS and has a whole-repository large-binary guard. The existing `.meta` checker currently scopes its missing-meta walk, orphan scan, and duplicate-GUID scan to `src/`; **AP-01 extends all three, with duplicate-GUID detection performed once across the combined `src/` + `Assets/GameArt/` universe.** The generator is expanded only for GameArt folders/CI safety, not production art-asset metas; actual asset importer blocks are created by Unity during AP-03. AP-06 may harden that baseline from H2 evidence, but it is not the first line of defense.'''
assert old in text
text = text.replace(old, new, 1)

old = '''1. **Extend meta scope first.** Update `tools/unity-ci/check-meta-integrity.sh` and `tools/unity-ci/generate-missing-metas.sh` so their tracked-path universe covers both `src/` and `Assets/GameArt/`; keep `src/`'s existing root exception and define the corresponding GameArt root/folder behavior explicitly.
2. **Prove the guard.** Before creating the real GameArt tree, use a temporary tracked-path fixture/mutation to prove the check fails on a missing GameArt `.meta` and returns clean after the meta is present; do not rely on the script merely being edited.
3. **Then create repository roots.** Create the exact `art-source/` and `Assets/GameArt/` roots/subtrees only as needed, with required Unity folder/file `.meta` identities committed from first introduction.
4. Add the authoritative `.art.json` schema/example.
5. Document allowed/forbidden runtime formats.
6. Add local scratch ignore only if actual tools need it.

**No production styling.** This is infrastructure only. **No tracked `Assets/GameArt/` path may predate steps 1–2.**'''
new = '''1. **Extend all checker enumerations first.** Update `tools/unity-ci/check-meta-integrity.sh` so (a) the missing-meta/ancestor walk and (b) orphan-meta scan cover both `src/` and `Assets/GameArt/`, and (c) duplicate-GUID detection is one **cross-tree** scan over both roots. Keep `src/`'s existing root exception and define the corresponding GameArt root/folder behavior explicitly.
2. **Constrain the generator.** Extend `tools/unity-ci/generate-missing-metas.sh` only for GameArt folders and CI-safety fixtures. It must not seed production art-asset metas: textures, vectors, fonts, and other imported art assets receive their importer-bearing `.meta` files from an actual Unity import in AP-03.
3. **Prove the guard.** Before creating the real GameArt tree, use temporary tracked-path fixtures/mutations to prove the checker fails on a missing GameArt `.meta`, catches an orphan GameArt `.meta`, and catches a duplicate GUID spanning `src/` and `Assets/GameArt/`; restore each fixture and prove clean status.
4. **Then create repository roots.** Create the exact `art-source/` and `Assets/GameArt/` roots/subtrees only as needed, with required Unity **folder** `.meta` identities committed from first introduction. Do not introduce production art files until their Unity-import path is available under AP-03.
5. Add the authoritative `.art.json` schema/example.
6. Document allowed/forbidden runtime formats.
7. Add local scratch ignore only if actual tools need it.

**No production styling.** This is infrastructure only. **No tracked `Assets/GameArt/` path may predate steps 1–3.**'''
assert old in text
text = text.replace(old, new, 1)

old = '- AP-01 orders `check-meta-integrity.sh` + `generate-missing-metas.sh` GameArt scope extension and a firing proof **before** the first tracked `Assets/GameArt/` path;'
new = '- AP-01 names all three `check-meta-integrity.sh` enumerations, requires duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan, restricts `generate-missing-metas.sh` to folders/CI safety, and requires firing proofs **before** the first tracked `Assets/GameArt/` path;'
assert old in text
text = text.replace(old, new, 1)

old = '| Broken Unity references | stable paths/GUIDs + AP-01 GameArt meta generator/check scope and mutation proof before first tracked GameArt path + AP-06 evidence-driven hardening |'
new = '| Broken Unity references | stable paths/GUIDs + AP-01 missing/orphan coverage + one cross-tree duplicate-GUID scan + folder-only generator scope + mutation proofs before first tracked GameArt path + AP-03 Unity-generated asset metas + AP-06 evidence-driven hardening |'
assert old in text
text = text.replace(old, new, 1)

history = '| 0.5 | 2026-09-05 | External follow-up: moved GameArt `.meta` checker/generator scope expansion from AP-06 to the **first action of AP-01**, before any tracked GameArt path; required a mutation/firing proof; retained only evidence-driven hardening in AP-06; removed the `where practical` hedge from integrated-asset identity preservation and routed legitimate move/rename exceptions through §6.4. |'
assert history in text
text = text.replace(history, history + '\n| 0.6 | 2026-09-06 | AP-01 execution refinement after external review: named all three checker enumerations; required duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan; restricted `generate-missing-metas.sh` to folders/CI safety rather than production art assets; assigned importer-bearing art-asset metas to actual Unity import in AP-03; expanded mutation proof to missing, orphan, and cross-tree duplicate cases. |', 1)
plan.write_text(text, encoding='utf-8')

manifest = Path('docs/tracking/file-manifest.md')
text = manifest.read_text(encoding='utf-8')
title = '# File Manifest (Post-Migration Baseline)\n\n'
current = title + '**Last Updated:**'
assert text.startswith(current)
text = text.replace(current, title + '**Last Updated (prior):**', 1)
entry = (
    '**Last Updated:** September 6, 2026 — **Art-pipeline plan v0.6 execution details tightened after external review; documentation only; G0 remains CLOSED pending owner acceptance.**\n'
    'Modified: `docs/planning/art-pipeline-foundation.md` **v0.5 → v0.6**, `docs/tracking/CHANGELOG.md`, and this manifest. AP-01 now names all three `check-meta-integrity.sh` enumerations (missing-meta/ancestor walk, orphan scan, duplicate-GUID scan), requires duplicate detection as one cross-tree `src/` + `Assets/GameArt/` scan, limits `generate-missing-metas.sh` expansion to GameArt folders/CI-safety fixtures, and assigns production art-asset importer metas to real Unity import in AP-03. The firing proof now covers missing, orphan, and cross-tree duplicate cases before any tracked GameArt path is created. Final pre-refinement CI run `34007813158` reproduced only the documented owner-held `sim_match_engine_close_chance` band (`MatchEngine.Tests`: 472 passed / 1 failed / 11 skipped; `SeasonSave.Tests`: 447 passed / 0 failed / 3 skipped); all other completed checks passed. No `src/`, `.cs`, `.asmdef`, workflow contract, runtime schema, RNG, draw order, `SPEC_INDEX.md`, or spec status changed.\n'
)
text = text.replace(title, title + entry, 1)
manifest.write_text(text, encoding='utf-8')

changelog = Path('docs/tracking/CHANGELOG.md')
text = changelog.read_text(encoding='utf-8')
divider = '---\n\n'
current = divider + '> **Last Updated:**'
assert current in text
text = text.replace(current, divider + '> **Last Updated (prior):**', 1)
entry_lines = [
    '> **Last Updated:** September 6, 2026 — **Art-pipeline plan v0.6 execution details tightened after external review; documentation only; G0 remains CLOSED pending explicit owner acceptance.**',
    '> `docs/planning/art-pipeline-foundation.md` advances **v0.5 → v0.6** without changing the high-level pipeline or opening implementation. AP-01 now names the three separate `src/`-scoped enumerations in `check-meta-integrity.sh`: the missing-meta/ancestor walk, orphan-meta scan, and duplicate-GUID scan. Missing/orphan coverage must span `src/` plus `Assets/GameArt/`; duplicate detection must be **one combined cross-tree scan**, so a GameArt GUID collision with an existing `src/` meta cannot escape two per-root checks.',
    '> `generate-missing-metas.sh` is now explicitly limited to GameArt **folders and CI-safety fixtures**. It may not synthesize production art-asset metas because its minimal stub lacks Unity importer blocks; textures, vectors, fonts, and other imported art assets receive importer-bearing `.meta` files from actual Unity import in AP-03. AP-01\'s mutation proof is expanded to missing-meta, orphan-meta, and cross-tree duplicate-GUID cases before any tracked GameArt path exists.',
    '> Final pre-refinement CI run `34007813158` reproduced only the documented owner-held `sim_match_engine_close_chance` RED band: `MatchEngine.Tests` **472 passed / 1 failed / 11 skipped**, `SeasonSave.Tests` **447 passed / 0 failed / 3 skipped**. All other completed checks passed; the PR has no `.cs` change and makes no runtime repair or rebaseline.',
    '> **Determinism/runtime declaration:** no snapshot/serialization schema change; no RNG stream/domain/draw-site/draw-order change; no runtime behavior, assembly graph, workflow contract, required status, `SPEC_INDEX.md` row, or spec status changed. G0 remains CLOSED until the owner explicitly accepts the refined plan.',
    '>',
    '',
]
text = text.replace(divider, divider + '\n'.join(entry_lines), 1)
changelog.write_text(text, encoding='utf-8')
