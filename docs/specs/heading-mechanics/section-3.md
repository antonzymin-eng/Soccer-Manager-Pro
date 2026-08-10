# Heading Mechanics Specification #10 — Section 3: Core Formulas, Algorithms, Pseudocode

**Created:** May 16, 2026
**Version:** 0.7
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
| `JUMP_APEX_FRACTION` | `[GT]` | dimensionless | (0, 1) | 0.50 | Apex location along the jump phase as a fraction of `JUMP_PHASE_DURATION_MS`.[^apex-tag] |

[^apex-tag]: Tag rationale (v0.2 L-3): `[GT]` not `[FIXED]` because the Stage 0 trajectory is synthetic per KD-18, not physical.
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
| `DUEL_DISTURBANCE_MAX` | `[GT]` | dimensionless | (0, 1] | 0.50 | Maximum disturbance factor applied to a duel loser's `contactQualityScalar`. Saturates at `DUEL_DISTURBANCE_GAP_SATURATION` (see §3.7 step 4 formula). |
| `DUEL_DISTURBANCE_GAP_SATURATION` | `[GT]` | dimensionless | (0, 1] | 0.20 | `baseScore` gap (winner − loser) at which `disturbanceFactor` saturates at `DUEL_DISTURBANCE_MAX`. Below this gap, disturbance grows linearly from 0; at or above, it is capped (v0.2 H-4). |
| `HEADING_CONTACT_BUFFER_CAPACITY` | `[GT]` | count | [4, 32] | 16 | Pre-sized backing-array capacity for the per-frame `ICollisionEventConsumer` buffer in §4.2.1 (v0.3 OI-005). Bound derived from §6.3 worst-case 3-way duel × 2 contact-pairs × small safety margin. Allocated once at `HeadingMechanics.Initialize()`; no per-tick heap allocation. |
| `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_S` | `[GT]` | s | (0, 5] | 1.2 | Projection time horizon for own-goal-shape flag. |
| `OWN_GOAL_TRAJECTORY_PROJECTION_HORIZON_M` | `[GT]` | m | (0, 60] | 18 | Projection distance horizon (pass-1 L-7). Flag invocation uses `min(time, distance)`. |
| `GRAVITY_MPS2` | `[CROSS]` | m/s² | n/a | 9.81 | Ball Physics #1. |
| `PITCH_LENGTH_M` | `[CROSS]` | m | n/a | 105 | Ball Physics #1 §1.2. |
| `PITCH_WIDTH_M` | `[CROSS]` | m | n/a | 68 | Ball Physics #1 §1.2. |
| `DOMAIN_TAG_HEADING` | `[CROSS]` | byte | n/a | `0x16` | Deterministic Simulation #16 §3.4 — ERR-010-001 RESOLVED May 16, 2026 (#16 §3.5 v1.0.2 patch). Read-only consumption; #16 owns the tag namespace. |
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

**Outputs.** Result struct
`(bool isEligible, int predictedContactFrame, int idealContactFrame,
MistimedDirection mistimedDirection)`. `idealContactFrame` is the
apex-aligned target frame against which `timingOffsetMs` is measured
in §3.4 — a per-call output, not a project constant (relocated from
§3.1 per pass-1 M-2). `mistimedDirection ∈ { None, Early, Late }`
encodes the cause when `isEligible = false` due to intent-staleness;
the caller in §4.6 emits the failed event (v0.2 M-2 separation of
concerns).

### Frame-Tolerance Rounding (v0.2 M-8 / H-3)

```
framesEarlyTolerance = (int) ceil(MAX_EARLY_TOLERANCE_MS / FRAME_MS)
framesLateTolerance  = (int) ceil(MAX_LATE_TOLERANCE_MS  / FRAME_MS)
```

Rounding is `ceil` (toward looser tolerance) so that boundary
frames remain eligible; the integer comparison in step 5 then uses
strict `>` / `<`. With the §3.1 candidate values: `framesEarlyTolerance
= ceil(140 / 16.67) = 9`; `framesLateTolerance = ceil(90 / 16.67)
= 6`.

### Pseudocode (pure predicate; no event side effects — v0.2 M-2)

