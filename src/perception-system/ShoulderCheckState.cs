// File:     src/perception-system/ShoulderCheckState.cs
// Created:  2026-06-27
// Author:   —
// Spec:     Perception System #7 §3.4; Match Engine design note §2.6 (Phase D D4 follow-up); Code Standards #20
// Purpose:  Read-only view over a ShoulderCheckScheduler's per-agent scheduling state and per-pair blind-side
//           counters so a host snapshot layer can serialize them canonically for deterministic save/restore.

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Immutable view over a <see cref="ShoulderCheckScheduler"/>'s cross-tick state — the per-agent
    /// next-check / window-expiry frames, the window-active flags, the per-agent
    /// <see cref="ShoulderCheckAnimData"/>, and the per-pair blind-side latency / confirmed arrays.
    /// Produced by <see cref="ShoulderCheckScheduler.CaptureState"/> for the Match Engine snapshot layer
    /// (design note §2.6). The arrays are references to the live, allocated-once-per-match instances
    /// (read-only serialization use only). Per-agent arrays have length <see cref="AgentCapacity"/>; the
    /// blind-side arrays have length <see cref="PairCapacity"/> (AgentCapacity²).
    /// </summary>
    public readonly struct ShoulderCheckState
    {
        /// <summary>Per-agent next scheduled shoulder-check frame. Length = AgentCapacity.</summary>
        public readonly int[] NextCheckFrame;

        /// <summary>Per-agent active-window expiry frame. Length = AgentCapacity.</summary>
        public readonly int[] WindowExpiryFrame;

        /// <summary>Per-agent window-active flag. Length = AgentCapacity.</summary>
        public readonly bool[] WindowActive;

        /// <summary>Per-agent current shoulder-check animation data. Length = AgentCapacity.</summary>
        public readonly ShoulderCheckAnimData[] AnimData;

        /// <summary>Per-pair blind-side latency counter (-1 = untracked). Length = PairCapacity.</summary>
        public readonly int[] BlindSideLatency;

        /// <summary>Per-pair blind-side confirmed flag. Length = PairCapacity.</summary>
        public readonly bool[] BlindSideConfirmed;

        /// <summary>Per-agent array length (MaxAgents).</summary>
        public int AgentCapacity => NextCheckFrame.Length;

        /// <summary>Flattened pair-array length (MaxAgents²).</summary>
        public int PairCapacity => BlindSideLatency.Length;

        /// <summary>Bundles the live scheduler arrays into an immutable view.</summary>
        public ShoulderCheckState(
            int[] nextCheckFrame,
            int[] windowExpiryFrame,
            bool[] windowActive,
            ShoulderCheckAnimData[] animData,
            int[] blindSideLatency,
            bool[] blindSideConfirmed)
        {
            NextCheckFrame     = nextCheckFrame;
            WindowExpiryFrame  = windowExpiryFrame;
            WindowActive       = windowActive;
            AnimData           = animData;
            BlindSideLatency   = blindSideLatency;
            BlindSideConfirmed = blindSideConfirmed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-27 | —      | Initial implementation — Match Engine Phase D D4 follow-up      |
// |         |            |        | perception shoulder-check-scheduler snapshot view.             |
#endregion
