// File:     src/shot-mechanics/GoalGeometry.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §4.1.1, Code Standards #20
// Purpose:  Value struct for goal geometry parameters (width, height, line position, post positions).
//           Passed by value to ShotPlacementResolver — no heap allocation. §4.1.1.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Goal geometry parameters for a single evaluation. Passed by value — no heap allocation.
    /// Coordinate note: GoalLineX is the pitch X-axis position of the goal line.
    /// LeftPostY / RightPostY are Y-axis (touchline) positions of the posts.
    /// CrossbarZ is the Z-axis (height) of the crossbar underside.
    /// Shot Mechanics #6 §4.1.1.
    /// </summary>
    public struct GoalGeometry
    {
        /// <summary>Full internal goal width (metres): left post to right post.</summary>
        public float GoalWidth;

        /// <summary>Goal height (metres): ground to underside of crossbar.</summary>
        public float GoalHeight;

        /// <summary>
        /// World-space X coordinate of the goal line (pitch X axis, goal-to-goal direction).
        /// Attacking-right goal: X = PitchLength (105m). Attacking-left goal: X = 0m.
        /// NOTE: Spec #6 v0.1 §4.1.1 (pre-implementation draft) names this field GoalLineZ; implementation uses GoalLineX to match
        /// the authoritative coordinate system (CLAUDE.md: X = goal-to-goal).
        /// </summary>
        public float GoalLineX;

        /// <summary>
        /// World-space Y coordinate of the left post (pitch Y axis, touchline direction).
        /// From the attacker's perspective facing the goal. §4.1.1.
        /// NOTE: Spec #6 v0.1 §4.1.1 (pre-implementation draft) names this field LeftPostX; implementation uses LeftPostY to match
        /// the authoritative coordinate system (CLAUDE.md: Y = touchline-to-touchline).
        /// </summary>
        public float LeftPostY;

        /// <summary>
        /// World-space Y coordinate of the right post.
        /// NOTE: Spec #6 v0.1 §4.1.1 (pre-implementation draft) names this field RightPostX; see LeftPostY note.
        /// </summary>
        public float RightPostY;

        /// <summary>
        /// World-space Z coordinate of the crossbar underside (pitch Z axis = height).
        /// NOTE: Spec #6 v0.1 §4.1.1 (pre-implementation draft) names this field CrossbarY; implementation uses CrossbarZ.
        /// </summary>
        public float CrossbarZ;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                        |
// | 1.0     | 2026-05-28 | —      | Extracted from GoalGeometryProvider.cs (H-2). |
// | 1.1     | 2026-06-01 | —      | AR-2 L-2: cite spec version (v0.1) in axis-rename notes.    |
#endregion
