// File:     src/defensive-ai/DefensiveAIConstants.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §6.1, FR-DA-007, Code Standards #20 §4.2 FR-CS-025
// Purpose:  Single constant catalogue for Spec #14. Contains all assignment thresholds,
//           hysteresis timing, offside-trap parameters, tackle-intent constants, anti-chaos
//           floors, GK zone bounds, and cross-spec scalar imports.

using TacticalDirector.PressingAI;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Complete constant catalogue for Defensive AI (Spec #14).
    /// Region order: Fixed → Derived → Cross → GT.
    /// FR-DA-007 / KD-14: single catalogue file per #20 §4.2 FR-CS-025.
    /// No [EST] constants — all promoted to [GT] per Appendix A v0.3.
    /// </summary>
    public static class DefensiveAIConstants
    {
        #region Cross

        // ── Cross-spec geometry constants ──────────────────────────────────────

        /// <summary>
        /// [CROSS: #1 §1.2] Pitch length in metres (goal-line to goal-line).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 105.0 m.
        /// Denominator in perceivedGoalProximity formula (§3.5.2).
        /// </summary>
        public const float PITCH_LENGTH_M = 105.0f;

        /// <summary>
        /// [CROSS: #1 §1.2] Pitch width in metres (touchline to touchline).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 68.0 m.
        /// Used in abandonedZoneCenter.y = PITCH_WIDTH_M / 2.0 = 34.0 m (§3.9.2).
        /// </summary>
        public const float PITCH_WIDTH_M = 68.0f;

        /// <summary>
        /// [CROSS: #1 §1.2] Midfield x-coordinate (half-line).
        /// Authoritative source: Ball Physics #1 §1.2. Value: 52.5 m.
        /// Offside trap trigger condition 2: ball must be in opponent half (§3.7.2).
        /// </summary>
        public const float HALF_LINE_X = 52.5f;

        /// <summary>
        /// [CROSS: #16 §3.4] RNG domain tag for Defensive AI.
        /// Authoritative source: Deterministic Simulation #16 §3.4 v1.0.5. Value: 0x1A.
        /// ERR-014-004 resolved May 18, 2026. Stage 0: no stochastic steps — tag reserved
        /// to lock the domain-tag block slot per §4.6 / KD-10.
        /// </summary>
        public const uint DomainTagDefensiveAI = 0x1Au;

        /// <summary>
        /// [CROSS: #13 §6.1] Pre-allocated capacity for agent arrays (both teams combined).
        /// Authoritative source: PressingAIConstants.SQUAD_SIZE. Value: 22.
        /// </summary>
        public const int SQUAD_SIZE = PressingAIConstants.SQUAD_SIZE;

        #endregion

        #region GT

        // ── Assignment Algorithm (§3.3) ────────────────────────────────────────

        /// <summary>
        /// [GT] Radius (m) within which an opponent qualifies as a MAN_MARK candidate
        /// for a given agent (§3.3.3). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float ManMarkCandidateRadiusM = Config.GetFloat("defensive-ai", "ManMarkCandidateRadiusM", 15.0f);

        /// <summary>
        /// [GT] Minimum opponent speed (m/s) to qualify as an INTERCEPT_RUNNER target (§3.3.3).
        /// Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float RunnerVelocityThresholdMS = Config.GetFloat("defensive-ai", "RunnerVelocityThresholdMS", 3.0f);

        // ── Last-Man Detection (§3.8) ──────────────────────────────────────────

        /// <summary>
        /// [GT] Ball-ahead buffer (m): emergency fires when
        /// distToOwnGoal(ball) &lt; distToOwnGoal(lastMan) + LAST_MAN_BALL_BUFFER_M (§3.8.1).
        /// Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float LastManBallBufferM = Config.GetFloat("defensive-ai", "LastManBallBufferM", 5.0f);

        /// <summary>
        /// [GT] Minimum distToOwnGoal(ball) for the last-man predicate to fire (§3.8.1).
        /// Prevents constant emergency triggering when ball is deep in own area.
        /// Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float LastManOwnHalfMinX = Config.GetFloat("defensive-ai", "LastManOwnHalfMinX", 5.0f);

        // ── Offside Trap (§3.7) ────────────────────────────────────────────────

        /// <summary>
        /// [GT] Ball speed ceiling (m/s) for offside trap trigger condition 1 (§3.7.2).
        /// Above this speed a through-ball can outrun the step-up.
        /// Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float OffsideBallSpeedThresholdMS = Config.GetFloat("defensive-ai", "OffsideBallSpeedThresholdMS", 4.0f);

        /// <summary>
        /// [GT] Forward advancement (distToOwnGoal increase, m) per trap execution (§3.7.4).
        /// Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float OffsideStepSizeM = Config.GetFloat("defensive-ai", "OffsideStepSizeM", 3.0f);

        /// <summary>
        /// [GT] Safety ceiling: defensive line may not advance beyond this distToOwnGoal
        /// value via the trap (§3.7.4). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float OffsideMaxDepthM = Config.GetFloat("defensive-ai", "OffsideMaxDepthM", 45.0f);

        /// <summary>
        /// [GT] Consecutive qualifying ticks required before the trap fires (§3.7.2).
        /// At 10 Hz: 300 ms minimum qualification window. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int OffsideTrapDwellTicks = Config.GetInt("defensive-ai", "OffsideTrapDwellTicks", 3);

        /// <summary>
        /// [GT] Reduced dwell threshold used when the manager requests the offside trap
        /// (#21 FR-TI-022 / KD-9 additive request): a requested trap arms more readily than the
        /// autonomous baseline. PINNED (#21 §5.6 / G2 balance pass, 2026-06-30 numerical-mirror).
        /// Invariant: 1 ≤ value ≤ <see cref="OffsideTrapDwellTicks"/> — the request only lowers
        /// (never raises) the arm threshold, and ≥ 1 keeps it a request, not a guarantee (the
        /// §3.7.2 conditions must still hold for at least one tick; a zero dwell would arm the
        /// trap the instant they momentarily coincide). Pinned at 1 (= 100 ms @ 10 Hz, the floor)
        /// against the 3-tick (300 ms) autonomous baseline: a manager dialling the trap on shaves
        /// the qualification window to its minimum without removing adjudication. Locked by
        /// <c>OffsideTrapControllerTests.RequestedDwell_InvariantPinned</c>. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int OffsideTrapRequestedDwellTicks = Config.GetInt("defensive-ai", "OffsideTrapRequestedDwellTicks", 1);

        /// <summary>
        /// [GT] Post-trap cooldown ticks before a new step-up may fire (§3.7.3).
        /// At 10 Hz: 1,000 ms cooldown. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int OffsideResetCooldownTicks = Config.GetInt("defensive-ai", "OffsideResetCooldownTicks", 10);

        /// <summary>
        /// [GT] Maximum x-spread (m) of the DEFENSE-line for it to be eligible for a
        /// coordinated step-up (§3.7.2 condition 3). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float LineCoherenceThresholdM = Config.GetFloat("defensive-ai", "LineCoherenceThresholdM", 8.0f);

        // ── Hysteresis and Assignment Timing (§3.11) ───────────────────────────

        /// <summary>
        /// [GT] Consecutive ticks a new mark candidate must be consistently preferred before
        /// the transition commits (§3.11.4). At 10 Hz: 400 ms dwell. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int MarkDwellTicks = Config.GetInt("defensive-ai", "MarkDwellTicks", 4);

        /// <summary>
        /// [GT] Maximum ticks before an unassigned zone receives cover after a ball switch
        /// (§5.6.2 T-DA-EXP-002 criterion). At 10 Hz: 200 ms. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int ReassignLatencyTicks = Config.GetInt("defensive-ai", "ReassignLatencyTicks", 2);

        // ── Tackle Intent (§3.6) ───────────────────────────────────────────────

        /// <summary>
        /// [GT] Radius (m) within which an agent's assigned opponent qualifies for tackle
        /// intent evaluation (§3.6.2). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float TackleEligibleRadiusM = Config.GetFloat("defensive-ai", "TackleEligibleRadiusM", 3.0f);

        /// <summary>
        /// [GT] Minimum teammates behind the agent (within y-corridor) before COMMIT mode
        /// is permitted (§3.6.2). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int TackleCommitCoverageFloor = Config.GetInt("defensive-ai", "TackleCommitCoverageFloor", 1);

        /// <summary>
        /// [GT] Approach angle (rad, ~20°) below which JOCKEY is preferred over HOLD when
        /// COMMIT is disallowed (§3.6.2). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float TackleJockeyAngleRad = Config.GetFloat("defensive-ai", "TackleJockeyAngleRad", 0.35f);

        /// <summary>
        /// [GT] Half-width (m) of the y-axis corridor used to count "teammates behind" for
        /// coverage depth (§3.6.2). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float CoverageDepthCorridorM = Config.GetFloat("defensive-ai", "CoverageDepthCorridorM", 5.0f);

        // ── Anti-Chaos Invariants (§3.10) ──────────────────────────────────────

        /// <summary>
        /// [GT] Minimum DEFENSE-line agents that must remain in ZONAL mode after assignment.
        /// Invariant 1 in §3.10 (FR-DA-025). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int MinBacklineAgents = Config.GetInt("defensive-ai", "MinBacklineAgents", 3);

        /// <summary>
        /// [GT] Maximum simultaneous MAN_MARK assignments across the HOLD_SHAPE pool.
        /// Invariant 2 in §3.10 (FR-DA-026). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int MaxManMarkAssignments = Config.GetInt("defensive-ai", "MaxManMarkAssignments", 4);

        /// <summary>
        /// [GT] Maximum displacement (m) of a non-ZONAL assignment from the agent's #12
        /// baseline anchor. Invariant 3 in §3.10 (FR-DA-027). Defensive AI #14 §6.1.
        /// </summary>
        public static readonly float MaxMarkDisplacementM = Config.GetFloat("defensive-ai", "MaxMarkDisplacementM", 20.0f);

        // ── GK Zone (§3.9) ─────────────────────────────────────────────────────

        /// <summary>
        /// [GT] Lower bound of GK expected zone (m, signed distToOwnGoal). Allows GK to
        /// stand slightly behind goal-line without triggering a false COVER_GK_ZONE override.
        /// Not used in Stage 0 trigger logic; reserved for Stage 1+ zone visualisation (§3.9.1).
        /// Defensive AI #14 §6.1. TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float GkExpectedZoneMinX = -2.0f;

        /// <summary>
        /// [GT] Maximum distToOwnGoal (m) at which the GK is considered "in position".
        /// Exceeding this value triggers the COVER_GK_ZONE check when emergency is active (§3.9.1).
        /// Defensive AI #14 §6.1. TODO: replace with config loader (Stage 1).
        /// </summary>
        public const float GkExpectedZoneMaxX = 15.0f;

        /// <summary>
        /// [GT] Safety release duration (ticks): if COVER_GK_ZONE override has been active
        /// this many consecutive ticks, the cover agent reverts to ZONAL (§3.9.2).
        /// At 10 Hz: 2,000 ms. Defensive AI #14 §6.1.
        /// </summary>
        public static readonly int CoverGkZoneMaxTicks = Config.GetInt("defensive-ai", "CoverGkZoneMaxTicks", 20);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                |
// | 1.0     | 2026-05-29 | —      | Initial implementation. 26-entry catalogue (22 [GT] + 4 [CROSS]).                   |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1: SQUAD_SIZE mirror corrected to reference PressingAIConstants.SQUAD_SIZE;   |
// |         |            |        |   added using TacticalDirector.PressingAI; literal copy violated [CROSS] semantics. |
// | 1.2     | 2026-06-30 | —      | #21 §5.6 / G2: OffsideTrapRequestedDwellTicks (FR-TI-022) PINNED — illustrative →    |
// |         |            |        |   pinned at 1 with the numerical-mirror invariant 1 ≤ value ≤ OffsideTrapDwellTicks  |
// |         |            |        |   documented; locked by OffsideTrapControllerTests.RequestedDwell_InvariantPinned.   |
#endregion