```
function EligibilityPredicate(agent, ball, intent, currentFrame):
    // (1) Aerial-phase check (KD-18). Stage 0 aerial phase is owned by #10.
    //     AM #2 ground state must be exitable (not GROUNDED / STUMBLING).
    if agent.movementState in { GROUNDED, STUMBLING }:
        return (false, -, -, None)

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
    if predictedContactFrame is None:
        return (false, -, idealContactFrame, None)

    // (4) Body-part check (KD-3). Head is unconditional for the head zone.
    if contactBodyPart(agent, predictedContactFrame) != Head:
        return (false, predictedContactFrame, idealContactFrame, None)

    // (5) Intent-staleness handling (KD-17 / FR-HE-018) — pass-1 M-5.
    //     Per v0.2 M-2, no event emission inside the predicate;
    //     the §4.6 caller inspects `mistimedDirection` and emits.
    if predictedContactFrame < idealContactFrame - framesEarlyTolerance:
        return (false, predictedContactFrame, idealContactFrame, Early)
    if predictedContactFrame > idealContactFrame + framesLateTolerance:
        return (false, predictedContactFrame, idealContactFrame, Late)

    return (true, predictedContactFrame, idealContactFrame, None)
```

`targetIntent`, `powerIntent`, and `contactPointIntent` are held
fixed after commit (KD-17 (a), (c)). Only
`predictedContactFrame` is re-evaluated each 60 Hz tick. The
§4.6 caller routes `mistimedDirection = Early/Late` into
`emitFailedAttempt(agent, MistimedEarly/MistimedLate)` exactly
once per tick per agent.

### Worked Example — Corner Cross

Corner cross delivered at 8 m/s incoming horizontal speed. Defender
commits header at 10 Hz tick `T`. Initial prediction: contact at
frame `T+9`. `idealContactFrame = T+9`. On each subsequent physics
tick `T+1`, `T+2`, …, the predicate re-evaluates. At `T+5` the
ball deflects off another defender (Collision System #3 contact
event mid-flight); the re-prediction returns `T+16`, which
strictly exceeds `T+9 + framesLateTolerance` (= `T+9 + 6 = T+15`
at `MAX_LATE_TOLERANCE_MS = 90` with `ceil` rounding per §3.2
Frame-Tolerance Rounding). The predicate returns `(false,
T+16, T+9, Late)`; the §4.6 caller emits
`HeaderAttemptFailedEvent { failureCause: MistimedLate }`.

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

### `jumpStartFrame` Source (v0.2 M-3)

The synthetic jump trajectory is anchored at `jumpStartFrame`,
the 60 Hz physics-tick index at which the agent first leaves the
ground for the committed header. It is set deterministically as:

```
jumpStartFrame = first currentFrame >= attemptCommittedTick · 6
                 at which:
                   - HeaderIntent is still latched, AND
                   - agent.movementState ∉ { GROUNDED, STUMBLING }
                     (i.e. the agent has cleared any preceding
                     AM #2 ground-recovery state)
```

The `· 6` factor converts the 10 Hz tactical tick index to a
60 Hz physics frame index (`TICK_RATE_PHYSICS_HZ /
TICK_RATE_TACTICAL_HZ = 6`). `jumpStartFrame` is populated
on a new field of the per-attempt `HeaderContactState` (§2.2),
written exactly once by §4.6 on the first frame the conditions
above hold, and read by §3.3 every subsequent frame until
landing. No AM #2 amendment is required (KD-18); the agent's
ground exit is observed via existing `agent.movementState`.

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
                  / headingAttrScale(agent)
systematicErrorM  = ||contactPointActual - contactPointIntent||
                  / headingAttrScale(agent)
pointError        = systematicErrorM + pointNoiseM
pointQuality      = 1 - clamp01(pointError / CONTACT_POINT_ERROR_SIGMA_M)

contactQualityScalar = TIMING_POINT_BLEND_ALPHA · timingQuality
                     + (1 - TIMING_POINT_BLEND_ALPHA) · pointQuality
```

where:

```
headingAttrScale(agent) = 1 + CONTACT_POINT_HEADING_ATTR_COEFF
                            · (Heading_norm - 0.5)
