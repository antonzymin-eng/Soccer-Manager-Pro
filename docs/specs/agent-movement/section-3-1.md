# Agent Movement Specification - Section 3.1: Movement State Machine

**Created:** February 9, 2026, 12:30 AM PST  
**Version:** 1.2
**Status:** Draft (Revised)
**Dependencies:** Ball Physics Spec #1 (state machine pattern reference), Section 3.2 (Locomotion)

**Updated:** March 4, 2026, 12:00 AM PST

---

## v1.1 Changelog

**CRITICAL FIX:**

1. **Piecewise fatigue model adopted (Appendix A.6.3 resolution):** Replaced linear
   aerobic modifier formula with piecewise model from Section 3.2.4. Changed
   `AEROBIC_MODIFIER_FLOOR` from 0.4 to 0.70, added `AEROBIC_MODIFIER_THRESHOLD` = 0.5.

---


## v1.2 Changelog (March 4, 2026)

**CRITICAL FIXES:**

1. **STUMBLE_SPEED_THRESHOLD corrected from 5.5 to 2.2 m/s:** Section 3.4 (authoritative for stumble mechanics) activates stumble risk above JOG_ENTER (2.2 m/s). Section 3.5 v1.2 correctly updated this value, but Section 3.1 was never updated from its original 5.5f. The comment "Set to SPRINT_EXIT" was incorrect per Section 3.4 design: stumble risk should activate during jogging, not only during sprinting.

2. **STUMBLE_TURN_ANGLE deprecated:** Section 3.4 uses a fraction-based safe zone model (SAFE_FRACTION_MIN/MAX), not an absolute angle threshold. The 60-degree constant was vestigial from an earlier design. Replaced with reference to Section 3.4.4 authoritative stumble trigger model. Constant retained at 60.0f for backward compatibility but marked deprecated with redirect.

---


## 3.1 Movement State Machine

### 3.1.1 Design Philosophy

The agent movement state machine governs which physics formulas apply at any given moment and constrains which transitions are physically plausible. It mirrors the architectural pattern established in Ball Physics Spec #1:

- **Hysteresis on all speed-based transitions** Ã¢â‚¬â€ separate ENTER and EXIT thresholds prevent oscillation when an agent hovers near a speed boundary
- **State determines physics** Ã¢â‚¬â€ each state activates a specific set of movement formulas (Section 3.2) and constraints
- **External commands filtered through state** Ã¢â‚¬â€ higher-level systems (AI, tactical) issue movement *requests*; the state machine determines what is physically achievable from the current state
- **Minimum dwell time** Ã¢â‚¬â€ certain states enforce a minimum duration before transition is permitted (e.g., STUMBLING requires recovery time)

### 3.1.2 State Definitions

```csharp
/// <summary>
/// Agent movement state machine states.
/// Determines which locomotion physics are applied and what transitions are valid.
/// 
/// Design note: States describe PHYSICAL condition, not intent.
/// An agent "wanting to sprint" but physically decelerating is in DECELERATING, not SPRINTING.
/// Intent is handled by the command layer (Section 4), not the state machine.
/// </summary>
public enum AgentMovementState
{
    /// <summary>
    /// Agent stationary, no active movement.
    /// Physics: No locomotion forces applied. Facing direction maintained.
    /// Entry: Speed drops below IDLE_ENTER threshold (0.1 m/s).
    /// Typical duration: Variable (waiting for ball, set piece, etc.)
    /// </summary>
    IDLE,

    /// <summary>
    /// Low-speed locomotion. Full directional freedom.
    /// Physics: Linear acceleration, unrestricted turning.
    /// Entry: Speed exceeds IDLE_EXIT threshold (0.3 m/s) but below JOG_ENTER.
    /// Typical speed range: 0.3Ã¢â‚¬â€œ2.0 m/s
    /// Real-world equivalent: Walking pace, positional adjustment.
    /// </summary>
    WALKING,

    /// <summary>
    /// Medium-speed locomotion. Good directional control.
    /// Physics: Exponential acceleration curve, moderate turning constraints.
    /// Entry: Speed exceeds JOG_ENTER threshold (2.2 m/s).
    /// Typical speed range: 2.0Ã¢â‚¬â€œ5.5 m/s
    /// Real-world equivalent: Defensive jog, loose marking, recovery run.
    /// </summary>
    JOGGING,

    /// <summary>
    /// Maximum-effort locomotion. Reduced turning ability.
    /// Physics: Full exponential acceleration, tight turning radius constraint.
    /// Entry: Speed exceeds SPRINT_ENTER threshold (5.8 m/s).
    /// Typical speed range: 5.5Ã¢â‚¬â€œ10.2 m/s
    /// Real-world equivalent: Chasing through ball, counter-attack, pressing trigger.
    /// Fatigue cost: Highest. Sprint duration limited by stamina.
    /// </summary>
    SPRINTING,

    /// <summary>
    /// Active speed reduction. Agent is braking.
    /// Physics: Deceleration curves applied (controlled or emergency).
    /// Entry: Movement command requests lower speed than current, AND current speed > WALK threshold.
    /// Typical duration: 0.3Ã¢â‚¬â€œ1.5 seconds depending on initial speed and mode.
    /// </summary>
    DECELERATING,

    /// <summary>
    /// Loss of balance during sharp maneuver or collision aftermath.
    /// Physics: Reduced control, momentum-driven movement, no voluntary direction change.
    /// Entry: Turn angle exceeds safe threshold at current speed, OR collision knockback.
    /// Minimum dwell time: 300Ã¢â‚¬â€œ800ms (attribute-dependent, Balance primary).
    /// Fatigue cost: Moderate (recovery effort).
    /// </summary>
    STUMBLING,

    /// <summary>
    /// Agent on the ground after fall or heavy collision.
    /// Physics: No locomotion. Position fixed. Recovery timer active.
    /// Entry: Collision force exceeds knockdown threshold, OR stumble at extreme speed.
    /// Minimum dwell time: 600Ã¢â‚¬â€œ2500ms (attribute-dependent, Strength + Balance,
    ///   modified by GroundedReason).
    /// Note: Collision System (Spec #3) triggers this state; this spec defines recovery.
    /// </summary>
    GROUNDED
}

/// <summary>
/// Reason an agent entered the GROUNDED state.
/// Affects recovery dwell time Ã¢â‚¬â€ voluntary actions recover faster
/// because the agent controlled their landing.
///
/// COLLISION: Triggered by Collision System (Spec #3). Involuntary.
/// SLIDING_TACKLE: Triggered by Collision System (Spec #3). Voluntary.
///   Agent chose to slide; recovery is faster (controlled landing).
/// DIVING_HEADER: Triggered by Heading Mechanics (Spec #9). Voluntary.
///   Agent dove for header; recovery is moderate (partial control).
/// </summary>
public enum GroundedReason
{
    COLLISION,
    SLIDING_TACKLE,
    DIVING_HEADER
}
```

