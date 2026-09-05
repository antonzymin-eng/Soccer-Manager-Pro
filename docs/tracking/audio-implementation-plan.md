# Audio Implementation & Production Plan

> **Created:** September 4, 2026
> **Version:** 1.0
> **Status:** READY FOR IMPLEMENTATION
> **Branch:** `audio/t0-contract-foundation`
> **Governs:** Audio & Sound Design #51 implementation plus the production pipeline for shippable audio assets.
> **Authority:** `docs/specs/audio-sound-design/` remains authoritative for #51 contracts. This plan sequences implementation and production; where it conflicts with the approved spec, the spec wins unless a defect is back-propagated in the same landing.

---

## 0. Why this plan exists

Audio can proceed in parallel with backend, UI, art, and localization, but only if we avoid two failure modes:

1. building a playback stack before the contract boundaries are stable; and
2. producing large volumes of sound before the import/catalogue/mix path has been proven end to end.

The approved #51 spec already defines the engineering sequence T0→T3. It deliberately does **not** define the production workflow for source recordings, edits, masters, runtime exports, rights, approvals, or replacement of provisional material. This plan joins those two halves.

Current verified state:

- `src/` contains no audio implementation.
- `Assets/` contains no audio content yet.
- #51 is APPROVED and defines a leaf `TacticalDirector.Audio` assembly.
- The repository already routes `.wav`, `.mp3`, `.ogg`, `.aif`, and `.aiff` through Git LFS.
- The project is pinned to Unity `6000.4.9f1`.
- #51's T-phase sequence is T0 pure contracts → T1 playback/mix contracts and settings fragment → T2 shell mapping + host binding and first audible output → T3 captions/deeper presentation.

### T0 specification defect discovered during planning

`CueEntry` requires #51-owned `AssetRef` and #51-owned `CueParams`, and the approved text explicitly says #51 declares its own `CueParams`; however the architecture file list does not include either type and the data-structure section does not define their concrete shape. T0 must not invent these contracts silently. The first implementation landing must resolve this as a spec+code+test back-propagation before `CueEntry` / `CueCatalogue` are considered complete.

---

# Part I — High-level plan evolution

## HLP v0.1

1. Reconcile #51 and create the audio assembly.
2. Implement T0→T3 in spec order.
3. Create audio folders and import settings.
4. Produce UI, match, crowd, music, and later commentary assets.
5. Integrate settings and captions.
6. Mix, optimize, and ship.

### Critique 1

This is directionally correct but too linear and too vague.

- It treats audio production as something that starts only after engineering, wasting parallelism.
- It does not protect against bulk asset creation before the runtime path is proven.
- It does not define rights/provenance, source/master/runtime separation, naming, versioning, or provisional-asset replacement.
- It assumes the audio technology choice is settled without an explicit middleware decision.
- It has no measurable vertical-slice gate.
- It does not expose dependencies on #38 settings, #48 cue emission, and #49 caption rendering.
- It gives no Early Access content boundary, creating a risk that spoken commentary becomes a critical-path content project even though #51 places it in the deeper tier.

## HLP v0.2

Run five parallel workstreams behind explicit gates:

- **A — Contracts & engineering:** #51 T0→T3.
- **B — Asset pipeline & tooling:** folders, naming, manifests, import policy, validation, Git LFS.
- **C — Audio production:** briefs, source, edit, master, export, registration, approval.
- **D — Integration & mix:** shell mapping, Unity host binding, settings, captions, ducking, final mix.
- **E — QA & release:** performance, build size, rights, accessibility, regression, Early Access freeze.

Require a small audible vertical slice before scaling production.

### Critique 2

This fixes the structure but still leaves several implementation risks:

- The vertical slice needs exact content and exact pass/fail criteria.
- Native Unity audio versus FMOD/Wwise still needs a decision rule.
- T0 can proceed without #38/#48/#49, while later work cannot; that dependency boundary should be explicit.
- The asset catalogue must not force the leaf #51 assembly to reference Unity types.
- The plan needs a source-of-truth rule so filenames, `CueKey`s, Unity bindings, captions, and rights metadata cannot drift independently.
- Provisional assets need an explicit non-shipping marker and replacement gate.
- Asset approvals need a definition; otherwise "done" means only "a file exists."

