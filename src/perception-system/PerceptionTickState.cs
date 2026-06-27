// File:     src/perception-system/PerceptionTickState.cs
// Created:  2026-06-27
// Author:   —
// Spec:     Perception System #7 §3.3, §3.4, §3.5; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view bundling a PerceptionSystem's cross-tick state (recognition-latency tracker,
//           shoulder-check scheduler, per-agent ball-perception carry-over) so a host snapshot layer can
//           serialize it canonically for deterministic save/restore.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Immutable view over a <see cref="PerceptionSystem"/>'s cross-tick state — the
    /// <see cref="RecognitionLatencyState"/> and <see cref="ShoulderCheckState"/> sub-views plus the
    /// per-agent ball-perception carry-over arrays (previous-frame visibility, last perceived position,
    /// and accumulated staleness frames, each length <see cref="AgentCount"/>). Produced by
    /// <see cref="PerceptionSystem.CaptureState"/> for the Match Engine snapshot layer (design note §2.6),
    /// the perception analogue of the mechanics-AI CaptureState seams. The arrays/sub-views reference the
    /// live, allocated-once-per-match instances (read-only serialization use only).
    /// </summary>
    public readonly struct PerceptionTickState
    {
        /// <summary>Recognition-latency tracker cross-tick counters (§3.3).</summary>
        public readonly RecognitionLatencyState Latency;

        /// <summary>Shoulder-check scheduler cross-tick state (§3.4).</summary>
        public readonly ShoulderCheckState ShoulderCheck;

        /// <summary>Per-agent previous-frame ball-visible flag (§3.5). Length = AgentCount.</summary>
        public readonly bool[] BallVisiblePrev;

        /// <summary>Per-agent last perceived ball position (§3.5). Length = AgentCount.</summary>
        public readonly Vector2[] BallPerceivedPositionPrev;

        /// <summary>Per-agent accumulated ball-staleness frames (§3.5). Length = AgentCount.</summary>
        public readonly int[] BallStalenessFramesPrev;

        /// <summary>Per-agent array length (MaxAgents).</summary>
        public int AgentCount => BallVisiblePrev.Length;

        /// <summary>Bundles the live tracker/scheduler sub-views and ball-prev arrays into an immutable view.</summary>
        public PerceptionTickState(
            in RecognitionLatencyState latency,
            in ShoulderCheckState shoulderCheck,
            bool[] ballVisiblePrev,
            Vector2[] ballPerceivedPositionPrev,
            int[] ballStalenessFramesPrev)
        {
            Latency                   = latency;
            ShoulderCheck             = shoulderCheck;
            BallVisiblePrev           = ballVisiblePrev;
            BallPerceivedPositionPrev = ballPerceivedPositionPrev;
            BallStalenessFramesPrev   = ballStalenessFramesPrev;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | perception cross-tick snapshot bundle view.                    |
#endregion
