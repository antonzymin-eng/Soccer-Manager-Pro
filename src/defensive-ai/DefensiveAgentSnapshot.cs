// File:     src/defensive-ai/DefensiveAgentSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.3, §4.4, Code Standards #20
// Purpose:  Per-agent immutable snapshot provided to DefensiveAITick each tick.
//           Populated by the match orchestrator from #7 perception, #12 Positioning AI,
//           and #13 Pressing AI state at tick start.

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Snapshot of one agent's state for a single Defensive AI tick.
    /// All fields are set by the match orchestrator before calling
    /// <see cref="DefensiveAITick.Tick"/>. Defensive AI #14 §2.3 / §4.4.
    ///
    /// Both own-team and opponent agents appear in <see cref="DefensiveSnapshot.Agents"/>;
    /// callers filter by <see cref="TeamId"/>. Opponents contribute their positions and
    /// perceived attributes; own-team agents also carry formation and press-role data.
    ///
    /// Fatigue convention: 0.0 = fully rested, 1.0 = fully fatigued (CLAUDE.md / FR-DA-008).
    /// </summary>
    public struct DefensiveAgentSnapshot
    {
        /// <summary>Unique agent identifier (int32 EntityId).</summary>
        public int EntityId;

        /// <summary>TeamId of this agent (0 = home, 1 = away).</summary>
        public int TeamId;

        /// <summary>World-space position (X, Y). Z ignored at Stage 0.</summary>
        public Vector2 Position;

        /// <summary>World-space velocity vector (m/s). X, Y components used for speed and direction checks.</summary>
        public Vector2 Velocity;

        /// <summary>
        /// True when this agent is active (on the pitch).
        /// False for substituted or red-carded agents; these are excluded from all
        /// pool-membership and counting loops (FR-DA-008 / #7 §3.10 isActive).
        /// </summary>
        public bool IsActive;

        /// <summary>True when this agent is the team's goalkeeper (FR-DA-009).</summary>
        public bool IsGoalkeeper;

        /// <summary>True when this agent currently has possession of the ball.</summary>
        public bool HasBall;

        /// <summary>
        /// Positioning AI #12 formation slot (world-space X, Y). Own-team agents only;
        /// zero for opponents. Used for displacement cost baseline and ZONAL target (§3.4 / §3.3).
        /// </summary>
        public Vector2 BaselineSlot;

        /// <summary>
        /// Positioning AI #12 line assignment. Own-team agents only; defaults to Midfield for opponents.
        /// Used by the anti-chaos backline count (§3.10 Invariant 1 / FR-DA-025).
        /// </summary>
        public LineId Line;

        /// <summary>
        /// Pressing AI #13 role assignment this tick. Own-team agents only; defaults to HoldShape for opponents.
        /// Used by HoldShapePoolFilter to exclude PRIMARY_PRESS and COVER_SHADOW agents (FR-DA-010 / KD-4).
        /// </summary>
        public PressRole PressRole;

        /// <summary>
        /// Perceived FirstTouch attribute on the 1–20 skill scale, as returned by #7 perception.
        /// Used in threat score numerator (FR-DA-017 / §3.5). Opponent agents only; 1 for own-team.
        /// </summary>
        public float PerceivedFirstTouch;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
