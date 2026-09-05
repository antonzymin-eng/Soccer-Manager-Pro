# System XI — Art Pipeline Plan

**Status:** PLANNING — IMPLEMENTATION GATED  
**Started:** September 4, 2026  
**Plan revision:** v0.2  
**Implementation gate:** CLOSED until this plan is accepted.  
**Purpose:** Define a production-grade art pipeline that can run in parallel with simulation, UI/UX, localization, audio, and management-layer development without creating asset debt, rights risk, or presentation-layer coupling.

---

## 0. Planning method and review history

The art stream deliberately begins with the pipeline plan rather than asset volume. The first foundation draft was reviewed as if it were about to govern several years of production.

### High-level pass 1 — initial A0→A4 sequence

Initial structure:

1. foundation;
2. visual language;
3. first vertical asset kit;
4. scalable content production;
5. marketing/release art.

#### Critique 1

The sequence was directionally correct but still behaved like an art backlog rather than a pipeline specification. It did not sufficiently define:

- asset lifecycle and approval states;
- stable identity versus file paths and Unity GUIDs;
- replacement, deprecation, and migration behavior;
- measurable technical/performance budgets;
- source-control ownership for binary masters that cannot merge cleanly;
- a reproducible source→export→Unity path;
- where rights/provenance become blocking rather than advisory;
- exactly when automation becomes justified;
- the boundary between shipped game art and store/marketing art.

### High-level pass 2 — pipeline phases plus technical contract

The plan was revised to add explicit technical-contract, QA, lifecycle, provenance, and automation phases.

#### Critique 2

A second weakness remained: it was too linear. Art direction and technical pipeline design do not need to wait on each other after the small foundation is set. Conversely, automation should not be fully designed before a representative vertical slice reveals which manual steps are actually repetitive and error-prone. Marketing exports also should not live in the Unity shipping tree.

### High-level pass 3 — accepted structure for detailed planning

The settled high-level structure is therefore:

- **H0 — Governance & pipeline foundation**
- **H1A — Visual language / art direction** and **H1B — Technical pipeline contract**, in parallel
- **H2 — Representative vertical slice**, dependent on H1A + H1B
- **H3 — Pipeline hardening & automation**, derived from H2 evidence
- **H4 — Scaled family production**, parallel by family after family-specific approval
- **H5 — Experience integration, accessibility, localization, and optimization**, continuous with H4
- **H6 — Release/marketing art**, outside the Unity shipping tree
- **H7 — Continuous asset operations**, replacement/deprecation/provenance/style maintenance

#### Critique 3

This structure resolves the sequencing problem but could still become too bureaucratic for a solo developer. The final plan therefore applies four controls:

1. subjective art quality stays human-reviewed rather than pretending to be machine-verifiable;
2. automation is added only after a rule is stable or a repeated failure is observed;
3. low-volume hand-authored assets use lightweight per-asset metadata, while high-volume families may move to batch manifests only when needed;
4. runtime catalogs/addressables/atlas systems are not introduced until an actual consumer and measured loading requirement justify them.

No further high-level structural defect was found. The remainder of this document is the detailed implementation plan built on that structure.

---

## 1. Objectives and non-negotiable constraints

The pipeline must let production-quality art arrive incrementally while preserving these constraints:

1. **Art must not block backend implementation.** Simulation/domain assemblies never depend on art assets or art tooling.
2. **Art changes must remain presentation-only.** Replacing a portrait, badge, pitch texture, icon, or stadium image must not change match outcomes or management simulation state.
3. **UI must not depend on final content volume.** Screens work first with a small representative production set and intentional fallbacks.
4. **Style is locked before volume.** No mass generation of portraits, badges, kits, stadiums, or icon families before their family style is approved.
5. **Production assets require known rights.** Unknown provenance or unresolved trademark/likeness status blocks shipping use.
6. **No baked user-facing text by default.** Localization remains live UI text; art provides non-linguistic visual assets.
7. **Source and runtime exports are separate.** Editable masters do not leak into the shipping asset tree.
8. **Unity GUID stability matters.** Once an exported production asset is integrated, ordinary revisions replace the file in place rather than churn its path/GUID.
9. **Fallback is not placeholder.** Dynamic content families must have deliberate, shippable default art; temporary developer placeholder art is never mistaken for release coverage.
10. **Stage-1 visual quality is a release gate.** The roadmap requires a professional 2D match presentation and rejects placeholder-quality visuals.

