// File:     src/season-save/SeasonSaveCodec.cs
// Created:  2026-07-22
// Modified: 2026-08-08 (balance pass D2: the frame gains the #30 appearance sub-blob;
//           SEASON_SAVE_FORMAT_VERSION 3 -> 4 per ERR-041-010(b))
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §3 layout / KD-2..KD-8;
//           Season & Competition Loop #30 Appendix B (frame), FR-SN-019/020/023;
//           Training System #29 §4.4 / FR-TR-018; Injuries & Medical #41 §4.4 / FR-MD-017;
//           Match Engine design note §5 Phase G-Phase 3; Deterministic Simulation #16 §3.2.4.1
//           (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the season save frame: packs the living-world composite blob, the
//           season-state blob, the #29 training blob, the #41 medical blob, the #30 appearance blob,
//           and an optional match save
//           blob into one version-gated, self-describing frame and deframes it back, fail-loud on any
//           framing / length-bound / trailing-byte violation. Treats each sub-blob as opaque (never
//           parses it — each keeps its own version gate). No file I/O (that is SeasonSaveManager), so
//           the codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Encodes / decodes the season save frame (unified-season-save-design.md §3 / #30 Appendix B). The
    /// frame is one <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/>-gated wrapper around a
    /// <c>matchPresent</c> flag and six length-prefixed opaque sub-blobs, in order: the living-world
    /// composite (always), the season state (always), the #29 training block (always), the #41 medical
    /// block (always), the #30 appearance block (always), and the match save (only when a match was in
    /// progress). Serialized through
    /// <see cref="CanonicalSerializer"/>. Off the 60 Hz hot path (a save is a host action), so
    /// allocation is permitted.
    /// <para>
    /// <b>Why the two new blocks are mandatory rather than flagged.</b> The match block is optional
    /// because a season genuinely either has an in-progress match or does not. Training and medical
    /// state has no such absent case: a game that tracks none of it has an <i>empty</i> set, which its
    /// own codec encodes as a well-formed zero-club block. Making them optional would add two more
    /// presence flags to distinguish "no clubs" from "no block" — a distinction with no meaning — and
    /// would need a second frame bump the day #29/#41 are wired.
    /// </para>
    /// </summary>
    public static class SeasonSaveCodec
    {
        // matchPresent flag byte (KD-3).
        private const byte MatchAbsent  = 0;
        private const byte MatchPresent = 1;

        /// <summary>
        /// Encodes a season blob from the living-world composite blob, the season-state blob, the #29
        /// training blob, the #41 medical blob, the #30 appearance blob, and an optional match save
        /// blob. Presence keys on
        /// <paramref name="matchBlobOrNull"/> being <c>null</c> (KD-8): null ⇒ no match (flag 0, no
        /// match block); non-null ⇒ the bytes are written after the appearance block (flag 1). Fail-loud on
        /// a null <paramref name="worldBlob"/> or <paramref name="seasonBlob"/>, or an unbound
        /// <paramref name="training"/> / <paramref name="medical"/> / <paramref name="appearance"/> —
        /// a season save always carries all five mandatory blobs (KD-3 / FR-SN-019 / FR-TR-018 /
        /// FR-MD-017 / #30 Appendix B). The buffer is sized exactly to the content;
        /// see <see cref="Decode"/> for the inverse (kept adjacent so a layout change is edited in one
        /// place — R1).
        /// <para>
        /// <b>The two new blocks are typed, not bare <c>byte[]</c>.</b> They have the same byte shape as
        /// each other, so a transposition in this parameter list produced a file that framed, decoded
        /// and reloaded without a single throw (ERR-029-005 / ERR-041-009). The leading magic in each
        /// block now catches it at load; <see cref="TrainingBlock"/> / <see cref="MedicalBlock"/> catch
        /// it at compile time, which is where a positional mistake should die.
        /// </para>
        /// </summary>
        public static byte[] Encode(
            byte[] worldBlob,
            byte[] seasonBlob,
            in TrainingBlock training,
            in MedicalBlock medical,
            in AppearanceBlock appearance,
            byte[] matchBlobOrNull)
        {
            if (worldBlob == null)
            {
                throw new ArgumentNullException(nameof(worldBlob),
                    "A season save always carries a living-world composite blob (KD-3).");
            }

            if (seasonBlob == null)
            {
                throw new ArgumentNullException(nameof(seasonBlob),
                    "A season save always carries a season-state blob (FR-SN-019).");
            }

            // The wrappers' constructors reject a null, but `default(TrainingBlock)` skips the
            // constructor entirely and reads Bytes == null — the same hole `default(ClubTrainingStates)`
            // opens one layer down, and closed here the same way.
            byte[] trainingBlob = training.Bytes;
            if (trainingBlob == null)
            {
                throw new ArgumentNullException(nameof(training),
                    "A season save always carries a training block — an empty one is a zero-club " +
                    "block, not a null or a default(TrainingBlock) (FR-TR-018).");
            }

            byte[] medicalBlob = medical.Bytes;
            if (medicalBlob == null)
            {
                throw new ArgumentNullException(nameof(medical),
                    "A season save always carries a medical block — an empty one is a zero-club " +
                    "block, not a null or a default(MedicalBlock) (FR-MD-017).");
            }

            byte[] appearanceBlob = appearance.Bytes;
            if (appearanceBlob == null)
            {
                throw new ArgumentNullException(nameof(appearance),
                    "A season save always carries an appearance block — an empty one is a zero-club " +
                    "block, not a null or a default(AppearanceBlock) (#30 Appendix B).");
            }

            bool hasMatch = matchBlobOrNull != null;
            int size = 4                              // SEASON_SAVE_FORMAT_VERSION
                     + 1                              // matchPresent flag
                     + 4 + worldBlob.Length           // world length prefix + body
                     + 4 + seasonBlob.Length          // season length prefix + body
                     + 4 + trainingBlob.Length        // training length prefix + body
                     + 4 + medicalBlob.Length         // medical length prefix + body
                     + 4 + appearanceBlob.Length;     // appearance length prefix + body
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

            CanonicalSerializer.WriteU32(buf, ref o, (uint)seasonBlob.Length);
            Array.Copy(seasonBlob, 0, buf, o, seasonBlob.Length); o += seasonBlob.Length;

            CanonicalSerializer.WriteU32(buf, ref o, (uint)trainingBlob.Length);
            Array.Copy(trainingBlob, 0, buf, o, trainingBlob.Length); o += trainingBlob.Length;

            CanonicalSerializer.WriteU32(buf, ref o, (uint)medicalBlob.Length);
            Array.Copy(medicalBlob, 0, buf, o, medicalBlob.Length); o += medicalBlob.Length;

            CanonicalSerializer.WriteU32(buf, ref o, (uint)appearanceBlob.Length);
            Array.Copy(appearanceBlob, 0, buf, o, appearanceBlob.Length); o += appearanceBlob.Length;

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
        /// Decodes a season blob produced by <see cref="Encode"/> into its six opaque sub-blobs.
        /// Fail-loud (throws) on: a null blob; a <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/>
        /// mismatch (KD-4, no Stage-0 migration — a v1, v2 or v3 file is rejected
        /// here); a <c>matchPresent</c> flag that is neither 0 nor 1; a length prefix (world, season,
        /// training, medical, appearance, or match) that would read past the blob; or any trailing bytes after the
        /// declared content (KD-8 / R1 truncation guard). The inner sub-blob version drift is caught by
        /// <see cref="TacticalDirector.LivingWorld.WorldStore.Restore"/> /
        /// <see cref="SeasonStateCodec.Decode"/> /
        /// <see cref="TacticalDirector.TrainingSystem.TrainingSaveCodec.Decode"/> /
        /// <see cref="TacticalDirector.InjuriesMedical.MedicalSaveCodec.Decode"/> /
        /// <see cref="AppearanceSaveCodec.Decode"/> / the match decode path.
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
            byte[] seasonBlob = ReadBlock(blob, ref o, len, "season state");
            byte[] trainingBlob = ReadBlock(blob, ref o, len, "training block");
            byte[] medicalBlob = ReadBlock(blob, ref o, len, "medical block");
            byte[] appearanceBlob = ReadBlock(blob, ref o, len, "appearance block");

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

            return new SeasonSaveBlobs(
                worldBlob, seasonBlob, trainingBlob, medicalBlob, appearanceBlob, matchBlob);
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
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1: the frame gains the season-state sub-blob between the    |
// |         |            |        | world and match blocks (Appendix B); Encode takes seasonBlob     |
// |         |            |        | (fail-loud on null — always present, unlike the match).          |
// | 1.2     | 2026-08-06 | —      | #29/#41 T1: the frame gains the training and medical sub-blobs   |
// |         |            |        | between the season block and the optional match block; both are  |
// |         |            |        | mandatory (an empty set is a zero-club block, not a null).       |
// | 1.3     | 2026-08-06 | —      | ERR-029-005 / ERR-041-009 (AR pass 1, H): the training and       |
// |         |            |        | medical parameters become TrainingBlock / MedicalBlock. The two  |
// |         |            |        | payloads are byte-shape-identical, so transposing them in a list |
// |         |            |        | of five byte[] framed, decoded and reloaded without a throw.     |
// | 1.4     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): the frame gains the mandatory  |
// |         |            |        | #30 appearance sub-blob between the medical block and the        |
// |         |            |        | optional match block (SEASON_SAVE_FORMAT_VERSION 3 -> 4); typed  |
// |         |            |        | as AppearanceBlock per the ERR-029-005 discipline.               |
// | 1.5     | 2026-08-08 | —      | Balance-pass AR pass 4 (L5, doc only): the file-header Purpose    |
// |         |            |        | and Encode's doc predated the appearance block — "all four"       |
// |         |            |        | mandatory blobs and a five-item list; now five-of-six with the    |
// |         |            |        | appearance blob named (the SeasonSaveManager v1.6 doc-drift       |
// |         |            |        | class, one file over).                                            |
#endregion
