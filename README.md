# Tactical Director: Football Management Simulation

**Created:** December 30, 2025, 11:50 AM PST
**Last Updated:** July 14, 2026 (**Match-flow model completion LANDED** — throw-ins, corners,
goal kicks, fouls/cards, offside, substitutions, half-time break, and full-time end (previously
only kickoff + goal-restart existed). Design doc adversarially reviewed to convergence, then
implemented, then the code itself adversarially reviewed to convergence (catching an
`OffsideEvaluator` bug where too few defenders left an accumulator at an `Infinity` sentinel
instead of `NaN`, inverting the offside rule for every finite attacker position). New
`src/match-engine/RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; three new
Tier A events (`OffsideCalledEvent` 0x18, `RestartAwardedEvent` 0x19, `MatchPhaseChangedEvent`
0x1A). `MatchEngine.cs` v1.31 gains restart routing, per-tick foul/card detection with sent-off
tracking, offside evaluation on pass reception, a public `SubstitutePlayer` (bench-roster swap,
pending-event queue flushed at the next Resolve phase per an AR-5 fix), and half-time/full-time
transition handling. **`SNAPSHOT_SCHEMA_VERSION` 14 → 15.** New test suites for restarts, offside,
fouls/cards, substitutions, and match-flow transitions. Full dotnet gate not runnable in this
environment — verified by exhaustive manual code review in place of `dotnet test`. See
`docs/tracking/match-flow-completion-design.md`, `docs/tracking/match-engine-design.md` v2.0, and
`src/CLAUDE.md` v2.17.)
**Last Updated (prior):** July 13, 2026, later same day (**P1 real perf harness landed** —
`StopwatchPerfHarness` + `MatchEngineCapstonePerfHarness` boot the real capstone scenario and
Stopwatch-time each `RunTick`, superseding the synthetic `run.sh` stub that ran no `src/` code.
The Linux run is stamped non-certifying; a certified number still needs pinned-host access
(Windows 11 / Unity 6000.4.9f1) plus a real cert run. `CertifiedPerfBaseline.Stage0CertPlatformPin`
updated to the Unity-6 tuple string.)
**Last Updated (prior):** July 13, 2026 (**Unity engine version bumped: 2022.3.62f1 → Unity
6000.4.9f1, graphics API pinned DX11 — documentation-only, no recertification performed.**
`certification-platform.md` → v1.3, Status reverted `✅ PINNED` → `⏳ RECERT REQUIRED` per its own
Maintenance Rule; every downstream unblocker it previously closed (`FR-DS-009-GATE`, `FR-PO-052`,
the §7.5 D1 test-runner pin, `EnvironmentFingerprint`) is blocked again until a real cert run
executes against the new tuple. Historical `Unity 2022.3` citations inside already-`APPROVED`
spec section files deliberately left untouched — frozen approval-time records, per this project's
"historical rows preserved verbatim" convention.)
**Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate landed** — goal
detection (`MatchEngine.cs` v1.30: Resolve-phase goal check + scoring + centre-spot restart +
`GoalAwardedEvent`) and the match-length/halves model (`MATCH_TICKS_TOTAL` = 324,000 ticks,
`HALF_TIME_BOUNDARY_TICK` = 162,000). **`SNAPSHOT_SCHEMA_VERSION` 13 → 14.** This activates
Tactical Presets **#26**'s half-time trigger and live goalDiff/clock ladder inputs — its
engine-substrate gates (§9.1) are now closed; only the §9.2 `[GT]` balance-pass review remains.
Full dotnet gate: PASSED, 0 failures.)
**Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25/#26 wiring landed**, all
default-behaviour-neutral — Balanced ⇒ Off/None/Off/Human are exact identities, byte-identical
default match: the #24 build-up overlay + #23 dismark offset `SlotComposer` stages;
`positioning-ai/RotationController.cs` (#25) wired into `PositioningAITick`; the #23 marked-pass-
target penalty in the Decision Tree's `UtilityScorer`; and **#26**'s full T1–T4 stack (preset→
config projection, kickoff/interval decision gate, kickoff scoring, mid-match adaptation ladder)
via `ManagerDecisionGate`/`ManagerProfile`/`ManagerAdaptation`. `SNAPSHOT_SCHEMA_VERSION` 11 → 12
→ 13 across the two commits. Full dotnet gate: PASSED, 0 failures both times.)
**Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 — Dismarking &
Marker-Awareness AI, Scripted Build-Up Structures, Positional Rotations, Tactical Presets &
AI-Manager Selection — all advanced `IN REVIEW` → `APPROVED`.** PASS-1 section-file adversarial
reviews resolved same day; lead-developer R-01..R-05 sign-off granted on all four; the seven
cross-spec back-prop ERRs filed and landed atomically; the #26 Bradley citation VERIFIED. `docs/
specs/dismarking-ai/`, `build-up-structures/`, `positional-rotations/`, `tactical-presets/`
folders now exist. **`SPEC_INDEX.md`: 26 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** T0 scaffolding
(behaviour-neutral data types + pure math) landed the same day.)
**Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 gates closed where closable** — §8
citations verified/replaced/reclassified across all four specs (one #26 row remains pending with
a recorded environment-blocked attempt); #25 Appendix A completed for all three formation
families; #26 A.1 preset compositions pinned against the #21 enums. Remaining before `APPROVED`:
the #26 Bradley citation, back-prop ERRs, #26 engine-substrate gates, R-01..R-05 sign-off.)
**Last Updated (prior):** July 8, 2026 (Candidates **#23–#26 promoted to section files at `IN REVIEW`** —
`docs/specs/dismarking-ai/` (#23), `docs/specs/build-up-structures/` (#24),
`docs/specs/positional-rotations/` (#25), `docs/specs/tactical-presets/` (#26), each a full
11-file spec set (v0.1) authored from its July 7 design supplement per that supplement's §6
promotion pipeline. `SPEC_INDEX.md` now reads **22 APPROVED / 4 IN REVIEW**; the RESERVED entries
are retired. Section-file PASS-1 adversarial reviews are pending per each spec's §9.3 — no `src/`
code lands until each is `APPROVED`.)
**Last Updated (prior):** July 7, 2026, later same day (Two design supplements opened, scoping the four
items the same day's tactical-theory cross-reference flagged as too large for a cheap seam
reuse: `docs/tracking/advanced-positional-behaviors-design.md` (dismarking, scripted build-up
structures, positional rotations — candidate specs #23–#25) and
`docs/tracking/game-model-ai-manager-design.md` (tactical preset library + AI-manager
selection/adaptation — candidate #26). DESIGN SUPPLEMENT stage only, pre-promotion, no code —
see root `CLAUDE.md` OPEN ISSUES and `SPEC_INDEX.md` "RESERVED" section.)
**Last Updated (prior):** July 7, 2026 (Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8 — a `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius), a Positioning AI #12 rest-defense coverage check (dampens risky PASS/SHOOT/DRIBBLE when insufficient cover is left behind while attacking), a half-spaces PASS bonus (routes each agent's existing #12 lane into the Decision Tree #8 utility scorer), and a curving-press blind-side bias (#13 nudges the primary presser's approach toward the ball carrier's blind side). All four default to today's exact behaviour (byte-identical) until a manager sets a non-default tactic. `SNAPSHOT_SCHEMA_VERSION` 10 → 11. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Last Updated:** July 7, 2026, later same day (Tactical-theory research cross-reference follow-up: after user review, three of the four July 7 tactical additions were corrected. A `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius) stands unchanged. The rest-defense coverage check (Positioning AI #12) is redesigned so its PASS/SHOOT/DRIBBLE dampener only applies in proportion to the ball carrier's own tactical awareness — an unaware carrier is never silently corrected for what is really a manager-facing setup flaw. The half-spaces PASS bonus is reverted entirely — half-spaces are an exploitable spatial gap that requires tactical/player instructions to exploit, not a flat statistical bonus. The curving-press mechanic is redesigned from a flat "blind-side approach" bias into a cover-shadow curve (#13): a presser now bends their pursuit path toward denying a nearby passing option, scaled by their own defensive/physical/mental attributes, so a poor or low-effort defender barely curves at all. `SNAPSHOT_SCHEMA_VERSION` 10 → 11 (unaffected by the corrections — none of the reverted/redesigned items were ever serialized). Prior July 7, 2026 (initial landing, since corrected): Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Project Type:** Full-scale football management simulation
**Development Timeline:** Open-ended passion project, staged releases
**Target:** The Football Manager Killer
**Current Stage:** Stage 0+1 — Implementation (coding begun May 19, 2026). All 26 approved specs
(20 Stage-0 + 6 Stage-1 forward specs #21–#26) have active `src/` implementations; the match
engine composition root now includes goal detection, halves/full-time, and the complete
match-flow model (throw-ins, corners, goal kicks, fouls/cards, offside, substitutions).

---

## PROJECT VISION

Build the definitive football management simulation with:
- **Observable Systems:** Every tactic has visible, measurable impact
- **Deep Physics:** Sim-quality ball physics and player movement
- **Psychological Depth:** Complex social and mental systems
- **Transparent Mechanics:** Users understand WHY things happen

---

## DOCUMENTATION HIERARCHY

### PRIMARY AUTHORITY

**[Master Development Plan v1.0](docs/planning/master-development-plan.md)**
The definitive 10-year roadmap. All development decisions reference this document.

**What it contains:**
- 6 development stages (Stage 0 through Stage 5)
- Quality gates and success criteria
- Technical architecture foundation
- Resource requirements and timelines
- Risk mitigation strategies

**Current stage:** Stage 0 / Stage 0+1 — Physics Foundation (Year 1)
**Current phase:** Implementation + AR-hardening (Stage 0 specs all APPROVED May 18, 2026; coding began May 19, 2026; AR-hardening sweep complete June 7, 2026)

---

### DESIGN REFERENCE LIBRARY

These four volumes describe the FULL vision. Features are implemented incrementally across stages.

**[Master Volume I: Physics & Simulation Core](docs/planning/master-vol-1-physics-core.md)**
The 10Hz heartbeat, match logic, physical environment, and tactical micro-physics.

**Implementation timeline:**
- Stage 0-1: Core physics (ball, movement, basic AI)
- Stage 2: Match simulation with statistics
- Stage 5: Full environmental physics and 85,000 entity simulation

---

**[Master Volume II: Human Systems & Behavioral Sociology](docs/planning/master-vol-2-human-systems.md)**
Psychology, social graphs, communication, atmosphere, and personality evolution.

**Implementation timeline:**
- Stage 2: Basic morale and form
- Stage 4: Full H-Gate system, social graphs, media interactions
- Ongoing: Personality evolution and social dynamics

---

**[Master Volume III: Club Operations & Strategic Management](docs/planning/master-vol-3-club-operations.md)**
Tactical systems, recruitment, finance, governance, youth development, and training.

**Implementation timeline:**
- Stage 1: Tactical system (formations, instructions)
- Stage 2: Basic squad management and transfers
- Stage 3: Full financial systems, staff, infrastructure
- Stage 4+: Deep recruitment AI, board dynamics

---

**[Master Volume IV: Technical Implementation & Systems Engineering](docs/planning/master-vol-4-tech-implementation.md)**
Architecture, optimization, data ontology, tooling, modding support, and UI/UX.

**Implementation timeline:**
- Stage 0: Core architecture (Fixed64, DoD, event system)
- Stage 1: 2D rendering, UI framework
- Stage 2: Save system, database architecture
- Stage 3+: Advanced tooling, modding API, multiplayer infrastructure

---

### SUPPORTING DOCUMENTATION

**[Development Best Practices](docs/planning/development-best-practices.md)**
Technical wisdom extracted from project analysis. Read before starting each stage.

**What it contains:**
- Performance profiling methodology
- Testing strategies
- Technical debt prevention
- Anti-patterns to avoid
- Quality gate framework
- Optimization guidelines
- Specification writing standards
- **Specification approval checklist template**

**When to reference:**
- Before starting each development stage
- When making major technical decisions
- During code reviews
- Monthly as process reminder

---

## CURRENT STATUS

### Stage 0+1: Physics Foundation — Implementation

**Goal:** Build the physics and simulation core that all future systems depend on.

**Progress:** Implementation Phase (coding begun May 19, 2026)
**Spec Phase Started:** February 2, 2026
**Spec Phase Completed:** May 18, 2026 — all 20 Stage-0 specs APPROVED; 6 further Stage-1 forward specs (#21 Jun 20, #22 Jun 22, #23–#26 Jul 10) APPROVED since
**Deliverables:** 26 comprehensive specification documents + active `src/` implementation for specs #1–#8, #10–#19, #21–#26, plus the unnumbered Match Engine composition root

**Summary (July 14, 2026):** All 20 Stage-0 specs APPROVED, plus 6 Stage-1 forward specs — #21
Tactical Instructions, #22 Living World System, #23 Dismarking & Marker-Awareness AI, #24
Scripted Build-Up Structures, #25 Positional Rotations, #26 Tactical Presets & AI-Manager
Selection — all APPROVED. Every spec #1–#20 plus #21–#26 has an active `src/` implementation. A
non-certifying Linux dotnet compile/test CI gate (`tools/dotnet-ci/`) compiles the entire `src/`
tree and runs the full NUnit suite on every push; quarantine list is empty. **Match Engine**
(`src/match-engine/`, design note at `docs/tracking/match-engine-design.md`) — the composition
root wiring every subsystem into the deterministic-sim 7-phase tick pipeline — now includes goal
detection + the match-length/halves model (Jul 11), the #21–#26 tactical wiring (Jul 10–11), and
the **complete match-flow model** (Jul 14): throw-ins, corners, goal kicks, fouls/cards
(sent-off tracking), offside, substitutions, and half-time/full-time transitions.
**`SNAPSHOT_SCHEMA_VERSION` is now 15.** The Unity engine target was bumped 2022.3.62f1 → Unity
6000.4.9f1 (DX11) on Jul 13 — documentation-only; the Stage-0 platform-certification pin reverted
to `⏳ RECERT REQUIRED` pending a real cert run on the new tuple. `src/CLAUDE.md` coding guide
(v2.17) governs all C# authoring.
**Total specification output:** ~5 MB+ across 120+ files (see file-manifest.md)
**Implementation active:** #1–#20 implemented; #21–#26 all implemented and wired (behaviour-neutral
at their identity defaults until a manager/preset sets non-default tactics); match-engine
composition root includes goal detection, halves/full-time, and the full match-flow model.

---

### Specification Writing Schedule

**Priority 1 — Physics Foundation:**
1. ✅ Ball Physics — **APPROVED** Feb 8, 2026 (~50 pages, 44 tests, audited)
2. ✅ Agent Movement — **APPROVED** Apr 27, 2026 (~130 pages, 83 tests, audited)
3. ✅ Collision System — **APPROVED** Feb 19, 2026 (~50 pages, ~70 tests, audited; §9 back-filled Apr 26)
4. ✅ First Touch Mechanics — **APPROVED** Feb 22, 2026 (~70 pages, 72 tests, audited)
5. ✅ Pass Mechanics — **APPROVED** May 6, 2026 (audit March 25 — all 19 findings + F-A01/F-A02 resolved; re-approved May 6)

**Priority 2 — Core Gameplay:**
6. ✅ Shot Mechanics — **APPROVED** Apr 27, 2026 (~70 pages, 104 tests, audited)
7. ✅ Perception System — **APPROVED** Apr 22, 2026 (~80 pages, 92 tests; §9 v1.7 signed off)
8. ✅ Decision Tree — **APPROVED** Apr 27, 2026 (~55+ pages, draft-level quality gate; comprehensive audit candidate before implementation)
9. ✅ Fixed64 Math Library — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented deferral; §9.8 implementation-time deliverables remain post-APPROVED follow-ups. Implementation deferred to Stage 5 per §8.1 v0.2 stage-gating.)