```

(v0.2 H-2 fix.) Both the systematic miss `||contactPointActual −
contactPointIntent||` and the random noise `pointNoiseM` are
**divided** by `headingAttrScale(agent)`. Higher Heading attribute
therefore yields a numerically smaller `pointError` for the same
underlying physical contact geometry — i.e. high-Heading players
produce a tighter point-error distribution, which is the stated
intent. At `Heading_norm = 1.0`, `headingAttrScale = 1.2`, both
error sources are scaled to ~83% of their baseline. The
`pointQuality` denominator is the bare `CONTACT_POINT_ERROR_SIGMA_M`
constant (no longer multiplied by `headingAttrScale`), so the
mapping from physical error to quality is fixed across players;
only the physical error itself varies with attribute. Cited as
`FM-010-002` (§8.4).

### RNG Draw-Site Wiring (KD-10, FR-HE-024, pass-1 M-4)

Both Gaussian draws above route through #16's `NextGaussian` API
with registered draw-site IDs `DRAW_SITE_TIMING_JITTER` and
`DRAW_SITE_CONTACT_POINT_ERROR` (declared in §4.4). No phantom
draw sites exist.

### `timingJitterMs` Semantics (v0.2 M-9)

`timingJitterMs` models **sub-frame execution noise**: per-attempt
micro-variations in the contact instant within the
`actualContactFrame` window that do not shift the integer frame
index (so eligibility in §3.2 is unaffected) but do shift the
effective contact moment continuously, affecting quality. This
contrasts with execution noise that would push contact into the
adjacent frame — that magnitude of variation is already modelled
by Decision Tree #8's tactical-tick choice of commit moment and
by the §3.2 eligibility envelope (`MAX_EARLY/LATE_TOLERANCE_MS`).
Consequently, the jitter is applied **post-eligibility** to
`timingOffsetMs` only, never to `predictedContactFrame` or
`actualContactFrame`.

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

### `headerLaunchAngle` Geometry (v0.2 H-5)

Stage 0 launch angle is pure reflection geometry off the head
contact point. Head angular velocity (`ω_head` from §3.6) affects
the **outgoing spin** (§3.6 spin transfer) but does NOT modulate
the launch direction at Stage 0 — biomechanical data tying head
angular velocity to launch-angle deflection is not currently
calibrated against published references (§8.3), and introducing a
free coefficient here would violate KD-11. A Stage 1+ refinement
may add a `LAUNCH_ANGLE_HEAD_VELOCITY_COEFF [GT]` when validation
data warrants (deferred to §7.12).

```
incident      = -normalize(incomingBallVelocity)
normal        = normalize(contactPointActual_worldspace
                           - agent.headCentre_worldspace)
reflectedDir  = 2 · dot(incident, normal) · normal - incident
launchAngle   = asin(reflectedDir.z / ||reflectedDir||)
```

The intended launch direction (toward `targetIntent`) is realized
by §3.5.1, which derives the contact point that reflects the ball
at the target. `pointError` (§3.4) captures deviation from that
geometric ideal.

> **ERR-010-002 (August 9, 2026) — the aim had no owner.** This
> paragraph previously read: *"the upstream choice of
> `contactPointIntent`: Decision Tree #8 selects a contact point on
> the head surface such that the reflected vector points at the
> target."* Decision Tree #8 **cannot emit a header at all** —
> `ActionType` ordinal 8 overflows the 3-bit composure-noise field
> (wiring backlog W9), so the header producer is, and for the whole
> of Stage 0 has been, the match-engine proximity trigger. It
> supplied `contactPointIntent = 0` and a fixed `targetIntent`, and
> §3.5's reflection read neither, so **every header was a passive
> mirror**: the ball left the head along the reflection of its own
> incoming path and the player had no influence on where it went.
> This is the `ERR-011-010` shape — a decision delegated to a system
> that structurally cannot make it, and therefore made by nobody.
> §3.5.1 takes the derivation back into #10. Two further defects
> were fixed with it, both recorded in the error log: the contact
> point was recomputed independently in two places from ball-vs-head
> geometry (a parallel surface that agreed only by coincidence), and
> the world-space contact point was rebuilt from its **2-D**
> head-local projection, pinning `contactPointActual.z` to the head
> centre — so the reflection normal was permanently horizontal,
> `reflected.z = v̂_in.z`, and **a descending ball was headed further
> down**. No header could lift the ball.

### 3.5.1 Aim Realization (ERR-010-002)

The aim is realized in three steps, all pure, all free of new
`[GT]` constants — the Heading attribute is itself the dial, so
this stays inside the KD-W1 freeze while heading is unwired.

**Step 1 — the launch direction.** A `targetIntent` is a
*destination*, reached by an arc, not a straight line: aiming
along the straight line to a distant ground-level point sends the
ball into the turf a few metres away. Solve the gravity-only
projectile launch toward it, at the outgoing speed a perfect
contact would carry (`contactQualityScalar = 1`; solving at the
achieved speed would be circular, since achieved speed follows
from quality and quality follows from the aim error):

```
# Degenerate entry (MUST): a non-finite input, a target directly above or below
# the contact point (R ~ 0), or no outgoing speed has no launch direction at all.
# Return zero — Step 2 is specified to propagate that zero, and without this guard
# the low-root divide below is by g·R = 0 and every downstream vector is NaN.
if any of contactPoint, targetIntent, v is non-finite:  aimDir = 0; stop
R           = ||targetIntent.xy - contactPoint.xy||
if R < ε or v < ε:                                      aimDir = 0; stop

