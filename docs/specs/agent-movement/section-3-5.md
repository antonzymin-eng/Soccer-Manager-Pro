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

## 3.5.4 Physical Properties Interface

### AgentPhysicalProperties

This struct is the data contract between the Agent Movement System and the Collision System (Spec #3). Every frame, each agent provides these properties for collision detection and response calculations.

```csharp
/// <summary>
/// Physical properties of an agent for collision calculations.
/// Provided by Agent Movement System, consumed by Collision System (Spec #3).
///
/// Data contract:
///   - Agent Movement populates this struct every frame
///   - Collision System reads these values for spatial queries and momentum transfer
///   - NO direct modification by Collision System (read-only interface)
///
/// Lifetime: Valid for exactly one frame. Regenerated each frame from Agent state.
/// </summary>
public struct AgentPhysicalProperties
{
    /// <summary>
    /// Current position (world space, meters).
    /// Used for spatial partitioning and distance checks.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Current velocity (m/s).
    /// Used for momentum transfer calculations in collisions.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Agent mass (kg).
    /// Derived from Strength attribute via formula in Section 3.5.4.2.
    /// Range: 72.5 kg (Strength 1) to 100 kg (Strength 20).
    /// Used for momentum conservation in collisions: p = m Ã— v.
    /// </summary>
    public float Mass;

    /// <summary>
    /// Collision detection radius (meters).
    /// Represents agent's physical footprint â€” circular hitbox in 2D (XY plane).
    /// Derived from Strength attribute via formula in Section 3.5.4.3.
    /// Range: 0.3525m (Strength 1) to 0.50m (Strength 20).
    /// Two agents collide when distance < (radius1 + radius2).
    /// </summary>
    public float HitboxRadius;

    /// <summary>
    /// Strength attribute rating (1-20).
    /// Used by Collision System for:
    ///   - Foul detection thresholds
    ///   - Who gets knocked over in a 50/50 challenge
    ///   - Shielding effectiveness
    /// Higher Strength = more likely to win physical duels.
    /// </summary>
    public int Strength;

    /// <summary>
    /// Is agent on the ground (GROUNDED state)?
    /// If true, collision detection treats agent as obstacle, not active participant.
    /// Other agents can collide with grounded agent but grounded agent doesn't generate
    /// collision responses (momentum transfer disabled while down).
    /// </summary>
    public bool IsGrounded;
}
```

### 3.5.4.2 Mass Derivation Formula

Agent mass is derived from Strength attribute only (Stage 0 simplification). See `Agent.CalculateMass()` design note regarding future Height attribute consideration.

```csharp
/// <summary>
/// Calculates agent mass from Strength attribute.
/// Range: [72.5 kg, 100 kg] for Strength [1, 20].
///
/// Derivation:
///   Base mass (Strength 1): 70 kg + (1/20 Ã— 30 kg) = 72.5 kg
///   Max mass (Strength 20): 70 kg + (20/20 Ã— 30 kg) = 100 kg
///
/// Used in collision momentum transfer:
///   Î”v = (m2 Ã— v2 - m1 Ã— v1) / (m1 + m2)
///
/// Examples:
///   Strength 1:  72.5 kg (lightweight winger)
///   Strength 10: 85.0 kg (average midfielder)
///   Strength 20: 100 kg (powerful striker, strong CB)
/// </summary>
float mass = 70f + (strength / 20f) * 30f;
```

### 3.5.4.3 Hitbox Radius Derivation Formula

Hitbox radius is derived from Strength attribute only (Stage 0 simplification).

```csharp
/// <summary>
/// Calculates agent hitbox radius from Strength attribute.
/// Range: [0.3525m, 0.50m] for Strength [1, 20].
///
/// Derivation:
///   Base radius (Strength 1): 0.35m + (1/20 Ã— 0.15m) = 0.3525m
///   Max radius (Strength 20): 0.35m + (20/20 Ã— 0.15m) = 0.50m
///
/// Used in collision detection:
///   if (distance < radius1 + radius2) â†’ collision occurred
///
/// Examples:
///   Strength 1:  0.3525m (narrow hitbox, agile winger)
///   Strength 10: 0.4250m (average midfielder)
///   Strength 20: 0.5000m (wide hitbox, physical striker)
/// </summary>
float radius = 0.35f + (strength / 20f) * 0.15f;
```

---

## 3.5.5 Animation Data Contract

### AnimationDataContract

Defines the interface between movement physics and the animation system (Stage 1). In Stage 0, this struct is populated but not consumed.

```csharp
/// <summary>
/// Animation data contract for rendering system.
/// Movement system populates this struct every frame; animation system consumes it.
///
/// Version history:
///   v1: Stage 0 â€” core movement state, speed, facing, lean
///   v2+: Stage 1+ â€” ball-at-feet, injury, fatigue visual indicators
///
/// Backward compatibility: Animation system checks ContractVersion before reading
/// Stage 1+ fields. If version < 2, Stage 1+ fields default to safe values.
/// </summary>
public struct AnimationDataContract
{
    /// <summary>
    /// Contract version number.
    /// Current: 1 (Stage 0).
    /// Incremented when new fields added in future stages.
    /// </summary>
    public int ContractVersion;

    // ================================================================
    // CORE MOVEMENT STATE (Version 1)
    // ================================================================

    /// <summary>
    /// Current movement state (IDLE, WALKING, JOGGING, SPRINTING, etc.).
    /// Animation system selects base animation based on this.
    /// </summary>
    public AgentMovementState MovementState;

    /// <summary>
    /// Current speed (m/s).
    /// Used for animation blending (walkâ†’jogâ†’sprint transition smoothing).
    /// </summary>
    public float Speed;

    /// <summary>
    /// Direction agent is facing (normalized 2D vector).
    /// Controls character model rotation.
    /// </summary>
    public Vector2 FacingDirection;

    /// <summary>
    /// Direction of movement (normalized 3D vector).
    /// If speed < 0.01 m/s, this is zero vector (not moving).
    /// Used for animation selection (forward run vs backward run vs strafe).
    /// </summary>
    public Vector3 MovementDirection;

    /// <summary>
    /// Body lean angle (degrees, -90Â° to +90Â°).
    /// Positive = leaning right, negative = leaning left.
    /// Used for procedural animation (body tilt when turning).
    /// </summary>
    public float LeanAngle;

    /// <summary>
    /// Is agent actively decelerating?
    /// Triggers braking animation (leaning backward, arm balance).
    /// </summary>
    public bool IsDecelerating;

    /// <summary>
    /// Is agent stumbling?
    /// Triggers stumble recovery animation (off-balance, arm flailing).
    /// </summary>
    public bool IsStumbling;

    /// <summary>
    /// Time in current state (seconds).
    /// Used for animation timing (e.g., GROUNDED recovery animation duration).
    /// </summary>
    public float TimeInState;

    // ================================================================
    // STAGE 1+ EXTENSIONS (Version 2+, unused in Stage 0)
    // ================================================================

    /// <summary>
    /// Does agent have ball at feet?
    /// If true, triggers dribbling animation instead of running.
    /// Default: false (Stage 0 â€” no ball interaction yet).
    /// </summary>
    public bool HasBallAtFeet;

    /// <summary>
    /// Injury severity (0.0 = none, 1.0 = severe).
    /// Triggers limping animation blend.
    /// Default: 0.0 (Stage 0 â€” no injury system yet).
    /// </summary>
    public float InjuryLevel;

    /// <summary>
    /// Fatigue level (0.0 = fresh, 1.0 = exhausted).
    /// Affects animation playback speed and posture (hunched when tired).
    /// Default: 0.0 (Stage 0 â€” fatigue affects physics but not animation yet).
    /// </summary>
    public float FatigueVisualIntensity;
}
```

---

## 3.5.6 Configuration Structures

### PlayerAttributes

Consolidated attribute structure for agent capabilities. All attributes use 1-20 integer scale.

```csharp
/// <summary>
/// Player attribute ratings for physical and technical capabilities.
/// All attributes: 1-20 integer scale (1 = poor, 10 = average, 20 = world-class).
///
/// PROVISIONAL ATTRIBUTE LIST:
/// This is Stage 0 movement system's view of required attributes.
/// Spec #20 (Code Standards) will create master attribute registry consolidating
/// all 20 specification requirements. Attribute names may change after Week 4.
///
/// PERFORMANCE NOTE (v1.3): This is a large struct (~84 bytes, 23+ fields). Size increased from ~80 bytes in v1.2 by addition of KickPower, WeakFootRating, Crossing (3 × 4 bytes).
/// Passed by value in C#. Consider passing by readonly ref in hot paths if
/// profiling shows struct copy overhead. Agent.Attributes is marked readonly
/// which helps, but method parameters still copy the whole struct.
///
/// Consumed by:
///   - Movement System (this spec): Pace, Acceleration, Agility, Balance, Strength, Stamina
///   - Collision System (Spec #3): Strength, Balance, Aggression
///   - Pass Mechanics (Spec #5): Passing, Vision, Technique, KickPower, WeakFootRating, Crossing
///   - Shot Mechanics (Spec #6): Finishing, LongShots, Technique, Composure
///   - First Touch (Spec #4): FirstTouch, Technique, Composure
///   [... other specs TBD â€" Spec #20 will produce master attribute registry]
/// </summary>
public struct PlayerAttributes
{
    // ================================================================
    // PHYSICAL ATTRIBUTES (Movement System)
    // ================================================================

    /// <summary>
    /// Top speed potential (linear mapping to max velocity).
    /// Range: 7.5 m/s (Pace 1) to 10.2 m/s (Pace 20).
    /// Real-world references: 
    ///   Pace 20 = Kylian MbappÃ©, Alphonso Davies (~37 km/h)
    ///   Pace 15 = Mohamed Salah, Leroy SanÃ© (~34 km/h)
    ///   Pace 10 = Average midfielder (~30 km/h)
    ///   Pace 5  = Slow center-back (~27 km/h)
    /// </summary>
    public int Pace;

    /// <summary>
    /// How quickly agent reaches top speed from standstill.
    /// Range: 2.5s (Accel 20) to 3.5s (Accel 1) to reach 90% top speed.
    /// High Acceleration = explosive first step (e.g., Lionel Messi).
    /// Low Acceleration = slow buildup (e.g., tall target man).
    /// 
    /// FIXED v1.2: Corrected range from "4.5s" back to "3.5s" per Section 3.2
    /// derivation: Tâ‚‰â‚€ = 2.3026 / k_min = 2.3026 / 0.658 = 3.5 seconds.
    /// </summary>
    public int Acceleration;

    /// <summary>
    /// Turning speed sensitivity â€” how much speed degrades turning ability.
    /// Affects k_turn parameter in Section 3.4 turn rate formula:
    ///   Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)
    /// High Agility â†’ low k_turn (0.35) â†’ less speed penalty on turning.
    /// Low Agility â†’ high k_turn (0.78) â†’ severe speed penalty on turning.
    /// Also affects deceleration distance and directional speed multipliers.
    /// 
    /// FIXED v1.2: Description corrected. Agility affects k_turn (speed sensitivity),
    /// NOT base turn rate. All agents share the same 720Â°/s base rate at zero speed.
    /// The differentiation happens at higher speeds where low-Agility agents lose
    /// turning ability much faster.
    /// </summary>
    public int Agility;

    /// <summary>
    /// Resistance to stumbling and falling during sharp maneuvers.
    /// Primary factor in stumble probability calculation (Section 3.4.4).
    /// Range: 30% stumble chance (Balance 1) to 5% (Balance 20) at max turn rate.
    /// High Balance = stays upright in tight turns (e.g., N'Golo KantÃ©).
    /// Low Balance = easily knocked off stride.
    /// Also affects GROUNDED and STUMBLING recovery dwell time.
    /// Also applies as modifier to turn rate (Section 3.4.2).
    /// </summary>
    public int Balance;

    /// <summary>
    /// Physical power and mass.
    /// Affects: agent mass (72.5â€“100 kg), hitbox radius (0.35â€“0.50m), shielding.
    /// High Strength = wins physical duels, harder to knock off ball.
    /// Low Strength = loses 50/50s, pushed off ball easily.
    /// Also affects GROUNDED recovery time (combined with Balance).
    /// </summary>
    public int Strength;

    /// <summary>
    /// Endurance and stamina regeneration rate.
    /// Affects: stamina pool size, regeneration rate (Spec #13 FatigueSystem).
    /// High Stamina = can sprint repeatedly, slow fatigue accumulation.
    /// Low Stamina = limited sprint capacity, quick exhaustion.
    /// Does NOT directly affect movement physics â€” fatigue modifier does.
    /// </summary>
    public int Stamina;

    // ================================================================
    // TECHNICAL ATTRIBUTES (Other Specs)
    // ================================================================

    /// <summary>Ball control when receiving a pass. Used by Spec #11.</summary>
    public int FirstTouch;

    /// <summary>Pass accuracy and weight. Used by Spec #4.</summary>
    public int Passing;

    /// <summary>Shot accuracy and power. Used by Spec #5.</summary>
    public int Finishing;

    /// <summary>Long-range shooting ability. Used by Spec #5.</summary>
    public int LongShots;

    /// <summary>Aerial ability (heading accuracy/power). Used by Spec #9.</summary>
    public int Heading;

    /// <summary>General technical skill (touch, dribbling, ball manipulation).</summary>
    public int Technique;

    /// <summary>Ability to find space and create passing lanes. Used by Specs #4, #12.</summary>
    public int Vision;

    /// <summary>Performance under pressure. Used by Specs #4, #5.</summary>
    public int Composure;

    /// <summary>Tendency to commit fouls and aggressive challenges. Used by Spec #3.</summary>
    public int Aggression;

    // ================================================================
    // KICKING / PASSING ATTRIBUTES (Pass Mechanics — AM-002-001)
    // ================================================================

    /// <summary>
    /// Kicking power — determines maximum achievable ball velocity on passes and shots.
    /// Range: 1–20 (1 = weak kick, 10 = average professional, 20 = thunderous striker).
    ///
    /// Used by:
    ///   - Pass Mechanics Spec #5 §3.2: primary factor in kick speed formula.
    ///     v_kick = vOffset + ((KickPower / ATTR_MAX) × (vMax - vOffset)) × (1 - fatigue × FATIGUE_POWER_REDUCTION)
    ///
    /// Real-world references:
    ///   KickPower 20 = Cristiano Ronaldo, Gareth Bale (shots >120 km/h)
    ///   KickPower 15 = Strong-passing central midfielder (~100 km/h)
    ///   KickPower 10 = Average professional (~85 km/h)
    ///   KickPower  5 = Technically gifted but physically slight player (~70 km/h)
    ///
    /// ADDED: v1.3 — ERR-007 resolution (AM-002-001).
    /// </summary>
    public int KickPower;

    /// <summary>
    /// Weak foot rating — quality of the player's non-dominant foot.
    /// Range: 1–5 (1 = very weak, 3 = adequate, 5 = two-footed).
    ///
    /// SCALE NOTE: Uses 1–5, NOT 1–20. This is intentional. Weak foot ability
    /// has five meaningful tiers matching Football Manager convention:
    ///   1 = Rarely uses weak foot (strong penalty)
    ///   2 = Can use but inconsistently
    ///   3 = Reasonably comfortable
    ///   4 = Mostly comfortable
    ///   5 = Equally strong on both feet (no penalty)
    ///
    /// Used by:
    ///   - Pass Mechanics Spec #5 §3.7: scales penalty multiplier when
    ///     PassRequest.IsWeakFoot == true.
    ///     WeakFootMultiplier = 0.50 + (WeakFootRating - 1) / 4 × 0.50
    ///     Range: 0.50 (rating 1) to 1.00 (rating 5, no penalty).
    ///
    /// ADDED: v1.3 — ERR-007 resolution (AM-002-001).
    /// </summary>
    public int WeakFootRating;

    /// <summary>
    /// Crossing accuracy — quality of crosses delivered from wide areas.
    /// Range: 1–20 (1 = poor delivery, 10 = reliable, 20 = elite crosser).
    ///
    /// Separate from Passing to allow independent specialisation:
    ///   - A direct ball-playing midfielder may have Passing 18, Crossing 10
    ///   - A traditional winger may have Passing 14, Crossing 18
    ///
    /// Used by:
    ///   - Pass Mechanics Spec #5 §3.5: replaces Passing as the accuracy attribute
    ///     when PassType is CROSS (any sub-type: Flat, Whipped, High).
    ///
    /// Real-world references:
    ///   Crossing 20 = David Beckham, Trent Alexander-Arnold
    ///   Crossing 15 = Competent wide midfielder
    ///   Crossing 10 = Average full-back
    ///   Crossing  5 = Centre-back pressed into wide role
    ///
    /// ADDED: v1.3 — ERR-007 resolution (AM-002-001).
    /// </summary>
    public int Crossing;

    // Additional attributes TBD pending other spec requirements
    // Examples: Positioning, Anticipation, Concentration, Decisions, etc.
    // Spec #20 (Code Standards) will produce master attribute registry.
}
```

### MovementConstants

All magic numbers from Sections 3.1-3.4 consolidated here for easy tuning.

```csharp
/// <summary>
/// Global constants for agent movement system.
/// All tunable parameters in one place for ease of balancing.
///
/// Modification policy: Values frozen after Stage 0 approval. Changes in Stage 1+
/// require regression testing of all movement scenarios.
///
/// FIXED v1.2: Full cross-reference verification against Sections 3.1, 3.2, 3.3, 3.4.
/// All values now match their authoritative source sections.
/// </summary>
public static class MovementConstants
{
    // ================================================================
    // STATE TRANSITION THRESHOLDS (Section 3.1.3 â€” AUTHORITATIVE)
    // ================================================================

    // IDLE state
    public const float IDLE_ENTER_THRESHOLD = 0.1f;  // m/s
    public const float IDLE_EXIT_THRESHOLD = 0.3f;   // m/s

    // WALKING state
    public const float WALK_ENTER_THRESHOLD = 0.3f;  // m/s (same as IDLE_EXIT)
    // No WALK_EXIT â€” uses JOG_ENTER as boundary

    // JOGGING state
    public const float JOG_ENTER_THRESHOLD = 2.2f;   // m/s
    public const float JOG_EXIT_THRESHOLD = 1.9f;    // m/s (FIXED v1.1: was 1.8, now matches Section 3.1.3)

    // SPRINTING state
    public const float SPRINT_ENTER_THRESHOLD = 5.8f; // m/s
    public const float SPRINT_EXIT_THRESHOLD = 5.5f;  // m/s -- FIXED v1.4: was 5.2, matches Section 3.1 SPRINT_EXIT

    // Hysteresis dead zones (calculated, not constants)
    // IDLE: [0.1, 0.3] m/s
    // JOG: [1.9, 2.2] m/s
    // SPRINT: [5.5, 5.8] m/s -- FIXED v1.4

    // ================================================================
    // STATE DWELL TIMES (Section 3.1.5)
    // ================================================================

    // STUMBLING minimum dwell time (Balance-dependent, Section 3.1.2 authoritative)
    // Range: 300-800ms
    public const float STUMBLE_MIN_DURATION_BALANCE_20 = 0.3f; // seconds
    public const float STUMBLE_MIN_DURATION_BALANCE_1 = 0.8f;  // seconds

    // GROUNDED minimum dwell time (variable, Section 3.1.5.2 authoritative)
    // Formula: base / ((Strength + Balance) / 40.0), modified by GroundedReason + CollisionForce
    // Range: 600-2500ms clamped
    public const float GROUNDED_BASE_DWELL = 1.0f;        // Base before attribute scaling
    public const float GROUNDED_MIN_DWELL = 0.6f;         // Absolute minimum (high attributes)
    public const float GROUNDED_MAX_DWELL = 2.5f;         // Absolute maximum (low attributes)
    public const float GROUNDED_COLLISION_DWELL_MIN = 0.65f; // Collision force scaling floor

    // ================================================================
    // STAMINA GATE THRESHOLDS (Section 3.1.4 â€” Forbidden Transitions)
    // ADDED v1.2: Named constants for values previously only in field comments.
    // ================================================================

    /// <summary>
    /// Minimum sprint reservoir to RE-ENTER SPRINTING state after depletion.
    /// Agent must recover above this before sprinting again (hysteresis).
    /// Referenced by FR-6: "Sprint reservoir must recover above
    /// SPRINT_RESERVOIR_REENTRY (0.35) before re-entering SPRINTING."
    /// FIXED v1.4: Changed from 0.20 (exit floor) to 0.35 (re-entry).
    /// See also SPRINT_RESERVOIR_FLOOR below for exit threshold.
    /// Referenced by Section 3.1.4 forbidden transition:
    ///   "Any state â†’ SPRINTING when sprintReservoir < SPRINT_RESERVOIR_REENTRY"
    /// </summary>
    public const float SPRINT_RESERVOIR_REENTRY = 0.35f; // FIXED v1.4: was 0.20

    /// <summary>
    /// Sprint reservoir level below which SPRINTING is forcibly exited.
    /// Agent drops from SPRINTING to JOGGING when reservoir falls below this.
    /// Re-entry requires reaching SPRINT_RESERVOIR_REENTRY (0.35) -- hysteresis gap.
    /// </summary>
    public const float SPRINT_RESERVOIR_FLOOR = 0.20f;

    /// <summary>
    /// Minimum aerobic pool to enter/sustain JOGGING state.
    /// Below this: forced transition to DECELERATING.
    /// Referenced by Section 3.1.4 forbidden transition:
    ///   "JOGGING â†’ JOGGING when aerobicPool < AEROBIC_JOG_FLOOR"
    /// </summary>
    public const float AEROBIC_JOG_FLOOR = 0.15f;

    // ================================================================
    // LOCOMOTION PARAMETERS (Section 3.2 â€” AUTHORITATIVE)
    // ================================================================

    // Top speed range
    public const float TOP_SPEED_MIN = 7.5f;   // m/s (Pace 1)
    public const float TOP_SPEED_MAX = 10.2f;  // m/s (Pace 20)

    // Acceleration time range (standstill to 90% top speed)
    // FIXED v1.2: Reverted ACCEL_TIME_MAX from 4.5 â†’ 3.5 per Section 3.2
    // Derivation: Tâ‚‰â‚€ = 2.3026 / k_min = 2.3026 / 0.658 = 3.5s
    public const float ACCEL_TIME_MIN = 2.5f;  // seconds (Acceleration 20)
    public const float ACCEL_TIME_MAX = 3.5f;  // seconds (Acceleration 1) â€” FIXED v1.2

    // Acceleration rate constants (Section 3.2 exponential model)
    public const float ACCEL_K_MIN = 0.658f;   // sâ»Â¹ (Acceleration 1, slowest)
    public const float ACCEL_K_MAX = 0.921f;   // sâ»Â¹ (Acceleration 20, fastest)

    // Deceleration stopping distances from sprint (per FR-3 revision)
    public const float CONTROLLED_DECEL_DIST_MIN = 3.0f; // meters (Agility 20)
    public const float CONTROLLED_DECEL_DIST_MAX = 5.0f; // meters (Agility 1)
    public const float EMERGENCY_DECEL_DIST_MIN = 2.5f;  // meters (Agility 20)
    public const float EMERGENCY_DECEL_DIST_MAX = 3.5f;  // meters (Agility 1)

    // ================================================================
    // DIRECTIONAL MOVEMENT (Section 3.3 â€” AUTHORITATIVE)
    // FIXED v1.2: Restored Agility-dependent ranges (v1.1 flattened to single values)
    // ================================================================

    // Lateral speed multipliers (Agility-scaled)
    public const float LATERAL_MULT_MIN = 0.65f;   // Agility 1 (worst lateral movement)
    public const float LATERAL_MULT_MAX = 0.75f;   // Agility 20 (best lateral movement)
    public const float LATERAL_MULT_PER_POINT =
        (LATERAL_MULT_MAX - LATERAL_MULT_MIN) / 19.0f;

    // Backward speed multipliers (Agility-scaled)
    public const float BACKWARD_MULT_MIN = 0.45f;  // Agility 1 (worst backward movement)
    public const float BACKWARD_MULT_MAX = 0.55f;  // Agility 20 (best backward movement)
    public const float BACKWARD_MULT_PER_POINT =
        (BACKWARD_MULT_MAX - BACKWARD_MULT_MIN) / 19.0f;

    // ================================================================
    // TURN RATE PARAMETERS (Section 3.4 â€” AUTHORITATIVE)
    // FIXED v1.2: Replaced v1.1's state-based multiplier model with Section 3.4's
    // hyperbolic decay model: Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)
    // ================================================================

    /// <summary>
    /// Base turn rate for ALL agents at zero speed (Â°/s).
    /// All agents turn at the same rate when stationary â€” Agility differentiates
    /// at speed via k_turn coefficient.
    /// </summary>
    public const float TURN_RATE_BASE = 720.0f; // deg/s (2 full rotations/sec)

    /// <summary>
    /// Turn rate speed-sensitivity at Agility 1 (most speed-sensitive = slowest at speed).
    /// Higher k_turn â†’ more turn rate loss at speed.
    /// Example at 9 m/s: Ï‰ = 720 / (1 + 0.78 Ã— 9) = 720 / 8.02 = 89.8Â°/s
    /// </summary>
    public const float K_TURN_MAX = 0.78f;  // Agility 1 (inverted: high k = worst agility)

    /// <summary>
    /// Turn rate speed-sensitivity at Agility 20 (least speed-sensitive = fastest at speed).
    /// Lower k_turn â†’ less turn rate loss at speed.
    /// Example at 9 m/s: Ï‰ = 720 / (1 + 0.35 Ã— 9) = 720 / 4.15 = 173.5Â°/s
    /// </summary>
    public const float K_TURN_MIN = 0.35f;  // Agility 20 (inverted: low k = best agility)

    /// <summary>Per-attribute-point decrease in k_turn (more agile = lower k).</summary>
    public const float K_TURN_PER_POINT = (K_TURN_MAX - K_TURN_MIN) / 19.0f;

    /// <summary>Turn rate safety cap (maximum ever allowed).</summary>
    public const float TURN_RATE_CAP = 720.0f; // deg/s

    // ================================================================
    // STUMBLE RISK (Section 3.4.4 â€” AUTHORITATIVE)
    // FIXED v1.2: All values now match Section 3.4 StumbleConstants
    // ================================================================

    /// <summary>
    /// Minimum speed for stumble system activation (m/s).
    /// Below this, no stumble risk regardless of turn rate.
    /// Set to JOG_ENTER per Section 3.4: "At walking pace, players have enough
    /// time and ground contact to recover from any direction change."
    /// FIXED v1.2: Changed from 3.0 â†’ 2.2 to match Section 3.4.
    /// </summary>
    public const float STUMBLE_SPEED_THRESHOLD = 2.2f; // m/s â€” FIXED v1.2

    /// <summary>
    /// Safe turn rate fraction at Balance 1 (smallest safe zone).
    /// Agent can use 55% of max turn rate before entering risk zone.
    /// FIXED v1.2: Section 3.4 uses fraction-based thresholds, not absolute deg/s.
    /// </summary>
    public const float SAFE_FRACTION_MIN = 0.55f; // Balance 1

    /// <summary>
    /// Safe turn rate fraction at Balance 20 (largest safe zone).
    /// Agent can use 85% of max turn rate before entering risk zone.
    /// </summary>
    public const float SAFE_FRACTION_MAX = 0.85f; // Balance 20

    /// <summary>Per-attribute-point increase in safe fraction.</summary>
    public const float SAFE_FRACTION_PER_POINT =
        (SAFE_FRACTION_MAX - SAFE_FRACTION_MIN) / 19.0f;

    /// <summary>
    /// Maximum stumble probability per frame at Balance 1.
    /// Applied when agent is at maximum turn rate (fully in risk zone).
    /// FIXED v1.2: Changed from 0.35 â†’ 0.30 to match Section 3.4.
    /// </summary>
    public const float STUMBLE_PROB_MAX = 0.30f; // Balance 1 â€” FIXED v1.2

    /// <summary>
    /// Minimum stumble probability per frame at Balance 20.
    /// Even world-class balance has some risk at absolute max turn rate.
    /// </summary>
    public const float STUMBLE_PROB_MIN = 0.05f; // Balance 20

    /// <summary>Per-attribute-point decrease in stumble probability.</summary>
    public const float STUMBLE_PROB_PER_POINT =
        (STUMBLE_PROB_MAX - STUMBLE_PROB_MIN) / 19.0f;

    // Lean angle max (visual only, no gameplay effect)
    public const float MAX_LEAN_ANGLE = 40f; // degrees -- FIXED v1.4: was 45, matches Section 3.4 authoritative value

    // ================================================================
    // SAFETY LIMITS
    // ================================================================

    /// <summary>
    /// Absolute maximum speed for any agent. Physics clamp.
    /// 12.0 m/s â‰ˆ 43.2 km/h. Well above any realistic sprint (~37 km/h).
    /// Consistent with Section 2.4 runaway velocity detection.
    /// </summary>
    public const float MAX_SPEED = 12.0f; // m/s

    /// <summary>
    /// Absolute maximum acceleration for any agent. Physics clamp.
    /// FIXED v1.2: Changed from 15.0 â†’ 8.0 m/sÂ² to match Section 3.2.
    /// Section 3.2 derived 8.0 as safety clamp above max attribute-derived values.
    /// </summary>
    public const float MAX_ACCELERATION = 8.0f; // m/sÂ² â€” FIXED v1.2

    /// <summary>Maximum turn rate safety clamp.</summary>
    public const float MAX_TURN_RATE = 720f; // deg/s (matches TURN_RATE_BASE)

    /// <summary>Speed below which velocity is zeroed (prevents numerical drift).</summary>
    public const float MIN_VELOCITY_MAGNITUDE = 0.001f; // m/s

    // State machine oscillation detection
    public const int MAX_STATE_CHANGES_PER_SECOND = 6;
    public const float STATE_LOCK_DURATION = 0.5f;  // seconds
}
```

### PerformanceContext

**ADDED v1.1, FIXED v1.2:** Minimal definition to satisfy forward reference from Section 3.2.

```csharp
/// <summary>
/// Performance modifiers applied to movement calculations.
/// Full specification in Section 3.2.1.
///
/// STAGE 0 DEFAULTS: All modifiers are 1.0 (neutral, no effect).
/// STAGE 2+: Form, fatigue, psychology systems populate these values.
///
/// This minimal definition satisfies the forward reference from Section 3.2.
/// See Section 3.2.1 for full documentation of modifier semantics and application.
///
/// FIXED v1.2: Removed constructor that accepted Agent reference (was inconsistent
/// with struct semantics). Use PerformanceContext.Default static factory instead.
/// </summary>
public struct PerformanceContext
{
    /// <summary>
    /// Current form modifier (0.7â€“1.15 range).
    /// Stage 0: Always 1.0 (neutral).
    /// Stage 2+: Recent performance affects attributes temporarily.
    /// </summary>
    public float FormModifier;

    /// <summary>
    /// Fatigue modifier (0.6â€“1.0 range).
    /// Stage 0: Always 1.0 (no fatigue).
    /// Stage 1+: Updated by FatigueSystem (Spec #13) at 10Hz.
    /// Applied to: Pace, Acceleration, Agility.
    /// </summary>
    public float FatigueModifier;

    /// <summary>
    /// Psychology modifier (0.8â€“1.1 range).
    /// Stage 0: Always 1.0 (neutral).
    /// Stage 3+: Match context affects mentality (crowd, score, injury).
    /// Applied to: Composure, Aggression, Decision-making.
    /// </summary>
    public float PsychologyModifier;

    /// <summary>
    /// Creates default context (all modifiers = 1.0).
    /// Used in Stage 0 where modifiers have no effect.
    /// </summary>
    public static PerformanceContext Default => new PerformanceContext
    {
        FormModifier = 1.0f,
        FatigueModifier = 1.0f,
        PsychologyModifier = 1.0f
    };

    /// <summary>
    /// Evaluates an attribute through all active modifiers.
    /// Stage 0: Returns rawAttribute unchanged (all modifiers = 1.0).
    /// Stage 2+: Applies form Ã— fatigue Ã— psychology.
    /// </summary>
    /// <param name="rawAttribute">Raw attribute value (1-20)</param>
    /// <returns>Modified attribute value (can exceed 1-20 range)</returns>
    public float EvaluateAttribute(int rawAttribute)
    {
        return rawAttribute * FormModifier * FatigueModifier * PsychologyModifier;
    }
}
```

---

## 3.5.7 Debug & Telemetry Structures

### MovementTelemetryEvent

For debugging and replay reconstruction. Logged every frame (or on state changes) to ring buffer for analysis.

```csharp
/// <summary>
/// Telemetry event for movement debugging and replay reconstruction.
/// Logged to ring buffer (1000 most recent events per agent).
///
/// Usage:
///   - Debug visualization in editor (draw movement path, state timeline)
///   - Replay reconstruction (deterministic replay from event stream)
///   - Performance profiling (identify expensive frames)
///   - Regression testing (compare event streams across versions)
///
/// Ring buffer tuning: 1000 events at 60Hz = ~16.7 seconds of history per agent.
/// 22 agents Ã— 1000 events Ã— ~24 bytes/event â‰ˆ 528 KB total memory footprint.
/// This is acceptable for Stage 0 debugging. Stage 1 may optimize by:
///   - Reducing buffer size to 500 events (8 seconds, 264 KB)
///   - Selective logging (state transitions only, not every frame)
///   - Compression for replay storage (delta encoding, RLE)
/// Performance impact measured in Section 6 (Performance Analysis).
///
/// FIXED v1.1: Message field changed from string â†’ MessageCode enum to eliminate
/// heap allocations. At 60Hz Ã— 22 agents, string allocations were causing 1320
/// allocations/second = significant GC pressure. Enum codes are zero-cost.
/// </summary>
public struct MovementTelemetryEvent
{
    /// <summary>Timestamp (match time in seconds).</summary>
    public float Timestamp;

    /// <summary>Agent ID (0-21).</summary>
    public int AgentID;

    /// <summary>Event type.</summary>
    public MovementEventType EventType;

    /// <summary>Agent position when event occurred.</summary>
    public Vector3 Position;

    /// <summary>Agent velocity when event occurred.</summary>
    public Vector3 Velocity;

    /// <summary>Movement state when event occurred.</summary>
    public AgentMovementState State;

    /// <summary>Movement command active when event occurred.</summary>
    public MovementCommand Command;

    /// <summary>
    /// Additional context (state-dependent).
    /// Examples: stumble probability, turn rate, lean angle.
    /// </summary>
    public float ContextValue;

    /// <summary>
    /// Debug message code (FIXED v1.1: enum instead of string).
    /// Predefined codes eliminate heap allocations.
    /// See MessageCode enum for available codes.
    /// </summary>
    public MessageCode Message;
}

/// <summary>
/// Types of movement events logged to telemetry.
/// </summary>
public enum MovementEventType
{
    FRAME_UPDATE,           // Regular frame tick (logged every frame if verbose mode)
    STATE_TRANSITION,       // Changed movement state
    STUMBLE_OCCURRED,       // Stumble risk check triggered stumble
    EMERGENCY_DECEL,        // Emergency deceleration command executed
    VELOCITY_CLAMPED,       // Velocity exceeded MAX_SPEED and was clamped
    NUMERICAL_RECOVERY,     // NaN/Infinity detected and recovered
    COMMAND_IGNORED,        // Command rejected (e.g., commanded SPRINT during STUMBLING)
    SAFETY_OVERRIDE,        // OverrideSafetyConstraints flag was true
    STATE_LOCK_ACTIVATED,   // Oscillation detected, state locked
    COLLISION_IMPACT        // Forward reference â€” logged by Collision System (Spec #3)
}

/// <summary>
/// ADDED v1.1: Predefined debug message codes for telemetry.
/// Replaces heap-allocated strings with zero-cost enum values.
/// Reduces GC pressure from 1320 allocations/second to zero.
///
/// Usage: event.Message = MessageCode.STUMBLE_RISK_TRIGGERED;
/// In debug UI, convert to string via MessageCode.ToString() only when displayed.
/// </summary>
public enum MessageCode
{
    NONE,                           // No message (default)
    
    // State transitions
    IDLE_TO_WALKING,
    WALKING_TO_JOGGING,
    JOGGING_TO_SPRINTING,
    SPRINTING_TO_JOGGING,
    JOGGING_TO_WALKING,
    WALKING_TO_IDLE,
    TRANSITION_TO_DECELERATING,
    TRANSITION_TO_STUMBLING,
    TRANSITION_TO_GROUNDED,
    GROUNDED_RECOVERY_COMPLETE,
    STUMBLE_RECOVERY_COMPLETE,
    
    // Command rejections
    SPRINT_REJECTED_STUMBLING,
    SPRINT_REJECTED_STAMINA,
    JOG_REJECTED_STAMINA,
    COMMAND_REJECTED_STATE_LOCK,
    
    // Stumble system
    STUMBLE_RISK_TRIGGERED,
    STUMBLE_PROBABILITY_CALCULATED,
    STUMBLE_ROLL_FAILED_SAFE,
    STUMBLE_ROLL_SUCCEEDED_FELL,
    
    // Deceleration
    EMERGENCY_DECEL_REQUESTED,
    CONTROLLED_DECEL_ACTIVE,
    DECEL_COMPLETE_STOPPED,
    
    // Safety systems
    VELOCITY_CLAMPED_MAX_SPEED,
    NAN_DETECTED_POSITION,
    NAN_DETECTED_VELOCITY,
    INFINITY_DETECTED,
    RECOVERED_FROM_INVALID_STATE,
    OSCILLATION_DETECTED_LOCKED,
    
    // Turn rate
    TURN_RATE_CLAMPED,
    STRAFE_PENALTY_APPLIED,
    TURN_RATE_SAFE_NO_STUMBLE,
    
    // Fatigue gates
    SPRINT_GATE_BLOCKED,
    JOG_GATE_BLOCKED,
    FORCED_DECEL_STAMINA,
    
    // Collision triggers
    GROUNDED_BY_COLLISION,
    GROUNDED_BY_SLIDE_TACKLE,
    GROUNDED_BY_DIVING_HEADER,
    
    // Safety overrides
    SAFETY_OVERRIDE_ACTIVE,
    INSTANT_TURN_ALLOWED,
    STATE_VALIDATION_BYPASSED,
    
    // Performance
    FRAME_TIME_EXCEEDED_BUDGET,
    CACHE_INVALIDATION,
    
    // Debug markers
    BREAKPOINT_HIT,
    MANUAL_EVENT_MARKER,
    TEST_CASE_BOUNDARY
}
```

---

## End of Section 3.5

**Page Count:** ~22 pages  
**Next Section:** Section 4 â€” Implementation Details (Code Organization, Dependencies, Configuration)

---

## CRITICAL CROSS-REFERENCES

This section defines all data structures referenced in:
- Section 3.1 (State Machine) â†’ `AgentMovementState`, `GroundedReason`, `Agent.CurrentState`, `Agent.TimeInState`
- Section 3.2 (Locomotion) â†’ `PerformanceContext`, `Agent.Velocity`, `Agent.Speed`
- Section 3.3 (Directional Movement) â†’ `FacingMode`, `Agent.FacingDirection`, `MovementCommand.FacingMode`
- Section 3.4 (Turning) â†’ `Agent.CurrentTurnRate`, `Agent.LeanAngle`, `Agent.IsInStumbleRiskZone`

All subsequent specifications that interact with agent movement (Collision System, Pass Mechanics, First Touch, etc.) must use these defined structures as the interface contract.

---

## IMPLEMENTATION NOTES

1. **Memory Layout Optimization (Deferred to Stage 1):**
   - Agent struct is currently class-based for ease of development
   - Stage 1 may convert to struct + ECS pattern for cache efficiency
   - Current design supports either approach without API changes
   - See Agent class documentation for ECS refactoring considerations

2. **Fixed64 Support (Deferred to Stage 5):**
   - All floats can be replaced with Fixed64 type via conditional compilation
   - Formulas written to avoid float-specific operations (no Mathf.Lerp, etc.)
   - Ensures deterministic simulation for multiplayer

3. **Animation Data Contract Future-Proofing:**
   - AnimationDataContract includes Stage 1+ fields (HasBallAtFeet, InjuryLevel) even though unused in Stage 0
   - Prevents breaking changes when Stage 1 animation system comes online
   - All fields default to safe values (false, 0.0) if not populated
   - ContractVersion field (always 1 in Stage 0) enables backward compatibility checking

4. **Collision Interface Immutability:**
   - AgentPhysicalProperties is regenerated every frame (not cached)
   - Prevents stale data if agent state changes mid-frame
   - Collision System must not store references to this struct across frames

5. **MovementCommand Extensibility:**
   - Current struct is minimum viable interface for Stage 0
   - Tactical AI systems (Spec #7, #12) may extend with additional fields
   - Factory methods cover common patterns but are not exhaustive
   - Extensions must maintain backward compatibility with existing consumers

6. **PlayerAttributes Cross-Spec Coordination (updated v1.3):**
   - v1.3 adds KickPower, WeakFootRating, Crossing per AM-002-001 (ERR-007 resolution)
   - Full attribute list remains provisional pending coordination with all 20 specs
   - Spec #20 (Code Standards) will create master attribute registry
   - Any attribute name changes after Week 4 require impact analysis
   - Each spec must document which attributes it consumes and their semantics

7. **Telemetry Ring Buffer Tuning:**
   - 1000 events/agent = ~16.7s history at 60Hz, ~528 KB for 22 agents
   - Acceptable for Stage 0 debugging, may optimize in Stage 1
   - Profiling in Section 6 will measure actual performance impact
   - Compression strategies available if memory becomes constraint

8. **GROUNDED Dwell Time Variable Formula (v1.1):**
   - Base 1.0s scaled by (Strength + Balance) / 40.0
   - Modified by GroundedReason: COLLISION (1.0x), SLIDING_TACKLE (0.6x), DIVING_HEADER (0.7x)
   - Collision force scaling applied only for involuntary knockdowns
   - Clamped to [0.6s, 2.5s] range
   - See Section 3.1.5.2 for full formula and examples

9. **STUMBLE Attribute Correction (v1.1, refined v1.2):**
   - Primary attribute is Balance (not Agility)
   - Dwell time range: 300-800ms (not 300-500ms)
   - Stumble probability uses fraction-based safe zone model per Section 3.4
   - Constants renamed and values aligned with Section 3.4 StumbleConstants

10. **Speed Caching Implementation (v1.1):**
    - Speed is now properly cached with dirty flag
    - Invalidated whenever Velocity changes
    - Documentation now matches actual implementation
    - Eliminates redundant sqrt() calls per frame

11. **Turn Rate Model Alignment (v1.2):**
    - Section 3.4 uses hyperbolic decay: Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)
    - All agents share 720Â°/s base rate at zero speed
    - Agility affects k_turn (0.35â€“0.78), which controls speed sensitivity
    - Balance applies as multiplicative modifier (0.85â€“1.0)
    - State-based multipliers from v1.1 were incorrect and have been removed
    - See Section 3.4.2 for full derivation and examples

12. **FacingMode Simplification (v1.2):**
    - Section 3.3 defines exactly two modes: AUTO_ALIGN and TARGET_LOCK
    - "Freeze facing" behavior achieved via TARGET_LOCK with zero-delta target
    - This eliminates redundant enum values while preserving all functionality
    - See Section 3.3.4.2 for complete facing update logic
