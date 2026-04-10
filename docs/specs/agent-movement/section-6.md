# Agent Movement Specification â€” Section 6: Future Extensions

**Purpose:** Authoritative roadmap for all planned agent movement extensions â€” what changes at each stage, what architectural hooks exist today, what risks each extension introduces, and what is permanently excluded  
**Created:** February 14, 2026, 12:00 PM PST  
**Updated:** February 14, 2026, 1:30 PM PST  
**Version:** 1.1  
**Status:** Draft  
**Dependencies:** Section 1.2 (Out of Scope), Section 3.1 (State Machine), Section 3.2 (Locomotion Model), Section 3.3 (Directional Movement), Section 3.4 (Turning Mechanics), Section 3.5 (Data Structures), Section 4 (Implementation Details), Section 5 (Performance Analysis â€” pending), Ball Physics Spec #1 Section 7 (Future Extensions), Master Vol 1 (Physics Core), Master Vol 2 (Human Systems), Master Development Plan v1.0

---

## CHANGELOG v1.0 â†’ v1.1

**CRITICAL FIXES:**

1. **Added Stage 3â€“4 Extensions (Issue #1):** New section 6.3 covers Stage 3 (Management Depth, Year 5â€“6) and Stage 4 (Human Systems, Year 7â€“8) extensions including form modifier activation, psychology system integration, and injury system hooks. Stage 5 renumbered to 6.4.

2. **Flagged LOD distance thresholds as placeholders (Issue #2):** LOD tier boundaries (20m/40m) now marked with âš ï¸ PLACEHOLDER warning and include derivation rationale based on pitch geometry (penalty area 16.5m, half-pitch 52.5m).

3. **Added Section 5 pending notes (Issue #3):** All cross-references to Section 5 now include "(pending Section 5 completion)" qualifier. Performance budget values flagged as provisional.

**MODERATE FIXES:**

4. **Strengthened Kinetic Profile deferral rationale (Issue #4):** Section 6.2.3 now includes quantitative analysis showing Â±5% effect is below perceptual threshold, with explicit criteria for revisiting in Stage 3.

5. **Added psychology modifier activation (Issue #5):** New section 6.3.2 documents when PsychologyModifier transitions from 1.0 (neutral) to active values, including integration with Master Vol 2 H-Gate system.

6. **Specified BASE_WIND_COST constant (Issue #6):** Section 6.2.2 now defines `BASE_WIND_COST = 0.05` fatigue units per second per m/s headwind, with derivation rationale.

7. **Qualified Section 5 cross-references (Issue #7):** All references to Section 5.3, 5.6 now note "pending Section 5 completion" with placeholder values clearly marked.

**MINOR FIXES:**

8. **Aligned stage/year terminology (Issue #8):** All stage references now use "Stage N (Year Xâ€“Y)" format matching Master Development Plan exactly.

9. **Added goalkeeper exclusion to 6.4 table (Issue #9):** Permanently Excluded Features table now includes "Goalkeeper-specific movement" with cross-reference to Spec #10.

10. **Flagged LOD distribution estimate (Issue #10):** Agent distribution (6/8/8) now marked as âš ï¸ PLACEHOLDER with note that actual distribution depends on match ball position analysis.

---

## 6. FUTURE EXTENSIONS

### Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded agent movement extensions. Future extension references in Sections 1.2, 3.5 (AnimationDataContract), 3.6 (PitchConfiguration), and 4.1 (file structure notes) are summaries derived from the analysis herein. If a conflict exists between those sections and this one, **this section takes precedence**.

**Design philosophy:** Stage 0 agent movement was designed with future extensions in mind. Specific architectural decisions â€” the `PerformanceContext` modifier chain, the `AnimationDataContract` struct with version field, the `PitchConfiguration` extensible design, the `AgentMovementState` enum with reserved values, and the Fixed64 migration path â€” were made to minimize rework when extensions are implemented. This section documents how each extension connects to existing hooks and identifies gaps where new architecture is required.

**Comparison to Ball Physics:** Agent Movement has more extension points than Ball Physics due to:
- 22 agents vs. 1 ball (LOD optimization opportunities)
- Attribute system with modifier pipeline (dribbling, psychology, form)
- Animation integration (visual feedback layer)
- Cross-spec dependencies (Collision, First Touch, Tactical AI)

**Stage/Year Mapping (per Master Development Plan v1.0):**

| Stage | Years | Focus | Agent Movement Extensions |
|-------|-------|-------|---------------------------|
| Stage 0 | Year 1 | Physics Foundation | Core system (this spec) |
| Stage 1 | Year 2 | Tactical Demo | Animation, dribbling |
| Stage 2 | Year 3â€“4 | V1 Release | Surface, weather, altitude |
| Stage 3 | Year 5â€“6 | Management Depth | Form system activation |
| Stage 4 | Year 7â€“8 | Human Systems | Psychology, injury hooks |
| Stage 5 | Year 9â€“10 | Global Simulation | Fixed64, LOD |
| Stage 6 | Year 11+ | Multiplayer | Determinism validation |

---

### 6.1 Stage 1 Extensions (Year 2)

Stage 1 adds visualization, animation, and dribbling mechanics. Agent movement changes are modest â€” the architecture already supports these features through existing data contracts and modifier pipelines.

#### 6.1.1 Animation System Integration

**What it does:** The rendering system consumes `AnimationDataContract` to drive character animations â€” selecting animation clips, blending between states, and applying procedural effects (body lean, stride variation).

**Existing hook:** `AnimationDataContract` (Section 3.5.5) is already populated every frame with:
- `MovementState` â€” selects base animation (idle, walk, jog, sprint)
- `Speed` â€” drives animation playback speed and blend weights
- `FacingDirection` â€” controls character model rotation
- `MovementDirection` â€” selects forward/backward/strafe variants
- `LeanAngle` â€” drives procedural body tilt during turns
- `IsDecelerating` â€” triggers braking animation
- `IsStumbling` â€” triggers stumble recovery animation
- `TimeInState` â€” controls animation timing (e.g., GROUNDED recovery duration)

**What changes:**
- Animation system (Stage 1 Rendering Spec) reads `AnimationDataContract` from each agent
- No changes to any agent movement code

**Integration contract:**
- Animation system must read `AnimationDataContract` after physics update, before render
- `ContractVersion` field (currently 1) enables backward compatibility checking
- Animation system must handle all `AgentMovementState` enum values, including GROUNDED and STUMBLING

**New functionality required in Animation System (not this spec):**
- Animation clip selection based on `MovementState`
- Blend tree driven by `Speed` (walkâ†’jogâ†’sprint transitions)
- Procedural lean angle application from `LeanAngle`
- Stumble and recovery animation sequences
- Strafe and backward run animation variants

**Performance impact:** Zero on agent movement. Animation reads `AnimationDataContract` (already computed), no additional physics computation.

**Risk:** Low. The data contract is defined, versioned, and populated. The animation system is a pure consumer with no feedback into physics.

#### 6.1.2 Dribbling Locomotion Modifiers

**What it does:** When an agent has the ball at their feet, their movement capabilities are reduced â€” slower top speed, reduced agility, decreased acceleration.

**Existing hook:** `PerformanceContext` modifier chain (Section 3.2.1, Section 3.5.6). The `EvaluateAttribute()` method already applies `FormModifier Ã— FatigueModifier Ã— PsychologyModifier`. A new `DribblingModifier` slots into this pipeline.

**Proposed modifier structure:**
```csharp
public struct PerformanceContext
{
    // ... existing modifiers ...
    
    /// <summary>
    /// Dribbling modifier (0.80â€“1.0 range).
    /// Stage 0: Always 1.0 (no ball interaction).
    /// Stage 1+: Set by First Touch System (Spec #11) when ball at feet.
    /// Applied to: Pace, Acceleration, Agility.
    /// </summary>
    public float DribblingModifier;
    
    public float EvaluateAttribute(int rawAttribute)
    {
        return rawAttribute * FormModifier * FatigueModifier 
             * PsychologyModifier * DribblingModifier;  // Add to chain
    }
}
```

**âš ï¸ PLACEHOLDER VALUES â€” NOT SPECIFICATIONS:**

The following values are illustrative estimates only. They must NOT be used as specifications without validation:

| Attribute | Dribbling Multiplier | Source Status |
|-----------|---------------------|---------------|
| Pace (top speed) | Ã—0.85 (15% reduction) | âš ï¸ Placeholder â€” needs GPS tracking validation |
| Acceleration | Ã—0.90 (10% reduction) | âš ï¸ Placeholder â€” needs biomechanics validation |
| Agility (turn rate) | Ã—0.80 (20% reduction) | âš ï¸ Placeholder â€” needs gameplay testing |

**Final values require:** (a) published research comparing dribbling vs. open-run locomotion, or (b) systematic gameplay tuning during Stage 1 implementation with A/B testing.

**What changes:**
- Add `DribblingModifier` field to `PerformanceContext` struct
- Multiply into `EvaluateAttribute()` chain
- First Touch System (Spec #11) sets modifier value based on ball proximity

**Dependencies:**
- First Touch Mechanics (Spec #11) determines when dribbling state is active
- Ball Physics (Spec #1) provides ball position for proximity check
- Collision System (Spec #3) may trigger ball loss, clearing dribbling state

**Performance impact:** One additional multiply per `EvaluateAttribute()` call. At ~10 attribute evaluations per agent per frame Ã— 22 agents = 220 multiplies/frame. Negligible (<0.01ms).

**Risk:** Low for implementation, Medium for gameplay balance. The modifier pipeline exists; the risk is in tuning values that feel realistic without making dribbling frustrating.

#### 6.1.3 Visual Feedback (Lean Angles, Stride Variation)

**What it does:** Exposes additional movement data to the renderer for enhanced visual fidelity â€” exaggerated lean angles during sharp turns, variable stride length based on speed, arm swing synchronization.

**Existing hooks:**
- `AnimationDataContract.LeanAngle` already calculated per Section 3.4
- `AnimationDataContract.Speed` provides stride frequency basis
- `AnimationDataContract.IsDecelerating` triggers braking pose

**What changes:**
- Possibly add `StrideLength` and `StrideFrequency` fields to `AnimationDataContract`
- Possibly add `ArmSwingPhase` for upper body animation sync

**Proposed additions (Stage 1 decision):**
```csharp
// AnimationDataContract extensions (Version 2)
public float StrideLength;      // Meters per stride, derived from Speed
public float StrideFrequency;   // Strides per second
public float ArmSwingPhase;     // 0.0â€“1.0 cycle phase for arm sync
```

**Performance impact:** 3 additional float calculations per agent if added. Negligible.

**Risk:** Low. These are pure output fields with no feedback into physics. Decision to add them can be deferred until animation system requirements are finalized.

#### 6.1.4 Slow-Motion Replay Interpolation

**What it does:** Allows match replay at reduced speed (0.1Ã—â€“1.0Ã—) with smooth agent movement visualization between 60Hz physics frames.

**Existing hooks:**
- `Agent.Position` and `Agent.Velocity` are continuous values suitable for linear interpolation
- `MovementTelemetryEvent` (Section 3.5.7) logs state snapshots for replay reconstruction
- All physics is deterministic for a given input sequence

**What changes:**
- Replay system reconstructs agent trajectories by re-running physics at original `dt` but rendering interpolated frames between physics steps
- No changes to agent movement code

**Architecture decision: Re-simulation vs. Keyframe Interpolation**

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| **Re-simulation** | Bit-perfect accuracy; minimal storage | Requires full input replay; CPU cost during replay (22Ã— Ball Physics) | Preferred for Stage 1 |
| **Keyframe interpolation** | Fast playback; low CPU | Requires dense keyframes (22 agents Ã— 60Hz = 1320 keyframes/sec); interpolation artifacts on sharp turns | Defer to Stage 2 if re-simulation proves too slow |

**New functionality required in Replay System (not this spec):**
- `MovementCommand` recording for each agent each frame
- Deterministic replay engine that feeds recorded commands to agent movement
- Frame interpolation for sub-physics-step rendering
- Replay speed control (0.1Ã—, 0.25Ã—, 0.5Ã—, 1.0Ã—)

**Performance impact:** During replay, agent movement runs at same cost as live simulation. At 0.1Ã— speed, physics tick rate stays at 60Hz but renderer draws more visual frames between ticks â€” this is rendering cost, not physics cost.

**Risk:** Medium. The main risk is replay system's ability to perfectly reproduce all 22 agents' inputs. If any `MovementCommand` is missed or reordered, replay will diverge. Agent movement itself is deterministic given identical inputs (validated by Section 3.7 test cases).

**Open question:** Should `MovementTelemetryEvent` ring buffer size increase for replay? Current 1000 events per agent = ~16.7s history. May need configurable size or event-driven recording. **Recommendation:** Defer ring buffer changes until replay system requirements are finalized in Stage 1 planning.

---

### 6.2 Stage 2 Extensions (Year 3â€“4)

Stage 2 adds environmental complexity â€” surface conditions, weather effects, and stadium-specific modifiers. These extensions modify how agent movement receives input but do not change core locomotion formulas.

#### 6.2.1 Surface Traction Modifiers

**What it does:** Per-tile pitch surface conditions affect acceleration, deceleration, and turning â€” wet grass reduces traction, worn areas are slippery, artificial turf has different grip characteristics.

**Existing hooks:**
- `PerformanceContext` modifier chain can accept a `SurfaceTractionModifier`
- `PitchConfiguration` (Section 3.6.5) is designed for extensibility
- Ball Physics `SurfaceType` enum (Spec #1, Section 3.1.2) defines surface categories

**What changes:**
1. Add `SurfaceTractionModifier` field to `PerformanceContext`
2. Query current tile's `SurfaceType` based on agent position
3. Apply traction modifier to acceleration, deceleration, and turn rate

**Proposed modifier mapping:**

| SurfaceType | Traction Modifier | Effect |
|-------------|-------------------|--------|
| GRASS_DRY | 1.00 | Baseline |
| GRASS_WET | 0.85 | 15% reduced grip |
| GRASS_FROZEN | 0.70 | 30% reduced grip |
| ARTIFICIAL_DRY | 1.05 | 5% improved grip |
| ARTIFICIAL_WET | 0.90 | 10% reduced grip |

**âš ï¸ Values are estimates** â€” require gameplay testing for balance.

**Integration with Ball Physics:**
- Agent Movement and Ball Physics must use consistent `SurfaceType` values
- Both systems query the same `PitchConditionSystem` (external dependency)
- Tile boundaries must be handled consistently â€” agent position determines which tile's properties apply

**What changes in code:**
```csharp
// In locomotion calculation (Section 3.2):
float baseTraction = 1.0f;  // Stage 0
float surfaceTraction = PitchConditionSystem.GetTractionAt(agent.Position);  // Stage 2
float effectiveAccel = baseAccel * surfaceTraction;
```

**Performance impact:** One positionâ†’tile lookup per agent per frame. With spatial grid, this is O(1). Adds ~0.001ms per agent.

**Risk:** Medium. Integration with Ball Physics' surface system requires coordination:
- Both specs must use identical `SurfaceType` enum values
- Tile boundary handling must match (Ball Physics Section 7.2.1 notes potential discontinuity)
- `PitchConditionSystem` becomes a shared dependency

#### 6.2.2 Weather Effects on Movement

**What it does:** Weather conditions affect agent locomotion â€” wet pitch reduces traction (overlaps with 6.2.1), headwind increases sprinting energy cost, rain affects visibility (Perception System, not this spec).

**Existing hooks:**
- `PerformanceContext` for traction effects
- `FatigueModifier` update (10Hz heartbeat) can include weather energy cost
- `PitchConfiguration` can store weather state

**What changes:**

**Wind resistance (SPRINTING state only):**
```csharp
// Constants
const float BASE_WIND_COST = 0.05f;  // Fatigue units per second per m/s headwind

// Only affects agents sprinting into headwind
if (state == SPRINTING && Vector3.Dot(velocity, windDirection) < 0)
{
    // Headwind increases energy expenditure
    float headwindFactor = -Vector3.Dot(velocity.normalized, windDirection.normalized);
    float windEnergyCost = BASE_WIND_COST * headwindFactor * windSpeed;
    fatigueAccumulator += windEnergyCost * dt;
}
```

**BASE_WIND_COST derivation:**
- At 10 m/s headwind (strong wind), sprinting agent accumulates 0.5 extra fatigue units/second
- Normal fatigue accumulation while sprinting â‰ˆ 1.0 units/second (per Fatigue System, Spec #13)
- Thus strong headwind adds ~50% fatigue overhead â€” significant but not dominant
- Value is tunable; 0.05 is conservative starting point

**Why wind resistance is small for players:**
- Ball Physics wind effect is significant because ball has high drag coefficient (Cd â‰ˆ 0.47) and low mass (0.43 kg)
- Player has low drag coefficient (Cd â‰ˆ 1.0â€“1.2 for human body) but high mass (70â€“90 kg)
- At 10 m/s wind, ball experiences ~0.5 N drag force, player experiences ~50 N â€” but player mass is 150Ã— higher
- Net effect: wind is second-order for players, first-order for ball

**Performance impact:** One dot product and conditional per sprinting agent per frame. Most agents aren't sprinting most frames. Negligible.

**Risk:** Low. Wind is a minor effect on players. The main risk is over-engineering â€” recommendation is to implement only the fatigue cost component, not a full aerodynamic model.

#### 6.2.3 Kinetic Profile System

**What it does:** Per-player running style (heel-striker, forefoot runner, neutral) from Master Vol 1 Â§2.2. Different running styles have slightly different acceleration curves and injury risk profiles.

**Existing hooks:**
- `PlayerAttributes` can add a `KineticProfile` enum
- Acceleration curve parameters (Section 3.2) can vary by profile

**Proposed profiles:**

| Profile | Acceleration Bonus | Top Speed Bonus | Injury Risk | Notes |
|---------|-------------------|-----------------|-------------|-------|
| HEEL_STRIKER | +5% | -2% | Higher | Common in defenders |
| FOREFOOT | -3% | +3% | Lower | Common in wingers |
| NEUTRAL | 0% | 0% | Baseline | Most players |

**Quantitative impact analysis:**

The Â±5% effect on acceleration translates to:
- Baseline acceleration: ~4.0 m/sÂ² (typical player)
- HEEL_STRIKER: 4.2 m/sÂ² (+0.2 m/sÂ²)
- FOREFOOT: 3.88 m/sÂ² (-0.12 m/sÂ²)

Time to reach 7 m/s from standing:
- Baseline: ~1.75s
- HEEL_STRIKER: ~1.67s (0.08s faster)
- FOREFOOT: ~1.80s (0.05s slower)

**Perceptual threshold:** Human perception of timing differences in sports is ~50â€“100ms. The 80ms difference for HEEL_STRIKER is at the edge of perceptibility. The Â±3% top speed effect (Â±0.24 m/s on 8 m/s top speed) is below perceptual threshold in most game situations.

**Recommendation: DEFER TO STAGE 3.**

Rationale:
1. Effect magnitude (Â±5%) is at or below perceptual threshold
2. Implementation effort (new attribute, formula modification) exceeds gameplay benefit
3. Injury system (Stage 2+) is prerequisite for injury risk differentiation
4. Resources better spent on higher-impact Stage 2 features

**Revisit criteria for Stage 3:**
- If injury system implementation reveals kinetic profile as natural integration point
- If sports science data emerges with larger validated effect sizes
- If gameplay testing shows need for player differentiation beyond attributes

#### 6.2.4 Altitude Effects

**What it does:** High-altitude stadiums (e.g., Mexico City at 2,240m, La Paz at 3,640m) reduce stamina recovery rate due to lower oxygen partial pressure.

**Existing hooks:**
- `FatigueModifier` update runs at 10Hz heartbeat â€” easy to add multiplier
- `PitchConfiguration` can include altitude parameter
- `MatchConfiguration` (future spec) will include venue data

**What changes:**
```csharp
// In fatigue system (10Hz update):
float altitudeMeters = matchConfig.Venue.Altitude;
float altitudeModifier = 1.0f - (altitudeMeters / 10000f);  // Linear falloff
staminaRegenRate *= Mathf.Clamp(altitudeModifier, 0.7f, 1.0f);
```

At 2,000m: 80% stamina regen rate
At 3,500m: 65% stamina regen rate (clamped to 70% minimum)

**Performance impact:** One multiply per 10Hz update = 2.2 multiplies per frame (22 agents / 10). Negligible.

**Risk:** Low. Simple multiplier on existing system. The only decision is the altitude curve shape â€” linear is a reasonable starting point; can refine with sports science data.

---

### 6.3 Stage 3â€“4 Extensions (Year 5â€“8)

Stage 3 (Management Depth) and Stage 4 (Human Systems) activate the modifier systems that were stubbed in Stage 0. These extensions do not add new physics formulas â€” they populate the `PerformanceContext` modifiers that have been returning 1.0 (neutral) since Stage 0.

#### 6.3.1 Form Modifier Activation (Stage 3)

**What it does:** The `FormModifier` field in `PerformanceContext` transitions from always-1.0 to dynamically computed values based on recent player performance.

**Existing hook:** `PerformanceContext.FormModifier` (Section 3.5.6) is already multiplied into `EvaluateAttribute()`. In Stage 0â€“2, this value is always 1.0.

**Stage 3 activation:**
```csharp
// FormSystem calculates form based on recent matches
float recentForm = FormSystem.CalculateForm(player.RecentMatches);

// Map to modifier range (0.85â€“1.10)
// Good form (8.0+ rating) â†’ up to 1.10
// Average form (6.0â€“7.0) â†’ 1.00
// Poor form (<5.5) â†’ down to 0.85
agent.PerformanceContext.FormModifier = MapFormToModifier(recentForm);
```

**Modifier range:** 0.85â€“1.10 (per Section 3.5.6 documentation)
- This means a player in poor form has 15% reduced effective attributes
- A player in excellent form has 10% boosted effective attributes
- Asymmetric range reflects that poor form is more impactful than good form

**Update frequency:** Form is recalculated between matches, not during matches. The `FormModifier` value is set at match initialization and remains constant throughout the match.

**What changes in Agent Movement:**
- Nothing in Stage 3 â€” the hook already exists
- `FormSystem` (Master Vol 2 implementation) writes to `FormModifier`
- Agent Movement reads via existing `EvaluateAttribute()` calls

**Performance impact:** Zero additional cost in Agent Movement. Form calculation happens pre-match.

**Risk:** Low for Agent Movement. Risk is in FormSystem's calculation algorithm (not this spec).

#### 6.3.2 Psychology Modifier Activation (Stage 4)

**What it does:** The `PsychologyModifier` field in `PerformanceContext` transitions from always-1.0 to dynamically computed values based on match context, implementing Master Vol 2's H-Gate happiness system.

**Existing hook:** `PerformanceContext.PsychologyModifier` (Section 3.5.6) is already multiplied into `EvaluateAttribute()`. In Stage 0â€“3, this value is always 1.0.

**Stage 4 activation:**

The H-Gate system (Master Vol 2, Section 3) models player psychology through two axes:
- **Confidence:** Belief in ability to succeed (affected by recent performance)
- **Self-Efficacy:** Belief in control over outcomes (affected by team dynamics)

```csharp
// PsychologySystem calculates modifier from H-Gate state
HGateState hgate = PsychologySystem.GetHGateState(player);

// Map to modifier range (0.85â€“1.05)
// High confidence + high efficacy â†’ up to 1.05
// Low confidence OR low efficacy â†’ down to 0.85
// Neutral state â†’ 1.00
agent.PerformanceContext.PsychologyModifier = hgate.ToMovementModifier();
```

**Modifier range:** 0.85â€“1.05 (narrower than Form)
- Psychology effects on raw movement are more subtle than form effects
- Primary psychology impact is on decision-making (Spec #7), not locomotion
- Movement modifier captures "playing scared" or "playing with swagger"

**Update frequency:** Psychology can change during a match:
- Goal scored: Confidence boost for scorer, team
- Goal conceded: Confidence drop for defenders, goalkeeper
- Yellow card: Efficacy drop (less control)
- Crowd reaction: Various effects based on home/away

Updates occur at 10Hz tactical heartbeat, same as fatigue.

**What changes in Agent Movement:**
- Nothing in Stage 4 â€” the hook already exists
- `PsychologySystem` (Master Vol 2 implementation) writes to `PsychologyModifier`
- Agent Movement reads via existing `EvaluateAttribute()` calls

**Integration with AnimationDataContract:**
- Stage 4 may add `PsychologicalState` enum to AnimationDataContract
- Enables visual feedback (confident body language vs. nervous posture)
- This is an animation system decision, not Agent Movement

**Performance impact:** Zero additional cost in Agent Movement. Psychology calculations happen at 10Hz in separate system.

**Risk:** Medium. The H-Gate system is complex (Master Vol 2), and its interaction with movement could create unexpected emergent behavior. **Recommendation:** Extensive playtesting during Stage 4 to validate that psychology effects feel natural and don't create frustrating gameplay loops.

#### 6.3.3 Injury System Hooks (Stage 4)

**What it does:** Provides data hooks for the Injury System to affect movement and receive movement data for injury probability calculations.

**Existing hooks:**
- `AnimationDataContract.InjuryLevel` field (Stage 1+ extension, Section 3.5.5) â€” currently always 0.0
- `Agent.CurrentState` exposes STUMBLING and GROUNDED states
- `StumbleConstants` (Section 3.4) provide stumble probability data

**Stage 4 integration points:**

**1. Movement â†’ Injury System (injury triggers):**
```csharp
// Injury System can query:
- agent.CurrentState == GROUNDED  // Knockdown occurred
- agent.GroundedReason            // COLLISION, SLIDING_TACKLE, etc.
- agent.TimeInState               // Duration on ground
- stumbleEvent.TurnRateAttempted  // Overly aggressive turn
- stumbleEvent.SpeedAtMoment      // Speed when stumble occurred
```

**2. Injury System â†’ Movement (injury effects):**
```csharp
// Injury System sets:
agent.AnimationDataContract.InjuryLevel = severity;  // 0.0â€“1.0

// Movement System reads (if implemented):
if (agent.InjuryLevel > 0.5f)
{
    // Severe injury: reduce max speed, acceleration
    effectiveSpeed *= (1.0f - agent.InjuryLevel * 0.3f);
}
```

**What changes in Agent Movement:**
- Stage 4 decision: Should `InjuryLevel` affect physics, or only animation?
- If physics: Add injury multiplier to PerformanceContext
- If animation-only: No Agent Movement changes

**Recommendation:** Animation-only for v1. Full physics integration for v2 (Stage 5).

Rationale:
1. Movement physics is already complex; adding another modifier increases testing burden
2. Visual limping (animation) provides player feedback without gameplay impact
3. Injury impact on attributes (overall rating drop) already handled by attribute system

**Risk:** Low if animation-only. Medium if physics integration chosen.

---

### 6.4 Stage 5 Extensions (Year 9â€“10)

Stage 5 adds multiplayer determinism through Fixed64 migration and performance optimizations through LOD systems. This is the "Global Simulation" release implementing Master Vol 1's full scope.

#### 6.4.1 Fixed64 Migration for Multiplayer Determinism

**What it does:** Replaces all `float` operations with `Fixed64` (64-bit fixed-point) to achieve bit-identical physics results across all platforms â€” required for lockstep multiplayer.

**Existing hook:** Same migration path as Ball Physics (Spec #1, Section 7.4.1). The design philosophy of using static methods and value-type structs enables type substitution without architectural changes.

**Scope comparison:**

| Metric | Ball Physics | Agent Movement | Ratio |
|--------|--------------|----------------|-------|
| Source files | 3 | 7+ | 2.3Ã— |
| Formulas | ~20 | ~50 | 2.5Ã— |
| Vector operations | ~15/frame | ~40/agent/frame | 58Ã— (with 22 agents) |
| sqrt() calls | 2/frame | ~4/agent/frame | 44Ã— |

Agent Movement is significantly larger than Ball Physics â€” more files, more formulas, more intermediate calculations, and 22Ã— the entity count.

**Migration approach:**
1. Create `AgentMovementMath.cs` abstraction layer (mirrors Ball Physics approach)
2. Replace `Vector3` with `Vector3Fixed` throughout
3. Replace `Mathf` calls with `AgentMovementMath` equivalents
4. Implement Fixed64 backend behind abstraction layer
5. Conditional compilation switches between float and Fixed64 backends

**Performance impact estimate:**

| Operation | Float Cost | Fixed64 Cost | Multiplier |
|-----------|-----------|--------------|------------|
| Add/Sub/Mul | 1 cycle | 1â€“2 cycles | 1â€“2Ã— |
| Division | 5â€“10 cycles | 20â€“40 cycles | 4Ã— |
| sqrt() | 10â€“20 cycles | 50â€“100 cycles | 5Ã— |

**Per-agent estimate:**
- Current: ~0.09ms per agent âš ï¸ (pending Section 5 completion â€” this is a placeholder)
- Fixed64: ~0.25â€“0.35ms per agent (2.8â€“3.9Ã— increase)

**22-agent total:**
- Current: ~2.0ms for all agents âš ï¸ (pending Section 5 completion)
- Fixed64: ~5.5â€“7.7ms for all agents

**âš ï¸ EXCEEDS 3.0ms BUDGET** â€” Fixed64 migration alone would blow the frame budget (pending Section 5 confirmation of actual budget).

**Mitigation strategies (must implement before Fixed64):**

| Strategy | Estimated Savings | When |
|----------|-------------------|------|
| SIMD batching (P2) | 30â€“40% | Before Fixed64 |
| SoA layout (P2) | 10â€“20% | Before Fixed64 |
| LOD system (6.4.2) | 30â€“40% | Before or with Fixed64 |
| Combined | 50â€“70% | â€” |

With P2 optimizations + LOD: Fixed64 estimate drops to 1.6â€“3.0ms â€” within budget.

**Risk:** HIGH. This is the highest-risk extension for Agent Movement. Unlike Ball Physics (single entity, simple formulas), Agent Movement processes 22 entities with complex interdependencies. **Action:** Revisit during Stage 5 planning with actual profiling data from Stages 1â€“4.

**Open questions:**
- Should Fixed64 migration happen incrementally (file by file) or atomically (all at once)?
- Can LOD system (6.4.2) use float for LOD 1/2 agents while LOD 0 uses Fixed64? (Determinism implications unclear)
- Which Fixed64 library to use? Must benchmark sqrt() accuracy (<4 ULP error required per Ball Physics KR-3)

#### 6.4.2 LOD (Level of Detail) Movement

**What it does:** Agents far from the ball receive simplified physics updates, reducing total computation cost while maintaining visual and gameplay fidelity for the active play area.

**Design: Three LOD Tiers**

| Tier | Distance from Ball | Update Rate | Calculations Skipped | Determinism |
|------|-------------------|-------------|---------------------|-------------|
| LOD 0 | <20m | 60Hz | None | Full |
| LOD 1 | 20â€“40m | 60Hz | Turning, stumble checks | Full |
| LOD 2 | >40m | 30Hz | Above + simplified locomotion | Full |

**âš ï¸ PLACEHOLDER THRESHOLDS:**

The 20m and 40m boundaries are provisional estimates based on pitch geometry:
- Penalty area depth: 16.5m â€” LOD 0 should cover active penalty area play
- Half-pitch: 52.5m â€” agents beyond half-pitch during opponent's attack are rarely involved
- 20m threshold ensures ~6 agents in LOD 0 during typical play (ball + nearby players)
- 40m threshold is midpoint between penalty area and half-pitch

**These thresholds require validation** through:
1. Match simulation analysis of ball position distributions
2. Gameplay testing for visual discontinuities at tier boundaries
3. Performance profiling to confirm savings estimates

**Tier assignment logic:**
```csharp
float distanceToBall = Vector3.Distance(agent.Position, ball.Position);

// Hysteresis to prevent oscillation at boundaries
const float HYSTERESIS = 2.0f;

if (agent.LODTier == 0 && distanceToBall > 20f + HYSTERESIS)
    agent.LODTier = 1;
else if (agent.LODTier == 1 && distanceToBall < 20f - HYSTERESIS)
    agent.LODTier = 0;
else if (agent.LODTier == 1 && distanceToBall > 40f + HYSTERESIS)
    agent.LODTier = 2;
else if (agent.LODTier == 2 && distanceToBall < 40f - HYSTERESIS)
    agent.LODTier = 1;
```

**LOD 1 simplifications:**
- Skip `AgentTurning.Calculate()` â€” use instant facing toward target
- Skip stumble probability checks â€” agents can't stumble when far from action
- Skip `AgentSafety.ValidateBalanceRisk()` â€” only validate position/velocity

**LOD 2 simplifications (in addition to LOD 1):**
- Update at 30Hz (every other frame)
- Use linear interpolation toward target position instead of full locomotion
- Skip state machine transitions â€” hold current state

**Performance benefit estimate:**

âš ï¸ **PLACEHOLDER DISTRIBUTION** â€” actual agent distribution depends on match ball position analysis:

| LOD Distribution | Agents | Per-Agent Cost | Total |
|------------------|--------|----------------|-------|
| LOD 0 (full) | 6 | 0.09ms | 0.54ms |
| LOD 1 (partial) | 8 | 0.05ms | 0.40ms |
| LOD 2 (minimal) | 8 | 0.02ms | 0.16ms |
| **Total** | 22 | â€” | **1.10ms** |

Compared to 2.0ms without LOD = **45% reduction**.

**Note:** The 6/8/8 distribution assumes ball is typically in one half with most players nearby. Actual distribution varies significantly based on:
- Match state (attacking vs. defending)
- Formation (compact vs. spread)
- Ball position (central vs. wide)

Validation requires simulating 100+ matches and measuring actual tier distributions.

**Determinism guarantee:**
- LOD tier assignment is deterministic (based on ball position, which is deterministic)
- LOD 1 and LOD 2 calculations must produce identical results on all platforms
- Replay system must record LOD tier assignments or re-derive them

**Goalkeeper exception:**
- Goalkeepers have `AlwaysLOD0 = true` flag regardless of ball distance
- Rationale: Goalkeeper positioning is tactically critical even when ball is distant
- Adds 0â€“2 agents to LOD 0 count (depending on match state)

**Gameplay considerations:**
- Players watching the match see all 22 agents animated smoothly
- LOD affects physics precision, not visual quality
- Off-ball positioning AI runs at full fidelity (AI decisions are separate from physics)

**Risk:** Medium. Main risks:
1. LOD boundaries create visible behavior discontinuities when agents cross thresholds
2. Determinism for LOD 2's 30Hz update requires careful frame counting
3. Agent distribution estimates may be significantly wrong

**Recommendation:** Implement LOD 0/1 in Stage 3, defer LOD 2 (30Hz) until Stage 4 after determinism implications are analyzed.

---

### 6.5 Permanently Excluded Features

The following features are **permanently excluded** from the Agent Movement specification. This list takes precedence over any conflicting statements in other sections.

| Feature | Reason | Alternative |
|---------|--------|-------------|
| Individual muscle simulation | Performance prohibitive (1000+ DOF per agent), no gameplay value | Aggregate attribute system captures outcomes |
| Skeletal physics (ragdoll) for normal movement | Reserved for post-collision/injury states only | Animation system handles normal movement |
| Clothing/kit aerodynamic drag | Negligible at player speeds (<0.01% effect on drag) | None needed |
| Shoe stud grip modeling | Abstracted into surface friction | SurfaceTractionModifier (Stage 2) |
| Ground deformation from footsteps | Ball Physics handles pitch degradation; player footsteps excluded for performance | PitchConditionSystem tracks degradation zones |
| Per-step ground reaction forces | Too granular, no gameplay value | Acceleration curves capture aggregate effect |
| Biomechanical injury modeling | Deferred to Injury System (Stage 4+, Spec TBD) | Section 1.2 documents this exclusion |
| Fatigue visualization (sweat, breathing) | Rendering concern, not physics | Animation System (Stage 1) handles visual cues |
| Age-related physical decline | Career mode feature (Stage 4+), not match physics | Attribute system can decay over seasons |
| Real-time motion capture input | Out of scope for simulation game | Pre-recorded animation data |
| Goalkeeper-specific movement | Different movement model (diving, shuffling) | Spec #10: Goalkeeper Mechanics |

**Rationale for exclusions:**

These features share common disqualifying characteristics:
1. **Performance prohibitive:** Would exceed frame budget without proportional gameplay benefit
2. **No gameplay differentiation:** Would not create observable tactical differences
3. **Out of scope:** Belong to other systems (Animation, Injury, Goalkeeper, Career Mode)
4. **Diminishing returns:** Complexity far exceeds perceptual benefit

---

### 6.6 Extensibility Design Analysis

#### 6.6.1 Prepared Extension Points

The following hooks are already present in Stage 0 code, requiring only configuration or parameter changes to enable extensions:

| Extension Point | Location | Mechanism | Extensions Served |
|-----------------|----------|-----------|-------------------|
| PerformanceContext modifier chain | Section 3.2.1, 3.5.6 | Add new modifier fields | 6.1.2 (Dribbling), 6.2.1 (Surface), 6.2.2 (Weather), 6.3.1 (Form), 6.3.2 (Psychology) |
| AnimationDataContract | Section 3.5.5 | Add new fields, increment ContractVersion | 6.1.1 (Animation), 6.1.3 (Visual Feedback), 6.3.3 (Injury) |
| AnimationDataContract.ContractVersion | Section 3.5.5 | Version check for backward compatibility | All animation extensions |
| PitchConfiguration | Section 3.6.5 | Extensible struct with clamped ranges | 6.2.1 (Surface tiles), 6.2.4 (Altitude) |
| MovementCommand | Section 3.5.2 | Add new fields/modes | Dribbling commands, formation commands |
| FacingMode enum | Section 3.3 | Add new modes | Ball tracking, player marking |
| AgentMovementState enum | Section 3.1 | Add new states | JUMPING (headers), SLIDING (tackles) |
| MovementTelemetryEvent | Section 3.5.7 | Add new event types | 6.1.4 (Replay), debugging |
| FatigueModifier 10Hz update | Section 3.2 | Add multipliers to existing pipeline | 6.2.2 (Weather), 6.2.4 (Altitude) |
| FormModifier field | Section 3.5.6 | Set non-1.0 value | 6.3.1 (Form activation) |
| PsychologyModifier field | Section 3.5.6 | Set non-1.0 value | 6.3.2 (Psychology activation) |
| Static method design | All Section 3.x | No instance state, enables type substitution | 6.4.1 (Fixed64) |

#### 6.6.2 Gaps Requiring New Architecture

The following extensions require architectural changes beyond parameter/field additions:

| Gap | Required For | Estimated Work | When to Address |
|-----|--------------|----------------|-----------------|
| Dribbling state detection | 6.1.2 | Medium â€” integration with Spec #11 (First Touch) | Stage 1 planning |
| SurfaceTractionModifier | 6.2.1 | Small â€” new modifier in PerformanceContext, tile query | Stage 2 start |
| PitchConditionSystem integration | 6.2.1, 6.2.2 | Medium â€” shared dependency with Ball Physics | Stage 2 start |
| FormSystem integration | 6.3.1 | Small â€” FormSystem writes to existing field | Stage 3 start |
| PsychologySystem integration | 6.3.2 | Medium â€” H-Gate state to modifier mapping | Stage 4 start |
| InjuryLevel physics integration | 6.3.3 | Small â€” optional additional modifier | Stage 4 (if chosen) |
| AgentMovementMath abstraction | 6.4.1 | Large â€” wrapper for all math operations | Stage 5 start |
| Vector3Fixed type | 6.4.1 | Medium â€” custom struct replacing Unity Vector3 | Stage 5 with Fixed64 library |
| LOD tier assignment | 6.4.2 | Medium â€” distance-based tier logic, per-agent flag | Stage 3 |
| LOD-aware update loop | 6.4.2 | Medium â€” conditional execution paths by tier | Stage 3 |
| 30Hz update scheduling | 6.4.2 (LOD 2) | Small â€” frame counter, even/odd dispatch | Stage 4 |
| Goalkeeper AlwaysLOD0 flag | 6.4.2 | Small â€” boolean flag check | Stage 3 |

#### 6.6.3 Cross-Spec Dependencies for Extensions

| Extension | Depends On | Dependency Type |
|-----------|-----------|-----------------|
| 6.1.2 Dribbling | First Touch (Spec #11) | State trigger |
| 6.2.1 Surface Traction | Ball Physics (Spec #1) | Shared SurfaceType enum |
| 6.2.1 Surface Traction | Collision System (Spec #3) | Spatial grid for tile lookup |
| 6.2.2 Weather | Ball Physics (Spec #1) | Shared weather state |
| 6.3.1 Form | Master Vol 2 FormSystem | Modifier source |
| 6.3.2 Psychology | Master Vol 2 H-Gate System | Modifier source |
| 6.3.3 Injury | Injury System (Spec TBD) | Bidirectional data |
| 6.4.1 Fixed64 | Fixed64 Math Library (Spec #8) | Type definitions |
| 6.4.2 LOD | Ball Physics (Spec #1) | Ball position for distance calc |

---

### 6.7 Backwards Compatibility Guarantees

When future extensions are implemented, the following invariants **must** be preserved:

1. **All Section 3.7 unit tests continue to pass** with default parameters (no dribbling, dry grass, sea level, no wind, all modifiers = 1.0). Extensions add new test cases but must not break existing ones.

2. **O(1) per-agent complexity is maintained.** No extension may introduce loops over other agents, spatial queries, or variable-length iterations in the per-agent update path. (Exception: LOD tier assignment may query ball position â€” this is O(1) per agent.)

3. **Zero-allocation policy is maintained.** No extension may introduce heap allocations in the physics update loop. New structs (`SurfaceTractionModifier`, `Vector3Fixed`) must be value types.

4. **Per-agent budget ceiling is maintained.** With default parameters, per-agent update must remain under 0.14ms (p99). âš ï¸ This value is pending Section 5 completion. Fixed64 migration may increase this ceiling â€” the new ceiling must be documented and approved before implementation.

5. **State machine transition logic produces identical results for identical inputs.** State thresholds, hysteresis values, and transition conditions must not change based on active extensions. Extensions may add new states but must not alter existing state logic.

6. **AnimationDataContract remains backward compatible.** New fields must be added at end of struct. `ContractVersion` must increment. Animation systems must handle older contract versions gracefully.

7. **PerformanceContext.EvaluateAttribute() returns identical values for default modifiers.** When all modifiers are 1.0, output must equal input. New modifiers must default to 1.0.

8. **Form and Psychology modifiers are invisible when inactive.** Stage 0â€“2 tests must pass without FormSystem or PsychologySystem present. Modifier fields default to 1.0 and require no external dependency to function.

---

### 6.8 Cross-References

| Topic | Section | Summary |
|-------|---------|---------|
| Permanent exclusions list | Section 1.2 | Subset of 6.5 (this section is authoritative) |
| PerformanceContext definition | Section 3.5.6 | Modifier struct with EvaluateAttribute() |
| AnimationDataContract definition | Section 3.5.5 | Data contract for rendering |
| PitchConfiguration definition | Section 3.6.5 | Pitch dimensions and surface properties |
| State machine design | Section 3.1 | AgentMovementState enum |
| Locomotion model | Section 3.2 | Acceleration curves, speed calculations |
| Turning mechanics | Section 3.4 | Turn rate, stumble system |
| File structure | Section 4.1 | PitchConfiguration migration note |
| Performance budgets | Section 5.3 | Per-agent timing targets âš ï¸ (pending Section 5 completion) |
| P2 optimizations | Section 5.6 | SIMD, SoA, LOD âš ï¸ (pending Section 5 completion) |
| Ball Physics Fixed64 migration | Spec #1, Section 7.4.1 | Parallel migration path |
| Ball Physics surface extensions | Spec #1, Section 7.2.1 | Shared SurfaceType system |
| First Touch Mechanics | Spec #11 | Dribbling state trigger |
| Goalkeeper Mechanics | Spec #10 | Goalkeeper-specific movement (excluded from this spec) |
| Fixed64 Math Library | Spec #8 | Type definitions for migration |
| Master Development Plan | Section 1 | Stage 0â†’1â†’2â†’...â†’6 dependency chain |
| Master Vol 1, Section 2.2 | â€” | Kinetic profiles (heel-striker, forefoot, neutral) |
| Master Vol 1, Section 4.1 | â€” | Pitch degradation model |
| Master Vol 1, Section 4.2 | â€” | Weather system (wind, rain, temperature) |
| Master Vol 2, Section 3 | â€” | H-Gate happiness system |
| Master Vol 2, FormSystem | â€” | Form calculation for 6.3.1 |

---

### 6.9 Known Risks and Open Questions

The following items require decisions during future stage planning. They are documented here so they are not forgotten.

**KR-1: Dribbling modifier value validation.**
The placeholder values in 6.1.2 (Ã—0.85 pace, Ã—0.80 agility, Ã—0.90 acceleration) are unsourced estimates. If used without validation, they may create unrealistic gameplay. **Recommendation:** Stage 1 planning must include either (a) literature review for GPS tracking studies comparing dribbling vs. open-run speeds, or (b) gameplay testing protocol with A/B comparison. Do not ship dribbling modifiers without validation.

**KR-2: SurfaceType enum synchronization.**
Agent Movement and Ball Physics both depend on `SurfaceType` enum (6.2.1). If either spec adds enum values independently, deserialization will break. **Recommendation:** Before Stage 2, establish `SurfaceType` as a shared definition in `TacticalDirector.Core.Physics.Common` namespace. Both specs import from shared location. Same mitigation as Ball Physics KR-6.

**KR-3: Fixed64 budget overage.**
Section 6.4.1 estimates Fixed64 migration will exceed the 3.0ms frame budget (5.5â€“7.7ms for 22 agents). This is the highest-risk extension. **Recommendation:** P2 optimizations (SIMD, SoA) and LOD system (6.4.2) must be implemented and validated before Fixed64 migration begins. Fixed64 migration should not start until combined optimizations demonstrate <3.0ms is achievable.

**KR-4: LOD tier boundary discontinuities.**
When an agent crosses the 20m or 40m threshold from ball, physics fidelity changes abruptly. This could cause visible "pops" in agent behavior. **Recommendation:** Hysteresis zones (e.g., LOD 0â†’1 at 22m, LOD 1â†’0 at 18m) are included in 6.4.2 design. Monitor during Stage 3 playtesting for effectiveness.

**KR-5: LOD 2 determinism with 30Hz update.**
LOD 2 agents update at 30Hz while LOD 0/1 agents update at 60Hz. Frame counting must be deterministic â€” if agent A is LOD 2 on frame N, it must be LOD 2 on frame N in replay. **Recommendation:** LOD tier assignment must be recorded in replay data OR derived deterministically from ball position (which is recorded). Verify determinism before shipping LOD 2.

**KR-6: PitchConfiguration ownership migration.**
Section 3.6.5 defines `PitchConfiguration` in Agent Movement namespace. Section 4.1.1 notes it should migrate to shared namespace for Collision System. If migration happens mid-Stage-1, it breaks in-progress work. **Recommendation:** Execute migration as first task of Collision System implementation (Spec #3), before any collision code references pitch dimensions.

**KR-7: Goalkeeper LOD exception.**
Should goalkeepers always be LOD 0 regardless of ball distance? A goalkeeper 50m from ball during opponent's attack is still tactically critical (positioning, communication). **Recommendation:** Add `AlwaysLOD0` flag to goalkeeper agents. This is now documented in 6.4.2.

**KR-8: Animation system feedback loop.**
Section 6.1.1 states animation is a "pure consumer" with no feedback into physics. But what if animation timing affects gameplay perception (e.g., player expects tackle to land based on animation)? **Recommendation:** Stage 1 animation implementation must document any cases where animation timing diverges from physics timing. Animation system must never alter physics state â€” only consume it.

**KR-9: Form/Psychology modifier interaction.**
In Stage 4, both FormModifier and PsychologyModifier will be active simultaneously. Their multiplicative combination could produce extreme values:
- Best case: 1.10 Ã— 1.05 = 1.155 (+15.5% effective attributes)
- Worst case: 0.85 Ã— 0.85 = 0.7225 (-27.75% effective attributes)

The worst case may feel punishing. **Recommendation:** Consider clamping total modifier product to [0.75, 1.15] range, or using additive rather than multiplicative combination. Decide during Stage 4 planning.

**KR-10: Section 5 dependency.**
Multiple values in this section reference Section 5 (Performance Analysis) which is not yet written. Specific pending items:
- Per-agent budget ceiling (0.14ms p99) â€” placeholder
- Total frame budget (3.0ms) â€” placeholder
- Operation counts for Fixed64 estimates â€” derived from Section 5

**Recommendation:** After Section 5 completion, review Section 6 for consistency and update any conflicting values.

---

**END OF SECTION 6**

---

## Document Status

**Section 6 Completion:**
- âœ… Stage 1 extensions documented with existing hooks identified (6.1)
- âœ… Stage 2 extensions documented with integration requirements (6.2)
- âœ… Stage 3â€“4 extensions documented with modifier activation details (6.3) â€” **NEW in v1.1**
- âœ… Stage 5 extensions documented with performance estimates and risks (6.4)
- âœ… Permanent exclusions consolidated with rationale (6.5)
- âœ… Extensibility design analysis â€” hooks, gaps, dependencies (6.6)
- âœ… Backwards compatibility guarantees (6.7)
- âœ… Cross-references to all related sections (6.8)
- âœ… Known risks and open questions documented (6.9)

**Page Count:** ~15 pages

**Issues Resolved:** 10/10 from v1.0 self-critique

**Ready for:** Review and critique

**Next Sections:** Section 7 (References), Appendices, Section 9 (Approval Checklist)
