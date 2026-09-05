# System XI — Art Pipeline Foundation

**Status:** ACTIVE FOUNDATION  
**Started:** September 4, 2026  
**Purpose:** Establish an art-production pipeline that can run in parallel with simulation, management-layer, and UI development without creating asset debt or coupling art production to unfinished game systems.

## 1. Pipeline objective

The art pipeline must deliver production-quality visual assets incrementally while preserving three constraints:

1. **Art work must not block backend implementation.** Assets bind through stable presentation seams and may be replaced without changing simulation behavior.
2. **UI development must not depend on final content volume.** The client should work with a small representative asset set first, then scale through registries/catalogs rather than bespoke references.
3. **Stage-1 visual quality is a release gate.** The master development plan requires a professional-quality 2D match presentation and explicitly rejects placeholder art at the Stage-1 quality gate. Art therefore starts before the rest of Stage 1 is complete rather than arriving as a final polish pass.

## 2. Source-of-truth split

Two roots are used deliberately:

- `art-source/` — editable production sources, references, working files, provenance notes, and export instructions. Unity does not consume this tree directly.
- `Assets/GameArt/` — Unity-ready exported assets and Unity-side materials/prefabs/import configuration.

Source files and final binaries covered by `.gitattributes` use Git LFS. Unity `.meta` files for imported production assets are committed so GUIDs remain stable.

Do not place game C# under `Assets/GameArt/`; runtime source code remains in `src/` through the repository's existing `Assets/Scripts` junction/symlink arrangement.

## 3. Asset families

The pipeline is divided into independent families so production can progress even when some game systems are unfinished.

| Family | Near-term priority | Initial use | Examples |
|---|---:|---|---|
| Visual identity | P0 | UI + public-facing builds | wordmark, logo treatment, palette, typography rules, key motifs |
| UI art | P0 | UI framework/screens | icons, panels, separators, buttons, status marks, data-viz textures |
| 2D match view | P0 | Stage-1 tactical demo | pitch surface, grass detail, player markers, ball, trails, event/effect marks |
| Club/competition identity | P1 | management screens | fictional badges, kits, competition marks, flags/region marks |
| People | P1 | squad/staff screens | generated or illustrated player/staff portraits, silhouettes, fallback portraits |
| Stadium/environment | P2 | club/home/match presentation | stadium backdrops, tunnel/stand imagery, weather/environment layers |
| Marketing/store | P2 | Steam demo/Early Access | capsules, screenshots, key art, logo variants, press kit |

The first production slice is intentionally **visual identity + UI art + 2D match view**. These are already useful to UI work and directly satisfy the next visual quality gate. Large portrait, stadium, badge, and kit libraries come after the style is stable.

## 4. Art-direction sequence

### A0 — Foundation (current slice)

- establish source/export folder boundaries;
- establish naming/versioning rules;
- establish provenance/licensing requirements;
- define import/export defaults;
- define acceptance checks;
- create the first asset backlog.

### A1 — Visual language

Produce a compact art-direction sheet before mass asset generation:

- primary/secondary palette;
- typography hierarchy and numeric/data treatment;
- panel geometry, corner/radius rules, borders and separators;
- icon style and stroke/shape rules;
- image treatment for portraits/stadiums;
- match-view visual language;
- motion/feedback principles;
- light/dark/background contrast rules;
- minimum accessibility contrast targets.

No large batch of portraits, badges, stadiums, or icons should be generated before A1 is accepted. Style drift becomes expensive once hundreds of assets exist.

### A2 — First vertical asset kit

Build one small, complete production set that lets UI and match-view work proceed with non-placeholder assets:

- System XI working-title wordmark treatment;
- 12–20 core navigation/status icons;
- panel/background/separator set;
- 2D pitch surface treatment;
- home/away player marker system with shirt number/readability treatment;
- goalkeeper variation;
- ball and trail/effect treatment;
- goal/card/substitution/event markers;
- 4 representative fictional club badges;
- 4 representative fictional kit/color sets;
- 8 representative player portraits plus fallback silhouette.

This is a **style-validation set**, not the final content library.

### A3 — Scalable content production

After A2 is accepted, scale each family independently using controlled templates and manifests:

- badge generator/template system;
- kit template system;
- portrait production/generation workflow;
- stadium/background workflow;
- competition/region marks;
- expanded icon library.

### A4 — Marketing/release art

Create store assets only after the in-game visual identity is stable so the store page reflects the actual product.

## 5. Naming and versioning

Unity-ready files use lower snake case and encode their family and purpose:

```text
ui_icon_calendar.png
ui_icon_finances.png
ui_panel_primary_9slice.png
match_pitch_grass_base.png
match_marker_player_home.png
match_marker_goalkeeper_away.png
club_badge_fictional_0001.png
portrait_player_fictional_0001.png
stadium_bg_fictional_0001.png
```

Rules:

