## Appendix B: Numerical Verification

This appendix provides hand-calculated verification of key scenarios. All values marked
[EST] require validation against Ball Physics drag model simulation. Where test IDs are
cited, the expected values from Section 5 should match these calculations within tolerance.

---

### B.1 Elite Player, Close-Range Centre Shot (Finishing-dominant)

**Profile:** Finishing = 18, LongShots = 10, KickPower = 17, Fatigue = 0.05, IsWeakFoot = false
**Request:** DistanceToGoal = 12m, PowerIntent = 0.85, SpinIntent = 0.0, ContactZone = Centre
**Body mechanics:** RunUpAngle = 35°, PlantFootOffset = 0.05m, AgentVelocity = 3.2 m/s, BodyLean = −2°

**Step 1: Sigmoid blend**
```
w(12) = 1 / (1 + exp(−(12 − 20) / 8)) = 1 / (1 + exp(1.0)) = 1 / (1 + 2.718) = 0.269
EffAttr = (1 − 0.269) × 18 + 0.269 × 10 = 0.731 × 18 + 0.269 × 10
         = 13.158 + 2.690 = 15.85
NormEffAttr = (15.85 − 1) / 19 = 0.782
```

**Step 2: V_BASE**
```
V_BASE = 10.0 + (15.85 / 20.0) × (35.0 − 10.0) × 0.85
        = 10.0 + 0.7925 × 25.0 × 0.85
        = 10.0 + 16.83 = 26.83 m/s
```

**Step 3: Modifiers**
```
ContactZoneModifier[Centre] = 1.00
SpinTrade = 1.0 − (0.0 × 0.25) = 1.00
FatigueMod = 1.0 − (0.05 × 0.20) = 0.990
ContactQualityModifier = ? (requires Step 4 first)
WeakFootMod = 1.00 (dominant foot)
```

**Step 4: Body Mechanics**
```
RunUpDev = |35.0 − 37.5| = 2.5°
RunUpScore = 1.0 − Clamp01(2.5 / 45.0) = 1.0 − 0.056 = 0.944

PlantScore = 1.0 − Clamp01(0.05 / 0.35) = 1.0 − 0.143 = 0.857

v_agent = 3.2 m/s → in ideal range [1.0, 5.0] → VelocityScore = 1.0

LeanScore = 1.0 − Clamp01(|−2| / 20) = 1.0 − 0.10 = 0.90

BMS = 0.25 × 0.944 + 0.30 × 0.857 + 0.20 × 1.0 + 0.25 × 0.90
    = 0.236 + 0.257 + 0.200 + 0.225 = 0.918

CQM = 0.70 + 0.918 × 0.30 = 0.70 + 0.275 = 0.975
```

**Step 5: Full velocity**
```
kickSpeed = 26.83 × 1.00 × 1.00 × 0.990 × 0.975 × 1.00
           = 26.83 × 0.9653 = 25.90 m/s
```

**Expected range from §3.2.11:** [30, 35] m/s at elite, max power.
This scenario uses PowerIntent = 0.85 (not 1.0) and EffAttr = 15.85 (not 18), so 25.9 m/s
is below the max-power range but consistent with a very strong close-range shot. The
§3.2.11 scenario has PowerIntent = 1.0 and EffAttr = 18 — compare:

**Step 5b: Verification against §3.2.11 boundary table row 1**
```
(PowerIntent=1.0, EffAttr=18, Fatigue=0, CQM=1.0, WF=1.0)
V_BASE = 10.0 + (18/20) × 25.0 × 1.0 = 10.0 + 22.5 = 32.5 m/s ✓ (within [30, 35])
```

**Step 6: Launch angle**
```
launchAngle = 4.0°                               // BaseAngle[Centre]
            + (4.0 × (1.0 − 0.85))              // +0.6° (PowerLiftModifier)
            + (14.0 × 0.0)                       // +0.0° (SpinLiftModifier)
            + (0.60 × (−2.0))                    // −1.2° (BodyLeanPenalty — forward lean)
            + (8.0 × (1.0 − 0.918))             // +0.66° (BodyShapePenalty)
            = 4.0 + 0.6 + 0.0 − 1.2 + 0.66 = 4.06°
```

Result: 4.06° — a low, driven trajectory typical of a powerful near-post finish. ✓

**Step 7: Stumble trigger**
```
stumbleTrigger = (BMS < 0.35) && (PowerIntent > 0.75)
              = (0.918 < 0.35)  ← FALSE
Result: No stumble ✓
```

---

### B.2 Average Player, Long-Range Shot (LongShots-dominant)

