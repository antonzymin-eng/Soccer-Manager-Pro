## SECTION 3: TECHNICAL SPECIFICATIONS (~20 pages)

### 3.1 Pass Type Taxonomy and Physical Profiles

#### 3.1.1 Overview: Why Classify Pass Types?

Each pass type has a distinct **physical signature** â€” a combination of velocity range, launch angle, and spin that produces a recognizable ball trajectory. A driven 30m pass looks and behaves differently from a lofted 30m pass. Classification enforces these distinctions explicitly rather than allowing continuous parameter space that would produce physically implausible results.

#### 3.1.2 Physical Profile Table

| PassType | Velocity Range (m/s) | Launch Angle | Dominant Spin | Distance Range |
|----------|---------------------|--------------|---------------|----------------|
| Ground | 5 â€“ 18 | 2Â° â€“ 5Â° | Topspin | 3m â€“ 30m |
| Driven | 14 â€“ 28 | 5Â° â€“ 12Â° | Strong Topspin | 15m â€“ 50m |
| Lofted | 12 â€“ 24 | 20Â° â€“ 45Â° | Backspin | 20m â€“ 60m |
| ThroughBall | 8 â€“ 22 | 2Â° â€“ 5Â° | Topspin | 10m â€“ 40m |
| AerialThrough | 14 â€“ 22 | 25Â° â€“ 40Â° | Backspin | 20m â€“ 50m |
| Cross (flat) | 16 â€“ 26 | 8Â° â€“ 15Â° | Sidespin | 20m â€“ 45m |
| Cross (whipped) | 14 â€“ 24 | 15Â° â€“ 25Â° | Strong Sidespin | 25m â€“ 50m |
| Cross (high) | 12 â€“ 20 | 25Â° â€“ 40Â° | Backspin+Sidespin | 25m â€“ 50m |
| Chip | 8 â€“ 16 | 45Â° â€“ 65Â° | Backspin | 3m â€“ 20m |

**âš  Note:** All values in this table are design-authority placeholders derived from observational football data and will require [GT] audit in Section 8. Velocity maximums in particular must be cross-checked against Ball Physics Â§3.1 maximum velocity constants.

#### 3.1.3 Ground Pass Profile (Â§3.1.3 â€” detail)

**Use case:** Short-to-medium range passes on the ground. The foundational pass type.

**Physical characteristics:**
- Near-zero launch angle (2â€“5Â°) â€” enough to clear minor surface irregularities, not enough to create meaningful aerial phase
- Moderate topspin causes ball to accelerate slightly on first bounce contact, then roll
- Velocity front-loaded: ball decelerates via grass friction after landing
- Stage 0: Ball Physics owns deceleration via friction model

**Attribute mapping:**
- Velocity: primarily Power attribute Ã— distance scaling factor
- Accuracy: primarily Passing attribute
- Topspin magnitude: Technique attribute (higher technique = cleaner contact = more consistent spin)

#### 3.1.4 Driven Pass Profile (Â§3.1.4 â€” detail)

**Use case:** Firm, penetrating passes cutting through defensive lines. Higher velocity than ground pass, slightly elevated to clear legs.

**Physical characteristics:**
- Low but meaningful elevation (5â€“12Â°) â€” ball bounces once or twice at medium range, stays low
- Strong topspin keeps trajectory flat and speeds up bounce
- Hardest type to receive cleanly (high velocity into first touch)

**Attribute mapping:**
- Velocity: Power attribute dominates (less distance scaling than ground pass)
- Accuracy: Passing + Technique (technique governs contact cleanliness)
- Spin: Technique

#### 3.1.5 Lofted Pass Profile (Â§3.1.5 â€” detail)

**Use case:** Switching play, long diagonal passes, switching flanks, playing over a press.

**Physical characteristics:**
- High trajectory (20â€“45Â°), significant aerial phase
- Backspin holds ball in air longer, reduces run on landing
- Significant accuracy challenge: small angular error at kick = large positional error at target

**Attribute mapping:**
- Velocity: Power attribute (needs significant power at longer ranges)
- Launch angle: Distance-dependent (shorter loft = steeper; longer = shallower to carry distance)
- Accuracy: Passing attribute has heightened effect on error magnitude (lofted passes are more sensitive to error)

#### 3.1.6 Through Ball Profile (Â§3.1.6 â€” detail)

**Use case:** Penetrating pass into space behind a defensive line for a runner.

**Physical characteristics:**
- Ground trajectory (same profile as Ground pass physically)
- The key distinction is **target resolution**: target is a projected position, not a player position
- Lead distance calculation is the primary complexity (see Â§3.5)

**Attribute mapping:**
- Vision attribute (if defined in AgentAttributes â€” forward reference to Decision Tree and attribute design): affects lead distance quality. If Vision is not available in Stage 0, use Technique as proxy.
- Passing: accuracy
- Power: velocity

#### 3.1.7 Cross Profile (Â§3.1.7 â€” detail)

**Use case:** Wide delivery into the penalty area from wide positions.

