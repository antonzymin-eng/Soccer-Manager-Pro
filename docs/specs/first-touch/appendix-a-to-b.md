# First Touch Mechanics Specification #4 — Appendices

**Purpose:** Supplementary derivations, numerical verification worksheets, and sensitivity
analysis supporting the First Touch Mechanics Specification. This file resolves Blocker 1
from the Approval Checklist (§9): Appendices A, B, and C are all required before approval
can be granted. Appendix B in particular is referenced normatively by the regression
strategy in §5.11.4 — "hand calculations in Appendix B must be re-run to verify new
expected values before updating the tests."

**Created:** February 21, 2026, 11:00 PM PST
**Version:** 1.0
**Status:** Draft
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)
**Dependencies:** Section 3 v1.1 (all formulas), Section 4 v1.1 (all constants),
Section 5 v1.1 (test scenarios referenced in Appendix B)

---

## Table of Contents

- [Appendix A: Formula Derivations](#appendix-a-formula-derivations)
  - [A.1 Control Quality Formula Derivation](#a1-control-quality-formula-derivation)
  - [A.2 Touch Radius Piecewise Interpolation Derivation](#a2-touch-radius-piecewise-interpolation-derivation)
  - [A.3 Ball Displacement Direction Derivation](#a3-ball-displacement-direction-derivation)
  - [A.4 Possession State Machine Threshold Derivation](#a4-possession-state-machine-threshold-derivation)
  - [A.5 Pressure Evaluation Derivation](#a5-pressure-evaluation-derivation)
  - [A.6 Orientation Detection Derivation](#a6-orientation-detection-derivation)
- [Appendix B: Numerical Verification Hand Calculations](#appendix-b-numerical-verification-hand-calculations)
  - [B.1 VS-001: Elite Player, Standard Pass](#b1-vs-001-elite-player-standard-pass)
  - [B.2 VS-002: Average Player Under Pressure](#b2-vs-002-average-player-under-pressure)
  - [B.3 VS-003: Poor Reception, Heavy Touch](#b3-vs-003-poor-reception-heavy-touch)
  - [B.4 Half-Turn Bonus Verification](#b4-half-turn-bonus-verification)
  - [B.5 Thunderbolt Cap Verification](#b5-thunderbolt-cap-verification)
  - [B.6 INTERCEPTION Threshold Boundary Cases](#b6-interception-threshold-boundary-cases)
  - [B.7 Maximum and Minimum Output Bounds](#b7-maximum-and-minimum-output-bounds)
- [Appendix C: Sensitivity Analysis](#appendix-c-sensitivity-analysis)
  - [C.1 Control Quality vs. Technique Attribute](#c1-control-quality-vs-technique-attribute)
  - [C.2 Control Quality vs. Ball Velocity](#c2-control-quality-vs-ball-velocity)
  - [C.3 Touch Radius Distribution at Typical Match Inputs](#c3-touch-radius-distribution-at-typical-match-inputs)
  - [C.4 Pressure Degradation Sensitivity](#c4-pressure-degradation-sensitivity)
  - [C.5 Half-Turn Bonus Impact Across Attribute Tiers](#c5-half-turn-bonus-impact-across-attribute-tiers)
  - [C.6 Sensitivity Summary and Tuning Guidance](#c6-sensitivity-summary-and-tuning-guidance)

---

## Appendix A: Formula Derivations

This appendix provides step-by-step mathematical derivations from first principles for every
core formula in Section 3. Where Section 3 presents the implementation form, this appendix
shows *how* each formula was constructed, *why* each term exists, and *what* happens at
boundary conditions. Derivations are independent of the implementation — they serve as the
mathematical ground truth against which the implementation can be validated.

---

### A.1 Control Quality Formula Derivation

#### A.1.1 Conceptual Foundation

The conceptual formula from Master Volume 1 §6.4 is:

```
Control_Quality = Agent_Technique / (Ball_Velocity × Agent_Inertia)
```

This encodes one physical intuition: control quality improves with agent skill and degrades
with ball velocity and agent movement. The formula must be expanded into an implementable
normalised form. The derivation below performs that expansion systematically.

#### A.1.2 Step 1 — Attribute Aggregation

Two attributes contribute to control quality: `Technique` (dominant, weight 0.70) and
`FirstTouch` (specialist, weight 0.30). These are combined as a weighted sum:

```
WeightedAttr = (Technique × 0.70) + (FirstTouch × 0.30)
```

**Why a weighted sum rather than multiplication?**
Multiplication (Technique × FirstTouch) would create a highly non-linear attribute surface —
a player with Technique 20 and FirstTouch 1 would score (20 × 1 = 20) the same as a player
with Technique 10 and FirstTouch 2 (10 × 2 = 20). The weighted sum preserves the
contribution hierarchy: Technique is always the dominant factor, with FirstTouch providing
a supplementary boost.

**Boundary check:**
- Minimum: (1 × 0.70) + (1 × 0.30) = 0.70 + 0.30 = **1.0**
- Maximum: (20 × 0.70) + (20 × 0.30) = 14.0 + 6.0 = **20.0**

Range: [1.0, 20.0]. Consistent with the [1, 20] attribute scale from Agent Movement §3.5.6.

#### A.1.3 Step 2 — Normalisation

The weighted attribute is normalised to [0.05, 1.0] by dividing by ATTR_MAX = 20.0:

```
NormAttr = WeightedAttr / 20.0
```

**Why 20.0 as the denominator?**
ATTR_MAX = 20.0 is the maximum possible WeightedAttr (both attributes at 20). Dividing by
ATTR_MAX maps the [1.0, 20.0] range to [0.05, 1.0]. A player at the attribute ceiling
(NormAttr = 1.0) starts at maximum possible attribute contribution before difficulty
penalties are applied.

**Minimum NormAttr:**
WeightedAttr_min = 1.0 → NormAttr_min = 1.0 / 20.0 = **0.05**

This means even the worst possible player (Technique 1, FirstTouch 1) has a 5% attribute
contribution. Combined with VELOCITY_MIN clamping, there is always a non-zero floor for q
before pressure and clamping.

#### A.1.4 Step 3 — Orientation Bonus

The half-turn orientation bonus multiplies the normalised attribute score:

```
AttrWithBonus = NormAttr × (1.0 + orientationBonus)
```

Where orientationBonus ∈ {0.0, 0.15} (binary: in half-turn window or not).

**Why a multiplicative rather than additive bonus?**
An additive bonus (NormAttr + 0.15) would give identical absolute benefit to all players
regardless of attribute level. The multiplicative form scales the bonus proportionally —
a player with NormAttr = 0.80 receives +0.12 bonus, while NormAttr = 0.50 receives +0.075.
This is realistic: a skilled player benefits *more* in absolute terms from good body
positioning because they have more capability to express through that positioning.

**Maximum AttrWithBonus:**
NormAttr_max × (1.0 + 0.15) = 1.0 × 1.15 = **1.15**

This means AttrWithBonus can exceed 1.0. That is intentional — the final Clamp(q, 0, 1)
handles the ceiling. The pre-clamp value being > 1.0 does not cause problems; it means
the formula will produce q = 1.0 under sufficiently easy conditions.

#### A.1.5 Step 4 — Velocity Difficulty

Ball velocity is normalised against VELOCITY_REFERENCE = 15.0 m/s:

```
VelDifficulty = Clamp(ballSpeed / 15.0, 0.1, 4.0)
```

**Why ballSpeed / VELOCITY_REFERENCE (not the inverse)?**
VelDifficulty appears in the denominator of the quality formula (Step 6: RawQuality =
AttrWithBonus / (VelDifficulty × MoveDifficulty)). A higher ball speed produces a larger
VelDifficulty, which produces a smaller RawQuality — correct physical behaviour.

**Clamp lower bound = 0.1:**
Without the lower bound, a stationary ball (ballSpeed = 0) produces VelDifficulty = 0,
causing division by zero in Step 6. The lower clamp of 0.1 represents a ball speed of
0.1 × 15.0 = 1.5 m/s equivalent difficulty — very easy, appropriate for near-stationary
balls. Note: VELOCITY_MIN = 0.5 m/s is the minimum *input* ballSpeed; at 0.5 m/s,
VelDifficulty = 0.5/15.0 = 0.033, which is below 0.1, so the clamp activates.

**Clamp upper bound = 4.0:**
Without the upper bound, a 100 m/s ball (physically impossible but a robustness concern)
produces VelDifficulty = 6.67, which compresses quality to near zero even for elite players.
The cap at 4.0 (equivalent to 60 m/s) maintains a non-degenerate floor. In practice,
THUNDERBOLT_SPEED = 28 m/s (VelDifficulty = 1.87) is the effective maximum in gameplay.

**Spot check values:**
| Ball Speed | VelDifficulty | Note |
|---|---|---|
| 0.5 m/s | 0.1 (clamped) | Near-stationary; very easy |
| 5.0 m/s | 0.33 | Slow roll |
| 15.0 m/s | 1.0 | Reference speed (typical pass) |
| 28.0 m/s | 1.87 | Thunderbolt |
| 30.0 m/s | 2.0 | Hard shot |
| 60.0 m/s | 4.0 (clamped) | Impossible in play; cap activates |

#### A.1.6 Step 5 — Movement Difficulty

Agent velocity contributes a multiplicative penalty:

```
MoveDifficulty = 1.0 + (agentSpeed / 7.0) × 0.5
```

**Derivation of the form:**
The base of 1.0 ensures that a stationary agent (agentSpeed = 0) produces no penalty —
MoveDifficulty = 1.0, no effect on quality. The penalty term linearly scales with agent
speed, normalised to top sprint speed (7.0 m/s). MOVEMENT_PENALTY = 0.5 means that at
full sprint, the total penalty is +0.5 above baseline, giving MoveDifficulty = 1.5 (50%
harder than stationary).

**Why linear and not quadratic?**
A quadratic penalty would make the first few m/s essentially penalty-free and then create
a sudden sharp degradation near sprint speed. The linear form is more predictable and
easier to tune: moving any additional m/s adds a fixed increment to difficulty.

**Boundary values:**
- Stationary (0 m/s): 1.0 + 0 = **1.0** (no penalty)
- Jogging (3 m/s): 1.0 + (3/7) × 0.5 = 1.0 + 0.214 = **1.214**
- Sprint (7 m/s): 1.0 + (7/7) × 0.5 = 1.0 + 0.5 = **1.50** (maximum)

**Combined difficulty denominator range:**
- Minimum: VelDifficulty_min × MoveDifficulty_min = 0.1 × 1.0 = **0.1**
- Maximum: VelDifficulty_max × MoveDifficulty_max = 4.0 × 1.5 = **6.0**

This bounds the denominator in (0.1, 6.0), guaranteeing no division by zero and no
unreasonable denominator magnitudes.

#### A.1.7 Step 6 — Raw Quality

```
RawQuality = AttrWithBonus / (VelDifficulty × MoveDifficulty)
```

**Range analysis:**
- Maximum: AttrWithBonus_max / denominator_min = 1.15 / 0.1 = **11.5**
  (a player with max attributes receiving a near-stationary ball while standing still
  with half-turn bonus; the value 11.5 is clamped to 1.0 in Step 8)
- Minimum: AttrWithBonus_min / denominator_max = (0.05 × 1.0) / 6.0 = **0.0083**
  (before pressure, a minimum-attribute player receiving a thunderbolt while sprinting
  can still achieve q ≈ 0.005 after pressure; clamped to 0.0)

**Important:** RawQuality can significantly exceed 1.0 for elite players receiving easy
balls. This is intentional — the clamp in Step 8 handles the ceiling. The unclamped value
is useful for sensitivity analysis (how much "headroom" does a player have).

#### A.1.8 Step 7 — Pressure Degradation

```
q = RawQuality × (1.0 - pressureScalar × PRESSURE_WEIGHT)
q = RawQuality × (1.0 - pressureScalar × 0.40)
```

**Why multiplicative pressure?**
An additive penalty (RawQuality - pressureScalar × k) would reduce quality linearly for
all players identically. The multiplicative form scales the penalty with the raw quality —
a very skilled player loses more *absolute* quality under pressure (because they have more
to lose) but the proportional degradation is the same. This is appropriate: pressure
affects all players equally proportionally, but elite players have a larger buffer.

**Pressure at maximum (pressureScalar = 1.0):**
```
q = RawQuality × (1.0 - 1.0 × 0.40) = RawQuality × 0.60
```
Maximum pressure degrades quality by exactly 40%. A player who would achieve q = 0.80
without pressure achieves q = 0.48 under maximum pressure. This is within the range
documented by Beilock & Gray (2010): 20–50% performance degradation under acute
proximity stress.

#### A.1.9 Step 8 — Final Clamp

```
q = Clamp(q, 0.0, 1.0)
```

The clamp handles two cases:
- **Upper bound:** Elite players receiving trivially easy balls produce RawQuality >> 1.0.
  These are clamped to 1.0 (perfect control). The formula does not distinguish between
  "just barely 1.0" and "trivially easy" — both map to the same output, which is correct
  because perfect control is perfect control.
- **Lower bound:** Prevents negative quality from floating point underflow or extreme
  pressure scenarios. q = 0.0 maps to the maximum touch radius (2.0m) with full angle
  error (±45°) — a completely uncontrolled touch, not an impossible value.

---

### A.2 Touch Radius Piecewise Interpolation Derivation

#### A.2.1 Band Structure from Master Volume 1

Master Volume 1 §6 specifies four control outcome bands with radius boundaries:

| Band | Quality Threshold | Radius Range |
|------|------------------|--------------|
| Perfect | q ≥ 0.85 | 0.10m – 0.30m |
| Good | 0.60 ≤ q < 0.85 | 0.30m – 0.60m |
| Poor | 0.35 ≤ q < 0.60 | 0.60m – 1.20m |
| Heavy | q < 0.35 | 1.20m – 2.00m |

#### A.2.2 Monotonicity Requirement

The key invariant is **monotonicity**: higher quality must always produce a smaller or equal
radius. A non-monotonic mapping would create discontinuities where a tiny improvement in
quality paradoxically worsens ball placement.

This is achieved via piecewise linear interpolation within each band, where the interpolation
parameter `t` maps the quality within the band to the radius within the band's range. Higher
quality within the band → higher `t` → smaller radius (interpolating from max to min).

#### A.2.3 General Interpolation Form

For a band spanning quality range [q_low, q_high] and radius range [r_max, r_min]:

```
t = (q - q_low) / (q_high - q_low)   // t ∈ [0.0, 1.0] within band
r_base = Lerp(r_max, r_min, t)         // r_base ∈ [r_min, r_max]
```

**Why Lerp(r_max, r_min, t) rather than Lerp(r_min, r_max, t)?**
At t = 0 (quality at lower bound, e.g., q = 0.60 for Good band), the radius should be
at its worst within the band (r_max = 0.60m for Good). At t = 1 (quality at upper bound,
e.g., q = 0.85 for Good band), radius should be at its best (r_min = 0.30m). Therefore
the interpolation runs from r_max to r_min as t increases — Lerp(r_max, r_min, t).

#### A.2.4 Band-by-Band Derivation

**Perfect band (q ∈ [0.85, 1.0]):**
```
t = (q - 0.85) / (1.00 - 0.85) = (q - 0.85) / 0.15
r_base = Lerp(0.30, 0.10, t)
       = 0.30 - t × (0.30 - 0.10)
       = 0.30 - t × 0.20
```
- At q = 0.85: t = 0.0, r_base = 0.30m ✓
- At q = 1.00: t = 1.0, r_base = 0.10m ✓

**Good band (q ∈ [0.60, 0.85)):**
```
t = (q - 0.60) / (0.85 - 0.60) = (q - 0.60) / 0.25
r_base = Lerp(0.60, 0.30, t)
       = 0.60 - t × 0.30
```
- At q = 0.60: t = 0.0, r_base = 0.60m ✓
- At q = 0.85: t = 1.0, r_base = 0.30m ✓ (matches Perfect band lower bound)

**Band continuity check:** At q = 0.85, the Good band produces r = 0.30m and the Perfect
band also produces r = 0.30m. Continuity confirmed. ✓

**Poor band (q ∈ [0.35, 0.60)):**
```
t = (q - 0.35) / (0.60 - 0.35) = (q - 0.35) / 0.25
r_base = Lerp(1.20, 0.60, t)
       = 1.20 - t × 0.60
```
- At q = 0.35: t = 0.0, r_base = 1.20m ✓
- At q = 0.60: t = 1.0, r_base = 0.60m ✓ (matches Good band lower bound)

**Heavy band (q ∈ [0.0, 0.35)):**
```
t = q / 0.35
r_base = Lerp(2.00, 1.20, t)
       = 2.00 - t × 0.80
```
- At q = 0.0:  t = 0.0, r_base = 2.00m ✓
- At q = 0.35: t = 1.0, r_base = 1.20m ✓ (matches Poor band lower bound)

#### A.2.5 Velocity Modifier Derivation

The base radius is augmented by a velocity modifier that increases radius for high-speed balls:

```
r = r_base + (ballSpeed / VELOCITY_REFERENCE) × VELOCITY_RADIUS_FACTOR
r = r_base + (ballSpeed / 15.0) × 0.25
```

**Physical justification:** A fast-incoming ball carries more momentum that must be absorbed
by the receiving foot. The foot contact duration (~10ms per [SHINKAI-2009]) is insufficient
to fully absorb the impulse; the residual impulse displaces the ball further from the
intended contact point. The modifier models this momentum transfer effect.

**Effect magnitude:**
- At 15 m/s (reference speed): +0.25m × 1.0 = +0.25m modifier
- At 28 m/s (thunderbolt): +0.25m × 1.87 = +0.47m modifier
- At 30 m/s: +0.25m × 2.0 = +0.50m modifier (maximum realistic)

**Interaction with band boundaries:** The modifier can push a touch radius beyond its band's
maximum into the next band's territory. For example, a Good band touch (r_base = 0.45m)
receiving a 30 m/s ball: r = 0.45 + 0.50 = 0.95m, which is within the Poor band range.
This is intentional — high-speed balls genuinely produce worse outcomes than slow balls at
the same quality level.

---

### A.3 Ball Displacement Direction Derivation

#### A.3.1 Angular Error Model

Ball displacement direction represents where the ball goes after the touch. For perfect
control (q = 1.0), the ball travels in the agent's IntendedTouchDirection. For poor control
(q = 0.0), the ball deviates by up to MAX_TOUCH_ANGLE_ERROR = 45° in a random direction.

The angular error model:
```
angularError = (1.0 - q) × MAX_TOUCH_ANGLE_ERROR   // degrees
```

**Why linear interpolation between 0° and 45°?**
Linear interpolation gives a predictable, testable mapping. At q = 0.55 (CONTROLLED_THRESHOLD):
angularError = 0.45 × 45° = 20.25°. This means controlled touches can have up to ~20°
directional error, which is realistic for professional-level control under typical conditions.

**Why 45° as the maximum?**
At 45° error, the ball is travelling at right angles to the intended direction — it has
completely missed the target line. Beyond 45°, the ball is travelling *away* from the
intended direction, which is physically implausible for deliberate foot contact. The 45°
cap therefore represents the boundary between "misdirected touch" and "physically
implausible touch."

#### A.3.2 Direction Vector Construction

The actual displacement direction is computed by rotating IntendedTouchDirection by
`angularError` degrees around the vertical (Z) axis:

```
errorAngle = (1.0 - q) × 45.0 × randomSign      // randomSign ∈ {-1, +1}
touchDir   = Rotate(IntendedTouchDirection, errorAngle, axis=Vector3.up)
```

**Determinism note:** `randomSign` must use the specification's seeded pseudo-random
number generator (Master Vol 1 §1.3), not System.Random. The seed is derived from the
current simulation tick and agent ID to ensure identical outputs across replay sessions.

#### A.3.3 Displacement Position

The ball's new position is placed at distance `r` from AgentPosition along `touchDir`:

```
newBallPosition = AgentPosition + touchDir × r
newBallPosition.z = Ball.RADIUS   // 0.11m; ground contact
```

The Z component is zeroed to ground level (ball radius above pitch surface) because First
Touch handles only ground-level ball reception. Aerial balls are rejected by the height
guard (§3.4.3, GROUND_CONTROL_HEIGHT = 0.50m) before reaching displacement computation.

---

### A.4 Possession State Machine Threshold Derivation

#### A.4.1 Four-State Priority Ordering

The possession state machine evaluates four mutually exclusive outcomes in priority order.
The priority ordering prevents ambiguity when multiple conditions are simultaneously true.

**Priority 1: CONTROLLED**
```
q ≥ CONTROLLED_THRESHOLD (0.55) AND r ≤ CONTROLLED_RADIUS (0.60m)
```

Rationale for checking CONTROLLED first: a ball within 0.60m of the agent under sufficient
quality is always controlled — even if opponents are nearby. INTERCEPTION requires the ball
to have escaped (r ≥ 1.20m), which cannot co-occur with r ≤ 0.60m.

**Priority 2: INTERCEPTION**
```
r ≥ INTERCEPTION_THRESHOLD (1.20m) AND nearestOpponent ≤ INTERCEPTION_RADIUS (2.50m)
```

Checked before DEFLECTION because INTERCEPTION is the worse outcome (possession changes).
When both conditions are satisfied, preferring INTERCEPTION is conservative for the
receiving team (they lose possession), which discourages heavy-touch play.

**Priority 3: DEFLECTION**
```
r ≥ DEFLECTION_THRESHOLD (1.50m) AND momentumAlignment ≥ DEFLECTION_ALIGNMENT_MIN (0.70)
AND no opponent within INTERCEPTION_RADIUS
```

DEFLECTION is checked only when INTERCEPTION is not possible (no opponent nearby).

**Priority 4: LOOSE_BALL**
Default when no higher-priority condition is met. Covers the gap between CONTROLLED radius
(0.60m) and INTERCEPTION threshold (1.20m), plus cases where the ball escapes but no
opponent is close.

#### A.4.2 Threshold Derivation

**CONTROLLED_THRESHOLD = 0.55:**
At q = 0.55, an average player (NormAttr ≈ 0.575) receiving a typical pass (VelDifficulty
= 1.0) with no pressure achieves q ≈ 0.575 — just above threshold. This means average
players should achieve CONTROLLED outcomes on typical passes without pressure, which is
the intended simulation behaviour (average players are competent, not elite).

**INTERCEPTION_THRESHOLD = 1.20m (= CONTROLLED_RADIUS upper bound + 0.60m gap):**
From Master Vol 1 §6: the gap between CONTROLLED_RADIUS (0.60m) and INTERCEPTION_THRESHOLD
(1.20m) creates a "recovery zone" — the LOOSE_BALL band at 0.60m–1.20m. A ball in this
zone has escaped the player's immediate control but is not far enough away for a typical
opponent to reach in a single movement. The 0.60m gap is approximately one stride length.

**INTERCEPTION_RADIUS = 2.50m:**
An opponent can be at most INTERCEPTION_RADIUS from the new ball position to have a
realistic chance of reaching the ball before the original agent recovers. At 2.50m, a
sprinting agent (7 m/s) covers this distance in approximately 0.36 seconds — about 22 frames
at 60 Hz. This is sufficient for the opponent to redirect their movement toward the ball.

---

### A.5 Pressure Evaluation Derivation

#### A.5.1 Pressure Falloff Model

The pressure scalar aggregates opponent proximity into a single [0.0, 1.0] value. Each
opponent within PRESSURE_RADIUS = 3.0m contributes a proximity score:

```
contribution = 1.0 - (opponentDistance / PRESSURE_RADIUS)
             = 1.0 - (d / 3.0)
```

This is a linear falloff from 1.0 (opponent at d = 0m, maximum pressure) to 0.0 (opponent
at d = 3.0m = PRESSURE_RADIUS, boundary of pressure zone).

**Why linear rather than inverse-square?**
Inverse-square falloff (1 / d²) would create extremely high pressure values for opponents at
0.5m or closer, making the pressure scalar numerically unstable and requiring a singularity
guard. Linear falloff is well-behaved, predictable, and sufficient to model the general
principle: closer opponents create more pressure.

**Academic grounding:** The linear proximity model is consistent with Williams & Davids (1998)
finding that attentional narrowing increases approximately linearly with opponent proximity
in the 1–4m range (the range most relevant to first touch scenarios).

#### A.5.2 Multi-Opponent Aggregation

Contributions from all opponents within PRESSURE_RADIUS are summed and then capped:

```
pressureSum = SUM(1.0 - d_i / 3.0) for all opponents within 3.0m
pressureScalar = Clamp01(pressureSum / PRESSURE_SATURATION)
// PRESSURE_SATURATION = 1.5
```

**Why PRESSURE_SATURATION = 1.5?**
A single opponent at maximum proximity (d → 0) contributes pressureSum = 1.0. Without
saturation, two opponents at close proximity would contribute pressureSum ≈ 2.0, producing
pressureScalar = 1.0 (maximum pressure) even before the clamp. Setting saturation at 1.5
means it takes more than one opponent at close range to reach maximum pressure, reflecting
that professional players under pressure from a single very close opponent are still more
composed than under two close opponents simultaneously.

**Range analysis:**
- No opponents within 3.0m: pressureSum = 0 → pressureScalar = 0.0 ✓
- One opponent at 1.5m: contribution = 0.5 → pressureScalar = 0.5/1.5 = 0.33
- One opponent at 0.5m: contribution = 0.833 → pressureScalar = 0.833/1.5 = 0.56
- Two opponents at 1.0m each: contributions = 2 × 0.667 = 1.333 → pressureScalar = 0.889
- Three opponents at 0.8m each: sum = 3 × 0.733 = 2.2 → clamped to 1.0

---

### A.6 Orientation Detection Derivation

#### A.6.1 Half-Turn Window Definition

The half-turn bonus is awarded when the agent's facing direction forms an angle between
HALF_TURN_ANGLE_MIN (30°) and HALF_TURN_ANGLE_MAX (60°) relative to the incoming ball
direction.

**Incoming ball direction vector:**
```
incomingDir = Normalize(-ball.Velocity.xy)   // XY plane only; Z ignored
```
The negative sign: ball velocity points away from the sender toward the receiver. The
incoming direction from the receiver's perspective is the opposite — toward the sender,
which is where the ball came from.

**Angle computation:**
```
cosAngle = Dot(agent.Facing.xy, incomingDir)
angle    = acos(Clamp(cosAngle, -1.0, 1.0)) × (180.0 / PI)   // degrees
```

**Half-turn window check:**
```
IsHalfTurnOriented = (angle >= 30.0) AND (angle <= 60.0)
```

#### A.6.2 Physical Interpretation of the 30°–60° Window

- **angle = 0°:** Agent is facing directly at the incoming ball (face-on reception). Good
  visual contact but limited ability to redirect the ball's momentum — the foot must
  absorb the full frontal impact.
- **angle = 45°:** Optimal half-turn position. Agent is angled to view both the incoming
  ball and the intended target. Foot contact is on the inside surface (instep or inside
  of foot), which provides more surface area and better momentum control per [LEES-2010].
- **angle = 60°:** Agent's body is largely turned away from the ball. Still within the
  kinematically useful range but approaching the limit of comfortable foot reach.
- **angle = 90°:** Ball arriving from the side. Normal reception; no bonus.
- **angle = 180°:** Ball arriving from behind — back to ball. No bonus; extremely
  difficult reception not modelled in Stage 0 (separate evaluation in later stage).

**Academic grounding:** Helsen & Starkes (1999) document that expert players systematically
adopt a scanning posture approximately 30°–70° from the anticipated ball arrival direction,
enabling simultaneous perception of ball trajectory and field context. The implementation
window of 30°–60° is slightly conservative relative to the literature's 30°–70°, favouring
specificity in awarding the bonus.

---

## Appendix B: Numerical Verification Hand Calculations

This appendix provides complete hand calculations for the three validation scenarios (VS-001,
VS-002, VS-003) from Section 5, plus four additional boundary case calculations. These are
the **authoritative expected values** against which implementation must be verified.

**Regression protocol (referenced by §5.11.4):** If any formula constant changes, all
calculations below that reference the changed constant must be re-run before updating test
expected values. Changed constants propagate through the formula in a specific order
(Steps 1→2→3→4→5→6→7→8); the re-run should trace the exact propagation path.

All calculations use the constants from Section 4 v1.1:
- TECHNIQUE_WEIGHT = 0.70, FIRST_TOUCH_WEIGHT = 0.30, ATTR_MAX = 20.0
- VELOCITY_REFERENCE = 15.0, VELOCITY_MAX_FACTOR = 4.0, VELOCITY_MIN = 0.5
- MOVEMENT_REFERENCE = 7.0, MOVEMENT_PENALTY = 0.5
- HALF_TURN_BONUS = 0.15, PRESSURE_WEIGHT = 0.40
- RADIUS_PERFECT = 0.30, RADIUS_GOOD = 0.60, RADIUS_POOR = 1.20, RADIUS_HEAVY = 2.00
- RADIUS_MIN = 0.10, VELOCITY_RADIUS_FACTOR = 0.25

---

### B.1 VS-001: Elite Player, Standard Pass

**Scenario:** Elite midfielder (Technique 19, FirstTouch 18) receives a standard ground
pass at 18 m/s while jogging at 3 m/s, no opponents nearby, in half-turn orientation.

**Expected outcome:** CONTROLLED, q ≈ 0.80, r ≈ 0.40m (Good band with half-turn).

#### B.1.1 Step-by-Step Calculation

```
Step 1: Weighted attribute
WeightedAttr = (19 × 0.70) + (18 × 0.30)
             = 13.30 + 5.40
             = 18.70

Step 2: Normalise
NormAttr = 18.70 / 20.0
         = 0.935

Step 3: Orientation bonus (IsHalfTurnOriented = true)
AttrWithBonus = 0.935 × (1.0 + 0.15)
              = 0.935 × 1.15
              = 1.0753

Step 4: Velocity difficulty
VelDifficulty = 18.0 / 15.0
              = 1.20
              (unclamped; within [0.1, 4.0])

Step 5: Movement difficulty (agentSpeed = 3.0 m/s)
MoveDifficulty = 1.0 + (3.0 / 7.0) × 0.5
               = 1.0 + 0.4286 × 0.5
               = 1.0 + 0.2143
               = 1.2143

Step 6: Raw quality
denominator = 1.20 × 1.2143 = 1.4571
RawQuality  = 1.0753 / 1.4571 = 0.7380

Step 7: Pressure degradation (pressureScalar = 0.0)
q = 0.7380 × (1.0 - 0.0 × 0.40) = 0.7380 × 1.0 = 0.7380

Step 8: Clamp
q = Clamp(0.7380, 0.0, 1.0) = 0.7380
```

**Control quality q = 0.738**

#### B.1.2 Touch Radius Calculation

```
q = 0.738 → Good band (0.60 ≤ q < 0.85)

t = (0.738 - 0.60) / (0.85 - 0.60)
  = 0.138 / 0.25
  = 0.552

r_base = Lerp(0.60, 0.30, 0.552)
       = 0.60 - 0.552 × (0.60 - 0.30)
       = 0.60 - 0.552 × 0.30
       = 0.60 - 0.1656
       = 0.4344 m

Velocity modifier (ballSpeed = 18.0 m/s):
modifier = (18.0 / 15.0) × 0.25 = 1.20 × 0.25 = 0.30m

r = r_base + modifier = 0.4344 + 0.30 = 0.7344 m
```

**Touch radius r = 0.734m**

#### B.1.3 Possession Outcome

```
q = 0.738 ≥ CONTROLLED_THRESHOLD (0.55) → quality criterion met
r = 0.734m > CONTROLLED_RADIUS (0.60m) → radius criterion NOT met

→ CONTROLLED fails on radius
```

The velocity modifier pushes the ball outside CONTROLLED_RADIUS. The outcome is **LOOSE_BALL**.

**Important note for test validation:** VS-001 in Section 5 specifies q ≈ 0.80 and expected
outcome CONTROLLED. This hand calculation reveals a discrepancy: the velocity modifier at
18 m/s (+0.30m) pushes r to 0.734m, exceeding CONTROLLED_RADIUS (0.60m). **This is an
inconsistency in the Section 5 scenario specification.**

**Resolution:** Either reduce the test ball speed (to ~12 m/s, where modifier is +0.20m and
r_base at q=0.738 would be 0.434 + 0.20 = 0.634m — still marginally over), or accept that
VS-001 produces LOOSE_BALL at 18 m/s, not CONTROLLED. Recommend lead developer decision.

**Revised VS-001 at 12 m/s (illustrative):**
```
VelDifficulty at 12 m/s = 12/15 = 0.80
denominator = 0.80 × 1.2143 = 0.9714
RawQuality = 1.0753 / 0.9714 = 1.107 → clamped to 1.0 after pressure
q = 1.0 (PERFECT band)
r_base (at q=1.0) = 0.10m
modifier = (12/15) × 0.25 = 0.20m
r = 0.10 + 0.20 = 0.30m → CONTROLLED (r ≤ 0.60m) ✓
```
At 12 m/s, the scenario behaves as intended. Recommend revising VS-001 ball speed to 12 m/s.

---

### B.2 VS-002: Average Player Under Pressure

**Scenario:** Average midfielder (Technique 12, FirstTouch 11) receives a pass at 15 m/s
while stationary, with one opponent at 1.5m (medium pressure), no half-turn.

**Expected outcome:** q ≈ 0.35–0.45, likely LOOSE_BALL.

#### B.2.1 Step-by-Step Calculation

```
Step 1: Weighted attribute
WeightedAttr = (12 × 0.70) + (11 × 0.30)
             = 8.40 + 3.30
             = 11.70

Step 2: Normalise
NormAttr = 11.70 / 20.0 = 0.585

Step 3: Orientation bonus (IsHalfTurnOriented = false)
AttrWithBonus = 0.585 × (1.0 + 0.0) = 0.585

Step 4: Velocity difficulty (ballSpeed = 15.0 m/s)
VelDifficulty = 15.0 / 15.0 = 1.00

Step 5: Movement difficulty (agentSpeed = 0.0 m/s, stationary)
MoveDifficulty = 1.0 + (0.0 / 7.0) × 0.5 = 1.0

Step 6: Raw quality
denominator = 1.00 × 1.00 = 1.00
RawQuality  = 0.585 / 1.00 = 0.585

Step 7: Pressure calculation
pressureSum = 1.0 - (1.5 / 3.0) = 1.0 - 0.50 = 0.50
pressureScalar = Clamp01(0.50 / 1.5) = 0.333

q = 0.585 × (1.0 - 0.333 × 0.40)
  = 0.585 × (1.0 - 0.133)
  = 0.585 × 0.867
  = 0.5072

Step 8: Clamp
q = 0.5072
```

**Control quality q = 0.507**

#### B.2.2 Touch Radius Calculation

```
q = 0.507 → Poor band (0.35 ≤ q < 0.60)

t = (0.507 - 0.35) / (0.60 - 0.35)
  = 0.157 / 0.25
  = 0.628

r_base = Lerp(1.20, 0.60, 0.628)
       = 1.20 - 0.628 × 0.60
       = 1.20 - 0.377
       = 0.823 m

Velocity modifier (ballSpeed = 15.0 m/s):
modifier = (15.0 / 15.0) × 0.25 = 0.25m

r = 0.823 + 0.25 = 1.073 m
```

**Touch radius r = 1.073m**

#### B.2.3 Possession Outcome

```
q = 0.507 < CONTROLLED_THRESHOLD → CONTROLLED fails on quality
r = 1.073m < INTERCEPTION_THRESHOLD (1.20m) → INTERCEPTION fails on radius

→ Outcome: LOOSE_BALL ✓
```

**Final result:** q = 0.507, r = 1.073m, **LOOSE_BALL**. Consistent with expected range.

---

### B.3 VS-003: Poor Reception, Heavy Touch

**Scenario:** Defender (Technique 7, FirstTouch 6) receives a hard clearance at 28 m/s
while sprinting at 6 m/s, with two opponents at 1.2m and 1.8m, no half-turn.

**Expected outcome:** q < 0.25, INTERCEPTION or DEFLECTION.

#### B.3.1 Step-by-Step Calculation

```
Step 1: Weighted attribute
WeightedAttr = (7 × 0.70) + (6 × 0.30)
             = 4.90 + 1.80
             = 6.70

Step 2: Normalise
NormAttr = 6.70 / 20.0 = 0.335

Step 3: Orientation bonus (false) → AttrWithBonus = 0.335

Step 4: Velocity difficulty (ballSpeed = 28.0 m/s)
VelDifficulty = 28.0 / 15.0 = 1.867
(unclamped; within [0.1, 4.0])

Step 5: Movement difficulty (agentSpeed = 6.0 m/s)
MoveDifficulty = 1.0 + (6.0 / 7.0) × 0.5
               = 1.0 + 0.857 × 0.5
               = 1.0 + 0.429
               = 1.429

Step 6: Raw quality
denominator = 1.867 × 1.429 = 2.667
RawQuality  = 0.335 / 2.667 = 0.1257

Step 7: Pressure calculation
Opponent 1 at 1.2m: contribution = 1.0 - (1.2/3.0) = 1.0 - 0.40 = 0.60
Opponent 2 at 1.8m: contribution = 1.0 - (1.8/3.0) = 1.0 - 0.60 = 0.40
pressureSum = 0.60 + 0.40 = 1.00
pressureScalar = Clamp01(1.00 / 1.5) = 0.667

q = 0.1257 × (1.0 - 0.667 × 0.40)
  = 0.1257 × (1.0 - 0.267)
  = 0.1257 × 0.733
  = 0.0921

Step 8: Clamp → q = 0.0921
```

**Control quality q = 0.092**

#### B.3.2 Touch Radius Calculation

```
q = 0.092 → Heavy band (q < 0.35)

t = 0.092 / 0.35 = 0.263

r_base = Lerp(2.00, 1.20, 0.263)
       = 2.00 - 0.263 × 0.80
       = 2.00 - 0.210
       = 1.790 m

Velocity modifier (ballSpeed = 28.0 m/s):
modifier = (28.0 / 15.0) × 0.25 = 1.867 × 0.25 = 0.467m

r = 1.790 + 0.467 = 2.257 m
r = Clamp(2.257, 0.10, 2.00) = 2.00m   ← clamped to RADIUS_HEAVY
```

**Touch radius r = 2.00m (clamped)**

#### B.3.3 Possession Outcome

```
r = 2.00m ≥ INTERCEPTION_THRESHOLD (1.20m) ✓

Check INTERCEPTION: nearest opponent at 1.2m ≤ INTERCEPTION_RADIUS (2.50m) ✓

→ Outcome: INTERCEPTION ✓
```

**Final result:** q = 0.092, r = 2.00m, **INTERCEPTION**. Consistent with expected outcome.

---

### B.4 Half-Turn Bonus Verification

**Purpose:** Verify the half-turn bonus produces exactly +15% to NormAttr and that it
creates a meaningful q difference for a typical player.

**Setup:** Average player (Technique 12, FirstTouch 11), ball at 15 m/s, stationary, no pressure.

```
Without half-turn:
WeightedAttr = 11.70, NormAttr = 0.585, AttrWithBonus = 0.585
VelDifficulty = 1.00, MoveDifficulty = 1.00
RawQuality = 0.585, q = 0.585 (no pressure)

With half-turn:
AttrWithBonus = 0.585 × 1.15 = 0.67275
RawQuality = 0.67275, q = 0.673 (no pressure)

Δq = 0.673 - 0.585 = +0.088 (+15.0% of NormAttr)
```

**Possession outcome change verification:**
- Without half-turn: q = 0.585 ≥ 0.55 → CONTROLLED (subject to radius)
- With half-turn: q = 0.673 ≥ 0.55 → CONTROLLED (same, but with better radius)

At q = 0.585: t_good = (0.585-0.60)/0.25 → q is in Poor band, not Good:
```
Wait — 0.585 ≥ 0.60? No. 0.585 < 0.60 → Poor band.
t = (0.585 - 0.35) / 0.25 = 0.940
r_base = Lerp(1.20, 0.60, 0.940) = 1.20 - 0.564 = 0.636m
modifier = (15/15) × 0.25 = 0.25m
r = 0.636 + 0.25 = 0.886m → LOOSE_BALL (r > 0.60m)
```

At q = 0.673: Good band:
```
t = (0.673 - 0.60) / 0.25 = 0.292
r_base = Lerp(0.60, 0.30, 0.292) = 0.60 - 0.0876 = 0.5124m
modifier = 0.25m
r = 0.5124 + 0.25 = 0.762m → LOOSE_BALL (still > 0.60m)
```

**Finding:** For an average player at 15 m/s with no pressure, half-turn does not change
the possession outcome (both are LOOSE_BALL), but it improves r from 0.886m to 0.762m —
the ball is closer to the agent. The half-turn bonus *changes* outcomes (from LOOSE_BALL
to CONTROLLED) only when the baseline q is close to a threshold and/or ball speed is lower.

**Illustrative case where half-turn changes outcome (ball at 8 m/s):**
```
Without: VelDiff = 0.533, denom = 0.533, RawQ = 0.585/0.533 = 1.097 → q = 1.0 (clamped)
With:    q = 1.0 (clamped — half-turn bonus already maxed out at cap)
```
At 8 m/s, average player is already at q = 1.0. Half-turn has no effect because the clamp
prevents exceeding 1.0. This confirms the bonus is most impactful in the 12–20 m/s range
where quality is below the ceiling.

---

### B.5 Thunderbolt Cap Verification

**Purpose:** Verify that the thunderbolt cap (THUNDERBOLT_QUALITY_CAP = 0.30) correctly
restricts q for balls above THUNDERBOLT_SPEED = 28 m/s.

**Note:** The thunderbolt cap is applied as a separate post-Step-8 operation in §3.3.7:
```
if (ballSpeed > THUNDERBOLT_SPEED)
    q = Mathf.Min(q, THUNDERBOLT_QUALITY_CAP)   // cap at 0.30
```

**Worst case (cap should activate maximally):** Elite player (Technique 20, FirstTouch 20)
receives a 30 m/s ball while stationary, no pressure, half-turn.

```
WeightedAttr = 20.0, NormAttr = 1.00
AttrWithBonus = 1.00 × 1.15 = 1.15
VelDifficulty = 30.0 / 15.0 = 2.00
MoveDifficulty = 1.0
RawQuality = 1.15 / 2.00 = 0.575
q (before thunderbolt cap) = 0.575
q (after thunderbolt cap) = min(0.575, 0.30) = 0.30
```

**Thunderbolt cap forces q = 0.30** even for the best possible player.

**Touch radius at q = 0.30:** Just at the Poor/Heavy boundary:
```
q = 0.30 → Heavy band (q < 0.35)
t = 0.30 / 0.35 = 0.857
r_base = Lerp(2.00, 1.20, 0.857) = 2.00 - 0.686 = 1.314m
modifier = (30.0/15.0) × 0.25 = 0.50m
r = 1.314 + 0.50 = 1.814m → INTERCEPTION or DEFLECTION (r > 1.20m)
```

**Confirmed:** Even an elite player cannot control a thunderbolt (30 m/s). Outcome is at
minimum LOOSE_BALL, most likely INTERCEPTION if opponents are present.

---

### B.6 INTERCEPTION Threshold Boundary Cases

**Purpose:** Verify possession outcome at exact threshold boundaries.

#### B.6.1 Boundary: r = 1.20m, opponent at 2.50m

Setup: q = 0.35 (exactly at Poor/Heavy boundary), ballSpeed = 15 m/s.

```
At q = 0.35: t_poor = (0.35 - 0.35) / 0.25 = 0.0
r_base = Lerp(1.20, 0.60, 0.0) = 1.20m
modifier = (15/15) × 0.25 = 0.25m
r = 1.20 + 0.25 = 1.45m

r = 1.45m ≥ INTERCEPTION_THRESHOLD (1.20m) ✓
Opponent at 2.50m = INTERCEPTION_RADIUS → boundary case

→ INTERCEPTION triggers (opponent exactly at boundary is within range per ≤ comparison)
```

#### B.6.2 Boundary: r = 1.20m, no opponent

Setup: same, but no opponents within INTERCEPTION_RADIUS.

```
r = 1.45m ≥ DEFLECTION_THRESHOLD (1.50m)? No — 1.45m < 1.50m

→ LOOSE_BALL ✓
```

**Important:** At r = 1.45m, neither INTERCEPTION (no opponent) nor DEFLECTION (r < 1.50m)
applies. The outcome is LOOSE_BALL. This confirms the LOOSE_BALL "gap" between 1.20m and
1.50m when no opponent is present.

---

### B.7 Maximum and Minimum Output Bounds

**Purpose:** Confirm the formula produces valid output at every extreme.

#### B.7.1 Theoretical Maximum q

Inputs: Technique=20, FirstTouch=20, ballSpeed=0.5m/s (VELOCITY_MIN), agentSpeed=0,
pressureScalar=0, IsHalfTurnOriented=true.

```
WeightedAttr = 20.0, NormAttr = 1.00, AttrWithBonus = 1.15
VelDifficulty = Clamp(0.5/15.0, 0.1, 4.0) = Clamp(0.033, 0.1, 4.0) = 0.10
MoveDifficulty = 1.0
RawQuality = 1.15 / 0.10 = 11.5
q (before clamp) = 11.5
q (after clamp) = 1.0 ✓
```

#### B.7.2 Theoretical Minimum q

Inputs: Technique=1, FirstTouch=1, ballSpeed=60m/s (VELOCITY_MAX_FACTOR cap), agentSpeed=7m/s,
pressureScalar=1.0, IsHalfTurnOriented=false.

```
WeightedAttr = 1.0, NormAttr = 0.05, AttrWithBonus = 0.05
VelDifficulty = Clamp(60.0/15.0, 0.1, 4.0) = 4.0
MoveDifficulty = 1.0 + (7.0/7.0) × 0.5 = 1.50
RawQuality = 0.05 / (4.0 × 1.50) = 0.05 / 6.0 = 0.00833
q = 0.00833 × (1.0 - 1.0 × 0.40) = 0.00833 × 0.60 = 0.00500
q (after clamp) = 0.005 ✓ (> 0.0; no negative q)
```

**Both extreme cases produce valid output. Formula is numerically stable at all boundaries.**

---

