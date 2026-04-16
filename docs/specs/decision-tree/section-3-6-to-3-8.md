## 3.6 PerceptionSnapshot Intake Interface

### 3.6.1 Interface Definition

This section provides the authoritative definition of `ReceiveSnapshot()`, resolving
the interface that Perception System Specification #7, §4.5.3 explicitly deferred to
this specification.

```csharp
// ============================================================
// File: DecisionTree.cs
// Method: ReceiveSnapshot
// Called by: SimulationLoopOrchestrator — once per agent per heartbeat tick,
//            after the Perception System batch completes for that heartbeat.
// NOT a Unity event or message bus subscription.
// NOT a coroutine. Synchronous execution required.
// ============================================================

/// <summary>
/// Receives a PerceptionSnapshot for this agent and runs the full
/// 6-step Decision Tree pipeline synchronously.
/// </summary>
/// <param name="snapshot">
/// The PerceptionSnapshot produced by the Perception System for this agent
/// this heartbeat tick. Passed BY VALUE — DT receives a copy, not a reference.
/// The snapshot is valid only for the duration of this method call. The DT
/// must not cache a reference or retain any ReadOnlySpan fields from the
/// snapshot after this method returns.
/// </param>
public void ReceiveSnapshot(PerceptionSnapshot snapshot);
```

**Key properties of this interface:**

- **Direct method call.** No event bus, no subscription, no shared buffer (OQ-1 resolution,
  §1.4 and Outline §OQ-1). The orchestrator calls this method explicitly for each agent in
  ascending `AgentId` order, after all 22 Perception System evaluations complete for the
  tick. This is KD-7 (§1.4) — sequential evaluation order.

- **Pass by value.** `PerceptionSnapshot` is a value struct. The DT receives its own copy.
  No two agents' DT evaluations share snapshot memory. This resolves AR-3 (outline):
  `ReadOnlySpan<PerceivedAgent>` lifetime concerns are eliminated because the DT works
  on a copy, not on a span into the Perception System's buffer.

- **Synchronous, single-frame return.** The entire 6-step pipeline (Steps 1–6, §2.1.2)
  executes inside this method call. `DispatchAction()` (§3.5) is called before the method
  returns. The orchestrator does not advance to the next agent until `ReceiveSnapshot()`
  returns.

- **No return value.** The method has no return value. Dispatch side effects (movement
  commands, Pass/Shot request submission) are the observable outputs. `DecisionMadeEvent`
  (§2.2.7) is published internally.

---

### 3.6.2 Delivery Contract

The orchestrator must satisfy the following constraints when calling `ReceiveSnapshot()`:

1. **Ordering:** Perception System completes all 22 agent snapshots for tick N before
   any `ReceiveSnapshot()` call for tick N is made. No agent's DT evaluation begins
   until all Perception evaluations for that tick are done. This is KD-1 and KD-7.

2. **Per-tick delivery:** Each agent receives exactly one `ReceiveSnapshot()` call per
   heartbeat tick. The sole exception is forced refresh (§3.6.3).

3. **Agent ID ordering:** Agents are called in ascending `AgentId` order (0 through 21).
   This guarantees determinism — combined with the per-option noise hash (§3.3.3), any
   given match seed and tick number produces an identical 22-agent decision sequence.

4. **Frame counter:** `DecisionContext.CurrentFrame` must be set to the orchestrator's
   current simulation frame before each `ReceiveSnapshot()` call. The frame counter is
   monotonically increasing across the simulation lifetime.

---

### 3.6.3 Forced Refresh Handling

Perception System §4.5.2 warns that forced-refresh snapshots may arrive outside the
normal 10Hz heartbeat cadence — e.g., immediately after a goal, a corner kick set piece,
or a tackle. The DT must accept these without error.

**Handling rule:**

- A forced-refresh snapshot for agent N uses the same `ReceiveSnapshot()` method
  signature. No separate interface exists for forced refresh.
- On forced refresh, the DT re-evaluates **that agent only**. The other 21 agents are
  not re-evaluated.
- If the agent is currently in `EXECUTING` state (a dispatched action is in progress),
  the forced refresh does not cancel the in-flight execution. The DT evaluates fresh
  options, compares the selected action with the in-flight action, and dispatches a new
  command only if the selected action differs.
- If the in-flight action and the re-evaluated action are identical `ActionType`, the DT
  does not re-dispatch. Redundant dispatch of the same action type is suppressed to
  prevent thrashing in execution systems.

