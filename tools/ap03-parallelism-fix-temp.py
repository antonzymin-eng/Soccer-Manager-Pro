from pathlib import Path

p = Path('work/docs/design/art/art-technical-recipe-v1.md')
text = p.read_text(encoding='utf-8')
text = text.replace('**Created:** September 6, 2026  \n**Unity target:**', '**Created:** September 6, 2026  \n**Last Updated:** September 6, 2026  \n**Document version:** 0.2  \n**Unity target:**', 1)
old = '| Ukrainian/Cyrillic display coverage | **BLOCKED** | current Barlow upstream has an open Cyrillic-support issue; proposed substitute is IBM Plex Sans Condensed v3.0+ (§8.4) |\n| P0 source → export reproducibility | PENDING | create technical import probe after AP-02/G1 does not conflict with the chosen icon/source recipe |'
new = '| Ukrainian/Cyrillic display decision path | PASS WITH VISUAL DECISION PENDING | Barlow is unsuitable as sole Ukrainian display face; IBM Plex Sans Condensed is a technically viable replacement candidate routed to AP-02/G1 (§8.4). Final visual adoption is not a G2 prerequisite. |\n| P0 source → export reproducibility | PENDING | create a deliberately neutral technical import probe that does not depend on AP-02/G1; it may later be deleted/deprecated if it has no product use |'
assert old in text
text = text.replace(old, new, 1)
old = '''The first runtime import will use a deliberately non-product-specific **UI icon pipeline probe**, not a portrait/badge/stadium asset. Its job is to exercise transparency, 2D sprite import, LFS routing, GUID identity, source/export replacement, and small-asset settings with minimal style dependency.
'''
new = '''The first runtime import will use a deliberately non-product-specific **UI icon pipeline probe**, not a portrait/badge/stadium asset. Its job is to exercise transparency, 2D sprite import, LFS routing, GUID identity, source/export replacement, and small-asset settings with minimal style dependency.

The probe is intentionally **independent of G1**. AP-02 may continue refining the production icon language in parallel; AP-03 needs only a neutral geometric source that exercises the technical path. Passing G2 does not make that probe an approved production-style asset.
'''
assert old in text
text = text.replace(old, new, 1)
old = '''### 8.4 Proposed display substitute for AP-02 review: IBM Plex Sans Condensed

IBM Plex Sans Condensed v3.0 added Cyrillic support (194 glyphs per font) and Bulgarian Cyrillic forms; the family is distributed under the same IBM Plex OFL project.

Disposition: **preferred technical candidate to evaluate visually in AP-02** because it can keep the condensed display role while aligning with the existing IBM Plex Sans body family and removing the known Cyrillic hole.

This is a cross-workstream finding, **not a silent token change**. AP-02 must put the change in its token-change register and update the mockup token source in the same eventual landing if G1 accepts it.
'''
new = '''### 8.4 Proposed display substitute for AP-02 review: IBM Plex Sans Condensed

IBM Plex Sans Condensed v3.0 added Cyrillic support (194 glyphs per font) and Bulgarian Cyrillic forms; the family is distributed under the same IBM Plex OFL project.

Disposition: **preferred technical candidate to evaluate visually in AP-02** because it can keep the condensed display role while aligning with the existing IBM Plex Sans body family and removing the known Cyrillic hole. AP-02 has recorded this as explicit proposed token change T-01 rather than a silent substitution.

For **G2**, this closes the required decision-path problem: the inherited face is identified as unsuitable, a rights-compatible Cyrillic-capable candidate is identified, and final visual selection is routed to G1. G2 does **not** need to wait for G1 or vendor the final font binaries, because the accepted G2 contract requires the rights/script/fallback path to be explicit rather than final typography rollout. Exact-binary adoption and corpus validation remain required before a font is marked validated/shipping.
'''
assert old in text
text = text.replace(old, new, 1)
old = '''## 11. G2 closure checklist

G2 stays **OPEN** until all are true:

- [ ] AP-02/G1 supplies a compatible approved style target for the technical probe and resolves the display-font finding;
- [ ] one source asset exports reproducibly to PNG;
- [ ] `git check-attr` proves LFS routing on the actual runtime path;
- [ ] Unity 6000.4.9f1 imports the PNG and authors the production `.meta`;
- [ ] actual importer settings are recorded here;
- [ ] in-place replacement preserves GUID and live consumer reference;
- [ ] AP-01 `.meta`/duplicate-GUID gate passes after import and replacement;
- [ ] selected font versions/licenses are pinned;
- [ ] exact font binaries pass Ukrainian/Cyrillic corpus checks;
- [ ] font runtime packaging is offline and redistribution notices are planned;
- [ ] no speculative Addressables/atlas/catalog architecture was introduced.

If real Unity import cannot be executed, G2 remains pending regardless of how much static evidence is green.
'''
new = '''## 11. G2 closure checklist

G2 stays **OPEN** until all are true:

- [ ] one neutral technical source asset exports reproducibly to PNG without depending on G1 approval;
- [ ] `git check-attr` proves LFS routing on the actual runtime path;
- [ ] Unity 6000.4.9f1 imports the PNG and authors the production `.meta`;
- [ ] actual importer settings are recorded here;
- [ ] in-place replacement preserves GUID and live consumer reference;
- [ ] AP-01 `.meta`/duplicate-GUID gate passes after import and replacement;
- [x] font rights/script/fallback **decision path is explicit**: current Barlow display face is unsuitable for required Ukrainian coverage, IBM Plex Sans Condensed is the technical replacement candidate routed to AP-02/G1, and IBM Plex Sans / JetBrains Mono have viable Cyrillic paths;
- [ ] no speculative Addressables/atlas/catalog architecture was introduced.

The following remain required **before font binaries are validated/shipping**, but are not G2 blockers unless AP-03 itself begins shipping those binaries:

- exact selected font versions/files/hashes pinned;
- exact vendored binaries pass the Ukrainian corpus;
- offline runtime packaging and redistribution notices are verified.

If real Unity import cannot be executed, G2 remains pending regardless of how much static evidence is green. Conversely, G2 does not wait on G1: AP-02 and AP-03 remain parallel, and G1 owns the final visual acceptance of T-01.
'''
assert old in text
text = text.replace(old, new, 1)
old = '| 0.1 | 2026-09-06 | AP-03 technical contract/evidence ledger created. Records Unity 6000.4.9f1 target, source/export/import/GUID/LFS proof procedure, initial importer candidates, and font rights/script audit. Identifies Barlow Condensed Cyrillic gap and proposes IBM Plex Sans Condensed for AP-02 visual review. G2 explicitly remains open pending real Unity import and exact-binary font proof. |'
new = old + '\n| 0.2 | 2026-09-06 | Hostile-review sequencing correction: removes accidental G1/final-font-binary prerequisites from G2, makes the import probe explicitly style-neutral/G1-independent, and treats the font audit as an explicit rights/script/fallback decision path. G2 still requires real Unity import/replacement evidence; final font binary validation remains a later shipping requirement unless AP-03 vendors fonts. |'
assert old in text
text = text.replace(old, new, 1)
p.write_text(text, encoding='utf-8')
