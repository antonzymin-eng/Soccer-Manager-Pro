// File:     src/pressing-ai/PressingAgentSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §4.2, Code Standards #20
// Purpose:  Per-agent immutable snapshot provided to PressingAITick each tick.
//           Populated by the match orchestrator from authoritative game state.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Snapshot of one agent's state for a single Pressing AI tick.
    /// All fields are set by the match orchestrator before calling PressingAITick.Tick().
    /// Pressing AI #13 §4.2.
    ///
    /// Convention: Fatigue follows 0.0 = fully rested, 1.0 = fully fatigued (CLAUDE.md fatigue convention).
    /// </summary>
    public struct PressingAgentSnapshot
    {
        /// <summary>Unique agent identifier. Matches EntityId used throughout the system.</summary>
        public int EntityId;

        /// <summary>TeamId of this agent (0 = home, 1 = away).</summary>
        public int TeamId;

        /// <summary>World-space position (X, Y). Z is ignored at Stage 0.</summary>
        public Vector2 Position;

        /// <summary>World-space velocity vector (m/s). X, Y components used for lookahead.</summary>
        public Vector2 Velocity;

        /// <summary>Normalised facing direction (unit vector, X-Y plane).</summary>
        public Vector2 Facing;

        /// <summary>
        /// Fatigue scalar [0, 1]. 0.0 = fully rested; 1.0 = fully fatigued.
        /// Agents with Fatigue >= PressingAIConstants.PressFatigueCeiling are ineligible for press roles.
        /// </summary>
        public float Fatigue;

        /// <summary>
        /// FirstTouch attribute on the 1–20 skill scale.
        /// Used by WeakReceiver trigger (§3.1.4) and threat-score skill term (§3.4).
        /// </summary>
        public float FirstTouchAttribute;

        /// <summary>
        /// Quality scalar of the agent's most recent touch [0, 1].
        /// Used by BadTouch trigger (§3.1.1). 0 = worst touch, 1 = perfect.
        /// </summary>
        public float LastTouchQuality;

        /// <summary>
        /// Ball speed immediately after the agent's last touch (m/s).
        /// Used by BadTouch trigger (§3.1.1).
        /// </summary>
        public float PostTouchBallSpeed;

        /// <summary>True when this agent is the team's goalkeeper.</summary>
        public bool IsGoalkeeper;

        /// <summary>True when this agent currently has possession of the ball.</summary>
        public bool HasBall;

        /// <summary>
        /// True when this agent is active (on the pitch).
        /// False for substituted or red-carded agents; these are excluded from all
        /// eligibility and counting loops. §4.4.1 (sources: #7 §3.10 isActive field).
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// Positioning AI formation slot (world-space X, Y).
        /// Used by InvariantEnforcer (§3.9) to check MaxPressDisplacementM for shadow assignments.
        /// </summary>
        public Vector2 BaselineSlot;

        /// <summary>
        /// Positioning AI line assignment. Used by InvariantEnforcer (§3.9 invariant 2)
        /// to count backline agents in the defensive third.
        /// </summary>
        public TacticalDirector.PositioningAI.LineId Line;

        /// <summary>
        /// Defensive positioning discipline, raw 1–20 scale (mirrors Decision Tree #8's
        /// <c>DtAgentAttributes.Positioning</c> — the closest existing proxy for a defensive/marking
        /// attribute; no dedicated Marking/Tackling attribute exists yet in the master attribute
        /// table). Cheap-item addition (new §7.12 cover-shadow curve): used by
        /// <see cref="CoverShadowCurve.ComputeCurveEffectiveness"/> — a poorly-positioned defender
        /// curves less effectively.
        /// </summary>
        public float DefensivePositioningAttribute;

        /// <summary>
        /// Physical capacity to execute a curving pursuit at full effort — average of WorkRate,
        /// Pace, and Stamina (each raw 1–20 scale, mirroring <c>DtAgentAttributes</c>). Cheap-item
        /// addition (new §7.12): a low-effort/low-pace/tired defender cannot sustain the curve.
        /// </summary>
        public float PhysicalEffortAttribute;

        /// <summary>
        /// Mental read of the game needed to curve correctly rather than closing down directly —
        /// average of Decisions and Anticipation (each raw 1–20 scale, mirroring
        /// <c>DtAgentAttributes</c>). Cheap-item addition (new §7.12).
        /// </summary>
        public float MentalSharpnessAttribute;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1: added IsActive field (§4.4.1 / #7 §3.10). |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition (redesign, new §7.12): + DefensivePositioningAttribute / |
// |         |            |        |   PhysicalEffortAttribute / MentalSharpnessAttribute — gate the cover-shadow  |
// |         |            |        |   curving technique on the presser's own attributes (raw 1–20 scale, mirrors  |
// |         |            |        |   DtAgentAttributes; MatchEngine is the sole writer, sourced from the same     |
// |         |            |        |   _dtAttrs the Decision Tree already reads — no duplicate attribute invented). |
#endregion
