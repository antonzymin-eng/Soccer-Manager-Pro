# Positioning AI Specification #12 — Detailed Outline

**Created:** May 15, 2026
**Last Updated:** May 15, 2026
**Version:** 1.0
**Status:** DRAFT — addresses all 13 findings (5 H / 5 M / 3 L) from the
May 6, 2026 adversarial review at the bottom of `outline.md`.
Ready for section-file authoring.
**Companion documents:** `outline.md` (high-level + adversarial review).
**Unblocks:** Phase C Priority 4 specs #13 / #14 / #15 — each linearly
depends on Positioning AI publishing role anchors, transition rules,
and the spacing model. After this outline lands, downstream specs can
begin their own outline phase against named (if not yet APPROVED)
boundaries.

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan that
resolves every adversarial-review finding. For each subsection: the FRs
it will publish, the boundary declarations it will hold, and the
cross-references it will emit. Detailed enough that `section-1.md` …
`section-9-approval-checklist.md` and `appendices.md` can be drafted
mechanically from this document.

This document does **not** publish FR text in normative form — that
text lands in `section-2.md`. The detailed outline records every FR's
intended rule, conformance level (`MUST`/`SHOULD`/`MAY`), and source
spec/section so the FR table can be authored without re-deriving rules.

---

## METADATA HEADER (resolves H-1)

