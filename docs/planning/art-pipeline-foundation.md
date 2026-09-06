# System XI — Art Pipeline Plan

**Status:** ACCEPTED — AP-01 AUTHORIZED  
**Started:** September 4, 2026  
**Last Updated:** September 6, 2026  
**Version:** 0.7  
**Implementation gate:** G0 ACCEPTED September 6, 2026 by owner; AP-01 authorized.  
**Purpose:** Define a production-grade art pipeline that can run in parallel with simulation, UI/UX, localization, audio, and management-layer development without creating asset debt, rights risk, or presentation-layer coupling.

---

## 1. Authority, grounding, and current project reality

This plan governs **art production workflow**. It does not reopen decisions already owned elsewhere.

### 1.1 Authority order

When two surfaces disagree, use this order:

1. **APPROVED specifications** govern behavior, architecture, ownership, and runtime boundaries. For client presentation, the primary authority is UI / Client Framework #38 under `docs/specs/ui-client-framework/`.
2. **The Master Development Plan** (`docs/planning/master-development-plan.md`) governs stage-level delivery/quality gates, including Stage-1 professional visual quality and the rejection of placeholder-quality presentation.
3. **The chosen UI design reference** (`docs/design/ui-mockups/`) governs the current visual baseline where no approved specification pins a visual value. Its July 25, 2026 decision is already made: **`touchline` is the chosen direction**.
4. **This art-pipeline plan** governs how visual assets are sourced, reviewed, exported, integrated, validated, scaled, replaced, and retired.
5. **Future art-direction/family-recipe documents** may formalize or extend the chosen visual baseline but may not silently contradict items 1–3.
6. **Individual asset metadata** records the status and provenance of a particular production candidate; it does not create project-wide policy.

If a future art review wants to reverse `touchline`, that is an explicit owner-level visual-direction change. It must update the design reference and the derived art-direction document together. AP-02 is **not** a direction-selection exercise.

### 1.2 Existing visual baseline

`docs/design/ui-mockups/README.md` v1.1 records the July 25, 2026 choice of `touchline`: an **analyst tool with broadcast accents**, rather than the rejected `stadium` broadcast-graphics direction. `docs/design/ui-mockups/assets/tokens.css` already defines the current starting tokens for:

- dark neutrals and surface hierarchy;
- electric-green brand/accent treatment;
- semantic status colors;
- categorical data-visualization colors;
- spacing and radii;
- label/display typography behavior;
- panel/button/HUD geometry;
- display, body, and monospace font stacks.

Those mockups are explicitly non-normative design references, not shipped code. Where they disagree with an APPROVED specification, the specification wins. Within pure visual styling not otherwise specified, the `touchline` block is the baseline AP-02 derives from rather than a candidate competing with new directions.

### 1.3 Existing typography candidates

The current mockups already nominate:

- **Barlow Condensed** for display typography;
- **IBM Plex Sans** as the `touchline` body face;
- **JetBrains Mono** for monospaced/data treatment;
- **Barlow** remains present in the mockup package and is used by the rejected `stadium` direction.

The open pipeline work is therefore not “invent a font strategy from scratch.” It is to verify and record commercial redistribution rights, choose the minimum actually shipped family/weight set, verify Ukrainian/Cyrillic and other required script coverage with localization, define fallback behavior, and replace network font loading with a distributable runtime path.

No font becomes a shipping dependency merely because a mockup loads it.

### 1.4 Existing client implementation surfaces

The art pipeline must target what actually exists:

- `src/ui-framework/` — the approved #38 presentation framework substrate and live-frame/view-model seams;
- `src/match-client-core/` — host-free render models, projection, camera, interpolation, and match presentation decisions;
- `src/match-client-unity/` — Unity binding; P4b code is landed in `MatchClientBehaviour.cs`, while the UGUI shell P5b and remaining on-host P6 work are still open;
- `src/match-client-web/` — existing web presentation host, useful as a comparison/reference surface but not a replacement for the product's Unity client;
- `src/client-app/` — client screen-flow/screen identity substrate;
- `docs/design/ui-mockups/` — dense management-screen visual reference; these pages contain hard-coded mock data and are not runtime screens.

UI/Client Framework #38 explicitly keeps management/tactics screen implementations in later Wave-7 screen specs and treats UGUI rendering as a host-gated implementation concern. The art plan therefore must not invent a management runtime consumer that does not yet exist.

---

## 2. Objectives and non-negotiable constraints

The pipeline must let production-quality art arrive incrementally while preserving these constraints:

