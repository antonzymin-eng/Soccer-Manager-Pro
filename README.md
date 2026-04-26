# Tactical Director: Football Management Simulation

**Created:** December 30, 2025, 11:50 AM PST
**Last Updated:** April 26, 2026
**Project Type:** Full-scale football management simulation
**Development Timeline:** Open-ended passion project, staged releases
**Target:** The Football Manager Killer
**Current Stage:** Stage 0 (Specification Phase)

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

**[Master Development Plan v1.0](Master_Development_Plan_v1_0.md)**
The definitive 10-year roadmap. All development decisions reference this document.

**What it contains:**
- 6 development stages (Stage 0 through Stage 5)
- Quality gates and success criteria
- Technical architecture foundation
- Resource requirements and timelines
- Risk mitigation strategies

**Current stage:** Stage 0 — Physics Foundation (Year 1)
**Current phase:** Specification Writing (Week 12 of 20)

---

### DESIGN REFERENCE LIBRARY

These four volumes describe the FULL vision. Features are implemented incrementally across stages.

**[Master Volume I: Physics & Simulation Core](Master_Vol_1_Physics_Core.md)**
The 10Hz heartbeat, match logic, physical environment, and tactical micro-physics.

**Implementation timeline:**
- Stage 0-1: Core physics (ball, movement, basic AI)
- Stage 2: Match simulation with statistics
- Stage 5: Full environmental physics and 85,000 entity simulation

---

**[Master Volume II: Human Systems & Behavioral Sociology](Master_Vol_2_Human_Systems.md)**
Psychology, social graphs, communication, atmosphere, and personality evolution.

**Implementation timeline:**
- Stage 2: Basic morale and form
- Stage 4: Full H-Gate system, social graphs, media interactions
- Ongoing: Personality evolution and social dynamics

---

**[Master Volume III: Club Operations & Strategic Management](Master_Vol_3_Club_Operations.md)**
Tactical systems, recruitment, finance, governance, youth development, and training.

**Implementation timeline:**
- Stage 1: Tactical system (formations, instructions)
- Stage 2: Basic squad management and transfers
- Stage 3: Full financial systems, staff, infrastructure
- Stage 4+: Deep recruitment AI, board dynamics

---

**[Master Volume IV: Technical Implementation & Systems Engineering](Master_Vol_4_Tech_Implementation.md)**
Architecture, optimization, data ontology, tooling, modding support, and UI/UX.

**Implementation timeline:**
- Stage 0: Core architecture (Fixed64, DoD, event system)
- Stage 1: 2D rendering, UI framework
- Stage 2: Save system, database architecture
- Stage 3+: Advanced tooling, modding API, multiplayer infrastructure

---

### SUPPORTING DOCUMENTATION

**[Development Best Practices](Development_Best_Practices.md)**
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

### Stage 0: Physics Foundation (Year 1)

**Goal:** Build the physics and simulation core that all future systems depend on.

**Progress:** Specification Phase
**Started:** February 2, 2026
**Deliverables:** 20 comprehensive specification documents