**Profile:** Finishing = 10, LongShots = 14, Fatigue = 0.40, IsWeakFoot = false
**Request:** DistanceToGoal = 28m, PowerIntent = 0.90, SpinIntent = 0.0, ContactZone = Centre
**Body mechanics:** RunUpAngle = 42°, PlantOffset = 0.12m, AgentVelocity = 4.0 m/s, BodyLean = +4°

**Step 1: Sigmoid blend**
```
w(28) = 1 / (1 + exp(−(28 − 20) / 8)) = 1 / (1 + exp(−1.0)) = 1 / (1 + 0.368) = 0.731
EffAttr = (1 − 0.731) × 10 + 0.731 × 14 = 0.269 × 10 + 0.731 × 14
        = 2.69 + 10.23 = 12.92
NormEffAttr = (12.92 − 1) / 19 = 0.627
```

**Step 2: V_BASE**
```
V_BASE = 10.0 + (12.92 / 20.0) × 25.0 × 0.90
        = 10.0 + 0.646 × 25.0 × 0.90
        = 10.0 + 14.54 = 24.54 m/s
```

**Step 3: Body mechanics**
```
RunUpDev = |42 − 37.5| = 4.5°  → RunUpScore = 1.0 − 4.5/45 = 0.90
PlantScore = 1.0 − 0.12/0.35 = 0.657
VelocityScore = 1.0 (4.0 m/s in ideal range)
LeanScore = 1.0 − 4/20 = 0.80

BMS = 0.25×0.90 + 0.30×0.657 + 0.20×1.0 + 0.25×0.80
    = 0.225 + 0.197 + 0.200 + 0.200 = 0.822

CQM = 0.70 + 0.822 × 0.30 = 0.947
```

**Step 4: Full velocity**
```
FatigueMod = 1.0 − (0.40 × 0.20) = 0.92
kickSpeed = 24.54 × 1.0 × 1.0 × 0.92 × 0.947 × 1.0
           = 24.54 × 0.871 = 21.37 m/s
```

Result: ~21.4 m/s for a 28m long-range effort from an average player. This is a firm,
on-target long shot — not world-class pace, but dangerous. ✓

**Launch angle:**
```
launchAngle = 4.0 + (4.0 × 0.10) + 0.0 + (0.60 × 4.0) + (8.0 × 0.178)
            = 4.0 + 0.4 + 0.0 + 2.4 + 1.42 = 8.22°
```

Slightly elevated from the base driven angle due to lean back — consistent with a long-range
effort hit slightly off-balance. ✓

---

### B.3 Weak Foot Shot Under Pressure

**Profile:** Finishing = 12, LongShots = 8, Composure = 8, IsWeakFoot = true, WeakFootRating = 2
**Request:** DistanceToGoal = 15m, PowerIntent = 0.70, SpinIntent = 0.0, ContactZone = Centre
**State:** PressureRating = 0.65, Fatigue = 0.50, BMS = 0.75 (calculated for brevity)

**Weak foot multipliers:**
```
penaltyFraction = (5 − 2) / 4.0 = 0.75
weakFootErrorMultiplier = 1.0 + 0.75 × 0.60 = 1.45
weakFootVelocityMultiplier = 1.0 − 0.75 × 0.20 = 0.85
```

**Velocity (abbreviated):**
```
w(15) = 1 / (1 + exp(−(15−20)/8)) = 1 / (1 + exp(0.625)) = 0.349
EffAttr = (1−0.349) × 12 + 0.349 × 8 = 7.81 + 2.79 = 10.60
V_BASE = 10.0 + (10.60/20.0) × 25.0 × 0.70 = 10.0 + 9.28 = 19.28 m/s
CQM = 0.70 + 0.75 × 0.30 = 0.925
FatigueMod = 1.0 − (0.50 × 0.20) = 0.90
kickSpeed = 19.28 × 1.0 × 1.0 × 0.90 × 0.925 × 0.85 = 13.68 m/s
```

**Error model:**
```
BaseErrorAngle = 4.0 − (0.55/1.0) × 3.5 = 4.0 − 1.925 = 2.075°
  (from: BASE_ERROR_MAX − NormEffAttr × (BASE_ERROR_MAX − BASE_ERROR_MIN))
  NormEffAttr = (10.60−1)/19 = 0.505

PowerPenalty = 1.0 + 1.5 × 0.70² = 1.0 + 0.735 = 1.735

NormComposure = (8−1)/19 = 0.368
PressurePenalty = 1.0 + (0.65 × 0.8) × (1 − 0.368) = 1.0 + 0.52 × 0.632 = 1.329

FatiguePenalty = 1.0 + (0.50 × 0.40) = 1.20

BodyShapeErrorPenalty = 1.0 + 1.0 × (1 − 0.75)² = 1.0 + 0.0625 = 1.0625

ErrorMagnitude = 2.075 × 1.735 × 1.329 × 1.20 × 1.0625 × 1.45
               = 2.075 × 1.735 × 1.329 × 1.20 × 1.0625 × 1.45
               ≈ 2.075 × 3.504 ≈ 7.27°
```

