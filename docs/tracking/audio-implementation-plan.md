# Audio Implementation & Production Plan

> **Created:** September 4, 2026
> **Last Updated:** September 4, 2026 — v1.1 final readiness revision
> **Version:** 1.1
> **Status:** READY FOR IMPLEMENTATION
> **Branch:** `audio/t0-contract-foundation`
> **Governs:** Audio & Sound Design #51 implementation plus the production pipeline for shippable audio assets.
> **Authority:** `docs/specs/audio-sound-design/` remains authoritative for #51 contracts. `docs/tracking/path-to-playable-roadmap.md` governs implementation sequencing. A recorded defect against an assembly-less spec is discharged with its T0 code + test, not as isolated pre-code hardening.

---

## 0. Purpose and verified starting point

Audio can proceed in parallel with backend, UI, art, and localization, but it needs two synchronized tracks:

1. **framework/integration** — #51 T0→T3; and
2. **production** — source creation, editing, mastering, export, rights/provenance, import, audition, mix, approval, replacement, and release validation.

The approved #51 spec already defines the engineering architecture and deliberately leaves actual audio content to production.

Verified starting point:

- `src/` contains no audio implementation.
- `Assets/` contains no audio content yet.
- #51 is APPROVED and requires a leaf `TacticalDirector.Audio` assembly.
- Git LFS already covers `.wav`, `.mp3`, `.ogg`, `.aif`, and `.aiff`.
- Unity is pinned to `6000.4.9f1`.
- #51's engineering sequence is:
  - **T0:** pure contracts/catalogue/ducking, silent;
  - **T1:** playback API, pure mixer math, settings fragment, still silent;
  - **T2:** shell `CueId → CueKey` mapping + completeness + Unity host binding, first audible output;
  - **T3:** caption binding/deeper presentation and optional commentary-audio depth.

### Recorded T0 defect

Planning exposed a real spec-to-code gap: `CueEntry` requires #51-owned `AssetRef` and #51-owned `CueParams`, and the approved text states that #51 declares its own `CueParams`, but §4's file list omits both and §2 does not define their concrete shapes.

**Sequencing rule:** record this now, decide the intended repair during T0 design, but **do not land a prose-only fix ahead of the assembly**. The spec correction, code, and regression tests land together in the T0 implementation PR, consistent with the roadmap's no-pre-T0-hardening rule.

---

# Part I — High-level plan critique loop

## HLP v0.1

1. Reconcile #51 and create the audio assembly.
2. Implement T0→T3 in order.
3. Create audio folders/import settings.
4. Produce UI, match, crowd, music, and later commentary assets.
5. Integrate settings/captions.
6. Mix, optimize, and ship.

### Critique 1

Correct direction, but too linear and too vague:

- wastes parallel production work;
- permits bulk content before the end-to-end path is proven;
- ignores rights/provenance and provisional-content control;
- has no source/master/runtime separation;
- has no middleware decision rule;
- hides dependencies on #38, #48, and #49;
- has no measurable vertical-slice gate;
- risks making deep-tier spoken commentary an Early Access blocker.

## HLP v0.2

Use five parallel workstreams:

- **A — Contracts & engineering:** #51 T0→T3.
- **B — Pipeline/tooling:** folders, naming, metadata, import policy, validation, Git LFS.
- **C — Production:** brief → source → edit → master → export → register → approve.
- **D — Integration/mix:** shell mapping, Unity binding, settings, captions, ducking, mix.
- **E — QA/release:** performance, build size, rights, accessibility, regression, EA freeze.

Require an audible vertical slice before scaling production.

### Critique 2

Much better, but still incomplete:

- vertical slice needs exact contents and pass/fail conditions;
- native Unity vs FMOD/Wwise needs an evidence-based decision point;
- the #51 leaf must not acquire Unity asset types;
- `CueKey`, filenames, runtime paths, captions, and rights need a drift-resistant source-of-truth model;
- provisional files need a release-blocking state;
- "approved audio" needs a formal definition;
- pre-T0 spec fixes must obey the roadmap's spec+code+test landing rule.

