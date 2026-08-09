// File:     src/player-progression/ProgressionSaveCodec.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.5 (the save codec), §4.2, KD-4, FR-PG-016/017/018/019,
//           F3/F5; ERR-028-004 (§3.5 named the RNG domain tag as the block's identifier);
//           Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the #28 career-state sub-blob — one opaque, independently version-gated
//           block under #30's season save. Encodes the per-club per-player PlayerRecord + PlayerLifecycle
//           map in canonical key order and reads it back, fail-loud on any magic / version / length-bound
//           / ordering / trailing-byte violation. No file I/O (that is SeasonSaveManager), so the codec
//           is exhaustively unit-testable in memory.

using System;
using System.Text;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// Encodes / decodes the #28 career-state sub-blob (§3.5). The blob carries every club's per-player
    /// <see cref="PlayerRecord"/> and its <see cref="PlayerLifecycle"/> overlay, plus the store-level
    /// <c>NextPlayerId</c> cursor (FR-PG-011).
    /// <para>
    /// <b>Layout</b> (pinned here and mirrored in #28 §3.5; the sibling blocks' discipline):
    /// </para>
    /// <code>
    /// u32  PROGRESSION_SAVE_MAGIC          ("PROG" — BEFORE the version)
    /// u32  PROGRESSION_SAVE_FORMAT_VERSION
    /// i32  nextPlayerId
    /// u32  clubCount
    /// per club, ascending ClubId:
    ///     i32  clubId
    ///     u32  playerCount
    ///     per player, ascending PlayerId:
    ///         i32  playerId
    ///         str  firstName                (u32 length + ASCII)
    ///         str  lastName
    ///         i32  age                      (derived cache; recomputed on the next AdvanceDay)
    ///         u8   position                 (PlayerPosition ordinal)
    ///         i32  attribute[0 .. 30]       (AttrIdx order, [1,20])
    ///         i32  weakFootRating
    ///         i32  potentialAbility
    ///         i32  currentAbility           (derived cache; recompute-equals-stored is locked)
    ///         i64  growthCursor             (the ONLY accumulator, FR-PG-002)
    ///         i64  birthWorldDay            (the authoritative age anchor, FR-PG-005; SIGNED — a
    ///                                        player born before the day-0 epoch is ordinary, ERR-028-006)
    ///         u8   retirementFlag
    ///         u32  retirementDay
    ///         u32  lastAdvancedWorldDay    (sentinel uint.MaxValue = never advanced)
    /// </code>
    /// <para>
    /// <b>The block says which format it is.</b> The leading
    /// <see cref="PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC"/> is not decoration. §3.5 as
    /// approved specified <c>VERSION -> DOMAIN_TAG_PLAYER_PROGRESSION -> …</c>, i.e. version-first with
    /// an RNG domain tag standing in as the identifier — and every sub-blob format in this save stack
    /// sits at version 1, so a transposed <c>byte[]</c> at the frame would decode a sibling's bytes
    /// against this layout with no gate tripped. A version gate separates generations of ONE format,
    /// never one format from another; and the magic is deliberately NOT an RNG domain tag, which is a
    /// hash-domain separator with an unrelated job. See ERR-028-004 / ERR-029-005 / ERR-041-009.
    /// </para>
    /// <para>
    /// <b>The club id is written, not implied by position</b> — the ERR-041-008 rule: identity carried
    /// by list order is an implicit agreement with a sibling blob this codec is forbidden to read.
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> The block is a map keyed by <c>(clubId, playerId)</c>, so
    /// <see cref="Encode"/> canonicalizes to ascending keys — two equal state sets always produce
    /// identical bytes regardless of the caller's roster order — and <see cref="Decode"/> requires that
    /// order, so a corrupt blob cannot smuggle in a duplicate key and
    /// <see cref="ProgressionEngine"/> can binary-search what comes back.
    /// </para>
    /// <para>
    /// <b>No RNG cursor is persisted</b>, because this landing has no draw site: new-game
    /// <c>PotentialAbility</c> is seeded deterministically (<see cref="ProgressionEngine.SeedFrom"/>) and
    /// the daily step is draw-free (FR-PG-002). The <c>player-progression.regen</c> stream registers at
    /// the first regen, which is the deferred season-boundary landing's concern.
    /// </para>
    /// <para>
    /// Off the 60 Hz hot path (a save is a host action), so allocation is permitted.
    /// </para>
    /// </summary>
    public static class ProgressionSaveCodec
    {
        private const string Subject = "Progression save";
        private const string SetName = "progression";

        // Layout widths, named so the size computation reads as the layout it mirrors.
        private const int BytesPerClubHeader = 4 + 4;   // clubId i32 + playerCount u32

        // The MINIMUM width of one player: every fixed field, plus the two string length prefixes with
        // empty bodies. Used as the ReadCount bound, which the helper documents as a minimum.
        private const int MinBytesPerPlayer =
            4                       // playerId
            + 4 + 4                 // firstName length + lastName length (empty bodies)
            + 4                     // age
            + 1                     // position
            + AttrIdx.Count * 4     // attributes
            + 4                     // weakFootRating
            + 4                     // potentialAbility
            + 4                     // currentAbility
            + 8                     // growthCursor
            + 8                     // birthWorldDay (SIGNED — a pre-epoch birth is ordinary, ERR-028-006)
            + 1                     // retirementFlag
            + 4                     // retirementDay
            + 4;                    // lastAdvancedWorldDay

        /// <summary>
        /// Encodes every club's career states into the #28 sub-blob, canonicalized to ascending club id
        /// and, within a club, ascending player id. The caller's arrays are never mutated. The buffer is
        /// sized exactly to the content; see <see cref="Decode"/> for the inverse (kept adjacent so a
        /// layout change is edited in one place).
        /// </summary>
        /// <param name="clubs">The clubs to persist. An empty array is legal and encodes a well-formed,
        /// zero-club block — the honest state of a game that tracks no careers yet, not a missing blob.</param>
        /// <param name="nextPlayerId">The store-level monotonic id cursor (FR-PG-011).</param>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> is null, or a club's arrays
        /// were never bound (a <c>default(ClubCareerStates)</c> element).</exception>
        /// <exception cref="ArgumentException">Two clubs share a club id, or two players within one club
        /// share a player id — a duplicate key has no defined winner (FR-PG-019).</exception>
        /// <exception cref="InvalidOperationException">A record being encoded carries an undefined
        /// <see cref="PlayerPosition"/> ordinal, or a non-ASCII name — both are contracts
        /// <see cref="Decode"/> itself refuses, so writing either would produce a file no load of it
        /// could accept (the never-write-what-Decode-refuses rule).</exception>
        public static byte[] Encode(ClubCareerStates[] clubs, int nextPlayerId)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            int[] clubOrder = SaveBlobFramingHelpers.CanonicalOrder(
                ClubIdsOf(clubs), SetName, "club id", "FR-PG-019");

            // Per-club player orders, computed once and reused by the sizing pass and the write pass —
            // two independent orderings of the same data is the parallel-surface defect one file down.
            var playerOrders = new int[clubs.Length][];
            for (int i = 0; i < clubs.Length; i++)
            {
                RequireBound(clubs[i], i);
                playerOrders[i] = SaveBlobFramingHelpers.CanonicalOrder(
                    PlayerIdsOf(clubs[i]), SetName, "player id in club " + clubs[i].ClubId, "FR-PG-019");
            }

            int size = 4 + 4 + 4 + 4;   // magic + version + nextPlayerId + clubCount
            for (int i = 0; i < clubs.Length; i++)
            {
                size += BytesPerClubHeader;
                ClubCareerStates club = clubs[i];
                for (int p = 0; p < club.Count; p++)
                {
                    size += MinBytesPerPlayer
                          + AsciiLength(club.Records[p].FirstName, club.ClubId, club.Records[p].PlayerId, "first")
                          + AsciiLength(club.Records[p].LastName, club.ClubId, club.Records[p].PlayerId, "last");
                }
            }

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(buf, ref o, PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteI32(buf, ref o, nextPlayerId);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubs.Length);

            for (int ci = 0; ci < clubOrder.Length; ci++)
            {
                ClubCareerStates club = clubs[clubOrder[ci]];
                int[] order = playerOrders[clubOrder[ci]];

                CanonicalSerializer.WriteI32(buf, ref o, club.ClubId);
                CanonicalSerializer.WriteU32(buf, ref o, (uint)club.Count);

                for (int pi = 0; pi < order.Length; pi++)
                {
                    WritePlayer(buf, ref o, club.ClubId, in club.Records[order[pi]], in club.Lifecycles[order[pi]]);
                }
            }

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "ProgressionSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }
            return buf;
        }

        /// <summary>
        /// Decodes a blob produced by <see cref="Encode"/>. Fail-loud (F3/F5) on: a null blob; a missing
        /// or wrong <see cref="PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC"/>; a
        /// <see cref="PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION"/> mismatch (no
        /// cross-version migration at Stage 0); a count prefix the remaining bytes could not back; keys
        /// that are not strictly ascending; an undefined position ordinal; or trailing bytes.
        /// </summary>
        /// <param name="blob">The block's bytes.</param>
        /// <param name="nextPlayerId">The decoded store-level id cursor.</param>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Any gate above trips.</exception>
        public static ClubCareerStates[] Decode(byte[] blob, out int nextPlayerId)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int total = blob.Length;
            int o = 0;

            // Magic BEFORE version: a sibling block reaching this codec must be refused as the wrong
            // FORMAT, not mis-diagnosed as the wrong generation of this one (ERR-028-004).
            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "magic");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    Subject + " magic 0x" + magic.ToString("X8") + " != expected 0x" +
                    PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC.ToString("X8") +
                    " — these bytes are not a #28 career-state block (ERR-028-004).");
            }

            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "format version");
            uint version = CanonicalSerializer.ReadU32(blob, ref o);
            if (version != PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    Subject + " format version " + version + " != expected " +
                    PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (F3).");
            }

            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "nextPlayerId");
            nextPlayerId = CanonicalSerializer.ReadI32(blob, ref o);

            int clubCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, total, BytesPerClubHeader, Subject, "club");

            var clubs = new ClubCareerStates[clubCount];
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubCount; c++)
            {
                SaveBlobFramingHelpers.Require(o, 4, total, Subject, "club id");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(clubId, ref previousClubId, Subject, "club id", c);

                int playerCount = SaveBlobFramingHelpers.ReadCount(
                    blob, ref o, total, MinBytesPerPlayer, Subject, "player in club " + clubId);

                var records = new PlayerRecord[playerCount];
                var lifecycles = new PlayerLifecycle[playerCount];
                long previousPlayerId = long.MinValue;

                for (int p = 0; p < playerCount; p++)
                {
                    ReadPlayer(blob, ref o, total, clubId, p, ref previousPlayerId,
                        out records[p], out lifecycles[p]);
                }

                clubs[c] = new ClubCareerStates(clubId, records, lifecycles);
            }

            if (o != total)
            {
                throw new InvalidOperationException(
                    Subject + " has " + (total - o) + " trailing byte(s) after the declared content " +
                    "— truncated, padded, or corrupt (F5).");
            }

            return clubs;
        }

        // ── Write helpers ─────────────────────────────────────────────────────────────

        private static void WritePlayer(
            byte[] buf, ref int o, int clubId, in PlayerRecord rec, in PlayerLifecycle life)
        {
            if (!Enum.IsDefined(typeof(PlayerPosition), rec.Position))
            {
                throw new InvalidOperationException(
                    "Player " + rec.PlayerId + " in club " + clubId + " carries an undefined " +
                    "PlayerPosition ordinal " + (int)rec.Position + " — Decode refuses it, so writing " +
                    "it would produce a file no load of it could accept.");
            }

            CanonicalSerializer.WriteI32(buf, ref o, rec.PlayerId);
            CanonicalSerializer.WriteString(buf, ref o, rec.FirstName);
            CanonicalSerializer.WriteString(buf, ref o, rec.LastName);
            CanonicalSerializer.WriteI32(buf, ref o, rec.Age);
            CanonicalSerializer.WriteU8(buf, ref o, (byte)(int)rec.Position);

            int[] attrs = rec.Attributes.ToArray();
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, attrs[i]);
            }
            CanonicalSerializer.WriteI32(buf, ref o, rec.Attributes.WeakFootRating);

            CanonicalSerializer.WriteI32(buf, ref o, life.PotentialAbility);
            CanonicalSerializer.WriteI32(buf, ref o, life.CurrentAbility);
            CanonicalSerializer.WriteI64(buf, ref o, life.GrowthCursor);
            CanonicalSerializer.WriteI64(buf, ref o, life.BirthWorldDay);
            CanonicalSerializer.WriteU8(buf, ref o, life.RetirementFlag ? (byte)1 : (byte)0);
            CanonicalSerializer.WriteU32(buf, ref o, life.RetirementDay);
            CanonicalSerializer.WriteU32(buf, ref o, life.LastAdvancedWorldDay);
        }

        // ── Read helpers ──────────────────────────────────────────────────────────────

        private static void ReadPlayer(
            byte[] blob, ref int o, int total, int clubId, int index, ref long previousPlayerId,
            out PlayerRecord rec, out PlayerLifecycle life)
        {
            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "player id");
            int playerId = CanonicalSerializer.ReadI32(blob, ref o);
            SaveBlobFramingHelpers.RequireAscending(
                playerId, ref previousPlayerId, Subject, "player id in club " + clubId, index);

            string firstName = ReadGuardedString(blob, ref o, total, "first name");
            string lastName = ReadGuardedString(blob, ref o, total, "last name");

            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "age");
            int age = CanonicalSerializer.ReadI32(blob, ref o);

            SaveBlobFramingHelpers.Require(o, 1, total, Subject, "position");
            int position = CanonicalSerializer.ReadU8(blob, ref o);
            if (false) // MUTANT: undefined-position decode guard removed
            {
                throw new InvalidOperationException(
                    Subject + " player " + playerId + " in club " + clubId + " carries undefined " +
                    "PlayerPosition ordinal " + position + " — corrupt save.");
            }

            SaveBlobFramingHelpers.Require(o, AttrIdx.Count * 4, total, Subject, "attributes");
            var attrValues = new int[AttrIdx.Count];
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                attrValues[i] = CanonicalSerializer.ReadI32(blob, ref o);
            }

            SaveBlobFramingHelpers.Require(o, 4, total, Subject, "weak-foot rating");
            int weakFoot = CanonicalSerializer.ReadI32(blob, ref o);

            var attributes = new PlayerAttributes();
            attributes.FromArray(attrValues);
            attributes.WeakFootRating = weakFoot;

            rec = new PlayerRecord
            {
                PlayerId = playerId,
                FirstName = firstName,
                LastName = lastName,
                Age = age,
                Position = (PlayerPosition)position,
                Attributes = attributes
            };

            SaveBlobFramingHelpers.Require(o, 4 + 4 + 8 + 8 + 1 + 4 + 4, total, Subject, "lifecycle overlay");
            life = new PlayerLifecycle
            {
                PotentialAbility = CanonicalSerializer.ReadI32(blob, ref o),
                CurrentAbility = CanonicalSerializer.ReadI32(blob, ref o),
                GrowthCursor = (long)CanonicalSerializer.ReadU64(blob, ref o),
                BirthWorldDay = (long)CanonicalSerializer.ReadU64(blob, ref o),
                RetirementFlag = CanonicalSerializer.ReadU8(blob, ref o) != 0,
                RetirementDay = CanonicalSerializer.ReadU32(blob, ref o),
                LastAdvancedWorldDay = CanonicalSerializer.ReadU32(blob, ref o)
            };
        }

        // CanonicalSerializer.ReadString reads its own u32 length and then indexes the buffer without a
        // bound check, so a corrupt length would over-read. This reads the prefix, bounds it against the
        // blob (Require refuses a negative `need`, which is where a length above int.MaxValue lands),
        // and only then decodes.
        private static string ReadGuardedString(byte[] blob, ref int o, int total, string what)
        {
            SaveBlobFramingHelpers.Require(o, 4, total, Subject, what + " length");
            uint length = CanonicalSerializer.ReadU32(blob, ref o);
            SaveBlobFramingHelpers.Require(o, (int)length, total, Subject, what + " body");
            if (length == 0u)
            {
                return string.Empty;
            }
            string s = Encoding.ASCII.GetString(blob, o, (int)length);
            o += (int)length;
            return s;
        }

        // ── Shared helpers ────────────────────────────────────────────────────────────

        private static void RequireBound(in ClubCareerStates club, int index)
        {
            if (club.Records == null || club.Lifecycles == null)
            {
                throw new ArgumentNullException(
                    "clubs",
                    "Club at index " + index + " was never bound — a default(ClubCareerStates) skips " +
                    "the constructor that rejects null arrays.");
            }
        }

        private static int[] ClubIdsOf(ClubCareerStates[] clubs)
        {
            var ids = new int[clubs.Length];
            for (int i = 0; i < clubs.Length; i++)
            {
                ids[i] = clubs[i].ClubId;
            }
            return ids;
        }

        private static int[] PlayerIdsOf(in ClubCareerStates club)
        {
            var ids = new int[club.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = club.Records[i].PlayerId;
            }
            return ids;
        }

        // ASCII is the CanonicalSerializer string contract (§3.2.4.1). A non-ASCII name would be written
        // as '?' and read back different, so it is refused at the write site rather than silently
        // mangled — the never-write-what-Decode-refuses rule, applied to a lossy encode.
        private static int AsciiLength(string value, int clubId, int playerId, string which)
        {
            if (value == null)
            {
                return 0;
            }
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] > 0x7F)
                {
                    throw new InvalidOperationException(
                        "Player " + playerId + " in club " + clubId + " has a non-ASCII " + which +
                        " name — the canonical string encoding is ASCII (#16 §3.2.4.1), so this would " +
                        "round-trip to a different name.");
                }
            }
            return value.Length;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1: the career-state sub-blob codec. Magic-led per         |
// |         |            |        | ERR-028-004 (§3.5 had specified version-first with the RNG     |
// |         |            |        | domain tag as identifier); club id written not implied;        |
// |         |            |        | canonical ascending keys both ways; guarded string read;       |
// |         |            |        | non-ASCII names and undefined positions refused at Encode.     |
#endregion