---

## 2. High-level dependency model

```text
H0 Governance / foundation
       |
       +---------------------+
       |                     |
       v                     v
H1A Visual language     H1B Technical contract
       |                     |
       +----------+----------+
                  |
                  v
          H2 Vertical slice
                  |
                  v
      H3 Hardening / automation
                  |
      +-----------+-----------+----------------+
      |           |           |                |
      v           v           v                v
   UI family   Match art   Club/people      Stadiums
      \           |           /                /
       +----------+----------+----------------+
                  |
                  v
       H5 Integration / optimization
                  |
                  v
          H6 Release art

H7 Operations runs continuously after the first production assets exist.
```

H1A and H1B may proceed concurrently. H2 cannot begin until both are sufficiently locked. H4 family streams may proceed concurrently after H3 and the applicable family style gate.

---

## 3. Repository and source-of-truth model

### 3.1 Root responsibilities

- `docs/design/art/` — approved visual-language documentation, family recipes, review checklists, and art decisions.
- `art-source/` — editable production masters, templates, production metadata, generation records, and legally usable source material.
- `Assets/GameArt/` — Unity-ready shipping exports plus Unity materials/prefabs/import data.
- `release-art/` — final non-runtime store/press/release exports. These do **not** ship in the game build.

`release-art/` is introduced only when H6 begins; it is not needed during the initial pipeline slices.

### 3.2 Planned source tree

```text
art-source/
  _templates/
  _quarantine/
  identity/
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

`_quarantine/` contains material that may be useful for investigation but is **not cleared for production**. It must never export into `Assets/GameArt/`.

A local-only scratch/work directory may be added and ignored once the chosen authoring tools make that useful. Tool-specific scratch formats are not committed merely because a tool generates them.

### 3.3 Planned Unity export tree

```text
Assets/GameArt/
  Identity/
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

Marketing/store images remain outside this tree.

### 3.4 Existing repository constraints

The repository already routes common texture/model/audio/font formats through Git LFS and has a whole-repository binary-size guard. The current Unity `.meta` integrity gate, however, is scoped to `src/`; H3 extends equivalent integrity checks to `Assets/GameArt/` before production volume grows.

Runtime C# remains in `src/`; art work does not move game code into `Assets/GameArt/`.

---

## 4. Asset taxonomy and ownership

| Family | Priority | Near-term consumer | Production starts after |
|---|---:|---|---|
| Visual identity | P0 | UI, demos, public builds | H1A style lock |
| UI art | P0 | UI framework/screens | H1A + H1B |
| 2D match view | P0 | Stage-1 tactical demo | H1A + H1B |
| Club/competition identity | P1 | squad/season/competition screens | H2 accepted + family sample |
| People | P1 | squad/staff screens | H2 accepted + portrait treatment lock |
| Stadium/environment | P2 | club/home/match presentation | H2 accepted + background treatment lock |
| Release/store | P2 | Steam/other storefronts, press | in-game identity stable + real capture pipeline |

Each family gets a **family recipe** under `docs/design/art/` before batch production. A recipe contains only the rules specific to that family: master format, crop/composition, export sizes, import profile, fallbacks, quality checks, and approved style examples.

---

## 5. Asset identity, lifecycle, and change control

### 5.1 Three identities must not be confused

1. **Art asset ID** — semantic identity used in production metadata, e.g. `ui.icon.calendar` or `match.marker.player`.
2. **Repository path** — where the current source/export lives; paths may occasionally migrate.
3. **Unity GUID** — the Unity reference identity created by `.meta`; once integrated, ordinary revisions preserve it.

The art asset ID survives source-tool changes and is not derived from a creator or generation tool.

### 5.2 Lifecycle

Tracked production candidates use these states:

```text
candidate
  -> source-approved
  -> exported
  -> integrated
  -> validated
  -> release-ready
```

Terminal/side states:

- `rejected` — candidate intentionally abandoned;
- `deprecated` — formerly valid asset no longer used;
- `quarantined` — rights/provenance or content concern prevents production use.

Exploratory sketches do not need lifecycle metadata until they become production candidates.

### 5.3 Replacement rule

For an existing production asset:

