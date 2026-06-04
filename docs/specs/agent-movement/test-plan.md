# Agent Movement Specification — Test Plan

**Created:** June 4, 2026
**Status:** Draft (Stage 0+1 regression anchor)
**Authoring spec:** Agent Movement #2
**Coverage tier:** Tier A (deterministic) per Deterministic Simulation #16 §1.1.1
**Test framework:** NUnit (Unity Test Runner, EditMode) per Code Standards #20 / Testing Strategy #19 §7.5 D2

---

## 1. PURPOSE AND SCOPE

Agent Movement #2 §5 is a *performance* analysis section, not a test plan; §6/§7/§9
do not enumerate tests either. The placeholder `AgentMovementTests.cs` (created
2026-05-26) cited a non-existent "§5.1 with 85 test scenarios" — that section was a
fiction. This document is the **authoritative** test catalogue for the Agent Movement
assembly until the Spec #2 body absorbs a §5.5 Test Plan.

The initial roster is **regression-anchored**: every test ID below names a specific
adversarial-review (AR) finding that produced an observable bug, hand-tracked through
the AR series for that fix. The point is to lock the fix in executable form so a
future refactor cannot silently re-introduce the bug.

The plan is open-ended — additional T-AM-IDs should be appended (not renumbered) as
new coverage is authored. Cross-spec test plans (Collision System #3 ↔ Agent Movement
collision interaction, First Touch #4 ↔ Agent Movement possession transition) belong
to the **consuming** spec, not this one.

---

## 2. TEST ID CONVENTION

`T-AM-NNN` — three-digit zero-padded, allocated in order of authoring. Once allocated,
**never reused** even if the test is deleted; tombstone the ID with a `RETIRED:`
note inline so callers can grep for the prior anchor.

Blocks are grouped by the file under test, not by the AR ordinal, so a new fix lands
next to its sibling assertions rather than at the end of the file.

| Block | Range | File under test |
|---|---|---|
| Dwell-formula unit tests | T-AM-001..009 | `AgentStateMachine.CalculateGroundedDwell` |
| Pipeline collision integration | T-AM-010..029 | `AgentMovementSystem.Update` (Step 3) |
| Safety-override integration | T-AM-030..039 | `AgentMovementSystem.Update` (Step 10/11) |
| OscillationGuard | T-AM-040..049 | `OscillationGuard.RecordAndCheck` |
| State-machine pure logic | T-AM-050..069 | `AgentStateMachine.EvaluateFromX` |
| Future (locomotion, turning, fatigue) | T-AM-070..099 | reserved |

---

## 3. TEST ROSTER

Each row records: ID, file under test, AR anchor, scenario summary, regression
hazard. Detailed assertion code lives in the test source — this table is the index.

### 3.1 Dwell-formula unit tests — `AgentStateMachine.CalculateGroundedDwell`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-001 | AR-9 M-1 | Default attrs (balance=10, strength=10), `COLLISION`, force=1.0 → dwell = 2.0 s | If formula constants drift, max-force dwell collapses. |
| T-AM-002 | AR-9 M-1 | Default attrs, `COLLISION`, force=0.0 → dwell = 1.3 s (= base × CollisionDwellMin) | Pins the force=0 floor so AR-9 M-1 has a numeric anchor. |
| T-AM-003 | AR-2 H-1 | Default attrs, `SLIDING_TACKLE`, force=1.0 → dwell = base × SlidingTackleDwellMult, clamped | Verifies reason multiplier reaches the formula (AR-2 H-1 was the gap). |
| T-AM-004 | AR-2 L-2 | Min attrs (balance=1, strength=1) → dwell clamped at `GroundedDwellClampMax` | Prevents float division explosion when attribute denom approaches zero. |
| T-AM-005 | — | Max attrs (balance=20, strength=20) → dwell clamped at `GroundedDwellClampMin` | Prevents elite players being effectively immune to grounding. |
| T-AM-006 | AR-8 L-2 | `collisionForce` > 1.0 forwarded to formula → `Clamp01` floors `forceScale` at 1.0 | Cache writes also clamp (see T-AM-014); defence-in-depth. |

### 3.2 Pipeline collision integration — `AgentMovementSystem.Update` Step 3

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-010 | AR-9 M-1 | Entry frame: `isCollisionKnockdown=true`, force=1.0 → `state.CurrentState=GROUNDED`, `state.CollisionForce=1.0`, `state.GroundedReason=COLLISION` | Cache write contract — every downstream test depends on it. |
| T-AM-011 | AR-9 M-1 | Dwell frames: after entry-frame at force=1.0, run 90 frames (1.5 s) with `isCollisionKnockdown=false`, incoming `collisionForce=0` → still GROUNDED | **PRIMARY AR-9 M-1 regression lock.** If `EvaluateState` is passed the incoming `collisionForce` instead of `state.CollisionForce`, dwell collapses to ~1.3 s and this releases by frame 78. |
| T-AM-012 | AR-9 M-1 | After T-AM-011, run another 36 frames (to ~2.1 s total) → released to IDLE | Anchors the upper bound so T-AM-011 cannot be satisfied by an infinite-dwell regression. |
| T-AM-013 | AR-5 M-2 | Enter GROUNDED at force=0.5, advance 20 frames, deliver second knockdown at force=1.0 → `state.CollisionForce=1.0`, `state.TimeInState=0` | If second-hit refresh regresses, the second impulse is silently dropped and dwell rides out the first hit's lower force. |
| T-AM-014 | AR-8 L-2 | Knockdown delivered with `collisionForce=2.0` → `state.CollisionForce=1.0` (Clamp01 on cache write) | Both transition branch (line ~136) and refresh branch (line ~176) clamp; second-hit case at force=2.0 also clamps. |
| T-AM-015 | AR-6 M-1 | GROUNDED dwell expires on the same frame a fresh collision arrives → next-frame state is GROUNDED (refreshed), **not** a one-frame IDLE flicker | AR-6 M-1 dropped the `current != GROUNDED` guard on knockdown short-circuit; verifies no IDLE frame appears mid-knockdown. |
| T-AM-016 | AR-6 M-2 | Lock `OscillationGuard` via 7 fast transitions, then deliver knockdown → transition to GROUNDED bypasses the guard | If guard bypass regresses, a knockdown that follows a flap sequence is delayed by `LockDuration`. |
| T-AM-017 | AR-7 M-1 | After T-AM-016 transitions and dwell expires, the post-recovery `GROUNDED→IDLE` transition completes immediately | Without `OscillationGuard.Initialize()` on the collision branch, the stale lock blocks the recovery transition. |
| T-AM-018 | AR-3 R3-M-1 | After dwell expires and transition to IDLE fires → `state.GroundedReason=NONE`, `state.CollisionForce=0.0` | Restores the field invariant "GroundedReason == NONE when CurrentState != GROUNDED". |

### 3.3 Safety-override integration — `AgentMovementSystem.Update` Steps 10/11

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-030 | AR-5 M-1 | Override command + pre-corrupted `state.Velocity = (NaN, 0)` → `LastValidPosition / LastValidVelocity / LastValidFacing` unchanged across the frame | **PRIMARY AR-5 M-1 regression lock.** Tooling-injected NaN must not poison the recovery cache, or the agent is stuck in a permanent recovery loop the moment override flips off. |
| T-AM-031 | AR-7 M-2 | Override command + pre-corrupted `state.Velocity = (NaN, 0)` → `state.Speed` preserved from prior frame (not assigned `NaN`) | If `state.Speed = Velocity.magnitude` escapes the validity gate, next-frame `EvaluateState` runs with NaN speed and silently flips to arbitrary states. |
| T-AM-032 | AR-5 M-1 | Override command + finite valid trajectory → `LastValid*` and `Speed` updated to current values | Verifies the gate is not too aggressive — happy-path tooling sessions must still refresh the cache. |
| T-AM-033 | AR-4 M-5 | Non-override (normal) frame + pre-corrupted `state.Position = (NaN, 0)` → after frame, `state.Position == state.LastValidPosition` (recovery snap) and `recovered` path taken (so `LastValid*` NOT overwritten with the post-recovery values) | Verifies the non-override recovery path also preserves the cache so subsequent NaN frames keep snapping to the same anchor. |

### 3.4 OscillationGuard unit tests — `OscillationGuard.RecordAndCheck`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-040 | — | Fresh `Initialize()` + single transition → not blocked | Sentinel `NegativeInfinity` initialisation; covers the AR-1 false-positive-at-t=0 hazard. |
| T-AM-041 | — | 7 transitions across 0.6 s (> `MaxTransitionsPerSecond`) → 7th call returns `true` (locked) | Lock activation; consumed by T-AM-016 setup. |
| T-AM-042 | AR-4 M-2 | After lock fires, a transition during the lock window returns `true` | Lock window enforcement. |
| T-AM-043 | AR-4 M-2 | After `LockDuration` elapses and ring buffer was reset on lock entry, the next transition returns `false` (no indefinite re-lock) | Closes the AR-4 M-2 indefinite-lockout corner case — pre-lock timestamps could keep `recentCount > 6` after the lock window expired without the ring-buffer reset. |

---

## 4. NON-COVERAGE (NAMED)

The following surfaces are **deliberately not covered** by this roster. Each line
records the reason and the issue that opens coverage:

- **Locomotion acceleration / deceleration formulas (§3.2.3–§3.2.5).** No AR finding
  has produced a bug here; coverage opens when the first AR-N reports a locomotion
  defect or when Stage 1 lands the §5.5 Test Plan.
- **Turn-rate / lean-angle formulas (§3.4).** Same rationale.
- **Fatigue accumulation (§3.1.3 table).** AR review has not flagged a fatigue
  regression. Coverage opens with the first dual-energy spec edit.
- **`UpdateAllAgents` goalkeeper-skip / array-length validation.** Currently asserted
  via `Debug.Assert` in dev builds (AR-5 L-2 / AR-8 M-1). Promote to NUnit coverage
  when the §5.5 Test Plan defines the assert-vs-test boundary.
- **Cross-spec collision integration (Collision System #3 producing the
  `isCollisionKnockdown` signal).** Owned by Spec #3's test plan, not here.

---

## 5. DETERMINISM AND FRAMEWORK NOTES

- All tests **must be deterministic**. No `System.Random`, no `DateTime.Now`, no
  `Time.deltaTime` (FR-CS-036 / FR-CS-042). Time inputs are explicit `float t`
  accumulators.
- Default tick rate: 60 Hz physics, `dt = 1.0f / 60.0f`. Pipeline tests construct
  `AgentMovementSystem(60.0f)`.
- Tooling-only `MovementCommand` (override-safety branch) is constructed via the
  `internal static MovementCommand.ToolingOverrideOnly_NaNInjection(...)` factory.
  The test assembly is granted access via `InternalsVisibleTo` in
  `src/agent-movement/AssemblyInfo.cs`. **Production game logic MUST NOT call this
  factory** — it is a regression-test seam only.
- Floating-point assertions use `Assert.AreEqual(expected, actual, tolerance)` with
  an explicit tolerance. Default tolerance for dwell-time assertions: `0.001f`. For
  per-frame integration assertions, tolerance scales with `dt` (`dt × eps`).

---

## 6. VERSION HISTORY

| Version | Date       | Author | Notes                                                                                                |
|---------|------------|--------|------------------------------------------------------------------------------------------------------|
| 0.1     | 2026-06-04 | —      | Initial regression-anchored roster (T-AM-001..018, 030..033, 040..043). Locks AR-3 R3-M-1, AR-4 M-2, AR-4 M-5, AR-5 M-1, AR-5 M-2, AR-6 M-1, AR-6 M-2, AR-7 M-1, AR-7 M-2, AR-8 L-2, AR-9 M-1. |
