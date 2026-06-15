# Goalkeeper Mechanics Specification #11 — Section 3: Core Formulas, Algorithms, Pseudocode

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Specify the formulas, algorithms, pseudocode, and
constant catalogue that govern Goalkeeper Mechanics. All formulas
consume only inputs declared in §1.4 dependencies and emit only
outputs declared in §2.

---

## 3.1 GK State Machine

States: `Resting`, `Set`, `Anticipate`, `Diving`, `Airborne`,
`HandsOnBall`, `Recovering`, `Distributing`, `Rushing`, `OneOnOne`,
`Smothered`.

### 3.1.1 Transition table

Each row is `(from, to, trigger, tick-rate, source spec)`.

| From | To | Trigger | Tick-rate | Source |
|------|----|---------|-----------|--------|
| `Resting` | `Set` | `BallState.position` enters attacking third (`x` past `BALL_ATTACKING_THIRD_X_M` for attacker-controlled possession) | 10 Hz | #11 / #8 |
| `Set` | `Anticipate` | Decision Tree #8 sets `gkAnticipationScore > ANTICIPATE_THRESHOLD` | 10 Hz | #8 |
| `Set` | `Anticipate` | `ShotExecutedEvent` consumed (early predictive) | 60 Hz event | #6 §4.5 |
| `Anticipate` | `Diving` | Decision Tree #8 commits `SaveIntent` with valid `targetHand` | 10 Hz | #8 |
| `Diving` | `Airborne` | Dive launch impulse applied (60 Hz physics) | 60 Hz | #11 §3.3 |
| `Airborne` | `HandsOnBall` | #3 hand-ball contact event with positive `handlingQualityScalar ≥ MIN_HANDLING_QUALITY` AND `≥ CATCH_THRESHOLD` (caught path) | 60 Hz | #3 / #11 §3.5 |
| `Airborne` | `Recovering` | #3 hand-ball contact event with `handlingQualityScalar < CATCH_THRESHOLD` (parry / deflect / spill paths) | 60 Hz | #3 / #11 §3.5 |
| `Airborne` | `Recovering` | Ground re-entry (`agentZ ≤ 0`) without contact event (F-01 / F-02 / F-03) | 60 Hz | #11 §3.3 |
| `HandsOnBall` | `Distributing` | Decision Tree #8 commits `DistributeIntent` AND `currentTick ≥ releaseTickEarliest` | 10 Hz | #8 |
| `HandsOnBall` | `HandsOnBall` | `currentTick − claimTick ≥ GK_HOLD_MAX_TICKS` (Laws-of-the-Game 6-second rule; forced release) | 10 Hz | KD-9 / FR-GK-028 |
| `Distributing` | `Recovering` | Distribution release frame reached (windup elapsed; `passIntent` published) | 60 Hz | #11 §3.8 |
| `Recovering` | `Set` | Recovery-to-line cooldown elapsed (`RECOVERY_COOLDOWN_TICKS`) OR GK XY already within `GK_REACTIVE_RADIUS_M` of #12 baseline (v0.2 AR-S1-M5: OR not AND — prevents stall when GK is already at baseline after distribution release) | 10 Hz | #11 §3.3.0 |
| `Recovering` | `Resting` | Possession transitions to GK's own team in defensive third | 10 Hz | #11 / #8 |
| `Set` | `Rushing` | Decision Tree #8 commits `RushIntent` with `commitmentLevel > RUSH_COMMIT_THRESHOLD` | 10 Hz | #8 |
| `Anticipate` | `Rushing` | Decision Tree #8 commits `RushIntent` (preferred over `SaveIntent` per #8 priority) | 10 Hz | #8 |
| `Rushing` | `Smothered` | #3 hand-ball contact event during rush | 60 Hz | #3 / #11 §3.7 |
| `Rushing` | `OneOnOne` | Attacker within `ONE_VS_ONE_TRIGGER_RADIUS_M` of GK during rush AND `BallState.PossessorId == attackerId` | 60 Hz | #11 §3.7 |
| `Rushing` | `Recovering` | F-08 ball intercepted (`BallState.PossessorId` becomes non-GK and not the original attacker target) | 60 Hz | KD-15 / F-08 |
| `Smothered` | `HandsOnBall` | Hand-ball contact resolves with `handlingQualityScalar ≥ CATCH_THRESHOLD` | 60 Hz | #11 §3.5 |
| `Smothered` | `Recovering` | Hand-ball contact resolves with `handlingQualityScalar < CATCH_THRESHOLD` | 60 Hz | #11 §3.5 |
| `OneOnOne` | `Diving` | Decision Tree #8 commits `SaveIntent` (1v1 dive path; KD-20 coefficients apply) | 10 Hz | #8 |
| `OneOnOne` | `Smothered` | GK closes within `SMOTHER_TRIGGER_RADIUS_M` of attacker AND attacker shot pending | 60 Hz | #11 |
| `Resting` | `Resting` | Default holding state when ball is in own / middle thirds with own possession | 10 Hz | #11 |

Iteration order is deterministic per #16 §3.2. With one GK per
side, iteration-order ambiguity is restricted to multi-attacker
scenarios in §3.6 (cross-claim duel).

### 3.1.2 Pseudocode for state evaluation

```
on TacticalTick(currentTick):                   // 10 Hz
    for gk in {homeGK, awayGK}:                 // #16 §3.2 entity order
        ballState = BallPhysics.GetBallState(currentTick)
        intent    = DecisionTree.GetGKIntent(gk.agentId, currentTick)
        gk.state  = evaluateTacticalTransition(gk.state, ballState, intent, currentTick)

on PhysicsFrame(currentFrame):                  // 60 Hz
    for gk in {homeGK, awayGK}:
        gk.state = evaluatePhysicsTransition(gk.state, currentFrame)
        if gk.state in {Diving, Airborne}:
            integrateDiveKinematics(gk, currentFrame)         // §3.3
        if gk.state in {Airborne, OneOnOne, Smothered, Rushing}:
            resolveCandidateContacts(gk, currentFrame)        // §3.5 / §3.6
```

---

## 3.2 Shot Reaction Pipeline (KD-2, KD-18)

**Inputs.** `ShotExecutedEvent` from #6 §4.5; `PlayerAttributes`
from AM #2 §3.5.6 (read fields: `Reflexes`, `Composure`,
`OneVsOne`); Perception System #7 visibility-cone latency.

**Outputs.** `requiredReactionMs`, `reactionWindowAchieved`,
`predictedContactFrame`, dive-direction commit at the 10 Hz
tactical tick following shot detection.

### 3.2.1 `shotDetectedTickMs`