## HLP v1.0 — settled

Seven gates govern the work:

1. **G0 — Design ready:** all T0 ambiguities are recorded with an implementable intended resolution; stable identity rules and Early Access scope are frozen. No isolated spec hardening and no bulk content.
2. **G1 — Pipeline ready:** source/runtime layout, naming, production/provenance metadata, provisional state, rights policy, and validation contract exist.
3. **G2 — T0 green and silent:** T0 code + any required spec back-prop + tests land together. #51 is pure, leaf, host-free, and silent.
4. **G3 — Audible vertical slice:** native Unity host binding proves a complete small path through UI/SFX/Crowd/Music and at least one real #48→shell→#51 match cue. Bulk production starts only after this passes.
5. **G4 — Integration complete:** #38 settings persistence, #49 captions, mapping completeness, mute/volume/ducking, and observer-neutrality are live.
6. **G5 — Early Access content complete:** approved UI, core match SFX, crowd, and music batches; no referenced provisional assets. Spoken commentary remains optional unless separately promoted.
7. **G6 — Release ready:** final mix, performance/memory/build-size, rights, accessibility, host verification, and release validation pass.

### Technology decision

Default to **Unity native audio through Early Access**. Do not add FMOD/Wwise speculatively.

Re-open the decision at G3 only if the vertical slice demonstrates a concrete cost/requirement that native Unity cannot satisfy economically: e.g. dynamic-music authoring, voice-management complexity, profiling/authoring limitations, or localization/streaming scale. If no evidence requires middleware at G3, freeze native Unity through the Early Access release so content is not migrated mid-production.

### Early Access audio scope

**Required**

- UI feedback/notifications;
- core on-pitch SFX;
- crowd ambience/reactions;
- menu/management/match music sufficient to avoid silence/repetition fatigue;
- master + per-bus volume/mute;
- caption decisions for information-bearing cues;
- no broken loops, clipping, missing mappings, provisional shipping files, or rights gaps.

**Not required for the first EA audio milestone**

- spoken play-by-play commentary;
- advanced reverb/occlusion;
- data-driven bus topology;
- elaborate adaptive-music systems;
- cinematic audio pipelines.

---

# Part II — Detailed plan critique loop

## Detailed v0.1

P0 contract reconciliation → P1 pipeline → P2 T0 → P3 T1 → P4 T2 vertical slice → P5 settings/captions → P6 production → P7 mix/QA → P8 EA freeze.

### Critique 1

Missing:

- exact phase deliverables/exits;
- asset lifecycle;
- folder/source rules;
- catalogue/provenance fields;
- batch order;
- host-free vs host-gated evidence;
- rollback/decision point for middleware.

## Detailed v0.2

Added formal gates, asset lifecycle, batch ordering, metadata split, native-Unity default, and test split.

### Critique 2

Remaining weaknesses:

- one giant manifest would become a conflict hotspot;
- runtime paths must not become stable identity;
- codec/loudness values should be measured tuning data, not premature spec constants;
- provisional/approved states need release enforcement;
- pre-T0 spec hardening conflicted with the roadmap;
- PR/review boundaries were not explicit enough.

## Detailed v1.1 — implementation-ready

---

## P0 — Planning/design freeze; record, do not pre-harden

**Purpose:** eliminate implementation ambiguity without violating the roadmap rule for assembly-less specs.

### Actions

1. Re-read all #51 sections + appendices as T0 authority.
2. Record the `AssetRef` / #51 `CueParams` defect and decide the exact intended shape for the T0 landing.
3. **Do not edit #51 in isolation.** The correction lands with P2 code and regression tests.
4. Verify ERR-048-001 remains landed: #51 never references #48; the shell joins `CueId → CueKey`.
5. Verify #38 owns the one client-local settings store; #51 owns only its fragment.
6. Freeze stable identity rules:
   - `CueKey` is the durable audio identity;
   - filenames and Unity paths are replaceable implementation detail;
   - `AudioBus` ordinals are APPEND-only;
   - every cue has an explicit caption decision.
