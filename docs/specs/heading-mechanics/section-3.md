# Heading Mechanics Specification #10 — Section 3: Core Formulas, Algorithms, Pseudocode

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Define every formula, predicate, and algorithm that the
Heading Mechanics pipeline executes — eligibility, jump kinematics,
contact-quality scalar, power/launch-angle generation, spin transfer,
contested duel resolution, own-goal-shape flagging, failed-attempt
handling, and boundary algorithms against neighbouring specs.

---

## 3.1 Master Physical Profile Table

Every numeric constant published by this specification appears in
the table below (KD-11 / FR-HE-014). Every symbol that appears in
§3.2–§3.8 pseudocode bodies is either present here with a source
tag, or explicitly named as a per-call output / local variable in
the relevant subsection.

Candidate values are illustrative pre-tuning anchors. Values
flagged `TBD-VALUE` await Stage 0 calibration; values flagged
`TBD-ALLOC` await an upstream allocation (see Outstanding column).

| Constant | Source Tag | Unit | Valid Range | Candidate | Citation / Notes |
|----------|-----------|------|-------------|-----------|------------------|
| `HEAD_CONTACT_VOLUME_RADIUS_M` | `[GT]` | m | [0.10, 0.25] | 0.18 | Effective radius around the head centre that admits ball contact. |
| `HEAD_CONTACT_VOLUME_HEIGHT_M` | `[GT]` | m | [0.10, 0.30] | 0.22 | Vertical extent of the contact volume. |
| `MAX_EARLY_TOLERANCE_MS` | `[GT]` | ms | [50, 200] | 140 | Earliest allowable signed timing offset (pass-1 H-1). |
| `MAX_LATE_TOLERANCE_MS` | `[GT]` | ms | [30, 140] | 90 | Latest allowable signed timing offset. Numerically smaller than `MAX_EARLY_TOLERANCE_MS` reflecting that late headers degrade faster than early ones (pass-1 H-1). |
| `EARLY_LABEL_THRESHOLD_MS` | `[GT]` | ms | [10, 80] | 40 | Telemetry-bucket boundary; NOT a formula gate per KD-2. |
| `LATE_LABEL_THRESHOLD_MS` | `[GT]` | ms | [10, 80] | 40 | Telemetry-bucket boundary; NOT a formula gate per KD-2. |
| `TIMING_POINT_BLEND_ALPHA` | `[GT]` | dimensionless | (0, 1) | 0.55 | The `α` weight on `timingQuality` in the §3.4 convex combination. |
| `MIN_CONTACT_QUALITY` | `[GT]` | dimensionless | (0, 0.5] | 0.20 | §3.7 cutoff below which a duel loser emits `HeaderAttemptFailedEvent` instead of a poor-quality `HeaderExecutedEvent`. |
| `FRAME_MS` | `[DERIVED]` | ms | n/a | ≈16.67 | `FRAME_MS = 1000 / TICK_RATE_PHYSICS_HZ`. |
| `JUMP_REACH_BASE_M` | `[FIXED]` | m | n/a | 2.20 | Anatomical baseline: average standing head-height plus typical no-effort reach. |
| `JUMP_REACH_K_STRENGTH` | `[GT]` | m | [0.0, 0.5] | 0.18 | Sensitivity of `JumpReach` to `Strength_norm`. |
| `JUMP_REACH_K_BALANCE` | `[GT]` | m | [0.0, 0.4] | 0.10 | Sensitivity of `JumpReach` to `Balance_norm`. |
| `JUMP_REACH_K_HEADING` | `[GT]` | m | [0.0, 0.4] | 0.12 | Sensitivity of `JumpReach` to `Heading_norm` (pass-1 H-2). Covers jump-timing skill until §7.10. |
| `JUMP_PHASE_DURATION_MS` | `[GT]` | ms | [400, 900] | 650 | Total ground-to-ground aerial-phase length for Stage 0 synthetic trajectory (KD-18). |
| `JUMP_APEX_FRACTION` | `[GT]` | dimensionless | (0, 1) | 0.50 | Apex location along the jump phase as a fraction of `JUMP_PHASE_DURATION_MS`. `[GT]` not `[FIXED]` because the Stage 0 trajectory is synthetic per KD-18, not physical. |
| `POWER_BASE_MPS` | `[GT]` | m/s | [4, 12] | 7.0 | Baseline header outgoing speed. |
| `POWER_K_STRENGTH` | `[GT]` | m/s | [0, 8] | 4.0 | Strength contribution to outgoing speed. |
| `POWER_K_HEADING` | `[GT]` | m/s | [0, 8] | 5.0 | Heading-attribute contribution to outgoing speed. |
| `POWER_FATIGUE_COEFF` | `[GT]` | dimensionless | [0, 0.5] | 0.18 | Fatigue penalty coefficient (0 = no degradation; CLAUDE.md fatigue convention). |
| `CONTACT_POINT_ERROR_SIGMA_M` | `[GT]` | m | (0, 0.10] | 0.03 | Mean point-error scale; baseline denominator for `pointQuality`. |
| `CONTACT_POINT_NOISE_SIGMA_M` | `[GT]` | m | [0, 0.05] | 0.012 | Amplitude of per-attempt point-error Gaussian noise via `DRAW_SITE_CONTACT_POINT_ERROR` (pass-1 M-4). |
| `TIMING_JITTER_SIGMA_MS` | `[GT]` | ms | [0, 30] | 8 | Amplitude of per-attempt timing-noise Gaussian via `DRAW_SITE_TIMING_JITTER` (pass-1 M-4). |
| `CONTACT_POINT_HEADING_ATTR_COEFF` | `[GT]` | dimensionless | [0, 1] | 0.40 | Heading-attribute scaling of `CONTACT_POINT_ERROR_SIGMA_M`. |
| `SPIN_TRANSFER_COEFF` | `[GT]` | dimensionless | [0, 1] | 0.55 | Multiplier on derived `headAngularVelocity` contribution to outgoing spin. |
| `SPIN_PRESERVATION_BASE` | `[GT]` | dimensionless | (0, 1] | 0.60 | Scale-factor base for `spinPreservationFactor` (see §3.6 formula). |
| `SPIN_TRANSFER_REVERSAL_THRESHOLD` | `[GT]` | m | (0, 0.04] | 0.015 | Contact-point axial offset beyond which `spinPreservationFactor` goes negative (spin reverses). |
| `DUEL_BALANCE_WEIGHT` | `[GT]` | dimensionless | [0, 1] | 0.30 | `w_B` in §3.7 score formula. |
| `DUEL_STRENGTH_WEIGHT` | `[GT]` | dimensionless | [0, 1] | 0.35 | `w_S` in §3.7. |
| `DUEL_HEADING_WEIGHT` | `[GT]` | dimensionless | [0, 1] | 0.35 | `w_H` in §3.7. Sum of three weights = 1.0 by construction. |
| `DUEL_TIEBREAK_EPSILON` | `[GT]` | dimensionless | (0, 0.10] | 0.02 | Near-tie threshold gating RNG perturbation (pass-1 H-5). |
| `DUEL_TIEBREAK_NOISE_AMPLITUDE` | `[GT]` | dimensionless | (0, 0.10] | 0.01 | RNG perturbation amplitude applied only when score gap < `DUEL_TIEBREAK_EPSILON` (pass-1 H-5). |
| `DUEL_DISTURBANCE_MAX` | `[GT]` | dimensionless | (0, 1] | 0.50 | Maximum disturbance factor applied to a duel loser's `contactQualityScalar`. |
| `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` | `[GT]` | s | (0, 5] | 1.2 | Projection time horizon for own-goal-shape flag. |
| `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M` | `[GT]` | m | (0, 60] | 18 | Projection distance horizon (pass-1 L-7). Flag invocation uses `min(time, distance)`. |
| `GRAVITY_MPS2` | `[CROSS]` | m/s² | n/a | 9.81 | Ball Physics #1. |
| `PITCH_LENGTH_M` | `[CROSS]` | m | n/a | 105 | Ball Physics #1 §1.2. |
| `PITCH_WIDTH_M` | `[CROSS]` | m | n/a | 68 | Ball Physics #1 §1.2. |
| `DOMAIN_TAG_HEADING` | `[CROSS-PENDING]` | byte | n/a | `0x16` (TBD-ALLOC) | Deterministic Simulation #16 §3.4 — back-prop ERR-010-001; allocation slot per #17 `0x15` precedent. Promoted to `[CROSS]` atomically with #16 §3.4 patch landing. |
| `TICK_RATE_TACTICAL_HZ` | `[CROSS]` | Hz | n/a | 10 | CLAUDE.md. |
| `TICK_RATE_PHYSICS_HZ` | `[CROSS]` | Hz | n/a | 60 | CLAUDE.md. |

