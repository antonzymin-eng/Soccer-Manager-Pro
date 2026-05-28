// File:     src/heading-mechanics/HeadingMechanicsConstants.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.1, KD-11, FR-HE-014, Code Standards #20
// Purpose:  All numeric constants for the heading mechanics system. No magic literals in formula files.
//           Region order: Fixed → Derived → Cross → GT.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// All constants for the heading mechanics system. Every symbol in §3.2–§3.8 pseudocode
    /// bodies that is a constant appears here with its source tag (KD-11 / FR-HE-014).
    /// No magic literals permitted in any formula or system file.
    /// Heading Mechanics #10 §3.1.
    /// </summary>
    public static class HeadingMechanicsConstants
    {
        #region Fixed

        /// <summary>[FIXED] Anatomical baseline head reach (m): average standing head-height plus typical no-effort reach.
        /// Heading Mechanics #10 §3.1.</summary>
        public const float JUMP_REACH_BASE_M = 2.20f;

        /// <summary>[FIXED] Maximum value for any player attribute [1–20]. Agent Movement #2 §3.5.6 / Shot Mechanics #6 §3.2.</summary>
        public const float ATTR_MAX = 20.0f;

        /// <summary>[FIXED] Minimum attribute floor for normalisation clamping. §3.3.</summary>
        public const float ATTR_MIN = 1.0f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Physics frame duration in milliseconds.
        /// Formula: 1000 / TickRatePhysicsHz. Heading Mechanics #10 §3.1 / FM-010 (§3.2).
        /// Source constants: HeadingMechanicsConstants.TickRatePhysicsHz.
        /// </summary>
        public static readonly float FrameMs = 1000.0f / TickRatePhysicsHz;

        /// <summary>
        /// [DERIVED] Number of 60 Hz physics frames per 10 Hz tactical tick.
        /// Formula: TickRatePhysicsHz / TickRateTacticalHz. Heading Mechanics #10 §3.3.
        /// Source constants: TickRatePhysicsHz, TickRateTacticalHz.
        /// </summary>
        public static readonly int FramesPerTacticalTick =
            (int)(TickRatePhysicsHz / TickRateTacticalHz);

        /// <summary>
        /// [DERIVED] Early-tolerance window in 60 Hz frames (ceil, toward looser tolerance).
        /// Formula: ceil(MaxEarlyToleranceMs / FrameMs). Heading Mechanics #10 §3.2.
        /// Source constants: MaxEarlyToleranceMs, FrameMs.
        /// </summary>
        public static readonly int FramesEarlyTolerance =
            Mathf.CeilToInt(MaxEarlyToleranceMs / FrameMs);

        /// <summary>
        /// [DERIVED] Late-tolerance window in 60 Hz frames (ceil, toward looser tolerance).
        /// Formula: ceil(MaxLateToleranceMs / FrameMs). Heading Mechanics #10 §3.2.
        /// Source constants: MaxLateToleranceMs, FrameMs.
        /// </summary>
        public static readonly int FramesLateTolerance =
            Mathf.CeilToInt(MaxLateToleranceMs / FrameMs);

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Gravitational acceleration (m/s²).
        /// Authoritative source: BallPhysicsConstants.Environment.GRAVITY. Ball Physics #1 §1.2.
        /// Value: 9.81 m/s².
        /// </summary>
        public static readonly float GravityMps2 = BallPhysicsConstants.Environment.GRAVITY;

        /// <summary>
        /// [CROSS] Pitch length in metres (X axis: goal-to-goal).
        /// Authoritative source: BallPhysicsConstants.Pitch.LENGTH. Ball Physics #1 §1.2.
        /// Value: 105.0 m.
        /// </summary>
        public static readonly float PitchLengthM = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>
        /// [CROSS] Pitch width in metres (Y axis: touchline-to-touchline).
        /// Authoritative source: BallPhysicsConstants.Pitch.WIDTH. Ball Physics #1 §1.2.
        /// Value: 68.0 m.
        /// </summary>
        public static readonly float PitchWidthM = BallPhysicsConstants.Pitch.WIDTH;

        /// <summary>
        /// [CROSS] FIFA goal width (m).
        /// Authoritative source: BallPhysicsConstants.Pitch.GOAL_WIDTH. Ball Physics #1 §1.2.
        /// Value: 7.32 m.
        /// </summary>
        public static readonly float GoalWidthM = BallPhysicsConstants.Pitch.GOAL_WIDTH;

        /// <summary>
        /// [CROSS] FIFA goal height (m).
        /// Authoritative source: BallPhysicsConstants.Pitch.GOAL_HEIGHT. Ball Physics #1.
        /// Value: 2.44 m.
        /// TODO: mirror from BallPhysicsConstants.Pitch.GOAL_HEIGHT when that constant is added.
        /// </summary>
        public static readonly float GoalHeightM = 2.44f; // TODO: replace with BallPhysicsConstants.Pitch.GOAL_HEIGHT

        /// <summary>
        /// [CROSS] Heading subsystem domain tag.
        /// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_HEADING.
        /// Deterministic Simulation #16 §3.4 — ERR-010-001 RESOLVED May 16, 2026.
        /// Value: 0x16. TODO: mirror from DeterministicSimConstants.DOMAIN_TAG_HEADING when that file exists.
        /// </summary>
        public static readonly uint DomainTagHeading = 0x16; // TODO: mirror from DeterministicSimConstants

        /// <summary>
        /// [CROSS] Physics/render loop tick rate (Hz).
        /// Authoritative source: CLAUDE.md Heartbeat Tick Rate.
        /// Value: 60 Hz. TODO: mirror from ProjectConstants.TickRatePhysicsHz when that file is created.
        /// </summary>
        public static readonly float TickRatePhysicsHz = 60.0f; // TODO: mirror from ProjectConstants

        /// <summary>
        /// [CROSS] Tactical AI loop tick rate (Hz).
        /// Authoritative source: CLAUDE.md Heartbeat Tick Rate.
        /// Value: 10 Hz. TODO: mirror from ProjectConstants.TickRateTacticalHz when that file is created.
        /// </summary>
        public static readonly float TickRateTacticalHz = 10.0f; // TODO: mirror from ProjectConstants

        #endregion

        #region GT

        // ── §3.2 Eligibility ──────────────────────────────────────────────────────────

        /// <summary>[GT] Effective radius (m) of the sphere around head centre that admits ball contact. §3.1 / §3.2.</summary>
        public static readonly float HeadContactVolumeRadiusM = 0.18f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Vertical half-extent (m) of the contact volume. §3.1 / §3.2.</summary>
        public static readonly float HeadContactVolumeHeightM = 0.22f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Earliest allowable signed timing offset (ms). Distinct from MaxLateToleranceMs per FR-HE-022 / pass-1 H-1. §3.1.</summary>
        public static readonly float MaxEarlyToleranceMs = 140.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Latest allowable signed timing offset (ms). Numerically smaller than MaxEarlyToleranceMs — late headers degrade faster. FR-HE-022 / pass-1 H-1. §3.1.</summary>
        public static readonly float MaxLateToleranceMs = 90.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.3 Jump Kinematics ─────────────────────────────────────────────────────

        /// <summary>[GT] Sensitivity of JumpReach to Strength_norm (m). §3.1 / §3.3.</summary>
        public static readonly float JumpReachKStrength = 0.18f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sensitivity of JumpReach to Balance_norm (m). §3.1 / §3.3.</summary>
        public static readonly float JumpReachKBalance = 0.10f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sensitivity of JumpReach to Heading_norm (m). Covers jump-timing skill until §7.10. FR-HE-021 / pass-1 H-2. §3.1.</summary>
        public static readonly float JumpReachKHeading = 0.12f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Total ground-to-ground aerial phase duration (ms) for the Stage 0 synthetic trajectory. KD-18. §3.1.</summary>
        public static readonly float JumpPhaseDurationMs = 650.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Apex location as a fraction of JumpPhaseDurationMs. [GT] not [FIXED]: trajectory is synthetic (KD-18 footnote). §3.1.</summary>
        public static readonly float JumpApexFraction = 0.50f; // TODO: replace with config loader (Stage 1)

        // ── §3.4 Contact Quality ─────────────────────────────────────────────────────

        /// <summary>[GT] Telemetry-bucket early boundary (ms). NOT a formula gate (KD-2). §3.1.</summary>
        public static readonly float EarlyLabelThresholdMs = 40.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Telemetry-bucket late boundary (ms). NOT a formula gate (KD-2). §3.1.</summary>
        public static readonly float LateLabelThresholdMs = 40.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Alpha weight on timingQuality in the §3.4 convex combination. §3.1.</summary>
        public static readonly float TimingPointBlendAlpha = 0.55f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Baseline denominator for pointQuality; mean point-error scale (m). §3.1.</summary>
        public static readonly float ContactPointErrorSigmaM = 0.03f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Amplitude of per-attempt point-error Gaussian noise via DRAW_SITE_CONTACT_POINT_ERROR (m). pass-1 M-4. §3.1.</summary>
        public static readonly float ContactPointNoiseSigmaM = 0.012f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Amplitude of per-attempt timing-noise Gaussian via DRAW_SITE_TIMING_JITTER (ms). pass-1 M-4. §3.1.</summary>
        public static readonly float TimingJitterSigmaMs = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Heading-attribute scaling coefficient for contact point error. §3.1 / §3.4.</summary>
        public static readonly float ContactPointHeadingAttrCoeff = 0.40f; // TODO: replace with config loader (Stage 1)

        // ── §3.5 Power & Launch Angle ────────────────────────────────────────────────

        /// <summary>[GT] Baseline header outgoing speed (m/s). §3.1.</summary>
        public static readonly float PowerBaseMps = 7.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength contribution to outgoing speed (m/s per unit norm). §3.1.</summary>
        public static readonly float PowerKStrength = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Heading-attribute contribution to outgoing speed (m/s per unit norm). §3.1.</summary>
        public static readonly float PowerKHeading = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty coefficient [0, 0.5]. 0 = no degradation. CLAUDE.md fatigue convention. §3.1.</summary>
        public static readonly float PowerFatigueCoeff = 0.18f; // TODO: replace with config loader (Stage 1)

        // ── §3.6 Spin Transfer ───────────────────────────────────────────────────────

        /// <summary>[GT] Multiplier on derived headAngularVelocity contribution to outgoing spin. §3.1.</summary>
        public static readonly float SpinTransferCoeff = 0.55f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Scale-factor base for spinPreservationFactor. §3.1.</summary>
        public static readonly float SpinPreservationBase = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Contact-point axial offset beyond which spinPreservationFactor goes negative (m). §3.1.</summary>
        public static readonly float SpinTransferReversalThreshold = 0.015f; // TODO: replace with config loader (Stage 1)

        // ── §3.7 Duel Resolution ─────────────────────────────────────────────────────

        /// <summary>[GT] Minimum contactQualityScalar; duel loser below this emits HeaderAttemptFailedEvent. FR-HE-026. §3.1.</summary>
        public static readonly float MinContactQuality = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance weight w_B in §3.7 base-score formula FM-010-005. §3.1.</summary>
        public static readonly float DuelBalanceWeight = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength weight w_S in §3.7 base-score formula FM-010-005. §3.1.</summary>
        public static readonly float DuelStrengthWeight = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Heading weight w_H in §3.7 base-score formula FM-010-005. Sum of three weights = 1.0. §3.1.</summary>
        public static readonly float DuelHeadingWeight = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Near-tie threshold gating RNG perturbation. Non-tie scores are NEVER perturbed. FR-HE-023 / pass-1 H-5. §3.1.</summary>
        public static readonly float DuelTiebreakEpsilon = 0.02f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] RNG perturbation amplitude applied only when score gap &lt; DuelTiebreakEpsilon. pass-1 H-5. §3.1.</summary>
        public static readonly float DuelTiebreakNoiseAmplitude = 0.01f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum disturbance factor applied to a duel loser's contactQualityScalar. §3.1.</summary>
        public static readonly float DuelDisturbanceMax = 0.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] baseScore gap at which disturbanceFactor saturates at DuelDisturbanceMax. v0.2 H-4. §3.1.</summary>
        public static readonly float DuelDisturbanceGapSaturation = 0.20f; // TODO: replace with config loader (Stage 1)

        // ── §3.8 Own-Goal Flag ───────────────────────────────────────────────────────

        /// <summary>[GT] Projection time horizon for own-goal-shape flag (s). §3.1.</summary>
        public static readonly float OwnGoalProjectionHorizonS = 1.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Projection distance horizon for own-goal-shape flag (m). pass-1 L-7. §3.1.</summary>
        public static readonly float OwnGoalProjectionHorizonM = 18.0f; // TODO: replace with config loader (Stage 1)

        // ── §4.2.1 Buffer / Draw Sites ───────────────────────────────────────────────

        /// <summary>[GT] Pre-allocated collision-event buffer capacity for ICollisionEventConsumer (§4.2.1).
        /// Bound: 3-way duel × 2 contact-pairs × safety margin. Allocated once at Initialize(). §3.1.</summary>
        public static readonly int HeadingContactBufferCapacity = 16; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum number of active header intents tracked simultaneously (one per agent). §4.6.</summary>
        public static readonly int MaxAgents = 22; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum participants tracked in a single contested duel. §3.7.</summary>
        public static readonly int MaxDuelParticipants = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum simultaneous contested duels per physics frame. §3.7.</summary>
        public static readonly int MaxSimultaneousDuels = 4; // TODO: replace with config loader (Stage 1)

        // ── Draw-Site IDs (registered with Deterministic Simulation #16 §4.5) ────────

        /// <summary>[GT] Draw-site ID for §3.7 near-tie perturbation (NextFloat). Heading Mechanics #10 §4.4.</summary>
        public static readonly int DrawSiteDuelTiebreak = 10; // TODO: register with #16 §4.5

        /// <summary>[GT] Draw-site ID for §3.4 contact-point noise Gaussian (NextGaussian). Heading Mechanics #10 §4.4.</summary>
        public static readonly int DrawSiteContactPointError = 11; // TODO: register with #16 §4.5

        /// <summary>[GT] Draw-site ID for §3.4 timing-jitter Gaussian (NextGaussian). Heading Mechanics #10 §4.4.</summary>
        public static readonly int DrawSiteTimingJitter = 12; // TODO: register with #16 §4.5

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
