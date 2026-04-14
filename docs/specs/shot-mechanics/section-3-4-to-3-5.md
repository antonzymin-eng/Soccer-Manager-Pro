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