### 3.1.3 Speed Thresholds with Hysteresis

All speed-based transitions use separate ENTER and EXIT thresholds. The gap between them is the **dead zone** Ã¢â‚¬â€ within this range, the agent remains in its current state regardless of speed.

```csharp
/// <summary>
/// Movement state machine thresholds.
/// All values in m/s.
///
/// Hysteresis rationale: Without dead zones, an agent accelerating through
/// a boundary (e.g., 2.0 m/s walkÃ¢â€ â€™jog) would oscillate between states
/// on consecutive frames due to floating-point jitter and acceleration
/// curve discretization at 60Hz.
///
/// Dead zone width: 0.2Ã¢â‚¬â€œ0.3 m/s chosen to span ~3Ã¢â‚¬â€œ5 frames of typical
/// acceleration, preventing oscillation while remaining imperceptible
/// to observers.
/// </summary>
public static class MovementThresholds
{
    // ================================================================
    // IDLE Ã¢â€ â€ WALKING
    // ================================================================

    /// <summary>Speed below which agent enters IDLE (m/s)</summary>
    /// <remarks>
    /// Agent must slow to near-zero before considered stationary.
    /// Matches Ball Physics MIN_VELOCITY pattern (0.1 m/s).
    /// </remarks>
    public const float IDLE_ENTER = 0.1f;

    /// <summary>Speed above which agent exits IDLE to WALKING (m/s)</summary>
    /// <remarks>
    /// Dead zone: 0.1Ã¢â‚¬â€œ0.3 m/s. Agent in IDLE stays IDLE until clearly moving.
    /// Prevents micro-movements from triggering walking animations.
    /// </remarks>
    public const float IDLE_EXIT = 0.3f;

    // ================================================================
    // WALKING Ã¢â€ â€ JOGGING
    // ================================================================

    /// <summary>Speed above which agent enters JOGGING from WALKING (m/s)</summary>
    /// <remarks>
    /// 2.2 m/s Ã¢â€°Ë† 7.9 km/h. Real-world walk-to-jog transition occurs
    /// at approximately 2.0Ã¢â‚¬â€œ2.5 m/s (Diedrich & Warren, 1995).
    /// </remarks>
    public const float JOG_ENTER = 2.2f;

    /// <summary>Speed below which agent exits JOGGING to WALKING (m/s)</summary>
    /// <remarks>
    /// Dead zone: 1.9Ã¢â‚¬â€œ2.2 m/s (0.3 m/s gap).
    /// Agent jogging at 2.0 m/s stays JOGGING; must drop to 1.9 to walk.
    /// </remarks>
    public const float JOG_EXIT = 1.9f;

    // ================================================================
    // JOGGING Ã¢â€ â€ SPRINTING
    // ================================================================

    /// <summary>Speed above which agent enters SPRINTING from JOGGING (m/s)</summary>
    /// <remarks>
    /// 5.8 m/s Ã¢â€°Ë† 20.9 km/h. Below professional sprint speed (~25Ã¢â‚¬â€œ36 km/h)
    /// but above casual running. This threshold marks the point where
    /// turning constraints become significant and fatigue cost increases.
    /// </remarks>
    public const float SPRINT_ENTER = 5.8f;

    /// <summary>Speed below which agent exits SPRINTING to JOGGING (m/s)</summary>
    /// <remarks>
    /// Dead zone: 5.5Ã¢â‚¬â€œ5.8 m/s (0.3 m/s gap).
    /// Agent sprinting at 5.6 m/s stays SPRINTING; must drop to 5.5 to jog.
    /// </remarks>
    public const float SPRINT_EXIT = 5.5f;

    // ================================================================
    // STUMBLE & GROUNDED RECOVERY
    // ================================================================

    /// <summary>Minimum time in STUMBLING state before recovery (seconds)</summary>
    /// <remarks>
    /// Base value; actual dwell time = BASE / (Balance / 20.0).
    /// Balance 20: 300ms. Balance 10: 600ms. Balance 5: 1200ms.
    /// Clamped to range [300, 1500]ms.
    /// </remarks>
    public const float STUMBLE_MIN_DWELL_BASE = 0.6f;

    /// <summary>Minimum time in GROUNDED state before recovery (seconds)</summary>
    /// <remarks>
    /// Base value for collision knockdowns; actual dwell time varies by reason.
    /// Collision: dwell = BASE / ((Strength + Balance) / 40.0).
    /// Voluntary slide: dwell = BASE Ãƒâ€” 0.6 (faster recovery, controlled landing).
    /// Diving header: dwell = BASE Ãƒâ€” 0.7 (partial control on landing).
    ///
    /// Collision examples at BASE = 1.0:
    ///   Elite (S+B=40): 1.0 / 1.0 = 1.0s
    ///   Average (S+B=24): 1.0 / 0.6 = 1.67s
    ///   Low (S+B=10): 1.0 / 0.25 = 4.0s Ã¢â€ â€™ clamped to 2.5s
    /// Clamped to range [0.6, 2.5]s.
    ///
    /// Validation: professional players typically regain feet in 0.8Ã¢â‚¬â€œ1.5s
    /// after non-injury knockdowns. Elite values (1.0s) match this range.
    /// </remarks>
    public const float GROUNDED_MIN_DWELL_BASE = 1.0f;

    // ================================================================
    // STUMBLE TRIGGER THRESHOLDS
    // ================================================================

    /// <summary>
    /// DEPRECATED v1.2: Section 3.4 uses fraction-based safe zone model
    /// (SAFE_FRACTION_MIN/MAX = 0.55/0.85 of max turn rate), not an absolute
    /// angle threshold. Retained for backward compatibility only.
    /// See Section 3.4.4 for authoritative stumble trigger model.
    /// </summary>
    /// <remarks>
    /// At sprint speed, attempting to turn more than 60Ã‚Â° in a single
    /// physics frame risks loss of balance. Actual stumble probability
    /// is modulated by Agility and Balance (Section 3.4).
    /// Below sprint speed, sharper turns are permitted without stumble risk.
    /// </remarks>
    public const float STUMBLE_TURN_ANGLE = 60.0f;

    /// <summary>
    /// Speed (m/s) above which sharp turns incur stumble risk.
    /// Set to JOG_ENTER per Section 3.4: at walking pace, players have enough
    /// time and ground contact to recover from any direction change.
    /// FIXED v1.2: Changed from 5.5 (SPRINT_EXIT) to 2.2 (JOG_ENTER)
    /// to match Section 3.4 authoritative stumble mechanics.
    /// </summary>
    public const float STUMBLE_SPEED_THRESHOLD = 2.2f;

    /// <summary>
    /// Minimum stumble risk floor applied regardless of calculation.
    /// Ensures elite players (Agility 20 + Balance 20) can still stumble
    /// on extreme maneuvers (e.g., 140Ã‚Â° cut at 9.5 m/s).
    /// 
    /// With resistance = 1.0 for elite players, this floor means stumble
    /// only triggers when raw stumble_risk also exceeds 1.0, which requires
    /// approximately: speed > 9 m/s AND turnAngle > 120Ã‚Â°.
    /// This is intentionally rare but not impossible.
    /// </summary>
    public const float MIN_STUMBLE_RISK = 0.03f;

    // ================================================================
    // SAFETY LIMITS
    // ================================================================

    /// <summary>Maximum agent speed under any conditions (m/s)</summary>
    /// <remarks>
    /// 12.0 m/s Ã¢â€°Ë† 43.2 km/h. Exceeds fastest recorded footballer sprint
    /// (MbappÃƒÂ© ~38 km/h Ã¢â€°Ë† 10.6 m/s) by safe margin. Any speed above this
    /// indicates a physics error. Consistent with FR-2.4 failure recovery.
    /// </remarks>
    public const float MAX_SPEED = 12.0f;

    /// <summary>Maximum turn rate under any conditions (degrees/second)</summary>
    /// <remarks>
    /// 720Ã‚Â°/s = 2 full rotations per second. Only reachable at walking speed.
    /// Sprint turn rate is much lower (~120Ã¢â‚¬â€œ180Ã‚Â°/s depending on agility).
    /// </remarks>
    public const float MAX_TURN_RATE = 720.0f;

    /// <summary>Maximum state transitions per second before oscillation guard triggers</summary>
    public const int MAX_TRANSITIONS_PER_SECOND = 6;

    // ================================================================
    // DUAL-ENERGY FATIGUE MODEL
    // ================================================================
    //
    // Agents operate on two independent energy pools:
    //
    // 1. AEROBIC POOL (match fitness)
    //    - Range: 1.0 (fresh) Ã¢â€ â€™ 0.0 (completely spent)
    //    - Drains slowly throughout the match regardless of activity
    //    - Drain rate scaled by Stamina attribute (high stamina = slower drain)
    //    - Minimal recovery during play (only meaningful recovery at half-time)
    //    - Affects: top speed, acceleration rate, turn rate (all via modifier)
    //    - Think: "85th minute legs" Ã¢â‚¬â€ everything is slower
    //
    // 2. SPRINT RESERVOIR (burst capacity)
    //    - Range: 1.0 (full) Ã¢â€ â€™ 0.0 (empty)
    //    - Drains FAST during SPRINTING state
    //    - RECOVERS during low-intensity states (IDLE, WALKING, slow JOGGING)
    //    - Drain/recovery rates scaled by Stamina attribute
    //    - Affects: ability to ENTER/MAINTAIN sprint state (hard gates below)
    //    - Think: "anaerobic capacity" Ã¢â‚¬â€ sprint, rest, sprint again
    //
    // The two pools interact:
    //    - Aerobic depletion reduces sprint reservoir recovery rate
    //      (tired players take longer to "catch their breath")
    //    - Formula: effective_recovery = base_recovery Ãƒâ€” aerobic_pool
    //    - At aerobic 0.5 (half-time tired), sprint recovery is halved
    //
    // This spec defines the RATES and GATES.
    // The actual pool values are maintained externally (Match Simulator)
    // and passed into movement system each frame.
    // Full stamina calculation formulas are in Section 3.5.

    // Ã¢â€â‚¬Ã¢â€â‚¬ Sprint Reservoir Gates Ã¢â€â‚¬Ã¢â€â‚¬

    /// <summary>
    /// Sprint reservoir level below which sprinting is physically impossible.
    /// Agent is forced from SPRINTING Ã¢â€ â€™ JOGGING regardless of command.
    ///
    /// At 0.20 (20% remaining), the anaerobic system is depleted.
    /// The agent's legs physically cannot sustain sprint-effort output.
    /// This is the EMERGENCY CUTOFF after gradual degradation.
    ///
    /// Important: top speed is continuously reduced as reservoir depletes
    /// (Section 3.2). A sprinter at reservoir 0.30 is already noticeably
    /// slower than at 1.0. The floor is the hard gate where the body
    /// refuses entirely Ã¢â‚¬â€ it should rarely be the first thing the player
    /// notices. The gradual slowdown is the primary feedback mechanism.
    ///
    /// Recovery: at IDLE/WALKING drain rates, returns above gate in ~8-12s.
    /// This models the real "hands on knees, catch breath" recovery window.
    /// </summary>
    public const float SPRINT_RESERVOIR_FLOOR = 0.20f;

    /// <summary>
    /// Sprint reservoir level required to ENTER sprint state.
    /// Higher than FLOOR to prevent immediate re-entry after hitting the wall.
    ///
    /// Hysteresis: must recover to 0.35 before sprinting again.
    /// Gap (0.35 - 0.20 = 0.15) ensures ~4-6 seconds of non-sprint recovery
    /// before the agent can sprint again, matching real-world patterns.
    /// </summary>
    public const float SPRINT_RESERVOIR_REENTRY = 0.35f;

    /// <summary>
    /// Aerobic pool level below which jogging becomes unsustainable.
    /// Agent forced to WALKING. Only triggers in extreme late-match scenarios.
    ///
    /// At 0.15 aerobic, the player is completely spent Ã¢â‚¬â€ think: extra time
    /// after 120 minutes, or a player with very low Stamina attribute.
    /// </summary>
    public const float AEROBIC_JOG_FLOOR = 0.15f;

    // Ã¢â€â‚¬Ã¢â€â‚¬ Drain Rates (per second, at Stamina 10 baseline) Ã¢â€â‚¬Ã¢â€â‚¬

    /// <summary>
    /// Sprint reservoir drain rate while SPRINTING (per second).
    /// At Stamina 10: reservoir drains from 1.0 to 0.0 in ~12 seconds.
    /// At Stamina 20: ~18 seconds (elite endurance sprinter).
    /// At Stamina 5: ~8 seconds (poor stamina).
    ///
    /// Formula: actual_drain = BASE_DRAIN / (stamina / 10.0)
    /// </summary>
    public const float SPRINT_DRAIN_RATE = 0.083f;  // 1.0 / 12s

    /// <summary>
    /// Aerobic pool drain rate (per second). Always active during match.
    /// At Stamina 10: pool drains from 1.0 to 0.0 over ~90 minutes (5400s).
    /// At Stamina 20: ~120 minutes (elite, stays fresh deep into extra time).
    /// At Stamina 5: ~60 minutes (visibly fatigued by 60th minute).
    ///
    /// Formula: actual_drain = BASE_DRAIN / (stamina / 10.0)
    ///
    /// Substitution handling: a substitute entering at the 70th minute
    /// starts with aerobic_pool = 1.0 (fresh). This is correct behavior.
    /// The Match Simulator (not this spec) owns substitution resets and
    /// must initialize both energy pools for incoming players.
    /// </summary>
    public const float AEROBIC_DRAIN_RATE = 0.000185f;  // 1.0 / 5400s

    // Ã¢â€â‚¬Ã¢â€â‚¬ Recovery Rates (per second, at Stamina 10 baseline) Ã¢â€â‚¬Ã¢â€â‚¬

    /// <summary>
    /// Sprint reservoir recovery rate while IDLE (per second).
    /// At Stamina 10: recovers from 0.0 to 1.0 in ~20 seconds.
    /// Modified by aerobic pool: effective = base Ãƒâ€” aerobic_pool.
    /// Late-match (aerobic 0.5): recovery takes ~40 seconds.
    ///
    /// Formula: actual_recovery = BASE_RECOVERY Ãƒâ€” (stamina / 10.0) Ãƒâ€” aerobic_pool
    /// </summary>
    public const float SPRINT_RECOVERY_IDLE = 0.05f;  // 1.0 / 20s

    /// <summary>
    /// Sprint reservoir recovery rate while WALKING (per second).
    /// Slightly slower than IDLE (still moving, not fully resting).
    /// </summary>
    public const float SPRINT_RECOVERY_WALKING = 0.04f;  // 1.0 / 25s

    /// <summary>
    /// Sprint reservoir recovery rate while JOGGING (per second).
    /// Slow recovery Ã¢â‚¬â€ aerobic system is engaged, limited anaerobic recovery.
    /// At Stamina 10: recovers from 0.0 to 1.0 in ~50 seconds while jogging.
    /// </summary>
    public const float SPRINT_RECOVERY_JOGGING = 0.02f;  // 1.0 / 50s

    /// <summary>
    /// Sprint reservoir drain/recovery while SPRINTING: drain only, no recovery.
    /// While DECELERATING: recovery at JOGGING rate (still high effort).
    /// While STUMBLING/GROUNDED: recovery at WALKING rate (involuntary rest).
    /// </summary>

    // Ã¢â€â‚¬Ã¢â€â‚¬ State-to-Rate Mapping Ã¢â€â‚¬Ã¢â€â‚¬
    //
    // State        Ã¢â€â€š Sprint Reservoir Ã¢â€â€š Aerobic Pool Ã¢â€â€š Notes
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    // IDLE         Ã¢â€â€š +RECOVERY_IDLE   Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Best recovery
    // WALKING      Ã¢â€â€š +RECOVERY_WALK   Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Good recovery
    // JOGGING      Ã¢â€â€š +RECOVERY_JOG    Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Slow recovery
    // SPRINTING    Ã¢â€â€š -SPRINT_DRAIN    Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Fast depletion
    // DECELERATING Ã¢â€â€š +RECOVERY_JOG    Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Still high effort
    // STUMBLING    Ã¢â€â€š +RECOVERY_WALK   Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Involuntary rest
    // GROUNDED     Ã¢â€â€š +RECOVERY_WALK   Ã¢â€â€š -DRAIN_RATE  Ã¢â€â€š Involuntary rest

    // -- Aerobic Modifier Application (v1.1: PIECEWISE MODEL) --

    /// <summary>
    /// Aerobic pool threshold below which movement penalties begin.
    /// Above this threshold, no penalty is applied (modifier = 1.0).
    /// CROSS-REFERENCE: Section 3.2.4 AerobicPoolToSpeedModifier() is authoritative.
    /// </summary>
    public const float AEROBIC_MODIFIER_THRESHOLD = 0.50f;

    /// <summary>
    /// Minimum movement modifier at complete aerobic exhaustion (pool = 0.0).
    /// Agent retains 70% of movement capability even when completely spent.
    /// v1.1 CHANGE: Increased from 0.4 to 0.70 per Appendix A.6.3 resolution.
    /// 
    /// Worked examples (piecewise model):
    ///   Pool 1.0: modifier = 1.00 (fresh)
    ///   Pool 0.5: modifier = 1.00 (threshold - no penalty yet)
    ///   Pool 0.3: modifier = 0.88 (12% reduction)
    ///   Pool 0.0: modifier = 0.70 (30% reduction - floor)
    /// </summary>
    public const float AEROBIC_MODIFIER_FLOOR = 0.70f;

    // COLLISION FORCE SCALING
    // ================================================================

    /// <summary>
    /// Collision force is normalized to 0.0Ã¢â‚¬â€œ1.0 by Collision System (Spec #3).
    /// This value scales GROUNDED dwell time for collision knockdowns.
    ///
    /// Reference force levels (defined by Spec #3, listed here for context):
    ///   0.0Ã¢â‚¬â€œ0.2: Light contact (shoulder nudge) Ã¢â‚¬â€ rarely causes GROUNDED
    ///   0.2Ã¢â‚¬â€œ0.5: Medium contact (hip check, standing tackle) Ã¢â‚¬â€ may cause GROUNDED
    ///   0.5Ã¢â‚¬â€œ0.8: Heavy contact (full sliding tackle, high-speed collision)
    ///   0.8Ã¢â‚¬â€œ1.0: Extreme (full-speed head-on, reckless challenge)
    ///
    /// Dwell scaling formula:
    ///   scaled_dwell = base_dwell Ãƒâ€” (COLLISION_DWELL_MIN + (1.0 - COLLISION_DWELL_MIN) Ãƒâ€” collisionForce)
    ///
    /// At force 0.0: dwell Ãƒâ€” 0.65 (light knockdown, fast recovery)
    /// At force 0.5: dwell Ãƒâ€” 0.825
    /// At force 1.0: dwell Ãƒâ€” 1.0 (full calculated dwell, worst case)
    ///
    /// This means the attribute-based dwell (Section 3.1.5) represents the
    /// MAXIMUM recovery time for the heaviest possible collision. Lighter
    /// collisions recover proportionally faster. The 0.65 floor ensures
    /// that any collision forceful enough to trigger GROUNDED (determined
    /// by Spec #3's knockdown threshold) still produces a meaningful
    /// recovery period Ã¢â‚¬â€ if the force was truly negligible, Spec #3
    /// should not have triggered GROUNDED in the first place.
    /// </summary>
    public const float COLLISION_DWELL_MIN = 0.65f;
}
```

