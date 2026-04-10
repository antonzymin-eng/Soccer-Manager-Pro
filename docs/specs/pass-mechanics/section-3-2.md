# Pass Mechanics Specification #5 — Section 3.2: Pass Velocity Model

**File:** `Pass_Mechanics_Spec_Section_3_2_v1_0.md`

**Purpose:** Defines the complete pass velocity model for Pass Mechanics Specification #5.
Specifies the formula for computing scalar launch speed from pass type, intended distance,
KickPower attribute, and fatigue; the velocity clamping policy; constants; and the rationale
for every design decision. This is the authoritative implementation reference for
`PassVelocityCalculator.cs`. Full mathematical derivations are in Appendix A.1.

**Created:** February 21, 2026
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisite:** Section 3.1 v1.1 (PhysicalProfile record, including `vOffset` field)

**Amendment incorporated:**
- Amendment AM-003-001 — Velocity formula V_OFFSET correction. This section is written
  with AM-003-001 fully incorporated; the amendment document is superseded by this file
  for implementation purposes. The amendment document is retained in the project as the
  audit record of why the formula changed from the Outline's pure-product form.

**Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower` field name is provisional. The attribute exists in
  `PlayerAttributes` conceptually but the exact field name is pending ERR-007 resolution.
  This section uses `KickPower` throughout. When ERR-007 is resolved, update all
  references to match the confirmed name in a single find-and-replace pass.
- `[GT]` — Gameplay-Tunable constant. Value is a physically-motivated estimate requiring
  calibration during playtesting. Stored in `PassConstants.cs`, not hardcoded.

---

## Table of Contents

- [3.2.1 Responsibilities and Scope](#321-responsibilities-and-scope)
- [3.2.2 Inputs and Outputs](#322-inputs-and-outputs)
- [3.2.3 Base Velocity Formula](#323-base-velocity-formula)
- [3.2.4 KickPower Attribute Scaling](#324-kickpower-attribute-scaling)
- [3.2.5 Fatigue Modifier](#325-fatigue-modifier)
- [3.2.6 Velocity Clamping Policy](#326-velocity-clamping-policy)
- [3.2.7 Full Formula — Implementation Reference](#327-full-formula--implementation-reference)
- [3.2.8 Boundary Verification](#328-boundary-verification)
- [3.2.9 Constants Reference](#329-constants-reference)
- [3.2.10 Failure Modes](#3210-failure-modes)
- [3.2.11 Design Decisions and Rationale](#3211-design-decisions-and-rationale)
- [3.2.12 Cross-Specification Dependencies](#3212-cross-specification-dependencies)
- [3.2.13 Open Issues](#3213-open-issues)

---

## 3.2.1 Responsibilities and Scope

§3.2 computes a **scalar kick speed** (m/s) for a given pass. It is called once per pass
execution during the INITIATING state of the Pass Execution State Machine (§3.8). The
output is used in two places:

1. As the magnitude component of the kick velocity Vector3 (combined with launch angle
   from §3.3 and kick direction from §3.6) to form the argument to `Ball.ApplyKick()`.
2. As the denominator in the time-of-flight estimate used by §3.6 (Target Resolution)
   for through-ball lead distance calculation.

§3.2 does **not**:
- Compute launch angle (§3.3)
- Compute spin (§3.4)
- Apply error deviation (§3.5)
- Model ball deceleration during flight (Ball Physics owns this)
- Select pass type (Decision Tree #8 owns this)

---

## 3.2.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `PassType` | `PassRequest.PassType` | enum | 7 values | Profile lookup key |
| `IntendedDistance` | `PassRequest.IntendedDistance` | float | > 0.0 m | Distance to target at time of pass request |
| `KickPower` `[ERR-007]` | `AgentAttributes.KickPower` | float | [1.0, 20.0] | Clamped at ATTR_MAX before use |
| `Fatigue` | `AgentState.Fatigue` | float | [0.0, 1.0] | 0 = fully rested; 1 = fully fatigued |
| `PhysicalProfile` | §3.1 — loaded from `PassTypeProfiles` | record | — | Contains vMin, vOffset, vMax, distMax |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `kickSpeed` | §3.3, §3.6, `Ball.ApplyKick()` | float (m/s) | Always within [vMin, vMax]; never zero |

---

## 3.2.3 Base Velocity Formula

### Design Requirement

The formula must satisfy three constraints simultaneously:

1. **Mid-range realism:** A player with neutral attributes (KickPower=10) making a
   typical pass (Ground, 20m) must produce a velocity consistent with observed football
   data (StatsBomb: ground passes 8–22 m/s at elite level; mid-range player ≈ 10–13 m/s).
2. **Monotonicity:** V must increase strictly with both KickPower and Distance, up to the
   V_MAX clamp. A stronger player must always hit harder; a longer pass must always require
   more force (before clamping).
3. **Determinism:** Identical inputs must produce bit-identical output.

### Formula

```
V_base(K, D, PassType) = vOffset(PassType)
                       + (K / ATTR_MAX) × (D / distMax(PassType)) × (vMax(PassType) - vOffset(PassType))