### Removed in v0.1 (formerly in outline v1.0)

- `IDEAL_CONTACT_FRAME_OFFSET` — relocated to §3.2 as a per-call
  output of the eligibility predicate (pass-1 M-2). Not a
  project-level constant.
- `GLANCING_ANGLE_THRESHOLD_RAD` — no caller; deferred to §7.11 as
  a Stage 1+ telemetry-classifier concern (pass-1 L-3).

---

## 3.2 Eligibility Predicate

**Inputs.** Agent kinematic state (Agent Movement #2 §3.5.1);
`BallState` snapshot (Ball Physics #1) at the current 60 Hz tick;
`HeaderIntent` (Decision Tree #8) latched at
`attemptCommittedTick`; the Stage 0 #10-owned synthetic jump
trajectory (KD-18, §3.3).

**Outputs.** `bool isEligible`, `int predictedContactFrame`,
`int idealContactFrame` (the apex-aligned target frame against
which `timingOffsetMs` is measured in §3.4 — a per-call output, not
a project constant; relocated from §3.1 per pass-1 M-2).

### Pseudocode

```
function EligibilityPredicate(agent, ball, intent, currentFrame):
    // (1) Aerial-phase check (KD-18). Stage 0 aerial phase is owned by #10.
    //     AM #2 ground state must be exitable (not GROUNDED / STUMBLING).
    if agent.movementState in { GROUNDED, STUMBLING }: return (false, -, -)

    // (2) BallState freshness (F-06 / FR-HE-033).
    if currentFrame - ball.snapshotFrame > 1:
        ball = BallPhysics.GetBallState(currentTime)   // re-query

    // (3) Predict ball trajectory through attempt window;
    //     compute candidate contact frame (intersection of ball
    //     path with HEAD_CONTACT_VOLUME around agent head position).
    apexFrame  = computeJumpApexFrame(agent, currentFrame)   // §3.3
    idealContactFrame = apexFrame

    predictedContactFrame =
        intersectBallPathWithContactVolume(ball, agent, apexFrame,
                                            HEAD_CONTACT_VOLUME_RADIUS_M,
                                            HEAD_CONTACT_VOLUME_HEIGHT_M)
    if predictedContactFrame is None: return (false, -, idealContactFrame)

    // (4) Body-part check (KD-3). Head is unconditional for the head zone.
    if contactBodyPart(agent, predictedContactFrame) != Head:
        return (false, predictedContactFrame, idealContactFrame)

    // (5) Intent-staleness handling (KD-17 / FR-HE-018) — pass-1 M-5.
    framesEarlyTolerance = MAX_EARLY_TOLERANCE_MS / FRAME_MS
    framesLateTolerance  = MAX_LATE_TOLERANCE_MS  / FRAME_MS
    if predictedContactFrame < idealContactFrame - framesEarlyTolerance:
        emitFailedAttempt(agent, MistimedEarly); return (false, …)
    if predictedContactFrame > idealContactFrame + framesLateTolerance:
        emitFailedAttempt(agent, MistimedLate);  return (false, …)

    return (true, predictedContactFrame, idealContactFrame)
```

`targetIntent`, `powerIntent`, and `contactPointIntent` are held
fixed after commit (KD-17 (a), (c)). Only
`predictedContactFrame` is re-evaluated each 60 Hz tick.

### Worked Example — Corner Cross

Corner cross delivered at 8 m/s incoming horizontal speed. Defender
commits header at 10 Hz tick `T`. Initial prediction: contact at
frame `T+9`. `idealContactFrame = T+9`. On each subsequent physics
tick `T+1`, `T+2`, …, the predicate re-evaluates. At `T+5` the
ball deflects off another defender (Collision System #3 contact
event mid-flight); the re-prediction returns `T+14`, which exceeds
`T+9 + framesLateTolerance` (= `T+9 + 5` at `MAX_LATE_TOLERANCE_MS
= 90`). `HeaderAttemptFailedEvent { failureCause: MistimedLate }`
is emitted.

---

## 3.3 Jump Kinematics Integration

### `JumpReach` (KD-4, FR-HE-021, pass-1 H-2)

```
JumpReach_m = JUMP_REACH_BASE_M
            + JUMP_REACH_K_STRENGTH · Strength_norm
            + JUMP_REACH_K_BALANCE  · Balance_norm
            + JUMP_REACH_K_HEADING  · Heading_norm
```

where `*_norm = attribute / 100` (attributes are integers in
[0, 100] per AM #2 §3.5.6). Cited as `FM-010-001` (§8.4).

### Stage 0 Synthetic Jump Trajectory (KD-18, FR-HE-019)

AM #2 §3.6 does not publish Z>0 kinematics. The Stage 0 trajectory
is synthesized inside #10:

```
phase_t            = (currentFrame - jumpStartFrame) · FRAME_MS
apexFrame          = jumpStartFrame + round(JUMP_PHASE_DURATION_MS
                                             · JUMP_APEX_FRACTION
                                             / FRAME_MS)
totalPhaseFrames   = round(JUMP_PHASE_DURATION_MS / FRAME_MS)

// Parabolic interpolation peaking at apexFrame with peak JumpReach_m.
u                  = (currentFrame - jumpStartFrame) / totalPhaseFrames
agentHeadZ(frame)  = JumpReach_m · 4 · u · (1 - u)
                     // standing height baseline absorbed into JumpReach_m
```

The aerial phase exits on `currentFrame > jumpStartFrame +
totalPhaseFrames`. Landing transitions the agent back to AM #2
ground state. If the attempt used a diving-header posture, the
agent enters `GROUNDED` with
`GroundedReason = DIVING_HEADER` per AM #2 §3.1.2 — the only
documented use of this enum value at Stage 0.

At Stage 1+, when AM #2 grows native Z kinematics (§7.8), this
synthetic trajectory retires; the surface shifts to reading AM #2
apex-frame `agentZ` and adding the anatomical head-above-COM
offset. Until then, `JumpReach` is the source-of-truth.

Per FR-HE-031, `JumpReach` is computed once per jump phase, not
per physics tick.

Worked example with sensitivity analysis: see Appendix B.1.

---

## 3.4 Contact-Quality Scalar (KD-2, FR-HE-002)

### Asymmetric Timing Tolerance (pass-1 H-1)

Late headers degrade faster than early ones, so the early/late
tolerances are separate `[GT]` constants and the formula is
piecewise.

```
timingJitterMs    = TIMING_JITTER_SIGMA_MS
                  · rng.NextGaussian(DRAW_SITE_TIMING_JITTER)
timingOffsetMs    = (actualContactFrame - idealContactFrame) · FRAME_MS
                  + timingJitterMs

if timingOffsetMs <= 0:
    timingQuality = 1 - clamp01(-timingOffsetMs / MAX_EARLY_TOLERANCE_MS)
else:
    timingQuality = 1 - clamp01( timingOffsetMs / MAX_LATE_TOLERANCE_MS)

pointNoiseM       = CONTACT_POINT_NOISE_SIGMA_M
                  · rng.NextGaussian(DRAW_SITE_CONTACT_POINT_ERROR)
pointError        = ||contactPointActual - contactPointIntent|| + pointNoiseM
pointQuality      = 1 - clamp01(pointError /
                               (CONTACT_POINT_ERROR_SIGMA_M
                                · headingAttrScale(agent)))

contactQualityScalar = TIMING_POINT_BLEND_ALPHA · timingQuality
                     + (1 - TIMING_POINT_BLEND_ALPHA) · pointQuality
```

where:

```
headingAttrScale(agent) = 1 + CONTACT_POINT_HEADING_ATTR_COEFF
                            · (Heading_norm - 0.5)
```

Higher Heading attribute tightens the point-error distribution
(centred on 0.5 → unit scale). Cited as `FM-010-002` (§8.4).

### RNG Draw-Site Wiring (KD-10, FR-HE-024, pass-1 M-4)

Both Gaussian draws above route through #16's `NextGaussian` API
with registered draw-site IDs `DRAW_SITE_TIMING_JITTER` and
`DRAW_SITE_CONTACT_POINT_ERROR` (declared in §4.4). No phantom
draw sites exist.

### Telemetry Label Assignment (KD-2, FR-HE-020, pass-1 L-1)

| `timingOffsetMs` | Label |
|------------------|-------|
| `< -EARLY_LABEL_THRESHOLD_MS` | `Early` |
| `[-EARLY_LABEL_THRESHOLD_MS, +LATE_LABEL_THRESHOLD_MS]` | `OnTime` |
| `> +LATE_LABEL_THRESHOLD_MS` | `Late` |

Labels are emitted into `HeaderExecutedEvent.contactQualityLabel`
and the `heading.contact.quality.label` counter. They are NEVER
consumed by §3.5–§3.7.

---

## 3.5 Power & Launch-Angle Generation

```
EffectiveAttribute = Heading_norm · (1 - POWER_FATIGUE_COEFF · fatigue)
PowerMps           = POWER_BASE_MPS
                   + POWER_K_STRENGTH · Strength_norm
                   + POWER_K_HEADING  · EffectiveAttribute
outgoingSpeed      = PowerMps · PowerIntent · contactQualityScalar
launchAngle        = headerLaunchAngle(contactPointActual,
                                       headVelocityVector,
                                       incomingBallVelocity)
```

`outgoingSpeed` is cited as `FM-010-003` (§8.4). Fatigue follows
CLAUDE.md convention `0 = rested, 1 = fatigued` (KD-9, FR-HE-011).

### `headerLaunchAngle` Geometry

Reflection-style geometry off the head contact point, modulated by
head angular velocity:

```
incident         = -normalize(incomingBallVelocity)
normal           = normalize(contactPointActual_worldspace
                              - agent.headCentre_worldspace)
reflectedDir     = 2 · dot(incident, normal) · normal - incident
adjustedDir      = rotate(reflectedDir, ω_head · ANGULAR_COEFF)
launchAngle      = asin(adjustedDir.z / ||adjustedDir||)
```

`ω_head` is `headAngularVelocity` derived in §3.6. `ANGULAR_COEFF`
is absorbed into the head-velocity contribution; no new constant
is published here (covered by `SPIN_TRANSFER_COEFF` semantically;
the geometric coupling on launch angle is implicit in
`reflectedDir`).

### Worked Example

Corner cross delivered at 20 m, `PowerIntent = 0.8`,
`contactQualityScalar = 0.92`, `Heading_norm = 0.75`,
`Strength_norm = 0.70`, fatigue = 0.2:

```
EffectiveAttribute = 0.75 · (1 - 0.18·0.2) = 0.75 · 0.964 = 0.723
PowerMps          = 7.0 + 4.0·0.70 + 5.0·0.723
                  = 7.0 + 2.80 + 3.615 = 13.415 m/s
outgoingSpeed     = 13.415 · 0.8 · 0.92 ≈ 9.87 m/s
```

Launch angle ~12° above horizontal for a typical attacking
forehead-centre contact.

---

## 3.6 Spin Transfer (KD-16, FR-HE-015)

### `headAngularVelocity` Derivation (pass-1 H-3, FR-HE-032)

AM #2 does not publish a head-segment angular velocity. #10
derives it locally from already-available data, avoiding any
upstream amendment to APPROVED #2:

```
headAngularVelocity = neckRotationRate
                    + finiteDifference(headOrientation,
                                        prevFrameHeadOrientation,
                                        FRAME_MS)
```

where `headOrientation` is computed each frame from `agent.facing`
(AM #2 §3.5.1) and the per-frame `contactPointIntent` aim vector;
`neckRotationRate` is `[DERIVED]` from agent angular velocity (AM
#2 XY-plane yaw rate via finite difference of `agent.facing`)
projected onto the head-aim vector. This is a Stage 0
approximation; at Stage 1+ if AM #2 publishes a head-segment
skeletal API, the derivation simplifies to a direct read
(deferred to §7.9).

### Spin Output

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset_m
                              / SPIN_TRANSFER_REVERSAL_THRESHOLD)

reversalTerm           = max(0, -spinPreservationFactor) · incomingSpin
                         // when contactPointAxialOffset exceeds
                         // SPIN_TRANSFER_REVERSAL_THRESHOLD,
                         // spinPreservationFactor goes negative →
                         // outgoing spin component opposes incoming.

outgoingSpin           = SPIN_TRANSFER_COEFF · headAngularVelocity
                       + (incomingSpin · spinPreservationFactor)
                       - reversalTerm
```

`outgoingSpin` is cited as `FM-010-004` (§8.4). The
`spinPreservationFactor` formula is closed-form; every symbol is
either a §3.1 constant or a documented per-call value (M-1
closure).

### Worked Example

Incoming topspin 8 rad/s; `contactPointAxialOffset = 0.02 m`;
`SPIN_PRESERVATION_BASE = 0.6`; `SPIN_TRANSFER_REVERSAL_THRESHOLD =
0.015 m`:

```
factor       = 0.6 · (1 - 0.02 / 0.015) = 0.6 · -0.333 = -0.2
reversalTerm = 0.2 · 8 = 1.6 rad/s
contribution = (8 · -0.2) - 1.6 = -3.2 rad/s   (backspin reversal)
```

Adding the `SPIN_TRANSFER_COEFF · headAngularVelocity` term gives
the final `outgoingSpin`.

Boundary derivation: Appendix A.3.

---

## 3.7 Contested Duel Resolution (KD-8, FR-HE-010, FR-HE-017)

**Inputs.** Collision System #3 contact-event list at the
candidate contact frame; participating agents within
`HEAD_CONTACT_VOLUME`.

### Algorithm

1. **Iteration order.** Iterate participants in Deterministic
   Simulation #16 §3.2 entity order (FR-HE-017). Iteration order is
   deterministic regardless of arrival order in the contact-event
   list.

2. **Base score.**
   ```
   baseScore[i] = DUEL_BALANCE_WEIGHT  · Balance_norm[i]
                + DUEL_STRENGTH_WEIGHT · Strength_norm[i]
                + DUEL_HEADING_WEIGHT  · Heading_norm[i]
   ```
   Cited as `FM-010-005` (§8.4).

3. **Near-tie tiebreak (pass-1 H-5, FR-HE-023).** Rank participants
   by `baseScore` descending. If `baseScore[rank0] -
   baseScore[rank1] < DUEL_TIEBREAK_EPSILON`, invoke the tiebreak
   perturbation: each participant `i` within
   `DUEL_TIEBREAK_EPSILON` of `baseScore[rank0]` receives an
   additive `DUEL_TIEBREAK_NOISE_AMPLITUDE ·
   rng.NextFloat(DRAW_SITE_DUEL_TIEBREAK)`. Re-rank. Non-tie scores
   are NEVER perturbed.

4. **Winner emission.** Highest scorer wins; emits
   `HeaderExecutedEvent`. Losers receive `disturbanceFactor ∈
   [0, DUEL_DISTURBANCE_MAX]` (scaled by `baseScore` gap), applied
   multiplicatively to their `contactQualityScalar` (`q' = q ·
   (1 - disturbanceFactor)`). If `q' < MIN_CONTACT_QUALITY`, the
   loser emits `HeaderAttemptFailedEvent` instead of a poor-
   quality `HeaderExecutedEvent` (FR-HE-026).

5. **Multi-way (3+) duels.** Winner-only emits
   `HeaderExecutedEvent`; all losers emit
   `HeaderAttemptFailedEvent` with `failureCause = DisturbedInDuel`
   (wording aligned with §2.3 F-04 per pass-1 L-5, FR-HE-027).

### Worked Example

Two strikers (A, B) and one defender (D) contesting a corner.
Attributes (Heading / Strength / Balance, normalized): A = (0.80,
0.62, 0.70), B = (0.75, 0.58, 0.66), D = (0.78, 0.80, 0.72).

```
baseScore_A = 0.30·0.70 + 0.35·0.62 + 0.35·0.80 = 0.210 + 0.217 + 0.280 = 0.707
baseScore_B = 0.30·0.66 + 0.35·0.58 + 0.35·0.75 = 0.198 + 0.203 + 0.263 = 0.664
baseScore_D = 0.30·0.72 + 0.35·0.80 + 0.35·0.78 = 0.216 + 0.280 + 0.273 = 0.769
```

Gap `D − A = 0.062 > DUEL_TIEBREAK_EPSILON = 0.02`, so no
perturbation. Defender wins.

---

## 3.8 Own-Goal-Shape Flag Computation (KD-6, FR-HE-007, FR-HE-025)

```
horizon_s = OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S
horizon_m = OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M

trajectory   = projectBallistic(outgoingVelocity, contactPosition,
                                 GRAVITY_MPS2)
terminate_at = first(t such that t >= horizon_s
                                  OR arcLength(trajectory, t) >= horizon_m)
flag         = trajectory.path(0 .. terminate_at)
                          .intersects(ownGoalBoundingBox(agent.team))
```

### Dual Horizon (pass-1 L-7)

Projection terminates at whichever of (a) `horizon_s` elapsed
simulated time or (b) `horizon_m` travelled-distance arc-length is
reached first. A flat header travels much further per second than
a looping header, so a pure time horizon over-reaches on flat
trajectories and under-reaches on loops; the distance cap binds
the flat case, and the time cap binds the loop case.

The flag is published in `HeaderExecutedEvent.ownGoalShapedTrajectory`
and the `heading.own_goal_shaped.flag` counter. Goal-line crossing
adjudication is owned by Event System #17 / Match Referee (KD-6).

Projection geometry: Appendix A.4.

---

## 3.9 Failed-Attempt Pipeline (KD-12, FR-HE-006)

```
function emitFailedAttempt(agent, cause):
    // (1) Do NOT call Ball.ApplyKick. Ball trajectory unchanged.

    // (2) Compute miss distance: closest approach of ball path to
    //     agent head centre across the attempt window.
    missDistanceM = computeClosestApproach(ball, agent, attemptWindow)

    // (3) Publish.
    EventBus.Publish(HeaderAttemptFailedEvent {
        agentId       = agent.id,
        matchTime     = currentMatchTime,
        missDistanceM = missDistanceM,
        timingOffsetMs = currentTimingOffsetMs,
        failureCause  = cause           // F-01..F-04, F-07
    })

    // (4) Increment heading.attempt.failed.cause counter.
```

`failureCause` is mapped from the failure mode table (§2.3):

| Detection | `failureCause` |
|-----------|----------------|
| F-01 early | `MistimedEarly` |
| F-01 late | `MistimedLate` |
| F-02, F-03 | `PositionedPoorly` |
| F-04 (duel loss) | `DisturbedInDuel` |
| F-07 (envelope clamp) | absorbed into `pointError`; no failed event |

`Ball.ApplyKick` is invoked only on successful contact (§4.3).

---

## 3.10 Boundary Algorithms

### Boundary with First Touch #4 (KD-3)

```
if contactBodyPart(agent, contactFrame) == Head:
    route to Spec #10                   // §3.2 onwards
else:
    route to Spec #4                    // First Touch
```

Spec #10 owns this rule definitively. The First Touch #4 0.5 m
height threshold does NOT gate head contacts. The boundary
predicate has no fall-through case — every ball contact is body-
part-classified before routing.

### Boundary with Goalkeeper #11 (KD-7)

```
if agent.role == Goalkeeper AND contactBodyPart == Head:
    route to Spec #10                   // GK head contacts unchanged
    // Spec #11 may override HeaderIntent derivation upstream;
    // #10 physics layer is invariant.
```

### Boundary with Collision System #3 (KD-8)

```
contactEvents = CollisionSystem.GetContactEventsAtFrame(contactFrame)
for evt in contactEvents:
    // Read-only consumption. No #3 contract redefinition.
    useContactNormal(evt.normal)
    useRelativeVelocity(evt.relativeVelocity)
```