1. **Art must not block backend implementation.** Simulation/domain assemblies never depend on art assets or art tooling.
2. **Art changes are presentation-only.** Replacing a portrait, badge, pitch texture, icon, stadium image, or font must not change simulation truth.
3. **The UI does not depend on final content volume.** Consumers work first with a representative production set and deliberate fallbacks.
4. **The chosen `touchline` direction is inherited, not reopened.** Art-direction work formalizes and extends it into unresolved asset families.
5. **Family style is locked before volume.** No mass generation of portraits, badges, kits, stadiums, or icon families before a representative family sample is accepted.
6. **Production assets require known rights.** Unknown provenance, redistribution rights, trademark status, or likeness status blocks release-ready use.
7. **No baked user-facing text by default.** Localization remains live UI text; reusable art is language-neutral unless an exception is explicitly approved.
8. **Editable sources and runtime exports are separate.** Authoring masters do not leak into the shipping Unity tree.
9. **Unity GUID stability matters.** Once a production export is integrated, ordinary revisions preserve its semantic identity, path, and `.meta` GUID. Necessary move/rename exceptions follow §6.4 and require explicit GUID/reference verification.
10. **Fallback is not placeholder.** Dynamic families receive intentional shippable defaults; temporary developer art never counts as release coverage.
11. **Stage-1 visual quality is a release gate.** `master-development-plan.md` requires a professional-quality, readable match presentation and explicitly rejects placeholder art at the Stage-1 quality gate.
12. **Typography is a licensed runtime dependency.** Font binaries require recorded redistribution rights and localization/script coverage.
13. **The current production contract is 2D-first.** A future 3D art pipeline requires a separately approved family recipe rather than implicit extension of these rules.
14. **No speculative runtime asset architecture.** Catalogs, Addressables, atlases, batch manifests, and importer automation are introduced only when a concrete consumer or measured repeated failure justifies them.

---

## 3. High-level plan

```text
G0  Accept grounded pipeline plan
 |
 +-------------------------+
 |                         |
 v                         v
H1A Formalize/extend   H1B Technical contract
    touchline               |
 |                         |
 +------------+------------+
              |
              v
      H2 Representative slice
              |
              v
      H3 Hardening/automation
              |
 +------------+------------+----------------+
 |            |            |                |
 v            v            v                v
UI         Match art    Club/people      Stadiums
 |            |            |                |
 +------------+------------+----------------+
              |
              v
 H5 Integration/localization/accessibility/perf
              |
              v
        H6 Release art

H7 asset operations begin once production assets exist.
```

### H0 — Governance and repository contract

Accept this plan first. After G0, AP-01 first extends the repository's Unity `.meta` helper/check scope to `Assets/GameArt/`; only after that guard is active does it create the art-source/runtime directory contract, metadata schema/example, and first tracked GameArt paths. The planning PR itself does **not** pre-land AP-01 content.

### H1A — `touchline` formalization and unresolved-family extension

Convert the already-chosen UI reference into a concise art-direction source for production assets. Extend only the visual questions the UI mockups do not settle: product identity/wordmark, 2D match markers/effects, portrait treatment, fictional club identity, stadium/background imagery, and motion/feedback principles.

### H1B — Technical pipeline contract

Prove source → export → Unity for one P0 test asset, lock metadata/provenance shape, import recipes, naming, font distribution/coverage decisions, replacement behavior, and objective validation boundaries.

H1A and H1B may run concurrently after AP-01.

### H2 — Representative production slice

Create one deliberately small production-quality kit and test it against named real/reference surfaces before any large family scales.

### H3 — Pipeline hardening and automation

Automate only stable objective rules and repeated H2 failure modes.

### H4 — Scaled family production

Scale UI, match, fictional club identity, people, stadium/environment, and typography independently behind family sample gates.

### H5 — Continuous integration and optimization

As real screens consume art, validate localization, accessibility, runtime memory/load behavior, resolution behavior, and UI composition.

### H6 — Release/marketing art

Keep storefront and press exports outside the Unity shipping tree and derive them from the actual product identity/build.

### H7 — Continuous asset operations

Support replacement, deprecation, style migration, provenance updates, regeneration, unused-asset cleanup, and GUID/reference integrity.

---

## 4. Repository/source-of-truth model to establish in AP-01

No directories below are considered implemented by this planning document. AP-01 creates them only after G0.

### 4.1 Art sources

```text
art-source/
  _templates/
  _quarantine/
  identity/
  typography/
  ui/
    icons/
    panels/
    data-viz/
  match/
    pitch/
    markers/
    events/
    effects/
  clubs/
    badges/
    kits/
  people/
    portraits/
    fallbacks/
  stadiums/
  marketing/
```

`_quarantine/` is never an export source. Unlicensed reference images should normally remain external links/notes rather than copied files.

### 4.2 Unity runtime exports

```text
Assets/GameArt/
  Identity/
  Fonts/
  UI/
    Icons/
    Panels/
    DataViz/
  Match/
    Pitch/
    Markers/
    Events/
    Effects/
  Clubs/
    Badges/
    Kits/
  People/
    Portraits/
    Fallbacks/
  Stadiums/
```

