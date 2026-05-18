# Attacking AI Specification #15 — Section 4: Architecture & Integration

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 4.1 Architecture Overview

`AttackingAI` is a single subsystem that runs on the **10 Hz tactical
scheduler**. It is a pure-function subsystem except for two authoritative
state stores: `AttackHysteresisState[N]` (per-agent dwell counters) and
`TransitionHoldState` (per-team countdown after possession loss). Both
stores are included in the per-tick digest per KD-11 / #16 §6.2.

**Stage binding:** Runtime activates at Stage 1 (see §1.8). Stage 0
deliverable = this specification only. No `src/AttackingAI/` code is
written until all 20 specs reach `APPROVED`.

The subsystem produces, per 10 Hz tick:
- One `AttackDirective` for the team currently in possession.
- One `AttackIntent` per off-ball agent (i.e., all outfield agents
  excluding the GK and ball carrier).

These outputs are consumed by the **orchestrator**, which:
1. Composes `AttackIntent.runTargetPosition` into the `RunIntent`
   writer-layer slot owned by #12 §4.5 (Stage 1+), before #8 reads
   the composed target position for its `MOVE_TO_POSITION` utility.
2. Makes `AttackIntent.role` visible to #8's `PASS` utility via the
   `TacticalContext.AttackIntent[]?` field extension (ERR-015-002,
   Option B — mirrors the `PressDirective?`/`MarkDirective?` precedent
   established by #13 / #14).

No 60 Hz per-frame work. All agent steering at 60 Hz is owned by
Agent Movement #2 via the action output of Decision Tree #8.

---

## 4.2 File Structure (Stage 1+; `#20 §4.2` compliant)

All source files below are **Stage 1+ deliverables**. They are named
here to establish the canonical module boundary and to confirm that
`AttackingAIConstants.cs` is the single constant catalogue per
FR-CS-025 / KD-14.

```
src/AttackingAI/                                     (Stage 1+ — all files)
├── AttackingAITick.cs           (10 Hz entry point; calls subsystems in §3.13 order)
├── AttackingPoolBuilder.cs      (§3.2 — GK and ball-carrier exclusion)
├── RoleAssigner.cs              (§3.3 role assignment + §3.4 RunParameters generation)
├── SupportHeuristic.cs          (§3.5 support radius computation)
├── WidthHolder.cs               (§3.6 width-holding protocol; TOUCHLINE_HOLD_DIST_M)
├── WeakSideController.cs        (§3.7 weak-side positioning)
├── OverloadDetector.cs          (§3.8 overload zone detection)
├── TransitionController.cs      (§3.9 transition-to-defense hold; SET-then-DECREMENT)
├── InvariantEnforcer.cs         (§3.11 anti-chaos invariants; demotion cascade)
├── AttackHysteresis.cs          (§3.12 dwell-time state; authoritative simulation state)
└── AttackingAIConstants.cs      (SINGLE constant catalogue per FR-CS-025 / KD-14)
```

**One catalogue rule (KD-14):** All constants (`[GT]`, `[DERIVED]`,
`[CROSS]`) live in `AttackingAIConstants.cs`. No constants in any other
`src/AttackingAI/` file. No magic numbers in formula code. This mirrors
the `DefensiveAIConstants.cs` / `PressingAIConstants.cs` conventions in
#14 / #13.

---

## 4.3 Internal Module Contracts

All inter-module data is passed as `readonly struct` parameters on the
10 Hz call stack. No `class` types on the hot path per Performance
Optimization Strategy #18 §3.7 zero-allocation rule.

| Module | Input | Output |
|---|---|---|
| `AttackingAITick` | Perception snapshot, #12 baseline slots, #12 phase, team config | `AttackDirective`, `AttackIntent[]` |
| `AttackingPoolBuilder` | Perception snapshot (agent list, ball carrier EntityId, GK EntityId via `PlayerRole.Goalkeeper`) | `AttackPool` (EntityId[], max 10) |
| `RoleAssigner` | `AttackPool`, #12 `formationSlot` per agent (`lineMembership`, `lateralPct`), `AttackHysteresisState[]`, `teamAttackAngle`, style-profile constants | `AttackIntent[]` (role + RunParameters?) |
| `SupportHeuristic` | Perception snapshot (ball carrier position), `AttackPool`, `SUPPORT_RADIUS_M × supportMult` | Per-agent SUPPORT_BALL eligibility bool[] |
| `WidthHolder` | `AttackIntent[]`, ball position, `TOUCHLINE_HOLD_DIST_M`, `MIN_WIDTH_HOLDERS` | `AttackIntent[]` (promotions applied) |
| `WeakSideController` | `AttackPool`, ball position, `WEAK_SIDE_FAR_Y_M`, `WEAK_SIDE_DEPTH_OFFSET_M`, `MIN_WEAK_SIDE_AGENT_THRESHOLD` | `EntityId?` (WEAK_SIDE assignee) |
| `OverloadDetector` | `AttackIntent[]`, ball position, `OVERLOAD_ZONE_WIDTH_M`, `OVERLOAD_COUNT` | `overloadActive bool`, `overloadFlank` (LEFT/RIGHT) |
| `TransitionController` | `prevPhase`, `currentPhase`, `TransitionHoldState` (mutable) | `AttackDirective` (frozen or empty) |
| `InvariantEnforcer` | `AttackIntent[]`, style-profile `MAX_RUNNERS`, `MIN_SUPPORT_AGENTS`, `OWN_HALF_RUN_BLOCK_M`, `HALF_LINE_X`, `MAX_INVARIANT_PASSES` | `AttackIntent[]` (demotion cascade applied) or all-default |
| `AttackHysteresis` | `AttackHysteresisState[]` (mutable), `RoleCandidate[]`, `ATTACK_DWELL_TICKS` | `AttackHysteresisState[]` (updated) |

---

## 4.4 Upstream Integration Contracts (read-only at tick start)

These are the data surfaces #15 reads. All are read-only; #15 never
writes to upstream data stores.

| Source | Data | Access Pattern | Stage |
|---|---|---|---|
| #7 Perception System §3.7–§3.10 | Perception snapshot: per-agent positions, ball position, ball carrier `EntityId`, `PlayerRole` (GK identification), attribute lookups (`Pace`, `Stamina`, `Dribbling`) | Read-only snapshot at 10 Hz tick start | Stage 0 (spec text); Stage 1 (runtime) |
| #12 Positioning AI §2.2 | Per-agent `formationSlot` (`lineMembership` `LineMembership`, `lateralPct` `float`) via `BaselineDefensiveShapeView` read-only view | Read-only per-agent lookup | Stage 1+ |
| #12 Positioning AI §4.5.1 | Per-team phase enum (`IN_POSSESSION` / `OUT_OF_POSSESSION` / `TRANSITION`) via `PositioningAI.GetPhase(TeamId)` | Read once per tick | Stage 1 |
| #12 Positioning AI §4.5.2 | Per-agent `LineMembership` via `PositioningAI.GetLine(EntityId)` (used for RUNNER eligibility in §3.3) | Read-only per-agent lookup | Stage 1 |
| Match configuration record | `teamAttackAngle` — match-half constant (`0.0 rad` for team attacking x=105; `π rad` for team attacking x=0) | Read-once at match init | Stage 0 (spec text) |

**Q4 resolution note:** GK is identified via `PlayerRole.Goalkeeper` from
the #7 perception snapshot. This is the same identification method used by
Pressing AI #13 (§3.2) and Defensive AI #14 (§3.2). No direct #11 accessor
needed or created (KD-7; Interface Design Principle).

**Q2 resolution note:** `formationSlot.lateralPct` is confirmed as a
`float` field (0–1) in #12 §2.2. `PositioningAI.GetLine(EntityId)` returns
`LineMembership` per #12 §4.5.2 v0.3 (elevated to Stage 1 via ERR-013-008,
May 17, 2026). Used in §3.3 for RUNNER eligibility check (`ATTACK` or
`MIDFIELD`) and in §3.6 for width-holding check (`laneAssignment` lateral
bin values `LEFT_WIDE` / `RIGHT_WIDE`).

