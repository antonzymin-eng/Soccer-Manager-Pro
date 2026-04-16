# Agent Movement Specification â€” Section 3.5: Data Structures

**Purpose:** Defines all classes, structs, and enums used by the Agent Movement System. This is the authoritative reference for data contracts between the movement system and external systems (Collision System, Perception, Animation, Tactical AI). Every public-facing data structure referenced in Sections 3.1-3.4 is defined here with full field documentation and usage notes.

**Created:** February 10, 2026, 3:45 PM PST  
**Updated:** March 4, 2026, 12:00 AM PST  
**Version:** 1.4  
**Status:** Draft — ERR-007 resolved (AM-002-001 applied)  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion & PerformanceContext), Section 3.3 (Directional Movement), Section 3.4 (Turning & Momentum)

---
---

## CHANGELOG v1.3 -> v1.4 (March 4, 2026)

**CRITICAL FIXES:**

1. **SPRINT_EXIT_THRESHOLD corrected from 5.2 to 5.5 m/s:** Section 3.1 (authoritative for state machine thresholds) defines SPRINT_EXIT = 5.5f. Section 3.5 had 5.2f with no justification for the discrepancy. Dead zone comment also corrected from [5.2, 5.8] to [5.5, 5.8].

2. **Coordinate system origin corrected:** Agent.Position comment changed from "Origin at pitch center" to "Origin at corner (0,0,0)" per Ball Physics Spec #1 convention and Section 3.6 PitchConfiguration. This conflict was flagged in Section 3.6 v1.1 but never applied to Section 3.5 despite advancing through v1.2 and v1.3.

3. **SPRINT_RESERVOIR_REENTRY corrected from 0.20 to 0.35:** FR-6 (Sections 1-2) specifies 0.35 as the re-entry threshold. The 0.20 value is the exit floor (SPRINT_RESERVOIR_FLOOR). Added separate SPRINT_RESERVOIR_FLOOR constant at 0.20 for clarity. Both values now match FR-6 hysteresis design.

**MODERATE FIXES:**

4. **MAX_LEAN_ANGLE corrected from 45 to 40 degrees:** Section 3.4 (authoritative for lean angle) defines MAX_LEAN_ANGLE = 40.0f. Section 3.5 had 45f with no justification.

5. **Telemetry event size note added:** Section 4 v1.1 identified MovementTelemetryEvent as ~96 bytes, not ~24 bytes. Advisory comment added to struct.

**No physics formulas or other structures changed.** This is a corrections-only amendment.

## CHANGELOG v1.1 â†’ v1.2

**CRITICAL FIXES (New Spec Conflicts Found & Resolved):**