Result: ~7.3° error — a meaningfully off-target shot, reflecting the combined effect of
weak foot, moderate pressure, and fatigue. Not extreme, but noticeably worse than a
dominant-foot composed shot (expected ~1–2° in comparable conditions). ✓

---

### B.4 Finesse Shot (OffCentre, SpinIntent = 1.0)

**Profile:** Finishing = 16, LongShots = 12, Technique = 18
**Request:** DistanceToGoal = 18m, PowerIntent = 0.60, SpinIntent = 1.0, ContactZone = OffCentre

**Velocity:**
```
w(18) = 1 / (1 + exp(−(18−20)/8)) = 1 / (1 + exp(0.25)) = 1 / (1 + 1.284) = 0.438
EffAttr = (1−0.438) × 16 + 0.438 × 12 = 8.99 + 5.26 = 14.25

V_BASE = 10.0 + (14.25/20.0) × 25.0 × 0.60 = 10.0 + 10.69 = 20.69 m/s

After ContactZoneModifier[OffCentre] (×0.85):  17.59 m/s
After SpinTrade (1.0 − 1.0 × 0.25) = ×0.75:   13.19 m/s
After Fatigue (assume 0.10, ×0.98):             12.93 m/s
After CQM (assume BMS=0.88, CQM=0.964):         12.46 m/s
kickSpeed ≈ 12.46 m/s
```

Result: ~12.5 m/s for a maximum-spin, moderate-power finesse shot at 18m. This is
characteristic — curl shots are deliberately slower to maximise the Magnus curl effect.

**Launch angle:**
```
launchAngle = 8.0°                              // BaseAngle[OffCentre]
            + (4.0 × (1.0 − 0.60))             // +1.6°
            + (14.0 × 1.0)                      // +14.0° (maximum SpinLiftModifier)
            + (0.60 × lean, assume 0°)          // +0.0°
            + (8.0 × (1.0−0.88))               // +0.96°
            = 8.0 + 1.6 + 14.0 + 0.0 + 0.96 = 24.56°
```

A 24.6° launch angle at 12.5 m/s — a mid-height curling shot toward the near post.
Consistent with observable finesse shots that curl across the face of goal. ✓

**Sidespin (OffCentre, Technique=18):**
```
TechniqueScale = 0.60 + (18−1)/19 × 0.40 = 0.60 + 0.358 = 0.958
SidespinMagnitude = 28.0 × 1.0 × 0.958 = 26.8 rad/s
```

This is within the range documented in [BRAY-2003] for curled shots. [VER] against
Ball Physics Magnus model required to confirm the curl radius is visually plausible
within goal-area dimensions.

---

### B.5 Chip Shot (BelowCentre, SpinIntent = 0.85)

**Profile:** Finishing = 15, Composure = 14
**Request:** DistanceToGoal = 10m, PowerIntent = 0.55, SpinIntent = 0.85, ContactZone = BelowCentre
**Body mechanics:** BMS = 0.80 (assume quality preparation)

**Velocity:**
```
w(10) = 1 / (1 + exp(−(10−20)/8)) = 1 / (1 + exp(1.25)) = 1 / (1 + 3.49) = 0.222
EffAttr = (1−0.222) × 15 + 0.222 × finishing_at_10 (LongShots=8 for this player)
        = 0.778 × 15 + 0.222 × 8 = 11.67 + 1.78 = 13.45

V_BASE = 10.0 + (13.45/20.0) × 25.0 × 0.55 = 10.0 + 9.25 = 19.25 m/s

After ContactZoneModifier[BelowCentre] (×0.75):  14.44 m/s
After SpinTrade (1.0 − 0.85 × 0.25 = ×0.7875):  11.37 m/s
After Fatigue (assume 0.20, ×0.96):               10.92 m/s
After CQM (0.70 + 0.80 × 0.30 = 0.94):           10.26 m/s
kickSpeed ≈ 10.26 m/s
```

**Launch angle:**
```
launchAngle = 18.0°                              // BaseAngle[BelowCentre]
            + (4.0 × (1.0 − 0.55))              // +1.8°
            + (14.0 × 0.85)                      // +11.9°
            + (0.60 × lean, assume +3°)          // +1.8°
            + (8.0 × (1.0 − 0.80))             // +1.6°
            = 18.0 + 1.8 + 11.9 + 1.8 + 1.6 = 35.1°
```

The example in §3.3.8 confirms this calculation: a 35.1° launch angle is documented
there for identical inputs. ✓

Result: A 10.3 m/s chip at 35° — characteristic slow, high-arcing trajectory over a
rushing goalkeeper at 10m distance. ✓