---

## 4.5 Downstream Integration Contracts

### 4.5.1 To Decision Tree #8 (Stage 1+ — declared, not implemented)

`AttackIntent.role` informs #8's `PASS` utility at Stage 1: a `RUNNER`
agent is a higher-priority pass target than a `HOLD_WIDTH` agent, because
the runner is making a timed run into space. The specific mechanism is
**ERR-015-002** — back-prop to #8 §2.2.6 / §3.1.7:

**Mechanism selected (Option B):** Add `TacticalContext.AttackIntent[]?`
as a nullable array field on `TacticalContext`, mirroring the
`PressDirective?` nullable field established by Pressing AI #13 via
ERR-013-001 (May 17, 2026) and the `MarkDirective?` nullable field
established by Defensive AI #14 via ERR-014-001. A `null` value at Stage 0
(before #15 activates) is a well-formed signal to #8 that no coordinated
attacking intent is available.

At Stage 1, the orchestrator writes the `AttackIntent[]` array into
`TacticalContext` before the #8 action-selection loop runs for each
off-ball agent. #8's `MOVE_TO_POSITION` utility uses the `runTargetPosition`
from the relevant agent's `AttackIntent` (when role = `RUNNER`) as the
target, overriding the #12 baseline slot for that tick.

No interface text, no code stub, and no #8 amendment text lives in this
spec at Stage 0 (CLAUDE.md "Interface Design Principle"). The amendment
text for #8 §2.2.6 and §3.1.7 is filed as ERR-015-002 and will be
authored when #15 reaches APPROVED and Stage 1 implementation begins.

### 4.5.2 To Positioning AI #12 (Stage 1+ — declared, not implemented)

`AttackIntent.runTargetPosition` (for agents with role = `RUNNER`) is
written by the orchestrator into the `RunIntent` writer-layer slot declared
in #12 §4.5.2 as a Stage 1+ data structure. This is an **overlay** on the
#12 baseline slot — it is a temporary deviation for one tick, not a
permanent slot modification.

**Composition rule (per #12 §4.5 / KD-4):**
1. #12 publishes the baseline `formationSlot` per agent.
2. The orchestrator reads `AttackIntent` from #15 for each off-ball agent.
3. Where an agent has role = `RUNNER`, the orchestrator writes
   `RunIntent.targetPosition = AttackIntent.runParameters.runTargetPosition`
   into the #12 `RunIntent` slot for that agent.
4. #8's `MOVE_TO_POSITION` utility reads the composed result (baseline slot
   overridden by `RunIntent` where present).
5. Where #15 emits no `RunIntent` for an agent (role ≠ `RUNNER`), the agent
   defaults to its #12 baseline slot.

The `RunIntent` struct and its writer-layer mechanism belong to #12
(`positioning-ai/section-4.md` §4.5.2). #15 is the declared consumer of
that writer-layer slot; the interface is produced when both #12 and #15
reach `APPROVED` (Interface Design Principle).

### 4.5.3 AttackIntentSnapshot (read-only view)

`AttackIntentSnapshot` is a read-only projection of the `AttackIntent[]`
array, consumed by:
- #17 Event System channel emission at Stage 1 (`ATTACK_RUN_STARTED` /
  `OVERLOAD_DECLARED` transitions — ERR-015-003 / ERR-015-004).
