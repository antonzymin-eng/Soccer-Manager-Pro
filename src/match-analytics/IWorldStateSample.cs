// File:     src/match-analytics/IWorldStateSample.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 / §3.4, FR-AN-002 / FR-AN-010 / FR-AN-011,
//           Code Standards #20
// Purpose:  The observational world-state sample the aggregator bins for territorial share and
//           heatmaps — the second and last input #37 is permitted (FR-AN-002).

using UnityEngine;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// A read-only positional view of the world for one sampled tick: where the ball is, and where
    /// each agent is with the team it belongs to (the KD-6 agent→team map).
    ///
    /// <para>Deliberately narrower than <c>MatchEngine</c>'s observation surface — it exposes only the
    /// positions §3.4 bins, so no future derivation can quietly start reading engine state FR-AN-002
    /// does not permit.</para>
    /// </summary>
    public interface IWorldStateSample
    {
        /// <summary>Ball position in the pitch plane (metres, corner-origin: x goal-to-goal, y across).</summary>
        Vector2 BallPosition { get; }

        /// <summary>Number of agent slots this sample covers.</summary>
        int AgentCount { get; }

        /// <summary>Pitch-plane position of agent <paramref name="index"/> (metres).</summary>
        Vector2 AgentPosition(int index);

        /// <summary>Team of agent <paramref name="index"/> (0 = home, 1 = away), snapshotted at boot (KD-6).</summary>
        int AgentTeamId(int index);

        /// <summary>
        /// Whether agent <paramref name="index"/> is still participating.
        ///
        /// <para>Load-bearing for §3.4: a sent-off agent is deliberately left standing ON the pitch as
        /// a collision body (match-engine §5.Z Phase H), so a heatmap that bins every slot
        /// unconditionally would pour the rest of the match into whichever cell that player froze in
        /// — one dismissal at 20′ dominates the grid. Excluding them is what keeps the map a picture
        /// of where the match was played.</para>
        /// </summary>
        bool AgentIsActive(int index);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): the §3.4 positional sample seam.    |
// | 1.1     | 2026-07-27 | —      | AR-1 M-2: AgentIsActive added — without it the §3.4 binning    |
// |         |            |        | loop kept sampling sent-off agents, who stay frozen on the     |
// |         |            |        | pitch as collision bodies (§5.Z Phase H).                      |
#endregion
