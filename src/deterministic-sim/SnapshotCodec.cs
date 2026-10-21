// File:     src/deterministic-sim/SnapshotCodec.cs
// Created:  2026-05-29
// Modified: 2026-10-21 (Gate-J J-05 / P1b: the digest hashes one reused contiguous preimage buffer —
//           CNG on Windows, a reused managed SHA-256 elsewhere — instead of creating a SHA256 and
//           copying the whole payload through TransformFinalBlock every tick)
// Modified (prior): 2026-08-22 (ERR-016-011: the §3.2.3 preimage gets a single owner, ComputeSnapshotDigest,
//           and the new ValidateCurrentDigest re-derives a loaded record's own digest — replay step 4b)
// Author:   —
// Spec:     Deterministic Simulation #16 §3.9.2, §3.2.3, §3.2.4.1, §3.4, §4.2.2, §4.6.1, Code Standards #20
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

        // Offset of the payload bytes inside _preimage: the header region, then the 0x11 domain tag.
        private const int PayloadPreimageOffset = HeaderPreimageBytes + 1;

        private static readonly byte[] s_zeroDigest = new byte[DeterministicSimConstants.SHA256_BYTES];

        private readonly byte[] _prevDigest;

        // The whole §3.2.3 preimage, contiguous and reused every tick: header ‖ 0x11 ‖ payload. One
        // contiguous range is what lets a single one-shot hash call (CngSha256) cover it, and copying
        // the payload in is far cheaper than hashing it. Sized for MaxSnapshotBytes, which every
        // SnapshotPayload is allocated with.
        private byte[] _preimage;
        private readonly byte[] _digest;

        // Managed SHA-256 for hosts without CNG (the Linux gate). Created on first use and reused.
        private SHA256 _managedSha;

        /// <summary>
        /// Constructs a SnapshotCodec.
        /// prevDigest is all-zero at match start; restored from the last committed snapshot on load.
        /// </summary>
        public SnapshotCodec()
        {
            _prevDigest = new byte[DeterministicSimConstants.SHA256_BYTES];
            _preimage   = new byte[PayloadPreimageOffset + DeterministicSimConstants.MaxSnapshotBytes];
            _digest     = new byte[DeterministicSimConstants.SHA256_BYTES];
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

            ComputeSnapshotDigest(header, payload, _digest);

            Array.Copy(_digest, 0, header.CurrentSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);

            // Advance chain
            Array.Copy(_digest, 0, _prevDigest, 0, DeterministicSimConstants.SHA256_BYTES);
        }

        /// <summary>
        /// The single owner of the §3.2.3 / corpus D-04 snapshot-digest preimage:
        /// <c>SHA-256( 0x12 ‖ schemaVersion ‖ tick ‖ prevSnapshotDigest ‖ envFpDigest ‖ 0x11 ‖ payloadBytes )</c>.
        /// <para>
        /// Extracted at ERR-016-011 so <see cref="Encode"/> and
        /// <see cref="ValidateCurrentDigest"/> cannot drift: two hand-written derivations of one
        /// preimage that agree only by inspection is the ERR-010-002 defect class, and a verifier
        /// computing a *different* preimage from the recorder would reject every honest record.
        /// </para>
        /// <para>
        /// Reads only from <paramref name="header"/> / <paramref name="payload"/>, and writes only the
        /// reused preimage buffer and <paramref name="digest"/> — it does NOT touch the chain state,
        /// so a validator may call it freely.
        /// </para>
        /// </summary>
        private void ComputeSnapshotDigest(SnapshotHeader header, SnapshotPayload payload, byte[] digest)
        {
            int payloadBytes = payload.BytesWritten;
            if (_preimage.Length < PayloadPreimageOffset + payloadBytes)
            {
                // Unreachable for a SnapshotPayload (its buffer is MaxSnapshotBytes); kept so an
                // oversized payload is hashed whole rather than truncated.
                _preimage = new byte[PayloadPreimageOffset + payloadBytes];
            }

            byte[] pre = _preimage;
            int o = 0;
            CanonicalSerializer.WriteU8 (pre, ref o, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_HEADER);
            CanonicalSerializer.WriteU32(pre, ref o, header.SchemaVersion);
            CanonicalSerializer.WriteU64(pre, ref o, header.Tick);
            Array.Copy(header.PrevSnapshotDigest, 0, pre, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;
            byte[] envFpDigest = header.Fingerprint != null ? header.Fingerprint.ComputeDigest() : s_zeroDigest;
            Array.Copy(envFpDigest, 0, pre, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;
            CanonicalSerializer.WriteU8 (pre, ref o, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_PAYLOAD);
            Buffer.BlockCopy(payload.PayloadBytes, 0, pre, o, payloadBytes);
            o += payloadBytes;

            if (CngSha256.IsAvailable && CngSha256.TryHash(pre, o, digest))
            {
                return;
            }

            if (_managedSha == null)
            {
                _managedSha = SHA256.Create();
            }
            if (!_managedSha.TryComputeHash(new ReadOnlySpan<byte>(pre, 0, o), digest, out int written) ||
                written != DeterministicSimConstants.SHA256_BYTES)
            {
                throw new InvalidOperationException("SHA-256 did not produce a 32-byte snapshot digest.");
            }
        }

        /// <summary>
        /// Replay step 4b (§4.2.2): re-derives this record's OWN digest from the header and payload
        /// just loaded and compares it against the stored <see cref="SnapshotHeader.CurrentSnapshotDigest"/>.
        /// Returns <c>ERR_DS_SNAPSHOT_DIGEST_MISMATCH</c> on any difference; 0 on match.
        /// <para>
        /// Distinct from <see cref="ValidatePrevDigest"/>, which checks the chain LINK to the
        /// predecessor. Step 4a answers "is this the record that should follow the last one?"; this
        /// answers "are these bytes the record they claim to be?". Before ERR-016-011 nothing asked
        /// the second question, so an altered payload — or an altered stored digest — loaded clean.
        /// </para>
        /// <para>
        /// Pure with respect to the digest chain: it does not advance <c>_prevDigest</c>, so calling it
        /// mid-replay cannot disturb step 4a for the next record. Replay-side only — not on the tick
        /// path.
        /// </para>
        /// </summary>
        public ushort ValidateCurrentDigest(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_decodeMarker.Auto();

            if (header == null || payload == null ||
                header.CurrentSnapshotDigest == null ||
                header.CurrentSnapshotDigest.Length != DeterministicSimConstants.SHA256_BYTES)
            {
                return DeterministicSimConstants.ERR_DS_SNAPSHOT_DIGEST_MISMATCH;
            }

            ComputeSnapshotDigest(header, payload, _digest);

            for (int i = 0; i < DeterministicSimConstants.SHA256_BYTES; i++)
            {
                if (_digest[i] != header.CurrentSnapshotDigest[i])
                {
                    return DeterministicSimConstants.ERR_DS_SNAPSHOT_DIGEST_MISMATCH;
                }
            }

            return 0;
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
// | 1.3     | 2026-08-22 | —      | ERR-016-011: the §3.2.3 preimage gets a SINGLE owner,            |
// |         |            |        | ComputeSnapshotDigest, called by both Encode and the new public  |
// |         |            |        | ValidateCurrentDigest (replay step 4b). Two hand-written         |
// |         |            |        | derivations of one preimage is the ERR-010-002 class, and here   |
// |         |            |        | it fails in BOTH directions: a drifting verifier rejects every   |
// |         |            |        | honest record, an incomplete one accepts tampering. The          |
// |         |            |        | validator is pure with respect to the chain — it does not       |
// |         |            |        | advance _prevDigest — so it may run between steps 4a and 5.     |
// | 1.4     | 2026-10-21 | —      | Gate-J J-05 / perception-shared-pass-design.md P1b. The digest   |
// |         |            |        | no longer calls SHA256.Create() per tick or passes the payload   |
// |         |            |        | through TransformFinalBlock, which copied all ~35 KB of it — the |
// |         |            |        | whole of RunTick's per-tick allocation. ComputeSnapshotDigest    |
// |         |            |        | writes header ‖ 0x11 ‖ payload into one reused buffer and hashes |
// |         |            |        | it with CngSha256 on Windows (Unity Mono's SHA256Managed was the |
// |         |            |        | largest single cost of a tick) or a reused managed SHA-256       |
// |         |            |        | elsewhere, into a reused digest buffer. Same preimage bytes,     |
// |         |            |        | same digest; still the single preimage owner (ERR-016-011).     |
#endregion
