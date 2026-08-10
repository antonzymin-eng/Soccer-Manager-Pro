// File:     src/player-progression/tests/ProgressionSaveCodecTests.cs
// Created:  2026-08-08
// Modified: 2026-08-10
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.5 (the save codec), §4.2, KD-4,
//           FR-PG-016/017/018/019, F3/F5; ERR-028-004 (§3.5 named the RNG domain tag as the block's
//           identifier — the sibling-confusion lock); ERR-028-014 (the never-advanced sentinel retired
//           from #28's legal STORE states — Encode and Decode now refuse it); Deterministic
//           Simulation #16 §3.2.4.1 (CanonicalSerializer); Code Standards #20
// Purpose:  M4 — ProgressionSaveCodec's own dedicated suite (mirrors MedicalSaveCodecTests' house
//           pattern): the round-trip field-identity FR-PG-018 demands, the canonical key order that
//           makes the bytes deterministic, every fail-loud gate (magic, version, bounds, ordering,
//           undefined position, non-ASCII names, trailing bytes), and the ERR-028-004 sibling-confusion
//           proof against a hand-built #29 training block (player-progression may not reference
//           training-system, so the block is hand-built rather than encoded through TrainingSaveCodec).
//           v1.1 adds mutation-audit locks for six ReadPlayer/RequireGloballyUniquePlayerIds guards a
//           mutation sweep proved dead, plus the new PotentialAbility L3 gate (ProgressionSaveCodec.cs
//           §ReadPlayer) and its own lock — PA is the F1 growth ceiling and had no range gate before
//           this pass. v1.2 locks the new RequireNoNeverAdvancedSentinel guards at Encode and Decode
//           (ERR-028-014).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    /// <summary>
    /// Tests for <see cref="ProgressionSaveCodec"/>. The central property is FR-PG-018: every
    /// <see cref="PlayerRecord"/> / <see cref="PlayerLifecycle"/> field round-trips identically, keyed
    /// by <c>(ClubId, PlayerId)</c> — serialize, don't regenerate. The rest is the fail-loud surface
    /// (F3/F5), the canonical-order guarantee, and the ERR-028-004 magic-led sibling-confusion proof.
    /// </summary>
    [TestFixture]
    public sealed class ProgressionSaveCodecTests
    {
        // TrainingSystemConstants.TRAINING_SAVE_MAGIC ('T''R''N''G'). player-progression cannot
        // reference training-system (the reference-direction rule), so the sibling-confusion test below
        // hand-builds a plausible #29-shaped blob rather than encoding a real one through
        // TrainingSaveCodec.
        private const uint TrainingSaveMagic = 0x54524E47;

        private static PlayerRecord Rec(
            int id, string first, string last, int age, PlayerPosition pos, int weakFoot)
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(id);
            rec.FirstName = first;
            rec.LastName = last;
            rec.Age = age;
            rec.Position = pos;
            PlayerAttributes attrs = rec.Attributes;
            attrs.WeakFootRating = weakFoot;
            rec.Attributes = attrs;
            return rec;
        }

        private static PlayerLifecycle Life(
            int potential, int current, long cursor, long birthDay,
            bool retired, uint retiredDay, uint lastAdvanced) =>
            new PlayerLifecycle
            {
                PotentialAbility = potential,
                CurrentAbility = current,
                GrowthCursor = cursor,
                BirthWorldDay = birthDay,
                RetirementFlag = retired,
                RetirementDay = retiredDay,
                LastAdvancedWorldDay = lastAdvanced,
            };

        private static ClubCareerStates Club(
            int clubId, params (PlayerRecord rec, PlayerLifecycle life)[] players)
        {
            var records = new PlayerRecord[players.Length];
            var lifecycles = new PlayerLifecycle[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                records[i] = players[i].rec;
                lifecycles[i] = players[i].life;
            }
            return new ClubCareerStates(clubId, records, lifecycles);
        }

        // Deliberately out of ascending order (club 2 before 1; within club 2, player 11 before 10) —
        // Encode must canonicalize regardless of the order it is handed.
        //
        // Player 10's LastAdvancedWorldDay used to BE PROGRESSION_NOT_ADVANCED_SENTINEL, to prove the
        // sentinel round-trips as itself. ERR-028-014 retired the sentinel from #28's legal STORE
        // states, and Encode now refuses it (Encode_NeverAdvancedSentinel_FailsLoud_ERR028014 below) —
        // so a fixture every round-trip/fail-loud test shares can no longer carry it. Player 11's
        // uint.MaxValue - 1 already covers "a near-maximum value round-trips correctly"; 12345u here
        // is just an ordinary mid-range value, distinct from the other three players' cursors.
        private static ClubCareerStates[] TwoClubs() => new[]
        {
            Club(2,
                (Rec(11, "Bruno", "Silva", 29, PlayerPosition.Forward, 3),
                 Life(8200, 7400, -120L, -50000L, retired: true, retiredDay: 900u, lastAdvanced: 900u)),
                (Rec(10, "Aidan", "Cole", 19, PlayerPosition.Defender, 1),
                 Life(5000, 3100, 0L, 30000L, retired: false, retiredDay: 0u, lastAdvanced: 12345u))),
            Club(1,
                (Rec(7, "Marco", "Diaz", 33, PlayerPosition.Midfielder, 4),
                 Life(6000, 5900, 200L, -80000L, retired: false, retiredDay: 0u, lastAdvanced: 0u)),
                (Rec(5, "Femi", "Adeyemi", 21, PlayerPosition.Goalkeeper, 2),
                 Life(7000, 4200, -364L, 10000L, retired: false, retiredDay: 0u,
                      lastAdvanced: uint.MaxValue - 1))),
        };

        // ── Round-trip ──────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_EveryFieldSurvives_FRPG018()
        {
            const int NextPlayerId = 12;
            ClubCareerStates[] got = ProgressionSaveCodec.Decode(
                ProgressionSaveCodec.Encode(TwoClubs(), NextPlayerId), out int gotNextPlayerId);

            Assert.AreEqual(NextPlayerId, gotNextPlayerId, "the store-level id cursor must survive (FR-PG-011).");
            Assert.AreEqual(2, got.Length, "both clubs must survive");
            Assert.AreEqual(1, got[0].ClubId, "clubs come back in ascending club id");
            Assert.AreEqual(2, got[1].ClubId);

            // Club 1: players ascend 5, 7.
            Assert.AreEqual(5, got[0].Records[0].PlayerId);
            Assert.AreEqual("Femi", got[0].Records[0].FirstName);
            Assert.AreEqual("Adeyemi", got[0].Records[0].LastName);
            Assert.AreEqual(21, got[0].Records[0].Age);
            Assert.AreEqual(PlayerPosition.Goalkeeper, got[0].Records[0].Position);
            Assert.AreEqual(2, got[0].Records[0].Attributes.WeakFootRating);
            Assert.AreEqual(7000, got[0].Lifecycles[0].PotentialAbility);
            Assert.AreEqual(4200, got[0].Lifecycles[0].CurrentAbility);
            Assert.AreEqual(-364L, got[0].Lifecycles[0].GrowthCursor);
            Assert.AreEqual(10000L, got[0].Lifecycles[0].BirthWorldDay);
            Assert.IsFalse(got[0].Lifecycles[0].RetirementFlag);
            Assert.AreEqual(0u, got[0].Lifecycles[0].RetirementDay);
            Assert.AreEqual(uint.MaxValue - 1, got[0].Lifecycles[0].LastAdvancedWorldDay);

            Assert.AreEqual(7, got[0].Records[1].PlayerId);
            Assert.AreEqual("Marco", got[0].Records[1].FirstName);
            Assert.AreEqual(33, got[0].Records[1].Age);
            Assert.AreEqual(PlayerPosition.Midfielder, got[0].Records[1].Position);
            Assert.AreEqual(200L, got[0].Lifecycles[1].GrowthCursor);
            Assert.AreEqual(-80000L, got[0].Lifecycles[1].BirthWorldDay);

            // Club 2: players ascend 10, 11.
            Assert.AreEqual(10, got[1].Records[0].PlayerId);
            Assert.AreEqual(12345u, got[1].Lifecycles[0].LastAdvancedWorldDay,
                "an ordinary mid-range cursor must survive exactly. (The never-advanced sentinel is no " +
                "longer legal STORE data to round-trip at all since ERR-028-014 — see " +
                "Encode_NeverAdvancedSentinel_FailsLoud_ERR028014 / Decode_NeverAdvancedSentinel_" +
                "FailsLoud_ERR028014 below for that half of the contract.)");

            Assert.AreEqual(11, got[1].Records[1].PlayerId);
            Assert.AreEqual(PlayerPosition.Forward, got[1].Records[1].Position);
            Assert.IsTrue(got[1].Lifecycles[1].RetirementFlag);
            Assert.AreEqual(900u, got[1].Lifecycles[1].RetirementDay);
            Assert.AreEqual(-120L, got[1].Lifecycles[1].GrowthCursor,
                "GrowthCursor is signed — decline must survive as a negative value.");
            Assert.AreEqual(-50000L, got[1].Lifecycles[1].BirthWorldDay,
                "BirthWorldDay is signed — a pre-epoch birth must survive as a negative value (ERR-028-006).");
        }

        [Test]
        public void RoundTrip_EmptySet_IsAWellFormedBlock()
        {
            byte[] blob = ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), nextPlayerId: 0);
            Assert.AreEqual(16, blob.Length,
                "an empty block is exactly the magic + the version + nextPlayerId + a zero club count");

            ClubCareerStates[] got = ProgressionSaveCodec.Decode(blob, out int nextPlayerId);
            Assert.AreEqual(0, got.Length);
            Assert.AreEqual(0, nextPlayerId);
        }

        // ── Canonical order ─────────────────────────────────────────────────────────

        [Test]
        public void Encode_IsOrderIndependent_SameStateSameBytes()
        {
            var forward = new[]
            {
                Club(1,
                    (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u)),
                    (Rec(9, "C", "D", 22, PlayerPosition.Forward, 2), Life(6000, 4000, 10, 0, false, 0u, 2u))),
                Club(2,
                    (Rec(3, "E", "F", 25, PlayerPosition.Defender, 3), Life(7000, 5000, -10, 0, false, 0u, 3u))),
            };
            var shuffled = new[]
            {
                Club(2,
                    (Rec(3, "E", "F", 25, PlayerPosition.Defender, 3), Life(7000, 5000, -10, 0, false, 0u, 3u))),
                Club(1,
                    (Rec(9, "C", "D", 22, PlayerPosition.Forward, 2), Life(6000, 4000, 10, 0, false, 0u, 2u)),
                    (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))),
            };

            CollectionAssert.AreEqual(
                ProgressionSaveCodec.Encode(forward, 10),
                ProgressionSaveCodec.Encode(shuffled, 10),
                "the same state set must encode to the same bytes whatever order it is presented in " +
                "(canonical ordering — FR-PG-019).");
        }

        [Test]
        public void Encode_OfADecodedBlob_IsByteIdentical()
        {
            byte[] once = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            ClubCareerStates[] decoded = ProgressionSaveCodec.Decode(once, out int nextPlayerId);
            byte[] twice = ProgressionSaveCodec.Encode(decoded, nextPlayerId);
            CollectionAssert.AreEqual(once, twice, "decode ∘ encode must be a fixed point");
        }

        [Test]
        public void Encode_DoesNotMutateTheCallersArrays()
        {
            ClubCareerStates[] clubs = TwoClubs();
            int firstClubIdBefore = clubs[0].ClubId;
            int firstPlayerIdBefore = clubs[0].Records[0].PlayerId;

            ProgressionSaveCodec.Encode(clubs, 12);

            Assert.AreEqual(firstClubIdBefore, clubs[0].ClubId, "the club array must not be reordered");
            Assert.AreEqual(firstPlayerIdBefore, clubs[0].Records[0].PlayerId,
                "the player array must not be sorted in place");
        }

        // ── Encode-side fail-loud gates ────────────────────────────────────────────

        [Test]
        public void Encode_DuplicatePlayerId_FailsLoud()
        {
            var clubs = new[]
            {
                Club(1,
                    (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u)),
                    (Rec(5, "C", "D", 21, PlayerPosition.Forward, 2), Life(6000, 4000, 0, 0, false, 0u, 1u))),
            };

            Assert.Throws<ArgumentException>(() => ProgressionSaveCodec.Encode(clubs, 10));
        }

        [Test]
        public void Encode_DuplicateClubId_FailsLoud()
        {
            Assert.Throws<ArgumentException>(
                () => ProgressionSaveCodec.Encode(new[] { Club(3), Club(3) }, 10));
        }

        [Test]
        public void Encode_UnboundClub_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(
                () => ProgressionSaveCodec.Encode(new ClubCareerStates[1], 10));
        }

        [Test]
        public void Encode_NullSet_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => ProgressionSaveCodec.Encode(null, 10));
        }

        [Test]
        public void Encode_UndefinedPosition_FailsLoud()
        {
            PlayerRecord bad = Rec(5, "A", "B", 20, (PlayerPosition)99, 1);
            var clubs = new[] { Club(1, (bad, Life(5000, 3000, 0, 0, false, 0u, 1u))) };

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Encode(clubs, 10),
                "Decode refuses an undefined position ordinal, so writing one would produce a file no " +
                "load of it could accept (the never-write-what-Decode-refuses rule).");
        }

        [Test]
        public void Encode_NonAsciiFirstName_FailsLoud()
        {
            PlayerRecord bad = Rec(5, "André", "B", 20, PlayerPosition.Midfielder, 1);
            var clubs = new[] { Club(1, (bad, Life(5000, 3000, 0, 0, false, 0u, 1u))) };

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Encode(clubs, 10),
                "the canonical string encoding is ASCII (#16 §3.2.4.1); a non-ASCII name would " +
                "round-trip to a different name and must be refused at write, not silently mangled.");
        }

        [Test]
        public void Encode_NonAsciiLastName_FailsLoud()
        {
            PlayerRecord bad = Rec(5, "A", "André", 20, PlayerPosition.Midfielder, 1);
            var clubs = new[] { Club(1, (bad, Life(5000, 3000, 0, 0, false, 0u, 1u))) };

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Encode(clubs, 10));
        }

        // ── Cross-club uniqueness (mutation-audit locks) ───────────────────────────
        //
        // A mutation audit proved that deleting RequireGloballyUniquePlayerIds at either Encode or
        // Decode leaves the whole suite green. Encode_DuplicatePlayerId_FailsLoud above puts both
        // duplicates in ONE club, which CanonicalOrder already refuses before this gate ever runs, so
        // it is not a lock on this gate — these two are the cross-club case only this gate catches.

        [Test]
        public void Encode_CrossClubDuplicatePlayerId_FailsLoud()
        {
            var clubs = new[]
            {
                Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))),
                Club(2, (Rec(5, "C", "D", 21, PlayerPosition.Forward, 2), Life(6000, 4000, 0, 0, false, 0u, 1u))),
            };

            Assert.Throws<ArgumentException>(() => ProgressionSaveCodec.Encode(clubs, 10),
                "player id 5 appears in both club 1 and club 2 — a career requires globally unique " +
                "player ids (ERR-041-019 / ERR-027-004), and CanonicalOrder alone cannot see across " +
                "clubs.");
        }

        [Test]
        public void Decode_CrossClubDuplicatePlayerId_FailsLoud()
        {
            // Encode refuses this shape (the test above), so a corrupt/hand-edited file is the only
            // way bytes carrying a cross-club duplicate can reach Decode. Two single-club blobs are
            // encoded separately (each internally valid on its own) and their club bodies spliced
            // together under one shared header — club ids ascend (1 then 2) and each club's own
            // player id ascends trivially (one player each), so only the cross-club global-uniqueness
            // gate at Decode can catch the splice.
            byte[] club1Blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            byte[] club2Blob = ProgressionSaveCodec.Encode(
                new[] { Club(2, (Rec(5, "C", "D", 21, PlayerPosition.Forward, 2), Life(6000, 4000, 0, 0, false, 0u, 1u))) },
                10);

            const int HeaderBytes = 4 + 4 + 4 + 4;   // magic + version + nextPlayerId + clubCount
            byte[] club1Body = new byte[club1Blob.Length - HeaderBytes];
            Array.Copy(club1Blob, HeaderBytes, club1Body, 0, club1Body.Length);
            byte[] club2Body = new byte[club2Blob.Length - HeaderBytes];
            Array.Copy(club2Blob, HeaderBytes, club2Body, 0, club2Body.Length);

            byte[] spliced = new byte[HeaderBytes + club1Body.Length + club2Body.Length];
            int o = 0;
            CanonicalSerializer.WriteU32(spliced, ref o, PlayerProgressionConstants.PROGRESSION_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(spliced, ref o, PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteI32(spliced, ref o, 10);
            CanonicalSerializer.WriteU32(spliced, ref o, 2u);
            Array.Copy(club1Body, 0, spliced, o, club1Body.Length);
            o += club1Body.Length;
            Array.Copy(club2Body, 0, spliced, o, club2Body.Length);

            Assert.Throws<ArgumentException>(() => ProgressionSaveCodec.Decode(spliced, out _),
                "a hand-spliced blob carrying the same player id in two different (ascending, " +
                "internally well-formed) clubs must still be refused — the load-side mirror of " +
                "Encode's gate (ERR-041-019 / ERR-027-004).");
        }

        // ── Never-advanced sentinel (mutation-audit locks) ─────────────────────────
        //
        // ERR-028-014 retired PROGRESSION_NOT_ADVANCED_SENTINEL from #28's legal STORE states —
        // ProgressionEngine.FromBlocks (which Restore calls straight after Decode) refuses it — but
        // neither Encode nor Decode validated LastAdvancedWorldDay at all, so a hand-built block
        // carrying the sentinel encoded happily into a file that could never be loaded. Same class as
        // the cross-club uniqueness gate above (ERR-028-011(a)), one ERR later.

        [Test]
        public void Encode_NeverAdvancedSentinel_FailsLoud_ERR028014()
        {
            var clubs = new[]
            {
                Club(1,
                    (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1),
                     Life(5000, 3000, 0L, 0L, retired: false, retiredDay: 0u,
                          lastAdvanced: PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL))),
            };

            Assert.Throws<ArgumentException>(() => ProgressionSaveCodec.Encode(clubs, 10),
                "ProgressionEngine.FromBlocks refuses the never-advanced sentinel as a progression " +
                "cursor (ERR-028-014) — writing it would produce a file its own restore path can never " +
                "load.");
        }

        [Test]
        public void Decode_NeverAdvancedSentinel_FailsLoud_ERR028014()
        {
            // Encode refuses this shape (the test above), so a hand-edited file is the only way bytes
            // carrying the sentinel can reach Decode. Encode a well-formed player carrying an ordinary
            // cursor, then patch the LastAdvancedWorldDay field in place to the sentinel value.
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // PositionOffset(42) + position(1) + attributes(AttrIdx.Count*4) + weakFootRating(4)
            //   + potentialAbility(4) + currentAbility(4) + growthCursor(8) + birthWorldDay(8)
            //   + retirementFlag(1) + retirementDay(4) ⇒ lastAdvancedWorldDay
            const int LastAdvancedWorldDayOffset =
                42 + 1 + AttrIdx.Count * 4 + 4 + 4 + 4 + 8 + 8 + 1 + 4;
            int o = LastAdvancedWorldDayOffset;
            CanonicalSerializer.WriteU32(blob, ref o, PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL);

            Assert.Throws<ArgumentException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a hand-edited file carrying the never-advanced sentinel must be refused here — the " +
                "codec's own half of never-write-what-load-refuses (ERR-028-014) — rather than reaching " +
                "ProgressionEngine.FromBlocks with a less specific message.");
        }

        // ── Decode-side value-range gates (mutation-audit locks) ───────────────────
        //
        // A mutation audit proved that deleting any one of the L3 range gates in ReadPlayer — or the
        // ReadGuardedString body-length bound — leaves the whole suite green. This block is the roster
        // NOW (KD-4): a corrupt value flows straight into PlayerAttributeProjection / the match engine,
        // so each gate needs a test that fails if it is reverted. All five reuse the single-player
        // fixture and byte layout already established by Decode_UndefinedPositionOrdinal_FailsLoud
        // (PositionOffset = 42).

        [Test]
        public void Decode_AttributeOutOfRange_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // magic(4)+version(4)+nextPlayerId(4)+clubCount(4)+clubId(4)+playerCount(4)+playerId(4)
            //   +firstNameLen(4)+"A"(1)+lastNameLen(4)+"B"(1)+age(4)+position(1) = 43 ⇒ attribute[0]
            const int Attribute0Offset = 43;
            int o = Attribute0Offset;
            CanonicalSerializer.WriteI32(blob, ref o, PlayerProgressionConstants.ATTRIBUTE_MAX + 1);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "an attribute outside [ATTRIBUTE_MIN, ATTRIBUTE_MAX] must be refused — this block is " +
                "the roster now (KD-4), so a corrupt attribute would flow straight into " +
                "PlayerAttributeProjection and the match engine.");
        }

        [Test]
        public void Decode_WeakFootOutOfRange_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // PositionOffset(42) + position(1) + attributes(31 * 4 = 124) = 167 ⇒ weakFootRating
            const int WeakFootOffset = 42 + 1 + AttrIdx.Count * 4;
            int o = WeakFootOffset;
            CanonicalSerializer.WriteI32(blob, ref o, PlayerDatabaseConstants.WEAK_FOOT_MAX + 1);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a weak-foot rating outside [WEAK_FOOT_MIN, WEAK_FOOT_MAX] must be refused — a " +
                "different scale from the [1,20] attributes, gated separately.");
        }

        [Test]
        public void Decode_NegativeAge_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // magic(4)+version(4)+nextPlayerId(4)+clubCount(4)+clubId(4)+playerCount(4)+playerId(4)
            //   +firstNameLen(4)+"A"(1)+lastNameLen(4)+"B"(1) = 38 ⇒ age
            const int AgeOffset = 38;
            int o = AgeOffset;
            CanonicalSerializer.WriteI32(blob, ref o, -1);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a negative age must be refused — not representable in the model. The authoritative " +
                "anchor is birthWorldDay, which MAY legitimately be negative (ERR-028-006); the " +
                "derived age cache may not.");
        }

        [Test]
        public void Decode_GuardedStringLengthOverrunsBlob_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // magic(4)+version(4)+nextPlayerId(4)+clubCount(4)+clubId(4)+playerCount(4)+playerId(4)
            //   = 28 ⇒ firstName length prefix
            const int FirstNameLenOffset = 28;
            int o = FirstNameLenOffset;
            CanonicalSerializer.WriteU32(blob, ref o, (uint)(blob.Length + 1000));

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a string length prefix that overruns the remaining blob must throw — not over-read " +
                "past the buffer (ReadGuardedString's own bound, ahead of CanonicalSerializer.ReadString's " +
                "unchecked index).");
        }

        [Test]
        public void Decode_PotentialAbilityOutOfRange_FailsLoud()
        {
            // The gate this test locks: L3 covered attributes, weak-foot and age but not
            // PotentialAbility, the F1 growth ceiling — a corrupt negative value would silently freeze
            // a player's growth forever (AbilityModel.TrySpendOnePoint returns false once
            // CurrentAbility >= PotentialAbility).
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // PositionOffset(42) + position(1) + attributes(124) + weakFootRating(4) = 171 ⇒ potentialAbility
            const int PotentialAbilityOffset = 42 + 1 + AttrIdx.Count * 4 + 4;
            int o = PotentialAbilityOffset;
            CanonicalSerializer.WriteI32(blob, ref o, PlayerProgressionConstants.PA_MIN - 1);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a PotentialAbility outside [PA_MIN, ABILITY_MAX] must be refused — it is the F1 " +
                "growth ceiling, and a corrupt value below PA_MIN would silently freeze a player's " +
                "growth forever.");
        }

        // ── Decode-side fail-loud gates ─────────────────────────────────────────────

        [Test]
        public void Decode_Null_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => ProgressionSaveCodec.Decode(null, out _));
        }

        [Test]
        public void Decode_WrongMagic_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o, 0xDEADBEEFu);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }

        [Test]
        public void Decode_ATrainingBlock_FailsLoud_ERR028004()
        {
            // player-progression cannot reference training-system (the reference-direction rule), so
            // the #29 block is hand-built rather than encoded through TrainingSaveCodec: just its magic
            // (TrainingSystemConstants.TRAINING_SAVE_MAGIC = 'T''R''N''G' = 0x54524E47) followed by
            // enough bytes to look like a plausible version + zero-club count. The magic alone must be
            // enough to refuse it — every later gate here would otherwise pass a well-formed empty block.
            byte[] trainingLike = new byte[16];
            int o = 0;
            CanonicalSerializer.WriteU32(trainingLike, ref o, TrainingSaveMagic);
            CanonicalSerializer.WriteU32(trainingLike, ref o, 1u);   // a plausible format version
            CanonicalSerializer.WriteI32(trainingLike, ref o, 0);    // a plausible nextPlayerId-shaped field
            CanonicalSerializer.WriteU32(trainingLike, ref o, 0u);   // a plausible zero-count field

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(trainingLike, out _),
                "a #29 training block must be refused by the magic, not misread as #28 career state " +
                "(ERR-028-004) — every sub-blob format in this save stack sits at version 1, so a " +
                "version gate alone could not have caught this.");
        }

        [Test]
        public void Decode_WrongFormatVersion_FailsLoud_F3()
        {
            byte[] blob = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            int o = 4;   // past the magic
            CanonicalSerializer.WriteU32(blob, ref o,
                PlayerProgressionConstants.PROGRESSION_SAVE_FORMAT_VERSION + 1);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _),
                "a PROGRESSION_SAVE_FORMAT_VERSION mismatch must fail loud — no migration at Stage 0 (F3).");
        }

        [Test]
        public void Decode_TrailingBytes_FailsLoud_F5()
        {
            byte[] blob = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(padded, out _));
        }

        [Test]
        public void Decode_Truncated_FailsLoud_F5()
        {
            byte[] blob = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            var chopped = new byte[blob.Length - 3];
            Array.Copy(blob, chopped, chopped.Length);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(chopped, out _));
        }

        [Test]
        public void Decode_TruncatedClubCountPrefix_FailsLoud()
        {
            // The count prefix itself is cut off mid-integer — fewer than 4 bytes remain to read it.
            byte[] blob = ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0);
            var chopped = new byte[13];   // magic(4) + version(4) + nextPlayerId(4) + 1 byte of clubCount
            Array.Copy(blob, chopped, chopped.Length);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(chopped, out _));
        }

        [Test]
        public void Decode_OversizeClubCount_FailsLoud()
        {
            // The overflow class the ReadCount bound exists for: a crafted count near uint.MaxValue must
            // be refused against the bytes that remain.
            byte[] blob = ProgressionSaveCodec.Encode(TwoClubs(), 12);
            int o = 12;   // magic + version + nextPlayerId
            CanonicalSerializer.WriteU32(blob, ref o, uint.MaxValue);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }

        [Test]
        public void Decode_OversizePlayerCount_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            int o = 4 + 4 + 4 + 4 + 4;   // magic, version, nextPlayerId, clubCount, clubId
            CanonicalSerializer.WriteU32(blob, ref o, 0x7FFFFFFFu);

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }

        [Test]
        public void Decode_UndefinedPositionOrdinal_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[] { Club(1, (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u))) },
                10);
            // magic(4)+version(4)+nextPlayerId(4)+clubCount(4)+clubId(4)+playerCount(4)+playerId(4)
            //   +firstNameLen(4)+"A"(1)+lastNameLen(4)+"B"(1)+age(4) = 42 ⇒ the position byte
            const int PositionOffset = 42;
            blob[PositionOffset] = 200;

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }

        [Test]
        public void Decode_NonAscendingClubIds_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(new[] { Club(1), Club(2) }, 0);
            int o = 16;   // magic + version + nextPlayerId + clubCount ⇒ first club id
            CanonicalSerializer.WriteI32(blob, ref o, 5);   // 5 then 2 — no longer ascending

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }

        [Test]
        public void Decode_NonAscendingPlayerIds_FailsLoud()
        {
            byte[] blob = ProgressionSaveCodec.Encode(
                new[]
                {
                    Club(1,
                        (Rec(5, "A", "B", 20, PlayerPosition.Midfielder, 1), Life(5000, 3000, 0, 0, false, 0u, 1u)),
                        (Rec(6, "C", "D", 21, PlayerPosition.Forward, 2), Life(6000, 4000, 0, 0, false, 0u, 1u))),
                },
                10);

            int o = 4 + 4 + 4 + 4 + 4 + 4;   // magic, version, nextPlayerId, clubCount, clubId, playerCount
            CanonicalSerializer.WriteI32(blob, ref o, 9);   // first player id 5 -> 9, now above the second

            Assert.Throws<InvalidOperationException>(() => ProgressionSaveCodec.Decode(blob, out _));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-08 | —      | M4: the codec's own dedicated suite (mirrors MedicalSaveCodecTests |
// |         |            |        | / TrainingSaveCodecTests). Round-trip field identity (FR-PG-018),  |
// |         |            |        | canonical-order determinism, the encode-side duplicate / unbound / |
// |         |            |        | undefined-position / non-ASCII guards, and the decode fail-loud    |
// |         |            |        | gates (magic, F3, F5, oversize counts, non-ascending keys), plus   |
// |         |            |        | the ERR-028-004 sibling-confusion lock against a hand-built #29    |
// |         |            |        | training-shaped block (player-progression cannot reference         |
// |         |            |        | training-system).                                                   |
// | 1.1     | 2026-08-09 | —      | Mutation-audit locks for six guards a mutation sweep proved dead:   |
// |         |            |        | cross-club RequireGloballyUniquePlayerIds at Encode and at Decode  |
// |         |            |        | (distinct from the existing single-club duplicate test, which      |
// |         |            |        | CanonicalOrder already refuses before this gate runs), the         |
// |         |            |        | attribute / weak-foot / age L3 range gates, and ReadGuardedString's |
// |         |            |        | body-length bound (proven by observing the mutant throw the WRONG  |
// |         |            |        | exception type — ArgumentOutOfRangeException from an unchecked     |
// |         |            |        | Encoding.ASCII.GetString — rather than the codec's own              |
// |         |            |        | InvalidOperationException). Also locks the new PotentialAbility    |
// |         |            |        | range gate ProgressionSaveCodec.cs's ReadPlayer gained this pass   |
// |         |            |        | (the F1 growth ceiling had no gate before it). Every lock proven   |
// |         |            |        | by deleting its guard, confirming exactly the new test failed, and |
// |         |            |        | restoring it.                                                       |
// | 1.2     | 2026-08-10 | —      | Two new tests lock RequireNoNeverAdvancedSentinel at Encode and    |
// |         |            |        | Decode (ERR-028-014, the ERR-028-011(a) class one ERR later):      |
// |         |            |        | ProgressionEngine.FromBlocks refuses the never-advanced sentinel   |
// |         |            |        | as a progression cursor, but neither Encode nor Decode validated   |
// |         |            |        | it before this pass. Proven by mutation: deleting either call site |
// |         |            |        | left its new test the only failure in the suite. TwoClubs() player |
// |         |            |        | 10 (formerly carrying the sentinel itself, to prove it round-      |
// |         |            |        | tripped as itself) now carries an ordinary 12345u — that fixture   |
// |         |            |        | is shared by every round-trip/fail-loud test in this file, and     |
// |         |            |        | Encode's new guard refuses it, so the eight tests that build a     |
// |         |            |        | blob through TwoClubs() would otherwise fail on a fixture the      |
// |         |            |        | codec's own contract no longer allows. RoundTrip_EveryField-       |
// |         |            |        | Survives_FRPG018's assertion updated to match; the sentinel-       |
// |         |            |        | round-trips-as-itself property it used to check is superseded by   |
// |         |            |        | the new ERR-028-014 tests, which check the opposite (the sentinel  |
// |         |            |        | is refused, not carried).                                          |
#endregion