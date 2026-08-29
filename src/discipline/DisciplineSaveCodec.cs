// File:     src/discipline/DisciplineSaveCodec.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest (finding B, doc/message only — v1.3: Decode's negative-PlayerId refusal
//           message cited a packed-id integer-division derivation ERR-044-014 deleted from
//           OnClubFixturePlayed the same day (DisciplineRules.cs v1.7) — a guard justified by retired
//           machinery. Restated on surviving grounds: unresolvable identity, MigratePlayerId's own F2
//           refusal of a negative migration target, and this decoder's own strictly-ascending ordering
//           invariant depending on non-negative keys. No behaviour change.)
// Modified: 2026-08-13 (#44 C1/C2 adversarial review round 5, L14 — Decode's doc corrected: it claimed
//           the negative-PlayerId check ran BEFORE the ordering comparison; the ordering check is at
//           :172, the negativity check at :186 — the reverse. Behaviour unchanged; message-only — v1.2)
// Author:   —
// Spec:     Discipline & Suspensions #44 §4.4 + Appendix B (the sub-blob layout) / FR-DC-014/015/016;
//           F3 / §2.3 F2; ERR-044-001 (the magic-before-version correction, ERR-029-005/ERR-041-009
//           class); Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the #44 discipline sub-blob — one opaque, independently version-gated
//           block under #30's season frame, carrying every discipline row in canonical key order.
//           Fail-loud on any framing / ordering / range / trailing-byte violation. No file I/O.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// Encodes / decodes the discipline sub-blob (#44 Appendix B, as corrected by ERR-044-001).
    /// <para><b>Layout:</b></para>
    /// <code>
    /// u32  DISCIPLINE_SAVE_MAGIC          ("DISC")
    /// u32  DISCIPLINE_SAVE_FORMAT_VERSION
    /// u32  entryCount                     (0 at genesis)
    /// per entry, STRICTLY ascending (PlayerId, CompetitionId):
    ///     i32  playerId
    ///     i32  competitionId
    ///     i32  yellows                    (&gt;= 0)
    ///     i32  banMatchesRemaining        (&gt;= 0)
    /// </code>
    /// <para>
    /// <b>The block says which format it is.</b> Appendix B specified it version-first with no magic —
    /// the fourth appearance of the defect ERR-029-005 / ERR-041-009 turned into a MUST and ERR-028-004
    /// hit again. It matters acutely here: every sub-blob format under the season frame sits at version
    /// 1, and <c>version | count | i32 fields</c> is the shape of the progression, training and medical
    /// blocks too, so without the leading magic a transposed <c>byte[]</c> in
    /// <c>SeasonSaveCodec.Encode</c>'s now-seven identically-typed payloads would decode cleanly,
    /// completely and silently as the wrong subsystem's state. Corrected as <b>ERR-044-001</b>; the
    /// compile-time half is <c>SeasonSave.DisciplineBlock</c>.
    /// </para>
    /// <para>
    /// <b>Recompute-on-load is not an option</b> (KD-1, FR-DC-014): no ledgers are retained, so a
    /// season's cards exist nowhere else. A load that failed to reconstruct a ban would silently make a
    /// suspended player available.
    /// </para>
    /// <para>
    /// <b>No RNG-state field of any kind</b> (FR-DC-016) — #44 has none to write (FR-DC-019).
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> <see cref="DisciplineState"/> already holds rows in canonical
    /// ascending <c>(PlayerId, CompetitionId)</c> order, so <see cref="Encode"/> needs no
    /// canonicalisation pass and <see cref="Decode"/> requires that order back — two equal states
    /// always produce identical bytes, and a duplicate key cannot reach the file.
    /// </para>
    /// <para>Off the 60 Hz hot path (a save is a host action), so allocation is permitted.</para>
    /// </summary>
    public static class DisciplineSaveCodec
    {
        /// <summary>playerId + competitionId + yellows + banMatchesRemaining, all i32.</summary>
        private const int BytesPerEntry = 4 + 4 + 4 + 4;

        /// <summary>magic + version + entryCount.</summary>
        private const int HeaderBytes = 4 + 4 + 4;

        /// <summary>
        /// Encodes the tally into the #44 sub-blob. An empty state encodes a well-formed zero-entry
        /// block — which is what every save written before a single card is shown carries, and is not
        /// the same thing as an absent block.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        public static byte[] Encode(DisciplineState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            byte[] buf = new byte[HeaderBytes + state.Count * BytesPerEntry];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, DisciplineConstants.DISCIPLINE_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)state.Count);

            for (int i = 0; i < state.Count; i++)
            {
                DisciplineEntry entry = state.EntryAt(i);
                CanonicalSerializer.WriteI32(buf, ref o, entry.PlayerId);
                CanonicalSerializer.WriteI32(buf, ref o, entry.CompetitionId);
                CanonicalSerializer.WriteI32(buf, ref o, entry.Yellows);
                CanonicalSerializer.WriteI32(buf, ref o, entry.BanMatchesRemaining);
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "DisciplineSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }

            return buf;
        }

        /// <summary>
        /// Decodes a discipline sub-blob produced by <see cref="Encode"/>. Fail-loud (throws) on: a null
        /// blob; a magic mismatch (these bytes are some other format — checked FIRST); a version
        /// mismatch (no Stage-0 migration); a truncated read or a count prefix past the blob;
        /// non-ascending <c>(PlayerId, CompetitionId)</c> — checked per entry BEFORE the negative-
        /// <c>PlayerId</c> check below it (L14: an earlier revision of this comment claimed the reverse
        /// order); a negative <c>PlayerId</c> (§2.3 F2); a negative <c>Yellows</c> or
        /// <c>BanMatchesRemaining</c>; an all-zero row; or trailing bytes. Every one of them is
        /// <b>F3</b> (FR-DC-015), so which one fires first is not load-bearing — for every entry but the
        /// first, a negative id also breaks strict ascent against the previous real key and is caught by
        /// the ordering check anyway; only entry 0, compared against the unconditionally-lower
        /// <c>long.MinValue</c> sentinel, can reach the dedicated negativity check on its own.
        /// <para>
        /// The all-zero refusal is not in Appendix B's field table but follows from FR-DC-017: an
        /// absent row is the ONE representation of a clean player, so a stored <c>(0, 0)</c> is a
        /// corrupt file, and accepting it would give two equivalent careers different bytes forever
        /// after. It is refused here as well as at <see cref="DisciplineState.FromEntries"/> so the
        /// file boundary never accepts what the composition boundary rejects — the asymmetry #29/#41's
        /// AR pass 6 filed as a Medium at four separate boundaries.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Any framing, ordering or range violation above — all of them mean the blob is corrupt.</exception>
        public static DisciplineState Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int len = blob.Length;
            int o = 0;

            SaveBlobFramingHelpers.Require(o, 4, len, "Discipline save", "format magic");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != DisciplineConstants.DISCIPLINE_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    "Discipline save magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    DisciplineConstants.DISCIPLINE_SAVE_MAGIC.ToString("X8") +
                    " — these bytes are not a #44 discipline block (ERR-029-005 / ERR-044-001: a " +
                    "version gate cannot tell one format from another).");
            }

            SaveBlobFramingHelpers.Require(o, 4, len, "Discipline save", "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Discipline save format version " + format + " != expected " +
                    DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0.");
            }

            int entryCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, BytesPerEntry, "Discipline save", "entry");

            var entries = new DisciplineEntry[entryCount];
            long previousPlayerId = long.MinValue;
            long previousCompetitionId = long.MinValue;

            for (int i = 0; i < entryCount; i++)
            {
                SaveBlobFramingHelpers.Require(o, BytesPerEntry, len, "Discipline save", "entry");
                int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                int competitionId = CanonicalSerializer.ReadI32(blob, ref o);
                int yellows = CanonicalSerializer.ReadI32(blob, ref o);
                int banMatchesRemaining = CanonicalSerializer.ReadI32(blob, ref o);

                // The key is a PAIR, so SaveBlobFramingHelpers.RequireAscending — which carries one
                // int against one cursor — cannot express it. Same posture, same message shape.
                if (playerId < previousPlayerId
                    || (playerId == previousPlayerId && competitionId <= previousCompetitionId))
                {
                    throw new InvalidOperationException(
                        "Discipline save entry " + i + " has key (" + playerId + ", " + competitionId +
                        "), which does not strictly follow (" + previousPlayerId + ", " +
                        previousCompetitionId + ") — entries MUST be strictly ascending by " +
                        "(PlayerId, CompetitionId) (F3). Every lookup in DisciplineState is a binary " +
                        "search over that order, so an unordered block would silently lose a carried " +
                        "player's bans.");
                }
                previousPlayerId = playerId;
                previousCompetitionId = competitionId;

                if (playerId < 0)
                {
                    throw new InvalidOperationException(
                        "Discipline save entry " + i + " has PlayerId = " + playerId + "; PlayerId " +
                        "MUST be >= 0 (F3 / §2.3 F2). A negative id is an unresolvable identity: it " +
                        "names no real player, DisciplineRules.MigratePlayerId refuses one as a " +
                        "migration target for the same reason, and this decoder's own strictly-" +
                        "ascending (PlayerId, CompetitionId) ordering invariant depends on every key " +
                        "being non-negative.");
                }

                if (yellows < 0 || banMatchesRemaining < 0)
                {
                    throw new InvalidOperationException(
                        "Discipline save entry " + i + " (player " + playerId + ", competition " +
                        competitionId + ") has Yellows = " + yellows + ", BanMatchesRemaining = " +
                        banMatchesRemaining + "; both MUST be >= 0 (F3).");
                }

                if (yellows == 0 && banMatchesRemaining == 0)
                {
                    throw new InvalidOperationException(
                        "Discipline save entry " + i + " (player " + playerId + ", competition " +
                        competitionId + ") is all-zero. FR-DC-017 makes an absent row the ONE " +
                        "representation of a clean player, so a stored (0, 0) row is corrupt — " +
                        "accepting it would let two equivalent careers serialize different bytes.");
                }

                entries[i] = new DisciplineEntry(playerId, competitionId, yellows, banMatchesRemaining);
            }

            if (o != len)
            {
                throw new InvalidOperationException(
                    "Discipline save has " + (len - o) + " trailing byte(s) after " + entryCount +
                    " entries (F3) — the blob is longer than its own contents describe.");
            }

            return DisciplineState.FromEntries(entries);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T1, roadmap C1): the Appendix B      |
