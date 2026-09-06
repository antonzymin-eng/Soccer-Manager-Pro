# System XI — Art Technical Recipe v1

**Status:** IN PROGRESS — G2 PENDING REAL UNITY IMPORT  
**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Document version:** 0.2  
**Unity target:** `6000.4.9f1 (f7258d6eebbe)`  
**Parent plan:** `docs/planning/art-pipeline-foundation.md` v0.8+  
**Repository contract:** AP-01 landed in PR #365  

---

## 1. Purpose

AP-03 proves the initial production path from editable source to runtime export and actual Unity import. This document records the technical recipe and evidence; it does **not** close G2 until the required Unity 6000.4.9f1 import/replacement proof exists.

The workstream may establish static/repository evidence in parallel with AP-02, but it must not hand-author a production art-file `.meta` merely to simulate an import.

---

## 2. Evidence status

| Requirement | Status | Evidence / remaining action |
|---|---|---|
| Unity version pinned | PASS | `ProjectSettings/ProjectVersion.txt`: 6000.4.9f1, revision `f7258d6eebbe` |
| Source/runtime separation | PASS | AP-01 `art-source/README.md`; runtime root is `Assets/GameArt/` |
| `.art.json` production-candidate record | PASS | AP-01 schema/template |
| Managed `.meta` / project-wide GUID enforcement | PASS | AP-01 checker + mutation proof |
| PNG Git LFS routing | STATIC PASS | root `.gitattributes`: `*.png filter=lfs diff=lfs merge=lfs -text` |
| TTF/OTF Git LFS routing | STATIC PASS | root `.gitattributes`: both font extensions use LFS |
| Initial font rights audit | PASS WITH DISPLAY BLOCKER | §8: all three current candidates are OFL-licensed, but Barlow Condensed lacks current upstream Cyrillic support |
| Ukrainian/Cyrillic body coverage | PASS FOR CANDIDATE | IBM Plex Sans upstream states Cyrillic support; actual vendored binary still must be glyph-tested before shipping |
| Ukrainian/Cyrillic mono coverage | PASS FOR CANDIDATE | JetBrains Mono official character/language lists include Ukrainian Cyrillic |
| Ukrainian/Cyrillic display decision path | PASS WITH VISUAL DECISION PENDING | Barlow is unsuitable as sole Ukrainian display face; IBM Plex Sans Condensed is a technically viable replacement candidate routed to AP-02/G1 (§8.4). Final visual adoption is not a G2 prerequisite. |
| P0 source → export reproducibility | PENDING | create a deliberately neutral technical import probe that does not depend on AP-02/G1; it may later be deleted/deprecated if it has no product use |
| Actual Unity import | **PENDING** | must be performed in Unity 6000.4.9f1; no hand-authored production file meta permitted |
| Importer settings captured from Unity | **PENDING** | record actual `.meta`/Inspector values after import |
| In-place replacement preserves GUID | **PENDING** | replace probe bytes/source export, reimport, verify `.meta` GUID unchanged and consumer reference intact |
| G2 | **OPEN** | cannot pass without the pending Unity evidence above |

---

## 3. Initial P0 technical probe

The first runtime import will use a deliberately non-product-specific **UI icon pipeline probe**, not a portrait/badge/stadium asset. Its job is to exercise transparency, 2D sprite import, LFS routing, GUID identity, source/export replacement, and small-asset settings with minimal style dependency.

The probe is intentionally **independent of G1**. AP-02 may continue refining the production icon language in parallel; AP-03 needs only a neutral geometric source that exercises the technical path. Passing G2 does not make that probe an approved production-style asset.

Planned semantic identity:

- art asset ID: `ui.pipeline.import-probe`;
- planned source: `art-source/ui/icons/ap03_import_probe.svg` (or the AP-02-approved vector-master equivalent);
- planned runtime export: `Assets/GameArt/UI/Icons/ap03_import_probe.png`;
- planned metadata sidecar: adjacent to the source as `ap03_import_probe.art.json`;
- runtime filename is semantic/lower-snake-case; no revision suffix.

**Do not create the runtime PNG on the branch until the same operation can pass through actual Unity import and commit the Unity-authored `.meta`.** AP-01's checker intentionally makes a half-imported state fail.