**Frame counter on forced refresh:** The orchestrator must use the current simulation
frame number when calling `ReceiveSnapshot()` for a forced refresh, not the tick-boundary
frame number. This ensures the noise hash in §3.3.3 produces a distinct result for the
forced refresh evaluation.

---

### 3.6.4 Snapshot Lifetime Constraint

This constraint restates AR-3 from the outline as an implementation requirement.

> **The DT must not retain any reference to snapshot data beyond the scope of
> `ReceiveSnapshot()`.** All data needed for the pipeline must be copied into
> `DecisionContext` (Step 2, §2.1.2) during the method call. After `ReceiveSnapshot()`
> returns, the snapshot copy is invalid from the DT's perspective.

**Rationale:** Even though the snapshot is passed by value (§3.6.1), it may contain
`ReadOnlySpan<T>` fields pointing into the Perception System's per-tick buffer.
Retaining a reference to the `PerceptionSnapshot` struct does not extend the lifetime
of those spans. After the Perception System tick buffer is reused for the next tick,
any retained span reference becomes a dangling reference — a silent data corruption.

**Enforcement:** The §5 integration tests include a test (IT-DT-AR3) that verifies the
DT does not store any `PerceptionSnapshot` field in a class-level variable accessible
outside `ReceiveSnapshot()`. This must be a static analysis check in CI, not just a
runtime test.

---

## 3.7 State Machine

### 3.7.1 States

The Decision Tree maintains a per-agent state machine. Each of the 22 agents has its own
independent `DtState` value; no shared state machine exists across agents.

| State | Meaning | Entry Condition |
|---|---|---|
| `IDLE` | No action in progress. Agent awaits the next heartbeat. | Initial state; post-action-completion; post-interrupt recovery. |
| `EVALUATING` | Pipeline running. Steps 1–6 executing inside `ReceiveSnapshot()`. | Heartbeat tick received; `ReceiveSnapshot()` called by orchestrator. |
| `EXECUTING` | Action dispatched. Execution system is performing the action. | `DispatchAction()` completed successfully (Step 6 returned). |
| `INTERRUPTED` | Execution cancelled by external event. Transitioning to IDLE. | `TackleContactEvent` or forced-refresh-with-new-action during EXECUTING. |

**IDLE vs EXECUTING distinction:** An agent in `IDLE` has no dispatched action. An agent
in `EXECUTING` has a dispatched action that the execution system is running (e.g., pass
windup in progress). At Stage 0, HOLD and MOVE_TO_POSITION are continuous — they are
re-dispatched every heartbeat tick. PASS and SHOOT have windup/contact phases that span
multiple simulation frames; the DT remains in EXECUTING across those frames.

---

### 3.7.2 Transition Table

| From | To | Trigger | Guard | Action on Transition |
|---|---|---|---|---|
| `IDLE` | `EVALUATING` | Heartbeat tick received (`ReceiveSnapshot()` called) | None | Begin Step 1 of pipeline |
| `EVALUATING` | `EXECUTING` | `DispatchAction()` completes (Step 6 returns) | Action dispatched to an execution system | Publish `DecisionMadeEvent`; record `DispatchedActionType` |
| `EVALUATING` | `IDLE` | `DispatchAction()` produces HOLD (no execution system engaged) | HOLD selected or fallback-to-HOLD (FR-08) | Publish `DecisionMadeEvent` with `FallbackToHold = true` if applicable |
| `EXECUTING` | `IDLE` | Execution system confirms action complete | Completion signal received (stub at Stage 0) | Clear `DispatchedActionType` |
| `EXECUTING` | `IDLE` | Next heartbeat tick — continuous actions (HOLD, MOVE) | `ActionType` in {HOLD, MOVE_TO_POSITION} | Re-dispatch same or new action type; re-enter EVALUATING then EXECUTING |
| `EXECUTING` | `INTERRUPTED` | `TackleContactEvent` received from Collision System | Agent is in EXECUTING and tackle flag set | Cancel in-flight action signal to execution system |
| `EXECUTING` | `INTERRUPTED` | Forced refresh produces different ActionType | Forced refresh re-evaluates to different action | Cancel in-flight action; prepare new dispatch |
| `INTERRUPTED` | `IDLE` | One frame elapsed after interrupt | — | Clear interrupt flag; clear `DispatchedActionType` |

