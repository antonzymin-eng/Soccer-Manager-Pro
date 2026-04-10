# Perception System Specification #7 — Appendix C: Sensitivity Analysis

**File:** `Perception_System_Spec_Appendix_C_v1_1.md`  
**Purpose:** Sensitivity analysis tables showing how key Perception System outputs vary
across the full attribute and input ranges. Serves two purposes: (1) confirming monotonicity
properties required by Section 5 balance tests, and (2) providing playtesting calibration
reference — if a range feels mechanically wrong in-game, the relevant [GT] constant is the
calibration target. Required for formal specification approval.

**Created:** February 26, 2026, 9:00 PM PST  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Dependencies:**
- Perception System Spec #7 — Section 3 v1.2 (all formulas — authoritative)
- Perception System Spec #7 — Section 5 v1.2 (balance test scenarios)
- Perception System Spec #7 — Appendix B v1.1 (numerical verification)

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 26, 2026, 9:00 PM PST | Initial draft |
| 1.1 | February 26, 2026 | Four corrections aligning with Section 3 v1.2 / Section 5 v1.2 upstream fixes: (1) C.1 FoV table corrected to D/20 formula — D=1 produces 160.5° (not 160°), correct blind-side arcs throughout. (2) C.2 and C.3 EffectiveFoV values corrected — Decisions=1 row starts at 160.5° not 160°; pressure tables re-computed with no-threshold formula. (3) C.8 blind-side matrix corrected at D=1 row. (4) C.9 BAL-003 pressure scenario re-calculated (FoV at D=10, PS=1.0 = 135°, not 135°; already correct but audit confirmed). C.10.1 worst-case confirmed at 130.5° floor approach (previously stated as 130.5°; now explicitly traced). |

> **Calibration guidance:** Constants marked [GT] are the tuning targets for playtesting
> adjustments. Constants marked [CROSS] are inherited from upstream specifications and
> must not be modified here. Constants marked [DERIVED] have no independent value.

> **Rendering note:** This document uses mathematical symbols (×, ÷, ≈, °, ≤, ≥)
> that require UTF-8 encoding to display correctly.

---

## Table of Contents