## HLP v1.0 — settled

The project will use **seven gates**, with engineering and production running in parallel where safe:

1. **G0 — Contract ready:** resolve T0 spec defects and freeze naming/identity rules. No bulk content.
2. **G1 — Pipeline ready:** source/runtime folders, asset manifest, rights metadata, naming/versioning, provisional markers, import policy, validation path.
3. **G2 — T0 green and silent:** pure #51 assembly + catalogue/ducking/caption invariants compile and test host-free. No playback.
4. **G3 — Audible vertical slice:** native Unity host binding proves one complete path through UI/match/crowd/music categories, with #48→shell→#51 mapping where applicable. No bulk expansion before this passes.
5. **G4 — Integration complete:** settings persistence through #38, captions through #49, mapping completeness, mute/volume/ducking, observer neutrality.
6. **G5 — Early Access content complete:** approved production batches for UI, core match SFX, crowd, and music; commentary voice remains optional/deep-tier unless separately promoted.
7. **G6 — Release ready:** final mix, performance, memory/build-size, rights/provenance, accessibility, platform-host checks, and no provisional assets.

### Technology decision

Use **Unity's native audio stack for the Early Access path by default**. Do not add FMOD/Wwise before the vertical slice. Re-open the decision only if the slice demonstrates a concrete requirement native Unity cannot meet economically, such as unmanageable dynamic-music authoring, voice-management complexity, profiler/authoring needs, or localization/streaming requirements. Middleware adoption after large-scale content begins is expensive, so this decision is re-evaluated at G3 and then frozen through Early Access.

### Early Access scope

Required:

- UI feedback and notifications.
- Core on-pitch SFX.
- Crowd ambience/reactions.
- Menu/match music sufficient to avoid silence and repetition fatigue.
- Master + per-bus volume/mute.
- Caption decisions for information-bearing cues.
- No clipping, broken loops, missing mappings, or unlicensed assets.

Not required for the first Early Access audio milestone:

- spoken play-by-play commentary;
- advanced reverb/occlusion;
- data-driven bus topology;
- elaborate adaptive-music systems;
- cinematic audio pipelines.

---

# Part II — Detailed plan evolution

## Detailed plan v0.1

Initial decomposition:

- P0 contract reconciliation
- P1 asset pipeline
- P2 T0
- P3 T1
- P4 T2 vertical slice
- P5 settings/captions
- P6 content production
- P7 mix/QA
- P8 Early Access freeze

### Detailed critique 1

The decomposition is usable but still underspecified.

- It lacks exact deliverables and exit criteria for every phase.
- It does not specify the lifecycle of one asset from brief to shipping file.
- It does not define source/master/runtime folder separation.
- It does not define the manifest fields needed to join catalogue, rights, captions, and Unity bindings.
- It does not define batch order, so high-cost crowd/music work could start before cheap UI/SFX proves the pipeline.
- It lacks a rollback rule for middleware and import-policy experiments.
- It does not say which tests are host-free versus Unity-host-gated.

## Detailed plan v0.2

Add formal gates, a manifest-driven asset lifecycle, batch ordering, native-Unity default, and separate host-free/host-gated verification.

### Detailed critique 2

Remaining issues:

- A single manifest can become a monolith and merge-conflict hotspot; split immutable identity/catalogue data from production/provenance metadata or generate one from the other.
- Runtime paths must not become stable identities; filenames and Unity paths are implementation details, while `CueKey` is the stable identity.
- Loudness and codec numbers should not be frozen before listening/profiling. Technical masters can be standardized, but runtime compression/mix targets should be measured tuning data.
- The content process needs a formal definition of "approved" and a mechanism that prevents provisional assets from entering a release build.

## Detailed plan v1.0 — implementation-ready

### P0 — Planning freeze and contract reconciliation

**Purpose:** remove ambiguity before any substantial code or content production.

**Actions**

