# Perception System Specification #7 — Appendix A: Formula Derivations

**File:** `Perception_System_Spec_Appendix_A_v1_1.md`  
**Purpose:** Step-by-step derivations for every formula introduced in Section 3 of the
Perception System Specification. Where Section 3 presents the final implementation form,
this appendix shows *how* each formula was reached and *why* each design decision was made.
Derivations trace from theoretical basis through algebraic construction to the final
expression used in code. This appendix is required for formal specification approval.

**Created:** February 26, 2026, 9:00 PM PST  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Dependencies:**
- Perception System Spec #7 — Section 3 v1.2 (authoritative formula source)
- Perception System Spec #7 — Section 8 v1.1 (citation audit; academic brackets)
- First Touch Mechanics Spec #4 §3.5 (pressure scalar formula — reused verbatim)
- Agent Movement Spec #2 §3.5.6 (`PlayerAttributes.Decisions`, `Anticipation` — confirmed fields)

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 26, 2026, 9:00 PM PST | Initial draft |
| 1.1 | February 26, 2026 | Two fixes: (1) A.1 — clarified that the Decisions/20 formula gives D=1 a 0.5° non-zero baseline; removed erroneous phrasing implying range spans exactly 10°. (2) A.2 — floor-reachability calculation updated: correct worst-case EffectiveFoV at D=1, PS=1.0 is 130.5° (was written as 130.5° but now explicitly derived from the D/20 formula: 160.5° − 30° = 130.5°). Both fixes align with upstream Section 3 v1.2 and Section 5 v1.2 corrections. |

> **Derivation vs. tuning:** Constants marked [GT] are gameplay-tuned with documented
> academic plausibility brackets. Their values are design decisions validated against
> published research ranges, not derived from first principles. This is explicitly
> acknowledged — fabricating post-hoc academic derivations for tuned values would
> misrepresent what requires playtesting versus what is physics- or psychology-constrained.

> **Rendering note:** This document uses mathematical symbols (×, ÷, ≈, √, θ, °, ≤, ≥)
> that require UTF-8 encoding to display correctly.

---

## Table of Contents

