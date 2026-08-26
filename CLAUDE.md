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

**Implementation:** `src/` holds **34 production assemblies**. Every Stage-0 spec is implemented except **#9 Fixed64** (deferred to Stage 5+ by design) and **#20 Code Standards** (a style guide, not a coded subsystem). A `MatchEngine` composition root wires the subsystems into the deterministic-sim 7-phase tick pipeline, and **a production match now plays** — the possession bootstrap (§5.Z Phase H, July 26, 2026) closed ERR-030-014, under which every match had been a 90-minute 0–0 deadlock with the ball never in motion. **Match Analytics #37 T0 landed July 27, 2026** (`src/match-analytics/` — value types + the pure `XgLocationModel`; no engine wiring yet), giving it a `src/` assembly for the first time.

**The live gap is now the project's dominant fact.** With the specification phase closed, **20 of the 53
APPROVED specs have no `src/` assembly at all** — the 10 listed below plus the ten approved on July 27.
(It was 22 until August 5, 2026, when **#29 Training and #41 Injuries & Medical** landed T0 assemblies together.)
The specification frontier runs a long way ahead of the implementation, which is a deliberate posture
(specify before coding), and it makes one habit dangerous: **"the spec is APPROVED" now says nothing
whatsoever about whether code exists.** It is true of ~42% of the registry.

**The 20 with no assembly:** #31 Transfers, #32 Scouting, #33 Personalities/Morale, #34 Staff, #40 Finances, #42 Youth, #43 Competition Structure, #44 Discipline, #45 Board, #49 Localization — plus the ten approved on July 27: #35, #36, #39, #46, #47, #48, #50, #51, #53, #54.

Sequencing for closing the gap is in `docs/tracking/path-to-playable-roadmap.md`, which is now the
project's live critical path. **Check `src/` before assuming a consumer is available** — the assembly map
above is the reliable index, not the spec registry. **And do not harden, extend or wire a spec
that has no assembly ahead of its own T0 landing** — findings against it are RECORDED and discharged at T0,
in the same commit as the code they govern (`path-to-playable-roadmap.md` **C6**). For *where any entity is
defined and whether it is implemented*, `docs/tracking/data-contract-index.md` is the pointer index.

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
├── src/                            ← Implementation (coding began May 19, 2026) — 34 production assemblies
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
| `player-progression` | **#28** | T0 + **T1/T2a (Aug 8, 2026)** — `ProgressionEngine` (KD-7 sole writer), `ProgressionSaveCodec` (magic-led, ERR-028-004), the FR-PG-021 batch `AdvanceDay`, and the `SquadFor` projection. **Owns the career roster** (KD-4): `Squad` is immutable and seed-rebuilt, so evolving attributes live here and nowhere else. Draw-free — the regen stream is NOT promoted, since the season boundary is deferred |
| `training-system` | **#29** | T0 (Aug 5, 2026) + T1 (Aug 6) — the day step, the growth-input read, the match-entry projection, the `InjuryRiskContribution` #41 reads, and now `TrainingSaveCodec` in #30's season frame. Draw-free by design (FR-TR-008). **T2 (Aug 6) wired it**: `PlayerCareerStates` in `season-save` constructs and owns the per-club set, and `SeasonLoop` drives the day step at slot 2. **Slot 1 is LIVE** since #28 T2a (Aug 8) — `ERR-029-006` fully resolved |
| `injuries-medical` | **#41** | T0 (Aug 5, 2026) + T1 (Aug 6) — the recovery-then-draw day step, the keyed occurrence draw (`DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`; **no** registered stream, ERR-041-002), and now `MedicalSaveCodec` in #30's season frame. **T2 (Aug 6) wired it**: `PlayerCareerStates` owns the per-club set and `SeasonLoop` drives the day step at slot 4, after #29's. The occurrence dial is **ARMED** (FR-MD-027) since the Aug 7 balance pass, measured in the football band |
| `season-save` | **#30** Season & Competition Loop | Also hosts the league bootstrap, the unified season save-file root, and — since #29/#41 T2 — `PlayerCareerStates`, the #30-side owner of both subsystems' per-club state. Since #28 T2a it also hosts `ProgressionSquads` — the `ISquadProvider` projection over #28's block, which lives here because `ISquadProvider` is a `match-engine` type #28 §4.1 forbids #28 to reference |
| `match-analytics` | **#37** Match Analytics & Statistics | T0 only — value types + `XgLocationModel`; no engine wiring, no aggregator. Presentation-layer derivation: **no sim assembly may reference it** (guarded mechanically) |
| `ui-framework` | **#38** UI / Client Framework | T0 substrate only; no screens, no UGUI binding |
| `performance-optimization`, `testing-strategy` | #18, #19 | Infrastructure only — no game-loop types |
| `project-constants` | — | Shared `[GT]` config; read-only by all |
| `match-engine` | — | **Composition root.** Not a numbered spec; governed by `docs/tracking/match-engine-design.md` |
| `match-viewer`, `match-client-core`, `match-client-unity` | — | Presentation tooling / client seams; not numbered specs |
| `match-client-web` | — | The browser match client (roadmap B6). Not a numbered spec; governed by `docs/tracking/browser-match-client-design.md`. The only assembly above BOTH `ui-framework` and `match-analytics`. **NOT the shipping UI** — B6 was reversed to full Unity on Aug 3, 2026; retained as the host-free reference harness. Keep green, do not extend |
| `client-app` | — | **The client composition layer** (roadmap B9c, Aug 7, 2026): the four screens' `ScreenId` catalogue + the `ClientScreenFlow` navigation graph, above `ui-framework` (its only reference). Not a numbered spec; governed by `docs/tracking/interactive-unity-client-design.md` (§5-P5a resolution / v0.17). Exists because FR-UI-010 forbids the framework hard-coding screens and `match-client-unity` is gate-invisible — the P5b binding navigates only through this assembly's five moves |

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
| `data-contract-index.md` | `docs/tracking/data-contract-index.md` | Entity → owning spec § → assembly **pointer** index (restates nothing; the specs remain the authority) |
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
`spec-promotion`, `dotnet-gate`, `steward`. Each was derived from measured repetition — most from
the last 200 commits, `steward` from a two-week session sweep that found PR CI triage and merge
conflicts costing real work with no repo-specific packaging — and carries the traps this project has
actually hit; `.claude/skills/README.md` records that evidence. The ones that need a review or gate
step invoke `adversarial-review` or `dotnet-gate` rather than restating them.

`orientation` is account-level, not in this repo.

---

## OPEN ISSUES

> Full entries live in **`docs/tracking/open-issues.md`**; closed ones in
> **`docs/tracking/open-issues-resolved.md`**. Read the owning file before acting on
> any item below — the titles here are an index, not the record.
>
> **When resolving an issue, move its entry to the resolved archive in the same
> commit.** Do not re-inline entries into this file.
>
> **Long landing narratives live in `docs/tracking/landing-history.md`** (created August 22, 2026).
> Six bullets below had grown into full landing accounts — 72,711 bytes, 70% of this whole file, one
> of them 38.6% on its own — while this section's own contract says the titles here are an index. That
> text was **moved verbatim**, never edited or summarised, and each bullet now carries a one-paragraph
> index entry pointing at its section there. Nothing was deleted and **no item's open/closed status
> changed**. Read `open-issues.md` for the record, `landing-history.md` for the reasoning, and treat a
> moved narrative as frozen at the date of its move — where it disagrees with the owning files, they
> win.
>
> **That discrepancy is CLOSED (August 22, 2026, owner call).** The move had surfaced an index of 17
> bullets against a record of 16 entries, the extra being **#29 Training / #41 Injuries & Medical**,
> which had no owning entry at all. It was verified resolved against the code — `SeasonLoop`
> slot 1 LIVE, the #41 occurrence dial ARMED, every chain `ERR-` id closed — and filed straight to
> `open-issues-resolved.md`, since there was no `open-issues.md` entry to move. **Index and record now
> both read 15.** Three stale `◑` status markers found in the same check (`ERR-029-006`,
> `ERR-041-010`, `ERR-041-001` each led with `◑` while their own cell text already recorded `✅`) were
> corrected in `spec-error-log.md` without editing their narratives.

**15 active** / 46 resolved. *August 22, 2026, latest — TWO ENTRIES ARCHIVED, and the index-vs-record discrepancy this file recorded earlier the same day is closed with them. **(1) `EnvironmentFingerprint.floatModelHash`:** both remainders on the replay-identity contract are now closed — `ERR-016-009` (`buildHash`) earlier the same day, and `ERR-016-010` (`SaveManager` writes `Fingerprint = null`) later the same day, the latter turning out to be a §3.9.2 normative-layout contradiction in four respects rather than one missing field. Archived whole and verbatim with a resolution annotation. **(2) `#29 Training / #41 Injuries & Medical`:** verified resolved against the code and filed straight to the archive, having never had an `open-issues.md` entry to move — which is exactly the discrepancy the `landing-history.md` split had recorded and left for an owner call. Counts re-derived by direct count after the change — `grep -c '^- \*\*'` returns **15** on `open-issues.md` and **46** on `open-issues-resolved.md`, and this index now carries **15** bullets, so the two agree for the first time in the record.* *August 22, 2026 — DE-DUPLICATION: the football-judgment proxy review had **two** active entries in `open-issues.md` and **two** bullets in this file, so the active count had been double-counting one issue. The second of each was the record of the concurrent `claude/football-judgment-proxy-review-pq12dz` branch (PR #305), whose competing `ERR-008-021` fix the August 7 merge did NOT keep. This file now carries one bullet; `open-issues.md` carries one entry; the PR #305 record was moved to `open-issues-resolved.md` **verbatim**, under a blockquote annotation explaining that it is a superseded parallel record rather than a resolved issue, with two of its facts (the genuine CI-run-404 verification, and the unlanded AR-1 H-1 single-goalkeeper-candidate selection) carried forward into both survivors. Precedent: this file's own August 2, 2026 archiving of "a duplicated pair". Counts re-derived by direct count after the change — `grep -c '^- \*\*'` returns **16** on `open-issues.md` and **44** on `open-issues-resolved.md`.* *August 12, 2026 — the A4a calibration entry added at the head, taking the active count to 17 (re-derived by direct count of `open-issues.md`: `grep -c '^- \*\*'` returns 17).* *Re-filed August 2, 2026 — eight entries archived (six closed-but-unmoved, plus a duplicated pair); three titles amended to lead with what remains open rather than what has landed. August 4, 2026 — the wiring-backlog entry added at the head; later same day, the football-judgment remediation entry added above it. August 7, 2026 — a two-red-scenario-locks entry was filed at the head from the B9c gate run and RESOLVED the same evening at the `80d97c8` merge: main's `b162a00` had already diagnosed both failures per-seed and rebaselined both bands by owner call, 45 minutes before the entry was filed. It lived ~1 hour; see the archive. August 8, 2026 — the tree-wide header/version hygiene backlog entry added at the head from `tools/recurring-defect-lint.py`'s first sweep, taking the active count to 15; RESOLVED later the same day by a dedicated hygiene pass (owner chose the hygiene pass over a CI ratchet) — all 275 ERRORs fixed, `python3 tools/recurring-defect-lint.py --repo .` now reports 0 ERROR tree-wide — taking the active count back to 14. **CORRECTION, August 10, 2026: the "14 active" claim above was already wrong before this branch existed** — a direct count of `docs/tracking/open-issues.md` (`grep -c '^- \*\*'`) returns **15**, not 14, against the unchanged 43 resolved; no entry in the file accounts for the sixteenth-vs-fifteenth discrepancy, so the prior figure was never re-derived after some earlier edit. This branch then added one further active entry (the `#28 Player Progression` bullet's owning record, filed to `open-issues.md` to match this file's own index bullet — see above), taking the true count to **16 active / 43 resolved**, re-derived by direct count of both files on 2026-08-10.*

- **A4a round-resolution calibration RAN (August 12, 2026) — the fit is done; the verdict is two-part and both halves are owner decisions, not runs to redo.** 198 real 90-minute `MatchEngine` matches captured over ~1.4 h; the three `QuickSim` `[GT]`s fitted by least squares and their "provisional, not fitted" warning retired; KD-8 **Step 0 PASSED**. **Mean agreement PASS** — after **`ERR-030-033` RESOLVED**: the ±0.25 per-bucket bar sat *below the corpus's own noise floor* (15 of 22 bucket-sides had a standard error larger than the whole bar), so KD-8 now screens on `max(0.25, 2·se)` with ±0.25 kept as a floor; measured worst |z| = 2.06, pooled χ² = 16.0 on 19 dof. **Distribution shape FAIL — `ERR-030-034`, the surviving half of roadmap risk row 1:** the engine is over-dispersed at **z = +5.40** and produces far fewer draws than Poisson (19.2% vs 26.8%), and the two findings are **substantially independent** — the draw deficit's mechanism is NOT established and is not expressible by any mixed-Poisson consistent with the measured home/away correlation. Successor **KD-7a (NB2)** is pre-decided, specified and **deliberately NOT adopted**, on four measured gates. The corpus is committed, so a re-fit against a new family costs seconds. **Also corrected in passing:** the "3.09 goals/match" figure was a *calibration-grid* mean read as if it were a league — the football-comparable numbers are **2.70 ± 0.13** (balanced fixtures) and **2.93 ± 0.15** league-weighted, so the engine has **not** overshot football's goal rate and no `[GT]` was moved. *Full narrative: `docs/tracking/landing-history.md` §1. Owning record: `open-issues.md`.*

- **Football-judgment proxy review — 29 itemized findings open across **19** specs (32 until batch 1 landed Aug 22, 2026); **21 workable today**; the §6 doctrine governs every fix.** The review (`docs/tracking/football-judgment-proxy-review.md`) swept all 53 APPROVED specs for continuous football judgments collapsed into thresholds or bare geometry. **Landed:** `ERR-008-020` (template), `ERR-008-019` (long-shot cliff), and the `ERR-008-021`/`ERR-008-022`/`ERR-008-023` shot-lane chain — plus **batch 1 of the workable queue (Aug 22, 2026)**, spec + code together per §6.3: `ERR-028-020` (age-band growth ramp), `ERR-028-021` (per-player retirement day), `ERR-041-020` (the missing age term in #41's risk assembly). An adversarial-review round over that landing then found and fixed three more Highs (`ERR-028-022`, `ERR-028-023`, `ERR-041-021`). **A second review round (Aug 24) ran only partially** — two of its four lenses (arithmetic, mutation) died on a session limit before producing a finding, so the loop is not yet converged; what it did find corrected two stale claims left standing inside `spec-error-log.md` itself. **Next per §6.3.1:** batch 2 (keeper), pending a fresh post-batch measurement. Also carried: PR #305's unlanded AR-1 H-1 (single-goalkeeper-candidate selection for the P3 exemption) is recorded as real, follow-up-worthy work. *Full narrative: `docs/tracking/landing-history.md` §2 (frozen at the pre-batch-1 snapshot). Owning record: `open-issues.md`.*

- **#28 Player Progression — T1 + T2a LANDED August 8, 2026 (roadmap D1, part one).** `ERR-029-006` CLOSED, #30's KD-2 slot 1 is LIVE, and **the career roster has moved off the world seed**. The load-bearing decision is **KD-4**: `Squad` is immutable and `League` is seed-rebuilt at load, so a player's evolving attributes had nowhere to live and nowhere to persist — #28's block is now the serialized roster and `ProgressionSquads` the single provider every consumer reads through, which retires roadmap A3's property that a career is reopenable from the world seed alone. `SEASON_SAVE_FORMAT_VERSION` **4 → 5**; no draw site, so no stream registration and no digest/schema question. Four ERRs filed and resolved in the same commit (**-003** new-game PA has no derivation and a 0 default makes the daily step a silent no-op; **-004** the save block identified itself by RNG domain tag — the ERR-029-005 MUST arriving in a third spec; **-005** no per-day cursor against a fixture day that runs its slots twice, i.e. a silent ~11% growth-rate error; **ERR-030-030** five stale null-seam sites). **REMAINDER:** the season boundary (retiree removal + 1:1 regen) and with it the `player-progression.regen` stream, plus T3's deep curve. *Full narrative: `docs/tracking/landing-history.md` §4. Owning record: `open-issues.md`.*

- **Match-engine wiring backlog — W1, C1 and W2 are BUILT-BUT-DISABLED; 8 built subsystems (W3–W10) still have no production caller, and the KD-W1 `[GT]` freeze follows.** **W1** (August 4) gave the keeper's rush its first production caller but has never been executed. **C1** (August 8, `ERR-012-011`) fixed phase classification to read from TEAM possession — measured final-third `InPoss` 7.5% → 40.8% — but **its own value claim was RETRACTED**: the pre-implementation council refuted the rationale before any code, and the football got slightly *worse* (deepest composed slot 23.0 → 25.7 m from goal). **W2** (August 12, `ERR-014-006`) makes a player in control dispossessed for the first time in this engine — and **ships with `TackleContactRadiusM` at 0** pending W6, because arming it collapses `sim_match_engine_inposs_gate` to 0.501 via a stall whose root cause is not isolated. So *"no player has ever made a tackle"* is retired as a statement about the code, not yet about a shipped match. `sim_match_engine_close_chance` is **owner-held RED by decision** (August 11) — an 18-seed paired bisect prices C1's cost at −0.189 ± 0.038 while the shot-lane chain it had been blamed on moves it −0.027 ± 0.039, so it is a mechanism failure, not a bound-tuning question. **Next in sequence: W4** (keeper perception), then **W12**. *Full narrative: `docs/tracking/landing-history.md` §5. Owning records: `match-engine-wiring-backlog.md` v1.9, `open-issues.md`.*

- Conversion at contact — the CLAIM defect FIXED (ERR-011-008, §5.Z.23); REMAINDER: the `pointQuality` lottery is blocked on a design decision (measured: the geometry-aware form collapses catches to zero and no `[GT]` in range recovers them) — **PARKED August 4, 2026: the keeper rush trigger (wiring backlog W1) changes the contact geometry the decision turns on** — and parry placement is unfixed but currently costless
- Close-chance creation — the DRIBBLE-direction defect FIXED (ERR-008-018, §5.Z.24: the average final-third dribble pointed AWAY from goal); REMAINDER: the funnel itself did not move — the ball still enters the box on 5% of final-third episodes, and the bound is now localized to #8 §3.1.3 generating PASS candidates only at a teammate's CURRENT POSITION, so the tree cannot pass to a place. **Amended Aug 17, 2026:** the Acceptance-3 regression blamed on the -021/-022/-023 shot-lane chain is REFUTED by bisect (chain effect −0.027 ± 0.039, t = −0.70; the −0.119 that drove the rebaseline is the 4.6th percentile of its own two-seed estimator) — the live `sim_match_engine_close_chance` failure is C1 / `ERR-012-011`, and KD-W1 has nothing to pull back on the shot lane (`close-chance-creation-design.md` §11 / v2.3)
- Injury / aging research alignment — design supplement OPENED, AR-converged, awaiting owner sign-off
- Foul/card heuristic issues ~7 red cards per 9 minutes of played football — the most visible unrealism in a match now that matches actually play
- Goalkeeper Mechanics (#11) / Heading Mechanics (#10) engine integration — Phase 1 (opt-in) LANDED; the GK/Heading attribute projections now have a live consumer — REMAINDER: `CollisionConsumer` AGENT_BALL duel fan-out, DT-emitted HEADER (ordinal 8 → composure-noise rebaseline), attribute-modulated save commit
- Advanced positional behaviors + game-model/AI-manager tactics — design supplements OPENED (candidate specs #23–#26) — all four promoted to specs and landed; REMAINDER: #26 §9.2 own-`[GT]` balance review
- Living World (#22) season/world loop — slices 1–7 LANDED (incl. the KD-10 season composition root + the InteractionTextGenerator wired into it + deep-memory auto-cite + the opt-in arc-trigger evaluator / `world.arcs` sub-stream); upstream-gated services open
- UI / Client Framework (#38) — T0 substrate LANDED; Wave-7 screens + the UGUI binding remain open.
  **The governance question the August 7, 2026 P5a landing surfaced — where the four screens'
  `ScreenId` catalogue and navigation graph live — was RESOLVED by owner decision later the same day
  and LANDED as `src/client-app/`** (`TacticalDirector.ClientApp`, references only `ui-framework`;
  roadmap B9c, `interactive-unity-client-design.md` v0.17). The `match-engine` composition-root
  precedent carried it: FR-UI-010 makes a concrete screen set composition, not framework, and
  composition lives above what it wires; `match-client-unity` was rejected as gate-invisible (§12
  rule 1). Roadmap §6 item 2's C3 management screens inherit the same home by precedent. What
  remains open of #38 is unchanged: the Wave-7 screens (each gated on its data spec having `src/`)
  and the UGUI binding
- **Presentation layer — the minimal match viewer landed; the interactive Unity client's host-free phases are ALL complete, and what is left is host verification rather than unwritten code.** Complete head-lessly: **P0/P2** (July 24), **P1/P3** (July 27), **the head-less half of P6** (August 3 — `MatchSession.TickOnce/CaptureSave/RestoreFrom` + `TickStampedCommandReplay`, meeting PM-1's determinism exit criterion), **P4a** the render model (August 3) and **P5a** the shell's decisions (August 7). P4 and P5 were each split on the standing **keep-logic-out-of-`MonoBehaviour`s** rule: every decision lives in gate-compiled `match-client-core`/`ui-framework`, the Unity types only assign transforms and forward input, and extending the CI shim to fake `MonoBehaviour`/`GameObject` is **explicitly refused** (a lifecycle-free stand-in lets a dead render loop report green — ERR-030-014 one layer up). **P4b's Unity binding landed as code August 15, 2026** (`src/match-client-unity/MatchClientBehaviour.cs`), but that assembly is excluded from the CI gate by design and **has never compiled or run**. **REMAINDER: P5b's UGUI shell and the on-host half of P4b/P6** — scene boot, 60 FPS, live tactical input through a screen, and the FR-PO-052-class render-loop perf capture. All need the pinned host; the host block itself cleared July 19, 2026. **ESCALATED August 3, 2026 — the owner reversed roadmap B6: the product ships this Unity client, not the web-hosted viewer**, so P4/P5 are the critical path and a cert-host run is budgeted *per landing*, not once at the end. P5b's one open layering decision was resolved August 7 by landing `src/client-app/`. *Full narrative: `docs/tracking/landing-history.md` §15. Owning record: `open-issues.md`.*

- Approval tags created locally, not yet pushed
- Assembly layer taxonomy (Spec #20 §3.5.2) places 19 of 34 assemblies — ERR-020-002 proposal filed, awaiting owner sign-off (the two August 5 additions, `training-system` and `injuries-medical`, and the August 7 addition, `client-app`, are unplaced like the other 12)
