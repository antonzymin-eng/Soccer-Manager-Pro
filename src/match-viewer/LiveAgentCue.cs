// File:     src/match-viewer/LiveAgentCue.cs
// Created:  2026-07-27
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1 KD-P1-6,
//           Code Standards #20
// Purpose:  The per-agent presentation cues a View draws beside a position: booking state, sending-off,
//           and whether this pitch slot is currently occupied by a substitute.

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// One agent's non-positional presentation cues, sampled between engine ticks from
    /// <c>MatchEngine</c>'s public observation surface.
    ///
    /// <para><b>Why one struct rather than parallel arrays (KD-P1-6).</b> These ride beside
    /// <c>LiveMatchFrame.AgentPositions</c> as a single array. Three separate arrays would widen the
    /// frame's constructor every time a cue is added, and P1 exists precisely so the render layer targets
    /// the frame shape once — so future per-agent cues extend this struct instead.</para>
    ///
    /// <para>Unlike positions, these change perhaps thirty times in a match; they are re-sampled every
    /// tick anyway because a frame must be internally consistent (a cue array from a different tick than
    /// the positions it annotates is a bug waiting to be drawn).</para>
    /// </summary>
    public readonly struct LiveAgentCue
    {
        /// <summary>Yellow cards held (0 or 1 — a second yellow is promoted to a sending-off).</summary>
        public readonly int YellowCards;

        /// <summary>True when this agent has been sent off. A sent-off agent still has a position (they
        /// remain a physical body in the engine) but takes no part in play.</summary>
        public readonly bool IsSentOff;

        /// <summary>Bench slot now occupying this pitch slot, or −1 when the original starter is on.</summary>
        public readonly int BenchSlot;

        /// <summary>True when a substitute currently occupies this pitch slot.</summary>
        public bool IsSubstitute => BenchSlot >= 0;

        /// <summary>
        /// True when the agent currently in this pitch slot is a goalkeeper.
        ///
        /// <para><b>Why this is per-frame and not roster metadata.</b> The engine rewrites a slot's
        /// goalkeeper flag when a substitution fills it (<c>MatchEngine.SubstitutePlayer</c> copies
        /// the bench player's flag into the on-pitch array), so a keeper coming off for an outfield
        /// player — or an outfield player for a keeper — moves which slot is the goalkeeper.
        /// <c>LiveMatchStreamer</c> caches team ids once because they genuinely cannot change; this
        /// flag can, so it rides the frame with the rest of the per-tick cues.</para>
        /// </summary>
        public readonly bool IsGoalkeeper;

        /// <summary>Constructs one agent's cue set.</summary>
        public LiveAgentCue(int yellowCards, bool isSentOff, int benchSlot, bool isGoalkeeper)
        {
            YellowCards  = yellowCards;
            IsSentOff    = isSentOff;
            BenchSlot    = benchSlot;
            IsGoalkeeper = isGoalkeeper;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame, KD-P1-6): the   |
// |         |            |        | per-agent booking / sent-off / substitute cue set.             |
// | 1.1     | 2026-08-03 | —      | P4a: + IsGoalkeeper, the first cue added via the KD-P1-6        |
// |         |            |        | extension mechanism this struct was created for. It is here    |
// |         |            |        | rather than in LiveMatchStreamer's boot-cached roster because  |
// |         |            |        | a substitution rewrites the slot's goalkeeper flag, so the     |
// |         |            |        | cached copy went stale the moment a keeper was substituted.    |
// |         |            |        | The ctor gains a fourth parameter (no default: a producer that |
// |         |            |        | forgets it must fail to compile, not silently report no        |
// |         |            |        | keeper on the pitch).                                          |
#endregion
