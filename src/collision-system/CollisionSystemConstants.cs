// File:     src/collision-system/CollisionSystemConstants.cs
// Created:  2026-05-25
// Modified: 2026-05-25  [v1.2]
// Author:   —
// Spec:     Collision System #3 §3.1.1, §3.3.1, §4.3.1, Code Standards #20
// Purpose:  All constants for the collision system. No literals in formula code.

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Spatial hash grid configuration. Collision System #3 §3.1.1.
    /// </summary>
    public static class SpatialHashConstants
    {
        #region Fixed

        /// <summary>[FIXED] Minimum distance to avoid division by zero (m). §3.2.1.</summary>
        public const float MIN_DISTANCE_EPSILON = 0.0001f;

        /// <summary>
        /// [FIXED] Ball entity ID sentinel (agents use 0–21).
        /// Convention defined in Collision System #3 §4.3.1.
        /// Value: -1.
        /// </summary>
        public const int BALL_ENTITY_ID = -1;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Grid cell size in metres.
        /// Formula: >= max_combined_agent_radius = 0.50m + 0.50m = 1.0m. §3.1.1.
        /// Source: AgentMovement #2 §3.5.4.3 (HitboxRadius max 0.50m).
        /// </summary>
        public static readonly float CellSize = 1.0f;

        /// <summary>
        /// [DERIVED] Grid width in cells (X / pitch length).
        /// Formula: ceil(105 / CellSize) + 1 = 106. §3.1.1.
        /// Source: Ball Physics #1 §1.2 pitch length 105m.
        /// </summary>
        public static readonly int GridWidth = 106;

        /// <summary>
        /// [DERIVED] Grid height in cells (Y / pitch width).
        /// Formula: ceil(68 / CellSize) + 1 = 69. §3.1.1.
        /// Source: Ball Physics #1 §1.2 pitch width 68m.
        /// </summary>
        public static readonly int GridHeight = 69;

        /// <summary>
        /// [DERIVED] Total cells.
        /// Formula: GridWidth * GridHeight. §3.1.1.
        /// Source: SpatialHashConstants.GridWidth, SpatialHashConstants.GridHeight.
        /// </summary>
        public static readonly int TotalCells = GridWidth * GridHeight; // 7,314

        /// <summary>
        /// [DERIVED] Virtual slot index used to map BALL_ENTITY_ID in CollisionPairBitfield. §3.2.4.
        /// Formula: AgentCapacity. Agents occupy slots 0 to AgentCapacity-1; ball receives the next slot.
        /// Source: SpatialHashConstants.AgentCapacity.
        /// </summary>
        public static readonly int BallVirtualIndex = 22; // must equal AgentCapacity (GT); kept literal to avoid static-field init-order dependency

        /// <summary>
        /// [DERIVED] Row size for the triangular pair-index formula in CollisionPairBitfield. §3.2.4.
        /// Formula: BallVirtualIndex + 1 = 23. §3.2.4.
        /// Source: SpatialHashConstants.BallVirtualIndex.
        /// </summary>
        public static readonly int PairFormulaRowSize = BallVirtualIndex + 1;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Ball radius (m).
        /// Authoritative source: BallPhysicsConstants.Ball.RADIUS (Ball Physics #1 §2.1). Value: 0.11m.
        /// </summary>
        public static readonly float BallRadius = BallPhysicsConstants.Ball.RADIUS;

        #endregion

        #region GT

        /// <summary>[GT] Number of agents per match (22 = 11 per team). §3.2.4.</summary>
        public static readonly int AgentCapacity = 22; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max entities per cell before warning. §4.3.1.</summary>
        public static readonly int CellDensityWarning = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max collision pairs processed per frame. §4.3.1 Safety.</summary>
        public static readonly int MaxCollisionPairs = 50; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max loop iterations; prevents infinite loops from corrupt state. §4.3.1 Safety.</summary>
        public static readonly int MaxIterations = 1000; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Collision physics parameters. Collision System #3 §3.3.1.
    /// </summary>
    public static class CollisionPhysicsConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Physics/render loop tick rate (Hz). Used to convert impulse (kg·m/s) to force (N): F = j × Hz.
        /// Authoritative source: Ball Physics #1 §1.2 / root CLAUDE.md "Heartbeat Tick Rate". Value: 60 Hz.
        /// </summary>
        public const float PHYSICS_TICK_HZ = 60f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Maximum impulse magnitude (kg·m/s) — safety ceiling.
        /// Formula: max_mass(100) × max_relative_speed(20.4) × (1 + e) / 2 × 1.5 margin ≈ 2000. §3.3.1.
        /// Source: AgentMovement #2 §3.5.4.2 (max mass 100kg), §3.2 (sprint 10.2 m/s).
        /// </summary>
        public static readonly float MaxImpulseMagnitude = 2000f;

        /// <summary>
        /// [DERIVED] Reference force (N) for normalising impact force to [0,1] for AgentMovement.
        /// Formula: FallForceBase + AttributeMax × FallForcePerStrength + FallProbabilityRange = 2000 N.
        /// Source: FallThresholdConstants, PlayerAttributeConstants.AttributeMax.
        /// At or above this force, collisionForce = 1.0 → maximum grounded dwell in AgentStateMachine. §3.3.1.
        /// </summary>
        public static readonly float MaxCollisionForceRef =
            FallThresholdConstants.FallForceBase
            + PlayerAttributeConstants.AttributeMax * FallThresholdConstants.FallForcePerStrength
            + FallThresholdConstants.FallProbabilityRange;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Ball radius (m) — mirrors SpatialHashConstants.BallRadius.
        /// Authoritative source: BallPhysicsConstants.Ball.RADIUS (Ball Physics #1 §2.1). Value: 0.11m.
        /// </summary>
        public static readonly float BallRadius = SpatialHashConstants.BallRadius;

        #endregion

        #region GT

        /// <summary>[GT] Coefficient of restitution for agent-agent collision (0=inelastic, 1=elastic). §3.3.1.</summary>
        public static readonly float CoefficientOfRestitution = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Impulse scale for same-team collisions (spatial awareness reduces hard contact). §3.3.1.</summary>
        public static readonly float SameTeamMomentumScale = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max ball Z height for ground-contact detection (m); above → aerial duel. §3.2.1 / FR-03.</summary>
        public static readonly float AgentReachHeight = 2.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Slight over-separation multiplier applied to penetration depth to prevent re-contact on the next frame. §3.3.2.
        /// Value of 1.01 pushes entities 1% beyond exact contact surface.
        /// </summary>
        public static readonly float SeparationSlop = 1.01f; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Minimum squared vector magnitude below which a response (impulse or position correction) is skipped. §3.4.1.
        /// Avoids applying negligible responses that would only introduce floating-point noise.
        /// Effective magnitude threshold: sqrt(MinResponseSqrMagnitude) ≈ 0.01 m or m/s.
        /// </summary>
        public static readonly float MinResponseSqrMagnitude = 0.0001f; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Fall and stumble force thresholds. Collision System #3 §3.3.1 / FR-05.
    /// </summary>
    public static class FallThresholdConstants
    {
        #region GT

        /// <summary>[GT] Base force threshold for falling (N). §3.3.1. Strength-1 agent: 550N.</summary>
        public static readonly float FallForceBase = 500f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Additional fall threshold per Strength point (N/point). §3.3.1.</summary>
        public static readonly float FallForcePerStrength = 50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Stumble threshold as fraction of fall threshold. §3.3.1.</summary>
        public static readonly float StumbleThresholdFraction = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Force range over which P(fall) interpolates 0→1 above threshold (N). §3.3.1.</summary>
        public static readonly float FallProbabilityRange = 500f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max penetration before tunneling warning (m). §3.3.1.</summary>
        public static readonly float MaxPenetrationDepth = 0.5f; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Contact type classification angle thresholds. Collision System #3 §3.3.6.
    /// </summary>
    public static class ContactClassificationConstants
    {
        #region GT

        /// <summary>[GT] Dot-product threshold for shoulder-to-shoulder (parallel velocities). §3.3.6.</summary>
        public static readonly float ShoulderDotThreshold = 0.7f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Dot-product threshold for from-behind detection. §3.3.6.</summary>
        public static readonly float BehindDotThreshold = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum speed for instigator direction to be meaningful (m/s). §3.3.6.</summary>
        public static readonly float MinSpeedForClassification = 0.1f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum victim speed to test from-behind case (m/s). §3.3.6.</summary>
        public static readonly float MinVictimSpeedBehind = 1.0f; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Agent physical property constants derived from Agent Movement #2 §3.5.4.
    /// Used by AgentPhysicalProperties.From() to compute hitbox radius and mass from Strength.
    /// </summary>
    public static class AgentPhysicsSnapshotConstants
    {
        #region Cross

        /// <summary>
        /// [CROSS] Base hitbox radius for Strength 1 agents (m).
        /// Authoritative source: Agent Movement #2 §3.5.4.3.
        /// Formula context: HitboxRadius = HitboxRadiusBase + (Strength - 1) × HitboxRadiusPerStrength.
        /// Range: 0.3525m (Strength 1) – 0.50m (Strength 20).
        /// </summary>
        public static readonly float HitboxRadiusBase = 0.3525f;

        /// <summary>
        /// [CROSS] Hitbox radius increment per Strength point (m/point).
        /// Authoritative source: Agent Movement #2 §3.5.4.3.
        /// </summary>
        public static readonly float HitboxRadiusPerStrength = 0.0075f;

        /// <summary>
        /// [CROSS] Base body mass for Strength 1 agents (kg).
        /// Authoritative source: Agent Movement #2 §3.5.4.2.
        /// Formula context: Mass = MassBase + (Strength - 1) × MassPerStrength.
        /// Range: 72.5 kg (Strength 1) – 100.0 kg (Strength 20).
        /// </summary>
        public static readonly float MassBase = 72.5f;

        /// <summary>
        /// [CROSS] Body mass increment per Strength point (kg/point).
        /// Authoritative source: Agent Movement #2 §3.5.4.2.
        /// </summary>
        public static readonly float MassPerStrength = 1.5f;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                    |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                           |
// | 1.1     | 2026-05-25 | —      | Adversarial review fix pass. H-3: BALL_ENTITY_ID retag [CROSS]→[FIXED], moved to Fixed   |
// |         |            |        | region. H-4: BallRadius references BallPhysicsConstants.Ball.RADIUS (no hardcoded        |
// |         |            |        | literal). M-1: CollisionPhysicsConstants region order corrected to Derived→Cross→GT.    |
// |         |            |        | M-1b: MaxCollisionForceRef [DERIVED] constant added (normalises impact force to [0,1]).  |
// |         |            |        | H-3: AgentPhysicsSnapshotConstants class added (hitbox/mass formula constants from #2).  |
// | 1.2     | 2026-05-25 | —      | Pass-3+4. P3-1: MinResponseSqrMagnitude [GT] added. P4-1: AgentCapacity [GT] added;      |
// |         |            |        | BallVirtualIndex [DERIVED] and PairFormulaRowSize [DERIVED] added to SpatialHashConstants|
// |         |            |        | to replace magic literals 22/23 in CollisionPairBitfield (FR-CS-016).                    |
#endregion