**Hysteresis Diagram:**

```
  Speed (m/s)
    Ã¢â€â€š
 10.2 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  Elite top speed (Pace 20)
    Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
    Ã¢â€â€š                  Ã¢â€â€š       SPRINTING          Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€š  (full accel, tight turn) Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
  5.8 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  SPRINT_ENTER Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â² (enter sprint)
    Ã¢â€â€š     dead zone
  5.5 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  SPRINT_EXIT  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â¼ (exit sprint)
    Ã¢â€â€š                  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
    Ã¢â€â€š                  Ã¢â€â€š       JOGGING            Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€š (exp accel, moderate turn)Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
  2.2 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  JOG_ENTER   Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â² (enter jog)
    Ã¢â€â€š     dead zone
  1.9 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  JOG_EXIT    Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â¼ (exit jog)
    Ã¢â€â€š                  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
    Ã¢â€â€š                  Ã¢â€â€š       WALKING            Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€š (linear accel, free turn) Ã¢â€â€š
    Ã¢â€â€š                  Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
  0.3 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  IDLE_EXIT   Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â² (exit idle)
    Ã¢â€â€š     dead zone
  0.1 Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬ Ã¢â€â‚¬  IDLE_ENTER  Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬ Ã¢â€“Â¼ (enter idle)
    Ã¢â€â€š                  Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
  0.0                  Ã¢â€â€š         IDLE             Ã¢â€â€š
                       Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
```