**Continuous action re-dispatch note:** HOLD and MOVE_TO_POSITION are logically
continuous but architecturally single-tick. Every heartbeat, an agent selecting HOLD
transitions EVALUATING → EXECUTING → IDLE → EVALUATING in sequence. This appears as
a rapid cycle but is correct — the agent re-evaluates each tick and re-issues the
movement command. The execution system (Agent Movement) receives a repeated HOLD command
each tick, which is idempotent from its perspective.

PASS and SHOOT are not re-dispatched on each tick. Once PASS enters WINDUP state in
Pass Mechanics, the DT is in EXECUTING until the execution system signals completion
(or the DT receives an interrupt). The DT does not re-evaluate during PASS execution
unless a forced refresh arrives.

---

### 3.7.3 Transition Invariants

1. **No direct IDLE → EXECUTING transition.** Every dispatch must pass through EVALUATING.
   EVALUATING is the only state in which pipeline logic runs.

2. **INTERRUPTED has a one-frame lifetime.** The DT never remains in INTERRUPTED for more
   than one simulation frame. INTERRUPTED → IDLE is automatic; no external signal is
   required. This ensures the agent returns to evaluation within one frame of any interrupt.

3. **No simultaneous multi-state condition.** Each agent has exactly one `DtState` at
   any moment. The state machine is fully deterministic given the same input sequence.

4. **EVALUATING is not externally visible.** External systems (Collision System, Pass
   Mechanics) do not query DT state. The only externally relevant states are IDLE and
   EXECUTING: IDLE means "no dispatched action" and EXECUTING means "action in progress."
   EVALUATING and INTERRUPTED are internal transient states.

---

### 3.7.4 State Machine Diagram

```
                    ┌─────────────────────────────────────────────┐
                    │         Heartbeat tick received              │
                    │       (ReceiveSnapshot() called)             │
                    ▼                                              │
              ┌──────────┐                                         │
   ──────────►│   IDLE   │◄────────────────────────────────────────┤
   (initial   └──────────┘   Execution complete / HOLD dispatched  │
    state)         │                                               │
                   │ [always]                                      │
                   ▼                                               │
            ┌────────────┐                                         │
            │ EVALUATING │                                         │
            └────────────┘                                         │
                   │                                               │
          ┌────────┴────────┐                                      │
          │                 │                                      │
     [HOLD/fallback]   [PASS/SHOOT/DRIBBLE/                        │
          │             MOVE/PRESS/INTERCEPT]                      │
          │                 │                                      │
          ▼                 ▼                                      │
        IDLE◄──────── ┌──────────┐ ◄── Tackle / forced refresh ───┤
                      │EXECUTING │                                  │
                      └──────────┘                                 │
                            │                                      │
                     [Tackle / forced                              │
                      refresh w/ new                              │
                      ActionType]                                  │
                            │                                      │
                            ▼                                      │
                     ┌─────────────┐                               │
                     │ INTERRUPTED │──── (1 frame) ───────────────►┘
                     └─────────────┘
```

---

## 3.8 Edge Cases

### 3.8.1 Edge Case Catalogue

The following edge cases define expected Decision Tree behaviour under abnormal but
foreseeable conditions. Each must be handled without requiring external intervention.

---

**EC-DT-01 — Agent has possession but no visible teammates and not in shooting range.**

> Condition: `DecisionContext.HasPossession = true`;
> `PerceptionSnapshot.VisibleTeammateCount = 0`; `GoalOpeningScore < SHOOT_MIN_THRESHOLD` [GT].
>
> Expected behaviour: `GenerateOptions()` §3.1 generates DRIBBLE and HOLD candidates
> (possession-available actions that do not require a visible target). PASS and SHOOT
> are not generated (no teammates / goal not viable). DRIBBLE utility depends on
> `SpaceScore`; HOLD utility is always generated at `BaseUtility = 0.25`. The agent
> dribbles if space exists, holds if under pressure.
>
> This is not a failure mode. A striker isolated at the back with no open passing option
> should hold or dribble. The utility model produces this outcome without special handling.

---

**EC-DT-02 — Agent in penalty box with ball; goal visible.**

