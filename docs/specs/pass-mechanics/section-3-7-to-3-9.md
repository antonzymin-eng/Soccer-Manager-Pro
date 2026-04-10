# Pass Mechanics Specification #5 — Section 3.7–3.9: Weak Foot Penalty, Execution State Machine, and Event Publishing

**File:** `Pass_Mechanics_Spec_Section_3_7_to_3_9_v1_0.md`
**Purpose:** Defines the weak foot penalty model (§3.7), the six-state pass execution
state machine (§3.8), and the event publishing contracts (§3.9) for Pass Mechanics
Specification #5. §3.7 applies accuracy and power penalties when the executing agent
uses their non-dominant foot. §3.8 manages the lifecycle from PassRequest reception
through Ball.ApplyKick() to completion. §3.9 defines the events published at state
transitions. Together these subsections complete the Section 3 Technical Specifications.

**Created:** March 7, 2026, 2:00 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 3.1 v1.1, Section 3.2 v1.0, Section 3.3–3.4 v1.0, Section 3.5–3.6 v1.0

**Dependencies:**
- §3.5 — Error model (WeakFootModifier is part of the multiplicative chain)
- §3.2 — Velocity model (weak foot also applies a power penalty)
- §4 — Integration contracts (ApplyKick, tackle interrupt, event structs)
- Agent Movement Spec #2 — `AgentAttributes.WeakFootRating` [1–5]
- Collision System Spec #3 — Tackle interrupt polling flag
- Appendix A.6 (weak foot derivation)

---

## Table of Contents

