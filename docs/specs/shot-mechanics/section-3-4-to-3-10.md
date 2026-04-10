# Shot Mechanics Specification #6 — Section 3 (Part 2): Technical Specifications §3.4–§3.10

**File:** `Shot_Mechanics_Spec_Section_3_4_to_3_10_v1_2.md`
**Purpose:** Defines the authoritative technical implementation for seven Shot Mechanics
sub-systems: Spin Vector Calculation (§3.4), Placement Resolution (§3.5), Error Model
(§3.6), Body Mechanics Evaluation (§3.7), Weak Foot Penalty (§3.8), Shot State Machine
(§3.9), and Event Publishing (§3.10). These seven sub-systems complete the execution
pipeline after velocity and launch angle have been computed. Every formula, constant,
and failure mode is specified to implementation-ready detail. This file is the primary
reference for `SpinVectorCalculator.cs`, `PlacementResolver.cs`, `ErrorCalculator.cs`,
`BodyMechanicsEvaluator.cs`, `WeakFootPenaltyApplier.cs`, `ShotStateMachine.cs`, and
`ShotEventEmitter.cs`.

**Created:** February 22, 2026, 11:59 PM PST
**Version:** 1.2
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisites confirmed:**
- Shot Mechanics Outline v1.2 (approved — ShotType enum eliminated, all OIs resolved)
- Shot Mechanics Section 1 v1.1 (scope, KDs 1–7 locked)
- Shot Mechanics Section 2 v1.0 (data structures, FR-01–FR-11, 13-step pipeline)
- Shot Mechanics Section 3 Part 1 v1.1 (§3.1–§3.3 specified — ShotValidator,
  VelocityCalculator, LaunchAngleCalculator)
- Ball Physics Spec #1 (approved) — Ball.ApplyKick() and Magnus model confirmed stable
- Agent Movement Spec #2 (approved) — AgentPhysicalProperties confirmed in §3.5.4;
  STUMBLING signal confirmed in §3.1 hysteresis
- Collision System Spec #3 (approved) — tackle interrupt flag interface confirmed
- Pass Mechanics Spec #5 (approved) — weak foot formula and state machine patterns reused directly

**Dependency Flags:** None. All hard dependencies confirmed stable.

**Continuing conventions from Part 1:**
- **[GT]** — gameplay-tunable constant; stored in `ShotConstants.cs`, never hardcoded
- **[EST]** — estimate derived from literature or StatsBomb data; requires validation
- **[VER]** — requires numerical verification against Ball Physics simulation (Appendix B)

---

## Table of Contents