> Condition: `FieldZone == ATTACKING`; `HasPossession = true`; goal visible in snapshot;
> `DistanceToGoal ≤ PENALTY_BOX_DIST` [GT] ≈ 18m.
>
> Expected behaviour: SHOOT `BaseUtility` uses the `ATTACKING` zone multiplier (1.0 in
> zone table, §3.2). `GoalOpeningScore` is high if goalkeeper is not well-positioned.
> SHOOT utility should dominate unless pressure is very high (`P ≥ 0.8`) and a clear
> pass lane exists (high-Vision teammate in space). This is the intended behaviour —
> agents in the box should shoot.
>
> Validation: Balance test BAL-DT-02 (§5.7) verifies that under A_Finishing ≥ 12,
> A_Composure ≥ 10, `GoalOpeningScore ≥ 0.6`, and `P ≤ 0.4`, SHOOT is selected as the
> highest-utility action in ≥ 80% of simulated heartbeats.

---

**EC-DT-03 — All opponents out of visible range (agent perceives no opponents).**

> Condition: `PerceptionSnapshot.VisibleOpponentCount = 0`.
>
> Expected behaviour: `PressureScalar P = 0.0` (no visible pressure). Risk penalties
> are minimised. MOVE_TO_POSITION urgency based on formation slot distance dominates
> for out-of-possession agents (no pressing opportunity exists with zero visible
> opponents). For in-possession agents, PASS and DRIBBLE risk penalties drop to their
> minimums, making progressive actions more viable.
>
> This scenario occurs legitimately for deep midfielders in wide formations or agents
> near the pitch boundary. No special handling required — the utility model responds
> naturally to P = 0.

---

**EC-DT-04 — `PerceptionSnapshot.BallVisible = false`.**

> Condition: Agent cannot perceive the ball (outside field of view; occluded).
>
> Expected behaviour: All possession-dependent actions (PASS, SHOOT, DRIBBLE, HOLD,
> INTERCEPT) are either not generated (PASS/SHOOT/DRIBBLE — require active possession
> or ball visibility) or scored at minimum utility (INTERCEPT — no intercept point
> derivable). MOVE_TO_POSITION and PRESS are not ball-dependent. The agent falls back
> to MOVE_TO_POSITION or PRESS depending on `PossessionState` and pressing instruction.
>
> This is correct: an agent who cannot see the ball should reposition, not attempt to
> intercept or pass. The utility model handles this without special-case branching.

---

**EC-DT-05 — Forced refresh snapshot arrives mid-heartbeat (agent already EXECUTING).**

> Condition: `ReceiveSnapshot()` is called by the orchestrator while the agent is in
> EXECUTING state (pass windup in progress; forced refresh triggered by goal or set piece).
>
> Expected behaviour: per §3.6.3 — DT re-evaluates. If the re-evaluated action is a
> different `ActionType` than the in-flight action: transition to INTERRUPTED, cancel
> in-flight execution, dispatch new action. If the re-evaluated action is the same
> `ActionType`: suppress re-dispatch; continue current execution.
>
> The in-flight execution cancellation signal is a documented responsibility of this
> specification. The mechanism (method call vs. shared flag) is an implementation
> detail deferred to §4.

---

**EC-DT-06 — Two agents both select PRESS on the same opponent (same-tick).**

> Condition: Agents A and B both evaluate at the same heartbeat tick and both select
> PRESS targeting opponent C.
>
> Expected behaviour: Both agents issue `MovementCommand` PRESS commands targeting
> opponent C's last-known position. Agent Movement (#2) and the Collision System (#3)
> are responsible for resolving physical convergence on the same space. The Decision
> Tree does not attempt to coordinate PRESS assignments between agents — this is
> explicitly out of scope at Stage 0 (Formation System, Stage 1+).
>
> This produces occasional double-pressing which is realistic: real teams sometimes send
> two players to the same ball. Stage 1 Formation System will add press-assignment
> coordination.

---

**EC-DT-07 — Agent with `Composure = 1` under maximum pressure (P = 1.0).**

> Condition: Lowest possible Composure attribute (1/20); PressureScalar `P = 1.0`.
>
> Expected behaviour: `ComposureNoiseBound` reaches its maximum (NOISE_MAX = 0.20,
> §3.3.4). `EffectiveUtility` values are significantly perturbed from `ScoredUtility`.
> The agent may select a suboptimal action — for example, SHOOT from a low-probability
> position instead of PASS to an unmarked teammate.
>
> This is **correct behaviour, not a bug** (outline §3.8 and KD-3 §1.4). Low-Composure
> agents under extreme pressure are supposed to make poor decisions. The noise model
> exists to produce this differentiation. If the outcome appears pathological (SHOOT
> from own half), balance tests BAL-DT-03 and BAL-DT-04 (§5.7) will catch it by
> verifying that AR-4 noise bounds prevent catastrophic choices even at minimum Composure.

