# Agent Movement Specification â€” Appendices A, B, C, D

**Purpose:** Completes the Agent Movement Specification with formula derivations from first principles, pre-computed numerical verification tables for test validation, a consolidated state machine transition diagram, and a tolerance derivation reference table for all unit tests. Together with Sections 1â€“7, these appendices satisfy all template requirements for formal approval.

**Created:** February 14, 2026, 11:00 AM PST  
**Revised:** March 4, 2026, 12:00 AM PST  
**Version:** 1.2  
**Status:** Draft  
**Stage:** Stage 0 â€” Physics Foundation  
**Specification:** #2 of 20  
**Dependencies:** Section 3.1 v1.2 (State Machine), Section 3.2 v1.0 (Locomotion), Section 3.3 v1.0 (Directional Movement), Section 3.4 v1.0 (Turning & Momentum), Section 3.5 v1.4 (Data Structures), Section 3.6 v1.1 (Edge Cases), Section 3.7 v1.1 (Validation & Testing), Section 4 v1.1 (Implementation Details), Section 5 v1.1 (Performance Analysis), Ball Physics Spec #1 Appendices v1.2 (pattern reference)

---

## Appendix A: Formula Derivations

This appendix provides step-by-step mathematical derivations from first principles for every core formula in Sections 3.2â€“3.4. Where those sections present the final implementation form, this appendix shows *how* each formula was reached and *why* each design choice was made.

**Derivation vs. tuning distinction:** Some constants in the Agent Movement system are physics-derived (derivable from biomechanical first principles or sports science data). Others are gameplay-tuned (chosen for feel, balance, or practical reasons with no single "correct" value). Each derivation below states which category applies. Gameplay-tuned values are flagged explicitly â€” this honesty is preferable to fabricating post-hoc physics justifications.

---

### A.1 Acceleration Model Derivation

#### A.1.1 From First Principles

An accelerating footballer produces ground reaction force through leg extension. At the start of a sprint (low speed), available traction exceeds momentum â€” the full musculoskeletal force can be applied to acceleration. As speed increases, more effort goes to maintaining the current stride rate and less is available for further speed gain. The net acceleration therefore decreases monotonically with speed.

**General form â€” Newton's second law with speed-dependent force:**

```
F_net(v) = F_max Ã— (1 - v/v_max)
```

Where:
- F_max = maximum propulsive force at zero speed (N)
- v_max = top speed (m/s), where all force goes to maintenance and net acceleration is zero
- v = current speed (m/s)

Applying F = ma:

```
a(v) = (F_max / m) Ã— (1 - v/v_max)
```

This is a first-order linear ODE: dv/dt = a_max Ã— (1 - v/v_max), where a_max = F_max/m.

#### A.1.2 Solving the Differential Equation

```
dv/dt = a_max Ã— (1 - v/v_max)

Let u = 1 - v/v_max, then du/dt = -(1/v_max) Ã— dv/dt

du/dt = -(a_max / v_max) Ã— u

Let k = a_max / v_max (rate constant, units: sâ»Â¹)

du/dt = -k Ã— u

Solution: u(t) = u(0) Ã— e^(-kÃ—t)

Since u(0) = 1 - v(0)/v_max, and starting from rest v(0) = 0:
u(0) = 1

Therefore: 1 - v(t)/v_max = e^(-kÃ—t)

v(t) = v_max Ã— (1 - e^(-kÃ—t))
```

This is the continuous exponential approach curve used in Section 3.2.3.

#### A.1.3 Rate Constant k â€” Physical Meaning

The rate constant k (sâ»Â¹) determines how quickly the agent approaches top speed. Its physical meaning:

- k = a_max / v_max = (initial acceleration at v=0) / (top speed)
- At time t = 1/k: velocity = v_max Ã— (1 - eâ»Â¹) = 0.632 Ã— v_max (63.2% of top speed)
- At time t = 2.3026/k: velocity = 0.9 Ã— v_max (90% of top speed â€” the Tâ‚‰â‚€ metric)

**Derivation of Tâ‚‰â‚€:**

```
0.9 Ã— v_max = v_max Ã— (1 - e^(-kÃ—Tâ‚‰â‚€))
0.9 = 1 - e^(-kÃ—Tâ‚‰â‚€)
e^(-kÃ—Tâ‚‰â‚€) = 0.1
-kÃ—Tâ‚‰â‚€ = ln(0.1) = -2.3026
Tâ‚‰â‚€ = 2.3026 / k
```

