// File:     src/season-save/AppearanceSaveCodec.cs
// Created:  2026-08-07
// Modified: 2026-08-08 (AR pass 10 L6: the duplicate-key refusals cite the owning pin — v1.1)
// Author:   —
// Spec:     Season & Competition Loop #30 Appendix B (the appearance sub-blob, ERR-041-010(b));
//           unified-season-save-design.md §3 / KD-2; ERR-029-005 / ERR-041-009 (the magic-before-
//           version rule); Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer);
//           Code Standards #20
// Purpose:  Pure byte codec for the #30 appearance sub-blob — one opaque, independently version-gated
//           block under the season frame, carrying every club's per-player AppearanceState in
//           canonical key order. Fail-loud on any framing / ordering / trailing-byte violation. No
//           file I/O (that is SeasonSaveManager), so the codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Encodes / decodes the appearance sub-blob (#30 Appendix B). The blob is one
    /// <see cref="SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION"/>-gated payload carrying every
    /// club's per-player <see cref="AppearanceState"/>.
    /// <para><b>Layout:</b></para>
    /// <code>
    /// u32  APPEARANCE_SAVE_MAGIC       ("APPR")
    /// u32  APPEARANCE_SAVE_FORMAT_VERSION
    /// u32  clubCount
    /// per club, ascending ClubId:
    ///     i32  clubId
    ///     u32  playerCount
    ///     per player, ascending PlayerId:
    ///         i32  playerId
    ///         u32  recentBits
    ///         u32  bitsAsOfWorldDay
    /// </code>
    /// <para>
    /// <b>The block says which format it is</b> (ERR-029-005 / ERR-041-009): every sub-blob format in
    /// the save stack sits at version 1, so the leading magic — not the version — is what stops these
    /// bytes being mis-read as, or fed to, a sibling codec. Checked before the version, like both
    /// siblings.
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> The block is a map keyed by <c>(clubId, playerId)</c>:
    /// <see cref="Encode"/> canonicalizes to ascending keys and <see cref="Decode"/> requires them, so
    /// two equal state sets always produce identical bytes and a duplicate key cannot reach the file.
    /// </para>
    /// <para>
    /// <b>No <c>[GT]</c> gating on decode:</b> <c>RecentBits</c> is structurally valid at any value
    /// (bits outside the configured window are dead weight the read masks off), and gating it against
    /// <c>AppearanceWindowDays</c> would turn a window retune into data loss — the ERR-029-004 rule.
    /// </para>
    /// <para>Off the 60 Hz hot path (a save is a host action), so allocation is permitted.</para>
    /// </summary>
    public static class AppearanceSaveCodec
    {
        // Layout widths, named so the size computation reads as the layout it mirrors.
        private const int BytesPerClubHeader = 4 + 4;   // clubId i32 + playerCount u32
        private const int BytesPerPlayer = 4 + 4 + 4;   // playerId, recentBits, bitsAsOfWorldDay

        /// <summary>
        /// Encodes every club's appearance states into the #30 sub-blob, canonicalized to ascending
        /// club id and, within a club, ascending player id. The caller's arrays are never mutated.
        /// </summary>
        /// <param name="clubs">The clubs to persist. An empty array encodes a well-formed zero-club block.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> is null, or a club's arrays
        /// were never bound (a <c>default(ClubAppearanceStates)</c> element).</exception>
        /// <exception cref="ArgumentException">Two clubs share a club id, or two players within one
        /// club share a player id — a duplicate key has no defined winner.</exception>
        public static byte[] Encode(ClubAppearanceStates[] clubs)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            int[] clubOrder = SaveBlobFramingHelpers.CanonicalOrder(
                ClubIdsOf(clubs), "appearance", "club id", "#30 Appendix B.1");

            int size = 4 + 4 + 4;   // magic + version + clubCount
            for (int i = 0; i < clubs.Length; i++)
            {
                size += BytesPerClubHeader + clubs[i].Count * BytesPerPlayer;
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, SeasonSaveConstants.APPEARANCE_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubs.Length);

            for (int c = 0; c < clubOrder.Length; c++)
            {
                ClubAppearanceStates club = clubs[clubOrder[c]];
                int[] playerOrder = SaveBlobFramingHelpers.CanonicalOrder(
                    RequireBound(club), "appearance", "player id", "#30 Appendix B.1");

                CanonicalSerializer.WriteI32(buf, ref o, club.ClubId);
                CanonicalSerializer.WriteU32(buf, ref o, (uint)club.Count);

                for (int p = 0; p < playerOrder.Length; p++)
                {
                    int index = playerOrder[p];
                    AppearanceState state = club.States[index];

                    CanonicalSerializer.WriteI32(buf, ref o, club.PlayerIds[index]);
                    CanonicalSerializer.WriteU32(buf, ref o, state.RecentBits);
                    CanonicalSerializer.WriteU32(buf, ref o, state.BitsAsOfWorldDay);
                }
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "AppearanceSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }

            return buf;
        }

        /// <summary>
        /// Decodes an appearance sub-blob produced by <see cref="Encode"/> back into its per-club
        /// blocks, in ascending club id with each club's players in ascending player id. Fail-loud
        /// (throws) on: a null blob; a magic mismatch (these bytes are some other format — checked
        /// first); a version mismatch (no Stage-0 migration); a truncated read or a length prefix past
        /// the blob; non-ascending club or player ids; or trailing bytes.
        /// </summary>
        /// <param name="blob">The bytes to decode.</param>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Any framing or ordering violation listed above — all of them mean the blob is corrupt.</exception>
        public static ClubAppearanceStates[] Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int len = blob.Length;
            int o = 0;

            SaveBlobFramingHelpers.Require(o, 4, len, "Appearance save", "format magic");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != SeasonSaveConstants.APPEARANCE_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    "Appearance save magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    SeasonSaveConstants.APPEARANCE_SAVE_MAGIC.ToString("X8") +
                    " — these bytes are not a #30 appearance block (ERR-029-005: a version gate " +
                    "cannot tell one format from another).");
            }

            SaveBlobFramingHelpers.Require(o, 4, len, "Appearance save", "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Appearance save format version " + format + " != expected " +
                    SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0.");
            }

            int clubCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, BytesPerClubHeader, "Appearance save", "club");
            var clubs = new ClubAppearanceStates[clubCount];
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubCount; c++)
            {
                SaveBlobFramingHelpers.Require(o, 4, len, "Appearance save", "club id");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(clubId, ref previousClubId, "Appearance save", "club id", c);

                int playerCount = SaveBlobFramingHelpers.ReadCount(
                    blob, ref o, len, BytesPerPlayer, "Appearance save", "player");
                var playerIds = new int[playerCount];
                var states = new AppearanceState[playerCount];
                long previousPlayerId = long.MinValue;

                for (int p = 0; p < playerCount; p++)
                {
                    SaveBlobFramingHelpers.Require(o, BytesPerPlayer, len, "Appearance save", "player appearance state");

                    int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                    SaveBlobFramingHelpers.RequireAscending(
                        playerId, ref previousPlayerId, "Appearance save", "player id in club " + clubId, p);

                    uint recentBits = CanonicalSerializer.ReadU32(blob, ref o);
                    uint bitsAsOfWorldDay = CanonicalSerializer.ReadU32(blob, ref o);

                    playerIds[p] = playerId;
                    states[p] = new AppearanceState
                    {
                        RecentBits = recentBits,
                        BitsAsOfWorldDay = bitsAsOfWorldDay,
                    };
                }

                clubs[c] = new ClubAppearanceStates(clubId, playerIds, states);
            }

            // ── Trailing-byte guard ───────────────────────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Appearance save blob has " + (len - o) + " trailing byte(s) after the declared " +
                    "content — truncated, padded, or corrupt.");
            }

            return clubs;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static int[] ClubIdsOf(ClubAppearanceStates[] clubs)
        {
            var ids = new int[clubs.Length];
            for (int i = 0; i < clubs.Length; i++)
            {
                ids[i] = clubs[i].ClubId;
            }

            return ids;
        }

        // A default(ClubAppearanceStates) has null arrays — Count reads 0, so it would encode as an
        // empty club rather than announcing itself. Refuse it, like both sibling codecs.
        private static int[] RequireBound(ClubAppearanceStates club)
        {
            if (club.PlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(club),
                    "Club " + club.ClubId + " has no bound arrays (a default(ClubAppearanceStates)) — " +
                    "construct it, or pass an explicitly empty one.");
            }

            return club.PlayerIds;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2, ERR-041-010(b)): the     |
// |         |            |        | APPR sub-blob — magic-before-version, canonical key order,        |
// |         |            |        | trailing-byte guard, no [GT] gating on decode.                    |
// | 1.1     | 2026-08-08 | —      | Balance-pass AR pass 10 (L6): the canonical-order refusals cite    |
// |         |            |        | #30 Appendix B.1 — the APPR block's OWNING pin — not FR-MD-025,    |
// |         |            |        | a sibling spec's id on a block #41 is forbidden to describe        |
// |         |            |        | (KD-2/KD-7).                                                       |
#endregion
