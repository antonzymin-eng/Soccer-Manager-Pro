// File:     src/attacking-ai/AttackingSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Attacking AI #15 §2.3, §3.13, Code Standards #20; Tactical Instructions #21 §3.3 (FR-TI-021)
// Purpose:  Pre-allocated tick input container for one 10 Hz attacking-AI evaluation.
//           Sealed class; Agents array is allocated once at construction (FR-AT-030 zero-alloc).

using UnityEngine;

using TacticalDirector.PressingAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Tick input container for one 10 Hz attacking-AI evaluation. Constructed once per
    /// team per match; orchestrator writes fields before every Tick() call.
    /// Pre-allocated <see cref="Agents"/> array ensures zero heap allocation on the hot path.
    /// Attacking AI #15 §2.3 / §3.13.
    /// </summary>
    public sealed class AttackingSnapshot
    {
        /// <summary>Monotonically increasing tick index (≥ 0). Used for F1 stale detection.</summary>
        public int   TickIndex           { get; set; }

        /// <summary>EntityId of the team running the attacking AI this tick.</summary>
        public int   AttackingTeamId     { get; set; }

        /// <summary>Ball position (X, Y) in metres at tick start. #7 §3.7.</summary>
        public Vector2 BallPosition      { get; set; }

        /// <summary>
        /// EntityId of the current ball carrier. −1 when the ball is loose (no possessor).
        /// A value of −1 is treated as OUT_OF_POSSESSION (FR-AT-007 / §2.3 note).
        /// </summary>
        public int   BallCarrierEntityId { get; set; }

        /// <summary>
        /// Ball carrier's pitch position (X, Y) in metres. Valid only when
        /// <see cref="BallCarrierEntityId"/> ≥ 0. Used for run-target origin (§3.4) and
        /// HOLD_WIDTH / WEAK_SIDE target X (§3.6 / §3.7).
        /// </summary>
        public Vector2 BallCarrierPosition { get; set; }

        /// <summary>
        /// Attack direction for this half: 0.0 rad = team attacking x=105 goal;
        /// π rad = team attacking x=0 goal. Match-half constant (§2.3 / §3.4).
        /// </summary>
        public float TeamAttackAngle     { get; set; }

        /// <summary>
        /// #21 T2 routing field (FR-TI-021): the team's manager <see cref="FocusPlay"/>, translated
        /// through <see cref="TacticTranslation.PreferredFlank"/> into an overload flank bias. The
        /// auto-property zero-value default is <see cref="FocusPlay.Mixed"/> (no lateral preference)
        /// = the <c>TeamTactic.Balanced</c> identity (FR-TI-031), so a default snapshot is
        /// behaviour-neutral. The match-engine Phase-D writer routes the active tactic here (v1.19);
        /// <see cref="OverloadDetector"/> consumes it as a flank-preference bias (v1.1) — a preferred
        /// ball-side flank lowers the overload trigger count; null (Mixed / ThroughMiddle) leaves it
        /// unchanged. Magnitude pending the §5.6 / G2 balance pass.
        /// </summary>
        public TacticalDirector.TacticalInstructions.FocusPlay FocusPlay { get; set; }

        /// <summary>
        /// Pre-allocated per-agent snapshot array. Capacity = SQUAD_SIZE.
        /// Orchestrator writes active agent data before each Tick() call.
        /// </summary>
        public AttackingAgentSnapshot[] Agents { get; }

        /// <summary>Constructs the snapshot with a pre-allocated Agents array.</summary>
        public AttackingSnapshot()
        {
            Agents = new AttackingAgentSnapshot[PressingAIConstants.SQUAD_SIZE];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                          |
// | 1.1     | 2026-06-29 | —      | #21 T2: + FocusPlay routing field (FR-TI-021); Mixed zero-value   |
// |         |            |        |   identity, OverloadDetector consumption deferred to Phase-D.     |
// | 1.2     | 2026-06-29 | —      | Doc: Phase-D writer landed (MatchEngine v1.19); OverloadDetector  |
// |         |            |        |   flank-pref consumption now deferred to §5.6/G2 (doc-only).      |
// | 1.3     | 2026-06-29 | —      | Doc: OverloadDetector now consumes FocusPlay as a flank-pref bias |
// |         |            |        |   (null = unchanged = neutral).                                  |
#endregion