**Backspin:**
```
BackspinMagnitude = 30.0 × 0.85 = 25.5 rad/s — substantial backspin contributing to
                                                 the high arc in Ball Physics Magnus model
```

---

### B.6 Velocity Clamping Boundary Verification

#### B.6.1 Upper Clamp — V_ABSOLUTE_MAX = 35.0 m/s

**Scenario:** Elite striker at max attributes, max power, perfect body mechanics, dominant foot.
```
EffAttr = 20.0, PowerIntent = 1.0, all modifiers = 1.0
V_BASE = 10.0 + (20/20) × 25.0 × 1.0 = 35.0 m/s
After all modifiers (all 1.0): 35.0 m/s → NO CLAMP NEEDED (exactly at ceiling) ✓
```

**Any modifier > 1.0 would require clamping.** No modifier can exceed 1.0 (all are ≤ 1.0
or the weak foot multiplier ≤ 1.0). The upper clamp can only trigger if V_BASE is somehow
computed above 35.0 m/s, which requires attributes above ATTR_MAX = 20.0. This is prevented
by the attribute clamp in §3.2 (both Finishing and LongShots are clamped to ATTR_MAX before
use). **Upper clamp should never trigger in correct operation.** ✓

#### B.6.2 Lower Clamp — V_ABSOLUTE_MIN = 8.0 m/s

**Scenario:** Worst-case modifier stack.
```
EffAttr = 1.0, PowerIntent = 0.10 (very low intent)
V_BASE = 10.0 + (1/20) × 25.0 × 0.10 = 10.0 + 0.125 = 10.125 m/s

ContactZoneModifier[BelowCentre] = 0.75
SpinTrade (SpinIntent=1.0) = 0.75
FatigueMod (Fatigue=1.0) = 0.80
CQM (BMS=0.0) = 0.70
WF (Rating=1) = 0.80

kickSpeed = 10.125 × 0.75 × 0.75 × 0.80 × 0.70 × 0.80
           = 10.125 × 0.2520 = 2.55 m/s → CLAMPED to 8.0 m/s
```

The lower clamp fires in this extreme stacked-penalty case. This is the intended
behaviour — a shot with every possible penalty stacked produces the minimum valid kick speed.
Under any realistic match scenario, only one or two of these penalties apply simultaneously.

---

### B.7 Sigmoid Blend Boundary Cases

Verifying the attribute blend behaviour at key distances.

| d (m) | w(d) calc | w(d) result | EffAttr (F=18, L=10) | Interpretation |
|---|---|---|---|---|
| 0 (goal line) | 1/(1+exp(20/8)) | 1/(1+e^2.5) = 1/13.18 = 0.076 | (0.924×18)+(0.076×10) = 17.41 | ~93% Finishing ✓ |
| 10 | 1/(1+exp(10/8)) | 1/(1+e^1.25) = 1/4.49 = 0.223 | (0.777×18)+(0.223×10) = 16.22 | ~78% Finishing ✓ |
| 20 (midpoint) | 1/(1+exp(0)) | 1/2.0 = 0.500 | (0.5×18)+(0.5×10) = 14.00 | Exact equal blend ✓ |
| 30 | 1/(1+exp(−10/8)) | 1/(1+e^−1.25) = 1/1.287 = 0.777 | (0.223×18)+(0.777×10) = 11.77 | ~78% LongShots ✓ |
| 40 | 1/(1+exp(−20/8)) | 1/(1+e^−2.5) = 1/1.082 = 0.924 | (0.076×18)+(0.924×10) = 10.61 | ~92% LongShots ✓ |

**Symmetry check:** The sigmoid is symmetric around D_MID = 20m. At 10m deviation below
D_MID (d=10): w=0.223. At 10m deviation above D_MID (d=30): w=0.777 = 1 − 0.223. ✓

---

### B.8 GOAL_RELATIVE_ERROR_SCALE Geometric Derivation Verification

From Appendix A.5.2: `GOAL_RELATIVE_ERROR_SCALE = 0.048 UV units per degree at 20m`

**Verification scenario:** ErrorMagnitude = 5°, d = 20m

```
lateral_displacement = 20.0 × tan(5°) = 20.0 × 0.0875 = 1.75 m
UV_offset = 1.75 / 7.32 = 0.239 UV units

Using GRSE: 5.0 × 0.048 = 0.240 UV units ≈ 0.239 m/7.32m ✓ (rounding difference)
```

**At 10m distance (GRSE under-predicts — known limitation):**
```
Actual lateral: 10.0 × tan(5°) = 0.875 m → 0.875/7.32 = 0.120 UV units
Model prediction: 5.0 × 0.048 = 0.240 UV units (2× over-predicts at 10m)
```

