# Attacking AI Specification #15 — Section 2: Functional Requirements, Data Structures, Inputs, Failure Modes

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 2.1 Functional Requirements

Conformance levels follow RFC 2119: **MUST** is normative; **SHOULD** is a
strong recommendation subject to documented override; **MAY** is permissive.
All citations resolve against either a KD in §1.5, a downstream section
number in this spec, or a named upstream spec and section. All 36 FRs below
carry conformance level **MUST** unless explicitly noted otherwise.

| FR | Subject | Conformance | Source / § |
|---|---|---|---|
| FR-AT-001 | Attacking AI runs on the 10 Hz tactical loop (one evaluation per 100 ms tick). No per-frame (60 Hz) work is produced. | MUST | CLAUDE.md / KD-2 |
| FR-AT-002 | Output per tick is one `AttackDirective` per possessing team plus one `AttackIntent` per agent in the off-ball movement pool. | MUST | KD-2 |
| FR-AT-003 | Agent iteration order during pool evaluation and role assignment is EntityId-sorted ascending. | MUST | #16 §3.2.5 / KD-11 |
| FR-AT-004 | `AttackDirective`, `AttackIntent[]`, `RunParameters`, `AttackHysteresisState`, and `TransitionHoldState` contribute to the per-tick determinism digest. | MUST | #16 §6.2 / KD-11 |
| FR-AT-005 | Any RNG calls within #15 use `DeterministicRngService` with domain tag `DOMAIN_TAG_ATTACKING_AI = 0x1B` (`[CROSS: #16 §3.4]`). | MUST | #16 §3.4 / KD-11 |
| FR-AT-006 | The goalkeeper is unconditionally excluded from the off-ball movement pool. The GK never receives an `AttackIntent`. | MUST | KD-7 |
| FR-AT-007 | The current ball carrier is excluded from the off-ball movement pool on every tick. | MUST | KD-3 |
| FR-AT-008 | Phase gating: if the #12 phase for this team is `OUT_OF_POSSESSION`, emit an empty `AttackDirective` and return immediately without executing the role-assignment algorithm. | MUST | KD-5 / §3.1 |
| FR-AT-009 | Transition phase: if the #12 phase for this team is `TRANSITION`, or if the phase just changed from `IN_POSSESSION` to any non-`IN_POSSESSION` state, dispatch to §3.9 (`TransitionController`) and return its directive. The directive is a frozen copy of the last `IN_POSSESSION` directive for `TRANSITION_HOLD_TICKS [GT]` ticks, then an empty directive. | MUST | KD-6 / §3.9 |
| FR-AT-010 | All off-ball movement is fully parameterised by `RunParameters`. No `PatternType`, `RunType`, or `OverlapType` enum exists anywhere in the algorithm or data structures. | MUST | KD-8 |
| FR-AT-011 | `RunParameters` contains exactly three fields: `depthOffset_m f32`, `lateralOffset_m f32`, `runTriggerTick i32`. The run angle is a derived quantity computed at use-site only as `atan2(lateralOffset_m, depthOffset_m)`; it is never stored in this struct. | MUST | KD-8 / §3.4 |
| FR-AT-012 | The attacking-role catalog is exactly: `RUNNER`, `SUPPORT_BALL`, `HOLD_WIDTH`, `WEAK_SIDE`. No additional roles may be introduced in this spec. | MUST | §3.3 |
| FR-AT-013 | Agents within `SUPPORT_RADIUS_M [GT]` of the ball carrier (scaled by `styleProfile.supportMult`) default to `SUPPORT_BALL` unless assigned `RUNNER`. | MUST | §3.5 |
| FR-AT-014 | At least `MIN_WIDTH_HOLDERS [GT]` agents hold width on the near-touchline side at all times when the pool size permits. | MUST | §3.6 |
| FR-AT-015 | At least one agent holds the `WEAK_SIDE` position on each tick when pool size ≥ `MIN_WEAK_SIDE_AGENT_THRESHOLD [GT]`. | MUST | §3.7 |
| FR-AT-016 | An overload is declared when ≥ `OVERLOAD_COUNT [GT]` non-WEAK_SIDE agents are within the `OVERLOAD_ZONE_WIDTH_M [GT]` Y-corridor centred on the ball's Y-coordinate on the same flank. | MUST | §3.8 |
| FR-AT-017 | Team-style profile constants (`DEPTH_MULT`, `TIMING_MULT`, `SUPPORT_MULT`, `MAX_RUNNERS_OVERRIDE`) are applied as scale factors. The algorithm code is identical across all three profiles (`POSSESSION` / `DIRECT` / `COUNTER_ATTACK`); only the constant values differ. No conditional branching on profile identity is permitted. | MUST | KD-12 / §3.10 |
| FR-AT-018 | Anti-chaos invariant 1: the total count of agents simultaneously assigned `RUNNER` role must not exceed `MAX_RUNNERS [GT]` (as modified by the active profile's `MAX_RUNNERS_OVERRIDE`). | MUST | KD-13 |
| FR-AT-019 | Anti-chaos invariant 2: at least `MIN_SUPPORT_AGENTS [GT]` agents must be assigned `SUPPORT_BALL` or `HOLD_WIDTH` at all times when the pool size permits. | MUST | KD-13 |
| FR-AT-020 | Anti-chaos invariant 3: no `RUNNER` run-target position may be assigned whose distance-to-own-goal scalar places the target more than `OWN_HALF_RUN_BLOCK_M [GT]` past the half-line into own territory. | MUST | KD-13 |
| FR-AT-021 | All three anti-chaos invariants (FR-AT-018 / FR-AT-019 / FR-AT-020) are checked POST-assignment and PRE-publication on every tick that the algorithm runs. | MUST | KD-13 / §3.11 |
| FR-AT-022 | Assignment transitions use the dwell-time hysteresis pattern from Agent Movement #2 §3.1, parameterised by `ATTACK_DWELL_TICKS [GT]`. | MUST | KD-11 / §3.12 |
| FR-AT-023 | A role or run-target transition for an agent fires only after the new candidate has been continuously preferred for `ATTACK_DWELL_TICKS [GT]` consecutive ticks. | MUST | §3.12 |
| FR-AT-024 | Failure mode F1 — stale perception: if the perception snapshot `tickIndex` is less than `currentTick`, freeze and re-emit the previous tick's `AttackDirective` and `AttackIntent[]` verbatim without running the algorithm. | MUST | §2.4 |
| FR-AT-025 | Failure mode F2 — #12 slot unavailable: if `BaselineShape.GetSlot(agent)` returns `SENTINEL_NO_SLOT` for any agent in the pool, emit an empty `AttackDirective` for this tick without running the full algorithm. | MUST | §2.4 |
| FR-AT-026 | Failure mode F3 — anti-chaos invariants unresolvable: if the invariant-enforcement loop (§3.11) cannot produce a clean directive after `MAX_INVARIANT_PASSES [GT]` iterations, emit an all-default directive (all agents assigned `HOLD_WIDTH` or `SUPPORT_BALL`; no runners) and emit a `dev-log` warning `ATTACKING_INVARIANT_FALLBACK`. | MUST | §2.4 / KD-13 |
| FR-AT-027 | Failure mode F4 — phase unavailable from #12: if `PositioningAI.GetPhase(TeamId)` returns an error or is absent, treat the team phase as `OUT_OF_POSSESSION` (the safest fallback — suppresses all attacking computation). | MUST | §2.4 |
| FR-AT-028 | Every formula in §3 includes: units, valid input ranges (explicit bounds or assertions), and at least one worked numeric example either inline in §3 or in Appendix A. | MUST | CLAUDE.md |
| FR-AT-029 | Every constant in this spec carries exactly one tag: `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, or `[CROSS-PENDING]`. No constant may appear without a tag. | MUST | CLAUDE.md |
| FR-AT-030 | All constants live in a single catalogue file `AttackingAIConstants.cs`, organised into `#region` blocks per Code Standards #20 §4.2. No magic numbers appear in formula or algorithm code. | MUST | #20 FR-CS-025 / KD-14 |
| FR-AT-031 | No interface or accessor surface is produced against any consumer spec (including #8, #12, #13, #14) at Stage 0. | MUST | CLAUDE.md Interface Design Principle |
| FR-AT-032 | The fatigue input convention is `0.0 = fully rested`, `1.0 = fully fatigued`. Any inversion is a critical error and must be caught by the §5 test suite. | MUST | CLAUDE.md / KD-1 |
| FR-AT-033 | Stage-1 activation is gated on all three of: (a) ERR-015-002 resolved — `TacticalContext.AttackIntent[]?` field ratified in #8 §2.2.6; (b) Positioning AI #12 reaches `APPROVED`; (c) ERR-015-003 / ERR-015-004 — #17 channel rows for `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` landed. | MUST | KD-17 / §7 |
| FR-AT-034 | The dangerous-zone surrogate metric is declared and measurable: a shot is classified as a dangerous-zone shot when the ball position satisfies `distance(ball, opponentGoalCentre) ≤ DANGER_ZONE_MAX_DIST_M [GT]` AND `|ball.y − opponentGoalCentre.y| ≤ DANGER_ZONE_CORRIDOR_HW_M [GT]`. The count of such shots per match is the Stage-0-feasible chance-quality metric. | MUST | KD-10 / §5.7 |
| FR-AT-035 | Tactical-identity acceptance criteria are measurable without an xG model: the `DIRECT` profile MUST produce ≥ `DIRECT_RUN_COUNT_DELTA [GT]` more RUNNER assignments per simulated match than the `POSSESSION` profile (via `AttackIntent` role histograms); the `COUNTER_ATTACK` profile MUST produce ≥ `COUNTER_TRANSITION_SPEED_DELTA_TICKS [GT]` fewer `TRANSITION_HOLD_TICKS` on average (via directive emission log). | MUST | KD-10 / §5.8 |
| FR-AT-036 | `AttackDirective.overloadActive` is set to `true` and `AttackDirective.overloadFlank` is set to the appropriate value (LEFT or RIGHT) whenever the overload condition in FR-AT-016 is met. | MUST | §3.8 / FR-AT-016 |

---

## 2.2 Data Structures

All structures are **Stage 1 runtime** (spec authored at Stage 0). All fields
use the C# value-type / `readonly struct` convention per Code Standards #20
§4.2 unless marked mutable (state structs). Field types use C# conventions
(`float` = 32-bit IEEE 754, `int` = 32-bit signed, `bool`, `byte`).

### 2.2.1 `AttackDirective` (Stage 1; spec'd at Stage 0)

One instance per team per tick. Carries the team-level attacking coordination
parameters for this tick. Written by #15; read by the match orchestrator and
(at Stage 1) by #8 `TacticalContext.AttackIntent[]?` (once ERR-015-002 is
ratified). Contributes to the per-tick digest (FR-AT-004).

```csharp
readonly struct AttackDirective
{
    TeamId  team;
    bool    overloadActive;          // true when FR-AT-016 overload condition is met
    Flank   overloadFlank;           // LEFT | RIGHT; meaningful only when overloadActive
    int     transitionHoldTick;      // countdown ticks remaining on possession-loss hold;
                                     // 0 when IN_POSSESSION; decrements each tick in §3.9
}
```

`overloadFlank` is a spatial discriminator in the AI layer — it never enters
the physics pipeline. `Flank` is an AI-layer enum (LEFT / RIGHT); it is not a
`PatternType` or movement-pattern enum (KD-8). When `overloadActive` is
`false`, the value of `overloadFlank` is undefined and consumers must not
read it.

`transitionHoldTick` is set to `TRANSITION_HOLD_TICKS [GT]` on the tick that
possession is lost, decremented each tick by §3.9 (`TransitionController`),
and reset to 0 by §3.1 when `IN_POSSESSION` resumes. The COUNTER_ATTACK
profile sets `TRANSITION_HOLD_TICKS = 0` — possession-loss causes an
immediately empty directive.

### 2.2.2 `AttackIntent` (Stage 1; spec'd at Stage 0)

One instance per off-ball agent per tick. Carries the agent's current
attacking role and (where applicable) the run parameters for that tick.
Written by #15; read by the orchestrator and (at Stage 1) by #8 §3.1.7
(`MOVE_TO_POSITION`). Contributes to the per-tick digest (FR-AT-004).

```csharp
readonly struct AttackIntent
{
    EntityId        agent;
    AttackRole      role;              // RUNNER | SUPPORT_BALL | HOLD_WIDTH | WEAK_SIDE
    RunParameters?  runParameters;     // non-null only when role == RUNNER;
                                       // null otherwise (SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE)
    int             validThroughTick;  // equals currentTick; guard against stale reads
}
```

`role` uses a C# enum type that is internal to the AI layer and never
propagates into the physics pipeline (CLAUDE.md "Parameter-Based Physics").
`runParameters` is a nullable value type; consumers must null-check before
reading. `validThroughTick` is defensive hygiene: any consumer reading an
`AttackIntent` with `validThroughTick < currentTick` must treat it as stale
and fall back to the agent's #12 baseline slot.

### 2.2.3 `RunParameters` (Stage 1; spec'd at Stage 0)

Sub-struct of `AttackIntent`. Carried as a value type (embedded, not
referenced). This struct has **exactly three fields** (FR-AT-011). The run
angle is a derived quantity computed at use-site; it is not stored here.

```csharp
readonly struct RunParameters
{
    float   depthOffset_m;     // forward distance from ball carrier in teamAttackAngle direction
                                // units: metres; valid range: [5.0, 40.0] m (Clamp applied in §3.4)
    float   lateralOffset_m;   // perpendicular offset; positive = toward y=68 touchline
                                // when attacking x=105 goal; units: metres;
                                // valid range: [−34.0, 34.0] m (Clamp applied in §3.4)
    int     runTriggerTick;    // tick index at which the runner commits (starts moving);
                                // always >= currentTick + 1 (future only)
}
```

**Derived angle (computed at use-site only, never stored):**
```
runAngle_rad = atan2(lateralOffset_m, depthOffset_m)
```

This derivation fully parameterises any named attacking pattern in gameplay
vocabulary (KD-8). No `PatternType`, `RunType`, or `OverlapType` constant
is required anywhere in the codebase.

**Stage-0 note:** `RunParameters` describes a run target relative to the
ball carrier's current position. The orchestrator converts this to a pitch-
frame `Vector2` target position at Stage 1 by applying `teamAttackAngle`
(see §3.4). The conversion formula and worked example are in §3.4.

### 2.2.4 `AttackHysteresisState` (Stage 1; mutable, digested per KD-11)

One instance per off-ball agent (maintained across ticks in authoritative
simulation state). Tracks how long the agent has held its current
`AttackRole`; gates transitions per the #2 §3.1 dwell-time pattern (KD-11,
FR-AT-022).