```
shotDetectedTickMs = shotExecutedEvent.matchTimeMs
                   + PERCEPTION_BASE_LATENCY_MS
                     · perceptionLatencyScale(gk)
```

`perceptionLatencyScale(gk)` is `[DERIVED]` from #7 §3 visibility
latency, scaled by `Reflexes_norm`. Stage 0 approximation:

```
perceptionLatencyScale(gk) = 1 - PERCEPTION_REFLEXES_SCALE · Reflexes_norm
```

with `PERCEPTION_REFLEXES_SCALE = 0.30` `[GT]`. The exact #7 surface
is pinned during implementation; the scale function is closed-form
and parameter-based per KD-1.

### 3.2.2 `requiredReactionMs`

```
requiredReactionMs = REACTION_BASE_MS
                   - REACTION_REFLEXES_COEFF · Reflexes_norm
                   + REACTION_BALL_SPEED_COEFF
                     · max(0, ballSpeed - REACTION_BALL_SPEED_REF_MPS)
                   + (state == OneOnOne ?
                        -ONE_VS_ONE_REACTION_COEFF · OneVsOne_norm : 0)
```

The `OneVsOne` term is **subtractive** (positive `OneVsOne` reduces
required reaction time, modelling 1v1 specialism) and is gated on
the GK state machine being in `OneOnOne` per KD-20.

### 3.2.3 `reactionOffsetMs` and `reactionWindowAchieved` (KD-18)

```
elapsedSinceShotMs = currentMatchTimeMs - shotDetectedTickMs
reactionOffsetMs   = elapsedSinceShotMs - requiredReactionMs

if reactionOffsetMs <= 0:
    // reacted EARLY — penalised by misdirection risk
    reactionWindowAchieved = 1 - clamp01(-reactionOffsetMs / REACTION_EARLY_TOLERANCE_MS)
else:
    // reacted LATE — penalised by reach loss
    reactionWindowAchieved = 1 - clamp01( reactionOffsetMs / REACTION_LATE_TOLERANCE_MS)
```

The two tolerances are distinct `[GT]` constants per KD-18.
`REACTION_LATE_TOLERANCE_MS` is numerically smaller than
`REACTION_EARLY_TOLERANCE_MS` (late commits decay faster than early
commits in real-world GK psychophysics; Williams & Burwitz 1993,
Savelsbergh et al. 2002).

### 3.2.4 Telemetry-label assignment (KD-2)

```
if reactionWindowAchieved >  REFLEXIVE_LABEL_THRESHOLD: label = Reflexive
elif reactionWindowAchieved < SLUGGISH_LABEL_THRESHOLD: label = Sluggish
else:                                                   label = Standard
```

Labels are emitted on `SaveAttemptedEvent.reactionLabel`; physics
NEVER branches on them.

### 3.2.5 Worked example

**Scenario.** 25 m/s shot from 18 m out (flight time ≈ 720 ms); GK
with `Reflexes_norm = 0.80`; ball-speed ref 18 m/s; coefficients
`REACTION_BASE_MS = 350`, `REACTION_REFLEXES_COEFF = 100`,
`REACTION_BALL_SPEED_COEFF = 8` (ms per m/s),
`REACTION_BALL_SPEED_REF_MPS = 18`,
`REACTION_EARLY_TOLERANCE_MS = 120`,
`REACTION_LATE_TOLERANCE_MS = 80`,
`PERCEPTION_BASE_LATENCY_MS = 120`,
`PERCEPTION_REFLEXES_SCALE = 0.30`. Not in `OneOnOne` state.

Compute:
- `perceptionLatencyScale = 1 - 0.30 · 0.80 = 0.76`.
- `shotDetectedTickMs = t_shot + 120 · 0.76 = t_shot + 91.2 ms`.
- `requiredReactionMs = 350 - 100 · 0.80 + 8 · (25 - 18) = 350 - 80 + 56 = 326 ms`.
- At a tick 250 ms after shot detection (i.e. 341 ms after
  `t_shot`): `elapsedSinceShotMs = 250`,
  `reactionOffsetMs = 250 - 326 = -76 ms`. Early commit.
- `reactionWindowAchieved = 1 - clamp01(76 / 120) = 1 - 0.633 = 0.367`.
- Label: `Standard` (assuming
  `SLUGGISH_LABEL_THRESHOLD = 0.30` and
  `REFLEXIVE_LABEL_THRESHOLD = 0.75`).

**Interpretation.** The GK has committed early — the dive is
already launched 76 ms before the model says the GK could have
seen the shot well enough to react. This is the pre-shot
anticipation path: the 10 Hz tactical tick committed before
ball-flight legibility, banking on attacker tells. The reaction
scalar drops from 1.0 toward 0 as `|reactionOffsetMs|` grows.

---

## 3.3 Dive Kinematics (KD-12)

**Stage 0 synthetic dive trajectory** owned by Spec #11 per KD-12.
AM #2 §3.6 defers Z>0 movement to Stage 1+; this subsection owns
the dive's vertical component until that migration.

### 3.3.1 Dive launch impulse

```
// Lateral dive axis is Y (touchline-to-touchline): the goal mouth spans Y, so
// the keeper dives left/right across the goal along Y, NOT along goal-to-goal X (§1.2).
diveDirectionY     = sign(targetHandY - gkY)      // ∈ {-1, 0, +1}
diveLaunchImpulse  = DIVE_LAUNCH_BASE_MPS
                   + DIVE_LAUNCH_K_STRENGTH · Strength_norm
                   + DIVE_LAUNCH_K_AERIAL   · Aerial_norm
                   - DIVE_LAUNCH_FATIGUE_COEFF · fatigue
```

`diveLaunchImpulse` is applied as a single-frame XY velocity
addition to `gk.kinematics` per AM #2 §3.5.1 update protocol.

### 3.3.2 Dive timing jitter

```
diveTimingJitterMs = DIVE_TIMING_JITTER_SIGMA_MS
                   · rng.NextGaussian(DRAW_SITE_DIVE_TIMING_JITTER, DOMAIN_TAG_GOALKEEPER)
```

The Gaussian is zero-mean unit-variance scaled by sigma per #16
§4.5 draw-site protocol.

### 3.3.3 Synthetic Z trajectory