1. Re-read all #51 section files plus appendices as the implementation authority.
2. File and fix the T0 defect for missing #51 `AssetRef` / `CueParams` shape and ensure the §4 file list, §2 data structures, tests, and appendices agree.
3. Confirm #48's ERR-048-001 back-prop remains landed: #51 never references #48; shell mapping joins `CueId`→`CueKey`.
4. Confirm #38 owns the client-local settings store; #51 contributes only a fragment.
5. Freeze the Early Access content boundary above.
6. Freeze native Unity audio as the default until G3.
7. Freeze stable identity rules:
   - `CueKey` is the durable audio identity.
   - asset path and filename are replaceable implementation details.
   - bus ordinals are APPEND-only.
   - every catalogue entry has an explicit caption decision.

**Exit — G0**

- No unresolved type/ownership ambiguity needed by T0.
- No #51→#48/#49/sim dependency.
- Early Access audio scope recorded.
- Technology choice recorded as "Unity native until G3 evidence says otherwise."

---

### P1 — Audio production pipeline substrate

This phase may run in parallel with P2 after G0, but bulk production remains prohibited until G3.

#### P1.1 Folder model

Use two layers:

```text
content/audio/
├── source/                 # archival/source masters; Unity does not import these
│   ├── ui/
│   ├── sfx/
│   ├── crowd/
│   ├── music/
│   └── commentary/
├── provenance/             # license/source/creator records
└── briefs/                 # production briefs and acceptance notes

Assets/Audio/
├── Runtime/
│   ├── UI/
│   ├── SFX/
│   ├── Crowd/
│   ├── Music/
│   └── Commentary/
├── Catalogue/              # host-side binding/config artifacts
└── Test/                   # development-only audition assets where needed
```

Do not place editable source sessions inside `Assets/`; Unity should import only runtime renditions and host binding artifacts.

#### P1.2 File naming

Runtime export convention:

`au_<bus>_<family>_<semantic-name>_<variant>_vNN.<ext>`

Examples:

- `au_ui_nav_confirm_01_v01.wav`
- `au_sfx_ball_kick_03_v02.wav`
- `au_crowd_reaction_goal_positive_02_v01.wav`

Rules:

- lowercase snake-case;
- semantic event names, not screen coordinates or temporary task numbers;
- variant number separate from revision number;
- filename is never the stable identity; `CueKey` is.

#### P1.3 Source/master policy

- Canonical finished masters: lossless WAV, 48 kHz, 24-bit unless a source cannot support it.
- Keep the uncompressed master outside Unity runtime folders.
- Runtime compression/load type is selected by category and measured on the Unity host rather than frozen by prose:
  - short UI/SFX optimize for immediate latency;
  - long music/crowd optimize for memory/streaming;
  - final codec/quality values are tuning data established during G3/G6 profiling.
- No normalization or loudness target is declared a universal constant before the reference mix exists. No digital clipping is ever acceptable.

#### P1.4 Metadata split

Avoid one giant manifest.

**Identity/catalogue data** — stable, code-adjacent:

- `CueKey`
- bus
- semantic category
- caption decision / `CaptionId` or justified `NoCaption`
- variant set identity

**Production/provenance data** — asset-adjacent:

- runtime asset filename/path
- source master reference
- creator/vendor/source
- license/contract reference
- allowed commercial/platform use
- attribution requirement
- AI-generated / AI-assisted status if applicable
- revision
- approval state
- approver/date
- provisional flag

A tooling step validates that every catalogue asset has exactly one production/provenance record and vice versa for shipping assets.

#### P1.5 Provisional asset rule

Every placeholder/prototype audio file is explicitly `provisional=true` in production metadata.

Release builds fail validation if a referenced shipping cue is provisional. This prevents "temporary" sounds from becoming Early Access assets by inertia.

#### P1.6 Rights rule

No third-party, stock, commissioned, recorded, or generated asset becomes `approved` without provenance sufficient to demonstrate commercial game-distribution rights. Unknown provenance means non-shipping, regardless of quality.

**Exit — G1**

- folder structure exists;
- naming policy documented;
- Git LFS confirmed for runtime/source audio formats;
- production/provenance schema exists;
- provisional/approved states exist;
- validation design can detect missing, duplicate, or provisional shipping assets.

---

### P2 — #51 T0: pure contract foundation

Follow #51 §7 T0 exactly after P0's spec repair.

**Production assembly**