Every tracked file/folder created under `Assets/GameArt/` must receive the appropriate committed Unity `.meta` identity from the moment it is introduced. **Before the first GameArt path is created, AP-01 extends `tools/unity-ci/check-meta-integrity.sh` across all three of its current `src/`-scoped enumerations: (1) the missing-meta/ancestor walk, (2) the orphan-meta scan, and (3) duplicate-GUID detection. Missing/orphan coverage must include both `src/` and `Assets/GameArt/`; duplicate-GUID detection must be one combined cross-tree scan so a GUID collision between the two roots cannot pass.** `tools/unity-ci/generate-missing-metas.sh` may be expanded only for GameArt **folders and CI-safety fixtures**; it must not synthesize production `.meta` files for art assets such as textures, vector exports, or font binaries. Those asset metas come from an actual Unity import under AP-03. AP-01 proves the checker fails on a missing GameArt `.meta` before any real GameArt path is committed.

### 4.3 Release art

```text
release-art/
```

This root is introduced only when H6 begins. Final Steam/store/press exports do **not** live under `Assets/GameArt/`. Editable marketing masters live under `art-source/marketing/`.

### 4.4 Runtime code boundary

C# remains in `src/` through the repository's existing Unity project arrangement. `Assets/GameArt/` contains no game-domain code.

### 4.5 Existing repository mechanics

The repo already routes common textures, 3D formats, audio, video, and fonts through Git LFS and has a whole-repository large-binary guard. The existing `.meta` checker currently scopes its missing-meta walk, orphan scan, and duplicate-GUID scan to `src/`; **AP-01 extends all three, with duplicate-GUID detection performed once across the combined `src/` + `Assets/GameArt/` universe.** The generator is expanded only for GameArt folders/CI safety, not production art-asset metas; actual asset importer blocks are created by Unity during AP-03. AP-06 may harden that baseline from H2 evidence, but it is not the first line of defense.

---

## 5. Asset taxonomy and ownership

| Family | Priority | Current/near-term evidence surface | Scale begins after |
|---|---:|---|---|
| Visual identity | P0 | `touchline` UI reference + public builds | AP-02 family extension accepted |
| Typography/fonts | P0 | `tokens.css`, UI/localization | rights + script/fallback review + AP-03 |
| UI art | P0 | `docs/design/ui-mockups/`, later P5b/Wave-7 UGUI screens | AP-04 sample accepted; runtime subgate when consumer exists |
| 2D match | P0 | `src/match-client-core/`, `src/match-client-unity/` | AP-05 match integration accepted |
| Club identity | P1 | later management screens/data | H2 accepted + family sample |
| People | P1 | later squad/staff screens/data | H2 accepted + portrait sample |
| Stadium/environment | P2 | actual consuming presentation surface | H2 accepted + surface exists |
| Release/store | P2 | Steam/press | in-game identity stable + real capture path |
| Future 3D | deferred | later roadmap consumer | explicit 3D recipe/approval |

Each scaled family gets a small recipe under `docs/design/art/` stating only family-specific source format, crop/composition, export sizes, import profile, fallbacks, quality checks, and approved examples.

---

## 6. Asset identity, lifecycle, and change control

### 6.1 Three identities

Do not conflate:

1. **Art asset ID** — stable semantic production identity, e.g. `ui.icon.calendar` or `match.marker.player`.
2. **Repository path** — location of the current source/export.
3. **Unity GUID** — runtime reference identity from `.meta` after Unity integration.

### 6.2 Lifecycle

```text
candidate
  -> source-approved
  -> exported
  -> integrated
  -> validated
  -> release-ready
```

Side/terminal states:

- `rejected` — intentionally abandoned;
- `deprecated` — formerly valid, being/been removed from use;
- `quarantined` — cannot proceed because of rights/provenance/content concern.

Exploration does not require metadata until it becomes a production candidate.

### 6.3 Revision rule

For a continuing semantic asset, revise the same committed source/export path and let Git provide history. **Do not create `_v001`, `_v002`, `_final`, or `_new` copies merely to simulate source history.**

A filename suffix is permitted only when variants intentionally coexist as different semantic assets, not as revisions of one asset. Temporary tool-autosave/history files stay local/ignored rather than becoming the production source contract.

### 6.4 Replacement and deprecation

- same semantic asset, improved execution → replace bytes in place;
- genuinely different semantic variant → new asset ID and asset;
- move/rename an integrated export only when necessary and verify GUID/reference preservation;
- remove deprecated assets only after all consumers are migrated or the asset is deliberately retained as a fallback.

### 6.5 Style version

`docs/design/art/art-direction-v1.md` owns the production `style_version` after AP-02. Family recipes declare which style version they implement. A material style change names affected families and migration disposition rather than allowing silent partial drift.

---

## 7. Production metadata and provenance

### 7.1 Authoritative low-volume format

For initial production candidates, the authoritative machine-readable record is an adjacent **`.art.json` sidecar**, e.g.:

```text
art-source/ui/icons/ui_icon_calendar.svg
art-source/ui/icons/ui_icon_calendar.art.json
```

Required initial fields:

