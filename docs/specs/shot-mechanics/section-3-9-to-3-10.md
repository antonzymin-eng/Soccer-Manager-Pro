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