```

Where:
- `K`            = KickPower [1.0, 20.0] `[ERR-007-PENDING]`
- `D`            = IntendedDistance (metres)
- `ATTR_MAX`     = 20.0 (maximum attribute value per [MASTER-VOL2])
- `vOffset`      = per-type minimum practical kick speed [GT] (§3.1.4, §3.2.9)
- `distMax`      = per-type maximum distance [GT] (§3.1.4)
- `vMax`         = per-type maximum launch speed [GT] (§3.1.4)

### Why V_OFFSET (Not V_MIN) as the Interpolation Base

The Outline's initial formula used `vMin` as the interpolation base:
`V_base = vMin + (K/ATTR_MAX) × (D/distMax) × (vMax - vMin)`.

Appendix B numerical verification (OI-App-B-01) showed this under-powered mid-range
inputs by ~1.3 m/s: at K=10, D=20m for Ground, it produced 8.71 m/s against the PV-001
expected range [10, 16] m/s.

Root cause: `vMin` is the **absolute safety floor** for Ball.ApplyKick() — the minimum
a player could possibly generate before the system rejects the pass as physically invalid.
A mid-range player on a mid-range pass does not generate near-minimum force. `vOffset`
represents the **practical minimum kick speed** — the minimum mechanical energy expended
when executing a valid pass of this type. `vOffset > vMin` always. See Appendix A.1
for the full derivation and Amendment AM-003-001 for the problem statement.

---

## 3.2.4 KickPower Attribute Scaling

```
PowerScale(K, D) = (K / ATTR_MAX) × (D / distMax(PassType))
```

**Interpretation:** PowerScale is a normalised product in [0.0, 1.0]. At K=20 and
D=distMax, PowerScale = 1.0 — the player achieves the maximum additional velocity
above vOffset. At K=1 and D=1m, PowerScale ≈ 0 — the player is at the vOffset floor.

**Monotonicity verification:**

- ∂V_base/∂K = (D / distMax) × (vMax - vOffset) / ATTR_MAX > 0  ✓ (strictly increasing in K)
- ∂V_base/∂D = (K / ATTR_MAX) × (vMax - vOffset) / distMax > 0  ✓ (strictly increasing in D)

Both partial derivatives are strictly positive for all valid inputs (K > 0, D > 0,
vMax > vOffset), confirming monotonicity before clamping.

**Attribute clamping pre-computation:**

```csharp
// Clamp KickPower to valid range before formula application
float K = Mathf.Clamp(agentAttributes.KickPower, 1.0f, ATTR_MAX);
```

This guard ensures the formula never receives out-of-range input. An agent whose
KickPower exceeds ATTR_MAX due to a data error is treated as ATTR_MAX — not an exception.

---

## 3.2.5 Fatigue Modifier

```
FatigueModifier(F) = 1.0f - (F × FATIGUE_POWER_REDUCTION)
```

Where:
- `F` = `AgentState.Fatigue` [0.0, 1.0]
- `FATIGUE_POWER_REDUCTION` = [GT] constant ∈ [0.15, 0.25] (see §3.2.9)

**Boundary verification:**

| Fatigue | FatigueModifier | Interpretation |
|---------|----------------|----------------|
| 0.0 (fully rested)   | 1.00 | No power reduction |
| 0.25                 | 0.95 | 5% power reduction |
| 0.50                 | 0.90 | 10% power reduction |
| 0.75                 | 0.85 | 15% power reduction |
| 1.0 (fully fatigued) | 0.80 | 20% power reduction (at FATIGUE_POWER_REDUCTION=0.20) |

FatigueModifier is always in (0.0, 1.0] — it can only reduce velocity, never amplify
it. At F=1.0, FatigueModifier = 1.0 - FATIGUE_POWER_REDUCTION > 0.0 because
FATIGUE_POWER_REDUCTION < 1.0.

**Note:** `FATIGUE_POWER_REDUCTION` governs velocity degradation from fatigue.
A separate constant `FATIGUE_ACCURACY_REDUCTION` governs error angle degradation
(§3.5 — Error Model). These are independent. A fatigued player hits both softer
and less accurately, but the two effects are tuned separately.

**Academic grounding:** [ALI-2011] confirms monotonic velocity degradation with fatigue
in football-specific endurance contexts. The linear functional form is a Stage 0
simplification; non-linear forms are deferred to Stage 2+.

---

## 3.2.6 Velocity Clamping Policy

```csharp
float V_unclamped = V_base × FatigueModifier;
float kickSpeed   = Mathf.Clamp(V_unclamped, profile.vMin, profile.vMax);
```

**Why clamp after multiplying by FatigueModifier (not before)?**

If V_base were clamped to vMin before applying FatigueModifier, then a fatigued player
already at the vMin floor would be further degraded below vMin — violating the clamp
contract. Clamping after multiplication ensures the floor is always enforced on the
final output, not on the pre-fatigue velocity.

**Why vMin as the clamp floor (not vOffset)?**

vOffset is the practical minimum a player generates. But Ball.ApplyKick() must never
receive zero or near-zero velocity regardless of how degraded a player's state becomes.
vMin is the absolute defensive guard. In normal operation V_unclamped never falls below
vOffset (because V_base ≥ vOffset by construction). The vMin clamp exists as a defensive
guard against unexpected numerical edge cases (floating-point underflow, out-of-range
fatigue values passed in) and must never be the primary floor in normal execution.

If `kickSpeed` reaches `vMin` in testing, this is a signal that either:
(a) The input combination is at the extreme edge of the design envelope, or
(b) There is a bug upstream producing out-of-range inputs.

Log the event at DEBUG level when this occurs: `Debug.Log("Pass velocity clamped to vMin — investigate inputs")`.

---

## 3.2.7 Full Formula — Implementation Reference

```csharp
/// <summary>
/// Computes the scalar kick speed for a pass.
/// Called once per pass execution during INITIATING state.
/// </summary>
/// <param name="passType">Pass type enum (determines profile)</param>
/// <param name="intendedDistance">Distance to target in metres (> 0)</param>
/// <param name="kickPower">Agent KickPower attribute [1, 20] [ERR-007-PENDING]</param>
/// <param name="fatigue">Agent fatigue [0, 1]; 0 = rested, 1 = exhausted</param>
/// <param name="profile">Physical profile from PassTypeProfiles (§3.1)</param>
/// <returns>Scalar kick speed in m/s, clamped to [profile.vMin, profile.vMax]</returns>
public static float ComputeKickSpeed(
    PassType passType,
    float intendedDistance,
    float kickPower,
    float fatigue,
    PhysicalProfile profile)
{
    // Guard: zero or negative distance is a caller error — should be caught by FM-07
    // before reaching this function. Defensive check only.
    if (intendedDistance <= 0f)
    {
        Debug.LogError($"ComputeKickSpeed called with distance={intendedDistance}. FM-07 should have fired.");
        return profile.vMin;
    }

    // Clamp KickPower to valid attribute range
    float K = Mathf.Clamp(kickPower, 1.0f, PassConstants.ATTR_MAX);

    // Clamp distance to valid profile range (do not error — use distMax as ceiling)
    float D = Mathf.Min(intendedDistance, profile.distMax);

    // Step 1: Base velocity from KickPower and Distance
    float powerScale    = (K / PassConstants.ATTR_MAX) * (D / profile.distMax);
    float V_base        = profile.vOffset + powerScale * (profile.vMax - profile.vOffset);

    // Step 2: Apply fatigue degradation
    float fatigueModifier = 1.0f - (fatigue * PassConstants.FATIGUE_POWER_REDUCTION);
    float V_unclamped     = V_base * fatigueModifier;

    // Step 3: Clamp to profile bounds
    float kickSpeed = Mathf.Clamp(V_unclamped, profile.vMin, profile.vMax);

    // Debug log if vMin clamp fires in normal operation (should not happen)
    if (kickSpeed <= profile.vMin + 0.001f)
        Debug.Log($"[PassVelocity] kickSpeed clamped to vMin ({profile.vMin}) for {passType}. K={K}, D={D}, Fatigue={fatigue}");

    return kickSpeed;
}
```

> **Note:** This pseudocode is for specification clarity. Actual implementation will follow
> Unity coding standards. Field name `kickPower` will update to the confirmed name on
> ERR-007 resolution.

---

## 3.2.8 Boundary Verification

These checks verify the formula behaves correctly at every boundary condition. All values
use constants from §3.2.9.

**Check 1: Maximum power, maximum distance → V_MAX**

```
K = 20, D = distMax_Ground = 35m, Fatigue = 0:
V_base = 8.0 + (20/20) × (35/35) × (18.0 - 8.0) = 8.0 + 1.0 × 1.0 × 10.0 = 18.0 m/s
FatigueModifier = 1.0 - 0 = 1.0
V_unclamped = 18.0 × 1.0 = 18.0 m/s
kickSpeed = clamp(18.0, 5.0, 18.0) = 18.0 m/s = V_MAX_Ground ✓
```

**Check 2: Minimum power, minimum distance → Near V_OFFSET**

```
K = 1, D = 1m, Fatigue = 0, Ground:
PowerScale = (1/20) × (1/35) = 0.05 × 0.0286 = 0.00143
V_base = 8.0 + 0.00143 × 10.0 = 8.0 + 0.0143 ≈ 8.014 m/s
kickSpeed = clamp(8.014, 5.0, 18.0) = 8.014 m/s
```

Result: Slightly above vOffset (8.0), well above vMin (5.0). Correct — minimum power at
minimum distance does not approach the vMin floor. FM-07 fires before D=0 is reached.

**Check 3: Mid-range inputs → PV-001 validation**

```
K = 10, D = 20m, Fatigue = 0, Ground:
PowerScale = (10/20) × (20/35) = 0.5 × 0.571 = 0.2857
V_base = 8.0 + 0.2857 × 10.0 = 8.0 + 2.857 = 10.857 m/s
kickSpeed = clamp(10.857, 5.0, 18.0) = 10.857 m/s
PV-001 expects: [10, 16] m/s → 10.857 ∈ [10, 16] ✓
```

**Check 4: Full fatigue reduces velocity monotonically**

```
K = 15, D = 30m, Ground, FATIGUE_POWER_REDUCTION = 0.20:
V_base = 8.0 + (15/20) × (30/35) × 10.0 = 8.0 + 0.75 × 0.857 × 10.0 = 8.0 + 6.43 = 14.43 m/s