**Priority 3 — Advanced Physics:**
10. ✅ Heading Mechanics — **APPROVED** May 16, 2026 (PASS-1 + OI closure + R-01..R-05 sign-off same day; ERR-010-001 resolved: `DOMAIN_TAG_HEADING = 0x16`)
11. ✅ Goalkeeper Mechanics — **APPROVED** May 18, 2026 (ERR-011-001 resolved: `DOMAIN_TAG_GOALKEEPER = 0x1D`; 44 FRs; ~79 constants)
12. ✅ Positioning AI — **APPROVED** May 18, 2026 (ERR-012-001 resolved: `DOMAIN_TAG_POSITIONING_AI = 0x17`; 47 active FRs; no `[EST]` remain)

**Priority 4 — Tactical AI:**
13. ✅ Pressing AI — **APPROVED** May 17, 2026 (all gate items resolved; R-01..R-05 signed; 26 constants)
14. ✅ Defensive AI — **APPROVED** May 18, 2026 (ERR-014-001 + ERR-014-004 resolved; 37 FRs; 26 constants)
15. ✅ Attacking AI — **APPROVED** May 18, 2026 (ERR-015-001/002/005 back-props closed; 36 FRs; 85 tests)
16. ✅ Deterministic Simulation — **APPROVED** May 14, 2026 (Tier 2 Final Approval; §9 v1.7; all §9.4.2 gates cleared; golden-vector files hkdf-sha256-kat v1.1, siphash-2-4-kat v1.1, serialize-canonical-corpus v1.0; §8.3.1 cross-spec re-audit complete; ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1)

