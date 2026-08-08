# Heading Mechanics Specification #10 — Section 2: Functional Requirements, Data Structures & Failure Modes

**Created:** May 16, 2026
**Version:** 0.3
**Status:** DRAFT
**Purpose:** Catalogue the functional requirements (`FR-HE-NNN`), the
data structures consumed and produced by the Heading Mechanics
pipeline, the expected failure modes with detection and recovery
rules, and the telemetry surface emitted on the trace pipeline.

---

## 2.1 Functional Requirements Catalogue

The table below enumerates the functional requirements that govern
#10's behaviour. Each FR cites the originating KD (§1.3) and the
target subsection in which the conformance is realized.

| ID | Statement | Conformance | Source KD | Target § |
|----|-----------|-------------|-----------|----------|
| FR-HE-001 | A header is eligible iff (a) the agent is in the Stage-0 #10-owned aerial phase (KD-18), (b) the predicted ball position at the contact frame lies inside `HEAD_CONTACT_VOLUME`, and (c) the predicted contact body part is `Head`. | MUST | KD-3 | §3.2 |
| FR-HE-002 | The contact-quality scalar is a continuous value in `[0, 1]`; no branching on `Early` / `OnTime` / `Late` labels is permitted inside §3.5–§3.7 formulas. | MUST | KD-2 | §3.4 |
| FR-HE-003 | No `HeaderType` / `HeaderClass` / `HeaderStyle` enum exists at any layer of the implementation. | MUST | KD-1 | §3.2–§3.8 |
| FR-HE-004 | `JumpReach` is `[DERIVED]`; no new `PlayerAttribute` is added to Agent Movement #2. | MUST | KD-4 | §3.3 |
| FR-HE-005 | Pass Mechanics #5 inputs are consumed via `BallState.{velocity, spin, position}` only; Pass-specific labels are NOT consumed. | MUST | KD-5 | §3.2, §4.2 |
| FR-HE-006 | A failed header attempt MUST NOT call `Ball.ApplyKick`; it MUST publish a `HeaderAttemptFailedEvent`. | MUST | KD-12 | §3.9 |
| FR-HE-007 | The own-goal-shaped trajectory flag is FLAGGED, NOT adjudicated. Goal-line crossing adjudication is owned by Event System #17 / Match Referee. | MUST | KD-6 | §3.8 |
| FR-HE-008 | All randomness MUST route through `DeterministicRngService` with a registered draw-site ID. | MUST | KD-10 | §3.4, §3.7, §4.4 |
| FR-HE-009 | Goalkeeper head contacts execute the #10 pipeline unchanged; only the upstream `HeaderIntent` source differs. | MUST | KD-7 | §3.2, §3.10 |
| FR-HE-010 | Contested-duel resolution consumes Collision System #3 contact data as-published; no #3 redefinition. | MUST | KD-8 | §3.7, §3.10 |
| FR-HE-011 | Fatigue convention is `0 = rested`, `1 = fatigued`. | MUST | KD-9 | §3.5 |
| FR-HE-012 | All positional inputs and outputs use the corner-origin coordinate system (Ball Physics #1 §1.2). | MUST | KD-9 | §3.2, §3.8 |
| FR-HE-013 | Tick-rate split: `HeaderIntent` is committed on the 10 Hz tactical loop; eligibility, jump kinematics, contact resolution and ball-velocity emission run on the 60 Hz physics loop. | MUST | KD-9 | §4.6 |
| FR-HE-014 | Every numeric constant published by #10 carries exactly one of `[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]`. | MUST | KD-11 | §3.1 |
| FR-HE-015 | Outgoing spin is computed by #10 §3.6 and passed to Ball Physics #1 via `Ball.ApplyKick(spin)`. | MUST | KD-16 | §3.6, §4.3 |
| FR-HE-016 | Set-piece headers (corner, free kick) are in Stage 0 scope; the kick itself is not. | MUST | KD-13 | §3.2, §7.5 |
| FR-HE-017 | Iteration over contested-duel participants follows Deterministic Simulation #16 §3.2 entity ordering. | SHOULD | KD-10 | §3.7 |
| FR-HE-018 | `targetIntent`, `powerIntent`, and `contactPointIntent` are locked at commit; `predictedContactFrame` is re-evaluated every physics tick until contact or attempt-window close. | MUST | KD-17 | §3.2 |
| FR-HE-019 | Stage 0 vertical-axis (Z) jump kinematics during the aerial-contact window are owned by #10 §3.3 (synthetic apex-altitude trajectory). | MUST | KD-18 | §3.3 |
| FR-HE-020 | Telemetry label for the centred quality bucket is `OnTime` (NOT `Perfect`). | MUST | KD-2 (pass-1 L-1) | §2.4, §3.4 |
| FR-HE-021 | `JumpReach` formula includes a `JUMP_REACH_K_HEADING · Heading_norm` term. | MUST | KD-4 (pass-1 H-2) | §3.3 |
| FR-HE-022 | The early-timing tolerance and late-timing tolerance are distinct `[GT]` constants (`MAX_EARLY_TOLERANCE_MS`, `MAX_LATE_TOLERANCE_MS`). | MUST | pass-1 H-1 | §3.1, §3.4 |
| FR-HE-023 | The contested-duel tiebreak perturbation is gated by `DUEL_TIEBREAK_EPSILON`; non-tie score gaps are NOT perturbed by RNG. | MUST | pass-1 H-5 | §3.7 |
| FR-HE-024 | The contact-quality scalar formula injects Gaussian noise via two registered draw sites (`DRAW_SITE_TIMING_JITTER`, `DRAW_SITE_CONTACT_POINT_ERROR`); no phantom draw sites exist. | MUST | KD-10 (pass-1 M-4) | §3.4, §4.4 |
| FR-HE-025 | Outgoing-velocity own-goal projection uses the dual horizon `min(time, distance)` of `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` and `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M`. | MUST | KD-6 (pass-1 L-7) | §3.8 |
| FR-HE-026 | A duel loser whose disturbance-adjusted `contactQualityScalar` falls below `MIN_CONTACT_QUALITY` emits `HeaderAttemptFailedEvent` (not a poor-quality `HeaderExecutedEvent`). | MUST | KD-8 | §3.7 |
| FR-HE-027 | Contested duels (2-way and 3+ way) emit exactly one full-quality `HeaderExecutedEvent` for the winner; each loser emits either a disturbed `HeaderExecutedEvent` (if `q' ≥ MIN_CONTACT_QUALITY`) or a `HeaderAttemptFailedEvent` (if `q' < MIN_CONTACT_QUALITY`) — semantics uniform across participant counts (v0.2 M-5). | MUST | KD-8 | §2.3 F-04, §3.7 |
| FR-HE-028 | `HeaderAttemptFailedEvent.failureCause` is one of `MistimedEarly` / `MistimedLate` / `PositionedPoorly` / `DisturbedInDuel`. | MUST | KD-12 | §2.2, §3.9 |
| FR-HE-029 | A `HeaderIntent` whose `targetIntent` lies outside the pitch bounding box is clamped to the nearest in-bounds point; a telemetry warning is emitted; the attempt is NOT failed on this basis alone. | MUST | KD-12 | §2.3 F-05 |
| FR-HE-030 | The `contactPointIntent` 2-D parameter is clamped to the head-local envelope; clamping incurs a `pointError` contribution but is NOT a hard failure. | MUST | KD-2 | §2.3 F-07, §3.4 |
| FR-HE-031 | `JumpReach` is computed once per jump phase, not per physics tick. | SHOULD | KD-4 | §3.3, §6 |
| FR-HE-032 | `headAngularVelocity` is derived locally from AM #2 `agent.facing` finite-difference; no AM #2 amendment is required at Stage 0. | MUST | KD-16 (pass-1 H-3) | §3.6, §7.9 |
| FR-HE-033 | A `BallState` snapshot older than one physics frame MUST be re-queried before contact resolution proceeds. | MUST | KD-12 | §2.3 F-06, §3.2 |
| FR-HE-034 | `WeakAerialSide` asymmetry, concussion accumulation, and bicycle-kick distinct kinematics are deferred to Stage 1+ per §7. | MUST | KD-14, KD-15 | §7.1, §7.2, §7.3 |
| FR-HE-035 | The Stage 0 per-tick steady-state cost budget is ≤80 µs; the p99 duel-frame tail budget is ≤180 µs. | MUST | pass-1 H-4 | §4.5, §6.1 |

---

## 2.2 Data Structures

All structures are struct-based and zero-allocation per CLAUDE.md
("When Writing Code"). Field ordering below is illustrative; final
ordering follows Spec #20 §3.x packing conventions at implementation
time.

### `HeaderIntent` (consumed)

Source: Decision Tree #8 at the 10 Hz tactical tick.

```
struct HeaderIntent {
    float    powerIntent;             // [0, 1]
    Vector2  contactPointIntent;      // head-local coordinates (m);
                                      // origin at head centre,
                                      // +x = agent.facing forward,
                                      // +y = agent-left lateral
    Vector3  targetIntent;            // corner-origin coordinates (m)
    int      attemptCommittedTick;    // 10 Hz tactical tick of commit;
                                      // consumed by §3.3 to derive
                                      // jumpStartFrame (v0.2 L-1 / M-3)
}
```

`targetIntent`, `powerIntent`, and `contactPointIntent` are locked
at commit per KD-17 / FR-HE-018; they are NOT re-issued by #8
mid-attempt.

### `HeaderContactState` (internal)

Per-frame internal structure during 60 Hz contact resolution.

```
struct HeaderContactState {
    int     jumpStartFrame;           // 60 Hz frame of ground exit;
                                      // written once per attempt by §4.6
                                      // (v0.2 M-3)
    int     predictedContactFrame;    // re-evaluated each 60 Hz tick
    int     idealContactFrame;        // per-call output of §3.2 (apex-aligned)
    int     actualContactFrame;       // set by §4.6 on the
                                      // currentFrame == predictedContactFrame
                                      // branch, before §3.4 (v0.2 M-4)
    float   timingOffsetMs;           // signed; positive = late
    Vector2 contactPointError;        // 2-D head-local error (m)
    float   contactQualityScalar;     // [0, 1]
    float   disturbanceFactor;        // [0, DUEL_DISTURBANCE_MAX]
}
```

### `HeaderExecutedEvent` (emitted)

Published on every successfully contacted header. Field set mirrors
Shot Mechanics #6 §4.5 `ShotExecutedEvent` for cross-spec uniformity.

```
struct HeaderExecutedEvent {
    int        agentId;
    float      matchTime;
    float      contactQualityScalar;       // [0, 1]
    ContactQualityLabel contactQualityLabel; // Early | OnTime | Late
    Vector2    contactPoint;               // head-local actual contact point
    BallState  incomingBallState;          // snapshot at contact frame
    Vector3    outgoingVelocity;
    Vector3    outgoingSpin;
    int?       contestedDuelId;            // null when uncontested
    bool       ownGoalShapedTrajectory;
    SetPieceContext? setPieceContext;      // OpenPlay | Corner | FreeKick (telemetry)
}
```

`contactQualityLabel` is telemetry only and is NEVER consumed by
the physics formula (KD-2 / FR-HE-002). `setPieceContext` is also
telemetry only; physics is uniform across delivery types per KD-13.
`OnTime` is the label for the centred bucket (KD-2 / pass-1 L-1 /
FR-HE-020).

### `HeaderAttemptFailedEvent` (emitted)

Published on every missed contact.

```
struct HeaderAttemptFailedEvent {
    int            agentId;
    float          matchTime;
    float          missDistanceM;
    float          timingOffsetMs;
    FailureCause   failureCause;       // MistimedEarly | MistimedLate
                                       // | PositionedPoorly | DisturbedInDuel
}
```

### `ContestedDuelContext` (internal)

Populated when ≥2 eligible agents share `HEAD_CONTACT_VOLUME`
simultaneously.

```
struct ContestedDuelContext {
    int                       duelId;
    ReadOnlySpan<int>         participantAgentIds;   // ordered per #16 §3.2
    int                       winnerAgentId;
    ReadOnlySpan<float>       disturbanceFactorByAgent;
}
```

---

## 2.3 Failure Modes

| ID | Failure | Detection | Recovery | Telemetry |
|----|---------|-----------|----------|-----------|
| F-01 | Mistimed jump — ball passed contact volume before jump apex. | `timingOffsetMs > MAX_LATE_TOLERANCE_MS` (or `< -MAX_EARLY_TOLERANCE_MS`). | `HeaderAttemptFailedEvent` per KD-12 / FR-HE-006. | `failureCause = MistimedLate` (resp. `MistimedEarly`); `heading.attempt.failed.cause` counter increment. |
| F-02 | Jump apex below ball altitude at predicted contact frame. | `JumpReach_m < ballZ(contactFrame)`. | `HeaderAttemptFailedEvent`. | `failureCause = PositionedPoorly`. |
| F-03 | Contact body part is `Head` but ball position is outside `HEAD_CONTACT_VOLUME` at every frame of the attempt window. | Eligibility predicate §3.2 returns `isEligible = false` for every frame. | `HeaderAttemptFailedEvent`. | `failureCause = PositionedPoorly`. |
| F-04 | Two or more simultaneously eligible headers (contested duel). | §3.2 predicate true for ≥2 agents at overlapping candidate frames. | Resolved per §3.7. Winner emits full-quality `HeaderExecutedEvent`. Each loser emits either a disturbed `HeaderExecutedEvent` (if `q' ≥ MIN_CONTACT_QUALITY`) or `HeaderAttemptFailedEvent` with `failureCause = DisturbedInDuel` (if `q' < MIN_CONTACT_QUALITY`). Uniform across 2-way and 3+ way duels (v0.2 M-5 / FR-HE-027). | `heading.duel.outcome` counter. |
| F-05 | Decision Tree #8 supplied a `targetIntent` outside the pitch bounding box. | `!pitchBoundingBox.Contains(targetIntent)`. | Clamp to nearest in-bounds point per FR-HE-029; emit telemetry warning. NOT a hard failure. | Warning channel; no `HeaderAttemptFailedEvent`. |
| F-06 | `BallState` snapshot stale (>1 physics frame old). | `currentFrame - ballState.snapshotFrame > 1`. | Re-query Ball Physics #1; do NOT extrapolate. | Diagnostic channel; no `HeaderAttemptFailedEvent`. |
| F-07 | `contactPointIntent` outside head-local coordinate envelope. | Distance to envelope edge > 0. | Clamp to envelope edge; the clamp delta contributes to `pointError` in §3.4 (so the quality penalty arises naturally from the formula). | No standalone telemetry beyond the existing `contactQualityScalar` histogram. |

---

## 2.4 Telemetry Surface

The trace pipeline channel-registry **schema** is owned by
Performance Optimization #18 Appendix F.0 (Stage 0 deliverable;
populated subsystem-channel rows are explicitly Stage 0+1 per
#18 Appendix F.0 / §3.8.2 / §7.2). The five `heading.*` rows
below are declared here (Stage 0 spec deliverable) and populated
into the registry at first `src/Gameplay/Heading/` commit
(Stage 0+1 deliverable). v0.3 OI-002 closure: reframed against
#18's actual schedule (v0.1 / v0.2 incorrectly cited #18 §3.10,
which is the constants catalogue not the channel registry).

