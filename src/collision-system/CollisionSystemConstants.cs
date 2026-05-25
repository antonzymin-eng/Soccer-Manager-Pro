// File:     src/collision-system/CollisionSystemConstants.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.1.1, §3.3.1, §4.3.1, Code Standards #20
// Purpose:  All constants for the collision system. No literals in formula code.

using UnityEngine;

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

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Ball entity ID sentinel (agents use 0–21).
        /// Authoritative source: Collision System #3 §4.3.1 — convention owned here.
        /// Value: -1.
        /// </summary>
        public const int BALL_ENTITY_ID = -1;

        /// <summary>
        /// [CROSS] Ball radius (m).
        /// Authoritative source: BallPhysicsConstants (Ball Physics #1 §2.1). Value: 0.11m.
        /// </summary>
        public static readonly float BallRadius = 0.11f;

        #endregion

        #region GT

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
        #region Cross

        /// <summary>
        /// [CROSS] Ball radius (m) — mirrors SpatialHashConstants.BallRadius.
        /// Authoritative source: Ball Physics #1 §2.1. Value: 0.11m.
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

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Maximum impulse magnitude (kg·m/s) — safety ceiling.
        /// Formula: max_mass(100) × max_relative_speed(20.4) × (1 + e) / 2 × 1.5 margin ≈ 2000. §3.3.1.
        /// Source: AgentMovement #2 §3.5.4.2 (max mass 100kg), §3.2 (sprint 10.2 m/s).
        /// </summary>
        public static readonly float MaxImpulseMagnitude = 2000f;

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
    /// Grounded state duration parameters. Collision System #3 §3.3.1 / FR-05.
    /// </summary>
    public static class GroundedDurationConstants
    {
        #region GT

        /// <summary>[GT] Minimum grounded duration (s). §3.3.1.</summary>
        public static readonly float DurationMin = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum grounded duration (s). §3.3.1.</summary>
        public static readonly float DurationMax = 2.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base grounded duration before Agility reduction (s). §3.3.1.</summary>
        public static readonly float DurationBase = 1.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Grounded duration reduction per Agility point (s/point). §3.3.1.</summary>
        public static readonly float DurationPerAgility = 0.03f; // TODO: replace with config loader (Stage 1)

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
}

#region VersionHistory
// | Version | Date       | Author | Notes              |
// | 1.0     | 2026-05-25 | —      | Initial draft.     |
#endregion