```csharp
struct AttackHysteresisState
{
    AttackRole  currentRole;           // the role this agent is currently locked into
    int         dwellCounter;          // ticks the current role has been stably preferred;
                                       // isStable() fires when dwellCounter >= ATTACK_DWELL_TICKS;
                                       // increments each tick while currentRole is preferred;
                                       // resets to 0 on role transition
    AttackRole  candidateRole;         // new role being evaluated for transition
    int         candidateDwell;        // consecutive ticks the candidateRole has been preferred;
                                       // transition commits when candidateDwell >= ATTACK_DWELL_TICKS;
                                       // resets to 0 when candidateRole changes
}
```

`dwellCounter` accumulates (increments) while the same `candidateRole` continues
to equal `currentRole`. When a new `candidateRole` is preferred, `candidateDwell`
accumulates; when `candidateDwell >= ATTACK_DWELL_TICKS [GT]`, the transition
commits: `currentRole = candidateRole`, `dwellCounter = 0`, `candidateDwell = 0`.
`isStable()` checks `dwellCounter >= ATTACK_DWELL_TICKS`; while stable, the agent's
role is retained without re-evaluation. This state is authoritative simulation state
and is included in the per-tick digest (FR-AT-004).

Note: `prevPhase` (the team's possession phase from the previous tick) is stored in
`TransitionHoldState` (§2.2.5), not here — it is per-team state, not per-agent state.

### 2.2.5 `TransitionHoldState` (Stage 1; mutable, digested per KD-11)

One instance per team (maintained across ticks). Tracks the countdown
following a possession-loss event.

```csharp
struct TransitionHoldState
{
    int              transitionHoldTick;  // countdown; set to TRANSITION_HOLD_TICKS on first
                                          // possession-loss tick; decremented each tick by §3.9;
                                          // 0 when IN_POSSESSION or countdown expired
    PossessionPhase  prevPhase;           // the team's possession phase from the previous tick;
                                          // used in §3.13 to detect the first tick of a phase
                                          // transition (prevPhase == IN_POSSESSION and
                                          // currentPhase != IN_POSSESSION)
}
```

`transitionHoldTick` is SET on the first tick that phase transitions from
`IN_POSSESSION` to `TRANSITION` or `OUT_OF_POSSESSION` (§3.9 step 1), then
DECREMENTED each subsequent tick (§3.9 step 2). Reset to 0 by §3.1 when
`IN_POSSESSION` resumes. For the COUNTER_ATTACK profile, `TRANSITION_HOLD_TICKS
= 0` means the set-then-decrement logic immediately emits an empty directive.
`prevPhase` is written at the end of every tick in §3.13
(`transitionHoldState.prevPhase = phase`) and read at the start of the next
tick to detect possession-loss transitions. This state is authoritative
simulation state and included in the per-tick digest (FR-AT-004).

### 2.2.6 `AttackIntentSnapshot` (Stage 1; read-only view)

A read-only view over the current tick's `AttackDirective` and
`AttackIntent[]` array, exposed for `ATTACK_RUN_STARTED` /
`OVERLOAD_DECLARED` channel emission (#17, Stage 1) and for integration
tests (§5). The snapshot carries the same fields as `AttackDirective` plus
the full per-agent `AttackIntent[]` slice.

```csharp
readonly struct AttackIntentSnapshot
{
    AttackDirective     directive;          // team-level output for this tick
    ReadOnlySpan<AttackIntent> intents;    // per-agent outputs; EntityId-ascending
    int                 tickIndex;          // tick at which snapshot was captured
}
```

This struct is a zero-copy view over the authoritative arrays; it does not
own the memory it exposes. Channels are deferred to Stage 1 — see §7.5,
ERR-015-003, ERR-015-004.

### 2.2.7 `BaselineShapeView` (Stage 1; read-only, declared here as boundary)

A read-only view over #12's per-agent formation data, consumed by #15 at
tick start. Exposes `formationSlot` (type `Vector2` — baseline position),
`lineMembership` (type `LineMembership`), `lateralPct` (type `float`,
[0, 1]), and `laneAssignment` (type `LaneAssignment`). Also provides the
team-level `phase` enum. #15 never writes through this view.

This struct's concrete field surface is declared in Positioning AI #12
§2.2 and §4.5; #15 cites those sections via XC-015-010 (`formationSlot`
field names), XC-015-011 (`RunIntent` writer-layer), XC-015-012
(`GetPhase`), and XC-015-013 (`GetLine`). At Stage 0 no accessor code
exists (FR-AT-031). `SENTINEL_NO_SLOT` = `Vector2.NegativeInfinity` per
#12 §2.2 (distinct from NaN).

---

## 2.3 Inputs (Read-Only at Tick Start)

All inputs below are consumed as read-only values captured at the start of
the 10 Hz tactical tick. No mid-tick re-reads occur. If any input listed
here is unavailable or stale, the appropriate failure mode (§2.4) applies.

| Source | Field | Type | Notes |
|---|---|---|---|
| #7 Perception §3.7 | Per-agent positions | `Vector2[N]` | EntityId-keyed; includes all agents on both teams; #15 filters to possessing team excluding GK + ball carrier |
| #7 Perception §3.7 | Ball position | `Vector3` | Z not used by #15; X,Y used for support-radius, width-holding, weak-side, overload computations |
| #7 Perception §3.9 | Possession owner (ball carrier) | `EntityId?` | `null` for loose ball; if `null`, #15 treats as `OUT_OF_POSSESSION` (FR-AT-008) |
| #7 Perception §3.10 | Per-agent `isActive` | `bool` | Substituted / red-carded agents excluded from the pool before any computation |
| #7 Perception §3.7 | Per-agent `PlayerRole` | enum | Used to identify and exclude GK (`PlayerRole.Goalkeeper`, FR-AT-006) |
| #7 Perception §3.7–3.10 | Per-agent `Pace` attribute | `float` (normalised [0,1]) | Declared for use in future `runTriggerTick` calibration (Stage 1+); normalised as `(attr − 1) / 19` |
| #7 Perception §3.7–3.10 | Per-agent `Stamina` attribute | `float` (normalised [0,1]) | Declared for use in future fatigue-adjusted support radius (Stage 1+); normalised as `(attr − 1) / 19` |
| #7 Perception §3.7–3.10 | Per-agent `Dribbling` attribute | `float` (normalised [0,1]) | Declared for use in future RUNNER eligibility weighting (Stage 1+); normalised as `(attr − 1) / 19` |
| #12 Positioning AI (`BaselineShapeView`) | Per-agent `formationSlot` | `Vector2` | Baseline anchor position; used in §3.4 `lateralPct` derivation and §3.6 width-holding Y computation; `SENTINEL_NO_SLOT = Vector2.NegativeInfinity` triggers F2 |
| #12 Positioning AI (`BaselineShapeView`) | Per-agent `lineMembership` | `LineMembership` | `DEFENSE` / `MIDFIELD` / `ATTACK`; controls RUNNER eligibility in §3.3 (ATTACK or MIDFIELD only) |
| #12 Positioning AI (`BaselineShapeView`) | Per-agent `lateralPct` | `float` [0,1] | Fractional Y position within the pitch; used in §3.4 to compute `lateralOffset_m` |
| #12 Positioning AI (`BaselineShapeView`) | Per-agent `laneAssignment` | `LaneAssignment` | 5-bin: LEFT_WIDE / LEFT / CENTRE / RIGHT / RIGHT_WIDE; used in §3.6 to identify near-touchline agents for width-holding |
| #12 Positioning AI | Team phase enum | `Phase` | `IN_POSSESSION` / `OUT_OF_POSSESSION` / `TRANSITION`; phase gate in §3.1 (FR-AT-008); obtained via `PositioningAI.GetPhase(TeamId)` |
| Match configuration | `teamAttackAngle` | `float` (radians) | Match-half constant: `0.0 rad` for team attacking x=105; `π rad` for team attacking x=0. NOT ball-carrier velocity. Sourced from match configuration record, not the #7 snapshot. |
| #15-internal | Prior `AttackHysteresisState[N]` | struct array | From previous tick; drives hysteresis pre-check in §3.12 (FR-AT-022) |
| #15-internal | Prior `TransitionHoldState` | struct | From previous tick; drives §3.9 countdown logic (FR-AT-009) |

**Note on GK exclusion:** The GK's `EntityId` is identified via
`PlayerRole.Goalkeeper` from the #7 perception snapshot. #15 removes the
GK from the pool before any computation begins (FR-AT-006). The GK's
position is readable via the #7 snapshot, but the GK itself never enters
the `AttackIntent` output loop.

**Note on ball carrier exclusion:** The ball carrier's `EntityId` is read
from `perceptionSnapshot.possessionOwner`. When `possessionOwner` is
`null` (loose ball), #15 emits an empty directive (equivalent to
`OUT_OF_POSSESSION` treatment) because no coordinated off-ball movement
is meaningful without an identified carrier (FR-AT-007).