### 3.1.4 Transition Table

All valid state transitions with conditions. Invalid transitions (not listed) are rejected by the state machine.

```
Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
Ã¢â€â€š FROM         Ã¢â€â€š TO           Ã¢â€â€š CONDITION                                       Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š IDLE         Ã¢â€â€š WALKING      Ã¢â€â€š speed > IDLE_EXIT (0.3 m/s)                     Ã¢â€â€š
Ã¢â€â€š IDLE         Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š WALKING      Ã¢â€â€š IDLE         Ã¢â€â€š speed < IDLE_ENTER (0.1 m/s)                    Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š JOGGING      Ã¢â€â€š speed > JOG_ENTER (2.2 m/s)                    Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š JOGGING      Ã¢â€â€š WALKING      Ã¢â€â€š speed < JOG_EXIT (1.9 m/s)                     Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š SPRINTING    Ã¢â€â€š speed > SPRINT_ENTER (5.8 m/s) AND             Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š sprintReservoir >= SPRINT_RESERVOIR_REENTRY     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š (0.35)                                          Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š DECELERATING Ã¢â€â€š Command requests speed < current AND            Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > JOG_EXIT                                Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š DECELERATING Ã¢â€â€š aerobicPool < AEROBIC_JOG_FLOOR (0.15)         Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š SPRINTING    Ã¢â€â€š JOGGING      Ã¢â€â€š speed < SPRINT_EXIT (5.5 m/s)                  Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š JOGGING      Ã¢â€â€š sprintReservoir < SPRINT_RESERVOIR_FLOOR (0.20) Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š DECELERATING Ã¢â€â€š Command requests speed < current                Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š STUMBLING    Ã¢â€â€š Turn angle > STUMBLE_TURN_ANGLE (60Ã‚Â°) AND      Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š stumble check fails (Section 3.4)               Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š DECELERATING Ã¢â€â€š IDLE         Ã¢â€â€š speed < IDLE_ENTER (0.1 m/s)                    Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š WALKING      Ã¢â€â€š speed < JOG_EXIT AND speed > IDLE_EXIT          Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š JOGGING      Ã¢â€â€š Command requests acceleration AND               Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > JOG_EXIT                                Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š SPRINTING    Ã¢â€â€š Command requests acceleration AND               Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š speed > SPRINT_ENTER                            Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š STUMBLING    Ã¢â€â€š Emergency decel at speed > STUMBLE_SPEED AND   Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š stumble check fails                             Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š GROUNDED     Ã¢â€â€š Collision knockdown (external trigger)          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š STUMBLING    Ã¢â€â€š IDLE         Ã¢â€â€š Dwell time elapsed AND speed < IDLE_ENTER       Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š WALKING      Ã¢â€â€š Dwell time elapsed AND speed < JOG_EXIT         Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š JOGGING      Ã¢â€â€š Dwell time elapsed AND speed > JOG_EXIT         Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š GROUNDED     Ã¢â€â€š Speed > STUMBLE_SPEED at stumble start          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š AND secondary stumble check fails               Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š GROUNDED     Ã¢â€â€š IDLE         Ã¢â€â€š Recovery dwell time elapsed                     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š              Ã¢â€â€š (agent always returns to IDLE after grounded)   Ã¢â€â€š
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
```

