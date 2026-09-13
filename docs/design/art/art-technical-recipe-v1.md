# System XI — Art Technical Recipe v1

**Status:** IN PROGRESS — G2 EVIDENCE COMPLETE; awaiting G2 review  
**Created:** September 6, 2026  
**Last Updated:** September 12, 2026  
**Document version:** 0.6  
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
| `.art.json` production-candidate record | PASS | AP-01 schema/template. The probe's `art-source/ui/icons/ap03_import_probe.art.json` satisfies the schema's required keys, enums and `asset_id` pattern. It is classified **`source_kind: generated`** — the SVG was written by an AI agent (Claude Code, claude-opus-5) — and carries the plan §7.2 `generation` record the schema requires for that kind |
| Managed `.meta` / project-wide GUID enforcement | PASS | AP-01 checker + mutation proof |
| PNG Git LFS routing | PASS | proof run `34060090061` (planned path) **and** the actual committed probe: `git check-attr` resolves `filter=lfs`, `diff=lfs`, `merge=lfs`, and the committed blob is an LFS pointer (§7.1) |
| TTF/OTF Git LFS routing | PASS | proof run `34060090061`: planned GameArt `.ttf` and `.otf` paths both resolve `filter=lfs`, `diff=lfs`, `merge=lfs` |
| Initial font rights audit | PASS | §8: the G1-accepted stack — PT Sans Narrow (display, §8.4a), IBM Plex Sans (body), JetBrains Mono (mono/data) — is all SIL OFL 1.1. PT Sans Narrow carries Reserved Font Names. The inherited Barlow Condensed display blocker is resolved by G1 retiring it (§8.3) |
| Ukrainian/Cyrillic body coverage | PASS FOR CANDIDATE | IBM Plex Sans upstream states Cyrillic support; actual vendored binary still must be glyph-tested before shipping |
| Ukrainian/Cyrillic mono coverage | PASS FOR CANDIDATE | JetBrains Mono official character/language lists include Ukrainian Cyrillic |
| Ukrainian/Cyrillic display decision path | PASS — VISUAL ROLE ACCEPTED AT G1; EXACT-FONT VALIDATION PENDING | Barlow is unsuitable as sole Ukrainian display face (§8.3). G1 rejected IBM Plex Sans Condensed visually (§8.4) and accepted **PT Sans Narrow** / equivalent humanist condensed face (§8.4a): OFL 1.1 with Reserved Font Names, `cyrillic` subsets declared upstream, Regular + Bold only. Exact binary, RFN handling for any conversion, and the Ukrainian corpus proof remain shipping requirements, not G2 blockers. |
| P0 source → export reproducibility | **PASS** | §4.4: resvg 0.47.0, three exports of the original source byte-identical (`826f667f…`); the revised source likewise (`0018e255…`); output invariant to LF vs CRLF source line endings |
| No premature runtime art | PASS (superseded by the import) | proof run `34060090061` asserted `Assets/GameArt/` absent *before* import. AP-03 has now created it legitimately through a Unity import; the only GameArt content is the probe and its Unity-authored folder/file metas |
| Actual Unity import | **PASS** | Unity 6000.4.9f1 editor on the pinned Windows 11 host, September 12, 2026; commit `dd0d1ff5` (§5.1) |
| Importer settings captured from Unity | **PASS** | §5.1: first-import defaults, applied candidate settings, generated `.meta` fields, dimensions, format and memory |
| In-place replacement preserves GUID | **PASS** | §6.1: GUID `24746b6a9f9592e41be3206d04b7b96e` unchanged, `.meta` byte-identical, consumer reference resolves; commit `abc07f2f` |
| G2 | **OPEN — evidence complete** | every §11 checklist item is ticked with recorded evidence; G2 passes only on review, not on this ledger's own say-so |

---

## 3. Initial P0 technical probe

