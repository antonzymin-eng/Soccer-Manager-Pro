from pathlib import Path

ROOT = Path('work')
plan = ROOT / 'docs/planning/art-pipeline-foundation.md'
text = plan.read_text(encoding='utf-8')
text = text.replace('**Version:** 0.7  ', '**Version:** 0.8  ', 1)

old = "Missing/orphan coverage must include both `src/` and `Assets/GameArt/`; duplicate-GUID detection must be one combined cross-tree scan so a GUID collision between the two roots cannot pass."
new = "Missing/orphan coverage must include both `src/` and `Assets/GameArt/`; duplicate-GUID detection must be one combined project-wide Unity-asset scan across all tracked `.meta` files under `Assets/` plus the junction-backed `src/` tree, so a GameArt GUID collision with any existing Unity asset cannot pass."
assert old in text
text = text.replace(old, new, 1)

old = "The existing `.meta` checker currently scopes its missing-meta walk, orphan scan, and duplicate-GUID scan to `src/`; **AP-01 extends all three, with duplicate-GUID detection performed once across the combined `src/` + `Assets/GameArt/` universe.**"
new = "The existing `.meta` checker currently scopes its missing-meta walk, orphan scan, and duplicate-GUID scan to `src/`; **AP-01 extends missing/orphan coverage to `Assets/GameArt/`, while duplicate-GUID detection becomes one combined scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree.**"
assert old in text
text = text.replace(old, new, 1)

old = "1. **Extend all checker enumerations first.** Update `tools/unity-ci/check-meta-integrity.sh` so (a) the missing-meta/ancestor walk and (b) orphan-meta scan cover both `src/` and `Assets/GameArt/`, and (c) duplicate-GUID detection is one **cross-tree** scan over both roots. Keep `src/`'s existing root exception and define the corresponding GameArt root/folder behavior explicitly."
new = "1. **Extend all checker enumerations first.** Update `tools/unity-ci/check-meta-integrity.sh` so (a) the missing-meta/ancestor walk and (b) orphan-meta scan cover both managed roots, `src/` and `Assets/GameArt/`, while (c) duplicate-GUID detection is one **project-wide Unity-asset scan** across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree. Keep `src/`'s existing root exception and define the corresponding GameArt root/folder behavior explicitly. Duplicate detection must not be split per root: a GameArt GUID colliding with `Assets/README.md.meta`, a future scene/plugin meta, or any other tracked Unity asset must fail."
assert old in text
text = text.replace(old, new, 1)

old = "3. **Prove the guard.** Before creating the real GameArt tree, use temporary tracked-path fixtures/mutations to prove the checker fails on a missing GameArt `.meta`, catches an orphan GameArt `.meta`, and catches a duplicate GUID spanning `src/` and `Assets/GameArt/`; restore each fixture and prove clean status."
new = "3. **Prove the guard.** Before creating the real GameArt tree, use temporary tracked-path fixtures/mutations to prove the checker fails on a missing GameArt `.meta`, catches an orphan GameArt `.meta`, and catches duplicate GUIDs between GameArt and both (i) `src/` and (ii) another tracked `Assets/` asset; restore each fixture and prove clean status."
assert old in text
text = text.replace(old, new, 1)

old = "- AP-01 names all three `check-meta-integrity.sh` enumerations, requires duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan, restricts `generate-missing-metas.sh` to folders/CI safety, and requires firing proofs **before** the first tracked `Assets/GameArt/` path;"
new = "- AP-01 names all three `check-meta-integrity.sh` enumerations, requires missing/orphan coverage for `src/` + `Assets/GameArt/` and duplicate-GUID detection as one project-wide scan across tracked `Assets/**/*.meta` plus `src/**/*.meta`, restricts `generate-missing-metas.sh` to folders/CI safety, and requires firing proofs **before** the first tracked `Assets/GameArt/` path;"
assert old in text
text = text.replace(old, new, 1)