dz          = targetIntent.z - contactPoint.z
disc        = v⁴ - g·(g·R² + 2·dz·v²)

if disc >= 0:
    tanθ    = (v² - sqrt(disc)) / (g·R)          # low root
    aimDir  = normalize(flat·1 + ẑ·tanθ)
else:
    # Out of ballistic range at this speed: degrade to the
    # maximum-range launch toward the target — the angle at which
    # disc vanishes — rather than failing (P1, continuous never a
    # cliff).
    radicand = v² - 2·g·dz
    if radicand <= ε²:
        aimDir  = ẑ            # unreachable at ANY angle; straight up is the closest approach
    else:
        tanθ    = v / sqrt(radicand)
        aimDir  = normalize(flat·1 + ẑ·tanθ)
```

The **low** root is taken: a header is a driven contact, not a
lob, and the flat solution also spends least time in the air.
`disc < 0` means the target is beyond ballistic range at this
speed; the direction then degrades continuously to the true
maximum-range launch angle toward it, `tanθ = v / sqrt(v² −
2·g·dz)`, rather than failing — P1, continuous never a cliff. That
angle equals 45° **only when `dz == 0`.** A header contacts near
2.3 m and its targets sit on the ground, so `dz < 0` on essentially
every real header, and this branch is the ordinary case for a
defensive clearance — it is what makes one long and high. An
earlier draft of this step used a flat 45° unconditionally, which
put a discontinuous step at the reachability boundary rather than
the P1 continuity this branch claims: measured, the step was 9.98°
across a 4 cm change in target distance at the boundary, and 4.38°
at the production nominal speed of 11.2 m/s. Fixed in the same
adversarial-review pass as Step 2 below, `HeadingAim.ComputeAimDirection`
v1.1. When even the true maximum-range angle cannot bring the ball
nearer the target — it sits higher than this speed reaches at any
launch angle, `radicand <= ε²` — `aimDir` is straight up (`+ẑ`, this
project's up axis per Ball Physics #1 §1.2, not Unity's `+Y`): the
closest approach available, and the honest answer rather than a
degenerate direction.

**Step 2 — the normal that realizes it.** Inverting the §3.5
reflection gives the half-vector exactly:

```
aimNormal = normalize(incident + aimDir)
```

with **no hemisphere bound** — and that is deliberate, not an
omission. An earlier draft of this step carried one ("the struck
surface must face the oncoming ball, or it is the back of the
player's skull, so project onto that boundary"), and it was
removed before implementation because it can never fire. For unit
vectors, `dot(incident + aimDir, incident) = 1 + dot(aimDir,
incident) ≥ 0`: the half-vector is always in the forward
hemisphere already, for every choice of `aimDir`. Equivalently,
from the reflected side: with `c = dot(incident, normal) ∈ [0, 1]`
the reflected direction satisfies `dot(reflected, incident) = 2c²
− 1`, which sweeps the whole of `[−1, 1]` — every outgoing
direction is producible off the front of the head, so no aim ever
needs the back of the skull. This is this project's "guard on an
unreachable branch" defect class (root `CLAUDE.md` Traps table):
a check that reads as a safety bound but is provably dead ships
green precisely because nothing can exercise it, so it is recorded
here as a proof rather than left as unreachable code.

The one genuinely degenerate input is `aimDir` exactly opposite
`incident` — the ball would have to pass through the head
unchanged, which no reflection off a sphere produces — and that
case returns no solution (`Vector3.zero`), handled by the caller
the same way any other degenerate aim is (Step 3 falls back to the
geometric normal). What actually bounds the aim is not geometry
but the player: how far he can steer toward it, which is Step 3's
attribute-graded blend.

**Step 3 — what the player actually achieves.**

```
achievedNormal = normalize(geometricNormal
                 + Heading_norm · (aimNormal - geometricNormal))