The first runtime import will use a deliberately non-product-specific **UI icon pipeline probe**, not a portrait/badge/stadium asset. Its job is to exercise transparency, 2D sprite import, LFS routing, GUID identity, source/export replacement, and small-asset settings with minimal style dependency.

The probe is intentionally **independent of G1**. AP-02 may continue refining the production icon language in parallel; AP-03 needs only a neutral geometric source that exercises the technical path. Passing G2 does not make that probe an approved production-style asset.

Semantic identity (as landed):

- art asset ID: `ui.pipeline.import-probe`;
- source: `art-source/ui/icons/ap03_import_probe.svg` — a 128×128 viewBox with a rounded-square outline (`#5A6270`, 8 px stroke) and a filled circle, on a transparent background;
- runtime export: `Assets/GameArt/UI/Icons/ap03_import_probe.png`;
- metadata sidecar: `art-source/ui/icons/ap03_import_probe.art.json` — `source_kind: generated` with a `generation` record (AI-agent-written SVG markup; no image model, no third-party inputs, not adopted for release);
- runtime filename is semantic/lower-snake-case; no revision suffix.

**Do not create the runtime PNG on the branch until the same operation can pass through actual Unity import and commit the Unity-authored `.meta`.** AP-01's checker intentionally makes a half-imported state fail. *(Satisfied: the PNG entered Git in the same commit as its Unity-authored `.meta`, `dd0d1ff5`.)*

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

### 4.4 SVG → PNG exporter (recipe, proven on the probe)