**Forbidden Transitions (enforced):**
- IDLE Ã¢â€ â€™ SPRINTING (must pass through WALKING Ã¢â€ â€™ JOGGING first)
- IDLE Ã¢â€ â€™ JOGGING (must pass through WALKING first)
- WALKING Ã¢â€ â€™ SPRINTING (must pass through JOGGING first)
- GROUNDED Ã¢â€ â€™ any state except IDLE (must recover fully)
- STUMBLING Ã¢â€ â€™ SPRINTING (must stabilize before sprinting again)
- Any state Ã¢â€ â€™ STUMBLING without speed > STUMBLE_SPEED_THRESHOLD (low-speed turns never cause stumbles)
- Any state Ã¢â€ â€™ SPRINTING when sprintReservoir < SPRINT_RESERVOIR_REENTRY (must recover)
- JOGGING Ã¢â€ â€™ JOGGING when aerobicPool < AEROBIC_JOG_FLOOR (forced deceleration)

### 3.1.5 State Transition Logic

```csharp
/// <summary>
/// Evaluates and applies movement state transitions.
/// Called once per physics frame (60Hz) BEFORE locomotion formulas.
///
/// Design: Returns new state without modifying agent directly.
/// Caller is responsible for applying state change.
/// Mirrors BallStateMachine pattern from Spec #1.
/// </summary>
public static class AgentStateMachine
{
    /// <summary>
    /// Evaluates the next movement state based on current conditions.
    /// </summary>
    /// <param name="current">Current movement state</param>
    /// <param name="speed">Current scalar speed (m/s)</param>
    /// <param name="commandSpeed">Requested target speed from AI/command layer (m/s)</param>
    /// <param name="turnAngle">Requested turn angle this frame (degrees)</param>
    /// <param name="dwellTimer">Time spent in current state (seconds)</param>
    /// <param name="balance">Agent Balance attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="agility">Agent Agility attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="strength">Agent Strength attribute (1Ã¢â‚¬â€œ20)</param>
    /// <param name="sprintReservoir">Current sprint reservoir level (1.0 = full, 0.0 = empty)</param>
    /// <param name="aerobicPool">Current aerobic pool level (1.0 = fresh, 0.0 = spent)</param>
    /// <param name="isCollisionKnockdown">External flag from Collision System</param>
    /// <param name="collisionForce">Normalized collision force (0.0Ã¢â‚¬â€œ1.0), only valid when isCollisionKnockdown=true</param>
    /// <returns>New state (may be same as current if no transition)</returns>
    public static AgentMovementState EvaluateState(
        AgentMovementState current,
        float speed,
        float commandSpeed,
        float turnAngle,
        float dwellTimer,
        int balance,
        int agility,
        int strength,
        float sprintReservoir,
        float aerobicPool,
        bool isCollisionKnockdown,
        float collisionForce = 0.0f)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 1: External collision knockdown overrides everything Ã¢â€â‚¬Ã¢â€â‚¬
        // Collision System (Spec #3) determines knockdown; we just respond.
        if (isCollisionKnockdown && current != AgentMovementState.GROUNDED)
        {
            return AgentMovementState.GROUNDED;
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 2: State-specific transition evaluation Ã¢â€â‚¬Ã¢â€â‚¬
        switch (current)
        {
            case AgentMovementState.IDLE:
                return EvaluateFromIdle(speed);

            case AgentMovementState.WALKING:
                return EvaluateFromWalking(speed);

            case AgentMovementState.JOGGING:
                return EvaluateFromJogging(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir, aerobicPool);

            case AgentMovementState.SPRINTING:
                return EvaluateFromSprinting(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir);

            case AgentMovementState.DECELERATING:
                return EvaluateFromDecelerating(speed, commandSpeed, turnAngle,
                    balance, agility, sprintReservoir, aerobicPool);

            case AgentMovementState.STUMBLING:
                return EvaluateFromStumbling(speed, dwellTimer, balance);

            case AgentMovementState.GROUNDED:
                return EvaluateFromGrounded(dwellTimer, balance, strength);

            default:
                // Safety: unknown state Ã¢â€ â€™ force IDLE
                return AgentMovementState.IDLE;
        }
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    // Per-state evaluation methods
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

    private static AgentMovementState EvaluateFromIdle(float speed)
    {
        if (speed > MovementThresholds.IDLE_EXIT)
            return AgentMovementState.WALKING;

        return AgentMovementState.IDLE;
    }

    private static AgentMovementState EvaluateFromWalking(float speed)
    {
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;

        if (speed > MovementThresholds.JOG_ENTER)
            return AgentMovementState.JOGGING;

        return AgentMovementState.WALKING;
    }

    private static AgentMovementState EvaluateFromJogging(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir, float aerobicPool)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Aerobic exhaustion: too spent to jog Ã¢â€â‚¬Ã¢â€â‚¬
        if (aerobicPool < MovementThresholds.AEROBIC_JOG_FLOOR)
            return AgentMovementState.DECELERATING;

        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        // Ã¢â€â‚¬Ã¢â€â‚¬ Sprint entry: requires sufficient sprint reservoir Ã¢â€â‚¬Ã¢â€â‚¬
        // Uses REENTRY threshold (higher than FLOOR) to enforce recovery gap
        if (speed > MovementThresholds.SPRINT_ENTER
            && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
            return AgentMovementState.SPRINTING;

        if (speed > MovementThresholds.SPRINT_ENTER
            && sprintReservoir < MovementThresholds.SPRINT_RESERVOIR_REENTRY)
            return AgentMovementState.JOGGING;  // Block sprint entry, reservoir too low

        if (commandSpeed < speed - 0.5f)  // Meaningful deceleration requested
            return AgentMovementState.DECELERATING;

        return AgentMovementState.JOGGING;
    }

    private static AgentMovementState EvaluateFromSprinting(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir)
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Priority 1: Sprint reservoir depleted Ã¢â€â‚¬Ã¢â€â‚¬
        // Agent physically cannot sustain sprint effort below this threshold.
        // This is the "hit the wall" moment Ã¢â‚¬â€ legs refuse to sprint.
        // Uses FLOOR (lower than REENTRY) so agent squeezes out every last
        // bit of sprint before being forced down. Re-entry requires recovering
        // back to REENTRY threshold (see EvaluateFromJogging).
        if (sprintReservoir < MovementThresholds.SPRINT_RESERVOIR_FLOOR)
            return AgentMovementState.JOGGING;

        if (speed < MovementThresholds.SPRINT_EXIT)
            return AgentMovementState.JOGGING;

        // Ã¢â€â‚¬Ã¢â€â‚¬ Stumble check for sharp turns at sprint speed Ã¢â€â‚¬Ã¢â€â‚¬
        if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
            && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD)
        {
            if (ShouldStumble(speed, turnAngle, balance, agility))
                return AgentMovementState.STUMBLING;
        }

        if (commandSpeed < speed - 0.5f)
            return AgentMovementState.DECELERATING;

        return AgentMovementState.SPRINTING;
    }

    private static AgentMovementState EvaluateFromDecelerating(
        float speed, float commandSpeed, float turnAngle,
        int balance, int agility, float sprintReservoir, float aerobicPool)
    {
        // Settled to idle
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;

        // Slowed to walking pace
        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        // Re-acceleration requested (energy-gated)
        if (commandSpeed > speed + 0.5f)
        {
            if (speed > MovementThresholds.SPRINT_ENTER
                && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
                return AgentMovementState.SPRINTING;
            if (speed > MovementThresholds.JOG_EXIT
                && aerobicPool >= MovementThresholds.AEROBIC_JOG_FLOOR)
                return AgentMovementState.JOGGING;
        }

        // Emergency decel stumble check
        if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
            && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD)
        {
            if (ShouldStumble(speed, turnAngle, balance, agility))
                return AgentMovementState.STUMBLING;
        }

        return AgentMovementState.DECELERATING;
    }

    private static AgentMovementState EvaluateFromStumbling(
        float speed, float dwellTimer, int balance)
    {
        float requiredDwell = CalculateStumbleDwell(balance);

        if (dwellTimer < requiredDwell)
            return AgentMovementState.STUMBLING;  // Still recovering

        // Recovery complete Ã¢â‚¬â€ transition based on residual speed
        if (speed < MovementThresholds.IDLE_ENTER)
            return AgentMovementState.IDLE;
        if (speed < MovementThresholds.JOG_EXIT)
            return AgentMovementState.WALKING;

        return AgentMovementState.JOGGING;  // Never straight to SPRINTING
    }

    private static AgentMovementState EvaluateFromGrounded(
        float dwellTimer, int balance, int strength)
    {
        float requiredDwell = CalculateGroundedDwell(balance, strength);

        if (dwellTimer < requiredDwell)
            return AgentMovementState.GROUNDED;  // Still recovering

        return AgentMovementState.IDLE;  // Always return to IDLE
    }

    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
    // Helper calculations
    // Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬

    /// <summary>
    /// Determines if a sharp turn at speed causes a stumble.
    /// 
    /// Stumble probability increases with:
    ///   - Higher speed (more momentum to redirect)
    ///   - Sharper turn angle (more lateral force required)
    /// Stumble probability decreases with:
    ///   - Higher Agility (better body control)
    ///   - Higher Balance (better stability)
    ///
    /// Formula:
    ///   stumble_risk = (speed / MAX_SPEED) Ãƒâ€” (turnAngle / 180) Ãƒâ€” difficulty_base
    ///   resistance   = ((agility + balance) / 40.0)
    ///   stumble      = stumble_risk > resistance
    ///
    /// This is a deterministic check, NOT random. Given identical inputs,
    /// the result is always the same (required for replay determinism).
    /// 
    /// Edge cases:
    ///   - Agility 20 + Balance 20 = resistance 1.0 Ã¢â€ â€™ almost never stumbles,
    ///     but MIN_STUMBLE_RISK (0.03) ensures extreme turns (>120Ã‚Â° at >9 m/s)
    ///     can still cause loss of balance even for elite players.
    ///   - Agility 1 + Balance 1 = resistance 0.05 Ã¢â€ â€™ stumbles on most
    ///     sharp turns at speed (physically ungifted player)
    /// </summary>
    private static bool ShouldStumble(
        float speed, float turnAngle, int balance, int agility)
    {
        float difficulty = 1.5f;  // Tunable base difficulty

        float stumbleRisk = (speed / MovementThresholds.MAX_SPEED)
                          * (turnAngle / 180.0f)
                          * difficulty;

        // Floor: even elite players have a minimum stumble risk on
        // extreme maneuvers. Without this, Agility 20 + Balance 20
        // would be physically immune to stumbling, which is unrealistic.
        stumbleRisk = Mathf.Max(stumbleRisk, MovementThresholds.MIN_STUMBLE_RISK);

        float resistance = (agility + balance) / 40.0f;

        return stumbleRisk > resistance;
    }

    /// <summary>
    /// Calculates minimum dwell time in STUMBLING state.
    /// Higher Balance = faster recovery.
    ///
    /// Formula: dwell = BASE / (balance / 20.0)
    /// Clamped to [0.3, 1.5] seconds.
    ///
    /// Examples:
    ///   Balance 20: 0.6 / 1.0 = 0.6s Ã¢â€ â€™ clamped to 0.6s
    ///   Balance 10: 0.6 / 0.5 = 1.2s
    ///   Balance  5: 0.6 / 0.25 = 2.4s Ã¢â€ â€™ clamped to 1.5s
    ///   Balance  1: 0.6 / 0.05 = 12.0s Ã¢â€ â€™ clamped to 1.5s
    /// </summary>
    private static float CalculateStumbleDwell(int balance)
    {
        float balanceFactor = Mathf.Max(balance / 20.0f, 0.05f);  // Prevent div by zero
        float dwell = MovementThresholds.STUMBLE_MIN_DWELL_BASE / balanceFactor;
        return Mathf.Clamp(dwell, 0.3f, 1.5f);
    }

    /// <summary>
    /// Calculates minimum dwell time in GROUNDED state.
    /// Varies by reason (collision vs voluntary), collision force, and attributes.
    ///
    /// Step 1 Ã¢â‚¬â€ Base dwell from reason:
    ///   Collision knockdown: base = BASE
    ///   Voluntary slide:     base = BASE Ãƒâ€” 0.6
    ///   Diving header:       base = BASE Ãƒâ€” 0.7
    ///
    /// Step 2 Ã¢â‚¬â€ Attribute scaling:
    ///   scaled = base / ((strength + balance) / 40.0)
    ///
    /// Step 3 Ã¢â‚¬â€ Collision force scaling (COLLISION reason only):
    ///   final = scaled Ãƒâ€” (COLLISION_DWELL_MIN + (1.0 - COLLISION_DWELL_MIN) Ãƒâ€” collisionForce)
    ///   At force 0.0 (light nudge): final = scaled Ãƒâ€” 0.5
    ///   At force 0.5 (standing tackle): final = scaled Ãƒâ€” 0.75
    ///   At force 1.0 (full-speed collision): final = scaled Ãƒâ€” 1.0
    ///   For voluntary actions (slide, header): force scaling skipped (controlled landing)
    ///
    /// Clamped to [0.5, 2.5] seconds.
    ///
    /// Examples Ã¢â‚¬â€ Collision at force 0.8 (heavy tackle):
    ///   forceScale = 0.65 + 0.35 Ãƒâ€” 0.8 = 0.93
    ///   S20+B20: 1.0 / 1.0 Ãƒâ€” 0.93 = 0.93s
    ///   S12+B12: 1.0 / 0.6 Ãƒâ€” 0.93 = 1.55s
    ///   S5+B5:   1.0 / 0.25 Ãƒâ€” 0.93 = 3.72s Ã¢â€ â€™ clamped to 2.5s
    ///
    /// Examples Ã¢â‚¬â€ Collision at force 0.2 (shoulder nudge):
    ///   forceScale = 0.65 + 0.35 Ãƒâ€” 0.2 = 0.72
    ///   S20+B20: 1.0 / 1.0 Ãƒâ€” 0.72 = 0.72s
    ///   S12+B12: 1.0 / 0.6 Ãƒâ€” 0.72 = 1.2s
    ///
    /// Examples Ã¢â‚¬â€ Voluntary sliding tackle:
    ///   S20+B20: 0.6 / 1.0 = 0.6s (no force scaling)
    ///   S12+B12: 0.6 / 0.6 = 1.0s
    /// </summary>
    private static float CalculateGroundedDwell(
        int balance, int strength,
        GroundedReason reason = GroundedReason.COLLISION,
        float collisionForce = 1.0f)
    {
        float reasonMultiplier = reason switch
        {
            GroundedReason.COLLISION      => 1.0f,
            GroundedReason.SLIDING_TACKLE => 0.6f,
            GroundedReason.DIVING_HEADER  => 0.7f,
            _ => 1.0f
        };

        float combinedFactor = Mathf.Max((strength + balance) / 40.0f, 0.05f);
        float dwell = (MovementThresholds.GROUNDED_MIN_DWELL_BASE * reasonMultiplier)
                     / combinedFactor;

        // Apply collision force scaling for involuntary knockdowns only.
        // Voluntary actions (slide, header) have controlled landings Ã¢â‚¬â€
        // force magnitude is irrelevant to recovery time.
        if (reason == GroundedReason.COLLISION)
        {
            float forceScale = MovementThresholds.COLLISION_DWELL_MIN
                             + (1.0f - MovementThresholds.COLLISION_DWELL_MIN)
                             * Mathf.Clamp01(collisionForce);
            dwell *= forceScale;
        }

        return Mathf.Clamp(dwell, 0.5f, 2.5f);
    }
}
```