7. Freeze Early Access scope and native-Unity-until-G3 technology posture.

### Exit — G0

- every type/ownership question required to implement T0 has a recorded intended resolution;
- no pre-T0 prose-only hardening landed;
- no #51→#48/#49/sim dependency is planned;
- EA scope and middleware decision rule are fixed.

---

## P1 — Production pipeline substrate

May run in parallel with P2 after G0. **Bulk content remains blocked until G3.**

### P1.1 Repository layout

```text
content/audio/
├── source/                 # source recordings/stems/finished masters; not Unity-imported
│   ├── ui/
│   ├── sfx/
│   ├── crowd/
│   ├── music/
│   └── commentary/
├── provenance/             # creator/vendor/license/rights/approval records
└── briefs/                 # purpose + acceptance notes

Assets/Audio/
├── Runtime/
│   ├── UI/
│   ├── SFX/
│   ├── Crowd/
│   ├── Music/
│   └── Commentary/
└── Catalogue/              # host-side binding/config artifacts only
```

Avoid permanent audition/test audio under `Assets/`; test material belongs outside runtime content unless a Unity-host test specifically needs it, in which case release validation must exclude it.

### P1.2 Recoverable source rule

A shippable asset must be reproducible from a retained source package:

- source recording/stems and edit notes; or
- DAW/project file + dependencies; or
- commissioned/vendor master plus contract/source record.

If proprietary/huge project sessions are stored outside Git, provenance metadata records the immutable archive location and version. A runtime WAV with no recoverable source is not a complete production asset.

### P1.3 Runtime naming

`au_<bus>_<family>_<semantic-name>_<variant>_vNN.<ext>`

Examples:

- `au_ui_nav_confirm_01_v01.wav`
- `au_sfx_ball_kick_03_v02.wav`
- `au_crowd_reaction_goal_positive_02_v01.wav`

Rules:

- lowercase snake-case;
- semantic event name, not screen location/task number;
- variant number is separate from revision;
- filename/path never substitutes for `CueKey`.

### P1.4 Master/runtime policy

- Canonical finished masters: lossless WAV, normally 48 kHz / 24-bit.
- Preserve higher-quality/raw source material when it is useful for future remastering.
- Unity runtime rendition lives under `Assets/Audio/Runtime`.
- Runtime codec/quality/load type is measured by category at G3/G6, not frozen now:
  - short UI/SFX prioritize latency;
  - long music/crowd prioritize memory/streaming.
- No universal loudness target is frozen before a reference mix exists. No digital clipping is acceptable.

### P1.5 Metadata: split identity from production

**Identity/catalogue data**

- `CueKey`;
- bus;
- semantic category;
- caption decision (`CaptionId` or justified `NoCaption`);
- variant-set identity.

**Production/provenance data**

- runtime asset reference;
- source package reference;
- creator/vendor/source;
- license/contract reference;
- commercial/platform rights;
- attribution requirement;
- AI-generated/AI-assisted status if relevant;
- revision;
- approval status;
- approver/date;
- `provisional` flag.

Do not make one monolithic mutable file mandatory. Validation joins the identity catalogue and production records and asserts one complete shipping record per referenced runtime asset.

### P1.6 Provisional and rights rules

- Every placeholder is explicitly `provisional=true`.
- A release validator rejects referenced provisional content.
- Unknown/incomplete rights = non-shipping regardless of quality.
- Rights/provenance are recorded when the source enters the pipeline, not at release cleanup.

### P1.7 Validation contract

A small host-free validator should eventually check at minimum:

- duplicate identities/production records;
- missing referenced files;
- orphan shipping files;
- missing provenance/rights fields;
- referenced provisional assets in release mode;
- invalid naming/revision state.

Do not make this validator own Unity import behavior; committed Unity metadata/host checks own that half.

### Exit — G1

Layout, naming, source recoverability, metadata schema, provisional policy, rights policy, and validator contract exist. Only vertical-slice assets may be created before G3.

