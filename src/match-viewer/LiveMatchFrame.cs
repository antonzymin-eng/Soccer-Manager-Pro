// File:     src/match-viewer/LiveMatchFrame.cs
// Created:  2026-07-15
// Modified: 2026-07-27 (interactive Unity client §5-P1: richer observation frame — per-agent booking /
//           sent-off / substitute cues, the derived match period, and the latched last restart)
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md) +
//           interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1,
//           Code Standards #20
// Purpose:  One captured live-match snapshot: tick, ball, possession, agent positions + per-agent
//           cues, score, match period, the latched last restart, and the match-ended flag. Captured
//           by LiveMatchStreamer between engine ticks, served as JSON by LiveMatchServer.
//           Deliberately a separate type from ReplayFrame (which the already-reviewed post-hoc
//           replay/export pipeline consumes) — this frame carries fields (score, match-ended,
//           discipline, restart) a live HUD needs that a saved replay never did.

using UnityEngine;

using TacticalDirector.MatchEngine;

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

        /// <summary>Per-agent booking / sent-off / substitute cues, indexed by roster index in lockstep
        /// with <see cref="AgentPositions"/> (P1 KD-P1-6). Same exclusive-ownership convention.</summary>
        public readonly LiveAgentCue[] AgentCues;

        /// <summary>Substitutions used, indexed by team id (0 = home, 1 = away). Same exclusive-ownership
        /// convention as the two arrays above.</summary>
        public readonly int[] SubstitutionsUsed;

        /// <summary>Which period the clock is in (derived engine-side; P1 KD-P1-2).</summary>
        public readonly MatchPeriod Period;

        /// <summary>
        /// The most recent restart the streamer has observed, or <see cref="RestartCue.None"/> if none
        /// has occurred yet in this streaming session (P1 KD-P1-3).
        /// <para>LATCHED BY THE STREAMER, not by the engine: the engine reports a restart only for the
        /// tick it happened on, so a View polling at anything below the tick rate would miss every
        /// restart. The streamer observes every tick and holds the last one here. Pair with
        /// <see cref="LastRestartTick"/> to decide how long to keep showing it.</para>
        /// </summary>
        public readonly RestartCue LastRestart;

        /// <summary>Team (0/1) awarded <see cref="LastRestart"/>, or −1 when that is
        /// <see cref="RestartCue.None"/>.</summary>
        public readonly int LastRestartTeam;

        /// <summary>The tick <see cref="LastRestart"/> was applied on; 0 when none has been observed.
        /// A View fades its restart caption on <c>Tick − LastRestartTick</c>.</summary>
        public readonly ulong LastRestartTick;

        /// <summary>Constructs a frame. The caller owns <paramref name="agentPositions"/>,
        /// <paramref name="agentCues"/> and <paramref name="substitutionsUsed"/> exclusively — never
        /// retain or mutate them after this call.</summary>
        public LiveMatchFrame(
            ulong tick,
            Vector3 ballPosition,
            int possessingAgentId,
            Vector2[] agentPositions,
            int homeScore,
            int awayScore,
            bool matchEnded,
            LiveAgentCue[] agentCues,
            int[] substitutionsUsed,
            MatchPeriod period,
            RestartCue lastRestart,
            int lastRestartTeam,
            ulong lastRestartTick)
        {
            Tick              = tick;
            BallPosition      = ballPosition;
            PossessingAgentId = possessingAgentId;
            AgentPositions    = agentPositions;
            HomeScore         = homeScore;
            AwayScore         = awayScore;
            MatchEnded        = matchEnded;
            AgentCues         = agentCues;
            SubstitutionsUsed = substitutionsUsed;
            Period            = period;
            LastRestart       = lastRestart;
            LastRestartTeam   = lastRestartTeam;
            LastRestartTick   = lastRestartTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: sampled live-match frame (tick / ball /      |
// |         |            |        | possession / agent positions / score / match-ended) for the   |
// |         |            |        | interactive match view's streamer + server.                   |
// | 1.1     | 2026-07-27 | —      | P1 richer observation frame (interactive-unity-client-design   |
// |         |            |        | §5-P1): + per-agent LiveAgentCue[] (booking / sent-off /       |
// |         |            |        | substitute, KD-P1-6), per-team SubstitutionsUsed, the derived  |
// |         |            |        | MatchPeriod (KD-P1-2), and the streamer-latched last restart   |
// |         |            |        | (cue / team / tick, KD-P1-3). Assembly gains a match-engine    |
// |         |            |        | using for the two engine-owned presentation enums.             |
#endregion