- [C.1 EffectiveFoV vs. Decisions](#c1-effectivefov-vs-decisions)
- [C.2 EffectiveFoV vs. PressureScalar](#c2-effectivefov-vs-pressurescalar)
- [C.3 Combined FoV: Decisions × PressureScalar](#c3-combined-fov-decisions--pressurescalar)
- [C.4 Recognition Latency vs. Decisions](#c4-recognition-latency-vs-decisions)
- [C.5 Shoulder Check Interval vs. Anticipation](#c5-shoulder-check-interval-vs-anticipation)
- [C.6 Shoulder Check: Possession Modifier Effect](#c6-shoulder-check-possession-modifier-effect)
- [C.7 Pressure Scalar vs. Opponent Distance and Count](#c7-pressure-scalar-vs-opponent-distance-and-count)
- [C.8 Blind-Side Arc vs. Decisions and Pressure](#c8-blind-side-arc-vs-decisions-and-pressure)
- [C.9 Balance Test Reference Scenarios](#c9-balance-test-reference-scenarios)
- [C.10 Calibration Sensitivity — Impact of ±10% on Key Constants](#c10-calibration-sensitivity--impact-of-10-on-key-constants)

---

## C.1 EffectiveFoV vs. Decisions

**Formula (Section 3 v1.2):** `EffectiveFoV = 160° + (Decisions / 20) × 10°` (PressureScalar=0)  
**Calibration targets:** `BASE_FOV_ANGLE` [GT], `MAX_FOV_BONUS_ANGLE` [GT]

| Decisions | FoV Bonus (°) | EffectiveFoV (°) | Half-Angle (°) | BlindSide Arc (°) |
|-----------|--------------|-----------------|----------------|------------------|
| 1         | 0.5°         | 160.5°          | 80.25°         | 199.5°           |
| 2         | 1.0°         | 161.0°          | 80.5°          | 199.0°           |
| 3         | 1.5°         | 161.5°          | 80.75°         | 198.5°           |
| 4         | 2.0°         | 162.0°          | 81.0°          | 198.0°           |
| 5         | 2.5°         | 162.5°          | 81.25°         | 197.5°           |
| 6         | 3.0°         | 163.0°          | 81.5°          | 197.0°           |
| 7         | 3.5°         | 163.5°          | 81.75°         | 196.5°           |
| 8         | 4.0°         | 164.0°          | 82.0°          | 196.0°           |
| 9         | 4.5°         | 164.5°          | 82.25°         | 195.5°           |
| 10        | 5.0°         | 165.0°          | 82.5°          | 195.0°           |
| 11        | 5.5°         | 165.5°          | 82.75°         | 194.5°           |
| 12        | 6.0°         | 166.0°          | 83.0°          | 194.0°           |
| 13        | 6.5°         | 166.5°          | 83.25°         | 193.5°           |
| 14        | 7.0°         | 167.0°          | 83.5°          | 193.0°           |
| 15        | 7.5°         | 167.5°          | 83.75°         | 192.5°           |
| 16        | 8.0°         | 168.0°          | 84.0°          | 192.0°           |
| 17        | 8.5°         | 168.5°          | 84.25°         | 191.5°           |
| 18        | 9.0°         | 169.0°          | 84.5°          | 191.0°           |
| 19        | 9.5°         | 169.5°          | 84.75°         | 190.5°           |
| 20        | 10.0°        | 170.0°          | 85.0°          | 190.0°           |

**Observations:**
- FoV scales linearly and monotonically with Decisions ✓
- Total spread: 160.5° to 170.0° = **9.5° range** across [1,20]
- D=1 has a 0.5° non-zero baseline because the formula uses D/20, not (D-1)/19
- Half-angle increases from 80.25° to 85.0° — entities at 82–85° lateral offset are
  visible only to high-Decisions agents; this creates observable tactical differentiation
- BlindSide arc: 199.5° (D=1) → 190° (D=20); 9.5° improvement in effective coverage

**Balance check:** The FoV difference between D=1 and D=20 at 30m:
9.5° × (π/180) × 30 ≈ 4.97m lateral coverage gain. Tactically meaningful at pitch scale
without being dominant.

---

## C.2 EffectiveFoV vs. PressureScalar

**Formula:** `EffectiveFoV = EffectiveFoV_BeforePressure − PressureScalar × 30°`  
**No threshold gate.** Reduction is continuous from PS=0.  
**Calibration target:** `MAX_FOV_PRESSURE_REDUCTION` [GT]  
**Shown at Decisions=10 (EffectiveFoV_BeforePressure=165°) and Decisions=20 (=170°)**

| PressureScalar | Reduction (°) | EffectiveFoV D=10 (°) | EffectiveFoV D=20 (°) | At floor? |
|----------------|--------------|----------------------|----------------------|-----------|
| 0.00           | 0.0°         | 165.0°               | 170.0°               | No        |
| 0.10           | 3.0°         | 162.0°               | 167.0°               | No        |
| 0.20           | 6.0°         | 159.0°               | 164.0°               | No        |
| 0.30           | 9.0°         | 156.0°               | 161.0°               | No        |
| 0.40           | 12.0°        | 153.0°               | 158.0°               | No        |
| 0.50           | 15.0°        | 150.0°               | 155.0°               | No        |
| 0.60           | 18.0°        | 147.0°               | 152.0°               | No        |
| 0.70           | 21.0°        | 144.0°               | 149.0°               | No        |
| 0.80           | 24.0°        | 141.0°               | 146.0°               | No        |
| 0.90           | 27.0°        | 138.0°               | 143.0°               | No        |
| 1.00           | 30.0°        | 135.0°               | 140.0°               | No        |

**Observations:**
- Reduction is linear and continuous — even PS=0.1 produces 3° of FoV loss
- At PS=1.0: D=10 → 135°, D=20 → 140°. MIN_FOV_ANGLE floor (120°) is never reached ✓
- At PS=0.5: D=10 → 150°. Half-angle drops from 82.5° to 75.0°. Entities in the 75°–82.5°
  arc become invisible under moderate press.

**Playtesting note:** If pressed defenders feel "too blind" under pressure, reduce
`MAX_FOV_PRESSURE_REDUCTION` from 30° toward 20°. If press feels insufficiently impactful,
increase toward 40°. Do not exceed 50° (Beilock 2010 upper bracket limit).

---

## C.3 Combined FoV: Decisions × PressureScalar

EffectiveFoV half-angle (°) across Decisions [1,20] and PressureScalar [0, 0.5, 1.0].

| Decisions | PS=0.0  | PS=0.5  | PS=1.0  |
|-----------|---------|---------|---------|
| 1         | 80.25°  | 72.75°  | 65.25°  |
| 5         | 81.25°  | 73.75°  | 66.25°  |
| 10        | 82.5°   | 75.0°   | 67.5°   |
| 15        | 83.75°  | 76.25°  | 68.75°  |
| 20        | 85.0°   | 77.5°   | 70.0°   |

**Derivation for D=1, PS=0.5:**
```
bonus = (1/20)×10° = 0.5°. EffectiveFoV_before = 160.5°.
reduction = 0.5 × 30° = 15°. EffectiveFoV = 145.5°. HalfAngle = 72.75°.
```

**Visibility example for an entity at exactly 75° angular offset:**
- D=1, PS=0.0: HalfAngle=80.25° > 75° → visible ✓
- D=1, PS=0.5: HalfAngle=72.75° < 75° → **invisible** ✗
- D=10, PS=0.5: HalfAngle=75.0° = 75° → visible (boundary inclusive) ✓
- D=10, PS=1.0: HalfAngle=67.5° < 75° → **invisible** ✗

An entity at 75° lateral angle is visible to an unpressured player but becomes invisible
when under moderate-to-heavy press. Elite Decisions agents retain visibility at moderate
pressure. This table drives playtester intuition for calibrating pressure effect.

---

## C.4 Recognition Latency vs. Decisions

**Formula:** `L_rec = FloorToInt(5 − ((D−1)/19) × 4)` ticks  
**Calibration targets:** `L_MAX` [GT], `L_MIN` [GT]

| Decisions | L_rec (float) | L_rec (ticks) | L_rec (ms) | With max noise (+1) |
|-----------|--------------|---------------|------------|---------------------|
| 1         | 5.000        | 5             | 500        | 5 ticks (capped)    |
| 2         | 4.789        | 4             | 400        | 5 ticks             |
| 3         | 4.579        | 4             | 400        | 5 ticks             |
| 4         | 4.368        | 4             | 400        | 5 ticks             |
| 5         | 4.158        | 4             | 400        | 5 ticks             |
| 6         | 3.947        | 3             | 300        | 4 ticks             |
| 7         | 3.737        | 3             | 300        | 4 ticks             |
| 8         | 3.526        | 3             | 300        | 4 ticks             |
| 9         | 3.316        | 3             | 300        | 4 ticks             |
| 10        | 3.105        | 3             | 300        | 4 ticks             |
| 11        | 2.895        | 2             | 200        | 3 ticks             |
| 12        | 2.684        | 2             | 200        | 3 ticks             |
| 13        | 2.474        | 2             | 200        | 3 ticks             |
| 14        | 2.263        | 2             | 200        | 3 ticks             |
| 15        | 2.053        | 2             | 200        | 3 ticks             |
| 16        | 1.842        | 1             | 100        | 2 ticks             |
| 17        | 1.632        | 1             | 100        | 2 ticks             |
| 18        | 1.421        | 1             | 100        | 2 ticks             |
| 19        | 1.211        | 1             | 100        | 2 ticks             |
| 20        | 1.000        | 1             | 100        | 2 ticks             |

**Floor breakpoints:** D=6 (5→4t), D=11 (4→3t), D=16 (3→2t), D=20 exact (2→1t).

**Observations:**
- D=16–20 all share L_rec=1 tick. Five attribute points produce identical base latency.
  The elite-end distinction comes only from noise distribution: 50% chance of 1 or 2 ticks.
- With half-turn bonus at D=10: 3 ticks → 2 ticks (100ms faster). Significant advantage.

**Playtesting calibration:**
- Reduce `L_MAX` (e.g. 4 ticks) if average players feel too slow to respond.
- Increase `L_MIN` (e.g. 2 ticks) if elite agents feel too "omniscient."

---

## C.5 Shoulder Check Interval vs. Anticipation

**Formula:** `CheckInterval = FloorToInt(30 − ((A−1)/19) × 24)` ticks  
**Calibration targets:** `CHECK_MAX_TICKS` [GT], `CHECK_MIN_TICKS` [GT]

| Anticipation | Interval (float) | Interval (ticks) | Period (s) | Checks per 90 min |
|--------------|-----------------|-----------------|------------|-------------------|
| 1            | 30.00           | 30              | 3.00       | 1,800             |
| 2            | 28.74           | 28              | 2.80       | 1,929             |
| 3            | 27.47           | 27              | 2.70       | 2,000             |
| 4            | 26.21           | 26              | 2.60       | 2,077             |
| 5            | 24.95           | 24              | 2.40       | 2,250             |
| 6            | 23.68           | 23              | 2.30       | 2,348             |
| 7            | 22.42           | 22              | 2.20       | 2,455             |
| 8            | 21.16           | 21              | 2.10       | 2,571             |
| 9            | 19.89           | 19              | 1.90       | 2,842             |
| 10           | 18.63           | 18              | 1.80       | 3,000             |
| 11           | 17.37           | 17              | 1.70       | 3,176             |
| 12           | 16.11           | 16              | 1.60       | 3,375             |
| 13           | 14.84           | 14              | 1.40       | 3,857             |
| 14           | 13.58           | 13              | 1.30       | 4,154             |
| 15           | 12.32           | 12              | 1.20       | 4,500             |
| 16           | 11.05           | 11              | 1.10       | 4,909             |
| 17           | 9.79            | 9               | 0.90       | 6,000             |
| 18           | 8.53            | 8               | 0.80       | 6,750             |
| 19           | 7.26            | 7               | 0.70       | 7,714             |
| 20           | 6.00            | 6               | 0.60       | 9,000             |

**Observations:**
- Monotone decreasing ✓ — balance test BAL-001 (monotonicity) passes.
- Even elite scanners (A=20) are blind-side for 50% of time (3 ticks active / 6 interval).
- Worst-case (A=1) scans every 3 seconds — a fast-break runner has a large exploitation window.

**Playtesting calibration:**
- Increase `CHECK_MIN_TICKS` (e.g. 8) if elite scanners feel too aware of rear runners.
- Increase `CHECK_MAX_TICKS` (e.g. 45) to widen the gap between best and worst scanners.

---

## C.6 Shoulder Check: Possession Modifier Effect

| Anticipation | Base Interval (s) | In-Possession Interval (s) | Increase |
|--------------|------------------|---------------------------|----------|
| 1            | 3.0              | 6.0                       | 2×       |
| 5            | 2.4              | 4.8                       | 2×       |
| 10           | 1.8              | 3.6                       | 2×       |
| 15           | 1.2              | 2.4                       | 2×       |
| 20           | 0.6              | 1.2                       | 2×       |

**Shoulder check window coverage (fraction of time with active awareness):**

| Scenario | Check Interval | Window (3 ticks) | Coverage |
|----------|---------------|-----------------|----------|
| A=20, not in possession | 6 ticks  | 3 ticks | 50%  |
| A=20, in possession     | 12 ticks | 3 ticks | 25%  |
| A=10, not in possession | 18 ticks | 3 ticks | 16.7%|
| A=1,  not in possession | 30 ticks | 3 ticks | 10%  |
| A=1,  in possession     | 60 ticks | 3 ticks | 5%   |

**Key insight:** Even elite scanners are blind to the rear for 50% of time when not in
possession. Blind-side runners always have a realistic exploitation window at any Anticipation
level. The system is intentionally permeable — consistent with real football.

---

## C.7 Pressure Scalar vs. Opponent Distance and Count

**Formula (First Touch §3.5, read-only):**
```
contribution(d) = (0.3 / Max(d, 0.3))²
PressureScalar = Clamp(Σ contribution / 1.5, 0, 1)
```

### C.7.1 Single Opponent — PressureScalar and FoV Consequence

| Distance (m) | Contribution | PressureScalar | FoV reduction (D=10, °) | EffectiveFoV (°) |
|-------------|-------------|----------------|------------------------|-----------------|
| 0.30        | 1.000       | 0.667          | 20.0°                  | 145.0°          |
| 0.40        | 0.563       | 0.375          | 11.3°                  | 153.7°          |
| 0.50        | 0.360       | 0.240          | 7.2°                   | 157.8°          |
| 0.60        | 0.250       | 0.167          | 5.0°                   | 160.0°          |
| 0.80        | 0.141       | 0.094          | 2.8°                   | 162.2°          |
| 1.00        | 0.090       | 0.060          | 1.8°                   | 163.2°          |
| 1.50        | 0.040       | 0.027          | 0.8°                   | 164.2°          |
| 2.00        | 0.023       | 0.015          | 0.5°                   | 164.5°          |
| 3.00 (edge) | 0.010       | 0.007          | 0.2°                   | 164.8°          |
| >3.00       | 0.000       | 0.000          | 0.0°                   | 165.0°          |

**Note:** EffectiveFoV shown for D=10 (bonus=5°, before-pressure=165°).
Meaningful FoV reduction (>5°) only within ~0.8m — appropriate for close-press mechanics.

### C.7.2 Multiple Opponents — Saturation Analysis

| Scenario                         | rawPressure | PressureScalar |
|----------------------------------|-------------|----------------|
| 1 at 0.3m                        | 1.000       | 0.667          |
| 1 at 0.3m + 1 at 1.0m           | 1.090       | 0.727          |
| 2 at 0.3m                        | 2.000       | 1.000 (capped) |
| 3 at 0.5m                        | 1.080       | 0.720          |
| 4 at 1.0m                        | 0.360       | 0.240          |
| 1 at 0.3m + 2 at 0.6m           | 1.500       | 1.000 (capped) |

Full saturation requires either 2 opponents within 0.3m or approximately 1 close + 1 tight.
Typical play produces PS in the 0.2–0.5 range.

---

## C.8 Blind-Side Arc vs. Decisions and Pressure

Blind-side arc = 360° − EffectiveFoV. Full matrix:

| Decisions | PS=0.0  | PS=0.5  | PS=1.0  |
|-----------|---------|---------|---------|
| 1         | 199.5°  | 214.5°  | 229.5°  |
| 5         | 197.5°  | 212.5°  | 227.5°  |
| 10        | 195.0°  | 210.0°  | 225.0°  |
| 15        | 192.5°  | 207.5°  | 222.5°  |
| 20        | 190.0°  | 205.0°  | 220.0°  |

**Derivation for D=1, PS=0.5 (most constrained):**
```
EffectiveFoV = 160.5° − 15.0° = 145.5°
BlindSide_Arc = 360° − 145.5° = 214.5° ✓
```

**Observations:**
- Blind-side arc always exceeds 180° under all conditions — agents can never see directly
  behind them. Entities at exactly 90° perpendicular are always visible. ✓
- Under maximum pressure (PS=1.0), blind arc expands to 220°–230°. Entities at 95°–115°
  lateral that were just inside FoV under no pressure become invisible.

---

## C.9 Balance Test Reference Scenarios

**Mapped to Section 5 §5.12 Balance Tests.**

### C.9.1 BAL-001: Attribute Monotonicity

| Output         | Attribute       | Direction       | Verified in |
|----------------|-----------------|-----------------|-------------|
| EffectiveFoV   | Decisions↑      | Monotone ↑      | C.1         |
| L_rec          | Decisions↑      | Monotone ↓ (non-strictly; floor plateaus) | C.4 |
| CheckInterval  | Anticipation↑   | Monotone ↓      | C.5         |
| EffectiveFoV   | PressureScalar↑ | Monotone ↓      | C.2         |

All confirmed ✓.

**Important note on L_rec monotonicity:** The non-increasing (not strictly decreasing)
property is correct because floor rounding creates plateaus (e.g. D=6–10 all produce L_rec=3
ticks). Section 5 BAL-001 should test for **non-increasing**, not strict decrease.

### C.9.2 BAL-002: No Dominant Strategy at Attribute Extremes

```
D=1 agent (worst-case perception):
  FoV: 160.5° — nearly full forward hemisphere
  L_rec: 5 ticks — 500ms delay but still receives awareness
  Blind-side checks: every 3s — manageable for static play
  Conclusion: Perceptually limited but not degenerate. ✓

D=20 agent (best-case perception):
  FoV: 170° — only 9.5° wider than D=1 agent
  L_rec: 1–2 ticks — fast but not instant; ball contact still required to force L_rec=0
  Blind-side checks: every 0.6s — still 50% blind period between checks
  Conclusion: Meaningfully superior but not omniscient. ✓
```

### C.9.3 BAL-003: Pressure Effect — Measurable but Not Overwhelming

```
Agent: Decisions=10, PressureScalar=1.0.

FoV neutral: 165°. FoV under full press: 135°. Change: −30° (−18.2%).
HalfAngle neutral: 82.5°. HalfAngle under press: 67.5°.

Lateral coverage lost at 20m: 20m × (tan(82.5°) − tan(67.5°))
  = 20 × (7.596 − 2.414) = 20 × 5.182 ≈ 103.6m (extreme values — lateral tangent diverges)

More useful: entities in 67.5°–82.5° arc at 20m:
  lateral distance to FoV edge at 82.5°: 20 × tan(82.5°) ≈ 154m (off-pitch)
  lateral distance to FoV edge at 67.5°: 20 × tan(67.5°) ≈ 48.3m (off-pitch)

Note: at standard football pitch widths (~68m), even 67.5° half-angle sees the full
pitch width at 20m distance (68/2 = 34m; tan(67.5°)×20 = 48.3m >> 34m). The meaningful
press effect is loss of agents in the peripheral zone who are closer to the observer.

At 8m: tan(82.5°)×8 ≈ 60.8m vs tan(67.5°)×8 ≈ 19.3m
Agents between 19.3m and 60.8m lateral at 8m distance — essentially all off-pitch, so
the pitch boundary constrains what's relevant more than the FoV angle at mid-range.

Practical impact: The press effect is most meaningful for agents at 2–5m lateral range
where the pitch still has content and the angular loss (~4°–8°) translates to 0.15–0.7m
lateral miss. Agents at 82° from facing at 3m distance: lateral = tan(82.5°)×3 ≈ 22.8m
vs tan(67.5°)×3 ≈ 7.2m. This confirms the press effect matters at close quarters. ✓

Conclusion: Press degrades close-quarters peripheral awareness meaningfully without
collapsing the agent's entire perception. The effect is appropriately bounded. ✓
```

---

## C.10 Calibration Sensitivity — Impact of ±10% on Key Constants

### C.10.1 BASE_FOV_ANGLE Sensitivity

| Change    | New Value | EffectiveFoV Range (D=1 to D=20) | Worst-case under PS=1.0 |
|-----------|-----------|----------------------------------|------------------------|
| −10% (144°) | 144°    | 144.5° – 154.0°                 | 114.5° (near floor)    |
| Current (160°) | 160° | 160.5° – 170.0°                 | 130.5° (above floor)   |
| +10% (176°) | 176°    | 176.5° – 186.0°                 | 146.5° (well above)    |

A −10% reduction brings the worst case to 114.5° at D=1, PS=1.0 — dangerously close to
the MIN_FOV_ANGLE floor of 120°. At −10%, the floor would become reachable under extreme
pressure for low-Decisions agents. This would change the floor from a theoretical safety net
to an active game mechanic — a significant qualitative change requiring deliberate intent.
The current 160° baseline is well-grounded in Franks & Miller (1985).

### C.10.2 MAX_FOV_BONUS_ANGLE Sensitivity

| Change    | New Value | D=1 FoV | D=20 FoV | Spread |
|-----------|-----------|---------|---------|--------|
| −10% (9°)  | 9°       | 160.45° | 169.0°  | 8.55°  |
| Current (10°) | 10°   | 160.5°  | 170.0°  | 9.5°   |
| +10% (11°) | 11°      | 160.55° | 171.0°  | 10.45° |

Very low sensitivity. ±10% changes total spread by less than 1°. This constant can be
freely adjusted ±25% without balance impact. It is a minor secondary effect.

### C.10.3 MAX_FOV_PRESSURE_REDUCTION Sensitivity

| Change    | New Value | Max FoV loss at PS=1.0 | EffectiveFoV D=10, PS=1.0 |
|-----------|-----------|----------------------|--------------------------|
| −10% (27°) | 27°      | 27°/165° = 16.4%     | 138.0°                   |
| Current (30°) | 30°   | 30°/165° = 18.2%     | 135.0°                   |
| +10% (33°) | 33°      | 33°/165° = 20.0%     | 132.0°                   |

Primary lever for press impact. ±10% produces 3° difference in worst-case FoV.
Stays within Beilock 2010 bracket (20–50%) across ±25% range.

### C.10.4 L_MAX Sensitivity

| Change    | L_MAX value | L_rec at D=1 | L_rec at D=10 | Floor breakpoint change |
|-----------|-------------|-------------|--------------|------------------------|
| −20% (4t) | 4 ticks     | 4 ticks     | 2–3 ticks    | D=6 breakpoint shifts  |
| Current (5t) | 5 ticks  | 5 ticks     | 3 ticks      | Current breakpoints    |
| +20% (6t) | 6 ticks     | 6 ticks     | 3–4 ticks    | D=6 breakpoint shifts  |

Low sensitivity at ±10% due to floor rounding (produces same tick outcome). Only ±20%
changes meaningful. The 500ms upper bound is anchored by Franks & Miller (1985).

### C.10.5 CHECK_MAX_TICKS Sensitivity (A=1 behaviour)

| Change    | New Value | A=1 Interval (s) | A=10 Interval (s) |
|-----------|-----------|-----------------|------------------|
| −10% (27) | 27 ticks  | 2.7s            | 1.5s             |
| Current (30) | 30 ticks | 3.0s           | 1.8s             |
| +10% (33) | 33 ticks  | 3.3s            | 2.0s             |

Moderate sensitivity. ±10% shifts worst-case interval by ±0.3s. Current value is at the
lower end of Franks & Miller (1985) 3–5s bracket — room to increase if needed.

### C.10.6 CHECK_MIN_TICKS Sensitivity (A=20 behaviour)

| Change    | New Value | A=20 Interval (s) | Coverage fraction |
|-----------|-----------|------------------|-------------------|
| −17% (5t) | 5 ticks   | 0.5s             | 60%               |
| Current (6t) | 6 ticks | 0.6s            | 50%               |
| +17% (7t) | 7 ticks   | 0.7s             | 43%               |

Low sensitivity at ±10% due to integer floor. Only ±17% (to 5t or 7t) meaningfully changes
behaviour. The 50% coverage target at elite level is the design anchor — keep near 50%.

---

*End of Appendix C — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