---

## P2 — #51 T0 pure contract foundation

**This is the first audio implementation PR.** Any required #51 spec correction lands in the same PR/commit series as the code and regression proof.

### Production assembly

`src/audio/` → `TacticalDirector.Audio`, `noEngineReferences: true`, zero production references.

### Land

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

### Tests/invariants

- fixed/APPEND-only bus ordinals;
- `Master` cannot be a cue routing target;
- duplicate `CueKey` refused;
- missing/default caption decision refused;
- unjustified `NoCaption` refused;
- duck trigger==target refused;
- attenuation cycle refused;
- #51 references nothing;
- no sim types;
- no deterministic RNG;
- all contract tests run host-free.

### Explicitly excluded

No `AudioSource`, `AudioClip`, shell adapter, #48 types, settings persistence, Unity playback, or final content.

### Exit — G2

T0 + spec back-prop + tests are green, and the game remains exactly silent.

---

## P3 — #51 T1 playback contract/mixer/settings fragment

### Land

- `IAudioPlayback` API;
- pure `AudioMixer` gain composition;
- `AudioSettingsFragment`;
- per-bus/master gain and mute validation;
- invalid-field reset-to-default behavior;
- duck-gain calculation from bus activity.

### Rules

- still no shell adapter or audible binding;
- #51 remains Unity-free;
- settings own no file/path/serializer;
- corrupt preference values never block launch;
- tuning stays client-side and out of sim state.

### Tests

- unity-gain identity;
- mute dominance;
- monotone gain composition;
- corrupt/partial settings recovery;
- ducking reads bus activity only;
- no serialization/RNG/sim state.

### Exit

T1 host-free green and silent.

---

## P4 — T2 integration and G3 audible vertical slice

Split into two reviewable slices while preserving #51's rule that completeness arrives with the adapter.

### P4A — shell adapter + completeness, still silent

Land outside #51:

- shell `CueSinkAdapter`;
- typed `CueId → CueKey` table;
- build-time "every emit-able `CueId` maps" proof;
- reverse "every mapping target exists" proof;
- runtime unmapped cue = silent no-op;
- fully-qualified translation between #48 and #51 `CueParams`.

This may land before audible playback because it creates no window in which unchecked cues can make sound.

### P4B — Unity host binding + vertical assets

Add the smallest Unity-specific playback binding required to:

- resolve a runtime asset;
- play on its routed bus;
- apply composed gain/mute;
- expose bus activity for ducking;
- stop/replace where the pure API requires it.

Unity types remain in host/client code, never `TacticalDirector.Audio`.

### Vertical-slice assets

Only this minimal set is authorized before G3:

- **UI:** one confirm/click + one notification;
- **SFX:** whistle + ball strike;
- **Crowd:** one seamless bed + one reaction;
- **Music:** one seamless loop;
- **Commentary:** no spoken line required.

At least one match cue must traverse the real `#48 → shell → #51 → Unity` path.

### G3 pass/fail

Pass only if:

- every vertical cue resolves to a defined `CueKey` and runtime asset;
- deliberately unmapped runtime cue is silent while build-time completeness detects the defect;
- UI/SFX/Crowd/Music route to correct buses;
- master/per-bus gain and mute affect real output;
- ducking is demonstrated from bus activity, not sim polling;
- audio-enabled and audio-disabled same-seed runs have identical deterministic digests/RNG cursors;
- no forbidden #51 dependency appears;
- source→master→runtime→register→import→audition workflow is repeatable;
- provenance is complete for every slice asset;
- host profiling records memory/CPU/voice/latency behavior sufficient for initial import settings;
- native Unity audio is either accepted through EA or rejected with concrete G3 evidence **before** bulk production.

### Exit — G3

The end-to-end audio path is proven. Bulk production is now permitted.

---

## P5 — Settings, captions, accessibility

May overlap P6 once G3 is green.

### #38 settings

