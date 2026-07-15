// File:     src/match-viewer/LiveMatchFrame.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Code Standards #20
// Purpose:  One captured live-match snapshot: tick, ball, possession, agent positions, score, and
//           the match-ended flag. Captured by LiveMatchStreamer between engine ticks, served as
//           JSON by LiveMatchServer. Deliberately a separate type from ReplayFrame (which the
//           already-reviewed post-hoc replay/export pipeline consumes) — this frame carries fields
//           (score, match-ended) a live HUD needs that a saved replay never did.

using UnityEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// One sampled live-match frame. All values are copies taken between engine ticks via
    /// <c>MatchEngine</c>'s public observation surface; the frame never aliases live world state.
    /// Positions are metres in the corner-origin frame (Ball Physics #1 §1.2). Immutable after
    /// construction; the caller (<c>LiveMatchStreamer</c>) hands over exclusive ownership of
    /// <see cref="AgentPositions"/> — the same convention <c>ReplayFrame</c> documents.
    /// </summary>
    public readonly struct LiveMatchFrame
    {
        /// <summary>The 60 Hz physics tick this frame was sampled at.</summary>
        public readonly ulong Tick;

        /// <summary>Ball position (x, y, z) in metres.</summary>
        public readonly Vector3 BallPosition;

        /// <summary>Possessing agent's roster index, or −1 when the ball is loose.</summary>
        public readonly int PossessingAgentId;

        /// <summary>Agent positions (x, y) in metres, indexed by roster index [0, SQUAD_SIZE).</summary>
        public readonly Vector2[] AgentPositions;

        /// <summary>Home team's (team 0) current goal count.</summary>
        public readonly int HomeScore;

        /// <summary>Away team's (team 1) current goal count.</summary>
        public readonly int AwayScore;

        /// <summary>True once full time has fired — gameplay is frozen; the streamer auto-pauses on this.</summary>
        public readonly bool MatchEnded;

        /// <summary>Constructs a frame. The caller owns <paramref name="agentPositions"/> exclusively — never retain or mutate it after this call.</summary>
        public LiveMatchFrame(
            ulong tick,
            Vector3 ballPosition,
            int possessingAgentId,
            Vector2[] agentPositions,
            int homeScore,
            int awayScore,
            bool matchEnded)
        {
            Tick              = tick;
            BallPosition      = ballPosition;
            PossessingAgentId = possessingAgentId;
            AgentPositions    = agentPositions;
            HomeScore         = homeScore;
            AwayScore         = awayScore;
            MatchEnded        = matchEnded;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: sampled live-match frame (tick / ball /      |
// |         |            |        | possession / agent positions / score / match-ended) for the   |
// |         |            |        | interactive match view's streamer + server.                   |
#endregion