**Note on player attributes at Stage 0:** `Pace`, `Stamina`, and `Dribbling`
are declared as inputs now to establish the boundary surface; the Stage 0
algorithm in §3.3–§3.4 does not yet consume them (the formulas use constant
multipliers only). Stage 1+ will wire these attributes into eligibility
weighting and `runTriggerTick` calibration. No `[EST]` constants are
introduced here for that use; they are declared in §3 when the consuming
formula is defined.

---

## 2.4 Failure Modes and Recovery

Each failure mode below includes its detection condition, recovery action,
the `dev-log` warning token (if applicable), and a reference to the §5 test
that verifies the recovery. Test IDs are assigned in §5.

---

**F1 — Stale Perception Snapshot**

- **Detection:** `perceptionSnapshot.tickIndex < currentTick`.
- **Recovery:** Re-emit the previous tick's `AttackDirective` and entire
  `AttackIntent[]` verbatim. Do not invoke the role-assignment algorithm.
  Emit a `dev-log` warning with the stale delta: `ATTACKING_STALE_PERCEPTION
  delta=(currentTick − snapshot.tickIndex)`.
- **FR:** FR-AT-024.
- **Test:** T-AT-F1-STALE (unit test; §5.2).

---

**F2 — #12 Slot Unavailable**

- **Detection:** `BaselineShapeView.GetSlot(agent)` returns
  `SENTINEL_NO_SLOT` (`Vector2.NegativeInfinity`) for any agent in the
  initial off-ball pool (after GK and ball carrier have been excluded).