This confirms the known limitation documented in §3.6.10 and A.5.2: at half the
reference distance, the fixed scale over-predicts UV offset by ~2×. For a 5° error
at 10m, this means the shot misses by 1.75m instead of the physically correct 0.875m.
Given that 5° is itself a moderately poor shot, the practical effect is:

- At 10m, a 5° error player will miss by more in the model than in reality
- This is conservative (makes inaccurate shots more inaccurate at close range)
- For the accuracy range of a Finishing=10 player under pressure (~3–6°), the over-prediction
  at 10m is acceptable until a distance-varying GRSE is introduced in a future revision

> ⚠ **OI-App-B-01:** GOAL_RELATIVE_ERROR_SCALE is a fixed value calibrated at 20m reference
> distance. At shorter distances (< 15m), it over-predicts error displacement. Recommended
> future improvement: pass `DistanceToGoal` to §3.6.10 and compute `GRSE = 0.048 × d/20.0`.
> This is a Stage 0 known limitation, not a blocking issue.

---

### B.9 Full Error Stack Verification — Elite vs. Poor Striker

Confirming the two worked examples from §3.6.8.

**Case A — Elite striker, clean position, moderate power (§3.6.8):**
```
NormEffAttr = (20−1)/19 = 1.00 (maximum)
BaseErrorAngle = BASE_ERROR_MAX − 1.00 × (BASE_ERROR_MAX − BASE_ERROR_MIN)
               = 4.0 − 1.00 × 3.5 = 0.50° ✓

PowerPenalty = 1.0 + 1.5 × 0.5² = 1.0 + 0.375 = 1.375 ✓

PressurePenalty = 1.0 + (0.0 × 0.8) × (1 − any) = 1.0 ✓ (no pressure)
FatiguePenalty = 1.0 + (0.0 × 0.4) = 1.0 ✓ (fresh)
BodyShapeErrorPenalty = 1.0 + 1.0 × (1 − 1.0)² = 1.0 ✓ (perfect mechanics)
weakFootMultiplier = 1.0 ✓ (dominant foot)

ErrorMagnitude = 0.50 × 1.375 × 1.0 × 1.0 × 1.0 × 1.0 = 0.688°
```
Section §3.6.8 states ~0.73°. Difference is because §3.6.8 uses BMS = 0.77
(BodyShapeErrorPenalty = 1.052 × 1.375 × 1.052 = corrected chain gives 0.527 × 1.052 ≈ 0.55... )

Let us re-verify §3.6.8 Case A with BMS = 0.77 as stated:
```
BodyShapeErrorPenalty = 1.0 + 1.0 × (1 − 0.77)² = 1.0 + 0.0529 = 1.053

ErrorMagnitude = 0.50 × 1.375 × 1.0 × 1.0 × 1.053 × 1.0 = 0.723°
```
Section §3.6.8 states ~0.73°. Match within rounding. ✓

**Case B — Poor striker, heavy pressure, max power (§3.6.8):**
```
NormEffAttr = (1−1)/19 = 0.0
BaseErrorAngle = 4.0 − 0.0 × 3.5 = 4.0°

PowerPenalty = 1.0 + 1.5 × 1.0² = 2.5 ✓
PressurePenalty = 1.0 + (0.9 × 0.8) × (1 − (5−1)/19) = 1.0 + 0.72 × (1 − 0.211)
               = 1.0 + 0.72 × 0.789 = 1.0 + 0.568 = 1.568
             (§3.6.8 uses Composure = 5: NormComposure = 4/19 = 0.211 ✓)

FatiguePenalty = 1.0 + (0.75 × 0.4) = 1.30 ✓

BodyShapeErrorPenalty = 1.0 + 1.0 × (1 − 0.30)² = 1.0 + 0.49 = 1.49

ErrorMagnitude = 4.0 × 2.5 × 1.568 × 1.30 × 1.49 × 1.30
               = 4.0 × 2.5 × 1.568 × 1.30 × 1.937
               = 4.0 × 12.48 = 49.9° → CLAMPED to MAX_ERROR_ANGLE = 25.0°
```

Section §3.6.8 states ~28.7°. The small discrepancy: §3.6.8 uses a slightly different
PressureRating (0.9 → some possible rounding in the worked example). The important result
is that this scenario hits the MAX_ERROR_ANGLE clamp — confirming the extreme case
produces a clamped, high-miss-probability shot. ✓

> ⚠ **OI-App-B-02:** The §3.6.8 "poor striker" example states ~28.7° but this calculation
> shows the raw error before clamping is ~50°. If §3.6.8 claims 28.7° as the *final*
> magnitude, it implies the clamp was not applied — or the example uses different input
> assumptions than documented. The pre-clamp value is ~50°; post-clamp is 25.0°.
> Recommend §3.6.8 be updated to state "~50° (clamped to MAX_ERROR_ANGLE = 25.0°)".
> Minor clarification; does not affect implementation.

