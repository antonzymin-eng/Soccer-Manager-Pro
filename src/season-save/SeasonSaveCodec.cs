// File:     src/season-save/SeasonSaveCodec.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2..KD-8;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §3.2.4.1
//           (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the season save frame: packs the living-world composite blob and an
//           optional match save blob into one version-gated, self-describing frame and deframes it back,
//           fail-loud on any framing / length-bound / trailing-byte violation. Treats each sub-blob as
//           opaque (never parses it — each keeps its own version gate). No file I/O (that is
//           SeasonSaveManager), so the codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Encodes / decodes the season save frame (unified-season-save-design.md §3). The frame is one
    /// <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/>-gated wrapper around a
    /// <c>matchPresent</c> flag and two length-prefixed opaque sub-blobs: the living-world composite
    /// (always) and the match save (only when a match was in progress). Serialized through
    /// <see cref="CanonicalSerializer"/>. Off the 60 Hz hot path (a save is a host action), so
    /// allocation is permitted.
    /// </summary>
    public static class SeasonSaveCodec
    {
        // matchPresent flag byte (KD-3).
        private const byte MatchAbsent  = 0;
        private const byte MatchPresent = 1;

        /// <summary>
        /// Encodes a season blob from the living-world composite blob and an optional match save blob.
        /// Presence keys on <paramref name="matchBlobOrNull"/> being <c>null</c> (KD-8): null ⇒ no match
        /// (flag 0, no match block); non-null ⇒ the bytes are written after the world block (flag 1).
        /// Fail-loud on a null <paramref name="worldBlob"/> (a season always has a world). The buffer is
        /// sized exactly to the content; see <see cref="Decode"/> for the inverse (kept adjacent so a
        /// layout change is edited in one place — R1).
        /// </summary>
        public static byte[] Encode(byte[] worldBlob, byte[] matchBlobOrNull)
        {
            if (worldBlob == null)
            {
                throw new ArgumentNullException(nameof(worldBlob),
                    "A season save always carries a living-world composite blob (KD-3).");
            }

            bool hasMatch = matchBlobOrNull != null;
            int size = 4                              // SEASON_SAVE_FORMAT_VERSION
                     + 1                              // matchPresent flag
                     + 4 + worldBlob.Length;          // world length prefix + body
            if (hasMatch)
            {
                size += 4 + matchBlobOrNull.Length;   // match length prefix + body
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU8(buf, ref o, hasMatch ? MatchPresent : MatchAbsent);

            CanonicalSerializer.WriteU32(buf, ref o, (uint)worldBlob.Length);
            Array.Copy(worldBlob, 0, buf, o, worldBlob.Length); o += worldBlob.Length;

            if (hasMatch)
            {
                CanonicalSerializer.WriteU32(buf, ref o, (uint)matchBlobOrNull.Length);
                Array.Copy(matchBlobOrNull, 0, buf, o, matchBlobOrNull.Length); o += matchBlobOrNull.Length;
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "SeasonSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " + buf.Length +
                    " — the size computation is out of sync with Encode.");
            }
            return buf;
        }

        /// <summary>
        /// Decodes a season blob produced by <see cref="Encode"/> into its two opaque sub-blobs.
        /// Fail-loud (throws) on: a null blob; a <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/>
        /// mismatch (KD-4, no Stage-0 migration); a <c>matchPresent</c> flag that is neither 0 nor 1; a
        /// length prefix (world or match) that would read past the blob; or any trailing bytes after the
        /// declared content (KD-8 / R1 truncation guard). The inner sub-blob version drift is caught by
        /// <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> / the match decode path.
        /// </summary>
        public static SeasonSaveBlobs Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            int len = blob.Length;
            int o = 0;

            Require(o, 4, len, "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Season save file format version " + format + " != expected " +
                    SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (KD-4).");
            }

            Require(o, 1, len, "matchPresent flag");
            byte matchFlag = CanonicalSerializer.ReadU8(blob, ref o);
            if (matchFlag != MatchAbsent && matchFlag != MatchPresent)
            {
                throw new InvalidOperationException(
                    "Season save matchPresent flag " + matchFlag + " is neither 0 nor 1 — corrupt save.");
            }

            byte[] worldBlob = ReadBlock(blob, ref o, len, "world composite");

            byte[] matchBlob = null;
            if (matchFlag == MatchPresent)
            {
                matchBlob = ReadBlock(blob, ref o, len, "match save");
            }

            // ── Trailing-byte guard (KD-8 / R1) ───────────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Season save file has " + (len - o) + " trailing byte(s) after the declared content " +
                    "— truncated, padded, or corrupt (R1).");
            }

            return new SeasonSaveBlobs(worldBlob, matchBlob);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        // Reads a length-prefixed opaque block: the u32 length, refuses a length that would read past
        // the blob, then copies out the bytes verbatim. Fail-loud (KD-8), never over-read or OOM.
        private static byte[] ReadBlock(byte[] blob, ref int o, int total, string what)
        {
            Require(o, 4, total, what + " length");
            uint blockLen = CanonicalSerializer.ReadU32(blob, ref o);
            Require(o, (int)blockLen, total, what + " body");
            byte[] block = new byte[blockLen];
            Array.Copy(blob, o, block, 0, (int)blockLen);
            o += (int)blockLen;
            return block;
        }

        private static void Require(int offset, int need, int total, string what)
        {
            // Overflow-safe (the MatchSaveCodec.Require posture): compare against (total - offset) rather
            // than (offset + need), since a corrupt length prefix can push `need` near int.MaxValue and
            // `offset + need` would wrap negative and slip past the guard. `offset` is always in
            // [0, total] here (every read is guarded), so `total - offset` is a safe non-negative int.
            if (need < 0 || offset > total || need > total - offset)
            {
                throw new InvalidOperationException(
                    "Season save file truncated reading " + what + " (need " + need + " byte(s) at offset " +
                    offset + " of " + total + ").");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-22 | —      | Initial implementation. |
#endregion