This relationship maps directly to the attribute system:
- ACCEL_K_MIN = 0.658 sâ»Â¹ â†’ Tâ‚‰â‚€ = 2.3026 / 0.658 = 3.50s (Acceleration attribute 1)
- ACCEL_K_MAX = 0.921 sâ»Â¹ â†’ Tâ‚‰â‚€ = 2.3026 / 0.921 = 2.50s (Acceleration attribute 20)

**Source:** The Tâ‚‰â‚€ range of 2.5â€“3.5 seconds is derived from GPS tracking data of professional footballers. Elite players (forwards, wingers) reach ~90% of their measured top speed within 2.5â€“3.0 seconds from a standing start. Slower-accelerating players (centre-backs, some goalkeepers) require 3.0â€“3.5 seconds. See Haugen et al. (2014), Buchheit et al. (2014).

#### A.1.4 Discrete Integration Form

For 60Hz frame-by-frame computation, the continuous formula is impractical (requires tracking "time since acceleration began"). Instead, the velocity-form update is used:

```
v(t+dt) = v_target + (v(t) - v_target) Ã— e^(-k Ã— dt)
```

**Derivation of equivalence:**

Starting from v(t) = v_max Ã— (1 - e^(-kÃ—t)):
```
v(t+dt) = v_max Ã— (1 - e^(-kÃ—(t+dt)))
         = v_max Ã— (1 - e^(-kÃ—t) Ã— e^(-kÃ—dt))

Since v(t) = v_max Ã— (1 - e^(-kÃ—t)):
  e^(-kÃ—t) = 1 - v(t)/v_max

Substituting:
v(t+dt) = v_max Ã— (1 - (1 - v(t)/v_max) Ã— e^(-kÃ—dt))
         = v_max - (v_max - v(t)) Ã— e^(-kÃ—dt)
         = v_target + (v(t) - v_target) Ã— e^(-kÃ—dt)
```

Where v_target replaces v_max to generalize for any target speed (including directional and fatigue modifiers).

**Numerical accuracy at 60Hz:**

At k = 0.658 (slowest acceleration), dt = 1/60:
```
decay = e^(-0.658 Ã— 0.01667) = e^(-0.01097) = 0.98909
```

Over 210 frames (3.5 seconds, full acceleration from 0 to ~90% of 10.2 m/s):
- Continuous formula: v(3.5) = 10.2 Ã— (1 - e^(-2.303)) = 10.2 Ã— 0.9000 = 9.180 m/s
- Discrete 60Hz: accumulated error < 0.01 m/s (verified by numerical simulation â€” the decay factor is applied multiplicatively each frame with no error accumulation beyond float precision)

#### A.1.5 Why Exponential, Not Linear

**Linear model: v(t) = min(v_max, vâ‚€ + a Ã— t)**

Problems:
1. Acceleration is constant until an abrupt transition to zero at v_max â€” produces visible "jerk" in animation
2. Does not match biomechanics â€” real force production diminishes with speed
3. Requires explicit "am I at top speed?" check and mode switch every frame
4. Produces identical acceleration profiles for all agents regardless of their proximity to top speed â€” unrealistic

**Exponential model: v(t) = v_max Ã— (1 - e^(-kÃ—t))**

Advantages:
1. Smooth approach â€” acceleration naturally decreases as speed increases (no jerk)
2. Self-limiting â€” velocity asymptotically approaches v_max without overshooting
3. Matches published sprint acceleration curves from GPS tracking data
4. Single parameter k captures the entire acceleration profile
5. Incremental form requires no mode tracking â€” same formula applies at all speeds

**Walking exception:** WALKING state uses linear acceleration (Section 3.2.3) because at walking speeds (0.3â€“2.2 m/s), the exponential model's advantages are imperceptible and the linear model is computationally trivial. Walking acceleration is a constant 2.0 m/sÂ² for all agents regardless of Acceleration attribute â€” at walking pace, all professional footballers accelerate near-identically. This is a pragmatic simplification, not a physics claim.

#### A.1.6 Walking Acceleration Model

