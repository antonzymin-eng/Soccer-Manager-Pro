// File:     src/player-progression/ProgressionSaveCodec.cs
// Created:  2026-08-08
// Modified: 2026-08-23 (football-judgment proxy review, batch-1 adversarial finding
//           classifyageband-growth-claim-stale — v1.6)
//           (AR pass 8, M-1/L-4/L-5 — Decode's four shared gates throw
//           InvalidOperationException; CurrentAbility and RetirementDay/Flag gated; the null-name
//           non-idempotency documented — v1.5)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.5 (the save codec), §4.2, KD-4, FR-PG-016/017/018/019,
//           F3/F5; ERR-028-004 (§3.5 named the RNG domain tag as the block's identifier);
//           ERR-028-014 (the never-advanced sentinel retired from #28's legal STORE states — Encode and
//           Decode now refuse it, the ERR-028-011(a) class one ERR later);
//           Deterministic Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the #28 career-state sub-blob — one opaque, independently version-gated
//           block under #30's season save. Encodes the per-club per-player PlayerRecord + PlayerLifecycle
//           map in canonical key order and reads it back, fail-loud on any magic / version / length-bound
//           / ordering / trailing-byte violation. No file I/O (that is SeasonSaveManager), so the codec
//           is exhaustively unit-testable in memory. ReadPlayer's L3 range gates now cover
//           PotentialAbility (the F1 growth ceiling) alongside attributes, weak-foot and age. Encode and
//           Decode both refuse the never-advanced sentinel as a progression cursor (ERR-028-014). Encode
//           and Decode now enforce the SAME four value-range rules through one shared
//           DescribeOutOfRangeValues owner, closing the never-write-what-Decode-refuses gap (AR pass 5).

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
    ///         i32  currentAbility           (derived cache; recompute-equals-stored is gated by
    ///                                        DescribeOutOfRangeValues and locked by test, L-4)
    ///         i64  growthCursor             (the ONLY accumulator, FR-PG-002)
    ///         i64  birthWorldDay            (the authoritative age anchor, FR-PG-005; SIGNED — a
    ///                                        player born before the day-0 epoch is ordinary, ERR-028-006)
    ///         u8   retirementFlag
    ///         u32  retirementDay           (gated against retirementFlag: 0 while unset, at-or-before
    ///                                        lastAdvancedWorldDay once set — L-4)
    ///         u32  lastAdvancedWorldDay    (sentinel uint.MaxValue = never advanced)
    /// </code>
    /// <para>
    /// <b>Known, deliberate non-idempotency: a <c>null</c> name round-trips to <c>""</c> (L-4/L-5, AR
    /// pass 8).</b> <c>CanonicalSerializer.WriteString</c> writes length 0 for a <c>null</c> string and
    /// <c>ReadGuardedString</c> returns <see cref="string.Empty"/> for a zero-length body — the BYTES a
    /// <c>null</c> name and an empty name produce are identical, so this is not a round-trip defect (the
    /// same input always produces the same output) and no sim code reads names, so it has no downstream
    /// effect. Recorded here so the next reader does not re-file it: this is a property of
    /// <c>CanonicalSerializer</c>'s string contract (#16 §3.2.4.1), not of this codec, and is deliberately
    /// left alone rather than "fixed" by, say, refusing a <c>null</c> name at <see cref="Encode"/> — that
    /// would turn a harmless identity collapse into a new fail-loud surface for no behavioural gain.
    /// </para>
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
    /// <b>The never-advanced sentinel is not a legal STORE state.</b> ERR-028-014 retired
    /// <see cref="PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL"/> from the set of values
    /// <see cref="PlayerLifecycle.LastAdvancedWorldDay"/> may carry once a career exists —
    /// <see cref="ProgressionEngine.SeedFrom"/> anchors the cursor at the seed day, so nothing legitimate
    /// writes it, and <see cref="ProgressionEngine.FromBlocks"/> throws on it. Both <see cref="Encode"/>
    /// and <see cref="Decode"/> refuse it too, for the same reason <see cref="Encode"/> and
    /// <see cref="Decode"/> refuse a cross-club duplicate player id (ERR-028-011(a)): a hand-built block
    /// carrying the sentinel would otherwise encode happily into a file its own restore path can never
    /// load, and a hand-edited file carrying it would otherwise reach <see cref="ProgressionEngine.FromBlocks"/>
    /// and be refused there with a less specific message than the codec can give at the boundary where
    /// the bytes are still in hand.
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
        /// share a player id — a duplicate key has no defined winner (FR-PG-019); or a player carries
        /// <see cref="PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL"/> as its progression
        /// cursor — not a legal STORE state (ERR-028-014), so writing it would produce a file
        /// <see cref="ProgressionEngine.FromBlocks"/> refuses to load.</exception>
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

            // Bind-check FIRST (L4): ClubIdsOf reads ClubId off every element, and two
            // `default(ClubCareerStates)` entries both read club id 0 — so keying before this ran
            // reported "Duplicate club id 0" for what is actually an unbound block. Diagnose the real
            // cause.
            for (int i = 0; i < clubs.Length; i++)
            {
                RequireBound(clubs[i], i);
            }

            int[] clubOrder = SaveBlobFramingHelpers.CanonicalOrder(
                ClubIdsOf(clubs), SetName, "club id", "FR-PG-019");

            // Per-club player orders, computed once and reused by the sizing pass and the write pass —
            // two independent orderings of the same data is the parallel-surface defect one file down.
            var playerOrders = new int[clubs.Length][];
            for (int i = 0; i < clubs.Length; i++)
            {
                playerOrders[i] = SaveBlobFramingHelpers.CanonicalOrder(
                    PlayerIdsOf(clubs[i]), SetName, "player id in club " + clubs[i].ClubId, "FR-PG-019");
            }

            // M2: never write what Restore refuses. CanonicalOrder only rejects a duplicate WITHIN a
            // club, but a career needs player ids globally unique across clubs (ERR-041-019), and
            // ProgressionEngine.Restore enforces exactly that — so without this gate Encode produced a
            // file its own restore path rejects. This is the balance-pass AR pass-4 finding recurring
            // in the landing whose own engine guard cites the same ERR.
            RequireGloballyUniquePlayerIds(clubs);

            // Same class, one ERR later (ERR-028-011(a) → ERR-028-014): ERR-028-014 retired the
            // never-advanced sentinel from #28's legal STORE states, and ProgressionEngine.FromBlocks
            // (which Restore calls straight after Decode) throws on it — but neither Encode nor Decode
            // validated LastAdvancedWorldDay at all, so a hand-built block carrying the sentinel encoded
            // happily into a file that could never be loaded. Never write what FromBlocks refuses.
            RequireNoNeverAdvancedSentinel(clubs);

            // Same class again, and the third time it has been the SAME rule: §3.5 states
            // never-write-what-Decode-refuses as a MUST, and Encode enforced four fewer predicates than
            // Decode did. Decode range-gates attributes, weak-foot, negative age and PotentialAbility
            // (the L3 block in ReadPlayer); Encode gated none of them, and ProgressionEngine.FromBlocks
            // gates none either — so a store carrying PotentialAbility = 0 encoded happily through
            // Snapshot() and was then refused by Restore/Load FOREVER. Because #28's block IS the roster
            // (KD-4), that is an unloadable career, failing at the one boundary where nothing can be
            // recovered. The rules now have ONE owner (DescribeOutOfRangeValues) that both sides call,
            // rather than a second hand-copied walk — the parallel-surface trap this codec has already
            // been bitten by twice.
            RequireValuesInRange(clubs);

            // M3: never write a club SquadFor cannot project. PlayerDatabase.Squad's own constructor
            // requires between 1 and CLUB_SQUAD_SIZE players; #28's block IS the roster (KD-4), and
            // FromBlocks/Encode/Decode had no such gate at all — so a store carrying a 0- or 30-player
            // club advanced, saved and loaded cleanly and then threw from SquadFor, mid-round, inside
            // ISquadProvider.ResolveByClubId, after earlier fixtures in that round had already been
            // applied. Never write what SquadFor cannot project.
            RequireClubSizeInRange(clubs);

            // FR-PG-011 at the write boundary too (AR pass 5): Restore is Decode + FromBlocks, so a
            // cursor this side accepted and FromBlocks refuses is a blob that loads never.
            RequireIdCursorAheadOfCarriedIds(clubs, nextPlayerId);

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
        /// that are not strictly ascending; an undefined position ordinal; a player carrying
        /// <see cref="PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL"/> as its progression
        /// cursor — not a legal STORE state (ERR-028-014), refused here rather than left for
        /// <see cref="ProgressionEngine.FromBlocks"/> to reject with a less specific message; or trailing
        /// bytes.
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

            // The load-side mirror of Encode's gate (M2): ascending keys are checked per club, which
            // cannot see a collision ACROSS clubs. A hand-edited file carrying one would otherwise
            // reach ProgressionEngine.Restore and be refused there with a far less specific message —
            // or, on the FromBlocks route, not at all.
            //
            // M-1 (AR pass 8): Decode owes InvalidOperationException ("this file is corrupt"), never
            // the ArgumentException the shared Require* wrappers below throw for Encode's ("this
            // argument is invalid") — so Decode calls the Describe* half directly and throws its own
            // type here, the same split DescribeOutOfRangeValues already uses. Before this fix all four
            // of these gates threw ArgumentException(nameof(clubs)/nameof(nextPlayerId)) even from this
            // Decode path, naming arguments Decode's own signature never has — contradicting this
            // method's own <exception> doc, DescribeOutOfRangeValues' documented rationale, and #28
            // §3.5 (ERR-029-004/ERR-041-009's MUST: decode value gates throw InvalidOperationException,
            // matching the framing gates' type).
            string globalDupViolation = DescribeGlobalDuplicatePlayerId(clubs);
            if (globalDupViolation != null)
            {
                throw new InvalidOperationException(Subject + " " + globalDupViolation);
            }

            // The load-side mirror of Encode's sentinel gate: a hand-edited file carrying the
            // never-advanced sentinel would otherwise reach ProgressionEngine.FromBlocks (via Restore)
            // and be refused there with a message about the wrong subsystem's contract, not this
            // codec's own (ERR-028-014).
            string sentinelViolation = DescribeNeverAdvancedSentinel(clubs);
            if (sentinelViolation != null)
            {
                throw new InvalidOperationException(Subject + " " + sentinelViolation);
            }

            // The load-side mirror of Encode's M3 gate: a hand-edited file carrying a club SquadFor
            // cannot project would otherwise decode cleanly and only throw later, mid-round, inside
            // ISquadProvider.ResolveByClubId.
            string clubSizeViolation = DescribeClubSizeOutOfRange(clubs);
            if (clubSizeViolation != null)
            {
                throw new InvalidOperationException(Subject + " " + clubSizeViolation);
            }

            // Same mirror for FR-PG-011 (AR pass 5). Refusing here means a corrupt cursor is caught at
            // the boundary where the bytes are still in hand, rather than by FromBlocks one call later.
            string cursorViolation = DescribeIdCursorNotAheadOfCarriedIds(clubs, nextPlayerId);
            if (cursorViolation != null)
            {
                throw new InvalidOperationException(Subject + " " + cursorViolation);
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
            if (!Enum.IsDefined(typeof(PlayerPosition), position))
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

            // L3: range-gate what comes back. This block is the ROSTER now (KD-4), so a corrupt or
            // hand-edited attribute flows straight into PlayerAttributeProjection and the match engine
            // — an attribute of 9999 or a negative age would simply be played. The sibling career
            // codecs gate their own value contracts for the same reason; #28's stakes are higher
            // because nothing downstream re-derives these from a trusted source.
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

            // L3 (continued): PotentialAbility is the F1 growth ceiling — AbilityModel.TrySpendOnePoint
            // returns false once CurrentAbility >= PotentialAbility, so a corrupt or hand-edited value
            // below the floor would silently freeze this player's growth forever, and one above the
            // ceiling would silently unbound it. The neighbouring L3 gates above cover attributes,
            // weak-foot and age but read off `rec`; this one reads off `life`, which is only fully read
            // at this point, so it necessarily sits after `rec` is built rather than before it (unlike
            // its neighbours) — the check itself is otherwise the same shape.
            // All four L3 value gates now run from the ONE owner, here rather than inline above, because
            // `life` is only fully read at this point and the PA gate needs it. The gates are therefore
            // evaluated after `rec`/`life` are built rather than during the read — which changes nothing
            // observable (the values are identical, and nothing between the read and here can act on a
            // bad one), and buys the property that Encode cannot drift from Decode: reverting either
            // side's call leaves the other enforcing the same rule from the same code.
            string violation = DescribeOutOfRangeValues(rec, life, clubId);
            if (violation != null)
            {
                throw new InvalidOperationException(Subject + " " + violation);
            }
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

        // M-1 (AR pass 8): this rule is shared by Encode/FromBlocks (ArgumentException-owing bad-argument
        // boundaries) and Decode (an InvalidOperationException-owing corrupt-file boundary) — the same
        // split DescribeOutOfRangeValues below already solves. Describe returns the violation text with
        // no throw; each boundary's own thin wrapper (or, for Decode, its own inline call in Decode
        // above) picks the exception type it owes.
        //
        // The cross-club uniqueness rule, shared by Encode and Decode so the two boundaries cannot
        // drift (ERR-041-019 / ERR-027-004). ProgressionEngine enforces the same rule at its own entry
        // points; this is the codec's half, so neither a written file nor a read one can carry the
        // collision.
        internal static string DescribeGlobalDuplicatePlayerId(ClubCareerStates[] clubs)
        {
            var seen = new System.Collections.Generic.Dictionary<int, int>();
            for (int c = 0; c < clubs.Length; c++)
            {
                for (int p = 0; p < clubs[c].Count; p++)
                {
                    int id = clubs[c].Records[p].PlayerId;
                    if (seen.TryGetValue(id, out int owner))
                    {
                        return "player id " + id + " appears in both club " + owner + " and club " +
                            clubs[c].ClubId + ". A career requires GLOBALLY unique player ids, not " +
                            "merely club-scoped ones (#27 FR-SQ-010 / ERR-041-019) — duplicates would " +
                            "share career and injury state silently.";
                    }
                    seen.Add(id, clubs[c].ClubId);
                }
            }
            return null;
        }

        /// <summary>Encode's half — throws <see cref="ArgumentException"/> (a bad argument). Decode
        /// calls <see cref="DescribeGlobalDuplicatePlayerId"/> directly and throws
        /// <see cref="InvalidOperationException"/> instead (M-1, AR pass 8).</summary>
        internal static void RequireGloballyUniquePlayerIds(ClubCareerStates[] clubs)
        {
            string violation = DescribeGlobalDuplicatePlayerId(clubs);
            if (violation != null)
            {
                throw new ArgumentException(violation, nameof(clubs));
            }
        }

        // The never-advanced-sentinel rule, shared by Encode and Decode for the same reason
        // RequireGloballyUniquePlayerIds is above it — the ERR-028-011(a) class, one ERR later:
        // ProgressionEngine.FromBlocks refuses PROGRESSION_NOT_ADVANCED_SENTINEL as a STORE state
        // (ERR-028-014 — SeedFrom is the only legal way to anchor a career's cursor), so this is the
        // codec's own half of never-write-what-load-refuses. Same M-1 Describe/Require split as above.
        internal static string DescribeNeverAdvancedSentinel(ClubCareerStates[] clubs)
        {
            for (int c = 0; c < clubs.Length; c++)
            {
                for (int p = 0; p < clubs[c].Count; p++)
                {
                    if (clubs[c].Lifecycles[p].LastAdvancedWorldDay
                        == PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
                    {
                        return "player " + clubs[c].Records[p].PlayerId + " in club " + clubs[c].ClubId +
                            " carries the never-advanced sentinel as its progression cursor. The " +
                            "sentinel is not a legal STORE state (ERR-028-014) — a career's lived " +
                            "history has to start somewhere it can be checked against the world " +
                            "clock; seed through ProgressionEngine.SeedFrom, which anchors it.";
                    }
                }
            }
            return null;
        }

        /// <summary>Encode's half — throws <see cref="ArgumentException"/> (a bad argument). Decode
        /// calls <see cref="DescribeNeverAdvancedSentinel"/> directly and throws
        /// <see cref="InvalidOperationException"/> instead (M-1, AR pass 8).</summary>
        internal static void RequireNoNeverAdvancedSentinel(ClubCareerStates[] clubs)
        {
            string violation = DescribeNeverAdvancedSentinel(clubs);
            if (violation != null)
            {
                throw new ArgumentException(violation, nameof(clubs));
            }
        }

        /// <summary>
        /// The SINGLE owner of #28's per-player value ranges. Returns <c>null</c> when every value is
        /// in range, otherwise the violation text (already phrased to follow a subject prefix).
        /// <para>
        /// This returns a message rather than throwing because the two callers owe DIFFERENT exception
        /// contracts — <see cref="Decode"/> throws <see cref="InvalidOperationException"/> ("this file is
        /// corrupt"), <see cref="Encode"/> throws <see cref="ArgumentException"/> on <c>clubs</c> ("this
        /// argument is invalid") — and collapsing them would either break the decode locks that assert
        /// the type or mislabel a caller's bad argument as a corrupt file. What must not be duplicated is
        /// the RULE, and it is not: both sides call this.
        /// </para>
        /// </summary>
        internal static string DescribeOutOfRangeValues(in PlayerRecord rec, in PlayerLifecycle life, int clubId)
        {
            int[] attrValues = rec.Attributes.ToArray();
            for (int i = 0; i < attrValues.Length; i++)
            {
                if (attrValues[i] < PlayerProgressionConstants.ATTRIBUTE_MIN
                    || attrValues[i] > PlayerProgressionConstants.ATTRIBUTE_MAX)
                {
                    return "player " + rec.PlayerId + " in club " + clubId + " carries attribute " +
                        i + " = " + attrValues[i] + ", outside [" +
                        PlayerProgressionConstants.ATTRIBUTE_MIN + ", " +
                        PlayerProgressionConstants.ATTRIBUTE_MAX + "] — corrupt save.";
                }
            }

            if (rec.Attributes.WeakFootRating < PlayerDatabaseConstants.WEAK_FOOT_MIN
                || rec.Attributes.WeakFootRating > PlayerDatabaseConstants.WEAK_FOOT_MAX)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries weak-foot " +
                    rec.Attributes.WeakFootRating + ", outside [" + PlayerDatabaseConstants.WEAK_FOOT_MIN +
                    ", " + PlayerDatabaseConstants.WEAK_FOOT_MAX + "] — corrupt save.";
            }

            if (rec.Age < 0)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries age " + rec.Age +
                    " — negative ages are not representable in the model (corrupt save). Note the age " +
                    "field is a DERIVED cache; the authoritative anchor is birthWorldDay, which may " +
                    "legitimately be negative (ERR-028-006).";
            }

            if (life.PotentialAbility < PlayerProgressionConstants.PA_MIN
                || life.PotentialAbility > PlayerProgressionConstants.ABILITY_MAX)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries potentialAbility " +
                    life.PotentialAbility + ", outside [" + PlayerProgressionConstants.PA_MIN + ", " +
                    PlayerProgressionConstants.ABILITY_MAX + "] — corrupt save.";
            }

            // AR pass 5 (recorded), fixed here. BirthWorldDay was the ONLY lifecycle field with no gate,
            // and it is the AUTHORITATIVE age anchor — every other age value in the model is a derived
            // cache of it. An anchor far below the floor narrowed the derived age to int.MinValue, which
            // — at the time this gate was written — ClassifyAgeBand read as Growth under the retired
            // age-only band step (so the player grew forever and retirement never fired), and which this
            // very method then refuses as a negative age — making a career that loaded, advanced and
            // projected fine PERMANENTLY unsavable. The upper bound is the world clock's own ceiling: a
            // player born on the current day is age 0, which is ordinary, but an anchor beyond
            // uint.MaxValue cannot correspond to any reachable world day.
            // **Corrected (classifyageband-growth-claim-stale, football-judgment proxy review batch-1):**
            // ClassifyAgeBand no longer reads int.MinValue as Growth — since ERR-028-020 it reads the
            // continuous accrual curve, and both AccruedBandPoints cumulatives are 0 at a hugely negative
            // age, so it now returns Stable. The int-narrowing this gate guards against is unchanged;
            // only that one downstream symptom is history rather than present behaviour.
            // AR pass 6 (High), the structural half being the loop exits in GrowthProjection. Pass 5
            // called BirthWorldDay "the ONLY lifecycle field with no range gate"; that claim was
            // checkable and false — GrowthCursor had none either, and it is the one accumulator every
            // attribute change flows through. Out of range it did not corrupt data, it WEDGED the day
            // step: the drain loop ground upward one POINT_COST at a time with no failure exit, from a
            // save file that round-tripped byte-exact.
            //
            // The legal band needs no [GT] judgement — it is derivable. Both loops leave
            // |GrowthCursor| <= POINT_COST - 1 after any completed step, and SeedLifecycle writes ±1, so
            // |GrowthCursor| < POINT_COST is exactly the serialized invariant.
            if (life.GrowthCursor <= -PlayerProgressionConstants.POINT_COST
                || life.GrowthCursor >= PlayerProgressionConstants.POINT_COST)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries growthCursor " +
                    life.GrowthCursor + ", outside (" + (-PlayerProgressionConstants.POINT_COST) + ", " +
                    PlayerProgressionConstants.POINT_COST + ") — a completed day step always leaves the " +
                    "cursor below one whole point (corrupt save).";
            }

            long minBirthWorldDay =
                -(long)PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                * PlayerProgressionConstants.DAYS_PER_YEAR;
            if (life.BirthWorldDay < minBirthWorldDay || life.BirthWorldDay > uint.MaxValue)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries birthWorldDay " +
                    life.BirthWorldDay + ", outside [" + minBirthWorldDay + ", " + uint.MaxValue +
                    "] — the anchor must derive an age the model can represent (corrupt save).";
            }

            // L-4 (AR pass 8): CurrentAbility is a DERIVED cache (FR-PG-003) — it self-heals on the
            // NEXT day step, but post-AR-pass-6 a stale restored value is read by the spend loop BEFORE
            // that step's recompute, so the first threshold crossing is refused against the stale value
            // and the fraction is DISCARDED (pass 6 changed the refusal branch's accrual from retain to
            // discard) — one whole [1,20] point lost PERMANENTLY per corrupt restore, where before it
            // was only a delay. The layout comment above (`i32 currentAbility`) claimed "recompute-
            // equals-stored is locked"; no such lock existed until this gate.
            //
            // Guarded on position validity FIRST: AbilityModel.ComputeCA indexes PositionAttributeBias
            // by the raw position ordinal with no bounds check, so calling it against an undefined
            // ordinal throws IndexOutOfRangeException — proven by mutation-run probe (an Encode call
            // carrying PlayerPosition 99 crashed here, ahead of WritePlayer's own Enum.IsDefined gate,
            // which is Encode's existing, more specific refusal for exactly this state and must be the
            // one that fires). An undefined position is not this predicate's problem to diagnose; skip
            // the CA check and let the boundary's own position gate (WritePlayer / ReadPlayer) refuse it.
            if (Enum.IsDefined(typeof(PlayerPosition), rec.Position))
            {
                int computedCurrentAbility = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);
                if (life.CurrentAbility != computedCurrentAbility)
                {
                    return "player " + rec.PlayerId + " in club " + clubId + " carries currentAbility " +
                        life.CurrentAbility + " but its attributes recompute to " + computedCurrentAbility +
                        " — CurrentAbility is a DERIVED cache (FR-PG-003) and must equal " +
                        "ComputeCA(attributes) exactly; a stale mismatch costs the spend loop a whole " +
                        "point permanently at the next threshold crossing, not just a delay (corrupt " +
                        "save).";
                }
            }

            // L-4 (continued): RetirementFlag is sticky — set once, at the world day it fires, and
            // never cleared (§3.4) — so RetirementDay's own legal range is a function of the flag, not
            // an independent field. Unset must carry 0 (nothing has fired); set must carry a day at or
            // before LastAdvancedWorldDay (it cannot fire on a day this player was never advanced to).
            if (!life.RetirementFlag)
            {
                if (life.RetirementDay != 0)
                {
                    return "player " + rec.PlayerId + " in club " + clubId + " carries retirementDay " +
                        life.RetirementDay + " with retirementFlag false — an unset flag must carry " +
                        "retirementDay 0 (corrupt save).";
                }
            }
            else if (life.RetirementDay > life.LastAdvancedWorldDay)
            {
                return "player " + rec.PlayerId + " in club " + clubId + " carries retirementDay " +
                    life.RetirementDay + " ahead of lastAdvancedWorldDay " + life.LastAdvancedWorldDay +
                    " — retirementFlag is only ever SET (sticky), at the world day it fires, so it can " +
                    "never exceed the day this player was last advanced to (corrupt save).";
            }

            return null;
        }

        /// <summary>
        /// FR-PG-011: the id cursor must exceed every carried <c>PlayerId</c>, or the next allocation
        /// collides with a live player and one player ends up with two careers.
        /// <para>
        /// This lived ONLY in <see cref="ProgressionEngine.FromBlocks"/>, so both codec sides admitted a
        /// bad cursor — and since <c>Restore</c> is <c>Decode</c> + <c>FromBlocks</c>, <see cref="Encode"/>
        /// could write a blob that loads NEVER. Probe-verified before the fix: encoding a club carrying
        /// players {10, 11} with <c>nextPlayerId: 0</c> produced a blob that decoded cleanly and whose
        /// <c>Restore</c> then threw forever. The codec's own round-trip fixture satisfied the rule only
        /// by coincidence (cursor 12 against a max id of 11) and nothing asserted that it must.
        /// </para>
        /// M-1 (AR pass 8): returns the violation text rather than throwing, the same split
        /// <see cref="DescribeOutOfRangeValues"/> uses — Encode/<see cref="ProgressionEngine.FromBlocks"/>
        /// owe <see cref="ArgumentException"/> (a bad argument); Decode owes
        /// <see cref="InvalidOperationException"/> (a corrupt file) and calls this directly.
        /// </summary>
        internal static string DescribeIdCursorNotAheadOfCarriedIds(ClubCareerStates[] clubs, int nextPlayerId)
        {
            int highest = int.MinValue;
            for (int c = 0; c < clubs.Length; c++)
            {
                for (int p = 0; p < clubs[c].Count; p++)
                {
                    if (clubs[c].Records[p].PlayerId > highest)
                    {
                        highest = clubs[c].Records[p].PlayerId;
                    }
                }
            }

            if (highest != int.MinValue && nextPlayerId <= highest)
            {
                return "the id cursor is " + nextPlayerId + " but this career already carries player " +
                    highest + ". The next allocated id would collide with a live player, which is " +
                    "exactly what serializing the cursor exists to prevent (FR-PG-011).";
            }
            return null;
        }

        /// <summary>Encode's / <see cref="ProgressionEngine.FromBlocks"/>'s half — throws
        /// <see cref="ArgumentException"/> (a bad argument). See
        /// <see cref="DescribeIdCursorNotAheadOfCarriedIds"/> for the rule.</summary>
        internal static void RequireIdCursorAheadOfCarriedIds(ClubCareerStates[] clubs, int nextPlayerId)
        {
            string violation = DescribeIdCursorNotAheadOfCarriedIds(clubs, nextPlayerId);
            if (violation != null)
            {
                throw new ArgumentException(violation, nameof(nextPlayerId));
            }
        }

        /// <summary>
        /// Encode's half of never-write-what-Decode-refuses for the four value ranges. See the call site
        /// in <see cref="Encode"/> for why this was missing and what it cost.
        /// </summary>
        internal static void RequireValuesInRange(ClubCareerStates[] clubs)
        {
            for (int c = 0; c < clubs.Length; c++)
            {
                for (int p = 0; p < clubs[c].Count; p++)
                {
                    string violation = DescribeOutOfRangeValues(
                        clubs[c].Records[p], clubs[c].Lifecycles[p], clubs[c].ClubId);
                    if (violation != null)
                    {
                        throw new ArgumentException(
                            "Refusing to write a block Decode would refuse to read back: " + violation,
                            nameof(clubs));
                    }
                }
            }
        }

        /// <summary>
        /// M3: the per-club roster-size rule, shared by <see cref="Encode"/>, <see cref="Decode"/> and
        /// <see cref="ProgressionEngine.FromBlocks"/> — exactly the pattern
        /// <see cref="RequireValuesInRange"/> and <see cref="RequireIdCursorAheadOfCarriedIds"/> already
        /// follow. <see cref="PlayerDatabase.Squad"/>'s own constructor requires between 1 and
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/> players, and #28's block IS the roster
        /// (KD-4) — <see cref="ProgressionEngine.SquadFor"/> builds a <c>Squad</c> straight off it — so a
        /// club outside that range is state every one of the three boundaries must refuse, not just the
        /// one that happens to call <c>SquadFor</c> next.
        /// <para>
        /// M-1 (AR pass 8): returns the violation text rather than throwing, the same split
        /// <see cref="DescribeOutOfRangeValues"/> uses — Encode/<see cref="ProgressionEngine.FromBlocks"/>
        /// owe <see cref="ArgumentException"/> (a bad argument); Decode owes
        /// <see cref="InvalidOperationException"/> (a corrupt file) and calls this directly.
        /// </para>
        /// </summary>
        internal static string DescribeClubSizeOutOfRange(ClubCareerStates[] clubs)
        {
            for (int c = 0; c < clubs.Length; c++)
            {
                int count = clubs[c].Count;
                if (count < 1 || count > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
                {
                    return "club " + clubs[c].ClubId + " carries " + count + " player(s), outside [1, " +
                        PlayerDatabaseConstants.CLUB_SQUAD_SIZE + "] — PlayerDatabase.Squad's own " +
                        "constructor bound (M3). #28's block IS the roster (KD-4), so a club outside " +
                        "this range would encode/decode cleanly and only throw later, mid-round, " +
                        "inside ISquadProvider.ResolveByClubId, after earlier fixtures in that round " +
                        "had already been applied to the table.";
                }
            }
            return null;
        }

        /// <summary>Encode's / <see cref="ProgressionEngine.FromBlocks"/>'s half — throws
        /// <see cref="ArgumentException"/> (a bad argument). See
        /// <see cref="DescribeClubSizeOutOfRange"/> for the rule.</summary>
        internal static void RequireClubSizeInRange(ClubCareerStates[] clubs)
        {
            string violation = DescribeClubSizeOutOfRange(clubs);
            if (violation != null)
            {
                throw new ArgumentException(violation, nameof(clubs));
            }
        }

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
// | 1.1     | 2026-08-09 | —      | ReadPlayer gains an L3 range gate for PotentialAbility against |
// |         |            |        | [PA_MIN, ABILITY_MAX] — the F1 growth ceiling had no gate      |
// |         |            |        | alongside the neighbouring attribute / weak-foot / age gates,  |
// |         |            |        | so a corrupt negative value would silently freeze a player's   |
// |         |            |        | growth forever (AbilityModel.TrySpendOnePoint returns false    |
// |         |            |        | once CurrentAbility >= PotentialAbility). Placed after `life`  |
// |         |            |        | is read, unlike its neighbours, since PA lives on the          |
// |         |            |        | lifecycle overlay rather than the record.                       |
// | 1.2     | 2026-08-10 | —      | New RequireNoNeverAdvancedSentinel guard, called from both     |
// |         |            |        | Encode and Decode (ERR-028-014, the ERR-028-011(a) class one   |
// |         |            |        | ERR later): ERR-028-014 retired the never-advanced sentinel    |
// |         |            |        | from #28's legal STORE states and ProgressionEngine.FromBlocks |
// |         |            |        | refuses it, but neither Encode nor Decode validated it — a     |
// |         |            |        | hand-built block carrying the sentinel encoded happily into a  |
// |         |            |        | file that could never be loaded. Proven by mutation: deleting  |
// |         |            |        | either call site left its new lock the only failure.           |
// | 1.3     | 2026-08-10 | —      | AR pass 5 (High): Encode enforced FOUR fewer predicates than    |
// |         |            |        | Decode, breaking §3.5's never-write-what-Decode-refuses MUST —  |
// |         |            |        | Decode range-gates attributes, weak-foot, negative age and      |
// |         |            |        | PotentialAbility; neither Encode nor FromBlocks gated any of    |
// |         |            |        | them, so a store carrying e.g. PotentialAbility = 0 encoded     |
// |         |            |        | happily and was then refused by Restore/Load forever — an       |
// |         |            |        | unloadable career, since #28's block IS the roster (KD-4). New  |
// |         |            |        | DescribeOutOfRangeValues(in PlayerRecord, in PlayerLifecycle,    |
// |         |            |        | int clubId) is the ONE owner of the four range rules, called    |
// |         |            |        | from the new RequireValuesInRange(clubs) on Encode and from     |
// |         |            |        | ReadPlayer on Decode; the two sides keep DIFFERENT exception    |
// |         |            |        | types deliberately (ArgumentException on clubs for a bad        |
// |         |            |        | argument; InvalidOperationException for a corrupt file) — what  |
// |         |            |        | is shared is the rule, not the throw. Mutation-verified:         |
// |         |            |        | reverting the Encode call fails 6 tests.                         |
// | 1.4     | 2026-08-11 | —      | AR pass 6, M3. New RequireClubSizeInRange(clubs) — the per-club   |
// |         |            |        | roster-size rule PlayerDatabase.Squad's own constructor enforces  |
// |         |            |        | but neither Encode, Decode nor FromBlocks did — called from both   |
// |         |            |        | Encode and Decode (FromBlocks' call is ProgressionEngine.cs v1.5).  |
// |         |            |        | A 0- or 30-player club previously encoded/decoded cleanly and only |
// |         |            |        | threw from SquadFor mid-round. Same exception-type split as the    |
// |         |            |        | other shared boundary rules (ArgumentException from both sides,    |
// |         |            |        | matching RequireGloballyUniquePlayerIds / RequireIdCursorAhead-    |
// |         |            |        | CarriedIds, not the DescribeOutOfRangeValues split). Mutation-      |
// |         |            |        | verified: reverting either call site fails the new locks.          |
// | 1.5     | 2026-08-11 | —      | AR pass 8. THREE fixes. **M-1:** RequireGloballyUniquePlayerIds,     |
// |         |            |        | RequireNoNeverAdvancedSentinel, RequireClubSizeInRange and            |
// |         |            |        | RequireIdCursorAheadOfCarriedIds each split into a Describe*          |
// |         |            |        | (returns the violation text, throws nothing) + the existing           |
// |         |            |        | Require* wrapper (ArgumentException, for Encode/FromBlocks) — the     |
// |         |            |        | same pattern DescribeOutOfRangeValues already used. Decode now calls  |
// |         |            |        | each Describe* directly and throws InvalidOperationException, since   |
// |         |            |        | before this fix all four threw ArgumentException(nameof(clubs)/       |
// |         |            |        | nameof(nextPlayerId)) even from Decode, naming arguments Decode's own |
// |         |            |        | signature never has — contradicting Decode's own <exception> doc,     |
// |         |            |        | DescribeOutOfRangeValues' documented rationale, and #28 §3.5           |
// |         |            |        | (ERR-029-004/ERR-041-009's MUST). Mutation-verified: reverting the     |
// |         |            |        | four Decode call sites to the old Require* calls fails exactly the     |
// |         |            |        | three retyped locks in ProgressionSaveCodecTests.cs (Decode_           |
// |         |            |        | AClubWithNoPlayers_FailsLoud, Decode_CrossClubDuplicatePlayerId_       |
// |         |            |        | FailsLoud, Decode_NeverAdvancedSentinel_FailsLoud_ERR028014).          |
// |         |            |        | **L-4:** DescribeOutOfRangeValues gains two more gates — CurrentAbility|
// |         |            |        | must equal ComputeCA(attributes) exactly (guarded on position         |
// |         |            |        | validity first: ComputeCA indexes PositionAttributeBias by the raw    |
// |         |            |        | ordinal with no bounds check and would otherwise throw                |
// |         |            |        | IndexOutOfRangeException for an undefined position, ahead of          |
// |         |            |        | WritePlayer's own, more specific Enum.IsDefined refusal — probe-      |
// |         |            |        | verified by mutation), and the RetirementDay/RetirementFlag pair      |
// |         |            |        | (unset ⇒ day == 0; set ⇒ day <= LastAdvancedWorldDay, since the flag   |
// |         |            |        | is sticky and only ever set at the day it fires). The layout           |
// |         |            |        | comment's "recompute-equals-stored is locked" claim was false before  |
// |         |            |        | this gate; corrected to describe what is now actually enforced and    |
// |         |            |        | locked. Mutation-verified: reverting the two new checks fails exactly |
// |         |            |        | the three new L-4 locks (Decode_CurrentAbilityDoesNotMatchRecomputed- |
// |         |            |        | Value_FailsLoud, Decode_RetirementDayNonZeroWithFlagUnset_FailsLoud,  |
// |         |            |        | Decode_RetirementDayAheadOfLastAdvancedWorldDay_FailsLoud); reverting |
// |         |            |        | the position-validity guard alone fails the pre-existing Encode_      |
// |         |            |        | UndefinedPosition_FailsLoud lock (probe-verified, no new test         |
// |         |            |        | needed). **L-5:** documented the null-name → "" round-trip as a       |
// |         |            |        | known, deliberate non-idempotency (CanonicalSerializer's own string   |
// |         |            |        | contract — bytes are identical either way, no sim code reads names,   |
// |         |            |        | codec unchanged).                                                     |
// | 1.6     | 2026-08-23 | —      | Football-judgment proxy review, batch-1 adversarial finding            |
// |         |            |        | classifyageband-growth-claim-stale. The DescribeOutOfRangeValues       |
// |         |            |        | comment for BirthWorldDay's lower bound said ClassifyAgeBand reads     |
// |         |            |        | int.MinValue as Growth — true when written, false today: since        |
// |         |            |        | ERR-028-020 ClassifyAgeBand reads the continuous accrual curve and     |
// |         |            |        | returns Stable at a hugely negative age (both AccruedBandPoints        |
// |         |            |        | cumulatives are 0 there). Annotated as history; the int-narrowing      |
// |         |            |        | concern the gate exists for is unchanged. No code change.              |
#endregion
