// File:     src/defensive-ai/TackleIntentRequest.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.3, §3.6, Code Standards #20
// Purpose:  Per-agent tackle intent produced for HOLD_SHAPE agents within tackle-eligible
//           range. Not retained across ticks; consumed within the same tick cycle.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Tackle intent for one HOLD_SHAPE agent within <c>TACKLE_ELIGIBLE_RADIUS_M</c>
    /// of its assigned opponent. Produced by <see cref="TackleIntentEvaluator"/>; surfaced
    /// to the orchestrator which passes it to Decision Tree #8 (→ Collision System #3).
    /// Not retained across ticks. Defensive AI #14 §2.2.3 / §3.6.
    /// </summary>
    public struct TackleIntentRequest
    {
        /// <summary>EntityId of the HOLD_SHAPE agent.</summary>
        public int AgentEntityId;

        /// <summary>Recommended tackle mode for this agent this tick.</summary>
        public TackleMode Mode;

        /// <summary>EntityId of the opponent being tackled.</summary>
        public int TargetEntityId;

        /// <summary>
        /// Angle (rad) between agent→opponent direction and agent velocity direction.
        /// Range [0, π]. 0 = perfect head-on approach; π/2 = lateral; π = from behind.
        /// </summary>
        public float ApproachAngle;

        /// <summary>
        /// Count of own-team agents between this agent and the own goal within the
        /// COVERAGE_DEPTH_CORRIDOR_M lateral band. Range [0, 10]. Used for COMMIT eligibility.
        /// </summary>
        public byte CoverageDepth;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
