// File:     src/deterministic-sim/SnapshotPayload.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.9.2, §3.4, Code Standards #20
// Purpose:  Holds the serialized authoritative state bytes (Tier A + Tier B) and the DespawnLog
//           for one snapshot tick. SHA-256 over this buffer is the CurrentSnapshotDigest.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Serialized authoritative state for one snapshot tick.
    /// Contains Tier A + Tier B fields from all authoritative subsystems and the DespawnLog entries.
    /// SHA-256 over PayloadBytes is the CurrentSnapshotDigest embedded in SnapshotHeader.
    /// Deterministic Simulation #16 §3.9.2.
    /// </summary>
    public sealed class SnapshotPayload
    {
        /// <summary>Pre-allocated byte buffer for the canonical serialized state.</summary>
        public readonly byte[] PayloadBytes;

        /// <summary>Number of bytes written into PayloadBytes (valid range [0, Capacity]).</summary>
        public int BytesWritten;

        /// <summary>Maximum capacity in bytes.</summary>
        public int Capacity => PayloadBytes.Length;

        /// <summary>
        /// Allocates the payload buffer at match start.
        /// No allocation on hot path after this call.
        /// </summary>
        public SnapshotPayload()
        {
            PayloadBytes = new byte[DeterministicSimConstants.MaxSnapshotBytes];
            BytesWritten = 0;
        }

        /// <summary>Resets the write cursor for the next tick's snapshot serialization.</summary>
        public void Reset()
        {
            BytesWritten = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
