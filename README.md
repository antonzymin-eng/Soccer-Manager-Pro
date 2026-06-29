# Tactical Director: Football Management Simulation

**Created:** December 30, 2025, 11:50 AM PST
**Last Updated:** June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Project Type:** Full-scale football management simulation
**Development Timeline:** Open-ended passion project, staged releases
**Target:** The Football Manager Killer
**Current Stage:** Stage 0+1 — Implementation (coding begun May 19, 2026)

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
**Spec Phase Completed:** May 18, 2026 — all 20 specs APPROVED
**Deliverables:** 20 comprehensive specification documents + initial code for Ball Physics and Agent Movement

**Summary (June 29, 2026):** All 20 Stage-0 specs APPROVED, plus 2 Stage-1 forward specs (#21 Tactical Instructions, #22 Living World System) APPROVED. Every spec #1–#20 has an active `src/` implementation. A non-certifying Linux dotnet compile/test CI gate (`tools/dotnet-ci/`) now compiles the entire `src/` tree and runs all 1,165+ NUnit tests on every push; quarantine list is empty. **Match Engine** (`src/match-engine/`, design note at `docs/tracking/match-engine-design.md`) — the composition root wiring every subsystem into the deterministic-sim 7-phase tick pipeline — has Phases A–F complete (boot, full canonical world-state snapshot serialization at schema v8, Physics/Resolve/AI phase wiring through Positioning→Pressing→Defensive→Attacking→DecisionTree, an Events-phase possession-changed producer/consumer, and the Phase F capstone closed-loop scenario on the #19 `ScenarioRunner`). **Match Engine integration is complete.** **Tactical Instructions (#21)** now drives the live AI: T0 data layer + T2 consumer seams across DecisionTree/Pressing/Positioning/Defensive/Attacking (behaviour-neutral at identity) + runtime activation via `MatchEngine.SetTeamTactic` and an in-code `TeamTacticConfig` source + boot applier (on-disk tactic-file format deferred to Stage 0+1 per the #19 D1 precedent). `src/CLAUDE.md` coding guide (v1.76) governs all C# authoring.
**Total specification output:** ~4.33 MB+ across 100+ files (see file-manifest.md)
**Implementation active:** #1–#20 implemented; #21 Tactical Instructions T0 + T2 (DecisionTree/Pressing/Positioning/Defensive/Attacking consumer seams, behaviour-neutral) + runtime activation (per-team `SetTeamTactic` + in-code `TeamTacticConfig` source + boot applier) landed; #22 scaffolded (T0); match-engine integration Phases A–F complete

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

**Status Key:**
- ⏳ Not started
- 📝 In progress
- 🔍 In review
- ⏸ Suspended
- ✅ Approved
- 🔒 Locked (implementation begun)

**Locked (implementation begun):** Ball Physics #1, Agent Movement #2, Collision System #3, First Touch #4, Pass Mechanics #5, Shot Mechanics #6, Perception System #7, Decision Tree #8, Heading Mechanics #10, Goalkeeper Mechanics #11, Positioning AI #12, Pressing AI #13, Defensive AI #14, Attacking AI #15, Deterministic Simulation #16

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
│   │   └── attacking-ai/              [Spec #15 — APPROVED May 18]
│   └── tracking/
│       ├── PROGRESS.md                 [Schedule and milestone tracking]
│       ├── spec-error-log.md           [Cross-spec architectural errors]
│       ├── fix-manifest-pass-mechanics.md [Per-audit fix tracking — Pass Mechanics #5]
│       └── file-manifest.md            [Authoritative file inventory]
└── src/                                [Implementation — coding begun May 19, 2026]
    ├── CLAUDE.md                       [Coding guide v1.8 — read before writing code]
    ├── Core/Physics/Ball/              [Spec #1 — known deviation from src/ball-physics/]
    │   ├── BallPhysicsConstants.cs
    │   ├── BallState.cs
    │   ├── BallPhysicsCore.cs
    │   ├── BallStateMachine.cs
    │   ├── BallGroundInteraction.cs
    │   ├── BallCollision.cs
    │   ├── BallEventLogger.cs
    │   ├── SurfaceProperties.cs
    │   └── Tests/  [BallPhysicsCoreTests, BallIntegrationTests, BallStateMachineTests]
    └── agent-movement/                 [Spec #2 — 16 source files]
```

---

## NEXT IMMEDIATE STEPS

### Outstanding (as of May 29, 2026)

**Specification phase:** ✅ COMPLETE — all 20 specs APPROVED (May 18, 2026).

**Implementation phase — active frontier:**
1. 🔒 Ball Physics (#1) — at `src/Core/Physics/Ball/`. Structural deviation from `src/ball-physics/` fix pass pending.
2. 🔒 Agent Movement (#2) — at `src/agent-movement/`. Tests landed May 27 (AR-2/AR-3).
3. 🔒 Collision System (#3), First Touch (#4), Pass Mechanics (#5), Shot Mechanics (#6) — all coded and AR-clean (May 25–28).
4. 🔒 Heading Mechanics (#10) — 25 source files coded and AR-clean (May 28).
5. 🔒 Goalkeeper Mechanics (#11) — 36 source files coded and AR-clean (May 28).
6. 🔒 Perception System (#7) — 14 source files coded and AR-clean (May 29).
7. 🔒 Decision Tree (#8) — 36 files coded and AR-clean (May 29). AR-1: 2H+3M+4L fixed; AR-2: clean.
8. 🔒 Positioning AI (#12) — 20 files coded and AR-clean (May 29). AR-1+AR-2+AR-3 complete.
9. 🔒 Pressing AI (#13) — 21 files coded and AR-clean (May 29). AR-1: 3H+1M+1L fixed; AR-2: clean.
10. 🔒 Defensive AI (#14) — 19 files (18 .cs + 1 asmdef) coded and AR-clean (May 29). AR-1: 2H+1M fixed; AR-2: clean.
11. 🔒 Attacking AI (#15) — 24 files (23 .cs + 1 asmdef) coded and AR-clean (May 29). AR-1: 2H+4M fixed; AR-2: 2L fixed; AR-3: clean.
12. 🔒 Deterministic Simulation (#16) — 23 files (21 .cs + 2 asmdef) coded and AR-clean (May 29). AR-1: 4H+4M fixed; AR-2: 1L fixed; AR-3: 1L fixed; AR-3 clean.
13. ⏳ Structural fix: move Ball Physics from `src/Core/Physics/Ball/` → `src/ball-physics/` (spec-canonical path).

**Operations / housekeeping:**
5. ⏳ Push 12+ approval tags (HTTP 403 branch-protection); run squash-merge precondition check from CLAUDE.md OPEN ISSUES before `git push --tags`.
6. ⏳ Stage-0 host-platform pin in `docs/tracking/certification-platform.md` (lead-developer task; blocks first `FR-DS-009-GATE` cert run and `FR-PO-052` Stage 0+1 perf-gate activation).
7. ⏳ Fix smart-quote regression in `first-touch/section-7.md` L19–20.

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
18. ⏳ Resolve structural deviation: move Ball Physics to `src/ball-physics/`

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

---

## LICENSE

**TBD** — To be determined before Stage 2 commercial release

---

**Remember:** This is a 10-year project. Quality over speed. Build it right.

**Current Focus:** Implement remaining AI specs per `src/CLAUDE.md`. Active frontier: Positioning AI (#12) complete (May 29); next up: Pressing AI (#13) or Defensive AI (#14).
