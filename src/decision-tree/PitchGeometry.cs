// File:     src/decision-tree/PitchGeometry.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.2.1.4, Code Standards #20
// Purpose:  Static pitch geometry constants owned by the Decision Tree.
//           Goals are fixed landmarks that agents always know. Not perceptual data.
//           See §3.2.1.4 design decision rationale.

using UnityEngine;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Fixed pitch geometry for a standard 105m × 68m pitch (corner-origin coordinates).
    /// Origin at (0,0,0) = corner of pitch. X = goal-to-goal, Y = touchline-to-touchline.
    /// Goal width: 7.32m (each post ±3.66m from centre). Ball Physics #1 §1.2.
    /// Decision Tree #8 §3.2.1.4.
    /// </summary>
    public static class PitchGeometry
    {
        // [FIXED] — FIFA regulation pitch dimensions
        public const float PitchLengthM      = 105.0f;
        public const float PitchWidthM       = 68.0f;
        public const float GoalWidthM        = 7.32f;
        public const float GoalHalfWidthM    = 3.66f;
        public const float GoalHeightM       = 2.44f;

        // [FIXED] — goal centre Y = pitch half-width
        private const float GoalCentreY = 34.0f;

        // ── Home team perspective (home attacks toward x=105) ──────────────────

        /// <summary>Centre of the opponent goal for the home team. §3.2.1.4.</summary>
        public static readonly Vector2 HomeOpponentGoalCentre = new Vector2(PitchLengthM, GoalCentreY);

        /// <summary>Left goal post of opponent goal (home perspective, y-axis).</summary>
        public static readonly Vector2 HomeOpponentGoalPostL = new Vector2(PitchLengthM, GoalCentreY - GoalHalfWidthM);

        /// <summary>Right goal post of opponent goal (home perspective, y-axis).</summary>
        public static readonly Vector2 HomeOpponentGoalPostR = new Vector2(PitchLengthM, GoalCentreY + GoalHalfWidthM);

        /// <summary>Centre of the own goal for the home team.</summary>
        public static readonly Vector2 HomeOwnGoalCentre = new Vector2(0.0f, GoalCentreY);

        // ── Away team perspective (away attacks toward x=0) ───────────────────

        /// <summary>Centre of the opponent goal for the away team.</summary>
        public static readonly Vector2 AwayOpponentGoalCentre = new Vector2(0.0f, GoalCentreY);

        public static readonly Vector2 AwayOpponentGoalPostL = new Vector2(0.0f, GoalCentreY - GoalHalfWidthM);
        public static readonly Vector2 AwayOpponentGoalPostR = new Vector2(0.0f, GoalCentreY + GoalHalfWidthM);

        /// <summary>Centre of the own goal for the away team.</summary>
        public static readonly Vector2 AwayOwnGoalCentre = new Vector2(PitchLengthM, GoalCentreY);

        // ── Zone boundaries (x-axis) ──────────────────────────────────────────
        // [DERIVED] — split pitch into thirds
        public const float DefensiveZoneMaxX  = 35.0f;
        public const float AttackingZoneMinX  = 65.0f;

        // ── Team-neutral API ──────────────────────────────────────────────────

        /// <summary>
        /// Opponent goal centre for a given team. teamId=0 → home, teamId=1 → away.
        /// Consistent with AgentState.TeamId convention. §3.2.1.4.
        /// </summary>
        public static Vector2 GetOpponentGoalCentre(int teamId)
            => teamId == 0 ? HomeOpponentGoalCentre : AwayOpponentGoalCentre;

        /// <summary>Left opponent goal post (lower Y) for the given team.</summary>
        public static Vector2 GetOpponentGoalPostL(int teamId)
            => teamId == 0 ? HomeOpponentGoalPostL : AwayOpponentGoalPostL;

        /// <summary>Right opponent goal post (higher Y) for the given team.</summary>
        public static Vector2 GetOpponentGoalPostR(int teamId)
            => teamId == 0 ? HomeOpponentGoalPostR : AwayOpponentGoalPostR;

        /// <summary>Own goal centre for the given team.</summary>
        public static Vector2 GetOwnGoalCentre(int teamId)
            => teamId == 0 ? HomeOwnGoalCentre : AwayOwnGoalCentre;

        /// <summary>
        /// Compute FieldZone for a ball position x-coordinate (home-team perspective).
        /// §3.2.1.3.
        /// </summary>
        public static FieldZone ComputeFieldZone(float posX)
        {
            if (posX <= DefensiveZoneMaxX) return FieldZone.DEFENSIVE;
            if (posX >= AttackingZoneMinX) return FieldZone.ATTACKING;
            return FieldZone.MIDFIELD;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
