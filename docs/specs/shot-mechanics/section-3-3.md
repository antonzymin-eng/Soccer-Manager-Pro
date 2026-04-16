## 3.3 Launch Angle Derivation

### 3.3.1 Responsibilities and Scope

§3.3 (LaunchAngleCalculator) computes the **vertical launch angle** (degrees above
horizontal) at which the ball leaves the foot. This angle, combined with the horizontal
aim direction from §3.5 (PlacementResolver), fully specifies the 3D velocity direction
passed to `Ball.ApplyKick()`.

Launch angle is **not** assigned from a per-type lookup table. It emerges from four
additive terms: a base angle determined by `ContactZone`, a power-driven loft modifier,
a spin-driven loft modifier, and body mechanics penalties. Named shot labels (driven,
chip, finesse) are irrelevant to this calculation — they are Decision Tree vocabulary.

§3.3 **does**:
- Select base launch angle from `ContactZone` (the only zone-to-angle mapping)
- Apply `PowerLiftModifier` (lower power → slightly more loft)
- Apply `SpinLiftModifier` (`BelowCentre` + high `SpinIntent` → chip arc)
- Apply `BodyLeanPenalty` from body lean measurement (§3.7 input)
- Apply `BodyShapePenalty` from `BodyMechanicsScore` (§3.7 input)
- Clamp output to `[LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX]`

§3.3 **does not**:
- Compute the horizontal aim direction (§3.5)
- Apply error deviation to the launch angle (§3.6 applies error in goal-relative (u,v)
  space; §3.3 operates only on the vertical dimension before error application)
- Compute body lean or body mechanics score (these are §3.7 outputs passed as inputs)

---

### 3.3.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `request.ContactZone` | ShotRequest | ContactZone | enum | Pre-validated by §3.1 |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `request.SpinIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `bodyLeanAngleDeg` | §3.7 BodyMechanicsEvaluator | float | [−45°, +45°] | Negative = leaning forward; positive = leaning back |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | float | [0.0, 1.0] | Low score → high BodyShapePenalty |

**Output:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `launchAngleDeg` | §3.5 PlacementResolver / velocity vector construction | float (degrees) | [LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX] = [−5°, 70°] |

---

### 3.3.3 Base Angle by ContactZone

The contact point on the ball face is the primary determinant of trajectory elevation.
This is the only zone-to-value lookup in the launch angle calculation.

| ContactZone | `BaseAngle` (degrees) | Source | Physical interpretation |
|---|---|---|---|
| `Centre` | 4.0° | [GT]; informed by [LEES-1998] (2–8° for full instep) | Low, driven trajectory typical of powered shots |
| `BelowCentre` | 18.0° | [GT]; informed by [INOUE-2014] (lift increases sharply with under-ball contact) | Chipped/lifted trajectory; ball scooped upward |
| `OffCentre` | 8.0° | [GT]; informed by [ASAI-2002] (curled shots have modest elevation) | Moderate lift; curl trajectory stays usable |

**Rationale for discrete base angles by zone rather than a continuous function:**

`ContactZone` is a discrete enum (three values). The decision to use a lookup table
rather than a continuous function of, e.g., estimated contact angle, is intentional:
the physical contact geometry (where on the ball face the foot strikes) is the Decision
Tree's intent signal, encoded as one of three canonical positions. A continuous mapping
would imply precision in contact point measurement that does not exist in the model.
The three values capture the three fundamentally different trajectory families.

---

### 3.3.4 Power Lift Modifier

At lower power, the foot tends to come through the ball with a slightly more upward
trajectory (less committed "through" the ball), producing marginally more loft. At
maximum power, the foot drives through with a flat, forceful trajectory, keeping the
ball low.

```
PowerLiftModifier = POWER_LIFT_SCALE × (1.0f − PowerIntent)
POWER_LIFT_SCALE = 4.0° [GT]
```

| PowerIntent | Modifier contribution | Interpretation |
|---|---|---|
| 0.0 | +4.0° | Gentle tap; more loft than a driven shot |
| 0.5 | +2.0° | Moderate; slight elevation above base |
| 1.0 | 0.0° | Full power drive; no additional loft |