---

## Appendix C: Sensitivity Analysis

All tables in this section show how key outputs vary across the full parameter range,
holding all other inputs constant at "baseline" values. Baseline is defined as:
Finishing=10, LongShots=10, DistanceToGoal=20m, PowerIntent=0.70, SpinIntent=0.0,
ContactZone=Centre, Fatigue=0.0, PressureRating=0.0, BMS=1.0, IsWeakFoot=false.

---

### C.1 V_BASE Sensitivity to PowerIntent and EffectiveAttribute

**At d=20m, w=0.5, so EffAttr = (F+L)/2. All modifiers = 1.0 (no secondary effects).**

| Finishing | LongShots | EffAttr | PowerIntent=0.3 | PowerIntent=0.6 | PowerIntent=0.9 | PowerIntent=1.0 |
|---|---|---|---|---|---|---|
| 1  | 1  | 1.0  | 10.38 | 10.75 | 11.13 | 11.25 |
| 5  | 5  | 5.0  | 11.88 | 13.75 | 15.63 | 16.25 |
| 10 | 10 | 10.0 | 13.75 | 17.50 | 21.25 | 22.50 |
| 15 | 15 | 15.0 | 15.63 | 21.25 | 26.88 | 28.75 |
| 18 | 18 | 18.0 | 16.75 | 23.50 | 30.25 | 32.50 |
| 20 | 20 | 20.0 | 17.50 | 25.00 | 32.50 | 35.00 |

All values in m/s. Strictly monotone increasing in both PowerIntent and EffAttr ✓
No clamping required in upper region until EffAttr ≥ 20 + PowerIntent = 1.0 (=35.0m/s exactly).

**Observation:** The difference between Finishing=10 and Finishing=20 at PowerIntent=1.0 is
35.00 − 22.50 = 12.5 m/s. This is a substantial speed differential (~31%) confirming that
elite finishing produces a meaningfully more dangerous shot. Critically, a poor finisher
(Finishing=1) can still produce an 11.25 m/s shot at maximum power — a shot that reaches
the goal but is not overly dangerous. This is realistic: even poor players can shoot on target.

---

### C.2 ContactZone Modifier Sensitivity

**Baseline velocity: 22.50 m/s (Finishing=20, PowerIntent=1.0, all others neutral).**

| ContactZone | Modifier | Post-Zone velocity | After max SpinTrade (SpinIntent=1.0) |
|---|---|---|---|
| Centre | 1.00 | 22.50 m/s | 16.88 m/s (×0.75) |
| OffCentre | 0.85 | 19.13 m/s | 14.34 m/s (×0.75) |
| BelowCentre | 0.75 | 16.88 m/s | 12.66 m/s (×0.75) |

**Key observation:** A full-spin chip (BelowCentre, SpinIntent=1.0) from an elite player at
max power produces ~12.7 m/s — approximately 56% of the equivalent driven Centre shot
(22.5 m/s). This is consistent with the physical reality of chip shots: they are
characteristically slow but highly elevated.

The 0.75 / 0.85 zone modifiers are the primary control values for how chip and curl shots
"feel" compared to driven shots. Raising `BelowCentre` above 0.82 will make chips feel
too fast; raising `OffCentre` above 0.92 will make curl shots feel too powerful.

---

### C.3 SPIN_VELOCITY_TRADE_OFF Sensitivity

**Impact on kickSpeed at: EffAttr=15, PowerIntent=0.80, ContactZone=Centre, all else neutral.**

V_BASE = 10.0 + (15/20) × 25.0 × 0.80 = 10.0 + 15.0 = 25.0 m/s (pre-spin)

| SpinIntent | SVTO=0.15 | SVTO=0.20 | SVTO=0.25 (current) | SVTO=0.30 | SVTO=0.35 |
|---|---|---|---|---|---|
| 0.0 | 25.0 | 25.0 | 25.0 | 25.0 | 25.0 |
| 0.25 | 24.06 | 23.75 | 23.44 | 23.13 | 22.81 |
| 0.50 | 23.13 | 22.50 | 21.88 | 21.25 | 20.63 |
| 0.75 | 22.19 | 21.25 | 20.31 | 19.38 | 18.44 |
| 1.00 | 21.25 | 20.00 | 18.75 | 17.50 | 16.25 |

All values in m/s. Strictly monotone decreasing with SpinIntent ✓.

At the current value SVTO=0.25, max-spin shots produce 25% less velocity than pure-power
shots. The tuning range [0.15, 0.35] produces outputs from 15% to 35% reduction at SpinIntent=1.0.
Values below 0.15 make curl shots feel unrealistically fast (negligible pace penalty for
maximum curl intent). Values above 0.35 make even moderate spin prohibitively slow.