- Do not put version numbers in the final Unity filename unless multiple variants intentionally coexist.
- Working-source revisions may use `_v001`, `_v002`, etc. under `art-source/`.
- Stable identity is carried by the Unity asset GUID, not by renaming files for every revision.
- Names must describe game meaning, not the artist/tool used to create the file.
- Avoid spaces and ambiguous abbreviations.

## 6. Export/import defaults

These are defaults, not immutable requirements. Deviations are recorded when needed.

### UI and 2D match textures

- PNG with transparency where needed.
- sRGB/color textures; no unnecessary normal maps.
- Keep editable masters at 2× the largest expected display size where practical.
- Prefer 9-sliced UI panels over many near-duplicate fixed-size panels.
- Avoid text baked into textures; localized text remains live UI text.
- Avoid rasterizing small UI icons below the master size; export down only when profiling justifies it.

### Portraits

- square source framing;
- neutral or transparent/canonical background treatment defined by A1;
- 1024×1024 working master recommended; 512×512 Unity export is the initial target unless actual UI usage warrants more;
- preserve consistent head scale, eye line, crop, lighting, and background treatment across the set.

### Stadium/background art

- 16:9 working composition unless a target screen specifies otherwise;
- 2560×1440 working/export target for initial UI backgrounds;
- compose important detail away from text-safe UI zones.

### 2D match view

The match view should favor **procedural/layout-driven elements over baked screenshots**:

- pitch markings should be geometry/UI primitives where practical;
- grass texture provides material character, not gameplay geometry;
- player markers and event effects remain separate assets so team colors, numbers, selection, and tactical overlays can change dynamically;
- visual interpolation remains a rendering concern and never changes simulation state.

## 7. Provenance, licensing, and fictional-first rule

Every externally sourced or generated production asset must have enough provenance to answer:

- where it came from;
- who created/generated it;
- what license or usage right applies;
- whether attribution is required;
- whether it depicts or derives from a real identifiable person, club, competition, sponsor, or trademark;
- whether it is safe for commercial distribution.

Until explicit rights are secured, production content is **fictional-first**:

- no real club crests;
- no real league/competition logos;
- no sponsor marks;
- no copied kit designs;
- no real-player likenesses presented as licensed game content.

Reference material may inform art direction, but production assets must be original or appropriately licensed.

## 8. AI-assisted asset policy

AI-assisted generation may be used as a production accelerator, especially for fictional portraits, backgrounds, exploration, and concept work, but it does not remove art-direction or provenance requirements.

For production use:

- retain the prompt/workflow or enough generation metadata to reproduce the style;
- record the generation tool/model when known;
- perform human selection and correction;
- reject obvious real-person likenesses, watermarks, embedded text, distorted logos, or untraceable copied marks;
- maintain consistency through controlled reference sheets/templates rather than unconstrained one-off prompts;
- do not treat raw generated output as automatically shippable.

## 9. Acceptance checks

An asset is production-ready only when the applicable checks pass:

1. **Purpose:** tied to a real screen/surface or approved reusable family.
2. **Style:** conforms to the current A1 art-direction sheet.
3. **Readability:** works at its actual in-game size.
4. **Technical:** correct dimensions/alpha/color space/compression/import type.
5. **Scalability:** does not require unique code for one visual unless intentionally exceptional.
6. **Localization:** contains no baked-in user-facing text unless explicitly justified.
7. **Rights:** provenance/licensing is known and commercially acceptable.
8. **Performance:** appropriate memory/atlas/compression behavior for the target surface.
9. **Fallback:** dynamic content families have a defined missing/default asset where needed.
10. **Unity identity:** committed `.meta` file preserves the GUID once imported.

## 10. Initial backlog

### Now

- [ ] Art-direction sheet v0.1
- [ ] Palette + typography study
- [ ] UI icon language study
- [ ] 2D match-view style study
- [ ] Decide portrait treatment: realistic generated, illustrated, or stylized hybrid
- [ ] Decide stadium/background treatment

### First production kit

- [ ] System XI working-title wordmark
- [ ] Core UI panel kit
- [ ] First 12–20 UI icons
- [ ] Match pitch treatment
- [ ] Player/goalkeeper/ball marker set
- [ ] Goal/card/substitution event marks
- [ ] 4 fictional badges
- [ ] 4 fictional kits/color identities
- [ ] 8 fictional portraits + fallback

### Pipeline hardening after the kit works in Unity

- [ ] add Unity import presets/automation where repeated manual settings become evident;
- [ ] add asset-catalog/registry only when runtime lookup requirements are concrete;
- [ ] add automated validation for dimensions/naming/provenance where the checks are stable enough to avoid false precision;
- [ ] define atlas/addressable strategy from measured UI/runtime needs rather than prematurely.

## 11. Current implementation boundary

This foundation deliberately does **not** add speculative runtime art systems or new simulation dependencies. It establishes how art is produced and where exported assets live. Runtime loaders/catalogs should be added only when a concrete UI/match-view consumer requires them.

That keeps the art stream parallel to architecture/backend work while avoiding the same phantom-consumer problem already recognized by the UI framework design.
