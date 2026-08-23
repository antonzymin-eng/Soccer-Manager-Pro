// File:     src/deterministic-sim/SnapshotHeader.cs
// Created:  2026-05-29
// Modified: 2026-08-22 (ERR-016-009: BuildHash — the #16 §2.3.2 build identity — and its required
//           Initialize parameter)
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3, §2.3.2, §3.9.2, §3.4, Code Standards #20
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

        /// <summary>
        /// The §2.3.2 <c>buildHash</c> — which compiled binaries produced this snapshot (64 lowercase
        /// hex chars from <see cref="BuildIdentity.ComputeHash(System.Collections.Generic.IReadOnlyList{BuildModule})"/>).
        /// Sits BESIDE <see cref="Fingerprint"/>, which pins the host and float model, not the binary.
        /// <para>
        /// <b>Deliberately outside every digest preimage</b> (§2.3.2 rule 2): it is an identity
        /// compared by equality at restore, not state a digest protects — and a per-build value inside
        /// the §3.2.3 header preimage would make every golden vector unreproducible by construction.
        /// </para>
        /// <para>
        /// Null only on a header reconstructed from a format that does not carry it — the Stage-0
        /// <see cref="SaveManager"/> layout (§3.9.2), which likewise drops <see cref="Fingerprint"/>.
        /// <see cref="Initialize"/> refuses to leave it empty, and a format that DOES carry it must
        /// refuse an empty value at both ends rather than admit a save of unknown build identity.
        /// </para>
        /// </summary>
        public string BuildHash;

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
        /// <paramref name="buildHash"/> is REQUIRED (#16 FR-DS-014): a header written by a running
        /// engine always has a build to name, so an empty one is refused here rather than allowed to
        /// reach a save file and disable the restore gate silently. The one legitimately
        /// build-hash-less header is reconstructed from a format that never carried it, and that path
        /// assigns the field directly rather than calling Initialize.
        /// </summary>
        public void Initialize(
            ulong tick,
            byte[] prevDigest,
            EnvironmentFingerprint fingerprint,
            string buildHash)
        {
            if (string.IsNullOrEmpty(buildHash))
            {
                throw new System.ArgumentException(
                    "A snapshot header written by a running engine must carry the §2.3.2 buildHash " +
                    "(FR-DS-014) — see MatchEngineBuildIdentity.BuildHash.", nameof(buildHash));
            }

            SchemaVersion = DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION;
            DigestVersion = DeterministicSimConstants.DETERMINISM_DIGEST_VERSION;
            Tick          = tick;
            Fingerprint   = fingerprint;
            BuildHash     = buildHash;
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
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                        |
// | 1.1     | 2026-08-22 | —      | ERR-016-009: new BuildHash field (the #16 §2.3.2 build         |
// |         |            |        | identity, beside the fingerprint rather than inside it, and    |
// |         |            |        | deliberately outside every digest preimage) + Initialize takes |
// |         |            |        | it as a REQUIRED parameter, refusing an empty value.           |
#endregion