### 3.1.6 State-Dependent Physics Activation

Each state activates a specific subset of locomotion formulas defined in Sections 3.2Ã¢â‚¬â€œ3.5. This table is the authoritative mapping.

```
Ã¢â€Å’Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â
Ã¢â€â€š State        Ã¢â€â€š Accel  Ã¢â€â€š Decel  Ã¢â€â€š Turn   Ã¢â€â€šDirectionÃ¢â€â€š Fatigue  Ã¢â€â€š VoluntaryÃ¢â€â€š
Ã¢â€â€š              Ã¢â€â€š (3.2)  Ã¢â€â€š (3.2)  Ã¢â€â€š (3.4)  Ã¢â€â€š Mult    Ã¢â€â€š (3.5)    Ã¢â€â€š Control  Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š (3.3)   Ã¢â€â€š          Ã¢â€â€š          Ã¢â€â€š
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¼Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â¤
Ã¢â€â€š IDLE         Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š Free   Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€š Passive  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š WALKING      Ã¢â€â€š Linear Ã¢â€â€š Linear Ã¢â€â€š Free   Ã¢â€â€š  Yes    Ã¢â€â€š Passive  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š JOGGING      Ã¢â€â€š Exp    Ã¢â€â€š Ctrl   Ã¢â€â€šModerateÃ¢â€â€š  Yes    Ã¢â€â€š Active   Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š Aero gateÃ¢â€â€š          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š : 0.15   Ã¢â€â€š          Ã¢â€â€š
Ã¢â€â€š SPRINTING    Ã¢â€â€š Exp    Ã¢â€â€š Ctrl   Ã¢â€â€š Tight  Ã¢â€â€š  Yes    Ã¢â€â€š Active+  Ã¢â€â€š Full     Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€šSprint gatÃ¢â€â€š          Ã¢â€â€š
Ã¢â€â€š              Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š        Ã¢â€â€š         Ã¢â€â€š e: 0.20  Ã¢â€â€š          Ã¢â€â€š
Ã¢â€â€š DECELERATING Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€šCtrl/EmgÃ¢â€â€š LimitedÃ¢â€â€š  Yes    Ã¢â€â€š Active   Ã¢â€â€š Partial  Ã¢â€â€š
Ã¢â€â€š STUMBLING    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š Drag   Ã¢â€â€š  None  Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€šRecovery  Ã¢â€â€š None     Ã¢â€â€š
Ã¢â€â€š GROUNDED     Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š   Ã¢â‚¬â€    Ã¢â€â€š  None  Ã¢â€â€š   Ã¢â‚¬â€     Ã¢â€â€šRecovery  Ã¢â€â€š None     Ã¢â€â€š
Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Â´Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€Ëœ
```