- improve/iterate the existing semantic asset → replace source and exported bytes **in place**;
- create a genuinely new semantic variant → new art asset ID and new Unity asset;
- rename/move an integrated Unity asset only when necessary and verify references/GUID preservation;
- never duplicate a file solely to simulate version history; Git provides history.

### 5.4 Style version

The approved visual language has an explicit `style_version`. Family recipes declare the style version they implement. A major style change triggers a migration audit of affected families rather than silent partial drift.

---

## 6. Production metadata and provenance

### 6.1 Initial low-volume record

During H2/H3, each low-volume production candidate has a lightweight adjacent JSON record, for example:

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

### 6.2 Generated assets

For AI-assisted production, additionally retain enough information to audit and reproduce the result when practical:

- provider/tool and model/version when known;
- prompt/workflow or a stable production recipe reference;
- reference/input assets used and their rights status;
- material manual edits;
- generation date/batch identifier.

Raw generation output is a candidate, never automatically production-ready.

### 6.3 High-volume families

Do **not** design a complex batch-manifest system now. If portraits or another family grows large enough that per-asset sidecars become operationally expensive, H4 may introduce a versioned batch manifest after real workflow measurements. The common required fields remain the same.

### 6.4 Rights gate

Unknown or ambiguous rights status is a hard block on `release-ready`.

Until explicit rights are secured, content is fictional-first:

- no real club crests or competition logos;
- no sponsor marks;
- no copied real kit designs;
- no unlicensed real-player likenesses;
- no externally sourced reference image redistributed in the repo unless its license permits that use.

Reference research may be stored as links/notes rather than copied images.

---

## 7. H0 — Governance & foundation

### Deliverables

- final accepted pipeline plan;
- source/export boundary;
- family taxonomy;
- asset lifecycle;
- semantic asset-ID rule;
- provenance/rights policy;
- branch/binary ownership policy;
- implementation sequence and gates.

### Gate G0 — Pipeline plan accepted

G0 passes when:

- the plan is explicit enough to implement without inventing major policy during production;
- no substantial art volume has been produced prematurely;
- runtime architecture remains unaffected;
- remaining unknowns are deliberately deferred to measured future gates rather than accidentally omitted.

---

## 8. H1A — Visual language / art direction

H1A is an exploration-and-decision phase, not volume production.

### 8.1 Required studies

Develop 2–3 coherent direction boards covering the same representative surfaces so comparisons are meaningful. Each direction must demonstrate:

- System XI working-title wordmark treatment;
- core palette and neutrals;
- typography hierarchy and numeric/data presentation;
- panel geometry, separators, strokes, shadows, corner language;
- icon geometry/stroke/fill language;
- data visualization treatment;
- portrait treatment;
- badge/kit treatment;
- stadium/background image treatment;
- 2D match pitch and marker treatment;
- motion/feedback principles;
- dark/light/background contrast posture;
- subtle Ukrainian/Eastern-European visual references if used, without reducing the brand to national motifs.

### 8.2 Evaluation dimensions

Each direction is scored/reviewed for:

- football-management readability;
- distinctiveness versus direct competitors;
- suitability for dense data screens;
- match-view readability at small scale;
- feasibility for a solo/AI-assisted production pipeline;
- ability to scale across thousands of fictional people/clubs;
- accessibility and contrast;
- suitability for marketing/key art;
- consistency with the UI/UX direction.

### 8.3 Output

`docs/design/art/art-direction-v1.md` plus approved reference boards/assets.

### Gate G1 — Style lock

G1 passes only when one direction is selected and the rules are specific enough that two independently created assets can be judged as conforming or non-conforming.

Mass production remains blocked before G1.

---

## 9. H1B — Technical pipeline contract

H1B runs in parallel with H1A.

### 9.1 Decisions to lock

- repository path rules;
- source master versus runtime export formats;
- naming rules;
- semantic art asset IDs;
- source/export mapping;
- metadata/provenance format;
- Unity import profiles by family;
- replacement/deprecation behavior;
- binary/LFS workflow;
- static and Unity-side validation split;
- provisional texture-size targets for the vertical slice;
- rules for when atlases/addressables/catalogs may be introduced.

### 9.2 Naming

Unity-ready filenames use lower snake case and describe game meaning:

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

Rules:

- no spaces;
- no creator/tool names;
- no routine `_final`, `_new`, `_v2` naming in Unity exports;
- source working revisions may use revision suffixes if the authoring workflow requires them;
- team-color variants are not duplicated when runtime tinting can correctly express them;
- baked localized text is prohibited unless explicitly approved.