The header below is the template every section file copies verbatim
(adapted from Shot Mechanics #6 / Decision Tree #8).

| Field | Value |
|-------|-------|
| Spec # | 12 |
| Title | Positioning AI |
| Folder | `docs/specs/positioning-ai/` |
| Priority | 3 (Stage 0 — Phase B keystone) |
| Status | NOT STARTED (this outline phase) |
| Owner | Lead developer (gameplay-AI domain) |
| Approved Dependencies (upstream) | #1 Ball Physics; #2 Agent Movement; #7 Perception System; #8 Decision Tree; #16 Deterministic Simulation; #17 Event System; #20 Code Standards |
| Pending Dependencies (none — outline phase) | — |
| Downstream Consumers | #13 Pressing AI; #14 Defensive AI; #15 Attacking AI (Phase C linear chain) |
| Stage Binding | Stage 0 (`float` arithmetic, state-snapshot determinism). Fixed64 binding deferred to Stage 5+ per #9 §8.1. |
| Estimated Effort | 5–7 working days (outline + 9 section files + appendices + adversarial review pass) |

---

## CROSS-CUTTING DESIGN DECISIONS

These decisions are referenced throughout the outline. They are stated
once here and cited below by KD-number, never restated.

- **KD-1 — Cite-not-redefine.** Spec #12 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec. It
  cites and binds. In particular: corner-origin coordinate system
  (Ball Physics #1 §1.2), fatigue convention `0=rested,1=fatigued`
  (CLAUDE.md), tick-rate split (10 Hz tactical / 60 Hz physics),
  EntityId no-reuse binding (#2 §2.5 XC-002-001 + #8 §1.7.3 XC-008-001),
  perception output schema (#7 §3.7–§3.10), decision-tree output
  schema (#8 §3.2 final-action selection). Resolves M-7 (coordinate
  system), L-11 (fatigue convention), M-9 (tick-rate split).

- **KD-2 — 10 Hz tactical positioning targets; 60 Hz steering owned
  by #2 Agent Movement.** Positioning AI runs on the 10 Hz tactical
  loop. Output is a `PositionTarget` (3D point + arrival tolerance +
  facing hint) per controlled agent, written once per tick. Agent
  Movement #2 §3.x consumes the target on each 60 Hz physics frame
  and produces the actual `Vector3` steering force. Spec #12 does NOT
  emit per-frame steering — that would duplicate #2's authority and
  re-create the ERR-001 / ERR-004 phantom-interface pattern. Resolves
  M-9 (tick-rate split).

- **KD-3 — Boundary with Decision Tree #8.** #8 selects the
  *final action* (pass / shoot / dribble / hold) for the on-ball
  agent and broadcasts the intent (#8 §3.2). #12 selects the
  *positional target* for every off-ball agent (the other 21 agents)
  AND for the on-ball agent's no-ball reference frame (where they
  *would* be if they released the ball). #12 NEVER overrides a #8
  action decision; #8 NEVER writes off-ball positional targets. The
  interface is one-directional: #12 reads #8's on-ball action
  intent (via #17 Event channel `ACTION_INTENT`) to bias support
  shape (e.g., shrink shape toward a long-pass receiver). #8 does
  not consume #12 output at Stage 0. Resolves H-3.

- **KD-4 — Boundary with Pressing AI #13.** #12 publishes the
  *baseline defensive shape* (lines, lateral compactness, vertical
  compactness) given phase = out-of-possession. #13 publishes the
  *press trigger* (when to break baseline shape to engage the
  ball-carrier) and the *press-shape modifier* (how the lines
  collapse around the trigger). #12 → #13 interface: #12 exposes
  read-only `BaselineDefensiveShape` per agent (anchor, lane,
  line-membership). #13 applies a `PressOverride` displacement on
  top of the baseline. The compositor rule (§3.7 below) defines
  precedence: press override wins on the triggering agent, baseline
  wins on the rest, subject to shape-integrity guards. Resolves H-3.

- **KD-5 — Boundary with Defensive AI #14.** #14 handles
  *micro-defensive intent* — mark assignment, cover/track decisions,
  drop vs hold-line — for the on-ball-side defensive triad. #12
  publishes the *macro shape*; #14 picks individual responsibilities
  within the shape. Interface: #12 exposes `LineMembership` and
  `LaneAssignment`; #14 emits `MarkTargetEntityId` per agent. #12
  consumes nothing from #14 at Stage 0. Resolves H-3.

- **KD-6 — Boundary with Attacking AI #15.** #15 handles
  *off-ball runs* (third-man, overlap, blindside, decoy). #15 emits a
  `RunIntent` that *displaces* the #12 baseline anchor for the
  running agent for the duration of the run. Compositor precedence:
  active `RunIntent` wins on the running agent; baseline wins on
  the rest; shape-integrity guards may abort a run if it would
  break a hard spacing constraint (§3.6 below). Resolves H-3.

- **KD-7 — Formation data ownership.** Formation archetypes
  (4-3-3, 4-2-3-1, 4-4-2, 3-5-2, 3-4-3, 5-3-2 — the six Stage 0
  shipped archetypes) are stored in a **tactical-instruction
  config asset** owned by Spec #12 §6 (constants catalogue
  `FormationCatalogue.cs`) at Stage 0. At Stage 1+, coach UI and
  save-game data layer above this; the data schema is forward-
  compatible. The constant catalogue tags each cell `[GT]` (lateral
  / longitudinal offset percentages) or `[DERIVED]` (line membership
  inferred from offset clustering). NO magic numbers in formula
  code. Resolves H-5, M-10.

- **KD-8 — Hysteresis pattern reuse (Agent Movement #2 §3.1
  binding).** Anchor selection, line membership, and lane
  occupation all use the dwell-time + dead-zone hysteresis pattern
  established by #2 §3.1 for steering-mode transitions. Spec #12
  does NOT define a new hysteresis algorithm — it parameterises
  the #2 pattern with positioning-specific dwell windows and
  dead-zone radii (`[GT]`-tagged in §6). Resolves M-8 and protects
  against ERR-001 / ERR-004.

- **KD-9 — Determinism binding (#16).** All positioning state is
  *authoritative simulation state* per #16 §3.2: it influences
  future tick outcomes and MUST appear in the per-tick digest.
  Specifically: (a) agent iteration order is the canonical EntityId
  sort from #16 §3.2.5; (b) any stochastic micro-jitter for
  tie-breaking uses `DeterministicRngService` with domain tag
  `DOMAIN_TAG_POSITIONING_AI` (new value to be allocated in #16
  §3.4 — tracked below as ERR-012-001); (c) `PositionTarget`
  contributes to the canonical-state digest at the per-spec digest
  scope #16 §6.2 defines for tactical-AI outputs. Resolves M-6.

- **KD-10 — Event System binding (#17).** Spec #12 *consumes*
  three #17 channels (`PHASE_CHANGE`, `ACTION_INTENT`,
  `POSSESSION_TURNOVER`) and *produces* two
  (`SHAPE_TRANSITION`, `LINE_BREACH_ALERT`). Channel schemas are
  declared in §4.4 / §4.5 of this spec and registered in the #17
  channel registry. No event production / consumption happens
  outside this declared set. Resolves L-12.

- **KD-11 — Stage 0 scope discipline.** Authoring tools, coach
  UI, save-game persistence, ML-tuned shape parameters, and
  set-piece positioning are **explicitly out of Stage 0 scope**.
  These belong in §7 as Stage 1+ / Stage 2+ deferrals. Resolves
  H-4.

- **KD-12 — Constant-tag discipline.** Every constant in every
  section MUST carry exactly one of `[GT]`, `[EST]`, `[FIXED]`,
  `[DERIVED]`, `[CROSS]`. The constant catalogue (`§6.1`) is the
  single source of truth; no magic numbers in formula code per
  CLAUDE.md. Resolves M-10.

---

## BOUNDARY MATRIX (resolves H-3)

A single table that downstream specs cite read-only. Lives at
`section-1.md` §1.6 (Interface Boundaries).

| Boundary | Spec #12 owns | Other spec owns | Coupling direction | Coupling mechanism |
|---|---|---|---|---|
| #8 Decision Tree | Off-ball positional targets | On-ball action selection | #12 reads #8 | `ACTION_INTENT` event (read-only) |
| #2 Agent Movement | 10 Hz `PositionTarget` output | 60 Hz steering toward target | #2 reads #12 | Direct struct read at 60 Hz frame start |
| #7 Perception | (none) | Filtered world model | #12 reads #7 | Snapshot read at tick start |
| #13 Pressing | Baseline out-of-possession shape | Press trigger + displacement | Compositor at §3.7 | `PressOverride` write-into-pipeline |
| #14 Defensive | Macro shape (lines, lanes) | Micro mark/cover assignment | #14 reads #12 | Read-only `LineMembership` / `LaneAssignment` |
| #15 Attacking | Baseline support shape | Off-ball run displacements | Compositor at §3.7 | `RunIntent` write-into-pipeline |
| #16 Determinism | `PositionTarget` digest scope | Digest format + iteration rule | #12 conforms to #16 | Iteration-order rule + domain-tagged RNG |
| #17 Event System | 3 channels consumed, 2 produced | Channel registry + record format | #12 conforms to #17 | Channel-registry entries (§4.4/§4.5) |

---

## SECTION 1 — INTRODUCTION, SCOPE, DEPENDENCIES, KEY DECISIONS

### 1.1 Purpose
- One-paragraph problem statement: who consumes Positioning AI and
  why off-ball positioning is the keystone for Phase C tactical AI.
- Cite CLAUDE.md "Project Identity" — Football Manager parity.

### 1.2 Scope (in / out)
- **In:** anchor selection, role-to-anchor mapping, ball-relative
  offsets, phase transitions, shape compactness, lane occupation,
  line membership, context modifiers (score, fatigue, tactical
  intensity), compositor with #13/#14/#15.
- **Out (per KD-11):** authoring tools, coach UI, set-piece
  positioning, throw-in / corner / free-kick shapes, ML-tuned
  parameters, save-game persistence, multiplayer-specific
  determinism (Stage 5+).

### 1.3 Dependencies
- Upstream APPROVED: #1, #2, #7, #8, #16, #17, #20.
- Downstream consumers: #13, #14, #15 (all NOT STARTED — outline
  publishes named boundaries, no FRs cross-cited yet).

### 1.4 Key Domain Concepts
- **Anchor:** a 3D point on the pitch a role gravitates toward when
  ball position, phase, and context are at "neutral" values.
- **Ball-relative offset:** vector applied to anchor as a function
  of `Ball.position - PitchOrigin` and current phase.
- **Lane:** longitudinal column (5 lanes: LW / LH / C / RH / RW)
  used as a discrete occupation grid for spacing checks.
- **Line:** lateral row (Defense / Midfield / Attack) used as a
  discrete occupation grid for vertical compactness.
- **Phase:** in-possession / out-of-possession / transition-to-
  attack / transition-to-defense (the four #17 `PHASE_CHANGE`
  values).
- **Compactness:** team-level scalar of how tightly the 10 outfield
  agents cluster — lateral and vertical components.

### 1.5 Key Design Decisions (cross-reference to KD-1..KD-12 above)

### 1.6 Interface Boundaries (the Boundary Matrix above)

### 1.7 Coordinate & Convention Bindings
- Corner origin per #1 §1.2 (resolves M-7).
- Fatigue `0=rested,1=fatigued` per CLAUDE.md (resolves L-11).
- 10 Hz tactical / 60 Hz physics per CLAUDE.md (resolves M-9).
- EntityId no-reuse per #2 §2.5 / #8 §1.7.3.

### 1.8 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES, FAILURE MODES

### 2.1 Functional Requirements Table

Provisional FR enumeration (text lands in `section-2.md`). Estimate
~45–55 FRs, prefixed `FR-PA-NNN`. Conformance levels noted.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-PA-001 | Tick rate is 10 Hz | MUST | CLAUDE.md / KD-1 |
| FR-PA-002 | Output is one `PositionTarget` per controlled agent per tick | MUST | KD-2 |
| FR-PA-003 | Iteration order is EntityId-sorted | MUST | #16 §3.2.5 / KD-9 |
| FR-PA-004 | `PositionTarget` contributes to per-tick digest | MUST | #16 §6.2 / KD-9 |
| FR-PA-005 | RNG calls use `DOMAIN_TAG_POSITIONING_AI` | MUST | #16 §3.4 / KD-9 |
| FR-PA-006 | No allocation on hot path | MUST | CLAUDE.md / #18 §3.7 |
| FR-PA-007 | Six formation archetypes shipped at Stage 0 | MUST | KD-7 |
| FR-PA-008 | Anchor selection uses dwell-time hysteresis | MUST | #2 §3.1 / KD-8 |
| FR-PA-009 | Line membership uses dead-zone hysteresis | MUST | #2 §3.1 / KD-8 |
| FR-PA-010 | Lane occupation uses dead-zone hysteresis | MUST | #2 §3.1 / KD-8 |
| FR-PA-011 | Phase transitions emit `SHAPE_TRANSITION` event | MUST | KD-10 |
| FR-PA-012 | Hard spacing constraint violations emit `LINE_BREACH_ALERT` | MUST | KD-10 |
| FR-PA-013 | Press override compositor precedence (§3.7) | MUST | KD-4 |
| FR-PA-014 | Run intent compositor precedence (§3.7) | MUST | KD-6 |
| FR-PA-015 | Shape-integrity guard may abort a `RunIntent` | MAY | KD-6 |
| FR-PA-016 | Fatigue input convention `0=rested` | MUST | CLAUDE.md / KD-1 |
| FR-PA-017 | Score-modifier input range [-3, +3] (cap clamps) | MUST | KD-11 / §3.5 |
| FR-PA-018 | Tactical-intensity input range [0, 1] | MUST | §3.5 |
| FR-PA-019..045 | (Algorithm-specific FRs — populated during §3 drafting) | — | — |
| FR-PA-046 | Failure mode: missing perception snapshot → freeze last `PositionTarget` | MUST | §2.4 |
| FR-PA-047 | Failure mode: invalid formation index → fall back to 4-4-2 | MUST | §2.4 |
| FR-PA-048 | Failure mode: NaN in ball-relative offset → clamp to anchor | MUST | §2.4 |

### 2.2 Data Structures

| Struct | Purpose | Owner |
|---|---|---|
| `PositionTarget` | Per-agent positional output (Vector3 + tolerance + facing) | #12 |
| `FormationArchetype` | 11-role offset table (lateral % / longitudinal % per role) | #12 §6.1 |
| `BaselineDefensiveShape` | Read-only struct exposed to #13 / #14 | #12 |
| `BaselineSupportShape` | Read-only struct exposed to #15 | #12 |
| `PressOverride` | Displacement struct written by #13 (read here) | #13 (#12 declares schema) |
| `RunIntent` | Displacement struct written by #15 (read here) | #15 (#12 declares schema) |
| `LineMembership` | Enum {Defense, Midfield, Attack, Floating} per agent | #12 |
| `LaneAssignment` | Enum {LW, LH, C, RH, RW} per agent | #12 |
| `ContextModifierInputs` | Score, fatigue (team mean), tactical intensity | #12 (consumed) |

### 2.3 Inputs (read-only at tick start)
- Perception snapshot (#7 §3.7).
- Ball state (#1 §3.x via perception).
- `ACTION_INTENT` from #8 (latest pending intent only).
- Phase enum from #17 `PHASE_CHANGE` (latest value).
- Context modifier inputs (score / fatigue mean / tactical-intensity).

### 2.4 Failure Modes (resolves implicit §2 template requirement)
- **F1 — Missing perception snapshot:** freeze previous tick's
  `PositionTarget` for all agents; emit `LINE_BREACH_ALERT` with
  reason `PERCEPTION_STALE`. Recovers on next valid snapshot.
- **F2 — Invalid formation index:** fall back to 4-4-2 archetype;
  emit alert with `FORMATION_INVALID`. Stage 1+ surfacing in
  coach UI.
- **F3 — NaN in computed offset:** clamp to raw anchor (zero
  offset). Always recoverable because the anchor is itself bounded.
- **F4 — Phase event arrives mid-tick:** defer application to next
  tick boundary (deterministic ordering).
- **F5 — Compositor produces target outside pitch bounds:** clamp
  to nearest in-bounds point with a 0.5m margin from touchlines.

### 2.5 Version History

---

## SECTION 3 — CORE FORMULAS AND ALGORITHMS

### 3.1 Anchor Computation
- Inputs: role, formation archetype, ball position (corner-origin).
- Formula: `anchor = pitchSize * formationOffset[role]` where
  `formationOffset` is the `[GT]`-tagged 11×2 table.
- Worked example: 4-3-3, left-winger, neutral phase → (78.0, 6.8).

### 3.2 Ball-Relative Offset
- Phase-dependent offset function `f(ball.x, ball.y, phase, role)`.
- Piecewise linear with three break-points per axis (`[GT]`-tagged
  in §6 — pull-toward-ball factors per role per phase).
- Worked example: when ball is in own defensive third and team is
  out-of-possession, attacking-midfielder anchor pulls back 8m.

### 3.3 Line Membership
- Definition: cluster of agents within a longitudinal window.
- Algorithm: stable k=3 longitudinal partition with dead-zone
  hysteresis (KD-8). Goalkeeper excluded.
- Dead-zone radius `LINE_HYSTERESIS_M` = 3.0m `[GT]`.

### 3.4 Lane Occupation
- Definition: count of agents per lateral lane (5 bins).
- Soft constraint: target ≤2 per lane in midfield third.
- Hard constraint: ≤3 per lane anywhere (violation emits
  `LINE_BREACH_ALERT`).
- Dead-zone hysteresis on lane boundaries (KD-8), width
  `LANE_HYSTERESIS_M` = 2.0m `[GT]`.

### 3.5 Context Modifiers
- Score effect: chase-goal multiplier on attacking compactness
  scales linearly with `(opponent_score - own_score)` clamped to
  [-3, +3] (`[GT]` table per role).
- Fatigue effect: team-mean fatigue ∈ [0,1] reduces lateral
  compactness by `up to FATIGUE_LATERAL_RELAX_M = 4.0m` `[GT]`.
- Tactical-intensity effect: input ∈ [0,1] scales vertical
  compactness target. (Reminder: `0 = rested`, `1 = fatigued`.)

### 3.6 Spacing Constraints (hard + soft)
- Hard: pairwise minimum separation `MIN_AGENT_SEPARATION_M = 1.5m`
  `[FIXED]` (cited from #3 collision radius).
- Soft: same-line same-lane co-occupation penalty (cost function).
- Resolution: shift the *later-EntityId* agent first (deterministic
  tie-break per KD-9).

### 3.7 Compositor (#13 / #15 overrides on top of baseline)
- Step 1: compute baseline `PositionTarget` for every agent.
- Step 2: apply `PressOverride` (from #13) if present and not
  shape-integrity-violating.
- Step 3: apply `RunIntent` (from #15) if present and not
  shape-integrity-violating.
- Step 4: enforce hard spacing (§3.6).
- Step 5: clamp to pitch bounds.
- Deterministic ordering: EntityId-sorted iteration in each step
  (KD-9).

### 3.8 Hysteresis (binding to #2 §3.1)
- Cite-not-redefine. Parameters:
  - `ANCHOR_DWELL_TICKS` = 5 ticks (500ms) `[GT]`.
  - `LINE_HYSTERESIS_M` = 3.0m `[GT]` (cross-ref §3.3).
  - `LANE_HYSTERESIS_M` = 2.0m `[GT]` (cross-ref §3.4).

### 3.9 Determinism (binding to #16)
- Iteration order, RNG domain tag, digest scope (per KD-9).
- New domain tag allocation: `DOMAIN_TAG_POSITIONING_AI` =
  `0x16` (next after #17's `DOMAIN_TAG_EVENT_LEDGER` = `0x15`).
  Tracked as **ERR-012-001** in `spec-error-log.md` for atomic
  patch into #16 §3.4 — `[CROSS-PENDING]` until #16 §3.4 patch
  lands, at which point promoted to `[CROSS]`.

### 3.10 Constants Catalogue (forward reference to §6.1)

### 3.11 Pseudocode (per-tick main loop)

### 3.12 Version History

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS

### 4.1 Architecture Overview
- Single subsystem `PositioningAI` living on the 10 Hz tactical
  scheduler.
- Pure-function design: inputs in → `PositionTarget[]` out. No
  hidden state beyond hysteresis dwell counters.

### 4.2 File Structure
```
src/PositioningAI/
├── PositioningAITick.cs           (entry point, 10 Hz)
├── AnchorCalculator.cs            (§3.1 + §3.2)
├── ShapeAnalyzer.cs               (§3.3 + §3.4)
├── ContextModifier.cs             (§3.5)
├── SpacingResolver.cs             (§3.6)
├── PositioningCompositor.cs       (§3.7)
├── HysteresisState.cs             (dwell counters)
├── FormationCatalogue.cs          (constant catalogue)
└── PositioningConstants.cs        (all [GT]/[FIXED]/[DERIVED] values)
```

### 4.3 Internal Module Contracts (struct signatures)

### 4.4 Upstream Integration Contracts
- Perception snapshot read (#7 §3.7 schema).
- `ACTION_INTENT` event consume (#17 channel).
- `PHASE_CHANGE` event consume (#17 channel).
- `POSSESSION_TURNOVER` event consume (#17 channel).

### 4.5 Downstream Integration Contracts
- `PositionTarget[]` exposed to #2 Agent Movement (60 Hz steering).
- `BaselineDefensiveShape` / `BaselineSupportShape` exposed
  read-only to #13 / #14 / #15.
- `SHAPE_TRANSITION` event produced on phase boundaries.
- `LINE_BREACH_ALERT` event produced on hard-spacing violations.

### 4.6 Determinism & Safety Boundaries (binding to #16)
- Iteration order, RNG domain tag, digest scope per KD-9.
- "Authoritative state" enumeration per #16 §3.2 (positioning
  outputs only — hysteresis dwell counters ARE authoritative
  state and digested).

### 4.7 Cross-Specification Validation Checks
- At Stage 0+1 perf-gate activation (per #18 FR-PO-052), this
  spec's outputs are sampled into the trace pipeline.
- Test bindings to #19 testing strategy (referenced in §5).

### 4.8 Version History

---

## SECTION 5 — TEST PLAN

### 5.1 Test Counts (verifiable target — resolves L-13)

| Category | Target count | Source |
|---|---|---|
| Unit tests (anchor calculation, hysteresis, compositor) | ≥40 | §3.1–§3.8 surface area |
| Integration tests (full-team shape under phase transitions) | ≥12 | §3.7 compositor matrix |
| Determinism regression tests | ≥6 | #16 §5 |
| Performance tests | ≥3 | §6 budgets |
| Tactical-correctness scenarios | ≥8 | Appendix B archetype profiles |
| **Total** | **≥69** | — |

### 5.2 Unit Test List (representative — full enumeration in `section-5.md`)
- Anchor at neutral ball position matches formation table within
  ±0.01m.
- Ball-relative offset clamps at extreme ball positions.
- Line-membership hysteresis: agent oscillating at boundary stays
  in original line for ≥5 ticks.
- Lane-occupation hysteresis: same.
- Compositor: press override wins on triggering agent only.
- Compositor: run-intent aborts cleanly when shape-integrity
  would break.
- Failure modes F1–F5 each have a dedicated test.

### 5.3 Integration Test List
- Phase transition (in-possession → out-of-possession) under each
  of the 6 shipped formations produces no `LINE_BREACH_ALERT`.
- Compositor cross-test: #13 press + #15 run on the same agent
  resolves deterministically by precedence rule.

### 5.4 Determinism Regression (binding to #16 §5)
- Replay a 90-minute simulated match; per-tick digest matches
  bit-for-bit on Stage-0 pinned host.

### 5.5 Performance Validation (binding to §6 / #18)

### 5.6 Tactical-Correctness Scenarios (binding to Appendix B)

### 5.7 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS AND BUDGETS

### 6.1 Constant Catalogue
- Full table of every `[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` /
  `[CROSS]` / `[CROSS-PENDING]` constant defined in this spec.
- Cross-references to §3.x where each constant is used.
- Tags audited at Approval Checklist time.

### 6.2 Hot Path Enumeration
- Per #18 KD-10 (per-spec §6 union → hot-path list).
- Main per-tick loop: 22 agents × (anchor + offset + line + lane +
  spacing + compositor) ≈ O(n) with one O(n²) pairwise pass at
  §3.6 (n=22 → 484 pairs, well below per-tick budget).

### 6.3 Per-Tick Budget (Stage 0)
- Target: **≤0.15 ms** per 10 Hz tactical tick on the pinned
  Stage 0 host (placeholder — value pinned at section-file draft
  against `certification-platform.md` once filled).
- 0 allocation per tick on hot path (binding to #18 §3.7).

### 6.4 Per-Frame Budget (60 Hz)
- This spec produces NO per-frame work. Per-frame consumption is
  owned by #2 Agent Movement.

### 6.5 Memory Footprint
- `PositionTarget[22]` + hysteresis state ≈ <2 KB per match.
- Formation catalogue: ~6 archetypes × 11 roles × 16 bytes ≈ 1 KB
  read-only.

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1+ — Authoring Tools (deferred per KD-11)
- Coach UI for live shape adjustment.
- Custom-formation editor.
- Tactical instruction sliders bound to §3.5 modifiers.

### 7.2 Stage 1+ — Set-Piece Positioning
- Throw-in / corner / free-kick / kick-off shapes.

### 7.3 Stage 2+ — ML-Tuned Parameters
- Off-line training over scout-tagged match corpus to fit the
  `[GT]` tables in §6.1.

### 7.4 Stage 5+ — Fixed64 Migration
- Per #9 §8.1 and CLAUDE.md "When Writing Code". Currently
  `float`-based; binding deferred.

### 7.5 Stage 5+ — Multiplayer Determinism
- Host-platform pinning generalises to cross-platform parity.

### 7.6 Version History

---

## SECTION 8 — REFERENCES AND CITATIONS

### 8.1 Cross-Spec References
- #1 Ball Physics §1.2, §3.x.
- #2 Agent Movement §2.5 (XC-002-001), §3.1 (hysteresis pattern).
- #7 Perception System §3.7–§3.10.
- #8 Decision Tree §1.7.3 (XC-008-001), §3.2 (action selection).
- #16 Deterministic Simulation §3.2, §3.2.5, §3.4, §5, §6.2.
- #17 Event System §3.4 (channel registry), §3.10 (catalogue).
- #18 Performance Optimization §3.7 (`[HotPathAllocExempt]`),
  §6 (budget roll-up).
- #19 Testing Strategy §3, §4 (test taxonomy).
- #20 Code Standards (file naming, struct conventions).

### 8.2 CLAUDE.md Invariants Bound
- Corner origin, fatigue convention, tick-rate split,
  zero-allocation hot path, constant-tag policy, Interface Design
  Principle.

### 8.3 Typed Cross-Reference IDs
- `XC-012-001` … (allocated during section-file authoring).
- `ERR-012-001` (DOMAIN_TAG_POSITIONING_AI allocation in #16
  §3.4).

### 8.4 Version History

---

## SECTION 9 — APPROVAL CHECKLIST

### 9.1 Self-Contained Spec Content (verifiable today)
- All 13 review findings resolved (mapping table below).
- All FRs cross-referenced to source.
- All constants tagged.
- All cross-spec citations grep-verified.
- All worked examples numerically reproducible.

### 9.2 Cross-Spec Sign-Offs Required
- #16 (DOMAIN_TAG_POSITIONING_AI allocation patch landed).
- #13 / #14 / #15 (boundary acknowledgement — but these are
  NOT STARTED at this outline stage, so this row is informational
  only; sign-off becomes a precondition only when downstream
  specs reach `IN REVIEW`).

### 9.3 KD-Sequencing Preconditions
- (a) #16 §3.4 DOMAIN_TAG_POSITIONING_AI allocation MERGED.
- (b) All `[CROSS-PENDING]` tags promoted to `[CROSS]` after (a).
- (c) Lead-developer R-01..R-05 review pass.

### 9.4 Finding-to-Resolution Map (resolves all 13)

| Review finding | Severity | Resolved by |
|---|---|---|
| 1. Missing metadata header | H | "Metadata Header" section above |
| 2. Section plan deviates from template | H | §1–§9 mapping above (CLAUDE.md template) |
| 3. Boundary with #8/#13/#14/#15 unstated | H | KD-3..KD-6 + Boundary Matrix |
| 4. Authoring-tool scope creep | H | KD-11 + §7.1 deferral |
| 5. Formation data ownership undefined | H | KD-7 + §6.1 `FormationCatalogue.cs` |
| 6. No determinism plan | M | KD-9 + §3.9 + §4.6 |
| 7. Coordinate convention unstated | M | KD-1 + §1.7 |
| 8. Hysteresis pattern missing | M | KD-8 + §3.8 |
| 9. Tick-rate split unstated | M | KD-2 + §1.7 + §6.3/§6.4 |
| 10. Constant-tag policy not invoked | M | KD-12 + §6.1 |
| 11. Fatigue interaction unmentioned | L | KD-1 + §1.7 + §3.5 |
| 12. Event production/consumption | L | KD-10 + §4.4 / §4.5 |
| 13. Test-pyramid hint missing | L | §5.1 test-count table |

### 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

### 9.6 Version History

---

## APPENDICES

### Appendix A — Derivations
- Anchor formula derivation from formation %.
- Compactness scalar derivation (vertical + lateral).
- Dead-zone hysteresis dwell-time selection (binding to #2 §3.1
  proof).

### Appendix B — Formation Archetype Profiles
- 4-3-3, 4-2-3-1, 4-4-2, 3-5-2, 3-4-3, 5-3-2.
- Per-archetype 11-role offset table (lateral % / longitudinal %).
- Per-archetype tactical-correctness scenario (binding to §5.6).

### Appendix C — Debug Overlays (Stage 0+1 deferred surface)
- Pitch-overlay visualisation spec for development builds.
- NOT a Stage 0 deliverable — listed here so the convention is
  pre-committed.

### Appendix D — Sensitivity Analysis
- Hysteresis dwell window sensitivity.
- Lane / line dead-zone radius sensitivity.

### Appendix E — Worked Examples (numerically reproducible)
- Three full per-tick walk-throughs across 4-3-3 / 4-4-2 / 3-5-2.

### Appendix F — Glossary
- Anchor, lane, line, phase, compactness, baseline shape,
  override, compositor.

---

## NEXT STEPS (post-outline)

1. Allocate `DOMAIN_TAG_POSITIONING_AI = 0x16` in #16 §3.4 — file
   `ERR-012-001` in `docs/tracking/spec-error-log.md`. This is a
   patch-level revision to #16 (already `APPROVED`); coordinate
   with lead developer.
2. Draft `section-1.md` from §1 above.
3. Draft `section-2.md` (FR table) — populate FR-PA-019..045.
4. Draft `section-3.md` (formulas + pseudocode).
5. Draft `section-4.md` (architecture).
6. Draft `section-5.md` (test plan).
7. Draft `section-6.md` (budgets + constant catalogue).
8. Draft `section-7.md` (deferrals).
9. Draft `section-8.md` (references — grep-verify all citations).
10. Draft `section-9-approval-checklist.md`.
11. Draft `appendices.md`.
12. Adversarial review pass (PASS-1) against the section files.
13. Apply fix pass v0.2.
14. Flip `SPEC_INDEX.md` row 12 `NOT STARTED → IN REVIEW`.
15. Lead-developer sign-off.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 15, 2026 | AI agent (claude/positioning-ai-specs-50o0D) | Initial detailed outline. Resolves all 13 findings from `outline.md` May 6 adversarial review (5 H / 5 M / 3 L). Ready for section-file authoring. |
