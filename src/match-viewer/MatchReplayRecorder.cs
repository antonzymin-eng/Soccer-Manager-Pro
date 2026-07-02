// File:     src/match-viewer/MatchReplayRecorder.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling), Code Standards #20; observes the Match Engine
//           composition root (docs/tracking/match-engine-design.md)
// Purpose:  Runs a MatchEngine tick loop and samples world state between ticks through the engine's
//           public observation surface, producing an immutable MatchReplay for the HTML exporter.

using System;
using System.Collections.Generic;

using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Records a match replay by ticking a <see cref="MatchEngine.MatchEngine"/> and sampling its
    /// public observation surface (<c>BallView</c> / <c>AgentView</c> / <c>PossessingAgentId</c>)
    /// between ticks. Pure observer: it never mutates engine state, so recording a match is
    /// digest-identical to running the same engine unobserved.
    /// </summary>
    public static class MatchReplayRecorder
    {
        /// <summary>
        /// Boots a fresh engine from <paramref name="matchSeed"/> and records
        /// <paramref name="numTicks"/> ticks. Convenience overload of
        /// <see cref="Record(MatchEngine.MatchEngine, ulong, int, int)"/>.
        /// </summary>
        /// <param name="matchSeed">Deterministic match seed the engine is booted with.</param>
        /// <param name="numTicks">Number of 60 Hz physics ticks to run (&gt; 0).</param>
        /// <param name="sampleStride">Ticks per captured frame (&gt; 0); frame 0 (kickoff) is always captured.</param>
        public static MatchReplay Record(
            ulong matchSeed,
            int numTicks,
            int sampleStride = MatchViewerConstants.DefaultSampleStride)
        {
            return Record(new MatchEngine.MatchEngine(matchSeed), matchSeed, numTicks, sampleStride);
        }

        /// <summary>
        /// Records <paramref name="numTicks"/> ticks of an already-configured engine (e.g. one a
        /// caller applied a <c>TeamTacticConfig</c> to before kickoff). The engine should be at its
        /// boot state (tick 0) so frame 0 is the kickoff state; recording from a later tick is
        /// permitted and simply starts the frame sequence there.
        /// </summary>
        /// <param name="engine">The engine to tick and observe.</param>
        /// <param name="matchSeed">Seed recorded as replay provenance (the engine does not re-expose it).</param>
        /// <param name="numTicks">Number of 60 Hz physics ticks to run (&gt; 0).</param>
        /// <param name="sampleStride">Ticks per captured frame (&gt; 0); the pre-run state is always captured.</param>
        public static MatchReplay Record(
            MatchEngine.MatchEngine engine,
            ulong matchSeed,
            int numTicks,
            int sampleStride = MatchViewerConstants.DefaultSampleStride)
        {
            if (engine == null) { throw new ArgumentNullException(nameof(engine)); }
            if (numTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numTicks), numTicks, "numTicks must be > 0.");
            }
            if (sampleStride <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleStride), sampleStride, "sampleStride must be > 0.");
            }

            int[]  teamIds      = new int[MatchEngineConstants.SQUAD_SIZE];
            bool[] isGoalkeeper = new bool[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                teamIds[i]      = engine.AgentTeamId(i);
                isGoalkeeper[i] = engine.AgentIsGoalkeeper(i);
            }

            // +2: the pre-run frame plus every stride tick (upper bound; exact when stride divides numTicks).
            var frames = new List<ReplayFrame>(numTicks / sampleStride + 2);
            frames.Add(CaptureFrame(engine));

            for (int t = 1; t <= numTicks; t++)
            {
                engine.RunTick();
                if (t % sampleStride == 0 || t == numTicks)
                {
                    frames.Add(CaptureFrame(engine));
                }
            }

            return new MatchReplay(
                matchSeed,
                DeterministicSimConstants.PHYSICS_TICK_HZ,
                sampleStride,
                MatchEngineConstants.PITCH_LENGTH_M,
                MatchEngineConstants.PITCH_WIDTH_M,
                teamIds,
                isGoalkeeper,
                frames);
        }

        private static ReplayFrame CaptureFrame(MatchEngine.MatchEngine engine)
        {
            var positions = new Vector2[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                positions[i] = engine.AgentView(i).Position;
            }
            return new ReplayFrame(
                engine.CurrentTick,
                engine.BallView.Position,
                engine.PossessingAgentId,
                positions);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: seed + pre-configured-engine overloads;     |
// |         |            |        | fail-loud guards; kickoff frame + stride sampling + final     |
// |         |            |        | tick always captured.                                         |
#endregion