**Magnitude note:** The power lift effect is intentionally small (maximum 4°). Its
primary role is distinguishing a placed shot from a driven shot at the same ContactZone,
not producing dramatically different trajectories. Chip trajectory elevation is
governed primarily by `BelowCentre` base angle and `SpinLiftModifier`, not power lift.

---

### 3.3.5 Spin Lift Modifier

Deliberate backspin applied below the ball's equator produces additional lift.
This is the primary mechanism by which a chip arc is achieved — `BelowCentre` contact
combined with high `SpinIntent` generates the characteristic high, looping trajectory.

```
SpinLiftModifier = SPIN_LIFT_SCALE × SpinIntent
SPIN_LIFT_SCALE = 14.0° [GT]
```

| SpinIntent | Modifier contribution | Interpretation |
|---|---|---|
| 0.0 | 0.0° | No deliberate spin; no additional lift |
| 0.5 | +7.0° | Moderate curl/chip intent |
| 1.0 | +14.0° | Maximum chip intent; strongly lifted arc |

**Key interaction with ContactZone:** The full effect of `SpinLiftModifier` is only
meaningful when combined with `BelowCentre` contact. A `Centre` contact with
`SpinIntent = 1.0` would add 14° but the physical model for sidespin (OffCentre) would
be applied differently by §3.4. The designer should note that `SpinLiftModifier` applies
equally across all ContactZones — the gameplay interpreter (Decision Tree) must ensure
that high `SpinIntent` accompanies appropriate `ContactZone` selection for realistic chip
trajectories. This is documented as a caller responsibility.

---

### 3.3.6 Body Lean Penalty

Body lean is the most significant source of unintended shot elevation. An agent leaning
backward at contact applies force at an upward angle through the ball, lifting it. This
is the primary cause of shots skied over the crossbar in real football and is a key
differentiator between Shot Mechanics and Pass Mechanics (which rarely produces
dramatically ballooned trajectories).

The body lean angle (`bodyLeanAngleDeg`) is computed by §3.7 from
`AgentPhysicalProperties.BodyLeanAngle`. The sign convention:
- **Negative values**: leaning forward (forward over the ball) → reduces launch angle
- **Positive values**: leaning backward (standing upright or falling back) → increases launch angle

```
BodyLeanPenalty = bodyLeanAngleDeg × BODY_LEAN_TRANSFER_COEFFICIENT
BODY_LEAN_TRANSFER_COEFFICIENT = 0.60 [GT]
```

| Body lean | Penalty contribution | Interpretation |
|---|---|---|
| −20° (strongly forward) | −12°  | Lower than base angle; reduces risk of skying |
| −5° (slightly forward) | −3° | Slightly lower; forward press over ball |
| 0° (neutral) | 0° | Neutral stance; no modifier |
| +10° (slight lean back) | +6° | Modest additional lift |
| +25° (significant lean back) | +15° | Strongly lifted trajectory; high risk of crossbar or over |
| +40° (falling back) | +24° | Severely elevated; shot likely over bar |

**Rationale for coefficient (0.60) rather than 1:1 transfer:** A 1:1 transfer would mean
a 40° backward lean produces a 40° launch angle addition — an unrealistically direct
mechanical correspondence. In practice, the foot contact and body rotation are not
rigidly coupled. The 0.60 coefficient represents partial mechanical coupling. This value
will require empirical tuning against real match footage of ballooned shots.

**Important:** Negative `BodyLeanPenalty` (forward lean) can reduce `launchAngleDeg`
below `BaseAngle`. This is physically meaningful — an agent leaning strongly over
the ball at Centre contact could produce a trajectory close to ground level or even
slightly downward (daisy-cutter), which is clamped by `LAUNCH_ANGLE_MIN` (§3.3.9).

---

### 3.3.7 Body Shape Penalty

Where `BodyLeanPenalty` captures a directional effect from lean angle, `BodyShapePenalty`
captures general stance instability — cramped plant foot, off-balance run-up, or
uncoordinated approach — which produces **random-direction** launch angle error beyond
the intended elevation. Since §3.6 handles horizontal error, `BodyShapePenalty` here
adds unpredictable vertical scatter to the launch angle.