- [3.7 Weak Foot Penalty Model](#37-weak-foot-penalty-model)
  - [3.7.1 Responsibilities and Scope](#371-responsibilities-and-scope)
  - [3.7.2 Inputs and Outputs](#372-inputs-and-outputs)
  - [3.7.3 WeakFootModifier Formula — Accuracy](#373-weakfootmodifier-formula--accuracy)
  - [3.7.4 WeakFootModifier Formula — Power](#374-weakfootmodifier-formula--power)
  - [3.7.5 Implementation Reference](#375-implementation-reference)
  - [3.7.6 Constants Reference](#376-constants-reference)
  - [3.7.7 Boundary Verification](#377-boundary-verification)
  - [3.7.8 Design Decisions and Rationale](#378-design-decisions-and-rationale)
- [3.8 Pass Execution State Machine](#38-pass-execution-state-machine)
  - [3.8.1 Responsibilities and Scope](#381-responsibilities-and-scope)
  - [3.8.2 State Definitions](#382-state-definitions)
  - [3.8.3 State Transitions](#383-state-transitions)
  - [3.8.4 INITIATING — Validation and Computation](#384-initiating--validation-and-computation)
  - [3.8.5 WINDUP — Timer and Interrupt Handling](#385-windup--timer-and-interrupt-handling)
  - [3.8.6 CONTACT — ApplyKick Execution](#386-contact--applykick-execution)
  - [3.8.7 FOLLOW_THROUGH — Cosmetic Timer](#387-follow_through--cosmetic-timer)
  - [3.8.8 Urgency Effect on Windup Duration](#388-urgency-effect-on-windup-duration)
  - [3.8.9 Implementation Reference](#389-implementation-reference)
  - [3.8.10 Constants Reference](#3810-constants-reference)
  - [3.8.11 Failure Modes](#3811-failure-modes)
  - [3.8.12 Design Decisions and Rationale](#3812-design-decisions-and-rationale)
- [3.9 Event Publishing](#39-event-publishing)
  - [3.9.1 Responsibilities and Scope](#391-responsibilities-and-scope)
  - [3.9.2 PassAttemptEvent](#392-passattemptevent)
  - [3.9.3 PassCancelledEvent](#393-passcancelledevent)
  - [3.9.4 Events NOT Published by Pass Mechanics](#394-events-not-published-by-pass-mechanics)
  - [3.9.5 Stage 0 Stub Implementation](#395-stage-0-stub-implementation)
  - [3.9.6 Design Decisions and Rationale](#396-design-decisions-and-rationale)
- [Cross-Specification Dependencies](#cross-specification-dependencies)
- [Open Issues](#open-issues)
- [Version History](#version-history)

---

## 3.7 Weak Foot Penalty Model

### 3.7.1 Responsibilities and Scope

§3.7 applies accuracy and power penalties when the executing agent uses their
non-preferred foot. The `IsWeakFoot` flag is set by the Decision Tree (#8) on the
PassRequest struct — Pass Mechanics does not reason about which foot the agent
prefers. It only applies the penalty when told to.

The weak foot penalty model produces two outputs:
1. An **accuracy multiplier** (WeakFootModifier) applied to the §3.5 error chain
2. A **power reduction** (WeakFootPowerPenalty) applied to the §3.2 kick speed

Both are derived from `WeakFootRating` [1–5], a coarser scale than the standard
[1–20] attribute range (see DD-3.7-02).

---

### 3.7.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `IsWeakFoot` | `PassRequest.IsWeakFoot` | bool | — | Set by Decision Tree (#8) |
| `WeakFootRating` | `AgentAttributes.WeakFootRating` | int | [1, 5] | 1 = worst, 5 = ambidextrous |

**Outputs:**

| Output | Destination | Type | Range | Notes |
|--------|-------------|------|-------|-------|
| `WeakFootModifier` | §3.5 error chain (multiplier) | float | [1.0, 1.30] | 1.0 = no penalty |
| `WeakFootPowerPenalty` | §3.2 velocity (multiplier) | float | [0.85, 1.0] | 1.0 = no penalty |

---

### 3.7.3 WeakFootModifier Formula — Accuracy

```
if IsWeakFoot == false:
    WeakFootModifier = 1.0   (preferred foot — no penalty)
else:
    PenaltyFraction = 1.0 - ((WeakFootRating - 1) / 4.0)
    WeakFootModifier = 1.0 + PenaltyFraction × WEAK_FOOT_BASE_PENALTY
```

| WeakFootRating | PenaltyFraction | WeakFootModifier | Error Increase |
|---------------|----------------|-----------------|----------------|
| 1 (worst) | 1.00 | 1.300 | +30% error |
| 2 | 0.75 | 1.225 | +22.5% error |
| 3 | 0.50 | 1.150 | +15% error |
| 4 | 0.25 | 1.075 | +7.5% error |
| 5 (ambidextrous) | 0.00 | 1.000 | No penalty |

[CAREY-2001] documents 15–25% accuracy degradation for weak-foot passes in elite
competition. WEAK_FOOT_BASE_PENALTY = 0.30 produces a maximum of 30%, slightly
above the documented range. See OI-App-C-01 — retained as [GT] for playtesting
calibration rather than forced to the academic bound.

---

### 3.7.4 WeakFootModifier Formula — Power

Weak-foot passes are also struck with less power. A separate multiplier reduces
the kickSpeed output from §3.2:

```
if IsWeakFoot == false:
    WeakFootPowerPenalty = 1.0   (preferred foot — no reduction)
else:
    PenaltyFraction = 1.0 - ((WeakFootRating - 1) / 4.0)
    WeakFootPowerPenalty = 1.0 - PenaltyFraction × WEAK_FOOT_POWER_PENALTY
```

| WeakFootRating | WeakFootPowerPenalty | Velocity Reduction |
|---------------|---------------------|-------------------|
| 1 (worst) | 0.850 | −15% power |
| 2 | 0.888 | −11.25% power |
| 3 | 0.925 | −7.5% power |
| 4 | 0.963 | −3.75% power |
| 5 (ambidextrous) | 1.000 | No reduction |

The power penalty is applied after the §3.2 fatigue modifier but before clamping:
```
V_unclamped = V_base × FatigueModifier × WeakFootPowerPenalty
kickSpeed = clamp(V_unclamped, vMin, vMax)
```

---

### 3.7.5 Implementation Reference

```csharp
/// <summary>
/// Computes the weak foot accuracy modifier for the error chain.
/// Returns 1.0 if IsWeakFoot is false. Pure function.
/// </summary>
public static float ComputeWeakFootAccuracyModifier(bool isWeakFoot, int weakFootRating)
{
    if (!isWeakFoot) return 1.0f;

    int R = Mathf.Clamp(weakFootRating, 1, 5);
    float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
    return 1.0f + penaltyFraction * PassConstants.WEAK_FOOT_BASE_PENALTY;
}

/// <summary>
/// Computes the weak foot power penalty for the velocity calculation.
/// Returns 1.0 if IsWeakFoot is false. Pure function.
/// </summary>
public static float ComputeWeakFootPowerPenalty(bool isWeakFoot, int weakFootRating)
{
    if (!isWeakFoot) return 1.0f;

    int R = Mathf.Clamp(weakFootRating, 1, 5);
    float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
    return 1.0f - penaltyFraction * PassConstants.WEAK_FOOT_POWER_PENALTY;
}
```

---

### 3.7.6 Constants Reference

| Constant | Value | Tag | Source | Notes |
|----------|-------|-----|--------|-------|
| `WEAK_FOOT_BASE_PENALTY` | 0.30 | [GT] | [CAREY-2001] 15–25% | Maximum +30% error at Rating=1 |
| `WEAK_FOOT_POWER_PENALTY` | 0.15 | [GT] | Design decision | Maximum −15% power at Rating=1 |

---

### 3.7.7 Boundary Verification

**Check 1: Preferred foot — no penalty (WF-001)**
```
IsWeakFoot = false, Rating = any
WeakFootModifier = 1.0 ✓
WeakFootPowerPenalty = 1.0 ✓
```

**Check 2: Worst weak foot — maximum penalty (WF-002)**
```
IsWeakFoot = true, Rating = 1
WeakFootModifier = 1.0 + 1.0 × 0.30 = 1.30 ✓
WeakFootPowerPenalty = 1.0 - 1.0 × 0.15 = 0.85 ✓
```

**Check 3: Ambidextrous — no penalty even with IsWeakFoot = true (WF-003)**
```
IsWeakFoot = true, Rating = 5
PenaltyFraction = 1.0 - (4/4) = 0.0
WeakFootModifier = 1.0 + 0.0 × 0.30 = 1.0 ✓
WeakFootPowerPenalty = 1.0 - 0.0 × 0.15 = 1.0 ✓
```

**Check 4: Monotone decrease as Rating improves (WF-004)**
```
Modifiers at R = {1, 2, 3, 4, 5}: {1.300, 1.225, 1.150, 1.075, 1.000}
Strictly decreasing ✓
```

---

### 3.7.8 Design Decisions and Rationale

**DD-3.7-01: Weak foot is a scalar penalty, not a separate system**

A full weak-foot system would model different contact points, different spin
profiles, and different trajectory shapes for each foot. At Stage 0, this complexity
is unwarranted — a single error multiplier and power reduction capture the essential
gameplay effect: weak-foot passes are less accurate and less powerful. Stage 1
extends this with a full `FootPreferenceProfile` (§7.1.4).

**DD-3.7-02: WeakFootRating uses [1, 5] scale, not [1, 20]**

Foot preference is a coarser trait than passing skill. Five levels are sufficient:
1 = severe weakness (no left foot whatsoever), 2 = poor, 3 = competent,
4 = strong secondary foot, 5 = fully ambidextrous. A [1, 20] scale would imply
precision that does not exist in football scouting data.

**DD-3.7-03: IsWeakFoot set by Decision Tree, not computed here**

Pass Mechanics does not know which foot the agent prefers. The Decision Tree (#8)
determines foot selection based on body orientation, ball position, and tactical
context, then sets `IsWeakFoot` on the PassRequest. This keeps foot-selection
reasoning in the Decision Tree where it belongs (KD-5, §1.3).

**DD-3.7-04: Separate accuracy and power penalties**

A single penalty could scale both accuracy and power identically. Separate constants
(WEAK_FOOT_BASE_PENALTY for accuracy, WEAK_FOOT_POWER_PENALTY for power) allow
independent tuning. In practice, weak-foot accuracy degrades more than weak-foot
power — a player can still strike hard with their weak foot, but with less control.
The 30% accuracy / 15% power split reflects this asymmetry.

---

## 3.8 Pass Execution State Machine

### 3.8.1 Responsibilities and Scope

§3.8 manages the six-state lifecycle of a pass execution from PassRequest reception
through Ball.ApplyKick() to completion. It is the orchestrator — it calls all
other §3.x subsystems in sequence during CONTACT state, handles tackle interrupts
during WINDUP state, and publishes events at state transitions.

The state machine runs on the 60Hz physics frame loop. It advances once per frame
while a pass is in progress. Most frames are simple timer decrements (WINDUP and
FOLLOW_THROUGH states); the computationally expensive §2.2.3 pipeline runs only
on the single CONTACT frame.

---

### 3.8.2 State Definitions

```
PassExecutionState (enum)
{
    IDLE,            // No pass in progress. Awaiting PassRequest.
    INITIATING,      // PassRequest received. Validation in progress.
    WINDUP,          // Kick preparation. Timer counting down. Interruptible.
    CONTACT,         // Ball struck. ApplyKick() called this frame. Non-interruptible.
    FOLLOW_THROUGH,  // Post-kick animation phase. Cosmetic timer only.
    COMPLETE         // Pass execution finished. Transition to IDLE next frame.
}
```

| State | Duration | Physics Work | Interruptible | Notes |
|-------|----------|-------------|---------------|-------|
| IDLE | Indefinite | None | N/A | Default resting state |
| INITIATING | 1 frame | Validation only | No | Single-frame transition |
| WINDUP | Profile-dependent (4–15 frames at 60Hz) | None | **Yes** — tackle interrupt cancels | Timer from PhysicalProfile.WINDUP_FRAMES |
| CONTACT | 1 frame | Full §2.2.3 pipeline | **No** — ball is leaving foot | ApplyKick() called; events published |
| FOLLOW_THROUGH | Profile-dependent (6–10 frames at 60Hz) | None | No | Cosmetic only; no physics |
| COMPLETE | 1 frame | Cleanup | No | Transitions to IDLE |

---

### 3.8.3 State Transitions

```
                 PassRequest received
                        │
                        ▼
    IDLE ──────► INITIATING ──────► WINDUP
     ▲                │ (fail)         │
     │                ▼                │ (tackle interrupt)
     │            IDLE (FM-01/07)      ▼
     │                              IDLE + PassCancelledEvent
     │
     │              (timer expires)
     │                │
     │                ▼
     │            CONTACT ──────► FOLLOW_THROUGH ──────► COMPLETE ──► IDLE
     │              │
     │              ├── ApplyKick() called
     │              └── PassAttemptEvent published
     │
     └───────────────────────────────────────────────────────────────┘
```

**Transition rules (exhaustive):**

| From | To | Trigger | Side Effects |
|------|-----|---------|-------------|
| IDLE | INITIATING | PassRequest received from Decision Tree | None |
| INITIATING | WINDUP | Validation passes (possession, valid type, D > 0) | All §3.x computations prepared but not applied |
| INITIATING | IDLE | Validation fails (FM-01 or FM-07) | Log error; PassResult.Outcome = Invalid |
| WINDUP | CONTACT | windupFramesRemaining == 0 | Computed values applied; ApplyKick() called |
| WINDUP | IDLE | Tackle interrupt flag detected | PassCancelledEvent published; Outcome = Cancelled |
| CONTACT | FOLLOW_THROUGH | ApplyKick() completes successfully | PassAttemptEvent published |
| FOLLOW_THROUGH | COMPLETE | followThroughFramesRemaining == 0 | None |
| COMPLETE | IDLE | Automatic next-frame transition | PassResult returned to caller |

**Forbidden transitions:**
- CONTACT → IDLE (cannot skip FOLLOW_THROUGH)
- FOLLOW_THROUGH → WINDUP (cannot re-enter preparation)
- Any state → CONTACT directly (must go through WINDUP)

---

### 3.8.4 INITIATING — Validation and Computation

On the single INITIATING frame:

1. **Possession check:** Confirm executing agent has ball possession (§4.2.2).
   Fail → FM-01 → IDLE.
2. **PassType validation:** Confirm PassType is a valid enum value.
   Fail → FM-01 → IDLE.
3. **Distance validation:** Confirm IntendedDistance > 0.
   Fail → FM-07 → IDLE.
4. **Profile lookup:** Get PhysicalProfile from PassTypeProfiles (§3.1.12).
5. **Pre-compute all §3.x values:** Target resolution (§3.6), velocity (§3.2),
   launch angle (§3.3), spin (§3.4), error (§3.5), weak foot (§3.7). These values
   are stored but not applied until CONTACT.
6. **Calculate windup duration:** windupFrames = ComputeWindupFrames(profile, urgency)
   (§3.8.8).
7. **Transition to WINDUP** with timer set to windupFrames.

**Why pre-compute during INITIATING rather than CONTACT?**

Pre-computing spreads the computational cost across two frames — validation and
computation on the INITIATING frame, application and ApplyKick() on the CONTACT
frame. This keeps the CONTACT frame fast (§6.3). The stored values are immutable
once computed; no input can change between INITIATING and CONTACT because the
agent is locked into the pass execution animation.

---

### 3.8.5 WINDUP — Timer and Interrupt Handling

Each frame during WINDUP:

```
1. Poll tackle interrupt flag from Collision System (§4.4.2):
   tackled = CollisionSystem.GetAndClearTackleFlag(agentId)

2. If tackled:
   - Transition WINDUP → IDLE
   - Set PassResult.Outcome = Cancelled
   - Set PassResult.CancelReason = TACKLE_INTERRUPT
   - Publish PassCancelledEvent (§3.9.3)
   - Return immediately

3. Decrement windupFramesRemaining

4. If windupFramesRemaining == 0:
   - Transition WINDUP → CONTACT
```

**Timing detail:** The tackle flag is polled at the *start* of each WINDUP frame,
before the timer is decremented. This means a tackle on the final WINDUP frame
still cancels the pass — the ball has not yet left the foot.

---

### 3.8.6 CONTACT — ApplyKick Execution

On the single CONTACT frame, the full §2.2.3 pipeline executes:

```
1. Re-sample pressure scalar from Collision System (§3.5.6)
   (Pressure is sampled at CONTACT, not INITIATING — §4.4.1)

2. Recompute error angle with fresh pressure (§3.5)

3. Apply error to kick direction (§3.6.7)

4. Construct final velocity Vector3 (§3.3.6)

5. Call Ball.ApplyKick(ref ball, velocity, spin, agentId, matchTime)
   - This call is synchronous
   - Ball Physics applies the velocity within the same frame (Step 8, §2.3)
   - Possession is released by Ball Physics (§4.2.3)

6. Populate PassResult with all computed values

7. Publish PassAttemptEvent (§3.9.2)

8. Transition CONTACT → FOLLOW_THROUGH with timer = FOLLOWTHROUGH_FRAMES
```

**Critical invariant (PSM-006):** Ball.ApplyKick() is called **exactly once** per
pass execution. Never zero times (unless cancelled), never more than once. A double
call would apply double velocity to the ball. The state machine structure enforces
this — CONTACT is a single-frame state that immediately transitions to FOLLOW_THROUGH.

---

### 3.8.7 FOLLOW_THROUGH — Cosmetic Timer

Each frame during FOLLOW_THROUGH:

```
1. Decrement followThroughFramesRemaining

2. If followThroughFramesRemaining == 0:
   - Transition FOLLOW_THROUGH → COMPLETE
```

FOLLOW_THROUGH has no physics effects. It exists to:
- Prevent the agent from immediately initiating another action after kicking
- Provide animation timing data (PassAnimationData) for Stage 1
- Model the realistic recovery time after striking the ball

Tackle interrupts during FOLLOW_THROUGH are **ignored** — the ball has already
left the foot. A collision during follow-through is handled by Collision System
as a standard agent-agent contact, not as a pass cancellation.

---

### 3.8.8 Urgency Effect on Windup Duration

Higher urgency reduces windup duration — a rushed pass has a shorter preparation
phase but higher error (§3.5.4 UrgencyModifier).

```
windupFrames = max(
    MIN_WINDUP_FRAMES,
    round(profile.WINDUP_FRAMES × (1.0 - Urgency × URGENCY_WINDUP_REDUCTION))
)
```

| Urgency | Windup Multiplier | Ground (base 8 frames) | Driven (base 12 frames) |
|---------|-------------------|----------------------|------------------------|
| 0.0 (measured) | 1.00 | 8 frames (133ms) | 12 frames (200ms) |
| 0.5 | 0.75 | 6 frames (100ms) | 9 frames (150ms) |
| 1.0 (rushed) | 0.50 | 4 frames (67ms) | 6 frames (100ms) |

MIN_WINDUP_FRAMES = 3 (50ms at 60Hz) — the physical minimum time to execute a
kicking motion. Even a panic clearance requires at least 50ms.

---

### 3.8.9 Implementation Reference

```csharp
/// <summary>
/// State machine update. Called once per frame by the simulation loop.
/// </summary>
public void Update(float matchTime)
{
    switch (_state)
    {
        case PassExecutionState.IDLE:
            // Nothing to do — awaiting PassRequest
            break;

        case PassExecutionState.INITIATING:
            // Validation and pre-computation completed in Execute()
            // Transition to WINDUP was set by Execute()
            break;

        case PassExecutionState.WINDUP:
            // Step 1: Check for tackle interrupt
            if (CollisionSystem.GetAndClearTackleFlag(_request.AgentID))
            {
                _result.Outcome = PassOutcome.Cancelled;
                _result.CancelReason = CancelReason.TACKLE_INTERRUPT;
                EventBusStub.Publish(CreatePassCancelledEvent(matchTime));
                _state = PassExecutionState.IDLE;
                return;
            }
            // Step 2: Decrement timer
            _windupFramesRemaining--;
            if (_windupFramesRemaining <= 0)
            {
                _state = PassExecutionState.CONTACT;
            }
            break;

        case PassExecutionState.CONTACT:
            ExecuteContact(matchTime);  // Full §2.2.3 pipeline
            _state = PassExecutionState.FOLLOW_THROUGH;
            _followThroughFramesRemaining = _profile.FOLLOWTHROUGH_FRAMES;
            break;

        case PassExecutionState.FOLLOW_THROUGH:
            _followThroughFramesRemaining--;
            if (_followThroughFramesRemaining <= 0)
            {
                _state = PassExecutionState.COMPLETE;
            }
            break;

        case PassExecutionState.COMPLETE:
            _state = PassExecutionState.IDLE;
            break;
    }
}
```

---

### 3.8.10 Constants Reference

#### Per-Type Timing Constants (from §3.1.4 PhysicalProfile)

| Pass Type | WINDUP_FRAMES | FOLLOWTHROUGH_FRAMES | Total (at 60Hz) |
|-----------|--------------|---------------------|-----------------|
| Ground | 8 | 6 | 233ms |
| Driven | 12 | 8 | 333ms |
| Lofted | 15 | 10 | 417ms |
| ThroughBall | 8 | 6 | 233ms |
| AerialThrough | 14 | 10 | 400ms |
| Cross (Flat) | 12 | 8 | 333ms |
| Cross (Whipped) | 12 | 8 | 333ms |
| Cross (High) | 14 | 10 | 400ms |
| Chip | 10 | 8 | 300ms |

> All timing values are [GT]. Source: [FRANKS-1985] provides plausibility bracket
> for total kick execution time (200–500ms). Windup/follow-through split is a
> design decision for gameplay feel.

#### Universal Timing Constants

| Constant | Value | Tag | Notes |
|----------|-------|-----|-------|
| `MIN_WINDUP_FRAMES` | 3 | [GT] | 50ms floor — physical minimum for kick motion |
| `URGENCY_WINDUP_REDUCTION` | 0.50 | [GT] | At Urgency=1.0, windup is halved |

---

### 3.8.11 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| FM-01 | Agent does not have possession at INITIATING | IDLE; Outcome = Invalid; log error | Programming error in Decision Tree |
| FM-03 | Tackle interrupt during WINDUP | IDLE; Outcome = Cancelled; PassCancelledEvent | Normal game event |
| FM-06 | Tackle interrupt during CONTACT or FOLLOW_THROUGH | Ignore; clear flag; pass proceeds | Ball already left foot |
| FM-07 | IntendedDistance ≤ 0 at INITIATING | IDLE; Outcome = Invalid; log error | Programming error in Decision Tree |
| FM-04 | NaN in computed velocity at CONTACT | IDLE; Outcome = Cancelled; log error; ApplyKick NOT called | Defensive guard — should not occur |

---

### 3.8.12 Design Decisions and Rationale

**DD-3.8-01: Six states, not three or four**

A minimal state machine could use IDLE → EXECUTING → IDLE. The six-state design
separates concerns: INITIATING for validation, WINDUP for interruptibility, CONTACT
for the single ApplyKick() frame, FOLLOW_THROUGH for animation, COMPLETE for cleanup.
Each state has a clear responsibility and a distinct set of allowed transitions.

**DD-3.8-02: WINDUP is interruptible; CONTACT is not**

Once the ball leaves the foot (CONTACT), the pass cannot be undone. A tackle during
WINDUP represents a defender dispossessing the agent before the kick — realistic and
tactically important. A tackle during CONTACT would require "un-kicking" a ball
already in flight — physically impossible.

**DD-3.8-03: Pre-computation at INITIATING, application at CONTACT**

Splitting computation across two frames reduces CONTACT frame cost. Values computed
at INITIATING are stored immutably. The agent is locked into the pass animation
during WINDUP, so no input changes between computation and application.

Exception: PressureScalar is re-sampled at CONTACT (§3.8.6 Step 1). Pressure can
change during WINDUP if opponents move. This is the only value re-computed at
CONTACT, and it is the only sub-system that depends on external state at kick time.

**DD-3.8-04: FOLLOW_THROUGH is cosmetic but mandatory**

Skipping FOLLOW_THROUGH (CONTACT → IDLE) would allow an agent to immediately start
another action. This is physically unrealistic and creates exploitation potential
(instant pass chains). The timer enforces a realistic recovery delay.

**DD-3.8-05: MIN_WINDUP_FRAMES = 3 (50ms floor)**

Even a fully rushed pass (Urgency = 1.0) requires a minimum physical preparation
time. 50ms at 60Hz (3 frames) is the minimum time for a ballistic leg motion from
a standing position. Below this, the kick animation would be physically implausible.

---

## 3.9 Event Publishing

### 3.9.1 Responsibilities and Scope

§3.9 defines the events published by Pass Mechanics at state transitions. Two events
are published at Stage 0:

1. **PassAttemptEvent** — published at CONTACT state after Ball.ApplyKick() succeeds
2. **PassCancelledEvent** — published when a tackle interrupt cancels the pass during WINDUP

These events are consumed by the Event System (#17) at Stage 1. In Stage 0, they are
published to a no-op stub (§3.9.5) that validates struct compliance without dispatch.

§3.9 does **not** publish:
- PassCompletedEvent (owned by First Touch #4 — on successful reception)
- PassInterceptedEvent (owned by Ball Physics #1 — on possession change during flight)
- PassOutOfPlayEvent (owned by Out-of-Play System — on touchline/goal line crossing)

---

### 3.9.2 PassAttemptEvent

Published at: CONTACT state, after Ball.ApplyKick() completes (§3.8.6 Step 7).

```csharp
public struct PassAttemptEvent
{
    public int         AgentId;          // ID of the passing agent
    public int         TeamId;           // Team of the passing agent
    public PassType    PassType;         // Pass type executed
    public CrossSubType CrossSubType;    // Cross sub-type (Flat if non-cross)
    public Vector3     TargetPosition;   // Pre-error aim point
    public Vector3     FinalVelocity;    // Velocity vector passed to ApplyKick()
    public Vector3     FinalSpin;        // Spin vector passed to ApplyKick()
    public float       ErrorAngleDeg;    // Error magnitude applied (degrees)
    public float       KickSpeed;        // Scalar launch speed (m/s)
    public float       LeadDistance;     // Through-ball lead (0 for player-targeted)
    public bool        IsWeakFoot;       // True if non-preferred foot was used
    public int         TargetAgentId;    // -1 for space-targeted passes
    public int         Frame;            // Simulation frame at kick
    public float       MatchTime;        // Match time in seconds at kick
}
```

**Struct size:** ~100 bytes. All value types — no heap allocation.

---

### 3.9.3 PassCancelledEvent

Published at: WINDUP → IDLE transition (tackle interrupt only, §3.8.5).

```csharp
public struct PassCancelledEvent
{
    public int          AgentId;         // ID of the agent whose pass was cancelled
    public int          TeamId;          // Team of the executing agent
    public CancelReason CancelReason;    // TACKLE_INTERRUPT (only valid reason in Stage 0)
    public PassType     PassType;        // Pass type that was in progress
    public int          Frame;           // Simulation frame at cancellation
    public float        MatchTime;       // Match time in seconds at cancellation
}

public enum CancelReason
{
    TACKLE_INTERRUPT,     // Collision System set tackle flag during WINDUP
    // Additional reasons reserved for Stage 2+ (e.g., STUMBLE_INTERRUPT)
}
```

**Invalid request rejection does NOT produce PassCancelledEvent.** An invalid
PassRequest (FM-01, FM-07) is rejected silently with a logged error. It represents
a programming error, not a game event. See KD-8 (§1.3).

---

### 3.9.4 Events NOT Published by Pass Mechanics

| Event | Actual Publisher | Trigger |
|-------|-----------------|---------|
| `PassCompletedEvent` | First Touch (#4) | Receiving agent achieves controlled touch |
| `PassInterceptedEvent` | Ball Physics (#1) | Possession changes to opponent during flight |
| `PassOutOfPlayEvent` | Out-of-Play System (TBD) | Ball crosses touchline or goal line |

Pass Mechanics must not listen for these events. If Pass Mechanics appears to need
information from PassCompletedEvent, that is a sign of scope creep (§4.6.2).

---

### 3.9.5 Stage 0 Stub Implementation

```csharp
// Stage 0 stub — replace with real Event System (#17) at Stage 1
public static class EventBusStub
{
    public static void Publish<T>(T evt) where T : struct
    {
    #if DEVELOPMENT_BUILD
        Debug.Log($"[EventBus STUB] {typeof(T).Name} @ frame {GetFrame(evt)}");
    #endif
        // No dispatch. Event is discarded.
    }
}
```

The stub must be **replaced, not removed** at Stage 1. The replacement must accept
the same struct signatures without modification. If Event System (#17) requires
different publish signatures, this specification requires a corresponding amendment.

---

### 3.9.6 Design Decisions and Rationale

**DD-3.9-01: Struct events, not class events**

Struct-based events require zero heap allocation. At ~1000 pass events per match,
class-based events would produce ~1000 GC allocations in the hot path. The
zero-allocation policy (§6.8 P0) prohibits this.

**DD-3.9-02: PassAttemptEvent carries full context**

The event includes velocity, spin, error angle, target position, and all metadata.
This allows the Statistics Engine (Stage 1) to perform complete pass analysis
without querying back into Pass Mechanics. The trade-off is a larger struct (~100
bytes), but this is published once per pass, not per frame.

**DD-3.9-03: Invalid request rejection is silent, not evented (KD-8)**

An FM-01 or FM-07 rejection indicates a programming bug in the Decision Tree. It
should not appear in game event streams or statistics. It is logged for debugging
only. Only a tackle interrupt — a real game event — produces a cancellation event.

---

## Cross-Specification Dependencies

| Dependency | Owner | Status | Blocks |
|------------|-------|--------|--------|
| `AgentAttributes.WeakFootRating` [1–5] | Agent Movement §3.5 | ✅ Resolved (ERR-007) | None |
| `CollisionSystem.GetAndClearTackleFlag(agentId)` | Collision System §4 / XC-4.4-02 | ⚠ REQUIRED | §3.8 tackle handling |
| `Ball.ApplyKick()` signature | Ball Physics §3.1.11.2 | ✅ Resolved (ERR-006) | None |
| Event System (#17) stub interface | §4.6.3 | ✅ Defined (stub) | None |
| PhysicalProfile.WINDUP_FRAMES, FOLLOWTHROUGH_FRAMES | §3.1.4 | ✅ Defined | None |

---

## Open Issues

| OI ID | Issue | Severity | Resolution |
|-------|-------|----------|------------|
| OI-3.7-01 | WEAK_FOOT_BASE_PENALTY = 0.30 exceeds [CAREY-2001] 25% max | LOW | Retained as [GT]; calibrate during playtesting (OI-App-C-01) |
| OI-3.8-01 | Collision System must add TackleContactFlag to agent record | MODERATE | Cross-spec action XC-4.4-02; blocks implementation |
| OI-3.8-02 | WINDUP_FRAMES and FOLLOWTHROUGH_FRAMES values are [GT] placeholders | LOW | Calibrate during playtesting for animation feel |
| OI-3.9-01 | PassAttemptEvent does not carry EventVersion field | LOW | Add at Stage 1 before statistics consumers are written (KR-8, §7.8) |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | March 7, 2026, 2:00 PM PST | Claude (AI) / Anton | Initial draft. WeakFoot accuracy and power penalty models. Six-state machine with full transition table. Urgency-driven windup reduction. Two event struct definitions. All formulas derived from Appendix A.6. State machine architecture from §2.2.3 and §4.4.2. Event structs from §4.6.1. |

---

*End of Section 3.7–3.9 — Pass Mechanics Specification #5*

*This completes Section 3 (Technical Specifications) for Pass Mechanics Specification #5.*