**Physical characteristics:**
- Three sub-types with distinct signatures:
  - **Flat/driven cross:** Low trajectory, maximum pace, high sidespin for curl
  - **Whipped cross:** Mid-trajectory, strong inswinging or outswinging sidespin curve
  - **High cross:** High trajectory, backspin, designed to hang for headed finish
- Sidespin creates Magnus curve effect â€” Ball Physics handles the actual curve; Pass Mechanics provides the spin vector

**Attribute mapping:**
- Crossing attribute (if defined); if not available in Stage 0, use Passing as proxy
- Technique: sidespin magnitude (more precise contact = more controlled curve)
- Power: flat cross velocity

**âš  Design risk:** Cross classification may justify a `CrossType` sub-enum or additional fields on `PassRequest`. Decision to be made during Section 3 drafting.

#### 3.1.8 Chip Profile (Â§3.1.8 â€” detail)

**Use case:** Lobbing the ball over a defender or goalkeeper at short-to-medium range.

**Physical characteristics:**
- Steep launch angle (45â€“65Â°)
- Strong backspin â€” ball drops more steeply on descent, holds up on landing
- Shorter distance cap (chip over 25m is rarely viable)
- Lower velocity than other types for equivalent distance â€” relies on arc

**Attribute mapping:**
- Technique: dominant (chip requires precise contact, not power)
- Power: secondary
- Accuracy: Passing attribute

### 3.2 Pass Velocity Model

#### 3.2.1 Base Velocity Formula

Derivation approach (full derivation in Appendix A):

The intended ball velocity at target arrival (V_target) must be specified. The launch velocity (V_launch) is then back-calculated accounting for drag and gravity over the expected trajectory. For Stage 0 ground passes, this simplification is acceptable:

```
V_launch = V_base(PassType) Ã— PowerScale(Power, Distance) Ã— FatigueModifier(Fatigue)
```

Where:
- `V_base(PassType)`: baseline velocity for the pass type at reference distance
- `PowerScale(Power, Distance)`: scales Power attribute [1â€“20] to velocity, adjusted for distance
- `FatigueModifier(Fatigue)`: reduces effective power as fatigue increases

Full formula derivation, constants definition, and clamping logic defined in Section 3.2 of the full spec.

#### 3.2.2 Power Attribute Scaling

Power attribute maps [1â€“20] to a velocity multiplier. Elite passer (Power=20) at maximum range achieves maximum type-defined velocity. Poor passer (Power=5) at long range may fail to achieve minimum lofted velocity â€” triggering a type downgrade warning (logged; not an error).

#### 3.2.3 Fatigue Modifier

```
FatigueModifier = 1.0 - (Fatigue Ã— FATIGUE_POWER_REDUCTION)
// FATIGUE_POWER_REDUCTION = [GT] constant, likely ~0.15â€“0.25
// Fully fatigued player loses up to 25% of effective power
```

#### 3.2.4 Velocity Clamping

Every pass type has a `MIN_VELOCITY` and `MAX_VELOCITY` constant. Post-calculation clamp enforces physical plausibility.

### 3.3 Launch Angle Model

#### 3.3.1 Angle Derivation by Pass Type

Ground/Driven: fixed shallow angle with minor distance correction.

Lofted/Chip: angle is a function of distance. For a given horizontal distance and desired hang time, the required launch angle can be derived from projectile motion. Stage 0 uses simplified projectile equations (ignoring drag for angle selection; drag is applied in Ball Physics during flight).

```
// Simplified projectile angle for target distance D, desired apex height H:
// Î¸ = atan2(2H, D)  [simplified; full derivation in Appendix A]
```

Cross angles: sub-type dependent (flat: 8â€“15Â°; whipped: 15â€“25Â°; high: 25â€“40Â°).

### 3.4 Spin Vector Calculation

#### 3.4.1 Spin Axis and Magnitude

Spin is represented as a `Vector3` angular velocity (rad/s):
- Topspin: positive rotation around the ball's lateral axis (forward-rolling)
- Backspin: negative rotation around the ball's lateral axis (backward-rolling)
- Sidespin: rotation around the ball's vertical axis (produces lateral Magnus curve)

```
SpinMagnitude = SPIN_BASE(PassType) Ã— TechniqueScale(Technique)
// SPIN_BASE: per-type baseline spin rate [rad/s]
// TechniqueScale: maps Technique [1â€“20] to [0.5 â€“ 1.5] multiplier
```

#### 3.4.2 Interface to Ball Physics Magnus Effect

Pass Mechanics provides the spin vector. Ball Physics Â§3.1 applies Magnus force per frame. Pass Mechanics does not compute trajectory â€” it only initiates spin state.

### 3.5 Error Model

#### 3.5.1 Error Philosophy

Error in this system is **angular deviation at the kick point**. A small angular error at the foot becomes a large positional miss at 40m. This is physically correct and produces realistic behaviour: short passes are inherently more accurate than long passes even at identical error angles.