`src/audio/` → `TacticalDirector.Audio`, `noEngineReferences: true`, zero production assembly references.

**Land**

- `AudioConstants`
- `AudioBus`
- `CueKey`
- `CaptionId`
- `CaptionDecision`
- resolved `AssetRef`
- resolved #51 `CueParams`
- `CueEntry`
- `CueCatalogue`
- `DuckingRow`
- `DuckingTable`
- test assembly

**Required invariant tests**

- bus set and APPEND-only ordinals;
- `Master` cannot be a cue routing target;
- duplicate `CueKey` refused;
- missing/default caption decision refused;
- unjustified `NoCaption` refused;
- duck trigger==target refused;
- attenuation cycles refused;
- #51 references nothing;
- no sim types or deterministic RNG use;
- contract tests run host-free under the normal Linux gate.

**Do not land in P2**

- `AudioSource` / `AudioClip` references;
- shell adapter;
- #48 types;
- settings persistence;
- Unity playback;
- final assets.

**Exit — G2**

T0 compiles and tests green while the game remains exactly as silent as before.

---

### P3 — #51 T1: playback contract, mixer math, settings fragment

**Land**

- `IAudioPlayback`
- pure `AudioMixer` gain composition
- `AudioSettingsFragment`
- per-bus/master mute and gain validation
- reset-invalid-field-to-default logic
- duck gain computation over bus activity

**Rules**

- still no shell adapter and no audible host binding;
- #51 remains Unity-free and leaf-like;
- settings fragment owns no path/file/serializer;
- malformed preference values default without blocking launch;
- tuning values remain client config and never enter sim state.

**Tests**

- unity gain identity;
- mute dominance;
- monotone gain composition;
- corrupt/partial settings recovery;
- ducking only reads bus activity;
- no sim serialization/RNG/state exposure.

**Exit**

T1 is host-free green and remains silent by construction.

---

### P4 — #51 T2 + audible vertical slice

This is the critical gate. Bulk content production is still blocked until it passes.

#### P4.1 Shell integration

Land outside #51:

- `CueSinkAdapter` in the client composition root;
- typed `CueId`→`CueKey` mapping;
- build-time mapping completeness;
- runtime unmapped-cue silent no-op;
- fully qualified #48/#51 `CueParams` translation.

#### P4.2 Unity host binding

Add the smallest host-only playback implementation needed to:

- resolve an approved runtime asset;
- play it on the correct bus;
- apply composed gain/mute;
- report bus activity for ducking;
- stop/replace where the pure interface requires it.

Unity-specific types stay in the host binding, not `TacticalDirector.Audio`.

#### P4.3 Vertical-slice asset set

Create only enough production-quality or clearly provisional audio to exercise the whole system:

- **UI:** confirm/click + one notification;
- **SFX:** whistle + ball strike;
- **Crowd:** one seamless bed + one reaction;
- **Music:** one seamless menu or match loop;
- **Commentary:** no spoken line required; bus exists but content may remain deferred.

At least one match cue must travel through the real #48→shell→#51 path.

#### P4.4 G3 acceptance

The vertical slice passes only if:

- every test cue resolves to a defined `CueKey` and runtime asset;
- an intentionally unmapped cue is silent at runtime and caught by the build-time completeness test;
- UI, SFX, Crowd, and Music route to the intended buses;
- master/per-bus mute and gain work on real output;
- crowd/music ducking can be demonstrated from **bus activity**, not game-state reads;
- no audio-enabled run changes deterministic match digests or RNG cursors;
- no #51→#48/#49/sim reference is introduced;
- source→master→runtime→catalogue→Unity→audition workflow is repeatable;
- provenance exists for every slice asset;
- host profiling records memory/CPU/voice behavior sufficient to choose initial import settings;
- the middleware re-evaluation finds no demonstrated blocker to native Unity, or records a concrete reason to switch before scaling.

**Exit — G3**

One small but complete audible path is proven. Only now may content production scale.

---

### P5 — Settings, captions, and accessibility integration

May overlap early P6 production once G3 is green.

#### Settings via #38

- register #51's settings fragment with the single client-local settings store;
- surface master + `Music`, `SFX`, `Crowd`, `Commentary`, `UI` controls;
- default/reset behavior follows #51; no private audio settings file.