```
peakHandZ_m   = DIVE_PEAK_Z_BASE_M
              + DIVE_PEAK_Z_K_AERIAL   · Aerial_norm
              + DIVE_PEAK_Z_K_STRENGTH · Strength_norm
              - DIVE_FATIGUE_PEAK_Z_COEFF · fatigue
              + diveTimingJitterMs · DIVE_JITTER_PEAK_Z_COEFF

diveDurationMs   = DIVE_PHASE_DURATION_MS
                   // does NOT scale with attributes at Stage 0 per §7.4 deferral
diveDurationFrm  = round(diveDurationMs / FRAME_MS)
apexFrame        = diveLaunchFrame + diveDurationFrm / 2

// Parabolic Z interpolation, apex at apexFrame
handPathZ(frame) = peakHandZ_m
                 · max(0, 1 - ((frame - apexFrame) / (diveDurationFrm / 2))²)
```

The hand reach envelope at apex frame is computed in §3.3.4.

### 3.3.4 Hand reach envelope

The hand reach envelope is the set of XYZ points the GK's hand
can occupy during the airborne phase. Stage 0 model:

```
reachRadiusBase     = ARM_LENGTH_M
                    + DIVE_BODY_REACH_EXTENSION_M
reachRadius(frame)  = reachRadiusBase
                    + REACH_K_HANDLING · Handling_norm
                    + REACH_K_AERIAL   · Aerial_norm
                    + REACH_K_STRENGTH · Strength_norm
                    - REACH_FATIGUE_COEFF · fatigue
reachCenter(frame)  = (gkPos(frame).x,
                       gkPos(frame).y + diveDirectionY · DIVE_LAUNCH_DISPLACEMENT_M
                                              · t(frame),
                       handPathZ(frame))
```

where `t(frame) = (frame - diveLaunchFrame) / diveDurationFrm ∈
[0, 1]` is the dive progress and `gkPos(frame)` is the AM #2 XY
state read at frame.

### 3.3.5 State-machine effect

Dive launch enters `Airborne` (60 Hz). On ground re-entry
(`handPathZ(frame) ≤ 0` AND `t(frame) ≥ 1`), the GK transitions to
`Recovering` and AM #2 receives a `GROUNDED` event with
`GroundedReason.DIVING_HEADER` (KD-12 re-use; Stage 0). Stage 1+
migrates to `GroundedReason.DIVING_SAVE` per §7.5; AM #2 amendment
required at that time.

Telemetry disambiguation between header and save dives is provided
by `SaveAttemptedEvent.contactBodyPart = Hand` vs.
`HeaderExecutedEvent` emission.

---

## 3.3.0 Positioning AI #12 Consumer Contract (KD-13)

This subsection publishes the *Consumer Contract for GK Baseline
Position*. It is the explicit ratification event for the three
`[EST]` constants in Positioning AI #12 §3.3.3.

### 3.3.0.1 Inputs Spec #11 expects from #12

Read-only at every 10 Hz tactical tick:

```
PositioningAI.GetGKBaselineSlot(matchTime) → Vector2
```

The returned `gkBaselineSlot` is computed by #12 §3.3.3 using the
three constants `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`,
`GK_LATERAL_FACTOR` and the current ball position via #12's
`basisX` / `basisY` formation-aware functions.

### 3.3.0.2 What Spec #11 reserves for itself

- Any micro-adjustment within
  `GK_REACTIVE_RADIUS_M = 1.5 m` `[GT]` of `gkBaselineSlot` while
  the state machine is in `Resting` or `Set`.
- Full XY freedom (unconstrained by `GK_REACTIVE_RADIUS_M`) during
  `Anticipate`, `Diving`, `Airborne`, `HandsOnBall`, `Recovering`,
  `Rushing`, `OneOnOne`, `Smothered`.

### 3.3.0.3 Ratification mechanism

When Spec #11 reaches `IN REVIEW`, the three #12 GK constants in
`positioning-ai/section-3.md` §3.3.3 and `positioning-ai/section-6.md`
row entries promote `[EST]` → `[GT]` via a #12 patch revision
(v1.0.x), coordinated atomically with #11's `SPEC_INDEX.md` status
flip. The #12 owner co-signs the patch.

### 3.3.0.4 Forward-binding constraint

Any future #12 amendment to `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` /
`GK_LATERAL_FACTOR` is subject to Spec #11 §9.5 cross-spec re-audit
at Stage 1+ tunings.

---

## 3.4 Master Physical Profile Table

Per CLAUDE.md KD-9 / FR-GK-015: every numeric constant referenced
in §3.2–§3.8 has a row with source tag, unit, valid-range, and
citation. The C# constant catalogue at `GoalkeeperConstants.cs`
mirrors this table.

### 3.4.1 GK volume / reach constants

| Constant | Tag | Unit | Value (Stage 0 anchor) | Valid range | Citation |
|----------|-----|------|------------------------|-------------|----------|
| `GK_SAVE_VOLUME_RADIUS_M` | `[GT]` | m | 5.5 | [3.0, 8.0] | §3.5 eligibility |
| `GK_REACTIVE_RADIUS_M` | `[GT]` | m | 1.5 | [0.5, 3.0] | KD-13 / §3.3.0 |
| `ARM_LENGTH_M` | `[GT]` | m | 0.80 | [0.60, 1.00] | §3.3.4 |
| `DIVE_BODY_REACH_EXTENSION_M` | `[GT]` | m | 0.55 | [0.30, 0.90] | §3.3.4 |
| `DIVE_LAUNCH_DISPLACEMENT_M` | `[GT]` | m | 2.2 | [1.0, 4.0] | §3.3.4 |
| `REACH_K_HANDLING` | `[GT]` | m | 0.20 | [0.0, 0.40] | §3.3.4 |
| `REACH_K_AERIAL` | `[GT]` | m | 0.35 | [0.0, 0.60] | §3.3.4 |
| `REACH_K_STRENGTH` | `[GT]` | m | 0.10 | [0.0, 0.30] | §3.3.4 |
| `REACH_FATIGUE_COEFF` | `[GT]` | m | 0.18 | [0.0, 0.40] | §3.3.4 / KD-8 |