### 9.3 Initial export defaults

These are **vertical-slice defaults**, not permanent budgets.

**UI icons/panels**
- vector master where practical or high-resolution raster master;
- PNG runtime export with alpha when needed;
- mipmaps off unless measured use requires them;
- 9-slice panels preferred to fixed-size duplicates;
- no text baked into panels/icons.

**2D match art**
- separate pitch texture/material character from gameplay geometry/markings where practical;
- player, goalkeeper, ball, selection, event and tactical overlay layers remain independent;
- marker design must remain readable without relying on color alone;
- zoom behavior is tested before choosing final master/export size.

**Portraits**
- square framing;
- 1024×1024 working master target;
- 512×512 initial runtime export target;
- consistent eye line, head scale, lighting and background treatment.

**Stadium/backgrounds**
- 16:9 working composition unless a real surface specifies otherwise;
- initial 2560×1440 master/export study;
- reserve text-safe zones where UI overlays are expected.

### Gate G2 — Technical contract lock

G2 passes when one asset in each P0 family can be taken from source to Unity using documented steps without inventing settings ad hoc.

---

## 10. H2 — Representative vertical slice

The first production work is deliberately small and complete.

### 10.1 Asset kit

- System XI working-title wordmark treatment;
- 12–20 core navigation/status icons;
- representative panel/background/separator set;
- data-viz sample treatment;
- 2D pitch surface/material treatment;
- home/away player marker system using tint/shape/outline rules;
- goalkeeper variation;
- ball and trail/effect treatment;
- goal/card/substitution/event markers;
- 4 representative fictional club badges;
- 4 representative fictional kit/color identities;
- 8 representative fictional player portraits;
- one deliberate fallback portrait/silhouette;
- one representative stadium/background if the target screen is ready.

### 10.2 Integration surfaces

H2 must prove the kit on real presentation surfaces, not in an isolated asset viewer only:

- at least one dense management/UI screen or representative UI composition;
- the live/interpolated 2D match presentation surface;
- one dynamic-content example proving fallback behavior.

If a specific UI surface does not yet exist, a temporary **presentation harness** may be used, but it must exercise the same import/render path and cannot become a parallel UI architecture.

### 10.3 H2 review lenses

**Visual**
- coherent style;
- readable hierarchy;
- professional appearance;
- assets remain clear at actual render size;
- match markers readable in motion and under overlap.

**Technical**
- documented exports reproduce the integrated result;
- no source masters leak into the shipping tree;
- Unity GUIDs are stable;
- no unnecessary duplicated team/color variants;
- runtime texture dimensions are reasonable for observed use.

**Rights**
- 100% of production candidates have a known rights/provenance basis;
- no accidental real crest, sponsor, logo, likeness, watermark, or generated text artifact.

**Accessibility/localization**
- state is not represented by color alone where shape/outline can provide a secondary cue;
- icons do not depend on English letters where a language-neutral symbol is appropriate;
- no user-facing text is rasterized into reusable art.

### Gate G3 — Vertical slice accepted

G3 passes only when the assets look production-worthy **in the actual game context** and the source→export→import workflow is repeatable. If either visual quality or pipeline repeatability fails, revise H1A/H1B before scaling.

---

## 11. H3 — Pipeline hardening and automation

Automation is built from observed H2 friction.

### 11.1 Mandatory hardening after H2

Regardless of specific H2 findings:

- extend `.meta` missing/orphan/duplicate-GUID validation to `Assets/GameArt/`;
- enforce allowed runtime asset locations;
- enforce naming conventions for controlled production families;
- validate required provenance metadata for production assets;
- verify large binaries remain LFS-designated;
- define an explicit mechanism to prevent source-only masters from entering `Assets/GameArt/`.

### 11.2 Conditional automation

Add only when repeated manual settings/errors justify it:

- Unity `Preset`/`AssetPostprocessor` import rules;
- PNG dimension validation;
- family-specific max-size checks;
- alpha/import-type checks;
- automatic sprite slicing/import defaults;
- export scripts from chosen authoring tools;
- generated contact sheets for review;
- batch provenance helpers;
- atlas/addressable build logic.

### 11.3 CI philosophy

**Hard CI gates:** objective, stable, low-false-positive rules such as missing metadata, invalid paths, missing `.meta`, duplicate GUID, illegal source format in runtime tree, or un-LFS'd large binaries.