---

**EC-DT-08 — `GenerateOptions()` returns zero candidates.**

> Condition: All seven action types fail their availability gates in §3.1.
>
> Expected behaviour: FR-08 fallback (§2.3.4) — produce `AgentAction` with
> `ActionType = HOLD`, `BaseUtility = 0.0`. Log as failure mode FM-DT-08.
> Publish `DecisionMadeEvent` with `FallbackToHold = true`.
>
> This should not occur in normal play — HOLD and MOVE_TO_POSITION are always generated
> by §3.1 (they have no hard availability gate). Zero candidates requires HOLD generation
> to also fail, which requires `HasPossession = false` AND `PossessionState == OWN_TEAM`
> (the only condition under which HOLD is suppressed). This combination should not arise
> from valid match state. If FM-DT-08 fires in play, it indicates a bug in §3.1 option
> generation, not a valid edge case. It must be investigated and logged.

---

### 3.8.2 Edge Cases Deliberately Not Handled

The following scenarios are explicitly outside Decision Tree scope at Stage 0. They are
listed to prevent implementers from attempting to handle them here.

| Scenario | Owner | Stage |
|---|---|---|
| Press assignment coordination (no two agents pressing same target) | Formation System Spec (Stage 1) | Stage 1 |
| Goalkeeper specialist decision logic (dive, punch, distribute) | Goalkeeper Mechanics Spec (Stage 1) | Stage 1 |
| Set piece execution (corner, free kick, throw-in) | Set Piece System (Stage 2) | Stage 2 |
| Off-ball run scheduling (overlap, third-man, underlap) | Formation System / Run Coordination (Stage 1) | Stage 1 |
| Communication between agents (signalling for the ball) | Agent Communication System (Stage 2) | Stage 2 |
| Fatigue-based decision degradation beyond Composure model | Agent Attributes Extension (Stage 3) | Stage 3 |

---

## Section 3 Completion Summary

With §3.4 through §3.8 delivered, Section 3 of Decision Tree Specification #8 is now
complete. The full six-step pipeline is specified:

| Step | Location | Status |
|---|---|---|
| Step 1 — Validate snapshot | §3.1 (Section 3.1 v1.1) | ✅ Complete |
| Step 2 — Assemble DecisionContext | §2.1.2, §2.2.4 (Section 2 v1.1) | ✅ Complete |
| Step 3 — Generate options | §3.1 (Section 3.1 v1.1) | ✅ Complete |
| Step 4 — Score options | §3.2 (Section 3.2 v1.2) + §3.4 (this file) | ✅ Complete |
| Step 5 — Select action | §3.3 (Section 3.3 v1.0) | ✅ Complete |
| Step 6 — Dispatch action | §3.5 (this file) | ✅ Complete |
| Intake interface | §3.6 (this file) | ✅ Complete |
| State machine | §3.7 (this file) | ✅ Complete |
| Edge cases | §3.8 (this file) | ✅ Complete |

**New items logged:**
- ERR-011: `PassRequest.AgentID` (uppercase) vs `ShotRequest.AgentId` (mixed case) —
  naming inconsistency. Non-blocking. Should be harmonised before implementation.
- XC-3.5-10: `MovementController.SubmitCommand()` method name requires verification
  against Agent Movement §4 before §3.5 approval.

**Constant count (this file):** 16 §3.4 tactical constants + 7 §3.5 dispatch/movement
constants (`URGENCY_PRESSURE_SCALE`, `SPIN_INTENT_BELOW_CENTRE`, `SPIN_INTENT_OFF_CENTRE`,
`PLACEMENT_CORNER_OFFSET`, `MOVE_SPRINT_THRESHOLD`, `MOVE_JOG_THRESHOLD`, and
`PRESS_URGENCY_FACTOR` already counted in §3.4) = **23 new [GT] constants**, all in
`TacticalWeights.cs` or `UtilityWeights.cs` as indicated in §3.4.7.

**Next:** Section 4 — Architecture and Integration.

---

*End of Section 3.4 through 3.8 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*  
*v1.0 — March 05, 2026, 12:00 PM PST*