### 3.4.2 GK timing / hold-rule constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `GK_HOLD_MAX_TICKS` | `[FIXED]` | ticks @10 Hz | 60 | n/a (rule constant) | Laws of the Game (6-second rule); §3.1 / FR-GK-028 |
| `RECOVERY_COOLDOWN_TICKS` | `[GT]` | ticks @10 Hz | 6 | [2, 20] | §3.1 |
| `ANTICIPATE_THRESHOLD` | `[GT]` | dimensionless | 0.55 | [0.30, 0.80] | §3.1 / #8 |
| `RUSH_COMMIT_THRESHOLD` | `[GT]` | dimensionless | 0.60 | [0.40, 0.85] | §3.1 / §3.7 |
| `ONE_VS_ONE_TRIGGER_RADIUS_M` | `[GT]` | m | 8.0 | [5.0, 14.0] | §3.7 |
| `SMOTHER_TRIGGER_RADIUS_M` | `[GT]` | m | 1.8 | [1.0, 3.0] | §3.7 |
| `BALL_ATTACKING_THIRD_X_M` | `[DERIVED]` | m | `2 · PITCH_LENGTH_M / 3` (= 70.0 m) | derived | §3.1 (v0.2 AR-S1-M1: citation source corrected — derived from Ball Physics #1 §1.2 `PITCH_LENGTH_M`) |

### 3.4.3 Reaction-pipeline constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `REACTION_BASE_MS` | `[GT]` | ms | 350 | [200, 500] | §3.2 |
| `REACTION_REFLEXES_COEFF` | `[GT]` | ms | 100 | [50, 180] | §3.2 |
| `REACTION_BALL_SPEED_COEFF` | `[GT]` | ms / (m/s) | 8 | [3, 18] | §3.2 |
| `REACTION_BALL_SPEED_REF_MPS` | `[GT]` | m/s | 18 | [12, 25] | §3.2 |
| `REACTION_EARLY_TOLERANCE_MS` | `[GT]` | ms | 120 | [60, 200] | §3.2 / KD-18 |
| `REACTION_LATE_TOLERANCE_MS` | `[GT]` | ms | 80 | [40, 140] | §3.2 / KD-18 (numerically smaller) |
| `REFLEXIVE_LABEL_THRESHOLD` | `[GT]` | dimensionless | 0.75 | [0.60, 0.90] | §3.2 / KD-2 |
| `SLUGGISH_LABEL_THRESHOLD` | `[GT]` | dimensionless | 0.30 | [0.15, 0.45] | §3.2 / KD-2 |
| `PERCEPTION_BASE_LATENCY_MS` | `[CROSS]` | ms | (per #7 §3) | n/a | Perception System #7 §3 |
| `PERCEPTION_REFLEXES_SCALE` | `[GT]` | dimensionless | 0.30 | [0.10, 0.50] | §3.2 |
| `ONE_VS_ONE_REACTION_COEFF` | `[GT]` | ms | 40 | [10, 80] | §3.2 / KD-20 — SUBTRACTS from `requiredReactionMs` per §3.2.2 sign convention (positive `OneVsOne_norm` reduces required reaction time) |

### 3.4.4 Dive-kinematics constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `DIVE_LAUNCH_BASE_MPS` | `[GT]` | m/s | 3.5 | [2.0, 5.0] | §3.3 |
| `DIVE_LAUNCH_K_STRENGTH` | `[GT]` | m/s | 1.2 | [0.5, 2.0] | §3.3 |
| `DIVE_LAUNCH_K_AERIAL` | `[GT]` | m/s | 0.8 | [0.3, 1.5] | §3.3 |
| `DIVE_LAUNCH_FATIGUE_COEFF` | `[GT]` | m/s | 0.7 | [0.3, 1.5] | §3.3 / KD-8 |
| `DIVE_PHASE_DURATION_MS` | `[GT]` | ms | 600 | [400, 900] | §3.3 (Stage 0 flat; attribute-scaling deferred per §7.4) |
| `DIVE_PEAK_Z_BASE_M` | `[GT]` | m | 1.20 | [0.80, 1.70] | §3.3 |
| `DIVE_PEAK_Z_K_AERIAL` | `[GT]` | m | 0.70 | [0.30, 1.00] | §3.3 |
| `DIVE_PEAK_Z_K_STRENGTH` | `[GT]` | m | 0.30 | [0.10, 0.60] | §3.3 |
| `DIVE_FATIGUE_PEAK_Z_COEFF` | `[GT]` | m | 0.20 | [0.05, 0.40] | §3.3 / KD-8 |
| `DIVE_TIMING_JITTER_SIGMA_MS` | `[GT]` | ms | 25 | [5, 60] | §3.3 |
| `DIVE_JITTER_PEAK_Z_COEFF` | `[GT]` | m/ms | 0.002 | [0.0005, 0.005] | §3.3 |
| `WRONG_DIRECTION_THRESHOLD_M` | `[GT]` | m | 1.5 | [0.5, 3.0] | §2.3 F-02 |

### 3.4.5 Handling-quality constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `HANDLING_BASE` | `[GT]` | dimensionless | 0.45 | [0.20, 0.70] | §3.5 |
| `HANDLING_K_ATTR` | `[GT]` | dimensionless | 0.45 | [0.20, 0.70] | §3.5 |
| `HANDLING_K_BALL_SPEED` | `[GT]` | dimensionless | 0.025 | [0.0, 0.06] | §3.5 (v0.2 AR-S1-H3: unit corrected — formula divides by ref so the constant is dimensionless) |
| `HANDLING_BALL_SPEED_REF_MPS` | `[GT]` | m/s | 20 | [12, 28] | §3.5 |
| `HANDLING_FATIGUE_COEFF` | `[GT]` | dimensionless | 0.20 | [0.05, 0.40] | §3.5 / KD-8 |
| `HANDLING_NOISE_SIGMA` | `[GT]` | dimensionless | 0.06 | [0.0, 0.15] | §3.5 / KD-7 |
| `HANDLING_POINT_ERROR_SIGMA_M` | `[GT]` | m | 0.05 | [0.01, 0.12] | §3.5 / KD-7 |
| `HANDLING_REACTION_BLEND_ALPHA` | `[GT]` | dimensionless | 0.70 | [0.40, 0.90] | §3.5 / KD-2 |
| `CATCH_THRESHOLD` | `[GT]` | dimensionless | 0.78 | [0.65, 0.90] | §3.5 / KD-21 |
| `PARRY_THRESHOLD` | `[GT]` | dimensionless | 0.55 | [0.40, 0.70] | §3.5 / KD-21 |
| `DEFLECT_THRESHOLD` | `[GT]` | dimensionless | 0.30 | [0.20, 0.45] | §3.5 / KD-21 |
| `MIN_HANDLING_QUALITY` | `[GT]` | dimensionless | 0.10 | [0.05, 0.25] | §3.5 |
| `PARRY_VELOCITY_RETAIN_BASE` | `[GT]` | dimensionless | 0.45 | [0.20, 0.70] | §3.5 |
| `PARRY_VELOCITY_RETAIN_K_QUALITY` | `[GT]` | dimensionless | 0.30 | [0.0, 0.60] | §3.5 |
| `PARRY_DEFLECT_ANGLE_SIGMA_RAD` | `[GT]` | rad | 0.20 | [0.05, 0.50] | §3.5 |
| `CLUTCH_FIRMNESS_K_RETAIN` | `[GT]` | dimensionless | 0.30 | [0.0, 0.60] | §3.5 |
| `ONE_VS_ONE_HANDLING_COEFF` | `[GT]` | dimensionless | 0.12 | [0.0, 0.25] | §3.5 / KD-20 |

### 3.4.6 Rush-dispatch constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `RUSH_LAUNCH_BASE_MPS` | `[GT]` | m/s | 4.5 | [3.0, 6.5] | §3.7 |
| `RUSH_LAUNCH_K_PACE` | `[GT]` | m/s | 1.8 | [1.0, 3.0] | §3.7 |
| `RUSH_COMMIT_FATIGUE_COEFF` | `[GT]` | m/s | 0.9 | [0.3, 1.8] | §3.7 / KD-8 |

### 3.4.7 Distribution-geometry constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `THROW_RELEASE_HEIGHT_M` | `[GT]` | m | 1.95 | [1.60, 2.20] | §3.8 |
| `ROLL_RELEASE_HEIGHT_M` | `[GT]` | m | 0.15 | [0.05, 0.30] | §3.8 |
| `KICK_RELEASE_HEIGHT_M` | `[GT]` | m | 0.20 | [0.10, 0.35] | §3.8 |
| `THROW_WINDUP_MS` | `[GT]` | ms | 700 | [400, 1100] | §3.8 |
| `ROLL_WINDUP_MS` | `[GT]` | ms | 400 | [250, 700] | §3.8 |
| `KICK_WINDUP_MS` | `[GT]` | ms | 900 | [600, 1400] | §3.8 |
| `THROW_ACCURACY_COEFF` | `[GT]` | dimensionless | 0.85 | [0.5, 1.0] | §3.8.1 (v0.2 AR-S1-M3) — multiplies `powerIntent` by `coeff · Throwing_norm` |
| `KICK_ACCURACY_COEFF` | `[GT]` | dimensionless | 0.85 | [0.5, 1.0] | §3.8.1 (v0.2 AR-S1-M3) — multiplies `powerIntent` by `coeff · Kicking_norm` |

### 3.4.8 Cross-claim duel constants

| Constant | Tag | Unit | Value | Valid range | Citation |
|----------|-----|------|-------|-------------|----------|
| `CROSS_CLAIM_VOLUME_RADIUS_M` | `[GT]` | m | 2.2 | [1.4, 3.5] | §3.6 |
| `CROSS_CLAIM_DUEL_BALANCE_W` | `[GT]` | dimensionless | 0.20 | [0.10, 0.40] | §3.6 |
| `CROSS_CLAIM_DUEL_STRENGTH_W` | `[GT]` | dimensionless | 0.35 | [0.15, 0.55] | §3.6 |
| `CROSS_CLAIM_DUEL_AERIAL_W` | `[GT]` | dimensionless | 0.45 | [0.25, 0.65] | §3.6 (weights sum to 1.0) |
| `CROSS_CLAIM_TIEBREAK_EPSILON` | `[GT]` | dimensionless | 0.03 | [0.005, 0.10] | §3.6 |
| `CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE` | `[GT]` | dimensionless | 0.015 | [0.0, 0.05] | §3.6 / KD-7 |

### 3.4.9 Project-invariant cross-references

| Constant | Tag | Unit | Value | Source |
|----------|-----|------|-------|--------|
| `FRAME_MS` | `[DERIVED]` | ms | `1000 / TICK_RATE_PHYSICS_HZ` (= 16.667 ms) | CLAUDE.md |
| `GRAVITY_MPS2` | `[CROSS]` | m/s² | 9.81 | Ball Physics #1 |
| `PITCH_LENGTH_M` | `[CROSS]` | m | 105.0 | Ball Physics #1 §1.2 |
| `PITCH_WIDTH_M` | `[CROSS]` | m | 68.0 | Ball Physics #1 §1.2 |
| `PENALTY_AREA_DEPTH_M` | `[CROSS]` | m | 16.5 | Ball Physics #1 (anchor pinned during drafting) |
| `TICK_RATE_TACTICAL_HZ` | `[CROSS]` | Hz | 10 | CLAUDE.md |
| `TICK_RATE_PHYSICS_HZ` | `[CROSS]` | Hz | 60 | CLAUDE.md |
| `DOMAIN_TAG_GOALKEEPER` | `[CROSS: #16 §3.4]` | byte | `0x1D` | Deterministic Simulation #16 §3.4 v1.0.5 (May 18, 2026) — ERR-011-001 resolved; value `0x1D` (shifted from proposed `0x17` because Positioning AI #12 reached `APPROVED` first on May 18, 2026, claiming `0x17` per first-to-`APPROVED` precedent) |

Inventory discipline (KD-9 / FR-GK-042): every symbol that appears
in §3.2–§3.8 pseudocode bodies is either a row above or an
explicitly named per-call output / local variable in the relevant
subsection prose.

---

## 3.5 Handling-Quality Scalar (KD-1, KD-2, KD-21)

**Inputs.** Predicted hand contact point; actual hand contact point
(from #3 §3.4.2 contact event); ball speed at contact; GK
`Handling_norm`, `OneVsOne_norm`; current `fatigue`;
`reactionWindowAchieved` from §3.2; `clutchFirmness` from intent;
current state.

**Outputs.** `handlingQualityScalar ∈ [0, 1]`; one of `Ball.SetPossessor`
(catch) or `Ball.ApplyKick` (parry / deflect / spill) or
`SaveAttemptedEvent` with `failureCause` (miss); telemetry label.

### 3.5.1 Per-component computation

```
handlingScaleNoise   = HANDLING_NOISE_SIGMA
                     · rng.NextGaussian(DRAW_SITE_HANDLING_NOISE,
                                         DOMAIN_TAG_GOALKEEPER)

pointErrorNoise      = HANDLING_POINT_ERROR_SIGMA_M
                     · rng.NextGaussian(DRAW_SITE_HANDLING_POINT_NOISE,
                                         DOMAIN_TAG_GOALKEEPER)

contactPointError    = norm(handContactActual - targetHandContact)
                     + pointErrorNoise

pointQuality         = 1 - clamp01(contactPointError /
                                    HANDLING_POINT_ERROR_SIGMA_M)

speedFactor          = clamp01(1 - HANDLING_K_BALL_SPEED ·
                                   max(0, ballSpeed -
                                          HANDLING_BALL_SPEED_REF_MPS) /
                                          HANDLING_BALL_SPEED_REF_MPS)

attrFactor           = HANDLING_BASE
                     + HANDLING_K_ATTR · Handling_norm
                     - HANDLING_FATIGUE_COEFF · fatigue
                     + (state == OneOnOne
                          ? ONE_VS_ONE_HANDLING_COEFF · OneVsOne_norm
                          : 0)

rawHandling          = attrFactor · speedFactor · pointQuality
                     + handlingScaleNoise

handlingQualityScalar = clamp01(
                          HANDLING_REACTION_BLEND_ALPHA · rawHandling
                        + (1 - HANDLING_REACTION_BLEND_ALPHA)
                          · reactionWindowAchieved)
```

**Note (KD-7 single-purpose-per-site rule).** `pointErrorNoise` is
drawn from `DRAW_SITE_HANDLING_POINT_NOISE`, a SEPARATE draw site
from `DRAW_SITE_HANDLING_NOISE`. Sharing a draw site would entangle
two independent error sources (handling-scale noise vs.
point-error noise), violating #16 §4.5 draw-site registry's
single-purpose-per-site rule.

### 3.5.2 Band-to-action mapping (KD-21)

```
if handlingQualityScalar >= CATCH_THRESHOLD:
    Ball.SetPossessor(gkId)
    ball.velocity = gkHandVelocity         // parked at hand position
    label = Caught
elif handlingQualityScalar >= PARRY_THRESHOLD:
    Ball.ApplyKick(parryVelocity(quality, clutchFirmness),
                    incomingSpin, gkId, t)
    label = Parried
elif handlingQualityScalar >= DEFLECT_THRESHOLD:
    Ball.ApplyKick(deflectVelocity(quality, deflectionTarget),
                    incomingSpin, gkId, t)
    label = Deflected
elif handlingQualityScalar >= MIN_HANDLING_QUALITY:
    Ball.ApplyKick(spillVelocity(quality), incomingSpin, gkId, t)
    label = Spilled
else:
    // contact eligibility satisfied but quality below floor —
    // route to F-01..F-03 per detection conditions
    publish SaveAttemptedEvent with failureCause; ball unchanged
    label = Missed
```

### 3.5.3 Closed-form helpers

```
parryVelocity(quality, clutchFirmness):
    retain = PARRY_VELOCITY_RETAIN_BASE
           - PARRY_VELOCITY_RETAIN_K_QUALITY · quality
           - CLUTCH_FIRMNESS_K_RETAIN · clutchFirmness
    retain = clamp01(retain)
    deflectAngle = PARRY_DEFLECT_ANGLE_SIGMA_RAD · (1 - quality)
    return rotate(incomingVelocity · -retain, deflectAngle)

deflectVelocity(quality, deflectionTarget):
    retain = clamp01(PARRY_VELOCITY_RETAIN_BASE
                    - PARRY_VELOCITY_RETAIN_K_QUALITY · quality
                    + 0.10)                    // deflections retain slightly more
    targetDir = normalize(deflectionTarget - handContactActual)
    speed = retain · norm(incomingVelocity)
    return speed · targetDir

spillVelocity(quality):
    retain = PARRY_VELOCITY_RETAIN_BASE + 0.20  // spills retain more (poor handling)
    // v0.2 (AR-S1-H2): no Gaussian here — the upstream §3.5.1
    // handlingScaleNoise + pointErrorNoise already capture spill
    // variability. A second Gaussian here would violate KD-7
    // single-purpose-per-site rule for DRAW_SITE_HANDLING_NOISE.
    deflectAngle = PARRY_DEFLECT_ANGLE_SIGMA_RAD · (1 - quality)
    return rotate(incomingVelocity · -retain, deflectAngle)
```

**Monotonicity invariant (Appendix A.2).** `parryVelocity` retain is
strictly decreasing in `quality` (better quality → less rebound).
`deflectVelocity` retain is bounded below `parryVelocity` retain at
matched quality (deflection retains slightly more by construction).
`spillVelocity` retain exceeds parry retain at all quality (poor
handling preserves more incoming energy).

### 3.5.4 Worked example

**Scenario.** 22 m/s shot incoming; `handContactError = 0.03 m`
(close to target); `Handling_norm = 0.70`; `fatigue = 0.20`;
`reactionWindowAchieved = 0.70`; not in `OneOnOne`;
`clutchFirmness = 0.85`. Noise draws ≈ 0 for illustration.

Compute (using §3.4.5 anchors):
- `pointQuality = 1 - clamp01(0.03 / 0.05) = 1 - 0.60 = 0.40`.
- `speedFactor = clamp01(1 - 0.025 · 2 / 20) = clamp01(0.9975) = 0.9975`.
- `attrFactor = 0.45 + 0.45 · 0.70 - 0.20 · 0.20 = 0.45 + 0.315 - 0.04 = 0.725`.
- `rawHandling = 0.725 · 0.9975 · 0.40 = 0.289`.
- `handlingQualityScalar = clamp01(0.70 · 0.289 + 0.30 · 0.70)
                          = clamp01(0.202 + 0.21) = 0.412`.

Result: `0.412 < PARRY_THRESHOLD (0.55)` and `≥ DEFLECT_THRESHOLD
(0.30)` → label `Deflected`. The GK gets a hand on it but cannot
control the rebound; `Ball.ApplyKick` is invoked with
`deflectVelocity`. The poor `pointQuality` (0.40) is the dominant
factor — even an excellent reaction (`reactionWindowAchieved =
0.70`) and a strong attribute factor (0.725) cannot fully
compensate.

---

## 3.6 Cross-Claim & Aerial Duel Resolution (KD-14)

Cross / aerial / 1v1 duels among ≥2 agents within
`CROSS_CLAIM_VOLUME_RADIUS_M`. Algorithm mirrors Heading #10 §3.7
structure so the duel arithmetic is consistent across specs.

### 3.6.1 Body-part determination (KD-14)

```
// Stage 0 approximation: capsule-vs-sphere intersection priority
for each agent in candidates:
    handCapsuleHit = #3.IntersectsBallSphere(agent.handCapsule, ballSphere)
    headSphereHit  = #3.IntersectsBallSphere(agent.headSphere,  ballSphere)
    if handCapsuleHit AND headSphereHit:
        // priority by Z proximity to ball center
        agent.contactBodyPart = (|ball.z - agent.handZ| < |ball.z - agent.headZ|)
                                  ? Hand : Head
    elif handCapsuleHit: agent.contactBodyPart = Hand
    elif headSphereHit:  agent.contactBodyPart = Head
    else:                agent.contactBodyPart = None
```

Body part is determined by physical geometry, NOT by intent
(FR-GK-022).

**Surface citations (v0.2 AR-S1-M4).** `agent.handCapsule` and
`agent.headSphere` are Collision System #3 agent-shape colliders
(per #3 collider geometry definitions consumed via the agent's
`Agent` reference per #2 §3.5.1). `agent.handZ` and `agent.headZ`
derive from #3 collider centroids at the current frame.
`#3.IntersectsBallSphere` is the standard `ICollisionEventConsumer`
collision-query helper (#3 §3.4.2).

### 3.6.2 Routing

```
if winningAgent.contactBodyPart == Head:
    defer to Heading #10 §3.7 duel mechanism
    // #11 emits no SaveAttemptedEvent; #10 emits HeaderExecutedEvent
    return
// else: contact body part == Hand → resolve here
```

### 3.6.3 Hand-contact duel resolution

Iterate participants in #16 §3.2 entity order:

```
for agent in participants (in #16 §3.2 entity order):
    baseScore[agent] = CROSS_CLAIM_DUEL_BALANCE_W  · agent.Balance_norm
                     + CROSS_CLAIM_DUEL_STRENGTH_W · agent.Strength_norm
                     + CROSS_CLAIM_DUEL_AERIAL_W   · agent.Aerial_norm

rank participants by baseScore (descending; #16 §3.2 entity order
                                 for stable secondary sort)

// near-tie tiebreak — applied ONLY when scores are close
top, second = top two participants
if (baseScore[top] - baseScore[second]) < CROSS_CLAIM_TIEBREAK_EPSILON:
    perturbation = CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE
                 · rng.NextGaussian(DRAW_SITE_CROSS_CLAIM_TIEBREAK,
                                     DOMAIN_TAG_GOALKEEPER)
    baseScore[top]    += perturbation
    baseScore[second] -= perturbation
    re-rank
winner = participant with highest baseScore
```

### 3.6.4 Outcome emission

```
if winner == gk:
    invoke §3.5 handling-quality pipeline with winner as contacting agent
    emit BallClaimedEvent (if Caught) or SaveAttemptedEvent
         (if Parried / Deflected / Spilled / Missed)
else:
    winner runs its own subsystem (#10 §3.7 head; #4 / #5 / #6 body)
    for loser in {participants \ {winner}}:
        emit SaveAttemptedEvent with failureCause = DisturbedInDuel
              (if loser is gk)
        // outfielder losers emit their subsystem's equivalent failure event
```

### 3.6.5 Worked example

**Scenario.** GK + attacking striker contesting a corner;
`gk.Balance_norm = 0.70`, `gk.Strength_norm = 0.75`,
`gk.Aerial_norm = 0.80`; `striker.Balance_norm = 0.65`,
`striker.Strength_norm = 0.80`, `striker.Aerial_norm = 0.78`.

- `baseScore[gk] = 0.20 · 0.70 + 0.35 · 0.75 + 0.45 · 0.80 = 0.140 + 0.263 + 0.360 = 0.763`.
- `baseScore[striker] = 0.20 · 0.65 + 0.35 · 0.80 + 0.45 · 0.78 = 0.130 + 0.280 + 0.351 = 0.761`.
- Difference: `0.002 < 0.03 = CROSS_CLAIM_TIEBREAK_EPSILON`. Tiebreak invoked.
- Gaussian draw `g ~ N(0, 1)`; perturbation `= 0.015 · g`.
- For `g = +0.6`: `gk` becomes `0.763 + 0.009 = 0.772`; `striker`
  becomes `0.761 − 0.009 = 0.752`. GK wins.
- For `g = -0.7`: `gk` becomes `0.763 − 0.0105 = 0.7525`; `striker`
  becomes `0.761 + 0.0105 = 0.7715`. Striker wins.

The tiebreak preserves deterministic replay (same RNG seed → same
winner) while breaking ties with a tunable noise amplitude.

---

## 3.7 Rush / Sweep Dispatch (KD-15)

**State entry.** Decision Tree #8 `RushIntent` with
`commitmentLevel > RUSH_COMMIT_THRESHOLD` at the 10 Hz tactical
tick.

### 3.7.1 Launch impulse

```
rushLaunchMps = RUSH_LAUNCH_BASE_MPS
              + RUSH_LAUNCH_K_PACE · Pace_norm
              - RUSH_COMMIT_FATIGUE_COEFF · fatigue
```

`Pace_norm` is read from AM #2 `PlayerAttributes`.

### 3.7.2 Per-frame update

```
on PhysicsFrame(currentFrame) while state == Rushing:
    // intent-staleness policy (KD-15): rushTarget NOT re-read
    desiredDir = normalize(rushTarget - gkPos)
    gkPos     += desiredDir · rushLaunchMps · FRAME_MS / 1000

    // F-08 ball-interception check
    if BallState.PossessorId != null
       AND BallState.PossessorId != gkId
       AND BallState.PossessorId != initialAttackerTargetId:
        state = Recovering
        emit GoalkeeperRushEvent { rushPhase: Aborted,
                                    abortReason: BallIntercepted }
        return

    // contact check — hand contact triggers Smothered
    if #3 hand-ball contact event present for gk:
        state = Smothered
        // §3.5 handling-quality pipeline executes with OneVsOne
        //   coefficients if also in OneOnOne phase (KD-20)

    // 1v1 trigger
    if existsAttackerWithBallWithinRadius(ONE_VS_ONE_TRIGGER_RADIUS_M):
        state = OneOnOne
```

### 3.7.3 Rush abort policy (KD-15)

The ONLY abort trigger is F-08 (`BallIntercepted`). Ball-trajectory
changes during the rush do NOT cause abort; `rushTarget` is locked
on commit. This intent-staleness policy mirrors Heading #10 KD-17.

`Rushing → Recovering` on F-08 abort emits
`GoalkeeperRushEvent.abortReason = BallIntercepted`.
`AttackerBeatGK` is reserved for the case where the attacker
passes the GK without contact (rare; emitted on attacker
reaching pitch coordinates past `gkPos.x` while still in
`Rushing` state).

---

## 3.8 Distribution Generation (KD-6, KD-16)

Decision Tree #8 supplies `DistributeIntent` at the 10 Hz tactical
tick once the GK enters `HandsOnBall` state and
`releaseTickEarliest` has passed.

### 3.8.1 Release-point geometry (KD-16)

```
releaseHeight = match distributeIntent.deliveryKind:
                  Throw → THROW_RELEASE_HEIGHT_M
                  Roll  → ROLL_RELEASE_HEIGHT_M
                  Kick  → KICK_RELEASE_HEIGHT_M

windupMs      = match distributeIntent.deliveryKind:
                  Throw → THROW_WINDUP_MS
                  Roll  → ROLL_WINDUP_MS
                  Kick  → KICK_WINDUP_MS

// v0.2 AR-S1-M3: distribution-accuracy attribute modulation
accuracyCoeff = match distributeIntent.deliveryKind:
                  Throw → THROW_ACCURACY_COEFF · Throwing_norm
                  Roll  → 1.0
                  Kick  → KICK_ACCURACY_COEFF  · Kicking_norm

releasePoint  = gkPosition + Vector3(0, 0, releaseHeight)
emittedPowerIntent = distributeIntent.powerIntent · accuracyCoeff
```

### 3.8.2 Target validation (F-05, F-09)

```
if distributeIntent.targetReceiverId != null
   AND !agentRoster.contains(distributeIntent.targetReceiverId):
    // F-05: receiver substituted between commit and release
    distributeIntent.targetReceiverId = null
    distributeIntent.targetPoint     = lastKnownReceiverPosition
    emit telemetry warning "gk.distribution.target_receiver_missing"

if distributeIntent.targetPoint.x ∉ [0, PITCH_LENGTH_M]
   OR distributeIntent.targetPoint.y ∉ [0, PITCH_WIDTH_M]:
    // F-09: clamp to in-bounds
    distributeIntent.targetPoint = clampToInBounds(distributeIntent.targetPoint)
    emit telemetry warning "gk.distribution.target_out_of_bounds"
```

### 3.8.3 PassIntent emission

```
passIntent = PassIntent {
    sourceAgentId     = gkId,
    sourcePoint       = releasePoint,
    targetPoint       = distributeIntent.targetPoint,
    targetReceiverId  = distributeIntent.targetReceiverId,
    powerIntent       = emittedPowerIntent,
    spinIntent        = distributeIntent.spinIntent,
    deliveryKind      = mapToPassMechanicsDelivery(distributeIntent.deliveryKind)
}

PassMechanics.ConsumePassIntent(passIntent)         // #5 §3 intent surface
emit DistributionExecutedEvent { ..., releasePoint, windupDurationMs: windupMs }

state machine: Distributing → Recovering
```

### 3.8.4 `mapToPassMechanicsDelivery`

One-to-one structural mapping:

```
Throw → PassMechanics.DeliveryKind.LowDriven   (sub-cross height)
Roll  → PassMechanics.DeliveryKind.GroundRoll
Kick  → PassMechanics.DeliveryKind.Lofted
```

The mapping is structural only; #5 owns the resulting trajectory.

---

## 3.9 Failed-Save Pipeline

Failure modes F-01…F-04 trigger structural failure emission per
KD-11.

```
on failure detected (F-01 | F-02 | F-03 | F-04):
    // do NOT modify ball state
    // do NOT invoke Ball.ApplyKick
    // do NOT invoke Ball.SetPossessor

    emit SaveAttemptedEvent {
        agentId          = gkId,
        matchTime        = currentMatchTime,
        saveIntent       = ...,
        reactionWindowAchieved,
        handlingQualityScalar,
        handlingQualityLabel = Missed,
        reactionLabel,
        contactPoint     = handContactActual ?? predictedContactPoint,
        incomingBallState,
        outgoingBallVelocity = incomingBallState.velocity,  // unchanged
        outgoingBallSpin     = incomingBallState.spin,
        contestedDuelId,
        failureCause     = MistimedDive | WrongDirection | OutOfReach | DisturbedInDuel,
        contactBodyPart  = Hand
    }
    state machine: (Diving | Airborne | Anticipate | Smothered) → Recovering
```

F-07 (non-eligible state) does NOT emit `SaveAttemptedEvent`; the
ball-vs-GK contact (if any) is resolved by Collision System #3
standard rebound physics.

---

## 3.10 Boundary Algorithms

- **Boundary with Heading #10** (KD-4, KD-14): single predicate
  `if contactBodyPart == Head → Spec #10 §3.7 duel mechanism /
  §3.5 contact-quality pipeline`. No #11-local head-physics path
  exists.
- **Boundary with Positioning AI #12** (KD-3, KD-13): §3.3.0
  consumer contract; `gkBaselineSlot` consumed read-only;
  micro-adjustment within `GK_REACTIVE_RADIUS_M` is #11-owned.
- **Boundary with Pass Mechanics #5** (KD-6, KD-16): release-point
  geometry here (§3.8.1); trajectory there.
- **Boundary with Collision System #3** (KD-5): contact-event API
  consumed read-only via `ICollisionEventConsumer` (#3 §3.4.2);
  contact normal, relative velocity, impulse budget never
  redefined locally.

---

## 3.11 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; state machine (24 transitions), reaction pipeline, dive kinematics, §3.3.0 #12 consumer contract, master constants table (~70 rows / 8 subsections), handling-quality scalar with band-to-action mapping, cross-claim duel, rush dispatch, distribution generation, failed-save pipeline, boundary algorithms | self-pass-1 in `adversarial-review-section-files-v1.md` |
| 0.2 | May 16, 2026 | pass-1 fix pass | Resolves AR-S1-H2 (spillVelocity Gaussian removed — KD-7 single-purpose-per-site); AR-S1-H3 (HANDLING_K_BALL_SPEED unit corrected `per m/s` → `dimensionless`); AR-S1-M1 (BALL_ATTACKING_THIRD_X_M citation corrected to Ball Physics #1 §1.2); AR-S1-M3 (Throwing/Kicking attribute consumption wired in §3.8.1; THROW_ACCURACY_COEFF + KICK_ACCURACY_COEFF added to §3.4.7); AR-S1-M4 (§3.6.1 collider-surface citations to #3 added); AR-S1-M5 (`Recovering → Set` trigger amended to OR); AR-S1-L2 (`ONE_VS_ONE_REACTION_COEFF` sign documented in §3.4.3); AR-S1-L3 (`DOMAIN_TAG_GOALKEEPER` source-column references `ERR-011-001` explicitly) | self-pass-2 self-critique on v0.2 yields no further findings |
| 0.3 | June 14, 2026 | impl AR-3 fix pass | §3.3.1 / §3.3.4 lateral dive axis corrected X → Y. The goal mouth spans the Y axis (touchline-to-touchline) per §1.2, so `diveDirectionX = sign(targetHandX − gkX)` and the `reachCenter` X displacement dived the keeper toward/away from its own goal instead of across the goal mouth — shots placed wide in Y were unreachable. Now `diveDirectionY = sign(targetHandY − gkY)` and `reachCenter` displaces along Y with `gkPos.x` fixed. Same axis-error defect class as Ball Physics ERR-001-001 / Decision Tree ERR-008-003. Code: `GoalkeeperDiveKinematics.cs` v1.1, `GoalkeeperMechanics.cs` v1.4 | implementation adversarial review |
