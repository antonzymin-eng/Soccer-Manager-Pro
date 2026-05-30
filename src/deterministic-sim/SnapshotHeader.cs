// File:     src/deterministic-sim/SnapshotHeader.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3, §3.9.2, §3.4, Code Standards #20
// Purpose:  Snapshot header struct. Contains schema version, digest version, tick, environment
//           fingerprint reference, and the prev-snapshot digest chain link.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Snapshot header metadata.
    /// Written by the Snapshot phase; validated by steps 2–4 of the replay lifecycle.
    /// Deterministic Simulation #16 §2.3 / §3.9.2.
    /// </summary>
    public sealed class SnapshotHeader
    {
        /// <summary>Snapshot binary format schema version. §3.9.2 / §3.4.</summary>
        public uint SchemaVersion;

        /// <summary>Determinism digest format version. §3.4.</summary>
        public ushort DigestVersion;

        /// <summary>Physics tick at which this snapshot was committed (60 Hz, 0-based). §3.9.2.</summary>
        public ulong Tick;

        /// <summary>SHA-256 digest of the previous snapshot in the chain (32 bytes). §3.9.2 / §3.4.
        /// All-zero for the first snapshot of the match.</summary>
        public byte[] PrevSnapshotDigest;

        /// <summary>SHA-256 digest of this snapshot's payload bytes (32 bytes). §3.9.2.</summary>
        public byte[] CurrentSnapshotDigest;

        /// <summary>Environment fingerprint embedded at match start. §4.8 / §4.8.1.</summary>
        public EnvironmentFingerprint Fingerprint;

        /// <summary>Replay cursor at the EndOfSnapshot[Tick] boundary. §4.2.2 step 7.</summary>
        public ReplayCursor Cursor;

        /// <summary>Constructs a blank header (default values). Call Initialize() before serialization.</summary>
        public SnapshotHeader()
        {
            PrevSnapshotDigest    = new byte[DeterministicSimConstants.SHA256_BYTES];
            CurrentSnapshotDigest = new byte[DeterministicSimConstants.SHA256_BYTES];
        }

        /// <summary>
        /// Initialises all header fields for a new snapshot at the given tick.
        /// </summary>
        public void Initialize(
            ulong tick,
            byte[] prevDigest,
            EnvironmentFingerprint fingerprint)
        {
            SchemaVersion = DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION;
            DigestVersion = DeterministicSimConstants.DETERMINISM_DIGEST_VERSION;
            Tick          = tick;
            Fingerprint   = fingerprint;
            Cursor        = ReplayCursor.EndOfSnapshot(tick);

            if (prevDigest != null)
            {
                System.Array.Copy(prevDigest, 0, PrevSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);
            }
            else
            {
                System.Array.Clear(PrevSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
