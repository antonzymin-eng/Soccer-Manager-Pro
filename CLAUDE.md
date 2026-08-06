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

**Implementation:** `src/` holds **33 production assemblies**. Every Stage-0 spec is implemented except **#9 Fixed64** (deferred to Stage 5+ by design) and **#20 Code Standards** (a style guide, not a coded subsystem). A `MatchEngine` composition root wires the subsystems into the deterministic-sim 7-phase tick pipeline, and **a production match now plays** — the possession bootstrap (§5.Z Phase H, July 26, 2026) closed ERR-030-014, under which every match had been a 90-minute 0–0 deadlock with the ball never in motion. **Match Analytics #37 T0 landed July 27, 2026** (`src/match-analytics/` — value types + the pure `XgLocationModel`; no engine wiring yet), giving it a `src/` assembly for the first time.

**The live gap is now the project's dominant fact.** With the specification phase closed, **20 of the 53
APPROVED specs have no `src/` assembly at all** — the 10 listed below plus the ten approved on July 27.
(It was 22 until August 5, 2026, when **#29 Training and #41 Injuries & Medical** landed T0 assemblies together.)
The specification frontier runs a long way ahead of the implementation, which is a deliberate posture
(specify before coding), and it makes one habit dangerous: **"the spec is APPROVED" now says nothing
whatsoever about whether code exists.** It is true of ~42% of the registry.

**The 20 with no assembly:** #31 Transfers, #32 Scouting, #33 Personalities/Morale, #34 Staff, #40 Finances, #42 Youth, #43 Competition Structure, #44 Discipline, #45 Board, #49 Localization — plus the ten approved on July 27: #35, #36, #39, #46, #47, #48, #50, #51, #53, #54.

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
├── src/                            ← Implementation (coding began May 19, 2026) — 33 production assemblies
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
| `training-system` | **#29** | T0 only (Aug 5, 2026) — the day step, the growth-input read, the match-entry projection and the `InjuryRiskContribution` #41 reads. Draw-free by design (FR-TR-008); **inert — nothing constructs it** |
| `injuries-medical` | **#41** | T0 only (Aug 5, 2026) — the recovery-then-draw day step + the keyed occurrence draw (`DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`; **no** registered stream, ERR-041-002). **Inert — nothing constructs it**; T1 codec and T2 wiring open |
| `season-save` | **#30** Season & Competition Loop | Also hosts the league bootstrap and the unified season save-file root |
| `match-analytics` | **#37** Match Analytics & Statistics | T0 only — value types + `XgLocationModel`; no engine wiring, no aggregator. Presentation-layer derivation: **no sim assembly may reference it** (guarded mechanically) |
| `ui-framework` | **#38** UI / Client Framework | T0 substrate only; no screens, no UGUI binding |
| `performance-optimization`, `testing-strategy` | #18, #19 | Infrastructure only — no game-loop types |
| `project-constants` | — | Shared `[GT]` config; read-only by all |
| `match-engine` | — | **Composition root.** Not a numbered spec; governed by `docs/tracking/match-engine-design.md` |
| `match-viewer`, `match-client-core`, `match-client-unity` | — | Presentation tooling / client seams; not numbered specs |
| `match-client-web` | — | The browser match client (roadmap B6). Not a numbered spec; governed by `docs/tracking/browser-match-client-design.md`. The only assembly above BOTH `ui-framework` and `match-analytics`. **NOT the shipping UI** — B6 was reversed to full Unity on Aug 3, 2026; retained as the host-free reference harness. Keep green, do not extend |

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
| Tuning a machine that is missing pieces | Seven §5.Z passes fitted `[GT]`s against an engine where the keeper never left his line and **no player could tackle** (`GetAndClearTackleFlag` hardcoded `false` in both adapters). The hazard is diagnostic, not arithmetic: 18%-vs-11% conversion reads as "the shot model is too generous" when part of it is "nobody narrows the angle" | **KD-W1** — do not land a `[GT]` governing an unwired subsystem. Check `match-engine-wiring-backlog.md` first. Measure and fix defects freely; constants wait for one calibration pass against the complete engine |

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
| `match-engine-wiring-backlog.md` | *Which already-built match-engine code has no production caller* — the dormant-capability inventory (Aug 4, 2026), its wire-order, and **KD-W1** below. Match engine only; the 22 assembly-less specs stay with the roadmap above |

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

**14 active** / 41 resolved. *Re-filed August 2, 2026 — eight entries archived (six closed-but-unmoved, plus a duplicated pair); three titles amended to lead with what remains open rather than what has landed. August 4, 2026 — the wiring-backlog entry added at the head; later same day, the football-judgment remediation entry added above it.*

- **Football-judgment proxy review — 32 itemized findings open across 24 specs; doctrine (§6) governs the fixes; ERR-008-020 (template), ERR-008-019 (the founding long-shot cliff), ERR-008-021 (the shot lane) and ERR-008-022 (the review over -021) LANDED.** The review (`football-judgment-proxy-review.md`) swept all 53 APPROVED specs for continuous football judgments collapsed into thresholds/bare geometry. Its §6 remediation doctrine (owner-converged Aug 4) is binding on every fix: P1 continuous-never-cliff, P2 skill-as-discrimination-fidelity, P3 the attribute ownership ledger, P4 intent-as-first-class-object, P5 chain calibration pivoted on today's baseline (KD-W1). **ERR-008-020** (#8 §3.1.3.3 pass-lane threat model) landed Aug 4 as the template — spec+code same commit, gate NOT runnable in the authoring environment. **ERR-008-019** (#8 §3.2.3.1 long-shot cliff — the review's founding finding, whose original "FIXED" record was verified false at the -020 landing) landed Aug 5 under the soft-reserved id, re-verified free, and owner-revised the same day to the FULL-RANGE ramp (half-width 0.25 = the whole attribute: raw 1 exactly 0.05, raw 20 exactly 0.55, ~0.026 per point, no plateaus; P5 mean 0.30 preserved): **digest invariance was RETRACTED at the same-day adversarial review over that landing** — the argument assumed a 0.5 m possession radius, but the KD-H3 loose-ball pickup grants possession at **1.0 m** and leaves the ball where it lies, which makes a raw-19 MIDFIELD shot marginally generator-reachable (just above 34.0 m vs a 34.21 m range gate) and there ramp (≈ 0.524) ≠ step (0.55); behaviour change owner-intended, formula/constants/tests untouched; gate likewise not runnable. **ERR-008-021** (the §6.4 shot-lane follow-up -020 deferred) landed Aug 5: #8 §3.1.4.3/§3.2.3.2's occlusion test counted an opponent's WHOLE angular width when his angular centre fell inside the goal arc and NOTHING when it fell outside — a defender standing squarely across the near post scored the shooter a fully open goal, and 4 cm of lateral position stepped `GoalOpeningScore` 0.595 → 1.000 — and the width was body radius alone. Now the true angular OVERLAP of the blocking disc with the arc (continuous by construction — this one needed no ramp constant and deletes a tolerance epsilon) × Anticipation/Positioning ability read through the SHOOTER's Vision fidelity, with the **goalkeeper exempt from the ability term** because #11 §3.5/§3.7.0 owns keeper shot-stopping (P3). P5 exact: old rectangle and new trapezoid both integrate to `4h·halfArc` over a uniformly-placed blocker. **Digest invariance NOT claimed** — live on every generated shot; 10 test locks, 5 of the 9 evaluable against the old model failing on it (published as "9 / 5 of 8" and corrected at ERR-008-022); gate not runnable here. The 34-finding tally is unchanged (the shot lane was never itemized — it surfaced in §6.4), so 32 remain open. Recorded-not-fixed: `IsInShotPath`'s hard corridor end-bounds, and §3.2.10's constant catalogue, now left behind by five consecutive #8 landings. **ERR-008-022** landed Aug 6 from the **adversarial review over the -021 landing**: §3.1.4.3's lane test bounded the shooting lane by a plane through the goal **CENTRE**, which for any off-centre shooter cuts diagonally across the goal mouth — it discarded the **far-post** blocker on **20,213 of 20,213** sampled in-range off-centre shooters, dropped a keeper on his line at goal centre for *every* shooter position (shooter (95,20) read a **completely open goal**), and admitted an opponent standing *behind* the goal line at the keeper's radius, so -021's overlap model was denied much of the geometry it exists to price and **achieved substantially less than it claimed**. Two further predicates in the same derivation were **larger cliffs than the one -021 removed**: `GOAL_MIN_SHOT_DIST` (1.000 → 0.050 across 1 cm, taking the SHOOT option with it) and the goalkeeper classification (0.768 → 0.311 across 2 cm, which -021 *widened* to 0.551). All three fixed — goal-line-plane bound + two `[GT]` ramp widths, `gkness` lerping radius and the P3 exemption together. **Three -021 verification claims were false and are corrected:** the P5 exactness argument (holds only for `h ≤ halfArc`, up to **2×** above it — the stated reason no recalibration was needed, withdrawn), the test count ("9 locks / 5 of 8" → **10** / 9 evaluable / 5 fail / 4 pass), and the §3.2.3.2 worked example (its opponent was classified a **goalkeeper**, so all three of its numbers were unreachable). The suite was inadequate too — the over-blocking half had **no lock**, a mutant restoring the pre-fix full width passed all ten, and `NullAttributeView` was a **tautology** in both the pass and shot suites. Suite 10 → **15**; gate likewise not runnable. Tally still 32 (the shot lane was never itemized). Recorded-not-fixed: `MIN_GOAL_VISIBILITY` is still a hard predicate on option *existence*, the GK positional proxy still reads a deep defender as part-keeper, and §3.2.10's constant catalogue is now **six** #8 landings behind. Next in line per §6.3: the remaining formula-patch findings; mechanism-class items (pass-to-space, run signaling, #36/#27 selection) need design supplements first