**Review checklist:** subjective rules such as style quality, composition, likeness similarity, visual clutter, or whether an icon communicates clearly.

Do not convert subjective judgment into brittle CI merely to claim automation.

### Gate G4 — Hardened repeatability

G4 passes when a new conforming P0 asset can be added by following the documented recipe and CI reliably catches the known objective failure modes without requiring routine exceptions.

---

## 12. H4 — Scaled family production

Production scales by family, not through one giant content batch.

### 12.1 UI family

Order:

1. navigation/status icons;
2. panel/separator/background kit;
3. data-viz marks and chart textures where primitives are insufficient;
4. interaction states and focus/selection treatment;
5. secondary/specialized icon sets driven by real screens.

Avoid hundreds of speculative icons for screens whose data/commands do not yet exist.

### 12.2 Match family

Order:

1. pitch/material language;
2. player/goalkeeper/ball markers;
3. selection/possession/state feedback;
4. event marks;
5. tactical overlays/heatmap visual treatment;
6. restrained effects/celebration feedback where they improve readability.

Match art must preserve the simulation/view separation: rendering may visualize state but never infer or modify simulation truth.

### 12.3 Club identity family

Before scale, create a 12–20-club style sample spanning different visual archetypes. Review for unintended resemblance to real clubs. Then template/generate larger fictional sets.

Badge and kit systems should maximize identity through controlled shape, pattern, color and motif systems rather than one-off manual complexity.

### 12.4 People family

Before scale, lock:

- realism/stylization level;
- framing and crop;
- lighting;
- background;
- age-range behavior;
- diversity and geographic plausibility expectations;
- fallback treatment;
- generated-likeness rejection process.

Produce a representative batch before defining the final high-volume manifest/generation workflow.

### 12.5 Stadium/environment family

Begin only when a real screen/presentation surface consumes it. Establish composition safe zones, day/night/weather variants only where the game actually exposes those states, and avoid combinatorial variant generation ahead of demand.

### Per-family gate G5x

Each family scales only after:

- its family recipe is approved;
- a representative sample is integrated;
- rights/provenance handling works;
- objective validators cover the stable failure modes;
- fallback behavior is defined if the family is dynamic.

---

## 13. H5 — Experience integration, accessibility, localization, and optimization

H5 runs continuously as real UI/match surfaces consume art.

### 13.1 UI/UX boundary

Art owns visual assets and visual-language rules. UI/UX owns layout, interaction, navigation, live text, responsiveness, and behavioral state. Art does not encode layout logic into bespoke background images.

### 13.2 Localization boundary

- no baked user-facing copy by default;
- icons must not rely on culturally narrow text abbreviations where a neutral symbol is available;
- backgrounds leave flexible space for longer translated strings;
- RTL and font concerns are solved by UI/localization, but art must not prevent them.

### 13.3 Accessibility

- key state changes need more than color alone where practical;
- match home/away/selection state uses combinations of color, outline, shape, number, or marker treatment;
- visual hierarchy is checked under common color-vision deficiency simulations when the relevant screens exist;
- contrast is measured at the final UI layer, not assumed from source files in isolation.

### 13.4 Performance and memory budgets

Do not invent final budgets before H2 profiling. Instead:

1. H2 records actual texture dimensions, import compression, runtime memory and load behavior for the representative kit;
2. H3/H5 turn those measurements into provisional family budgets;
3. before H4 large-scale production, each high-volume family has a documented per-asset and per-screen/load-set budget;
4. atlas/addressables decisions are made from measured batching/loading needs, not fashion.

Any later platform-specific compression/profile is validated on the target build platform.

### 13.5 Resolution testing

At minimum, art must be reviewed at the actual target UI scale and a representative low/high desktop resolution before family scale. Wider platform/resolution coverage grows with the release target matrix.

---

## 14. H6 — Release and marketing art

Release art starts after the in-game visual identity is stable enough that marketing can truthfully represent the product.

### Outputs

- storefront capsules/key art;
- screenshots captured from real builds;
- logo variants;
- press-kit exports;
- social/community announcement art where needed;
- Early Access/demo visual package.

### Rules