1. **ACCEL_TIME_MAX Regression (Issue #1)**: Reverted from 4.5s â†’ **3.5s** to match Section 3.2 derivation (Tâ‚‰â‚€ = 2.3026 / k_min = 2.3026 / 0.658 = 3.5s). v1.1 changed this from v1.0's correct value without justification. PlayerAttributes.Acceleration description also corrected.

2. **MAX_ACCELERATION Conflict (Issue #2)**: Changed from 15.0 â†’ **8.0 m/sÂ²** to match Section 3.2 `MovementConstants.MAX_ACCELERATION`. Section 3.2 derived 8.0 as safety clamp above max decel (16.2 m/sÂ²) â€” the 15.0 value was inconsistent.

3. **FacingMode Enum Aligned with Section 3.3 (Issue #3)**: Section 3.3.4.2 defines exactly two modes: `AUTO_ALIGN` and `TARGET_LOCK`. v1.0 had four modes (MOVEMENT_DIRECTION, TARGET_LOCK, FIXED_DIRECTION, FROZEN), v1.1 changed to three (MOVEMENT_DIRECTION, TARGET_LOCK, MANUAL_OVERRIDE) â€” neither matched Section 3.3. Now aligned with Section 3.3 as authoritative source.

4. **Turn Rate Model Aligned with Section 3.4 (Issue #9, expanded)**: Full cross-reference revealed v1.1's state-based multiplier model (`BASE_TURN_RATE_MIN/MAX`, `TURN_RATE_SCALE_*`) fundamentally conflicted with Section 3.4's hyperbolic decay model (`Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)`). Replaced with correct constants from Section 3.4: `TURN_RATE_BASE`, `K_TURN_MIN/MAX`, `K_TURN_PER_POINT`.

5. **STUMBLE_SPEED_THRESHOLD Conflict**: Changed from 3.0 â†’ **2.2 m/s** to match Section 3.4 `StumbleConstants.STUMBLE_SPEED_THRESHOLD` (set to JOG_ENTER).

6. **Stumble Probability Max Conflict**: Changed from 0.35 â†’ **0.30** per frame to match Section 3.4 `StumbleConstants.STUMBLE_PROB_MAX`.

7. **Safe Turn Rate Model Conflict**: Section 3.4 uses fraction-based safe thresholds (`SAFE_FRACTION_MIN/MAX` = 0.55/0.85 of max turn rate), not absolute deg/s values. Replaced `SAFE_TURN_RATE_BALANCE_1/20` with correct `SAFE_FRACTION_MIN/MAX` constants.

**MODERATE FIXES:**

8. **Directional Multipliers Restored (Issue #4)**: v1.1 flattened Agility-dependent ranges to single constants. Restored per Section 3.3: lateral 0.65â€“0.75, backward 0.45â€“0.55, both Agility-scaled.

9. **AnimationData Speed Cache Bypass (Issue #5)**: Fixed `Velocity.magnitude > 0.01f` â†’ `Speed > 0.01f` in AnimationData property to use cached speed.

10. **PerformanceContext Constructor Fixed (Issue #6)**: Added Agent constructor back (missing in v1.1). Constructor uses `PerformanceContext.Default` instead of removed `PerformanceContext(this)` constructor.

11. **Stamina Gate Constants Added (Issue #8)**: Added `SPRINT_RESERVOIR_REENTRY` and `AEROBIC_JOG_FLOOR` named constants to MovementConstants, matching Section 3.1 forbidden transition references.

**MINOR FIXES:**

12. **DebugLabel String Advisory (Issue #7)**: Added performance note about string interning for factory methods and warning against interpolated strings.

13. **PlayerAttributes.Agility Description Fixed**: Corrected turn rate description to reflect Section 3.4's model (affects k_turn speed sensitivity, not base rate).

---

## CHANGELOG v1.2 → v1.3

**ERR-007 RESOLUTION — Amendment AM-002-001 Applied (February 21, 2026):**

1. **`KickPower` attribute added** — `int`, 1–20 scale.
   Primary kick velocity attribute. Consumed by Pass Mechanics Spec #5 §3.2.
   Resolves blocking dependency flagged in ERR-007 (Spec Error Log v1.0).

2. **`WeakFootRating` attribute added** — `int`, 1–5 scale.
   Controls weak-foot penalty magnitude. Consumed by Pass Mechanics Spec #5 §3.7.
   Uses 1–5 scale (not 1–20) matching Football Manager convention; see field
   documentation for WeakFootMultiplier derivation.

3. **`Crossing` attribute added** — `int`, 1–20 scale.
   Cross delivery accuracy. Consumed by Pass Mechanics Spec #5 §3.5.
   Separate from Passing to allow specialist winger differentiation.

4. **`Consumed by` comment updated** — Pass Mechanics entry corrected from
   `Passing, Vision, Technique` to `Passing, Vision, Technique, KickPower,
   WeakFootRating, Crossing`. Spec numbers also corrected (Pass Mechanics is #5,
   not #4; Shot Mechanics is #6; First Touch is #4).

5. **Struct size estimate updated** — ~80 bytes → ~84 bytes (3 × int = 12 bytes added).
   Performance note in struct header updated accordingly.

6. **Implementation note 6 updated** — Records v1.3 attribute additions.

**No physics formulas, constants, or other structures changed.** This is a purely
additive amendment. All v1.2 content remains valid.

---

## Table of Contents

- [3.5.1 Core Agent Class](#351-core-agent-class)
- [3.5.2 Movement State Machine Enums](#352-movement-state-machine-enums)
- [3.5.3 Movement Command Structures](#353-movement-command-structures)
- [3.5.4 Physical Properties Interface](#354-physical-properties-interface)
- [3.5.5 Animation Data Contract](#355-animation-data-contract)
- [3.5.6 Configuration Structures](#356-configuration-structures)
- [3.5.7 Debug & Telemetry Structures](#357-debug--telemetry-structures)

---

## 3.5.1 Core Agent Class

### Agent Class

The `Agent` class is the primary container for all movement-related state. Each outfield player (20 per match) has exactly one `Agent` instance managed by the match simulation loop.

```csharp
/// <summary>
/// Agent movement system core class.
/// Manages locomotion physics, state machine, and external interfaces for a single player.
///
/// Lifecycle:
///   1. Instantiated at match start with PlayerAttributes
///   2. Updated every physics frame (60Hz) via Update(dt, command)
///   3. State queried by Collision System, Perception, Tactical AI as needed
///   4. Destroyed at match end
///
/// Thread safety: NOT thread-safe. All access must occur on main simulation thread.
/// Determinism: Uses float by default. Fixed64 support via compiler flag (Stage 5+).
///
/// Design note: This class owns ONLY movement state. Ball interaction state is owned
/// by FirstTouchController (Spec #11). Tactical state is owned by TacticalBrain (Spec #7).
/// This separation prevents God Object antipattern.
///
/// Stage 1 refactoring consideration: This class-based design prioritizes clarity for
/// Stage 0 specification and implementation. Stage 1 may refactor to ECS (Entity Component
/// System) pattern for cache efficiency when simulating 22 agents at 60Hz. The public API
/// is designed to support either approach without breaking external dependencies.
/// </summary>
public class Agent
{
    // ================================================================
    // IDENTITY & CONFIGURATION
    // ================================================================

    /// <summary>
    /// Unique agent ID for this match instance.
    /// Range: 0-21 (22 total agents: 20 outfield + 2 goalkeepers).
    /// Used for collision filtering, event logging, and debug visualization.
    /// </summary>
    public readonly int AgentID;

    /// <summary>
    /// Team assignment (0 = home, 1 = away).
    /// Used for tactical AI queries, not movement physics.
    /// </summary>
    public readonly int TeamID;

    /// <summary>
    /// Is this agent a goalkeeper? 
    /// If true, this Agent instance should NOT be used â€” goalkeepers use
    /// GoalkeeperController (Spec #10) which has its own movement model.
    /// This flag prevents accidental use of outfield movement for keepers.
    /// </summary>
    public readonly bool IsGoalkeeper;

    /// <summary>
    /// Player attribute ratings (1-20 scale).
    /// Immutable after instantiation â€” attribute changes during career mode
    /// happen between matches, not within a match.
    /// 
    /// PERFORMANCE NOTE: This is a large struct (~80 bytes). Passed by value when
    /// used as method parameter. Consider passing by readonly ref in hot paths
    /// if profiling shows struct copying overhead.
    /// </summary>
    public readonly PlayerAttributes Attributes;

    // ================================================================
    // KINEMATIC STATE (Primary)
    // ================================================================

    /// <summary>
    /// Current position on pitch (meters).
    /// Coordinate system: Origin at corner (0,0,0) per Ball Physics Spec #1,
    /// +X = right (homeâ†’away),
    /// +Y = forward (home goalâ†’away goal), Z = height above ground.
    /// Updated every frame by integrating velocity: Position += Velocity * dt.
    /// Clamped to pitch boundaries by boundary collision handler (out of scope for this spec).
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Current velocity (m/s).
    /// Magnitude determines movement state transitions (Section 3.1).
    /// Direction is movement vector â€” can differ from FacingDirection.
    /// Updated by acceleration/deceleration formulas (Section 3.2).
    /// Clamped to [-MAX_SPEED, +MAX_SPEED] = [-12, +12] m/s per axis.
    /// 
    /// IMPORTANT: After modifying Velocity, call InvalidateSpeedCache() to
    /// ensure the cached Speed property remains accurate.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Direction agent is facing (normalized 2D vector on XY plane).
    /// Independent from Velocity direction â€” agent can face north while moving east.
    /// Rotates via UpdateFacingDirection() (Section 3.3.4) at turn rate from Section 3.4.2.
    /// Updated every frame when in voluntary control states (WALKING, JOGGING, SPRINTING, DECELERATING).
    /// Frozen in momentum-only states (STUMBLING, GROUNDED).
    /// Used by Perception System (Spec #6) for field-of-view calculations.
    /// </summary>
    public Vector2 FacingDirection;

    /// <summary>
    /// Current speed (magnitude of Velocity, m/s).
    /// FIXED (v1.1): Now properly cached. Recomputed only when _speedDirty flag is true.
    /// Used for state transition checks (Section 3.1) and turn rate calculations (Section 3.4).
    /// </summary>
    private float _cachedSpeed;
    private bool _speedDirty = true;
    
    public float Speed
    {
        get
        {
            if (_speedDirty)
            {
                _cachedSpeed = Velocity.magnitude;
                _speedDirty = false;
            }
            return _cachedSpeed;
        }
    }

    /// <summary>
    /// Mark speed cache as dirty. MUST be called whenever Velocity is modified.
    /// </summary>
    public void InvalidateSpeedCache()
    {
        _speedDirty = true;
    }

    // ================================================================
    // STATE MACHINE
    // ================================================================

    /// <summary>
    /// Current movement state (Section 3.1).
    /// Determines which physics formulas are active and which transitions are valid.
    /// Updated by MovementStateMachine.Update() every frame.
    /// 
    /// AUTHORITATIVE DEFINITION: Section 3.1.2 AgentMovementState enum.
    /// This section does NOT redefine the enum to prevent maintenance conflicts.
    /// </summary>
    public AgentMovementState CurrentState;

    /// <summary>
    /// Previous movement state (one frame ago).
    /// Used for detecting state transitions and triggering events.
    /// Example: if (CurrentState == SPRINTING && PreviousState == JOGGING) â†’ fire SprintStartEvent.
    /// </summary>
    public AgentMovementState PreviousState;

    /// <summary>
    /// Time in current state (seconds).
    /// Incremented every frame while state is unchanged.
    /// Reset to zero on state transition.
    /// Used for minimum dwell time enforcement (Section 3.1.7).
    /// </summary>
    public float TimeInState;

    /// <summary>
    /// Commanded state from higher-level systems.
    /// Tactical AI/Decision Tree requests a state (e.g., "I want to sprint"), but
    /// physics may override if not physically achievable from current state.
    /// Example: AI commands SPRINTING, but agent is STUMBLING â†’ command ignored until recovery.
    /// </summary>
    public AgentMovementState CommandedState;

    /// <summary>
    /// ADDED v1.1: Reason agent entered GROUNDED state.
    /// Required for variable recovery time calculation (Section 3.1.5).
    /// Values: COLLISION (involuntary), SLIDING_TACKLE (voluntary), DIVING_HEADER (voluntary).
    /// Voluntary actions have faster recovery due to controlled landing.
    /// </summary>
    public GroundedReason GroundedReason;

    /// <summary>
    /// ADDED v1.1: Collision force magnitude when entering GROUNDED state.
    /// Range: [0.0, 1.0] where 0.0 = light nudge, 1.0 = full-speed impact.
    /// Used to scale GROUNDED dwell time for involuntary knockdowns only.
    /// Voluntary actions (slides, headers) ignore this value.
    /// Populated by Collision System (Spec #3) when triggering GROUNDED transition.
    /// </summary>
    public float CollisionForce;

    // ================================================================
    // TURNING & MOMENTUM (Section 3.4)
    // ================================================================

    /// <summary>
    /// Current lean angle (degrees, -90Â° to +90Â°).
    /// Computed from centripetal acceleration when turning (Section 3.4.5).
    /// Positive = leaning right, negative = leaning left.
    /// Used by animation system (Stage 1) for body tilt visualization.
    /// No gameplay effect in Stage 0 â€” pure data output.
    /// </summary>
    public float LeanAngle;

    /// <summary>
    /// Angular velocity applied this frame (deg/s).
    /// How fast the agent is currently rotating (both path and facing).
    /// Clamped by turn rate model (Section 3.4.2).
    /// Used for stumble risk calculation (Section 3.4.4).
    /// </summary>
    public float CurrentTurnRate;

    /// <summary>
    /// Is agent currently in stumble risk zone?
    /// True when CurrentTurnRate exceeds safe threshold (Section 3.4.4.2).
    /// When true, stumble probability is evaluated each frame.
    /// Debug flag â€” not consumed by gameplay systems in Stage 0.
    /// </summary>
    public bool IsInStumbleRiskZone;

    // ================================================================
    // FATIGUE & STAMINA (Spec #13 Forward Reference)
    // ================================================================

    /// <summary>
    /// Aerobic stamina pool (joule-equivalents, dimensionless game units).
    /// Depletes during JOGGING state, regenerates passively.
    /// Gate threshold: JOGGING requires aerobicPool > AEROBIC_JOG_FLOOR (0.15) to enter/sustain.
    /// Below threshold: forced transition to DECELERATING.
    /// Managed by FatigueSystem (Spec #13), read-only for movement system.
    /// </summary>
    public float AerobicStaminaPool;

    /// <summary>
    /// Sprint stamina reservoir (dimensionless units).
    /// Depletes during SPRINTING state, regenerates during lower-intensity movement.
    /// Gate threshold: SPRINTING requires sprintReservoir > SPRINT_RESERVOIR_REENTRY (0.20)
    /// to enter/sustain. Below threshold: forced transition to JOGGING.
    /// Managed by FatigueSystem (Spec #13), read-only for movement system.
    /// </summary>
    public float SprintStaminaReservoir;

    /// <summary>
    /// Current fatigue level (0.0 = fresh, 1.0 = exhausted).
    /// Updated by FatigueSystem (Spec #13) at 10Hz tactical heartbeat rate.
    /// Movement system reads this value but does NOT modify it.
    /// Applied as modifier via PerformanceContext.FatigueModifier.
    /// </summary>
    public float FatigueLevel;

    /// <summary>
    /// Stamina regeneration rate (units/second).
    /// Derived from player's Stamina attribute and match context (intensity, time).
    /// Used by FatigueSystem, not movement system.
    /// </summary>
    public float StaminaRegenRate;

    // ================================================================
    // PERFORMANCE CONTEXT (Section 3.2.1)
    // ================================================================

    /// <summary>
    /// Gateway for all attribute evaluations.
    /// In Stage 0, all modifiers are 1.0 (neutral) â€” this adds zero overhead.
    /// In Stage 2+, this applies form, fatigue, tactical fit, psychology modifiers.
    /// 
    /// Usage pattern:
    ///   float effectivePace = PerformanceContext.EvaluateAttribute(Attributes.Pace);
    ///   // Never: float pace = Attributes.Pace; (spec violation)
    /// 
    /// CRITICAL: All attribute reads for gameplay calculations MUST route through this.
    /// Direct attribute access is a specification violation (enforced by Code Standards, Spec #20).
    /// </summary>
    public PerformanceContext PerformanceContext;

    // ================================================================
    // COLLISION INTERFACE (Spec #3 Dependency)
    // ================================================================

    /// <summary>
    /// Physical properties for collision detection and response.
    /// Populated every frame and consumed by Collision System (Spec #3).
    /// This is the data contract between Agent Movement and Collision System.
    /// See Section 3.5.4 for struct definition.
    /// </summary>
    public AgentPhysicalProperties PhysicalProperties => new AgentPhysicalProperties
    {
        Position = this.Position,
        Velocity = this.Velocity,
        Mass = CalculateMass(),           // Section 3.5.4.2
        HitboxRadius = CalculateHitboxRadius(), // Section 3.5.4.3
        Strength = Attributes.Strength,
        IsGrounded = (CurrentState == AgentMovementState.GROUNDED)
    };

    // ================================================================
    // ANIMATION DATA CONTRACT (Stage 1 Forward Reference)
    // ================================================================

    /// <summary>
    /// Animation state for rendering system (Stage 1).
    /// Defines the data contract for animation system integration.
    /// In Stage 0, this struct is populated but not consumed.
    /// See Section 3.5.5 for struct definition.
    /// 
    /// FIXED v1.2: Uses cached Speed property instead of Velocity.magnitude
    /// for MovementDirection check, avoiding redundant sqrt() call.
    /// </summary>
    public AnimationDataContract AnimationData => new AnimationDataContract
    {
        ContractVersion = 1,
        MovementState = this.CurrentState,
        Speed = this.Speed,
        FacingDirection = this.FacingDirection,
        MovementDirection = (Speed > 0.01f) ? Velocity.normalized : Vector3.zero, // FIXED v1.2: was Velocity.magnitude
        LeanAngle = this.LeanAngle,
        IsDecelerating = (CurrentState == AgentMovementState.DECELERATING),
        IsStumbling = (CurrentState == AgentMovementState.STUMBLING),
        TimeInState = this.TimeInState
    };

    // ================================================================
    // SAFETY & RECOVERY
    // ================================================================

    /// <summary>
    /// Last known valid position (before any failure).
    /// Updated every frame when state is valid.
    /// Used for recovery if NaN/Infinity detected (Section 2.4).
    /// </summary>
    private Vector3 LastValidPosition;

    /// <summary>
    /// Last known valid velocity.
    /// Used for recovery if numerical instability detected.
    /// </summary>
    private Vector3 LastValidVelocity;

    /// <summary>
    /// Number of state changes in the last 1 second.
    /// Used for oscillation detection (Section 2.4).
    /// If > MAX_STATE_CHANGES_PER_SECOND, state machine locks current state
    /// for STATE_LOCK_DURATION seconds.
    /// </summary>
    private int StateChangesThisSecond;

    /// <summary>
    /// Timestamp when oscillation lock activated.
    /// While locked, state machine ignores all transition requests.
    /// </summary>
    private float StateLockExpiry;

    // ================================================================
    // CONSTRUCTOR (RESTORED v1.2 â€” was missing in v1.1)
    // ================================================================

    /// <summary>
    /// Initializes agent with player attributes and spawn position.
    /// Called by match simulator at kickoff.
    /// </summary>
    /// <param name="agentID">Unique ID (0-21)</param>
    /// <param name="teamID">Team assignment (0 or 1)</param>
    /// <param name="attributes">Player attribute ratings (1-20 scale)</param>
    /// <param name="startPosition">Spawn position on pitch</param>
    /// <param name="startFacing">Initial facing direction</param>
    public Agent(int agentID, int teamID, PlayerAttributes attributes,
                 Vector3 startPosition, Vector2 startFacing)
    {
        AgentID = agentID;
        TeamID = teamID;
        IsGoalkeeper = (agentID == 0 || agentID == 11); // Indices 0 and 11 reserved for keepers
        Attributes = attributes;

        Position = startPosition;
        Velocity = Vector3.zero;
        FacingDirection = startFacing.normalized;
        _cachedSpeed = 0f;
        _speedDirty = false; // Velocity is zero, cache is valid

        CurrentState = AgentMovementState.IDLE;
        PreviousState = AgentMovementState.IDLE;
        TimeInState = 0f;
        CommandedState = AgentMovementState.IDLE;

        GroundedReason = GroundedReason.COLLISION; // Default, overwritten on GROUNDED entry
        CollisionForce = 0f;

        LeanAngle = 0f;
        CurrentTurnRate = 0f;
        IsInStumbleRiskZone = false;

        AerobicStaminaPool = 1.0f;   // Full pool at match start
        SprintStaminaReservoir = 1.0f; // Full reservoir at match start
        FatigueLevel = 0f;
        StaminaRegenRate = attributes.Stamina * 0.1f; // Placeholder formula

        // FIXED v1.2: Use PerformanceContext.Default instead of removed constructor
        PerformanceContext = PerformanceContext.Default;

        LastValidPosition = startPosition;
        LastValidVelocity = Vector3.zero;
        StateChangesThisSecond = 0;
        StateLockExpiry = 0f;
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    /// <summary>
    /// Main update loop called every physics frame (60Hz).
    /// Applies movement command, updates state machine, integrates physics.
    /// </summary>
    /// <param name="dt">Time step (typically 1/60 = 0.01667s)</param>
    /// <param name="command">Movement command from tactical AI/decision tree</param>
    public void Update(float dt, MovementCommand command)
    {
        // Implementation handled by MovementSystem.UpdateAgent() â€” this is the entry point
        // Actual logic in separate methods (UpdateStateMachine, ApplyAcceleration, etc.)
        // See Section 4.1 for detailed call sequence
    }

    /// <summary>
    /// Validates current state for NaN/Infinity/invalid values.
    /// Called every frame before physics integration.
    /// Returns true if state contains invalid values, false if safe.
    /// Pattern mirrors Ball.HasInvalidValues() from Ball Physics Spec #1.
    /// </summary>
    public bool HasInvalidValues()
    {
        return float.IsNaN(Position.x) || float.IsNaN(Position.y) || float.IsNaN(Position.z) ||
               float.IsInfinity(Position.x) || float.IsInfinity(Position.y) || float.IsInfinity(Position.z) ||
               float.IsNaN(Velocity.x) || float.IsNaN(Velocity.y) || float.IsNaN(Velocity.z) ||
               float.IsInfinity(Velocity.x) || float.IsInfinity(Velocity.y) || float.IsInfinity(Velocity.z) ||
               float.IsNaN(FacingDirection.x) || float.IsNaN(FacingDirection.y) ||
               float.IsInfinity(FacingDirection.x) || float.IsInfinity(FacingDirection.y);
    }

    /// <summary>
    /// Recovers from invalid state by restoring last valid values.
    /// Called when HasInvalidValues() returns true.
    /// Logs ERROR with full state snapshot.
    /// </summary>
    public void RecoverFromInvalidState()
    {
        Position = LastValidPosition;
        Velocity = LastValidVelocity;
        InvalidateSpeedCache();
        CurrentState = AgentMovementState.IDLE;
        TimeInState = 0f;
        // Log error with agent ID, position, velocity, command history
    }

    /// <summary>
    /// Stores current state as last valid state.
    /// Called every frame when HasInvalidValues() returns false.
    /// </summary>
    private void UpdateLastValidState()
    {
        LastValidPosition = Position;
        LastValidVelocity = Velocity;
    }

    // ================================================================
    // INTERNAL HELPER METHODS
    // ================================================================

    /// <summary>
    /// Calculates agent mass (kg) from Strength attribute.
    /// Used by collision system for momentum transfer (Spec #3).
    /// Formula in Section 3.5.4.2.
    /// 
    /// DESIGN NOTE (v1.1): Deriving both mass AND hitbox from Strength alone is a
    /// Stage 0 simplification. This means a fast, weak winger is always small and light.
    /// Future consideration: Add Height attribute (1-20 scale) for FM-killer parity.
    /// Height would affect hitbox radius independently, allowing tall/light players
    /// (e.g., Peter Crouch) and short/stocky players (e.g., Alexis SÃ¡nchez).
    /// Mass could use formula: base + (Strength Ã— 0.6) + (Height Ã— 0.4).
    /// Deferred to Stage 1+ pending attribute system consolidation (Spec #20).
    /// </summary>
    private float CalculateMass()
    {
        float strengthNormalized = Attributes.Strength / 20f; // 0.05 to 1.0
        return 70f + (strengthNormalized * 30f); // 72.5 kg to 100 kg range
    }

    /// <summary>
    /// Calculates agent hitbox radius (meters) from Strength attribute.
    /// Used by collision system for detection (Spec #3).
    /// Formula in Section 3.5.4.3.
    /// 
    /// See CalculateMass() design note regarding future Height attribute.
    /// </summary>
    private float CalculateHitboxRadius()
    {
        float strengthNormalized = Attributes.Strength / 20f; // 0.05 to 1.0
        return 0.35f + (strengthNormalized * 0.15f); // 0.3525m to 0.50m range
    }
}
```

---

## 3.5.2 Movement State Machine Enums

### AgentMovementState

**AUTHORITATIVE DEFINITION:** Section 3.1.2

This section does NOT redefine the `AgentMovementState` enum. All references to movement states use the definition from Section 3.1.2 to prevent maintenance conflicts and version drift.

**States (summary for reference):**
- `IDLE` â€” Stationary (< 0.1 m/s)
- `WALKING` â€” Low-speed locomotion (0.3â€“2.0 m/s)
- `JOGGING` â€” Medium-speed locomotion (2.0â€“5.5 m/s)
- `SPRINTING` â€” Maximum-effort locomotion (5.5â€“10.2 m/s)
- `DECELERATING` â€” Active braking
- `STUMBLING` â€” Lost balance, recovery phase (min dwell: 300â€“800ms, Balance-dependent)
- `GROUNDED` â€” On ground after collision/fall (min dwell: 600â€“2500ms, attribute-dependent)

**See Section 3.1.2 for full enum definition with documentation.**

### GroundedReason

**AUTHORITATIVE DEFINITION:** Section 3.1.2

Reason an agent entered the `GROUNDED` state. Affects recovery dwell time calculation.

```csharp
/// <summary>
/// Reason agent entered GROUNDED state.
/// See Section 3.1.2 for full definition.
/// </summary>
public enum GroundedReason
{
    COLLISION,        // Involuntary knockdown (Collision System trigger)
    SLIDING_TACKLE,   // Voluntary slide (controlled landing, faster recovery)
    DIVING_HEADER     // Voluntary dive (partial control, moderate recovery)
}
```

### FacingMode

**AUTHORITATIVE DEFINITION:** Section 3.3.4.2

**FIXED v1.2:** Aligned with Section 3.3 which defines exactly two modes. v1.0 had four modes (MOVEMENT_DIRECTION, TARGET_LOCK, FIXED_DIRECTION, FROZEN), v1.1 changed to three (MOVEMENT_DIRECTION, TARGET_LOCK, MANUAL_OVERRIDE) â€” neither matched Section 3.3. Now uses Section 3.3's canonical names.

```csharp
/// <summary>
/// Facing direction update mode.
/// AUTHORITATIVE DEFINITION: Section 3.3.4.2.
///
/// FIXED v1.2: Reduced to two modes per Section 3.3 authoritative definition.
/// The FROZEN/MANUAL_OVERRIDE functionality from v1.0/v1.1 is handled by
/// simply not issuing a facing update command â€” if no FacingMode is active,
/// facing direction is preserved from the previous frame.
/// </summary>
public enum FacingMode
{
    /// <summary>
    /// Facing direction auto-aligns toward movement direction.
    /// Used when agent has no specific look target â€” running forward,
    /// making a run, moving to position.
    /// Rotation rate governed by turn rate (Section 3.4).
    /// 
    /// NOTE: Named AUTO_ALIGN per Section 3.3 (was MOVEMENT_DIRECTION in v1.0/v1.1).
    /// </summary>
    AUTO_ALIGN,

    /// <summary>
    /// Facing direction rotates toward an explicit target position.
    /// Used when agent needs to watch the ball, mark an opponent,
    /// jockey, or receive a pass while moving to position.
    /// Rotation rate governed by turn rate (Section 3.4).
    /// Turn rate penalty applies when facing â‰  movement direction (Section 3.4.3).
    /// </summary>
    TARGET_LOCK
}
```

### DecelerationMode

Controls how deceleration force is applied when slowing down.

```csharp
/// <summary>
/// Deceleration mode selection.
/// Determines stopping distance and stumble risk trade-off.
/// </summary>
public enum DecelerationMode
{
    /// <summary>
    /// Smooth, safe deceleration.
    /// Stopping distance: 3.0â€“5.0m from sprint speed (Agility-dependent).
    /// No stumble risk.
    /// Used for: Normal movement transitions, positioning adjustments.
    /// </summary>
    CONTROLLED,

    /// <summary>
    /// Maximum braking force applied.
    /// Stopping distance: 2.5â€“3.5m from sprint speed (Agility-dependent).
    /// Stumble risk if speed was above sprint threshold at start.
    /// Used for: Emergency stops, last-ditch blocks, urgent direction changes.
    /// </summary>
    EMERGENCY
}
```

---

## 3.5.3 Movement Command Structures

### MovementCommand

The interface between tactical AI and movement physics. External systems issue commands; movement system executes them subject to physical constraints.

```csharp
/// <summary>
/// Movement command from tactical AI or external controller.
/// This is the INPUT to the movement system each frame.
///
/// Command vs State distinction:
///   - Command: "I want to sprint toward this position while facing the ball"
///   - State: "I am currently jogging because stamina prevents sprint"
///
/// The movement system receives commands and translates them into physically
/// achievable states and velocities. Commands may be partially or fully rejected
/// if they violate physics constraints.
///
/// Lifetime: Valid for exactly one frame. New command required each frame.
/// </summary>
public struct MovementCommand
{
    /// <summary>
    /// Target position on pitch (meters, world space).
    /// Agent accelerates toward this position subject to turn rate constraints.
    /// Used to compute desired velocity direction.
    /// </summary>
    public Vector3 TargetPosition;

    /// <summary>
    /// Desired movement intensity.
    /// Tactical AI requests WALKING/JOGGING/SPRINTING; physics validates feasibility.
    /// If requested state is not achievable (e.g., sprint while stumbling),
    /// movement system uses highest achievable state instead.
    /// </summary>
    public AgentMovementState DesiredState;

    /// <summary>
    /// Deceleration mode if slowing down.
    /// CONTROLLED: Safe, longer stopping distance.
    /// EMERGENCY: Fast stop, stumble risk.
    /// Ignored if agent is accelerating or maintaining speed.
    /// </summary>
    public DecelerationMode DecelerationMode;

    /// <summary>
    /// How to update facing direction this frame.
    /// AUTO_ALIGN: Face where you're moving (default).
    /// TARGET_LOCK: Face toward FacingTarget while moving.
    /// 
    /// FIXED v1.2: Two modes only, per Section 3.3 authoritative definition.
    /// To freeze facing (old FROZEN/MANUAL_OVERRIDE behavior), higher-level systems
    /// simply continue sending the same FacingTarget as current facing direction
    /// via TARGET_LOCK mode â€” the turn delta will be zero and facing is preserved.
    /// </summary>
    public FacingMode FacingMode;

    /// <summary>
    /// World-space position to face toward (if FacingMode == TARGET_LOCK).
    /// Commonly set to ball position for "move while watching ball" behavior.
    /// Ignored if FacingMode != TARGET_LOCK.
    /// </summary>
    public Vector3 FacingTarget;

    /// <summary>
    /// DANGER FLAG: Bypass safety constraints?
    /// If true, movement system skips:
    ///   - Turn rate clamping (allows instant 180Â° turns)
    ///   - State transition validation (allows IDLE â†’ SPRINTING in one frame)
    ///   - Stumble risk checks
    /// 
    /// USE CASE: Debug/testing only, or cinematic sequences where physics realism
    /// is intentionally sacrificed (e.g., goal celebration teleport).
    /// 
    /// NEVER set this to true in normal gameplay â€” breaks physics realism.
    /// Logged to telemetry for audit when active.
    /// </summary>
    public bool OverrideSafetyConstraints;

    /// <summary>
    /// Optional debug label for telemetry.
    /// Appears in movement event log to identify command source.
    /// Examples: "ChaseDownBall", "DefensiveTracking", "PressActivation".
    /// Max 32 characters, truncated if longer.
    /// 
    /// PERFORMANCE NOTE (v1.2): Factory method string literals are interned by the
    /// CLR and do not allocate on the heap. However, callers constructing custom
    /// commands with string interpolation (e.g., $"Chase_{targetId}") WILL allocate.
    /// In hot paths, use predefined string constants or MessageCode enum instead.
    /// </summary>
    public string DebugLabel;

    // ================================================================
    // FACTORY METHODS (Common Patterns)
    // ================================================================

    /// <summary>
    /// Creates a "move toward position at desired pace" command.
    /// Most common movement pattern â€” run/walk/jog to a target.
    /// </summary>
    public static MovementCommand MoveTo(Vector3 targetPos, AgentMovementState desiredPace)
    {
        return new MovementCommand
        {
            TargetPosition = targetPos,
            DesiredState = desiredPace,
            DecelerationMode = DecelerationMode.CONTROLLED,
            FacingMode = FacingMode.AUTO_ALIGN,
            FacingTarget = Vector3.zero,
            OverrideSafetyConstraints = false,
            DebugLabel = "MoveTo"
        };
    }

    /// <summary>
    /// Creates a "sprint to this position NOW" command with emergency braking.
    /// Used for urgent situations (last defender, through ball chase).
    /// </summary>
    public static MovementCommand SprintUrgent(Vector3 targetPos)
    {
        return new MovementCommand
        {
            TargetPosition = targetPos,
            DesiredState = AgentMovementState.SPRINTING,
            DecelerationMode = DecelerationMode.EMERGENCY,
            FacingMode = FacingMode.AUTO_ALIGN,
            FacingTarget = Vector3.zero,
            OverrideSafetyConstraints = false,
            DebugLabel = "SprintUrgent"
        };
    }

    /// <summary>
    /// Creates a "stop moving" command with controlled deceleration.
    /// Agent decelerates to IDLE state safely.
    /// Facing is locked by targeting current position (zero turn delta).
    /// </summary>
    public static MovementCommand Stop(Vector3 currentPosition)
    {
        return new MovementCommand
        {
            TargetPosition = Vector3.zero, // Target ignored when stopping
            DesiredState = AgentMovementState.IDLE,
            DecelerationMode = DecelerationMode.CONTROLLED,
            FacingMode = FacingMode.TARGET_LOCK,     // Lock facing via zero-delta trick
            FacingTarget = currentPosition + Vector3.forward, // Face forward, no rotation
            OverrideSafetyConstraints = false,
            DebugLabel = "Stop"
        };
    }

    /// <summary>
    /// Creates a "strafe while watching target" command.
    /// Agent moves toward targetPos but faces toward facingTarget (e.g., ball).
    /// </summary>
    public static MovementCommand StrafeWhileWatching(
        Vector3 targetPos, Vector3 facingTarget, AgentMovementState intensity)
    {
        return new MovementCommand
        {
            TargetPosition = targetPos,
            DesiredState = intensity,
            DecelerationMode = DecelerationMode.CONTROLLED,
            FacingMode = FacingMode.TARGET_LOCK,
            FacingTarget = facingTarget,
            OverrideSafetyConstraints = false,
            DebugLabel = "StrafeWatchBall"
        };
    }
}
```

---