#### Captions via #49

- #49 gains the legitimate `→ #51` reference when the producer is built;
- map each information-bearing `CaptionId` to localized text;
- ambience/texture normally use justified `NoCaption`;
- audio never emits display strings.

#### Accessibility acceptance

- information-carrying audible cues have caption decisions;
- disabling/muting a bus does not suppress equivalent visual/caption information where required;
- caption coverage checks run over the full built `CaptionId` roster.

**Exit — G4**

Settings persist correctly, captions are structurally complete, mapping is complete, and the full audio path remains observer-neutral.

---

### P6 — Production content scaling

Every batch follows the same lifecycle:

1. **Brief** — purpose, trigger, emotional/function target, duration/loop/variation needs, reference material, caption classification.
2. **Source** — record, synthesize, commission, license, or create; provenance created immediately.
3. **Edit** — clean, trim, de-noise if needed, loop construction, fades.
4. **Master** — lossless canonical master; preserve headroom; no clipping.
5. **Export** — runtime rendition with naming/version rules.
6. **Register** — production metadata + catalogue identity/bus/caption association.
7. **Import** — category policy applied by Unity host tooling.
8. **Audition in context** — never approve from a file player alone.
9. **Mix review** — relative level, masking, repetition, transitions, ducking.
10. **Approve or revise** — only approved, non-provisional assets can satisfy a shipping cue.

#### Batch order

**B1 — UI**

Cheapest and highest-frequency feedback; validates consistency and settings behavior.

- navigation hover/confirm/back where needed;
- toggles/sliders;
- success/error/notification;
- transfer/contract/board/inbox notification families only when their UI semantics stabilize.

Avoid one bespoke sound per screen; define semantic UI events that survive layout changes.

**B2 — Core match SFX**

- whistles;
- ball strikes/pass/shot families;
- net/goal impact where rendered;
- tackles/contacts where presentation emits a cue;
- substitution/restart/stoppage cues only where #48 exposes stable semantic events.

Variation is display-side only.

**B3 — Crowd**

Build as layers rather than one giant match recording:

- neutral bed;
- positive/negative swells;
- goal reaction;
- near-miss/tension where a stable presentation cue exists;
- home/away flavor only if presentation supplies the necessary non-sim coupling through approved seams.

Crowd should respond to emitted presentation cues and audio bus activity, never by #51 polling score/possession/etc.

**B4 — Music**

- main/menu identity;
- management/background loop set;
- optional match/replay state where presentation context owns the state;
- transition/stinger set only after base loop behavior is stable.

Prioritize low repetition fatigue over quantity.

**B5 — Management ambience and notifications**

Only after actual screen/flow usage demonstrates value. Do not add ambience merely because a bus exists.

**B6 — Spoken commentary — optional deep tier**

Separate content project. Do not put Early Access on its critical path without an explicit product decision. If promoted, it requires voice casting/generation policy, localization strategy, line-volume estimates, streaming/build-size budget, pronunciation handling, rights/consent, and expanded caption alignment.

#### Asset approval definition

An asset is `approved` only when all are true:

- semantic purpose is correct in-game;
- correct bus/category;
- no technical defect, clip, click, broken loop, or unintended silence;
- variation/repetition behavior acceptable in context;
- source/master/runtime chain is recoverable;
- provenance/rights complete;
- caption classification complete;
- runtime import settings reviewed;
- not provisional;
- review recorded.

**Exit — G5**

All Early Access-required cue families have approved assets with no missing mappings and no provisional referenced content.

---

### P7 — Mix, performance, and regression

#### Mix pass

Tune only on the real Unity host with representative gameplay/UI flows.

Order:

1. master headroom/reference level;
2. UI intelligibility;
3. core match SFX;
4. crowd bed/reactions;
5. music;
6. ducking attack/release/attenuation;
7. optional commentary last, because it changes every masking relationship.

Do not freeze loudness numbers in the spec. Record them as production tuning values after the reference mix exists.

#### Performance / memory

Measure at minimum:

- simultaneous active voices;
- memory resident audio;
- stream/decode CPU;
- first-play latency;
- scene/menu transition behavior;
- long-session leakage;
- build-size contribution by category.

