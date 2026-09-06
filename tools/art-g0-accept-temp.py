from pathlib import Path
import sys

root = Path(sys.argv[1])

plan = root / 'docs/planning/art-pipeline-foundation.md'
text = plan.read_text(encoding='utf-8')
text = text.replace('**Status:** PLANNING — IMPLEMENTATION GATED  ', '**Status:** ACCEPTED — AP-01 AUTHORIZED  ', 1)
text = text.replace('**Version:** 0.6  ', '**Version:** 0.7  ', 1)
text = text.replace('**Implementation gate:** G0 CLOSED pending owner acceptance.  ', '**Implementation gate:** G0 ACCEPTED September 6, 2026 by owner; AP-01 authorized.  ', 1)
needle = '## 21. G0 acceptance checklist\n\nG0 may be opened only when the owner accepts this grounded plan and all of these statements are true:\n'
replacement = '## 21. G0 acceptance checklist\n\n**Acceptance recorded:** September 6, 2026. The owner explicitly accepted G0. AP-01 is authorized; later art-production gates remain unchanged.\n\nG0 may be opened only when the owner accepts this grounded plan and all of these statements are true:\n'
assert needle in text
text = text.replace(needle, replacement, 1)
history = '| 0.6 | 2026-09-06 | AP-01 execution refinement after external review: named all three checker enumerations; required duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan; restricted `generate-missing-metas.sh` to folders/CI safety rather than production art assets; assigned importer-bearing art-asset metas to actual Unity import in AP-03; expanded mutation proof to missing, orphan, and cross-tree duplicate cases. |'
assert history in text
text = text.replace(history, history + '\n| 0.7 | 2026-09-06 | **G0 accepted by owner.** Planning gate closed successfully and AP-01 repository-contract implementation authorized. No bulk asset generation or later family-scale gate was opened. |', 1)
plan.write_text(text, encoding='utf-8')

manifest = root / 'docs/tracking/file-manifest.md'
text = manifest.read_text(encoding='utf-8')
title = '# File Manifest (Post-Migration Baseline)\n\n'
current = title + '**Last Updated:**'
assert text.startswith(current)
text = text.replace(current, title + '**Last Updated (prior):**', 1)
entry = ('**Last Updated:** September 6, 2026 — **Art-pipeline G0 accepted by owner; plan v0.7 authorizes AP-01 only.**\n'
         'Modified: `docs/planning/art-pipeline-foundation.md` **v0.6 → v0.7**, `docs/tracking/CHANGELOG.md`, and this manifest. The owner explicitly accepted G0 on September 6, 2026 after the v0.6 external-review close-out. AP-01 repository/meta-contract implementation is authorized; AP-02/AP-03 and later production/family-scale gates remain governed by the plan and no bulk asset generation is authorized by this acceptance record. Before this acceptance the art branch was verified to merge cleanly with current `main` (#357); this landing updates the branch from `main` before squash merge. No simulation/runtime semantics, RNG, draw order, spec status, or production asset content changed.\n')
text = text.replace(title, title + entry, 1)
manifest.write_text(text, encoding='utf-8')

changelog = root / 'docs/tracking/CHANGELOG.md'
text = changelog.read_text(encoding='utf-8')
divider = '---\n\n'
current = divider + '> **Last Updated:**'
assert current in text
text = text.replace(current, divider + '> **Last Updated (prior):**', 1)
lines = [
    '> **Last Updated:** September 6, 2026 — **Art-pipeline G0 accepted by owner; plan v0.7 authorizes AP-01 only.**',
    '> The owner explicitly accepted G0 after the grounded v0.6 review close-out. `docs/planning/art-pipeline-foundation.md` advances **v0.6 → v0.7**, records the acceptance date/actor, and changes status from planning-gated to **ACCEPTED — AP-01 AUTHORIZED**.',
    '> Scope remains deliberately narrow: this decision opens AP-01 repository/meta-contract implementation only. It does not authorize bulk asset generation or silently pass AP-02/AP-03/G1/G2/G3/G4/family gates.',
    '> The art branch is updated from current `main` (#357) before squash merge. The merge was previously verified conflict-free and #357 did not touch this plan or either art tracking row/header chain. Separate #357 governance/tracking defects are not repaired in this art PR.',
    '> **Determinism/runtime declaration:** no snapshot/serialization schema change; no RNG stream/domain/draw-site/draw-order change; no runtime behavior, assembly graph, spec status, or production art asset changed.',
    '>',
    '',
]
text = text.replace(divider, divider + '\n'.join(lines), 1)
changelog.write_text(text, encoding='utf-8')
