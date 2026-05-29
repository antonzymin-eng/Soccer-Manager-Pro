// File:     src/attacking-ai/AttackingAIConstants.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §6.1, FR-AT-030, Code Standards #20 §4.2 FR-CS-025
// Purpose:  Single constant catalogue for Spec #15. All GT / Derived / Cross constants;
//           no magic numbers in formula code. KD-14: one catalogue file only.
//           Region order: Derived → Cross → GT (no Fixed constants in this spec).

using UnityEngine;

using TacticalDirector.PressingAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Complete constant catalogue for Attacking AI (Spec #15).
    /// Region order: Derived → Cross → GT. No [EST] constants — all promoted to [GT] per §6.1.
    /// KD-14 / FR-AT-030: single catalogue file per #20 §4.2 FR-CS-025.
    /// </summary>
    public static class AttackingAIConstants
    {
        #region Derived

        /// <summary>
        /// [DERIVED] Final-third threshold X-coordinate for team attacking x=105.
        /// Formula: PITCH_LENGTH_M × 2/3 = 105.0 × 2/3 = 70.0 m. §6.1.3 / Appendix A §A.2.
        /// Source constants: <see cref="PITCH_LENGTH_M"/>.
        /// </summary>
        public static readonly float FinalThirdXM =
            PITCH_LENGTH_M * (2f / 3f);

        /// <summary>
        /// [DERIVED] Maximum lateral run-offset magnitude. Formula: PITCH_WIDTH_M × 0.5 = 34.0 m.
        /// Bounds the lateralOffset_m clamp in §3.4 to [−MaxLateralOffsetM, +MaxLateralOffsetM].
        /// Source constants: <see cref="PITCH_WIDTH_M"/>. Attacking AI #15 §3.4 / §6.1.3.
        /// </summary>
        public static readonly float MaxLateralOffsetM = PITCH_WIDTH_M * 0.5f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS: #1 §1.2] Pitch length in metres (goal-line to goal-line).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 105.0 m.
        /// </summary>
        public const float PITCH_LENGTH_M = 105.0f;

        /// <summary>
        /// [CROSS: #1 §1.2] Pitch width in metres (touchline to touchline).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 68.0 m.
        /// </summary>
        public const float PITCH_WIDTH_M = 68.0f;

        /// <summary>
        /// [CROSS: #1 §1.2] Midfield x-coordinate (half-line).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 52.5 m.
        /// </summary>
        public const float HALF_LINE_X = 52.5f;

        /// <summary>
        /// [CROSS: #16 §3.4] RNG domain tag for Attacking AI.
        /// Authoritative source: Deterministic Simulation #16 §3.4 v1.0.4. Value: 0x1B.
        /// ERR-015-001 resolved May 18, 2026. Stage 0: no stochastic steps —
        /// tag reserved to lock the domain-tag block slot per §4.6 / KD-10.
        /// </summary>
        public const uint DomainTagAttackingAI = 0x1Bu;

        /// <summary>
        /// [CROSS: #13 §6.1] Pre-allocated capacity for agent arrays (both teams combined).
        /// Authoritative source: PressingAIConstants.SQUAD_SIZE. Value: 22.
        /// </summary>
        public const int SQUAD_SIZE = PressingAIConstants.SQUAD_SIZE;

        #endregion

        #region GT

        // ── Pool Construction (§6.1.1) ─────────────────────────────────────────

        // (PITCH_LENGTH_M, PITCH_WIDTH_M, HALF_LINE_X, HALF_LINE_X moved to Cross region above.)

        // ── Role Assignment (§6.1.2) ────────────────────────────────────────────

        /// <summary>
        /// [GT] Anti-chaos baseline runner cap. Overridden per style profile by
        /// MAX_RUNNERS_POSSESSION / _DIRECT / _COUNTER (§6.1.5). Attacking AI #15 §6.1.2.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int MaxRunners = 2;

        /// <summary>
        /// [GT] Minimum pool size required before a WEAK_SIDE agent is assigned.
        /// Below this threshold, pool is too small to dedicate an agent to the far side.
        /// Attacking AI #15 §6.1.2 / §3.3 / §3.7 (FR-AT-015).
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int MinWeakSideAgentThreshold = 4;

        // ── Run Parameter Generation (§6.1.3) ──────────────────────────────────

        /// <summary>
        /// [GT] Minimum run-target depth offset after profile multiplier (clamp floor). Units: m.
        /// Attacking AI #15 §6.1.3 / §3.4.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float MinRunDepthM = 5.0f;

        /// <summary>
        /// [GT] Maximum run-target depth offset after profile multiplier (clamp ceiling). Units: m.
        /// Attacking AI #15 §6.1.3 / §3.4.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float MaxRunDepthM = 40.0f;

        /// <summary>
        /// [GT] Base run target depth before profile multiplier. Output clamped to
        /// [<see cref="MinRunDepthM"/>, <see cref="MaxRunDepthM"/>].
        /// Attacking AI #15 §6.1.3 / §3.4.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float BaseRunDepthM = 15.0f;

        /// <summary>
        /// [GT] Scale factor converting lateralPct deviation to lateral run offset. &lt; 1 so
        /// runs stay slightly narrower than the agent's baseline lane. Attacking AI #15 §6.1.3 / §3.4.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float LateralScale = 0.8f;

        /// <summary>
        /// [GT] Base run trigger delay in 10 Hz ticks before profile multiplier.
        /// Minimum 1 tick enforced by max(1, round(...)). Attacking AI #15 §6.1.3 / §3.4.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int BaseRunTriggerDelayTicks = 3;

        // ── Support, Width-Holding, Weak-Side (§6.1.4) ─────────────────────────

        /// <summary>
        /// [GT] Minimum effective support radius: floor applied after profile multiplier (§3.5).
        /// Prevents radius collapse under aggressive <see cref="SupportMult"/> values.
        /// Units: m. Attacking AI #15 §6.1.4 / §3.5.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float MinEffectiveRadiusM = 5.0f;

        /// <summary>
        /// [GT] Base support radius before profile multiplier. Effective radius =
        /// max(<see cref="MinEffectiveRadiusM"/>, SupportRadiusM × supportMult). Units: m.
        /// Attacking AI #15 §6.1.4 / §3.5.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float SupportRadiusM = 12.0f;

        /// <summary>
        /// [GT] Minimum agents holding near-touchline width per tick. Attacking AI #15 §6.1.4 / §3.6.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int MinWidthHolders = 2;

        /// <summary>
        /// [GT] Distance from the nearest touchline for a HOLD_WIDTH target position (NOT
        /// absolute Y). Formula in §3.6 derives absolute Y from this distance.
        /// Attacking AI #15 §6.1.4 / §3.6.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float TouchlineHoldDistM = 4.0f;

        /// <summary>
        /// [GT] Distance from the weak-side touchline for the WEAK_SIDE agent target.
        /// §3.7 formula: if ball on y=68 half, weakTarget.y = WEAK_SIDE_FAR_Y_M (near y=0).
        /// Attacking AI #15 §6.1.4 / §3.7.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float WeakSideFarYM = 8.0f;

        /// <summary>
        /// [GT] X-offset toward the opponent goal for the WEAK_SIDE agent target position.
        /// Target x = ballCarrier.x + WEAK_SIDE_DEPTH_OFFSET_M. Attacking AI #15 §6.1.4 / §3.7.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float WeakSideDepthOffsetM = 5.0f;

        // ── Team-Style Profile Multipliers (§6.1.5) ────────────────────────────
        // 5 families × 3 profiles = 15 GT constants (§3.10 "These 15 constants").

        /// <summary>[GT] POSSESSION profile depthOffset_m scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float DepthMultPossession = 0.8f;

        /// <summary>[GT] DIRECT profile depthOffset_m scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float DepthMultDirect = 1.4f;

        /// <summary>[GT] COUNTER_ATTACK profile depthOffset_m scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float DepthMultCounter = 1.6f;

        /// <summary>[GT] POSSESSION profile runTriggerTick delay scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float TimingMultPossession = 1.2f;

        /// <summary>[GT] DIRECT profile runTriggerTick delay scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float TimingMultDirect = 0.7f;

        /// <summary>[GT] COUNTER_ATTACK profile runTriggerTick delay scale factor. §6.1.5 / §3.4. TODO: config loader (Stage 1).</summary>
        public const float TimingMultCounter = 0.5f;

        /// <summary>[GT] POSSESSION profile SUPPORT_RADIUS_M scale factor. §6.1.5 / §3.5. TODO: config loader (Stage 1).</summary>
        public const float SupportMultPossession = 1.3f;

        /// <summary>[GT] DIRECT profile SUPPORT_RADIUS_M scale factor. §6.1.5 / §3.5. TODO: config loader (Stage 1).</summary>
        public const float SupportMultDirect = 0.8f;

        /// <summary>[GT] COUNTER_ATTACK profile SUPPORT_RADIUS_M scale factor. §6.1.5 / §3.5. TODO: config loader (Stage 1).</summary>
        public const float SupportMultCounter = 0.5f;

        /// <summary>[GT] POSSESSION profile MAX_RUNNERS cap (1 runner; conservative). §6.1.5 / §3.3. TODO: config loader (Stage 1).</summary>
        public const int MaxRunnersPossession = 1;

        /// <summary>[GT] DIRECT profile MAX_RUNNERS cap (3 runners). §6.1.5 / §3.3. TODO: config loader (Stage 1).</summary>
        public const int MaxRunnersDirect = 3;

        /// <summary>[GT] COUNTER_ATTACK profile MAX_RUNNERS cap (4 runners). §6.1.5 / §3.3. TODO: config loader (Stage 1).</summary>
        public const int MaxRunnersCounter = 4;

        /// <summary>[GT] POSSESSION profile transition-hold ticks (500 ms freeze). §6.1.5 / §3.9. TODO: config loader (Stage 1).</summary>
        public const int TransitionHoldTicksPossession = 5;

        /// <summary>[GT] DIRECT profile transition-hold ticks (500 ms freeze). §6.1.5 / §3.9. TODO: config loader (Stage 1).</summary>
        public const int TransitionHoldTicksDirect = 5;

        /// <summary>[GT] COUNTER_ATTACK profile transition-hold ticks (0 = instant recovery). §6.1.5 / §3.9. TODO: config loader (Stage 1).</summary>
        public const int TransitionHoldTicksCounter = 0;

        // ── Overload Detection (§6.1.6) ─────────────────────────────────────────

        /// <summary>
        /// [GT] Minimum non-WEAK_SIDE agents within the Y-corridor to declare an overload.
        /// Attacking AI #15 §6.1.6 / §3.8 (FR-AT-016).
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int OverloadCount = 3;

        /// <summary>
        /// [GT] Y-half-width of overload detection corridor: |agentY − ballY| ≤ this value.
        /// Units: m. Attacking AI #15 §6.1.6 / §3.8.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float OverloadZoneWidthM = 20.0f;

        // ── Anti-Chaos Invariants (§6.1.7) ──────────────────────────────────────

        /// <summary>
        /// [GT] Minimum combined SUPPORT_BALL + HOLD_WIDTH agents per tick (FR-AT-019).
        /// Attacking AI #15 §6.1.7 / §3.11 invariant 2.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int MinSupportAgents = 1;

        /// <summary>
        /// [GT] Run target must not be more than this distance past the half-line into own half.
        /// Units: m. Attacking AI #15 §6.1.7 / §3.11 invariant 3 (FR-AT-020).
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float OwnHalfRunBlockM = 5.0f;

        /// <summary>
        /// [GT] Maximum demotion-loop iterations before emitting all-default directive.
        /// Attacking AI #15 §6.1.7 / §3.11 (FR-AT-026).
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int MaxInvariantPasses = 3;

        // ── Hysteresis (§6.1.8) ──────────────────────────────────────────────────

        /// <summary>
        /// [GT] Dwell ticks before a role/target transition fires. At 10 Hz = 300 ms stability
        /// window. Attacking AI #15 §6.1.8 / §3.12 (FR-AT-022 / FR-AT-023).
        /// Promoted from [EST] to [GT] per Appendix A §A.1.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int AttackDwellTicks = 3;

        // ── Own-Half Run Block (§6.1.7 continued) ────────────────────────────────

        /// <summary>
        /// [GT] Absolute radian tolerance for detecting "near-zero" teamAttackAngle (i.e., team
        /// attacking toward x=105). Used by <see cref="InvariantEnforcer"/> §3.11 invariant 3
        /// to select the correct own-half depth formula branch. Units: radians.
        /// Attacking AI #15 §6.1.7 / §3.11.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float AttackAngleEpsilon = 0.01f;

        // ── Test Acceptance Criteria (§6.1.10) ───────────────────────────────────

        /// <summary>
        /// [GT] Max distance to opponent goal centre for dangerous-zone surrogate metric (§5.7).
        /// Units: m. Attacking AI #15 §6.1.10.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float DangerZoneMaxDistM = 20.0f;

        /// <summary>
        /// [GT] Half-width of dangerous zone for §5.7 surrogate metric.
        /// Derived from penalty-area half-width; Appendix A §A.3. Units: m.
        /// Attacking AI #15 §6.1.10.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float DangerZoneCorridorHwM = 10.16f;

        /// <summary>
        /// [GT] Min additional RUNNER assignments per 90-min match: DIRECT vs. POSSESSION profile.
        /// Test criterion for tactical-identity acceptance (§5.8 / FR-AT-035).
        /// Attacking AI #15 §6.1.10.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int DirectRunCountDelta = 15;

        /// <summary>
        /// [GT] Max mean transitionHoldTick for COUNTER_ATTACK per possession-loss event (§5.8).
        /// Value 0 reflects TRANSITION_HOLD_TICKS_COUNTER = 0. Attacking AI #15 §6.1.10.
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public const int CounterMaxHoldTicks = 0;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. 38+ constants; 3 [CROSS] + 1 [DERIVED] + GT. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1/H-2/L-1: added MinEffectiveRadiusM, MinRunDepthM, MaxRunDepthM (GT) and MaxLateralOffsetM (DERIVED). |
// | 1.2     | 2026-05-29 | —      | AR-3 L-1: added AttackAngleEpsilon GT constant for InvariantEnforcer §3.11 angle-comparison branch. |
#endregion
