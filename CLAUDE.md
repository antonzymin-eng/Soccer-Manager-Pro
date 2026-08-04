# CLAUDE.md — Tactical Director

> **Created:** March 26, 2026, 11:00 PM PST
> **Change log:** `docs/tracking/CHANGELOG.md` — the `**Last Updated:**` entry chain.
> **Open issues:** `docs/tracking/open-issues.md` — live blockers (`open-issues-resolved.md` for closed ones).
> **Purpose:** Authoritative rules for any AI agent (Claude Code, Claude chat, etc.) working on this project. Read this file completely before every task.

---

## PROJECT IDENTITY

**Tactical Director** is a football (soccer) simulation game targeting "Football Manager killer" ambitions. It follows a 10-year, 6-stage development plan. The project is solo-developed with AI assistance.

**Current phase:** Stage 0+1 — Implementation, with the specification frontier now running ahead of the code.

**Specifications:** `SPEC_INDEX.md` records **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED — every spec in the registry is approved.** The APPROVED set is the Stage-0 twenty (all APPROVED May 18, 2026) plus 23 Stage-1-forward and management-layer specs (#21–#34, #37, #38, #40–#45, #49). The last ten — #53, #35, #46, #36, #54, #47, #48, #50, #51, #39 — were promoted **and approved** on July 27, 2026, emptying the pre-promotion backlog and closing the specification phase entirely. The only candidate without a spec is **#52** (Multiplayer Transport), deliberately deferred behind the Stage-5 Fixed64 migration. **Approval approves the forward design, not an implementation** — see the live gap below, which is now the project's dominant fact.

**Implementation:** `src/` holds **31 production assemblies**. Every Stage-0 spec is implemented except **#9 Fixed64** (deferred to Stage 5+ by design) and **#20 Code Standards** (a style guide, not a coded subsystem). A `MatchEngine` composition root wires the subsystems into the deterministic-sim 7-phase tick pipeline, and **a production match now plays** — the possession bootstrap (§5.Z Phase H, July 26, 2026) closed ERR-030-014, under which every match had been a 90-minute 0–0 deadlock with the ball never in motion. **Match Analytics #37 T0 landed July 27, 2026** (`src/match-analytics/` — value types + the pure `XgLocationModel`; no engine wiring yet), giving it a `src/` assembly for the first time.

**The live gap is now the project's dominant fact.** With the specification phase closed, **22 of the 53
APPROVED specs have no `src/` assembly at all** — the 12 listed below plus the ten approved on July 27.
The specification frontier runs a long way ahead of the implementation, which is a deliberate posture
(specify before coding), and it makes one habit dangerous: **"the spec is APPROVED" now says nothing
whatsoever about whether code exists.** It is true of ~42% of the registry.

**The 22 with no assembly:** #29 Training, #31 Transfers, #32 Scouting, #33 Personalities/Morale, #34 Staff, #40 Finances, #41 Injuries, #42 Youth, #43 Competition Structure, #44 Discipline, #45 Board, #49 Localization — plus the ten approved on July 27: #35, #36, #39, #46, #47, #48, #50, #51, #53, #54.

Sequencing for closing the gap is in `docs/tracking/path-to-playable-roadmap.md`, which is now the
project's live critical path. **Check `src/` before assuming a consumer is available** — the assembly map
above is the reliable index, not the spec registry.

`src/CLAUDE.md` is the authoritative coding guide. Read it before writing any code.

---

## REPO STRUCTURE

```
Soccer-Manager-Pro/
├── CLAUDE.md                       ← You are here. Read first. Always.
├── .claude/                        ← Agent config: advisor council, orchestrator, project skills (see its README)
├── README.md                       ← Project overview, status, documentation hierarchy
├── Assets/ Packages/ ProjectSettings/   ← Unity project shell (target editor 6000.4.9f1, DX11)
├── docs/
│   ├── planning/                   ← Master volumes I–IV, master development plan, best practices
│   ├── design/ui-mockups/          ← Non-normative UI visual reference (not on any build path)
│   ├── specs/
│   │   ├── SPEC_INDEX.md           ← Canonical spec numbering and status — 53 folders, all APPROVED
│   │   └── <spec-folder>/          ← One folder per spec; see SPEC_INDEX.md for the number↔folder map
│   └── tracking/                   ← Progress, error log, file manifest, roadmaps, design supplements
├── src/                            ← Implementation (coding began May 19, 2026) — 31 production assemblies
│   ├── CLAUDE.md                   ← Coding guide (read before writing any code)
│   └── <assembly>/                 ← See the assembly map below
└── tools/
    ├── dotnet-ci/                  ← Non-certifying Linux compile/test gate (asmdef→csproj + Unity shim)
    ├── unity-ci/ perf-harness/ spec-stress/
    └── *.py, run-perf-local.sh     ← Budget auditor, seed selection, round-resolution fitter
```

**`src/` assembly map.** Most assemblies are named for their spec folder, but not all — several specs are
implemented inside a differently-named assembly, and several assemblies are not a numbered spec at all.
Do not infer the mapping from the folder name:

| Assembly | Spec | Notes |
|---|---|---|
| `ball-physics`, `agent-movement`, `collision-system`, `first-touch`, `pass-mechanics`, `shot-mechanics`, `heading-mechanics`, `goalkeeper-mechanics` | #1–#6, #10, #11 | Physics layer |
| `positioning-ai`, `pressing-ai`, `defensive-ai`, `attacking-ai` | #12–#15 | Mechanics layer. `positioning-ai` **also** hosts #23 dismarking, #24 build-up structures, #25 positional rotations |
| `decision-tree`, `perception-system` | #8, #7 | AI layer. `decision-tree` also carries #23's marked-pass-target penalty |
| `deterministic-sim`, `event-system` | #16, #17 | Cross-cutting foundations, referenced by all layers |
| `tactical-instructions` | #21 | Also hosts #26 tactical presets |
| `living-world` | #22 | |
| `player-database` | **#27** Squad / Player Data Layer | Name differs from the spec folder (`squad-player-data/`) |
| `player-progression` | **#28** | T0 only — draw-free core, not engine-wired |
| `season-save` | **#30** Season & Competition Loop | Also hosts the league bootstrap and the unified season save-file root |
| `match-analytics` | **#37** Match Analytics & Statistics | T0 only — value types + `XgLocationModel`; no engine wiring, no aggregator. Presentation-layer derivation: **no sim assembly may reference it** (guarded mechanically) |
| `ui-framework` | **#38** UI / Client Framework | T0 substrate only; no screens, no UGUI binding |
| `performance-optimization`, `testing-strategy` | #18, #19 | Infrastructure only — no game-loop types |
| `project-constants` | — | Shared `[GT]` config; read-only by all |
| `match-engine` | — | **Composition root.** Not a numbered spec; governed by `docs/tracking/match-engine-design.md` |
| `match-viewer`, `match-client-core`, `match-client-unity` | — | Presentation tooling / client seams; not numbered specs |
| `match-client-web` | — | **The PM-1 browser match client** (roadmap B6). Not a numbered spec; governed by `docs/tracking/browser-match-client-design.md`. The only assembly above BOTH `ui-framework` and `match-analytics` |

**Rules:**
- Each spec folder contains ONLY current-version files. No version suffixes in filenames. Git tracks history.
- `SPEC_INDEX.md` is the canonical source of truth for spec numbers, folder names, and approval status.
- `PROGRESS.md` is the canonical source of truth for schedule and milestone tracking.
- `src/CLAUDE.md` is the authoritative guide for all coding conventions, including the assembly layer taxonomy and the reference-direction rule (**AI → Mechanics → Physics, never the reverse**).
- An APPROVED spec does not imply an implementation exists. Check `src/` first.

---

## CRITICAL DOMAIN CONVENTIONS

These conventions have caused bugs. Memorize them.

### Coordinate System

**Authoritative source:** Ball Physics Spec #1, §1.2 and Appendix C.

| Axis | Direction | Range | Notes |
|------|-----------|-------|-------|
| X | Goal-to-goal (pitch length) | 0–105m | |
| Y | Touchline-to-touchline (pitch width) | 0–68m | |
| Z | Height (vertical, up) | 0m = ground | Ball center rests at 0.11m |

**Origin:** Corner of pitch (0, 0, 0) — NOT pitch center.

### Fatigue Convention

`0.0 = fully rested`, `1.0 = fully fatigued`. Any inversion is a critical error. This has been found inverted before (Pass Mechanics §2 FR-02, now fixed).

### Constant Tags

Every constant in every spec MUST have exactly one of these source tags:

| Tag | Meaning | Rule |
|-----|---------|------|
| `[GT]` | Gameplay-Tuned | Designer sets value; must live in tunable config |
| `[EST]` | Estimated | Placeholder; must be validated before implementation |
| `[FIXED]` | Fixed / physical law | Derived from physics; never tune |
| `[DERIVED]` | Derived from other constants | Formula must be documented; never set independently |
| `[CROSS]` | Cross-spec constant | Defined in another approved spec; consumed read-only here; never set independently in this spec. Citation must name the authoritative spec and section. Use `[CROSS]` only when the value is copied verbatim without modification — if a formula transforms it, tag the result `[DERIVED]`. |
| `[CROSS-PENDING]` | Cross-spec constant blocked on an upstream `IN PROGRESS` spec | Used when a spec consumes a constant that will be `[CROSS]` once the upstream authority spec reaches `APPROVED`, but the numeric value is not yet allocated. Citation must name the authoritative spec, section, and the `spec-error-log.md` back-prop ID tracking the allocation. Promoted to `[CROSS]` atomically with upstream approval. Use sparingly — every `[CROSS-PENDING]` tag is an outstanding cross-spec dependency that gates the consuming spec's own `APPROVED` transition. |

Constants live in their designated `.cs` constant catalogues — no magic numbers in formula code.

### Parameter-Based Physics (No Type Enums)

The Decision Tree supplies physical intent parameters (velocity, spin, angle). Physics systems translate these into vectors. There are NO `KickType`, `ShotType`, or `PassType` enums in the physics layer.

### Heartbeat Tick Rate

Tactical/AI loop: **10 Hz** (100ms per tick). Physics/render loop: **60 Hz** (~16.67ms per frame). These are different loops. Do not conflate them.

### Interface Design Principle

**Write interfaces only when both sides are specified.** Do not create interfaces against unspecified systems. This avoids phantom interface proliferation (ERR-001, ERR-004).

---

## CROSS-REFERENCE SYSTEM

Specs use typed cross-reference IDs:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `XC-` | Cross-spec reference | XC-001 |
| `FM-` | Formula reference | FM-003 |
| `EC-` | Edge case reference | EC-012 |
| `ERR-` | Spec Error Log entry | ERR-010 |

**KNOWN HAZARD — Spec Renumbering Cascades:** When any spec changes its canonical number, ALL cross-references across ALL files must be updated. This has been the single most recurring bug class in this project.

**KNOWN HAZARD — Stale Spec Numbers in Old Files:** Many files written before February 2026 contain wrong spec numbers from an earlier numbering scheme. A complete old-to-correct mapping is in `SPEC_INDEX.md` under "FORMER NUMBERING".

---

## SPEC FILE CONVENTIONS

### Template Structure (every spec follows this)

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, dependencies, key decisions |
| 2 | Functional requirements, data structures, failure modes |
| 3 | Core formulas, algorithms, pseudocode (subsections as needed) |
| 4 | Architecture, file layout, interface contracts |
| 5 | Test plan (unit + integration + validation scenarios) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and Stage 1+ deferrals |
| 8 | References, citations, DOI verification |
| 9 | Approval Checklist (quality gate) |
| Appendices | Derivations, verification tables, sensitivity analysis |

### Naming Inside Spec Folders

Files within a spec folder use descriptive names without version suffixes:

```
pass-mechanics/
├── outline.md
├── section-1.md
├── section-2.md
├── section-3-1.md          ← Subsections use hyphens
├── section-3-2.md
├── section-3-3-to-3-4.md   ← Grouped subsections
├── section-3-5-to-3-6.md
├── section-3-7-to-3-9.md
├── section-4.md
├── section-5.md
├── section-6.md
├── section-7.md
├── section-8.md
├── section-9-approval-checklist.md
├── appendices.md
└── audit-report.md          ← Comprehensive audit (if completed)
```

---

## AI BEHAVIORAL RULES

### Before Any Task

1. Read this entire `CLAUDE.md`.
2. Check `SPEC_INDEX.md` for current spec numbers and approval status.
3. If modifying a spec, read ALL files in that spec's folder first — not just the target file.
4. If the task involves cross-references, grep the entire `docs/specs/` tree for stale references before finishing.

### When Writing or Editing Specs

- Every constant must have a `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]` tag.
- Every formula must include units, valid input ranges, and at least one worked example.
- Never fabricate verification values in Approval Checklists. All values must be programmatically verifiable against source files.
- Append a version history entry to every modified file.
- Include creation date and purpose header on every new file.

### When Writing Code

- C# with Unity 6 LTS conventions (target editor: 6000.4.9f1; see `docs/tracking/certification-platform.md`).
- Struct-based, zero-allocation architecture in the game loop.
- All constants in designated constant catalogue files — no magic numbers.
- **Stage 0 uses `float`. Fixed64 migration is a Stage 5+ concern** (Spec #9 will define the library; existing approved physics specs are drafted against `float` and re-verified against Fixed64 only when cross-platform multiplayer becomes a requirement). Single-machine determinism (replay, save/load, debug rewind) is achieved via state snapshots, not deterministic arithmetic. Cross-platform bit-exact parity is a Stage 5 deliverable, not a Stage 0 quality gate.
- Deterministic replay is a hard requirement — no `System.Random`, no `DateTime.Now` in game logic.
- SplitMix64 for deterministic RNG. In Python tooling: omit `UL` suffix from C# constants; mask all intermediate multiplications with `& 0xFFFFFFFFFFFFFFFF`.

### Things That Have Gone Wrong Before (Learn From History)

| Trap | What happened | Prevention |
|------|---------------|------------|
| Stale spec numbers | Decision Tree was #7 in ~75 places; canonical is #8 | Always check SPEC_INDEX.md |
| Fabricated checklist values | Approval Checklist claimed sections existed that were never written | Verify every checklist entry against actual files |
| Inverted fatigue | FR-02 said "1 = rested" — opposite of correct | 0 = rested, 1 = fatigued. Always. |
| Wrong coordinate origin | "Pitch center" comment in Agent Movement §3.5 | Corner-origin is authoritative (Ball Physics §1.2) |
| Phantom interfaces | Interfaces written against unspecified consumer systems | Only write interfaces when both sides are specified |
| Superseded file references | Approval checklist pointed to v1.2 when current was v1.3 | Git versioning eliminates this (no version suffixes) |
| Never-compiled surfaces | Six consecutive spec test suites — and one *production* assembly (#8) — had never compiled; every "the suite enforces X" claim was unverifiable | The `tools/dotnet-ci` gate compiles the whole tree on every push. Never claim a suite enforces something without running it |
| Tests that verify composition runs, not that it *works* | The 600-tick capstone asserted tick count, cadence, finiteness, bounds and digest advance — all true of a match in which nothing happens. Every match was a 0–0 deadlock with the ball never in motion for months (ERR-030-014) | Assert the *outcome* the system exists to produce, not just that it ticks without throwing |
| Home-team-only worked examples | Three home/away asymmetry defects (#8 ERR-008-002) shipped because every spec example and every fixture used the home team | Mirror any team-relative geometry test to the away side |

---

## TRACKING DOCUMENTS

| Document | Location | Purpose |
|----------|----------|---------|
| `SPEC_INDEX.md` | `docs/specs/SPEC_INDEX.md` | Canonical spec numbering, folder mapping, approval status |
| `PROGRESS.md` | `docs/tracking/PROGRESS.md` | Schedule, milestones, weekly log |
| `CHANGELOG.md` | `docs/tracking/CHANGELOG.md` | The `**Last Updated:**` entry chain (was the CLAUDE.md header) |
| `open-issues.md` | `docs/tracking/open-issues.md` | Live blockers; `open-issues-resolved.md` holds closed entries |
| `spec-error-log.md` | `docs/tracking/spec-error-log.md` | Cross-spec architectural errors and remediation status |
| `file-manifest.md` | `docs/tracking/file-manifest.md` | Authoritative file inventory (update after every file change) |
| `fix-manifest-pass-mechanics.md` | `docs/tracking/fix-manifest-pass-mechanics.md` | Per-audit fix tracking (Pass Mechanics #5) |
| `certification-platform.md` | `docs/tracking/certification-platform.md` | Pinned Stage-0 host/engine tuple; recertification rule |
| `cert-run-runbook.md` | `docs/tracking/cert-run-runbook.md` | Step-by-step certification run procedure |

### Roadmaps (meta-planning — design no system, open no spec)

| Document | Answers |
|----------|---------|
| `management-layer-spec-roadmap.md` | *Which specs to author, in what order* — the #27–#51 management/off-pitch set, dependency graph, authoring waves |
| `path-to-playable-roadmap.md` | *Which code to land, in what order, to reach a playable build* — Track S (simulation) vs Track C (client), and the quantified constraints that ordering must respect |

### Design supplements

`docs/tracking/*-design.md` (42 files) is a governance class of its own: a **DESIGN SUPPLEMENT** is a
converged, adversarially-reviewed design note that either (a) precedes promotion to a numbered spec
(the #21/#22 precedent — a registry row lands only at promotion), or (b) permanently governs a surface that is
deliberately *not* a numbered spec (`match-engine-design.md` for the composition root is the canonical
example). A supplement is not a spec: it does not appear in `SPEC_INDEX.md` and confers no approval status.
Before designing anything, grep `docs/tracking/` — the surface likely already has one.

**As of July 27, 2026 every class-(a) supplement has been promoted**, so a supplement you find in
`docs/tracking/` is now either class (b) — a permanent governor, like `match-engine-design.md` — or the
**pre-promotion history of a spec that already exists**. In the second case the spec folder wins: a
supplement is frozen at its convergence and the section files carry the PASS-1 corrections made after it
(three supplements' proposed `ERR-` ids, for instance, were already filed and were reassigned at
promotion). Read the supplement for the *reasoning*; read the spec for the *contract*.

---

### Project skills and agent configuration

`.claude/` holds checked-in agent configuration — checked in rather than installed personally because
each piece encodes conventions that live in this repo and must version with them. `.claude/README.md`
is its index. Two kinds of thing live there:

**Agent patterns** change *who* does the work: `advisor` (a two-advisor council convened **before**
implementation, on Opus regardless of the session model), `orchestrator` (drives one
path-to-playable roadmap item end to end), `adversarial-review` (the **post**-implementation H/M/L
review loop), and `chat-review` (session analysis). Those surfaces deliberately do not overlap — see
the boundary table in `.claude/README.md`.

**Workflow encodings** change *how* a recurring job is done correctly:
`match-realism-pass`, `snapshot-schema-bump`, `err-file-and-backprop`, `landing-close-out`,
`spec-promotion`, `dotnet-gate`. Each was derived from measured repetition in the last 200 commits
and carries the traps this project has actually hit; `.claude/skills/README.md` records that
evidence. The two that need a review step invoke `adversarial-review` rather than restating it.

`orientation` is account-level, not in this repo.

---

## OPEN ISSUES

> Full entries live in **`docs/tracking/open-issues.md`**; closed ones in
> **`docs/tracking/open-issues-resolved.md`**. Read the owning file before acting on
> any item below — the titles here are an index, not the record.
>
> **When resolving an issue, move its entry to the resolved archive in the same
> commit.** Do not re-inline entries into this file.

**12 active** / 41 resolved. *Re-filed August 2, 2026 — eight entries archived (six closed-but-unmoved, plus a duplicated pair); three titles amended to lead with what remains open rather than what has landed.*

- Conversion at contact — the CLAIM defect FIXED (ERR-011-008, §5.Z.23); REMAINDER: the `pointQuality` lottery is blocked on a design decision (measured: the geometry-aware form collapses catches to zero and no `[GT]` in range recovers them), and parry placement is unfixed but currently costless
- Close-chance creation — the DRIBBLE-direction defect FIXED (ERR-008-018, §5.Z.24: the average final-third dribble pointed AWAY from goal); REMAINDER: the funnel itself did not move — the ball still enters the box on 5% of final-third episodes, and the bound is now localized to #8 §3.1.3 generating PASS candidates only at a teammate's CURRENT POSITION, so the tree cannot pass to a place
- Injury / aging research alignment — design supplement OPENED, AR-converged, awaiting owner sign-off
- Foul/card heuristic issues ~7 red cards per 9 minutes of played football — the most visible unrealism in a match now that matches actually play
- `EnvironmentFingerprint.floatModelHash` — hasher + §4.8.3 Mono mapping LANDED (Option A); §4.8.2 runtime MXCSR gate code LANDED (July 21, 2026); compiled plugin + certified live read LANDED July 22, 2026 (ERR-016-006) — REMAINDER: `SaveManager` still writes `Fingerprint = null`; load-bearing only where a real cert run reads a `SaveManager`-written save — no longer host-blocked (the certification host block cleared July 19, 2026 and the MXCSR plugin host block cleared July 22, 2026); the gap is unimplemented code, not host access
- Goalkeeper Mechanics (#11) / Heading Mechanics (#10) engine integration — Phase 1 (opt-in) LANDED; the GK/Heading attribute projections now have a live consumer — REMAINDER: `CollisionConsumer` AGENT_BALL duel fan-out, DT-emitted HEADER (ordinal 8 → composure-noise rebaseline), attribute-modulated save commit
- Advanced positional behaviors + game-model/AI-manager tactics — design supplements OPENED (candidate specs #23–#26) — all four promoted to specs and landed; REMAINDER: #26 §9.2 own-`[GT]` balance review
- Living World (#22) season/world loop — slices 1–7 LANDED (incl. the KD-10 season composition root + the InteractionTextGenerator wired into it + deep-memory auto-cite + the opt-in arc-trigger evaluator / `world.arcs` sub-stream); upstream-gated services open
- UI / Client Framework (#38) — T0 substrate LANDED; Wave-7 screens + the UGUI binding remain open
- Presentation layer — minimal match viewer LANDED; interactive Unity client remains open
- Approval tags created locally, not yet pushed
- Assembly layer taxonomy (Spec #20 §3.5.2) places 19 of 31 assemblies — ERR-020-002 proposal filed, awaiting owner sign-off
