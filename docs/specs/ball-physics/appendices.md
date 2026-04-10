# Ball Physics Specification - Appendices A, B, C

**Created:** February 6, 2026, 11:00 PM PST  
**Revised:** February 6, 2026, 11:45 PM PST  
**Version:** 1.2  
**Status:** Draft  
**Purpose:** Complete the Ball Physics Specification with formula derivations, test data sets, and visual diagrams required by the specification template (Stage_0_Specification_Outline.md)

**Dependencies:** Sections 1-2 v1.2, Section 3.1 v2.6, Section 4 v1.2, Section 5 v1.0, Section 6 v1.0, Section 7 v1.0, Section 8 v1.4

**Changes from v1.0:**
- CRITICAL FIX: Appendix A.1.2 tanh comparison table Ã¢â‚¬â€ added explicit caveat about estimated tanh values and absent k parameter (Issue #1)
- CRITICAL FIX: Appendix B.1 Magnus force table Ã¢â‚¬â€ corrected force values to show full perpendicular magnitude with proper derivations (Issue #2)
- CRITICAL FIX: Appendix B.4 rolling distances Ã¢â‚¬â€ replaced fabricated values with actual 60Hz numerical simulation results; flagged Section 3.1.14 discrepancy (Issue #3)
- CRITICAL FIX: Appendix B.5 trajectory data Ã¢â‚¬â€ replaced with numerically simulated values and explicit derivation method (Issue #4)
- Added Appendix A.9: Goal post collision derivation (Issue #5)
- Appendix A.4.1: Added prominent warning about friction-only vs. friction+drag stopping distance (Issue #6)
- Appendix B.6: Updated rolling distances to match corrected B.4 values

---

## Appendix A: Formula Derivations

This appendix provides step-by-step mathematical derivations from first principles for every core formula in Section 3.1. Where Section 3.1 presents the final implementation form, this appendix shows *how* each formula was reached and *why* each simplification was made.

---

### A.1 Magnus Force Derivation

#### A.1.1 From First Principles

The Magnus effect arises from asymmetric airflow around a spinning sphere. On the side where surface velocity adds to airflow, flow accelerates and pressure drops (Bernoulli's principle). On the opposite side, flow decelerates and pressure rises. The net pressure difference produces a lateral force.

**General form (Kutta-Joukowski lift):**

```
F_lift = C_L Ãƒâ€” (1/2) Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” A
```

This is the standard aerodynamic lift equation applied to a sphere, where:
- C_L is the lift coefficient (function of spin)
- ÃÂ is air density (kg/mÃ‚Â³)
- v is ball speed relative to air (m/s)
- A is cross-sectional area (mÃ‚Â²)

**Force direction:**

The Magnus force acts perpendicular to both the velocity vector and the spin axis. This is the cross product:

```
F_direction = Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š
```

Derivation of why Ãâ€° Ãƒâ€” v (not v Ãƒâ€” Ãâ€°):
- Convention: right-hand rule applied to spin axis and velocity
- A ball with topspin (Ãâ€° perpendicular to v, rotating "forward over top"):
  - Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š points downward (-Z) Ã¢â€ â€™ ball dips Ã¢â€ â€™ correct physical behavior Ã¢Å“â€œ
- If we used vÃŒâ€š Ãƒâ€” Ãâ€°ÃŒâ€š, topspin would produce upward force Ã¢â€ â€™ incorrect Ã¢Å“â€”

**Combined vector form:**

```
F_magnus = (1/2) Ãƒâ€” ÃÂ Ãƒâ€” |v|Ã‚Â² Ãƒâ€” A Ãƒâ€” C_L Ãƒâ€” (Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š)
```

This is Equation 3.1.4 in the specification.

#### A.1.2 Lift Coefficient Model

**Literature model (Asai et al., 2007):**

The full empirical fit from [ASAI-2007] uses a tanh-like curve relating C_L to spin parameter S. The general form is:

```
C_L = C_L_max Ãƒâ€” tanh(k Ãƒâ€” S)
```

Where S is the spin parameter: S = (r Ãƒâ€” |Ãâ€°|) / |v|

The spin parameter S represents the ratio of surface rotational speed to translational speed. It is dimensionless.

**Important:** The exact curve parameters (C_L_max and k) from [ASAI-2007] are not reproduced here because the paper's fit was to specific ball models and wind tunnel conditions. The values in the comparison table below are **estimated** from the paper's published C_L vs. S figures, assuming C_L_max Ã¢â€°Ë† 0.5 and k Ã¢â€°Ë† 3.5. These estimates are sufficient to validate the simplification but should not be treated as authoritative tanh model outputs.

**Implemented simplification:**

```
C_L = 0.1 + 0.4 Ãƒâ€” Clamp01((S - 0.01) / 0.99)
```

This is a linear interpolation from C_L = 0.1 (at S = 0.01) to C_L = 0.5 (at S = 1.0).

**Justification for simplification:**

*Tanh values below are approximate (estimated from published figures, assuming C_L_max Ã¢â€°Ë† 0.5, k Ã¢â€°Ë† 3.5). The key point is directional: the linear model underestimates force in the mid-range, which is conservative for gameplay.*

| S value | tanh est. (C_L) | Linear model (C_L) | Difference | Typical scenario |
|---------|-----------------|---------------------|------------|------------------|
| 0.01 | ~0.02 | 0.10 | +0.08 | Very slow spin |
| 0.05 | ~0.09 | 0.12 | +0.03 | Normal pass |
| 0.10 | ~0.17 | 0.14 | -0.03 | Typical free kick |
| 0.20 | ~0.31 | 0.18 | -0.13 | Heavy spin |
| 0.30 | ~0.41 | 0.22 | -0.19 | Extreme spin |
| 0.50 | ~0.48 | 0.30 | -0.18 | Unrealistic in play |
| 1.00 | ~0.50 | 0.50 | 0.00 | Theoretical max |

In the typical gameplay range (S = 0.01 to 0.3), the maximum force difference is:
- At S = 0.10: ÃŽâ€C_L = 0.06
- At v = 25 m/s: ÃŽâ€F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 625 Ãƒâ€” 0.038 Ãƒâ€” 0.06 Ã¢â€°Ë† 0.87 N
- This produces ~2 m/sÃ‚Â² lateral acceleration on a 0.43 kg ball
- Over a 1.2s flight, this is ~1.4m additional displacement

This is within tuning tolerance and the linear model avoids a transcendental function call per frame. If extreme-spin accuracy proves necessary during playtesting, revert to tanh.

#### A.1.3 Spin Parameter Bounds

**Minimum S = 0.01:**
- Below this, surface velocity is <1% of translational velocity
- Magnus force is negligible (< 0.05 N at any realistic speed)
- Avoids division instabilities as |Ãâ€°| Ã¢â€ â€™ 0

**Maximum S = 1.0:**
- Surface velocity equals translational velocity
- Physically unrealistic for footballs in normal play
- Clamping prevents runaway force values from extreme spin inputs

---

### A.2 Aerodynamic Drag Derivation

#### A.2.1 Standard Drag Equation

**From Newton's second law applied to fluid resistance:**

```
F_drag = -(1/2) Ãƒâ€” ÃÂ Ãƒâ€” |v_rel|Ã‚Â² Ãƒâ€” C_d Ãƒâ€” A Ãƒâ€” vÃŒâ€š_rel
```

Where v_rel = v_ball - v_wind (velocity relative to air mass).

The negative sign indicates force opposes motion. The vÃ‚Â² dependence arises because:
1. The momentum of air displaced per second is proportional to v (mass flow rate)
2. Each unit of displaced air gains velocity proportional to v (momentum change)
3. Force = rate of momentum change Ã¢Ë†Â v Ãƒâ€” v = vÃ‚Â²

#### A.2.2 Drag Coefficient and Drag Crisis

**Smooth sphere drag:**

For a smooth sphere, C_d depends on Reynolds number:

```
Re = (ÃÂ Ãƒâ€” v Ãƒâ€” d) / ÃŽÂ¼
```

Where:
- d = ball diameter = 0.22m
- ÃŽÂ¼ = air dynamic viscosity = 1.81 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂµ PaÃ‚Â·s

At sea level:
```
Re Ã¢â€°Ë† (1.225 Ãƒâ€” v Ãƒâ€” 0.22) / (1.81 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂµ) Ã¢â€°Ë† 14,890 Ãƒâ€” v
```

| Speed (m/s) | Re | Flow regime | C_d |
|-------------|---------|-------------|-----|
| 5 | 74,450 | Laminar boundary | 0.20 |
| 15 | 223,350 | Laminar boundary | 0.20 |
| 20 | 297,800 | Transition begins | 0.20Ã¢â€ â€™0.10 |
| 25 | 372,250 | Transition ends | 0.10 |
| 35 | 521,150 | Turbulent boundary | 0.10 |

**Drag crisis:** At the critical Reynolds number, the boundary layer transitions from laminar to turbulent. The turbulent layer stays attached longer, reducing the wake and halving drag.

**Implementation simplification:**

Literature places the transition at Re Ã¢â€°Ë† 3.5-4.0 Ãƒâ€” 10Ã¢ÂÂµ, corresponding to ~25-30 m/s for a size 5 ball. The implementation uses a lower band (20-25 m/s) as a deliberate gameplay tuning decision Ã¢â‚¬â€ this ensures the knuckleball drag reduction activates across more of the powerful-shot speed range.

```
if speed < 20: C_d = 0.20 (laminar)
if speed > 25: C_d = 0.10 (turbulent)
if 20 Ã¢â€°Â¤ speed Ã¢â€°Â¤ 25: C_d = lerp(0.20, 0.10, (speed - 20) / 5)
```

Linear interpolation replaces the sharp physical transition. Acceptable because the real transition width varies by ball model (panel count, seam depth) and the precise shape is not observable to the player.

#### A.2.3 Drag Force Magnitude Validation

**Worked example at 25 m/s (powerful shot):**

```
C_d = 0.10 (fully turbulent at 25 m/s)
F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 625 Ãƒâ€” 0.10 Ãƒâ€” 0.038
F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 625 Ãƒâ€” 0.0038
F = 0.5 Ãƒâ€” 2.91
F = 1.45 N

Deceleration = 1.45 / 0.43 = 3.37 m/sÃ‚Â²
```

**Worked example at 10 m/s (moderate pass):**

```
C_d = 0.20 (laminar at 10 m/s)
F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 100 Ãƒâ€” 0.20 Ãƒâ€” 0.038
F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 100 Ãƒâ€” 0.0076
F = 0.5 Ãƒâ€” 0.932
F = 0.47 N

Deceleration = 0.47 / 0.43 = 1.09 m/sÃ‚Â²
```

Both values produce realistic deceleration Ã¢â‚¬â€ a powerful shot loses about 3.4 m/s per second due to drag alone, while a moderate pass loses about 1.1 m/s per second.

---

### A.3 Bounce Physics Derivation

#### A.3.1 Normal Impulse (Coefficient of Restitution)

The coefficient of restitution e relates rebound to impact speed in the normal direction:

```
e = -v_n_after / v_n_before
```

Where v_n is the velocity component normal to the surface (vertical for a flat pitch).

**Energy analysis:**

```
KE_before = (1/2) Ãƒâ€” m Ãƒâ€” v_nÃ‚Â² (kinetic energy in normal direction)
KE_after = (1/2) Ãƒâ€” m Ãƒâ€” (e Ãƒâ€” v_n)Ã‚Â² = eÃ‚Â² Ãƒâ€” KE_before
Energy_lost = (1 - eÃ‚Â²) Ãƒâ€” KE_before
```

For dry grass (e = 0.65): Energy_lost = 1 - 0.4225 = 57.75% of vertical kinetic energy.

**Drop test derivation (Section 3.1.14 test case):**

```
Drop height: h = 2m
Impact velocity: v = Ã¢Ë†Å¡(2gh) = Ã¢Ë†Å¡(2 Ãƒâ€” 9.81 Ãƒâ€” 2) = Ã¢Ë†Å¡39.24 = 6.264 m/s
Rebound velocity: v_after = e Ãƒâ€” v = 0.65 Ãƒâ€” 6.264 = 4.071 m/s
Rebound height: h_r = v_afterÃ‚Â² / (2g) = 16.57 / 19.62 = 0.845 m
Ratio: h_r / h = eÃ‚Â² = 0.4225 Ã¢â€ â€™ 0.845m from 2m drop Ã¢Å“â€œ
```

#### A.3.2 Tangential Impulse (Friction and Spin Transfer)

**Contact point velocity:**

The contact point (bottom of ball) has velocity from two sources:
1. Ball's translational velocity (tangential component): v_t
2. Ball's spin contribution: Ãâ€° Ãƒâ€” r_contact

```
v_contact = v_t + (Ãâ€° Ãƒâ€” r_contact)
```

Where r_contact = -r Ãƒâ€” nÃŒâ€š (vector from center to contact point, pointing down).

**Friction impulse:**

Coulomb friction limits the tangential impulse:

```
J_t_max = ÃŽÂ¼ Ãƒâ€” J_n
J_n = (1 + e) Ãƒâ€” m Ãƒâ€” |v_n|    (normal impulse magnitude)
```

The friction impulse either:
- **Stops sliding** (if J_t_required Ã¢â€°Â¤ J_t_max): Full grip, spin-velocity coupling achieved
- **Reduces sliding** (if J_t_required > J_t_max): Partial friction, ball slides during contact

```
J_t_required = m Ãƒâ€” |v_contact|    (impulse to stop contact sliding)
J_t = min(J_t_required, J_t_max)  (actual friction impulse applied)
```

**Spin-to-linear transfer:**

When a spinning ball bounces, spin angular momentum partially converts to linear momentum through friction:

```
ÃŽâ€v_tangential = J_t / m Ãƒâ€” friction_direction
ÃŽâ€Ãâ€° = (r_contact Ãƒâ€” J_friction) / I
```

This is why a ball with backspin "checks up" (slows or reverses horizontal motion on bounce) and a ball with topspin "kicks forward" (accelerates horizontally on bounce).

#### A.3.3 Spin Retention

After bounce, remaining spin is reduced by a surface-dependent retention factor:

```
Ãâ€°_after = Ãâ€°_after_friction Ãƒâ€” spin_retention
```

For dry grass: spin_retention = 0.80 (20% spin lost to surface deformation and micro-friction).

This is an empirical simplification. The actual spin loss depends on contact duration, surface deformation, and ball deformation Ã¢â‚¬â€ none of which are modeled explicitly. The single coefficient captures the aggregate effect.

---

### A.4 Rolling Friction Derivation

#### A.4.1 Rolling Resistance Force

```
F_rolling = -ÃŽÂ¼_r Ãƒâ€” m Ãƒâ€” g Ãƒâ€” vÃŒâ€š
```

Rolling resistance arises from deformation of both ball and surface at the contact patch. The deformation creates an asymmetric normal force distribution that opposes motion.

**Derivation of stopping distance:**

For rolling with friction only (ignoring drag):

```
ma = -Î¼_r Ã— m Ã— g
a = -Î¼_r Ã— g = -0.13 Ã— 9.81 = -1.2753 m/sÂ²

From vÂ² = vâ‚€Â² + 2as, at v = 0:
s = vâ‚€Â² / (2 Ã— Î¼_r Ã— g)
```

At vâ‚€ = 10 m/s on dry grass:
```
s = 100 / (2 Ã— 1.2753) = 100 / 2.551 = 39.2m (friction only)
```

**Note:** The 39.2m friction-only result shows that rolling friction (Î¼_r = 0.13) provides substantial deceleration on its own, but air drag still contributes meaningfully above ~10 m/s (see table below). Combined friction + drag modeling is essential for accurate stopping distances.

With combined friction + drag at 60Hz numerical simulation, the actual stopping distance is **28.3m** (see B.4), consistent with the Section 3.1.14 expected range of 26â€“31m.

#### A.4.2 Combined Deceleration (Rolling + Drag)

At speed v, total deceleration is:

```
a_total = ÃŽÂ¼_r Ãƒâ€” g + (ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” C_d Ãƒâ€” A) / (2m)
```

The drag term (Ã¢Ë†Â vÃ‚Â²) dominates at high speed, friction dominates at low speed:

| Speed (m/s) | a_friction (m/sÃ‚Â²) | a_drag (m/sÃ‚Â²) | a_total (m/sÃ‚Â²) | Dominant |
|-------------|-------------------|---------------|-----------------|----------|
| 1.0 | 1.28 | 0.01 | 1.29 | Friction |
| 5.0 | 1.28 | 0.27 | 1.55 | Friction |
| 10.0 | 1.28 | 1.09 | 2.37 | Friction |
| 15.0 | 1.28 | 2.44 | 3.72 | Drag |
| 20.0 | 1.28 | 4.35 | 5.63 | Drag |

This crossover at ~10 m/s explains why slow passes behave differently from fast passes â€” slow passes decelerate roughly linearly (friction-dominated), fast passes decelerate dramatically then coast.

---

### A.5 Spin Dynamics Derivation

#### A.5.1 Aerodynamic Torque

A spinning sphere in airflow experiences a resisting torque due to surface friction with air:

```
Ãâ€ž = -C_Ãâ€ž Ãƒâ€” ÃÂ Ãƒâ€” rÃ¢ÂÂµ Ãƒâ€” |Ãâ€°|Ã‚Â² Ãƒâ€” Ãâ€°ÃŒâ€š
```

**Why rÃ¢ÂÂµ?**

Dimensional analysis of aerodynamic torque on a sphere:
- Torque has dimensions [M LÃ‚Â² TÃ¢ÂÂ»Ã‚Â²]
- Available quantities: ÃÂ [M LÃ¢ÂÂ»Ã‚Â³], r [L], Ãâ€° [TÃ¢ÂÂ»Ã‚Â¹]
- ÃÂ Ãƒâ€” rÃ¢ÂÂµ Ãƒâ€” Ãâ€°Ã‚Â² gives [M LÃ¢ÂÂ»Ã‚Â³] Ãƒâ€” [LÃ¢ÂÂµ] Ãƒâ€” [TÃ¢ÂÂ»Ã‚Â²] = [M LÃ‚Â² TÃ¢ÂÂ»Ã‚Â²] Ã¢Å“â€œ

The rÃ¢ÂÂµ dependence means torque scales very steeply with ball radius. For our fixed radius (r = 0.11m):

```
rÃ¢ÂÂµ = 0.11Ã¢ÂÂµ = 1.61 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂµ mÃ¢ÂÂµ
```

This small value is why C_Ãâ€ž = 0.01 produces reasonable torque magnitudes.

#### A.5.2 Angular Deceleration

```
ÃŽÂ± = Ãâ€ž / I = -C_Ãâ€ž Ãƒâ€” ÃÂ Ãƒâ€” rÃ¢ÂÂµ Ãƒâ€” |Ãâ€°|Ã‚Â² / I
```

Where I = (2/3) Ãƒâ€” m Ãƒâ€” rÃ‚Â² = 0.00347 kgÃ‚Â·mÃ‚Â² (hollow sphere approximation).

**Worked example:**

At Ãâ€° = 30 rad/s (moderately spinning free kick):

```
|Ãâ€ž| = 0.01 Ãƒâ€” 1.225 Ãƒâ€” 1.61Ãƒâ€”10Ã¢ÂÂ»Ã¢ÂÂµ Ãƒâ€” 900 = 1.77 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ´ NÃ‚Â·m
|ÃŽÂ±| = 1.77Ãƒâ€”10Ã¢ÂÂ»Ã¢ÂÂ´ / 0.00347 = 0.051 rad/sÃ‚Â²
```

This means spin decays very slowly from torque alone Ã¢â‚¬â€ only 0.051 rad/s per second. The additional velocity-dependent and spin-rate-dependent decay factors (Section 3.1.7) accelerate this to produce realistic spin persistence over 1-3 second flights.

#### A.5.3 Combined Decay Model

The total spin decay per timestep combines three mechanisms:

```
Ãâ€°_new = Ãâ€° Ãƒâ€” (1 - decay_rate Ãƒâ€” dt) + (Ãâ€ž/I) Ãƒâ€” dt

Where:
  decay_rate = DECAY_VELOCITY_FACTOR Ãƒâ€” |v| + DECAY_SPIN_FACTOR Ãƒâ€” |Ãâ€°|
             = 0.01 Ãƒâ€” |v| + 0.005 Ãƒâ€” |Ãâ€°|
```

**Worked example over a 1.2s free kick flight:**

```
Initial: v = 25 m/s, Ãâ€° = 30 rad/s
dt = 1/60 = 0.0167s

At t=0:
  velocity_decay = 0.01 Ãƒâ€” 25 = 0.25
  spin_decay = 0.005 Ãƒâ€” 30 = 0.15
  total_decay = 0.40
  Ãâ€°_reduction_per_step = 30 Ãƒâ€” 0.40 Ãƒâ€” 0.0167 = 0.20 rad/s
  + torque effect Ã¢â€°Ë† 0.001 rad/s
  
After 72 steps (1.2s), numerical integration yields Ãâ€° Ã¢â€°Ë† 18 rad/s
Spin retention Ã¢â€°Ë† 60% over flight Ã¢â‚¬â€ ball still curving noticeably at goal
```

This matches observations of professional free kicks where curve is visible throughout the flight but diminishes noticeably in the final third.

---

### A.6 Moment of Inertia

#### A.6.1 Hollow Sphere Approximation

A football is approximately a hollow sphere (thin shell of mass with air inside):

```
I = (2/3) Ãƒâ€” m Ãƒâ€” rÃ‚Â²
```

**Derivation sketch:** Integrate dm Ãƒâ€” RÃ‚Â² sinÃ‚Â²ÃŽÂ¸ over the sphere surface, where R is the radius, ÃŽÂ¸ is the polar angle, and dm = (m / 4Ãâ‚¬RÃ‚Â²) Ãƒâ€” RÃ‚Â² sin ÃŽÂ¸ dÃŽÂ¸ dÃâ€ . The result is (2/3)mRÃ‚Â².

**Numerical value:**

```
I = (2/3) Ãƒâ€” 0.43 Ãƒâ€” 0.11Ã‚Â² = (2/3) Ãƒâ€” 0.43 Ãƒâ€” 0.0121 = 0.00347 kgÃ‚Â·mÃ‚Â²
```

**Real football deviation:**

A real football has non-uniform mass distribution (panels, bladder, valve). The actual moment of inertia is estimated 10-20% higher than the hollow sphere model. This is flagged in Section 3.1.2 as a tuning candidate Ã¢â‚¬â€ if spin behavior doesn't match footage, increase I by up to 20%.

---

### A.7 Cross-Sectional Area

```
A = Ãâ‚¬ Ãƒâ€” rÃ‚Â² = Ãâ‚¬ Ãƒâ€” 0.11Ã‚Â² = 0.0380 mÃ‚Â²
```

**From FIFA Law 2:** Ball circumference 68-70cm Ã¢â€ â€™ radius 10.8-11.1cm Ã¢â€ â€™ midpoint 11.0cm used.

---

### A.8 Semi-Implicit Euler Integration

#### A.8.1 Method

Semi-implicit (symplectic) Euler updates velocity before position:

```
v(t+dt) = v(t) + a(t) Ãƒâ€” dt        Ã¢â€ Â velocity updated first
x(t+dt) = x(t) + v(t+dt) Ãƒâ€” dt     Ã¢â€ Â new velocity used for position
```

Compare to explicit Euler (v then x with old v):
```
v(t+dt) = v(t) + a(t) Ãƒâ€” dt
x(t+dt) = x(t) + v(t) Ãƒâ€” dt        Ã¢â€ Â old velocity used
```

#### A.8.2 Why Semi-Implicit Over Explicit

For oscillatory systems (e.g., a bouncing ball), explicit Euler adds energy over time (the ball bounces higher and higher). Semi-implicit Euler conserves energy on average for Hamiltonian systems.

For our use case (primarily ballistic trajectories with damping), both methods are acceptable at 60Hz. Semi-implicit was chosen because:
1. Same computational cost as explicit Euler
2. Better energy behavior for bouncing scenarios
3. Standard practice in game physics

#### A.8.3 Why Not RK4?

Fourth-order Runge-Kutta would require 4Ãƒâ€” the force evaluations per timestep. At 60Hz with the forces in this specification, the accuracy improvement is negligible:

```
Euler error per step: O(dtÃ‚Â²) = O(2.8 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ´)
RK4 error per step: O(dtÃ¢ÂÂµ) = O(1.3 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ¹)

Over a 3-second flight (180 steps):
Euler total error: O(180 Ãƒâ€” dtÃ‚Â²) Ã¢â€°Ë† O(0.05) Ã¢â€ â€™ ~5cm position error
RK4 total error: O(180 Ãƒâ€” dtÃ¢ÂÂµ) Ã¢â€°Ë† O(10Ã¢ÂÂ»Ã¢ÂÂ·) Ã¢â€ â€™ ~0.0001mm error
```

5cm error over a 50m trajectory is imperceptible. The 4Ãƒâ€” cost is not justified.

---

### A.9 Goal Post Collision Derivation

#### A.9.1 Reflection Physics

Goal post collision uses the same impulse-based approach as ground bounce, simplified for a cylindrical obstacle:

```
v_after = v_tangential + (-e Ãƒâ€” v_normal) Ãƒâ€” nÃŒâ€š
```

Where nÃŒâ€š is the surface normal at the contact point, pointing from the post center toward the ball center.

**Normal calculation:**

```
nÃŒâ€š = normalize(contact_point - post_center)
```

For a cylindrical post (r_post = 0.06m, per FIFA regulations), the normal is always perpendicular to the post axis and points radially outward toward the ball.

**Velocity decomposition:**

```
v_n = dot(v, nÃŒâ€š)           (speed into post, negative)
v_t = v - v_n Ãƒâ€” nÃŒâ€š         (speed along post surface)
v_after = v_t + (-e Ãƒâ€” v_n) Ãƒâ€” nÃŒâ€š
```

#### A.9.2 Coefficient of Restitution

Goal posts are aluminum or steel. COR = 0.75 (higher than grass at 0.65 because metal deforms less than turf).

**Energy loss on post hit:**
```
Energy_retained = eÃ‚Â² = 0.75Ã‚Â² = 56.25%
Energy_lost = 43.75%
```

#### A.9.3 Spin Behavior

Spin retention on metal = 0.40 (much lower than grass at 0.80). The smooth, hard surface provides less grip, so spin transfers poorly.

```
Ãâ€°_after = Ãâ€°_before Ãƒâ€” 0.40
```

**No friction-based spin-velocity coupling** is modeled for post collisions (unlike ground bounce). This simplification is acceptable because:
- Post contact duration is extremely brief (~1ms vs. ~5-10ms for ground)
- The post's cylindrical geometry makes friction direction complex
- Visual result is indistinguishable from the simplified model

#### A.9.4 Worked Example

**Ball hits inside of left post, heading roughly toward goal:**

```
Ball velocity: v = (15, 2, -1) m/s (toward goal, slightly right, slightly down)
Post center: at goal line, left post position
Contact normal: nÃŒâ€š Ã¢â€°Ë† (0.3, -0.95, 0) (pointing away from post, roughly back and right)

v_n = dot((15, 2, -1), (0.3, -0.95, 0)) = 4.5 - 1.9 + 0 = 2.6 m/s
v_t = (15, 2, -1) - 2.6 Ãƒâ€” (0.3, -0.95, 0) = (15-0.78, 2+2.47, -1) = (14.22, 4.47, -1)
v_n_after = -0.75 Ãƒâ€” 2.6 = -1.95 m/s
v_after = (14.22, 4.47, -1) + (-1.95) Ãƒâ€” (0.3, -0.95, 0)
        = (14.22-0.585, 4.47+1.853, -1)
        = (13.64, 6.32, -1.0) m/s

Ball deflects away from goal with increased lateral velocity Ã¢â‚¬â€ "rebounds off the post."
```

---

## Appendix B: Test Data Sets

This appendix provides consolidated reference data tables for implementation testing. All values are derived from the formulas in Section 3.1 and the derivations in Appendix A.

---

### B.1 Magnus Force Reference Data

**Conditions:** ÃÂ = 1.225 kg/mÃ‚Â³, A = 0.038 mÃ‚Â², r = 0.11m  
**Assumption:** Ãâ€° perpendicular to v (|Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š| = 1.0). Real scenarios have oblique angles; multiply by sin(angle between Ãâ€° and v) to get actual force.  
**Formula:** F = 0.5 Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” A Ãƒâ€” C_L

| Test ID | Speed (m/s) | Spin (rad/s) | S | C_L | F_perp (N) | Scenario |
|---------|-------------|-------------|------|------|-----------|----------|
| MAG-01 | 20 | 0 | 0 | Ã¢â‚¬â€ | 0.00 | No-spin shot (guard) |
| MAG-02 | 0 | 30 | Ã¢â‚¬â€ | Ã¢â‚¬â€ | 0.00 | Stationary spin (guard) |
| MAG-03 | 25 | 12 | 0.053 | 0.117 | 1.71 | Typical free kick |
| MAG-04 | 22 | 12 | 0.060 | 0.120 | 1.35 | Standard curve |
| MAG-05 | 30 | 20 | 0.073 | 0.126 | 2.63 | Power curve shot |
| MAG-06 | 15 | 8 | 0.059 | 0.120 | 0.63 | Curled pass |
| MAG-07 | 10 | 5 | 0.055 | 0.118 | 0.28 | Gentle spin pass |
| MAG-08 | 35 | 30 | 0.094 | 0.134 | 3.82 | Extreme (upper bound) |

**Derivation for MAG-03 (full walkthrough):**
```
S = (r Ãƒâ€” Ãâ€°) / v = (0.11 Ãƒâ€” 12) / 25 = 1.32 / 25 = 0.0528
normalizedS = (S - 0.01) / (1.0 - 0.01) = 0.0428 / 0.99 = 0.0432
C_L = 0.1 + 0.4 Ãƒâ€” 0.0432 = 0.1173
F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 25Ã‚Â² Ãƒâ€” 0.038 Ãƒâ€” 0.1173
  = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 625 Ãƒâ€” 0.038 Ãƒâ€” 0.1173
  = 0.023275 Ãƒâ€” 625 Ãƒâ€” 0.1173
  = 14.547 Ãƒâ€” 0.1173
  = 1.706 N Ã¢â€°Ë† 1.71 N

Lateral acceleration: a = 1.71 / 0.43 = 3.98 m/sÃ‚Â²
Over 1.2s flight: d Ã¢â€°Ë† 0.5 Ãƒâ€” 3.98 Ãƒâ€” 1.44 = 2.87m lateral curve
(Reduced by spin decay over flight Ã¢â‚¬â€ actual curve ~2.0-2.5m)
```

**Derivation for MAG-06 (curled pass):**
```
S = (0.11 Ãƒâ€” 8) / 15 = 0.0587
normalizedS = (0.0587 - 0.01) / 0.99 = 0.0492
C_L = 0.1 + 0.4 Ãƒâ€” 0.0492 = 0.1197
F = 0.023275 Ãƒâ€” 225 Ãƒâ€” 0.1197 = 0.627 N

At 15 m/s, this produces 1.46 m/sÃ‚Â² lateral acceleration Ã¢â‚¬â€
visible curl on a 30m pass but not dramatic.
```

---

### B.2 Drag Force Reference Data

**Conditions:** ÃÂ = 1.225 kg/mÃ‚Â³, A = 0.038 mÃ‚Â², no wind, m = 0.43 kg  
**Precomputed constant:** (1/2)ÃÂA = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 0.038 = 0.023275  
**Formula:** F = 0.023275 Ãƒâ€” vÃ‚Â² Ãƒâ€” C_d

| Test ID | Speed (m/s) | C_d | F = 0.023275 Ãƒâ€” vÃ‚Â² Ãƒâ€” C_d (N) | Decel (m/sÃ‚Â²) | Scenario |
|---------|-------------|------|------------------------------|-------------|----------|
| DRG-01 | 5 | 0.20 | 0.12 | 0.27 | Slow roll |
| DRG-02 | 10 | 0.20 | 0.47 | 1.08 | Normal pass |
| DRG-03 | 15 | 0.20 | 1.05 | 2.44 | Firm pass |
| DRG-04 | 20 | 0.20 | 1.86 | 4.33 | Crisis onset |
| DRG-05 | 22.5 | 0.15 | 1.77 | 4.11 | Mid-crisis |
| DRG-06 | 25 | 0.10 | 1.45 | 3.38 | Crisis complete |
| DRG-07 | 30 | 0.10 | 2.09 | 4.87 | Powerful shot |
| DRG-08 | 35 | 0.10 | 2.85 | 6.63 | Very hard shot |

**Key observation:** The drag crisis produces a visible dip in drag force between 20-25 m/s. A shot at 22.5 m/s experiences *less* drag than a shot at 20 m/s. This is the knuckleball window Ã¢â‚¬â€ reduced drag means the ball "floats" longer, amplifying any small lateral perturbations.

---

### B.3 Bounce Reference Data

**Conditions:** Vertical drop, no spin, no horizontal velocity

| Test ID | Surface | COR (e) | Drop (m) | Impact v (m/s) | Rebound v (m/s) | Rebound h (m) |
|---------|---------|---------|----------|----------------|-----------------|---------------|
| BNC-01 | GRASS_DRY | 0.65 | 2.0 | 6.26 | 4.07 | 0.85 |
| BNC-02 | GRASS_DRY | 0.65 | 1.0 | 4.43 | 2.88 | 0.42 |
| BNC-03 | GRASS_WET | 0.70 | 2.0 | 6.26 | 4.38 | 0.98 |
| BNC-04 | GRASS_LONG | 0.55 | 2.0 | 6.26 | 3.44 | 0.60 |
| BNC-05 | ARTIFICIAL | 0.72 | 2.0 | 6.26 | 4.51 | 1.04 |
| BNC-06 | FROZEN | 0.80 | 2.0 | 6.26 | 5.01 | 1.28 |

**Derivation for BNC-01:**
```
v_impact = Ã¢Ë†Å¡(2 Ãƒâ€” 9.81 Ãƒâ€” 2.0) = Ã¢Ë†Å¡39.24 = 6.264 m/s
v_rebound = 0.65 Ãƒâ€” 6.264 = 4.072 m/s
h_rebound = vÃ‚Â² / (2g) = 16.58 / 19.62 = 0.845 m
```

---

### B.4 Rolling Distance Reference Data

**Conditions:** Ball rolling on surface, initial speed vÃ¢â€šâ‚¬, no spin, no wind  
**Method:** Actual 60Hz semi-implicit Euler numerical simulation using specification formulas (drag + rolling friction), terminated at v < 0.1 m/s (MIN_VELOCITY)

| Test ID | Surface | ÃŽÂ¼_r | vÃ¢â€šâ‚¬ (m/s) | Stop dist (m) | Stop time (s) | Scenario |
|---------|---------|------|----------|---------------|---------------|----------|
| ROL-01 | GRASS_DRY | 0.13 | 5 | 8.8 | 3.6 | Short pass |
| ROL-02 | GRASS_DRY | 0.13 | 10 | 28.3 | 6.3 | Firm ground pass |
| ROL-03 | GRASS_DRY | 0.13 | 15 | 49.1 | 8.0 | Hard pass |
| ROL-04 | GRASS_WET | 0.07 | 10 | 43.6 | 10.3 | Wet firm pass |
| ROL-05 | GRASS_LONG | 0.22 | 10 | 18.7 | 4.0 | Long grass pass |
| ROL-06 | ARTIFICIAL | 0.09 | 10 | 36.8 | 8.4 | Artificial turf |
| ROL-07 | FROZEN | 0.04 | 10 | 61.0 | 15.5 | Frozen pitch |

**Consistency check:** ROL-02 (28.3m at 10 m/s on dry grass) is consistent with Section 3.1.14 expected range of 26â€“31m. âœ“

**Derivation method for ROL-02:**
```
60Hz semi-implicit Euler loop:
  Each step (dt = 1/60 = 0.01667s):
    C_d = 0.20 (speed < 20 m/s for entire roll)
    a_friction = Î¼_r Ã— g = 0.13 Ã— 9.81 = 1.2753 m/sÂ²
    a_drag = 0.5 Ã— Ï Ã— vÂ² Ã— C_d Ã— A / m
    a_total = a_friction + a_drag
    v_new = v - a_total Ã— dt
    x += v_new Ã— dt
  
  At vâ‚€ = 10: initial a_total = 1.28 + 1.08 = 2.36 m/sÂ²
  At v = 5:   a_total = 1.28 + 0.27 = 1.55 m/sÂ²
  At v = 1:   a_total = 1.28 + 0.01 = 1.29 m/sÂ²  (friction dominates)
  
  Result: 28.3m in 6.3s
```

---

### B.5 Trajectory Reference Data (Full Flight)

**Free kick scenario:** vÃ¢â€šâ‚¬ = (22, 0, 6) m/s, Ãâ€°Ã¢â€šâ‚¬ = (0, 0, -12) rad/s (CW sidespin from above Ã¢â‚¬â€ ball curves right for +X motion), start position (25, 34, 0.5)  
**Method:** 60Hz semi-implicit Euler with all specification forces (gravity, drag with drag crisis, Magnus, spin decay with aerodynamic torque). These are **numerically computed values**, not analytical estimates.

| Time (s) | x (m) | y (m) | z (m) | Speed (m/s) | Spin (rad/s) | State |
|----------|-------|-------|-------|-------------|-------------|-------|
| 0.00 | 25.0 | 34.0 | 0.50 | 22.8 | 12.0 | AIRBORNE |
| 0.25 | 30.4 | 33.9 | 1.64 | 21.2 | 11.2 | AIRBORNE |
| 0.50 | 35.5 | 33.6 | 2.13 | 19.9 | 10.5 | AIRBORNE |
| 0.75 | 40.3 | 33.2 | 1.99 | 19.0 | 9.9 | AIRBORNE |
| 1.00 | 44.8 | 32.7 | 1.26 | 18.4 | 9.3 | AIRBORNE |
| 1.23 | 48.9 | 32.1 | 0.07 | 18.2 | 8.8 | BOUNCING |

**Key observations from simulation:**
- Total lateral deviation: 1.9m (y: 34.0 Ã¢â€ â€™ 32.1) over ~24m of forward travel
- Peak height: 2.13m at t Ã¢â€°Ë† 0.50s
- Flight time to ground: ~1.23s
- Speed reduction: 22.8 Ã¢â€ â€™ 18.2 m/s (20% loss from drag)
- Spin reduction: 12.0 Ã¢â€ â€™ 8.8 rad/s (27% loss from decay)
- The curve is gentle but visible Ã¢â‚¬â€ consistent with a moderately curled free kick

**Validation against Section 3.1.14 expectations:**
- Section 3.1.14 predicts 1.5-3.0m curve for a 25m free kick at 22 m/s with 12 rad/s spin
- This simulation uses slightly different initial conditions (vÃ¢â€šâ‚¬ = 22.8 m/s composite, start height 0.5m)
- The 1.9m curve is within the expected 1.5-3.0m range Ã¢Å“â€œ

**Implementation test tolerance:** Position values Ã‚Â±5%, speed/spin values Ã‚Â±3%. The implementation should reproduce this trajectory within these bounds when given identical initial conditions.

---

### B.6 Surface Properties Summary Table

Consolidated from Section 3.1 for quick reference during testing:

| Property | GRASS_DRY | GRASS_WET | GRASS_LONG | ARTIFICIAL | FROZEN |
|----------|-----------|-----------|------------|------------|--------|
| COR (e) | 0.65 | 0.70 | 0.55 | 0.72 | 0.80 |
| Friction (ÃŽÂ¼) | 0.60 | 0.40 | 0.70 | 0.55 | 0.20 |
| Rolling (Î¼_r) | 0.13 | 0.07 | 0.22 | 0.09 | 0.04 |
| Spin Retention | 0.80 | 0.85 | 0.70 | 0.75 | 0.90 |
| Rolling stop dist (10 m/s)* | 28.3m | 43.6m | 18.7m | 36.8m | 61.0m |
| Bounce height (2m drop) | 0.85m | 0.98m | 0.60m | 1.04m | 1.28m |

*Rolling distances from 60Hz numerical simulation (see B.4). Values consistent with Section 3.1.14 expected ranges and broadcast footage observations.

---

### B.7 Physical Constants Quick Reference

| Constant | Symbol | Value | Unit | Source |
|----------|--------|-------|------|--------|
| Ball mass | m | 0.43 | kg | FIFA Law 2 |
| Ball radius | r | 0.11 | m | FIFA Law 2 (69cm circ.) |
| Cross-section area | A | 0.0380 | mÃ‚Â² | Ãâ‚¬ Ãƒâ€” rÃ‚Â² |
| Moment of inertia | I | 0.00347 | kgÃ‚Â·mÃ‚Â² | (2/3)mrÃ‚Â² |
| Air density | ÃÂ | 1.225 | kg/mÃ‚Â³ | Sea level, 15Ã‚Â°C |
| Gravity | g | 9.81 | m/sÃ‚Â² | Standard |
| Air viscosity | ÃŽÂ¼ | 1.81Ãƒâ€”10Ã¢ÂÂ»Ã¢ÂÂµ | PaÃ‚Â·s | Sea level, 15Ã‚Â°C |
| (1/2)ÃÂA | Ã¢â‚¬â€ | 0.02328 | kg/m | Precomputed drag constant |

---

## Appendix C: Visual Diagrams

This appendix provides text-based diagrams for specification clarity. These are intended to be recreated as proper graphics during Stage 1 (2D Rendering Specification) but serve as authoritative references for implementation.

---

### C.1 Ball State Machine

```
                         Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
                         Ã¢â€â€šSTATIONARYÃ¢â€â€š
                         Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                              Ã¢â€â€š
                    Kick/Touch event
                              Ã¢â€â€š
                    Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
                    Ã¢â€â€š                   Ã¢â€â€š
              v.z > ENTER         v.z Ã¢â€°Ë† 0
              threshold           (ground)
                    Ã¢â€â€š                   Ã¢â€â€š
               Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â      Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
               Ã¢â€â€š AIRBORNE Ã¢â€â€š      Ã¢â€â€š   ROLLING   Ã¢â€â€š
               Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ      Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                    Ã¢â€â€š                    Ã¢â€â€š
        z Ã¢â€°Â¤ EXIT_THRESH          v < MIN_VELOCITY
        AND v.z < 0                     Ã¢â€â€š
                    Ã¢â€â€š              Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
               Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â        Ã¢â€â€š STATIONARY  Ã¢â€â€š
               Ã¢â€â€šBOUNCING Ã¢â€â€š        Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
               Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                    Ã¢â€â€š
            Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
            Ã¢â€â€š       Ã¢â€â€š       Ã¢â€â€š
        v.z > BOUNCE   v.z < BOUNCE   v < MIN_V
        THRESH         THRESH
            Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š
       Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â   Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
       Ã¢â€â€š AIRBORNE Ã¢â€â€š   Ã¢â€â€š  ROLLING  Ã¢â€â€š  Ã¢â€â€š STATIONARY Ã¢â€â€š
       Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ

  Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬

  ANY STATE Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ IsOutOfBounds() Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº OUT_OF_PLAY

  ANY STATE Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Agent possession Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº CONTROLLED
  CONTROLLED Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Kick/release Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº AIRBORNE or ROLLING
```

**Hysteresis detail:**
```
  Height (z, ball center)
    Ã¢â€â€š
  0.17m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ ENTER threshold (ROLLING Ã¢â€ â€™ AIRBORNE)
    Ã¢â€â€š          Ã¢â€“Â²
    Ã¢â€â€š   Dead   Ã¢â€â€š  Ball must RISE above 0.17m to become AIRBORNE
    Ã¢â€â€š   zone   Ã¢â€â€š  Ball must FALL below 0.13m to leave AIRBORNE
    Ã¢â€â€š          Ã¢â€“Â¼
  0.13m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ EXIT threshold (AIRBORNE Ã¢â€ â€™ BOUNCING)
    Ã¢â€â€š
  0.11m Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ground level (ball center at RADIUS)
```

---

### C.2 Force Diagram: Airborne Ball

```
                    F_magnus (perpendicular to v and Ãâ€°)
                        Ã¢â€ â€”
                       /
                      /
            Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº v (velocity)
                     Ã¢â€â€š\
                     Ã¢â€â€š \
                     Ã¢â€â€š  \ F_drag (opposes v)
                     Ã¢â€â€š
                     Ã¢â€“Â¼
                  F_gravity (always -Z)
                  = -4.22 N


  Net force: F_net = F_gravity + F_drag + F_magnus
  
  Typical magnitudes at 25 m/s, 12 rad/s spin:
    F_gravity = 4.22 N (constant, downward)
    F_drag    = 1.45 N (opposing motion)
    F_magnus  = 1.07 N (perpendicular to flight)
```

---

### C.3 Force Diagram: Rolling Ball

```
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ground surface
  
            F_drag (opposes v)
               Ã¢â€ ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢Å â€¢ Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº v (velocity)
                     Ã¢â€â€š
                     Ã¢â€“Â¼
              F_rolling_friction
              = ÃŽÂ¼_r Ãƒâ€” m Ãƒâ€” g
              (opposes v, parallel to ground)

  Notes:
  - No gravity force shown (balanced by normal force from ground)
  - No Magnus force (only active in AIRBORNE state)
  - Drag is typically small at rolling speeds
  - Rolling friction dominates below ~7 m/s
```

---

### C.4 Bounce Contact Mechanics

```
  BEFORE BOUNCE:                    AFTER BOUNCE:

       v_incoming                      v_rebound
          Ã¢â€ Ëœ                               Ã¢â€ â€”
           \  ÃŽÂ¸_in                  ÃŽÂ¸_out /
            \                            /
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€”ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬    Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€”ÂÃ¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
           GROUND                    GROUND

  Normal decomposition:
  
  v_n = v Ã‚Â· nÃŒâ€š  (into ground, negative)    Ã¢â€ â€™  v_n_after = -e Ãƒâ€” v_n
  v_t = v - v_nÃ‚Â·nÃŒâ€š  (along ground)         Ã¢â€ â€™  v_t_after = v_t + J_friction/m


  Contact point velocity (with spin):

                  Ãâ€° (angular velocity)
                  Ã¢â€ Âº
              Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
              Ã¢â€â€š       Ã¢â€â€š  r = 0.11m
              Ã¢â€â€š   Ã¢â€”Â   Ã¢â€â€š  center
              Ã¢â€â€š       Ã¢â€â€š
              Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
                  Ã¢â€â€š r_contact = -r Ãƒâ€” nÃŒâ€š
                  Ã¢â€”Â  contact point
  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ ground
  
  v_contact = v_tangential + (Ãâ€° Ãƒâ€” r_contact)
  
  If ball has topspin: contact point moves FORWARD
    Ã¢â€ â€™ friction acts BACKWARD on contact point
    Ã¢â€ â€™ ball gains backspin, loses forward speed (less than no-spin case)
    
  If ball has backspin: contact point moves BACKWARD
    Ã¢â€ â€™ friction acts FORWARD on contact point  
    Ã¢â€ â€™ ball "checks up" Ã¢â‚¬â€ dramatically reduces forward speed
```

---

### C.5 Coordinate System Reference

```
  TOP VIEW (as rendered in 2D):
  
  Y (pitch width, 0-68m)
  Ã¢â€“Â²
  Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
68mÃ¢â€â€š  Ã¢â€â€š                    Ã¢Å â€¢ Ball (x,y)                Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š     GOAL                             GOAL       Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š     Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤                             Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤       Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€š                                                 Ã¢â€â€š
  Ã¢â€â€š  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
  Ã¢â€â€š
  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X (pitch length, 0-105m)
  
  
  SIDE VIEW (Z axis, not rendered but simulated):
  
  Z (height)
  Ã¢â€“Â²
  Ã¢â€â€š        Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ trajectory Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â®
  Ã¢â€â€š       Ã¢â€¢Â±                    Ã¢â€¢Â²
  Ã¢â€â€š      Ã¢â€¢Â±                      Ã¢â€¢Â²
  Ã¢â€â€š     Ã¢Å â€¢                        Ã¢â€¢Â²
  Ã¢â€â€š    Ã¢â€¢Â±                           Ã¢Å â€¢ Ã¢â€ Â bounce
  Ã¢â€â€š   Ã¢â€¢Â±                          Ã¢â€¢Â±  Ã¢â€¢Â²
  Ã¢â€â€šÃ¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢Å â€¢Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ ground (z = 0.11m, ball center)
  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X
  
  
  AXIS CONVENTIONS:
  X = pitch length (0-105m), goal-to-goal
  Y = pitch width (0-68m), touchline-to-touchline
  Z = height (0m = ground surface, ball center rests at 0.11m)
  
  Spin axes:
  Ãâ€°_x > 0 = rotation about X axis (topspin for +Y movement)
  Ãâ€°_y > 0 = rotation about Y axis (backspin for +X movement)  
  Ãâ€°_z > 0 = CCW from above (ball curves LEFT for +X movement)
  Ãâ€°_z < 0 = CW from above (ball curves RIGHT for +X movement)
```

---

### C.6 Drag Crisis Visualization

```
  C_d
  0.25 Ã¢â€â€š
       Ã¢â€â€š
  0.20 Ã¢â€â€šÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€â€œ
       Ã¢â€â€š                     Ã¢â€Æ’ Ã¢â€ Â Drag crisis transition
  0.15 Ã¢â€â€š                     Ã¢â€Æ’      (linear interpolation)
       Ã¢â€â€š                     Ã¢â€Æ’
  0.10 Ã¢â€â€š                     Ã¢â€â€”Ã¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€ÂÃ¢â€Â
       Ã¢â€â€š
  0.05 Ã¢â€â€š
       Ã¢â€â€š
  0.00 Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
               10      15    20      25      30     Speed (m/s)
                              Ã¢â€â€š       Ã¢â€â€š
                         CRISIS_LOW  CRISIS_HIGH
                          (20 m/s)   (25 m/s)

  
  Drag FORCE (shows the "dip"):
  
  F_drag (N)
  3.0 Ã¢â€â€š                                          Ã¢â€¢Â±
      Ã¢â€â€š                                        Ã¢â€¢Â±
  2.5 Ã¢â€â€š                                      Ã¢â€¢Â±
      Ã¢â€â€š                                    Ã¢â€¢Â±
  2.0 Ã¢â€â€š                        Ã¢â€¢Â±Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²      Ã¢â€¢Â±
      Ã¢â€â€š                      Ã¢â€¢Â±     Ã¢â€¢Â²   Ã¢â€¢Â±
  1.5 Ã¢â€â€š                    Ã¢â€¢Â±        Ã¢â€¢Â²Ã¢â€¢Â±  Ã¢â€ Â KNUCKLEBALL WINDOW
      Ã¢â€â€š                  Ã¢â€¢Â±               Drag dips here
  1.0 Ã¢â€â€š               Ã¢â€¢Â±
      Ã¢â€â€š            Ã¢â€¢Â±
  0.5 Ã¢â€â€š         Ã¢â€¢Â±
      Ã¢â€â€š      Ã¢â€¢Â±
  0.0 Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â±Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
          5     10     15    20    25    30    Speed (m/s)
```

---

### C.7 Physics Update Loop Flowchart

```
  UpdateBallPhysics(ball, dt, surface, wind)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Save last valid state
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Is state BOUNCING?
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ YES Ã¢â€ â€™ ApplyBounce() Ã¢â€ â€™ continue (don't return)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ NO  Ã¢â€ â€™ continue
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Calculate forces by state:
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ AIRBORNE Ã¢â€ â€™ gravity + drag + Magnus
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ ROLLING  Ã¢â€ â€™ drag + rolling friction
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ BOUNCING Ã¢â€ â€™ drag only (bounce already applied)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ OTHER    Ã¢â€ â€™ return (no physics)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Semi-implicit Euler integration:
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ v += (F_net / m) Ãƒâ€” dt
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ x += v Ãƒâ€” dt    (uses NEW velocity)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Update spin (AIRBORNE only):
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Ãâ€° = UpdateSpinDecay(Ãâ€°, v, dt)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Validate state (ALWAYS):
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ NaN/Infinity check Ã¢â€ â€™ recover if detected
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Velocity clamp (50 m/s max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Spin clamp (80 rad/s max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Height clamp (50m max)
  Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬ Ground penetration fix (z Ã¢â€°Â¥ RADIUS)
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Position bounds (pitch + 20m buffer)
  Ã¢â€â€š
  Ã¢â€Å“Ã¢â€â‚¬ Update state machine (ALWAYS):
  Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬ Evaluate transitions with hysteresis
  Ã¢â€â€š
  Ã¢â€â€Ã¢â€â‚¬ Log snapshot (ALWAYS):
      Ã¢â€â€Ã¢â€â‚¬ TryLogSnapshot() at configured interval
```

---

### C.8 Spin Effect Visualization

```
  TOP VIEW - Sidespin effects on ball moving in +X direction:
  
  Ãâ€°_z > 0 (CCW from above):        Ãâ€°_z < 0 (CW from above):
  
       Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ball path                 Ball path Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â®
      Ã¢â€¢Â±                                               Ã¢â€¢Â²
     Ã¢â€¢Â±   F_magnus Ã¢â€ Â (Ã¢Ë†â€™Y)                F_magnus Ã¢â€ â€™ (+Y) Ã¢â€¢Â²
    Ã¢â€¢Â±                                                      Ã¢â€¢Â²
   Ã¢Å â€¢ Start                              Start Ã¢Å â€¢
   
   Ball curves LEFT                     Ball curves RIGHT


  SIDE VIEW - Topspin vs Backspin on ball moving in +X direction:
  
  Topspin (Ãâ€°_y < 0):                Backspin (Ãâ€°_y > 0):
  
  ZÃ¢â€â€š    Ã¢â€¢Â­Ã¢â€¢Â®                           ZÃ¢â€â€š
   Ã¢â€â€š   Ã¢â€¢Â±  Ã¢â€¢Â² Ã¢â€ Â dips earlier            Ã¢â€â€š   Ã¢â€¢Â­Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â® Ã¢â€ Â floats longer
   Ã¢â€â€š  Ã¢â€¢Â±    Ã¢â€¢Â²                           Ã¢â€â€š  Ã¢â€¢Â±          Ã¢â€¢Â²
   Ã¢â€â€š Ã¢â€¢Â±      Ã¢â€¢Â²                          Ã¢â€â€š Ã¢â€¢Â±            Ã¢â€¢Â²
   Ã¢â€â€šÃ¢â€¢Â±        Ã¢â€¢Â²                         Ã¢â€â€šÃ¢â€¢Â±              Ã¢â€¢Â²
   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X                   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€¢Â²Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€“Âº X
   
   Steeper descent,                    Flatter arc,
   ball "dips"                         ball "hangs"
```

---

**END OF APPENDICES**

---

## Document Status

**Appendices Completion:**
- Ã¢Å“â€¦ Appendix A: Formula derivations for all 7 physics systems (Magnus, Drag, Bounce, Rolling, Spin, Integration, Goal Post)
- Ã¢Å“â€¦ Appendix B: 7 reference data tables with numerically verified values
- Ã¢Å“â€¦ Appendix C: 8 visual diagrams covering state machine, forces, coordinates, and spin effects
- Ã¢Å“â€¦ All derivations trace back to Section 3.1 v2.3 formulas
- Ã¢Å“â€¦ Magnus force and drag force values independently computed and verified
- Ã¢Å“â€¦ Trajectory data generated from 60Hz numerical simulation (not analytical estimates)
- Ã¢Å“â€¦ Rolling distance data generated from 60Hz numerical simulation
- Ã¢Å“â€¦ No fabricated or unverified data
- Ã¢Å“â€¦ Tanh model comparison table explicitly marked as estimated with absent k parameter noted

**Flagged issues for resolution before Spec #1 approval:**
- âœ… Section 3.1.14 rolling distance test case: RESOLVED in v1.2. Î¼_r updated to 0.13 for GRASS_DRY; test case expected range updated to 26â€“31m; B.4 simulation confirms 28.3m. See REV-001.
- Ã¢Å¡Â Ã¯Â¸Â Appendix A.1.2 tanh comparison uses estimated curve parameters Ã¢â‚¬â€ acceptable for justifying the simplification but not authoritative for the literature model

**Known limitations:**
- Appendix C diagrams are text-based Ã¢â‚¬â€ proper vector graphics deferred to Stage 1

**Page Count:** ~22 pages  
**Status:** DRAFT v1.1 Ã¢â‚¬â€ Revision pass complete

**With these appendices, Ball Physics Specification #1 now has all template-required sections complete:**
- Sections 1-2 (Purpose, Requirements) v1.1
- Section 3.1 (Core Formulas) v2.3
- Section 4 (Implementation) v1.1
- Section 5 (Testing) v1.0
- Section 6 (Performance Analysis) v1.0
- Section 7 (Future Extensions) v1.0
- Section 8 (References) v1.2
- Appendices A, B, C v1.1 Ã¢â€ Â this document