```
BodyShapePenalty = BODY_SHAPE_MAX_PENALTY × (1.0f − bodyMechanicsScore)
BODY_SHAPE_MAX_PENALTY = 8.0° [GT]
```

| BodyMechanicsScore | BodyShapePenalty | Interpretation |
|---|---|---|
| 1.0 (perfect) | 0.0° | No additional scatter |
| 0.8 (good) | +1.6° | Minor stance imperfection |
| 0.6 (poor) | +3.2° | Noticeable instability |
| 0.0 (worst case) | +8.0° | Severely compromised contact; shot trajectory unreliable |

**Note:** `BodyShapePenalty` is always positive (it adds scatter above the intended
angle). For the purpose of this formula, it operates as a positive addend. The physical
interpretation is that poor body shape tends to produce higher-than-intended trajectories
(the foot slips under the ball) more often than lower-than-intended (difficult to
compress the ball below intended angle without good stance). If playtesting indicates
this asymmetry is unrealistic, the penalty can be converted to a signed random term using
the deterministic hash — but this change requires §3.6 integration and is deferred.

---

### 3.3.8 Full Formula — Implementation Reference

```
launchAngleDeg = BaseAngle[ContactZone]
               + (POWER_LIFT_SCALE × (1.0f − PowerIntent))
               + (SPIN_LIFT_SCALE × SpinIntent)
               + (bodyLeanAngleDeg × BODY_LEAN_TRANSFER_COEFFICIENT)
               + (BODY_SHAPE_MAX_PENALTY × (1.0f − bodyMechanicsScore));

launchAngleDeg = Mathf.Clamp(launchAngleDeg, LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX);
```

**Worked example — Elite driven shot (Centre, PowerIntent 0.9, SpinIntent 0.0,
body lean −5°, BodyMechanicsScore 0.95):**

```
launchAngleDeg = 4.0                               // BaseAngle[Centre]
               + (4.0 × (1.0 − 0.9))              // +0.4° (small power lift)
               + (14.0 × 0.0)                      // +0.0° (no spin)
               + (−5.0 × 0.60)                     // −3.0° (slight forward lean)
               + (8.0 × (1.0 − 0.95))             // +0.4° (minor shape penalty)
             = 1.8°
```

A 1.8° launch angle on a 33 m/s shot is a low, fast, driven trajectory — realistic for
a well-struck penalty-area shot from a composed striker.

**Worked example — Chip shot (BelowCentre, PowerIntent 0.55, SpinIntent 0.85,
body lean +3°, BodyMechanicsScore 0.80):**

```
launchAngleDeg = 18.0                              // BaseAngle[BelowCentre]
               + (4.0 × (1.0 − 0.55))             // +1.8° (moderate power lift)
               + (14.0 × 0.85)                     // +11.9° (strong spin lift)
               + (3.0 × 0.60)                      // +1.8° (slight lean back)
               + (8.0 × (1.0 − 0.80))             // +1.6° (moderate shape penalty)
             = 35.1°
```

A 35° launch angle on a ~14 m/s shot (after velocity modifiers) produces the
characteristic high, looping chip trajectory over a rushing goalkeeper.

---

### 3.3.9 Launch Angle Clamping Policy

```
LAUNCH_ANGLE_MIN = −5.0°  [GT]  — permits daisy-cutter shots with strong forward lean
LAUNCH_ANGLE_MAX = 70.0°  [GT]  — permits extreme chip arcs; above this produces
                                  near-vertical trajectory physically implausible in football
```

**Rationale for `LAUNCH_ANGLE_MIN = −5°` (not 0°):** A slightly downward trajectory is
physically possible and desirable — elite players can deliberately strike the ball with
slight top-spin and downward force to bounce it powerfully along the ground. However,
angles below −5° represent physically implausible contact geometry (the foot would need
to strike downward through the ball) and are clamped.

