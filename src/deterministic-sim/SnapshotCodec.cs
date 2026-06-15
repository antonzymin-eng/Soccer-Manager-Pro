// File:     src/deterministic-sim/SnapshotCodec.cs
// Created:  2026-05-29
// Modified: 2026-06-15 (AR fix M-1: Encode computes the §3.2.3 chained digest, not payload-only)
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

        // §3.2.3 SnapshotHeader preimage width: 0x12 ‖ schemaVersion(u32) ‖ tick(u64)
        //                                        ‖ prevSnapshotDigest(32) ‖ envFpDigest(32).
        private const int HeaderPreimageBytes =
            1 + 4 + 8 + DeterministicSimConstants.SHA256_BYTES + DeterministicSimConstants.SHA256_BYTES;

        private static readonly byte[] s_payloadDomainTag =
            { DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_PAYLOAD };

        private static readonly byte[] s_zeroDigest = new byte[DeterministicSimConstants.SHA256_BYTES];

        private readonly byte[] _prevDigest;
        private readonly byte[] _headerPreimage; // reused per tick — no per-tick alloc for the header region

        /// <summary>
        /// Constructs a SnapshotCodec.
        /// prevDigest is all-zero at match start; restored from the last committed snapshot on load.
        /// </summary>
        public SnapshotCodec()
        {
            _prevDigest     = new byte[DeterministicSimConstants.SHA256_BYTES];
            _headerPreimage = new byte[HeaderPreimageBytes];
        }

        // ── Encode ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Finalizes the snapshot for durable storage.
        /// Computes the §3.2.3 chained SnapshotDigest:
        ///   SHA-256( 0x12 ‖ schemaVersion ‖ tick ‖ prevSnapshotDigest ‖ envFpDigest ‖ 0x11 ‖ payloadBytes ).
        /// Because PrevSnapshotDigest is part of the preimage, CurrentSnapshotDigest genuinely
        /// chains off its predecessor (a prior snapshot cannot be altered without invalidating
        /// every later digest). §4.6.1 / §3.2.3.
        /// </summary>
        public void Encode(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_encodeMarker.Auto();

            // Thread the previous digest into the header BEFORE hashing — it is part of the preimage.
            Array.Copy(_prevDigest, 0, header.PrevSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);

            // Build the §3.2.3 / corpus D-04 header preimage into the reused buffer.
            int o = 0;
            CanonicalSerializer.WriteU8 (_headerPreimage, ref o, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_HEADER);
            CanonicalSerializer.WriteU32(_headerPreimage, ref o, header.SchemaVersion);
            CanonicalSerializer.WriteU64(_headerPreimage, ref o, header.Tick);
            Array.Copy(header.PrevSnapshotDigest, 0, _headerPreimage, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;
            byte[] envFpDigest = header.Fingerprint != null ? header.Fingerprint.ComputeDigest() : s_zeroDigest;
            Array.Copy(envFpDigest, 0, _headerPreimage, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;

            // SnapshotDigest = SHA-256( headerPreimage ‖ 0x11 ‖ payloadBytes ).
            // TransformBlock feeds the three regions without allocating a combined preimage buffer.
            byte[] digest;
            using (SHA256 sha = SHA256.Create())
            {
                sha.TransformBlock(_headerPreimage, 0, o, null, 0);
                sha.TransformBlock(s_payloadDomainTag, 0, s_payloadDomainTag.Length, null, 0);
                sha.TransformFinalBlock(payload.PayloadBytes, 0, payload.BytesWritten);
                digest = sha.Hash;
            }

            Array.Copy(digest, 0, header.CurrentSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);

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
// | 1.2     | 2026-06-15 | —      | AR fix M-1: Encode now computes the spec §3.2.3 chained          |
// |         |            |        | SnapshotDigest = SHA-256(0x12‖schemaVersion‖tick‖prevDigest‖     |
// |         |            |        | envFpDigest ‖ 0x11‖payload). The prior payload-only digest was   |
// |         |            |        | not chained (an altered earlier snapshot left later digests      |
// |         |            |        | valid) and ignored the domain tags/header the corpus D-07 pins.  |
// |         |            |        | Unused ComputeSha256 helper removed; envFp digest sourced from   |
// |         |            |        | EnvironmentFingerprint.ComputeDigest().                          |
#endregion