The error angle is **not random**. It is computed deterministically from:
- Passing attribute (primary â€” direct accuracy)
- Pressure scalar (degradation from nearby opponents)
- Fatigue level (degradation as fatigue increases)
- Body orientation (penalty for misaligned body)
- Urgency modifier (rushed pass = higher error)
- Pass type modifier (lofted passes are more error-sensitive)

#### 3.5.2 Error Magnitude Formula

```
ErrorAngle = BASE_ERROR(PassType) 
           Ã— PassingModifier(Passing) 
           Ã— PressureModifier(PressureScalar) 
           Ã— FatigueModifier(Fatigue)
           Ã— OrientationModifier(BodyAngle)
           Ã— UrgencyModifier(Urgency)
           Ã— WeakFootModifier(IsWeakFoot, WeakFootRating)
```

Where each modifier is a scalar multiplier. Full derivation of each modifier, with constants, rationale, and [GT] flags in Section 3.5 of the full spec.

**Error magnitude bounds:**
- Minimum: `MIN_ERROR_DEGREES` per pass type (even perfect passer has slight natural variation)
- Maximum: `MAX_ERROR_DEGREES` per pass type (prevents comically bad passes at any attribute level)

#### 3.5.3 Error Direction

Error angle rotates the kick direction vector in a consistent plane (horizontal rotation in Stage 0 â€” no vertical error component for ground/driven passes; both horizontal and vertical for lofted/chip).

Direction is determined by a seeded deterministic hash of `{AgentID, FrameNumber, PassCount}` â€” produces consistent direction per event without System.Random.

#### 3.5.4 Error Application

```
// Rotate intended kick direction by error angle
Vector3 intendedDirection = (targetPosition - agentPosition).normalized;
Vector3 errorAxis = Vector3.up;  // horizontal error only (ground passes)
Vector3 actualKickDirection = Quaternion.AngleAxis(errorAngle, errorAxis) * intendedDirection;
Vector3 finalVelocity = actualKickDirection * kickSpeed;
// Elevation component added: finalVelocity.y += speed * sin(launchAngle)
```

### 3.6 Target Resolution

#### 3.6.1 Player-Targeted Pass

For player-targeted passes, aim at the receiver's **projected position** at estimated ball arrival time:

```
// Step 1: Estimate ball travel time (simplified â€” ignores deceleration in Stage 0)
float travelTime = distance / kickSpeed;  // [GT] â€” add drag correction in Stage 2

// Step 2: Project receiver position
Vector3 projectedReceiverPosition = receiver.Position + receiver.Velocity * travelTime;

// Step 3: Target is projected position
targetPosition = projectedReceiverPosition;
```

**Stage 0 limitation:** Linear projection only. Receiver may deviate from linear path; this is intentional â€” creates realistic scenario where poorly timed passes miss the run. Documented explicitly as a Stage 0 constraint.

#### 3.6.2 Space-Targeted Pass (Through Ball Lead)

Through balls target empty space. The key computation is the **interception point** â€” where ball and receiver arrive simultaneously:

```
// Find t such that:
// BallPosition(t) = ReceiverPosition(t)
// ball travels at kick speed; receiver moves at current velocity
// 
// Simplified Stage 0 (linear, no deceleration):
// |targetPosition - agentPosition| / kickSpeed = |targetPosition - receiverPosition| / receiverSpeed
// Solve for targetPosition (interception point)
// Full derivation: Appendix A Â§A.3
```

If no solution exists within pitch bounds, fall back to raw target coordinate (FM-04).

#### 3.6.3 Lead Distance Calculation

Lead distance is the distance ahead of the receiver's current position to the interception point. Published in `PassResult.LeadDistance` for analytics.

### 3.7 Weak Foot Model

#### 3.7.1 Weak Foot Detection

