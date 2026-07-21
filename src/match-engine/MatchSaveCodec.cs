// File:     src/match-engine/MatchSaveCodec.cs
// Created:  2026-07-21
// Modified: 2026-07-21
// Author:   —
// Spec:     On-disk match save file (docs/tracking/match-save-file-design.md) §3 layout / KD-1..KD-6;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §3.2.4.1
//           (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the on-disk match save blob: packs the boot matchSeed + the snapshot
//           (header, payload) pair into a single version-gated, self-describing blob and decodes it
//           back, fail-loud on any framing / length-bound / trailing-byte violation. No file I/O
//           (that is MatchSaveManager) — so the codec is exhaustively unit-testable in memory.

using System;
using System.Text;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Encodes / decodes the on-disk match save blob (match-save-file-design.md §3). The blob is one
    /// <see cref="MATCH_SAVE_FORMAT_VERSION"/>-gated frame around the boot seed, the
    /// <see cref="SnapshotHeader"/> (incl. its <see cref="EnvironmentFingerprint"/> and digest chain),
    /// and the <see cref="SnapshotPayload"/> world-state bytes. Serialized through
    /// <see cref="CanonicalSerializer"/> (the same −0.0 / NaN-canonicalising encoder the payload is
    /// written with). Off the 60 Hz hot path (a save is a host action), so allocation is permitted.
    /// </summary>
    public static class MatchSaveCodec
    {
        // Fixed-width, non-length-prefixed digest field width (SHA-256).
        private const int DigestBytes = DeterministicSimConstants.SHA256_BYTES;

        // Optional-block presence flags for the fingerprint (KD-3). A byte rather than the
        // CanonicalSerializer optional tag so the blob stays a plain fixed sequence.
        private const byte FingerprintAbsent  = 0;
        private const byte FingerprintPresent = 1;

        /// <summary>
        /// Encodes a save blob from the boot <paramref name="matchSeed"/> and a captured
        /// (<paramref name="header"/>, <paramref name="payload"/>) pair. The buffer is sized exactly to
        /// the content. See <see cref="Decode"/> for the inverse (both kept adjacent so a layout change
        /// is edited in one place — R1).
        /// </summary>
        public static byte[] Encode(ulong matchSeed, SnapshotHeader header, SnapshotPayload payload)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (header.PrevSnapshotDigest == null || header.PrevSnapshotDigest.Length != DigestBytes ||
                header.CurrentSnapshotDigest == null || header.CurrentSnapshotDigest.Length != DigestBytes)
            {
                throw new ArgumentException(
                    "SnapshotHeader digest arrays must each be exactly " + DigestBytes + " bytes.", nameof(header));
            }
            if (payload.BytesWritten < 0 || payload.BytesWritten > payload.Capacity)
            {
                throw new ArgumentException(
                    "SnapshotPayload.BytesWritten (" + payload.BytesWritten + ") is out of range [0, " +
                    payload.Capacity + "].", nameof(payload));
            }

            EnvironmentFingerprint fp = header.Fingerprint;
            byte[] buf = new byte[ComputeSize(fp, payload.BytesWritten)];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU64(buf, ref o, matchSeed);

            // ── SnapshotHeader block ──────────────────────────────────────────────────
            CanonicalSerializer.WriteU32(buf, ref o, header.SchemaVersion);
            CanonicalSerializer.WriteU16(buf, ref o, header.DigestVersion);
            CanonicalSerializer.WriteU64(buf, ref o, header.Tick);
            Array.Copy(header.PrevSnapshotDigest,    0, buf, o, DigestBytes); o += DigestBytes;
            Array.Copy(header.CurrentSnapshotDigest, 0, buf, o, DigestBytes); o += DigestBytes;
            CanonicalSerializer.WriteU64(buf, ref o, header.Cursor.Tick);
            CanonicalSerializer.WriteU8(buf,  ref o, header.Cursor.PhaseOrdinal);

            if (fp != null)
            {
                CanonicalSerializer.WriteU8(buf, ref o, FingerprintPresent);
                CanonicalSerializer.WriteI32(buf,    ref o, fp.WorkerCount);
                CanonicalSerializer.WriteString(buf, ref o, fp.SchedulerPolicy);
                CanonicalSerializer.WriteString(buf, ref o, fp.ReductionTopology);
                CanonicalSerializer.WriteString(buf, ref o, fp.SimdFeatureLevel);
                CanonicalSerializer.WriteString(buf, ref o, fp.FloatModelHash);
                CanonicalSerializer.WriteString(buf, ref o, fp.UnicodeNormalizationVersion);
            }
            else
            {
                CanonicalSerializer.WriteU8(buf, ref o, FingerprintAbsent);
            }

            // ── SnapshotPayload block ─────────────────────────────────────────────────
            CanonicalSerializer.WriteU32(buf, ref o, (uint)payload.BytesWritten);
            Array.Copy(payload.PayloadBytes, 0, buf, o, payload.BytesWritten); o += payload.BytesWritten;

            if (o != buf.Length)
            {
                // Guards ComputeSize against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "MatchSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " + buf.Length +
                    " — ComputeSize is out of sync with Encode.");
            }
            return buf;
        }

        /// <summary>
        /// Decodes a save blob produced by <see cref="Encode"/>. Fail-loud (throws) on: a
        /// <see cref="MATCH_SAVE_FORMAT_VERSION"/> mismatch (KD-1, no Stage-0 migration); a length
        /// prefix (fingerprint string or payload) that would read past the blob; a payload length
        /// exceeding <see cref="SnapshotPayload.Capacity"/>; or any trailing bytes after the declared
        /// content (KD-6 / R1 truncation guard). The inner snapshot schema versions ride inside the
        /// header/payload and are re-checked by <see cref="MatchEngine.RestoreFromSnapshot"/> itself.
        /// </summary>
        public static MatchSaveContents Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            int len = blob.Length;
            int o = 0;

            Require(o, 4, len, "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Match save file format version " + format + " != expected " +
                    MatchEngineConstants.MATCH_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (KD-1).");
            }

            Require(o, 8, len, "match seed");
            ulong matchSeed = CanonicalSerializer.ReadU64(blob, ref o);

            // ── SnapshotHeader block ──────────────────────────────────────────────────
            var header = new SnapshotHeader();

            Require(o, 4 + 2 + 8 + DigestBytes + DigestBytes + 8 + 1 + 1, len, "snapshot header");
            header.SchemaVersion = CanonicalSerializer.ReadU32(blob, ref o);
            header.DigestVersion = CanonicalSerializer.ReadU16(blob, ref o);
            header.Tick          = CanonicalSerializer.ReadU64(blob, ref o);
            Array.Copy(blob, o, header.PrevSnapshotDigest,    0, DigestBytes); o += DigestBytes;
            Array.Copy(blob, o, header.CurrentSnapshotDigest, 0, DigestBytes); o += DigestBytes;
            ulong cursorTick = CanonicalSerializer.ReadU64(blob, ref o);
            byte cursorPhase = CanonicalSerializer.ReadU8(blob, ref o);
            header.Cursor = new ReplayCursor(cursorTick, cursorPhase);

            byte fpFlag = CanonicalSerializer.ReadU8(blob, ref o);
            if (fpFlag == FingerprintPresent)
            {
                Require(o, 4, len, "fingerprint worker count");
                int    workerCount = CanonicalSerializer.ReadI32(blob, ref o);
                string scheduler   = ReadBoundedString(blob, ref o, len, "fingerprint schedulerPolicy");
                string reduction   = ReadBoundedString(blob, ref o, len, "fingerprint reductionTopology");
                string simd        = ReadBoundedString(blob, ref o, len, "fingerprint simdFeatureLevel");
                string floatModel  = ReadBoundedString(blob, ref o, len, "fingerprint floatModelHash");
                string unicode     = ReadBoundedString(blob, ref o, len, "fingerprint unicodeNormalizationVersion");
                var fp = new EnvironmentFingerprint(
                    workerCount, scheduler, reduction, simd, floatModel, unicode);
                fp.Lock(); // §4.8.1 lifecycle — a reconstructed fingerprint is already sealed
                header.Fingerprint = fp;
            }
            else if (fpFlag == FingerprintAbsent)
            {
                header.Fingerprint = null;
            }
            else
            {
                throw new InvalidOperationException(
                    "Match save file fingerprint-present flag " + fpFlag + " is neither 0 nor 1 — corrupt save.");
            }

            // ── SnapshotPayload block ─────────────────────────────────────────────────
            Require(o, 4, len, "payload length");
            uint payloadLen = CanonicalSerializer.ReadU32(blob, ref o);
            var payload = new SnapshotPayload();
            if (payloadLen > (uint)payload.Capacity)
            {
                throw new InvalidOperationException(
                    "Match save payload length " + payloadLen + " exceeds SnapshotPayload capacity " +
                    payload.Capacity + " — corrupt or incompatible save.");
            }
            Require(o, (int)payloadLen, len, "payload body");
            Array.Copy(blob, o, payload.PayloadBytes, 0, (int)payloadLen); o += (int)payloadLen;
            payload.BytesWritten = (int)payloadLen;

            // ── Trailing-byte guard (KD-6 / R1) ───────────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Match save file has " + (len - o) + " trailing byte(s) after the declared content " +
                    "— truncated, padded, or corrupt (R1).");
            }

            return new MatchSaveContents(matchSeed, header, payload);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static int ComputeSize(EnvironmentFingerprint fp, int payloadBytes)
        {
            int size = 4                       // MATCH_SAVE_FORMAT_VERSION
                     + 8                       // matchSeed
                     + 4 + 2 + 8               // SchemaVersion + DigestVersion + Tick
                     + DigestBytes + DigestBytes
                     + 8 + 1                   // Cursor.Tick + Cursor.PhaseOrdinal
                     + 1;                      // fingerprintPresent flag
            if (fp != null)
            {
                size += 4                      // WorkerCount
                      + StringSize(fp.SchedulerPolicy)
                      + StringSize(fp.ReductionTopology)
                      + StringSize(fp.SimdFeatureLevel)
                      + StringSize(fp.FloatModelHash)
                      + StringSize(fp.UnicodeNormalizationVersion);
            }
            size += 4 + payloadBytes;          // payloadLength + body
            return size;
        }

        // Mirrors CanonicalSerializer.WriteString's on-wire size (u32 length + ASCII bytes).
        private static int StringSize(string s) =>
            4 + (s == null ? 0 : Encoding.ASCII.GetByteCount(s));

        private static void Require(int offset, int need, int total, string what)
        {
            // Overflow-safe: compare against (total - offset) rather than (offset + need), since a
            // corrupt length prefix can push `need` near int.MaxValue and `offset + need` would wrap
            // negative and slip past the guard. `offset` is always in [0, total] here (every read is
            // guarded), so `total - offset` is a safe non-negative int.
            if (need < 0 || offset > total || need > total - offset)
            {
                throw new InvalidOperationException(
                    "Match save file truncated reading " + what + " (need " + need + " byte(s) at offset " +
                    offset + " of " + total + ").");
            }
        }

        // Bounded ReadString: reads the u32 length, refuses a length that would read past the blob,
        // then decodes the ASCII bytes. Fail-loud (KD-6) rather than CanonicalSerializer.ReadString's
        // unchecked read, which would throw IndexOutOfRange or read garbage on a corrupt length.
        private static string ReadBoundedString(byte[] blob, ref int o, int total, string what)
        {
            Require(o, 4, total, what + " length");
            uint slen = CanonicalSerializer.ReadU32(blob, ref o);
            if (slen == 0u)
            {
                return string.Empty;
            }
            Require(o, (int)slen, total, what + " body");
            string s = Encoding.ASCII.GetString(blob, o, (int)slen);
            o += (int)slen;
            return s;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-21 | —      | Initial implementation. |
#endregion
