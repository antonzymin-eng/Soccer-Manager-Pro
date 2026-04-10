# Perception System Specification #7 — Section 3: Core Models

**File:** `Perception_System_Spec_Section_3_v1_2.md`  
**Purpose:** Defines all mathematical models, data structures, and computational procedures
governing `PerceptionSnapshot` production. This section is the implementation authority for
the field of view model, shadow-cone occlusion, recognition latency, blind-side awareness,
shoulder check mechanic, ball perception, pressure scalar integration, and the complete
`PerceptionSnapshot` struct definition. All constants are audit-tagged. All formulas include
worked examples with numerical verification.

**Created:** February 24, 2026, 3:00 PM PST  
**Version:** 1.2  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections:**
- Section 1 v1.1 (approved) — scope, KD-1 through KD-7 locked
- Section 2 v1.0 (approved) — pipeline, functional requirements, preliminary struct

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 24, 2026, 3:00 PM PST | Initial draft |
| 1.1 | February 25, 2026 | Four fixes: (1) Peripheral arc boundary derived from BASE_FOV_HALF_ANGLE/2 (40°) — removes arbitrary 60° magic number. (2) Confirmation expiry reduced to 1 tick [DERIVED] — minimum sufficient to absorb single-tick boundary noise, not GT-tuned. (3) Noise changed to additive-only (+0/+1) — preserves L_MIN floor algebraically without secondary clamp. (4) Constants table updated: 12 GT, 3 CROSS, 2 DERIVED (was 12 GT, 3 CROSS, 1 GT). |
| 1.2 | February 26, 2026 | One fix: (1) §3.3.2 — L_rec rounding convention made explicit: floor() required (Mathf.FloorToInt). Previously the formula showed a float result with no documented conversion to integer ticks. Verification table updated to show float and floored-tick columns separately. This is a clarification only — the floor convention was always implied by the integer tick system; no constant values change. |

**Cross-Specification Constants Consumed (read-only):**
- First Touch Spec #4 §3.3.2: Half-turn orientation bonus = 15% L_rec reduction
- First Touch Spec #4 §3.5: Pressure scalar formula (CalculatePressureScalar) — reused verbatim
- Agent Movement Spec #2: `PlayerAttributes.Decisions` [1–20], `PlayerAttributes.Anticipation` [1–20]
- Collision System Spec #3: `spatialHash.QueryRadius(position, radius)` API

---

## Table of Contents