- **#29 Training / #41 Injuries & Medical — T0 assemblies landed August 5, 2026, both INERT.** `src/training-system/` and `src/injuries-medical/` exist and are downward-referencing only, but **nothing constructs either of them**: T1 (the two save codecs + the `SeasonSaveCodec` sub-blob composition, `TRAINING_SAVE_FORMAT_VERSION` / `MEDICAL_SAVE_FORMAT_VERSION` = 1 each) and T2 (the #30 tick-order slots, the `IsAvailable` read into squad selection, the FR-TR-025 / FR-MD-025 roster-membership handoff) are both open, and #29's `ComputeTrainingInput` returns `TrainingInput.Neutral` on both branches because #28's type still has no fields. **Both assemblies are now GATE-VERIFIED** (PR #299, run 394, August 5, 2026): build succeeded 0 errors, `TrainingSystem.Tests` **27/27** and `InjuriesMedical.Tests` **40/40** passed, 0 skipped in either, whole-tree gate PASSED with an empty quarantine. Every "the suite locks X" claim across the five review passes was written against never-executed code; all 67 now hold by execution. The authoring environment still has no .NET SDK (the installer stays 403 at the proxy), so CI remains the only compiler for this work. Filed with the landing: ERR-041-002 (#41 §2.2/§3.1's `rng.DrawKeyed` names an API #16 does not expose; resolved as a keyed SplitMix64 derivation, the ERR-030-012 posture) and ERR-041-001 closed (`DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` in code; ordinal 92 deliberately not allocated). **An adversarial review over the landing then found 2 High, 4 Medium, 4 Low, all fixed, converged pass 2** — headline **ERR-041-003**: `InjuryRiskMax` was declared `[GT]` in BOTH catalogues under different config sections, so one contract scale had two independently settable keys, and the equality test written to catch that passed unconditionally because the gate leaves the config unbound (re-tagged `[CROSS]`). Second High: `SetFocus` took the club's id and state arrays as separate arguments, so one club's ids with another's states — same length, no guard — wrote the wrong club's player; the command moved onto `TrainingSchedule.TrySetFocus`. **Recorded, not fixed:** both specs mitigate injury risk on the same three physical attributes, so robustness is priced in twice across the layers and #29's maximum risk never means certain occurrence at #41 — pinned by assertion for the balance pass. **A fifth review pass then measured the thing none of the first four had: the daily occurrence PROBABILITY through the real producer chain.** Every occurrence test in #41's suite forces the outcome with a risk the producer cannot reach, so nothing established what the wired system would do at career inputs — the ERR-030-014 shape one layer up. At today's illustrative `[GT]`s a freshly inserted regen is **23% likely to be injured on his first day** (`ConditionStart` sits 3000 below `ConditionMax` and the shortfall carries weight 1 on the very scale the draw denominator derives from), half-fatigued is **43% per day**, and the **default Balanced focus converges on exactly 0 forever** — its daily load equals the passive recovery, so fatigue never accrues and the conditioning shortfall goes to zero. Two to three orders of magnitude out at both ends, in opposite directions. Not retuned (KD-W1 — the subsystem is unwired); the three numbers are locked by a characterization test so the balance pass leaves a visible diff, and the single lever that rescales all of them is the shared `InjuryRiskMax` ceiling the `[CROSS]` mirror made one dial
- **Match-engine wiring backlog — W1 WIRED; 9 built subsystems still have no production caller, and the `[GT]` freeze (KD-W1) that follows.** **W1 landed August 4, 2026** — the keeper now comes off his line to reduce the shooting angle (`TryCommitRushIntents` gives `CommitRushIntent` its first production caller; a *chasing* defender does not keep him home, only a goal-side one, and how far he comes is his own `OneVsOne`/`Composure`/fatigue via new #11 §3.7.0). It surfaced two spec defects — `ERR-011-010` (§3.7 delegated the rush decision to Decision Tree #8, which has no keeper model and cannot acquire one, so the condition had no owner for ten weeks) and `ERR-011-009` (a rush that reached its target had no state-machine exit at all) — **but nothing in that landing has been executed: no .NET SDK in the authoring environment, so no gate run and no measurement**. Still dormant, headline: **no player has ever made a tackle** (`GetTackleIntentRequests` read by nobody + `GetAndClearTackleFlag` hardcoded `false` in both adapters ⇒ #5 §3.8.5's interrupt branch is unreachable). Full inventory and wire-order: `match-engine-wiring-backlog.md`
- Conversion at contact — the CLAIM defect FIXED (ERR-011-008, §5.Z.23); REMAINDER: the `pointQuality` lottery is blocked on a design decision (measured: the geometry-aware form collapses catches to zero and no `[GT]` in range recovers them) — **PARKED August 4, 2026: the keeper rush trigger (wiring backlog W1) changes the contact geometry the decision turns on** — and parry placement is unfixed but currently costless
- Close-chance creation — the DRIBBLE-direction defect FIXED (ERR-008-018, §5.Z.24: the average final-third dribble pointed AWAY from goal); REMAINDER: the funnel itself did not move — the ball still enters the box on 5% of final-third episodes, and the bound is now localized to #8 §3.1.3 generating PASS candidates only at a teammate's CURRENT POSITION, so the tree cannot pass to a place
- Injury / aging research alignment — design supplement OPENED, AR-converged, awaiting owner sign-off
- Foul/card heuristic issues ~7 red cards per 9 minutes of played football — the most visible unrealism in a match now that matches actually play
- `EnvironmentFingerprint.floatModelHash` — hasher + §4.8.3 Mono mapping LANDED (Option A); §4.8.2 runtime MXCSR gate code LANDED (July 21, 2026); compiled plugin + certified live read LANDED July 22, 2026 (ERR-016-006) — REMAINDER: `SaveManager` still writes `Fingerprint = null`; load-bearing only where a real cert run reads a `SaveManager`-written save — no longer host-blocked (the certification host block cleared July 19, 2026 and the MXCSR plugin host block cleared July 22, 2026); the gap is unimplemented code, not host access
- Goalkeeper Mechanics (#11) / Heading Mechanics (#10) engine integration — Phase 1 (opt-in) LANDED; the GK/Heading attribute projections now have a live consumer — REMAINDER: `CollisionConsumer` AGENT_BALL duel fan-out, DT-emitted HEADER (ordinal 8 → composure-noise rebaseline), attribute-modulated save commit
- Advanced positional behaviors + game-model/AI-manager tactics — design supplements OPENED (candidate specs #23–#26) — all four promoted to specs and landed; REMAINDER: #26 §9.2 own-`[GT]` balance review
- Living World (#22) season/world loop — slices 1–7 LANDED (incl. the KD-10 season composition root + the InteractionTextGenerator wired into it + deep-memory auto-cite + the opt-in arc-trigger evaluator / `world.arcs` sub-stream); upstream-gated services open
- UI / Client Framework (#38) — T0 substrate LANDED; Wave-7 screens + the UGUI binding remain open
- Presentation layer — minimal match viewer LANDED; interactive Unity client remains open. Its host-free
  phases are now ALL complete: P0/P2 (July 24), P1/P3 (July 27), **the head-less half of P6
  (August 3)** — `MatchSession.TickOnce/CaptureSave/RestoreFrom`, `TickStampedCommandReplay`, and the
  two §5-P6 closed-loop scenarios, which meet PM-1's determinism exit criterion — and **P4a, the render
  model (August 3)**: `PitchViewProjection` (the one corner-origin ⇄ centre-origin adapter),
  `PitchMarkings` (the IFAB catalogue as shapes, off the existing `[FIXED]` values),
  `MatchRoster`, and `MatchRenderProjection` → `AgentRenderModel`/`BallRenderModel`. P4 was split on
  the standing "keep logic out of `MonoBehaviour`s" rule: **P4a is every render decision, P4b is the
  binding.** REMAINDER: **P4b/P5 and the on-host half of P6** — the Unity binding, the UGUI shell,
  scene boot, 60 FPS, live tactical input through a screen, and the FR-PO-052-class render-loop perf
  capture. All need the pinned host; the host block itself cleared July 19, 2026, so the gap is
  unwritten code, not access.
  **ESCALATED August 3, 2026 — owner reversed roadmap B6: the product ships this Unity client, not the
  web-hosted viewer, so P4 is now the critical path rather than a later native target.** Two standing
  rules, recorded in `interactive-unity-client-design.md` §12 and `path-to-playable-roadmap.md` §7/C2:
  (1) **keep logic out of `MonoBehaviour`s** — the CI gate cannot compile `match-client-unity` and never
  will, so every decision lives in gate-compiled `match-client-core`/`ui-framework` and the Unity types
  only assign transforms and forward input; extending the Unity shim to fake `MonoBehaviour`/`GameObject`
  is **explicitly refused**, since a lifecycle-free stand-in lets a dead render loop report green
  (ERR-030-014's failure mode one layer up). (2) Budget a cert-host run **per P4/P5 landing**, not one at
  the end. Note `PM-1`'s three screen-facing exit criteria are open again — they were demonstrated on a
  surface that is no longer the product; its determinism criterion is met head-lessly and stays met
- Approval tags created locally, not yet pushed
- Assembly layer taxonomy (Spec #20 §3.5.2) places 19 of 33 assemblies — ERR-020-002 proposal filed, awaiting owner sign-off (the two August 5 additions, `training-system` and `injuries-medical`, are unplaced like the other 12)