- `asset_id`;
- `family`;
- `status`;
- `style_version`;
- `target_surface`;
- `source_kind` (`original`, `licensed`, `generated`, `public-domain`);
- `creator_or_tool`;
- `license_or_rights_basis`;
- `attribution_required`;
- `real_person_or_likeness`;
- `real_club_competition_or_trademark`;
- `export_paths`;
- `notes` when an exception exists.

AP-01 creates the schema/example. There is no competing markdown provenance record.

### 7.2 Generated/AI-assisted work

When generated material becomes a production candidate, also retain enough information to audit/reproduce it when practical:

- provider/tool and model/version when known;
- prompt/workflow or stable recipe reference;
- input/reference assets and their rights status;
- material manual edits;
- generation date/batch identifier;
- commercial-use basis applicable when adopted.

Generated output is always a candidate first. Human review rejects obvious real-person likenesses, watermarks, embedded/generated text, copied marks, or visual artifacts before progression.

### 7.3 Rights gate

Unknown or ambiguous rights status blocks `release-ready`.

Until explicit rights are secured, shippable content is fictional-first:

- no real club crests;
- no real competition logos;
- no sponsor marks;
- no copied real kit designs;
- no unlicensed real-player likenesses;
- no reference media redistributed in-repo without appropriate permission.

### 7.4 High-volume families

Do not design a batch-manifest system before evidence. If portraits or another family later makes per-asset sidecars operationally expensive, H4 may introduce a versioned batch format preserving the same required semantics.

---

## 8. H1A — Formalize and extend `touchline`

AP-02 begins from the July 25 `touchline` choice. It does **not** create 2–3 competing client directions.

### 8.1 Required derivation

Create `docs/design/art/art-direction-v1.md` by mapping the existing `touchline` tokens/reference into production-art rules:

- inherited core palette/neutrals/status/data-viz colors;
- inherited spacing/radius/panel/button/HUD tendencies relevant to art assets;
- inherited typography roles and current candidate families;
- icon stroke/fill/geometry rules consistent with the analyst-tool posture;
- data-viz asset rules for cases primitives cannot express;
- match-view pitch/marker/effect style extension;
- System XI working-title identity/wordmark extension;
- portrait style/crop/light/background extension;
- fictional badge/kit language extension;
- stadium/background image treatment;
- motion/feedback principles;
- accessibility/readability rules;
- any subtle Ukrainian/Eastern-European reference language, if used, without displacing the chosen analyst-tool identity.

### 8.2 Token-change rule

If AP-02 discovers a problem in an existing `touchline` token, the art-direction document records it as an explicit proposed token change and updates the design reference in the same eventual landing. It must not quietly create a second palette/type/radius source that disagrees with `tokens.css`.

### 8.3 Evaluation

Review the derived/extended system for:

- dense football-management readability;
- distinctiveness without sacrificing analyst-tool clarity;
- match-view readability at actual marker scale;
- scalability for fictional clubs/people;
- accessibility and contrast;
- feasibility for solo/AI-assisted production;
- consistency with #38's presentation boundary and the chosen UI reference.

### Gate G1 — Art-direction formalization accepted

G1 passes when:

- `touchline` remains clearly identified as the inherited chosen direction;
- the derived document does not contradict APPROVED #38 behavior/ownership;
- unresolved art families have enough rules to judge sample conformity;
- any proposed token changes are explicit rather than duplicated sources of truth.

---

## 9. H1B — Technical pipeline contract

H1B runs in parallel with AP-02 after AP-01.

### 9.1 Lock in AP-03

- source/runtime path rules;
- source and export file formats per P0 family;
- naming and semantic asset IDs;
- `.art.json` schema/example;
- source→export mapping;
- Unity import profile for the test asset;
- font binary ownership, rights record, script/fallback handoff;
- replacement/deprecation behavior;
- Git LFS workflow;
- objective CI versus subjective review split;
- provisional vertical-slice texture sizes;
- explicit criteria for when importer automation, atlases, Addressables, or catalogs become justified.

### 9.2 Runtime naming

Unity-ready filenames use lower snake case and game meaning:

```text
ui_icon_calendar.png
ui_panel_primary_9slice.png
match_pitch_grass_base.png
match_marker_player.png
match_marker_goalkeeper.png
club_badge_fictional_0001.png
portrait_player_fictional_0001.png
stadium_bg_fictional_0001.png
```

No spaces, tool/artist names, routine revision suffixes, or baked localized copy.

### 9.3 Initial technical defaults

These are vertical-slice study defaults, not permanent budgets.

**UI**
- vector master where practical, otherwise high-resolution raster master;
- PNG runtime export with alpha when needed;
- mipmaps off unless actual use measures a need;
- 9-slice panels instead of fixed-size duplicates;
- no baked user-facing text.

**Typography**
- begin by evaluating the current `touchline` candidates rather than introducing new families without cause;
- verify and record redistribution rights before committing runtime binaries;
- localization supplies required script/language coverage and fallback requirements;
- explicitly test Ukrainian/Cyrillic coverage before shipping adoption;
- vendor approved runtime font binaries rather than depend on the mockups' network load;
- ship only needed families/weights;
- do not rasterize text as a glyph-coverage workaround.