| Channel | Type | Buckets / Notes |
|---------|------|-----------------|
| `heading.contact.quality.scalar` | Histogram | Continuous `[0, 1]` value of `contactQualityScalar`. |
| `heading.contact.quality.label` | Counter | Three buckets: `Early` / `OnTime` / `Late` (KD-2 / FR-HE-020 / pass-1 L-1). |
| `heading.duel.outcome` | Counter | Three buckets: `Win` / `Loss` / `Disturbed`. |
| `heading.attempt.failed.cause` | Counter | Four buckets: `MistimedEarly` / `MistimedLate` / `PositionedPoorly` / `DisturbedInDuel`. |
| `heading.own_goal_shaped.flag` | Counter | Boolean flag per `HeaderExecutedEvent`; emitted on `true`. |

All channels are determinism-aware per Performance Optimization #18
KD-6 (#18 §6.4): trace emission must not perturb game state.

---

## 2.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | section authoring | Initial draft from `outline-detailed.md` v1.1. FR catalogue covers FR-HE-001..035; structs and failure modes enumerated. | pending |
| 0.2 | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: FR-HE-027 rewritten for uniform 2-way/3+ way loser semantics (M-5); F-04 row prose rewritten to match; `HeaderIntent.attemptCommittedTick` documented as consumed by §3.3 `jumpStartFrame` derivation (L-1); `HeaderContactState` adds `jumpStartFrame` field (M-3) and documents `actualContactFrame` assignment site (M-4); `contactPointIntent` head-local axis convention pinned (L-7). | pending |
| 0.3 | May 16, 2026 | drafter | APPROVAL. §2.4 trace-pipeline ownership re-framed from non-existent "#18 §3.10 channel rows" to #18 Appendix F.0 channel-registry schema (Stage 0 schema + Stage 0+1 populated rows). OI-002 RESOLVED. | granted |