- **Recovery:** Emit an empty `AttackDirective` (`overloadActive = false`,
  `transitionHoldTick = 0`) for this tick without invoking the full
  algorithm. Do not emit partial `AttackIntent[]` — the absence of a
  baseline anchor for any agent makes the full assignment undefined. Emit
  a `dev-log` warning `ATTACKING_SLOT_SENTINEL agentId=<EntityId>`.
- **Rationale:** A missing baseline slot is always a #12 internal anomaly
  (e.g., substitution race condition). The safe recovery is a no-op rather
  than a partial assignment that could produce anti-chaos violations.
- **FR:** FR-AT-025.
- **Test:** T-AT-F2-SENTINEL (unit test; §5.2).

---

**F3 — Anti-Chaos Invariants Unresolvable**

- **Detection:** After the iterative demotion loop in §3.11, one or more
  of the three anti-chaos invariants remains violated, and no further
  demotion is available to fix the directive. This occurs when the pool is
  too small to simultaneously satisfy the MIN_SUPPORT_AGENTS floor and the
  WEAK_SIDE requirement, or when RUNNER candidates have all been demoted
  but invariant 2 still cannot be satisfied.
- **Recovery:** Discard the entire candidate role assignment. Emit an
  all-default `AttackDirective` and `AttackIntent[]` for this tick: all
  pool agents are assigned `HOLD_WIDTH` or `SUPPORT_BALL` (no RUNNER, no
  WEAK_SIDE) using the simple nearest-to-ball-carrier heuristic for
  SUPPORT_BALL distribution. Set `overloadActive = false`. Emit a `dev-log`
  warning `ATTACKING_INVARIANT_FALLBACK`.