- register #51's fragment with the one client-local settings store;
- expose Master + Music/SFX/Crowd/Commentary/UI controls;
- persist without any private audio settings file;
- corrupt values default/continue per #51.

### #49 captions

- #49 gains the legitimate `→ #51` reference when the producer exists;
- map information-bearing `CaptionId`s to localized text;
- ambience/texture use justified `NoCaption` where appropriate;
- #51 emits identities, never display strings.

### Exit — G4

Settings persist, caption coverage is structurally complete, mapping is complete, and the live path remains observer-neutral.

---

## P6 — Production scaling

Every batch follows the same lifecycle:

1. **Brief** — purpose, trigger, functional/emotional goal, loop/length/variation needs, caption classification.
2. **Source** — record/synthesize/commission/license/create; provenance begins immediately.
3. **Edit** — cleanup, trim, fades, loop construction.
4. **Master** — canonical lossless master with headroom/no clipping.
5. **Export** — runtime rendition and revision.
6. **Register** — production metadata + catalogue/bus/caption association.
7. **Import** — Unity settings appropriate to category.
8. **Audition in game** — never approve from a file player alone.
9. **Mix review** — level, masking, repetition, transitions, ducking.
10. **Approve/revise** — only approved non-provisional assets satisfy shipping cues.

### Batch order

#### B1 — UI

- semantic navigation confirmation/back where useful;
- toggles/sliders;
- success/error/notification;
- flow-specific families only when the underlying UI semantics stabilize.

Avoid screen-specific one-offs; semantic UI events survive redesign.

#### B2 — Core match SFX

- whistles;
- ball strike/pass/shot families;
- net/goal impacts where presented;
- tackles/contacts where stable #48 cues exist;
- substitution/restart/stoppage only through stable presentation identities.

Variation is display-side only.

#### B3 — Crowd

Layered system:

- neutral bed;
- positive/negative swells;
- goal reaction;
- near-miss/tension only where stable presentation cues exist.

Crowd reacts to presentation cues/audio activity; #51 never polls score/possession/match state.

#### B4 — Music

- main/menu identity;
- management/background loop set;
- optional match/replay state from presentation/navigation context;
- stingers/transitions only after loop behavior is stable.

Optimize for low repetition fatigue before raw track count.

#### B5 — Management ambience/secondary notifications

Add only where real screen usage demonstrates value.

#### B6 — Spoken commentary — optional deep-tier project

Do not put EA on its critical path without a separate product decision. Promotion requires its own voice/localization/rights/pronunciation/build-size/streaming plan.

### Approval definition

An asset is `approved` only if all are true:

- correct semantic purpose in game;
- correct bus/category;
- no clip/click/broken loop/unintended silence;
- repetition/variation acceptable in context;
- source package recoverable;
- provenance/rights complete;
- caption classification complete;
- runtime import reviewed;
- not provisional;
- approval recorded.

### Exit — G5

All EA-required cue families have approved runtime assets. No mapped shipping cue references provisional/missing content.

---

## P7 — Mix, performance, regression

### Mix order

1. master reference/headroom;
2. UI intelligibility;
3. core match SFX;
4. crowd bed/reactions;
5. music;
6. ducking attack/release/attenuation;
7. optional commentary last.

Loudness/mix numbers are production tuning values established on the real reference mix, not speculative spec constants.

### Measure on Unity host

- active voice count;
- resident audio memory;
- stream/decode CPU;
- first-play latency;
- menu/scene transition behavior;
- long-session leakage;
- build-size contribution by category.

Use #51's existing budget ceilings as alert thresholds where applicable; real host capture is the evidence.

### Host-free regression

- catalogue validity;
- caption decisions;
- mapping completeness in shell tests;
- settings validation;
- assembly-direction locks;
- observer-neutral deterministic scenario;
- release metadata/provisional/rights validation.

### Unity-host regression

- actual playback/routing;
- mute/volume;
- ducking envelope;
- loop seams;
- import/load behavior;
- memory/CPU/latency;
- subjective mix acceptance.

---

## P8 — Early Access release gate

### G6 requires