```

**Degenerate contact (recorded, NOT fixed).** When the ball arrives
within ~0.1 mm of the head centre, `ballPosition − headCentre` is zero,
`geometricNormal` is zero, and this step's degenerate guard returns
that zero unchanged — so the aim is silently discarded and two
targets at opposite ends of the pitch produce the identical outgoing
vector. It is reachable only if the ball crosses from outside the
`HEAD_CONTACT_VOLUME_RADIUS_M` envelope to within 1e-4 m of the centre
in a single 16.7 ms frame, and §3.5's own degenerate fallback still
plays the contact, so it is recorded rather than fixed. The natural
fix, if it is ever wanted, is to fall back to the AIM normal there —
it is well defined precisely when the geometric one is not.

`geometricNormal` is `normalize(ballPosition − headCentre)`, the
pre-ERR-010-002 model, so **steer authority 0 is exactly the old
behaviour** and authority 1 places the ball. `Heading_norm` spans
the whole attribute range with no plateau at either end (raw 1 →
0.05, raw 20 → 1.00), which is the FULL-RANGE ramp shape settled
at `ERR-008-019`: the aim is skill (P2), not a switch.

`contactPointActual = headCentre + achievedNormal · r`, where `r`
is the radial magnitude of the geometric contact, clamped to
`HEAD_CONTACT_VOLUME_RADIUS_M` — preserved from the geometric
contact so §3.6's axial-offset input and §3.4's error scale keep
their existing footing and this change moves one thing.
`contactPointIntent` is `headCentre + aimNormal · r`, so §3.4's
`pointError` becomes, for the first time, a genuine **execution**
error: it was previously the distance between a hardcoded zero and
a geometric fact. A header steered hard away from its natural
rebound is therefore weaker as well as less accurate, which is the
football.

**`contactPointActual` is 3-D.** The head-local `Vector2`
(Appendix D: +x facing-forward, +y agent-left) is the frame §3.4
and §3.6 are defined over, and remains so — but it is a
*projection*, and the reflection MUST take its normal from the
full 3-D point. Reconstructing the world point from the 2-D
projection is the defect ERR-010-002 removed.

**Who chooses `targetIntent`.** #10 realizes an aim; it does not
choose one. The producer does — the match engine today
(`GkHeadingIntentSource.HeaderAimTarget`, governed by
`match-engine-design.md`), Decision Tree #8 when W9 lands. This is
the same split `ERR-011-010` settled for the keeper's rush: the
engine owns *when* and *at what*, #11/#10 own the
attribute-driven *how*. `contactPointIntent` remains on
`HeaderIntent` as the DT-supplied override for W9 and is **not
read by Stage-0 geometry** — the half-vector that realizes an aim
depends on the incoming velocity at contact, which no producer can
know at commit time, and KD-4 locks the intent at commit.

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

### Spin Output (v0.2 H-1)

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset_m
                              / SPIN_TRANSFER_REVERSAL_THRESHOLD)

outgoingSpin           = SPIN_TRANSFER_COEFF · headAngularVelocity
                       + (incomingSpin · spinPreservationFactor)
```

When `contactPointAxialOffset` exceeds
`SPIN_TRANSFER_REVERSAL_THRESHOLD`, `spinPreservationFactor` goes
negative and the `(incomingSpin · spinPreservationFactor)` term
already carries the sign flip — a single sign reversal proportional
to the axial-offset overshoot. (The v0.1 formula additionally
subtracted a `reversalTerm`, which double-counted the reversal;
the term has been removed.)

`outgoingSpin` is cited as `FM-010-004` (§8.4). The formula is
closed-form; every symbol is either a §3.1 constant or a
documented per-call value.

### Worked Example

Incoming topspin 8 rad/s; `contactPointAxialOffset = 0.02 m`;
`SPIN_PRESERVATION_BASE = 0.6`; `SPIN_TRANSFER_REVERSAL_THRESHOLD =
0.015 m`:

```
factor                 = 0.6 · (1 - 0.02 / 0.015) = 0.6 · -0.333 = -0.2
incomingContribution   = 8 · (-0.2) = -1.6 rad/s   (backspin reversal,
                                                    proportional to overshoot)
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

4. **Winner emission and per-loser disturbance (v0.2 H-4, M-5).**
   Highest scorer wins; emits `HeaderExecutedEvent`. For each
   loser `i`, the `baseScore` gap (after any step-3 perturbation)
   is `gap_i = baseScore[winner] − baseScore[i]`, and the
   disturbance factor is:

   ```
   disturbanceFactor_i = DUEL_DISTURBANCE_MAX
                       · clamp01(gap_i / DUEL_DISTURBANCE_GAP_SATURATION)
   ```

   Disturbance grows linearly from 0 at `gap = 0` to
   `DUEL_DISTURBANCE_MAX` at `gap ≥ DUEL_DISTURBANCE_GAP_SATURATION`,
   then saturates. The disturbance is applied multiplicatively to
   the loser's `contactQualityScalar`:

   ```
   q'_i = q_i · (1 - disturbanceFactor_i)
   ```

   If `q'_i < MIN_CONTACT_QUALITY`, loser `i` emits
   `HeaderAttemptFailedEvent` with `failureCause = DisturbedInDuel`
   (FR-HE-026). Otherwise loser `i` emits a disturbed-but-executed
   `HeaderExecutedEvent` carrying `contactQualityScalar = q'_i`
   and `contestedDuelId` set to the duel ID.

5. **Multi-way (3+) duels (v0.2 M-5 alignment).** The semantics
   above apply uniformly regardless of participant count: exactly
   one winner emits a full-quality `HeaderExecutedEvent`; each
   loser emits either a disturbed `HeaderExecutedEvent` (if `q'_i
   ≥ MIN_CONTACT_QUALITY`) or a `HeaderAttemptFailedEvent` (if
   `q'_i < MIN_CONTACT_QUALITY`). There is no separate "all-losers-fail"
   3+ way path. (v0.1 step 5 had an inconsistent winner-only-emits
   semantics for 3+ way duels; that has been removed.)

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
| F-04 (duel loser with `q' < MIN_CONTACT_QUALITY`) | `DisturbedInDuel` |
| F-04 (duel loser with `q' ≥ MIN_CONTACT_QUALITY`) | no failed event; disturbed `HeaderExecutedEvent` emitted per §3.7 step 4 |
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

---

## 3.11 Version History