- final exports live under `release-art/`, not `Assets/GameArt/`;
- editable marketing masters remain under `art-source/marketing/`;
- platform/store image dimensions and content rules are re-verified against current platform documentation at the time of release work;
- screenshots/key art must not imply licensed clubs/players/content the game does not actually include;
- external-facing art goes through the same provenance/rights gate.

### Gate G6 — Release-art ready

All required store/press assets are current, rights-safe, technically valid for the target platform, and visually consistent with the actual build.

---

## 15. H7 — Continuous asset operations

Once production assets exist, the pipeline must support maintenance rather than just creation.

### 15.1 Routine operations

- revise an asset in place;
- replace a rejected/weak asset;
- deprecate obsolete assets;
- audit unused exports;
- migrate a family to a new style version;
- update licensing/attribution records;
- regenerate high-volume family subsets;
- verify no orphaned Unity references or GUID collisions.

### 15.2 Deprecation

A deprecated asset is removed only after consumers are migrated. The PR removing it must distinguish:

- no longer referenced;
- deliberately retained fallback;
- replaced by another semantic asset.

### 15.3 Style migrations

A style-version change must name affected families and define whether assets are:

- compatible as-is;
- scheduled for opportunistic refresh;
- required to migrate before release.

This prevents half-migrated visual systems from becoming permanent.

---

## 16. Source control and binary collaboration policy

Binary master files do not merge reliably. Treat ownership accordingly.

### Rules

- one active editor/branch owns a given binary master at a time;
- art PRs are organized by coherent family/batch rather than unrelated asset dumps;
- do not generate hundreds of binary assets in one PR before the family pipeline has passed its sample gate;
- Git LFS must be installed for local art-production clones;
- large source masters and runtime binaries use the repository's LFS routing;
- externally sourced reference media is not committed unless redistribution is permitted;
- ordinary revisions replace bytes rather than create `_final_final2` copies;
- conflict resolution for binary masters means selecting/regenerating a source of truth, not attempting line-style merge semantics.

---

## 17. Definition of Ready and Definition of Done

### 17.1 Asset Definition of Ready

A production candidate is ready for implementation when:

- it has a real consumer/surface or belongs to an approved reusable family;
- the applicable family style is locked;
- source rights/provenance are known;
- target size/use is known well enough to choose an export recipe;
- it does not require speculative runtime architecture.

### 17.2 Asset Definition of Done

An asset is `validated` only when applicable checks pass:

1. correct semantic purpose;
2. conforms to current style/family recipe;
3. readable at real in-game size;
4. correct export/import settings;
5. known commercially acceptable provenance;
6. no forbidden baked localized text;
7. no accidental real trademark/likeness/watermark;
8. appropriate runtime dimensions/memory for the measured budget;
9. deliberate fallback exists where dynamic lookup can fail;
10. Unity `.meta`/GUID is committed and valid;
11. actual consumer renders it correctly.

`release-ready` adds release-specific rights, polish, and platform validation.

---

## 18. Implementation slices after G0 approval

No substantive asset production begins before the planning gate is explicitly opened.

### AP-01 — Repository contract

- create final art/design directory skeletons as needed;
- add local scratch ignore only if chosen tools need it;
- formalize source/export path rules;
- add initial per-asset metadata schema/example;
- document allowed/forbidden runtime formats.

**Output:** one minimal test asset can be represented without production styling.

### AP-02 — Art-direction exploration

- develop 2–3 visual directions on identical representative surfaces;
- compare using §8 evaluation dimensions;
- select one direction;
- publish `art-direction-v1.md` and family starter rules.

**Gate:** G1.

### AP-03 — Technical recipe proof

- take one P0 test asset through source→export→Unity;
- record actual importer behavior in Unity 6000.4.9f1;
- lock initial import recipe;
- verify `.meta`/GUID replacement behavior;
- verify LFS routing.

**Gate:** G2.

### AP-04 — Vertical asset kit

Produce only the H2 representative kit.

### AP-05 — Real-surface integration and review

- integrate the kit into real UI/match presentation surfaces;
- capture target-size screenshots/video;
- run visual, accessibility, rights and technical review;
- measure asset memory/load behavior;
- revise H1A/H1B rules where evidence requires it.

**Gate:** G3.

### AP-06 — Objective validators

- extend `.meta` integrity to `Assets/GameArt/`;
- add production-path/naming validation;
- add provenance completeness validation;
- add source-format/runtime-tree guard;
- add any H2-proven repeated technical checks.