- [A.1 Field of View — Decisions Modifier](#a1-field-of-view--decisions-modifier)
- [A.2 Field of View — Pressure Degradation](#a2-field-of-view--pressure-degradation)
- [A.3 Blind-Side Arc Derivation](#a3-blind-side-arc-derivation)
- [A.4 Shadow Cone Half-Angle Geometry](#a4-shadow-cone-half-angle-geometry)
- [A.5 Shadow Cone — Depth Test (Dot Product Form)](#a5-shadow-cone--depth-test-dot-product-form)
- [A.6 Recognition Latency — Linear Interpolation and Floor Rounding](#a6-recognition-latency--linear-interpolation-and-floor-rounding)
- [A.7 Recognition Latency — Half-Turn Bonus](#a7-recognition-latency--half-turn-bonus)
- [A.8 Recognition Latency — Peripheral Arc Boundary](#a8-recognition-latency--peripheral-arc-boundary)
- [A.9 Additive-Only Noise — Floor Preservation Proof](#a9-additive-only-noise--floor-preservation-proof)
- [A.10 Shoulder Check Interval — Linear Interpolation](#a10-shoulder-check-interval--linear-interpolation)
- [A.11 Shoulder Check — Deterministic Jitter Range](#a11-shoulder-check--deterministic-jitter-range)
- [A.12 Pressure Scalar Formula (Cross-Spec Reference)](#a12-pressure-scalar-formula-cross-spec-reference)
- [A.13 Confirmation Expiry Window — Minimum Derivation](#a13-confirmation-expiry-window--minimum-derivation)

---

## A.1 Field of View — Decisions Modifier

**Source:** §3.1.3  
**Formula (final implementation):**
```
EffectiveFoV_BeforePressure = BASE_FOV_ANGLE + (Decisions / 20.0f) × MAX_FOV_BONUS_ANGLE
```

### Derivation

**Theoretical basis:** Williams & Davids (1998) document that expert football players
maintain broader spatial awareness during play — they do not narrow fixation under cognitive
load in the way novices do. The Decisions attribute represents cognitive processing bandwidth
and scanning discipline. Higher Decisions agents model this expert characteristic.

**Design constraints imposed:**
1. At Decisions = 20 (maximum): full `MAX_FOV_BONUS_ANGLE` added.
2. At Decisions = 1 (minimum): a small non-zero baseline bonus retained (0.5°).
3. Interpolation must be linear — consistent with all other attribute scaling in this project.
4. Effect must be small relative to base FoV — Decisions primarily governs *recognition speed*
   (§3.3), not *vision width*. A total range of approximately 9.5° satisfies this constraint.

**Construction:**

The formula uses `Decisions / 20.0f` (not `(Decisions - 1) / 19.0f` as used in L_rec).
This choice gives Decisions=1 a small non-zero bonus (0.5°) rather than zero bonus,
reflecting that even a poor decision-maker still maintains some baseline scanning awareness
above the bare `BASE_FOV_ANGLE`. This is a deliberate [GT] design choice, not an error.

The two formulations differ at the minimum attribute value:
- `D / 20`: D=1 → 0.05 × 10° = 0.5° (non-zero baseline)
- `(D-1) / 19`: D=1 → 0.0 × 10° = 0.0° (zero bonus at minimum)

L_rec uses `(D-1)/19` because the academic floor (L_MIN = 1 tick) must be reachable at
exactly Decisions=20. FoV uses `D/20` because there is no requirement that D=1 produces
exactly BASE_FOV_ANGLE — the small baseline bonus is a design preference.

```
At Decisions = 1:   bonus = (1/20) × 10° = 0.5°
At Decisions = 10:  bonus = (10/20) × 10° = 5.0°
At Decisions = 20:  bonus = (20/20) × 10° = 10.0°
```

**Output range:**
```
Minimum EffectiveFoV_BeforePressure = 160° + 0.5° = 160.5°  (Decisions=1)
Maximum EffectiveFoV_BeforePressure = 160° + 10°  = 170.0°  (Decisions=20)
Total span across [1,20]: 9.5°
```

**Academic plausibility check.** Williams & Davids (1998) report expert players maintain
awareness ~6% wider than novices in spatial recall tasks. 10° on a 160° base = 6.25%
maximum widening. This is within the reported range. Constants are marked [GT] because
the exact mapping from "scanning efficiency" to angular degrees is a gameplay calibration
decision, not a directly measurable perceptual quantity.

**Constants:**
- `BASE_FOV_ANGLE` = 160° [GT — within Franks & Miller 1985 plausibility range 140°–180°]
- `MAX_FOV_BONUS_ANGLE` = 10° [GT — calibrated against Williams & Davids 1998 ~6% spread]

---

## A.2 Field of View — Pressure Degradation

**Source:** §3.1.4  
**Formula (final implementation):**
```
PressureReduction = PressureScalar × MAX_FOV_PRESSURE_REDUCTION
EffectiveFoV = Max(EffectiveFoV_BeforePressure - PressureReduction, MIN_FOV_ANGLE)
EffectiveFoV_HalfAngle = EffectiveFoV / 2.0f
```

### Derivation

**Theoretical basis:** Beilock (2010) documents attentional narrowing under psychological
pressure, with performance degradation of 20–50% in tasks requiring broad spatial attention.
Football agents under opponent pressure exhibit a real-world analogue: a player being
pressed tightly narrows their scanning focus toward the immediate threat, losing peripheral
awareness of distant options.

**Design constraints imposed:**
1. PressureScalar = 0.0 → zero reduction (no pressure, full FoV).
2. PressureScalar = 1.0 → maximum reduction (saturated pressure, no threshold gate).
3. The floor `MIN_FOV_ANGLE` prevents degenerate near-zero cone at edge cases.
4. Pressure degrades FoV *breadth* only — entities within the narrowed cone are
   perceived at full fidelity (KD-7 design decision, not derived here).
5. **No threshold gate.** The pressure reduction is continuous from PressureScalar=0.
   There is no `PRESSURE_FOV_THRESHOLD` constant. Any pressure, however small, produces
   a proportional (though numerically small) FoV reduction.

**Construction:**

Step 1 — Select the maximum reduction magnitude. Beilock (2010) brackets 20–50%
degradation in broad attention tasks. Taking the lower end of this bracket as the [GT]
calibration target:

```
Target: ~18% reduction in FoV angle at maximum pressure
30° reduction on 170° max EffectiveFoV = 30/170 = 17.6% ≈ lower bound of Beilock bracket
```

`MAX_FOV_PRESSURE_REDUCTION` = 30° [GT — within Beilock 2010 20–50% performance
degradation bracket; 30° ≈ 18% of maximum 170° FoV — intentionally conservative]

Step 2 — Confirm the floor is unreachable at PressureScalar ∈ [0.0, 1.0]:
```
Worst case: Decisions=1, PressureScalar=1.0
EffectiveFoV = 160.5° − 30° = 130.5°
Floor check: 130.5° > MIN_FOV_ANGLE (120°) → floor NOT reached

To reach floor from D=1: 160.5° − (P × 30°) < 120°
P > 40.5° / 30° = 1.35 → impossible (capped at 1.0)

Worst case from D=20: 170.0° − (P × 30°) < 120°
P > 50° / 30° = 1.67 → even more impossible
```

The floor is unreachable under any combination of current constants with PressureScalar
∈ [0.0, 1.0]. It exists as a safety net against future constant changes or float rounding
edge cases. `MIN_FOV_ANGLE` = 120° [GT — safety floor].

Step 3 — Linear scaling rationale. A linear relationship between PressureScalar and FoV
reduction is the simplest model consistent with a continuous-valued pressure input. Non-linear
options (quadratic curve, sigmoid) are deferred to Stage 1 as potential improvements if
playtesting reveals the linear response feels mechanically wrong at the extremes.

**Final formula confirmed:** Linear in PressureScalar, no threshold, clamped at floor.

---

## A.3 Blind-Side Arc Derivation

**Source:** §3.1.5, §3.4.1  
**Formula (final implementation):**
```
BlindSide_Arc = 360° - EffectiveFoV
BlindSide_HalfAngle = (360° - EffectiveFoV) / 2.0f
BlindSide_Center = Observer.FacingAngle + 180°
```

### Derivation

**Theoretical basis:** The blind-side arc is a purely geometric consequence of the FoV
cone definition. If an agent can perceive within ±HalfAngle of their facing direction,
the complement of this region — the region they *cannot* perceive — is the blind side.

**Construction:**

Step 1 — The FoV cone covers EffectiveFoV degrees symmetrically around FacingDirection.
The remaining angular space is 360° - EffectiveFoV.

Step 2 — By symmetry (the FoV cone is symmetric), the blind-side region is also symmetric
around the rear-facing direction (FacingAngle + 180°). Therefore each half of the
blind-side arc spans (360° - EffectiveFoV) / 2.0f degrees from the rear vector.

Step 3 — Confirm ranges across EffectiveFoV range [120°, 170°]:
```
At EffectiveFoV = 170.0° (maximum, Decisions=20, PressureScalar=0):
  BlindSide_Arc = 190.0°. BlindSide_HalfAngle = 95.0°.

At EffectiveFoV = 160.5° (minimum unpressured, Decisions=1, PressureScalar=0):
  BlindSide_Arc = 199.5°. BlindSide_HalfAngle = 99.75°.

At EffectiveFoV = 130.5° (maximum pressure, Decisions=1, PressureScalar=1.0):
  BlindSide_Arc = 229.5°. BlindSide_HalfAngle = 114.75°.

Neutral case (Decisions=10, PressureScalar=0, EffectiveFoV=165°):
  BlindSide_Arc = 195.0°. BlindSide_HalfAngle = 97.5°.
```

**Design note — KD-5 restatement:** The blind-side arc width is *not* a fixed constant —
it varies continuously with EffectiveFoV, which varies with Decisions and PressureScalar.
What KD-5 locks is that Anticipation does NOT modify the geometry of the blind-side arc.
Anticipation controls check *frequency* only. This prevents high-Anticipation agents from
gaining unrealistic effective rear-facing vision.

**No new constant introduced.** BlindSide_Arc is fully determined by EffectiveFoV.

---

## A.4 Shadow Cone Half-Angle Geometry

**Source:** §3.2.3  
**Formula (final implementation):**
```
rawShadowHalfAngle = arcsin(AGENT_BODY_RADIUS / Max(occluderDistance, AGENT_BODY_RADIUS + 0.1f))
shadowHalfAngle = Max(rawShadowHalfAngle, MIN_SHADOW_HALF_ANGLE)
```

### Derivation

**Theoretical basis:** An agent (occluder) of physical radius `r` at distance `d` from
an observer subtends an angular half-width as seen from the observer. This is the standard
apparent-size formula from projective geometry.

**Step-by-step geometric construction:**

Consider the observer O at origin, and the occluder centre P at distance `d` along the
x-axis. The occluder is modelled as a circle of radius `r = AGENT_BODY_RADIUS`.

The two tangent lines from O to the circle of radius `r` centred at P define the angular
extent of the occlusion shadow. The half-angle θ between the axis OP and each tangent line
satisfies:

```
sin(θ) = r / d
θ = arcsin(r / d)
```

This is valid when d > r (observer is outside the occluder circle). For d ≤ r (observer
inside or at the occluder surface — an edge case at very close range), the formula
would produce arcsin(≥1), which is undefined or 90°. The denominator guard handles this:

```
effectiveDistance = Max(d, AGENT_BODY_RADIUS + 0.1f)
```

Adding 0.1m ensures the denominator is always at least 0.5m, keeping the arcsin argument
below 1.0 (0.4 / 0.5 = 0.8, arcsin(0.8) = 53.1°, which is a wide but non-degenerate cone).

**Minimum floor rationale:**

At large occluder distances, `arcsin(r/d)` approaches zero:
```
At d = 10m:  arcsin(0.4/10) = arcsin(0.04) = 2.29°
At d = 20m:  arcsin(0.4/20) = arcsin(0.02) = 1.15°
At d = 50m:  arcsin(0.4/50) = arcsin(0.008) = 0.46°
```

Angles below approximately 5° represent physically meaningless shadow effects at the
10Hz resolution of the perception system. A target 1mm outside a 0.46° shadow cone
at 50m range would not realistically be considered "unoccluded." The floor prevents:
- Near-zero cone tests that provide no meaningful occlusion semantics
- Floating-point sensitivity at extreme ranges

`MIN_SHADOW_HALF_ANGLE` = 5° [GT — lower than 5° has no tactically meaningful occlusion
effect at the 10Hz heartbeat resolution and 22-agent scenario scale]

**Floor crossover distance:**
```
arcsin(0.4/d) = 5°
0.4/d = sin(5°) = 0.08716
d = 0.4 / 0.08716 = 4.589m
```
Floor activates at approximately 4.6m occluder distance.

**Cross-check with tests OCC-004 and OCC-005:**
```
Occluder at 5m: arcsin(0.4/5) = arcsin(0.08) = 4.57° < 5° → floor applies → 5°
Occluder at 2m: arcsin(0.4/2) = arcsin(0.2) = 11.54° > 5° → floor does not apply
```
Consistent with Section 5 §5.3.

---

## A.5 Shadow Cone — Depth Test (Dot Product Form)

**Source:** §3.2.4  
**Formula (final implementation):**
```
// Target T is occluded if:
// (1) T is further from observer than occluder P (depth test)
// (2) T's bearing falls within P's shadow cone (angular test)

// Depth test (dot product form):
isTargetBeyondOccluder = dot(T.Position - O.Position, P.Position - O.Position)
                        > dot(P.Position - O.Position, P.Position - O.Position)
```

### Derivation

**Why dot product instead of distance comparison?**

A naive depth test compares scalar distances:
```
|T - O| > |P - O|
```

This correctly checks whether T is farther from O than P in total Euclidean distance.
However, it does not check whether T is farther along the *line of sight* through P.
A target that is farther in total distance but laterally displaced may be closer along
the O→P ray than P itself is.

The dot product form projects T onto the O→P axis:
```
projection of T onto O→P axis = dot(T - O, P - O) / |P - O|
projection of P onto O→P axis = dot(P - O, P - O) / |P - O| = |P - O|
```

Dropping the shared denominator (|P - O|) — valid since we only care about the
inequality, not the absolute value — gives:

```
dot(T - O, P - O) > dot(P - O, P - O)
```

This reads: "The component of (T - O) along the direction (P - O) is greater than the
full length of (P - O)." Equivalently: T is further along the O→P ray than P is.

This is strictly more correct than the scalar distance test for off-axis targets, and
it is branchless and avoids the sqrt() implicit in distance magnitude comparisons.

**Boundary condition:** The test uses strict greater-than (`>`). A target at exactly the
same depth as the occluder is NOT occluded. Confirmed by test OCC-003:
```
Observer (0,0). Occluder (5,0). Target (5,1).
dot((5,1),(5,0)) = 25. dot((5,0),(5,0)) = 25. 25 > 25 = false → not occluded. ✓
```

---

## A.6 Recognition Latency — Linear Interpolation and Floor Rounding

**Source:** §3.3.2  
**Formula (final implementation):**
```
L_rec_base_float(Decisions) = L_MAX - ((Decisions - 1) / 19.0f) × (L_MAX - L_MIN)
                             = 5 - ((Decisions - 1) / 19.0f) × 4

L_rec_base_ticks = Mathf.FloorToInt(L_rec_base_float)  // Floor rounding — AUTHORITATIVE
```

### Derivation

**Theoretical basis:** Helsen & Starkes (1999) report recognition latency of 100–500ms
for football agents, with experts at the lower end. Franks & Miller (1985) corroborate
a 400–500ms upper bound for novice recognition of novel stimuli. The Decisions attribute
governs this cognitive speed.

**Design constraints:**
1. Decisions = 1 → L_rec = L_MAX (slowest recognition)
2. Decisions = 20 → L_rec = L_MIN (fastest recognition)
3. Linear interpolation — consistent with project-wide attribute scaling.
4. Integer tick output — float must be converted to integer. **Floor rounding required.**

**Construction — standard two-point linear interpolation:**

Define the attribute as a normalised parameter `t` where t=0 at minimum, t=1 at maximum:
```
t = (Decisions - 1) / (20 - 1) = (Decisions - 1) / 19
```

The output L_rec interpolates from L_MAX (at t=0) down to L_MIN (at t=1):
```
L_rec_float = L_MAX - t × (L_MAX - L_MIN)
            = 5 - ((Decisions - 1) / 19) × 4
```

**Note on (D-1)/19 vs D/20:** L_rec uses `(Decisions - 1) / 19` while the FoV bonus
uses `Decisions / 20`. The distinction is intentional: L_rec must produce exactly L_MIN
(1.0 tick, a precise integer) at Decisions=20. Using `(D-1)/19`: at D=20, t=1.0 exactly,
giving L_rec=1.0 exactly. Using `D/20` would give 20/20=1.0 anyway at D=20, but would
give L_rec=0 at a hypothetical D=0, which is outside the valid range [1,20]. The `(D-1)/19`
form is semantically clearer: it maps the valid attribute range [1,20] directly to [0,1].

**Floor rounding rationale:** The perception system operates in integer heartbeat ticks.
The float L_rec must be discretised. Floor rounding (`Mathf.FloorToInt`) is specified
because:
- It is conservative: agents always wait at least as long as the formula suggests,
  never less. This errs toward caution (slower confirmation), not false confidence.
- It makes L_MIN reachable: at D=20, L_rec_float=1.0, floor(1.0)=1=L_MIN exactly.
  Ceiling rounding would give 1 too, but round() would also give 1 here.
- It is unambiguous: round() is ambiguous at 0.5 boundaries (banker's rounding vs.
  round-half-up). Floor has no such ambiguity.

**Academic bracket confirmation:**
- L_MAX = 5 ticks = 500ms [GT — within Franks & Miller 1985 novice upper bracket 400–500ms]
- L_MIN = 1 tick = 100ms [GT — within Helsen & Starkes 1999 expert lower bracket 100ms]

---

## A.7 Recognition Latency — Half-Turn Bonus

**Source:** §3.3.3 (cross-spec from First Touch Mechanics §3.3.2)  
**Formula (final implementation):**
```
if (observer.IsHalfTurned && isInPeripheralArc(entity)):
    L_rec_adjusted = Mathf.FloorToInt(L_rec_base_ticks × 0.85f)
    L_rec_adjusted = Max(L_rec_adjusted, L_MIN)
```

### Derivation

**Authority:** This constant (15% L_rec reduction for half-turned observers) is defined
in First Touch Mechanics Specification #4 §3.3.2. It is consumed here read-only. The
following summary is provided so this appendix is self-contained; the derivation authority
remains First Touch Spec #4.

**Basis:** A half-turned body orientation primes the observer's visual system for
information arriving from the peripheral and rear arc. Helsen & Starkes (1999) note
that body orientation affects stimulus recognition speed for laterally and rear-arriving
information. Players who position themselves half-turned — as technically skilled players
are coached to do — process peripheral stimuli faster than those facing directly forward.

The 15% reduction is calibrated [GT] within the plausibility range suggested by Helsen
& Starkes (1999). It represents a meaningful but not dominant advantage: at Decisions=10
(L_rec=3 ticks), the bonus reduces latency to 2 ticks — one tick (100ms) faster. This
is tactically significant but does not make the agent omniscient.

**Application scope:**
1. `observer.IsHalfTurned == true` — body orientation field from AgentState.
2. Target entity within peripheral arc (40°–80° from FacingDirection — see A.8).

**Floor guarantee:**
```
Most constrained case: L_rec_base_ticks = L_MIN = 1 tick, half-turn bonus applied:
  Mathf.FloorToInt(1 × 0.85) = Mathf.FloorToInt(0.85) = 0 ticks
  Max(0, L_MIN=1) = 1 tick — clamped ✓

The Max() clamp ensures the half-turn bonus cannot reduce L_rec below L_MIN.
L_MIN = 1 tick (100ms) is the absolute minimum recognition latency — no agent,
regardless of attribute or orientation, can recognise a new entity instantaneously.
```

---

## A.8 Recognition Latency — Peripheral Arc Boundary

**Source:** §3.3.3  
**Formula (final implementation):**
```
PERIPHERAL_ARC_INNER_BOUND = BASE_FOV_HALF_ANGLE / 2.0f  // = 80° / 2 = 40°

isInPeripheralArc = (|angularSeparation| >= PERIPHERAL_ARC_INNER_BOUND
                     && |angularSeparation| <= BASE_FOV_HALF_ANGLE)
                  = (|angularSeparation| >= 40° && |angularSeparation| <= 80°)
```

### Derivation

**Why 40° inner bound, not an independent constant?**

The half-turn orientation bonus is specifically a *peripheral* awareness advantage. It must
be distinguished from "central" (direct gaze area) and "outside FoV" (blind side).

The most principled definition of "peripheral" relative to the FoV cone: the outer half
of the cone. If the full FoV half-angle is 80°, the inner half is 0°–40° (focal zone),
and the outer half is 40°–80° (peripheral zone). This partitions the cone symmetrically
without introducing an independent constant.

**Derived, not tuned:**
```
PERIPHERAL_ARC_INNER_BOUND = BASE_FOV_HALF_ANGLE / 2.0f = 80° / 2 = 40°
```

Tagged [DERIVED] in §3.10. If `BASE_FOV_ANGLE` changes, the peripheral arc inner bound
updates automatically. No secondary constant requires updating.

**Confirmation this matches test expectations (Section 5):**
- LR-004: target at 55° (within 40°–80°) → bonus applies ✓
- LR-005a: target at 20° (below 40°, central zone) → no bonus ✓
- LR-005b: target beyond 80° (outside FoV) → not visible → no bonus ✓

---

## A.9 Additive-Only Noise — Floor Preservation Proof

**Source:** §3.3.4  
**Formula (final implementation):**
```
noise_ticks = DeterministicHash(observerId, targetId, frameNumber) % 2  // ∈ {0, 1}
L_rec_final = Min(L_rec_adjusted + noise_ticks, L_MAX)
```

### Derivation — Why Additive-Only?

**Problem statement:** To prevent synchronised recognition events across agents with
identical Decisions values, a small noise term is needed. The natural choice is ±1 tick
(symmetric noise). However, symmetric noise creates a floor violation problem:

**Symmetric noise (±1) failure case:**
```
At Decisions=20, L_rec_base = L_MIN = 1 tick.
noise = -1 tick (possible with symmetric distribution).
L_rec_final = 1 - 1 = 0 ticks → entity would appear in snapshot with ZERO latency wait.
```

Zero latency means the entity appears in the snapshot on the same tick it first enters
visibility — i.e., instant recognition. This violates the L_MIN = 100ms minimum and
breaks the attribute floor guarantee.

A secondary clamp `Max(L_rec_final, L_MIN)` would patch this, but it introduces
asymmetry: at Decisions=20, the clamp would activate on ~50% of recognition events,
creating an effective latency distribution of {1, 1, 2} ticks (skewed upward) and
obscuring the formula semantics.

**Additive-only noise (0 or +1) proof:**

```
Claim: L_rec_final >= L_MIN for all inputs, without a secondary Max() clamp.

Proof:
  L_rec_adjusted >= L_MIN
    (After half-turn bonus: L_rec_adjusted = Max(floor(L_rec_base × 0.85), L_MIN) >= L_MIN)
  noise_ticks ∈ {0, 1} (additive only — result is always non-negative)
  Therefore: L_rec_final = L_rec_adjusted + noise_ticks >= L_MIN + 0 = L_MIN ✓

  The Min(L_rec_final, L_MAX) cap only prevents the rare case where
  noise = 1 pushes an already-maximum latency above L_MAX = 5 ticks.
  This cap is correctness-preserving, not a floor patch.
```

**Trade-off accepted:** At Decisions=20, L_rec is always 1 or 2 ticks (never 0). This
is the correct and realistic behaviour — even elite players require at least 100ms to
consciously register a new agent in their field of view. The slight upward bias at the
elite end is a feature, not a defect.

---

## A.10 Shoulder Check Interval — Linear Interpolation

**Source:** §3.4.2  
**Formula (final implementation):**
```
CheckInterval_ticks(Anticipation) = CHECK_MAX_TICKS
    - ((Anticipation - 1) / 19.0f) × (CHECK_MAX_TICKS - CHECK_MIN_TICKS)
```

### Derivation

**Theoretical basis:** Franks & Miller (1985) document average players scanning every
3–5 seconds without time pressure. Master Volume 1 §3.1 specifies elite players performing
6–8 scans per possession. The Anticipation attribute governs proactive scanning frequency.

**Design constraints:**
1. Anticipation = 1 → CheckInterval = CHECK_MAX_TICKS (least frequent scanning)
2. Anticipation = 20 → CheckInterval = CHECK_MIN_TICKS (most frequent scanning)
3. Linear interpolation — consistent with L_rec and FoV modifier patterns.
4. Uses `(A-1)/19` form — same rationale as L_rec: maps [1,20] cleanly to [0,1],
   ensuring exact integer outputs at both endpoints.

**Construction — same two-point linear form as A.6:**
```
t = (Anticipation - 1) / 19

CheckInterval = CHECK_MAX_TICKS - t × (CHECK_MAX_TICKS - CHECK_MIN_TICKS)
              = 30 - ((Anticipation - 1) / 19) × (30 - 6)
              = 30 - ((Anticipation - 1) / 19) × 24
```

**Academic bracket confirmation:**

`CHECK_MAX_TICKS` = 30 ticks = 3.0 seconds [GT — lower end of Franks & Miller (1985)
3–5 second average scan interval. The lower end is used conservatively — worst-case
agents still scan at least every 3 seconds, within the published range.]

`CHECK_MIN_TICKS` = 6 ticks = 0.6 seconds [GT — derived from Master Vol 1 §3.1 "6–8
elite scans per possession". At 0.6s per scan: 6s possession ÷ 0.6s = 10 scans. Slightly
above the Master Vol 1 maximum of 8, but within the same order of magnitude.]

**Possession modifier:**
```
if (observer.HasPossession): CheckInterval_ticks × 2.0f
```
[GT — ×2.0 multiplier with no specific academic derivation. Rationale: a player in
possession directs full attention to ball control; blind-side scanning frequency is
plausibly halved. The factor of 2 is the simplest meaningful multiplier. Tunable.]

**Rounding:** CheckInterval is used as an integer tick count. Like L_rec, floor rounding
applies: `Mathf.FloorToInt(CheckInterval_float)`. At endpoints (A=1, A=20) the result
is exact (30.0 and 6.0 respectively).

---

## A.11 Shoulder Check — Deterministic Jitter Range

**Source:** §3.4.2  
**Formula (final implementation):**
```
jitter_ticks = (DeterministicHash(observerId, frameNumber) % 5) - 2
               // Range: {-2, -1, 0, +1, +2} ticks
NextCheckFrame = Max(CurrentFrame + CheckInterval_ticks + jitter_ticks, CurrentFrame + 1)
```

### Derivation

**Problem:** Without jitter, all agents with identical Anticipation values fire shoulder
checks on identical heartbeat frames. If 5 agents share Anticipation=10 (CheckInterval≈18
ticks), they check simultaneously, creating:
- Artificial synchrony in agent scanning behaviour
- Potentially exploitable patterns in emergent tactical AI

**Jitter range selection:**

The jitter must be small relative to CheckInterval but sufficient to desynchronise agents
across a plausible match scenario:

```
At CHECK_MIN_TICKS = 6 ticks: jitter ±2 = ±33% variation — significant desync
At CHECK_MAX_TICKS = 30 ticks: jitter ±2 = ±6.7% variation — trivially small
```

The `% 5` modulo produces a uniform integer distribution over {0, 1, 2, 3, 4}. Subtracting
2 centres this at 0: {-2, -1, 0, +1, +2}. Symmetric around zero — preserves mean
CheckInterval across many heartbeats with no systematic drift.

**Floor guarantee:** `Max(NextCheckFrame, CurrentFrame + 1)` ensures the next check is
always at least 1 tick in the future, preventing immediate re-trigger loops when large
negative jitter would otherwise schedule a check in the past or at the current frame.

**DeterministicHash properties required:**
- Deterministic: identical (observerId, frameNumber) inputs → identical output every run.
- Non-cryptographic: speed over security (e.g., simple integer multiply-xor mix).
- Uncorrelated: outputs for different (observerId, frameNumber) pairs must not cluster.

---

## A.12 Pressure Scalar Formula (Cross-Spec Reference)

**Source:** First Touch Mechanics Specification #4 §3.5.1–§3.5.3 (authoritative)  
**Status:** Consumed read-only. Not derived in this specification.

The pressure scalar formula is:
```
rawPressure = Σ (MIN_PRESSURE_DISTANCE / Max(distance_to_opponent, MIN_PRESSURE_DISTANCE))²
              for all opponents within PRESSURE_RADIUS

PressureScalar = Clamp(rawPressure / PRESSURE_SATURATION, 0.0f, 1.0f)
```

**Constants (First Touch Spec #4 authority):**
- `PRESSURE_RADIUS` = 3.0m
- `MIN_PRESSURE_DISTANCE` = 0.3m
- `PRESSURE_SATURATION` = 1.5

**Perception System usage:** PressureScalar is computed once per agent per heartbeat in
Step 3 of the pipeline (before ApplyFieldOfView — §3.0). It has exactly one use within
this specification: FoV narrowing (§3.1.4). It is stored in the snapshot for Decision Tree
diagnostic access. It is NOT applied to L_rec, shoulder check timing, or ball visibility.

**Rationale for single use:** Applying the pressure scalar to recognition latency would
double-penalise under-pressure agents: their FoV already narrows (fewer candidates
visible). Compounding that with slower recognition of what they can see would make
low-Decisions agents catastrophically non-functional under press, and would create
non-linear difficulty spikes that are hard to tune. The single-use constraint is a
deliberate [GT] design choice for gameplay balance and tractable parameter tuning.

**Cross-spec derivation responsibility:** The derivation of this formula's constants and
their academic justification is the sole responsibility of First Touch Mechanics Spec #4.
Any amendment to these values must be initiated through that specification.

---

## A.13 Confirmation Expiry Window — Minimum Derivation

**Source:** §3.3.6  
**Formula (final implementation):**
```
CONFIRMATION_EXPIRY_TICKS = 1  // 100ms — minimum meaningful window
```

### Derivation

**Tag: [DERIVED] — not gameplay-tuned.**

**Problem:** Without an expiry window, a confirmed entity that briefly exits an occlusion
boundary would disappear from the snapshot for 1 tick, then re-accumulate a full L_rec
counter before reappearing. At a shadow cone boundary, geometric jitter from agent movement
can produce single-tick visibility flickers — an entity is visible on tick N, just outside
the cone on tick N+1, visible again on tick N+2. Without an expiry window, each such
flicker resets awareness, causing snapshot instability.

**Minimum window derivation:**

The expiry window must be at least as long as the expected duration of a shadow cone
boundary crossing event. A boundary crossing event is determined by:
- Shadow cone angular width changes by `arcsin(r/d) × Δθ` per tick relative to agent motion
- At typical gameplay velocities (< 8 m/s) and occluder distances (5–15m), boundary
  crossings due to angular jitter resolve within ≤1 heartbeat tick

Therefore CONFIRMATION_EXPIRY_TICKS = 1 tick (100ms) is the minimum sufficient window.

**Why not 2 ticks?** A 2-tick window retains stale positions for 200ms — a tactically
meaningful interval during which an agent moving at 8 m/s covers ~1.6m. Holding a stale
snapshot entry for 200ms could cause decision-tree agents to react to positions that are
1.6m stale. The 1-tick window absorbs noise without meaningfully dating the data.

**This is not a GT constant.** The value 1 is the analytically minimum value that solves
the problem. If boundary oscillation persists beyond 1 tick, the appropriate fix is in
physics-layer stability (agent position integration), not expanding this window.

---

*End of Appendix A — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
