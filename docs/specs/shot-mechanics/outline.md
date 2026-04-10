# Shot Mechanics Specification #6 — Detailed Outline

**Purpose:** Planning document for Shot Mechanics specification. Establishes scope, structure,
mathematical framework, and technical approach before full section drafting begins.
This document defines *what* will be written, not yet *how* it will be implemented in detail.

**Created:** February 22, 2026, 6:00 PM PST
**Updated:** March 6, 2026, 12:00 PM PST
**Version:** 1.3
**Status:** REVISED — Awaiting Lead Developer Re-approval
**Specification Number:** 6 of 20 (Stage 0)
**Estimated Effort:** ~32 hours
**Dependencies:** Ball Physics (#1), Agent Movement (#2), Collision System (#3), Pass Mechanics (#5)
**Downstream:** Goalkeeper Mechanics (#11) — ShotExecutedEvent struct only; no GK interface until Spec #10 written

**Change Summary (v1.2 → v1.3):**
Comprehensive audit corrections: Specification renumbering cascade applied per PROGRESS.md
and Perception System Spec #7 canonical numbering — Decision Tree #7→#8, Fixed64 Math
Library #8→#9, Heading Mechanics #9→#10, Goalkeeper Mechanics #10→#11. All downstream
references updated.

**Change Summary (v1.1 → v1.2):**
ShotType enum eliminated throughout. Named shot classifications (driven, placed, chip,
volley, half-volley, finesse) were Decision Tree intent labels, not physics primitives.
Ball Physics operates on velocity and spin vectors only — it does not need to know
what the kick was called. Shot Mechanics now receives physical intent parameters from
the Decision Tree and produces physical output. This is consistent with the KickType
resolution established during Pass Mechanics drafting. Twelve locations corrected.

---

## EXECUTIVE SUMMARY

Shot Mechanics governs everything from the moment an agent decides to shoot until the ball leaves
the foot. It is the highest-stakes action in football: low frequency, extreme physical output,
outcome-defining. Unlike passing, shot mechanics must model not just ball trajectory initiation
but also **placement intent vs. execution quality** — the gap between where a player aims and
where the ball actually goes is the central design problem.

The core model:

```
ShotResult = f(PowerIntent, ContactZone, SpinIntent, PlacementTarget) × ErrorVector × BodyMechanics
```

Where:
- `PowerIntent` [0.0–1.0] — how hard the agent attempts to strike the ball
- `ContactZone` — where on the ball face the foot makes contact (Centre / BelowCentre / OffCentre)
- `SpinIntent` [0.0–1.0] — degree of deliberate spin applied (0 = pure power, 1 = maximum curl/chip)
- `PlacementTarget` (u, v) — intended goal-mouth coordinate
- `ErrorVector` — deterministic inaccuracy from technique, pressure, fatigue, body shape
- `BodyMechanics` — stance, run-up angle, plant foot position, contact execution quality

The physical output — velocity magnitude, launch angle, and spin vector — emerges from
these parameters. Named shot labels (driven, chip, finesse, etc.) are Decision Tree
vocabulary describing intent. They are not enums passed to or used by this system.

**Output interface (Ball Physics §3.1.11.2):**
```
Ball.ApplyKick(velocity: Vector3, spin: Vector3, agentId: int, matchTime: float)
```

Shot Mechanics is a superset of Pass Mechanics in physical complexity but a subset in
frequency. It must model extreme velocities (20–35 m/s), high-spin variants, and
deliberate trajectory shaping — capabilities that are edge cases in passing but
central to shooting.

---

## SECTION 1: PURPOSE & SCOPE (~3 pages)

### 1.1 What This Specification Covers

- Translation of physical intent parameters (`PowerIntent`, `ContactZone`, `SpinIntent`,
  `PlacementTarget`) into `Ball.ApplyKick()` arguments
- Ball velocity model: `PowerIntent` × `EffectiveAttribute` × fatigue × contact quality × body shape
- Placement model: intended target on goal mouth (u, v), error distribution in goal-relative space
- Launch angle derivation from `ContactZone`, `PowerIntent`, body lean, and body mechanics score
- Spin vector calculation: topspin (dipping), sidespin (curl), backspin (chip/loft) — magnitudes
  driven by `SpinIntent` and `ContactZone`
- Shot error model: deterministic inaccuracy from Finishing, Composure, pressure, fatigue, lean
- Body mechanics: run-up angle, plant foot offset, contact point on ball
- Weak foot penalty (reuses WeakFootRating from Agent Movement §3.5.6)
- Shot execution state machine (IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE)
- Cancellation handling (tackle interrupts windup)
- Interface to `Ball.ApplyKick()` — same handoff as Pass Mechanics
- Power–accuracy trade-off: the higher `PowerIntent`, the larger the error cone
- `ShotExecutedEvent` publication for downstream consumers (GK Mechanics #11 when written)

### 1.2 What Is OUT of Scope

| Responsibility | Owner Specification |
|---|---|
| Decision to shoot, target selection, and intent parameter values | Decision Tree (#8) |
| Ball trajectory after kick (aerodynamics, Magnus, bounce) | Ball Physics (#1) |
| Goalkeeper save attempt and positioning | Goalkeeper Mechanics (#11) |
| Heading shots / all head contact regardless of height or agent state | Heading Mechanics (#10) |
| Collision detection — tackle interrupt during windup | Collision System (#3) |
| Goal detection and match state update | Match Referee / Event System (#17) |
| Set piece shots (penalties, free kicks) | Set Pieces System (Stage 1+) |
| xG model / shot quality statistics | Statistics Engine (Stage 1+) |
| Agent animation | Animation System (Stage 1+) |

### 1.3 Key Design Decisions

**KD-1: Power–accuracy trade-off is explicit in the formula.**
A player cannot simultaneously maximise velocity and minimise error. The model
applies a `PowerPenalty` scalar that widens the error cone as `PowerIntent` approaches 1.0.
Grounded in biomechanics literature (Lees & Nolan 1998).

**KD-2: Placement intent is goal-relative, not world-space.**
The shooter specifies a normalised target point on the goal mouth `(u, v)` where
`u ∈ [0,1]` (left to right) and `v ∈ [0,1]` (low to high). Error is applied in
goal-relative space, then converted to world-space velocity. Allows clean
"aimed at bottom corner but skied it" outcomes.

**KD-3: Intent parameters are caller-supplied. Shot Mechanics does not classify intent.**
The Decision Tree supplies `PowerIntent`, `ContactZone`, `SpinIntent`, and `PlacementTarget`.
Shot Mechanics translates these into physics. It does not re-derive intent from context,
does not maintain a named shot type taxonomy, and does not override caller-supplied values.
The physical output (velocity magnitude, angle, spin) is the emergent result of the
parameters — not a lookup against a type table.

**KD-4: No probabilistic dice rolls.**
All outcomes are deterministic given identical inputs. Mandatory for replay determinism.

**KD-5: Goalkeeper interface is event-struct only. No IGkResponseSystem until Spec #11 is written.**
Shot Mechanics publishes `ShotExecutedEvent` with full trajectory data. Goalkeeper Mechanics (#11)
will subscribe when written. No placeholder interface, no forward-declared method signatures.
*(Resolved: OI-005 — Option C)*

**KD-6: Scope boundary between Shot Mechanics and Heading Mechanics is contact body part, not ball height or agent state.**
Foot contact → Shot Mechanics, regardless of ball height or whether the agent is airborne.
Head contact → Heading Mechanics. The 0.5m height threshold used by First Touch does NOT apply here.
*(Resolved: OI-002)*

**KD-7: Contact zone on ball is simplified at Stage 0.**
Three contact zones: Centre (low, driven trajectory), BelowCentre (lifted, chip/loft),
OffCentre (curl, sidespin-dominant). These are physical descriptors of foot-to-ball
contact geometry — not shot type labels. Full biomechanical contact-point modelling
deferred to Stage 1+.

---

## SECTION 2: SYSTEM OVERVIEW (~4 pages)

### 2.1 Functional Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-1 | Translate a ShotRequest into a Ball.ApplyKick() call | Must |
| FR-2 | Derive velocity, launch angle, and spin from intent parameters and attributes | Must |
| FR-3 | Apply deterministic error based on attributes, fatigue, pressure, body shape | Must |
| FR-4 | Enforce power–accuracy trade-off | Must |
| FR-5 | Apply weak foot penalty using WeakFootRating | Must |
| FR-6 | Support windup cancellation via tackle interrupt | Must |
| FR-7 | Publish ShotExecutedEvent for downstream consumers | Must |
| FR-8 | Operate within 0.05ms execution budget | Must |
| FR-9 | Produce identical output for identical inputs (determinism) | Must |
| FR-10 | ShotExecutedEvent must carry sufficient data for Goalkeeper Mechanics (#11) to consume when written. No GK interface defined here. | Must |

### 2.2 Physical Parameter Space

The Decision Tree supplies these parameters. The table shows representative value ranges
that produce qualitatively distinct shot trajectories. These are emergent from the physics —
they are reference points for testing and tuning, not an enum.

| Intent Profile | PowerIntent | ContactZone | SpinIntent | Emergent velocity range | Emergent launch angle |
|---|---|---|---|---|---|
| Maximum power, low | 0.85–1.0 | Centre | 0.0–0.1 | 28–35 m/s | 2–8° |
| Placed, accuracy priority | 0.4–0.65 | Centre | 0.0–0.2 | 14–22 m/s | 4–12° |
| Chip over goalkeeper | 0.35–0.55 | BelowCentre | 0.6–1.0 | 10–18 m/s | 20–40° |
| High-velocity, ball dropping | 0.75–1.0 | Centre | 0.1–0.3 | 22–32 m/s | 0–15° |
| Curl to far post | 0.45–0.7 | OffCentre | 0.5–1.0 | 14–24 m/s | 5–18° |

Note: velocity and angle values are emergent outputs — they will be derived and verified
in Appendix B. The ranges above are initial estimates based on StatsBomb benchmarks
and sports science literature, to be confirmed against the governing formulas in §3.2–3.4.

### 2.3 System State Machine

```
IDLE
  └─► INITIATING       (ShotRequest received from Decision Tree)
        └─► WINDUP      (preparation phase — tackle-interruptible)
              └─► CONTACT   (ball strike — Ball.ApplyKick() called)
                    └─► FOLLOW_THROUGH   (post-contact agent state)
                          └─► COMPLETE   (state cleared, ShotExecutedEvent published)

WINDUP ──[tackle interrupt]──► CANCELLED
FOLLOW_THROUGH ──[stumble trigger]──► STUMBLING  (→ Agent Movement handles)
```

Durations (configurable constants — not keyed to shot type labels):
- WINDUP: 150–350ms — scales with `PowerIntent` (higher power = longer preparation)
  and `SpinIntent` (high spin requires more deliberate contact setup)
- CONTACT: single frame (deterministic)
- FOLLOW_THROUGH: 200–400ms (cosmetic, does not affect ball)

### 2.4 Data Flow

```
Decision Tree (#8)
  └─► ShotRequest {PowerIntent, ContactZone, SpinIntent, PlacementTarget, IsWeakFoot, ...}
        └─► ShotMechanics.ExecuteShot()
              ├─► PowerCalculator          → kick_speed (m/s)
              ├─► PlacementResolver        → launch_direction (Vector3)
              ├─► SpinCalculator           → spin_vector (Vector3)
              ├─► ErrorApplicator          → perturbed_direction (Vector3)
              ├─► BodyMechanicsEvaluator   → BodyMechanicsScore, final_velocity (Vector3)
              └─► Ball.ApplyKick(velocity, spin, agentId, matchTime)
                    └─► BallPhysics simulates from here
              └─► EventBus.Publish(ShotExecutedEvent)
```

### 2.5 Interfaces Required (Upstream Dependencies)

| Interface | Source Spec | Status |
|---|---|---|
| `Ball.ApplyKick()` | Ball Physics #1 §3.1.11.2 | ✅ Approved |
| `BallState` struct | Ball Physics #1 §3.1.2 | ✅ Approved |
| `PlayerAttributes.Finishing` (1–20) | Agent Movement #2 §3.5.6 | ✅ Approved |
| `PlayerAttributes.LongShots` (1–20) | Agent Movement #2 §3.5.6 | ✅ Approved |
| `PlayerAttributes.KickPower` (1–20) | Agent Movement #2 §3.5.6 v1.3 | ✅ Approved |
| `PlayerAttributes.Composure` (1–20) | Agent Movement #2 §3.5.6 | ✅ Approved |
| `PlayerAttributes.WeakFootRating` (1–5) | Agent Movement #2 §3.5.6 v1.3 | ✅ Approved |
| `PlayerAttributes.Technique` (1–20) | Agent Movement #2 §3.5.6 | ✅ Approved |
| `AgentPhysicalProperties` struct | Agent Movement #2 §3.5.4 | ✅ Approved |
| Tackle interrupt flag (windup cancel) | Collision System #3 | ✅ Approved |

### 2.6 Interfaces Produced (Downstream Dependencies)

| Interface | Consumer Spec | Status |
|---|---|---|
| `Ball.ApplyKick()` call | Ball Physics #1 | Stable |
| `ShotExecutedEvent` | Goalkeeper Mechanics #11 | Not yet written — event struct only |
| `ShotExecutedEvent` | Event System #17 | Not yet written |
| `ShotAnimationData` struct | Animation System (Stage 1) | Deferred — stub populated, unconsumed |

---

## SECTION 3: TECHNICAL SPECIFICATIONS (~14 pages — largest section)

### 3.1 ShotRequest Data Structure

Caller-supplied inputs from Decision Tree (#8). No shot type enum.

```
ShotRequest {
    ShootingAgentId    : int
    PowerIntent        : float [0.0–1.0]   // 1.0 = maximum power attempt
    ContactZone        : ContactZone        // Centre | BelowCentre | OffCentre
    SpinIntent         : float [0.0–1.0]   // 0.0 = no deliberate spin, 1.0 = maximum
    PlacementTarget    : Vector2            // (u,v) in goal-relative coords
    IsWeakFoot         : bool
    DistanceToGoal     : float             // metres — supplied by Decision Tree
    MatchTime          : float
}
```

`ContactZone` is a 3-value enum describing foot-to-ball contact geometry. It is a
physical descriptor, not a shot label. The Decision Tree supplies it based on what
contact the agent is attempting to make, not what the shot is called.

### 3.2 Velocity (Power) Model

**Attribute blending by distance (OI-001 — Option B):**

`LongShots` and `Finishing` are blended continuously using sigmoid weight `w(d)`
where `d` is `ShotRequest.DistanceToGoal`. At close range, `Finishing` dominates;
at long range, `LongShots` dominates. No discontinuous threshold.

```
w(d) = sigmoid((d − D_MID) / D_SCALE)       // w ∈ [0, 1]
EffectiveAttribute = (1 − w(d)) × Finishing + w(d) × LongShots
```

Calibration constants (gameplay-tuned [GT]):
- `D_MID` = 20m (blend midpoint — roughly the penalty area edge)
- `D_SCALE` = 8m (controls blend sharpness)

**Governing velocity formula:**

```
v_shot = V_BASE(PowerIntent, EffectiveAttribute)
         × ContactZoneModifier[ContactZone]
         × (1 − SpinIntent × SPIN_VELOCITY_TRADE_OFF)
         × (1 − fatigue × FATIGUE_POWER_REDUCTION)
         × ContactQualityModifier
         × WeakFootModifier
```

Where:
```
V_BASE(p, a) = V_MIN + (a / ATTR_MAX) × (V_MAX − V_MIN) × p
```

- `V_MIN` = 10 m/s (minimum achievable shot velocity — floor)
- `V_MAX` = 35 m/s (maximum achievable shot velocity — ceiling; world-class Finishing,
  maximum PowerIntent, perfect body mechanics)
- `ContactZoneModifier`: Centre = 1.0, BelowCentre = 0.75, OffCentre = 0.85 [GT]
- `SPIN_VELOCITY_TRADE_OFF` [GT]: deliberate spin requires a less-than-maximal strike;
  a full chip (SpinIntent 1.0) sacrifices ~25% velocity vs. same PowerIntent driven shot
- `KickPower` applies an additional secondary multiplier at high `PowerIntent` values
  where raw physical power is the dominant contributor

Velocity ranges emerge from the formula rather than being looked up by type name.
The reference ranges in §2.2 will be verified against this formula in Appendix B.

### 3.3 Placement and Launch Direction Model

Two-phase resolution:

**Phase 1 — Intended direction:**
Convert `PlacementTarget (u, v)` to world-space unit vector toward target point on goal mouth.

**Phase 2 — Error application:**
```
ErrorMagnitude = BASE_ERROR
                 × (1 − NormalisedFinishing)
                 × PowerPenalty(PowerIntent)
                 × PressureScalar
                 × FatigueScalar
                 × OrientationPenalty

ErrorAngle = ErrorMagnitude × DeterministicHash(matchSeed, agentId, frameNumber)
```

Where:
```
PowerPenalty(p) = 1 + POWER_PENALTY_COEFFICIENT × p²
```

Quadratic growth: error increases slowly at moderate power, sharply at maximum effort.
Consistent with Lees & Nolan (1998).

`DeterministicHash` uses `matchSeed + agentId + frameNumber` as combined seed.
Guarantees two agents shooting on the same frame produce distinct error vectors.
Signature is identical to Spec #15 RNG — migration is a drop-in replacement.
*(OI-004 — Option A)*

### 3.4 Launch Angle Model

Launch angle emerges from contact geometry and body mechanics — not from a type table.

```
LaunchAngle = BaseAngle[ContactZone]
              + PowerLiftModifier × (1 − PowerIntent)     // lower power = slightly more loft
              + SpinLiftModifier × SpinIntent              // BelowCentre + high SpinIntent = chip arc
              + BodyLeanPenalty                            // backward lean = unintended loft
              + BodyShapePenalty                           // poor stance = unpredictable angle
```

Where:
- `BaseAngle[Centre]`       = 4° [GT]
- `BaseAngle[BelowCentre]`  = 18° [GT]
- `BaseAngle[OffCentre]`    = 8° [GT]

`BodyLeanPenalty` is the primary source of shots skied over the bar — a leading cause
of missed shots in real football and a key differentiator from passing.

### 3.5 Spin Vector Model

Spin components are driven by `ContactZone` and `SpinIntent`, not by shot type label.

```
Spin.Topspin  = TopspinBase[ContactZone]  × (1 − SpinIntent) × PowerIntent
Spin.Sidespin = SidespinBase[ContactZone] × SpinIntent × (Technique / ATTR_MAX)
Spin.Backspin = BackspinBase[ContactZone] × SpinIntent
```

Where base values by zone [GT]:
- `Centre`:      TopspinBase high, SidespinBase low,  BackspinBase negligible
- `BelowCentre`: TopspinBase low,  SidespinBase low,  BackspinBase high
- `OffCentre`:   TopspinBase low,  SidespinBase high, BackspinBase low

`Technique` modulates sidespin magnitude — a high-Technique player can apply more
deliberate curl for a given `SpinIntent`. This drives curl radius variation.

Spin feeds directly into `Ball.ApplyKick()` spin parameter, processed by
Ball Physics Magnus effect model (Ball Physics §3.1.4).

### 3.6 Body Mechanics Model

Captures physical shooting stance quality. Inputs from AgentPhysicalProperties:

- **Run-up angle**: Ideal ~30–45° to goal line. Deviation degrades power and accuracy.
- **Plant foot offset**: Too close = cramped, reduced power. Too far = off-balance, accuracy penalty.
- **Agent velocity at contact**: Moving toward goal aids power; moving away degrades it.
- **Body lean**: Forward lean → low shot; backward lean → ballooned shot.

Collapsed to scalar `BodyMechanicsScore ∈ [0.0, 1.0]`. Score modulates:
- `ContactQualityModifier` in §3.2
- `BodyShapePenalty` in §3.4
- STUMBLING trigger in §3.8

Body mechanics are **read-only inputs** from Agent Movement — Shot Mechanics does not
mutate agent state during calculation.

### 3.7 Weak Foot Penalty Model

Identical architecture to Pass Mechanics §3.6:

```
WeakFootModifier = 1.0                          (dominant foot)
WeakFootModifier = WEAK_FOOT_BASE               (WeakFootRating = 1)
                 + (WeakFootRating − 1) / 4
                   × (1.0 − WEAK_FOOT_BASE)    (interpolated for 2–5)
```

Additional penalty vs. passing: weak foot shots suffer larger error cone expansion.
Shots demand more precise contact than passes; the same WeakFootRating produces a
larger accuracy deficit on a shot than on a pass.

### 3.8 Execution State Machine (Detail)

Mirrors Pass Mechanics state machine architecture.

Key differences from passing:
- WINDUP duration scales with `PowerIntent` and `SpinIntent` — not keyed to a type label
- CONTACT phase reads all intent parameters to determine single-frame velocity calculation
- FOLLOW_THROUGH: conditionally emits STUMBLING trigger to Agent Movement if
  `BodyMechanicsScore < STUMBLE_THRESHOLD` AND `PowerIntent > STUMBLE_POWER_THRESHOLD`.
  Reuses existing Agent Movement hysteresis pattern (§3.1) — no new interface required.
  *(OI-003)*
- CANCELLED state: ball remains at shooter's feet in ROLLING state (tackle successful)

Scope boundary: foot contact routes here regardless of agent airborne or grounded state.
Head contact always routes to Heading Mechanics (#10). *(OI-002)*

### 3.9 Edge Cases and Robustness

| Case | Handling |
|---|---|
| Shot from outside pitch boundary | Clamp agent position to valid zone; proceed |
| PowerIntent = 0.0 | Apply V_MIN floor; log warning |
| SpinIntent = 1.0 with Centre ContactZone | Proceed — result is heavy topspin with velocity trade-off |
| Shot blocked by teammate in path | Collision System handles post-kick; out of scope here |
| Agent Finishing = 1 (minimum) | Formula degrades cleanly to high-error, low-power output |
| ShotRequest arrives during non-IDLE state | Reject with log; Decision Tree must not double-submit |
| NaN / Infinity in velocity output | Safety clamp to V_MAX; log critical error |
| DistanceToGoal = 0.0 | Clamp to 1.0m minimum; log warning |

---

## SECTION 4: ARCHITECTURE & INTEGRATION (~4 pages)

### 4.1 Class Structure

```
ShotMechanicsSystem (main orchestrator)
  ├── ShotStateManager         (execution state machine)
  ├── ShotVelocityCalculator   (§3.2 power model)
  ├── ShotPlacementResolver    (§3.3 direction + error)
  ├── ShotSpinCalculator       (§3.5 spin vectors)
  ├── BodyMechanicsEvaluator   (§3.6 stance quality)
  ├── ShotErrorApplicator      (deterministic hash error)
  └── ShotEventEmitter         (ShotExecutedEvent publisher)

Data Structures:
  ShotRequest     (caller input — intent parameters, no type enum)
  ShotContext     (internal computation context)
  ShotResult      (output + diagnostics)
  ShotConstants   (all tunable values — single source of truth)
```

### 4.2 Dependencies on Upstream Specs

Identical dependency pattern to Pass Mechanics. No new interface patterns introduced.

### 4.3 ShotExecutedEvent — Goalkeeper Interface Boundary

Shot Mechanics publishes `ShotExecutedEvent` at CONTACT state. No shot type field —
the physical vectors carry all information GK Mechanics needs to assess save difficulty.

```
ShotExecutedEvent {
    ShootingAgentId    : int
    KickVelocity       : Vector3      // full velocity vector — direction + magnitude
    KickSpin           : Vector3      // spin vector — encodes curl, dip, loft implicitly
    PowerIntent        : float
    SpinIntent         : float
    EstimatedTarget    : Vector2      // goal-relative (u,v) — intended, pre-error
    MatchTime          : float
    BodyMechanicsScore : float        // for GK difficulty calculation in Spec #11
}
```

**No `IGkResponseSystem` interface is defined here.** Goalkeeper Mechanics (#11) will
subscribe when that spec is written. *(OI-005 — Option C)*

### 4.4 Collision System Interface (Tackle Interrupt)

Identical to Pass Mechanics pattern (polling flag during WINDUP state).
No new interface required.

### 4.5 Performance Budget

Shot Mechanics executes as a discrete event (not per-frame), identical to Pass Mechanics.

| Operation | Estimated Cost |
|---|---|
| State machine transition | ~0.001ms |
| Velocity calculation (including sigmoid blend) | ~0.004ms |
| Placement + error resolution | ~0.005ms |
| Spin calculation | ~0.002ms |
| Body mechanics evaluation | ~0.003ms |
| Ball.ApplyKick() call | ~0.001ms |
| Event emission | ~0.001ms |
| **Total** | **~0.017ms** |

Target budget: 0.05ms. Estimated headroom: ~3×. Comfortable.

---

## SECTION 5: TESTING (~5 pages)

### 5.1 Test Categories and Targets

| Category | Prefix | Target Count | Notes |
|---|---|---|---|
| Parameter Validation | PV-* | 8 | ShotRequest field ranges, ContactZone values |
| Velocity Model | SV-* | 12 | Power curve, attribute blend, weak foot, fatigue |
| Placement Resolution | SP-* | 10 | Target corners, centre, error distribution |
| Launch Angle | LA-* | 8 | ContactZone variants, body shape penalties |
| Spin Vector | SN-* | 8 | Topspin, sidespin, backspin by ContactZone + SpinIntent |
| Error Model | SE-* | 10 | Composure, pressure, power penalty |
| Body Mechanics | BM-* | 8 | Run-up angle, plant foot, lean |
| Weak Foot | WF-* | 6 | Rating 1–5, dominant vs non-dominant |
| State Machine | SSM-* | 8 | All transitions including cancel and stumble |
| Edge Cases | EC-* | 8 | Boundaries, NaN guards, floor/ceiling |
| Integration | IT-* | 12 | Full pipeline: Request → ApplyKick |
| Validation Scenarios | VS-* | 6 | Real-world shot outcome comparison |
| **Total** | | **~104** | 6.9× minimum requirement |

**Minimum requirement:** 10 unit + 5 integration = 15 minimum.
Targeting ~100+ consistent with Pass Mechanics standard.

### 5.2 Key Integration Tests

- **IT-001:** `PowerIntent 1.0`, top-rated Finishing 20 / KickPower 20, zero fatigue, zero pressure,
  Centre contact → verify velocity ≥ 32 m/s, error < 2°
- **IT-002:** Same parameters, dominant vs. weak foot → verify power and error delta matches
  WeakFootRating model
- **IT-003:** `PowerIntent 1.0`, Finishing 8, Centre contact → verify large error cone,
  velocity in mid-range
- **IT-004:** `PowerIntent 0.45`, `SpinIntent 0.8`, BelowCentre contact → verify backspin
  dominant, launch angle 25–35°
- **IT-005:** Tackle interrupt during WINDUP → verify CANCELLED state, ball in ROLLING state,
  no ApplyKick called
- **IT-006:** `SpinIntent 0.8`, OffCentre contact, Technique 18 → verify sidespin magnitude
  produces curl radius 8–15m when processed by Ball Physics Magnus model

### 5.3 Validation Scenarios (Real-World Comparison)

- **VS-001:** Maximum PowerIntent, Finishing 20, Centre contact → velocity ≤ 35 m/s
  (Ronaldo/Bale benchmark — StatsBomb)
- **VS-002:** PowerIntent 0.55, Centre contact → velocity range 14–22 m/s (side-foot finish benchmark)
- **VS-003:** BelowCentre, SpinIntent 0.8, PowerIntent 0.45 from 18m → hang time > 1.5s
  (chip over goalkeeper — visual plausibility)
- **VS-004:** PowerIntent 1.0, Composure 1, high pressure → large error; shot skied or wide
  (qualitative validation)
- **VS-005:** WeakFootRating 1 → ≥ 40% more directional error vs. dominant foot
- **VS-006:** Run-up angle deviation > 45° from ideal → measurable accuracy penalty in error formula

---

## SECTION 6: PERFORMANCE ANALYSIS (~2 pages)

Mirrors Pass Mechanics Section 6 structure:
- Operation count breakdown per pipeline stage
- Memory layout (ShotContext struct, cache-line analysis)
- Worst-case scenario (simultaneous shots — theoretically impossible but bounded)
- Fixed64 migration notes (same deferred approach as all Stage 0 specs)

---

## SECTION 7: FUTURE EXTENSIONS (~2 pages)

| Feature | Stage | Rationale |
|---|---|---|
| xG output from Shot Mechanics | Stage 1 | Requires Statistics Engine |
| Set piece shots (penalties, free kicks) | Stage 1+ | Separate system; distinct physics profile |
| Body part selection (laces, instep, outside) — extends ContactZone | Stage 1 | Requires animation integration |
| Surface effects (wet pitch reduces traction) | Stage 2 | Requires Pitch Condition System |
| Player-specific shot tendencies | Stage 2 | Extended attribute system |
| Full contact-point biomechanics | Stage 1 | Simplified ContactZone at Stage 0 |
| Deflection trajectory modification | Stage 1 | Collision System extension |
| Goalkeeper-informed shot selection | Stage 1 | AI system integration |

---

## SECTION 8: REFERENCES (~2 pages)

### Literature Sources (to be cited in full in Section 8)

| Ref | Source | Used For |
|---|---|---|
| [LEES-1998] | Lees & Nolan (1998), J. Sports Sciences | Power–accuracy trade-off biomechanics |
| [INOUE-2014] | Inoue et al. (2014), Procedia Engineering | Ball speed and spin relationship |
| [ASAI-2002] | Asai et al. (2002), Sports Engineering | Curved shot (finesse) spin mechanics |
| [CARRE-2004] | Carré et al. (2004), Sports Engineering | Ball aerodynamics (reused from Ball Physics #1) |
| [STATSBOMB] | StatsBomb Open Data | Shot velocity benchmarks, xG context |
| [FIFA-QUALITY] | FIFA Quality Programme (2024) | Ball behaviour standards (reused from Ball Physics #1) |

### Gameplay-Tuned Constants

Estimated ~50% of constants will be gameplay-tuned vs. literature-derived.
Consistent with Pass Mechanics (~55%). All [GT] values explicitly flagged with
tuning guidance per project standard.

---

## SECTION 9: APPROVAL CHECKLIST

Standard template (same structure as all prior specs). Completed after all sections
pass self-critique.

---

## APPENDICES

- **Appendix A:** Mathematical Derivations — V_BASE formula derivation, sigmoid blend
  derivation, error cone geometry, spin-to-curl radius derivation
- **Appendix B:** Numerical Verification — hand-calculated test cases for IT-001 through
  IT-006; verification that §2.2 reference ranges emerge correctly from §3.2–3.4 formulas
- **Appendix C:** Sensitivity Analysis — how output changes per ±10% variation in key
  constants; ContactZoneModifier sensitivity; SPIN_VELOCITY_TRADE_OFF sensitivity

---

## DESIGN DECISIONS LOG

| # | Issue | Decision | Rationale |
|---|---|---|---|
| OI-001 | LongShots vs. Finishing weighting | **Continuous sigmoid blend by distance.** `D_MID` = 20m, `D_SCALE` = 8m. | Continuous variable drives continuous blend. Hard threshold produces discontinuous feel. |
| OI-002 | Foot/heading scope boundary | **Contact body part is the discriminator.** Foot → Shot Mechanics. Head → Heading Mechanics. Height and airborne state irrelevant. | Unambiguous. No edge cases at height boundary. |
| OI-003 | STUMBLING trigger | **Conditional.** `BodyMechanicsScore < STUMBLE_THRESHOLD` AND `PowerIntent > STUMBLE_POWER_THRESHOLD`. Emits to Agent Movement. | Physical authenticity. Reuses existing hysteresis pattern — no new interface. |
| OI-004 | Deterministic hash seed | **`matchSeed + agentId + frameNumber`.** | Only construction guaranteeing distinct vectors for two agents on the same frame. |
| OI-005 | GK interface depth | **Event struct only. No IGkResponseSystem until Spec #11 written.** | Consistent with project interface principle. |
| OI-006 | ShotType enum | **Eliminated.** Decision Tree supplies physical intent parameters (`PowerIntent`, `ContactZone`, `SpinIntent`). Named shot labels are Decision Tree vocabulary, not physics primitives. Shot Mechanics translates parameters to vectors — it does not classify intent. | Consistent with KickType resolution in Pass Mechanics. Ball Physics operates on velocity and spin only. Type enum carries zero physics weight and would create false coupling. |

---

## OPEN ITEMS TRACKER

All items resolved. No open blockers.

| # | Item | Status | Resolution |
|---|---|---|---|
| OI-001 | LongShots vs. Finishing weighting | ✅ Resolved | Continuous sigmoid blend |
| OI-002 | Foot/heading scope boundary | ✅ Resolved | Contact body part is discriminator |
| OI-003 | STUMBLING trigger mechanism | ✅ Resolved | Conditional on BodyMechanicsScore + PowerIntent |
| OI-004 | Deterministic hash seed | ✅ Resolved | matchSeed + agentId + frameNumber |
| OI-005 | GK event interface depth | ✅ Resolved | Event struct only; no IGkResponseSystem |
| OI-006 | ShotType enum | ✅ Resolved | Eliminated — parameter-based model |

---

## ESTIMATED EFFORT

| Section | Est. Hours | Complexity |
|---|---|---|
| Section 1 (Purpose & Scope) | 1.5 | Low |
| Section 2 (System Overview) | 2.5 | Low |
| Section 3 (Technical Specs) | 12 | **High** |
| Section 4 (Architecture & Integration) | 3 | Medium |
| Section 5 (Testing) | 5 | Medium |
| Section 6 (Performance) | 1.5 | Low |
| Section 7 (Future Extensions) | 1 | Low |
| Section 8 (References) | 2 | Low |
| Appendices | 2.5 | Medium |
| Section 9 (Approval Checklist) | 1 | Low |
| **Total** | **~32 hours** | |

---

**END OF OUTLINE**

---
*Tactical Director — Specification #6 of 20*
*Stage 0: Physics Foundation*
*v1.3 — Spec renumbering cascade applied. ShotType enum eliminated. Awaiting lead developer re-approval before Section 1 re-draft.*
