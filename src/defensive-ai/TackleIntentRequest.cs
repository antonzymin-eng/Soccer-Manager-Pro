// File:     src/defensive-ai/TackleIntentRequest.cs
// Created:  2026-05-29
// Modified: 2026-08-12 (ApproachAngle XML doc corrected — it never encoded "from behind"; wiring backlog W2)
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
        /// Angle (rad) between the agent→opponent bearing and the agent's own velocity direction.
        /// Range [0, π]. 0 = running straight at his man; π/2 = running across him; π = running
        /// directly AWAY from him. A stationary agent is assigned π/2 as the worst case.
        ///
        /// <para><b>This carries no information about which side of the opponent the agent is on.</b>
        /// The previous wording — "π = from behind" — was wrong, and it is the kind of wrong that
        /// propagates: it describes the agent's position relative to the opponent, which is a
        /// different quantity from the one computed here, and the first cut of the wiring-backlog W2
        /// tackle design weighted a foul model on it before the error was caught. The authority is
        /// #14 §2.2.3, which says only "angle between agent→opponent and agent velocity" — the code
        /// has always matched the spec; only this comment did not. From-behind contact geometry is
        /// owned by Collision System #3's <c>ContactTypeClassifier</c> (<c>FROM_BEHIND</c>).</para>
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
// | 1.1     | 2026-08-12 | —      | ApproachAngle's XML doc claimed "π/2 = lateral; π = from behind".  |
// |         |            |        | Wrong: the value is the angle between the agent's own VELOCITY    |
// |         |            |        | and his bearing to the opponent, so π means running away from him |
// |         |            |        | and it says nothing about which side of him the agent is on. The  |
// |         |            |        | code has always matched #14 §2.2.3; only this comment did not.    |
// |         |            |        | Found by the wiring-backlog W2 pre-implementation council, after  |
// |         |            |        | the first W2 design had already weighted a foul model on the      |
// |         |            |        | misreading. Comment only — no behaviour change.                   |
#endregion