Fatigue = 0.0: V = 14.43 × 1.00 = 14.43 m/s
Fatigue = 0.5: V = 14.43 × 0.90 = 12.99 m/s
Fatigue = 1.0: V = 14.43 × 0.80 = 11.54 m/s

Sequence: 14.43 > 12.99 > 11.54 — strictly decreasing ✓ (PV-008 monotone test passes)
```

**Check 5: V_OFFSET_Chip, max power, D_MAX → V_MAX_Chip (PV-005)**

```
K = 20, D = 20m (D_MAX_Chip), Fatigue = 0, Chip:
V_base = 6.0 + (20/20) × (25/25) × (14.0 - 6.0) = 6.0 + 1.0 × 1.0 × 8.0 = 14.0 m/s
kickSpeed = clamp(14.0, 5.0, 14.0) = 14.0 m/s = V_MAX_Chip ✓
```

---

## 3.2.9 Constants Reference

All constants are stored in `PassConstants.cs`. All `[GT]` values are gameplay-tunable
and stored separately from compiled logic.

### Universal Constants

| Constant | Value | Source | Notes |
|----------|-------|--------|-------|
| `ATTR_MAX` | 20.0 | [MASTER-VOL2] | Maximum value for any PlayerAttribute |
| `FATIGUE_POWER_REDUCTION` | 0.20 [GT] | ACADEMIC-INFORMED [ALI-2011] | 20% velocity reduction at full fatigue; tune in [0.10, 0.30] |

### Per-Type Constants (stored in PhysicalProfile — §3.1.4)

| Pass Type     | vMin | vOffset | vMax | distMax |
|---------------|------|---------|------|---------|
| Ground        | 5.0  | 8.0     | 18.0 | 35.0    |
| Driven        | 10.0 | 12.0    | 28.0 | 50.0    |
| Lofted        | 8.0  | 9.0     | 22.0 | 55.0    |
| ThroughBall   | 6.0  | 8.5     | 20.0 | 40.0    |
| AerialThrough | 8.0  | 9.0     | 22.0 | 45.0    |
| Cross (Flat)  | 8.0  | 10.0    | 26.0 | 50.0    |
| Cross (Whipped)| 8.0 | 10.0    | 24.0 | 50.0    |
| Cross (High)  | 8.0  | 10.0    | 20.0 | 50.0    |
| Chip          | 5.0  | 6.0     | 14.0 | 20.0    |

> ⚠ All values are [GT] placeholder estimates. Require Ball Physics drag model simulation
> validation (XC-3.1-01) before finalisation. Do not approve §3.2 without this check.

### Citation Audit (§3.2 Scope)

| Constant / Formula | Disposition | Source |
|---|---|---|
| `ATTR_MAX = 20.0` | DESIGN-AUTHORITY | [MASTER-VOL2] §PlayerAttributes |
| V_base formula structure | ACADEMIC-INFORMED + [GT] | [LEES-1998], [KELLIS-2007] |
| `vOffset` per type | GAMEPLAY-TUNED [GT] | No academic source; practical minimum design choice |
| `vMin` per type | GAMEPLAY-TUNED [GT] | Safety floor; design decision |
| `vMax` per type | ACADEMIC-INFORMED + [GT] | [STATSBOMB-OPEN] distributions + design choice |
| `distMax` per type | GAMEPLAY-TUNED [GT] | Design decision per pass-type tactical intent |
| `FATIGUE_POWER_REDUCTION` | ACADEMIC-INFORMED + [GT] | [ALI-2011] direction; value is design choice |
| Clamping policy (post-fatigue) | DESIGN-AUTHORITY | [MASTER-VOL1] §1.3 determinism |

---

## 3.2.10 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| FM-07 | `IntendedDistance ≤ 0` | Return `PassResult(FM-07)`; do not call `ComputeKickSpeed` | Checked by caller before §3.2 is invoked; §3.2 has defensive log only |
| FM-02 | `KickPower` attribute read fails `[ERR-007]` | Use fallback value `KickPower = ATTR_MAX / 2 = 10.0`; log warning | Temporary until ERR-007 resolved |
| — | `kickSpeed` clamps to `vMin` in normal operation | Log at DEBUG; not a failure mode | Indicates extreme input; investigate if frequent |

---

## 3.2.11 Design Decisions and Rationale

**DD-3.2-01: Linear KickPower-to-velocity mapping**

A linear relationship was chosen over quadratic or inverse because:
(a) It is the simplest form satisfying monotonicity and is the easiest to audit.
(b) [KELLIS-2007] documents an approximately linear relationship between player foot
    speed and ball speed at contact over the typical range.
(c) Non-linearity at extremes is handled by the vMin/vMax clamping mechanism, producing
    a piecewise-linear response that approximates the concave-down curve in the data.
(d) Non-linear mapping is flagged as a Stage 1+ refinement if playtesting reveals
    mid-range attributes feeling "flat."

**DD-3.2-02: Fatigue applied after base velocity (not before)**

FatigueModifier multiplies V_base rather than reducing the KickPower input. This is
intentional: fatigue degrades a player's effective power output on this specific kick,
not their underlying attribute value. Applying fatigue to KickPower would require
recalculating PowerScale, which introduces an indirect coupling. Multiplying at the end
is simpler and logically equivalent at Stage 0.

**DD-3.2-03: Distance capped at distMax (not an error)**

If IntendedDistance > distMax, the distance is silently capped to distMax in the formula.
A warning is logged, but the pass is not rejected. This is consistent with §3.1.5 —
the Decision Tree should not request over-distance passes, but if it does, graceful
degradation is the correct response. The velocity at the cap equals vMax (maximum power)
or below — physically reasonable.

**DD-3.2-04: V_OFFSET as practical minimum (AM-003-001)**

The Outline's original formula treated vMin as the interpolation base, producing
physically implausible velocities for mid-range inputs. The vOffset constant represents
the minimum mechanical energy of a valid kick, independent of KickPower and Distance.
See Amendment AM-003-001 (retained for audit history) and Appendix A.1 for full derivation.

---

## 3.2.12 Cross-Specification Dependencies

| Dependency | Owner | Status | Blocks |
|------------|-------|--------|--------|
| `KickPower` attribute field name | Agent Movement §3.5 / ERR-007 | PENDING | Implementation only; formula unaffected |
| Ball Physics `MAX_BALL_SPEED` ≥ 28.0 m/s | Ball Physics §3.1 / XC-3.1-01 | REQUIRED before approval | `Driven.vMax` may need revision |
| `PhysicalProfile.vOffset` field | §3.1 v1.1 | ✓ Defined | None — resolved in §3.1 v1.1 |
| `Ball.ApplyKick()` signature | Ball Physics §3.1.11 / ERR-006 | PENDING (AM-001-001 drafted) | Implementation only |

---

## 3.2.13 Open Issues

| OI ID | Issue | Severity | Resolution |
|-------|-------|----------|------------|
| OI-3.2-01 | `vMin`, `vOffset`, `vMax` values require Ball Physics drag validation before finalisation | HIGH | Run Ball Physics simulation at each vMax to confirm field coverage. XC-3.1-01 prerequisite. |
| OI-3.2-02 | `FATIGUE_POWER_REDUCTION = 0.20` is a placeholder | LOW | Calibrate during playtesting. Tune in [0.10, 0.30]. |
| OI-3.2-03 | `KickPower` field name provisional `[ERR-007-PENDING]` | MODERATE (blocked) | Resolve ERR-007; single find-and-replace update |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 21, 2026 | Claude (AI) / Anton | Initial full section. Amendment AM-003-001 fully incorporated — `vOffset` interpolation base replaces the Outline's `vMin` base. Complete formula reference, constants, boundary verification, pseudocode, and citation audit included. Supersedes §3.2 content in Outline v1.0. |
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 1 instance); Chip distMax corrected 25m→20m in boundary check and constants table (M-01). |

---

*End of Section 3.2 — Pass Mechanics Specification #5*

*Next subsection: §3.3 — Launch Angle Derivation*
