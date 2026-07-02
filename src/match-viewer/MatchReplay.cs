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
using System.Globalization;

using UnityEngine;

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

        /// <summary>
        /// Physics ticks between captured frames (≥ 1). Provenance metadata only — the exported
        /// player derives playback timing from consecutive frame TICK deltas (which also cover the
        /// recorder's forced final capture at a shorter interval), never from this value.
        /// </summary>
        public int SampleStride { get; }

        /// <summary>Pitch length (goal-to-goal, X), metres.</summary>
        public float PitchLengthM { get; }

        /// <summary>Pitch width (touchline-to-touchline, Y), metres.</summary>
        public float PitchWidthM { get; }

        /// <summary>The sampled frames, oldest first (frame 0 = the engine's pre-run state).</summary>
        public ReadOnlyCollection<ReplayFrame> Frames { get; }

        /// <summary>Number of agents per frame.</summary>
        public int AgentCount => _teamIds.Length;

        /// <summary>Team id (0 = home, 1 = away) of roster <paramref name="index"/>.</summary>
        public int TeamId(int index)
        {
            GuardRosterIndex(index);
            return _teamIds[index];
        }

        /// <summary>True when roster <paramref name="index"/> is a goalkeeper.</summary>
        public bool IsGoalkeeper(int index)
        {
            GuardRosterIndex(index);
            return _isGoalkeeper[index];
        }

        /// <summary>Public-surface roster-index guard (parallel to the MatchEngine observation surface).</summary>
        private void GuardRosterIndex(int index)
        {
            if (index < 0 || index >= _teamIds.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "index must be a roster index in [0, AgentCount).");
            }
        }

        /// <summary>
        /// Constructs a replay. <paramref name="teamIds"/> / <paramref name="isGoalkeeper"/> are
        /// copied, and <paramref name="frames"/> is snapshot-copied BEFORE validation — the
        /// validated snapshot is what this instance exposes, so later mutation of the caller's
        /// list can neither change the replay nor smuggle an unvalidated frame past the guards.
        /// Every frame's <see cref="ReplayFrame.AgentPositions"/> must be non-null and match the
        /// roster length — an incoherent frame would otherwise reach the exporter as a short/long
        /// data row the canvas renderer silently no-ops on, so it is refused here (fail loud).
        /// NOTE: the per-frame position arrays themselves are NOT copied — immutability rests on
        /// the hand-over convention documented on <see cref="ReplayFrame"/> (the recorder never
        /// retains or mutates them). Do not hand this constructor arrays you keep writing to.
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
            if (teamIds.Length == 0)
            {
                throw new ArgumentException("the roster must contain at least one agent.", nameof(teamIds));
            }
            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "ticksPerSecond must be > 0.");
            }
            if (sampleStride <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleStride), sampleStride, "sampleStride must be > 0.");
            }
            // Project NaN-gate pattern: !(x > 0) also rejects NaN (which compares false).
            if (!(pitchLengthM > 0f) || float.IsInfinity(pitchLengthM))
            {
                throw new ArgumentOutOfRangeException(nameof(pitchLengthM), pitchLengthM, "pitchLengthM must be finite and > 0.");
            }
            if (!(pitchWidthM > 0f) || float.IsInfinity(pitchWidthM))
            {
                throw new ArgumentOutOfRangeException(nameof(pitchWidthM), pitchWidthM, "pitchWidthM must be finite and > 0.");
            }
            // Snapshot BEFORE validating: the guards below run against the copy this instance
            // keeps, so a caller mutating its list after construction cannot bypass them
            // (ReadOnlyCollection is a view, not a copy — wrapping the caller's list would let
            // frames.Add(...) inject unvalidated rows into an already-constructed replay).
            var snapshot = new List<ReplayFrame>(frames);

            if (snapshot.Count == 0)
            {
                // An empty replay would export a document whose player crashes at first render
                // (blank pitch, dead controls) — refused here per the fail-loud convention.
                throw new ArgumentException("frames must contain at least one frame.", nameof(frames));
            }
            for (int f = 0; f < snapshot.Count; f++)
            {
                Vector2[] positions = snapshot[f].AgentPositions;
                if (positions == null || positions.Length != teamIds.Length)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "frame {0} AgentPositions must be non-null and match the roster length {1}.",
                            f, teamIds.Length),
                        nameof(frames));
                }
                // Strictly increasing ticks: the player derives per-interval playback timing from
                // consecutive tick deltas, so dt <= 0 would stall or reverse playback.
                if (f > 0 && snapshot[f].Tick <= snapshot[f - 1].Tick)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "frame ticks must be strictly increasing (frame {0}: {1} after {2}).",
                            f, snapshot[f].Tick, snapshot[f - 1].Tick),
                        nameof(frames));
                }
                // Possession must be the loose sentinel (−1) or a roster index — an out-of-range
                // id would render no possession ring while the HUD names a nonexistent agent.
                if (snapshot[f].PossessingAgentId < -1 || snapshot[f].PossessingAgentId >= teamIds.Length)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "frame {0} PossessingAgentId {1} must be -1 (loose) or a roster index in [0, {2}).",
                            f, snapshot[f].PossessingAgentId, teamIds.Length),
                        nameof(frames));
                }
            }

            MatchSeed      = matchSeed;
            TicksPerSecond = ticksPerSecond;
            SampleStride   = sampleStride;
            PitchLengthM   = pitchLengthM;
            PitchWidthM    = pitchWidthM;
            _teamIds       = (int[])teamIds.Clone();
            _isGoalkeeper  = (bool[])isGoalkeeper.Clone();
            Frames         = new ReadOnlyCollection<ReplayFrame>(snapshot);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: immutable frame sequence + roster/pitch/    |
// |         |            |        | cadence metadata for the HTML exporter.                       |
// | 1.1     | 2026-07-02 | —      | AR-1 M-1: ctor refuses frames whose AgentPositions is null or |
// |         |            |        | mismatches the roster length (fail loud — the canvas renderer |
// |         |            |        | would silently no-op on a short/long data row). AR-1 L-1:     |
// |         |            |        | frame-array hand-over convention documented (arrays NOT       |
// |         |            |        | copied; immutability by convention per ReplayFrame doc).      |
// | 1.2     | 2026-07-02 | —      | AR-2: ctor refuses empty frames (exported player crashed at   |
// |         |            |        | first render — blank pitch, dead controls) and non-strictly-  |
// |         |            |        | increasing frame ticks (player per-interval timing needs      |
// |         |            |        | dt > 0); metadata guards (ticksPerSecond/sampleStride > 0,    |
// |         |            |        | pitch dims finite > 0, NaN-gate pattern). TeamId/IsGoalkeeper |
// |         |            |        | gain the roster-index guard (parallel to MatchEngine v1.25).  |
// | 1.3     | 2026-07-02 | —      | AR-3 M-1: frames list snapshot-copied BEFORE validation and   |
// |         |            |        | the SNAPSHOT wrapped — ReadOnlyCollection is a view, so       |
// |         |            |        | wrapping the caller's live list let frames.Add(...) inject    |
// |         |            |        | unvalidated rows past every AR-1/AR-2 ctor guard. AR-3 L:     |
// |         |            |        | PossessingAgentId validated (−1 or roster index — an out-of-  |
// |         |            |        | range id drew no ring while the HUD named a nonexistent       |
// |         |            |        | agent); empty roster refused.                                 |
// | 1.4     | 2026-07-02 | —      | AR-4 L (doc): SampleStride marked provenance-only — the       |
// |         |            |        | player times playback from frame tick deltas, never this.     |
#endregion