**Summary (per §9 files, April 26, 2026):** 4 approved (#1, #4, #7, plus #3 per registry — see PROGRESS.md status note), 1 suspended (#5), 3 in review (#2, #6, #8), 12 not started.
**Total specification output:** ~4.33 MB across 106 files (counts predate folder migration; see file-manifest.md)
**Comprehensive audits completed:** 6 of 7 finished specs

---

### Specification Writing Schedule

**Priority 1 — Physics Foundation (Weeks 1-4):**
1. ✅ Ball Physics — **APPROVED** Feb 8, 2026 (~50 pages, 44 tests, audited)
2. 🔍 Agent Movement — **IN REVIEW** (~130 pages, 83 tests, audited)
3. ✅ Collision System — **APPROVED** Feb 19, 2026 (~50 pages, ~70 tests, audited)
4. ✅ First Touch Mechanics — **APPROVED** Feb 22, 2026 (~70 pages, 72 tests, audited)
5. ⏸ Pass Mechanics — **SUSPENDED** (audit March 25, 2026 — 19 findings fixed; awaiting re-sign-off)

**Priority 2 — Core Gameplay:**
6. 🔍 Shot Mechanics — **IN REVIEW** (~70 pages, 104 tests, audited; ERR-010 must be fixed before sign-off)
7. ✅ Perception System — **APPROVED** Apr 22, 2026 (~80 pages, 92 tests; §9 v1.7 signed off)
8. 🔍 Decision Tree — **IN REVIEW** (Sections 1–9 + appendices drafted; ~55+ pages; awaiting sign-off)
9. ⏳ Fixed64 Math Library — Not started

**Priority 3 — Advanced Physics (Weeks 9-12):**
10. ⏳ Heading Mechanics
11. ⏳ Goalkeeper Mechanics
12. ⏳ Positioning AI

**Priority 4 — Tactical AI (Weeks 13-16):**
13. ⏳ Pressing AI
14. ⏳ Defensive AI
15. ⏳ Attacking AI
16. ⏳ Deterministic Simulation

**Priority 5 — Systems Architecture (Weeks 17-20):**
17. ⏳ Event System
18. ⏳ Performance Optimization Strategy
19. ⏳ Testing Strategy & Framework
20. ⏳ Code Standards & Style Guide

**Status Key:**
- ⏳ Not started
- 📝 In progress
- 🔍 In review
- ⏸ Suspended
- ✅ Approved
- 🔒 Locked (implementation begun)

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
TacticalDirector/
├── CLAUDE.md                           [AI behavioral rules — read first]
├── docs/
│   ├── planning/                       [Master volumes, dev plan, best practices]
│   ├── specs/
│   │   ├── SPEC_INDEX.md               [Canonical spec numbering and status]
│   │   ├── ball-physics/               [Spec #1 — APPROVED]
│   │   ├── agent-movement/             [Spec #2 — IN REVIEW]
│   │   ├── collision-system/           [Spec #3 — APPROVED]
│   │   ├── first-touch/                [Spec #4 — APPROVED]
│   │   ├── pass-mechanics/             [Spec #5 — SUSPENDED]
│   │   ├── shot-mechanics/             [Spec #6 — IN REVIEW]
│   │   ├── perception-system/          [Spec #7 — IN REVIEW]
│   │   ├── decision-tree/              [Spec #8 — IN REVIEW]
│   │   └── ...                         [Specs #9–#20 not started]
│   └── tracking/
│       ├── PROGRESS.md                 [Schedule and milestone tracking]
│       ├── spec-error-log.md           [Cross-spec architectural errors]
│       ├── fix-manifest-pass-mechanics.md [Per-audit fix tracking — Pass Mechanics #5]
│       └── file-manifest.md            [Authoritative file inventory]
└── src/                                [Empty until all 20 specs approved]
```

---

## NEXT IMMEDIATE STEPS

### Outstanding (as of April 26, 2026)

**Sign-offs pending:**
1. ⏳ Lead developer sign-off: Agent Movement (#2)
2. ⏳ Lead developer sign-off: Shot Mechanics (#6)
3. ⏳ Lead developer sign-off: Decision Tree (#8)
4. ⏳ Lead developer re-sign-off: Pass Mechanics (#5)
5. ⏳ Adjudicate Collision System (#3) status disagreement (§9 says PENDING; trackers say APPROVED)

**Documentation debt:**
6. ⏳ Rebuild `docs/tracking/spec-error-log.md` (currently stub-only; ERR-001 through ERR-008 missing)
7. ⏳ Fix ~74 stale spec-number references in body text across approved/in-review specs
8. ⏳ Reconcile `file-manifest.md` to post-migration folder names (currently flagged as pre-migration legacy)
9. ⏳ Resolve dangling references in CLAUDE.md (`.claudeignore`, `MIGRATION_GUIDE.md`)

**After Priority 2 sign-offs:**
10. ⏳ Decide: tag approved specs now (with renumbering debt) vs. fix-then-tag
11. ⏳ Begin Fixed64 Math Library Specification (#9) — also resolve scope question (Stage 0 vs. Stage 5+)
12. ⏳ Begin Heading Mechanics Specification (#10)

---

## ARCHIVED DOCUMENTS

The following documents have been superseded and moved to `/Archive/`:

**1. Focused_Tactical_Sim_Plan.md**
- **Reason:** Proposed 18-month simplified scope, contradicts Master Plan's 10-year full simulation vision
- **Superseded by:** Master Development Plan v1.0
- **Date archived:** December 30, 2025

**2. CRITICAL_ANALYSIS_Development_Risks.md**
- **Reason:** Valid warnings extracted into Development_Best_Practices.md
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
4. ✅ Ball Physics Specification approved (Feb 8)
5. ✅ Collision System Specification recorded as approved (Feb 19) — §9 file disagreement flagged
6. ✅ First Touch Mechanics Specification approved (Feb 22)
7. ⏸ Pass Mechanics Specification suspended (audit March 25 — re-sign-off pending)
8. ✅ Perception System Specification approved (Apr 22)
9. 🔍 Agent Movement, Shot Mechanics, Decision Tree awaiting sign-off
10. ⏳ Begin Fixed64 Math Library and Heading Mechanics after Priority 2 sign-offs
11. No coding until all 20 specs approved

---

## CONTACT & COMMUNITY

**Developer:** Solo developer with AI assistance
**Project Start:** December 29, 2025
**Current Phase:** Specification Writing

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

**Pending Tags:**
- `spec-ball-physics-v1.0-approved` — awaiting commit (approved Feb 8)
- `spec-agent-movement-v1.0-approved` — awaiting sign-off
- `spec-collision-system-v1.0-approved` — status disagreement; resolve before tagging
- `spec-first-touch-v1.0-approved` — awaiting commit (approved Feb 22)
- `spec-pass-mechanics-v1.0-approved` — awaiting re-sign-off (suspended)
- `spec-shot-mechanics-v1.0-approved` — awaiting sign-off
- `spec-perception-system-v1.0-approved` — awaiting commit (approved Apr 22)
- `spec-decision-tree-v1.0-approved` — awaiting sign-off

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
- Added FILE_MANIFEST.md reference
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
- Added Spec_Error_Log reference to documentation hierarchy and project structure
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

---

## LICENSE

**TBD** — To be determined before Stage 2 commercial release

---

**Remember:** This is a 10-year project. Quality over speed. Build it right.

**Current Focus:** Sign off Priority 2 specs (Agent Movement, Shot Mechanics, Perception System, Decision Tree), re-sign-off Pass Mechanics, then begin Priority 3.