- [3.4 Spin Vector Calculation](#34-spin-vector-calculation)
  - [3.4.1 Responsibilities and Scope](#341-responsibilities-and-scope)
  - [3.4.2 Inputs and Outputs](#342-inputs-and-outputs)
  - [3.4.3 Spin Axis Conventions](#343-spin-axis-conventions)
  - [3.4.4 Topspin Component](#344-topspin-component)
  - [3.4.5 Backspin Component](#345-backspin-component)
  - [3.4.6 Sidespin Component](#346-sidespin-component)
  - [3.4.7 Technique Modulation of Sidespin](#347-technique-modulation-of-sidespin)
  - [3.4.8 Spin–Power Interaction](#348-spinpower-interaction)
  - [3.4.9 Full Spin Vector Assembly](#349-full-spin-vector-assembly)
  - [3.4.10 Spin Magnitude Clamping](#3410-spin-magnitude-clamping)
  - [3.4.11 Boundary Verification Table](#3411-boundary-verification-table)
  - [3.4.12 Constants Reference](#3412-constants-reference)
  - [3.4.13 Failure Modes](#3413-failure-modes)
  - [3.4.14 Design Decisions and Rationale](#3414-design-decisions-and-rationale)
  - [3.4.15 Cross-Specification Dependencies](#3415-cross-specification-dependencies)
- [3.5 Placement Resolution](#35-placement-resolution)
  - [3.5.1 Responsibilities and Scope](#351-responsibilities-and-scope)
  - [3.5.2 Inputs and Outputs](#352-inputs-and-outputs)
  - [3.5.3 Goal Geometry Constants](#353-goal-geometry-constants)
  - [3.5.4 Goal Mouth World-Space Coordinates](#354-goal-mouth-world-space-coordinates)
  - [3.5.5 Target Point Derivation](#355-target-point-derivation)
  - [3.5.6 Aim Direction Derivation](#356-aim-direction-derivation)
  - [3.5.7 Launch Angle Encoding](#357-launch-angle-encoding)
  - [3.5.8 Boundary Cases](#358-boundary-cases)
  - [3.5.9 Constants Reference](#359-constants-reference)
  - [3.5.10 Failure Modes](#3510-failure-modes)
  - [3.5.11 Design Decisions and Rationale](#3511-design-decisions-and-rationale)
  - [3.5.12 Cross-Specification Dependencies](#3512-cross-specification-dependencies)
- [3.6 Error Model](#36-error-model)
  - [3.6.1 Responsibilities and Scope](#361-responsibilities-and-scope)
  - [3.6.2 Inputs and Outputs](#362-inputs-and-outputs)
  - [3.6.3 Base Error Angle — Attribute Derivation](#363-base-error-angle--attribute-derivation)
  - [3.6.4 Power Penalty Scalar](#364-power-penalty-scalar)
  - [3.6.5 Pressure Scalar](#365-pressure-scalar)
  - [3.6.6 Fatigue Scalar](#366-fatigue-scalar)
  - [3.6.7 Body Shape Scalar](#367-body-shape-scalar)
  - [3.6.8 Full Error Magnitude Formula](#368-full-error-magnitude-formula)
  - [3.6.9 Deterministic Error Direction](#369-deterministic-error-direction)
  - [3.6.10 Error Application in Goal-Relative Space](#3610-error-application-in-goal-relative-space)
  - [3.6.11 Error Clamping Policy](#3611-error-clamping-policy)
  - [3.6.12 Boundary Verification Table](#3612-boundary-verification-table)
  - [3.6.13 Constants Reference](#3613-constants-reference)
  - [3.6.14 Failure Modes](#3614-failure-modes)
  - [3.6.15 Design Decisions and Rationale](#3615-design-decisions-and-rationale)
  - [3.6.16 Cross-Specification Dependencies](#3616-cross-specification-dependencies)
- [3.7 Body Mechanics Evaluation](#37-body-mechanics-evaluation)
  - [3.7.1 Responsibilities and Scope](#371-responsibilities-and-scope)
  - [3.7.2 Inputs and Outputs](#372-inputs-and-outputs)
  - [3.7.3 Run-Up Angle Score](#373-run-up-angle-score)
  - [3.7.4 Plant Foot Offset Score](#374-plant-foot-offset-score)
  - [3.7.5 Agent Velocity at Contact Score](#375-agent-velocity-at-contact-score)
  - [3.7.6 Body Lean Measurement](#376-body-lean-measurement)
  - [3.7.7 Composite BodyMechanicsScore](#377-composite-bodymechanicsscore)
  - [3.7.8 ContactQualityModifier Derivation](#378-contactqualitymodifier-derivation)
  - [3.7.9 Stumble Trigger Condition](#379-stumble-trigger-condition)
  - [3.7.10 Boundary Verification Table](#3710-boundary-verification-table)
  - [3.7.11 Constants Reference](#3711-constants-reference)
  - [3.7.12 Failure Modes](#3712-failure-modes)
  - [3.7.13 Design Decisions and Rationale](#3713-design-decisions-and-rationale)
  - [3.7.14 Cross-Specification Dependencies](#3714-cross-specification-dependencies)
- [3.8 Weak Foot Penalty](#38-weak-foot-penalty)
  - [3.8.1 Responsibilities and Scope](#381-responsibilities-and-scope)
  - [3.8.2 Inputs and Outputs](#382-inputs-and-outputs)
  - [3.8.3 Error Cone Multiplier Formula](#383-error-cone-multiplier-formula)
  - [3.8.4 Velocity Reduction Formula](#384-velocity-reduction-formula)
  - [3.8.5 Shot vs. Pass Penalty Differential](#385-shot-vs-pass-penalty-differential)
  - [3.8.6 Boundary Verification Table](#386-boundary-verification-table)
  - [3.8.7 Constants Reference](#387-constants-reference)
  - [3.8.8 Failure Modes](#388-failure-modes)
  - [3.8.9 Design Decisions and Rationale](#389-design-decisions-and-rationale)
- [3.9 Shot State Machine](#39-shot-state-machine)
  - [3.9.1 Responsibilities and Scope](#391-responsibilities-and-scope)
  - [3.9.2 State Definitions](#392-state-definitions)
  - [3.9.3 Windup Duration Formula](#393-windup-duration-formula)
  - [3.9.4 Follow-Through Duration](#394-follow-through-duration)
  - [3.9.5 Tackle Interrupt Handling](#395-tackle-interrupt-handling)
  - [3.9.6 Stumble Transition](#396-stumble-transition)
  - [3.9.7 State Diagram](#397-state-diagram)
  - [3.9.8 Per-Frame Execution Guard](#398-per-frame-execution-guard)
  - [3.9.9 Constants Reference](#399-constants-reference)
  - [3.9.10 Failure Modes](#3910-failure-modes)
  - [3.9.11 Design Decisions and Rationale](#3911-design-decisions-and-rationale)
- [3.10 Event Publishing](#310-event-publishing)
  - [3.10.1 Responsibilities and Scope](#3101-responsibilities-and-scope)
  - [3.10.2 Events Published](#3102-events-published)
  - [3.10.3 ShotExecutedEvent — Field Population](#3103-shotexecutedevent--field-population)
  - [3.10.4 ShotCancelledEvent — Field Population](#3104-shotcancelledevent--field-population)
  - [3.10.5 ShotAnimationData — Stub Population](#3105-shotanimationdata--stub-population)
  - [3.10.6 Publication Timing and Ordering](#3106-publication-timing-and-ordering)
  - [3.10.7 Event Bus Capacity Contract](#3107-event-bus-capacity-contract)
  - [3.10.8 Failure Modes](#3108-failure-modes)
  - [3.10.9 Design Decisions and Rationale](#3109-design-decisions-and-rationale)
- [Section 3 (Part 2) Summary](#section-3-part-2-summary)
- [Full Section 3 Summary — §3.1 through §3.10](#full-section-3-summary--31-through-310)

---

## 3.4 Spin Vector Calculation

### 3.4.1 Responsibilities and Scope

§3.4 (SpinVectorCalculator) computes the **spin `Vector3`** (rad/s) encoding topspin,
sidespin, and backspin components for a committed shot. It is called once per shot
execution at CONTACT state, after §3.3 (LaunchAngleCalculator) and alongside §3.5
(PlacementResolver). The output spin vector is passed directly to `Ball.ApplyKick()`.

Spin is the mechanism by which shot trajectories diverge beyond simple projectile
physics. Topspin creates dipping driven shots. Backspin creates lofting chip trajectories
that check on landing. Sidespin creates curl — the primary tool of finesse strikes. The
separation between a visually inert simulation and one that looks like real football
depends significantly on this sub-system producing physically plausible spin vectors.

§3.4 **does**:
- Compute topspin, backspin, and sidespin scalar magnitudes from `ContactZone`,
  `SpinIntent`, and `PowerIntent`
- Modulate sidespin magnitude using the agent's `Technique` attribute
- Encode all three components into a world-space `Vector3` (signed axis convention below)
- Clamp total spin magnitude to `SPIN_ABSOLUTE_MAX`
- Guard against NaN/Infinity in all component calculations

§3.4 **does not**:
- Compute launch angle (§3.3)
- Apply spin to ball state (Ball Physics §3.1.4 owns Magnus effect)
- Determine the direction of sidespin (left vs. right curl); this is determined by
  `PlacementTarget.u` asymmetry in §3.5 — sidespin axis is aligned to aim direction
- Apply any weak foot modification to spin (§3.8 applies weak foot penalty to error and
  velocity only; spin is already degraded implicitly via ContactQualityModifier in §3.2)

**Position in pipeline:** §3.4 executes sixth in the evaluation chain. See §2.2.3 for
the full 13-step pipeline.

---

### 3.4.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `request.ContactZone` | ShotRequest | ContactZone enum | {Centre, BelowCentre, OffCentre} | Pre-validated |
| `request.SpinIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated |
| `attributes.Technique` | PlayerAttributes | int | [1, 20] | Agent Movement §3.5.6 |

**Output:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `spinVector` | Ball.ApplyKick() spin parameter | Vector3 (rad/s) | Magnitude ∈ [0, SPIN_ABSOLUTE_MAX] |

---

### 3.4.3 Spin Axis Conventions

Spin is encoded as a world-space angular velocity vector. The axis direction encodes the
rotation axis; the magnitude encodes rotation rate in rad/s.

Following the right-hand rule in a standard Unity-style Y-up world:

| Spin Type | Axis | Physical Effect in Ball Physics |
|---|---|---|
| **Topspin** | `−X` (ball rotating forward over top) | Ball curves downward; dips faster than drag alone [INOUE-2014] |
| **Backspin** | `+X` (ball rotating backward under belly) | Ball floats; decelerates more rapidly; checks hard on landing |
| **Sidespin (right curl)** | `+Y` | Ball curves right (from shooter's perspective) |
| **Sidespin (left curl)** | `−Y` | Ball curves left |

> **Note:** The X/Y axis assignments above assume a coordinate system where the shooter
> faces the +Z direction (toward goal). The final spin vector must be rotated from
> shooter-local space to world space using the agent's forward direction at contact.
> This rotation is applied at the end of §3.4.9 (Full Spin Vector Assembly).

---

### 3.4.4 Topspin Component

Topspin is the dominant spin type for Centre contact with high `PowerIntent`. It produces
the characteristic dipping driven shot seen at velocities above ~25 m/s.

```
TopspinMagnitude = TOPSPIN_BASE[ContactZone] × (1.0 − SpinIntent) × PowerIntent
```

Where `TOPSPIN_BASE` values by zone [GT]:

| ContactZone | TOPSPIN_BASE (rad/s) | Physical Rationale |
|---|---|---|
| `Centre` | 25.0 | Full instep contact generates forward rotation naturally |
| `BelowCentre` | 4.0 | Undercutting the ball suppresses topspin almost entirely |
| `OffCentre` | 8.0 | Sidespin-dominant; topspin is secondary |

**Formula rationale:**
- `(1.0 − SpinIntent)`: maximum topspin occurs at zero spin intent (pure power drive);
  increasing `SpinIntent` redirects effort toward deliberate sidespin or backspin
- `PowerIntent`: topspin scales with kick effort; gentle touches do not create fast rotation
- Literature: Inoue et al. (2014) measured 60–150 rad/s for driven shots at elite level;
  `TOPSPIN_BASE[Centre] × PowerIntent_max = 25 rad/s` [EST] is conservative and sits below
  the lower bound, requiring [VER] cross-check against Ball Physics Magnus parameters to
  confirm the simulation produces observable trajectory dip within the expected distance

---

### 3.4.5 Backspin Component

Backspin is the dominant spin type for `BelowCentre` contact with high `SpinIntent`.
It creates chipped-style trajectories with a distinctive floating quality and hard landing check.

```
BackspinMagnitude = BACKSPIN_BASE[ContactZone] × SpinIntent
```

Where `BACKSPIN_BASE` values by zone [GT]:

| ContactZone | BACKSPIN_BASE (rad/s) | Physical Rationale |
|---|---|---|
| `Centre` | 2.0 | Negligible; Centre contact cannot generate meaningful backspin |
| `BelowCentre` | 30.0 | Primary spin for chip/loft; undercutting ball maximises backspin |
| `OffCentre` | 6.0 | Minor; off-centre is sidespin-dominant |

**Formula rationale:**
- `SpinIntent` is the sole driver: backspin is a deliberate technical choice, not a
  consequence of power. A striker chipping the keeper at low power applies high backspin
  by choice; a striker blasting a BelowCentre shot with SpinIntent = 0 generates minimal backspin
- Note the deliberate asymmetry: BackspinMagnitude has no `PowerIntent` term, while
  TopspinMagnitude does. This reflects the physical distinction between driven contact
  (power rotates the ball forward) and deliberate chip contact (technique controls spin
  direction regardless of power)

---

### 3.4.6 Sidespin Component

Sidespin creates the curl trajectory that distinguishes finesse shots. It is dominant for
`OffCentre` contact with high `SpinIntent` and is modulated by `Technique` (§3.4.7).

```
SidespinMagnitude = SIDESPIN_BASE[ContactZone] × SpinIntent × TechniqueScale
```

Where `SIDESPIN_BASE` values by zone [GT]:

| ContactZone | SIDESPIN_BASE (rad/s) | Physical Rationale |
|---|---|---|
| `Centre` | 3.0 | Minimal; symmetric instep contact cannot generate significant sidespin |
| `BelowCentre` | 5.0 | Low; chip contact is backspin-optimised |
| `OffCentre` | 28.0 | Primary spin for curl shots; off-centre wrap generates strong rotation |

**Sidespin curl direction:**
The sign of the sidespin Y-component is determined by the horizontal component of the
aim direction relative to the shooter's forward vector. If the intended aim direction
has a leftward deviation from the agent's forward axis, sidespin is applied as `−Y`
(curl right, creating an inswing from the shooter's perspective to that target).
If rightward, sidespin is `+Y`. This ensures the curl creates a natural inswing rather
than a physically implausible ball that curves away from its intended destination.

> **Important:** Sidespin curl direction must be verified against Ball Physics Magnus
> model (Ball Physics §3.1.4) to confirm the handedness convention is consistent.
> Mark as [VER] — required before approval.

---

### 3.4.7 Technique Modulation of Sidespin

`Technique` is the only attribute that modulates spin. Its effect is exclusive to
sidespin — topspin and backspin are physical consequences of contact geometry and intent,
not technique. Sidespin requires deliberate wrapping of the foot around the ball, a skill
that separates technically gifted players from average ones.

```
TechniqueScale = TECHNIQUE_SPIN_BASE + (Technique − 1) / (ATTR_MAX − 1)
                 × (TECHNIQUE_SPIN_MAX − TECHNIQUE_SPIN_BASE)
```

Where:
- `TECHNIQUE_SPIN_BASE` = 0.6 [GT] — minimum sidespin scale (Technique = 1, worst)
- `TECHNIQUE_SPIN_MAX`  = 1.0 [GT] — maximum sidespin scale (Technique = 20, best)
- `ATTR_MAX`            = 20 (consistent with all attribute ranges in Agent Movement)

**Boundary checks:**
- `Technique = 1`:  `TechniqueScale = 0.6` → SidespinMagnitude at 60% of SIDESPIN_BASE
- `Technique = 10`: `TechniqueScale ≈ 0.77` → SidespinMagnitude at ~77% of SIDESPIN_BASE
- `Technique = 20`: `TechniqueScale = 1.0` → SidespinMagnitude at full SIDESPIN_BASE

**Rationale:** The 0.6 floor prevents poor-technique players from producing zero curl even
when `SpinIntent = 1.0` and `ContactZone = OffCentre` — some spin is always generated by
off-centre contact regardless of technique quality. The 0.4 range between floor and ceiling
produces a meaningful but not exaggerated difference between low-Technique and
high-Technique curlers. [VER] — validate that OffCentre / Technique = 20 curl radius
through Ball Physics Magnus model is visually plausible within a standard pitch width.

---

### 3.4.8 Spin–Power Interaction

For Centre contact, topspin and power are coupled: maximum power at Centre generates the
highest topspin. However, there is a secondary interaction: very high SpinIntent with
Centre contact partially suppresses topspin in favour of deliberate sidespin, allowing a
technically gifted player to add curl to what would otherwise be a pure power shot.

This is already captured implicitly in the formulas:
- `TopspinMagnitude` decreases as `SpinIntent` increases (via the `1 − SpinIntent` term)
- `SidespinMagnitude` increases as `SpinIntent` increases (via the `SpinIntent` term)

No additional interaction term is required. The formulas as written produce the correct
emergent behaviour: Centre + high SpinIntent + high Technique = a powerful curl shot
with reduced topspin.

---

### 3.4.9 Full Spin Vector Assembly

After computing the three component magnitudes, they are assembled into a shooter-local
spin vector, then rotated to world space.

```
// Step 1: Determine sidespin sign from aim direction
float aimHorizontalDeviation = Vector3.SignedAngle(agentForward, aimDirection_horizontal, Vector3.up);
float sidespinSign = (aimHorizontalDeviation >= 0.0f) ? 1.0f : −1.0f;

// Step 2: Build shooter-local spin vector
// Topspin: −X (forward rotation); Backspin: +X; Sidespin: ±Y
Vector3 spinLocal = new Vector3(
    BackspinMagnitude − TopspinMagnitude,  // X axis: positive = backspin, negative = topspin
    SidespinMagnitude × sidespinSign,       // Y axis: ± based on curl direction
    0.0f                                    // Z axis: unused
);

// Step 3: Rotate to world space using agent's orientation at contact
Quaternion agentRotation = Quaternion.LookRotation(agentForward, Vector3.up);
Vector3 spinWorld = agentRotation * spinLocal;

// Step 4: Clamp total spin magnitude (§3.4.10)
spinWorld = ClampSpinMagnitude(spinWorld, SPIN_ABSOLUTE_MAX);
```

**NaN Guard:** Before rotation, all three magnitude values are validated as finite.
If any component is NaN or Infinity, the component is zeroed and a warning is logged.
The shot proceeds with reduced spin rather than aborting (spin = 0 is a valid physical state).

---

### 3.4.10 Spin Magnitude Clamping

```
float totalMagnitude = spinWorld.magnitude;
if (totalMagnitude > SPIN_ABSOLUTE_MAX)
    spinWorld = spinWorld * (SPIN_ABSOLUTE_MAX / totalMagnitude);
```

`SPIN_ABSOLUTE_MAX` = **80.0 rad/s** [VER] — the Ball Physics hard limit defined in
`BallPhysicsConstants.Limits.MAX_SPIN` (Ball Physics §3.1.12). Ball Physics silently
clamps any spin above 80.0 rad/s inside `ApplyKick()`. Passing values above this limit
does not produce an error but yields unexpected Magnus behaviour (the simulation clamps
without informing the caller). Shot Mechanics therefore clamps at source.

> **Note on academic literature:** Inoue et al. (2014) measures spin up to 150 rad/s for
> elite kicked footballs. That upper bound applies to physical reality; the simulation
> constraint is 80.0 rad/s. The two values are deliberately different — 80.0 rad/s is
> a Ball Physics design choice, not a literature derivation. Marked `[VER]` (verified
> against Ball Physics §3.1.12) rather than `[EST]` (literature-estimated).

> **Note on formula constants:** All component base values (TOPSPIN_BASE, BACKSPIN_BASE,
> SIDESPIN_BASE) sit well below the 80.0 rad/s ceiling at their nominal values. The
> clamp remains a safety net against [GT] tuning that pushes components toward the ceiling.

---

### 3.4.11 Boundary Verification Table

| Scenario | ContactZone | SpinIntent | PowerIntent | Technique | Expected Result |
|---|---|---|---|---|---|
| Pure driven shot | Centre | 0.0 | 1.0 | any | Topspin dominant; sidespin ≈ 0; backspin ≈ 0 |
| Tap-in | Centre | 0.0 | 0.2 | any | Very low topspin; near-zero other components |
| Chip | BelowCentre | 1.0 | 0.5 | any | Backspin dominant; near-zero topspin |
| Curl (expert) | OffCentre | 1.0 | 0.7 | 20 | Sidespin dominant; > sidespin at Technique = 10 |
| Curl (novice) | OffCentre | 1.0 | 0.7 | 1 | Sidespin at 60% of expert value |
| Power + spin | Centre | 0.8 | 0.9 | 15 | Partial topspin suppression; moderate sidespin |
| Max shot | any | 1.0 | 1.0 | 20 | Total magnitude must not exceed SPIN_ABSOLUTE_MAX |

---

### 3.4.12 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `TOPSPIN_BASE[Centre]` | 25.0 rad/s | [EST] [GT] | Inoue et al. (2014) lower bound; conservative |
| `TOPSPIN_BASE[BelowCentre]` | 4.0 rad/s | [GT] | Design authority |
| `TOPSPIN_BASE[OffCentre]` | 8.0 rad/s | [GT] | Design authority |
| `BACKSPIN_BASE[Centre]` | 2.0 rad/s | [GT] | Design authority |
| `BACKSPIN_BASE[BelowCentre]` | 30.0 rad/s | [EST] [GT] | Derived from chip trajectory requirements |
| `BACKSPIN_BASE[OffCentre]` | 6.0 rad/s | [GT] | Design authority |
| `SIDESPIN_BASE[Centre]` | 3.0 rad/s | [GT] | Design authority |
| `SIDESPIN_BASE[BelowCentre]` | 5.0 rad/s | [GT] | Design authority |
| `SIDESPIN_BASE[OffCentre]` | 28.0 rad/s | [EST] [GT] | Asai et al. (2002); finesse shots |
| `TECHNIQUE_SPIN_BASE` | 0.6 | [GT] | Design authority |
| `TECHNIQUE_SPIN_MAX` | 1.0 | [GT] | Design authority |
| `ATTR_MAX` | 20 | Design authority | Agent Movement §3.5.6 |
| `SPIN_ABSOLUTE_MAX` | 80.0 rad/s | [VER] | Ball Physics §3.1.12 `MAX_SPIN` — simulation hard limit |

---

### 3.4.13 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.4-01 | NaN in any spin component | `float.IsNaN()` check before vector assembly | Zero the component; log warning; continue |
| FM-3.4-02 | Infinity in any spin component | `float.IsInfinity()` check before assembly | Zero the component; log warning; continue |
| FM-3.4-03 | Total spin magnitude > SPIN_ABSOLUTE_MAX | Post-assembly magnitude check | Normalise vector to SPIN_ABSOLUTE_MAX (§3.4.10) |
| FM-3.4-04 | Invalid ContactZone value | Enum range check | Log error; return zero spin Vector3; shot proceeds |

---

### 3.4.14 Design Decisions and Rationale

**DD-3.4-01: Three discrete ContactZone base values rather than a continuous contact model**

A continuous model (e.g., contact point as a 2D UV coordinate on the ball surface) would
be more physically accurate but produces no gameplay benefit at Stage 0. The Decision Tree
already reduces intent to three zones. A three-value lookup table is deterministic,
performance-negligible, and trivially auditable. Full contact-point modelling is deferred
to Stage 1+ (KD-7).

**DD-3.4-02: Technique exclusively modulates sidespin, not topspin or backspin**

Topspin and backspin are consequences of where the foot contacts the ball (Centre vs.
BelowCentre) — they do not require technique beyond making the intended contact. Sidespin
requires the player to deliberately wrap their foot around the ball, which is a skill
that separates technically gifted players. Applying Technique to all three spin components
would blur the physical distinction and reduce the strategic value of Technique as an
attribute. Players with high Technique should be meaningfully better at curl, not globally
better at all spin types.

**DD-3.4-03: Sidespin sign is derived from aim direction, not from a ShotRequest field**

Requiring the Decision Tree to specify sidespin direction explicitly would be a leaky
abstraction — the Decision Tree thinks in goal-relative terms (aim for bottom-left corner),
not in angular velocity signs. The sign is deterministically derivable from the horizontal
component of the aim direction. No information is lost.

---

### 3.4.15 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `ContactZone` enum | Shot Mechanics §2.4.1 | Three values: Centre, BelowCentre, OffCentre |
| `PlayerAttributes.Technique` | Agent Movement #2 §3.5.6 | Confirmed present; range [1, 20] |
| Magnus force model | Ball Physics #1 §3.1.4 | Spin vector consumed; [VER] required |
| `agentForward` direction at contact | Agent Movement #2 §3.5.4 | Frozen at INITIATING (§3.7) |

---

## 3.5 Placement Resolution

### 3.5.1 Responsibilities and Scope

§3.5 (PlacementResolver) converts the `PlacementTarget (u, v)` goal-relative coordinate
from the `ShotRequest` into a **world-space aim direction unit vector**. This aim direction
is then used in §3.6 (ErrorCalculator) to apply angular error, and subsequently combined
with the scalar kick speed from §3.2 and the launch angle from §3.3 to construct the
final `velocity: Vector3` for `Ball.ApplyKick()`.

§3.5 **does**:
- Read goal geometry from Match Config (post positions, crossbar height)
- Convert `(u, v)` normalised goal-mouth coordinates to a world-space 3D target point
- Compute a unit aim direction vector from agent position to target point
- Apply the launch angle from §3.3 by rotating the horizontal aim direction upward by
  `launchAngleDeg`
- Output a fully specified 3D unit direction vector

§3.5 **does not**:
- Apply error to the aim direction (§3.6 does this, after §3.5)
- Determine where the goal mouth is (Match Config owns goal positions)
- Clamp the PlacementTarget to valid range (§3.1 validation already guarantees this)
- Account for ball diameter in target point calculation (Stage 0 simplification — the
  target is a point on the goal mouth plane, not a contact zone on a physical crossbar)

**Position in pipeline:** §3.5 executes seventh, after §3.4 (SpinVectorCalculator).

---

### 3.5.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range / Notes |
|-------|--------|------|---------------|
| `request.PlacementTarget` | ShotRequest | Vector2 | `u, v ∈ [0.0, 1.0]`; pre-validated by §3.1 |
| `agentPosition` | AgentState (frozen at INITIATING) | Vector3 | World space |
| `goalMouthOrigin` | Match Config | Vector3 | Left post, ground level |
| `goalWidth` | Match Config | float | 7.32m (FIFA standard) |
| `goalHeight` | Match Config | float | 2.44m (FIFA standard) |
| `launchAngleDeg` | §3.3 LaunchAngleCalculator | float | [−5°, 70°] |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `aimDirection` | §3.6 ErrorCalculator | Vector3 (unit) | Pre-error 3D aim direction |
| `targetWorldPoint` | ShotResult.IntendedDirection (debug) | Vector3 | World-space target on goal mouth |

---

### 3.5.3 Goal Geometry Constants

Goal dimensions follow FIFA Law 1 (The Field of Play):

| Constant | Value | Source |
|---|---|---|
| `GOAL_WIDTH` | 7.32 m | FIFA Law 1 — fixed; not [GT] |
| `GOAL_HEIGHT` | 2.44 m | FIFA Law 1 — fixed; not [GT] |

These are not tunable constants — deviating from FIFA dimensions would break match
realism. They are read from Match Config at initialisation, not from `ShotConstants.cs`.

---

### 3.5.4 Goal Mouth World-Space Coordinates

Goal mouth corners are defined in world space. The coordinate assignments depend on
which goal is being targeted (Match Config provides the correct goal origin per team).

```
// goalMouthOrigin = world-space position of the LEFT post, at ground level
// (From the shooter's perspective facing the goal; consistent with u=0 meaning left post)

Vector3 leftPostGround  = goalMouthOrigin;
Vector3 rightPostGround = goalMouthOrigin + goalRightward * GOAL_WIDTH;
Vector3 leftPostTop     = goalMouthOrigin + Vector3.up * GOAL_HEIGHT;
Vector3 rightPostTop    = goalMouthOrigin + goalRightward * GOAL_WIDTH + Vector3.up * GOAL_HEIGHT;
```

Where `goalRightward` is the horizontal unit vector pointing from left post to right post
(from the shooter's facing direction). This is provided by Match Config and is invariant
for the duration of a match half.

---

### 3.5.5 Target Point Derivation

```
// Bilinear interpolation across the goal mouth plane
// u = 0 → left post, u = 1 → right post
// v = 0 → ground, v = 1 → crossbar

Vector3 targetWorldPoint =
    goalMouthOrigin
    + goalRightward × (request.PlacementTarget.x × GOAL_WIDTH)        // u interpolation (horizontal)
    + Vector3.up    × (request.PlacementTarget.y × GOAL_HEIGHT);       // v interpolation (vertical)
```

**Example verification:**
- `(0.5, 0.5)` → centre of goal mouth → `goalMouthOrigin + goalRightward × 3.66 + up × 1.22`
- `(0.05, 0.05)` → bottom-left corner → approximately 0.366m from left post, 0.122m from ground
- `(0.95, 0.95)` → top-right corner → approximately 0.366m from right post, 0.122m from crossbar

---

### 3.5.6 Aim Direction Derivation

```
// Step 1: Horizontal aim vector (ignoring vertical component)
Vector3 toTarget    = targetWorldPoint − agentPosition;
Vector3 horizontal  = new Vector3(toTarget.x, 0.0f, toTarget.z);
float   distance    = horizontal.magnitude;

// Guard: avoid division by zero if agent is standing on the goal line (degenerate case)
if (distance < 0.001f)
{
    // Agent is at goal mouth — aim directly forward along agent's facing direction
    horizontal = agentForward;
    distance   = 1.0f;
    Log.Warning("PlacementResolver: Agent at goal mouth — degenerate case; aim direction defaulted.");
}

Vector3 horizontalUnit = horizontal / distance;

// Step 2: Apply launch angle — tilt the horizontal unit vector upward by launchAngleDeg
float launchRad    = launchAngleDeg × Mathf.Deg2Rad;
Vector3 aimDirection = new Vector3(
    horizontalUnit.x * Mathf.Cos(launchRad),
    Mathf.Sin(launchRad),
    horizontalUnit.z * Mathf.Cos(launchRad)
).normalized;  // Re-normalise after trigonometric operation
```

The final `aimDirection` is a unit vector in 3D world space pointing from the agent's
contact point toward the intended target on the goal mouth, at the correct elevation angle.

---

### 3.5.7 Launch Angle Encoding

The launch angle is encoded into the aim direction by tilting the horizontal unit vector
by `launchAngleDeg` in the vertical plane defined by the aim direction.

**This approach is correct because:**
- The vertical plane of the shot (the "plane of kick") is fully specified by the
  horizontal aim direction and the vertical axis
- Tilting within this plane by `launchAngleDeg` correctly encodes elevation without
  introducing spurious off-plane components
- The resulting `aimDirection` and `kickSpeed` (from §3.2) combine via
  `velocity = aimDirection × kickSpeed` to produce the `velocity: Vector3` for `Ball.ApplyKick()`

After error application in §3.6, the final velocity is:
```
finalVelocity = finalDirection × kickSpeed
```
Where `finalDirection` is the post-error version of `aimDirection`.

---

### 3.5.8 Boundary Cases

| Case | Input | Handling |
|---|---|---|
| Agent at goal line | `distance < 0.001f` | Default to agent forward vector; log warning |
| `launchAngleDeg = 70°` | Max allowed | `Sin(70°) ≈ 0.94` — nearly vertical shot; valid |
| `launchAngleDeg = −5°` | Min allowed | `Sin(−5°) ≈ −0.087` — slightly downward; valid (grounded hard shot) |
| `PlacementTarget = (0.0, 0.0)` | Left post, ground | Aim at exact left post base; valid extreme |
| `PlacementTarget = (1.0, 1.0)` | Right post, crossbar junction | Aim at exact top-right corner; valid extreme |

---

### 3.5.9 Constants Reference

| Constant | Value | Source | Tunable? |
|---|---|---|---|
| `GOAL_WIDTH` | 7.32 m | FIFA Law 1 | No |
| `GOAL_HEIGHT` | 2.44 m | FIFA Law 1 | No |
| Degenerate distance threshold | 0.001 m | Design authority | No |

---

### 3.5.10 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.5-01 | Agent on goal line (degenerate geometry) | `distance < 0.001f` | Use agent forward; log warning; continue |
| FM-3.5-02 | NaN in `aimDirection` after normalisation | `float.IsNaN()` on any component | Log error; use agent forward as fallback; continue |
| FM-3.5-03 | Match Config returns null goal origin | Null check before bilinear interpolation | Log critical error; return ShotOutcome.Invalid |

---

### 3.5.11 Design Decisions and Rationale

**DD-3.5-01: PlacementTarget is goal-relative, not world-space (KD-2 enforcement)**

This is KD-2 from Section 1 — the Decision Tree specifies placement in goal-relative
terms, and §3.5 converts to world-space at execution time. The justification is that
goal-relative coordinates make error interpretation intuitive (positive v-error = skied),
clean the Decision Tree API (no world-space geometry knowledge required), and produce
a stable contract regardless of which end the team is attacking.

**DD-3.5-02: Launch angle encoded into the aim direction vector, not kept separate**

Keeping launch angle as a separate float through the pipeline would require every
downstream consumer to combine it with the horizontal direction. Encoding it into
`aimDirection` at §3.5 produces a fully specified 3D unit direction that §3.6 error
application and the final `Ball.ApplyKick()` call consume directly. This simplifies the
pipeline and removes a class of coordinate-combination bugs.

---

### 3.5.12 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| Goal geometry (post positions, dimensions) | Match Config (Stage 0 setup) | `goalMouthOrigin`, `goalRightward`, FIFA dimensions |
| `agentPosition` at contact | Agent Movement #2 §3.5.4 | Frozen at INITIATING by §3.7 |
| `agentForward` at contact | Agent Movement #2 §3.5.4 | Frozen at INITIATING; fallback for degenerate case |
| `launchAngleDeg` | §3.3 LaunchAngleCalculator | Computed prior in pipeline |

---

## 3.6 Error Model

### 3.6.1 Responsibilities and Scope

§3.6 (ErrorCalculator) computes a **deterministic angular error offset** and applies it
to the aim direction produced by §3.5 (PlacementResolver). The result is the `finalDirection`
unit vector — the actual direction the ball is kicked, incorporating all inaccuracy from
the shooter's attributes, physical state, and situation.

This is the most gameplay-critical sub-system in Shot Mechanics. It is where the gap
between player intent and execution quality is quantified. The error model determines
whether a Messi-class striker slots the ball into the corner or a career squad player
skies it into row Z under pressure.

§3.6 **does**:
- Derive a base error angle from the blended finishing/long-shots attribute (mirroring §3.2)
- Apply five multiplicative penalty scalars: PowerPenalty, PressurePenalty, FatiguePenalty,
  BodyShapePenalty, and WeakFootMultiplier
- Compute a deterministic error direction using `DeterministicHash(matchSeed, agentId, frameNumber)`
- Apply the error in goal-relative `(u, v)` space, then convert back to a 3D direction
- Clamp the final error magnitude to `[MIN_ERROR_ANGLE, MAX_ERROR_ANGLE]`

§3.6 **does not**:
- Compute the blended finishing/long-shots attribute (§3.2 computes this via the same
  sigmoid; §3.6 re-uses the same `NormalisedEffectiveAttribute` scalar passed from §3.2)
- Apply weak foot velocity penalty (§3.8 owns velocity; §3.6 receives the
  `WeakFootMultiplier` scalar as an input from §3.8)
- Compute body mechanics penalties (§3.7 computes `BodyMechanicsScore`; §3.6 receives it)

**Position in pipeline:** §3.6 executes eighth, after §3.5 (PlacementResolver) and after
§3.8 (WeakFootPenaltyApplier) has provided `WeakFootMultiplier`.

---

### 3.6.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `aimDirection` | §3.5 PlacementResolver | Vector3 (unit) | — | Pre-error 3D direction |
| `normalisedEffectiveAttribute` | §3.2 VelocityCalculator | float | [0.0, 1.0] | Same blended Finishing/LongShots; do not recompute |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated |
| `attributes.Composure` | PlayerAttributes | int | [1, 20] | Agent Movement §3.5.6 |
| `agentState.PressureRating` | AgentState | float | [0.0, 1.0] | Opponent proximity; Agent Movement §3.5.4 |
| `agentState.Fatigue` | AgentState | float | [0.0, 1.0] | 0 = fresh; 1 = exhausted |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | float | [0.0, 1.0] | Poor mechanics → more error |
| `weakFootMultiplier` | §3.8 WeakFootPenaltyApplier | float | [1.0, MAX_WF_MULTIPLIER] | 1.0 = no penalty |
| `matchSeed` | Match Config / determinism system | int | — | Fixed per match |
| `request.AgentId` | ShotRequest | int | — | Hash seed component |
| `request.FrameNumber` | ShotRequest | int | — | Hash seed component |
| `goalMouthOrigin`, `goalRightward` | Match Config | Vector3 | — | For goal-relative error application |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `finalDirection` | Velocity vector construction | Vector3 (unit) | Post-error aim direction |
| `errorOffset` | ShotResult.ErrorOffset | Vector2 | `(u, v)` offset in goal-relative space; diagnostics |
| `errorMagnitudeDeg` | ShotResult diagnostics | float | Total angular error applied (degrees) |

---

### 3.6.3 Base Error Angle — Attribute Derivation

Base error scales inversely with `normalisedEffectiveAttribute`. A world-class finisher
(attribute = 20) at the optimum distance has minimal base error; a poor shooter (attribute = 1)
has maximum base error even before situational penalties.

```
BaseErrorAngle = BASE_ERROR_MAX − (normalisedEffectiveAttribute × (BASE_ERROR_MAX − BASE_ERROR_MIN))
               = BASE_ERROR_MAX × (1.0 − normalisedEffectiveAttribute)
                 + BASE_ERROR_MIN × normalisedEffectiveAttribute
```

Where:
- `BASE_ERROR_MAX` = 4.0° [GT] — worst-case base error at attribute = 0 (normalised)
- `BASE_ERROR_MIN` = 0.5° [GT] — best-case base error at attribute = 20

**Boundary checks:**
- `normalisedEffectiveAttribute = 0.0` (attribute = 1): `BaseErrorAngle = 4.0°`
- `normalisedEffectiveAttribute = 1.0` (attribute = 20): `BaseErrorAngle = 0.5°`

**Literature calibration:** StatsBomb data for elite strikers shows on-target shot
accuracy of ~75% from central positions (10–20m). Modelling suggests BASE_ERROR_MIN
around 0.4–0.6° at that range is consistent with observed outcomes when combined with
power and pressure penalties. [EST] — requires Appendix B simulation validation.

---

### 3.6.4 Power Penalty Scalar

The power–accuracy trade-off is the central mechanic of shot quality (KD-1). Error
grows quadratically with `PowerIntent`.

```
PowerPenalty = 1.0 + POWER_PENALTY_COEFFICIENT × PowerIntent²
```

Where `POWER_PENALTY_COEFFICIENT` = 1.5 [GT].

**Boundary checks:**
- `PowerIntent = 0.0`: `PowerPenalty = 1.0` (no penalty — gentle touch)
- `PowerIntent = 0.5`: `PowerPenalty = 1.375`
- `PowerIntent = 1.0`: `PowerPenalty = 2.5` (150% error increase at maximum power)

**Rationale:** Quadratic growth is consistent with [LEES-1998] biomechanics: the
relationship between maximal muscular effort and precision loss is non-linear, with
error accelerating sharply near maximum effort. A linear power penalty would understate
the accuracy degradation at high power and overstate it at moderate power.

---

### 3.6.5 Pressure Scalar

Pressure from nearby opponents degrades composure and widens the error cone.

```
PressurePenalty = 1.0 + (agentState.PressureRating × PRESSURE_MAX_PENALTY)
                         × (1.0 − NormalisedComposure)
```

Where:
- `PRESSURE_MAX_PENALTY` = 0.8 [GT] — maximum error multiplier from pressure alone
- `NormalisedComposure = (Composure − 1) / (ATTR_MAX − 1)` — maps [1, 20] to [0.0, 1.0]

**Boundary checks:**
- `PressureRating = 0, any Composure`: `PressurePenalty = 1.0` (no opponent pressure)
- `PressureRating = 1.0, Composure = 1`: `PressurePenalty = 1.8` (+80% error)
- `PressureRating = 1.0, Composure = 20`: `PressurePenalty = 1.0` (composed player fully resists)

**Design note:** `Composure` as a modifier on pressure (not on base error) means a composed
player fully resists maximum pressure at the cost of having no base error reduction.
Composure is a reactive attribute — it matters in high-pressure situations, not in clean-through-ball
scenarios. This creates the desired tactical differentiation.

---

### 3.6.6 Fatigue Scalar

Physical fatigue degrades control across all shot parameters.

```
FatiguePenalty = 1.0 + (agentState.Fatigue × FATIGUE_MAX_PENALTY)
```

Where `FATIGUE_MAX_PENALTY` = 0.4 [GT].

**Boundary checks:**
- `Fatigue = 0.0` (fresh): `FatiguePenalty = 1.0`
- `Fatigue = 0.5` (half-exhausted): `FatiguePenalty = 1.2`
- `Fatigue = 1.0` (exhausted): `FatiguePenalty = 1.4` (+40% error)

**Note:** `FATIGUE_MAX_PENALTY` for shots (0.4) is larger than for passes (0.25 in
Pass Mechanics §3.5) because shots demand more precise contact — fatigue has a larger
effect on shooting accuracy than passing accuracy. This is consistent with match
observation: tired strikers miss more shots than tired players misplace passes.

---

### 3.6.7 Body Shape Scalar

Poor body mechanics (low `BodyMechanicsScore`) degrades placement accuracy beyond the
directional effect already captured by `BodyShapePenalty` in §3.3 (which affects launch
angle). The error model adds a horizontal accuracy penalty from poor stance.

```
BodyShapeErrorPenalty = 1.0 + BODY_SHAPE_ERROR_COEFFICIENT × (1.0 − bodyMechanicsScore)²
```

Where `BODY_SHAPE_ERROR_COEFFICIENT` = 1.0 [GT].

**Boundary checks:**
- `BodyMechanicsScore = 1.0` (perfect): `BodyShapeErrorPenalty = 1.0`
- `BodyMechanicsScore = 0.5`: `BodyShapeErrorPenalty = 1.25`
- `BodyMechanicsScore = 0.0` (catastrophic): `BodyShapeErrorPenalty = 2.0`

**Rationale:** Quadratic form mirrors the power penalty: degradation accelerates as
body mechanics worsen. Small deviations from ideal stance are forgiven; critical body
shape failures (agent off-balance, shooting across the body) are severely penalised.

---

### 3.6.8 Full Error Magnitude Formula

```
ErrorMagnitude = BaseErrorAngle
                 × PowerPenalty
                 × PressurePenalty
                 × FatiguePenalty
                 × BodyShapeErrorPenalty
                 × weakFootMultiplier
```

**Example — elite striker, clean position, moderate power:**
- `BaseErrorAngle = 0.5°` (Finishing = 20)
- `PowerPenalty = 1.375` (PowerIntent = 0.5)
- `PressurePenalty = 1.0` (no pressure)
- `FatiguePenalty = 1.0` (fresh)
- `BodyShapeErrorPenalty = 1.06` (BodyMechanicsScore = 0.77)
- `weakFootMultiplier = 1.0` (dominant foot)
- **Total: ~0.73°**

**Example — poor striker, under heavy pressure, maximum power:**
- `BaseErrorAngle = 4.0°` (Finishing = 1)
- `PowerPenalty = 2.5` (PowerIntent = 1.0)
- `PressurePenalty = 1.72` (PressureRating = 0.9, Composure = 5)
- `FatiguePenalty = 1.3` (Fatigue = 0.75)
- `BodyShapeErrorPenalty = 1.5` (BodyMechanicsScore = 0.3)
- `weakFootMultiplier = 1.60` (weak foot, Rating = 1 → penaltyFraction = 1.0 → 1.0 + 1.0 × 0.60 = 1.60)
- **Pre-clamp total: ~53.7°** — exceeds `MAX_ERROR_ANGLE`
- **Post-clamp total: 25.0°** (clamped by §3.6.11) — a heavily misdirected shot; physically plausible

> **Note on clamping:** The raw multiplication in this worst-case scenario produces ~54°.
> The §3.6.11 clamp at `MAX_ERROR_ANGLE = 25.0°` is the operative result. This confirms the
> clamp is necessary and meaningful — without it, the error model would produce extreme
> misdirection inconsistent with the defined output contract.

These extremes are verified numerically in Appendix B (§B.9).

---

### 3.6.9 Deterministic Error Direction

An error *direction* must be computed for the angular offset. This must be deterministic
(KD-4 — no probabilistic dice rolls).

```
// Hash produces a float in [0, 1] from three integer seeds
float hashValue = DeterministicHash(matchSeed, request.AgentId, request.FrameNumber);

// Map hash to a full rotation angle [0°, 360°)
float errorAngleDeg = hashValue × 360.0f;

// Convert to a 2D direction in goal-relative (u, v) space
Vector2 errorDirection = new Vector2(
    Mathf.Cos(errorAngleDeg × Mathf.Deg2Rad),
    Mathf.Sin(errorAngleDeg × Mathf.Deg2Rad)
);
```

**DeterministicHash implementation (Stage 0 local version):**
```
// A simple but adequate hash for Stage 0.
// Identical signature to Deterministic Simulation Spec #15 RNG — migration is drop-in.
float DeterministicHash(int seed1, int seed2, int seed3)
{
    uint h = (uint)seed1;
    h ^= (uint)seed2 * 2654435761u;   // Knuth multiplicative hash
    h ^= (uint)seed3 * 2246822519u;
    h ^= h >> 15;
    h *= 2246822519u;
    h ^= h >> 13;
    // Map to [0.0, 1.0)
    return (h & 0x00FFFFFFu) / (float)0x01000000u;
}
```

This guarantees two agents shooting on the same frame produce distinct error directions.

---

### 3.6.10 Error Application in Goal-Relative Space

Error is applied as an offset in `(u, v)` goal-relative space, then converted back to
a 3D world-space aim direction.

```
// Step 1: Compute (u, v) offset
Vector2 errorOffset = errorDirection × ErrorMagnitudeDeg × GOAL_RELATIVE_ERROR_SCALE;
// GOAL_RELATIVE_ERROR_SCALE converts degrees to goal-mouth units; see §3.6.13

// Step 2: Apply offset to PlacementTarget (before clamping — clamping happens at §3.1)
// Note: This (u, v) is NOT clamped to [0,1]. The error may push outside the goal mouth.
// That is correct — a shot aimed at the corner and pushed by error may miss.
Vector2 actualTarget = request.PlacementTarget + errorOffset;

// Step 3: Convert actualTarget (u, v) back to world-space using §3.5 bilinear formula
Vector3 actualTargetWorld =
    goalMouthOrigin
    + goalRightward × (actualTarget.x × GOAL_WIDTH)
    + Vector3.up    × (actualTarget.y × GOAL_HEIGHT);

// Step 4: Re-derive aim direction from agent position to actualTargetWorld
// (same as §3.5.6 but using actualTargetWorld instead of targetWorldPoint)
Vector3 finalDirection = ComputeAimDirection(agentPosition, actualTargetWorld, launchAngleDeg);
```

**GOAL_RELATIVE_ERROR_SCALE** [GT] converts angular error degrees to goal-relative UV
offset units. At a reference distance of 20m, 1° of angular error corresponds to
approximately `0.35m / 7.32m ≈ 0.048 goal-width units`. This scale factor allows
the error model to remain in intuitive angular units internally while producing
physically correct spatial offsets. See Appendix B for derivation.

---

### 3.6.11 Error Clamping Policy

```
// After all penalties are multiplied, clamp before direction application
ErrorMagnitude = Mathf.Clamp(ErrorMagnitude, MIN_ERROR_ANGLE, MAX_ERROR_ANGLE);
```

| Constant | Value | Rationale |
|---|---|---|
| `MIN_ERROR_ANGLE` | 0.15° [GT] | Even elite players do not achieve perfect accuracy; floor prevents effectively zero error |
| `MAX_ERROR_ANGLE` | 25.0° [GT] | Prevents backward-pass-style extreme misdirection from shot mechanics |

---

### 3.6.12 Boundary Verification Table

| Scenario | Attribute | PowerIntent | Pressure | Fatigue | BMS | WF | Expected ErrorMagnitude |
|---|---|---|---|---|---|---|---|
| Elite, clean, moderate power | 20 | 0.5 | 0.0 | 0.0 | 1.0 | 1.0 | ~0.69° |
| Elite, clean, maximum power | 20 | 1.0 | 0.0 | 0.0 | 1.0 | 1.0 | ~1.25° |
| Average, moderate pressure | 10 | 0.6 | 0.5 | 0.2 | 0.75 | 1.0 | ~4–6° [EST] |
| Poor, heavy pressure, max power | 1 | 1.0 | 0.9 | 0.75 | 0.3 | 1.3 | ~25° (clamped) |
| Any, no pressure or fatigue | any | 0.0 | 0.0 | 0.0 | 1.0 | 1.0 | ≥ MIN_ERROR_ANGLE |
| Elite, weak foot Rating=1 | 20 | 0.5 | 0.0 | 0.0 | 1.0 | 2.0 | ~1.38° |

All values marked [EST] require Appendix B numerical verification.

---

### 3.6.13 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `BASE_ERROR_MAX` | 4.0° | [GT] | Design authority; calibration target vs. StatsBomb |
| `BASE_ERROR_MIN` | 0.5° | [GT] | Design authority; calibration target vs. StatsBomb |
| `POWER_PENALTY_COEFFICIENT` | 1.5 | [GT] | Informed by [LEES-1998] |
| `PRESSURE_MAX_PENALTY` | 0.8 | [GT] | Design authority |
| `FATIGUE_MAX_PENALTY` | 0.4 | [GT] | Design authority (larger than Pass Mechanics 0.25) |
| `BODY_SHAPE_ERROR_COEFFICIENT` | 1.0 | [GT] | Design authority |
| `MIN_ERROR_ANGLE` | 0.15° | [GT] | Design authority |
| `MAX_ERROR_ANGLE` | 25.0° | [GT] | Design authority |
| `GOAL_RELATIVE_ERROR_SCALE` | 0.048 per degree at 20m | [EST] [VER] | Geometric derivation; Appendix B |

---

### 3.6.14 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.6-01 | ErrorMagnitude is NaN | `float.IsNaN()` check post-multiplication | Use MIN_ERROR_ANGLE; log error |
| FM-3.6-02 | `finalDirection` is zero vector | `magnitude < 0.001f` | Use `aimDirection` (no error applied); log error |
| FM-3.6-03 | DeterministicHash returns out-of-range | Assert `[0, 1)` range | Clamp; log error |
| FM-3.6-04 | `actualTargetWorld` unreachable (extreme error pushes behind shooter) | Dot product check on finalDirection vs. agentForward | Clamp to forward hemisphere; log warning |

---

### 3.6.15 Design Decisions and Rationale

**DD-3.6-01: Error applied in goal-relative (u, v) space, not in 3D angular space (KD-2)**

Applying error in angular space would require converting back to goal-relative coordinates
for the "skied high" / "pulled left" outcome interpretations needed by the Statistics Engine.
Applying in `(u, v)` space first keeps the model interpretable: the error offset is directly
readable as "how far off the intended corner did the shot land." The resulting world-space
direction is identical either way; the intermediate representation is the difference.

**DD-3.6-02: All five scalars are multiplicative, not additive**

An additive error model would allow individual penalty terms to independently overwhelm
the base, creating unrealistic single-factor dominance. Multiplicative composition means
all factors interact: high power on an already-pressured shot produces more degradation
than either factor alone, matching real football behaviour.

**DD-3.6-03: `normalisedEffectiveAttribute` is reused from §3.2, not recomputed**

The sigmoid blend of Finishing and LongShots by distance is computed once in §3.2 and
passed through the pipeline. Recomputing it in §3.6 would be redundant and could produce
inconsistencies if any input changed between the two calls (impossible in Stage 0 due to
the freeze at INITIATING, but a maintenance hazard in later stages).

---

### 3.6.16 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `normalisedEffectiveAttribute` scalar | §3.2 VelocityCalculator | Must be passed through pipeline; not recomputed |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | Scalar [0.0, 1.0] |
| `weakFootMultiplier` | §3.8 WeakFootPenaltyApplier | Scalar ≥ 1.0 |
| `PlayerAttributes.Composure` | Agent Movement #2 §3.5.6 | Confirmed; range [1, 20] |
| `AgentState.PressureRating` | Agent Movement #2 §3.5.4 | Opponent proximity scalar [0.0, 1.0] |
| `AgentState.Fatigue` | Agent Movement #2 §3.5.4 | Confirmed; [0.0, 1.0] |
| `DeterministicHash` function | Deterministic Simulation Spec #15 (Stage 0: local impl) | Hash signature confirmed; drop-in migration |
| Goal geometry | Match Config | `goalMouthOrigin`, `goalRightward`, FIFA dimensions |

---

## 3.7 Body Mechanics Evaluation

### 3.7.1 Responsibilities and Scope

§3.7 (BodyMechanicsEvaluator) evaluates the physical quality of the shooting stance
and collapses four biomechanical inputs into a **scalar `BodyMechanicsScore ∈ [0.0, 1.0]`**
and a signed **`bodyLeanAngleDeg`** float. These two outputs are the primary inputs to
§3.3 (launch angle penalties) and §3.6 (error magnitude penalties), and the stumble
trigger in §3.9 (state machine).

Body mechanics evaluation is the sub-system that creates the realistic variance between
a striker shooting in stride versus one who is off-balance, cramped, or reaching.
It is the foundation of simulation fidelity for the physical reality of football: good
technique produces good outcomes; poor technique produces errors independent of skill ratings.

§3.7 **executes first** in the CONTACT evaluation chain — before §3.2, §3.3, and §3.6.
Its outputs are required by all three of those sub-systems. See §2.2.3 for pipeline order.

§3.7 **does**:
- Read run-up angle, plant foot offset, agent velocity vector, and body lean from
  `AgentPhysicalProperties` (frozen at INITIATING)
- Score each component on a [0.0, 1.0] scale
- Produce a weighted composite `BodyMechanicsScore`
- Extract `bodyLeanAngleDeg` as a signed float (negative = forward lean, positive = backward)
- Derive `ContactQualityModifier` for §3.2
- Evaluate the stumble trigger condition

§3.7 **does not**:
- Modify agent state (read-only access to AgentPhysicalProperties — KD-3 from Section 1)
- Determine what body position the agent is in (Agent Movement owns state transitions)

---

### 3.7.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Notes |
|-------|--------|------|-------|
| `physProps.RunUpAngle` | AgentPhysicalProperties (frozen) | float (degrees) | Angle between run-up direction and ball-to-goal bearing |
| `physProps.PlantFootOffset` | AgentPhysicalProperties (frozen) | float (metres) | Signed lateral offset from ideal plant position |
| `physProps.AgentVelocityAtContact` | AgentPhysicalProperties (frozen) | Vector3 (m/s) | Agent's velocity vector at moment of contact |
| `physProps.BodyLeanAngle` | AgentPhysicalProperties (frozen) | float (degrees) | Signed; negative = forward lean, positive = backward |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0]; used in stumble trigger |

**Outputs:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `BodyMechanicsScore` | §3.3, §3.6, §3.9, ShotResult | float | [0.0, 1.0] |
| `bodyLeanAngleDeg` | §3.3 LaunchAngleCalculator | float (degrees) | [−45°, +45°] |
| `ContactQualityModifier` | §3.2 VelocityCalculator | float | [0.7, 1.0] |
| `stumbleTrigger` | §3.9 ShotStateMachine | bool | True if stumble threshold met |

---

### 3.7.3 Run-Up Angle Score

The ideal run-up angle for a shot is ~30–45° to the ball-to-goal bearing (informed by
biomechanics research on power generation in the kicking motion). Deviation degrades
both power transmission and accuracy.

```
RunUpAngleDev = |physProps.RunUpAngle − IDEAL_RUN_UP_ANGLE|   // Absolute deviation from ideal
RunUpScore    = 1.0 − Mathf.Clamp01(RunUpAngleDev / RUN_UP_TOLERANCE)
```

Where:
- `IDEAL_RUN_UP_ANGLE` = 37.5° [GT] — midpoint of 30–45° optimal range
- `RUN_UP_TOLERANCE`   = 45.0° [GT] — deviation at which `RunUpScore` reaches 0.0

**Boundary checks:**
- `RunUpAngleDev = 0°`: `RunUpScore = 1.0` (perfect approach)
- `RunUpAngleDev = 45°`: `RunUpScore = 0.0` (90° approach — shooting directly sideways)
- `RunUpAngleDev = 22.5°`: `RunUpScore = 0.5` (moderate deviation)

---

### 3.7.4 Plant Foot Offset Score

The plant foot should be placed approximately 15–25cm lateral to the ball (from
biomechanics studies of maximal kicking velocity). Too close cramps the swing; too far
causes off-balance contact.

```
PlantOffset = |physProps.PlantFootOffset|   // Unsigned; any lateral deviation penalised
PlantScore  = 1.0 − Mathf.Clamp01(PlantOffset / PLANT_FOOT_TOLERANCE)
```

Where `PLANT_FOOT_TOLERANCE` = 0.35m [GT] — offset at which `PlantScore` reaches 0.0.

**Boundary checks:**
- `PlantOffset = 0.0m`: `PlantScore = 1.0` (ideal placement)
- `PlantOffset = 0.35m`: `PlantScore = 0.0` (critically misplaced — very cramped or off-balance)
- `PlantOffset = 0.175m`: `PlantScore = 0.5` (moderate deviation)

---

### 3.7.5 Agent Velocity at Contact Score

An agent moving toward the goal at moderate speed transfers momentum effectively into
the kick. Moving away or laterally at speed degrades contact quality.

```
// Project agent velocity onto the goal direction (agentForward at contact)
float velocityTowardGoal = Vector3.Dot(physProps.AgentVelocityAtContact, agentForward);

VelocityScore:
  if (velocityTowardGoal ∈ [VELOCITY_IDEAL_MIN, VELOCITY_IDEAL_MAX]):
      VelocityScore = 1.0
  else if (velocityTowardGoal < VELOCITY_IDEAL_MIN):
      // Moving away from goal or static
      VelocityScore = Mathf.Clamp01(
          1.0 + velocityTowardGoal / VELOCITY_PENALTY_SCALE_NEGATIVE
      )
  else: // velocityTowardGoal > VELOCITY_IDEAL_MAX
      // Moving too fast — overrunning the ball
      VelocityScore = Mathf.Clamp01(
          1.0 − (velocityTowardGoal − VELOCITY_IDEAL_MAX) / VELOCITY_PENALTY_SCALE_POSITIVE
      )
```

Where [GT]:
- `VELOCITY_IDEAL_MIN` = 1.0 m/s — minimum forward velocity for ideal contact
- `VELOCITY_IDEAL_MAX` = 5.0 m/s — maximum before overrunning penalty begins
- `VELOCITY_PENALTY_SCALE_NEGATIVE` = 3.0 m/s — rate of score drop below ideal range
- `VELOCITY_PENALTY_SCALE_POSITIVE` = 4.0 m/s — rate of score drop above ideal range

---

### 3.7.6 Body Lean Measurement

Body lean is extracted directly from `AgentPhysicalProperties` — no calculation required.

```
bodyLeanAngleDeg = physProps.BodyLeanAngle
// Clamped to [−45°, +45°] at read time for safety
bodyLeanAngleDeg = Mathf.Clamp(bodyLeanAngleDeg, −45.0f, +45.0f);
```

Positive values (leaning backward) are the leading cause of skied shots. This value is
passed to §3.3 (BodyLeanPenalty) which uses it to add unintended loft.

A separate penalty for lean is incorporated into `BodyMechanicsScore` via `LeanScore`:

```
LeanScore = 1.0 − Mathf.Clamp01(|bodyLeanAngleDeg| / LEAN_TOLERANCE)
```

Where `LEAN_TOLERANCE` = 20.0° [GT] — lean angle at which `LeanScore` reaches 0.0.

---

### 3.7.7 Composite BodyMechanicsScore

The four component scores are combined with fixed weights into the composite scalar.

```
BodyMechanicsScore =
    WEIGHT_RUN_UP    × RunUpScore
  + WEIGHT_PLANT     × PlantScore
  + WEIGHT_VELOCITY  × VelocityScore
  + WEIGHT_LEAN      × LeanScore
```

Where weights [GT] must sum to 1.0:

| Weight Constant | Value | Rationale |
|---|---|---|
| `WEIGHT_RUN_UP` | 0.25 | Approach angle matters, but can be overcome by technique |
| `WEIGHT_PLANT` | 0.30 | Plant foot is the most direct determinant of contact quality |
| `WEIGHT_VELOCITY` | 0.20 | Movement contribution is significant but forgiving |
| `WEIGHT_LEAN` | 0.25 | Lean directly causes skied shots; equally weighted with run-up |

**Sum check:** `0.25 + 0.30 + 0.20 + 0.25 = 1.00` ✓

---

### 3.7.8 ContactQualityModifier Derivation

`ContactQualityModifier` is a velocity multiplier passed to §3.2. Poor body mechanics
reduces the effective velocity of the kick even when `PowerIntent` is high.

```
ContactQualityModifier = CQM_MIN + BodyMechanicsScore × (CQM_MAX − CQM_MIN)
```

Where:
- `CQM_MIN` = 0.7 [GT] — minimum velocity multiplier at `BodyMechanicsScore = 0.0`
- `CQM_MAX` = 1.0 [GT] — maximum velocity multiplier at `BodyMechanicsScore = 1.0`

**Boundary checks:**
- `BodyMechanicsScore = 0.0`: `ContactQualityModifier = 0.7` (30% velocity reduction — critically mis-struck)
- `BodyMechanicsScore = 0.5`: `ContactQualityModifier = 0.85`
- `BodyMechanicsScore = 1.0`: `ContactQualityModifier = 1.0` (no reduction)

---

### 3.7.9 Stumble Trigger Condition

A stumble is triggered when the agent is both off-balance and shooting at maximum effort.
This is the condition where the physical strain of the shot exceeds the agent's ability
to maintain stance.

```
bool stumbleTrigger =
    (BodyMechanicsScore < STUMBLE_BMS_THRESHOLD) &&
    (request.PowerIntent > STUMBLE_POWER_THRESHOLD);
```

Where:
- `STUMBLE_BMS_THRESHOLD`   = 0.35 [GT] — body mechanics quality below which stumble is possible
- `STUMBLE_POWER_THRESHOLD` = 0.75 [GT] — power intent above which stumble is possible

When `stumbleTrigger = true`:
- §3.9 (ShotStateMachine) transitions to STUMBLING at FOLLOW_THROUGH
- Agent Movement receives the stumble signal via the existing hysteresis pattern (§3.1
  of Agent Movement Spec #2 — no new interface required, per OI-003 resolution)
- `ShotExecutedEvent.StumbleTriggered = true` is published for GK difficulty estimation

**Rationale for threshold values:** STUMBLE_BMS_THRESHOLD = 0.35 means a player must
have poor mechanics on at least two or three components (not just one marginal one)
to be stumble-eligible. STUMBLE_POWER_THRESHOLD = 0.75 means only powerful shots can
trigger a stumble — a gentle touch cannot knock an agent off balance regardless of stance.

---

### 3.7.10 Boundary Verification Table

| Scenario | RunUpScore | PlantScore | VelScore | LeanScore | BMS | CQM | Stumble? |
|---|---|---|---|---|---|---|---|
| Perfect approach | 1.0 | 1.0 | 1.0 | 1.0 | 1.00 | 1.00 | No |
| Lateral approach | 0.0 | 0.8 | 0.9 | 0.9 | 0.635 | 0.891 | No (PowerIntent = 0.5) |
| Cramped plant foot | 0.9 | 0.0 | 0.8 | 0.7 | 0.555 | 0.867 | No |
| Off-balance, max power | 0.4 | 0.3 | 0.5 | 0.2 | 0.345 | 0.804 | Yes (PI > 0.75) |
| Critical mis-strike | 0.1 | 0.1 | 0.2 | 0.0 | 0.105 | 0.732 | Yes (PI > 0.75) |

---

### 3.7.11 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `IDEAL_RUN_UP_ANGLE` | 37.5° | [GT] | Midpoint of [LEES-1998] 30–45° optimal range |
| `RUN_UP_TOLERANCE` | 45.0° | [GT] | Design authority |
| `PLANT_FOOT_TOLERANCE` | 0.35 m | [GT] | Design authority |
| `VELOCITY_IDEAL_MIN` | 1.0 m/s | [GT] | Design authority |
| `VELOCITY_IDEAL_MAX` | 5.0 m/s | [GT] | Design authority |
| `VELOCITY_PENALTY_SCALE_NEGATIVE` | 3.0 m/s | [GT] | Design authority |
| `VELOCITY_PENALTY_SCALE_POSITIVE` | 4.0 m/s | [GT] | Design authority |
| `LEAN_TOLERANCE` | 20.0° | [GT] | Design authority |
| `WEIGHT_RUN_UP` | 0.25 | [GT] | Design authority |
| `WEIGHT_PLANT` | 0.30 | [GT] | Design authority |
| `WEIGHT_VELOCITY` | 0.20 | [GT] | Design authority |
| `WEIGHT_LEAN` | 0.25 | [GT] | Design authority |
| `CQM_MIN` | 0.7 | [GT] | Design authority |
| `CQM_MAX` | 1.0 | Design authority | Fixed upper bound |
| `STUMBLE_BMS_THRESHOLD` | 0.35 | [GT] | Design authority |
| `STUMBLE_POWER_THRESHOLD` | 0.75 | [GT] | Design authority |

---

### 3.7.12 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.7-01 | `AgentPhysicalProperties` null or uninitialized | Null check at start of evaluation | Return `BodyMechanicsScore = 0.5` (neutral); log error |
| FM-3.7-02 | `BodyLeanAngle` out of [−90°, +90°] | Range check after read | Clamp to [−45°, +45°]; log warning |
| FM-3.7-03 | Composite weights do not sum to 1.0 | Compile-time assertion: `WEIGHT_SUM_ASSERT` | Build failure |
| FM-3.7-04 | NaN in any component score | `float.IsNaN()` on each score | Replace with 0.5 (neutral); log error |

---

### 3.7.13 Design Decisions and Rationale

**DD-3.7-01: §3.7 executes before §3.2 despite being numbered after it**

The pipeline order (§3.7 → §3.2 → §3.3 → §3.4 → §3.5 → §3.6) differs from the
section numbering (§3.2 → §3.3 → §3.4 → §3.5 → §3.6 → §3.7). The pipeline order
reflects data dependency (§3.2 needs `ContactQualityModifier`); the section numbering
reflects specification organisation (body mechanics is a supporting sub-system
described after the primary calculations it feeds). See §2.2.3 for the authoritative
13-step pipeline.

**DD-3.7-02: Four component scores with fixed weights rather than a formula per input**

An alternative design would derive `BodyMechanicsScore` directly from each physical
input via a single formula. The four-component weighted average is preferred because:
it makes each component's contribution inspectable and independently tunable;
the weights can be adjusted by gameplay designers without touching formulas;
and the approach naturally handles missing components (a future aerial shot spec might
omit `RunUpScore` — the weights can be redistributed).

**DD-3.7-03: §3.7 is read-only — no agent mutation**

Per KD-3 of Section 1: Shot Mechanics does not mutate agent state during calculation.
The stumble trigger produced by §3.7 is a signal emitted at FOLLOW_THROUGH (§3.9);
Agent Movement owns the state transition. §3.7 only evaluates and reports.

---

### 3.7.14 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `AgentPhysicalProperties.RunUpAngle` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.PlantFootOffset` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.AgentVelocityAtContact` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.BodyLeanAngle` | Agent Movement #2 §3.5.4 | Confirmed present |
| Stumble hysteresis pattern | Agent Movement #2 §3.1 | Existing interface; no new signal required |

---

## 3.8 Weak Foot Penalty

### 3.8.1 Responsibilities and Scope

§3.8 (WeakFootPenaltyApplier) applies a penalty to both the **error cone** and
**kick velocity** when `ShotRequest.IsWeakFoot = true`. The penalties are derived from
`PlayerAttributes.WeakFootRating ∈ [1, 5]`.

The weak foot penalty architecture is identical to Pass Mechanics §3.7, with one
critical difference: **shot error cone expansion is larger than pass error cone
expansion** for the same `WeakFootRating`. Shots require more precise contact than
passes — the same technical deficit produces a larger accuracy penalty on a shot.

§3.8 **does**:
- Compute `weakFootErrorMultiplier` (≥ 1.0) for use in §3.6 error formula
- Compute `weakFootVelocityMultiplier` (≤ 1.0) for use in §3.2 velocity formula
- Return both as neutral (1.0 / 1.0) when `IsWeakFoot = false`

§3.8 **does not**:
- Determine which foot is weak (Decision Tree sets `IsWeakFoot` in `ShotRequest`)
- Modify spin components (weak foot degrades velocity and accuracy; spin remains
  controlled by ContactZone and SpinIntent — a weak-foot finesse shot still curls,
  just with less velocity and more error)

**Position in pipeline:** §3.8 executes second (immediately after §3.1 validation),
before §3.2–§3.7, so both multipliers are available to downstream sub-systems.

---

### 3.8.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range |
|-------|--------|------|-------|
| `request.IsWeakFoot` | ShotRequest | bool | — |
| `attributes.WeakFootRating` | PlayerAttributes | int | [1, 5] |

**Outputs:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `weakFootErrorMultiplier` | §3.6 ErrorCalculator | float | [1.0, SHOT_WF_ERROR_MAX] |
| `weakFootVelocityMultiplier` | §3.2 VelocityCalculator | float | [SHOT_WF_VEL_MIN, 1.0] |

---

### 3.8.3 Error Cone Multiplier Formula

```
if (!request.IsWeakFoot)
    return (weakFootErrorMultiplier: 1.0f, weakFootVelocityMultiplier: 1.0f);

// Linear interpolation from maximum penalty (Rating=1) to no penalty (Rating=5)
float penaltyFraction = (5 − WeakFootRating) / 4.0f;
// Rating=1: penaltyFraction=1.0 (max penalty)
// Rating=5: penaltyFraction=0.0 (ambidextrous — no penalty)

weakFootErrorMultiplier = 1.0f + penaltyFraction × SHOT_WF_BASE_ERROR_PENALTY;
```

Where `SHOT_WF_BASE_ERROR_PENALTY` = 0.60 [GT].

**Boundary checks:**

| WeakFootRating | penaltyFraction | weakFootErrorMultiplier | Effect |
|---|---|---|---|
| 1 (worst) | 1.0 | 1.60 | +60% error expansion |
| 2 | 0.75 | 1.45 | +45% error expansion |
| 3 | 0.50 | 1.30 | +30% error expansion |
| 4 | 0.25 | 1.15 | +15% error expansion |
| 5 (ambi) | 0.00 | 1.00 | No penalty |

**Comparison to Pass Mechanics §3.7:** Pass Mechanics uses `WEAK_FOOT_BASE_PENALTY = 0.30`
(maximum +30% error). Shot Mechanics uses 0.60 (maximum +60% error) — double the
penalty — reflecting the greater precision demand of shooting vs. passing.

---

### 3.8.4 Velocity Reduction Formula

Weak foot shots also produce lower velocity, as the non-preferred foot generates less
consistent power transfer.

```
weakFootVelocityMultiplier = 1.0f − penaltyFraction × SHOT_WF_VELOCITY_PENALTY;
```

Where `SHOT_WF_VELOCITY_PENALTY` = 0.20 [GT].

**Boundary checks:**

| WeakFootRating | weakFootVelocityMultiplier | Effect |
|---|---|---|
| 1 (worst) | 0.80 | 20% velocity reduction |
| 3 | 0.90 | 10% velocity reduction |
| 5 (ambi) | 1.00 | No reduction |

**Rationale:** A 20% velocity reduction at Rating=1 means a shot capped at 35 m/s
on the dominant foot achieves at most 28 m/s on the weak foot. This is meaningful but
not so extreme that weak-foot shots become physically implausible. [GT] — calibrate
during playtesting.

---

### 3.8.5 Shot vs. Pass Penalty Differential

| Metric | Pass Mechanics §3.7 | Shot Mechanics §3.8 | Differential |
|---|---|---|---|
| Error multiplier at Rating=1 | ×1.30 | ×1.60 | Shot 2× worse per [CAREY-2001] extrapolation |
| Velocity penalty at Rating=1 | None | ×0.80 | Shots penalise velocity; passes do not |
| Rating range | [1, 5] | [1, 5] | Identical |
| Formula structure | Linear interpolation | Linear interpolation | Identical |

---

### 3.8.6 Boundary Verification Table

| IsWeakFoot | Rating | ErrorMultiplier | VelocityMultiplier |
|---|---|---|---|
| false | any | 1.00 | 1.00 |
| true | 1 | 1.60 | 0.80 |
| true | 2 | 1.45 | 0.85 |
| true | 3 | 1.30 | 0.90 |
| true | 4 | 1.15 | 0.95 |
| true | 5 | 1.00 | 1.00 |

All transitions are strictly monotone ✓. No discontinuities ✓.

---

### 3.8.7 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `SHOT_WF_BASE_ERROR_PENALTY` | 0.60 | [GT] | Design authority; 2× Pass Mechanics value |
| `SHOT_WF_VELOCITY_PENALTY` | 0.20 | [GT] | Design authority |

---

### 3.8.8 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.8-01 | `WeakFootRating` out of [1, 5] | Range assertion | Clamp; log error |
| FM-3.8-02 | `weakFootErrorMultiplier` < 1.0 | Post-calculation assert | Clamp to 1.0; log error (cannot reduce error below baseline) |
| FM-3.8-03 | `weakFootVelocityMultiplier` > 1.0 | Post-calculation assert | Clamp to 1.0; log error |

---

### 3.8.9 Design Decisions and Rationale

**DD-3.8-01: Shot error penalty (0.60) is exactly double pass penalty (0.30)**

The factor-of-two differential is a design choice, not an academic derivation.
[CAREY-2001] documents passing accuracy degradation; equivalent shooting data is
not available in the literature at the required granularity. The doubling is a
conservative starting point: shooting clearly demands more precision than passing,
and a proportional penalty is defensible. [GT] — this is a primary playtesting target.

**DD-3.8-02: Velocity penalty applies to shots but not passes**

A weak-foot pass still reaches the intended target at the cost of accuracy. A weak-foot
shot loses both accuracy and power — the player cannot generate the same force through
the non-preferred leg. This reflects a well-observed phenomenon in football: players
shooting with their weak foot cannot consistently hit with the same power as their
dominant foot, while weak-foot passes are mainly an accuracy problem.

---

## 3.9 Shot State Machine

### 3.9.1 Responsibilities and Scope

§3.9 (ShotStateMachine) manages the complete shot execution lifecycle. It is the
orchestrator that sequences all other §3.x sub-systems, gates `Ball.ApplyKick()` to
exactly one call per execution, handles tackle interrupts, and emits the stumble signal
at FOLLOW_THROUGH.

The state machine is not a novel architecture — it is explicitly designed to mirror
Pass Mechanics §3.8. Any divergence from that pattern must be justified. Stage 0
introduces two differences from Pass Mechanics: windup duration is computed from
`PowerIntent`/`SpinIntent` (not a per-type lookup), and the STUMBLING transition at
FOLLOW_THROUGH has no Pass Mechanics equivalent.

§3.9 **does**:
- Manage transitions across all seven states
- Compute WINDUP duration from `PowerIntent` and `SpinIntent`
- Consume the tackle interrupt flag from Collision System #3
- Freeze `AgentPhysicalProperties` at INITIATING (capturing the snapshot for §3.7)
- Sequence the CONTACT evaluation pipeline: §3.7 → §3.8 → §3.2 → §3.3 → §3.4 → §3.5 → §3.6
- Call `Ball.ApplyKick()` exactly once per CONTACT state entry
- Trigger STUMBLING transition based on `stumbleTrigger` from §3.7
- Transition to COMPLETE and emit events via §3.10

§3.9 **does not**:
- Compute any physics values (all computation is delegated to §3.2–§3.8)
- Publish events directly (§3.10 owns all event publication)
- Modify agent movement state (signal emitted; Agent Movement owns the transition)

---

### 3.9.2 State Definitions

| State | Description | Duration | Entry Actions | Exit Conditions |
|---|---|---|---|---|
| `IDLE` | No shot in progress | Indefinite | None | ShotRequest received |
| `INITIATING` | Request received; validation in progress; AgentPhysicalProperties frozen | 1 frame | Freeze physProps; validate request | Pass → WINDUP; Fail → IDLE |
| `WINDUP` | Agent preparing kick; interruptible | `WINDUP_FRAMES` (computed) | Start windup timer | Timer → CONTACT; Tackle → IDLE |
| `CONTACT` | All calculations run; `Ball.ApplyKick()` called; irreversible | 1 frame | Run pipeline; call ApplyKick; publish ShotExecutedEvent | Always → FOLLOW_THROUGH |
| `FOLLOW_THROUGH` | Cosmetic follow-through; no physics | `FOLLOW_THROUGH_FRAMES` | Check stumble trigger | Timer + no stumble → COMPLETE; Timer + stumble → STUMBLING |
| `STUMBLING` | Agent stumbling; signal sent to Agent Movement | `STUMBLING_FRAMES` | Emit stumble signal to Agent Movement | Timer → COMPLETE |
| `COMPLETE` | Shot finished; cleanup | 1 frame | Cleanup; reset fields | Always → IDLE |

---

### 3.9.3 Windup Duration Formula

Unlike Pass Mechanics (which uses per-type windup frame lookup), Shot Mechanics computes
windup duration from `PowerIntent` and `SpinIntent`. High power requires a longer
backswing; high spin intent requires a more deliberate foot wrap around the ball.

```
// Base windup from PowerIntent (dominant factor)
int baseWindupFrames;
if (request.PowerIntent >= 0.80f)      baseWindupFrames = WINDUP_FRAMES_HIGH_POWER;
else if (request.PowerIntent >= 0.50f) baseWindupFrames = WINDUP_FRAMES_MED_POWER;
else                                   baseWindupFrames = WINDUP_FRAMES_LOW_POWER;

// Spin intent adds a small additional preparation time
int spinWindupBonus = Mathf.RoundToInt(request.SpinIntent × WINDUP_SPIN_BONUS_MAX);

int finalWindupFrames = baseWindupFrames + spinWindupBonus;
```

Where [GT]:
| Constant | Value | Duration at 60Hz | Rationale |
|---|---|---|---|
| `WINDUP_FRAMES_HIGH_POWER` | 14 frames | ~233ms | Full backswing for maximum effort |
| `WINDUP_FRAMES_MED_POWER` | 10 frames | ~167ms | Standard shot preparation |
| `WINDUP_FRAMES_LOW_POWER` | 7 frames | ~117ms | Quick stab / tap |
| `WINDUP_SPIN_BONUS_MAX` | 3 frames | ~50ms | Maximum additional time for deliberate spin setup |

**Example:** `PowerIntent = 0.9`, `SpinIntent = 0.8` →
`14 + round(0.8 × 3) = 14 + 2 = 16 frames` (~267ms).

**Comparison to Pass Mechanics:** Pass windup range is 8–15 frames. Shot windup range
is 7–17 frames — shots have a slightly wider spread, with maximum effort shots taking
longer than even lofted passes. This reflects the greater physical demand of a full-power
shot versus a passed ball.

---

### 3.9.4 Follow-Through Duration

```
FOLLOW_THROUGH_FRAMES = 8 frames (~133ms) [GT]
```

Follow-through is cosmetic at Stage 0. Its duration is fixed regardless of power or
spin intent, because the animation system (Stage 1+) will control visual follow-through
duration based on `ShotAnimationData`. The fixed 8-frame duration creates a brief
window after ball departure during which the agent is not yet available for new actions —
a gameplay-realism requirement.

---

### 3.9.5 Tackle Interrupt Handling

The tackle interrupt pattern is identical to Pass Mechanics §3.8.3:

```
// Called each frame while in WINDUP
void PollTackleInterrupt()
{
    // State must be WINDUP to process interrupt
    if (currentState != ShotState.WINDUP)
        return;   // Interrupt during CONTACT or later is ignored — ball is leaving the foot

    // Single atomic read-and-clear. API aligned with Pass Mechanics §4.4.2.
    bool tackleInterrupt = CollisionSystem.GetAndClearTackleFlag(agentId: request.AgentId);
    if (!tackleInterrupt)
        return;

    currentState = ShotState.IDLE;
    result.Outcome = ShotOutcome.Cancelled;
    shotEventEmitter.PublishShotCancelledEvent(request, ShotCancelReason.TackleInterrupt);
    // Ball.ApplyKick() is NOT called
}
```

**Interrupt source:** Collision System #3 sets a tackle interrupt flag polled each frame
by §3.9. This is the same polling pattern used in Pass Mechanics — no new interface
required.

**Why CONTACT and later ignore interrupts:** Once `Ball.ApplyKick()` has been called,
the ball is physically in flight. A tackle that connects with the shooter's follow-through
leg has no effect on the ball trajectory. The Collision System may still register a
player collision, but Shot Mechanics is not the relevant handler.

---

### 3.9.6 Stumble Transition

```
// Called at FOLLOW_THROUGH entry, after checking §3.7 stumbleTrigger
void OnFollowThroughEnter(bool stumbleTrigger)
{
    if (stumbleTrigger)
    {
        // Transition to STUMBLING instead of waiting for follow-through timer.
        currentState = ShotState.STUMBLING;
        // StumbleTriggered is set in ShotResult and mirrored in ShotExecutedEvent
        // (already published at CONTACT). Agent Movement subscribes to ShotExecutedEvent
        // and handles the STUMBLING state transition on its side (Mechanism C, §4.3.3).
        // Shot Mechanics does NOT call any Agent Movement write method directly.
    }
    // If no stumble, proceed to normal FOLLOW_THROUGH timer countdown
}
```

**STUMBLING duration:**
```
STUMBLING_FRAMES = 18 frames (~300ms) [GT]
```
A stumbling agent is incapacitated for ~300ms — long enough to be a meaningful gameplay
consequence without being punitive beyond the scale of a normal recovery. [GT] — primary
playtesting target.

---

### 3.9.7 State Diagram

```
stateDiagram-v2
    [*] --> IDLE
    IDLE --> INITIATING : ShotRequest received
    INITIATING --> WINDUP : Validation passed + physProps frozen
    INITIATING --> IDLE : Validation failed
    WINDUP --> CONTACT : Windup timer elapsed
    WINDUP --> IDLE : Tackle interrupt (ShotCancelledEvent published)
    CONTACT --> FOLLOW_THROUGH : Ball.ApplyKick() called (ShotExecutedEvent published)
    FOLLOW_THROUGH --> STUMBLING : stumbleTrigger = true
    FOLLOW_THROUGH --> COMPLETE : stumbleTrigger = false + timer elapsed
    STUMBLING --> COMPLETE : Stumbling timer elapsed
    COMPLETE --> IDLE : Cleanup complete
    IDLE --> [*]
```

---

### 3.9.8 Per-Frame Execution Guard

A strict execution guard prevents multiple `Ball.ApplyKick()` calls:

```
// Guard at CONTACT state entry
if (applyKickCalled)
{
    Log.Critical("ShotStateMachine: ApplyKick guard violation — multiple calls prevented.");
    return;
}
applyKickCalled = true;
Ball.ApplyKick(finalVelocity, finalSpin, request.AgentId, matchTime);
```

The `applyKickCalled` flag is reset to `false` at COMPLETE → IDLE transition.

---

### 3.9.9 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `WINDUP_FRAMES_HIGH_POWER` | 14 | [GT] | Design authority |
| `WINDUP_FRAMES_MED_POWER` | 10 | [GT] | Design authority |
| `WINDUP_FRAMES_LOW_POWER` | 7 | [GT] | Design authority |
| `WINDUP_SPIN_BONUS_MAX` | 3 | [GT] | Design authority |
| `FOLLOW_THROUGH_FRAMES` | 8 | [GT] | Design authority |
| `STUMBLING_FRAMES` | 18 | [GT] | Design authority |

---

### 3.9.10 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.9-01 | Multiple ApplyKick calls in one execution | `applyKickCalled` guard | Block second call; log critical |
| FM-3.9-02 | State machine in unknown state | Default branch in state switch | Log critical; transition to IDLE; reset |
| FM-3.9-03 | Windup timer underflow (negative frames) | Assert `finalWindupFrames > 0` | Clamp to 1; log error |
| FM-3.9-04 | physProps freeze occurs outside INITIATING | State assertion | Log error; freeze anyway (safer than null reference) |

---

### 3.9.11 Design Decisions and Rationale

**DD-3.9-01: Windup duration is parameter-derived, not type-keyed**

Pass Mechanics uses a `TYPE_WINDUP_FRAMES` lookup because pass type is a discrete label.
Shot Mechanics eliminated named type labels (Outline v1.2). Windup duration is therefore
derived from `PowerIntent` and `SpinIntent` — the same parameters that drive velocity and
angle. This is consistent with the parameter-based philosophy established in KD-3 and
avoids reintroducing an implicit type table through the back door.

**DD-3.9-02: STUMBLING is a distinct state, not a flag within FOLLOW_THROUGH**

Making stumbling a flag within FOLLOW_THROUGH would require consumers (Agent Movement,
the animation system in Stage 1+) to check both the state and a flag. A distinct
STUMBLING state is self-describing, directly observable, and clean to test. The
additional state adds zero computational cost.

**DD-3.9-03: CONTACT is irreversible — tackle interrupt is ignored**

Once the ball has been struck, physics owns the ball. A tackle interrupt arriving during
CONTACT is a timing edge case (the tackle was registered in the same frame as the
CONTACT transition). Attempting to cancel the shot at this point would require unwinding
Ball Physics state — a scope violation. The correct model: the tackle connects with the
shooter's follow-through, not the ball. No gameplay benefit justifies the complexity.

---

## 3.10 Event Publishing

### 3.10.1 Responsibilities and Scope

§3.10 (ShotEventEmitter) is responsible for **all event publication** from the Shot
Mechanics system. It is the sole entry point for events onto the event bus. No other
§3.x sub-system publishes events directly.

§3.10 **does**:
- Publish `ShotExecutedEvent` at CONTACT state completion
- Publish `ShotCancelledEvent` on tackle interrupt during WINDUP
- Populate `ShotAnimationData` stub (unconsumed at Stage 0)
- Enforce event bus queue safety (capacity check before push)

§3.10 **does not**:
- Decide when to publish (§3.9 calls §3.10 at the correct state transitions)
- Compute any field values (all values are taken from the completed `ShotResult` or
  `ShotRequest` — §3.10 only assembles and pushes)
- Subscribe to any events (§3.10 is a publisher only)

---

### 3.10.2 Events Published

| Event | Published When | Consumer (Stage 0) | Consumer (Future) |
|---|---|---|---|
| `ShotExecutedEvent` | CONTACT state — `Ball.ApplyKick()` called | Event bus only (no active consumer at Stage 0) | Goalkeeper Mechanics #11; Statistics Engine |
| `ShotCancelledEvent` | WINDUP tackle interrupt | Event bus only | Decision Tree #8 (for re-evaluation) |
| `ShotAnimationData` | CONTACT state — stub, not published to bus | None | Animation System (Stage 1+) |

**Invalid request rejection** is logged only — no event is published. This is consistent
with Pass Mechanics §3.10 policy: an invalid request is a programming error in the
Decision Tree, not an observable game event.

---

### 3.10.3 ShotExecutedEvent — Field Population

All fields are populated from `ShotResult`, `ShotRequest`, and the pipeline outputs
captured at CONTACT state.

```csharp
ShotExecutedEvent evt = new ShotExecutedEvent
{
    ShootingAgentId  = request.AgentId,
    TeamId           = agentState.TeamId,
    KickVelocity     = result.FinalVelocity,       // From §3.2 × §3.6 finalDirection
    KickSpin         = result.FinalSpin,            // From §3.4
    IntendedTarget   = request.PlacementTarget,     // (u, v) before error — xG analytics
    FinalDirection   = result.FinalDirection,        // Post-error direction — trajectory
    BodyMechanicsScore = result.BodyMechanicsScore,  // From §3.7
    PowerIntent      = request.PowerIntent,
    ContactZone      = request.ContactZone,
    DistanceToGoal   = request.DistanceToGoal,
    MatchTime        = matchTime,
    ContactFrame     = request.FrameNumber,
    StumbleTriggered = result.StumbleTriggered       // From §3.7 stumble trigger
};
eventBus.Publish(evt);
```

**Data completeness check for Goalkeeper Mechanics #11:**
All fields required for GK save-difficulty estimation are present:
- `KickVelocity` — speed and direction → interception trajectory computation ✓
- `KickSpin` — trajectory deviation from Magnus force ✓
- `BodyMechanicsScore` — irregular trajectory flag for poor-mechanics shots ✓
- `StumbleTriggered` — shooter off-balance → trajectory may be more unpredictable ✓
- `ContactZone` — trajectory class inference ✓

---

### 3.10.4 ShotCancelledEvent — Field Population

```csharp
ShotCancelledEvent evt = new ShotCancelledEvent
{
    AgentId      = request.AgentId,
    TeamId       = agentState.TeamId,
    CancelFrame  = request.FrameNumber,   // Frame on which tackle interrupt fired
    Reason       = ShotCancelReason.TackleInterrupt   // Only valid reason at Stage 0
};
eventBus.Publish(evt);
```

---

### 3.10.5 ShotAnimationData — Stub Population

```csharp
// Populated but NOT pushed to event bus at Stage 0.
// Stored in ShotResult for future animation system consumption.
ShotAnimationData animData = new ShotAnimationData
{
    AgentId           = request.AgentId,
    ContactZone       = request.ContactZone,
    PowerIntent       = request.PowerIntent,
    BodyMechanicsScore = result.BodyMechanicsScore,
    IsWeakFoot        = request.IsWeakFoot,
    WindupFrames      = windupFrames    // Computed by §3.9; stored in result
};
// result.AnimationData = animData;  // Stored; not published. Stage 1+ animation system
                                     // subscribes to event bus when implemented.
```

---

### 3.10.6 Publication Timing and Ordering

Within the CONTACT state frame:
1. `Ball.ApplyKick()` is called first (physics priority)
2. `ShotExecutedEvent` is published immediately after
3. `ShotAnimationData` stub is populated (no publication)

**Rationale for physics-first ordering:** If event processing caused any side effect
that modified ball state before `Ball.ApplyKick()` was called, the physics would be
corrupted. Calling `Ball.ApplyKick()` first is a defensive policy that eliminates
this class of ordering bug. This is consistent with Pass Mechanics event ordering.

For `ShotCancelledEvent`: published in the same frame as the WINDUP → IDLE transition,
before any subsequent system has a chance to read the (now-cancelled) shot state.

---

### 3.10.7 Event Bus Capacity Contract

Before each event publication, a capacity check is performed:

```csharp
if (!eventBus.HasCapacity(EventType.ShotExecuted))
{
    Log.Critical("ShotEventEmitter: Event bus at capacity. ShotExecutedEvent dropped.");
    // Shot proceeds physically — ball is already in flight. Event loss is logged.
    // This is a performance budget violation and must be treated as a critical bug.
}
else
{
    eventBus.Publish(evt);
}
```

**Event bus interface:** The event bus capacity contract and queue implementation are
owned by Event System Spec #17 (not yet written). At Stage 0, a minimal in-process
event bus with adequate capacity for the shot frequency of a 90-minute match (~50 shots
per team) is assumed sufficient. Full capacity specification is deferred to Spec #17.

---

### 3.10.8 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.10-01 | Event bus at capacity | Capacity check before publish | Log critical; drop event; ball physics unaffected |
| FM-3.10-02 | ShotResult contains incomplete data (null fields) | Assert all required fields non-null before population | Log critical; publish partial event with zero fields for non-critical data |
| FM-3.10-03 | ShotExecutedEvent published without Ball.ApplyKick() being called | `applyKickCalled` guard in §3.9 | Cannot occur if §3.9 guard is operative; belt-and-suspenders: assert guard in §3.10 |

---

### 3.10.9 Design Decisions and Rationale

**DD-3.10-01: §3.10 is the sole event publisher — no other sub-system publishes directly**

This ensures a single point of control over the event contract. If the event structure
changes (e.g., new fields for Goalkeeper Mechanics #11), only §3.10 requires modification.
Sub-systems that compute values add them to `ShotResult`; §3.10 reads from `ShotResult`.
This is the same ownership pattern used in Pass Mechanics.

**DD-3.10-02: ShotAnimationData is populated but not published at Stage 0**

Publishing an event with no subscriber wastes event bus capacity and creates a false
impression that the animation system is functional. Populating the stub in `ShotResult`
preserves the data for debugging and future migration. When the Animation System is
implemented in Stage 1+, it subscribes to the event bus and §3.10 is amended to push
the struct — a one-line change.

**DD-3.10-03: Invalid request rejection produces no event (log only)**

An invalid `ShotRequest` means the Decision Tree submitted a structurally broken request.
This is a programming error, not a game event. Publishing an event would cause consumers
to process a meaningless shot. Logging only keeps the event stream clean and makes the
bug immediately visible in diagnostics without corrupting downstream consumers.

---

## Section 3 (Part 2) Summary

| Sub-system | Owner file | Key output | Status |
|---|---|---|---|
| §3.4 Spin Vector Calculation | `SpinVectorCalculator.cs` | `spinVector: Vector3` (rad/s) | ✅ Specified |
| §3.5 Placement Resolution | `PlacementResolver.cs` | `aimDirection: Vector3` (unit) | ✅ Specified |
| §3.6 Error Model | `ErrorCalculator.cs` | `finalDirection: Vector3` (unit) | ✅ Specified |
| §3.7 Body Mechanics Evaluation | `BodyMechanicsEvaluator.cs` | `BodyMechanicsScore`, `bodyLeanAngleDeg`, `ContactQualityModifier`, `stumbleTrigger` | ✅ Specified |
| §3.8 Weak Foot Penalty | `WeakFootPenaltyApplier.cs` | `weakFootErrorMultiplier`, `weakFootVelocityMultiplier` | ✅ Specified |
| §3.9 Shot State Machine | `ShotStateMachine.cs` | Lifecycle; `Ball.ApplyKick()` gate; STUMBLING | ✅ Specified |
| §3.10 Event Publishing | `ShotEventEmitter.cs` | `ShotExecutedEvent`, `ShotCancelledEvent` | ✅ Specified |

**Validation required before Section 3 approval (from both Part 1 and Part 2):**
1. Appendix B numerical verification — all boundary verification table rows computed
   against Ball Physics drag model simulation for §3.2 (velocity) and §3.3 (launch angle)
2. §3.4 spin vectors [VER] — TOPSPIN_BASE and BACKSPIN_BASE verified against Ball Physics
   Magnus parameters to confirm observable trajectory dip and chip check within pitch distances
3. §3.4 sidespin [VER] — handedness convention confirmed consistent with Ball Physics §3.1.4
4. §3.6 BASE_ERROR constants calibrated against StatsBomb elite/poor striker completion rates
5. §3.6 GOAL_RELATIVE_ERROR_SCALE geometric derivation confirmed in Appendix B
6. §3.8 SHOT_WF_BASE_ERROR_PENALTY (0.60) reviewed against [CAREY-2001] extrapolation;
   reclassify as [GT] if academic grounding is insufficient

---

## Full Section 3 Summary — §3.1 through §3.10

| Sub-system | File | Key Output | Source |
|---|---|---|---|
| §3.1 ShotRequest Validation | `ShotValidator.cs` | Pass/fail gate | Part 1 ✅ |
| §3.2 Velocity Calculation | `VelocityCalculator.cs` | `kickSpeed` ∈ [8.0, 35.0] m/s | Part 1 ✅ |
| §3.3 Launch Angle Derivation | `LaunchAngleCalculator.cs` | `launchAngleDeg` ∈ [−5°, 70°] | Part 1 ✅ |
| §3.4 Spin Vector Calculation | `SpinVectorCalculator.cs` | `spinVector: Vector3` | Part 2 ✅ |
| §3.5 Placement Resolution | `PlacementResolver.cs` | `aimDirection: Vector3` | Part 2 ✅ |
| §3.6 Error Model | `ErrorCalculator.cs` | `finalDirection: Vector3` | Part 2 ✅ |
| §3.7 Body Mechanics Evaluation | `BodyMechanicsEvaluator.cs` | BMS + bodyLean + CQM + stumble | Part 2 ✅ |
| §3.8 Weak Foot Penalty | `WeakFootPenaltyApplier.cs` | Error and velocity multipliers | Part 2 ✅ |
| §3.9 Shot State Machine | `ShotStateMachine.cs` | Lifecycle + Ball.ApplyKick() gate | Part 2 ✅ |
| §3.10 Event Publishing | `ShotEventEmitter.cs` | ShotExecutedEvent + ShotCancelledEvent | Part 2 ✅ |

**Pipeline execution order (from §2.2.3, confirmed by Part 2 specification):**

```
INITIATING:
    [1]  §3.7  BodyMechanicsEvaluator   → BodyMechanicsScore, bodyLeanAngleDeg, CQM, stumbleTrigger
    [2]  §3.8  WeakFootPenaltyApplier   → weakFootErrorMultiplier, weakFootVelocityMultiplier

CONTACT:
    [3]  §3.1  ShotValidator            → Validation gate (already passed at INITIATING; re-confirmed)
    [4]  §3.2  VelocityCalculator       → kickSpeed (m/s)
    [5]  §3.3  LaunchAngleCalculator    → launchAngleDeg
    [6]  §3.4  SpinVectorCalculator     → spinVector (rad/s)
    [7]  §3.5  PlacementResolver        → aimDirection (unit vector, 3D)
    [8]  §3.6  ErrorCalculator          → finalDirection (unit vector, post-error)
    [9]  Compose: finalVelocity = finalDirection × kickSpeed
    [10] Ball.ApplyKick(ref ball, finalVelocity, spinVector, agentId, matchTime, logger)
    [11] §3.10 ShotEventEmitter         → ShotExecutedEvent published

FOLLOW_THROUGH / STUMBLING:
    [12] Stumble check → STUMBLING transition + Agent Movement signal (if triggered)
    [13] COMPLETE → IDLE
```

**Section 3 is now fully specified.**

**Next:** Section 4 — Architecture and Integration.

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 22, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. §3.4–§3.10 fully specified. |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | Cross-spec audit corrections: (A2) `SPIN_ABSOLUTE_MAX` corrected 150→80 rad/s — matches Ball Physics `MAX_SPIN` (§3.1.12); marked `[VER]`; note on Inoue (2014) literature vs simulation discrepancy added. (A5) `§3.9.5` tackle interrupt: `OnTackleInterruptReceived()` replaced with `PollTackleInterrupt()` using `CollisionSystem.GetAndClearTackleFlag()` — aligned with Pass Mechanics §4.4.2. (A7) `§3.9.6` stumble: `agentMovementInterface.SignalStumble()` direct call removed; replaced with Mechanism C comment — Agent Movement subscribes to `ShotExecutedEvent.StumbleTriggered`. (A1) Pipeline step [10]: `Ball.ApplyKick()` call updated to include `logger` parameter. |
| 1.2 | February 23, 2026 | Claude (AI) / Anton | §3.6.8 poor-striker example corrected: (1) `weakFootMultiplier` changed from `1.3` → `1.60` — Rating=1 yields penaltyFraction=1.0, so multiplier = 1.0 + 1.0×0.60 = 1.60; the value 1.3 corresponds to Rating=3, not Rating=1. (2) Pre-clamp total corrected from ~28.7° → ~53.7°; post-clamp total stated as 25.0° per §3.6.11 MAX_ERROR_ANGLE clamp. (3) Added note explaining clamp necessity and reference to Appendix B §B.9 verification. Identified via OI-App-B-02 in Appendices v1.0. |
| 1.3 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Version header corrected 1.0→1.2 to match filename. (2) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. |

---

*End of Section 3 (Part 2) — Shot Mechanics Specification #6*
*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
