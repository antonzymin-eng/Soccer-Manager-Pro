// File:     src/heading-mechanics/HeadingMechanicsConstants.cs
// Created:  2026-05-28
// Modified: 2026-08-09 (ERR-010-002 §3.5.1 constants, recorded retroactively at the AR over that landing;
//           MaxRangeLaunchComponent retired in the same pass — see version history rows 1.4 and 1.5)
// Author:   —
// Spec:     Heading Mechanics #10 §3.1, KD-11, FR-HE-014, Code Standards #20
// Purpose:  All numeric constants for the heading mechanics system. No magic literals in formula files.
//           Region order: Fixed → Derived → Cross → GT.
//           NOTE: TickRatePhysicsHz and TickRateTacticalHz are declared const float in Cross to prevent
//           C# static-field init ordering issues in Derived (const fields are available at compile time,
//           independent of declaration order).
//           NOTE: FramesEarlyTolerance and FramesLateTolerance are placed at the START of the GT region
//           (before other GT constants) because they depend on GT constants MaxEarlyToleranceMs and
//           MaxLateToleranceMs and on Derived constant FrameMs. C# initialises static readonly fields in
//           declaration order; placing them after their dependencies in GT ensures correct evaluation.

using UnityEngine;

using TacticalDirector.BallPhysics;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

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

        /// <summary>[FIXED] Coefficient in the standard parabolic arc formula 4u(1−u) giving peak = 1 at u = 0.5. §3.3 KD-18.</summary>
        public const float PARABOLA_AMPLITUDE = 4.0f;

        /// <summary>[FIXED] Midpoint of the normalised attribute range [0, 1]. Used in headingAttrScale formula §3.4.</summary>
        public const float ATTRIBUTE_NORM_MIDPOINT = 0.5f;

        /// <summary>[FIXED] Squared-magnitude epsilon for degenerate-vector guards (avoids division by zero). §3.5 / §3.6.</summary>
        public const float DEGENERACY_EPSILON_SQ = 1e-6f;

        /// <summary>[FIXED] Minimum value of u1 in the Box-Muller transform, guarding against log(0). HeadingRngServiceStub.</summary>
        public const float RNG_GUARD_EPSILON = 1e-7f;

        /// <summary>[FIXED] Squared-magnitude epsilon for degenerate-surface-normal and degenerate-reflection guards. §3.5.</summary>
        public const float SURFACE_NORMAL_EPSILON_SQ = 1e-8f;

        /// <summary>[FIXED] Milliseconds per second. Unit-conversion coefficient for ms ↔ s throughout §3.2–§3.8.</summary>
        public const float MS_PER_SECOND = 1000.0f;

        /// <summary>[FIXED] Coefficient in the standard reflection formula r = 2(n·d)n − d. §3.5 FM-010-003.</summary>
        public const float REFLECTION_FORMULA_COEFF = 2.0f;

        /// <summary>[FIXED] Half-coefficient in the kinematic equation s = v₀t + ½at². §3.3 KD-18 / §3.8.</summary>
        public const float KINEMATIC_HALF_COEFF = 0.5f;

        /// <summary>
        /// [FIXED] The 2 in the kinematic relation v² = u² + 2as, as it appears in §3.5.1's projectile
        /// launch-angle discriminant v⁴ − g(gR² + 2·Δz·v²). Distinct from
        /// <see cref="REFLECTION_FORMULA_COEFF"/>, which is the 2 of the reflection identity — same
        /// number, unrelated derivations, so they do not share a name (FR-CS-016).
        /// Heading Mechanics #10 §3.5.1 (ERR-010-002).
        /// </summary>
        public const float KINEMATIC_TWO_COEFF = 2.0f;

        /// <summary>
        /// [FIXED] The contact-quality scalar of a perfect contact, i.e. the top of §3.4's [0, 1] range.
        /// §3.5.1 solves the aim at the speed a perfect contact would carry, because solving it at the
        /// achieved speed would be circular — achieved speed follows from quality, and quality follows
        /// from the error between the aim and what was achieved. Heading Mechanics #10 §3.5.1 (ERR-010-002).
        /// </summary>
        public const float PERFECT_CONTACT_QUALITY = 1.0f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Physics frame duration in milliseconds.
        /// Formula: MS_PER_SECOND / TickRatePhysicsHz. Heading Mechanics #10 §3.1 / FM-010 (§3.2).
        /// Source constants: MS_PER_SECOND (Fixed const), TickRatePhysicsHz (Cross const — always available).
        /// </summary>
        public static readonly float FrameMs = MS_PER_SECOND / TickRatePhysicsHz;

        /// <summary>
        /// [DERIVED] Physics frame duration in seconds.
        /// Formula: FrameMs / MS_PER_SECOND. Heading Mechanics #10 §3.1.
        /// Source constants: FrameMs (Derived), MS_PER_SECOND (Fixed const — always available).
        /// </summary>
        public static readonly float FrameS = FrameMs / MS_PER_SECOND;

        /// <summary>
        /// [DERIVED] Number of 60 Hz physics frames per 10 Hz tactical tick.
        /// Formula: TickRatePhysicsHz / TickRateTacticalHz. Heading Mechanics #10 §3.3.
        /// Source constants: TickRatePhysicsHz, TickRateTacticalHz (both const — always available).
        /// </summary>
        public static readonly int FramesPerTacticalTick =
            (int)(TickRatePhysicsHz / TickRateTacticalHz);

        /// <summary>
        /// [DERIVED] Linear-magnitude epsilon for degenerate-length guards (m).
        /// Formula: sqrt(SURFACE_NORMAL_EPSILON_SQ). Heading Mechanics #10 §3.5.1 (ERR-010-002).
        /// Source constants: SURFACE_NORMAL_EPSILON_SQ (Fixed const — always available).
        /// Exists so §3.5.1's range and speed guards test the same threshold as §3.5's squared guards
        /// rather than introducing a second, silently different epsilon.
        /// </summary>
        public static readonly float SurfaceNormalEpsilon = Mathf.Sqrt(SURFACE_NORMAL_EPSILON_SQ);

        /// <summary>
        /// [DERIVED] Half the FIFA goal width (m). Used for own-goal bounding box computation (§3.8).
        /// Formula: BallPhysicsConstants.Pitch.GOAL_WIDTH × 0.5. Ball Physics #1 §1.2.
        /// Source constants: BallPhysicsConstants.Pitch.GOAL_WIDTH (const — always available).
        /// </summary>
        public static readonly float GoalHalfWidthM = BallPhysicsConstants.Pitch.GOAL_WIDTH * 0.5f;

        /// <summary>
        /// [DERIVED] Y coordinate of pitch centre (m). Centrepoint for own-goal bounding box in §3.8.
        /// Formula: BallPhysicsConstants.Pitch.WIDTH × 0.5. Ball Physics #1 §1.2.
        /// Source constants: BallPhysicsConstants.Pitch.WIDTH (const — always available).
        /// </summary>
        public static readonly float PitchCentreYM = BallPhysicsConstants.Pitch.WIDTH * 0.5f;

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
        /// [CROSS] Physics/render loop tick rate (Hz). Declared const float (not static readonly) so
        /// Derived-region constants (FrameMs, FramesPerTacticalTick) that reference it evaluate correctly
        /// regardless of C# static-field declaration order.
        /// Authoritative source: CLAUDE.md Heartbeat Tick Rate.
        /// Value: 60 Hz. TODO: mirror from ProjectConstants.TickRatePhysicsHz when that file is created.
        /// </summary>
        public const float TickRatePhysicsHz = 60.0f; // TODO: mirror from ProjectConstants

        /// <summary>
        /// [CROSS] Tactical AI loop tick rate (Hz). Declared const float for same reason as TickRatePhysicsHz above.
        /// Authoritative source: CLAUDE.md Heartbeat Tick Rate.
        /// Value: 10 Hz. TODO: mirror from ProjectConstants.TickRateTacticalHz when that file is created.
        /// </summary>
        public const float TickRateTacticalHz = 10.0f; // TODO: mirror from ProjectConstants

        #endregion

        #region GT

        // ── §3.2 Eligibility ──────────────────────────────────────────────────────────

        /// <summary>[GT] Effective radius (m) of the sphere around head centre that admits ball contact. §3.1 / §3.2.</summary>
        public static readonly float HeadContactVolumeRadiusM = Config.GetFloat("heading-mechanics", "HeadContactVolumeRadiusM", 0.18f);

        /// <summary>[GT] Vertical half-extent (m) of the contact volume. §3.1 / §3.2.</summary>
        public static readonly float HeadContactVolumeHeightM = Config.GetFloat("heading-mechanics", "HeadContactVolumeHeightM", 0.22f);

        /// <summary>[GT] Earliest allowable signed timing offset (ms). Distinct from MaxLateToleranceMs per FR-HE-022 / pass-1 H-1. §3.1.</summary>
        public static readonly float MaxEarlyToleranceMs = Config.GetFloat("heading-mechanics", "MaxEarlyToleranceMs", 140.0f);

        /// <summary>[GT] Latest allowable signed timing offset (ms). Numerically smaller than MaxEarlyToleranceMs — late headers degrade faster. FR-HE-022 / pass-1 H-1. §3.1.</summary>
        public static readonly float MaxLateToleranceMs = Config.GetFloat("heading-mechanics", "MaxLateToleranceMs", 90.0f);

        // [DERIVED] — placed here (after MaxEarlyToleranceMs/MaxLateToleranceMs) because C# static-field
        // initialisation is declaration-order dependent and these depend on GT values above and on FrameMs [Derived].
        // The XML tags remain [DERIVED]; only the placement in the GT region is an implementation concession.

        /// <summary>
        /// [DERIVED] Early-tolerance window in 60 Hz frames (ceil, toward looser tolerance).
        /// Formula: ceil(MaxEarlyToleranceMs / FrameMs). Heading Mechanics #10 §3.2.
        /// Source constants: MaxEarlyToleranceMs (GT, above), FrameMs (Derived).
        /// Placed in GT region to ensure correct C# static-init ordering.
        /// </summary>
        public static readonly int FramesEarlyTolerance =
            Mathf.CeilToInt(MaxEarlyToleranceMs / FrameMs);

        /// <summary>
        /// [DERIVED] Late-tolerance window in 60 Hz frames (ceil, toward looser tolerance).
        /// Formula: ceil(MaxLateToleranceMs / FrameMs). Heading Mechanics #10 §3.2.
        /// Source constants: MaxLateToleranceMs (GT, above), FrameMs (Derived).
        /// Placed in GT region to ensure correct C# static-init ordering.
        /// </summary>
        public static readonly int FramesLateTolerance =
            Mathf.CeilToInt(MaxLateToleranceMs / FrameMs);

        // ── §3.3 Jump Kinematics ─────────────────────────────────────────────────────

        /// <summary>[GT] Sensitivity of JumpReach to Strength_norm (m). §3.1 / §3.3.</summary>
        public static readonly float JumpReachKStrength = Config.GetFloat("heading-mechanics", "JumpReachKStrength", 0.18f);

        /// <summary>[GT] Sensitivity of JumpReach to Balance_norm (m). §3.1 / §3.3.</summary>
        public static readonly float JumpReachKBalance = Config.GetFloat("heading-mechanics", "JumpReachKBalance", 0.10f);

        /// <summary>[GT] Sensitivity of JumpReach to Heading_norm (m). Covers jump-timing skill until §7.10. FR-HE-021 / pass-1 H-2. §3.1.</summary>
        public static readonly float JumpReachKHeading = Config.GetFloat("heading-mechanics", "JumpReachKHeading", 0.12f);

        /// <summary>[GT] Total ground-to-ground aerial phase duration (ms) for the Stage 0 synthetic trajectory. KD-18. §3.1.</summary>
        public static readonly float JumpPhaseDurationMs = Config.GetFloat("heading-mechanics", "JumpPhaseDurationMs", 650.0f);

        /// <summary>[GT] Apex location as a fraction of JumpPhaseDurationMs. [GT] not [FIXED]: trajectory is synthetic (KD-18 footnote). §3.1.</summary>
        public static readonly float JumpApexFraction = Config.GetFloat("heading-mechanics", "JumpApexFraction", 0.50f);

        // ── §3.4 Contact Quality ─────────────────────────────────────────────────────

        /// <summary>[GT] Telemetry-bucket early boundary (ms). NOT a formula gate (KD-2). §3.1.</summary>
        public static readonly float EarlyLabelThresholdMs = Config.GetFloat("heading-mechanics", "EarlyLabelThresholdMs", 40.0f);

        /// <summary>[GT] Telemetry-bucket late boundary (ms). NOT a formula gate (KD-2). §3.1.</summary>
        public static readonly float LateLabelThresholdMs = Config.GetFloat("heading-mechanics", "LateLabelThresholdMs", 40.0f);

        /// <summary>[GT] Alpha weight on timingQuality in the §3.4 convex combination. §3.1.</summary>
        public static readonly float TimingPointBlendAlpha = Config.GetFloat("heading-mechanics", "TimingPointBlendAlpha", 0.55f);

        /// <summary>[GT] Baseline denominator for pointQuality; mean point-error scale (m). §3.1.</summary>
        public static readonly float ContactPointErrorSigmaM = Config.GetFloat("heading-mechanics", "ContactPointErrorSigmaM", 0.03f);

        /// <summary>[GT] Amplitude of per-attempt point-error Gaussian noise via DRAW_SITE_CONTACT_POINT_ERROR (m). pass-1 M-4. §3.1.</summary>
        public static readonly float ContactPointNoiseSigmaM = Config.GetFloat("heading-mechanics", "ContactPointNoiseSigmaM", 0.012f);

        /// <summary>[GT] Amplitude of per-attempt timing-noise Gaussian via DRAW_SITE_TIMING_JITTER (ms). pass-1 M-4. §3.1.</summary>
        public static readonly float TimingJitterSigmaMs = Config.GetFloat("heading-mechanics", "TimingJitterSigmaMs", 8.0f);

        /// <summary>[GT] Heading-attribute scaling coefficient for contact point error. §3.1 / §3.4.</summary>
        public static readonly float ContactPointHeadingAttrCoeff = Config.GetFloat("heading-mechanics", "ContactPointHeadingAttrCoeff", 0.40f);

        // ── §3.5 Power & Launch Angle ────────────────────────────────────────────────

        /// <summary>[GT] Baseline header outgoing speed (m/s). §3.1.</summary>
        public static readonly float PowerBaseMps = Config.GetFloat("heading-mechanics", "PowerBaseMps", 7.0f);

        /// <summary>[GT] Strength contribution to outgoing speed (m/s per unit norm). §3.1.</summary>
        public static readonly float PowerKStrength = Config.GetFloat("heading-mechanics", "PowerKStrength", 4.0f);

        /// <summary>[GT] Heading-attribute contribution to outgoing speed (m/s per unit norm). §3.1.</summary>
        public static readonly float PowerKHeading = Config.GetFloat("heading-mechanics", "PowerKHeading", 5.0f);

        /// <summary>[GT] Fatigue penalty coefficient [0, 0.5]. 0 = no degradation. CLAUDE.md fatigue convention. §3.1.</summary>
        public static readonly float PowerFatigueCoeff = Config.GetFloat("heading-mechanics", "PowerFatigueCoeff", 0.18f);

        // ── §3.6 Spin Transfer ───────────────────────────────────────────────────────

        /// <summary>[GT] Multiplier on derived headAngularVelocity contribution to outgoing spin. §3.1.</summary>
        public static readonly float SpinTransferCoeff = Config.GetFloat("heading-mechanics", "SpinTransferCoeff", 0.55f);

        /// <summary>[GT] Scale-factor base for spinPreservationFactor. §3.1.</summary>
        public static readonly float SpinPreservationBase = Config.GetFloat("heading-mechanics", "SpinPreservationBase", 0.60f);

        /// <summary>[GT] Contact-point axial offset beyond which spinPreservationFactor goes negative (m). §3.1.</summary>
        public static readonly float SpinTransferReversalThreshold = Config.GetFloat("heading-mechanics", "SpinTransferReversalThreshold", 0.015f);

        // ── §3.7 Duel Resolution ─────────────────────────────────────────────────────

        /// <summary>[GT] Minimum contactQualityScalar; duel loser below this emits HeaderAttemptFailedEvent. FR-HE-026. §3.1.</summary>
        public static readonly float MinContactQuality = Config.GetFloat("heading-mechanics", "MinContactQuality", 0.20f);

        /// <summary>[GT] Balance weight w_B in §3.7 base-score formula FM-010-005. §3.1.</summary>
        public static readonly float DuelBalanceWeight = Config.GetFloat("heading-mechanics", "DuelBalanceWeight", 0.30f);

        /// <summary>[GT] Strength weight w_S in §3.7 base-score formula FM-010-005. §3.1.</summary>
        public static readonly float DuelStrengthWeight = Config.GetFloat("heading-mechanics", "DuelStrengthWeight", 0.35f);

        /// <summary>[GT] Heading weight w_H in §3.7 base-score formula FM-010-005. Sum of three weights = 1.0. §3.1.</summary>
        public static readonly float DuelHeadingWeight = Config.GetFloat("heading-mechanics", "DuelHeadingWeight", 0.35f);

        /// <summary>[GT] Match-time tolerance (s) for grouping two contact events into the same contested duel. §3.7.</summary>
        public static readonly float DuelFrameMatchToleranceS = Config.GetFloat("heading-mechanics", "DuelFrameMatchToleranceS", 0.001f);

        /// <summary>[GT] Near-tie threshold gating RNG perturbation. Non-tie scores are NEVER perturbed. FR-HE-023 / pass-1 H-5. §3.1.</summary>
        public static readonly float DuelTiebreakEpsilon = Config.GetFloat("heading-mechanics", "DuelTiebreakEpsilon", 0.02f);

        /// <summary>[GT] RNG perturbation amplitude applied only when score gap &lt; DuelTiebreakEpsilon. pass-1 H-5. §3.1.</summary>
        public static readonly float DuelTiebreakNoiseAmplitude = Config.GetFloat("heading-mechanics", "DuelTiebreakNoiseAmplitude", 0.01f);

        /// <summary>[GT] Maximum disturbance factor applied to a duel loser's contactQualityScalar. §3.1.</summary>
        public static readonly float DuelDisturbanceMax = Config.GetFloat("heading-mechanics", "DuelDisturbanceMax", 0.50f);

        /// <summary>[GT] baseScore gap at which disturbanceFactor saturates at DuelDisturbanceMax. v0.2 H-4. §3.1.</summary>
        public static readonly float DuelDisturbanceGapSaturation = Config.GetFloat("heading-mechanics", "DuelDisturbanceGapSaturation", 0.20f);

        // ── §3.8 Own-Goal Flag ───────────────────────────────────────────────────────

        /// <summary>[GT] Projection time horizon for own-goal-shape flag (s). §3.1.</summary>
        public static readonly float OwnGoalProjectionHorizonS = Config.GetFloat("heading-mechanics", "OwnGoalProjectionHorizonS", 1.2f);

        /// <summary>[GT] Projection distance horizon for own-goal-shape flag (m). pass-1 L-7. §3.1.</summary>
        public static readonly float OwnGoalProjectionHorizonM = Config.GetFloat("heading-mechanics", "OwnGoalProjectionHorizonM", 18.0f);

        /// <summary>[GT] X-axis depth (m) of the own-goal bounding box used in the §3.8 intersection test. §3.8.</summary>
        public static readonly float OwnGoalBoundingBoxDepthM = Config.GetFloat("heading-mechanics", "OwnGoalBoundingBoxDepthM", 0.5f);

        // ── §4.2.1 Buffer / Draw Sites ───────────────────────────────────────────────

        /// <summary>[GT] Pre-allocated collision-event buffer capacity for ICollisionEventConsumer (§4.2.1).
        /// Bound: 3-way duel × 2 contact-pairs × safety margin. Allocated once at Initialize(). §3.1.</summary>
        public static readonly int HeadingContactBufferCapacity = Config.GetInt("heading-mechanics", "HeadingContactBufferCapacity", 16);

        /// <summary>[GT] Maximum number of active header intents tracked simultaneously (one per agent). §4.6.</summary>
        public static readonly int MaxAgents = Config.GetInt("heading-mechanics", "MaxAgents", 22);

        /// <summary>[GT] Maximum participants tracked in a single contested duel. §3.7.</summary>
        public static readonly int MaxDuelParticipants = Config.GetInt("heading-mechanics", "MaxDuelParticipants", 8);

        /// <summary>[GT] Maximum simultaneous contested duels per physics frame. §3.7.</summary>
        public static readonly int MaxSimultaneousDuels = Config.GetInt("heading-mechanics", "MaxSimultaneousDuels", 4);

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
// | Version | Date       | Author | Notes                                                                                      |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                                    |
// | 1.1     | 2026-05-28 | —      | AR-1 fix pass: M-1 circular init resolved (TickRatePhysicsHz/TickRateTacticalHz → const    |
// |         |            |        | float; FramesEarlyTolerance/FramesLateTolerance moved to GT after MaxEarly/Late);          |
// |         |            |        | M-2 PARABOLA_AMPLITUDE [FIXED] added; M-3 ATTRIBUTE_NORM_MIDPOINT [FIXED] added;             |
// |         |            |        | M-4/M-5 GoalHalfWidthM [DERIVED] + OwnGoalBoundingBoxDepthM [GT] added;                   |
// |         |            |        | L-1/L-2 DEGENERACY_EPSILON_SQ [FIXED] added; L-3 RNG_GUARD_EPSILON [FIXED] added.             |
// | 1.2     | 2026-05-28 | —      | AR-1 fix pass (cont.): SURFACE_NORMAL_EPSILON_SQ [FIXED] added (1e-8f guards).             |
// | 1.3     | 2026-05-28 | —      | AR-2 H-1: 5 [FIXED] constants renamed PascalCase → ALL_CAPS. AR-2 M-1:                    |
// |         |            |        | DuelFrameMatchToleranceS [GT] added. AR-2 M-2: MS_PER_SECOND [FIXED], FrameS [DERIVED];  |
// |         |            |        | FrameMs formula uses MS_PER_SECOND. AR-2 M-3: PitchCentreYM [DERIVED].                  |
// |         |            |        | AR-2 M-4: REFLECTION_FORMULA_COEFF [FIXED]. AR-2 M-6: KINEMATIC_HALF_COEFF [FIXED].     |
// | 1.4     | 2026-08-09 | —      | ERR-010-002 (§3.5.1 aim realization): + KINEMATIC_TWO_COEFF, PERFECT_CONTACT_QUALITY     |
// |         |            |        | [FIXED]; + SurfaceNormalEpsilon, MaxRangeLaunchComponent [DERIVED]. No new [GT], so      |
// |         |            |        | inside the KD-W1 freeze. ROW ADDED RETROACTIVELY at the adversarial review over that     |
// |         |            |        | landing — the landing itself shipped these four constants with no version row and no     |
// |         |            |        | Modified: update, the sixth consecutive FR-CS-056/057 recurrence in this repo.           |
// | 1.5     | 2026-08-09 | —      | AR over the ERR-010-002 landing: MaxRangeLaunchComponent RETIRED. sqrt(1/2) is the       |
// |         |            |        | max-range launch component only for a target at contact height; a header contacts near   |
// |         |            |        | 2.3 m and aims at the ground, so the constant asserted in its own name something that    |
// |         |            |        | was false on essentially every real header. §3.5.1's unreachable-target branch now       |
// |         |            |        | computes tan(theta) = v / sqrt(v^2 - 2*g*dz) inline; no constant replaces it.            |
#endregion
