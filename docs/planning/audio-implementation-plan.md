# System XI — Audio Implementation & Production Plan

**Status:** READY FOR G0 OWNER ACCEPTANCE  
**Started:** September 4, 2026  
**Last Updated:** September 7, 2026  
**Version:** 1.3  
**Implementation gate:** G0 CLOSED pending owner acceptance; no substantial audio implementation or bulk asset production is authorized by this document alone.  
**Governs:** Audio & Sound Design #51 implementation plus the production pipeline for shippable audio assets.

---

## 1. Authority, roadmap amendment, and current state

### 1.1 Authority order

Where surfaces disagree, use this order:

1. APPROVED `docs/specs/audio-sound-design/` (#51) governs audio architecture, identities, routing, settings semantics, captions, determinism, and T0→T3 contracts.
2. `docs/tracking/path-to-playable-roadmap.md` governs implementation sequencing, **as amended for the audio parallel workstream by `docs/planning/path-to-playable-audio-parallel-amendment.md`**.
3. This plan governs the audio implementation/production sequence and its quality gates.
4. Asset production metadata governs individual source/runtime files; it does not create architecture.

`docs/tracking/audio-sound-design.md` v0.4 is the historical design supplement from which #51 was promoted. It is **superseded by the APPROVED section files** and may be consulted for rationale only; it is never an implementation authority when it differs from the approved spec.

### 1.2 Roadmap status

The original Path-to-Playable roadmap deliberately deferred #48 and #51 past PM-3 because neither was required to prove a playable season. The owner subsequently directed that audio be developed **in parallel** with the main development stream. The companion roadmap amendment records the narrow change:

- #51 **T0/T1 and production-pipeline substrate may proceed in parallel** after G0 acceptance;
- this does **not** make audio part of the PM-2 critical path and must not block Track S;
- #51 T2/G3 remains blocked on **D48: #48 Match Presentation Depth T0**;
- caption-renderer integration remains blocked on **D49: #49 Localization & Accessibility T0**;
- the audio workstream does not silently reorder or implement those other specs inside #51 PRs.

### 1.3 Verified repository starting point

- No `src/audio/` assembly exists.
- No runtime audio content exists under `Assets/`.
- #48 has no `src/match-presentation-depth/` assembly and no live `CueId`/`ICueSink` implementation surface.
- #49 has no production assembly; therefore T3 caption rendering is not currently available.
- #38 `ui-framework` exists, but the audio plan must still honor FR-AU-022 if the shared settings-store integration is unavailable or declined.
- Git LFS already covers `.wav`, `.mp3`, `.ogg`, `.aif`, and `.aiff`.
- Unity is pinned to `6000.4.9f1`.

### 1.4 Recorded T0 defect — `ERR-051-001`

The planning review found a real pre-T0 specification defect:

- `CueEntry` contains #51-owned fields `AssetRef Asset` and `CueParams Params`;
- §2 says #51 declares its own `CueParams`, distinct from #48's;
- §4.2's declared file/type list omits **both** `AssetRef` and `CueParams`;
- neither concrete type shape is defined;
- therefore §4.2's CS0104 pre-check also never checked these two names.

Per Path-to-Playable C6, this finding is **recorded now** in #51 §7 and the ERR tracking surface, but the normative §2/§4 repair is **discharged at T0 with code and regression proof**, not hardened as prose ahead of the assembly.

---

# 2. High-level plan critique loop

## HLP v0.1

1. Build #51 T0→T3.
2. Add audio folders/import settings.
3. Produce UI, match, crowd, music, then commentary.
4. Integrate settings/captions.
5. Mix and ship.

### Critique 1

Too linear and under-specified. It wastes safe production parallelism, permits bulk content before the runtime path is proven, omits provenance/rights and provisional-content controls, hides #48/#49 dependencies, and provides no vertical-slice or middleware decision gate.

## HLP v0.2

Five workstreams:

- contracts/engineering;
- pipeline/tooling;
- production;
- integration/mix;
- QA/release.

Require a small audible vertical slice before volume production.

### Critique 2

Still incomplete. It does not resolve the Path-to-Playable deferral, assumes #48/#49 exist, conflates C6 recording with normative hardening, lacks collision handling for `AssetRef`/`CueParams` and Unity's `AudioMixer`, encodes routing/revision into filenames, assumes persistent #38 settings, leaves vertical-slice asset status ambiguous, lands neutrality too late, and forbids sim RNG without naming the replacement.

## HLP v1.2 — settled

The audio workstream uses **seven gates plus two external dependency gates**:

- **D48 — Match Presentation Depth T0 exists.** Required before T2 shell mapping / any real match cue can reach audio.
- **D49 — Localization & Accessibility T0 exists.** Required before caption rendering integration; not required for T0/T1 audio framework work.
- **G0 — Plan accepted.** Roadmap amendment, dependency boundaries, ERR-051-001 recording, identity rules, Early Access scope, and technology posture are accepted.
- **G1 — Pipeline substrate ready.** Source/runtime layout, provenance, rights, stable naming, provisional status, and validation contract exist.
- **G2 — T0 green and silent.** #51 pure contracts land together with the normative ERR-051-001 discharge and tests.
- **G3 — Audible vertical slice green.** Requires D48. A small Unity-native end-to-end slice proves real playback before bulk production.
- **G4 — Integration complete.** Settings use #38 where available or FR-AU-022's explicit in-memory fallback; caption rendering waits on D49.
- **G5 — Early Access content complete.** Approved UI/core-match/crowd/music content; no referenced provisional content.
- **G6 — Release-ready audio.** Mix/performance/build-size/rights/accessibility/host verification complete.

The primary simulation/season work remains higher priority. Audio may consume parallel capacity but must not stall PM-2/PM-3 critical-path work.

---

# 3. Technology and Early Access scope

## 3.1 Middleware decision

Default to **Unity native audio** through the G3 vertical slice. Do not add FMOD/Wwise speculatively.

At G3, re-open the decision only if measured evidence shows native Unity is uneconomical for a concrete need such as:

- dynamic-music authoring complexity;
- large voice-management requirements;
- profiling/authoring limitations;
- localized spoken-content streaming scale;
- platform-specific mixing constraints.

If G3 exposes no such blocker, native Unity is frozen through the first Early Access audio milestone to avoid mid-production migration.

## 3.2 Early Access required

- UI feedback and notifications;
- core on-pitch SFX;
- crowd bed/reactions;
- menu/management/match music sufficient to avoid silence and obvious repetition fatigue;
- master + per-bus gain/mute;
- explicit caption decisions for information-bearing cues;
- real caption rendering once D49 is available;
- complete rights/provenance;
- no missing mappings/assets, broken loops, clipping, or referenced provisional audio.

## 3.3 Explicitly not required for first EA audio milestone

- spoken play-by-play commentary;
- advanced reverb/occlusion;
- data-driven bus topology;
- elaborate adaptive music;
- cinematic audio pipelines.

Spoken commentary remains a separately promoted deep-tier content project.

---

# 4. P0 — G0 planning and recorded findings

### Actions

1. Accept this plan and the audio parallel roadmap amendment.
2. Keep `ERR-051-001` recorded, but do **not** patch normative §2/§4 until T0 code exists.
3. Freeze identity rules:
   - `CueKey` is durable audio identity;
   - `AudioBus` ordinal set is APPEND-only;
   - runtime file path/filename is replaceable implementation detail;
   - every catalogue entry carries an explicit caption decision;
   - routing lives in catalogue data, not filenames.
4. Verify #48/#49 dependency gates before each downstream phase rather than assuming they exist.
5. Freeze native-Unity-through-G3 posture.
6. Treat `docs/tracking/audio-sound-design.md` as superseded historical rationale only.

### Exit — G0

- roadmap conflict resolved by explicit amendment;
- ERR-051-001 recorded;
- D48/D49 state explicit;
- no speculative #48/#49 implementation inside #51;
- Early Access scope and technology decision rule accepted.

---

# 5. P1 — Production pipeline substrate

P1 may run in parallel with T0 after G0. **No bulk audio production before G3.**

## 5.1 Repository layout

```text
content/audio/
├── source/
│   ├── ui/
│   ├── sfx/
│   ├── crowd/
│   ├── music/
│   └── commentary/
├── provenance/
└── briefs/

Assets/Audio/
├── Runtime/
│   ├── UI/
│   ├── SFX/
│   ├── Crowd/
│   ├── Music/
│   └── Commentary/
└── Catalogue/
```

Editable/source masters remain outside the Unity runtime tree. Runtime imports under `Assets/Audio/` receive committed Unity `.meta` identities through real Unity import; audio work must obey the repository's project-wide `.meta` integrity rules established by AP-01.

## 5.2 Recoverable source rule

Every shipping asset has a recoverable source package:

- raw/source recording and edit notes; or
- DAW/project session plus dependencies; or
- vendor/commissioned master plus immutable source/contract reference.

If large/proprietary sessions live outside Git, provenance records their immutable archive location and revision.

## 5.3 Stable runtime filenames

Runtime filename convention:

`au_<family>_<semantic-name>_<variant>.<ext>`

Examples:

- `au_ui_nav_confirm_01.wav`
- `au_ball_kick_03.wav`
- `au_crowd_goal_positive_02.wav`

**Do not encode bus or revision in the filename.**

- Bus/routing belongs only to catalogue metadata.
- Revisions replace bytes in place and preserve the Unity `.meta` GUID where semantic identity is unchanged.
- Revision is recorded by Git/provenance metadata, not `_vNN` path churn.
- A new filename is for a genuinely coexisting semantic variant, not a remaster.

## 5.4 Master/runtime policy

- canonical finished master: lossless WAV, normally 48 kHz / 24-bit;
- preserve better raw sources when available;
- short UI/SFX optimize for latency;
- long crowd/music optimize for memory/streaming;
- exact runtime codec/quality/load mode is determined from G3/G6 Unity-host measurements;
- no universal loudness target is frozen before a real reference mix exists;
- digital clipping is never acceptable.

## 5.5 Metadata split

**Identity/catalogue data**

- `CueKey`;
- `AudioBus`;
- semantic cue category;
- caption decision / `CaptionId` or justified `NoCaption`;
- variant-set identity.

**Production/provenance data**

- runtime asset path/reference;
- source package reference;
- creator/vendor/source;
- license/rights basis and commercial/platform scope;
- attribution requirement;
- AI-generated/AI-assisted status where relevant;
- revision;
- approval state;
- approver/date;
- `provisional` flag.

Do not require one giant mutable manifest. Validation joins stable catalogue data to per-asset or family production records.

## 5.6 Provisional and rights policy

- all placeholders/prototypes are `provisional=true`;
- unknown/incomplete rights are non-shipping;
- release validation rejects any referenced provisional asset;
- provenance starts when the source enters the production pipeline, not during release cleanup.

## 5.7 G3 vertical-slice asset status

The small G3 asset set is explicitly authorized before bulk production and is **provisional by default**.

After G3 audition each slice asset either:

1. is promoted to `approved` if it already meets production/rights/mix requirements; or
2. remains provisional with a named replacement task in P6.

This avoids both contradictions: G3 can use real assets without authorizing a library-scale production push, and G5 still forbids shipping provisional content.

### Exit — G1

Layout, source recoverability, stable naming, metadata/provenance, rights, provisional states, and validation contract are defined. Only the explicitly bounded G3 slice may be authored before G3.

---

# 6. P2 — #51 T0 pure contract foundation

**First production-code audio landing.** The normative `ERR-051-001` correction to §2/§4 lands with this code and its mutant/regression proof.

## 6.1 T0 collision pre-check

Before authoring, re-run the project-wide name collision search for **all** #51 public types, explicitly including the two omitted by the original §4.2 pre-check:

- `AssetRef`;
- #51 `CueParams`.

Any collision discovered is resolved in the T0 design/spec/code landing, not papered over after compile failure.

## 6.2 Production assembly

`src/audio/` → `TacticalDirector.Audio`, `noEngineReferences: true`, zero production assembly references.

## 6.3 Land

- `AudioConstants`;
- `AudioBus`;
- `CueKey`;
- `CaptionId`;
- `CaptionDecision`;
- T0-resolved `AssetRef` — must carry a variant **set**, not only a single asset, because member
  selection is host-owned (§7.2); the T0 regression proof covers the multi-variant case;
- T0-resolved #51 `CueParams`;
- `CueEntry`;
- `CueCatalogue`;
- `DuckingRow`;
- `DuckingTable`;
- test assembly.

## 6.4 Required host-free locks

- bus ordinals stable and APPEND-only;
- `Master` cannot be a cue routing target;
- duplicate `CueKey` refused;
- `default(CaptionDecision)` refused;
- unjustified `NoCaption` refused;
- duck trigger==target refused;
- attenuation cycle refused;
- #51 production asmdef references nothing;
- no sim type/RNG/save state;
- no Unity type.

### Exit — G2

T0 code + normative ERR-051-001 discharge + tests are green. The game remains exactly silent.

---

# 7. P3 — T1 playback contract, mixer math, settings fragment

## 7.1 Land

- `IAudioPlayback`;
- pure `AudioMixer` gain composition;
- `AudioSettingsFragment`;
- master/per-bus gain+mute validation;
- invalid-field reset-to-default logic;
- duck-gain computation from bus activity.

## 7.2 Named display-side randomness source — host-owned selection

FR-AU-033 forbids deterministic-sim RNG. The replacement is explicit, and it is **not** a type inside
`TacticalDirector.Audio`:

**variant selection is host-owned. `AssetRef` exposes the variant set; the Unity host binding picks a
member from it using a display-only PRNG whose state is client-local, never serialized, and never exposed
to sim.**

Rules:

- `TacticalDirector.Audio` declares **no PRNG, no seed, no cursor and no selection state**. It stays a
  pure value-type assembly whose T0/T1 tests can assert purity without carve-outs.
- Given the same `CueKey`, #51's contract yields the same `AssetRef` — the *set*, not a member. Choosing
  the member is a presentation act and belongs on the same side of the boundary as `AudioSource`.
- The host seeds its selector once at composition from non-simulation entropy. No
  `DeterministicRngService`, domain tag, stream cursor, save field, or simulation seed is permitted.
- The exact PRNG is selected in P4B host code review for the target Unity surface. Nothing about it is
  #51's to specify beyond the prohibition above.

**Why host-side rather than #51-owned.** A seeded PRNG inside #51 would be mutable state in a leaf
assembly whose entire discipline is construction-time refusal over immutable value types, and it would be
a type §4.2's file inventory does not declare — the same defect class as ERR-051-001, introduced
deliberately one tier later. Keeping selection host-side removes the type instead of recording it.

**T0 consequence for ERR-051-001.** Because the host selects, `AssetRef` MUST be able to carry a variant
*set*, not just a single asset (Appendix B.2 already describes it as *"one asset, or a variant set for
display-side variation"*). That requirement is part of the ERR-051-001 discharge scope: whatever concrete
shape T0 lands for `AssetRef` has to satisfy it, and the T0 regression proof must cover the multi-variant
case, not only the single-asset one.

## 7.3 Settings branch

#51 defines only its fragment. It creates no file/path/serializer.

- If the #38 client-local settings store surface is available, bind to it in P5.
- If unavailable/declined, FR-AU-022 is the normative fallback: **in-memory settings with persistence deferred**.

No private sixth audio store is permitted.

### Exit

T1 host-free green and still silent.

---

# 8. D48 — external prerequisite for audible match integration

Before P4A begins, verify that #48 T0 has landed and actually provides the specified `CueId`, `CueParams`, and `ICueSink` surfaces.

Until D48 is green:

- P1/P2/P3 may continue;
- UI/crowd/music production may not scale beyond G3 prototypes because the complete runtime architecture is still unproven;
- no fake local copy of #48's identities may be created in #51 or the shell;
- P4A/P4B remain **BLOCKED**, not "planned as if built".

The audio workstream may coordinate with the separate no-code-spec implementation stream, but #48 T0 remains its own governed landing.

---

# 9. P4A — T2 shell mapping/completeness, host-free and still silent

**Requires D48.**

Land outside #51:

- shell `CueSinkAdapter`;
- typed `CueId → CueKey` table;
- build-time proof every emit-able `CueId` resolves;
- reverse proof every mapping target exists;
- runtime unmapped cue = silent no-op;
- fully qualified #48/#51 `CueParams` translation.

## 9.1 Observer-neutrality moves here

P4A is the first landing that wires sim/presentation output through the shell into the audio contract. Therefore the unconditional neutrality lock lands **now**, not at G3:

- same seed + same commands with audio adapter enabled vs no-op sink → byte-identical digest chain;
- every deterministic RNG cursor unchanged;
- no audio state serialized.

This is host-free and should fail if the adapter acquires a sim read or deterministic draw.

### Exit

Shell mapping/completeness and neutrality are green while playback is still silent.

---

# 10. P4B — Unity host binding + G3 audible vertical slice

**Requires P4A.**

## 10.1 Unity host boundary

Add the smallest Unity-side binding required to resolve/play assets, route buses, apply gain/mute, expose bus activity for ducking, and stop/replace as required by the pure API.

The **variant selector lands here too** (§7.2): given an `AssetRef` carrying a variant set, the host picks
the member with its client-local display PRNG, seeded once at composition from non-simulation entropy. It
is host state, so it never reaches a save, a digest or a sim read — which the P4A neutrality lock already
proves and continues to prove once this binding exists.

Unity types stay out of `TacticalDirector.Audio`.

## 10.2 `AudioMixer` collision discipline

#51 declares `TacticalDirector.Audio.AudioMixer`; Unity declares `UnityEngine.Audio.AudioMixer`.

Every host file that can see both MUST fully qualify both types **from the first draft**. Do not rely on a broad `using` or a misleading alias that makes the identity invisible in review. This is the same prevention rule already specified for the two `CueParams` types.

## 10.3 Vertical-slice assets

Only this set is authorized before G3:

- UI: one confirm/click + one notification;
- SFX: whistle + ball strike;
- Crowd: one seamless bed + one reaction;
- Music: one seamless loop;
- Commentary: no spoken line required.

At least one match cue must traverse the real `#48 → shell → #51 → Unity` path.

## 10.4 G3 pass/fail

Pass only if:

- every slice cue resolves to defined `CueKey` and runtime asset;
- an intentionally broken mapping is caught at build/test time while runtime behavior remains silent/no-throw;
- UI/SFX/Crowd/Music route to correct buses;
- master/per-bus gain and mute work on real output;
- ducking derives from bus activity, never sim polling;
- P4A neutrality locks remain green;
- no forbidden #51 dependency appears;
- source→master→runtime→catalogue→Unity→audition workflow is repeatable;
- every slice asset has provenance and explicit provisional/approved state;
- CPU/memory/voice/latency measurements are captured;
- native Unity audio is either accepted through EA or rejected with concrete measured evidence before bulk production.

### Exit — G3

End-to-end audio architecture is proven. Bulk production may now begin.

---

# 11. P5 — Settings and captions, split by dependency

## P5A — Settings

- Bind the #51 fragment to #38's single client settings store **if that surface exists**.
- Expose Master + Music/SFX/Crowd/Commentary/UI gain/mute.
- Corrupt fields default and launch continues.

**Fallback branch:** if the shared store is not available/accepted, use FR-AU-022 in-memory settings. G4/G6 must record which branch is active; lack of persistence does not authorize a private file.

## D49 — caption renderer prerequisite

Before P5B begins, verify #49 T0 exists and can legitimately add the inbound `#49 → #51` reference required by #51 §4.5.

## P5B — Caption rendering

**Requires D49.**

- #49 renders #51-owned `CaptionId` identities;
- information-bearing cues receive localized caption text;
- ambience/texture may use justified `NoCaption`;
- #51 never emits display strings.

### G4 definition

G4 is two-axis rather than falsely all-or-nothing:

- **G4A settings:** PASS with either the shared #38 store or explicitly recorded FR-AU-022 in-memory fallback.
- **G4B captions:** BLOCKED until D49, then PASS when renderer coverage is complete.

Early production can proceed after G3 while G4B is blocked. **EA release G6 still requires G4B unless the owner explicitly changes the EA accessibility scope.**

---

# 12. P6 — Scaled content production

Every asset batch follows:

1. brief;
2. source + provenance;
3. edit;
4. lossless master;
5. runtime export;
6. catalogue/production registration;
7. Unity import;
8. in-game audition;
9. mix/repetition review;
10. approve/revise.

## Batch order

### B1 — UI

Semantic UI confirmation/back/toggle/success/error/notification families. Avoid one-off sounds tied to screen coordinates or temporary layouts.

### B2 — Core match SFX

Whistles, ball strike/pass/shot families, goal/net impacts where presented, tackles/contacts where stable #48 cues exist, and restart/substitution/stoppage only where presentation identities are stable.

### B3 — Crowd

Layered bed + positive/negative swells + goal reaction + additional reactions only where stable presentation cues exist. #51 never derives these by polling match state.

### B4 — Music

Menu identity, management/background loop set, optional match/replay state from presentation/navigation context, then transitions/stingers after the base loops are stable.

### B5 — Management ambience/secondary notifications

Add only when real consuming screens demonstrate value.

### B6 — Spoken commentary

Separate deep-tier project requiring its own voice/localization/pronunciation/rights/consent/streaming/build-size plan before promotion.

## Asset approval definition

`approved` requires all of:

- correct semantic purpose in game;
- correct catalogue bus;
- no clip/click/broken loop/unintended silence;
- acceptable repetition/variation in context;
- recoverable source package;
- complete provenance/rights;
- caption classification complete;
- runtime import reviewed;
- not provisional;
- approval recorded.

### Exit — G5

Every EA-required cue family has approved assets; no referenced mapped cue is missing or provisional.

---

# 13. P7 — Mix, performance, regression

## 13.1 Mix order

1. master/headroom;
2. UI intelligibility;
3. core match SFX;
4. crowd;
5. music;
6. ducking;
7. optional commentary last.

Loudness/mix values are measured production tuning, not speculative spec constants.

## 13.2 Unity-host measurements

- active voices;
- resident audio memory;
- stream/decode CPU;
- first-play latency;
- transition behavior;
- long-session leakage;
- build-size contribution by family.

## 13.3 Host-free regression

- catalogue validity;
- caption decisions;
- mapping completeness;
- settings validation;
- assembly-direction locks;
- P4A observer neutrality;
- production metadata/provisional/rights validation.

## 13.4 Host-gated regression

- real playback/routing;
- gain/mute;
- ducking envelope;
- loop seams;
- import/load behavior;
- memory/CPU/latency;
- subjective mix acceptance.

---

# 14. P8 — Early Access release gate

G6 requires:

- zero referenced provisional assets;
- zero missing `CueId → CueKey` mappings;
- zero missing runtime assets for mapped `CueKey`s;
- G4B caption renderer coverage for information-bearing cues, unless explicitly waived by a new owner accessibility decision;
- complete rights/provenance;
- no accepted clipping or broken loop;
- settings branch recorded:
  - shared #38 persistence if available, **or**
  - explicit FR-AU-022 in-memory fallback — never private persistence;
- audio-on/audio-off digest and RNG locks green;
- #51 remains a leaf with no Unity/#48/#49/sim references;
- host playback checks pass on pinned Unity;
- build-size/memory impact reviewed;
- final subjective pass covers menu → management → full match → post-match.

---

# 15. Dependency and PR map

```text
Owner accepts G0 + roadmap amendment
       |
       +----------------------------+
       |                            |
 P1 pipeline                   P2 #51 T0 + ERR discharge
       |                            |
       |                        P3 #51 T1
       |                            |
       |                         [D48]
       |                            |
       +----------------------- P4A shell + neutrality
                                    |
                                P4B audible slice
                                    |
                                  G3
                       /------------+-------------\
                      /                            \
              P5A settings                    P6 content
                  |                              |
             [D49] → P5B captions               |
                      \                           /
                       \--------- P7 mix -------/
                                    |
                                   G6
```

Recommended PR boundaries:

1. **Audio planning/G0** — this plan + roadmap amendment + ERR recording/tracking only.
2. **P1 pipeline substrate** — folders/metadata/validator contract; no library-scale binaries.
3. **P2 T0** — normative ERR-051-001 discharge + pure #51 code + tests.
4. **P3 T1** — mixer/settings/display-random pure logic + tests.
5. **P4A** — #48 shell mapping/completeness + observer-neutrality proof; still silent.
6. **P4B** — Unity binding + minimal G3 assets + host evidence + middleware decision.
7. **P5A/P5B** — settings and captions may be separate because their dependencies differ.
8. **P6 batches** — small binary-reviewable batches, not one giant asset PR.
9. **P7/P8 hardening** — measured tuning, regression, release validation.

---

# 16. Prohibitions

Do not:

- treat #48/#49 as already implemented;
- let audio work block the main PM-2 simulation path;
- mass-produce content before G3;
- normatively fix ERR-051-001 before T0 code/test exists;
- put Unity types in `TacticalDirector.Audio`;
- let #51 reference #48, #49, or sim;
- create a private audio settings file;
- drive ducking from score/possession/world state;
- use deterministic-sim RNG for variation;
- declare a PRNG, seed, cursor or selection state inside `TacticalDirector.Audio`;
- encode bus or revision into runtime filenames;
- change asset paths for ordinary remasters;
- approve unknown-rights content;
- allow referenced provisional content into release;
- make spoken commentary a default EA blocker;
- adopt middleware without G3 evidence;
- treat host-free green tests as proof the game sounds correct.

---

# 17. Final critique and implementation-readiness conclusion

The final review specifically tested the plan for the failure modes found externally:

- **Roadmap contradiction:** closed by a narrow explicit audio-parallel amendment; audio remains non-critical to PM-2.
- **Phantom #48/#49 consumers:** closed by D48/D49 hard gates.
- **C6 misread:** closed; ERR-051-001 is recorded now but normative contract repair waits for T0 code/test.
- **Under-scoped collision analysis:** closed; `AssetRef`/`CueParams` are rechecked at T0 and `AudioMixer` is explicitly fully qualified at the Unity host boundary.
- **Filename drift:** closed; bus/revision removed from filenames; revisions preserve path/GUID.
- **ERR-038 assumption:** closed by explicit shared-store vs FR-AU-022 fallback branches.
- **Vertical-slice ambiguity:** closed; slice files are provisional by default and either promoted or replaced.
- **Neutrality timing:** moved to P4A, the first wired host-free landing.
- **Variation source:** named, and placed **host-side**. `AssetRef` exposes the variant set; the host
  selects a member with a client-local, non-serialized display PRNG. `TacticalDirector.Audio` declares no
  randomness type at all, so the leaf keeps its purity property and no undeclared type is introduced.
- **Historical supplement:** explicitly superseded by the approved spec.

No substantial implementation should begin until G0 is accepted. After acceptance, P1 and P2 are safe parallel first slices; P4 remains blocked until D48 is genuinely green.

---

## Version history

| Version | Date | Notes |
|---|---|---|
| 1.0 | 2026-09-04 | Initial converged plan after two high-level and two detailed critique/revision rounds. |
| 1.1 | 2026-09-04 | Corrected initial C6 interpretation and added PR boundaries/source recoverability. |
| 1.2 | 2026-09-06 | External-review close-out: roadmap amendment; explicit D48/D49 gates; C6 record-vs-discharge correction; expanded collision checks; stable filename/GUID rule; ERR-038 fallback; provisional G3 status; P4A neutrality; named display PRNG; historical supplement explicitly superseded. |
| 1.3 | 2026-09-07 | **Variant selection moved host-side (§7.2, §6.3, §10.1, §16).** v1.2 placed a display PRNG inside `TacticalDirector.Audio`, which would have put seeded mutable state in a leaf whose T0/T1 tests assert purity, and introduced a type §4.2's file inventory does not declare — the same defect class as ERR-051-001, one tier later. `AssetRef` now exposes the variant **set**, the Unity host binding selects the member with a client-local non-serialized display PRNG, and #51 declares no randomness type at all. Consequent T0 obligation added to the ERR-051-001 discharge scope: `AssetRef` must carry a variant set and the regression proof must cover the multi-variant case. FR-AU-033 is satisfied identically; nothing about the sim-RNG prohibition is relaxed. |