**Key:**
- **Accel**: Linear = constant rate; Exp = exponential curve `a(t) = a_max Ãƒâ€” (1 - e^(-kÃƒâ€”t))`
- **Decel**: Ctrl = controlled braking; Emg = emergency braking; Drag = friction-only (no voluntary braking)
- **Turn**: Free = unrestricted; Moderate = radius constraint; Tight = large radius constraint; Limited = reduced rate; None = momentum only
- **Direction Mult**: Whether directional speed multipliers (forward/lateral/backward) are applied
- **Fatigue**: Passive = minimal drain; Active = standard drain; Active+ = accelerated drain (sprint reservoir); Recovery = fatigue recovery paused; **Gate values** = state forcibly exited when respective energy pool drops below threshold (sprint reservoir for SPRINTING, aerobic pool for JOGGING)
- **Voluntary Control**: Full = agent can change direction/speed freely; Partial = can brake but limited steering; None = physics-only (momentum carries agent)

### 3.1.7 Oscillation Guard

Mirrors the Ball Physics pattern for detecting state machine instability.

```csharp
/// <summary>
/// Tracks state transitions to detect oscillation.
/// If transitions exceed MAX_TRANSITIONS_PER_SECOND, locks current state
/// for a cooldown period.
///
/// Implementation: Ring buffer of transition timestamps.
/// Check: Count transitions in last 1.0 second window.
/// </summary>
public struct OscillationGuard
{
    private const int BUFFER_SIZE = 8;
    private float[] _transitionTimes;  // Fixed-size, no heap alloc after init
    private int _writeIndex;
    private bool _isLocked;
    private float _lockUntilTime;

    /// <summary>Lock duration when oscillation detected (seconds)</summary>
    private const float LOCK_DURATION = 0.5f;

    /// <summary>
    /// Records a state transition and checks for oscillation.
    /// Returns true if the transition should be BLOCKED (oscillation detected).
    /// </summary>
    public bool RecordAndCheck(float currentTime)
    {
        // If currently locked, block transition
        if (_isLocked && currentTime < _lockUntilTime)
            return true;  // BLOCK

        _isLocked = false;

        // Record transition
        _transitionTimes[_writeIndex] = currentTime;
        _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;

        // Count transitions in last 1.0 second
        int recentCount = 0;
        for (int i = 0; i < BUFFER_SIZE; i++)
        {
            if (currentTime - _transitionTimes[i] < 1.0f)
                recentCount++;
        }

        if (recentCount > MovementThresholds.MAX_TRANSITIONS_PER_SECOND)
        {
            _isLocked = true;
            _lockUntilTime = currentTime + LOCK_DURATION;
            // Log WARNING: oscillation detected for agent [ID]
            return true;  // BLOCK
        }

        return false;  // ALLOW
    }
}
```

### 3.1.8 Coordinate System & Units

Consistent with Ball Physics Spec #1:

| Property | Axis | Unit | Notes |
|----------|------|------|-------|
| Position X | East-West | meters | Pitch length axis |
| Position Y | North-South | meters | Pitch width axis |
| Position Z | Vertical (up) | meters | 0 = ground level; agents always z Ã¢â€°Â¥ 0 |
| Velocity | XYZ | m/s | Magnitude = scalar speed |
| Facing direction | XY plane | unit vector | Z component always 0 for outfield players |
| Turn angle | Ã¢â‚¬â€ | degrees | Measured in XY plane, 0Ã‚Â° = no turn, 180Ã‚Â° = reversal |
| Time | Ã¢â‚¬â€ | seconds | Frame delta = 1/60 Ã¢â€°Ë† 0.01667s |

---

**End of Section 3.1**

**Page Count:** ~10 pages  
**Next Section:** Section 3.2 Ã¢â‚¬â€ Locomotion (Acceleration, Top Speed, Deceleration)