Decision Tree (#8) determines if the pass requires the non-preferred foot and sets `PassRequest.IsWeakFoot`. Pass Mechanics does not detect preferred foot â€” it applies the penalty if flagged.

#### 3.7.2 Weak Foot Penalty Formula

```
WeakFootMultiplier = WEAK_FOOT_BASE_PENALTY + (WeakFootRating / MAX_WEAK_FOOT_RATING) Ã— (1.0 - WEAK_FOOT_BASE_PENALTY)
// WeakFootRating [1â€“5]; higher = better weak foot
// WeakFootRating = 5: multiplier = 1.0 (no penalty)
// WeakFootRating = 1: multiplier = WEAK_FOOT_BASE_PENALTY (significant accuracy hit)
```

`WEAK_FOOT_BASE_PENALTY` is a [GT] constant (~0.5â€“0.7 â€” halving accuracy on very poor weak foot is likely reasonable).

### 3.8 Pass Execution State Machine

#### 3.8.1 State Definitions

| State | Description | Duration | Exit Conditions |
|-------|-------------|----------|-----------------|
| IDLE | No pass in progress | Indefinite | PassRequest received |
| INITIATING | Request received, validation in progress | 1 frame | Validation pass â†’ WINDUP; fail â†’ IDLE |
| WINDUP | Agent preparing kick; interruptible | TYPE_WINDUP_FRAMES[PassType] | Timer expires â†’ CONTACT; tackle â†’ IDLE |
| CONTACT | Ball.ApplyKick() called; irreversible | 1 frame | Always â†’ FOLLOW_THROUGH |
| FOLLOW_THROUGH | Cosmetic state; no physics | TYPE_FOLLOWTHROUGH_FRAMES | Timer expires â†’ IDLE |
| COMPLETE | Alias for IDLE entry; used for event timing | 0 frames | Immediately â†’ IDLE |

#### 3.8.2 Windup Duration by Pass Type

Windup duration represents the physical preparation time before kicking. All values [GT] and require gameplay testing:

| PassType | Windup Frames (60Hz) | Windup Duration (ms) | Rationale |
|----------|---------------------|---------------------|-----------|
| Ground | 8 | 133ms | Quick short pass |
| Driven | 12 | 200ms | Requires more leg swing |
| Lofted | 15 | 250ms | Larger swing for height |
| ThroughBall | 8 | 133ms | Same as ground |
| AerialThrough | 14 | 233ms | Lofted equivalent |
| Cross | 12 | 200ms | Comparable to driven |
| Chip | 10 | 167ms | Precise but compact |

**Urgency modifier:** High urgency (`Urgency â‰ˆ 1.0`) reduces windup by up to 40%. Reduced windup increases error via `UrgencyModifier` in the error formula.

#### 3.8.3 Cancellation During Windup

If Collision System detects a tackle contact on the passing agent during WINDUP:
1. Pass Mechanics transitions to IDLE
2. PassResult.Outcome = Cancelled
3. No `PassAttemptEvent` published (pass was never committed)
4. Tackle result handled by Collision System (#3)

#### 3.8.4 State Machine Diagram (Mermaid â€” for Appendix F)

```
stateDiagram-v2
    [*] --> IDLE
    IDLE --> INITIATING : PassRequest received
    INITIATING --> WINDUP : Validation passed
    INITIATING --> IDLE : Validation failed (FM-01, FM-07)
    WINDUP --> CONTACT : Windup timer complete
    WINDUP --> IDLE : Tackle interrupt (FM-06 exception)
    CONTACT --> FOLLOW_THROUGH : Ball.ApplyKick() called
    FOLLOW_THROUGH --> IDLE : Follow-through timer complete
    IDLE --> [*]
```

---

## SECTION 4: ARCHITECTURE & INTEGRATION (~4 pages)

### 4.1 File Structure

```
/Scripts/PassMechanics/
  PassExecutor.cs              â€” Main orchestrator; state machine; entry point
  PassVelocityCalculator.cs    â€” Velocity, launch angle, spin computation
  PassErrorCalculator.cs       â€” Deterministic error model
  PassTargetResolver.cs        â€” Player-targeted and space-targeted resolution
  PassTypeProfiles.cs          â€” Physical profile constants per pass type
  PassConstants.cs             â€” All tunable constants
  PassRequest.cs               â€” PassRequest struct
  PassResult.cs                â€” PassResult struct
  PassType.cs                  â€” PassType and PassOutcome enums
  PassEvents.cs                â€” PassAttemptEvent, PassCompletedEvent, etc.
  /Tests/
    PassVelocityTests.cs       â€” Unit tests Â§5.2.1
    PassErrorTests.cs          â€” Unit tests Â§5.2.2
    PassTargetTests.cs         â€” Unit tests Â§5.2.3
    PassStateMachineTests.cs   â€” Unit tests Â§5.2.4
    PassIntegrationTests.cs    â€” Integration tests Â§5.3
```

### 4.2 Integration with Ball Physics (#1)

**Interface contract (owned by Ball Physics Â§3.1.11.2):**
```csharp
Ball.ApplyKick(Vector3 velocity, Vector3 spin, KickType kickType)
```

Pass Mechanics maps `PassType` â†’ `KickType` before calling. If KickType enum in Ball Physics does not cover all pass types, a mapping table is maintained in `PassTypeProfiles.cs`.

**Constraint:** `ApplyKick` must be called exactly once per CONTACT state. Calling with zero velocity is forbidden â€” use PassOutcome.Cancelled instead.

### 4.3 Integration with Agent Movement (#2)

**Read operations (per pass execution):**
- `AgentAttributes`: Passing, Power, Technique, Fatigue, WeakFootRating
- `AgentState`: Position, Velocity, FacingDirection

**Write operations:** None. Pass Mechanics does not modify agent movement state. Agent possession loss is handled by Ball Physics possession system.

### 4.4 Integration with Collision System (#3)

**Pressure query:** Spatial hash `QueryRadius(agentPosition, PRESSURE_RADIUS_MAX)` called once at CONTACT state.

**Tackle interrupt:** Collision System notifies Pass Mechanics via callback or flag during WINDUP state. Interface contract to be defined jointly with Collision System spec.

### 4.5 Integration with First Touch (#4)

**No direct call.** Pass Mechanics initiates ball movement via Ball Physics. First Touch receives the ball when an agent makes contact. The connection is implicit through the ball state.

**Event chain:** PassAttemptEvent â†’ (ball in flight) â†’ FirstTouchEvent â†’ PassCompletedEvent (cross-spec event chain via Event System).

### 4.6 Integration with Event System (#17)

| Event | Published At | Contains |
|-------|-------------|---------|
| PassAttemptEvent | CONTACT state | AgentID, PassType, TargetPosition, ErrorAngle, Frame |
| PassCancelledEvent | WINDUP â†’ IDLE | AgentID, CancelReason, Frame |
| PassCompletedEvent | Triggered by First Touch (#4) | PasserID, ReceiverID, Distance, PassType |
| PassInterceptedEvent | Triggered by Ball Physics possession change | PasserID, InterceptorID, Frame |

---

## SECTION 5: TESTING (~8 pages)

### 5.1 Test Philosophy

All tests are deterministic. No test uses System.Random. Tests use fixed attribute values, fixed positions, and fixed frame numbers to produce predictable results.

Tests are categorized:
- **Unit (PV-):** Pass velocity model
- **Unit (PE-):** Pass error model
- **Unit (PT-):** Pass target resolution
- **Unit (PSM-):** Pass state machine
- **Unit (EC-):** Edge cases
- **Integration (IT-):** Multi-system tests
- **Validation Scenarios (VS-):** Hand-calculated ground truth

### 5.2 Unit Tests

**Target: 35 unit tests across 5 categories**

#### 5.2.1 Pass Velocity Tests (PV-) â€” 8 tests

| ID | Test | Expected |
|----|------|---------|
| PV-001 | Ground pass, Power=10, Distance=20m | V in [10â€“16] m/s range |
| PV-002 | Driven pass, Power=18, Distance=35m | V in [20â€“26] m/s range |
| PV-003 | Lofted pass, Power=10, Distance=40m | V in [14â€“20] m/s range |
| PV-004 | Ground pass, Power=1, Distance=30m (long for weak player) | V clamped to MIN_VELOCITY |
| PV-005 | Chip pass, Power=20, Distance=10m | V clamped to chip MAX_VELOCITY |
| PV-006 | Fatigue=1.0, Power=15, Driven | V reduced by FATIGUE_POWER_REDUCTION |
| PV-007 | Fatigue=0.0, Power=15, Driven | No fatigue reduction |
| PV-008 | All pass types: verify V within defined profile range | All within bounds |

#### 5.2.2 Pass Error Tests (PE-) â€” 10 tests

| ID | Test | Expected |
|----|------|---------|
| PE-001 | Passing=20, no pressure, no fatigue, aligned body | Error â‰¤ MIN_ERROR for pass type |
| PE-002 | Passing=1, max pressure, full fatigue, misaligned | Error = MAX_ERROR for pass type |
| PE-003 | Pressure=0.0 vs Pressure=1.0, other inputs fixed | Error increases monotonically with pressure |
| PE-004 | Fatigue=0.0 vs Fatigue=1.0, other inputs fixed | Error increases monotonically with fatigue |
| PE-005 | WeakFoot=true, WeakFootRating=1 vs Rating=5 | Rating=1 produces higher error than Rating=5 |
| PE-006 | WeakFoot=false | No weak foot penalty applied |
| PE-007 | Urgency=0.0 vs Urgency=1.0 | High urgency increases error |
| PE-008 | Lofted pass vs Ground pass, identical other inputs | Lofted error magnitude â‰¥ Ground error magnitude |
| PE-009 | Determinism check: same inputs Ã— 100 calls | All 100 error angles identical |
| PE-010 | Error direction: verify rotation direction is consistent | Error applied in correct axis |

#### 5.2.3 Pass Target Resolution Tests (PT-) â€” 8 tests

| ID | Test | Expected |
|----|------|---------|
| PT-001 | Player-targeted, stationary receiver | Target = receiver.Position |
| PT-002 | Player-targeted, receiver moving at 5 m/s | Target = projected position ahead |
| PT-003 | Through ball, receiver moving toward space | Valid interception point returned |
| PT-004 | Through ball, no valid interception (receiver too slow) | Falls back to raw target |
| PT-005 | Through ball, interception point outside pitch | Clamped to pitch boundary |
| PT-006 | Lead distance: verify = 0 for stationary receiver | LeadDistance = 0 |
| PT-007 | Lead distance: positive value for moving receiver | LeadDistance > 0 |
| PT-008 | Zero distance pass (agent at target) | Outcome = Invalid (FM-07) |

#### 5.2.4 State Machine Tests (PSM-) â€” 6 tests

| ID | Test | Expected |
|----|------|---------|
| PSM-001 | Valid request â†’ state progression to COMPLETE | IDLEâ†’INITIATINGâ†’WINDUPâ†’CONTACTâ†’FOLLOW_THROUGHâ†’IDLE |
| PSM-002 | Invalid request (no possession) | IDLEâ†’INITIATINGâ†’IDLE |
| PSM-003 | Tackle interrupt during WINDUP | State â†’ IDLE, Outcome = Cancelled |
| PSM-004 | Tackle interrupt during CONTACT | Ignored; pass proceeds |
| PSM-005 | Urgency=1.0 reduces windup duration | Windup frames < base duration |
| PSM-006 | Ball.ApplyKick called exactly once per CONTACT | Call count = 1 |

#### 5.2.5 Edge Case Tests (EC-) â€” 5 tests

| ID | Test | Expected |
|----|------|---------|
| EC-001 | NaN velocity post-calculation | Clamped to zero, Outcome = Cancelled |
| EC-002 | All attributes at minimum (1) | No crash; valid (if degraded) pass result |
| EC-003 | All attributes at maximum (20) | Maximum quality pass within type bounds |
| EC-004 | Target position exactly on pitch boundary | Valid pass; no clamp needed |
| EC-005 | Simultaneous tackle and CONTACT on same frame | CONTACT takes priority per FR-9 |

### 5.3 Integration Tests (IT-) â€” 12 tests

| ID | Test | Systems Involved |
|----|------|-----------------|
| IT-001 | Full pass chain: ground pass to stationary target | Pass Mechanics â†’ Ball Physics â†’ First Touch |
| IT-002 | Full pass chain: driven pass under pressure | Collision (#3) pressure â†’ Pass Mechanics â†’ Ball Physics |
| IT-003 | Through ball: receiver runs onto pass | Pass Mechanics (lead calc) â†’ Ball Physics â†’ First Touch |
| IT-004 | Intercepted pass: opponent intercepts in flight | Ball Physics possession change â†’ PassInterceptedEvent |
| IT-005 | Windup cancelled by tackle | Collision (#3) â†’ Pass Mechanics cancel â†’ no Ball.ApplyKick |
| IT-006 | Weak foot cross: accuracy degraded | Pass Mechanics error model â†’ Ball Physics curve trajectory |
| IT-007 | High-pressure pass: error visibly larger | Collision (#3) pressure â†’ Pass Mechanics â†’ error validation |
| IT-008 | Chip over defender: arc clears defender hitbox | Pass Mechanics launch angle â†’ Ball Physics trajectory â†’ Collision (#3) miss |
| IT-009 | Pass event chain: PassAttempt â†’ PassCompleted | Event System receives both events in correct order |
| IT-010 | Replay determinism: re-simulate 50 passes, verify identical results | Determinism validation |
| IT-011 | Fatigue effect over match duration: quality degrades | Simulated 90-minute fatigue progression â†’ error increases |
| IT-012 | Cross delivery: sidespin produces visible curve | Pass Mechanics spin vector â†’ Ball Physics Magnus â†’ curved trajectory |

### 5.4 Validation Scenarios (VS-) â€” 3 hand-calculated

**VS-001: Standard Ground Pass**
- Setup: Passing=15, Power=12, Distance=20m, no pressure, no fatigue, preferred foot, Urgency=0.0
- Hand-calculate expected velocity, error angle, launch angle
- Verify system output matches within tolerance (Â±0.1 m/s, Â±0.5Â°)

**VS-002: Through Ball Lead Calculation**
- Setup: Passer at (0,0), Receiver at (10,0) moving at (5,0) m/s, kick speed=18 m/s
- Hand-calculate interception point (Appendix A Â§A.3)
- Verify LeadDistance matches hand calculation within Â±0.2m

**VS-003: Lofted Pass Trajectory Handoff**
- Setup: Power=14, Distance=35m, Lofted pass type
- Hand-calculate V_launch and launch angle using simplified projectile equations
- Verify Ball.ApplyKick is called with velocity within Â±0.5 m/s and angle within Â±1Â°

### 5.5 Performance Tests â€” 5 tests

| ID | Test | Target |
|----|------|--------|
| PERF-001 | Single pass execution (all calculations) | p95 < 0.05ms |
| PERF-002 | 22 simultaneous pass attempts (stress test) | Total < 1.0ms |
| PERF-003 | Pressure query: 22 opponents in range | < 0.01ms per query |
| PERF-004 | Through ball lead calculation | < 0.005ms |
| PERF-005 | State machine transition overhead | < 0.001ms per transition |

**Test total: 35 unit + 12 integration + 3 VS + 5 performance = 55 tests**

---

## SECTION 6: PERFORMANCE ANALYSIS (~3 pages)

### 6.1 Performance Budget

Pass Mechanics is a **discrete event system** â€” it executes per-pass, not per-frame for all agents. Performance is measured per pass event, not per-frame.

**Budget target:** p95 < 0.05ms per pass execution (consistent with First Touch #4 model).

**Rationale:** At most ~2â€“3 passes initiated per second in normal match flow. Even at 10 passes/second (unrealistic), total pass computation = 0.5ms â€” well within the 5ms per-tick budget.

### 6.2 Operation Count Analysis

Per pass execution:

| Operation | Estimated Cost | Notes |
|-----------|---------------|-------|
| PassRequest validation | < 5 ops | Null checks, bounds checks |
| Target resolution (player) | ~20 ops | 3D projection, vector math |
| Target resolution (through ball) | ~40 ops | Interception calculation |
| Velocity calculation | ~15 ops | Multiply-add chain |
| Launch angle derivation | ~10 ops | atan/trig â€” expensive; profile carefully |
| Spin vector calculation | ~10 ops | Multiply-add |
| Pressure query (spatial hash) | ~30 ops per opponent in range | Amortized over pass events |
| Error magnitude calculation | ~30 ops | Multiplier chain |
| Error direction calculation | ~15 ops | Hash + Quaternion rotation |
| Error application | ~10 ops | Vector rotation |
| Ball.ApplyKick call | ~5 ops | Struct copy + method dispatch |
| Event publishing | ~5 ops | Struct creation + bus enqueue |
| **Total** | **~200â€“250 ops per pass** | Well within budget |

### 6.3 Memory Footprint

| Struct | Size | Notes |
|--------|------|-------|
| PassRequest | ~40 bytes | 2Ã— int, 2Ã— Vector3, 2Ã— float, 1Ã— bool |
| PassResult | ~40 bytes | 2Ã— Vector3, 2Ã— float, 2Ã— enum |
| PassTypeProfile | ~50 bytes Ã— 7 types = 350 bytes | Constant data, loaded once |
| PassConstants | ~200 bytes | All float constants |
| **Total runtime** | **~430 bytes** | Negligible |

### 6.4 Cache Behaviour

Pass Mechanics reads `AgentAttributes` and `AgentState` â€” both already in cache from Agent Movement update earlier in the same frame. No additional cache misses expected.

Spatial hash query may cause cache miss if agents are in non-adjacent cells. Acceptable given discrete-event nature (not per-frame for all agents).

---

## SECTION 7: FUTURE EXTENSIONS (~3 pages)

### 7.1 Stage 1: Body Part Differentiation

- Instep: baseline accuracy, strong topspin
- Outside of foot: accuracy penalty (~15â€“20%), natural sidespin added
- Laces: maximum power, slight accuracy penalty for driven passes
- Heel: low probability, low accuracy â€” emergent creative passes
- Implementation: add `BodyPart` field to `PassRequest`; extend `PassTypeProfiles` with per-body-part modifiers

### 7.2 Stage 1: Team Instruction Modifiers

- "Short passing" instruction â†’ velocity reduced toward minimum for pass type; precision emphasis
- "Direct" instruction â†’ through balls and driven passes preferred; higher velocity targets
- Implementation: `TeamInstructionContext` struct passed into PassExecutor; modifiers applied after base calculation

### 7.3 Stage 1: Pass Statistics Integration

- Per-player: attempted, completed, key passes, error angle distribution
- Consumed by Statistics Engine
- Implementation: PassAttemptEvent and PassCompletedEvent extended with statistics fields

### 7.4 Stage 2: Surface Condition Effects

- Wet pitch: ground passes roll further (reduced friction â€” Ball Physics handles this; no Pass Mechanics change needed)
- Wet pitch: lofted pass landing position becomes less predictable (receiver touch difficulty increases â€” First Touch change)
- Heavy pitch: driven passes slow more quickly (Ball Physics friction model change)
- Pass Mechanics impact: minimal â€” surface effects mostly affect Ball Physics and First Touch

### 7.5 Stage 2: Player-Specific Passing Tendencies

- Each player has a pass style tendency vector (e.g., prefers short passes, prefers long diagonals)
- Modifies `PassType` probability in Decision Tree (not Pass Mechanics)
- Pass Mechanics impact: `WeakFootRating` replaced by full `FootPreferenceProfile` struct

### 7.6 Stage 5+: Fixed64 Determinism

- Replace all `float` calculations with `Fixed64` types
- All trigonometric functions replaced with Fixed64-compatible approximations
- Spin vector and velocity vector fields migrate to `Fixed64Vector3`
- Full migration plan documented in coordination with Ball Physics Â§7.4 and Agent Movement Â§6.5

---

## SECTION 8: REFERENCES (~2 pages)

### 8.1 Academic References (to be sourced during drafting)

The following academic topics require sourced references during full section writing. DOIs required where available.

- Biomechanics of the football kick: force-velocity relationship, contact duration, launch angle measurement
  - Candidate: Lees & Nolan (1998) "The biomechanics of soccer: A review"
  - Candidate: Kellis & Katis (2007) studies on instep kick mechanics
- Ball velocity measurements from real match data
  - Candidate: StatsBomb open data â€” pass velocity distributions at elite level
  - Candidate: ProZone/Opta spatial data academic papers
- Spin rate measurement on kicked footballs
  - Candidate: Bray & Kerwin (2003) aerodynamics of football
- Passing accuracy under pressure studies (sports science)
  - Candidate: Literature on performance degradation under cognitive/physical pressure

### 8.2 Internal Specifications

| Spec | What We Reference |
|------|------------------|
| Ball Physics (#1) | `Ball.ApplyKick()` interface Â§3.1.11.2; KickType enum; Magnus force model |
| Agent Movement (#2) | `AgentAttributes` struct Â§3.5.4; Fatigue model; WeakFoot attribute definition |
| Collision System (#3) | Spatial hash query interface; tackle contact detection |
| First Touch (#4) | Reception quality context; what receiver experiences after pass |
| Decision Tree (#8) | PassRequest creation context; pass type selection rationale |

### 8.3 Empirically Tuned Constants â€” Pre-Audit

The following categories of constants are expected to be [GT] (Gameplay-Tuned) with no single academic source. This is consistent with ~40% [GT] ratio established in prior specs.

| Category | Constants | Source |
|----------|-----------|--------|
| Pass velocity ranges per type | All MIN/MAX velocities | [GT] â€” observational football data |
| Windup frame durations | All windup/follow-through values | [GT] â€” playtest driven |
| Error angle bounds | MIN_ERROR, MAX_ERROR per type | [GT] â€” gameplay balance |
| Urgency error multiplier | URGENCY_ERROR_SCALE | [GT] |
| Fatigue power reduction | FATIGUE_POWER_REDUCTION | [GT] â€” partial sports science basis |
| Weak foot base penalty | WEAK_FOOT_BASE_PENALTY | [GT] |
| Spin magnitude per type | SPIN_BASE values | [GT] â€” cross-ref with Ball Physics Magnus model |

Full citation audit (matching Appendix Â§8.6 pattern from Ball Physics spec) required during Section 8 drafting.

---

## APPENDICES

### Appendix A: Formula Derivations

**A.1 Pass Velocity Derivation**
- Full derivation of `V_launch` from intended `V_target`, distance, drag, and Power attribute
- Show intermediate steps; identify where drag is simplified away in Stage 0

**A.2 Launch Angle Derivation**
- Simplified projectile motion equations for each pass type
- Table: Distance Ã— PassType â†’ recommended launch angle

**A.3 Through Ball Lead Distance Derivation**
- Simultaneous equations for ball and receiver arrival time
- Solve for interception point analytically
- Edge cases: no solution, multiple solutions, solution outside pitch

**A.4 Error Magnitude Formula Derivation**
- Each modifier's derivation from attribute scale
- Bounds proof: show that valid attribute ranges cannot produce out-of-bound error angles

### Appendix B: Pass Velocity Reference Tables

- Table: PassType Ã— Distance Ã— Power attribute â†’ expected launch velocity
- All values computed from Appendix A.1 formula (not estimated)
- Used as test oracle for PV-001 through PV-008

### Appendix C: Error Magnitude Reference Tables

- Table: Passing attribute Ã— Pressure level Ã— Fatigue level â†’ expected error angle (degrees)
- For each pass type
- Used as test oracle for PE-001 through PE-009

### Appendix D: Tolerance Derivation Tables

- Per-test tolerance values with derivation rationale
- Matches pattern from Ball Physics Appendix D and First Touch Appendix D
- Tolerance = max(ABSOLUTE_TOLERANCE, RELATIVE_FRACTION Ã— expected_value)

### Appendix E: Pass Type Taxonomy Diagram (Mermaid)

```mermaid
graph TD
    P[Pass Intent] --> G[Ground]
    P --> D[Driven]
    P --> L[Lofted]
    P --> T[Through Ball - Ground]
    P --> AT[Through Ball - Aerial]
    P --> C[Cross]
    P --> CH[Chip / Lobbed]
    
    C --> CF[Flat Cross]
    C --> CW[Whipped Cross]
    C --> CH2[High Cross]
```

### Appendix F: State Machine Diagram (Mermaid)

Full Mermaid `stateDiagram-v2` diagram as outlined in Â§3.8.4, with all states, transitions, timing annotations, and cancellation conditions.

### Appendix G: Integration Sequence Diagram

Mermaid `sequenceDiagram` showing the full data flow from Decision Tree PassRequest through Ball.ApplyKick() to First Touch reception, including Event System publications at each milestone.

---

## OPEN QUESTIONS (to resolve before/during Section 3 drafting)

| # | Question | Impact | Recommended Resolution |
|---|----------|--------|----------------------|
| OQ-1 | Does `CrossType` require a sub-enum or additional field on `PassRequest`? | Determines data structure complexity | Add `CrossSubType` optional field; default = Flat |
| OQ-2 | Is `Vision` attribute available in Stage 0? If not, what's the through ball proxy? | Affects lead distance accuracy model | Use Technique as proxy in Stage 0; document as Stage 1 upgrade point |
| OQ-3 | Is `Crossing` attribute defined in AgentAttributes? | Affects cross accuracy model | Confirm with Agent Movement spec; use Passing as proxy if absent |
| OQ-4 | How does Decision Tree signal preferred foot? Is IsWeakFoot set by Decision Tree or computed here? | Affects ownership clarity | Decision Tree should set it â€” Pass Mechanics should not reason about preferred foot |
| OQ-5 | Should PassCancelledEvent be published on FM-01 (invalid request) or only on tackle interrupt? | Event System design consistency | Only tackle interrupt; invalid request is rejected silently with log |
| OQ-6 | Does Ball Physics KickType enum cover all 7 pass types? | Determines mapping complexity | ✅ RESOLVED — Ball Physics uses parameter-based `ApplyKick()` (no KickType enum). Pass Mechanics supplies velocity vector and spin directly. |

---