| Version | Date         | Author  | Notes |
|---------|--------------|---------|-------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1 |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 adversarial-review fix pass (21 findings: 5 H / 9 M / 7 L). H-1 §3.6 spin double-reversal removed; H-2 §3.4 `headingAttrScale` semantics realigned (errors divided by scale); H-3 §3.2 worked example off-by-one fixed (T+14 → T+16); H-4 §3.7 step 4 `disturbanceFactor` formula added + `DUEL_DISTURBANCE_GAP_SATURATION [GT]` row added to §3.1; H-5 §3.5 `ANGULAR_COEFF` removed (Stage 0 reflection-only launch angle, deferred to §7.12); M-2 `EligibilityPredicate` split into pure predicate + caller; M-3 `jumpStartFrame` source defined in §3.3; M-5 §3.7 step 5 2-way/3-way loser semantics aligned; M-8 frame-tolerance `ceil` rounding policy pinned in §3.2; M-9 `timingJitterMs` semantics paragraph added in §3.4; L-3 `JUMP_APEX_FRACTION` tag rationale moved to footnote. |
| 0.3     | May 16, 2026 | drafter | APPROVAL. §3.1 `DOMAIN_TAG_HEADING` promoted `[CROSS-PENDING] → [CROSS]` post #16 §3.5 v1.0.2 patch (ERR-010-001 RESOLVED). New `HEADING_CONTACT_BUFFER_CAPACITY [GT]` row added for §4.2.1 collision-event consumer buffer (OI-005). |
| 0.4     | August 9, 2026 | — | **ERR-010-002 — the header aim had no owner.** §3.5's realization paragraph delegated the aim to Decision Tree #8 ("selects a contact point on the head surface such that the reflected vector points at the target"); #8 cannot emit a header at all (`ActionType` ordinal 8 overflows the 3-bit composure-noise field — wiring backlog W9), so the producer has always been the match-engine proximity trigger, which supplied `contactPointIntent = 0` and a fixed `targetIntent`, and §3.5 read neither: **every header was a passive mirror.** The `ERR-011-010` shape. New **§3.5.1** takes the derivation back into #10 — ballistic launch solve to the target at the perfect-contact speed (low root; 45° max-range fallback out of range, P1 continuous), the reflecting half-vector bounded to the physically reachable hemisphere, and an achieved normal blended from the geometric normal by normalised Heading (FULL-RANGE ramp, `ERR-008-019` shape; authority 0 ≡ pre-fix behaviour). No new `[GT]` — the attribute is the dial, so this stays inside the KD-W1 freeze. `pointError` becomes a genuine execution error rather than the distance between a hardcoded zero and a geometric fact. §3.5.1 also pins that `contactPointActual` is 3-D (the 2-D head-local frame is §3.4/§3.6's definition domain and a projection of it) and states the producer/realizer split: the engine chooses `targetIntent`, #10 realizes it. **This row's "bounded to the physically reachable hemisphere" claim is corrected at v0.5 below — the shipped code never carried that bound; see the adversarial review of the landing.** |
| 0.5     | August 9, 2026 | — | **Adversarial review of the ERR-010-002 landing, Finding 1 (High).** §3.5.1 Step 2's normative text claimed the half-vector was "bounded to the hemisphere the ball can physically reach," projected onto that boundary when it fell outside. `HeadingAim.ComputeAimNormal` (the shipped implementation) carries no such bound and its XML doc proves one can never be needed: for unit vectors `dot(incident + aimDir, incident) = 1 + dot(aimDir, incident) ≥ 0`, so the half-vector is always in the forward hemisphere already — a "guard on an unreachable branch," this project's own recorded defect class. The spec was stale, not the code: Step 2 rewritten to state the no-bound design directly, carry the proof, and name the one genuinely degenerate input (`aimDir` exactly opposite `incident`, which returns no solution). No behaviour change — the code was already correct. |
| 0.6     | August 9, 2026 | — | **Adversarial review pass 2 over the ERR-010-002 landing, Finding H-1.** Step 1's normative text still claimed the out-of-range branch "degrades continuously to the 45° maximum-range launch," and the pseudocode carried neither the out-of-range formula nor the unreachable-height guard — commit `d93e0c8` had already replaced the hardcoded 45° with the true `dz`-dependent maximum-range angle, `tanθ = v / sqrt(v² − 2·g·dz)` (45° only when `dz == 0`; a header contacts near 2.3 m aiming at the ground, so `dz < 0` on essentially every real header — measured step 9.98° across a 4 cm boundary change, 4.38° at the 11.2 m/s production nominal speed), plus a guard returning a vertical launch when the target is unreachable at any angle. `48977fa` (the documentation half of this same review pass) landed before `d93e0c8` and was never back-propagated — this project's spec-and-code-same-commit doctrine failing inside the very review pass that exists to enforce it (the `ERR-041-012` shape). Fixed: prose corrected to name the true formula and the `dz == 0` special case, and both branches (out-of-range solve, unreachable-height guard) added to the pseudocode, matching `HeadingAim.ComputeAimDirection` v1.1's out-of-range branches exactly `[NARROWED at v0.7 — the BRANCHES matched; the METHOD did not. This row's original wording implied the whole pseudocode matched the code, and it omitted the degenerate-entry guard. AR pass 3, M-2]`. No behaviour change — the code was already correct; only the spec was stale. |
| 0.7     | August 9, 2026 | — | **Adversarial review pass 3, Findings M-2 and L-2.** M-2: Step 1's pseudocode carried neither the finiteness guard nor the degenerate guard the shipped `HeadingAim.ComputeAimDirection` has at its entry (`range < ε || speed < ε → zero`), so an implementer following this spec literally divides by `g·R = 0` for a target directly above or below the contact point and sends NaN into every downstream vector. Worse, Step 2 is specified to PROPAGATE that zero (v0.5 above) — the spec documented a handler for an input it never said how to generate. Guard added to the pseudocode as a MUST; the v0.6 row's "matching v1.1 exactly" claim is narrowed in place, since the branches matched and the method did not (the falsified-hand-verification class this project keeps filing). L-2: the dead-centre degeneracy is now recorded here — when the ball arrives within ~0.1 mm of the head centre the geometric normal is zero, Step 3's degenerate guard returns that zero, and the aim is silently discarded. Reachable only if the ball crosses from outside the 0.18 m contact volume to within 1e-4 m of the centre in one 16.7 ms frame; §3.5's own degenerate fallback still plays the contact. Recorded, NOT fixed. |
