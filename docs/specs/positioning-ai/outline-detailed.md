# Positioning AI Specification #12 — Detailed Outline

**Created:** May 15, 2026
**Last Updated:** May 15, 2026 (v1.1 — same day adversarial-review fix pass)
**Version:** 1.1
**Status:** DRAFT — v1.0 resolved all 13 findings from the May 6, 2026
review at the bottom of `outline.md`. v1.1 (same day) resolves a
further 13 findings (5 H / 5 M / 3 L) from the self-adversarial pass
filed against v1.0 — `adversarial-review-v1.md` (Findings AR-V1-01 …
AR-V1-13). Key v1.1 changes:
  - **KD-3 rewritten.** Verified against `decision-tree/section-3-1.md`
    §3.1.7 and `section-3-2.md` §3.2.6: #8 owns `MOVE_TO_POSITION`
    action selection and consumes `TacticalContext.FormationSlot` —
    #12 PUBLISHES those formation slots into `TacticalContext`, it
    does NOT replace #8's per-agent action loop. The "compositor" is
    therefore on the input side of #8, not the output side.
  - **KD-10 rewritten.** v1.0 fabricated three #17 channel names
    (`PHASE_CHANGE`, `ACTION_INTENT`, `POSSESSION_TURNOVER`) — none
    exist in `event-system/section-3.md`. Stage 0 architecture is
    `TacticalContext`-mediated (read-only struct shared with #8),
    not event-stream-mediated. Channel additions deferred to Stage 1+
    when downstream specs reach `IN REVIEW`.
  - **KD-9 demoted.** `DOMAIN_TAG_POSITIONING_AI` value is now `_TBD_`
    pending a Phase B/C block-allocation policy. Filed as
    `ERR-012-001` request rather than unilateral pre-emption of
    `0x16`.
  - **KD-13 added.** Compositor precedence rule moved to a justified
    conflict-policy table (§3.7) rather than fiat ordering.
  - **KD-14 added.** Spacing tie-break is cost-based, not
    EntityId-based; EntityId is only the terminal tie-break when
    costs are equal.
  - **KD-15 added.** Stage 0 budget pinned against a named reference
    host (developer workstation) with explicit caveat for cert host.
  - **KD-16 added.** Float-comparison policy at hard-spacing
    boundary (epsilon on squared distance).
  - **KD-17 added.** Single constant catalogue `PositioningAIConstants.cs`
    per #20 §4.2 FR-CS-025.
  - FR-PA-019..045 enumerated (resolves AR-V1-06).
  - Hysteresis constants demoted to `[EST]` pending derivation
    (resolves AR-V1-08).
  - Six-archetype claim cited to planning docs OR flagged for
    lead-developer ratification (resolves AR-V1-07).
**Companion documents:** `outline.md` (high-level + first review);
`adversarial-review-v1.md` (self-adversarial pass against v1.0).
**Unblocks:** Phase C Priority 4 specs #13 / #14 / #15.

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan
that resolves every adversarial-review finding (both the May 6 review
on `outline.md` and the v1.0 self-adversarial pass). For each
subsection: the FRs it will publish, the boundary declarations it
will hold, and the cross-references it will emit. Detailed enough
that `section-1.md` … `section-9-approval-checklist.md` and
`appendices.md` can be drafted mechanically.

This document does **not** publish FR text in normative form — that
text lands in `section-2.md`. The detailed outline records every
FR's intended rule, conformance level, and source spec/section so the
FR table can be authored without re-deriving rules.

---

## METADATA HEADER (resolves outline.md H-1)

| Field | Value |
|-------|-------|
| Spec # | 12 |
| Title | Positioning AI |
| Folder | `docs/specs/positioning-ai/` |
| Priority | 3 (Stage 0 — Phase B keystone) |
| Status | NOT STARTED (outline phase) |
| Owner | Lead developer (gameplay-AI domain) |
| Approved Dependencies | #1 Ball Physics; #2 Agent Movement; #7 Perception System; #8 Decision Tree; #16 Deterministic Simulation; #17 Event System; #20 Code Standards |
| Pending Dependencies | none at outline phase; ERR-012-001 (domain-tag allocation in #16 §3.4) opens at section-file draft |
| Downstream Consumers | #13 Pressing AI; #14 Defensive AI; #15 Attacking AI (Phase C linear chain) |
| Stage Binding | Stage 0 (`float`, state-snapshot determinism). Fixed64 deferred to Stage 5+ per #9 §8.1. |
| Estimated Effort | 5–7 working days |

---

## CROSS-CUTTING DESIGN DECISIONS

### KD-1 — Cite-not-redefine
Spec #12 never restates a CLAUDE.md invariant or a rule from another
approved spec. Cited: corner-origin coordinates (#1 §1.2); fatigue
`0=rested,1=fatigued` (CLAUDE.md); 10 Hz tactical / 60 Hz physics
(CLAUDE.md); EntityId no-reuse (#2 §2.5 XC-002-001 + #8 §1.7.3
XC-008-001); perception schema (#7 §3.7–§3.10); decision-tree action
schema (#8 §3.1, §3.2).

### KD-2 — 10 Hz tactical, 60 Hz steering owned by #2
Positioning AI runs on the 10 Hz tactical loop. Output is a
`FormationSlot` (3D target point + role + lane + line membership)
per controlled agent, written once per tick into a shared
`TacticalContext` struct. Agent Movement #2 §3.x consumes the
resolved `Action.TargetPosition` returned by #8 at 60 Hz steering.
Spec #12 does NOT emit per-frame steering and does NOT write
`Action.TargetPosition` directly — that field is owned by #8 (see
KD-3).

### KD-3 — Boundary with Decision Tree #8 (rewritten v1.1; verified)

**Verified facts** (grep against `decision-tree/section-3-1.md` §3.1.7
and `section-3-2.md` §3.2.6):
- #8 evaluates `MOVE_TO_POSITION` for every off-ball agent on every
  10 Hz tactical tick.
- The action's `TargetPosition` field is sourced from
  `TacticalContext.GetFormationSlot(AgentId)` (section-3-1.md L702,
  L707–725).
- §3.1.7 explicitly says "Stage 1 wires the Formation Engine" —
  Stage 0 uses hardcoded `TacticalContext` defaults.

**Therefore #12's Stage 0 role is to BE the Formation Engine that
populates `TacticalContext.FormationSlot[]`.** It is an upstream
producer of #8 inputs, NOT a competitor of #8 action selection.

**Boundary at Stage 0:**
- **#12 owns:** the `FormationSlot[22]` array inside
  `TacticalContext` — one slot per agent per tactical tick. Each
  slot includes the 3D target point, role tag, lane/line
  membership, and a stability hash for #8 hysteresis use.
- **#8 owns:** the per-agent action loop (PASS, SHOOT, DRIBBLE, HOLD,
  MOVE_TO_POSITION, PRESS, INTERCEPT) and the utility scoring that
  selects between them. `MOVE_TO_POSITION` reads
  `TacticalContext.FormationSlot[AgentId].TargetPosition` verbatim.
- **#12 → #8 coupling:** one direction, read-only,
  `TacticalContext`-mediated. No event channel needed at Stage 0.
- **#8 → #12 coupling:** none at Stage 0. #12 does not consume #8
  output. (At Stage 1+, when ball-carrier context biases shape, this
  may invert — explicitly deferred.)

This boundary inverts v1.0's claim. v1.0 said "#12 selects
positional targets for off-ball agents; #8 selects actions for the
on-ball agent." That was wrong: #8 selects actions for all 22 agents
and reads #12's slots as input to one of those actions.

### KD-4 — Boundary with Pressing AI #13
At Stage 0, #13 is NOT STARTED and #8 §3.2.7 already provides
independent PRESS utility scoring (uncoordinated, per-agent) per its
§1.4.21 deferral note ("No coordinated pressing ... Stage 1 —
Pressing AI #13 introduces coordinated press triggers"). #12 at
Stage 0 therefore provides the **baseline out-of-possession
`FormationSlot[]`**; agents who score PRESS highly in #8 will
deviate from their slot via #8's own action mechanism, not via a
#12-side compositor.

**Stage 1+ deferred binding (declared here only as a boundary
hint):** when #13 reaches `IN REVIEW`, it will publish a
`PressOverride` displacement layer that mutates
`TacticalContext.FormationSlot[]` BEFORE #8 reads it. #12 §7.x
declares the schema slot for that future displacement so #13 can
bind against a named surface — but **#12 does not implement the
override at Stage 0** (per CLAUDE.md "Interface Design Principle":
never write interfaces against unspecified consumers).

### KD-5 — Boundary with Defensive AI #14
At Stage 0, #14 is NOT STARTED. #12 publishes `LineMembership` and
`LaneAssignment` as read-only fields inside `FormationSlot`. Stage 1+
#14 may consume these to assign mark/cover responsibilities. **No
Stage 0 interface to #14.**

### KD-6 — Boundary with Attacking AI #15
At Stage 0, #15 is NOT STARTED. Off-ball runs at Stage 0 emerge from
#8's `MOVE_TO_POSITION` utility weighting against alternative
actions, not from a separate run system. #12 publishes baseline
support shape only. Stage 1+ `RunIntent` displacement is declared in
§7.x as a boundary hint, not implemented.

### KD-7 — Formation data ownership
Formation archetypes live as `static readonly` arrays in
`PositioningAIConstants.cs` (single catalogue per #20 §4.2
FR-CS-025; see KD-17). Each archetype is an 11×{lateralPct,
longPct, role, line, lane} table. **Stage 0 ships THREE archetypes**
(4-4-2, 4-3-3, 4-2-3-1) — the most common modern shapes; additional
shapes (3-5-2, 3-4-3, 5-3-2) are Stage 1+ deferrals. This number is
PROVISIONAL pending grep against `docs/planning/` (see "Outstanding
Outline-Phase Questions" below) — if planning docs commit to a
different count, the catalogue tracks the planning-doc number.

### KD-8 — Hysteresis pattern reuse (#2 §3.1 binding)
Anchor selection, line membership, and lane occupation use the
dwell-time + dead-zone hysteresis pattern from #2 §3.1. #12 does NOT
define a new algorithm — it parameterises the #2 pattern. All
hysteresis constants are `[EST]` at outline stage (resolves
AR-V1-08) pending the section-file derivation pass.

### KD-9 — Determinism binding (#16)
All `FormationSlot[]` writes are authoritative simulation state per
#16 §3.2 and appear in the per-tick digest at the scope #16 §6.2
defines for tactical-AI outputs. Agent iteration uses the canonical
EntityId sort from #16 §3.2.5. Any stochastic micro-jitter (e.g.,
tie-breaking when two roles can fill the same slot) uses
`DeterministicRngService` with domain tag
`DOMAIN_TAG_POSITIONING_AI = _TBD_`. **Value allocation is NOT
asserted by this outline** — it is requested via `ERR-012-001` in
`spec-error-log.md` as part of a Phase B/C block-allocation policy
covering #10/#11/#12/#13/#14/#15. Tag remains `[CROSS-PENDING]`
until lead-developer ratification of the block.

### KD-10 — Event System binding (#17) — REWRITTEN v1.1
v1.0 named three #17 channels (`PHASE_CHANGE`, `ACTION_INTENT`,
`POSSESSION_TURNOVER`). **None of these exist in
`event-system/section-3.md`.** They were fabricated. At Stage 0,
#12 does NOT produce or consume any #17 channels. All upstream
inputs come from `TacticalContext` (#8-shared struct) and #7
Perception snapshot; all downstream outputs are
`TacticalContext.FormationSlot[]` writes consumed by #8.

Future-channel boundary hints (Stage 1+ deferrals only — declared
in §7, NOT implemented at Stage 0):
- A future `SHAPE_TRANSITION` telemetry channel may be added when
  debug overlays land (Stage 0+1 deferred per Appendix C).
- A future `LINE_BREACH_ALERT` channel may be added when the
  authoring-tool surface lands (Stage 1+).
- Both would require atomic back-prop into #17 §3.10 — same pattern
  as ERR-017-001. NOT in scope for Stage 0.

Phase enumeration (in-possession / out-of-possession /
transition-to-attack / transition-to-defense) is computed LOCALLY in
#12 §3.0 from ball-position + possession-state inputs (already
present in #7 Perception output). Phase is NOT a cross-spec enum at
Stage 0.

### KD-11 — Stage 0 scope discipline
Out of Stage 0 scope (deferred to §7): authoring tools, coach UI,
save-game persistence, ML-tuned shape parameters, set-piece
positioning, custom-formation editor, telemetry event channels, the
Press/Run/Mark override layers from #13/#14/#15.

### KD-12 — Constant-tag discipline
Every constant carries exactly one of `[GT]`, `[EST]`, `[FIXED]`,
`[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`. All hysteresis and
spacing constants start at `[EST]` at outline stage; promotion to
`[GT]` happens during section-file authoring when each value gains
a worked-example justification (CLAUDE.md "When Writing or Editing
Specs").

### KD-13 — Compositor precedence (added v1.1)
Because Stage 0 has no #13/#15 overrides, the only intra-#12
compositor concerns are:
- (a) anchor vs ball-relative-offset (§3.1 vs §3.2): offset is
  applied additively, no precedence question.
- (b) two roles eligible for the same lane: resolved by anchor
  proximity to the candidate slot, then by EntityId as terminal
  tie-break (KD-14).
- (c) hard spacing violation between two computed slots: resolved
  by §3.6 cost-based displacement (KD-14).

The v1.0 "press-then-run-then-spacing" order is DELETED — those
layers don't exist at Stage 0. Stage 1+ §7.x declares a future
conflict-policy TABLE (not a fixed order) when downstream specs are
ready to argue precedence.

### KD-14 — Spacing tie-break is cost-based, not EntityId-based (added v1.1)
When §3.6 hard-spacing min-separation `MIN_AGENT_SEPARATION_M`
is violated between two computed slots, the agent displaced is the
one whose **post-displacement cost** (squared distance from its
anchor) is LOWER — i.e., the one with the smaller required move.
EntityId is the terminal tie-break only when costs are equal within
`SPACING_EPSILON_M2` (KD-16). This avoids the v1.0 defect where
high-EntityId agents would be systematically displaced over a full
match.

### KD-15 — Per-tick budget pinned against named reference host (added v1.1)
Until `certification-platform.md` is filled by lead developer (open
issue in CLAUDE.md, May 6, 2026), §6.3 budget targets are stated
against a **named developer-workstation reference**: Ryzen 7 5800X
@ 4.5 GHz single-thread, 32 GB DDR4-3200, Windows 11, Unity 2022.3
LTS, Mono backend, single-threaded measurement. Caveat: the
cert-pinned budget supersedes once available — values may move ±30%.
This is documented in §6.3 prose, NOT a placeholder
`TBD-NORMATIVE` tag.

### KD-16 — Float-comparison epsilon at hard-spacing boundary (added v1.1)
§3.6 spacing comparisons use squared distance with an explicit
epsilon `SPACING_EPSILON_M2 = 1e-4 m²` (= 1 cm at the boundary).
Comparisons of the form `if (distSq < MIN_AGENT_SEPARATION_M_SQ)`
are stable across CLAUDE.md-permitted `float` arithmetic variation
on the pinned Stage 0 host. This addresses the float-determinism
hazard from AR-V1-10.

### KD-17 — Single constant catalogue per #20 §4.2 (added v1.1)
ALL constants — including the formation archetype tables — live in
one file `PositioningAIConstants.cs`, organised into `#region`
blocks per #20 §4.2. The v1.0 split (`PositioningConstants.cs` +
`FormationCatalogue.cs`) is REJECTED as it violates FR-CS-025.

---

## BOUNDARY MATRIX (resolves outline.md H-3; revised v1.1)

| Boundary | #12 owns | Other owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `TacticalContext.FormationSlot[22]` | Per-agent action loop incl. `MOVE_TO_POSITION` | #8 reads #12 | Read shared `TacticalContext` struct at tick start | Yes |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering toward `Action.TargetPosition` | #2 reads #8 | #8's resolved action carries the target | Yes |
| #7 Perception | (none — read consumer) | Filtered world model | #12 reads #7 | Snapshot read at tick start | Yes |
| #13 Pressing | Schema slot for future `PressOverride` | Press trigger + displacement | (deferred) | Stage 1+ only | No |
| #14 Defensive | Read-only `LineMembership` / `LaneAssignment` exposed | Mark/cover assignment | (deferred) | Stage 1+ only | No |
| #15 Attacking | Schema slot for future `RunIntent` | Off-ball run system | (deferred) | Stage 1+ only | No |
| #16 Determinism | `FormationSlot[]` digest scope | Digest format + iteration rule | #12 conforms | Iteration order + domain-tagged RNG (`_TBD_`) | Yes |
| #17 Event System | (none at Stage 0) | Channel registry | (deferred) | No channels produced/consumed at Stage 0 | No |
| #20 Code Standards | (none — conformance only) | File / catalogue / naming rules | #12 conforms | `PositioningAIConstants.cs` per FR-CS-025 | Yes |

---

## SECTION 1 — INTRODUCTION, SCOPE, DEPENDENCIES, KEY DECISIONS

### 1.1 Purpose
One-paragraph problem statement: #12 is the Stage 0 Formation Engine
that #8 §3.1.7 references as "Stage 1" but is in fact promoted to
Stage 0 because every Phase C tactical-AI spec depends on a
non-hardcoded formation source. Cite CLAUDE.md "Project Identity".

### 1.2 Scope (in / out)
- **In:** anchor computation per role/formation, ball-relative
  offsets, phase computation (local), shape compactness, lane/line
  membership, context modifiers (score, fatigue, tactical
  intensity), hysteresis on anchor & line & lane.
- **Out (per KD-11):** authoring tools, coach UI, set-pieces,
  ML tuning, save-game, telemetry channels, override layers from
  #13/#14/#15.

### 1.3 Dependencies
Upstream APPROVED: #1, #2, #7, #8, #16, #17 (schema only — no
channels consumed), #20.

### 1.4 Key Domain Concepts
Anchor, ball-relative offset, lane (LW/LH/C/RH/RW), line
(Defense/Midfield/Attack), phase (local enum, 4 values),
compactness (lateral + vertical scalars), `FormationSlot` struct.

### 1.5 Key Design Decisions
Cross-reference KD-1..KD-17.

### 1.6 Interface Boundaries
The Boundary Matrix above.

### 1.7 Coordinate & Convention Bindings
Corner origin (#1 §1.2); fatigue `0=rested,1=fatigued`;
10 Hz tactical / 60 Hz physics; EntityId no-reuse.

### 1.8 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES, FAILURE MODES

### 2.1 Functional Requirements Table (provisional 48-entry enumeration)

Conformance: `MUST` unless noted. Citations point to KDs or source
sections. (Resolves AR-V1-06.)

| FR | Subject | Conf. | Source |
|---|---|---|---|
| FR-PA-001 | Tactical tick rate is 10 Hz | MUST | CLAUDE.md / KD-1 |
| FR-PA-002 | Output is one `FormationSlot` per agent per tick into shared `TacticalContext` | MUST | KD-2, KD-3 |
| FR-PA-003 | Agent iteration order is EntityId-sorted ascending | MUST | #16 §3.2.5 / KD-9 |
| FR-PA-004 | `FormationSlot[]` contributes to per-tick digest | MUST | #16 §6.2 / KD-9 |
| FR-PA-005 | RNG calls use `DOMAIN_TAG_POSITIONING_AI` (value TBD) | MUST | #16 §3.4 / KD-9 |
| FR-PA-006 | No allocation on hot path | MUST | #18 §3.7 |
| FR-PA-007 | Three formation archetypes shipped at Stage 0 (4-4-2, 4-3-3, 4-2-3-1) | MUST | KD-7 |
| FR-PA-008 | Anchor selection uses dwell-time hysteresis (binding to #2 §3.1) | MUST | KD-8 |
| FR-PA-009 | Line-membership transitions use dead-zone hysteresis | MUST | KD-8 |
| FR-PA-010 | Lane-occupation transitions use dead-zone hysteresis | MUST | KD-8 |
| FR-PA-011 | Single constant catalogue `PositioningAIConstants.cs` per #20 §4.2 | MUST | #20 FR-CS-025 / KD-17 |
| FR-PA-012 | Hard spacing `MIN_AGENT_SEPARATION_M` enforced between any two slots | MUST | §3.6 |
| FR-PA-013 | Soft spacing penalty when two agents share line∩lane | SHOULD | §3.6 |
| FR-PA-014 | Spacing displacement tie-break is cost-based, EntityId terminal | MUST | KD-14 |
| FR-PA-015 | Float comparisons use squared distance with `SPACING_EPSILON_M2` | MUST | KD-16 |
| FR-PA-016 | Fatigue input convention `0=rested` | MUST | CLAUDE.md / KD-1 |
| FR-PA-017 | Score-modifier input range [-3, +3] (clamped) | MUST | §3.5 |
| FR-PA-018 | Tactical-intensity input range [0, 1] | MUST | §3.5 |
| FR-PA-019 | Anchor formula: `anchor = pitchSize * formationOffset[role]` | MUST | §3.1 |
| FR-PA-020 | Ball-relative offset piecewise-linear in `ball.x`, `ball.y` | MUST | §3.1 |
| FR-PA-021 | Pull-toward-ball strength per role per phase from `[GT]` table | MUST | §3.2 |
| FR-PA-022 | Phase computed locally from ball position + possession state | MUST | KD-10 / §3.0 |
| FR-PA-023 | Phase transitions are hysteretic (no oscillation at boundaries) | MUST | §3.0 |
| FR-PA-024 | Line partition is stable k=3 longitudinal partition (GK excluded) | MUST | §3.3 |
| FR-PA-025 | Lane partition is 5-bin lateral classification | MUST | §3.4 |
| FR-PA-026 | Soft constraint: ≤2 agents per lane in midfield third | SHOULD | §3.4 |
| FR-PA-027 | Hard constraint: ≤3 agents per lane anywhere | MUST | §3.4 |
| FR-PA-028 | Context modifier composition is multiplicative on compactness | MUST | §3.5 |
| FR-PA-029 | Score modifier scales attacking compactness linearly | MUST | §3.5 |
| FR-PA-030 | Fatigue (team-mean) relaxes lateral compactness up to `FATIGUE_LATERAL_RELAX_M` | MUST | §3.5 |
| FR-PA-031 | Tactical intensity scales vertical compactness target | MUST | §3.5 |
| FR-PA-032 | Tactical intensity default source is per-archetype `[GT]` field (no UI at Stage 0) | MUST | KD-11 / AR-V1-11 |
| FR-PA-033 | Slot writes are clamped to pitch bounds with 0.5m touchline margin | MUST | §2.4 F5 |
| FR-PA-034 | `FormationSlot` includes stable hash for #8 hysteresis use | MUST | KD-3 |
| FR-PA-035 | Goalkeeper slot computed by dedicated formula (no line partition) | MUST | §3.3 |
| FR-PA-036 | Substituted/red-carded agents excluded from compactness computation | MUST | §2.4 |
| FR-PA-037 | Slot computation is pure function of (perception, ball, phase, formation, modifiers, prev hysteresis state) | MUST | §4.1 |
| FR-PA-038 | Hysteresis state itself is authoritative simulation state | MUST | §4.6 / KD-9 |
| FR-PA-039 | Formation archetype is fixed per side per match at Stage 0 (no in-match switch) | MUST | KD-11 |
| FR-PA-040 | All tunable constants tagged per KD-12 | MUST | KD-12 |
| FR-PA-041 | All formulas have a worked example in §3 or Appendix E | MUST | CLAUDE.md |
| FR-PA-042 | Failure mode F1: stale perception → freeze previous tick output | MUST | §2.4 |
| FR-PA-043 | Failure mode F2: invalid formation index → fall back to 4-4-2 | MUST | §2.4 |
| FR-PA-044 | Failure mode F3: NaN in offset → clamp to raw anchor | MUST | §2.4 |
| FR-PA-045 | Failure mode F4: mid-tick input change → defer to next tick boundary | MUST | §2.4 / AR-V1-09 |
| FR-PA-046 | Failure mode F5: slot outside pitch bounds → clamp with margin | MUST | §2.4 |
| FR-PA-047 | Failure mode F6: phase enum corruption → fall back to in-possession (least-aggressive shape) | MUST | §2.4 |
| FR-PA-048 | No interface produced against unspecified consumer specs (#13/#14/#15 at Stage 0) | MUST | CLAUDE.md / KD-4..6 / KD-11 |

### 2.2 Data Structures

| Struct | Purpose | Stage |
|---|---|---|
| `FormationSlot` | Per-agent slot output: target, role, lane, line, stability hash | 0 |
| `FormationArchetype` | 11×5 lookup: role, lateral%, long%, line, lane | 0 |
| `TacticalContext` | Shared with #8 — contains `FormationSlot[22]` | 0 (#8-owned struct; #12 fills the slice) |
| `ContextModifierInputs` | Score diff, team-mean fatigue, tactical-intensity | 0 |
| `HysteresisState` | Per-agent dwell counters for anchor / line / lane | 0 |
| `BaselineDefensiveShape` (read-only view) | Reserved name for #14 Stage 1+ consumption | 1+ |
| `PressOverride` schema | Reserved name for #13 Stage 1+ writer | 1+ |
| `RunIntent` schema | Reserved name for #15 Stage 1+ writer | 1+ |

### 2.3 Inputs (read-only at tick start)
- Perception snapshot (#7 §3.7) — agent positions, ball position,
  possession state.
- Ball state via perception.
- Context modifier inputs (computed by match orchestrator, exposed
  to #12 as a struct).

### 2.4 Failure Modes (F1–F6 above)
Section files enumerate each with detection condition, recovery
action, and test reference.

### 2.5 Version History

---

## SECTION 3 — CORE FORMULAS AND ALGORITHMS

### 3.0 Phase computation (local) — added v1.1
Phase ∈ {InPoss, OutOfPoss, TransToAtk, TransToDef} computed from:
- Possession owner (from #7 Perception).
- Ball longitudinal direction-of-travel filtered over 3 ticks.
- Hysteresis: transition phases sticky for ≥3 ticks before reverting.
`PHASE_HYSTERESIS_TICKS = 3` `[EST]`.

### 3.1 Anchor computation
`anchor = (pitchLengthM * formationOffset[role].x,
pitchWidthM * formationOffset[role].y, 0)`. Worked example:
4-3-3 LW → (78.0, 6.8, 0).

### 3.2 Ball-relative offset
Piecewise linear in (ball.x, ball.y), three break-points per axis,
weighted by role-and-phase `pullFactor`. Worked example: ball in
own defensive third + out-of-poss → AM anchor pulls back 8m.

### 3.3 Line membership
Stable k=3 longitudinal partition with dead-zone hysteresis.
GK excluded. `LINE_HYSTERESIS_M = 3.0m [EST]`.

### 3.4 Lane occupation
5-bin lateral classification with dead-zone hysteresis.
`LANE_HYSTERESIS_M = 2.0m [EST]`.

### 3.5 Context modifiers
Multiplicative composition. Score, fatigue, tactical-intensity
weights all `[GT]` in §6.

### 3.6 Spacing constraints (hard + soft)
- Hard: `MIN_AGENT_SEPARATION_M = 1.5m [FIXED]` (cited from #3
  collision radius).
- Soft: same-line∩lane co-occupation cost.
- Comparisons via squared distance with
  `SPACING_EPSILON_M2 = 1e-4 [FIXED]` (KD-16).
- Displacement tie-break: cost-based (KD-14); EntityId terminal.

### 3.7 Slot composition (Stage 0 — simplified per KD-13)
Per-tick order:
1. Compute baseline anchor (§3.1).
2. Apply ball-relative offset (§3.2).
3. Apply context modifiers (§3.5).
4. Compute line/lane membership with hysteresis (§3.3, §3.4).
5. Enforce hard spacing (§3.6) with cost-based displacement.
6. Clamp to pitch bounds.
7. Write into `TacticalContext.FormationSlot[AgentId]`.

No #13/#15 override step at Stage 0. §7 declares the Stage 1+ slot
in the pipeline.

### 3.8 Hysteresis (binding to #2 §3.1)
Cite-not-redefine. Parameters all `[EST]` at outline stage:
- `ANCHOR_DWELL_TICKS = 5` (500ms).
- `LINE_HYSTERESIS_M = 3.0m`.
- `LANE_HYSTERESIS_M = 2.0m`.
- `PHASE_HYSTERESIS_TICKS = 3`.

### 3.9 Determinism (binding to #16)
Iteration order; `DOMAIN_TAG_POSITIONING_AI = _TBD_` (ERR-012-001);
digest scope.

### 3.10 Constants catalogue (forward ref to §6.1)

### 3.11 Pseudocode (per-tick main loop)

### 3.12 Version History

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS

### 4.1 Architecture Overview
Single subsystem `PositioningAI` on the 10 Hz scheduler. Pure-function
design except for the hysteresis dwell counters (themselves
authoritative state).

### 4.2 File Structure (#20 §4.2 compliant — single catalogue)
```
src/PositioningAI/
├── PositioningAITick.cs           (10 Hz entry point)
├── PhaseClassifier.cs             (§3.0)
├── AnchorCalculator.cs            (§3.1 + §3.2)
├── ShapeAnalyzer.cs               (§3.3 + §3.4)
├── ContextModifier.cs             (§3.5)
├── SpacingResolver.cs             (§3.6)
├── SlotComposer.cs                (§3.7)
├── HysteresisState.cs             (dwell counters; authoritative state)
└── PositioningAIConstants.cs      (SINGLE catalogue — formation archetypes + all scalars)
```

### 4.3 Internal Module Contracts

### 4.4 Upstream Integration Contracts
- Perception snapshot read (#7 §3.7).
- `TacticalContext` write surface (#8-owned struct; #12 fills
  `FormationSlot[]` slice at tick start, BEFORE #8's per-agent loop).

### 4.5 Downstream Integration Contracts
- `TacticalContext.FormationSlot[]` consumed by #8 §3.1.7 (`MOVE_TO_POSITION`)
  and §3.2.6 (`MOVE_TO_POSITION` utility).
- `LineMembership` / `LaneAssignment` exposed read-only for Stage 1+
  #14 consumption.

### 4.6 Determinism & Safety Boundaries (binding to #16)
Iteration order; RNG domain tag; digest scope; hysteresis state ARE
digested.

### 4.7 Cross-Specification Validation Checks

### 4.8 Version History

---

## SECTION 5 — TEST PLAN

### 5.1 Test Counts (verifiable target)

| Category | Target | Source |
|---|---|---|
| Unit (anchor, offset, line, lane, hysteresis, spacing) | ≥40 | §3.1–§3.8 |
| Integration (full-team shape under phase transitions) | ≥10 | §3.7 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Tactical-correctness scenarios | ≥6 | Appendix B (one per archetype × 2 phases) |
| **Total** | **≥65** | — |

### 5.2 Unit Test List (representative)
- Anchor at neutral ball matches table within ±0.01m.
- Ball-relative offset clamps at extreme ball positions.
- Line hysteresis: oscillating agent at boundary stays in original
  line for ≥5 ticks.
- Lane hysteresis: same.
- Phase hysteresis: same.
- Spacing tie-break: cost-based displacement matches predicted agent
  (NOT EntityId-based).
- Float-epsilon: comparison at 1.5m ± 0.5cm boundary is stable.
- Each F1–F6 has a dedicated test.

### 5.3 Integration Test List
- Each archetype × each phase produces no spacing violations.
- Phase boundary crossings emit no oscillation over 50-tick window.

### 5.4 Determinism Regression (binding to #16 §5)
- 90-min match replay produces bit-identical per-tick digest on
  reference host.

### 5.5 Performance Validation (binding to §6)

### 5.6 Tactical-Correctness Scenarios (binding to Appendix B)

### 5.7 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS AND BUDGETS

### 6.1 Constant Catalogue
Full enumeration with tags. Hysteresis values `[EST]` at section-file
draft start; promotion to `[GT]` requires a derivation entry in
Appendix A per CLAUDE.md.

### 6.2 Hot Path Enumeration (#18 KD-10 binding)
Main per-tick loop: 22 agents × O(1) computation per phase except
§3.6 pairwise pass (22² = 484 pairs).

### 6.3 Per-Tick Budget (reference host per KD-15)
Target: ≤0.15 ms per 10 Hz tick on the named reference host
(Ryzen 7 5800X @ 4.5 GHz, single thread, Mono backend, Unity
2022.3 LTS). Caveat: cert host budget supersedes once
`certification-platform.md` is filled by lead developer; ±30%
variance possible.

### 6.4 Per-Frame Budget
N/A — no per-frame work.

### 6.5 Memory Footprint
`FormationSlot[22]` + hysteresis state ≈ <2 KB; archetype catalogue
≈ 1 KB read-only.

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1+ — Authoring tools (KD-11)
### 7.2 Stage 1+ — Set-piece positioning
### 7.3 Stage 1+ — `PressOverride` writer layer (#13 binding slot)
### 7.4 Stage 1+ — `RunIntent` writer layer (#15 binding slot)
### 7.5 Stage 1+ — `MarkAssignment` reader layer (#14 binding slot)
### 7.6 Stage 1+ — Additional archetypes (3-5-2, 3-4-3, 5-3-2)
### 7.7 Stage 1+ — Mid-match formation switch
### 7.8 Stage 1+ — Telemetry channels (`SHAPE_TRANSITION`, `LINE_BREACH_ALERT`) via #17 back-prop
### 7.9 Stage 2+ — ML-tuned `[GT]` parameter fitting
### 7.10 Stage 5+ — Fixed64 migration per #9
### 7.11 Stage 5+ — Cross-platform determinism

---

## SECTION 8 — REFERENCES AND CITATIONS

### 8.1 Cross-Spec References (grep-verified at section-file draft time)
- #1 §1.2, §3.x
- #2 §2.5 (XC-002-001), §3.1
- #7 §3.7–§3.10
- #8 §1.7.3 (XC-008-001), §3.1.7, §3.2.6
- #16 §3.2, §3.2.5, §3.4 (ERR-012-001), §5, §6.2
- #17 — schema citation only; no channels at Stage 0
- #18 §3.7, §6
- #19 §3, §4
- #20 §4.2 (FR-CS-025)

### 8.2 CLAUDE.md Invariants Bound
Corner origin, fatigue convention, tick rates, zero-alloc hot path,
constant-tag policy, Interface Design Principle (cited heavily —
explains why #13/#14/#15 overrides are deferred).

### 8.3 Typed Cross-Reference IDs
`XC-012-NNN` to be allocated at section-file draft.
`ERR-012-001` — `DOMAIN_TAG_POSITIONING_AI` allocation request in
#16 §3.4 (Phase B/C block).

### 8.4 Version History

---

## SECTION 9 — APPROVAL CHECKLIST

### 9.1 Self-Contained Spec Content
- All v1.0 outline findings (13) resolved.
- All v1.1 self-adversarial findings (13) resolved.
- All FRs cross-referenced.
- All constants tagged (with `[EST]` for outline-stage placeholders).
- All cross-spec citations grep-verified at draft time.

### 9.2 Cross-Spec Sign-Offs Required
- #16 lead-developer approval of `DOMAIN_TAG_POSITIONING_AI` value
  (via `ERR-012-001` Phase B/C block-allocation).
- #8 owner ack of `TacticalContext.FormationSlot[]` write contract
  (binding existing §3.1.7 + §3.2.6 producer-side).

### 9.3 KD-Sequencing Preconditions
- (a) `ERR-012-001` resolved (domain-tag value allocated).
- (b) All `[CROSS-PENDING]` tags promoted to `[CROSS]`.
- (c) Hysteresis `[EST]` constants promoted to `[GT]` with
  derivation entries in Appendix A.
- (d) Archetype count confirmed against `docs/planning/` (or
  ratified by lead developer).
- (e) Lead-developer R-01..R-05 review pass.

### 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md | 1. Missing metadata header | H | "Metadata Header" |
| outline.md | 2. Section plan deviates | H | §1–§9 mapping |
| outline.md | 3. Boundary unstated | H | KD-3..KD-6 + Boundary Matrix |
| outline.md | 4. Authoring scope creep | H | KD-11 + §7 |
| outline.md | 5. Formation data ownership | H | KD-7 + KD-17 + `PositioningAIConstants.cs` |
| outline.md | 6. No determinism plan | M | KD-9 + §3.9 + §4.6 |
| outline.md | 7. Coordinate convention | M | KD-1 + §1.7 |
| outline.md | 8. Hysteresis missing | M | KD-8 + §3.8 |
| outline.md | 9. Tick-rate split | M | KD-2 + §1.7 |
| outline.md | 10. Constant-tag policy | M | KD-12 + §6.1 |
| outline.md | 11. Fatigue interaction | L | KD-1 + §3.5 |
| outline.md | 12. Event production | L | KD-10 + §4.4/4.5 |
| outline.md | 13. Test-pyramid hint | L | §5.1 |
| AR-V1 | 01. DOMAIN_TAG unilateral allocation | H | KD-9 demoted to `_TBD_` + ERR-012-001 |
| AR-V1 | 02. #8 boundary mis-stated | H | KD-3 rewritten against #8 §3.1.7/§3.2.6 |
| AR-V1 | 03. Compositor ordering by fiat | H | KD-13 + §3.7 simplified (no Stage 0 overrides) |
| AR-V1 | 04. EntityId tie-break unfair | H | KD-14 cost-based |
| AR-V1 | 05. §6.3 placeholder budget | H | KD-15 named reference host |
| AR-V1 | 06. Placeholder FR-PA-019..045 | M | §2.1 fully enumerated (48 FRs) |
| AR-V1 | 07. Archetype count unsourced | M | KD-7 reduced to 3 + planning-doc grep deferred |
| AR-V1 | 08. Hysteresis `[GT]` premature | M | KD-12 + demoted to `[EST]` |
| AR-V1 | 09. Event-tick edge semantics | M | KD-10 (no events at Stage 0) + FR-PA-045 |
| AR-V1 | 10. Float-comparison hazard | M | KD-16 + FR-PA-015 |
| AR-V1 | 11. Tactical-intensity producer | L | KD-11 + FR-PA-032 (per-archetype default) |
| AR-V1 | 12. Two-catalogue file split | L | KD-17 + #20 FR-CS-025 binding |
| AR-V1 | 13. Phase enum unsourced | L | KD-10 (phase is local, not cross-spec) + §3.0 |

### 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

### 9.6 Version History

---

## APPENDICES

### Appendix A — Derivations
Anchor formula, compactness scalars, hysteresis dwell-time
selection (binding to #2 §3.1 proof), `SPACING_EPSILON_M2` choice.
Each `[EST]` constant gets one entry here when promoted to `[GT]`.

### Appendix B — Formation Archetype Profiles
4-4-2, 4-3-3, 4-2-3-1 at Stage 0. Per-archetype 11-role table.

### Appendix C — Debug Overlays (Stage 0+1 deferred)
Pre-committed convention only; not a Stage 0 deliverable.

### Appendix D — Sensitivity Analysis
Hysteresis dwell sensitivity; lane/line dead-zone sensitivity;
float-epsilon sensitivity at 1.5m boundary.

### Appendix E — Worked Examples
Three full per-tick walk-throughs (4-4-2 / 4-3-3 / 4-2-3-1).

### Appendix F — Glossary

---

## OUTSTANDING OUTLINE-PHASE QUESTIONS (open work, not blockers)

1. **Archetype count** — grep `docs/planning/master-development-plan.md`
   for Stage 0 formation-set commitments. If planning docs commit to a
   different count, update KD-7 + FR-PA-007.
2. **`DOMAIN_TAG_POSITIONING_AI` value** — file `ERR-012-001` and
   propose a Phase B/C block: `POSITIONING_AI = 0x16`,
   `HEADING = 0x17`, `GOALKEEPER = 0x18`, `PRESSING = 0x19`,
   `DEFENSIVE = 0x1A`, `ATTACKING = 0x1B`. Lead-developer ratifies.
3. **`TacticalContext` schema** — section-file draft must read
   `decision-tree/section-1.md` for the canonical struct definition
   to confirm there is room to add `FormationSlot[]` without breaking
   the #8 contract. Currently presumed but not grep-verified.
4. **`FormationSlot.StableHash` field** — needed for #8 hysteresis
   integration. Confirm with #8 owner at section-file draft.

---

## NEXT STEPS

1. Resolve the four "Outstanding Outline-Phase Questions" above.
2. Draft `section-1.md`.
3. Draft `section-2.md` (FR table — already enumerated above).
4. Draft `section-3.md`.
5. Draft `section-4.md`.
6. Draft `section-5.md`.
7. Draft `section-6.md` (promote `[EST]` → `[GT]` with derivations
   in Appendix A).
8. Draft `section-7.md`.
9. Draft `section-8.md` (grep-verify every citation).
10. Draft `section-9-approval-checklist.md`.
11. Draft `appendices.md`.
12. Adversarial review pass PASS-1 on section files.
13. v0.2 fix pass.
14. Flip `SPEC_INDEX.md` row 12 `NOT STARTED → IN REVIEW`.
15. Lead-developer R-01..R-05 sign-off.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 15, 2026 | AI agent (claude/positioning-ai-specs-50o0D) | Initial detailed outline. Resolves 13 findings from `outline.md` May 6 adversarial review. |
| 1.1 | May 15, 2026 (same day) | AI agent (claude/positioning-ai-specs-50o0D) | Self-adversarial pass against v1.0 — 13 findings (5 H / 5 M / 3 L). Major v1.1 changes: KD-3 rewritten against verified #8 §3.1.7/§3.2.6 text (#12 is the Stage 0 Formation Engine that feeds `TacticalContext`, not a competitor of #8 action selection); KD-10 rewritten (fabricated #17 channels removed; no Stage 0 channels); KD-9 demoted (`DOMAIN_TAG_POSITIONING_AI = _TBD_`); KD-13..KD-17 added (compositor simplification, cost-based tie-break, named reference host, float epsilon, single catalogue per #20 FR-CS-025). 48 FRs enumerated. Hysteresis constants demoted to `[EST]`. Archetype count reduced to 3 pending planning-doc grep. |
