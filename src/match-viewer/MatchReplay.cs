// File:     src/match-viewer/MatchReplay.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling), Code Standards #20
// Purpose:  Immutable recorded match replay: per-frame world-state samples plus the static
//           metadata (rosters, pitch, cadence) the exporter needs to render them.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// A recorded match replay: the sampled <see cref="ReplayFrame"/> sequence plus static
    /// metadata. Produced by <see cref="MatchReplayRecorder"/>, consumed by
    /// <see cref="HtmlReplayExporter"/>. Immutable after construction (list exposed read-only,
    /// per-team arrays copied in — parallels the #19 manifest ReadOnlyCollection seam).
    /// </summary>
    public sealed class MatchReplay
    {
        private readonly int[]  _teamIds;
        private readonly bool[] _isGoalkeeper;

        /// <summary>Match seed the recorded engine was booted with (replay provenance).</summary>
        public ulong MatchSeed { get; }

        /// <summary>Physics ticks per second of the recorded engine (60 Hz).</summary>
        public int TicksPerSecond { get; }

        /// <summary>Physics ticks between captured frames (≥ 1).</summary>
        public int SampleStride { get; }

        /// <summary>Pitch length (goal-to-goal, X), metres.</summary>
        public float PitchLengthM { get; }

        /// <summary>Pitch width (touchline-to-touchline, Y), metres.</summary>
        public float PitchWidthM { get; }

        /// <summary>The sampled frames, oldest first (frame 0 = pre-tick kickoff state).</summary>
        public ReadOnlyCollection<ReplayFrame> Frames { get; }

        /// <summary>Number of agents per frame.</summary>
        public int AgentCount => _teamIds.Length;

        /// <summary>Team id (0 = home, 1 = away) of roster <paramref name="index"/>.</summary>
        public int TeamId(int index) => _teamIds[index];

        /// <summary>True when roster <paramref name="index"/> is a goalkeeper.</summary>
        public bool IsGoalkeeper(int index) => _isGoalkeeper[index];

        /// <summary>
        /// Constructs a replay. <paramref name="teamIds"/> / <paramref name="isGoalkeeper"/> are
        /// copied; <paramref name="frames"/> is wrapped read-only (the recorder hands over ownership).
        /// </summary>
        public MatchReplay(
            ulong matchSeed,
            int ticksPerSecond,
            int sampleStride,
            float pitchLengthM,
            float pitchWidthM,
            int[] teamIds,
            bool[] isGoalkeeper,
            List<ReplayFrame> frames)
        {
            if (teamIds == null) { throw new ArgumentNullException(nameof(teamIds)); }
            if (isGoalkeeper == null) { throw new ArgumentNullException(nameof(isGoalkeeper)); }
            if (frames == null) { throw new ArgumentNullException(nameof(frames)); }
            if (teamIds.Length != isGoalkeeper.Length)
            {
                throw new ArgumentException(
                    "teamIds and isGoalkeeper must describe the same roster length.", nameof(isGoalkeeper));
            }

            MatchSeed      = matchSeed;
            TicksPerSecond = ticksPerSecond;
            SampleStride   = sampleStride;
            PitchLengthM   = pitchLengthM;
            PitchWidthM    = pitchWidthM;
            _teamIds       = (int[])teamIds.Clone();
            _isGoalkeeper  = (bool[])isGoalkeeper.Clone();
            Frames         = new ReadOnlyCollection<ReplayFrame>(frames);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: immutable frame sequence + roster/pitch/    |
// |         |            |        | cadence metadata for the HTML exporter.                       |
#endregion
