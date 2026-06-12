// File:     src/deterministic-sim/SnapshotCodec.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.9.2, §3.2.4.1, §3.4, §4.6.1, Code Standards #20
// Purpose:  Encodes and decodes snapshots in canonical binary format. Computes SHA-256 digest over
//           SnapshotPayload bytes. Validates schema/digest version on decode. Manages the digest chain.

using System;
using System.Security.Cryptography;
using Unity.Profiling;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Encodes SnapshotPayload bytes into a durable snapshot record and decodes them back.
    /// Computes SHA-256 over the serialized payload to build the digest chain.
    /// Validates schemaVersion and digestVersion on decode (replay step 2).
    /// Deterministic Simulation #16 §3.9.2 / §4.6.1.
    /// </summary>
    public sealed class SnapshotCodec
    {
        // ── Profiler markers ──────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_encodeMarker  = new ProfilerMarker("DeterministicSim.Encode");
        private static readonly ProfilerMarker s_decodeMarker  = new ProfilerMarker("DeterministicSim.Decode");

        // ── Digest chain state ────────────────────────────────────────────────────────

        private readonly byte[] _prevDigest;

        /// <summary>
        /// Constructs a SnapshotCodec.
        /// prevDigest is all-zero at match start; restored from the last committed snapshot on load.
        /// </summary>
        public SnapshotCodec()
        {
            _prevDigest = new byte[DeterministicSimConstants.SHA256_BYTES];
        }

        // ── Encode ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Finalizes the snapshot for durable storage.
        /// Computes SHA-256(payloadBytes[0..bytesWritten]) → CurrentSnapshotDigest.
        /// Sets PrevSnapshotDigest from the prior committed snapshot.
        /// §4.6.1.
        /// </summary>
        public void Encode(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_encodeMarker.Auto();

            // Compute payload digest
            byte[] digest = ComputeSha256(payload.PayloadBytes, payload.BytesWritten);
            Array.Copy(digest, 0, header.CurrentSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);

            // Thread in the previous digest for chain continuity
            Array.Copy(_prevDigest, 0, header.PrevSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);

            // Advance chain
            Array.Copy(digest, 0, _prevDigest, 0, DeterministicSimConstants.SHA256_BYTES);
        }

        // ── Decode (replay steps 1–2) ──────────────────────────────────────────────────

        /// <summary>
        /// Validates schema and digest version from the snapshot header bytes.
        /// Returns ERR_DS_SCHEMA_INCOMPATIBLE on mismatch; 0 on success.
        /// Corresponds to replay lifecycle steps 1–2 (§4.2.2).
        /// </summary>
        public ushort ValidateHeader(SnapshotHeader header)
        {
            using var _ = s_decodeMarker.Auto();

            if (header.SchemaVersion != DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION)
            {
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }

            if (header.DigestVersion != DeterministicSimConstants.DETERMINISM_DIGEST_VERSION)
            {
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }

            return 0;
        }

        /// <summary>
        /// Validates the prev-snapshot digest chain link.
        /// Returns ERR_DS_DIGEST_CHAIN_BREAK if the header's PrevSnapshotDigest does not match
        /// the stored expected predecessor digest. Corresponds to replay step 4 (§4.2.2).
        /// </summary>
        public ushort ValidatePrevDigest(SnapshotHeader header)
        {
            byte[] expected = _prevDigest;
            byte[] recorded = header.PrevSnapshotDigest;

            for (int i = 0; i < DeterministicSimConstants.SHA256_BYTES; i++)
            {
                if (expected[i] != recorded[i])
                {
                    return DeterministicSimConstants.ERR_DS_DIGEST_CHAIN_BREAK;
                }
            }

            return 0;
        }

        /// <summary>
        /// Advances the chain state after a successful snapshot load (replay context).
        /// Sets the stored prev-digest to CurrentSnapshotDigest so the next snapshot in the chain
        /// can be validated.
        /// </summary>
        public void CommitLoadedDigest(SnapshotHeader header)
        {
            Array.Copy(header.CurrentSnapshotDigest, 0, _prevDigest, 0, DeterministicSimConstants.SHA256_BYTES);
        }

        // ── SHA-256 helper ────────────────────────────────────────────────────────────

        /// <summary>Computes SHA-256 over buf[0..length). §3.9.2.</summary>
        private static byte[] ComputeSha256(byte[] buf, int length)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(buf, 0, length);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-12 | —      | Build fix (dotnet CI    |
// |         |            |        | gate): using            |
// |         |            |        | UnityEngine.Profiling   |
// |         |            |        | -> Unity.Profiling.     |
// |         |            |        | ProfilerMarker's actual |
// |         |            |        | namespace is            |
// |         |            |        | Unity.Profiling; the    |
// |         |            |        | old using was CS0246    |
// |         |            |        | under Unity and the     |
// |         |            |        | Linux compile gate      |
// |         |            |        | alike, so this assembly |
// |         |            |        | could not have compiled |
// |         |            |        | in-engine. No           |
// |         |            |        | functional change.      |
#endregion