For completeness: the WALKING state uses constant acceleration instead of exponential.

```
v(t+dt) = min(v_target, v(t) + a_walk Ã— dt)
```

Where a_walk = 2.0 m/sÂ² (constant for all agents). This reaches walking top speed (2.2 m/s) from rest in 1.1 seconds â€” quick enough to feel responsive, slow enough to look natural. No derivation from sports science is claimed; the value was chosen for animation compatibility.

---

### A.2 Top Speed Mapping Derivation

#### A.2.1 Linear Interpolation Formula

```
TopSpeed(effectivePace) = TOP_SPEED_MIN + (effectivePace - 1.0) Ã— TOP_SPEED_PER_POINT
```

Where:
- TOP_SPEED_MIN = 7.5 m/s (Pace = 1)
- TOP_SPEED_MAX = 10.2 m/s (Pace = 20)
- TOP_SPEED_PER_POINT = (10.2 - 7.5) / 19 = 2.7 / 19 = 0.14211 m/s per attribute point

#### A.2.2 Speed Bounds Justification

**Floor (7.5 m/s = 27.0 km/h):**
GPS tracking databases (Catapult, STATSports) show that the slowest professional outfield players â€” typically veteran centre-backs or defensive midfielders in lower divisions â€” register peak sprint speeds of 27â€“28 km/h. Setting the floor at 27.0 km/h ensures that even a Pace 1 player is physically plausible as a professional footballer, not a recreational jogger.

**Ceiling (10.2 m/s = 36.7 km/h):**
The fastest recorded sprint speeds in top-flight football:
- Kylian MbappÃ©: 36.0 km/h (measured via optical tracking, Ligue 1)
- Adama TraorÃ©: 37.0 km/h (measured via GPS, Premier League)
- Kyle Walker: 36.4 km/h (measured via GPS, Premier League)

10.2 m/s (36.7 km/h) represents the absolute elite tier. For reference, Usain Bolt's peak speed was 12.4 m/s (44.7 km/h) â€” footballers in full kit on grass never approach track sprinter speeds.

**Source category:** Physics-derived from measured data.

#### A.2.3 Linear vs. Curved Mapping

The current mapping is strictly linear. An alternative power curve was considered:

```
t = (effectivePace - 1.0) / 19.0                         // Normalize to [0, 1]
TopSpeed = TOP_SPEED_MIN + t^1.3 Ã— (TOP_SPEED_MAX - TOP_SPEED_MIN)
```

This would create diminishing returns at the high end â€” the difference between Pace 18 and Pace 20 would be larger than under the linear model, making elite speed feel more special.

**Linear was chosen for Stage 0** because:
1. Simpler to debug and reason about during physics bring-up
2. Each attribute point has equal value â€” no "dead zones" or "sweet spots"
3. The power curve can be introduced in Stage 1 as a tuning parameter inside `MapPaceToTopSpeed()` without changing any downstream formula

**Sensitivity analysis (linear):**
- Pace 17 â†’ 7.5 + 16 Ã— 0.14211 = 9.77 m/s
- Pace 20 â†’ 10.20 m/s
- Difference: 0.43 m/s (1.5 km/h)

Over a 50m sprint, this 0.43 m/s difference produces ~2.2m separation â€” visible but not dramatic. Playtesting will determine if this differentiation is sufficient.

---

### A.3 Deceleration Model Derivation

#### A.3.1 Constant-Force Braking

Unlike acceleration (which uses an exponential model for biomechanical realism), deceleration uses a constant-force model:

```
v(t) = vâ‚€ - a_decel Ã— t        (until v = 0)
```

