// File:     src/pressing-ai/PressingAIConstants.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §6.1, Code Standards #20
// Purpose:  Single constant catalogue for Spec #13. All trigger thresholds, hysteresis,
//           role caps, stamina costs, zone parameters, and anti-chaos floors.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Complete constant catalogue for Pressing AI (Spec #13).
    /// Region order: Fixed → Derived → Cross → GT.
    /// Tags: [FIXED] [DERIVED] [CROSS] [GT]. No [EST] — all promoted to [GT] per Appendix A v0.3.
    /// KD-15 / FR-PR-007: single catalogue file; no second constants file for this spec.
    /// </summary>
    public static class PressingAIConstants
    {
        #region Fixed

        /// <summary>[FIXED] Pitch length in metres (goal-line to goal-line). Ball Physics #1 §1.2. XC-013-001.</summary>
        public const float PITCH_LENGTH_M = 105.0f;

        /// <summary>[FIXED] Pitch width in metres (touchline to touchline). Ball Physics #1 §1.2. XC-013-001.</summary>
        public const float PITCH_WIDTH_M = 68.0f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Tactical heartbeat delta-time in seconds.
        /// Formula: 1.0 / 10 Hz. CLAUDE.md "Heartbeat Tick Rate". Value: 0.10 s.
        /// Source constants: 10 Hz tactical loop rate.
        /// </summary>
        public const float DT_TACTICAL = 0.10f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS: #12 §3.6.1] Squared distance epsilon for floating-point boundary comparisons.
        /// Authoritative source: PositioningAIConstants.SPACING_EPSILON_M2. Value: 1e-4 m².
        /// </summary>
        public const float SpacingEpsilonM2 = 1e-4f;

        /// <summary>
        /// [CROSS: #16 §3.4] RNG domain tag for Pressing AI. ERR-013-005 resolved 2026-05-17.
        /// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_PRESSING_AI. Value: 0x19.
        /// Stage 0: no stochastic steps — tag reserved to lock block slot per KD-ERR-013-005.
        /// </summary>
        public const uint DomainTagPressingAI = 0x19u;

        /// <summary>
        /// [CROSS: #12] Stage-0 squad size per team (1 GK + 10 outfield).
        /// Authoritative source: PositioningAIConstants.SQUAD_SIZE. Value: 11.
        /// Extended to 22 for full-match (both teams) buffer sizing.
        /// </summary>
        public const int SQUAD_SIZE = 22;

        /// <summary>
        /// [CROSS: #8 §3.1.8.1] Minimum stamina threshold for press eligibility.
        /// Authoritative source: Decision Tree #8 §3.1.8.1. Value: 0.20.
        /// #13 does not redefine — consumed read-only from #8 surface.
        /// </summary>
        public const float PressStaminaMinimum = 0.20f;

        /// <summary>
        /// [CROSS: #8 §3.1.8.2] Maximum distance (metres) from ball-carrier for press eligibility.
        /// Authoritative source: Decision Tree #8 §3.1.8.2. Value: 8.0 m.
        /// #13 does not redefine — consumed read-only from #8 surface.
        /// </summary>
        public const float PressTriggerDistanceM = 8.0f;

        #endregion

        #region GT

        // ── Trigger thresholds ─────────────────────────────────────────────────

        /// <summary>[GT] Touch quality below this threshold classifies as bad touch. §3.1.1. TODO: replace with config loader (Stage 1).</summary>
        public const float BadTouchThreshold = 0.40f;

        /// <summary>[GT] Post-touch ball speed (m/s) above which a bad touch is pressable. §3.1.1. TODO: replace with config loader (Stage 1).</summary>
        public const float BadTouchVelocityMS = 4.0f;

        /// <summary>[GT] Dot-product threshold below which a pass is classified backward. §3.1.2. TODO: replace with config loader (Stage 1).</summary>
        public const float BackwardPassThreshold = -0.30f;

        /// <summary>[GT] Distance to nearest touchline (m) that activates sideline-trap trigger. §3.1.3. TODO: replace with config loader (Stage 1).</summary>
        public const float SidelineTrapDistanceM = 8.0f;

        /// <summary>[GT] FirstTouch attribute (1–20 scale) below which a receiver is classified weak. §3.1.4. TODO: replace with config loader (Stage 1).</summary>
        public const float WeakReceiverThreshold = 10f;

        /// <summary>[GT] Perceived pressure scalar (0–1) at or above which a weak receiver is considered exposed. §3.1.4. TODO: replace with config loader (Stage 1).</summary>
        public const float WeakReceiverPressure = 0.50f;

        // ── Hysteresis ─────────────────────────────────────────────────────────

        /// <summary>[GT] Trigger dwell ticks before committed flag fires. §3.2. Promoted from [EST] per Appendix A.1. TODO: replace with config loader (Stage 1).</summary>
        public const int TriggerDwellTicks = 2;

        /// <summary>[GT] Trigger release ticks after raw condition clears before trigger is considered off. §3.2. Promoted from [EST] per Appendix A.2. TODO: replace with config loader (Stage 1).</summary>
        public const int TriggerReleaseTicks = 3;

        /// <summary>[GT] Role dwell ticks before a role transition commits. §3.6. Promoted from [EST] per Appendix A.3. TODO: replace with config loader (Stage 1).</summary>
        public const int RoleDwellTicks = 3;

        /// <summary>[GT] Lookahead ticks for projected interception point. §3.3. Promoted from [EST] per Appendix A.4. TODO: replace with config loader (Stage 1).</summary>
        public const int InterceptLookaheadTicks = 3;

        // ── Role / cover-shadow / threat ───────────────────────────────────────

        /// <summary>[GT] Maximum simultaneous cover-shadow assignments per press directive. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const int MaxCoverShadows = 2;

        /// <summary>[GT] Lerp fraction along carrier→receiver line for shadow lane position. §3.5. TODO: replace with config loader (Stage 1).</summary>
        public const float CoverShadowLaneFraction = 0.55f;

        /// <summary>[GT] Radius (m) around ball-carrier for candidate receiver search. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const float CoverShadowCandidateRadiusM = 20.0f;

        /// <summary>[GT] Weight for receiver progression gain in threat score formula. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const float ThreatProgressionW = 0.50f;

        /// <summary>[GT] Weight for receiver openness (1 − geometricPressure) in threat score formula. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const float ThreatOpenW = 0.30f;

        /// <summary>[GT] Weight for receiver skill (FirstTouch / 20) in threat score formula. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const float ThreatSkillW = 0.20f;

        /// <summary>[GT] Own-team defender count within radius that saturates geometric pressure signal. §3.4. TODO: replace with config loader (Stage 1).</summary>
        public const float ThreatPressureNormalizer = 3.0f;

        // ── Stamina / fatigue ──────────────────────────────────────────────────

        /// <summary>[GT] Fatigue increase per tick for the primary presser. §3.7. TODO: replace with config loader (Stage 1).</summary>
        public const float StaminaCostPrimaryPerTick = 0.0040f;

        /// <summary>[GT] Fatigue increase per tick for a cover-shadow agent. §3.7. TODO: replace with config loader (Stage 1).</summary>
        public const float StaminaCostShadowPerTick = 0.0020f;

        /// <summary>[GT] Fatigue ceiling above which an agent is excluded from press roles. §3.3 / §3.7. FR-PR-029. TODO: replace with config loader (Stage 1).</summary>
        public const float PressFatigueCeiling = 0.85f;

        // ── Disengage / reset / zone ───────────────────────────────────────────

        /// <summary>[GT] Ticks of no committed trigger before disengage fires. §3.8. TODO: replace with config loader (Stage 1).</summary>
        public const int DisengageTimeoutTicks = 8;

        /// <summary>[GT] Ticks of all-HOLD_SHAPE forced after a disengage or zone exit. §3.8. TODO: replace with config loader (Stage 1).</summary>
        public const int ResetLatencyTicks = 12;

        /// <summary>[GT] Minimum ball X (metres, own-team attacking +X) for pressing to be active. §3.8. TODO: replace with config loader (Stage 1).</summary>
        public const float PressZoneXMin = 35.0f;

        /// <summary>[GT] Maximum ball X (metres) for pressing zone upper guard. §3.8. Trivially-true upper bound at Stage 0. TODO: replace with config loader (Stage 1).</summary>
        public const float PressZoneXMax = 105.0f;

        // ── Anti-chaos floors ──────────────────────────────────────────────────

        /// <summary>[GT] Maximum active pressers (PRIMARY_PRESS + COVER_SHADOW) in the ball-side pitch third. §3.9 invariant (1). TODO: replace with config loader (Stage 1).</summary>
        public const int MaxPressersBallThird = 3;

        /// <summary>[GT] Minimum own Defense-line agents in own defensive third required. §3.9 invariant (2). TODO: replace with config loader (Stage 1).</summary>
        public const int MinBacklineAgents = 3;

        /// <summary>[GT] Maximum displacement (metres) of a COVER_SHADOW target from baseline formation slot. §3.9 invariant (3). TODO: replace with config loader (Stage 1).</summary>
        public const float MaxPressDisplacementM = 25.0f;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