- Test harnesses (§5 test catalogue).

The snapshot is produced by `AttackingAITick` immediately after
`AttackIntent[]` is published, before the per-tick digest is written.
No mutation after snapshot creation.

---

## 4.6 Determinism and Safety Boundaries (binding to #16)

All of the following are **authoritative simulation state** per #16 §3.2
and contribute to the per-tick digest per #16 §6.2:

| State | Scope | Digest contribution |
|---|---|---|
| `AttackDirective` | Per team, per tick | Full struct |
| `AttackIntent[]` | Per off-ball agent, per tick | Full array |
| `RunParameters[]` | Sub-struct of `AttackIntent[]` | Included via `AttackIntent[]` |
| `AttackHysteresisState[]` | Per agent, authoritative state | Dwell counters |
| `TransitionHoldState` | Per team, authoritative state | Countdown ticks remaining |

**Iteration order:** EntityId-ascending per #16 §3.2.5 (`XC-015-002` /
`XC-015-003`). All role-assignment loops, invariant-demotion loops, and
width-holding promotion loops iterate in this order. Tie-break on any
equal-priority decision is EntityId-ascending.

**RNG:** Any stochastic tie-breaking (e.g., two agents equidistant as
width-holder candidates) uses `DeterministicRngService` with domain tag
`DOMAIN_TAG_ATTACKING_AI = 0x1B` `[CROSS: #16 §3.4]` (ERR-015-001 resolved May 18, 2026; see
§1.3.3). No `System.Random`, no `DateTime.Now`, no frame-count sources
in the algorithm.