| Item | Value |
|---|---|
| Tool | `resvg` **0.47.0**, official `resvg-win64.zip` from `github.com/linebender/resvg` releases (tag `v0.47.0`) |
| Archive integrity | zip sha256 `5684e59ceaa53ce720b49efb441b0918ae99d04e8ce3f6f753664524592d67f1` — matches the digest GitHub publishes for the release asset; `resvg.exe` sha256 `433a7c744cff561ed64fcf73c7c04e239d7a07ae5f0aadbf1ba8471d63707402` |
| Install location | outside the repository (`%LOCALAPPDATA%\Programs\resvg-0.47.0\` on the pinned host); no binary is vendored |
| Command | `resvg --width <W> --height <H> <source.svg> <Assets/GameArt/.../<name>.png>` — the probe uses `--width 128 --height 128` |
| Why 0.47.0 and not latest | `v0.48.0`/`v0.48.1` (August 2026) publish **no Windows binary**; 0.47.0 is the newest release with an official win64 build. Moving the pin is a recipe change: re-run the reproducibility check and record new hashes |

Reproducibility evidence on the pinned host:

| Source revision | Source sha256 | Export sha256 | Size | Runs compared |
|---|---|---|---|---|
| original (`dd0d1ff5`) | `cd033dbe9a277f5ffc84d024147f5f6c935e33ff1b2ec50cf12c444f5ea98acc` (as authored, LF) | `826f667f5e58f35026e373babad6e1c240c618129659bb30503e621a1d7bb08a` | 2 298 B | 3 exports, byte-identical |
| revised (`abc07f2f`) | — | `0018e255ed05541f16eb96e47dc48c34c44d1f9fb20eaecec91b9549928f66ef` | 2 568 B | 2 exports byte-identical; the same source rewritten with LF and with CRLF line endings exports byte-identically |

The line-ending check matters because `.gitattributes` carries no rule for `*.svg` (it falls to `* text=auto`) and this host checks out with `core.autocrlf=true`, so the SVG's bytes on disk differ by platform. The export does not, so no `.gitattributes` change is needed for reproducibility.

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

### 5.1 Recorded Unity 6000.4.9f1 result (September 12, 2026)

Imported in the Unity 6000.4.9f1 editor on the pinned Windows 11 / DX11 / Mono host. Values were read from `TextureImporter` through the editor API immediately after each import, and cross-checked against the generated `.meta`.

**First-import defaults — what Unity chose with no settings applied.** Every row marked ≠ differs from the candidate, so the recipe must set it explicitly; a PNG dropped into `Assets/GameArt/` and left at defaults does not get the candidate profile.

| Setting | Unity first-import default | Candidate | |
|---|---|---|---|
| Texture Type | Default | Sprite (2D and UI) | ≠ |
| Sprite Mode | None | Single | ≠ |
| sRGB | on | on | = |
| Alpha Is Transparency | off (Alpha Source: From Input) | on | ≠ |
| Mip Maps | on | off | ≠ |
| Wrap Mode | Repeat | Clamp | ≠ |
| Filter Mode | Bilinear | Bilinear | = |
| Aniso level | 1 | — | |
| Compression | Compressed (Normal Quality) | editor/platform default | = |
| Max Size | 2048 | none pinned | |
| Non-Power-of-2 | ToNearest | — | |
| Read/Write | off | — | |

**After applying the candidate settings** (`SaveAndReimport`): Texture Type Sprite, Sprite Mode Single, sRGB on, Alpha Is Transparency on, Mip Maps off, Wrap Clamp, Filter Bilinear, Compression Compressed, Max Size 2048, Pixels Per Unit 100, pivot centre (0.5, 0.5), Read/Write off. **Automatic value Unity changed on its own:** Non-Power-of-2 went `ToNearest → None` when the type became Sprite (the probe is 128×128, so this has no effect here, but it will on NPOT exports).

**Generated `.meta` fields worth pinning** (`Assets/GameArt/UI/Icons/ap03_import_probe.png.meta`, sha256 `48a517931ff1dbc9f08c72a6b709328a824d983c6d77ea5a232af38d3289cfe0`): `serializedVersion: 13`; `textureType: 8`; `spriteMode: 1`; `enableMipMap: 0`; `sRGBTexture: 1`; `alphaIsTransparency: 1`; `alphaUsage: 1`; `textureSettings` `filterMode: 1`, `aniso: 1`, `wrapU/V/W: 1`; `nPOTScale: 0`; `maxTextureSize: 2048`; `compressionQuality: 50`; `spriteMeshType: 1` (Tight); `spriteExtrude: 1`; `spriteGenerateFallbackPhysicsShape: 1`; `spritePixelsToUnits: 100`. `platformSettings` holds three non-overridden entries — `DefaultTexturePlatform`, `Standalone`, `WebGL` — each `textureFormat: -1` (automatic), `textureCompression: 1`, `crunchedCompression: 0`.

**Imported result:** 128×128; Standalone automatic format **DXT5** (`RGBA_DXT5_SRGB`), 1 mip level; editor-reported runtime memory (`Profiler.GetRuntimeMemorySizeLong`) **33 672 bytes**. The sprite imported as `ap03_import_probe`, rect 128×128, 100 PPU, pivot (64, 64) px.

**Recorded, not yet pinned** — these are observations for AP-06 or a family recipe, not settings changed here:

- `spriteGenerateFallbackPhysicsShape: 1` is on by default. A UI icon needs no physics shape, but turning it off is an optimisation with no measured need at probe scale.
- `spriteMeshType: 1` (Tight) is the default. For UGUI `Image` rendering the mesh type is not used, but for `SpriteRenderer` Tight costs extra vertices on simple shapes.
- DXT5 at 128×128 is small, and the probe shows no visible banding. Compression quality has **not** been judged on real icon art. The candidate "platform default initially" stands until AP-04 has representative assets.
- `com.unity.ugui` is **not** in `Packages/manifest.json`, so the probe's consumer was a `SpriteRenderer`, not a UGUI `Image` (§6.1). The UGUI binding (P5b) will need the package added before any `Image`-based consumer exists.

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

### 6.1 Recorded proof (September 12, 2026)

| Step | Result |
|---|---|
| 1. Import + commit | Commit `dd0d1ff5`: PNG (LFS), Unity-authored file `.meta`, and the three Unity-authored folder metas `Assets/GameArt.meta` (`47f3359c00fa4cb4e88a49ea9afdd8b6`), `Assets/GameArt/UI.meta` (`3875ca4a73fc02d4a9555611b2e4fe44`), `Assets/GameArt/UI/Icons.meta` (`82801b7cb8e3c82418fc1fb53b76a9f5`). No meta was hand-written or generated by script. |
| 2. GUID | `24746b6a9f9592e41be3206d04b7b96e`; unchanged across first import and the candidate-settings reimport. |
| 3. Consumer | A prefab with one `SpriteRenderer` whose sprite is the probe. Its YAML serialized `m_Sprite: {fileID: 21300000, guid: 24746b6a9f9592e41be3206d04b7b96e, type: 3}` and `AssetDatabase.GetDependencies` listed the probe PNG. **Temporary by design:** created at `Assets/ArtPipelineProof/`, never committed, and deleted after step 7, so G2 introduces no consumer surface of its own (§11, no speculative architecture). |
| 4. In-place revision | Same source and export path. Inner circle `r=26 #8A93A3` → `r=32 #B4BBC7`, re-exported with the §4.4 command (`826f667f…` → `0018e255…`). |
| 5. Reimport | `AssetDatabase.ImportAsset(…, ForceUpdate \| ForceSynchronousImport)`. |
| 6. `.meta` identity | GUID identical, and the whole `.meta` **byte-identical** (sha256 `48a51793…` before and after). Commit `abc07f2f` changes only the SVG and the PNG pointer; the `.meta` is absent from its diff. |
| 7. Consumer resolves | After reimport the prefab's `SpriteRenderer.sprite` resolved to `ap03_import_probe` at the same path, GUID `24746b6a…` and local file id `21300000`. Its texture's `imageContentsHash` equals the freshly reimported texture's (`eb151f10deb6e5feeb50acdfd365ff2a`), so the reference sees the **new** content, not a cached copy. |
| Content actually changed | Pixel (64, 93), Unity's bottom-up coordinates, measured on both sides. The original export decodes to RGBA `(0,0,0,0)` there and the revised export to `(180,187,199,255)` = `#B4BBC7`. The reimported Unity texture samples `(181,186,198,255)` there — the revised colour within DXT5 error. |
| 8. AP-01 gate | `tools/unity-ci/check-meta-integrity.sh` — after step 1 (`dd0d1ff5`): **"Meta integrity OK: no managed-root missing/orphan metas and no duplicate GUIDs across tracked Assets/ + src/."**, exit 0. After replacement (`abc07f2f`): **same output, exit 0.** Both runs on the pinned host, Git Bash. |

---

## 7. Git/LFS proof

The repository already routes the initial binary categories through Git LFS:

- PNG;
- TTF;
- OTF.

Before G2 closes, record `git check-attr filter diff merge -- <runtime-path>` for the actual probe and any font binary introduced by this slice. Expected binary result is `filter=lfs`, `diff=lfs`, `merge=lfs`.

`tools/unity-ci/check-binaries.sh` remains the whole-repository large-binary safety net. LFS routing is a repository contract; AP-03 does not introduce a second binary storage system.

### 7.1 Recorded proof (September 12, 2026)

- `git check-attr filter diff merge -- Assets/GameArt/UI/Icons/ap03_import_probe.png` → `filter: lfs`, `diff: lfs`, `merge: lfs`.
- The committed blob is an LFS pointer, not image bytes, at **both** proof commits, and the two are different pointers because the replacement changed the image. The .meta blob is identical at both:
  - **initial import** `dd0d1ff5`: `oid sha256:826f667f5e58f35026e373babad6e1c240c618129659bb30503e621a1d7bb08a` / `size 2298` — the original export (§4.4);
  - **final replacement** `abc07f2f` and every later commit on the branch: `oid sha256:0018e255ed05541f16eb96e47dc48c34c44d1f9fb20eaecec91b9549928f66ef` / `size 2568` — the revised export (§4.4).
  
  Each oid equals the sha256 of the export it records. `git lfs ls-files` lists the path. Both LFS objects were uploaded by the `pre-push` hook, so the initial-import commit remains checkout-able, not a dangling pointer.
- Host: `git-lfs/3.7.0`, with the `filter.lfs.*` config and the `pre-push` hook installed.
- No font binary was introduced by this slice.
- `tools/unity-ci/check-binaries.sh` (pinned host, Git Bash, started on the `dd0d1ff5` tree; the replacement adds no binary over threshold): **"Binary guard OK: no un-LFS'd binaries over 1048576 bytes, no text files over 4194304 bytes."**, exit 0.

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

### 8.4 IBM Plex Sans Condensed — REJECTED at G1 (visual)

IBM Plex Sans Condensed v3.0 added Cyrillic support and was proposed here at v0.1 as the display substitute (AP-02 token change T-01). **G1 (owner, September 10, 2026) rejected it visually as too synthetic** — `art-direction-v1.md` §3.4 and §14 T-01. It is technically viable, but it is no longer a candidate unless the accepted role below fails exact-font validation and G1 is deliberately reopened.

Evidence:

- <https://github.com/IBM/plex/releases>

### 8.4a PT Sans Narrow — accepted display reference (G1), pending AP-03 exact-font validation

G1 accepted **PT Sans Narrow / an equivalent Cyrillic-capable humanist condensed face** for the display role (`art-direction-v1.md` §3.4, T-01). AP-03 owns the exact shipping binary, rights, offline packaging, glyph-corpus proof and fallback. It may substitute another face without reopening G1 only if the accepted visual role and dense-screen behaviour are preserved.

Upstream: ParaType, distributed in `google/fonts` as `ofl/ptsansnarrow`  
License: SIL Open Font License 1.1, **with Reserved Font Names "PT Sans" and "ParaType"** (`OFL.txt`: "Copyright (c) 2010, ParaType Ltd. … with Reserved Font Names 'PT Sans' and 'ParaType'")  
Upstream coverage claim: `METADATA.pb` subsets `cyrillic`, `cyrillic-ext`, `latin`, `latin-ext`  
Upstream files: `PT_Sans-Narrow-Web-Regular.ttf` (400) and `PT_Sans-Narrow-Web-Bold.ttf` (700) — **only two weights**; there is no semi-bold

Disposition: **accepted visual reference; rights-compatible; Cyrillic declared upstream.** Three things follow for AP-03 and are recorded as open, not resolved:

- **Reserved Font Names.** Under OFL 1.1 a Modified Version may not use a Reserved Font Name. §8.5 already forbids modifying or renaming a font in this slice. Any later subsetting, glyph-merging or format conversion of the *font file* must be checked against the RFN clause before it ships under the name "PT Sans". Whether generating a Unity/TextMeshPro atlas from an unmodified binary counts as creating a Modified Version is **not** decided here — it is a question for the typography import slice, not an assumption.
- **Weight availability.** The display role needs "one bold/semi-bold weight" (§8.5). Bold 700 exists; semi-bold does not, so the role is Bold unless an equivalent face is substituted.
- **Ukrainian proof.** The `cyrillic` subset declaration is upstream's claim. §9's corpus test on the exact vendored binary — Ґ ґ, Є є, І і, Ї ї, apostrophe forms — is still required before the face is marked validated.

Evidence (retrieved September 12, 2026):

- <https://github.com/google/fonts/blob/main/ofl/ptsansnarrow/METADATA.pb>
- <https://github.com/google/fonts/blob/main/ofl/ptsansnarrow/OFL.txt>
- `docs/design/art/art-direction-v1.md` §3.4, §14 T-01, §15
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

- [x] one neutral technical source asset exports reproducibly to PNG without depending on G1 approval — §4.4;
- [x] `git check-attr` proves LFS routing on the actual runtime path — §7.1;
- [x] Unity 6000.4.9f1 imports the PNG and authors the production `.meta` — §5.1, §6.1 step 1;
- [x] actual importer settings are recorded here — §5.1;
- [x] in-place replacement preserves GUID and live consumer reference — §6.1;
- [x] AP-01 `.meta`/duplicate-GUID gate passes after import and replacement — `check-meta-integrity.sh` **PASS** after import (`dd0d1ff5`) and after replacement (`abc07f2f`), §6.1 step 8. The whole-repo `check-binaries.sh` safety net (§7) is not a checklist item, and its run result is recorded in §7.1;
- [x] font rights/script/fallback **decision path is explicit**: current Barlow display face is unsuitable for required Ukrainian coverage; IBM Plex Sans Condensed was rejected visually at G1; G1-accepted PT Sans Narrow is OFL 1.1 (Reserved Font Names recorded) with upstream-declared Cyrillic subsets, §8.4a; IBM Plex Sans / JetBrains Mono have viable Cyrillic paths;
- [x] no speculative Addressables/atlas/catalog architecture was introduced — the slice adds only the probe SVG, its sidecar, one PNG, and Unity-authored metas; the §6.1 consumer was temporary and is not in the tree.

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
| 0.3 | 2026-09-06 | Static evidence recorded from run `34060090061`: Unity pin, planned PNG/TTF/OTF LFS attributes, no-premature-GameArt assertion, AP-01 integrity/binary baseline, and documentation-only scope all passed. Remaining G2 blockers are the real source/export/Unity import/importer/replacement/reference proof. *(The header still read 0.2 after this row landed; corrected at 0.4.)* |
| 0.4 | 2026-09-12 | **Real Unity evidence recorded** on the pinned Windows 11 / Unity 6000.4.9f1 host. New §4.4: exporter pinned to resvg 0.47.0 (newest release with an official win64 binary; 0.48.x ships none), with the release-digest match and byte-identical export evidence, including LF/CRLF invariance. New §5.1: first-import defaults — five differ from the candidate, so the recipe must set them explicitly — plus applied settings, generated `.meta` fields, DXT5 format and 33 672 B editor-reported memory. New §6.1: GUID `24746b6a…` and the whole `.meta` byte-identical across in-place revision, the temporary consumer reference resolving to the new content, and before/after pixels measured. New §7.1: actual LFS pointer. Commits `dd0d1ff5` (import) and `abc07f2f` (replacement). The §2 import/importer/replacement/reproducibility rows move to PASS. AP-01 `check-meta-integrity.sh` PASS after both commits; every §11 item ticked. G2 stays OPEN pending review. |
| 0.5 | 2026-09-12 | **Typography section re-synchronised with the G1 decision.** v0.1–0.4 still carried IBM Plex Sans Condensed as the proposed display substitute, but `art-direction-v1.md` v1.4 (G1 accepted September 10, 2026) had already rejected it visually and accepted PT Sans Narrow / an equivalent humanist condensed face. §8.4 now records that rejection. New §8.4a records PT Sans Narrow's upstream evidence: OFL 1.1 with Reserved Font Names 'PT Sans' and 'ParaType', `cyrillic`/`cyrillic-ext` subsets declared, Regular 400 + Bold 700 only. It also records three open consequences — RFN handling for any font-file conversion, no semi-bold weight, and the still-required Ukrainian corpus proof. §2 display row and the §11 font item updated. No Unity evidence changed; G2 remains OPEN pending review. |
| 0.6 | 2026-09-12 | **External review corrections (PR #405).** (1) §7.1 described one LFS pointer, the initial import's `826f667f…` / 2 298 B, as "the committed blob", but the branch head carries the replacement's `0018e255…` / 2 568 B. §7.1 now records both pointers against their commits. (2) The probe's `.art.json` said `source_kind: original` while naming an AI agent as creator. The plan §7.2 treats AI-assisted work as generated, so it is reclassified `generated` with the full `generation` record, and the rights basis no longer asserts original authorship. §2 and §3 updated to match. No Unity evidence, hash or GUID changed; G2 remains OPEN pending review. |
