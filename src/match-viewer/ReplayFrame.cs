// File:     src/match-viewer/ReplayFrame.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling), Code Standards #20
// Purpose:  One sampled world-state frame of a recorded match replay: tick, ball position,
//           possession, and all agent positions (copies — detached from the live engine buffers).

using UnityEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// One sampled replay frame. All values are copies taken between engine ticks via the
    /// <c>MatchEngine</c> public observation surface; the frame never aliases live world state.
    /// Positions are metres in the corner-origin frame (Ball Physics #1 §1.2).
    /// </summary>
    public readonly struct ReplayFrame
    {
        /// <summary>
        /// The 60 Hz physics tick this frame was sampled at. The recorder's first frame is the
        /// engine's pre-run state — tick 0 (kickoff) for a freshly booted engine, or the current
        /// tick when recording a pre-run engine (the recorder overload contract).
        /// </summary>
        public readonly ulong Tick;

        /// <summary>Ball position (x, y, z) in metres.</summary>
        public readonly Vector3 BallPosition;

        /// <summary>Possessing agent's roster index, or −1 when the ball is loose.</summary>
        public readonly int PossessingAgentId;

        /// <summary>
        /// Agent positions (x, y) in metres, indexed by roster index [0, SQUAD_SIZE). The array is
        /// allocated per frame by the recorder (viewer tooling — not on the simulation hot path)
        /// and is never mutated after construction.
        /// </summary>
        public readonly Vector2[] AgentPositions;

        /// <summary>Constructs a frame. The recorder owns <paramref name="agentPositions"/> exclusively.</summary>
        public ReplayFrame(ulong tick, Vector3 ballPosition, int possessingAgentId, Vector2[] agentPositions)
        {
            Tick              = tick;
            BallPosition      = ballPosition;
            PossessingAgentId = possessingAgentId;
            AgentPositions    = agentPositions;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: sampled frame (tick / ball / possession /   |
// |         |            |        | agent positions) for the match-viewer replay recorder.        |
// | 1.1     | 2026-07-02 | —      | AR-4 L (doc): Tick doc no longer claims "0 = kickoff" —       |
// |         |            |        | recording a pre-run engine starts at its current tick.        |
#endregion
