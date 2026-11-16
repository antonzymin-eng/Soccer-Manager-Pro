// File:     src/defensive-ai/MarkTargetComposer.cs
// Created:  2026-11-10
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14 §3.13.1, §4.5.1, ERR-014-010, Code Standards #20
// Purpose:  Pure static module: turns one agent's committed MarkAssignment into the world-independent
//           point the orchestrator hands to Decision Tree #8 as that agent's per-tick mark target.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Composes a per-agent mark target from a committed <see cref="MarkAssignment"/> (#14 §3.13.1,
    /// ERR-014-010). Everything is in the defending team's canonical frame (own goal at x = 0, #14 §2.3);
    /// the caller mirrors the result to world space. Pure: no state, no RNG, no allocation.
    ///
    /// <para>The point is <b>goal-side</b> of the marked opponent: it lies on the segment from the
    /// opponent's LIVE snapshot position <c>O</c> to the defended goal centre <c>G</c>, at the stand-off
    /// <c>s</c> from <c>O</c> (<c>|M − O| = s</c>), or at <c>G</c> itself when the opponent is within
    /// <c>s</c> of the goal. It is deliberately NOT <see cref="MarkAssignment.TargetPosition"/>: that is
    /// the opponent's own position, which a marker running to would arrive on top of his man instead of
    /// between him and the goal.</para>
    /// </summary>
    public static class MarkTargetComposer
    {
        /// <summary>
        /// Composes the mark target for <paramref name="assignment"/>. Returns <c>false</c> (no target;
        /// <paramref name="target"/> is zero and must not be read) when any of these holds:
        /// the mode is <see cref="MarkMode.Zonal"/> (no target — the agent keeps his slot);
        /// <c>ValidThroughTick != tick</c> (stale, §2.2.2); the stand-off is not a positive number;
        /// for MAN_MARK / INTERCEPT_RUNNER, the target opponent is absent from the snapshot or not active.
        /// COVER_GK_ZONE passes its zone centre through unchanged.
        /// </summary>
        /// <param name="assignment">The agent's committed assignment for this tick.</param>
        /// <param name="snapshot">The same tick's snapshot (canonical frame) the assignment was computed from.</param>
        /// <param name="tick">The tick the caller is composing for (compared with <c>ValidThroughTick</c>).</param>
        /// <param name="standoffM">Stand-off in metres, valid range (0, 3.0); see <see cref="TacticTranslation.MarkStandoffM"/>.</param>
        /// <param name="target">The canonical-frame mark point (m) when the result is true.</param>
        /// <returns>True when a mark target was composed.</returns>
        public static bool TryCompose(
            in MarkAssignment assignment,
            DefensiveSnapshot snapshot,
            int               tick,
            float             standoffM,
            out Vector2       target)
        {
            target = Vector2.zero;

            if (assignment.Mode == MarkMode.Zonal)
                return false;
            if (assignment.ValidThroughTick != tick)
                return false;
            if (!(standoffM > 0f)) // also rejects NaN
                return false;

            if (assignment.Mode == MarkMode.CoverGkZone)
            {
                // The zone centre IS the target (§3.9.2); there is no opponent to stand off from.
                target = assignment.TargetPosition;
                return true;
            }

            int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignment.TargetEntityId);
            if (oIdx < 0 || !snapshot.Agents[oIdx].IsActive)
                return false;

            Vector2 o = snapshot.Agents[oIdx].Position;
            Vector2 g = DefensiveAIConstants.OwnGoalCenter;
            float   vx = g.x - o.x;
            float   vy = g.y - o.y;
            float   len = Mathf.Sqrt(vx * vx + vy * vy);

            if (len > standoffM)
            {
                float k = standoffM / len;
                target = new Vector2(o.x + k * vx, o.y + k * vy);
            }
            else
            {
                target = g; // Within the stand-off of the goal: the point is the goal centre itself.
            }

            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-11-10 | —      | W13.1 / ERR-014-010: initial implementation of the §3.13.1 composition       |
// |         |            |        |   (goal-side stand-off point; COVER_GK_ZONE passthrough; no-target cases).   |
#endregion