**Phase-change detection:** `prevPhase` is stored in `TransitionHoldState`
from the previous tick so that the `TransitionController` (§3.9) can
detect the IN_POSSESSION → TRANSITION change without querying #12 twice.
`prevPhase` is authoritative simulation state and contributes to the digest.

---

## 4.7 Cross-Specification Validation Checks

The following checks are enforced at Stage 1 build time (grep-based) and
at runtime (assertion-based) to catch class-level spec violations:

| Check | Method | Binding |
|---|---|---|
| No GK in attacking pool | Build-time grep: no code path in `AttackingPoolBuilder.cs` inserts an agent with `PlayerRole.Goalkeeper` | KD-7 / FR-AT-006 |
| Ball carrier excluded | Build-time grep: no code path in `AttackingPoolBuilder.cs` inserts the `ballCarrierEntityId` | KD-3 / FR-AT-007 |
| No `PatternType` / `RunType` / `OverlapType` enum | Build-time grep: `grep -r "PatternType\|RunType\|OverlapType" src/AttackingAI/` returns zero results | KD-8 / FR-AT-010 |
| No PASS/SHOOT/DRIBBLE calls | Build-time grep: `grep -r "ActionType.PASS\|ActionType.SHOOT\|ActionType.DRIBBLE" src/AttackingAI/` returns zero results | KD-3 / #8 boundary |
| Fatigue convention | Runtime assertion: any fatigue input read from #7 snapshot is in `[0.0, 1.0]`; `0.0 = fully rested` | KD-1 / CLAUDE.md / FR-AT-032 |
| EntityId no-reuse | Inherited from #2 §2.5 (`XC-015-002`) and #8 §1.7.3 (`XC-015-003`); no #15-specific mechanism needed | FR-AT-003 |
| RunParameters angle not stored | Build-time grep: `RelativeAngle` / `relativeAngle` absent from `RunParameters` struct | FR-AT-011 |
| Single constant catalogue | Build-time grep: no constant declarations in any file other than `AttackingAIConstants.cs` within `src/AttackingAI/` | KD-14 / FR-AT-030 |

---

## 4.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. §4.1–§4.8 authored. Upstream/downstream integration contracts declared. ERR-015-002 Option B selected (mirrors #13/#14 precedent). GK identification via `PlayerRole.Goalkeeper` confirmed (Q4). `formationSlot.lateralPct` confirmed in #12 §2.2 (Q2). All determinism bindings cited. |
| 0.3 | May 18, 2026 | AI agent (claude-sonnet-4-6) | ERR-015-006 fix: promoted `[CROSS-PENDING]` in §4.6 RNG description to `[CROSS: #16 §3.4]`. Resolves A-03 FAIL from stress-test Tier A run 1. |
