// File:     src/attacking-ai/AttackingAgentSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.3, Code Standards #20
// Purpose:  Per-agent read-only tick input. Orchestrator pre-fills BaselineSlot and Line
//           from PositioningAI. Pace/Stamina/Dribbling declared for Stage 1+ use; not
//           consumed by Stage 0 algorithm.

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Per-agent read-only input snapshot for one 10 Hz attacking-AI tick.
    /// Pre-filled by the match orchestrator from #7 Perception and #12 Positioning AI.
    /// Attacking AI #15 §2.3.
    /// </summary>
    public readonly struct AttackingAgentSnapshot
    {
        /// <summary>Agent entity identifier.</summary>
        public int     EntityId     { get; }

        /// <summary>Team identifier.</summary>
        public int     TeamId       { get; }

        /// <summary>Agent's current pitch position (X, Y) in metres. #7 §3.7.</summary>
        public Vector2 Position     { get; }

        /// <summary>
        /// Agent's #12 formation baseline slot (X, Y) in metres. Pre-filled from
        /// PositioningAI.GetFormationSlot(). <c>Vector2.NegativeInfinity</c> = sentinel
        /// (F2 failure mode). #12 §2.2 / §4.5.
        /// </summary>
        public Vector2 BaselineSlot { get; }

        /// <summary>
        /// Agent's committed line membership (Defense / Midfield / Attack).
        /// Pre-filled from PositioningAI.GetLine(). Controls RUNNER eligibility (§3.3).
        /// #12 §4.5.2.
        /// </summary>
        public LineId  Line         { get; }

        /// <summary>True if this agent is the goalkeeper. GK excluded from pool (FR-AT-006 / KD-7).</summary>
        public bool    IsGoalkeeper { get; }

        /// <summary>True if this agent is the current ball carrier. Excluded from pool (FR-AT-007 / KD-3).</summary>
        public bool    HasBall      { get; }

        /// <summary>True if the agent is active. Inactive agents (substituted, red-carded) are excluded.</summary>
        public bool    IsActive     { get; }

        /// <summary>
        /// Normalised pace attribute [0, 1]. Convention: (raw − 1) / 19. Declared for Stage 1+
        /// use in runTriggerTick calibration; Stage 0 algorithm does not consume it. §2.3.
        /// </summary>
        public float   Pace         { get; }

        /// <summary>
        /// Normalised stamina attribute [0, 1] (0 = rested, 1 = fatigued per FR-AT-032).
        /// Declared for Stage 1+ use; Stage 0 algorithm does not consume it. §2.3.
        /// </summary>
        public float   Stamina      { get; }

        /// <summary>
        /// Normalised dribbling attribute [0, 1]. Declared for Stage 1+ RUNNER eligibility
        /// weighting; Stage 0 algorithm does not consume it. §2.3.
        /// </summary>
        public float   Dribbling    { get; }

        /// <summary>Constructs an AttackingAgentSnapshot.</summary>
        public AttackingAgentSnapshot(
            int entityId, int teamId, Vector2 position, Vector2 baselineSlot, LineId line,
            bool isGoalkeeper, bool hasBall, bool isActive,
            float pace, float stamina, float dribbling)
        {
            EntityId     = entityId;
            TeamId       = teamId;
            Position     = position;
            BaselineSlot = baselineSlot;
            Line         = line;
            IsGoalkeeper = isGoalkeeper;
            HasBall      = hasBall;
            IsActive     = isActive;
            Pace         = pace;
            Stamina      = stamina;
            Dribbling    = dribbling;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