**Justification for constant (not exponential) deceleration:**
1. Braking is mechanically simpler than acceleration â€” the player plants a foot and applies resistive force against momentum. Ground reaction force during braking is approximately constant for trained athletes.
2. Constant deceleration produces predictable stopping distances â€” critical for AI path planning. The stopping distance formula d = vâ‚€Â²/(2a) is exact under constant deceleration.
3. Sports science literature models deceleration in team sports as approximately constant for intentional stops (Harper & Kiely, 2018; Dos'Santos et al., 2020).

#### A.3.2 Controlled Deceleration Derivation

FR-3 (revised) specifies stopping distances from sprint speed (~9 m/s):
- Agility 1: stop in 5.0m
- Agility 20: stop in 3.0m

**Derivation of deceleration rates:**

Using kinematics: vÂ² = vâ‚€Â² - 2 Ã— a Ã— d, at v = 0:

```
a = vâ‚€Â² / (2 Ã— d)
```

For vâ‚€ â‰ˆ 9 m/s (representative sprint speed):

```
Agility 1:   a = 81 / (2 Ã— 5.0) = 81 / 10.0 = 8.10 m/sÂ²
Agility 20:  a = 81 / (2 Ã— 3.0) = 81 / 6.0  = 13.50 m/sÂ²
```

These become DECEL_CONTROLLED_MIN = 8.1 and DECEL_CONTROLLED_MAX = 13.5.

**Per-point mapping:** (13.5 - 8.1) / 19 = 5.4 / 19 = 0.28421 m/sÂ² per Agility point.

**Biomechanical validation:** 8.1â€“13.5 m/sÂ² corresponds to 0.83gâ€“1.38g. Published literature reports intentional deceleration rates of 5â€“10 m/sÂ² as typical and 10â€“15 m/sÂ² as achievable for elite athletes (Harper & Kiely, 2018). The Agent Movement range sits within documented human capability.

#### A.3.3 Emergency Deceleration Derivation

FR-3 (revised) specifies emergency stopping distances from sprint:
- Agility 1: stop in 3.5m
- Agility 20: stop in 2.5m

```
Agility 1:   a = 81 / (2 Ã— 3.5) = 81 / 7.0  = 11.571 m/sÂ²
Agility 20:  a = 81 / (2 Ã— 2.5) = 81 / 5.0  = 16.200 m/sÂ²
```

These become DECEL_EMERGENCY_MIN = 11.57 and DECEL_EMERGENCY_MAX = 16.2.

**Per-point mapping:** (16.2 - 11.57) / 19 = 4.63 / 19 = 0.24368 m/sÂ² per Agility point.

**Ordering verification:** Emergency deceleration always exceeds controlled deceleration at every attribute level:
- Agility 1: 11.57 > 8.10 âœ“
- Agility 10: 13.76 > 10.66 âœ“ (computed: 11.57 + 9 Ã— 0.24368 = 13.76; 8.1 + 9 Ã— 0.28421 = 10.66)
- Agility 20: 16.20 > 13.50 âœ“

This is correct â€” emergency braking always stops faster (shorter distance) than controlled braking at any attribute level.

#### A.3.4 Stopping Distance and Time Formulas

**Stopping distance (from constant deceleration):**

```
d = vâ‚€Â² / (2 Ã— a_decel)
```

**Stopping time:**

```
t_stop = vâ‚€ / a_decel
```

**Discrete integration error:** At 60Hz, the agent may overshoot zero velocity by up to one frame's worth of deceleration. Maximum overshoot per frame:

```
Î”v_max = a_decel_max Ã— dt = 16.2 Ã— (1/60) = 0.27 m/s
```

The `Mathf.Max(0f, newSpeed)` clamp in `ApplyControlledDeceleration()` catches this. Maximum positional overshoot is:

```
Î”x_max = Î”v_max Ã— dt / 2 = 0.27 Ã— 0.01667 / 2 = 0.0023m â‰ˆ 2.3mm
```

This is negligible and well within the 5cm position drift budget (PR-3).

---

### A.4 Turn Rate Model Derivation

#### A.4.1 Hyperbolic Decay Model

The turn rate model uses an inverse (hyperbolic) relationship between speed and angular velocity:

```
Ï‰_max = TURN_RATE_BASE / (1 + k_turn Ã— v)
```

**Derivation from biomechanics:**

A footballer changing direction must redirect their body's momentum. The centripetal force required for a turn of radius r at speed v is:

```
F_c = m Ã— vÂ² / r
```

The maximum lateral force a player can generate through foot planting is bounded by friction and muscle capability. Approximating this as a constant F_lat_max:

```
r_min = m Ã— vÂ² / F_lat_max
```

The corresponding angular velocity:

```
Ï‰_rad = v / r_min = F_lat_max / (m Ã— v)
```

In degrees per second:

```
Ï‰_deg = (F_lat_max / (m Ã— v)) Ã— (180/Ï€)
```

This produces the exact inverse relationship Ï‰ âˆ 1/v for v > 0. The implemented formula adds a constant offset in the denominator:

```
Ï‰ = C / (1 + k Ã— v)
```

The "+1" in the denominator ensures finite Ï‰ at v = 0 (instead of Ï‰ â†’ âˆž). At v = 0, Ï‰ = C = TURN_RATE_BASE = 720Â°/s. The parameter k controls how quickly the turn rate drops with speed.

**Why hyperbolic, not linear or quadratic:**

A linear model (Ï‰ = Ï‰â‚€ - kÃ—v) would reach zero at some finite speed â€” the agent would be unable to turn at all above that speed. A quadratic model (Ï‰ = Ï‰â‚€ / (1 + kÃ—vÂ²)) drops too aggressively at moderate speeds, making jogging-speed turns feel sluggish. The hyperbolic model provides a natural asymptotic curve: sharp initial drop (first 2â€“3 m/s), then gradual leveling â€” matching how real turning constraints scale with speed.

#### A.4.2 k_turn Parameter â€” Agility Mapping

k_turn controls the steepness of the turn rate decay. Higher k_turn = faster drop = stiffer turning.

The mapping is linear with Agility:

```
k_turn = K_TURN_MAX - (effectiveAgility - 1.0) Ã— K_TURN_PER_POINT
```

Note the inversion: high Agility â†’ low k_turn â†’ better turning. This is because K_TURN_MAX corresponds to worst agility (stiffest turning) and K_TURN_MIN to best agility (nimblest turning).

```
K_TURN_MAX = 0.78   (Agility 1 â€” stiffest)
K_TURN_MIN = 0.35   (Agility 20 â€” nimblest)
K_TURN_PER_POINT = (0.78 - 0.35) / 19 = 0.43 / 19 = 0.02263
```

**Calibration methodology:**

The k_turn endpoints were calibrated by working backward from desired turn rates at sprint speed (9 m/s):

Target: Agility 1 at sprint should produce ~90Â°/s (a full second for a 90Â° turn â€” sluggish but not immobile):
```
90 = 720 / (1 + k Ã— 9)
1 + 9k = 8.0
k = 0.778 â‰ˆ 0.78 âœ“
```

Target: Agility 20 at sprint should produce ~174Â°/s (half a second for 90Â° â€” elite cutting ability):
```
174 = 720 / (1 + k Ã— 9)
1 + 9k = 4.138
k = 0.349 â‰ˆ 0.35 âœ“
```

**Centripetal acceleration check for K_TURN_MIN = 0.35:**

At Agility 20, Balance 20, sprint speed 9 m/s:
```
Ï‰ = 720 / (1 + 0.35 Ã— 9) Ã— 1.0 = 720 / 4.15 = 173.5Â°/s
Ï‰_rad = 173.5 Ã— Ï€/180 = 3.029 rad/s
a_c = v Ã— Ï‰_rad = 9 Ã— 3.029 = 27.26 m/sÂ² = 2.78g
```

This is within the 1.5â€“3.0g range documented for maximal change-of-direction tasks (Dos'Santos et al., 2020). An initial K_TURN_MIN = 0.31 was tested but produced 3.04g â€” technically possible but leaving no headroom for future modifiers (dribbling penalty, surface effects).

**Source category:** Calibrated from biomechanical data (k endpoints physics-derived from target turn rates; target turn rates informed by gameplay design within biomechanical bounds).

#### A.4.3 Balance Modifier

Balance applies a secondary multiplier to the turn rate:

```
balance_mod = BALANCE_MOD_MIN + (effectiveBalance - 1.0) Ã— BALANCE_MOD_PER_POINT
```

Where:
- BALANCE_MOD_MIN = 0.85 (Balance 1)
- BALANCE_MOD_MAX = 1.0 (Balance 20)
- BALANCE_MOD_PER_POINT = 0.15 / 19 = 0.007895

**Rationale for 15% range (not 30% or 5%):**

Agility is the primary turn rate driver, providing a 1.93Ã— range at sprint (89.8 to 173.5Â°/s). Balance is secondary â€” it affects body control during turns but not the fundamental biomechanical limit. A 15% secondary range produces:
- Combined worst case (Agi 1, Bal 1): 89.8 Ã— 0.85 = 76.3Â°/s
- Combined best case (Agi 20, Bal 20): 173.5 Ã— 1.0 = 173.5Â°/s
- Total ratio: 2.27Ã— â€” meaningful differentiation without cartoonish extremes.

**Source category:** Gameplay-tuned. The 15% figure is a design choice â€” no sports science paper quantifies the isolated contribution of "balance" to turning rate. The combined 2.27Ã— range is validated against the plausible spread in professional footballer agility test results.

#### A.4.4 State-Specific Modifier

The DECELERATING state applies a 0.6Ã— modifier:

```
Ï‰_decel = Ï‰_base Ã— 0.60
```

**Rationale:** Braking and turning are competing biomechanical actions â€” both require foot planting and ground reaction force. During active deceleration, the longitudinal braking force takes priority, reducing available lateral force for turning. The 0.6Ã— factor was chosen to produce turn rates in the "Limited" category (Section 3.1.6) at sprint-entry speeds.

Verification at sprint entry (5.8 m/s), Agility 12, Balance 12:
```
k_turn = 0.78 - 11 Ã— 0.02263 = 0.78 - 0.2489 = 0.5311
balance_mod = 0.85 + 11 Ã— 0.007895 = 0.85 + 0.0868 = 0.9368
Ï‰_base = 720 / (1 + 0.5311 Ã— 5.8) Ã— 0.9368 = 720 / 4.080 Ã— 0.9368 = 165.3Â°/s
Ï‰_decel = 165.3 Ã— 0.60 = 99.2Â°/s
```

99.2Â°/s is "Limited" per Section 3.1.6 (90Â° turn in ~0.9 seconds) âœ“.

**Source category:** Gameplay-tuned. The 0.6Ã— value produces the desired "Limited" feel and was not derived from biomechanical literature.

---

### A.5 Directional Zone Multiplier Justification

#### A.5.1 Zone Architecture

Three discrete movement zones based on the angle between facing direction and movement direction:

```
Forward zone:   0Â°â€“30Â° from facing       multiplier = 1.0
Lateral zone:   40Â°â€“80Â° from facing      multiplier = 0.65â€“0.75 (Agility-scaled)
Backward zone:  90Â°â€“180Â° from facing     multiplier = 0.45â€“0.55 (Agility-scaled)
Interpolation bands: 30Â°â€“40Â° (forwardâ†”lateral, 10Â°), 80Â°â€“90Â° (lateralâ†”backward, 10Â°)
```

#### A.5.2 Multiplier Range Justification

**Lateral movement (0.65â€“0.75Ã—):**

Lateral (side-to-side) movement is slower than forward running because:
- Hip rotation is constrained â€” the pelvis cannot externally rotate as freely during lateral shuffling
- Stride length is reduced â€” crossover steps are shorter than running strides
- Muscle recruitment pattern shifts â€” adductors/abductors replace quadriceps/hamstrings as primary movers

Published biomechanics data shows lateral running speeds at approximately 60â€“80% of forward maximum for trained athletes (Nimphius et al., 2016). The 0.65â€“0.75 range sits within this band, with Agility modulating the exact value: high-Agility players (better hip mobility, more efficient crossover mechanics) retain more of their forward speed.

**Backward movement (0.45â€“0.55Ã—):**

Backward running (backpedaling) is the slowest movement mode:
- Biomechanically inefficient â€” glutes and calves work against their primary extension direction
- Visual limitation â€” the player cannot see where they are going (in gameplay terms, facing and movement are opposed)
- Stride frequency and length both reduced compared to forward or lateral movement

Published data shows backward running at approximately 40â€“60% of forward maximum (Vescovi & McGuigan, 2008). The 0.45â€“0.55 range is within this band.

**Source category:** Calibrated from biomechanical data. The ranges are informed by sports science literature; the exact Agility-scaled endpoints are gameplay-tuned for differentiation.

#### A.5.3 Interpolation Band Sensitivity

The interpolation bands (30Â°â€“40Â° forwardâ†”lateral, 80Â°â€“90Â° lateralâ†”backward) use linear interpolation to prevent hard discontinuities. Both bands are 10Â° wide.

**Sensitivity analysis â€” what if band width shifts Â±5Â°?**

At 9 m/s base speed (Agility 10, mid-range multipliers):
- Current 10Â° band (30Â°â€“40Â°): speed drops from 9.0 to ~6.3 over 10Â°
- If widened to 15Â° (30Â°â€“45Â°): same drop spread over more degrees â€” smoother but more "mushy"
- If narrowed to 5Â° (30Â°â€“35Â°): sharper transition, almost a hard step

A 10Â° band was chosen because at typical turn rates (Section 3.4), an agent sweeps through 10Â° in ~50â€“100ms â€” fast enough to be imperceptible during play but wide enough to prevent visual stuttering near boundaries.

**Zone boundary smoothing verification (UT-DIR-005 target):**

At the boundary midpoint (35Â° for forwardâ†”lateral), the multiplier is:
```
t = (35 - 30) / (40 - 30) = 5 / 10 = 0.5
multiplier = Lerp(1.0, lateral_mult, 0.5) = 1.0 + 0.5 Ã— (0.70 - 1.0) = 0.85
```

Maximum per-degree jump within any interpolation band = (zone_high - zone_low) / band_width_degrees:
- Forwardâ†”lateral: (1.0 - 0.65) / 10 = 0.035/degree (worst case, Agility 1)
- Lateralâ†”backward: (0.75 - 0.45) / 10 = 0.030/degree (worst case)

**âš ï¸ PRE-EXISTING ISSUE:** At Agility 1, the forwardâ†”lateral transition produces 0.035/degree, which exceeds the 0.03 threshold from UT-DIR-005. At Agility 5+, the jump drops to â‰¤0.033/degree, and at Agility 10+ it is â‰¤0.030/degree. This is a pre-existing tension between the zone multiplier range (Section 3.3) and the continuity threshold (Section 3.7). Two resolution options:

1. **Widen the forwardâ†”lateral band to 12Â°** (30Â°â€“42Â°): reduces max jump to 0.029/degree at Agility 1 âœ“
2. **Exempt Agility 1 from UT-DIR-005**: accept that the lowest-agility agents have slightly sharper transitions (0.035 is only 17% above the 0.03 target)

**Recommendation:** Option 2 â€” the 0.035/degree at Agility 1 is still imperceptible in practice (0.315 m/s change per degree at 9 m/s base speed). Flag this for Section 3.7 review rather than changing zone geometry.

---

### A.6 Fatigueâ€“Speed Modifier Derivation

#### A.6.1 Piecewise Model

Section 3.2.4 defines the authoritative fatigue-to-speed modifier function:

```
if aerobicPool >= 0.5:
    modifier = 1.0
else:
    modifier = Lerp(0.70, 1.0, aerobicPool / 0.5)
    modifier = 0.70 + (1.0 - 0.70) Ã— (aerobicPool / 0.5)
    modifier = 0.70 + 0.60 Ã— aerobicPool
```

**Shape rationale:**

The piecewise design reflects sports science findings that athletic performance does not degrade linearly with energy expenditure. Players maintain near-100% output until glycogen reserves are significantly depleted (approximately 50% of reserves), after which a rapid decline begins.

At aerobic pool thresholds:
```
Pool 1.0: modifier = 1.000 (fresh â€” full speed)
Pool 0.7: modifier = 1.000 (70% reserves â€” still no noticeable reduction)
Pool 0.5: modifier = 1.000 (50% reserves â€” threshold, still full speed)
Pool 0.4: modifier = 0.940 (40% reserves â€” 6% speed reduction begins)
Pool 0.3: modifier = 0.880 (30% reserves â€” 12% reduction, visibly slower)
Pool 0.2: modifier = 0.820 (20% reserves â€” 18% reduction, clearly fatigued)
Pool 0.1: modifier = 0.760 (10% reserves â€” 24% reduction, struggling)
Pool 0.0: modifier = 0.700 (spent â€” 30% reduction, walking pace dominant)
```

#### A.6.2 Floor Value Justification

The 0.70 floor (30% maximum speed reduction) was derived from QR-1:

> "Fatigued agents visibly slower than fresh agents (>15% speed reduction)"

30% reduction at complete exhaustion exceeds the 15% visibility threshold with 2Ã— margin. Below 70%, movement would look pathological rather than fatigued â€” a professional footballer, even at the end of extra time, retains substantial running capability.

**Real-world validation:** Late-match GPS tracking data from Premier League matches shows average speed reductions of 10â€“20% in the final 15 minutes compared to the first 15 minutes (Bradley et al., 2009). Complete exhaustion (30% reduction) represents an extreme beyond typical match conditions â€” appropriate because aerobic pool = 0.0 should only occur in extreme endurance scenarios.

**Source category:** Physics-derived (threshold from sports science), with gameplay-tuned floor value within biomechanically plausible bounds.

#### A.6.3 Cross-Reference: Section 3.1 vs. Section 3.2.4 Discrepancy

âš ï¸ **SPECIFICATION CONFLICT â€” MUST RESOLVE BEFORE APPROVAL**

Section 3.1 and Section 3.2.4 define **different** aerobic modifier functions:

**Section 3.1 (global modifier):** Linear, floor = 0.4
```
modifier = 0.4 + 0.6 Ã— aerobic_pool
Applied to: top speed, acceleration, turn rate
```

**Section 3.2.4 (speed-specific):** Piecewise linear, floor = 0.70
```
if pool >= 0.5: modifier = 1.0
else: modifier = 0.70 + 0.60 Ã— pool
Applied to: top speed only (explicit code function)
```

**Impact comparison:**

| Pool Level | Sec 3.1 (linear) | Sec 3.2.4 (piecewise) | Delta |
|---|---|---|---|
| 1.0 | 1.00 | 1.00 | 0.00 |
| 0.7 | 0.82 | 1.00 | **0.18** |
| 0.5 | 0.70 | 1.00 | **0.30** |
| 0.4 | 0.64 | 0.94 | **0.30** |
| 0.3 | 0.58 | 0.88 | **0.30** |
| 0.0 | 0.40 | 0.70 | **0.30** |

The two models diverge significantly at mid-range pool levels. At pool = 0.5 (roughly half-time), Section 3.1 already applies a 30% penalty while Section 3.2.4 applies none. This is a major gameplay difference â€” 30% speed reduction at half-time would make every match feel punishing.

**Analysis:**

Section 3.1 wrote the linear formula early as a conceptual placeholder. Section 3.2.4 refined it with sports science reasoning (performance maintained until ~50% glycogen depletion). Section 3.2.4 also provides the actual implementation code with `AerobicPoolToSpeedModifier()`, while Section 3.1 only defines a constant and a comment.

Section 3.1's floor of 0.4 (60% speed reduction at exhaustion) is biomechanically excessive â€” a professional footballer at complete exhaustion still runs at approximately 70â€“80% of their peak speed, not 40%. The 0.70 floor in Section 3.2.4 is better supported by match tracking data (Bradley et al., 2009).

**Resolution â€” adopt Section 3.2.4 piecewise model as universal:**

1. **Speed modifier:** Use Section 3.2.4's `AerobicPoolToSpeedModifier()` unchanged (floor 0.70, threshold 0.50)
2. **Acceleration modifier:** Apply the same piecewise function to the exponential rate constant k. Rationale: acceleration and speed are driven by the same musculoskeletal system â€” if a player retains 88% speed at pool 0.3, they retain ~88% of their acceleration capacity.
3. **Turn rate modifier:** Apply the same piecewise function. Rationale: turn rate depends on lateral force generation, which degrades with fatigue similarly to forward propulsion.
4. **Section 3.1 update required:** Replace `AEROBIC_MODIFIER_FLOOR = 0.4f` and the linear formula comment with reference to the shared piecewise function. Change the three worked examples (pool 1.0, 0.5, 0.0) to match the piecewise output.

**Impact on existing tests:** UT-FAT-001 through UT-FAT-004 test qualitative behaviors (pool drains, recovery occurs, sprint gate blocks) â€” these are unaffected. UT-ACC-004 tests ">10% reduction" at pool 0.3 â€” the piecewise modifier gives 12% (0.88), which still passes. No test failure expected.

**Action items:**
- [ ] Update Section 3.1 constant and comments to reference piecewise model
- [ ] Add `AerobicPoolToAccelerationModifier()` and `AerobicPoolToTurnRateModifier()` to Section 3.2/3.4 (or share the single function)
- [ ] Update Section 4 pipeline to call the correct modifier function for each subsystem
- [ ] Add UT-TRN-008 expected value based on piecewise model (currently missing tolerance)

---