- [3.0 Pipeline Execution Order](#30-pipeline-execution-order)
- [3.1 Field of View Model](#31-field-of-view-model)
- [3.2 Shadow Cone Occlusion Model](#32-shadow-cone-occlusion-model)
- [3.3 Recognition Latency Model](#33-recognition-latency-model)
- [3.4 Blind-Side Awareness and Shoulder Check](#34-blind-side-awareness-and-shoulder-check)
- [3.5 Ball Perception](#35-ball-perception)
- [3.6 Pressure Scalar Integration](#36-pressure-scalar-integration)
- [3.7 PerceptionSnapshot Struct Definition](#37-perceptionsnapshot-struct-definition)
- [3.8 Mid-Heartbeat Forced Refresh](#38-mid-heartbeat-forced-refresh)
- [3.9 Worked Examples](#39-worked-examples)
- [3.10 Constants Master Table](#310-constants-master-table)

---

## 3.0 Pipeline Execution Order

The complete per-agent perception pipeline executes once per 10Hz heartbeat. All six steps
are executed in strict order for a given agent before proceeding to the next agent. The
pipeline is not parallel at Stage 0; all 22 agents are processed sequentially within the
heartbeat window. Section 6 (Performance) specifies timing budgets.

```
Per-Agent Perception Pipeline (10Hz, sequential):

Step 1: CacheAttributes()
        Read AgentState.Position, AgentState.FacingDirection,
        PlayerAttributes.Decisions, PlayerAttributes.Anticipation.
        Cache for duration of this heartbeat. Attribute changes mid-heartbeat
        do not affect the in-flight computation (KD-2 rationale).

Step 2: QueryNearbyEntities()
        spatialHash.QueryRadius(observer.Position, MAX_PERCEPTION_RANGE)
        Returns: all AgentState entries + BallState within range.
        This is the candidate set — no filtering has occurred yet.

Step 3: ApplyFieldOfView()
        For each candidate entity: compute angular separation from FacingDirection.
        Discard entities outside EffectiveFoV half-angle.
        Compute EffectiveFoV first (requires PressureScalar — §3.6 called here).
        Result: FoV-filtered candidate set.

Step 4: ApplyOcclusionFilter()
        For each remaining opponent: compute shadow cone angular interval.
        For each candidate entity: check bearing against all shadow cones.
        Discard occluded entities (mark IsOccluded = true in debug record).
        Ball uses same occlusion test. Teammates do not cast shadow cones (OQ-1).

Step 5: ApplyRecognitionLatency()
        For each entity that survived Steps 3–4:
          - Ball: immediate (no latency). Proceed to Step 6 directly.
          - Agents: check latency counter. Increment if < L_rec. Only expose
            entities where counter has reached L_rec threshold.
        Entities in latency accumulation are tracked internally but NOT included
        in VisibleTeammates / VisibleOpponents. They are invisible to the DT.

Step 6: ApplyBlindSideAwareness()
        If shoulder check window is active (BlindSideWindowActive == true):
          Re-query entities in the blind-side arc (200° rear).
          These entities enter a separate L_rec accumulation queue.
          Only confirmed blind-side entities enter snapshot this heartbeat.
        Decrement window timer. Set BlindSideWindowActive = false if expired.
        Check if autonomous shoulder check fires this tick (§3.4.2).

Step 7: BuildPerceptionSnapshot()
        Assemble PerceptionSnapshot struct (§3.7).
        Populate all fields. Copy by value to Decision Tree intake.
        Publish PerceptionRefreshEvent if forced-refresh triggered (§3.8).
```

**Execution guarantee:** All 22 agents complete the full pipeline within 2ms per heartbeat
(100ms window). Per-agent budget: ~90µs. Full breakdown in Section 6.

---

## 3.1 Field of View Model

### 3.1.1 Theoretical Basis

Human peripheral vision spans approximately 180° of visual arc in the horizontal plane
[WILLIAMS-1998]. However, effective football-relevant perception — sufficient to recognise
agent positions, movement directions, and tactical intent — degrades significantly beyond
approximately 80° from the fixation axis. A defender tracking a ball cannot simultaneously
maintain reliable positional awareness of teammates at 90° peripheral to their gaze.

The base FoV of 160° is grounded in the Franks & Miller (1985) observation that experienced
players demonstrate reliable scene recall for elements within ~80° of their primary gaze
direction, with recall accuracy dropping sharply beyond that threshold [FRANKS-1985].

### 3.1.2 Base FoV Geometry

The field of view is a symmetric angular cone centered on `AgentState.FacingDirection`.

```
BASE_FOV_ANGLE = 160°  [GT — within Franks 1985 plausibility range of 140°–180°]
BASE_FOV_HALF_ANGLE = 80°

FoV test for candidate entity E:
  bearing_E = atan2(E.Position.y - Observer.Position.y,
                    E.Position.x - Observer.Position.x)
  angularSeparation = |bearing_E - observer.FacingAngle|
  
  // Wrap to [-180°, +180°]
  if (angularSeparation > 180°) angularSeparation -= 360°

  inFoV = (|angularSeparation| <= EffectiveFoV_HalfAngle)
```

All angular arithmetic is performed in float degrees at Stage 0. Migration to Fixed64
radians is documented in Section 7.

### 3.1.3 Attribute Modifier: Decisions

The `Decisions` attribute [1–20] represents a player's scanning efficiency and cognitive
processing bandwidth. Higher Decisions produces a marginally wider effective FoV because
high-Decisions players actively maintain broader spatial awareness rather than narrowing
fixation under cognitive load [WILLIAMS-1998].

```
MAX_FOV_BONUS_ANGLE = 10°  [GT — represents ~6% widening; calibrated against 
                             Williams & Davids 1998 expert-novice scanning data]

EffectiveFoV_BeforePressure = BASE_FOV_ANGLE + ((Decisions / 20.0f) × MAX_FOV_BONUS_ANGLE)
```

**Verification table:**

| Decisions | EffectiveFoV_BeforePressure |
|-----------|----------------------------|
| 1         | 160.5°                     |
| 5         | 162.5°                     |
| 10        | 165.0°                     |
| 15        | 167.5°                     |
| 20        | 170.0°                     |

*Note:* The Decisions modifier is intentionally small (+10° across the full range). Decisions
primarily governs recognition speed (§3.3), not vision width. A Decisions=1 player does not
have a dramatically narrower view; they are slower to act on what they see. The width bonus
is secondary and represents maintained scanning discipline, not physical eye geometry.

### 3.1.4 Pressure Degradation of FoV

Under pressure, attentional narrowing reduces effective FoV angle. This models the
empirically documented phenomenon of attentional tunnel-vision under psychological stress
[BEILOCK-2010]. The effect degrades FoV *breadth* — fewer agents enter the visible cone —
without affecting the fidelity of entities that are visible (KD-7).

```
// PressureScalar in [0.0, 1.0] from §3.6
// MAX_FOV_PRESSURE_REDUCTION = 30°  [GT — Beilock 2010: 20-50% performance degradation; 
//                                     30° on 170° range ≈ 18% — within lower bracket]
// MIN_FOV_ANGLE = 120°  [GT — absolute floor; prevents degenerate near-zero cone]

EffectiveFoV = EffectiveFoV_BeforePressure - (PressureScalar × MAX_FOV_PRESSURE_REDUCTION)
EffectiveFoV = Max(EffectiveFoV, MIN_FOV_ANGLE)
EffectiveFoV_HalfAngle = EffectiveFoV / 2.0f
```

**Pressure degradation examples:**

| Decisions | PressureScalar | EffectiveFoV_BeforePressure | Reduction | EffectiveFoV |
|-----------|----------------|-----------------------------|-----------|--------------|
| 10        | 0.0            | 165°                        | 0°        | 165°         |
| 10        | 0.5            | 165°                        | 15°       | 150°         |
| 10        | 1.0            | 165°                        | 30°       | 135°         |
| 1         | 1.0            | 160.5°                      | 30°       | 130.5°       |
| 20        | 1.0            | 170°                        | 30°       | 140°         |

*Observation:* Even under full pressure (PressureScalar = 1.0), no agent falls below 130°
effective FoV, well above the MIN_FOV_ANGLE floor of 120°. The floor only activates under
extreme edge-case combinations. This is the intended design — the floor is a safety net,
not a regularly-reached value.

### 3.1.5 Blind-Side Arc Derivation

The blind-side arc is the angular complement of the forward FoV cone. It is centered on
the direction exactly opposite to `FacingDirection`.

```
BlindSide_Arc = 360° - EffectiveFoV
```

Because EffectiveFoV ranges from 120° (floor) to 170° (maximum), the blind-side arc ranges
from 190° to 240°. At the neutral base case (Decisions=10, PressureScalar=0):

```
EffectiveFoV = 165°
BlindSide_Arc = 360° - 165° = 195°
BlindSide_HalfAngle = 97.5° on each side of the rear-facing vector
```

**Design note — KD-5 restatement:** The blind-side arc width is a direct mathematical
consequence of EffectiveFoV, which itself varies by attribute and pressure. What is fixed
(KD-5) is that the *structure* of the blind side is purely geometric — the agent genuinely
cannot perceive anything there unless a shoulder check fires. What varies continuously is
the exact boundary angle as EffectiveFoV varies. Anticipation controls check *frequency*,
not the arc geometry.

---

## 3.2 Shadow Cone Occlusion Model

### 3.2.1 Design Rationale

Full geometric raycasting against 22 agent bodies at 10Hz is unjustified at Stage 0 given
the tactical-resolution output. An opponent standing between observer and target should
create a plausible occlusion effect; the exact angular precision of a true bodycast is
irrelevant at this spatial scale. The shadow cone approximation is conservative — it
slightly over-occludes at very close range — which produces the correct behavioural error
direction: agents err toward *caution*, not toward *false confidence*. (KD-3).

Only opponents cast shadow cones at Stage 0. Teammate occlusion is deferred (OQ-1).

### 3.2.2 Occluder Body Radius

Each agent occupies a physical footprint. For shadow cone projection, agents are modelled
as vertical cylinders.

```
AGENT_BODY_RADIUS = 0.4m  [GT — adult human shoulder half-width approximation;
                            FIFA-standard agent capsule radius from Agent Movement §3.2]
```

### 3.2.3 Shadow Cone Geometry

For each opponent O that is in the observer's candidate set (within MAX_PERCEPTION_RANGE,
before FoV filtering), compute the angular shadow interval O casts from the observer's
perspective.

```
// Vector from observer to occluder
occluderVector = O.Position - Observer.Position
occluderDistance = |occluderVector|
occluderBearing = atan2(occluderVector.y, occluderVector.x)

// Half-angle of shadow cone: arcsin(AGENT_BODY_RADIUS / occluderDistance)
// Clamped to minimum angular width to handle very close occluders
shadowHalfAngle = arcsin(Min(AGENT_BODY_RADIUS / Max(occluderDistance, AGENT_BODY_RADIUS + 0.1f), 1.0f))
shadowHalfAngle = Max(shadowHalfAngle, MIN_SHADOW_HALF_ANGLE)

// MIN_SHADOW_HALF_ANGLE = 5°  [GT — prevents zero-width cones at extreme range;
//                               below this angle the occlusion is physically meaningless]

shadowInterval = [occluderBearing - shadowHalfAngle, occluderBearing + shadowHalfAngle]
```

### 3.2.4 Occlusion Test

For each candidate entity E that survived the FoV test:

```
isOccluded = false
foreach (OpponentAgent O in nearbyOpponents):
    if (O.AgentId == E.AgentId) continue  // entity cannot occlude itself
    if (O.DistanceToObserver >= E.DistanceToObserver) continue
        // Occluder must be CLOSER than target — object behind observer cannot occlude
    
    E_bearing = atan2(E.Position.y - Observer.Position.y,
                      E.Position.x - Observer.Position.x)
    
    // Wrap angular difference
    bearingDiff = |E_bearing - O.shadowInterval.center|
    if (bearingDiff > 180°) bearingDiff = 360° - bearingDiff
    
    if (bearingDiff <= shadowHalfAngle_O):
        isOccluded = true
        break  // One occluder is sufficient

if (isOccluded):
    exclude E from FoV-filtered set
    // Record in debug struct with IsOccluded = true (§3.7.3)
```

### 3.2.5 Numerical Verification

**Scenario:** Observer at (0, 0). Occluder (opponent) at (5, 0). Target at (10, 0).

```
occluderDistance = 5.0m
shadowHalfAngle = arcsin(0.4 / 5.0) = arcsin(0.08) = 4.59°

Target bearing from observer = atan2(0, 10) = 0°
Occluder center bearing = atan2(0, 5) = 0°
bearingDiff = |0° - 0°| = 0°

0° <= 4.59° → Target IS occluded ✓
```

**Scenario:** Observer at (0, 0). Occluder at (5, 0). Target at (10, 1).

```
Target bearing = atan2(1, 10) = 5.71°
bearingDiff = |5.71° - 0°| = 5.71°
5.71° > 4.59° → Target NOT occluded ✓
```

This demonstrates the correct edge case: a target 1 metre laterally displaced at 10 metres
range escapes an occluder 5 metres away, as expected physically.

### 3.2.6 Complexity

O(n × k) where n = candidate entities, k = nearby opponents. At n=22, k ≤ 10 in typical
play. Peak case: 21 candidates × 10 opponents = 210 angular interval tests per agent per
heartbeat. This is trivially fast; per-operation cost is two float comparisons and an
angle wrap. Section 6 confirms this is within budget.

---

## 3.3 Recognition Latency Model

### 3.3.1 Theoretical Basis

When a previously unseen agent enters an observer's field of view, the observer does not
instantly know they are there. A cognitive recognition delay occurs: stimulus detection,
figure-ground separation, agent classification. Helsen & Starkes (1999) report typical
recognition latency of 100–500ms for novel agent appearance, with expert players
demonstrating latencies in the lower portion of this range [HELSEN-1999]. Franks &
Miller (1985) corroborate a 400–500ms upper bound for novice recognition of new stimuli
in a sport context [FRANKS-1985].

The ball is exempt from this model (OQ-2): a football is an inanimate, distinctively
coloured, high-contrast object. Cognitive recognition latency models agent classification
delay, which does not apply to ball detection.

### 3.3.2 Base L_rec Formula

```
L_MAX = 5 heartbeat ticks (500ms for Decisions=1)   [GT — within Franks 1985 upper bracket]
L_MIN = 1 heartbeat tick  (100ms for Decisions=20)  [GT — within Helsen 1999 expert lower bracket]

L_rec_base(Decisions) = L_MAX - ((Decisions - 1) / 19.0f) × (L_MAX - L_MIN)
                       = 5 - ((Decisions - 1) / 19.0f) × 4

// Rounding convention — AUTHORITATIVE:
// The float result is converted to integer ticks via floor() — i.e. Mathf.FloorToInt().
// This is the required rounding method. Any other convention (round, ceil, truncate)
// will produce different tick values at non-boundary inputs and must not be used.
// At boundary inputs (D=1, D=20) the formula produces exact integers; no rounding applies.
L_rec_base_ticks = Mathf.FloorToInt(L_rec_base)
```

**Verification table:**

| Decisions | (D-1)/19 | L_rec_base (float) | L_rec_base (ticks, floor) | L_rec_base (ms) |
|-----------|----------|--------------------|---------------------------|-----------------|
| 1         | 0.000    | 5.000              | 5                         | 500             |
| 5         | 0.211    | 4.158              | 4                         | 400             |
| 10        | 0.474    | 3.105              | 3                         | 300             |
| 15        | 0.737    | 2.053              | 2                         | 200             |
| 20        | 1.000    | 1.000              | 1                         | 100             |

*Interpolation is linear and floored to integer ticks, consistent with project-wide attribute scaling convention.*

### 3.3.3 Modifiers

**Half-turn orientation bonus (cross-spec, read-only):**

```
// Source: First Touch Spec #4 §3.3.2 — established there, consumed here read-only
// Applies when: agent is currently in a half-turned body orientation
// Effect: 15% reduction in L_rec for entities in the peripheral/rear arc
if (observer.IsHalfTurned && entityInPeripheralArc(E)):
    L_rec = L_rec_base × 0.85f
```

This models the advantage of a pre-turned body position: the half-turn primes the observer's
visual system for peripheral/rear information, reducing the recognition delay for stimuli
from that direction.

**Entity in peripheral arc test:**

The half-turn bonus applies to entities in the outer half of the FoV cone — beyond the
central 45° "focal zone" and up to the FoV boundary. This is derived directly from
`BASE_FOV_HALF_ANGLE` (80°) without introducing a new constant.

```
// Outer half of cone = entities beyond BASE_FOV_HALF_ANGLE / 2 from FacingDirection
// BASE_FOV_HALF_ANGLE = 80° → focal zone = inner 40° → peripheral zone = 40° to 80°
// Rationale: half-turn primes peripheral/lateral awareness, not central fixation
PERIPHERAL_ARC_INNER_BOUND = BASE_FOV_HALF_ANGLE / 2.0f  // 40° — derived, not tuned

peripheralArc = (|angularSeparation| >= PERIPHERAL_ARC_INNER_BOUND
                 && |angularSeparation| <= BASE_FOV_HALF_ANGLE)
```

This boundary requires no new constant — it is fully determined by `BASE_FOV_HALF_ANGLE`.
If `BASE_FOV_ANGLE` changes, the peripheral arc threshold updates automatically.

**Ball contact forced refresh (§3.8):**

```
// On forced mid-heartbeat refresh triggered by ball contact:
// L_rec = 0 for all entities — full snapshot rebuilt from scratch for involved agents
```

### 3.3.4 Deterministic Noise

To prevent all agents with identical Decisions attributes from having perfectly synchronised
recognition events, a small deterministic noise term is applied per (observer, target) pair.

```
// DeterministicHash: non-cryptographic integer hash; no System.Random
// Consistent with project-wide determinism requirement (Spec #9)
noise_ticks = DeterministicHash(observerId, targetId, frameNumber) % 2
              // Result: 0 or 1 tick — noise only ever adds, never subtracts
              // Rationale: symmetric ±noise would reduce L_rec below L_MIN at peak Decisions,
              //            breaking the attribute floor guarantee. Additive-only noise
              //            preserves the L_MIN lower bound without a separate clamp step.

L_rec_final = Min(L_rec_base_modified + noise_ticks, L_MAX)
              // Min() only — L_MIN lower bound is preserved because:
              //   L_rec_base_modified >= L_MIN (formula minimum = 1 tick at Decisions=20)
              //   noise_ticks >= 0 (additive only)
              //   Therefore L_rec_final >= L_MIN always, without an explicit clamp
```

**Rationale for additive-only noise (0 or +1 tick):** Symmetric ±1 noise would allow
L_rec to drop below L_MIN at Decisions=20, violating the attribute floor guarantee and
requiring a second clamp. Additive-only noise preserves the L_MIN bound algebraically —
no clamp needed. The cost is a slight upward bias at the elite end (Decisions=20 agent
draws 100ms or 200ms latency, never 0ms), which is the correct and realistic behaviour.

### 3.3.5 Latency Counter Mechanics

The Perception System maintains a latency tracking dictionary per agent across heartbeats:

```csharp
// Persistent state across heartbeats — lives in PerceptionSystem, not PerceptionSnapshot
Dictionary<(int observerId, int targetId), int> _latencyCounters;

// Each heartbeat, for each entity E that is FoV-visible and not occluded:
if (!_latencyCounters.ContainsKey((observerId, E.AgentId))):
    // First time seen this cycle — initialise counter
    _latencyCounters[(observerId, E.AgentId)] = 0

_latencyCounters[(observerId, E.AgentId)] += 1

isConfirmed = (_latencyCounters[(observerId, E.AgentId)] >= L_rec_final)
// Only confirmed entities enter VisibleTeammates / VisibleOpponents
```

**Counter expiry:**

```
// If entity leaves visibility (fails FoV or occlusion test):
// Counter is REMOVED — reset on next appearance
// Prevents exploiting brief disappearances to retain stale awareness
_latencyCounters.Remove((observerId, E.AgentId))

// If entity has been confirmed and remains continuously visible:
// Counter is frozen at L_rec_final — no further incrementing
// Entity stays confirmed until it leaves visibility
```

**Design note:** This means an agent who steps behind a body for a single heartbeat and
re-emerges must re-accumulate the full L_rec before being re-confirmed. This is intentional
— brief disappearances should reset awareness, not merely pause it.

### 3.3.6 Expiry Window Exception

A confirmed entity that leaves visibility is not immediately removed from the snapshot.
A brief expiry window prevents rapid micro-flickering of snapshot contents when an entity
briefly passes through a shadow cone boundary. The window is set to the minimum meaningful
duration: 1 heartbeat tick (100ms). One tick is sufficient because shadow cone boundary
crossings are geometric events that resolve within a single tick; a longer window would
risk retaining stale snapshot data across tactically significant timescales.

```
CONFIRMATION_EXPIRY_TICKS = 1  // 100ms — minimum window to absorb single-tick boundary noise
                                // Derived: shadow cone crossings resolve in ≤1 tick; 
                                // no GT tuning required. If boundary oscillation persists
                                // beyond 1 tick, the cause is a physics-layer instability,
                                // not a perception parameter to paper over.

// When entity E leaves visibility:
//   Start expiry countdown: _expiryCounters[(observerId, E.AgentId)] = CONFIRMATION_EXPIRY_TICKS
//   Entity remains in snapshot with last known position for 1 tick
//   If entity becomes visible again within expiry window: counter reset, entity remains confirmed
//   If expiry reaches 0 without re-appearance: remove from snapshot, remove latency counter
```

This is the only case where a snapshot contains "stale" agent data. BallStalenessFrames
handles the equivalent for ball position (§3.5.2).

---

## 3.4 Blind-Side Awareness and Shoulder Check

### 3.4.1 Blind-Side Arc

The blind-side arc is the angular region not covered by the forward FoV cone (§3.1.5).
Entities in this region are invisible to the agent by default — they fail Step 3
(ApplyFieldOfView) and do not enter the candidate pipeline at all.

```
BlindSide_Center = Observer.FacingAngle + 180°  // Exact rear direction
BlindSide_HalfAngle = (360° - EffectiveFoV) / 2.0f

// Entity is in blind-side arc if:
angularSeparationFromRear = |entityBearing - BlindSide_Center|
if (angularSeparationFromRear > 180°) angularSeparationFromRear = 360° - angularSeparationFromRear
inBlindSide = (angularSeparationFromRear <= BlindSide_HalfAngle)
```

At the neutral case (Decisions=10, PressureScalar=0, EffectiveFoV=165°):
BlindSide_HalfAngle = (360° - 165°) / 2 = 97.5°

This means the blind-side arc spans 195° centered on the agent's rear. An entity directly
to the agent's rear is always in the blind side. An entity at 82.5° to either side of the
agent's facing direction sits exactly at the FoV boundary.

### 3.4.2 Autonomous Shoulder Check Trigger

Shoulder checks are triggered autonomously by the Perception System on a per-agent timer.
The Decision Tree does not request them (KD-6, OQ-3).

```
// CHECK_MAX_TICKS = 30 ticks (3.0s for Anticipation=1)
//   [GT — within Franks 1985 bracket: average player scans every 3–5s without pressure]
// CHECK_MIN_TICKS = 6 ticks (0.6s for Anticipation=20)
//   [GT — within Master Vol 1 §3.1: elite players perform 6–8 scans per possession;
//    at 0.6s per scan over 6s possession = 10 scans maximum; 6-tick floor is appropriate]

CheckInterval_ticks(Anticipation) = CHECK_MAX_TICKS 
    - ((Anticipation - 1) / 19.0f) × (CHECK_MAX_TICKS - CHECK_MIN_TICKS)
```

**Verification table:**

| Anticipation | (A-1)/19 | CheckInterval (ticks) | CheckInterval (s) |
|--------------|----------|-----------------------|-------------------|
| 1            | 0.000    | 30.0                  | 3.0               |
| 5            | 0.211    | 24.97                 | 2.5               |
| 10           | 0.474    | 18.93                 | 1.9               |
| 15           | 0.737    | 12.89                 | 1.3               |
| 20           | 1.000    | 6.0                   | 0.6               |

**Possession modifier (§3.4.4 from outline):**

```
// Agent in possession (has ball, dribbling):
// CheckInterval doubles — full attention required for ball control
if (observer.HasPossession):
    CheckInterval_ticks *= 2.0f
// Maximum effective interval: 60 ticks (6s) for Anticipation=1 in possession
// Minimum effective interval: 12 ticks (1.2s) for Anticipation=20 in possession
```

**Deterministic jitter:**

```
jitter_ticks = (DeterministicHash(observerId, frameNumber) % 5) - 2
// Range: [-2, +2] ticks
// Prevents mass synchronised shoulder checks across all agents simultaneously
// Applied after possession modifier, before comparison

NextCheckFrame = CurrentFrame + CheckInterval_ticks + jitter_ticks
NextCheckFrame = Max(NextCheckFrame, CurrentFrame + 1)  // Always at least 1 tick forward
```

**Trigger condition:**

```
if (CurrentFrame >= _nextCheckFrame[observerId]):
    FireShoulderCheck(observerId)
    _nextCheckFrame[observerId] = CurrentFrame + CheckInterval_ticks + jitter_ticks
```

### 3.4.3 Shoulder Check Window

When a shoulder check fires, the agent gains a temporary blind-side awareness window.

```
SHOULDER_CHECK_DURATION = 3 heartbeat ticks (300ms)
// [GT — per Master Vol 1 §3.1: "~0.3 seconds for a shoulder check movement"]

// Window state (persistent, per agent):
_blindSideWindowActive[observerId] = true
_blindSideWindowExpiry[observerId] = CurrentFrame + SHOULDER_CHECK_DURATION
```

**Blind-side entity processing during window:**

During an active shoulder check window, entities in the blind-side arc are:
1. Queried (via the same pipeline, against the blind-side half-angle)
2. Subjected to the **same FoV, occlusion, and L_rec checks** as forward entities
3. They enter a *separate* latency counter namespace: `_blindSideLatencyCounters`
4. Only confirmed blind-side entities enter `BlindSidePerceivedAgents` in the snapshot

**Critical design point:** Shoulder check does NOT grant instant awareness of blind-side
entities. They still accumulate L_rec. A single 3-tick window is sufficient to confirm
entities with Decisions ≥ 10 (L_rec ≤ 3 ticks), but may not confirm very slow recognisers
(Decisions=1, L_rec=5 ticks) in a single check. This is intentional — elite scanners
benefit more from frequent checks because they confirm faster.

**Window expiry:**

```
if (CurrentFrame >= _blindSideWindowExpiry[observerId]):
    _blindSideWindowActive[observerId] = false
    // Unconfirmed blind-side latency counters are cleared on window expiry
    // Confirmed entities that were added to snapshot remain until standard expiry
```

### 3.4.4 ShoulderCheckAnimData Stub

A stub struct is defined here for the Animation System (Stage 1+). At Stage 0 it is
populated but has no consumer.

```csharp
struct ShoulderCheckAnimData
{
    int     AgentId;
    int     FireFrame;          // Heartbeat tick on which check was triggered
    float   CheckDirection;     // Degrees: which side the check was directed (left/right of rear)
    bool    AnyEntityConfirmed; // Did at least one blind-side entity confirm this window?
    // Populated at Stage 0; consumed by Animation System at Stage 1+
}
```

---

## 3.5 Ball Perception

### 3.5.1 Ball Visibility Test

The ball is treated as a special entity with two differences from agent perception:

1. **No L_rec.** Ball recognition is immediate upon becoming visible (OQ-2).
2. **Visibility test is otherwise identical:** Range check (MAX_PERCEPTION_RANGE), FoV
   half-angle check, shadow cone occlusion test.

```
// Ball visibility pipeline (Step 3–4 only; Step 5 skipped):
bool ballInRange = (BallState.Position - Observer.Position).magnitude <= MAX_PERCEPTION_RANGE
bool ballInFoV   = (|bearingToBall - FacingAngle| <= EffectiveFoV_HalfAngle)
bool ballOccluded = CheckOcclusionForEntity(BallState.Position, nearbyOpponents)

BallVisible = ballInRange && ballInFoV && !ballOccluded
```

**Design note:** Ball occlusion by opponent bodies is realistic and important for gameplay.
A forward in a press should not "see" a pass to the ball-carrier through an intervening
defender's body. This affects tactical decision quality appropriately.

### 3.5.2 Stale Ball Position

When the ball leaves visibility, the observer does not immediately lose all knowledge of
the ball — they retain the last confirmed position. This is the `BallPerceivedPosition`
field, which may diverge from `BallState.Position` during invisibility periods.

```
// Each heartbeat:
if (BallVisible):
    BallPerceivedPosition = BallState.Position  // Update to ground truth
    BallStalenessFrames = 0
else:
    // BallPerceivedPosition retains last confirmed value — NOT updated
    BallStalenessFrames += 1

// BallPerceivedPosition is never reset to zero — it holds the last known position
// until the ball becomes visible again
```

**Staleness semantics:** `BallStalenessFrames` tells the Decision Tree how old the ball
position data is. At Stage 0 the Decision Tree uses this as a confidence weight:
a staleness of 0 means ground truth; a staleness of 20 ticks (2 seconds) means the ball
has been invisible for 2 seconds and the position estimate may be significantly wrong.

No predictive ball tracking is implemented at Stage 0. The Decision Tree receives the last
known position and must handle staleness internally. Stage 1 upgrade: Anticipation-driven
ball position prediction using last known velocity (flagged in Section 7).

### 3.5.3 Ball Perception Worked Example

**Scenario:** Ball passes behind an opponent 5m from observer, re-emerges 0.5 seconds later.

```
Tick 0:   Ball visible. BallPerceivedPosition = (50, 34). BallStalenessFrames = 0.
Tick 1:   Ball enters occluder shadow cone. BallVisible = false.
          BallPerceivedPosition = (50, 34) [unchanged]. BallStalenessFrames = 1.
Tick 2:   Still occluded. BallStalenessFrames = 2.
Tick 3:   Still occluded. BallStalenessFrames = 3.
Tick 4:   Still occluded. BallStalenessFrames = 4.
Tick 5:   Ball re-emerges. BallVisible = true. BallPerceivedPosition = (52, 35).
          BallStalenessFrames = 0.

Decision Tree received BallStalenessFrames=1 through 4 during occlusion — correctly
signalling degraded ball position confidence during the 400ms blind window.
```

---

## 3.6 Pressure Scalar Integration

### 3.6.1 Authority

The pressure scalar formula is defined in First Touch Mechanics Specification #4 §3.5,
where it governs control quality degradation. The formula is reused here verbatim under
the project principle of single-authority constants. Perception does not re-derive or modify
the formula.

**Authority:** First Touch Spec #4 §3.5.1–§3.5.3  
**Constant values (from First Touch §3.5):**
- `PRESSURE_RADIUS` = 3.0m
- `MIN_PRESSURE_DISTANCE` = 0.3m
- `PRESSURE_SATURATION` = 1.5

### 3.6.2 Formula Reference

```
// Reused verbatim from First Touch Spec #4 §3.5.2–3.5.3:

rawPressure = 0f
foreach (opponent O in spatialHash.Query(Observer.Position, PRESSURE_RADIUS)
         where O.TeamID != Observer.TeamID):
    distance = Max(|Observer.Position - O.Position|, MIN_PRESSURE_DISTANCE)
    rawPressure += (MIN_PRESSURE_DISTANCE / distance)²

PressureScalar = Clamp(rawPressure / PRESSURE_SATURATION, 0.0f, 1.0f)
```

### 3.6.3 Usage in Perception

Within the Perception System, `PressureScalar` has exactly one use: FoV narrowing (§3.1.4).

```
// PressureScalar is computed once per agent per heartbeat in Step 3 (before FoV test)
// It is stored in the snapshot for Decision Tree diagnostic use (§3.7.1)
// It is NOT used to degrade ball visibility, recognition latency, or shoulder check timing
```

**Rationale:** Applying the pressure scalar to recognition latency would double-penalise
under-pressure agents: their FoV already narrows (fewer candidates visible), compounding
that with slower recognition of what they can see would make low-Decisions agents
catastrophically non-functional under press. The single-use constraint is a deliberate
design choice for gameplay balance and tractable parameter tuning.

---

## 3.7 PerceptionSnapshot Struct Definition

This section is the authoritative definition of `PerceptionSnapshot`. Any field change
must be version-controlled through this specification. The Decision Tree consumes this
struct; it does not modify the struct definition.

### 3.7.1 Primary Struct

```csharp
/// <summary>
/// PerceptionSnapshot — authoritative output of the Perception System.
///
/// Produced once per 10Hz heartbeat per agent. Passed by value to the Decision Tree.
/// Represents the agent's complete knowledge of the world state at this tick.
/// Contains only confirmed, filtered, latency-resolved information.
///
/// OWNERSHIP: Defined exclusively by Perception System Spec #7.
/// CONSUMERS: Decision Tree Spec #8 (sole consumer at Stage 0).
/// MODIFICATION: Any field change requires a version bump of this specification.
/// </summary>
struct PerceptionSnapshot
{
    // ── Identity and Timing ──────────────────────────────────────────────────────

    int     ObserverId;
    // AgentId of the agent this snapshot describes.
    // Set to the observing agent's AgentState.AgentId.

    int     FrameNumber;
    // Heartbeat tick (10Hz counter) at which this snapshot was built.
    // Used by Decision Tree to detect stale snapshots (should never be > 1 tick old).

    bool    IsForceRefreshed;
    // True if this snapshot was produced by a mid-heartbeat forced refresh (§3.8).
    // False for all standard 10Hz heartbeat snapshots.

    // ── Ball Data ────────────────────────────────────────────────────────────────

    bool    BallVisible;
    // True if ball passed all three visibility tests (range, FoV, occlusion) this tick.

    Vector2 BallPerceivedPosition;
    // Last confirmed ball position. If BallVisible=true: equals ground truth this tick.
    // If BallVisible=false: retains last confirmed position (may be stale).
    // Never (0,0) after first ball sighting — retains last known value indefinitely.

    int     BallStalenessFrames;
    // Number of consecutive heartbeats during which BallVisible was false.
    // 0 = ball currently visible (ground truth). Higher values = older position estimate.
    // Decision Tree should weight ball confidence inversely with this value.

    // ── Visible Agents ───────────────────────────────────────────────────────────

    PerceivedAgent[] VisibleTeammates;
    // All teammate agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..10 (max 10 outfield teammates + GK at Stage 0).

    PerceivedAgent[] VisibleOpponents;
    // All opponent agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..11 (max 11 opponents).

    // ── Blind-Side Data ──────────────────────────────────────────────────────────

    bool    BlindSideWindowActive;
    // True during an active shoulder check window (§3.4.3).
    // When true: BlindSidePerceivedAgents is populated.

    int     BlindSideWindowExpiry;
    // FrameNumber at which the current shoulder check window expires.
    // Meaningless if BlindSideWindowActive = false.

    PerceivedAgent[] BlindSidePerceivedAgents;
    // Agents confirmed during the active shoulder check window.
    // Empty if BlindSideWindowActive = false.
    // Subject to same L_rec requirements as forward-arc agents (§3.4.3).

    // ── FoV Diagnostic ───────────────────────────────────────────────────────────

    float   EffectiveFoVAngle;
    // Actual FoV angle in degrees after Decisions modifier and pressure narrowing.
    // Stored for Decision Tree diagnostic use and for debug tooling.
    // Range: [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE] = [120°, 170°].

    float   PressureScalar;
    // Pressure scalar at the time of snapshot construction.
    // Range: [0.0, 1.0]. Stored for Decision Tree use in evaluating action quality.

    // ── Animation Stub ───────────────────────────────────────────────────────────

    ShoulderCheckAnimData ShoulderCheckAnim;
    // Stage 1+ stub. Populated at Stage 0; no consumer until Animation System written.
    // See §3.4.4 for struct definition.
}
```

### 3.7.2 PerceivedAgent Sub-Struct

```csharp
/// <summary>
/// PerceivedAgent — data for one visible agent in a PerceptionSnapshot.
///
/// All fields reflect agent state at the time of snapshot construction.
/// At Stage 0: position and velocity are ground truth for confirmed entities.
/// At Stage 1+: error modelling will introduce uncertainty into these values.
/// </summary>
struct PerceivedAgent
{
    int     AgentId;
    // AgentState.AgentId of the perceived agent.

    Vector2 PerceivedPosition;
    // Ground-truth position at time of snapshot (Stage 0).
    // Stage 1: will include perception error based on distance and Decisions attribute.

    Vector2 PerceivedVelocity;
    // Ground-truth velocity at time of snapshot (Stage 0).
    // Stage 1: will include direction estimation error for far agents.

    float   ConfidenceScore;
    // [0.0, 1.0]. At Stage 0: binary — 1.0 for confirmed, not present if unconfirmed.
    // Stage 1: continuous, degrading with distance and staleness.

    bool    IsInBlindSide;
    // True if this agent was confirmed via blind-side shoulder check window,
    // rather than via the forward FoV pipeline.

    int     LatencyCounterAtConfirmation;
    // Latency counter value when this agent was first confirmed.
    // Stored for diagnostic/tuning purposes. Not used by Decision Tree.
}
```

### 3.7.3 Debug Occlusion Record (Editor/Debug Only)

```csharp
/// <summary>
/// OcclusionDebugRecord — populated only when perception debug mode is active.
/// Not part of the production PerceptionSnapshot. Excluded from all timing budgets.
/// Used by Stage 1+ visual debug tooling to render shadow cones.
/// </summary>
struct OcclusionDebugRecord
{
    int     ObserverId;
    int     TargetId;
    bool    IsOccluded;
    int     OccludingAgentId;  // -1 if not occluded
    float   ShadowHalfAngle;   // Of the occluding agent's cone, in degrees
    int     FrameNumber;
}
```

---

## 3.8 Mid-Heartbeat Forced Refresh

### 3.8.1 Trigger Events

Certain physics-layer events produce a forced mid-heartbeat perception refresh for the
directly involved agents only. All other agents continue on the standard 10Hz schedule.

The complete list of qualifying trigger events at Stage 0:

| Event | Agents Refreshed | Rationale |
|-------|-----------------|-----------|
| Ball contact (pass/shot/first touch) | Ball-contacting agent + agents within 5m of ball | Contact event resets ball awareness; nearby agents re-evaluate |
| Tackle completion | Tackler + tackled agent | Possession change; both agents need updated snapshot |
| Possession change (non-tackle) | Ball-holder losing possession + new holder | Decision context changed; immediate re-evaluation warranted |

**Rationale for selectivity:** Forcing a full 22-agent refresh on any ball event would
eliminate the 10Hz cognitive cadence and approach the cost of a 60Hz system. Only the
directly involved agents receive a refresh; spectating agents continue on schedule.

### 3.8.2 Forced Refresh Procedure

```
ForcedRefresh(agentId, triggerEvent):
    // Standard pipeline §3.0 Steps 1–7, executed immediately (out of heartbeat schedule)
    // Step 5 override: L_rec = 0 for ALL entities in this refresh
    //   Rationale: the triggering event (ball contact, tackle) is a salient attention cue
    //   that eliminates recognition delay for the brief refresh window
    // Result: snapshot with IsForceRefreshed = true
    // Normal heartbeat schedule is NOT reset — next standard heartbeat fires on schedule
```

### 3.8.3 PerceptionRefreshEvent Stub

```csharp
/// <summary>
/// PerceptionRefreshEvent — published when a forced mid-heartbeat refresh occurs.
/// Stub at Stage 0; consumed by Event System Spec #17 when written.
/// </summary>
struct PerceptionRefreshEvent
{
    int     AgentId;
    int     TriggerFrame;
    string  TriggerEventType;  // "BallContact", "TackleCompletion", "PossessionChange"
    // Event System #17 will define subscription architecture
}
```

---

## 3.9 Worked Examples

### 3.9.1 Example A: Elite Midfielder, No Pressure

**Setup:**
- Observer: Decisions=18, Anticipation=16, FacingAngle=90° (facing "up" pitch)
- PressureScalar=0.05 (one opponent at ~2.5m)
- Three teammates in forward arc, one opponent at 70° offset, one teammate at 110° offset

**Step 1 — EffectiveFoV calculation:**
```
EffectiveFoV_BeforePressure = 160° + (18/20) × 10° = 160° + 9° = 169°
Reduction = 0.05 × 30° = 1.5°
EffectiveFoV = 169° - 1.5° = 167.5°
EffectiveFoV_HalfAngle = 83.75°
BlindSide_HalfAngle = (360° - 167.5°) / 2 = 96.25°
```

**Step 2 — FoV filter:**
```
Teammate at 70° offset: 70° ≤ 83.75° → IN FoV ✓
Opponent at 70° offset: IN FoV ✓  
Teammate at 110° offset: 110° > 83.75° → OUT (in blind side) ✗
Three forward teammates: assumed within PERIPHERAL_ARC_INNER_BOUND (40°) → all in focal zone, IN FoV ✓
```

**Step 3 — L_rec for opponent (first time seen):**
```
L_rec_base(Decisions=18) = 5 - (17/19) × 4 = 5 - 3.579 = 1.421 → floor to 2 ticks after noise
noise = DeterministicHash(observerId, opponentId, frame) % 2 = 1 (assumed)
L_rec_final = Min(1.421 + 1, L_MAX) = Min(2.421, 5) = 2 ticks (L_MIN preserved algebraically)

On tick 1 of seeing opponent: counter = 1, not confirmed
On tick 2: counter = 2 ≥ 2 → CONFIRMED. Opponent enters VisibleOpponents.
```

**Step 4 — Shoulder check:**
```
CheckInterval(Anticipation=16) = 30 - (15/19) × 24 = 30 - 18.947 = 11.05 ticks
With jitter: ±2 ticks → fires between tick 9 and 13 of last check
Observer NOT in possession → no possession doubling
```

If shoulder check fires this heartbeat (BlindSideWindowActive=true):
```
Teammate at 110° offset now queried in blind-side pipeline.
110° offset from facing = 70° offset from rear (180°)
70° ≤ BlindSide_HalfAngle (96.25°) → IN blind-side arc ✓
Subject to L_rec: must accumulate 2 ticks (same Decisions=18 formula)
Not confirmed on first tick of check — needs second consecutive tick to confirm.
```

**Resulting snapshot (tick 2 of scenario):**
```
VisibleTeammates: [3 forward teammates]  (all confirmed from prior ticks)
VisibleOpponents: [opponent at 70°]      (just confirmed tick 2)
BlindSidePerceivedAgents: []             (teammate at 110° not yet confirmed)
BallVisible: true, BallStalenessFrames: 0
EffectiveFoVAngle: 167.5°
PressureScalar: 0.05
```

---

### 3.9.2 Example B: Low-Attribute Defender Under Full Press

**Setup:**
- Observer: Decisions=4, Anticipation=3, FacingAngle=270°
- PressureScalar=0.85 (three opponents within 2m)
- Ball behind occluding opponent

**Step 1 — EffectiveFoV:**
```
EffectiveFoV_BeforePressure = 160° + (4/20) × 10° = 162°
Reduction = 0.85 × 30° = 25.5°
EffectiveFoV = 162° - 25.5° = 136.5° (above MIN_FOV_ANGLE = 120° ✓)
EffectiveFoV_HalfAngle = 68.25°
```

**Step 2 — L_rec for newly visible agent:**
```
L_rec_base(Decisions=4) = 5 - (3/19) × 4 = 5 - 0.632 = 4.368 → ≈4–5 ticks with noise
This agent will not enter snapshot for 400–500ms from first visibility.
```

**Step 3 — Ball occluded:**
```
Opponent O at distance 4m lies between observer and ball.
shadowHalfAngle = arcsin(0.4/4.0) = arcsin(0.1) = 5.74°
Ball bearing from observer differs from occluder bearing by 2° → 2° < 5.74° → occluded ✓
BallVisible = false. BallStalenessFrames increments.
```

**Step 4 — Shoulder check frequency:**
```
CheckInterval(Anticipation=3) = 30 - (2/19) × 24 = 30 - 2.526 = 27.47 ≈ 27 ticks (2.7s)
Observer NOT in possession → no penalty
This defender checks their blind side approximately once every 2.7 seconds.
Compare: elite scanner (Anticipation=20) checks every 0.6–0.8s.
Tactical consequence: defender is frequently blind to runs behind them.
```

---

## 3.10 Constants Master Table

All constants are tagged per project-wide citation methodology:
- **[GT]** — Gameplay-tuned; validated within academic plausibility range
- **[PHYS]** — Derived from physics; not tunable without changing the physical model
- **[CROSS]** — Defined in another specification; consumed read-only here

| Constant | Value | Tag | Source / Rationale |
|----------|-------|-----|--------------------|
| `MAX_PERCEPTION_RANGE` | 120m | [GT] | Full pitch diagonal; effectively uncapped. OQ-5 resolution. |
| `BASE_FOV_ANGLE` | 160° | [GT] | Within Franks 1985 plausibility range (140°–180°) |
| `MAX_FOV_BONUS_ANGLE` | 10° | [GT] | Williams & Davids 1998 expert-novice scanning data |
| `MAX_FOV_PRESSURE_REDUCTION` | 30° | [GT] | Beilock 2010: 20–50% performance degradation; 30° ≈ 18% of 170° |
| `MIN_FOV_ANGLE` | 120° | [GT] | Safety floor; prevents degenerate near-zero cone |
| `AGENT_BODY_RADIUS` | 0.4m | [GT] | Adult shoulder half-width; consistent with Agent Movement §3.2 |
| `MIN_SHADOW_HALF_ANGLE` | 5° | [GT] | Prevents zero-width cones; below this threshold occlusion is physically meaningless |
| `L_MAX` | 5 ticks (500ms) | [GT] | Franks 1985 upper bracket for novice recognition latency |
| `L_MIN` | 1 tick (100ms) | [GT] | Helsen 1999 expert lower bracket |
| `CONFIRMATION_EXPIRY_TICKS` | 1 tick (100ms) | [DERIVED] | Minimum window to absorb single-tick shadow cone boundary noise; not GT-tuned |
| `PERIPHERAL_ARC_INNER_BOUND` | 40° (= BASE_FOV_HALF_ANGLE / 2) | [DERIVED] | Derived from BASE_FOV_HALF_ANGLE; no independent value |
| `CHECK_MAX_TICKS` | 30 ticks (3.0s) | [GT] | Franks 1985: average player scans every 3–5s |
| `CHECK_MIN_TICKS` | 6 ticks (0.6s) | [GT] | Master Vol 1 §3.1: 6–8 elite scans per possession |
| `SHOULDER_CHECK_DURATION` | 3 ticks (300ms) | [GT] | Master Vol 1 §3.1: "~0.3 seconds for a shoulder check" |
| `PRESSURE_RADIUS` | 3.0m | [CROSS] | First Touch Spec #4 §3.5.1 — authoritative |
| `MIN_PRESSURE_DISTANCE` | 0.3m | [CROSS] | First Touch Spec #4 §3.5.2 — authoritative |
| `PRESSURE_SATURATION` | 1.5 | [CROSS] | First Touch Spec #4 §3.5.3 — authoritative |
| Half-turn L_rec reduction | 15% (×0.85) | [CROSS] | First Touch Spec #4 §3.3.2 — authoritative |

**[GT] constant count:** 12 of 17 total (71%). Two constants are [DERIVED] — determined
algebraically from other constants with no independent tuning. Three are [CROSS] — defined
in upstream specifications. This is within expected range for a cognitive simulation system.

---

## Section 3 Summary

Section 3 defines six interdependent models that together produce a complete, deterministic
`PerceptionSnapshot` for each of the 22 agents per 10Hz heartbeat:

**§3.1** — FoV geometry: BASE_FOV_ANGLE = 160°, Decisions modifier adds up to 10°,
pressure degrades up to 30°, floor at 120°. Blind-side arc is the mathematical complement.

**§3.2** — Shadow cone occlusion: angular interval computed from AGENT_BODY_RADIUS = 0.4m
and occluder distance. Conservative (over-occludes slightly at close range). O(n×k) cost.
Opponents only at Stage 0.

**§3.3** — Recognition latency: L_rec linear in Decisions from 500ms (D=1) to 100ms (D=20).
Half-turn bonus (-15%) applied to entities in peripheral arc (40°–80° from facing), derived
from BASE_FOV_HALF_ANGLE — no new constant. Additive-only noise (+0 or +1 tick) preserves
L_MIN floor algebraically. Counter tracking with 1-tick expiry window (derived minimum,
not GT-tuned) to absorb single-tick shadow cone boundary crossings.

**§3.4** — Shoulder check: autonomous, 0.6s–3.0s interval by Anticipation. 3-tick window.
Blind-side entities still subject to L_rec during window. Possession doubles interval.
`ShoulderCheckAnimData` stub defined for Stage 1+.

**§3.5** — Ball perception: immediate recognition when visible (no L_rec). Same FoV +
occlusion tests as agents. `BallStalenessFrames` tracks invisibility duration.

**§3.6** — Pressure scalar: formula reused verbatim from First Touch Spec #4 §3.5. Single
use: FoV narrowing only. Not applied to L_rec or shoulder check timing.

**§3.7** — `PerceptionSnapshot` struct: 12 fields, value type, copied to Decision Tree each
heartbeat. `PerceivedAgent` sub-struct defined. Debug record and animation stub defined.

**§3.8** — Forced refresh: three qualifying trigger events. Involved agents only. L_rec = 0
on forced refresh. Standard heartbeat schedule unaffected.

**§3.9** — Worked examples for elite midfielder (Decisions=18) and low-attribute defender
under full press, with numerical verification throughout.

**§3.10** — Constants table: 16 constants, 12 [GT], 3 [CROSS], 1 [GT]. All tagged.

---

**Next:** Section 4 — Integration Contracts

---

*End of Section 3 — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