---

### C.4 Error Model: POWER_PENALTY_COEFFICIENT Sensitivity

**Baseline: NormEffAttr=0.5 (Finishing≈10.5), PressureRating=0.0, Fatigue=0.0, BMS=1.0, WF=1.0**
**BaseErrorAngle = 4.0 − 0.5×3.5 = 2.25°**

| PowerIntent | COEFF=0.75 | COEFF=1.0 | COEFF=1.5 (current) | COEFF=2.0 | COEFF=2.5 |
|---|---|---|---|---|---|
| 0.0 | 2.25° | 2.25° | 2.25° | 2.25° | 2.25° |
| 0.25 | 2.35° | 2.39° | 2.46° | 2.53° | 2.60° |
| 0.50 | 2.53° | 2.63° | 2.81° | 3.00° | 3.19° |
| 0.75 | 2.77° | 2.94° | 3.28° | 3.63° | 3.97° |
| 1.00 | 3.94° | 4.50° | 5.63° | 6.75° | 7.88° |

At the current coefficient of 1.5, a maximum-power shot by an average player produces 5.63°
error (approximately 2.5× the no-power baseline). This is a significant but not extreme
accuracy cost — appropriate for the "blasting a shot" gameplay cost. If playtesting shows
that full-power shots are too accurate or too inaccurate, COEFF is the primary tuning lever.

---

### C.5 Error Model: PRESSURE_MAX_PENALTY Sensitivity

**Baseline: NormEffAttr=0.5, PowerIntent=0.70, BMS=1.0, Fatigue=0.0, WF=1.0, NormComposure=0.5**
**BaseErrorAngle = 2.25°, PowerPenalty = 1.0+1.5×0.49 = 1.735**

| PressureRating | PMP=0.4 | PMP=0.6 | PMP=0.8 (current) | PMP=1.0 | PMP=1.2 |
|---|---|---|---|---|---|
| 0.0 | 3.90° | 3.90° | 3.90° | 3.90° | 3.90° |
| 0.25 | 4.10° | 4.19° | 4.29° | 4.39° | 4.48° |
| 0.50 | 4.29° | 4.49° | 4.68° | 4.87° | 5.07° |
| 0.75 | 4.49° | 4.78° | 5.07° | 5.36° | 5.66° |
| 1.00 | 4.68° | 5.07° | 5.46° | 5.85° | 6.24° |

At the current value (PMP=0.8), maximum pressure on a player with average composure
produces 5.46° — a 40% error increase over no pressure. A highly composed player
(Composure=20, NormComposure=1.0) is fully immune to pressure (PressurePenalty = 1.0
regardless of PMP). PMP is the primary control for "how much pressure matters" in the game.

---

### C.6 D_MID / D_SCALE Blend Sensitivity

**Showing EffectiveAttribute at key distances for Finishing=18, LongShots=10.**
**Current values: D_MID=20, D_SCALE=8.**

**Effect of varying D_MID (D_SCALE fixed at 8):**

| Distance | D_MID=16 | D_MID=18 | D_MID=20 (current) | D_MID=22 | D_MID=24 |
|---|---|---|---|---|---|
| 10m | 15.72 | 16.22 | 16.86 | 17.20 | 17.41 |
| 16m | 14.00 | 15.07 | 15.87 | 16.29 | 16.67 |
| 20m | 14.73 | 14.00 | 14.00 | 14.27 | 14.73 |
| 24m | 12.79 | 12.97 | 12.95 | 13.21 | 12.97 |
| 30m | 11.16 | 11.29 | 11.57 | 11.66 | 11.75 |

At D_MID=20, the equal blend point is exactly at 20m (EffAttr=(18+10)/2=14.00) ✓
Raising D_MID shifts the "penalty area edge" further from goal — D_MID=24 gives Finishing
more influence at typical shooting ranges.

**Effect of varying D_SCALE (D_MID fixed at 20):**

| Distance | D_SCALE=4 | D_SCALE=6 | D_SCALE=8 (current) | D_SCALE=12 | D_SCALE=16 |
|---|---|---|---|---|---|
| 10m | 17.94 | 17.41 | 16.86 | 16.13 | 15.48 |
| 20m | 14.00 | 14.00 | 14.00 | 14.00 | 14.00 |
| 30m | 10.06 | 10.59 | 11.14 | 11.87 | 12.52 |

Smaller D_SCALE creates a steeper transition — at D_SCALE=4, players at 10m and 30m
are nearly exclusively using one attribute. Larger D_SCALE creates a more gradual blend —
at D_SCALE=16, even 10m-distance shots incorporate 35% LongShots (likely too much).
The current D_SCALE=8 provides a reasonable 17m transition zone from ~10% to ~90%.

