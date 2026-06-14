// File:     src/goalkeeper-mechanics/GoalkeeperConstants.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.4, KD-9, FR-GK-015, FR-GK-042, Code Standards #20
// Purpose:  All numeric constants for the goalkeeper mechanics system. No magic literals in formula files.
//           Region order: Fixed → Derived → Cross → GT.
//           NOTE: TickRatePhysicsHz and TickRateTacticalHz are declared const float in Cross to prevent
//           C# static-field init ordering issues in Derived (const fields are available at compile time,
//           independent of declaration order).

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// All constants for the goalkeeper mechanics system. Every symbol in §3.2–§3.8 pseudocode
    /// bodies that is a constant appears here with its source tag (KD-9 / FR-GK-015 / FR-GK-042).
    /// No magic literals permitted in any formula or system file.
    /// Goalkeeper Mechanics #11 §3.4.
    /// </summary>
    public static class GoalkeeperConstants
    {
        #region Fixed

        /// <summary>[FIXED] Maximum ticks a GK may hold the ball (6-second rule at 10 Hz tactical rate).
        /// Laws of the Game; Goalkeeper Mechanics #11 §3.1 / FR-GK-028.</summary>
        public const int GK_HOLD_MAX_TICKS = 60;

        /// <summary>[FIXED] Milliseconds per second. Unit-conversion coefficient for ms ↔ s throughout §3.2–§3.8.</summary>
        public const float MS_PER_SECOND = 1000.0f;

        /// <summary>[FIXED] Coefficient in the standard parabolic arc formula 4u(1−u) giving peak = 1 at u = 0.5.
        /// Used in §3.3.3 synthetic dive Z trajectory. §3.3.</summary>
        public const float PARABOLA_AMPLITUDE = 4.0f;

        /// <summary>[FIXED] Squared-magnitude epsilon for degenerate-vector guards (avoids division by zero). §3.3 / §3.5.</summary>
        public const float DEGENERACY_EPSILON_SQ = 1e-6f;

        /// <summary>[FIXED] Minimum value of u1 in any Box-Muller transform, guarding against log(0). §3.5 / §3.6 RNG.</summary>
        public const float RNG_GUARD_EPSILON = 1e-7f;

        /// <summary>[FIXED] Maximum value for any player attribute [1–20]. Agent Movement #2 §3.5.6.</summary>
        public const float ATTR_MAX = 20.0f;

        /// <summary>[FIXED] Minimum attribute floor for normalisation clamping. §3.3 / §3.5.</summary>
        public const float ATTR_MIN = 1.0f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Physics frame duration in milliseconds.
        /// Formula: MS_PER_SECOND / TickRatePhysicsHz. Goalkeeper Mechanics #11 §3.4.9.
        /// Source constants: MS_PER_SECOND (Fixed const), TickRatePhysicsHz (Cross const — always available).
        /// </summary>
        public static readonly float FrameMs = MS_PER_SECOND / TickRatePhysicsHz;

        /// <summary>
        /// [DERIVED] Number of 60 Hz physics frames per 10 Hz tactical tick.
        /// Formula: TickRatePhysicsHz / TickRateTacticalHz. Goalkeeper Mechanics #11 §3.4.9.
        /// Source constants: TickRatePhysicsHz, TickRateTacticalHz (both const — always available).
        /// </summary>
        public static readonly int FramesPerTacticalTick =
            (int)(TickRatePhysicsHz / TickRateTacticalHz);

        /// <summary>
        /// [DERIVED] X coordinate of the ball attacking-third boundary (m).
        /// Formula: 2 * PitchLengthM / 3. Goalkeeper Mechanics #11 §3.1 / §3.4.2.
        /// Source constants: PitchLengthM (Cross const — always available).
        /// </summary>
        public static readonly float BallAttackingThirdXM = 2f * BallPhysicsConstants.Pitch.LENGTH / 3f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Gravitational acceleration (m/s²).
        /// Authoritative source: BallPhysicsConstants.Environment.GRAVITY. Ball Physics #1 §1.2.
        /// Value: 9.81 m/s². STAGE 0 STATUS: declared-but-unconsumed — the Stage 0 synthetic dive Z
        /// is a fixed parabola (§3.3.3), not a gravity-integrated trajectory; consumed at Stage 1 when
        /// the dive binds to AM #2 native Z kinematics.
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
        /// [CROSS] Penalty area depth (m).
        /// Authoritative source: Ball Physics #1 (anchor pinned during drafting per §3.4.9).
        /// Value: 16.5 m. STAGE 0 STATUS: declared-but-unconsumed — handling-area / pickup-legality
        /// gating on the penalty box is not yet implemented; consumed at Stage 1.
        /// TODO: mirror from BallPhysicsConstants.Pitch.PENALTY_AREA_DEPTH when that constant is added.
        /// </summary>
        public static readonly float PenaltyAreaDepthM = 16.5f; // TODO: replace with BallPhysicsConstants.Pitch.PENALTY_AREA_DEPTH

        /// <summary>
        /// [CROSS] Goalkeeper subsystem domain tag.
        /// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER.
        /// Deterministic Simulation #16 §3.4 v1.0.5 — ERR-011-001 resolved May 18, 2026.
        /// Value: 0x1D. TODO: mirror from DeterministicSimConstants.DOMAIN_TAG_GOALKEEPER when that file exists.
        /// </summary>
        public static readonly uint DomainTagGoalkeeper = 0x1Du; // TODO: mirror from DeterministicSimConstants

        /// <summary>
        /// [CROSS] Perception System base latency (ms) at §3.2 perceptionLatencyScale.
        /// Authoritative source: Perception System #7 §3 (visibility-cone latency surface).
        /// Value: 120.0f — Stage 0 stub pending #7 implementation. TODO: mirror from PerceptionConstants.BaseLatencyMs.
        /// </summary>
        public static readonly float PerceptionBaseLatencyMs = 120.0f; // TODO: replace with PerceptionConstants.BaseLatencyMs (Stage 1)

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

        // ── §3.4.1 GK Volume / Reach ─────────────────────────────────────────────────

        /// <summary>[GT] Effective radius (m) of the sphere around GK that admits save-contact eligibility. §3.5 eligibility.</summary>
        public static readonly float GkSaveVolumeRadiusM = 5.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Radius (m) within which GK may micro-adjust from the #12 baseline in Resting/Set states. §3.3.0 / KD-13.</summary>
        public static readonly float GkReactiveRadiusM = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] GK arm length baseline (m). §3.3.4.</summary>
        public static readonly float ArmLengthM = 0.80f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Additional body-reach extension during dive (m). §3.3.4.</summary>
        public static readonly float DiveBodyReachExtensionM = 0.55f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lateral XY displacement of reach centre along dive direction at end of dive phase (m). §3.3.4.</summary>
        public static readonly float DiveLaunchDisplacementM = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Handling-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKHandling = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerial-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKAerial = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKStrength = 0.10f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty on reach radius (m per unit fatigue). §3.3.4 / KD-8.</summary>
        public static readonly float ReachFatigueCoeff = 0.18f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.2 GK Timing / Hold-Rule ─────────────────────────────────────────────

        /// <summary>[GT] Recovery-to-line cooldown in 10 Hz tactical ticks. §3.1.</summary>
        public static readonly int RecoveryCooldownTicks = 6; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Decision Tree anticipation score threshold for Resting → Anticipate transition. §3.1 / #8.</summary>
        public static readonly float AnticipateThreshold = 0.55f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Decision Tree rush-commitment level threshold for Set/Anticipate → Rushing transition. §3.1 / §3.7.</summary>
        public static readonly float RushCommitThreshold = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Radius (m) within which an attacker triggers OneOnOne state during rush. §3.7.</summary>
        public static readonly float OneVsOneTriggerRadiusM = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Radius (m) within which GK closes to smother the attacker. §3.7.</summary>
        public static readonly float SmotherTriggerRadiusM = 1.8f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.3 Reaction-Pipeline Constants ───────────────────────────────────────

        /// <summary>[GT] Baseline required reaction time (ms). §3.2.</summary>
        public static readonly float ReactionBaseMs = 350f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Reflexes-attribute reduction of required reaction time (ms per unit norm). §3.2.</summary>
        public static readonly float ReactionReflexesCoeff = 100f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball-speed penalty on required reaction time (ms per m/s above reference). §3.2.</summary>
        public static readonly float ReactionBallSpeedCoeff = 8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball-speed reference (m/s) above which speed penalty applies. §3.2.</summary>
        public static readonly float ReactionBallSpeedRefMps = 18f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Early-reaction tolerance (ms) before reactionWindowAchieved decays. §3.2 / KD-18.</summary>
        public static readonly float ReactionEarlyToleranceMs = 120f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Late-reaction tolerance (ms); numerically smaller than early (late commits decay faster). §3.2 / KD-18.</summary>
        public static readonly float ReactionLateToleranceMs = 80f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] reactionWindowAchieved threshold above which label = Reflexive (telemetry only; KD-2). §3.2.</summary>
        public static readonly float ReflexiveLabelThreshold = 0.75f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] reactionWindowAchieved threshold below which label = Sluggish (telemetry only; KD-2). §3.2.</summary>
        public static readonly float SluggishLabelThreshold = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Reflexes-attribute scale factor on perceptionLatencyScale. §3.2.</summary>
        public static readonly float PerceptionReflexesScale = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] OneVsOne subtractive coefficient on requiredReactionMs (positive value → REDUCES required time). §3.2 / KD-20.</summary>
        public static readonly float OneVsOneReactionCoeff = 40f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.4 Dive-Kinematics Constants ─────────────────────────────────────────

        /// <summary>[GT] Baseline dive launch impulse (m/s). §3.3.</summary>
        public static readonly float DiveLaunchBaseMps = 3.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength-attribute contribution to dive launch impulse (m/s per unit norm). §3.3.</summary>
        public static readonly float DiveLaunchKStrength = 1.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerial-attribute contribution to dive launch impulse (m/s per unit norm). §3.3.</summary>
        public static readonly float DiveLaunchKAerial = 0.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty on dive launch impulse (m/s per unit fatigue). §3.3 / KD-8.</summary>
        public static readonly float DiveLaunchFatigueCoeff = 0.7f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Dive airborne phase duration (ms). Stage 0 flat; attribute-scaling deferred per §7.4. §3.3.</summary>
        public static readonly float DivePhaseDurationMs = 600f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Baseline peak hand Z (m) at dive apex. §3.3.</summary>
        public static readonly float DivePeakZBaseM = 1.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerial-attribute contribution to peak hand Z (m per unit norm). §3.3.</summary>
        public static readonly float DivePeakZKAerial = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength-attribute contribution to peak hand Z (m per unit norm). §3.3.</summary>
        public static readonly float DivePeakZKStrength = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty on peak hand Z (m per unit fatigue). §3.3 / KD-8.</summary>
        public static readonly float DiveFatiguePeakZCoeff = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Standard deviation of dive timing jitter Gaussian (ms). §3.3.</summary>
        public static readonly float DiveTimingJitterSigmaMs = 25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Scaling of dive timing jitter on peak hand Z (m per ms of jitter). §3.3.</summary>
        public static readonly float DiveJitterPeakZCoeff = 0.002f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum XY displacement (m) before a dive is classified as wrong-direction (F-02). §2.3 F-02.
        /// STAGE 0 STATUS: declared-but-unconsumed — the F-02 wrong-direction failure-mode classification
        /// is not yet implemented in GoalkeeperMechanics (failed dives route to Recovering without an
        /// F-01/F-02/F-03 FailureCause split). Wired at Stage 1 with the §3.9 failed-save pipeline.</summary>
        public static readonly float WrongDirectionThresholdM = 1.5f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.5 Handling-Quality Constants ────────────────────────────────────────

        /// <summary>[GT] Baseline handling-quality factor (attribute-independent floor). §3.5.</summary>
        public static readonly float HandlingBase = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Handling-attribute contribution coefficient. §3.5.</summary>
        public static readonly float HandlingKAttr = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball-speed penalty coefficient (dimensionless; formula divides by ref). §3.5.</summary>
        public static readonly float HandlingKBallSpeed = 0.025f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball-speed reference (m/s) above which speed penalty applies. §3.5.</summary>
        public static readonly float HandlingBallSpeedRefMps = 20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty on handling quality. §3.5 / KD-8.</summary>
        public static readonly float HandlingFatigueCoeff = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Standard deviation of handling-scale noise Gaussian. §3.5 / KD-7.</summary>
        public static readonly float HandlingNoiseSigma = 0.06f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Standard deviation of contact-point-error noise Gaussian (m). §3.5 / KD-7.</summary>
        public static readonly float HandlingPointErrorSigmaM = 0.05f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Blend weight for rawHandling vs reactionWindowAchieved in final quality scalar. §3.5 / KD-2.</summary>
        public static readonly float HandlingReactionBlendAlpha = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK catches the ball. §3.5 / KD-21.</summary>
        public static readonly float CatchThreshold = 0.78f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK parries. §3.5 / KD-21.</summary>
        public static readonly float ParryThreshold = 0.55f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK deflects. §3.5 / KD-21.</summary>
        public static readonly float DeflectThreshold = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum handlingQualityScalar for any physical contact (below → Missed). §3.5.</summary>
        public static readonly float MinHandlingQuality = 0.10f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Baseline velocity-retain coefficient in parry. §3.5.</summary>
        public static readonly float ParryVelocityRetainBase = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Quality-sensitive reduction of parry retain (higher quality → less rebound). §3.5.</summary>
        public static readonly float ParryVelocityRetainKQuality = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Standard deviation of parry/deflect/spill deflection angle perturbation (rad). §3.5.</summary>
        public static readonly float ParryDeflectAngleSigmaRad = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] ClutchFirmness contribution to reducing parry retain (committed catch attempt → smaller rebound). §3.5.</summary>
        public static readonly float ClutchFirmnessKRetain = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] OneVsOne attribute bonus on handling quality. §3.5 / KD-20.</summary>
        public static readonly float OneVsOneHandlingCoeff = 0.12f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Extra velocity-retain bonus for deflect path over parry path (deflections retain slightly more). §3.5.3.</summary>
        public static readonly float DeflectRetainBonus = 0.10f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Extra velocity-retain bonus for spill path over parry path (poor handling preserves more incoming energy). §3.5.3.</summary>
        public static readonly float SpillRetainBonus = 0.20f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.6 Rush-Dispatch Constants ───────────────────────────────────────────

        /// <summary>[GT] Baseline rush launch speed (m/s). §3.7.</summary>
        public static readonly float RushLaunchBaseMps = 4.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Pace-attribute contribution to rush launch speed (m/s per unit norm). §3.7.</summary>
        public static readonly float RushLaunchKPace = 1.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue penalty on rush commit speed (m/s per unit fatigue). §3.7 / KD-8.</summary>
        public static readonly float RushCommitFatigueCoeff = 0.9f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.7 Distribution-Geometry Constants ───────────────────────────────────

        /// <summary>[GT] Release height for throw distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float ThrowReleaseHeightM = 1.95f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Release height for roll distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float RollReleaseHeightM = 0.15f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Release height for kick distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float KickReleaseHeightM = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup duration for throw distributions (ms). §3.8.</summary>
        public static readonly float ThrowWindupMs = 700f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup duration for roll distributions (ms). §3.8.</summary>
        public static readonly float RollWindupMs = 400f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup duration for kick distributions (ms). §3.8.</summary>
        public static readonly float KickWindupMs = 900f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Accuracy coefficient for throw; multiplies powerIntent by this × Throwing_norm. §3.8.1.</summary>
        public static readonly float ThrowAccuracyCoeff = 0.85f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Accuracy coefficient for kick; multiplies powerIntent by this × Kicking_norm. §3.8.1.</summary>
        public static readonly float KickAccuracyCoeff = 0.85f; // TODO: replace with config loader (Stage 1)

        // ── §3.4.8 Cross-Claim Duel Constants ────────────────────────────────────────

        /// <summary>[GT] Radius (m) of the contest volume for cross / aerial claim duels. §3.6.</summary>
        public static readonly float CrossClaimVolumeRadiusM = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance-attribute weight in cross-claim duel score. §3.6.</summary>
        public static readonly float CrossClaimDuelBalanceW = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Strength-attribute weight in cross-claim duel score. §3.6.</summary>
        public static readonly float CrossClaimDuelStrengthW = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerial-attribute weight in cross-claim duel score. Weights sum to 1.0. §3.6.</summary>
        public static readonly float CrossClaimDuelAerialW = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Score gap below which near-tie tiebreak perturbation is applied. §3.6.</summary>
        public static readonly float CrossClaimTiebreakEpsilon = 0.03f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Gaussian perturbation amplitude for cross-claim near-tie tiebreak. §3.6 / KD-7.</summary>
        public static readonly float CrossClaimTiebreakNoiseAmplitude = 0.015f; // TODO: replace with config loader (Stage 1)

        // ── §4.4.2 Buffer / System Constants ─────────────────────────────────────────

        /// <summary>[GT] Maximum simultaneous GK agents (one per side). §4.1 / §4.4.2.</summary>
        public static readonly int MaxGkAgents = 2; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum participants in a single cross-claim duel (per §3.6 buffer allocation). §3.6.</summary>
        public static readonly int MaxCrossClaimParticipants = 8; // TODO: replace with config loader (Stage 1)

        // ── Draw-Site IDs (registered with Deterministic Simulation #16 §4.5) ────────

        /// <summary>[GT] Draw-site ID for §3.5.1 handling-scale noise Gaussian (NextGaussian). #11 §4.4.2.</summary>
        public static readonly int DrawSiteHandlingNoise = 20; // TODO: register with #16 §4.5

        /// <summary>[GT] Draw-site ID for §3.5.1 contact-point-error noise Gaussian (NextGaussian). #11 §4.4.2.</summary>
        public static readonly int DrawSiteHandlingPointNoise = 21; // TODO: register with #16 §4.5

        /// <summary>[GT] Draw-site ID for §3.3.2 dive timing jitter Gaussian (NextGaussian). #11 §4.4.2.</summary>
        public static readonly int DrawSiteDiveTimingJitter = 22; // TODO: register with #16 §4.5

        /// <summary>[GT] Draw-site ID for §3.6.3 cross-claim near-tie tiebreak Gaussian (NextGaussian). #11 §4.4.2.</summary>
        public static readonly int DrawSiteCrossClaimTiebreak = 23; // TODO: register with #16 §4.5

        // ── Stage 0 Stubs / Approximations ───────────────────────────────────────────

        /// <summary>[GT] Stage 0 stub: anticipation score used when ball is in attacking third (full DT integration at Stage 1). §3.1.</summary>
        public static readonly float Stage0AnticipationScoreActive = 0.6f; // TODO: replace with Decision Tree #8 anticipation score (Stage 1)

        /// <summary>[GT] Stage 0 stub: fallback deflection projection distance (m) when SaveIntent has no DeflectionTarget. §3.5.3.</summary>
        public static readonly float Stage0DeflectFallbackProjectionM = 5.0f; // TODO: replace with proper fallback (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                          |
// | 1.1     | 2026-06-14 | —      | AR-6 L: Stage-0 declared-but-unconsumed doc-notes on GravityMps2 |
// |         |            |        | (synthetic dive Z is a fixed parabola), PenaltyAreaDepthM, and   |
// |         |            |        | WrongDirectionThresholdM (F-02 classification not yet wired).    |
#endregion
