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
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

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

        /// <summary>
        /// [DERIVED] Scalar epsilon for degenerate-component guards (avoids division by zero on a single
        /// axis, where the squared form above is the wrong scale).
        /// Formula: sqrt(DEGENERACY_EPSILON_SQ). Source constants: GoalkeeperConstants.DEGENERACY_EPSILON_SQ.
        /// Consumed by the §3.3.4 dive-direction interception guard (ERR-011-003). §3.3.
        /// </summary>
        public static readonly float DEGENERACY_EPSILON = Mathf.Sqrt(DEGENERACY_EPSILON_SQ);

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
        /// Deterministic Simulation #16 §3.4 v1.0.5 — ERR-011-002 resolved May 18, 2026.
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
        public static readonly float GkSaveVolumeRadiusM = Config.GetFloat("goalkeeper-mechanics", "GkSaveVolumeRadiusM", 5.5f);

        /// <summary>[GT] Radius (m) within which GK may micro-adjust from the #12 baseline in Resting/Set states. §3.3.0 / KD-13.</summary>
        public static readonly float GkReactiveRadiusM = Config.GetFloat("goalkeeper-mechanics", "GkReactiveRadiusM", 1.5f);

        /// <summary>[GT] GK arm length baseline (m). §3.3.4.</summary>
        public static readonly float ArmLengthM = Config.GetFloat("goalkeeper-mechanics", "ArmLengthM", 0.80f);

        /// <summary>[GT] Additional body-reach extension during dive (m). §3.3.4.</summary>
        public static readonly float DiveBodyReachExtensionM = Config.GetFloat("goalkeeper-mechanics", "DiveBodyReachExtensionM", 0.55f);

        /// <summary>[GT] Lateral XY displacement of reach centre along dive direction at end of dive phase (m). §3.3.4.</summary>
        public static readonly float DiveLaunchDisplacementM = Config.GetFloat("goalkeeper-mechanics", "DiveLaunchDisplacementM", 2.2f);

        /// <summary>
        /// [GT] Horizon (s) over which the §3.3.4 dive-direction interception extrapolates the ball to the
        /// keeper's plane (ERR-011-003). Beyond it the linear model is not credible — the ball will be
        /// touched, deflected or will bounce first — so the keeper dives at the ball's current lateral
        /// position instead of at a stale extrapolation. Sized just above the flight time of the slowest
        /// ball that can arm a save (GkSaveTriggerRangeM 16.5 m at GkSaveTriggerMinBallSpeedMps 3 m/s is
        /// 5.5 s, but a save that is 5 s away is not a dive), so it bounds the model without gating any
        /// realistic shot. Config key [goalkeeper-mechanics] DivePredictionHorizonS.
        /// </summary>
        public static readonly float DivePredictionHorizonS =
            Config.GetFloat("goalkeeper-mechanics", "DivePredictionHorizonS", 2.0f);

        /// <summary>[GT] Handling-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKHandling = Config.GetFloat("goalkeeper-mechanics", "ReachKHandling", 0.20f);

        /// <summary>[GT] Aerial-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKAerial = Config.GetFloat("goalkeeper-mechanics", "ReachKAerial", 0.35f);

        /// <summary>[GT] Strength-attribute contribution to reach radius (m per unit norm). §3.3.4.</summary>
        public static readonly float ReachKStrength = Config.GetFloat("goalkeeper-mechanics", "ReachKStrength", 0.10f);

        /// <summary>[GT] Fatigue penalty on reach radius (m per unit fatigue). §3.3.4 / KD-8.</summary>
        public static readonly float ReachFatigueCoeff = Config.GetFloat("goalkeeper-mechanics", "ReachFatigueCoeff", 0.18f);

        // ── §3.4.2 GK Timing / Hold-Rule ─────────────────────────────────────────────

        /// <summary>[GT] Recovery-to-line cooldown in 10 Hz tactical ticks. §3.1.</summary>
        public static readonly int RecoveryCooldownTicks = Config.GetInt("goalkeeper-mechanics", "RecoveryCooldownTicks", 6);

        /// <summary>[GT] Decision Tree anticipation score threshold for Resting → Anticipate transition. §3.1 / #8.</summary>
        public static readonly float AnticipateThreshold = Config.GetFloat("goalkeeper-mechanics", "AnticipateThreshold", 0.55f);

        /// <summary>[GT] Decision Tree rush-commitment level threshold for Set/Anticipate → Rushing transition. §3.1 / §3.7.</summary>
        public static readonly float RushCommitThreshold = Config.GetFloat("goalkeeper-mechanics", "RushCommitThreshold", 0.60f);

        /// <summary>[GT] Radius (m) within which an attacker triggers OneOnOne state during rush. §3.7.</summary>
        public static readonly float OneVsOneTriggerRadiusM = Config.GetFloat("goalkeeper-mechanics", "OneVsOneTriggerRadiusM", 8.0f);

        /// <summary>[GT] Radius (m) within which GK closes to smother the attacker. §3.7.</summary>
        public static readonly float SmotherTriggerRadiusM = Config.GetFloat("goalkeeper-mechanics", "SmotherTriggerRadiusM", 1.8f);

        // ── §3.4.3 Reaction-Pipeline Constants ───────────────────────────────────────

        /// <summary>[GT] Baseline required reaction time (ms). §3.2.</summary>
        public static readonly float ReactionBaseMs = Config.GetFloat("goalkeeper-mechanics", "ReactionBaseMs", 350f);

        /// <summary>[GT] Reflexes-attribute reduction of required reaction time (ms per unit norm). §3.2.</summary>
        public static readonly float ReactionReflexesCoeff = Config.GetFloat("goalkeeper-mechanics", "ReactionReflexesCoeff", 100f);

        /// <summary>[GT] Ball-speed penalty on required reaction time (ms per m/s above reference). §3.2.</summary>
        public static readonly float ReactionBallSpeedCoeff = Config.GetFloat("goalkeeper-mechanics", "ReactionBallSpeedCoeff", 8f);

        /// <summary>[GT] Ball-speed reference (m/s) above which speed penalty applies. §3.2.</summary>
        public static readonly float ReactionBallSpeedRefMps = Config.GetFloat("goalkeeper-mechanics", "ReactionBallSpeedRefMps", 18f);

        /// <summary>[GT] Early-reaction tolerance (ms) before reactionWindowAchieved decays. §3.2 / KD-18.</summary>
        public static readonly float ReactionEarlyToleranceMs = Config.GetFloat("goalkeeper-mechanics", "ReactionEarlyToleranceMs", 120f);

        /// <summary>[GT] Late-reaction tolerance (ms); numerically smaller than early (late commits decay faster). §3.2 / KD-18.</summary>
        public static readonly float ReactionLateToleranceMs = Config.GetFloat("goalkeeper-mechanics", "ReactionLateToleranceMs", 80f);

        /// <summary>[GT] reactionWindowAchieved threshold above which label = Reflexive (telemetry only; KD-2). §3.2.</summary>
        public static readonly float ReflexiveLabelThreshold = Config.GetFloat("goalkeeper-mechanics", "ReflexiveLabelThreshold", 0.75f);

        /// <summary>[GT] reactionWindowAchieved threshold below which label = Sluggish (telemetry only; KD-2). §3.2.</summary>
        public static readonly float SluggishLabelThreshold = Config.GetFloat("goalkeeper-mechanics", "SluggishLabelThreshold", 0.30f);

        /// <summary>[GT] Reflexes-attribute scale factor on perceptionLatencyScale. §3.2.</summary>
        public static readonly float PerceptionReflexesScale = Config.GetFloat("goalkeeper-mechanics", "PerceptionReflexesScale", 0.30f);

        /// <summary>[GT] OneVsOne subtractive coefficient on requiredReactionMs (positive value → REDUCES required time). §3.2 / KD-20.</summary>
        public static readonly float OneVsOneReactionCoeff = Config.GetFloat("goalkeeper-mechanics", "OneVsOneReactionCoeff", 40f);

        // ── §3.4.4 Dive-Kinematics Constants ─────────────────────────────────────────

        /// <summary>[GT] Baseline dive launch impulse (m/s). §3.3.</summary>
        public static readonly float DiveLaunchBaseMps = Config.GetFloat("goalkeeper-mechanics", "DiveLaunchBaseMps", 3.5f);

        /// <summary>[GT] Strength-attribute contribution to dive launch impulse (m/s per unit norm). §3.3.</summary>
        public static readonly float DiveLaunchKStrength = Config.GetFloat("goalkeeper-mechanics", "DiveLaunchKStrength", 1.2f);

        /// <summary>[GT] Aerial-attribute contribution to dive launch impulse (m/s per unit norm). §3.3.</summary>
        public static readonly float DiveLaunchKAerial = Config.GetFloat("goalkeeper-mechanics", "DiveLaunchKAerial", 0.8f);

        /// <summary>[GT] Fatigue penalty on dive launch impulse (m/s per unit fatigue). §3.3 / KD-8.</summary>
        public static readonly float DiveLaunchFatigueCoeff = Config.GetFloat("goalkeeper-mechanics", "DiveLaunchFatigueCoeff", 0.7f);

        /// <summary>[GT] Dive airborne phase duration (ms). Stage 0 flat; attribute-scaling deferred per §7.4. §3.3.</summary>
        public static readonly float DivePhaseDurationMs = Config.GetFloat("goalkeeper-mechanics", "DivePhaseDurationMs", 600f);

        /// <summary>[GT] Baseline peak hand Z (m) at dive apex. §3.3.</summary>
        public static readonly float DivePeakZBaseM = Config.GetFloat("goalkeeper-mechanics", "DivePeakZBaseM", 1.20f);

        /// <summary>[GT] Aerial-attribute contribution to peak hand Z (m per unit norm). §3.3.</summary>
        public static readonly float DivePeakZKAerial = Config.GetFloat("goalkeeper-mechanics", "DivePeakZKAerial", 0.70f);

        /// <summary>[GT] Strength-attribute contribution to peak hand Z (m per unit norm). §3.3.</summary>
        public static readonly float DivePeakZKStrength = Config.GetFloat("goalkeeper-mechanics", "DivePeakZKStrength", 0.30f);

        /// <summary>[GT] Fatigue penalty on peak hand Z (m per unit fatigue). §3.3 / KD-8.</summary>
        public static readonly float DiveFatiguePeakZCoeff = Config.GetFloat("goalkeeper-mechanics", "DiveFatiguePeakZCoeff", 0.20f);

        /// <summary>[GT] Standard deviation of dive timing jitter Gaussian (ms). §3.3.</summary>
        public static readonly float DiveTimingJitterSigmaMs = Config.GetFloat("goalkeeper-mechanics", "DiveTimingJitterSigmaMs", 25f);

        /// <summary>[GT] Scaling of dive timing jitter on peak hand Z (m per ms of jitter). §3.3.</summary>
        public static readonly float DiveJitterPeakZCoeff = Config.GetFloat("goalkeeper-mechanics", "DiveJitterPeakZCoeff", 0.002f);

        /// <summary>[GT] Minimum XY displacement (m) before a dive is classified as wrong-direction (F-02). §2.3 F-02.
        /// STAGE 0 STATUS: declared-but-unconsumed — the F-02 wrong-direction failure-mode classification
        /// is not yet implemented in GoalkeeperMechanics (failed dives route to Recovering without an
        /// F-01/F-02/F-03 FailureCause split). Wired at Stage 1 with the §3.9 failed-save pipeline.</summary>
        public static readonly float WrongDirectionThresholdM = Config.GetFloat("goalkeeper-mechanics", "WrongDirectionThresholdM", 1.5f);

        // ── §3.4.5 Handling-Quality Constants ────────────────────────────────────────

        /// <summary>[GT] Baseline handling-quality factor (attribute-independent floor). §3.5.</summary>
        public static readonly float HandlingBase = Config.GetFloat("goalkeeper-mechanics", "HandlingBase", 0.45f);

        /// <summary>[GT] Handling-attribute contribution coefficient. §3.5.</summary>
        public static readonly float HandlingKAttr = Config.GetFloat("goalkeeper-mechanics", "HandlingKAttr", 0.45f);

        /// <summary>[GT] Ball-speed penalty coefficient (dimensionless; formula divides by ref). §3.5.</summary>
        public static readonly float HandlingKBallSpeed = Config.GetFloat("goalkeeper-mechanics", "HandlingKBallSpeed", 0.025f);

        /// <summary>[GT] Ball-speed reference (m/s) above which speed penalty applies. §3.5.</summary>
        public static readonly float HandlingBallSpeedRefMps = Config.GetFloat("goalkeeper-mechanics", "HandlingBallSpeedRefMps", 20f);

        /// <summary>[GT] Fatigue penalty on handling quality. §3.5 / KD-8.</summary>
        public static readonly float HandlingFatigueCoeff = Config.GetFloat("goalkeeper-mechanics", "HandlingFatigueCoeff", 0.20f);

        /// <summary>[GT] Standard deviation of handling-scale noise Gaussian. §3.5 / KD-7.</summary>
        public static readonly float HandlingNoiseSigma = Config.GetFloat("goalkeeper-mechanics", "HandlingNoiseSigma", 0.06f);

        /// <summary>[GT] Standard deviation of contact-point-error noise Gaussian (m). §3.5 / KD-7.</summary>
        public static readonly float HandlingPointErrorSigmaM = Config.GetFloat("goalkeeper-mechanics", "HandlingPointErrorSigmaM", 0.05f);

        /// <summary>[GT] Blend weight for rawHandling vs reactionWindowAchieved in final quality scalar. §3.5 / KD-2.</summary>
        public static readonly float HandlingReactionBlendAlpha = Config.GetFloat("goalkeeper-mechanics", "HandlingReactionBlendAlpha", 0.70f);

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK catches the ball. §3.5 / KD-21.</summary>
        public static readonly float CatchThreshold = Config.GetFloat("goalkeeper-mechanics", "CatchThreshold", 0.78f);

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK parries. §3.5 / KD-21.</summary>
        public static readonly float ParryThreshold = Config.GetFloat("goalkeeper-mechanics", "ParryThreshold", 0.55f);

        /// <summary>[GT] handlingQualityScalar threshold at or above which the GK deflects. §3.5 / KD-21.</summary>
        public static readonly float DeflectThreshold = Config.GetFloat("goalkeeper-mechanics", "DeflectThreshold", 0.30f);

        /// <summary>[GT] Minimum handlingQualityScalar for any physical contact (below → Missed). §3.5.</summary>
        public static readonly float MinHandlingQuality = Config.GetFloat("goalkeeper-mechanics", "MinHandlingQuality", 0.10f);

        /// <summary>[GT] Baseline velocity-retain coefficient in parry. §3.5.</summary>
        public static readonly float ParryVelocityRetainBase = Config.GetFloat("goalkeeper-mechanics", "ParryVelocityRetainBase", 0.45f);

        /// <summary>[GT] Quality-sensitive reduction of parry retain (higher quality → less rebound). §3.5.</summary>
        public static readonly float ParryVelocityRetainKQuality = Config.GetFloat("goalkeeper-mechanics", "ParryVelocityRetainKQuality", 0.30f);

        /// <summary>[GT] Standard deviation of parry/deflect/spill deflection angle perturbation (rad). §3.5.</summary>
        public static readonly float ParryDeflectAngleSigmaRad = Config.GetFloat("goalkeeper-mechanics", "ParryDeflectAngleSigmaRad", 0.20f);

        /// <summary>[GT] ClutchFirmness contribution to reducing parry retain (committed catch attempt → smaller rebound). §3.5.</summary>
        public static readonly float ClutchFirmnessKRetain = Config.GetFloat("goalkeeper-mechanics", "ClutchFirmnessKRetain", 0.30f);

        /// <summary>[GT] OneVsOne attribute bonus on handling quality. §3.5 / KD-20.</summary>
        public static readonly float OneVsOneHandlingCoeff = Config.GetFloat("goalkeeper-mechanics", "OneVsOneHandlingCoeff", 0.12f);

        /// <summary>[GT] Extra velocity-retain bonus for deflect path over parry path (deflections retain slightly more). §3.5.3.</summary>
        public static readonly float DeflectRetainBonus = Config.GetFloat("goalkeeper-mechanics", "DeflectRetainBonus", 0.10f);

        /// <summary>[GT] Extra velocity-retain bonus for spill path over parry path (poor handling preserves more incoming energy). §3.5.3.</summary>
        public static readonly float SpillRetainBonus = Config.GetFloat("goalkeeper-mechanics", "SpillRetainBonus", 0.20f);

        // ── §3.4.6 Rush-Dispatch Constants ───────────────────────────────────────────

        /// <summary>[GT] Baseline rush launch speed (m/s). §3.7.</summary>
        public static readonly float RushLaunchBaseMps = Config.GetFloat("goalkeeper-mechanics", "RushLaunchBaseMps", 4.5f);

        /// <summary>[GT] Pace-attribute contribution to rush launch speed (m/s per unit norm). §3.7.</summary>
        public static readonly float RushLaunchKPace = Config.GetFloat("goalkeeper-mechanics", "RushLaunchKPace", 1.8f);

        /// <summary>[GT] Fatigue penalty on rush commit speed (m/s per unit fatigue). §3.7 / KD-8.</summary>
        public static readonly float RushCommitFatigueCoeff = Config.GetFloat("goalkeeper-mechanics", "RushCommitFatigueCoeff", 0.9f);

        // ── §3.4.7 Distribution-Geometry Constants ───────────────────────────────────

        /// <summary>[GT] Release height for throw distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float ThrowReleaseHeightM = Config.GetFloat("goalkeeper-mechanics", "ThrowReleaseHeightM", 1.95f);

        /// <summary>[GT] Release height for roll distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float RollReleaseHeightM = Config.GetFloat("goalkeeper-mechanics", "RollReleaseHeightM", 0.15f);

        /// <summary>[GT] Release height for kick distributions (m above gkPosition.z). §3.8.</summary>
        public static readonly float KickReleaseHeightM = Config.GetFloat("goalkeeper-mechanics", "KickReleaseHeightM", 0.20f);

        /// <summary>[GT] Windup duration for throw distributions (ms). §3.8.</summary>
        public static readonly float ThrowWindupMs = Config.GetFloat("goalkeeper-mechanics", "ThrowWindupMs", 700f);

        /// <summary>[GT] Windup duration for roll distributions (ms). §3.8.</summary>
        public static readonly float RollWindupMs = Config.GetFloat("goalkeeper-mechanics", "RollWindupMs", 400f);

        /// <summary>[GT] Windup duration for kick distributions (ms). §3.8.</summary>
        public static readonly float KickWindupMs = Config.GetFloat("goalkeeper-mechanics", "KickWindupMs", 900f);

        /// <summary>[GT] Accuracy coefficient for throw; multiplies powerIntent by this × Throwing_norm. §3.8.1.</summary>
        public static readonly float ThrowAccuracyCoeff = Config.GetFloat("goalkeeper-mechanics", "ThrowAccuracyCoeff", 0.85f);

        /// <summary>[GT] Accuracy coefficient for kick; multiplies powerIntent by this × Kicking_norm. §3.8.1.</summary>
        public static readonly float KickAccuracyCoeff = Config.GetFloat("goalkeeper-mechanics", "KickAccuracyCoeff", 0.85f);

        // ── §3.4.8 Cross-Claim Duel Constants ────────────────────────────────────────

        /// <summary>[GT] Radius (m) of the contest volume for cross / aerial claim duels. §3.6.</summary>
        public static readonly float CrossClaimVolumeRadiusM = Config.GetFloat("goalkeeper-mechanics", "CrossClaimVolumeRadiusM", 2.2f);

        /// <summary>[GT] Balance-attribute weight in cross-claim duel score. §3.6.</summary>
        public static readonly float CrossClaimDuelBalanceW = Config.GetFloat("goalkeeper-mechanics", "CrossClaimDuelBalanceW", 0.20f);

        /// <summary>[GT] Strength-attribute weight in cross-claim duel score. §3.6.</summary>
        public static readonly float CrossClaimDuelStrengthW = Config.GetFloat("goalkeeper-mechanics", "CrossClaimDuelStrengthW", 0.35f);

        /// <summary>[GT] Aerial-attribute weight in cross-claim duel score. Weights sum to 1.0. §3.6.</summary>
        public static readonly float CrossClaimDuelAerialW = Config.GetFloat("goalkeeper-mechanics", "CrossClaimDuelAerialW", 0.45f);

        /// <summary>[GT] Score gap below which near-tie tiebreak perturbation is applied. §3.6.</summary>
        public static readonly float CrossClaimTiebreakEpsilon = Config.GetFloat("goalkeeper-mechanics", "CrossClaimTiebreakEpsilon", 0.03f);

        /// <summary>[GT] Gaussian perturbation amplitude for cross-claim near-tie tiebreak. §3.6 / KD-7.</summary>
        public static readonly float CrossClaimTiebreakNoiseAmplitude = Config.GetFloat("goalkeeper-mechanics", "CrossClaimTiebreakNoiseAmplitude", 0.015f);

        // ── §4.4.2 Buffer / System Constants ─────────────────────────────────────────

        /// <summary>[GT] Maximum simultaneous GK agents (one per side). §4.1 / §4.4.2.</summary>
        public static readonly int MaxGkAgents = Config.GetInt("goalkeeper-mechanics", "MaxGkAgents", 2);

        /// <summary>[GT] Maximum participants in a single cross-claim duel (per §3.6 buffer allocation). §3.6.</summary>
        public static readonly int MaxCrossClaimParticipants = Config.GetInt("goalkeeper-mechanics", "MaxCrossClaimParticipants", 8);

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