- **FR:** FR-AT-026.
- **Test:** T-AT-F3-INVARIANT (unit test; §5.2).

---

**F4 — Phase Unavailable from #12**

- **Detection:** `PositioningAI.GetPhase(TeamId)` returns an error, throws,
  or returns a sentinel value not in {`IN_POSSESSION`, `OUT_OF_POSSESSION`,
  `TRANSITION`}.
- **Recovery:** Treat the team phase as `OUT_OF_POSSESSION` (safest fallback
  — suppresses all attacking computation and emits an empty directive). This
  is the same conservative-fallback pattern as #13 §2.4 F3 and #14 §2.4 F4.
  Emit a `dev-log` warning `ATTACKING_PHASE_UNAVAILABLE`.
- **Rationale:** Treating an unknown phase as `OUT_OF_POSSESSION` ensures
  #15 never fires its role-assignment algorithm in a state where the team
  may actually be in transition or under pressure — consistent with the
  "emit nothing rather than something wrong" principle used by #13 and #14.
- **FR:** FR-AT-027.
- **Test:** T-AT-F4-NOPHASE (unit test; §5.2).

---

**Additional invariant note — substituted and red-carded agents:**
Agents with `isActive = false` (per the #7 perception snapshot) are
filtered from the off-ball pool before any failure-mode logic runs. Their
last `AttackIntent` is not preserved (unlike F1 which preserves the full
previous directive); they simply cease to appear in the pool. No special
failure mode is required because the `isActive` filter is the first step
in §3.2 pool construction.

---

**Concurrent failure modes:** If F1 (stale perception) and F4 (phase
unavailable) both trigger simultaneously, F1 takes priority — re-emit the
frozen directive without consulting #12. This ordering ensures that the
most conservative response (freeze, no new computation) dominates when
multiple input sources are degraded.

---

## 2.5 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6) | Initial draft from `outline-detailed.md` v1.1. §2.1 (36 FRs FR-AT-001..036), §2.2 (7 structs with C# field tables), §2.3 (inputs table with 17 rows), §2.4 (F1–F4 failure modes with detection, recovery, dev-log token, test reference) authored. |
| 0.3 | May 18, 2026 | AI agent (claude-sonnet-4-6) | ERR-015-006 fix: promoted `[CROSS-PENDING]` in FR-AT-005 to `[CROSS: #16 §3.4]`. Resolves A-03 FAIL from stress-test Tier A run 1. |
| 0.2 | May 18, 2026 | AI agent (claude-sonnet-4-6) | Adversarial-review fixes: (1) §2.2.4 AttackHysteresisState: renamed `holdTicks`→`candidateDwell`; corrected `dwellCounter` description from "countdown/decrements" to accumulator (increments); moved `prevPhase` out (belongs in TransitionHoldState, not per-agent struct); (2) §2.2.5 TransitionHoldState: renamed `ticksRemaining`→`transitionHoldTick` (matches §3.9 algorithm); replaced `holdActive bool` with `prevPhase PossessionPhase` (per-team phase tracking used in §3.13 transition detection); (3) FR-AT-022, FR-AT-023: `[EST]`→`[GT]` for ATTACK_DWELL_TICKS (promoted at section-file draft); (4) §2.2.7 BaselineShapeView XC citation updated from stale XC-015-004 (#8 §1.7.3) to XC-015-010..013 (#12 §2.2 / §4.5). |