**Priority 5 — Systems Architecture (Weeks 17-20):**
17. ✅ Event System — **APPROVED** May 13, 2026 (section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete)
18. ✅ Performance Optimization Strategy — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices against #16 APPROVED text + #19 v1.0.1 patch-revised text; KD-2 sequencing satisfied; FR count 82.)
19. ✅ Testing Strategy & Framework — **APPROVED** May 15, 2026 (§9 v1.0 lead-developer sign-off; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices against #16 APPROVED text + #18 v0.3; KD-2 sequencing satisfied. Clears #18's last KD-2 gate.)
20. ✅ Code Standards & Style Guide — **APPROVED** May 11, 2026 (adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete)

**Stage-1 Forward Specs (beyond the Stage-0 set of 20):**
21. ✅ Tactical Instructions — **APPROVED** Jun 20, 2026 (manager-facing tactic input layer driving #8/#11–#15; 32 FRs; G2 `[GT]` balance pass landed Jun 30, 2026; T0 data layer + T2 consumer seams + runtime activation via `MatchEngine.SetTeamTactic` + per-agent `PlayerTactic` + on-disk tactic-file loaders all landed)
22. ✅ Living World System — **APPROVED** Jun 22, 2026 (off-pitch interaction-generation layer over vol-2/vol-3 human systems; 34 FRs; season/world loop (`WorldStore`) + `InteractionTextGenerator` + deep-memory auto-cite all landed at `src/living-world/`; BackgroundTierSim + arc triggers remain documented seams pending upstream canon)
23. ✅ Dismarking & Marker-Awareness AI — **APPROVED** Jul 10, 2026 (perception-derived marking-pressure signal; `SlotComposer` dismark offset stage + Decision Tree marked-pass-target penalty wired Jul 11)
24. ✅ Scripted Build-Up Structures — **APPROVED** Jul 10, 2026 (3-zone team-relative classifier + hysteresis; `SlotComposer` build-up overlay stage wired Jul 11)
25. ✅ Positional Rotations — **APPROVED** Jul 10, 2026 (adjacency-table two-sided rotation with hysteresis; `RotationController` wired into `PositioningAITick` Jul 11)
26. ✅ Tactical Presets & AI-Manager Selection — **APPROVED** Jul 10, 2026 (preset library over #21's parameter space + AI-manager kickoff/interval decision gate and adaptation ladder; full T1–T4 stack + the engine-substrate gates it depended on — goal detection, halves model — all landed Jul 11; only the §9.2 `[GT]` balance-pass review remains)

**Status Key:**
- ⏳ Not started
- 📝 In progress
- 🔍 In review
- ⏸ Suspended
- ✅ Approved
- 🔒 Locked (implementation begun)

**Locked (implementation begun):** Ball Physics #1, Agent Movement #2, Collision System #3, First Touch #4, Pass Mechanics #5, Shot Mechanics #6, Perception System #7, Decision Tree #8, Heading Mechanics #10, Goalkeeper Mechanics #11, Positioning AI #12, Pressing AI #13, Defensive AI #14, Attacking AI #15, Deterministic Simulation #16, Event System #17, Performance Optimization #18, Testing Strategy #19, Tactical Instructions #21 (full — T0/T2/runtime activation/per-agent tactics/disk loaders), Living World System #22 (season/world loop, ArcEngine, text generation), Dismarking & Marker-Awareness AI #23, Scripted Build-Up Structures #24, Positional Rotations #25, Tactical Presets & AI-Manager Selection #26. Fixed64 Math Library #9 and Code Standards #20 have no `src/` implementation by design (#9 implementation deferred to Stage 5+; #20 is a style guide, not a coded subsystem). Plus the unnumbered `src/match-engine/` composition root (goal detection, halves/full-time model, and the complete match-flow model — throw-ins, corners, goal kicks, fouls/cards, offside, substitutions).

**For detailed progress tracking, see:** [docs/tracking/PROGRESS.md](docs/tracking/PROGRESS.md)
**For file inventory, see:** [docs/tracking/file-manifest.md](docs/tracking/file-manifest.md)
**For cross-spec error tracking, see:** [docs/tracking/spec-error-log.md](docs/tracking/spec-error-log.md)

---

## QUALITY GATES

### Stage 0 Quality Gate (Must Pass Before Stage 1)

**Physics Quality:**
- ⬜ Pass trajectory with curve looks realistic
- ⬜ Player acceleration from standstill feels natural
- ⬜ Collisions produce sensible outcomes
- ⬜ AI makes reasonable decisions 90%+ of time

**Performance:**
- ⬜ <5ms per simulation tick (22 agents)
- ⬜ 60 FPS rendering (no stutters)

**Technical:**
- ⬜ Deterministic: Same seed = identical match
- ⬜ Cross-platform: Windows, Mac, Linux produce identical results

**Community:**
- ⬜ Positive feedback: "This looks/feels real"

**If ANY criteria fail → Do not proceed to Stage 1**

---

## DEVELOPMENT PHILOSOPHY

### Core Principles

**1. Quality Over Speed**
- No deadlines pressure
- Each stage must pass ALL quality gates
- Better to delay than ship broken foundation

**2. Specification Before Code**
- Full specification written first
- Community feedback on specs
- Code becomes implementation of spec
- **20 weeks of specs before ANY Stage 0 coding**

**3. Incremental Complexity**
- Start simple, prove it works
- Add complexity in stages
- Each stage is commercially releasable

**4. Community Involvement**
- Share progress openly
- Gather feedback early
- Build audience before launch

**5. Sustainable Pace**
- 40 hours/week maximum
- No crunch (10-year journey)
- Breaks between stages

### Key Architectural Decisions (Established During Specs 1-8)

- **Parameter-based physics:** No type enums (KickType, ShotType, PassType eliminated). Physics systems receive velocity/spin vectors; the Decision Tree supplies intent parameters.
- **Possession is agent state, not ball state:** BallState has no PossessingAgentId field (ERR-008).
- **Write interfaces only when both sides are specified:** Avoids phantom interface proliferation.
- **Struct-based zero-allocation architecture:** All hot-path data uses value types.
- **10Hz heartbeat for tactical decisions; 60Hz for physics:** Decision Tree runs at heartbeat rate; physics systems run per-frame.
- **Fatigue convention:** 0 = fully rested, 1 = fully fatigued (no exceptions).

---

## PROJECT STRUCTURE

```
Soccer-Manager-Pro/
├── CLAUDE.md                           [AI behavioral rules — read first]
├── docs/
│   ├── planning/                       [Master volumes, dev plan, best practices]
│   ├── specs/
│   │   ├── SPEC_INDEX.md               [Canonical spec numbering and status]
│   │   ├── ball-physics/               [Spec #1 — APPROVED]
│   │   ├── agent-movement/             [Spec #2 — APPROVED]
│   │   ├── collision-system/           [Spec #3 — APPROVED]
│   │   ├── first-touch/                [Spec #4 — APPROVED]
│   │   ├── pass-mechanics/             [Spec #5 — APPROVED]
│   │   ├── shot-mechanics/             [Spec #6 — APPROVED]
│   │   ├── perception-system/          [Spec #7 — APPROVED]
│   │   ├── decision-tree/              [Spec #8 — APPROVED (draft-level)]
│   │   ├── fixed64-math/               [Spec #9 — APPROVED]
│   │   ├── deterministic-sim/          [Spec #16 — APPROVED]
│   │   ├── event-system/               [Spec #17 — APPROVED]
│   │   ├── performance-optimization/   [Spec #18 — APPROVED]
│   │   ├── testing-strategy/           [Spec #19 — APPROVED]
│   │   ├── code-standards/             [Spec #20 — APPROVED]
│   │   ├── heading-mechanics/          [Spec #10 — APPROVED May 16]
│   │   ├── goalkeeper-mechanics/       [Spec #11 — APPROVED May 18]
│   │   ├── positioning-ai/             [Spec #12 — APPROVED May 18]
│   │   ├── pressing-ai/               [Spec #13 — APPROVED May 17]
│   │   ├── defensive-ai/              [Spec #14 — APPROVED May 18]
│   │   ├── attacking-ai/              [Spec #15 — APPROVED May 18]
│   │   ├── tactical-instructions/     [Spec #21 — APPROVED Jun 20]
│   │   ├── living-world/              [Spec #22 — APPROVED Jun 22]
│   │   ├── dismarking-ai/             [Spec #23 — APPROVED Jul 10]
│   │   ├── build-up-structures/       [Spec #24 — APPROVED Jul 10]
│   │   ├── positional-rotations/      [Spec #25 — APPROVED Jul 10]
│   │   └── tactical-presets/          [Spec #26 — APPROVED Jul 10]
│   └── tracking/
│       ├── PROGRESS.md                 [Schedule and milestone tracking]
│       ├── spec-error-log.md           [Cross-spec architectural errors]
│       ├── fix-manifest-pass-mechanics.md [Per-audit fix tracking — Pass Mechanics #5]
│       ├── file-manifest.md            [Authoritative file inventory]
│       ├── match-engine-design.md      [Match Engine composition-root design note — not a numbered spec]
│       └── certification-platform.md  [Stage-0 host platform pin]
└── src/                                [Implementation — coding begun May 19, 2026]
    ├── CLAUDE.md                       [Coding guide — read before writing code]
    ├── ball-physics/                   [Spec #1 — relocated May 30, 2026 from src/Core/Physics/Ball/]
    ├── agent-movement/                 [Spec #2]
    ├── collision-system/                [Spec #3]
    ├── first-touch/                     [Spec #4]
    ├── pass-mechanics/                  [Spec #5]
    ├── shot-mechanics/                  [Spec #6]
    ├── perception-system/               [Spec #7]
    ├── decision-tree/                   [Spec #8]
    ├── heading-mechanics/                [Spec #10]
    ├── goalkeeper-mechanics/             [Spec #11]
    ├── positioning-ai/                   [Spec #12]
    ├── pressing-ai/                       [Spec #13]
    ├── defensive-ai/                      [Spec #14]
    ├── attacking-ai/                      [Spec #15]
    ├── deterministic-sim/                 [Spec #16]
    ├── event-system/                       [Spec #17]
    ├── performance-optimization/           [Spec #18]
    ├── testing-strategy/                   [Spec #19 — ScenarioRunner closed-loop harness]
    ├── tactical-instructions/              [Spec #21 — full: T0/T2/runtime activation/per-agent tactics/disk loaders]
    ├── living-world/                       [Spec #22 — season/world loop, ArcEngine, text generation]
    ├── dismarking-ai/                       [Spec #23 — perception-derived marking pressure]
    ├── build-up-structures/                 [Spec #24 — 3-zone build-up classifier]
    ├── positional-rotations/                [Spec #25 — RotationController]
    ├── tactical-presets/                    [Spec #26 — preset library + AI-manager decision gate]
    └── match-engine/                       [Composition root — goal detection, halves/full-time, complete match-flow model; not a numbered spec]
```

Note: Fixed64 Math Library (#9) has no `src/` tree — implementation deferred to Stage 5+. Code Standards (#20) is a style guide, not a coded subsystem.

---

## NEXT IMMEDIATE STEPS

### Outstanding (as of July 14, 2026)

**Specification phase:** ✅ COMPLETE for the Stage-0 set of 20 (May 18, 2026), plus 6 Stage-1 forward specs APPROVED (#21 Tactical Instructions Jun 20; #22 Living World System Jun 22; #23–#26 Jul 10). `SPEC_INDEX.md`: **26 APPROVED / 0 IN REVIEW / 0 NOT STARTED.**

**Implementation phase — active frontier:**
1. 🔒 Specs #1–#8, #10–#20 — all coded, AR-clean, and covered by the non-certifying Linux `dotnet-ci` gate (full `src/` tree compiles; full NUnit suite; quarantine list empty).
2. 🔒 **Match Engine** (`src/match-engine/`, unnumbered composition root) — goal detection + the match-length/halves model landed Jul 11 (`SNAPSHOT_SCHEMA_VERSION` 14); the **complete match-flow model** (throw-ins, corners, goal kicks, fouls/cards with sent-off tracking, offside, substitutions, half-time break, full-time end) landed Jul 14 (`SNAPSHOT_SCHEMA_VERSION` 15). Both landings were adversarially reviewed (design + code) to convergence.
3. 🔒 **Tactical Instructions (#21)** — fully landed: T0/T2 consumer seams, runtime activation, per-agent `PlayerTactic` routing, the G2 balance pass, and on-disk team/player tactic-file loaders. No remaining Stage-1 leftovers.
4. 🔒 **Living World System (#22)** — season/world loop (`WorldStore`), `ArcEngine` + `ActiveSetMembership`, `InteractionTextGenerator` (incl. deep-memory auto-cite) all landed. Remaining seams (BackgroundTierSim summaries, arc trigger evaluators + `world.arcs` stream registration) are deliberately unbuilt — they'd consume vol-2/vol-3 canon and match-outcome events that don't exist yet (phantom-interface rule).
5. 🔒 **Specs #23–#26** — all APPROVED and wired Jul 10–11: dismarking (#23) and build-up (#24) `SlotComposer` stages, positional rotations (#25) `RotationController`, and tactical presets (#26)'s full T1–T4 stack (preset→config projection, decision gate, kickoff scoring, adaptation ladder) — the last gated on the goal-detection/halves-model engine substrate, which landed the same week. Only the #26 §9.2 `[GT]` balance-pass review remains, non-blocking.
6. ⏳ **Unity 6 recertification** — the engine target moved to Unity 6000.4.9f1 (DX11) on Jul 13, documentation-only. `certification-platform.md` reverted to `⏳ RECERT REQUIRED`; a real cert run on the new tuple (needs pinned Windows-11/Unity-6 host access, not available in this environment) is required before `FR-DS-009-GATE`/`FR-PO-052`/the §7.5 D1 test-runner pin/`EnvironmentFingerprint` re-unblock.
7. ⏳ **FR-PO-052 certified perf baseline** — the real Stopwatch-based harness (`StopwatchPerfHarness` / `MatchEngineCapstonePerfHarness`) landed Jul 13, superseding the old synthetic stub; the match-engine kickoff entry is still **Pending** (needs the same pinned-host cert run as above).

**Operations / housekeeping:**
8. ⏳ Push 12+ approval tags (HTTP 403 branch-protection); run squash-merge precondition check from CLAUDE.md OPEN ISSUES before `git push --tags`.
9. ⏳ Verify the `first-touch/section-7.md` smart-quote regression (flagged historically; status unverified).

**Resolved since the last update (no longer outstanding):**
- ✅ Structural fix: Ball Physics moved `src/Core/Physics/Ball/` → `src/ball-physics/` (May 30, 2026).
- ✅ Stage-0 host-platform pin landed in `docs/tracking/certification-platform.md` (Jun 7, 2026) — unblocked `FR-DS-009-GATE` and `FR-PO-052` activation (**since reverted to RECERT REQUIRED by the Jul 13 Unity 6 bump, see item 6 above**).
- ✅ Dotnet CI gate quarantine burn-down — all 30 quarantined tests adjudicated; quarantine list empty (Jun 13, 2026).
- ✅ Design supplements for #23–#26 promoted to full spec sets, approved, and implemented (Jul 7–14, 2026).

---

## ARCHIVED DOCUMENTS

The following documents have been superseded and moved to `/Archive/`:

**1. Focused_Tactical_Sim_Plan.md**
- **Reason:** Proposed 18-month simplified scope, contradicts Master Plan's 10-year full simulation vision
- **Superseded by:** Master Development Plan v1.0
- **Date archived:** December 30, 2025

**2. CRITICAL_ANALYSIS_Development_Risks.md**
- **Reason:** Valid warnings extracted into docs/planning/development-best-practices.md
- **Superseded by:** Development Best Practices
- **Date archived:** February 3, 2026

**3. Architecture_Plan_Critique.md**
- **Reason:** Conflicts resolved in Master Development Plan
- **Superseded by:** Master Development Plan v1.0
- **Date archived:** December 30, 2025

These remain available in `/Archive/` for historical reference but should NOT be followed.

---

## GETTING STARTED

### For New Team Members (Future)

1. Read **CLAUDE.md** (understand AI rules and conventions)
2. Read **Master Development Plan v1.0** (understand the roadmap)
3. Read **Development Best Practices** (understand the process)
4. Read Master Volumes I-IV (understand the vision)
5. Review current stage specifications (start with Ball Physics #1)
6. Review **docs/tracking/spec-error-log.md** (understand cross-spec issues)
7. Set up development environment (guide TBD)

### For Current Development (Solo)

1. ✅ Master Plan understood
2. ✅ Best Practices documented
3. ✅ Specification progress tracker created
4. ✅ All 20 specifications APPROVED (May 18, 2026) — spec phase COMPLETE
5. ✅ `src/CLAUDE.md` coding guide created (May 19, 2026; v1.8)
6. ✅ Ball Physics #1 — initial implementation at `src/Core/Physics/Ball/`
7. ✅ Agent Movement #2 — implementation + tests at `src/agent-movement/`
8. ✅ Collision System (#3), First Touch (#4), Pass Mechanics (#5), Shot Mechanics (#6) — all coded, AR-clean
9. ✅ Heading Mechanics (#10) — 25 source files, coded and AR-clean (May 28)
10. ✅ Goalkeeper Mechanics (#11) — 36 source files, coded and AR-clean (May 28)
11. ✅ Perception System (#7) — 14 source files, coded and AR-clean (May 29)
12. ✅ Decision Tree (#8) — 36 files, coded and AR-clean (May 29)
13. ✅ Positioning AI (#12) — 20 files, coded and AR-clean (May 29)
14. ✅ Pressing AI (#13) — 21 files, coded and AR-clean (May 29)
15. ✅ Defensive AI (#14) — 19 files, coded and AR-clean (May 29)
16. ✅ Attacking AI (#15) — 24 files, coded and AR-clean (May 29)
17. ✅ Deterministic Simulation (#16) — 23 files (21 .cs + 2 asmdef), coded and AR-clean (May 29)
18. ✅ Structural deviation resolved: Ball Physics relocated to `src/ball-physics/` (May 30, 2026)
19. ✅ Event System (#17), Performance Optimization (#18), Testing Strategy (#19) — coded, AR-clean
20. ✅ Non-certifying Linux `dotnet-ci` gate — full `src/` tree compile + 1,165+ NUnit tests on every push (Jun 12–13, 2026)
21. ✅ Match Engine composition root — Phases A–F complete, wiring all subsystems into the deterministic-sim 7-phase tick pipeline (Jun 28, 2026)
22. ✅ Tactical Instructions (#21) APPROVED + fully landed incl. per-agent tactics, balance pass, disk loaders (Jun 20–30, 2026)
23. ✅ Living World System (#22) APPROVED + season/world loop, ArcEngine, text generation landed (Jun 21 – Jul 3, 2026)
24. ✅ Specs #23–#26 (Dismarking, Build-Up Structures, Positional Rotations, Tactical Presets) APPROVED + wired (Jul 8–11, 2026)
25. ✅ Match Engine — goal detection, halves/full-time model, and the complete match-flow model (throw-ins, corners, goal kicks, fouls/cards, offside, substitutions) landed (Jul 11–14, 2026)
26. ⏳ Unity 6 recertification — engine target bumped to 6000.4.9f1/DX11 (Jul 13, 2026, docs-only); real cert run on the new tuple outstanding (needs pinned host access)
27. ⏳ FR-PO-052 certified perf baseline — real Stopwatch harness landed (Jul 13); first cert run on the pinned tuple still outstanding, blocked on item 26

---

## CONTACT & COMMUNITY

**Developer:** Solo developer with AI assistance
**Project Start:** December 29, 2025
**Current Phase:** Implementation (Stage 0+1)

**Community Channels (To be established):**
- Discord: TBD (Stage 1)
- Dev Blog: TBD (Stage 0 Month 6)
- Reddit: r/TacticalDirector TBD (Stage 1)
- Twitter: TBD (Stage 1)

---

## VERSION CONTROL DISCIPLINE

**Critical Habit:** Commit after each specification completion.

```bash
# After completing each specification:
git add docs/specs/[spec-folder]/
git add docs/tracking/PROGRESS.md
git commit -m "Complete [SpecName] Specification v1.0"
git push

# After approval:
git commit -m "Approve [SpecName] Specification - Ready for implementation"
git tag "spec-[specname]-v1.0-approved"
git push --tags
```

**Tag Status:**
- `spec-ball-physics-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-agent-movement-v1.0-approved` — created locally Apr 27, 2026; awaits push
- `spec-collision-system-v1.0-approved` — created locally Apr 26, 2026 (back-fill of Feb 19 sign-off); awaits push
- `spec-first-touch-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-pass-mechanics-v1.0-approved` — created locally May 6, 2026 (re-approved); awaits push
- `spec-event-system-v1.0-approved` — created locally May 13, 2026; awaits push
- `spec-shot-mechanics-v1.0-approved` — created locally Apr 27, 2026; awaits push
- `spec-perception-system-v1.0-approved` — created locally Apr 26, 2026; awaits push
- `spec-decision-tree-v1.0-approved` — created locally Apr 27, 2026 (draft-level approval); awaits push
- `spec-fixed64-math-v1.0-approved` — created locally May 15, 2026; awaits push
- `spec-testing-strategy-v1.0-approved` — created locally May 15, 2026; awaits push
- `spec-performance-optimization-v1.0-approved` — created locally May 15, 2026; awaits push

> **Tag push blocked**: `git push origin --tags` returns HTTP 403 from the current remote endpoint. After this branch merges to main with privileged credentials, run `git push origin spec-ball-physics-v1.0-approved spec-collision-system-v1.0-approved spec-first-touch-v1.0-approved spec-perception-system-v1.0-approved` from a context that allows tag creation. The tags are annotated and point at the merge commit (or its branch-tip predecessor if fast-forward).

**Why this matters:**
- Preserves specification history
- Enables rollback if implementation reveals design flaws
- Documents approval timeline
- Supports future audit trail

**Frequency:**
- After each specification section completed
- After each review cycle
- After final approval
- Weekly (minimum) during specification phase

---

## VERSION HISTORY

**v1.0 — December 30, 2025**
- Initial README creation
- Documentation hierarchy established
- Stage 0 specification schedule defined
- Archived obsolete planning documents

**v1.1 — February 3, 2026**
- Updated specification progress tracking
- Added Ball Physics Specification status (in progress)
- Added specification approval checklist reference
- Added version control discipline section
- Updated archive status (CRITICAL_ANALYSIS moved)
- Added progress tracker reference

**v1.2 — February 15, 2026**
- Updated to Week 2 status
- Ball Physics marked as APPROVED (Feb 8, 2026)
- Agent Movement marked as IN REVIEW (~130 pages, 83 tests)
- Added file-manifest.md reference
- Updated project structure to reflect current files
- Changed quality gate checkboxes to unchecked (pending implementation)
- Added pending git tags section
- Updated next steps for Week 2-3

**v1.3 — March 24, 2026**
- Updated to Week 8 status (was 6 weeks stale)
- Corrected spec numbering to canonical PROGRESS.md order
- 4 approved, 3 in review, 1 in progress (was: 1 approved, 1 in review)
- Added Decision Tree #8 as IN PROGRESS
- Added comprehensive audit process to status notes
- Added Key Architectural Decisions section
- Added spec-error-log.md reference to documentation hierarchy and project structure
- Updated pending git tags
- Updated Getting Started checklist
- Updated next steps for Week 8

**v1.4 — April 21, 2026**
- Updated to Week 12 status
- Pass Mechanics (#5): APPROVED → SUSPENDED (audit March 25, 2026)
- Decision Tree (#8): IN PROGRESS → IN REVIEW (Sections 1–9 + appendices all drafted)
- Progress summary corrected: 3 approved, 1 suspended, 4 in review
- Project structure updated to reflect current repo layout (docs/ paths)
- Tracking document links corrected to docs/tracking/ paths
- Added ⏸ SUSPENDED to status legend
- Added Decision Tree to pending git tags
- Updated Getting Started checklist
- Updated next immediate steps to Week 12 actions

**v1.5 — April 26, 2026**
- Removed scheduling milestones (passion project, no deadline)
- Perception System (#7) status: IN REVIEW → APPROVED (Apr 22, 2026; §9 v1.7 signed off)
- Approval count corrected to 4 per §9-file evidence (with Collision System #3 status disagreement flagged)
- Next Immediate Steps rewritten around outstanding sign-offs and documentation debt rather than week-12 actions
- Added explicit reference to ~74 stale spec-number references in body text (measured)
- Added pointer to spec-error-log.md rebuild and CLAUDE.md dangling references

**v1.6 — April 27, 2026**
- Lead developer sign-off pass: Agent Movement (#2), Shot Mechanics (#6), Decision Tree (#8) all APPROVED.
- Stage 0 spec count: 7 APPROVED, 1 SUSPENDED, 0 IN REVIEW, 12 NOT STARTED.
- Decision Tree (#8) approved at draft-level quality gate; comprehensive audit candidate before implementation.
- Renumber sweep marked complete (Apr 26).
- Fixed64 migration deferred to Stage 5+; Stage 0 uses float + state snapshots.
- Pass Mechanics (#5) is the only Priority 1–2 spec still outstanding (re-sign-off after March 25 audit).

**v1.8 — May 12, 2026**
- Code Standards & Style Guide (#20) reclassified NOT STARTED → APPROVED (May 11, 2026).
- Testing Strategy & Framework (#19) reclassified NOT STARTED → IN REVIEW (May 12, 2026). Approval gated by KD-2 preconditions (#16 Tier 2 APPROVED, #18 outline-level draft, all `TBD-NORMATIVE` tags resolved).
- Stage 0 spec count: **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**
- Project Structure block updated to break out testing-strategy/ and code-standards/ folders.

**v1.7 — May 6, 2026**
- Deterministic Simulation (#16) reclassified to IN PROGRESS (May 2, 2026).
- Fixed64 Math Library (#9) reclassified to IN REVIEW (May 6, 2026) after Pass 2 adversarial critique fixes landed.
- Stage 0 spec count: 7 APPROVED, 1 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.
- Project Structure block updated to reflect approved sign-offs and current statuses; #9 and #16 broken out from the "..." catch-all.
- Next Immediate Steps refreshed to May 6 reality; resolved documentation-debt items checked off (spec-error-log rebuild, file-manifest reconciliation).
- ERR-016-002 (EntityId no-reuse cross-spec back-propagation) added to outstanding documentation debt.

**v2.7 — May 29, 2026**
- Positioning AI (#12): 20 files (19 .cs + 1 asmdef) coded at `src/positioning-ai/`; AR-1+AR-2+AR-3 clean.
- `src/CLAUDE.md` coding guide updated to v1.17 (positioning-ai/ tree expanded).
- `docs/tracking/PROGRESS.md`, `README.md`, `CLAUDE.md` updated with #12 implementation status.

**v2.6 — May 29, 2026**
- Perception System (#7): 14 source files coded and AR-clean (AR-1: 3M+3L; AR-2: 3L) at `src/perception-system/`.
- Decision Tree (#8): 36 files (35 .cs + 1 asmdef) coded at `src/decision-tree/`; AR-1 (2H+3M+4L) + AR-2 (full sweep clean).
- `src/CLAUDE.md` coding guide updated to v1.16 (decision-tree/ tree expanded).
- `docs/tracking/PROGRESS.md` and `README.md` updated with #7 and #8 implementation status.

**v2.5 — May 28, 2026**
- Heading Mechanics (#10): 25 source files coded and AR-clean at `src/heading-mechanics/`.
- Goalkeeper Mechanics (#11): 36 source files coded at `src/goalkeeper-mechanics/`; AR-1 (5H+1M) + AR-2 (2M) adversarial review cycles complete.
- `src/CLAUDE.md` coding guide updated to v1.14 (goalkeeper-mechanics/ tree).
- `docs/tracking/file-manifest.md` updated with Heading Mechanics and Goalkeeper Mechanics source file inventories.
- `docs/tracking/PROGRESS.md` updated with implementation status for #10 and #11.

**v2.4 — May 25, 2026**
- All 20 specifications APPROVED. Stage 0 specification phase COMPLETE (May 18, 2026).
- Specs #10–#15 APPROVED May 16–18, 2026 (Heading Mechanics, Goalkeeper Mechanics, Positioning AI, Pressing AI, Defensive AI, Attacking AI).
- Coding phase begun May 19, 2026. `src/CLAUDE.md` coding guide created; at v1.8 as of May 25.
- Ball Physics (#1): initial implementation at `src/Core/Physics/Ball/` — 8 source files + 3 test files.
- Agent Movement (#2): initial implementation at `src/agent-movement/` — 16 source files.
- Known structural deviation: Ball Physics at `src/Core/Physics/Ball/` vs spec-canonical `src/ball-physics/` — fix pass planned.
- README updated to reflect implementation phase, full spec schedule, and actual `src/` structure.

**v2.3 — May 15, 2026 (later same day still)**
- Performance Optimization Strategy (#18) — APPROVED. §9 v1.0 lead-developer sign-off granted; v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices (60 instances) against #16 APPROVED text + #19 v1.0.1 patch-revised text. KD-2 sequencing satisfied.
- Stage 0 spec count: **14 APPROVED, 0 SUSPENDED, 0 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 5 spec set fully complete; only Priority 3+4 AI specs (#10–#15) remain in the spec phase.

**v2.2 — May 15, 2026 (later same day)**
- Testing Strategy & Framework (#19) — APPROVED. §9 v1.0 lead-developer sign-off granted; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices (51 citation-qualifier instances resolved against #16's APPROVED text and #18's IN REVIEW v0.3 surface). KD-2 sequencing satisfied.
- Stage 0 spec count: **13 APPROVED, 0 SUSPENDED, 1 IN REVIEW (#18), 0 IN PROGRESS, 6 NOT STARTED.** Priority 5 spec set now 75% complete.
- Downstream effect: clears Spec #18's last KD-2 gate for its own `IN REVIEW → APPROVED` advancement.

**v2.1 — May 15, 2026**
- Fixed64 Math Library (#9) — APPROVED. §9 v1.0 lead-developer sign-off granted; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral (Interface Design Principle). Stage 0 effect: none — Fixed64 binds to Stage 5+ per §8.1 v0.2.
- Event System (#17) §1.0.1 patch revision — `[CROSS-PENDING]` → `[CROSS]` promotion of `DOMAIN_TAG_EVENT_LEDGER` across all consuming sections + Appendix B literal-value inlining (`0x15`). ERR-017-001 FULLY RESOLVED.
- Stage 0 spec count: **12 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.** Priority 1–2 spec set fully complete.

**v2.0 — May 14, 2026 (later same day)**
- Deterministic Simulation (#16) — Tier 2 APPROVED. §9 v1.7 sign-off; all §9.4.2 gates cleared (golden-vector files complete; §8.3.1 cross-spec re-audit complete; §9.3 sign-offs granted with Stage-0 host-platform-pin caveat); ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in #16 §3.4 v1.0.1.
- Performance Optimization Strategy (#18) — IN PROGRESS → IN REVIEW. Section files v0.1 → v0.2 → v0.3 (PASS-1 + PASS-2 adversarial reviews; ERR-018-002..018 closed; FR count 82). Advancement to APPROVED gated on #19 reaching APPROVED per KD-2.
- Stage 0 spec count: **11 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.**
- Project Structure block: #16 broken out as APPROVED; #18 broken out as IN REVIEW; catch-all reduced to #10–#15.
- Next Immediate Steps fully rewritten around May 14 reality.

**v1.9 — May 13, 2026**
- Event System (#17) — APPROVED May 13, 2026. Section files authored from `outline-detailed.md` v1.1; section-files PASS 1 (3 H / 5 M / 12 L) and PASS 2 (2 H / 6 M / 7 L) adversarial critiques both resolved; all section files at v0.3; lead-developer sign-off granted.
- Pass Mechanics (#5) status corrected from stale SUSPENDED to APPROVED (re-approved May 6, 2026).
- Stage 0 spec count: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 7 NOT STARTED.**
- Priority 5 schedule row 17 updated from ⏳ NOT STARTED to ✅ APPROVED.
- Project Structure block: #17 broken out as APPROVED; catch-all updated to Specs #10–#15, #18.
- Tag list: `spec-pass-mechanics-v1.0-approved` corrected; `spec-event-system-v1.0-approved` added.

**v3.0 — June 29, 2026 (consolidates a month of unrecorded changes, May 30 – June 29)**
- Structural fix landed: Ball Physics relocated `src/Core/Physics/Ball/` → `src/ball-physics/` (May 30, 2026).
- AR-hardening sweep completed across all 18 then-coded sections (June 7, 2026); Stage-0 host-platform pin landed in `certification-platform.md` (June 7, 2026).
- Non-certifying Linux `dotnet-ci` gate landed (June 12–13, 2026): full `src/` tree compile + 1,165+ NUnit tests on every push; first-ever full compile surfaced 8 previously-dead build surfaces (notably ERR-017-002, the illegal-overload EventBus production assembly); 30-test quarantine fully adjudicated and cleared.
- Comprehensive Decision Tree (#8) audit (AR-2, June 11, 2026): 3H+11M+9L resolved — production assembly had never compiled; away-team zone/press/line-depth asymmetry bugs fixed.
- Pass Mechanics (#5) AR-9/AR-10 fix pass (June 11, 2026): test suite had never compiled since v1.1; 3 functional defects fixed.
- **Match Engine** composition root (`src/match-engine/`, unnumbered) built end-to-end: Phases A–F complete (June 19–28, 2026) — boot, full snapshot serialization (`SNAPSHOT_SCHEMA_VERSION` 8), Physics/Resolve/AI-phase wiring through all five tactical AI specs, Events-phase possession-changed pipeline, and the Phase F capstone closed-loop scenario.
- **Tactical Instructions (#21)** — second Stage-1 forward spec — APPROVED June 20, 2026; T0 data layer, T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking), and runtime activation (`MatchEngine.SetTeamTactic` + in-code `TeamTacticConfig`) landed through June 29, 2026.
- **Living World System (#22)** — third Stage-1 forward spec — APPROVED June 22, 2026; T0 scaffolding landed at `src/living-world/`.
- Spec count: **22 APPROVED** (20 Stage-0 + 2 Stage-1 forward specs), 0 outstanding in any other status.
- Project Structure, Locked list, Next Immediate Steps, and Getting Started checklist all corrected to match current `src/` and `docs/specs/` contents (this README had not been substantively updated since v2.7 / May 29, 2026 despite header dates being hand-advanced).

**v4.0 — July 14, 2026 (consolidates two weeks of unrecorded changes, June 30 – July 14)**
- **Specs #23–#26 promoted from design supplements to full 11-file spec sets, adversarially reviewed, and APPROVED** (July 8–10, 2026): Dismarking & Marker-Awareness AI (#23), Scripted Build-Up Structures (#24), Positional Rotations (#25), Tactical Presets & AI-Manager Selection (#26). Spec count: **26 APPROVED**, 0 outstanding in any other status.
- **#23/#24/#25/#26 implementation wiring landed** (July 10–11, 2026): T0 data-layer scaffolding, then the #24 build-up + #23 dismark `SlotComposer` stages, the #25 `RotationController`, the #23 Decision Tree marked-pass-target penalty, and #26's full T1–T4 stack (preset→config projection, kickoff/interval decision gate, kickoff scoring, mid-match adaptation ladder). All default-behaviour-neutral at each dial's identity value.
- **Match Engine — engine substrate landed** (July 11, 2026): goal detection (scoring, `GoalAwardedEvent`, centre-spot restart) and the match-length/halves model (`MATCH_TICKS_TOTAL`, `HALF_TIME_BOUNDARY_TICK`). `SNAPSHOT_SCHEMA_VERSION` 13 → 14. This closed #26's engine-substrate gates, activating its half-time trigger and live goalDiff/clock ladder inputs.
- **Unity engine version bumped 2022.3.62f1 → Unity 6000.4.9f1, DX11** (July 13, 2026) — documentation-only; no recertification performed. `certification-platform.md` Status reverted `✅ PINNED` → `⏳ RECERT REQUIRED`, re-blocking `FR-DS-009-GATE`, `FR-PO-052`, the §7.5 D1 test-runner pin, and `EnvironmentFingerprint` until a real cert run executes against the new tuple. A real Stopwatch-based perf harness (`StopwatchPerfHarness` / `MatchEngineCapstonePerfHarness`) landed the same day, superseding the old synthetic stub — still non-certifying pending pinned-host access.
- **Tactical Instructions (#21) Stage-1 leftovers landed** (June 30, 2026): per-agent `PlayerTactic` config surface, the §5.6/G2 `[GT]` balance pass, the §3.4 defensive-line depth recompute, and on-disk team/player tactic-file loaders. #21 has no remaining Stage-1 follow-ups.
- **Living World System (#22) season/world loop landed** (July 2–3, 2026): `WorldClock`/`WorldLoop`/`MemoryStore`/`ColdStore`, `ArcEngine` + `ActiveSetMembership`, `InteractionTextGenerator` with deep-memory auto-cite, and the `WorldStore` composition root (the KD-10 season-store prerequisite). BackgroundTierSim summaries and arc trigger evaluators remain documented seams, deliberately unbuilt pending upstream canon.
- **Presentation layer: minimal match viewer landed** (July 2, 2026): `src/match-viewer/` records a real `MatchEngine` run and exports a self-contained HTML canvas replay. Observer-neutral and digest-locked; not a determinism-pinned wire format.
- **Match-flow model completion landed** (July 14, 2026): throw-ins, corners, goal kicks, fouls/cards (with sent-off tracking), offside, substitutions, half-time break, and full-time end — the last major gap in the match engine (previously only kickoff + goal-restart existed). New `RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; three new Tier A events. `SNAPSHOT_SCHEMA_VERSION` 14 → 15. Design doc and code both adversarially reviewed to convergence.
- Header "Last Updated" chain, CURRENT STATUS, Specification Writing Schedule, Locked list, Project Structure, and Next Immediate Steps all reconciled to match current `src/` and `docs/specs/` contents (this README had drifted since v3.0 / June 29, 2026 despite the header being updated piecemeal through July 10).

---

## LICENSE

**TBD** — To be determined before Stage 2 commercial release

---

**Remember:** This is a 10-year project. Quality over speed. Build it right.

**Current Focus:** All 26 approved specs have active `src/` implementations. Active frontier: Unity 6 recertification (pinned-host access required) and the #26 `[GT]` balance-pass review — both non-blocking follow-ups, not new feature work.