The probe may be deleted/deprecated after G2 if it has no product use; its evidence remains in Git/this document.

---

## 4. Source and export formats

### 4.1 Initial P0 family

For UI/match 2D art:

- preferred editable master: SVG/vector source when the chosen family is fundamentally geometric;
- alternate editable master: high-resolution lossless raster when vector is not appropriate;
- initial Unity runtime export: PNG with alpha when needed;
- no baked localized copy.

SVG is source-side for the initial contract. Runtime SVG is not authorized merely because the source is vector.

### 4.2 Source-only formats

PSD, TIFF, EXR/HDR working masters, DCC-specific sources, generation working files, and reference material remain outside `Assets/GameArt/` unless a later evidence-based family recipe explicitly changes that rule.

### 4.3 Future formats

3D/model production remains AP-14. Atlases, Addressables, catalogs, and importer automation remain evidence-gated.

---

## 5. Unity import recipe — candidate settings to prove

These are **candidate settings**, not evidence. AP-03 records the actual Unity 6000.4.9f1 result after import and changes this table if the editor/consumer requires different values.

For the UI icon probe, evaluate:

| Setting | Candidate | Reason to test |
|---|---|---|
| Texture Type | Sprite (2D and UI) | intended UGUI/2D use |
| Sprite Mode | Single | one icon per export for the probe |
| sRGB | enabled | ordinary UI color asset |
| Alpha Is Transparency | enabled when alpha exists | edge handling |
| Mip Maps | disabled | small UI icon; no distance sampling expected |
| Wrap Mode | Clamp | avoid edge repetition |
| Filter Mode | Bilinear initially | check actual small-size rendering; change only with visual evidence |
| Compression | editor/platform default initially | measure quality/memory before pinning final family policy |
| Max Size | no final value yet | derive from actual export size/use, not an invented global budget |

After the first import, record:

1. Unity Inspector values;
2. relevant importer fields from the generated `.meta`;
3. imported texture dimensions/runtime memory where available;
4. any automatic value Unity writes that differs from this candidate table.

---

## 6. GUID replacement proof

G2 requires this exact sequence on the imported probe:

1. import the first PNG through Unity and commit its Unity-authored `.meta`;
2. record the GUID from that `.meta`;
3. bind/reference the asset in the smallest legitimate Unity presentation test surface available for the import proof;
4. revise the same semantic source/export **in place** (no `_v002` path);
5. reimport in Unity;
6. verify the `.meta` GUID is byte-identical before/after;
7. verify the consumer reference still resolves;
8. verify AP-01 project-wide duplicate-GUID check stays green.

If the path must be moved during the proof, move the asset and `.meta` together and treat that as a separate rename test rather than conflating it with ordinary replacement.

---

## 7. Git/LFS proof

The repository already routes the initial binary categories through Git LFS:

- PNG;
- TTF;
- OTF.

Before G2 closes, record `git check-attr filter diff merge -- <runtime-path>` for the actual probe and any font binary introduced by this slice. Expected binary result is `filter=lfs`, `diff=lfs`, `merge=lfs`.

`tools/unity-ci/check-binaries.sh` remains the whole-repository large-binary safety net. LFS routing is a repository contract; AP-03 does not introduce a second binary storage system.

---

## 8. Typography rights and script audit

This is a technical/rights audit of the candidates already present in the chosen mockups. It is not a new typography aesthetic exercise.

### 8.1 IBM Plex Sans — body candidate

Upstream: `IBM/plex`  
License: SIL Open Font License 1.1  
Upstream coverage claim: IBM Plex Sans supports Cyrillic among its supported scripts.

Disposition: **viable body candidate**, subject to testing the exact vendored binary against the project's required Ukrainian string corpus before shipping.

Evidence:

- <https://github.com/IBM/plex>

### 8.2 JetBrains Mono — mono/data candidate

Upstream: `JetBrains/JetBrainsMono`  
License: SIL Open Font License 1.1  
Upstream coverage evidence: official character/language documentation includes Cyrillic Ukrainian characters/language support.

Disposition: **viable mono/data candidate**, subject to exact-binary corpus test.

Evidence:

- <https://github.com/JetBrains/JetBrainsMono>
- <https://www.jetbrains.com/lp/mono/>