**Rationale for `LAUNCH_ANGLE_MAX = 70°`:** Angles above 70° produce near-vertical
trajectory that is physically unreachable in a football context (it would require the
agent to scoop the ball from beneath with the toe — a different mechanical action
outside this system's scope). The 70° ceiling is permissive enough to cover the highest
realistic chip without constraining legitimate gameplay outcomes.

---

### 3.3.10 Boundary Verification Table

All values [EST] pending Appendix B numerical verification.

| Scenario | Zone | PowerIntent | SpinIntent | Lean° | BM Score | Expected angle | Est. range |
|---|---|---|---|---|---|---|---|
| Driven, elite | Centre | 0.95 | 0.0 | −5° | 0.95 | ~1.5° | [−2°, 5°] [EST] |
| Placed, composed | Centre | 0.55 | 0.0 | 0° | 0.85 | ~6.0° | [3°, 10°] [EST] |
| Chip, deliberate | BelowCentre | 0.50 | 0.90 | +5° | 0.80 | ~38° | [30°, 50°] [EST] |
| Curl, crossing | OffCentre | 0.75 | 0.70 | 0° | 0.90 | ~19° | [12°, 25°] [EST] |
| Ballooned (lean back) | Centre | 0.85 | 0.0 | +35° | 0.65 | ~27° | [20°, 35°] [EST] |
| Extreme sky (max lean) | Centre | 0.90 | 0.0 | +45° | 0.50 | ~35° | [25°, 45°] [EST] |

---

### 3.3.11 Constants Reference

| Constant | Value | Source | Tuning Range | Notes |
|---|---|---|---|---|
| `BaseAngle[Centre]` | 4.0° | [GT]; [LEES-1998] informed | [2.0°, 7.0°] | Low driven trajectory reference |
| `BaseAngle[BelowCentre]` | 18.0° | [GT]; [INOUE-2014] informed | [14.0°, 24.0°] | Chip arc base |
| `BaseAngle[OffCentre]` | 8.0° | [GT]; [ASAI-2002] informed | [5.0°, 12.0°] | Curl trajectory base |
| `POWER_LIFT_SCALE` | 4.0° | [GT] | [2.0°, 6.0°] | Max additional loft at PowerIntent=0 |
| `SPIN_LIFT_SCALE` | 14.0° | [GT] | [10.0°, 20.0°] | Max additional loft at SpinIntent=1 |
| `BODY_LEAN_TRANSFER_COEFFICIENT` | 0.60 | [GT] | [0.45, 0.75] | Fraction of body lean transferred to angle |
| `BODY_SHAPE_MAX_PENALTY` | 8.0° | [GT] | [5.0°, 12.0°] | Max scatter at BodyMechanicsScore=0 |
| `LAUNCH_ANGLE_MIN` | −5.0° | [GT] | [−10°, 0°] | Daisy-cutter floor; do not lower below −10° |
| `LAUNCH_ANGLE_MAX` | 70.0° | [GT] | [60°, 75°] | Near-vertical ceiling |

---

### 3.3.12 Failure Modes

| FM ID | Trigger | Response |
|-------|---------|----------|
| FM-LA01 | `bodyLeanAngleDeg` outside [−45°, +45°] (§3.7 contract violation) | Log Warning; clamp to [−45°, +45°] before applying formula |
| FM-LA02 | `bodyMechanicsScore` outside [0, 1] (§3.7 contract violation) | Log Warning; clamp to [0, 1] |
| FM-LA03 | `launchAngleDeg` NaN after formula | Log Error; use `BaseAngle[ContactZone]` as fallback |
| FM-LA04 | `launchAngleDeg` exceeds `LAUNCH_ANGLE_MAX` after formula | Clamp silently; log DEBUG |
| FM-LA05 | `launchAngleDeg` below `LAUNCH_ANGLE_MIN` after formula | Clamp silently; log DEBUG |

---

### 3.3.13 Design Decisions and Rationale

**DD-3.3-01: Launch angle is emergent, not table-driven**

An earlier design (Outline v1.1 — superseded) assigned launch angles from a per-ShotType
lookup table. This was eliminated when ShotType enum was removed (Outline v1.2).
The emergent approach has three advantages: (a) it is consistent with the parameter-based
model — physical output follows physical inputs; (b) it produces continuous variation
within each ContactZone rather than discrete jumps between types; (c) it requires no
table maintenance as new intent profiles are added.

**DD-3.3-02: Body lean is the primary "shots skied" mechanism**

The choice to model `bodyLeanAngleDeg` as a direct addend (via coefficient) rather than
a binary "lean flag" ensures that the severity of shot elevation scales continuously with
the degree of lean. A slightly rushed shot with minor lean produces minor elevation
increase; a fully off-balance shot produces severe elevation. This granularity is the
key differentiator between this system and simpler football games.

**DD-3.3-03: BodyShapePenalty always positive**

The asymmetry (poor body shape adds angle, never subtracts) is a conservative design
choice. Poor stance most commonly produces higher-than-intended trajectories in real
football. If playtesting reveals that some poor-stance shots should produce lower
trajectories (heavy, mis-struck shots that trickle), a signed penalty can be introduced
in a future revision — but this adds complexity to §3.6 interaction.

---

### 3.3.14 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `ContactZone` enum definition | Shot Mechanics §2.4.1 (Section 2) | Three values: Centre, BelowCentre, OffCentre |
| `bodyLeanAngleDeg` | Shot Mechanics §3.7 BodyMechanicsEvaluator | Signed float; lean backward = positive; forward = negative |
| `bodyMechanicsScore` | Shot Mechanics §3.7 BodyMechanicsEvaluator | Scalar [0.0, 1.0] |
| `AgentPhysicalProperties.BodyLeanAngle` | Agent Movement #2 §3.5.4 | Confirmed present in approved spec |

---

## Section 3 (Part 1) Summary

| Sub-system | Owner file | Key output | Status |
|---|---|---|---|
| §3.1 ShotRequest Validation | `ShotValidator.cs` | Pass/fail gate; `ShotOutcome.Invalid` on failure | ✅ Specified |
| §3.2 Velocity Calculation | `VelocityCalculator.cs` | `kickSpeed` (m/s) ∈ [8.0, 35.0] | ✅ Specified |
| §3.3 Launch Angle Derivation | `LaunchAngleCalculator.cs` | `launchAngleDeg` ∈ [−5°, 70°] | ✅ Specified |
| §3.4 Spin Vector Calculation | `SpinVectorCalculator.cs` | `spinVector: Vector3` (rad/s) | ⬜ Part 2 |
| §3.5 Placement Resolution | `PlacementResolver.cs` | `aimDirection: Vector3` (unit) | ⬜ Part 2 |
| §3.6 Error Model | `ErrorCalculator.cs` | `finalDirection: Vector3` (unit) | ⬜ Part 2 |
| §3.7 Body Mechanics Evaluation | `BodyMechanicsEvaluator.cs` | `BodyMechanicsScore`, `BodyLeanAngle`, stumble trigger | ⬜ Part 2 |
| §3.8 Weak Foot Penalty | `WeakFootPenaltyApplier.cs` | `WeakFootModifier` scalar | ⬜ Part 2 |
| §3.9 Shot State Machine | `ShotStateMachine.cs` | Lifecycle management | ⬜ Part 2 |
| §3.10 Event Publishing | `ShotEventEmitter.cs` | `ShotExecutedEvent`, `ShotCancelledEvent` | ⬜ Part 2 |

**Critical validation required before approval:**
- Appendix B numerical verification: all boundary verification table rows must be
  computed against Ball Physics drag model simulation
- §3.2 V_BASE: verify that shots reach realistic field distance at each velocity
- §3.3 BaseAngles: verify trajectory arcs against StatsBomb shot height distribution data

**Next:** Section 3 Part 2 — §3.4 through §3.10.

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 22, 2026, 11:45 PM PST | Claude (AI) / Anton | Initial draft. §3.1–§3.3 fully specified. |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | Cross-spec audit correction: (A3) VR-01 possession check updated from `BallState.PossessingAgentId` to `AgentSystem.GetPossessor()` — ERR-008 Option B; no `PossessingAgentId` field exists on `BallState`. VR-01 note updated to match. |
| 1.2 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Version header corrected 1.0→1.1 to match filename. (2) §3.1.2 output reference corrected from '§3.7' to '§3.2 (CONTACT phase)'. (3) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. |

---

*End of Section 3 (Part 1) — Shot Mechanics Specification #6*
*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