**Gate:** G4.

### AP-07 — UI family scale

Expand only against screens that exist or have approved near-term designs.

### AP-08 — Match family scale

Expand match feedback/overlays while preserving readability and observer-neutrality.

### AP-09 — Club identity sample → scale

Establish a representative fictional set, then template/generate broader coverage after approval.

### AP-10 — Portrait sample → scale

Lock portrait treatment, reject likeness drift, then introduce high-volume generation/manifest tooling only if required.

### AP-11 — Stadium/environment sample → scale

Produce only for actual consuming surfaces and actual exposed states.

### AP-12 — Loading/atlas/addressable decision

Use measured H4/H5 data to decide whether current direct references are sufficient or a catalog/Addressables/atlas strategy is required. This is deliberately **not** an AP-01 architectural assumption.

### AP-13 — Release-art pipeline

Establish `release-art/`, current storefront export recipes, screenshot capture conventions and press-kit packaging when the release milestone approaches.

---

## 19. Concurrency with other project streams

| Art work | Can run concurrently with | Dependency/boundary |
|---|---|---|
| H1A art direction | backend, audio, localization, UI framework | coordinate visual tokens with UI/UX |
| H1B technical contract | backend, UI, localization | no runtime art catalog before consumer need |
| H2 UI asset kit | UI implementation | target surfaces must be stable enough to test real sizes |
| H2 match art | match viewer/rendering | never change sim state or invent domain truth |
| Club/portrait production | management data implementation | can generate samples early; full lookup integration waits on real IDs/content model |
| Stadium art | management/UI | wait for consuming surfaces/state requirements |
| Marketing art | all engineering | final exports wait for representative real build visuals |

The art stream can therefore progress continuously without becoming a dependency for simulation architecture.

---

## 20. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Style drift across AI/manual production | style lock + family recipes + representative sample gates |
| Hundreds of unusable assets generated early | no scale before G3/G5x |
| Real-club/player/trademark contamination | fictional-first + provenance + quarantine + human review |
| Binary repo bloat | existing LFS routing + binary guard + small coherent batches |
| Broken Unity references from file churn | stable export paths + committed `.meta` + extended GameArt GUID checks |
| Overengineered asset system | no catalog/addressables/batch manifest until measured consumer need |
| UI layouts coupled to art | art supplies reusable assets; UI owns layout/live text |
| Localization blocked by raster text | no baked user-facing text by default |
| Poor accessibility in match markers | color + outline/shape/number redundancy |
| Memory/load blow-up when content scales | H2 measurement → family budgets before H4 volume |
| Marketing diverges from product | release art derived from stable in-game identity and real build captures |
| AI-generated inconsistency/likeness artifacts | controlled recipes, sample review, manual selection/correction, provenance |

---

## 21. Plan readiness review

### Detailed-plan critique pass 1

The first detailed expansion risked hard-coding speculative technical choices—especially atlas/addressable architecture, final texture budgets, and high-volume metadata structure—before any production asset had been profiled. Those decisions were moved behind H2/H4 evidence gates.

### Detailed-plan critique pass 2

The plan still treated all asset families too uniformly. Portraits, icons, stadium backgrounds and match markers have different scaling and metadata economics. The revision introduced family recipes and allows high-volume batch tooling only where a family demonstrates the need.

### Detailed-plan critique pass 3

The remaining failure mode was process weight: too many records could slow a solo developer more than they protect the project. The final plan therefore tracks metadata only for production candidates, keeps subjective checks in review rather than CI, and limits hard automation to objective stable rules.

### Final assessment

The plan is implementation-ready once G0 is explicitly approved. It now specifies:

- what is produced and in what order;
- what can run concurrently;
- source and runtime ownership;
- lifecycle and stable identity;
- rights/provenance handling;
- technical export/import rules and when they become fixed;
- real-game vertical-slice validation before scale;
- objective CI versus subjective review boundaries;
- family-specific scaling;
- integration with UI/localization/accessibility;
- performance-budget derivation;
- release-art separation;
- maintenance/deprecation behavior;
- concrete implementation slices AP-01 through AP-13.

No substantial art-production work should proceed until the user accepts G0. After acceptance, **AP-01, AP-02 and AP-03 are the next work**, with AP-02 art-direction exploration and AP-03 technical recipe proof able to proceed substantially in parallel after the minimal AP-01 contract is established.