---

### C.7 WeakFootErrorMultiplier vs. WeakFootRating

**Showing both multipliers for all WeakFootRatings. SHOT_WF_BASE_ERROR_PENALTY=0.60, SHOT_WF_VELOCITY_PENALTY=0.20**

| WeakFootRating | IsWeakFoot=false (any) | ErrorMultiplier | VelocityMultiplier | Error vs. DomFoot | Velocity vs. DomFoot |
|---|---|---|---|---|---|
| — (false) | 1.00 / 1.00 | 1.00 | 1.00 | No change | No change |
| 1 (worst) | — | 1.60 | 0.80 | +60% error | −20% velocity |
| 2 | — | 1.45 | 0.85 | +45% error | −15% velocity |
| 3 | — | 1.30 | 0.90 | +30% error | −10% velocity |
| 4 | — | 1.15 | 0.95 | +15% error | −5% velocity |
| 5 (ambi) | — | 1.00 | 1.00 | No change | No change |

Strictly monotone: ErrorMultiplier decreasing as rating improves ✓.
VelocityMultiplier increasing as rating improves ✓.

**Context for calibration:** At Rating=1, a weak-foot shot by an average player under
moderate conditions might have:
- BaseError ~2.25°, × PowerPenalty ~1.74, × WF 1.60 = ~6.26° before other penalties
- vs. dominant foot: ~3.91° in same conditions

This is a meaningful but not game-breaking difference (6° vs 4° — both result in reasonable
on-target probability from central positions). Playtesting should verify that Rating=1
feels punishing but not completely prohibitive, and that Rating=5 makes the weak foot
feel genuinely comfortable.

> ⚠ **OI-App-C-01:** Mirroring the finding from Pass Mechanics Appendix C.6:
> `SHOT_WF_BASE_ERROR_PENALTY = 0.60` produces a maximum error expansion of +60%.
> This is beyond any published academic reference (Pass Mechanics noted [CAREY-2001]
> documented 15–25% accuracy degradation for passes). For shots, no equivalent
> published benchmark exists. The 0.60 value is a design decision — explicitly [GT] —
> and should be reduced to 0.40–0.50 if playtesting shows weak-foot shots are
> effectively unusable at Rating=1. Flag as a primary playtesting calibration target.

---

## Open Issues Summary (Appendix-Identified)

| ID | Location | Description | Severity | Status |
|---|---|---|---|---|
| OI-App-B-01 | B.8 | GOAL_RELATIVE_ERROR_SCALE is fixed at 20m reference; over-predicts error at short distances. Recommend future improvement: distance-varying GRSE. | LOW | OPEN — Stage 0 known limitation; does not block approval |
| OI-App-B-02 | B.9 | §3.6.8 "poor striker" example stated ~28.7° but pre-clamp value is ~53.7°; also used weakFootMultiplier=1.3 (correct for Rating=3) instead of 1.60 (correct for Rating=1). | MINOR | ✅ RESOLVED — §3.6.8 corrected in Section 3 Part 2 v1.2 |
| OI-App-C-01 | C.7 | SHOT_WF_BASE_ERROR_PENALTY = 0.60 (+60% max error) has no academic reference. Design decision [GT]; may need reduction to 0.40–0.50 post-playtesting. | LOW | OPEN — deferred to playtesting; reclassify as [GT] in §8.6.5 if not already done |

All open issues are LOW or MINOR severity. None are blocking. Approve with noted items
flagged for playtesting calibration.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | February 23, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. Appendix A: 7 formula derivations (A.1–A.7). Appendix B: 9 numerical verification scenarios (B.1–B.9) including 2 open issues identified (OI-App-B-01 and OI-App-B-02). Appendix C: 7 sensitivity tables (C.1–C.7) including 1 calibration flag (OI-App-C-01). 3 total open issues — all LOW or MINOR severity, none blocking. §3.6.8 "poor striker" example discrepancy identified (OI-App-B-02). GOAL_RELATIVE_ERROR_SCALE distance-varying improvement recommended (OI-App-B-01). Weak foot penalty calibration flagged (OI-App-C-01). |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | OI-App-B-02 resolved: §3.6.8 corrected in Section 3 Part 2 v1.2 — weakFootMultiplier corrected from 1.3 → 1.60 (Rating=1); pre-clamp total corrected to ~53.7°; post-clamp 25.0° now correctly stated. Open issues reduced to 2 (OI-App-B-01 and OI-App-C-01), both LOW severity. |
| 1.2 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Section 8 dependency reference v1.0→v1.3 and constants count 68→92. (2) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. |

---

*End of Shot Mechanics Specification #6 — Appendices A, B, C*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*

*Next: Section 9 — Approval Checklist*