**2D match**
- keep pitch material/texture character separate from gameplay geometry where practical;
- keep player/GK/ball/selection/event/tactical layers independent;
- do not rely on color alone for critical state;
- measure zoom/readability before fixing final export sizes.

**Portraits**
- square framing;
- 1024×1024 working target and 512×512 initial runtime study target;
- consistent eye line/head scale/light/background.

**Stadium/backgrounds**
- 16:9 study composition unless a real surface requires otherwise;
- 2560×1440 initial master/export study target;
- reserve UI-safe composition zones.

### Gate G2 — Technical contract proven

G2 passes when a P0 test asset is reproducibly taken from source through export into the actual Unity project using documented settings, with committed `.meta` identity and verified LFS behavior, and the font rights/script/fallback decision path is explicit. If Unity-host/import verification cannot be run, G2 remains pending rather than being paper-passed.

---

## 10. H2 — Representative vertical slice

### 10.1 Small asset kit

Produce only enough to validate the production system:

- System XI working-title wordmark treatment;
- approved provisional typography treatment;
- 12–20 core UI navigation/status icons;
- representative panel/separator set;
- data-viz sample;
- pitch surface/material treatment;
- home/away player marker system;
- goalkeeper variation;
- ball and restrained trail/effect treatment;
- goal/card/substitution/event markers;
- 4 representative fictional badges;
- 4 representative fictional kit/color identities;
- 8 fictional portraits plus one deliberate fallback;
- one stadium/background only if a real/reference target requires it.

This is a style/pipeline validation set, not a content library.

### 10.2 Named integration/evidence targets

#### Match path — real runtime target

The match slice targets:

- `src/match-client-core/` for the render/projection contract; and
- `src/match-client-unity/MatchClientBehaviour.cs` for the product Unity binding.

`src/match-client-web/` may be used for comparison and regression/reference output, but it cannot substitute for the final Unity-client integration claim. If Unity host verification is unavailable, the match runtime portion of G3 remains open.

#### Management/UI path — current reference plus future runtime target

There is no completed dense management UGUI screen today. Therefore:

- visual conformance is tested against the existing dense `touchline` pages in `docs/design/ui-mockups/` (for example the squad/tactics/data-heavy compositions);
- architecture/composition compatibility is checked against `src/ui-framework/` and `src/client-app/`;
- **runtime management integration is explicitly deferred** until P5b / the applicable Wave-7 screen implementation exists.

A generic presentation harness may help inspect assets, but **cannot close the runtime-management subgate**. This prevents a test harness from becoming an unfalsifiable substitute for the missing consumer.

### 10.3 H2 review lenses

**Visual:** style coherence, actual-size readability, dense-screen hierarchy, match-motion overlap/readability, professional finish.

**Technical:** reproducible export/import, no master leakage into runtime tree, stable GUIDs, no redundant color variants, sensible measured texture use.

**Rights:** complete provenance, no accidental real crest/sponsor/likeness/watermark, font redistribution rights for distributable builds.

**Accessibility/localization:** redundant cues beyond color, language-neutral reusable icons, no rasterized copy, required font/fallback scripts usable without art-side hacks.

### Gate G3 — Vertical slice acceptance

G3 is recorded as two explicit subresults:

- **G3-MATCH:** the representative match assets render correctly through the actual Unity match binding and are production-worthy in motion/context.
- **G3-UI:** the representative UI assets conform to the chosen `touchline` dense-screen reference; **runtime-management integration remains a separately recorded pending subresult until the real P5b/Wave-7 consumer exists**.

Scaling a family may rely only on the evidence relevant to that family. No document may summarize the management runtime subresult as passed before a real consumer exists.

---

## 11. H3 — Pipeline hardening and automation

Automation follows H2 evidence.

### 11.1 Mandatory objective hardening before volume

- retain the AP-01 `Assets/GameArt/` missing/orphan/duplicate-GUID checks as mandatory baseline enforcement and add only H2-discovered integrity checks that are not already covered;
- enforce allowed runtime asset roots/formats;
- enforce production naming rules;
- validate required `.art.json` completeness/status transitions needed for shipping;
- retain the whole-repo LFS/binary guard;
- prevent editable/source-only formats from entering `Assets/GameArt/` except explicitly approved Unity-native assets.

### 11.2 Conditional automation

Add only after repeated friction/errors justify it:

- Unity Presets/`AssetPostprocessor` rules;
- dimensions/max-size checks;
- alpha/import-type checks;
- sprite slicing defaults;
- authoring-tool export scripts;
- review contact sheets;
- batch provenance helpers;
- atlas/Addressables/catalog logic.

### 11.3 CI boundary

**Hard CI:** objective, stable checks such as paths, naming, metadata presence/schema, `.meta` integrity, illegal runtime formats, or LFS violations.

**Human review:** style quality, visual communication, likeness similarity, composition, clutter, and whether an asset feels coherent with `touchline`.

### Gate G4 — Hardened repeatability

