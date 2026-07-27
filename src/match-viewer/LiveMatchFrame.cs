// File:     src/match-viewer/LiveMatchFrame.cs
// Created:  2026-07-15
// Modified: 2026-07-27 (P1 AR-1 M-6: the score and restart triples collapsed into Scoreline /
//           RestartBanner, so no two constructor parameters share a type)
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
    /// construction; the caller (<c>LiveMatchStreamer</c>) hands over exclusive ownership of the three
    /// arrays — the same convention <c>ReplayFrame</c> documents.
    ///
    /// <para><b>On the constructor's shape.</b> Related values travel as one parameter
    /// (<see cref="Scoreline"/>, <see cref="RestartBanner"/>) rather than as loose scalars, so no two
    /// parameters share a type and none can be transposed silently. The frame is expected to keep
    /// growing as the client does; the rule that keeps that safe is that a new cue joins an existing
    /// carrier — <see cref="LiveAgentCue"/> for per-agent state, a value type for anything else — rather
    /// than widening this list with another bare <c>int</c> or <c>ulong</c>.</para>
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

        /// <summary>Per-agent booking / sent-off / substitute cues, indexed by roster index in lockstep
        /// with <see cref="AgentPositions"/> (P1 KD-P1-6). Same exclusive-ownership convention.</summary>
        public readonly LiveAgentCue[] AgentCues;

        /// <summary>Substitutions used, indexed by team id (0 = home, 1 = away). Same exclusive-ownership
        /// convention as the two arrays above.</summary>
        public readonly int[] SubstitutionsUsed;

        /// <summary>Goals, home and away.</summary>
        public readonly Scoreline Score;

        /// <summary>True once full time has fired — gameplay is frozen; the streamer auto-pauses on this.</summary>
        public readonly bool MatchEnded;

        /// <summary>Which period the clock is in (derived engine-side; P1 KD-P1-2).</summary>
        public readonly MatchPeriod Period;

        /// <summary>
        /// The most recent restart the streamer has observed, or <see cref="RestartBanner.None"/>.
        /// <para>LATCHED BY THE STREAMER, not by the engine: the engine reports a restart only for the
        /// tick it happened on, so a View polling at anything below the tick rate would miss every
        /// restart. The streamer observes every tick and holds the last one here.</para>
        /// </summary>
        public readonly RestartBanner Restart;

        /// <summary>Constructs a frame. The caller owns <paramref name="agentPositions"/>,
        /// <paramref name="agentCues"/> and <paramref name="substitutionsUsed"/> exclusively — never
        /// retain or mutate them after this call.</summary>
        public LiveMatchFrame(
            ulong tick,
            Vector3 ballPosition,
            int possessingAgentId,
            Vector2[] agentPositions,
            LiveAgentCue[] agentCues,
            int[] substitutionsUsed,
            Scoreline score,
            bool matchEnded,
            MatchPeriod period,
            RestartBanner restart)
        {
            Tick              = tick;
            BallPosition      = ballPosition;
            PossessingAgentId = possessingAgentId;
            AgentPositions    = agentPositions;
            AgentCues         = agentCues;
            SubstitutionsUsed = substitutionsUsed;
            Score             = score;
            MatchEnded        = matchEnded;
            Period            = period;
            Restart           = restart;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: sampled live-match frame (tick / ball /      |
// |         |            |        | possession / agent positions / score / match-ended) for the   |
// |         |            |        | interactive match view's streamer + server.                   |
// | 1.1     | 2026-07-27 | —      | P1 richer observation frame (interactive-unity-client-design   |
// |         |            |        | §5-P1): + per-agent LiveAgentCue[] (KD-P1-6), per-team         |
// |         |            |        | SubstitutionsUsed, the derived MatchPeriod (KD-P1-2), and the  |
// |         |            |        | streamer-latched last restart (KD-P1-3). Assembly gains a      |
// |         |            |        | match-engine using for the two engine-owned presentation enums.|
// | 1.2     | 2026-07-27 | —      | P1 AR-1 M-6: v1.1's ctor reached 13 positional parameters with |
// |         |            |        | two transposable ulongs (tick / lastRestartTick) and three     |
// |         |            |        | ints. Score → Scoreline, the restart triple → RestartBanner:   |
// |         |            |        | 10 parameters, no two sharing a type. HomeScore/AwayScore and  |
// |         |            |        | LastRestart* are replaced by Score / Restart (all readers      |
// |         |            |        | updated) rather than kept as duplicate accessors.              |
#endregion