// |         |            |        | sub-blob, magic-led per ERR-044-001, with F3 gates on version,   |
// |         |            |        | bounds, key order, value range, canonical minimality and         |
// |         |            |        | trailing bytes — refused at BOTH boundaries, not just this one.  |
// | 1.1     | 2026-08-13 | —      | AR fix (M1): Decode now refuses a negative PlayerId explicitly   |
// |         |            |        | (F3 / §2.3 F2) instead of letting it through as an unusually-    |
// |         |            |        | small-but-otherwise-valid key — the file boundary was the one    |
// |         |            |        | entry point that did not gate the player half of F2.             |
// | 1.2     | 2026-08-13 | —      | AR round 5 fix (L14): Decode's doc corrected — it claimed the    |
// |         |            |        | negative-PlayerId check (:186) ran BEFORE the ordering comparison |
// |         |            |        | (:172) could misread it as a real key; the code has always run    |
// |         |            |        | ordering first. Behaviour unchanged (both are F3 fail-loud, and   |
// |         |            |        | for any entry but the first a negative id trips the ordering      |
// |         |            |        | check anyway) — message-only fix, no code change.                 |
// | 1.3     | 2026-08-16, latest | — | Finding B, doc/message only. Decode's negative-PlayerId        |
// |         |            |        | refusal cited a packed-id integer-division derivation ERR-044-014 |
// |         |            |        | deleted from OnClubFixturePlayed the same day (DisciplineRules.cs |
// |         |            |        | v1.7) — a guard justified by retired machinery. Restated on       |
// |         |            |        | surviving grounds: unresolvable identity, MigratePlayerId's own   |
// |         |            |        | F2 refusal of a negative migration target, and this decoder's own |
// |         |            |        | strictly-ascending ordering invariant depending on non-negative   |
// |         |            |        | keys. No behaviour change.                                         |
#endregion