A new conforming P0 asset can follow the documented recipe and CI catches known objective failure modes without routine exceptions.

---

## 12. H4 — Family scaling

### 12.1 UI

Scale in response to real/approved screens: navigation/status icons → panel/separator kit → data-viz assets where primitives are insufficient → interaction/focus states → specialized screen-driven icons. Do not build hundreds of speculative icons.

### 12.2 Match

Pitch/material → player/GK/ball → possession/selection/state cues → event marks → tactical overlays/heatmap language → restrained feedback effects. Rendering visualizes published state; it never infers or changes simulation truth.

### 12.3 Fictional club identity

Create a 12–20-club sample across several archetypes, review for accidental resemblance to real clubs, then scale via controlled shape/pattern/color/motif systems.

### 12.4 People

Before volume, lock realism/stylization, crop, eye line, lighting, background, age behavior, diversity/geographic plausibility expectations, fallback treatment, and generated-likeness rejection. Only then determine whether batch generation/metadata tooling is warranted.

### 12.5 Stadium/environment

Produce only when a real consuming surface/state exists. Do not generate combinatorial day/night/weather variants ahead of exposed game states.

### 12.6 Typography

Use the minimum required approved family/weight set; verify redistribution rights, script coverage, fallback, dense numeric/tabular readability, and cross-screen layout effects. A future font replacement is a cross-screen regression event, not a cosmetic file swap.

### Per-family G5x

Scale only when the family recipe, representative sample, rights path, objective validation, and fallback behavior (where dynamic) are accepted.

---

## 13. H5 — UI/localization/accessibility/performance integration

### 13.1 UI/UX boundary

Art owns reusable visual assets and visual-language rules. UI/UX owns layout, navigation, interaction, responsive composition, live text, and behavioral state. Do not encode layout logic into bespoke backgrounds.

### 13.2 Localization

- no baked user-facing copy by default;
- avoid letter-based icons when a language-neutral symbol works;
- preserve room for translated strings;
- localization owns supported scripts/languages and fallback requirements;
- art/UI may choose only typography that satisfies those requirements or an approved fallback chain;
- art must not prevent RTL or alternate-font handling.

### 13.3 Accessibility

- critical state uses color plus outline/shape/number or another cue where practical;
- evaluate final-screen contrast rather than source swatches in isolation;
- test representative UI/match surfaces under common color-vision deficiency simulations once those surfaces exist.

### 13.4 Performance budgets

Do not invent final texture/memory/load budgets before profiling:

1. H2 records actual dimensions/import compression/runtime memory/load behavior;
2. H3/H5 derive provisional family budgets from those measurements;
3. each high-volume family gets a per-asset and relevant loaded-set budget before bulk H4 production;
4. atlas/Addressables decisions follow measured batching/loading needs.

---

## 14. H6 — Release/marketing art

When the in-game identity is stable enough to represent truthfully:

- storefront capsules/key art;
- real-build screenshots;
- logo variants;
- press-kit exports;
- community/social release art as needed;
- Early Access/demo package.

Rules:

- final exports live under `release-art/`, not `Assets/GameArt/`;
- masters live under `art-source/marketing/`;
- re-check current platform dimensions/content requirements at release time;
- do not imply licensed clubs/players/content the build does not contain;
- apply the same rights/provenance gate.

---

## 15. H7 — Continuous asset operations

Support:

- in-place revision;
- replacement and deprecation;
- unused-export audit;
- style-version migration;
- license/attribution updates;
- regeneration of high-volume subsets;
- GUID/reference integrity checks.

A style migration classifies each affected family as compatible, opportunistic refresh, or release-blocking migration.

---

## 16. Source-control and binary collaboration policy

- one active editor/branch owns a given binary master at a time;
- coherent family/batch PRs rather than unrelated binary dumps;
- no hundreds-asset PR before the family sample gate;
- Git LFS installed for art-producing clones;
- externally sourced reference media committed only when redistribution permits;
- Git is source revision history; do not preserve routine `_v001`/`_final_final2` copies;
- binary conflict resolution chooses/regenerates a source of truth rather than pretending to line-merge binaries.

---

## 17. Definition of Ready / Done

### 17.1 Ready

A production candidate is ready when:

- it has a real consumer/reference target or belongs to an approved reusable family;
- applicable `touchline`-derived/family rules are accepted;
- rights/provenance are known;
- target use/size is known enough to select an export recipe;
- it requires no speculative runtime architecture.

### 17.2 Validated

An asset reaches `validated` only when applicable checks pass:

1. semantic purpose is correct;
2. conforms to current art-direction/family recipe;
3. readable at actual intended size/context;
4. export/import settings correct;
5. commercially acceptable provenance known;
6. no forbidden baked localized text;
7. no accidental trademark/likeness/watermark;
8. runtime dimensions/memory appropriate to measured budget;
9. deliberate fallback exists where dynamic lookup can fail;
10. `.meta`/GUID committed and valid after Unity import;
11. actual consumer renders correctly when that consumer exists;
12. fonts additionally have redistribution rights and required script/fallback verification.

