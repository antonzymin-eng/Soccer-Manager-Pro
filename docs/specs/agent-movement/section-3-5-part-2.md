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