### 8.3 Barlow Condensed — inherited display candidate

Upstream: `jpt/barlow`  
License: SIL Open Font License 1.1  
Current upstream problem: Cyrillic support remains an open feature request/roadmap item; the public project currently documents Latin/Vietnamese evolution rather than shipped Cyrillic support.

Disposition: **not acceptable as the sole shipping display face for Ukrainian**. G2 cannot declare the existing three-font stack localization-complete while this remains unresolved.

Evidence:

- <https://github.com/jpt/barlow>
- <https://github.com/jpt/barlow/issues/16>

### 8.4 Proposed display substitute for AP-02 review: IBM Plex Sans Condensed

IBM Plex Sans Condensed v3.0 added Cyrillic support (194 glyphs per font) and Bulgarian Cyrillic forms; the family is distributed under the same IBM Plex OFL project.

Disposition: **preferred technical candidate to evaluate visually in AP-02** because it can keep the condensed display role while aligning with the existing IBM Plex Sans body family and removing the known Cyrillic hole. AP-02 has recorded this as explicit proposed token change T-01 rather than a silent substitution.

For **G2**, this closes the required decision-path problem: the inherited face is identified as unsuitable, a rights-compatible Cyrillic-capable candidate is identified, and final visual selection is routed to G1. G2 does **not** need to wait for G1 or vendor the final font binaries, because the accepted G2 contract requires the rights/script/fallback path to be explicit rather than final typography rollout. Exact-binary adoption and corpus validation remain required before a font is marked validated/shipping.

Evidence:

- <https://github.com/IBM/plex/releases>

### 8.5 Packaging rule for OFL fonts

For any selected font binary:

- vendor a fixed upstream version; do not depend on network font loading;
- retain the upstream copyright/license text with the distributed font notices;
- do not modify/rename the font during AP-03; modified-font naming obligations are therefore avoided in this slice;
- record exact upstream version/file names and hashes when binaries are adopted;
- ship only the weights actually needed by real screens.

Candidate minimum weights to validate, not yet vendor:

- display: one bold/semi-bold weight needed by the chosen display role;
- body: regular + semibold initially;
- mono/data: regular + semibold only if both appear in real compositions.

No font binary is committed until the visual decision and exact package/version are settled.

---

## 9. Required Ukrainian glyph/corpus proof

Before any font is marked validated, test the exact vendored binaries for at least:

- `А Б В Г Ґ Д Е Є Ж З И І Ї Й К Л М Н О П Р С Т У Ф Х Ц Ч Ш Щ Ь Ю Я`;
- lowercase equivalents including `ґ є і ї`;
- apostrophe usage (`’` and the project's normalized text form);
- numerals, percent, currency, punctuation, en/em dash, quotes;
- representative football strings supplied by localization (club/player/competition/UI labels).

Coverage alone is insufficient: inspect clipping, weight consistency, condensed-display legibility, tabular/numeric behavior where relevant, and fallback substitution.

---

## 10. Offline/runtime packaging

The mockups currently fetch fonts over the network. Shipping client rules:

- no runtime network dependency for core UI fonts;
- approved font binaries live under the eventual `Assets/GameArt/Fonts/` runtime family and receive Unity-authored metas;
- license/notices remain source/distribution documentation rather than decorative game assets;
- fallback chain is explicit and tested; missing glyphs must not silently fall to an arbitrary platform font;
- do not rasterize localized text to hide coverage gaps.

---

## 11. G2 closure checklist

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

---

## 12. Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-09-06 | AP-03 technical contract/evidence ledger created. Records Unity 6000.4.9f1 target, source/export/import/GUID/LFS proof procedure, initial importer candidates, and font rights/script audit. Identifies Barlow Condensed Cyrillic gap and proposes IBM Plex Sans Condensed for AP-02 visual review. G2 explicitly remains open pending real Unity import and exact-binary font proof. |
| 0.2 | 2026-09-06 | Hostile-review sequencing correction: removes accidental G1/final-font-binary prerequisites from G2, makes the import probe explicitly style-neutral/G1-independent, and treats the font audit as an explicit rights/script/fallback decision path. G2 still requires real Unity import/replacement evidence; final font binary validation remains a later shipping requirement unless AP-03 vendors fonts. |