`release-ready` adds release-specific rights, polish, and platform validation.

---

## 18. Implementation sequence after G0

### AP-01 — Repository contract

Only after G0, in this order:

1. **Extend all checker enumerations first.** Update `tools/unity-ci/check-meta-integrity.sh` so (a) the missing-meta/ancestor walk and (b) orphan-meta scan cover both `src/` and `Assets/GameArt/`, and (c) duplicate-GUID detection is one **cross-tree** scan over both roots. Keep `src/`'s existing root exception and define the corresponding GameArt root/folder behavior explicitly.
2. **Constrain the generator.** Extend `tools/unity-ci/generate-missing-metas.sh` only for GameArt folders and CI-safety fixtures. It must not seed production art-asset metas: textures, vectors, fonts, and other imported art assets receive their importer-bearing `.meta` files from an actual Unity import in AP-03.
3. **Prove the guard.** Before creating the real GameArt tree, use temporary tracked-path fixtures/mutations to prove the checker fails on a missing GameArt `.meta`, catches an orphan GameArt `.meta`, and catches a duplicate GUID spanning `src/` and `Assets/GameArt/`; restore each fixture and prove clean status.
4. **Then create repository roots.** Create the exact `art-source/` and `Assets/GameArt/` roots/subtrees only as needed, with required Unity **folder** `.meta` identities committed from first introduction. Do not introduce production art files until their Unity-import path is available under AP-03.
5. Add the authoritative `.art.json` schema/example.
6. Document allowed/forbidden runtime formats.
7. Add local scratch ignore only if actual tools need it.

**No production styling.** This is infrastructure only. **No tracked `Assets/GameArt/` path may predate steps 1–3.**

### AP-02 — Derive art direction from `touchline`

- inventory/map the chosen `touchline` tokens;
- publish `docs/design/art/art-direction-v1.md`;
- extend unresolved identity, match, portrait, club, stadium, motion rules;
- record any proposed token corrections explicitly;
- do not re-run a `stadium` vs `touchline` competition.

**Gate:** G1.

### AP-03 — Technical recipe proof

- one P0 test asset through source→export→Unity;
- record actual importer behavior in Unity 6000.4.9f1;
- prove `.meta`/GUID replacement behavior;
- prove LFS routing;
- lock initial import recipe;
- verify/record candidate font rights and required Cyrillic/Ukrainian coverage/fallback path.

**Gate:** G2.

### AP-04 — Representative vertical kit

Produce only §10.1.

### AP-05 — Named-surface integration/review

- match: `match-client-core` + actual `match-client-unity` binding;
- web renderer only as comparison/reference;
- UI: `touchline` dense-screen reference + `ui-framework`/`client-app` compatibility;
- runtime management subgate remains pending until P5b/Wave-7 consumer exists;
- capture actual-size screenshots/video where runnable;
- review rights/accessibility/technical quality;
- profile runtime asset use where a runtime consumer exists.

**Gate:** G3 subresults.

### AP-06 — Objective validators

- retain and re-prove the AP-01 GameArt `.meta` integrity baseline while adding any additional H2-proven meta/import integrity checks;
- production path/naming rules;
- `.art.json` validation;
- runtime/source-format guard;
- any additional H2-proven stable checks.

**Gate:** G4.

### AP-07 — UI family scale

Expand only against existing/approved near-term screens and the `touchline` source of truth.

### AP-08 — Match family scale

Expand match cues/overlays/effects only after G3-MATCH.

### AP-09 — Club identity sample → scale

Representative fictional set, resemblance review, then controlled scaling.

### AP-10 — Portrait sample → scale

Lock treatment/likeness rejection first; batch tooling only after measured need.

### AP-11 — Stadium/environment sample → scale

Only for actual consuming surfaces/states.

### AP-12 — Loading/atlas/Addressables decision

Use measured H4/H5 evidence to decide whether direct references are sufficient or a catalog/atlas/Addressables strategy is warranted.

### AP-13 — Release-art pipeline

Create `release-art/` and current storefront/capture/press recipes near the release milestone.

### AP-14 — Future 3D activation, only if required

Before production 3D content, approve a separate family recipe covering DCC/source format, coordinate/unit conventions, topology/LOD budgets, materials/textures, rigging/animation ownership where applicable, Unity import, and platform/runtime budgets.

---

## 19. Concurrency with other streams

| Art work | Can run with | Boundary |
|---|---|---|
| AP-02 touchline derivation | backend, audio, localization, UI | inherits existing UI direction; coordinates typography/scripts |
| AP-03 technical proof | backend, UI, localization | no speculative catalog/loading architecture |
| UI sample | UI/UX | mockups are visual reference; runtime pass waits for actual screen |
| Match sample | match client work | targets existing render/Unity seams; never mutates sim |
| Typography | localization + UI | rights + script/fallback are joint gates |
| Club/portrait samples | management-data work | samples can precede full data hookup; runtime lookup waits for real IDs/models |
| Stadium art | UI/client | waits for real surface/state requirement |
| Marketing art | engineering | final exports wait for representative real builds |

