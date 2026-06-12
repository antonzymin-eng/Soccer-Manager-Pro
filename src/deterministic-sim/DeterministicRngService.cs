// File:     src/deterministic-sim/DeterministicRngService.cs
// Created:  2026-05-29
// Modified: 2026-06-12 (golden-vector pass: full RFC 5869 multi-block Expand; HkdfExtract/HkdfExpand split)
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.1, §3.2.5, §3.2.5.1, §3.4, Code Standards #20
// Purpose:  Deterministic RNG service using HKDF-SHA256 key derivation and SipHash-2-4-64 stream hashing.
//           Implements the branch-safe reservation API (Reserve/DrawReserved/Skip) per §3.2.5.
//           SplitMix64 is used for the match-seed PRNG at construction; HKDF+SipHash is the per-stream crypto.

using System;
using System.Security.Cryptography;
using System.Text;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Deterministic RNG service: HKDF-SHA256 key derivation + SipHash-2-4-64 per-draw hashing.
    /// Branch-safe reservation API (Reserve/DrawReserved/Skip) ensures RNG cursor parity
    /// regardless of which branch is taken in conditional draw paths. §3.2.1 / §3.2.5.
    /// Constructor-injected (FR-CS-051–054). Zero heap allocation on hot path after construction.
    /// </summary>
    public sealed class DeterministicRngService
    {
        // ── SipHash-2-4 key material derived from matchSeed via HKDF-SHA256 ────────────

        private readonly ulong _k0;
        private readonly ulong _k1;

        // ── Stream registry (pre-allocated) ───────────────────────────────────────────

        private readonly RngStreamState[] _streams;
        private int _streamCount;

        // ── Constructor ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Derives the SipHash-2-4 key pair from the given match seed via HKDF-SHA256 per §3.2.1.
        /// Allocates the stream registry buffer. No further heap allocation on hot path.
        /// </summary>
        public DeterministicRngService(ulong matchSeed)
        {
            byte[] key = DeriveHkdfKey(matchSeed);
            _k0 = ReadUInt64LE(key, 0);
            _k1 = ReadUInt64LE(key, 8);

            _streams     = new RngStreamState[DeterministicSimConstants.MaxRngStreams];
            _streamCount = 0;
        }

        // ── Stream registration ───────────────────────────────────────────────────────

        /// <summary>
        /// Registers a draw-site stream and returns its stream index.
        /// Must be called before Reserve()/DrawReserved() for this site.
        /// §3.2.5.1: siteId must be stable; streamVersion must be bumped on reordering.
        /// </summary>
        public int RegisterStream(string siteId, int subsystemOrdinal, int entityId, ushort streamVersion)
        {
            if (_streamCount >= _streams.Length)
            {
                throw new InvalidOperationException("RNG stream registry full; increase MaxRngStreams.");
            }

            int idx = _streamCount++;
            _streams[idx].SiteId           = siteId;
            _streams[idx].SubsystemOrdinal = subsystemOrdinal;
            _streams[idx].EntityId         = entityId;
            _streams[idx].StreamVersion    = streamVersion;
            _streams[idx].StreamKey        = ComputeStreamKey(subsystemOrdinal, entityId, streamVersion);
            _streams[idx].RngCursor        = 0UL;
            _streams[idx].ActionOrdinal    = 0UL;
            _streams[idx].ClearReservation();
            return idx;
        }

        // ── Branch-safe reservation API ───────────────────────────────────────────────

        /// <summary>
        /// Opens a reservation window of exactly <paramref name="count"/> draws for the given stream.
        /// The cursor will advance by exactly <paramref name="count"/> when the window is closed,
        /// regardless of how many DrawReserved calls are actually made.
        /// Fails with ERR_DS_RNG_BUDGET_MISMATCH if a reservation is already open on this stream.
        /// §3.2.5.
        /// </summary>
        public ushort Reserve(int streamIndex, int count)
        {
            ref RngStreamState s = ref _streams[streamIndex];

            if (s.BudgetRemaining != 0)
            {
                return DeterministicSimConstants.ERR_DS_RNG_BUDGET_MISMATCH;
            }

            s.DeclaredBudget  = count;
            s.BudgetRemaining = count;
            s.DrawIndex       = 0;
            s.ActionOrdinal++;
            return 0;
        }

        /// <summary>
        /// Draws the value at <paramref name="index"/> within the current reservation window [0, count).
        /// Returns 0 and the draw value; returns ERR_DS_RNG_BUDGET_MISMATCH if index is out of range.
        /// §3.2.5.
        /// </summary>
        public ushort DrawReserved(int streamIndex, int index, out ulong value)
        {
            ref RngStreamState s = ref _streams[streamIndex];

            if ((uint)index >= (uint)s.DeclaredBudget)
            {
                value = 0UL;
                return DeterministicSimConstants.ERR_DS_RNG_BUDGET_MISMATCH;
            }

            value = ComputeDrawValue(s.StreamKey, s.ActionOrdinal, (uint)index);
            return 0;
        }

        /// <summary>
        /// Closes the reservation window and advances RngCursor by exactly the declared budget,
        /// whether or not all draws were consumed. §3.2.5.
        /// </summary>
        public void CloseReservation(int streamIndex)
        {
            ref RngStreamState s = ref _streams[streamIndex];
            s.RngCursor += (ulong)s.DeclaredBudget;
            s.ClearReservation();
        }

        /// <summary>
        /// Skips <paramref name="count"/> draws without producing values.
        /// Advances the cursor by count to maintain parity with branches that do draw. §3.2.5.
        /// </summary>
        public void Skip(int streamIndex, int count)
        {
            _streams[streamIndex].RngCursor += (ulong)count;
        }

        /// <summary>Returns a read-only reference to the stream state for snapshot serialization.</summary>
        public ref readonly RngStreamState GetStreamState(int streamIndex) => ref _streams[streamIndex];

        /// <summary>Number of registered streams.</summary>
        public int StreamCount => _streamCount;

        /// <summary>
        /// Restores stream state from snapshot during replay (§4.2.2 step 6).
        /// </summary>
        public ushort RestoreStream(int streamIndex, in RngStreamState restored)
        {
            if ((uint)streamIndex >= (uint)_streamCount)
            {
                return DeterministicSimConstants.ERR_DS_RNG_STREAM_MISSING;
            }

            _streams[streamIndex] = restored;
            return 0;
        }

        // ── HKDF-SHA256 key derivation (§3.2.1) ──────────────────────────────────────

        /// <summary>
        /// Derives 16 bytes of key material from the match seed via HKDF-SHA256.
        /// IKM = little-endian match seed bytes; salt = 32 zero bytes; info = "DS-RNG-KEY-v1".
        /// §3.2.1.
        /// </summary>
        private static byte[] DeriveHkdfKey(ulong matchSeed)
        {
            byte[] ikm  = new byte[8];
            WriteUInt64LE(ikm, 0, matchSeed);

            byte[] salt = new byte[DeterministicSimConstants.RNG_KDF_SALT_BYTES];
            byte[] info = Encoding.ASCII.GetBytes(DeterministicSimConstants.RNG_KDF_INFO);

            return HkdfSha256(ikm, salt, info, DeterministicSimConstants.RNG_KDF_OUTPUT_BYTES);
        }

        /// <summary>
        /// HKDF-SHA256 per RFC 5869: Extract then full multi-block Expand.
        /// KAT-fix: the pre-1.2 form emitted T(1) only (L ≤ 32), so it could not reproduce the
        /// RFC 5869 §A.1/§A.2 reference outputs (L = 42 / 82) and was unverifiable against the
        /// golden vectors. Construction-time only — heap allocation permitted (not hot path).
        /// §3.2.1 / golden vectors (hkdf-sha256-kat.md).
        /// </summary>
        internal static byte[] HkdfSha256(byte[] ikm, byte[] salt, byte[] info, int outputBytes)
        {
            byte[] prk = HkdfExtract(ikm, salt);
            return HkdfExpand(prk, info, outputBytes);
        }

        /// <summary>
        /// HKDF-Extract per RFC 5869 §2.2: PRK = HMAC-SHA256(salt, IKM).
        /// Exposed separately so the KAT suite can assert the intermediate PRK byte-exact
        /// (hkdf-sha256-kat.md lists PRK for every test case).
        /// </summary>
        internal static byte[] HkdfExtract(byte[] ikm, byte[] salt)
        {
            using (HMACSHA256 hmac = new HMACSHA256(salt))
            {
                return hmac.ComputeHash(ikm);
            }
        }

        /// <summary>
        /// HKDF-Expand per RFC 5869 §2.3:
        /// T(n) = HMAC-SHA256(PRK, T(n-1) ‖ info ‖ byte(n)), OKM = first L bytes of T(1) ‖ T(2) ‖ …
        /// Supports the full RFC range L ≤ 255 × 32.
        /// </summary>
        internal static byte[] HkdfExpand(byte[] prk, byte[] info, int outputBytes)
        {
            const int hashLen = DeterministicSimConstants.SHA256_BYTES;
            if (outputBytes <= 0 || outputBytes > 255 * hashLen)
            {
                throw new ArgumentOutOfRangeException(nameof(outputBytes),
                    outputBytes, "RFC 5869 §2.3 bounds: 0 < L <= 255 * HashLen.");
            }

            byte[] output = new byte[outputBytes];
            byte[] t      = new byte[0]; // T(0) = empty string
            int written   = 0;
            byte counter  = 1;

            using (HMACSHA256 hmac = new HMACSHA256(prk))
            {
                while (written < outputBytes)
                {
                    byte[] block = new byte[t.Length + info.Length + 1];
                    Array.Copy(t, 0, block, 0, t.Length);
                    Array.Copy(info, 0, block, t.Length, info.Length);
                    block[t.Length + info.Length] = counter;

                    t = hmac.ComputeHash(block);

                    int copy = Math.Min(hashLen, outputBytes - written);
                    Array.Copy(t, 0, output, written, copy);
                    written += copy;
                    counter++;
                }
            }

            return output;
        }

        // ── SipHash-2-4-64 (§3.2.1) ──────────────────────────────────────────────────

        /// <summary>
        /// Derives the stream key for (subsystemId, entityId, streamVersion).
        /// StreamKey = SipHash-2-4-64((k0,k1), subsystemId ‖ entityId ‖ streamVersion). §3.2.5.
        /// </summary>
        private ulong ComputeStreamKey(int subsystemOrdinal, int entityId, ushort streamVersion)
        {
            byte[] msg = new byte[10]; // int32 + int32 + uint16
            WriteInt32LE(msg, 0, subsystemOrdinal);
            WriteInt32LE(msg, 4, entityId);
            WriteUInt16LE(msg, 8, streamVersion);
            return SipHash24_64(_k0, _k1, msg);
        }

        /// <summary>
        /// Computes one draw value. Uses stackalloc to avoid heap allocation on the hot path (FR-CS-066).
        /// drawValue = SipHash-2-4-64((k0,k1), DOMAIN_TAG_RNGDRAW ‖ streamKey ‖ actionOrdinal ‖ drawIndex).
        /// AR-1 H-3: replaced new byte[21] with stackalloc Span&lt;byte&gt;[21]. §3.2.1 / §3.4.
        /// </summary>
        private ulong ComputeDrawValue(ulong streamKey, ulong actionOrdinal, uint drawIndex)
        {
            // stackalloc Span<T> is zero-allocation; statically bounded size (21 bytes). FR-CS-066 / §3.2.1.
            Span<byte> msg = stackalloc byte[21]; // 1 (domain tag) + 8 (streamKey) + 8 (actionOrdinal) + 4 (drawIndex)
            msg[0] = DeterministicSimConstants.DOMAIN_TAG_RNGDRAW;
            WriteUInt64LESpan(msg, 1, streamKey);
            WriteUInt64LESpan(msg, 9, actionOrdinal);
            WriteUInt32LESpan(msg, 17, drawIndex);
            return SipHash24_64(_k0, _k1, msg);
        }

        /// <summary>
        /// SipHash-2-4 64-bit per Aumasson &amp; Bernstein 2012.
        /// Accepts ReadOnlySpan&lt;byte&gt; so callers can pass stackalloc buffers without heap allocation.
        /// Golden vectors: Appendix A (siphash-2-4-kat.md, 64 entries; all verified byte-exact). §3.2.1.
        /// AR-1 H-3: signature changed from byte[] to ReadOnlySpan&lt;byte&gt;.
        /// </summary>
        internal static ulong SipHash24_64(ulong k0, ulong k1, ReadOnlySpan<byte> msg)
        {
            ulong v0 = k0 ^ 0x736f6d6570736575UL;
            ulong v1 = k1 ^ 0x646f72616e646f6dUL;
            ulong v2 = k0 ^ 0x6c7967656e657261UL;
            ulong v3 = k1 ^ 0x7465646279746573UL;

            int len   = msg.Length;
            int blocks = len / 8;

            for (int i = 0; i < blocks; i++)
            {
                ulong m = ReadUInt64LESpan(msg, i * 8);
                v3 ^= m;
                SipRound(ref v0, ref v1, ref v2, ref v3);
                SipRound(ref v0, ref v1, ref v2, ref v3);
                v0 ^= m;
            }

            // Last block (≤ 7 bytes) + length in high byte
            ulong last = (ulong)(len & 0xFF) << 56;
            int   tail  = len - blocks * 8;
            int   off   = blocks * 8;
            if (tail >= 7) last |= (ulong)msg[off + 6] << 48;
            if (tail >= 6) last |= (ulong)msg[off + 5] << 40;
            if (tail >= 5) last |= (ulong)msg[off + 4] << 32;
            if (tail >= 4) last |= (ulong)msg[off + 3] << 24;
            if (tail >= 3) last |= (ulong)msg[off + 2] << 16;
            if (tail >= 2) last |= (ulong)msg[off + 1] << 8;
            if (tail >= 1) last |= (ulong)msg[off + 0];

            v3 ^= last;
            SipRound(ref v0, ref v1, ref v2, ref v3);
            SipRound(ref v0, ref v1, ref v2, ref v3);
            v0 ^= last;

            v2 ^= 0xFFUL;
            SipRound(ref v0, ref v1, ref v2, ref v3);
            SipRound(ref v0, ref v1, ref v2, ref v3);
            SipRound(ref v0, ref v1, ref v2, ref v3);
            SipRound(ref v0, ref v1, ref v2, ref v3);

            return v0 ^ v1 ^ v2 ^ v3;
        }

        private static void SipRound(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                v0 += v1; v1 = RotL(v1, 13); v1 ^= v0; v0 = RotL(v0, 32);
                v2 += v3; v3 = RotL(v3, 16); v3 ^= v2;
                v0 += v3; v3 = RotL(v3, 21); v3 ^= v0;
                v2 += v1; v1 = RotL(v1, 17); v1 ^= v2; v2 = RotL(v2, 32);
            }
        }

        private static ulong RotL(ulong x, int n) => (x << n) | (x >> (64 - n));

        // ── Little-endian byte helpers (Span<byte> overloads for stackalloc hot-path) ─

        private static void WriteUInt64LESpan(Span<byte> buf, int offset, ulong value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
            buf[offset + 4] = (byte)(value >> 32);
            buf[offset + 5] = (byte)(value >> 40);
            buf[offset + 6] = (byte)(value >> 48);
            buf[offset + 7] = (byte)(value >> 56);
        }

        private static void WriteUInt32LESpan(Span<byte> buf, int offset, uint value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
        }

        // ReadOnlySpan<byte> version for SipHash24_64 message blocks (avoids type mismatch after Span migration).
        private static ulong ReadUInt64LESpan(ReadOnlySpan<byte> buf, int offset)
        {
            return (ulong)buf[offset]
                 | ((ulong)buf[offset + 1] << 8)
                 | ((ulong)buf[offset + 2] << 16)
                 | ((ulong)buf[offset + 3] << 24)
                 | ((ulong)buf[offset + 4] << 32)
                 | ((ulong)buf[offset + 5] << 40)
                 | ((ulong)buf[offset + 6] << 48)
                 | ((ulong)buf[offset + 7] << 56);
        }

        // ── Little-endian byte helpers (byte[] versions for non-hot-path calls) ──────

        private static ulong ReadUInt64LE(byte[] buf, int offset)
        {
            return (ulong)buf[offset]
                 | ((ulong)buf[offset + 1] << 8)
                 | ((ulong)buf[offset + 2] << 16)
                 | ((ulong)buf[offset + 3] << 24)
                 | ((ulong)buf[offset + 4] << 32)
                 | ((ulong)buf[offset + 5] << 40)
                 | ((ulong)buf[offset + 6] << 48)
                 | ((ulong)buf[offset + 7] << 56);
        }

        private static void WriteUInt64LE(byte[] buf, int offset, ulong value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
            buf[offset + 4] = (byte)(value >> 32);
            buf[offset + 5] = (byte)(value >> 40);
            buf[offset + 6] = (byte)(value >> 48);
            buf[offset + 7] = (byte)(value >> 56);
        }

        private static void WriteInt32LE(byte[] buf, int offset, int value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteUInt16LE(byte[] buf, int offset, ushort value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32LE(byte[] buf, int offset, uint value)
        {
            buf[offset]     = (byte)(value);
            buf[offset + 1] = (byte)(value >> 8);
            buf[offset + 2] = (byte)(value >> 16);
            buf[offset + 3] = (byte)(value >> 24);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-29 | —      | AR-1 H-3: ComputeDrawValue uses stackalloc Span<byte>; SipHash24_64  |
// |         |            |        | signature changed to ReadOnlySpan<byte>; Span write helpers added.   |
// | 1.2     | 2026-06-12 | —      | Golden-vector pass: HKDF Expand was T(1)-only (L ≤ 32) and could NOT |
// |         |            |        | reproduce RFC 5869 §A.1/§A.2 (L=42/82) — full multi-block Expand per |
// |         |            |        | §2.3 implemented; HkdfExtract/HkdfExpand split out so the KAT suite  |
// |         |            |        | asserts PRK byte-exact. No behaviour change for L ≤ 32 (production   |
// |         |            |        | path uses RNG_KDF_OUTPUT_BYTES = 16; T(1) prefix is identical).      |
#endregion