old = "| Broken Unity references | stable paths/GUIDs + AP-01 missing/orphan coverage + one cross-tree duplicate-GUID scan + folder-only generator scope + mutation proofs before first tracked GameArt path + AP-03 Unity-generated asset metas + AP-06 evidence-driven hardening |"
new = "| Broken Unity references | stable paths/GUIDs + AP-01 managed-root missing/orphan coverage + one project-wide tracked-Unity-meta duplicate-GUID scan across `Assets/` + `src/` + folder-only generator scope + mutation proofs before first tracked GameArt path + AP-03 Unity-generated asset metas + AP-06 evidence-driven hardening |"
assert old in text
text = text.replace(old, new, 1)

history = '| 0.7 | 2026-09-06 | **G0 accepted by owner.** Planning gate closed successfully and AP-01 repository-contract implementation authorized. No bulk asset generation or later family-scale gate was opened. |'
assert history in text
text = text.replace(history, history + '\n| 0.8 | 2026-09-06 | Post-acceptance Codex review correction before landing: preserved managed-root missing/orphan scope but widened duplicate-GUID detection to one project-wide scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree; expanded mutation proof to include a GameArt collision with another `Assets/` meta. G0 remains accepted and AP-01 authorization unchanged. |', 1)
plan.write_text(text, encoding='utf-8')

manifest = ROOT / 'docs/tracking/file-manifest.md'
text = manifest.read_text(encoding='utf-8')
title = '# File Manifest (Post-Migration Baseline)\n\n'
assert text.startswith(title + '**Last Updated:**')
text = text.replace(title + '**Last Updated:**', title + '**Last Updated (prior):**', 1)
entry = ('**Last Updated:** September 6, 2026 — **Art-pipeline plan v0.8 closes post-acceptance Codex GUID-scope finding; G0 remains accepted and AP-01 authorized.**\n'
         'Modified: `docs/planning/art-pipeline-foundation.md` **v0.7 → v0.8**, `docs/tracking/CHANGELOG.md`, and this manifest. A pre-merge Codex review found that duplicate-GUID detection limited to `src/` + `Assets/GameArt/` would miss collisions with other tracked Unity assets already under `Assets/` (for example `Assets/README.md.meta`) and future scenes/plugins. AP-01 now keeps missing/orphan enforcement on the managed `src/` + `Assets/GameArt/` roots but requires duplicate-GUID detection as one combined scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree, with mutation proof against both an existing `src/` meta and another `Assets/` meta. G0 acceptance and AP-01-only authorization are unchanged. No runtime, production asset, RNG, draw-order, workflow, or spec-status change.\n')
text = text.replace(title, title + entry, 1)
manifest.write_text(text, encoding='utf-8')

changelog = ROOT / 'docs/tracking/CHANGELOG.md'
text = changelog.read_text(encoding='utf-8')
divider = '---\n\n'
assert divider + '> **Last Updated:**' in text
text = text.replace(divider + '> **Last Updated:**', divider + '> **Last Updated (prior):**', 1)
lines = [
    '> **Last Updated:** September 6, 2026 — **Art-pipeline plan v0.8 closes post-acceptance Codex GUID-scope finding; G0 remains accepted and AP-01 authorized.**',
    '> Codex identified one real pre-merge defect in the v0.7 AP-01 execution contract: checking duplicate GUIDs only across `src/` + `Assets/GameArt/` is not Unity-project-wide. The repo already contains `Assets/README.md.meta`, and future scenes/plugins/other assets may live elsewhere under `Assets/`.',
    '> v0.8 therefore keeps missing-meta and orphan-meta enforcement scoped to the managed `src/` + `Assets/GameArt/` roots, but requires duplicate-GUID detection as **one combined scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree**. The mutation proof must demonstrate collisions from GameArt into both `src/` and another tracked `Assets/` meta.',
    '> **Gate declaration:** the owner\'s September 6 G0 acceptance remains valid; this correction narrows execution ambiguity without reopening visual direction or expanding authorization beyond AP-01.',
    '> **Determinism/runtime declaration:** no runtime behavior, snapshot/serialization schema, RNG stream/domain/draw site/draw order, production asset, workflow contract, `SPEC_INDEX.md` row, or spec status changed.',
    '>',
    '',
]
text = text.replace(divider, divider + '\n'.join(lines), 1)
changelog.write_text(text, encoding='utf-8')