---

## 20. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Reopening settled visual direction | `touchline` explicitly inherited; reversal requires owner decision + source-reference update |
| Competing visual sources of truth | precedence in §1; AP-02 derives from tokens and records explicit token changes |
| Premature repository implementation | planning PR stays planning-only; AP-01 begins after G0 |
| Style drift across manual/generated assets | derived art direction + family recipes + sample gates |
| Large unusable batches | no family scale before representative acceptance |
| Trademark/likeness contamination | fictional-first + `.art.json` provenance + quarantine + human review |
| Font/network/script failure | verify redistribution, vendor approved binaries, test required scripts/fallback |
| Broken Unity references | stable paths/GUIDs + AP-01 missing/orphan coverage + one cross-tree duplicate-GUID scan + folder-only generator scope + mutation proofs before first tracked GameArt path + AP-03 Unity-generated asset metas + AP-06 evidence-driven hardening |
| Binary repo bloat | existing LFS routing/binary guard + bounded batches |
| Overengineered loading system | Addressables/catalog/atlas decision deferred to measured need |
| Fake management integration claim | named real consumers; reference mockups cannot close runtime-management subgate |
| Localization blocked by raster copy | no baked user-facing text by default |
| Match accessibility/readability | redundant color/shape/outline/number cues + motion-context review |
| Memory/load growth | H2 measurement → family budgets before volume |
| Marketing diverges from product | real-build captures and stable in-game identity |
| Future 3D distorts 2D contract | separate explicit AP-14 activation |

---

## 21. G0 acceptance checklist

**Acceptance recorded:** September 6, 2026. The owner explicitly accepted G0. AP-01 is authorized; later art-production gates remain unchanged.

G0 may be opened only when the owner accepts this grounded plan and all of these statements are true:

- `touchline` is treated as the existing chosen visual direction, not a candidate to reselect;
- APPROVED #38 and the current client code are named as architecture/integration constraints;
- the plan does not claim a management UGUI consumer already exists;
- the planning PR contains no premature AP-01 runtime/source directory implementation;
- AP-01 names all three `check-meta-integrity.sh` enumerations, requires duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan, restricts `generate-missing-metas.sh` to folders/CI safety, and requires firing proofs **before** the first tracked `Assets/GameArt/` path;
- `.art.json` is the single authoritative initial provenance/production metadata format;
- routine source revisions use Git history, not `_v001` copies;
- Stage-1 visual-quality rationale points to `master-development-plan.md`;
- the plan has a standard version history;
- landing tracking (`file-manifest.md`, `CHANGELOG.md`) is synchronized before this planning document merges;
- CI differences unrelated to this documentation-only plan are classified against `main` rather than “fixed” here.

Opening G0 authorizes **AP-01 only**, followed by AP-02 and AP-03 substantially in parallel. It does not authorize bulk asset generation.

---

## 22. Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-09-04 | Initial art-pipeline foundation: source/export split, family priorities, provenance posture, first vertical-kit concept. |
| 0.2 | 2026-09-04 | Expanded into lifecycle-driven pipeline: H0–H7 phases, semantic asset identity, replacement/deprecation, vertical slice before scale, evidence-driven automation and performance budgets. |
| 0.3 | 2026-09-04 | Added typography rights/localization path and explicit future-3D activation boundary; retained G0 implementation gate. |
| 0.4 | 2026-09-05 | Grounding remediation after external review: recognized July 25 `touchline` decision and existing `tokens.css`; made APPROVED #38 and actual client trees explicit; changed H1A/AP-02 from direction selection to `touchline` derivation/extension; narrowed typography work to rights/offline distribution/script coverage; named match/UI integration evidence and kept nonexistent management UGUI integration pending; made `.art.json` authoritative; prohibited routine source revision suffixes; corrected Stage-1 visual-quality source to the Master Development Plan; added standard version history and G0 close-out checklist. |
| 0.5 | 2026-09-05 | External follow-up: moved GameArt `.meta` checker/generator scope expansion from AP-06 to the **first action of AP-01**, before any tracked GameArt path; required a mutation/firing proof; retained only evidence-driven hardening in AP-06; removed the `where practical` hedge from integrated-asset identity preservation and routed legitimate move/rename exceptions through §6.4. |
| 0.6 | 2026-09-06 | AP-01 execution refinement after external review: named all three checker enumerations; required duplicate-GUID detection as one cross-tree `src/` + `Assets/GameArt/` scan; restricted `generate-missing-metas.sh` to folders/CI safety rather than production art assets; assigned importer-bearing art-asset metas to actual Unity import in AP-03; expanded mutation proof to missing, orphan, and cross-tree duplicate cases. |
| 0.7 | 2026-09-06 | **G0 accepted by owner.** Planning gate closed successfully and AP-01 repository-contract implementation authorized. No bulk asset generation or later family-scale gate was opened. |