- zero referenced provisional assets;
- zero missing `CueId → CueKey` mappings;
- zero missing runtime assets for mapped cues;
- caption coverage for information-bearing cues;
- complete rights/provenance for every shipping asset;
- no accepted clipping/broken loops;
- settings persist through #38 and recover from corrupt values without blocking launch;
- audio-on/audio-off determinism locks stay green;
- #51 remains leaf and Unity/#48/#49/sim-free;
- host playback checks pass on pinned Unity;
- build-size/memory impact reviewed and accepted;
- final subjective pass covers menu → management → full match → post-match.

---

# Part III — Parallel map and PR boundaries

## Dependency map

```text
P0 design/recorded defect
        |
        +-----------------------+
        |                       |
P2 T0 + spec fix            P1 pipeline substrate
        |                       |
P3 T1                          |
        +-----------+-----------+
                    |
                 P4A shell
                    |
             P4B audible slice
                    |
              G3 AUDIBLE GREEN
              /             \
             /               \
P5 settings/captions      P6 content batches
             \               /
              \             /
               P7 mix + QA
                    |
               P8 EA gate
```

Dependency boundaries:

- **T0:** depends on nobody; spec defect is discharged here.
- **T1:** no #38 persistence required; only fragment behavior.
- **T2/P4:** depends on built #48 cue surfaces and client shell.
- **P5:** depends on #38 settings store and #49 caption renderer.
- **P6:** UI/crowd/music can scale after G3; semantic match SFX follows stable #48 cue identities.

## Recommended PR slices

1. **Audio pipeline skeleton** — P1 folders/metadata/validation contract; no bulk assets.
2. **Audio T0** — spec back-prop + `TacticalDirector.Audio` pure contracts + tests.
3. **Audio T1** — playback API/mixer/settings-fragment pure logic + tests.
4. **Audio T2A** — shell mapping/adapter/completeness; still silent.
5. **Audio T2B vertical slice** — Unity binding + minimal vertical assets + host evidence + middleware decision.
6. **Audio settings/captions** — #38/#49 integration.
7. **One PR per production batch/sub-batch** — avoid giant binary reviews.
8. **Mix/release hardening** — tuning, host performance evidence, release validator closure.

Each code PR gets the project's normal compile/test gate and adversarial review. Binary-heavy PRs include an asset inventory, provenance status, and in-game audition result rather than relying on diffs that cannot meaningfully review sound.

---

# Part IV — Prohibitions

Do not:

- mass-produce content before G3;
- land a prose-only T0 defect fix before the T0 assembly;
- put Unity types in `TacticalDirector.Audio`;
- let #51 reference #48, #49, or sim;
- create a private audio settings file;
- drive ducking from game state;
- use deterministic-sim RNG for sound variation;
- use filenames/paths as durable identities;
- approve unknown-rights content;
- allow provisional content into a release build;
- make spoken commentary a default EA blocker;
- choose middleware without G3 evidence;
- treat green host-free tests as proof that the game sounds correct.

---

# Part V — First implementation sequence

After approval of this plan:

1. keep the `AssetRef` / #51 `CueParams` issue recorded, but do not patch the spec alone;
2. land the small P1 pipeline/metadata skeleton;
3. implement P2/T0 and discharge the spec defect in the same landing with regression tests;
4. run the relevant full gate and adversarial review;
5. implement T1;
6. implement T2A;
7. build the G3 vertical slice before authorizing any large sound-library production.

This is the point at which the plan is considered **implementation-ready**: substantial engineering or production beyond these controlled slices should not begin until its preceding gate is green.

---

## Version history

| Version | Date | Notes |
|---|---|---|
| 1.0 | 2026-09-04 | Initial converged plan after two high-level and two detailed critique/revision rounds. |
| 1.1 | 2026-09-04 | Final readiness critique: corrected pre-T0 defect sequencing to obey the roadmap's record-now/discharge-with-code rule; added recoverable source-package policy, explicit P4A/P4B split, release exclusion for test assets, and PR/review boundaries. |
