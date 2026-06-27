// File:     src/perception-system/RecognitionLatencyState.cs
// Created:  2026-06-27
// Author:   —
// Spec:     Perception System #7 §3.3; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view over a RecognitionLatencyTracker's per-(observer,target) cross-tick counters so
//           a host snapshot layer can serialize them canonically for deterministic save/restore.

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Immutable view over a <see cref="RecognitionLatencyTracker"/>'s cross-tick state — the per-pair
    /// latency counters, the confirmation flags, and the expiry countdowns (each flattened
    /// <c>observer * MaxAgents + target</c>, length <see cref="PairCapacity"/>). Produced by
    /// <see cref="RecognitionLatencyTracker.CaptureState"/> for the Match Engine snapshot layer (design
    /// note §2.6). The arrays are references to the live, allocated-once-per-match instances (read-only
    /// serialization use only).
    /// </summary>
    public readonly struct RecognitionLatencyState
    {
        /// <summary>Per-pair ticks visible-but-unconfirmed (-1 = untracked). Length = PairCapacity.</summary>
        public readonly int[] LatencyCounters;

        /// <summary>Per-pair confirmed flag (counter reached L_rec). Length = PairCapacity.</summary>
        public readonly bool[] Confirmed;

        /// <summary>Per-pair expiry countdown after the entity leaves visibility. Length = PairCapacity.</summary>
        public readonly int[] ExpiryCounters;

        /// <summary>Flattened pair-array length (MaxAgents²).</summary>
        public int PairCapacity => LatencyCounters.Length;

        /// <summary>Bundles the live tracker arrays into an immutable view.</summary>
        public RecognitionLatencyState(int[] latencyCounters, bool[] confirmed, int[] expiryCounters)
        {
            LatencyCounters = latencyCounters;
            Confirmed       = confirmed;
            ExpiryCounters  = expiryCounters;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | perception recognition-latency snapshot view.                  |
#endregion