Use #51's existing budget ceilings as alert thresholds where applicable, while treating real host capture as the evidence.

#### Regression matrix

Host-free:

- catalogue validity;
- caption decisions;
- mapping completeness where both id spaces are available in shell tests;
- settings validation;
- assembly-direction locks;
- determinism/observer-neutrality scenario.

Unity-host-gated:

- actual playback;
- routing;
- mute/volume;
- ducking envelope behavior;
- loop seams;
- import/load behavior;
- memory/CPU/latency;
- subjective mix acceptance.

---

### P8 — Early Access release gate

**G6 requires all of the following:**

- no referenced provisional assets;
- no missing `CueId`→`CueKey` mapping;
- no missing runtime asset for a mapped `CueKey`;
- all information-bearing cues have caption coverage;
- all shipping assets have complete provenance/rights records;
- no digital clipping or broken loop accepted;
- settings survive restart via #38 and corrupt values recover without blocking launch;
- audio-on vs audio-off deterministic digest/RNG locks remain green;
- production #51 assembly remains a leaf with no Unity/#48/#49/sim references;
- host playback checks pass on the pinned Unity version;
- build-size and runtime-memory impact are reviewed and accepted;
- final subjective pass covers menu → management flow → full match → post-match, not isolated files.

---

## 3. Parallel execution map

```text
P0 Contract reconciliation
        |
        +-----------------------+
        |                       |
P2 T0 engineering          P1 pipeline substrate
        |                       |
P3 T1 engineering               |
        +-----------+-----------+
                    |
            P4 T2 vertical slice
                    |
              G3 AUDIBLE GREEN
              /             \
             /               \
P5 settings/captions      P6 content batches
             \               /
              \             /
               P7 mix + QA
                    |
              P8 EA release gate
```

Backend work, UI work, art work, and localization may continue independently. Audio dependencies are intentionally delayed:

- **P2/T0:** depends on nobody.
- **P3/T1:** depends on nobody for persistence; settings are only a fragment.
- **P4/T2:** depends on #48 cue surfaces and client-shell composition.
- **P5:** depends on #38 settings store and #49 caption renderer.
- **P6:** can produce UI/crowd/music in parallel once G3 proves the pipeline, but semantic match SFX must follow stable #48 cue identities.

---

## 4. What must not happen

- Do not begin a large SFX/music/crowd library before G3.
- Do not put Unity types in `TacticalDirector.Audio`.
- Do not let #51 reference #48, #49, or sim assemblies.
- Do not create a private audio settings file.
- Do not trigger ducking from score, possession, goal state, or other sim values.
- Do not use deterministic-sim RNG for sample variation.
- Do not use filenames/paths as durable cue identity.
- Do not approve an asset with unknown rights/provenance.
- Do not allow provisional assets into a release build.
- Do not make spoken commentary a default Early Access blocker.
- Do not select FMOD/Wwise speculatively; require G3 evidence.
- Do not treat green host-free tests as proof that the game actually sounds correct.

---

## 5. First implementation slice after approval of this plan

The first code/content work should be deliberately small:

1. Resolve the `AssetRef` / #51 `CueParams` specification gap.
2. Land P1's directory/metadata skeleton and validation contract, without bulk assets.
3. Land P2/T0 pure `TacticalDirector.Audio` contracts and tests.
4. Run the full relevant gate and adversarial review.
5. Only after T0 is clean, proceed to T1 and the G3 audible vertical slice.

No final sound-library production belongs in the first slice.

---

## 6. Plan acceptance

This plan is ready for implementation because the critique/revision cycle has closed the original gaps:

- engineering and production are separated but synchronized;
- every large expenditure is behind a smaller proof gate;
- the existing #51 T0→T3 architecture is preserved;
- asset rights and provenance are first-class;
- source/master/runtime separation is explicit;
- stable identities are separated from replaceable file paths;
- provisional content cannot silently ship;
- native Unity audio is the default but not an irreversible assumption;
- Early Access scope is bounded and does not require deep-tier spoken commentary;
- host-free versus host-gated evidence is explicit;
- every phase has an exit criterion and the first implementation slice is small enough to review adversarially.
