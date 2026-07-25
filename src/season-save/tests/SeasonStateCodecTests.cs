// File:     src/season-save/tests/SeasonStateCodecTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §3.6, Appendix B, FR-SN-019/022/023, §5.2 (T-SN-SAVE-*);
//           Code Standards #20
// Purpose:  Unit tests for the season-state sub-blob codec — in-memory round-trip field-identity
//           (FR-SN-022) across every state the layout carries, and every fail-loud gate FR-SN-023
//           requires (version mismatch, corrupt length prefixes, truncation, trailing bytes, and the
//           per-field coherence checks the decode path lands values through).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// In-memory <see cref="SeasonStateCodec"/> tests. The central property is FR-SN-022 round-trip
    /// field-identity; the rest lock the FR-SN-023 fail-loud posture, since a codec that silently
    /// accepted a corrupt blob would surface as a nonsensical season rather than an error.
    /// </summary>
    [TestFixture]
    public sealed class SeasonStateCodecTests
    {
        private const int ManagedClub = 11;
        private static int[] Clubs => new[] { 10, 11, 12, 13 };

        // Byte offsets into the encoded blob, from the Appendix B layout. Used by the corruption tests
        // so they perturb a NAMED field rather than a magic index.
        private const int OffsetVersion = 0;                                 // row 1  (u32)
        private const int OffsetSeed = 4;                                    // row 2  (u64)
        private const int OffsetSeasonNumber = 12;                           // row 3  (i32)
        private const int OffsetManagedClubId = 16;                          // row 3a (i32)
        private const int OffsetClubCount = 20;                              // row 4  (count)
        private const int OffsetFirstClubId = 24;                            // row 5  (i32[])

        private static SeasonState FreshSeason() =>
            SeasonState.CreateNew(
                Clubs, ManagedClub, seed: 0xABCDEF0123456789UL,
                objective: new BoardObjective(2),
                firstRoundDay: 5u, daysBetweenRounds: 7u, seasonNumber: 3);

        /// <summary>
        /// A season part-way through: round 0 resolved (table columns populated, those fixtures flagged
        /// played, cursor advanced) with a non-default board. Every Appendix B field is off its
        /// construction default, so a codec that dropped or transposed one cannot round-trip.
        /// </summary>
        private static SeasonState MidSeason()
        {
            SeasonState s = FreshSeason();
            int[] round0 = s.UnplayedFixtureIndicesInRound(0);
            for (int i = 0; i < round0.Length; i++)
            {
                Fixture f = s.FixtureAt(round0[i]);
                s.ApplyResult(new MatchResult(
                    f.HomeClubId, f.AwayClubId, homeGoals: 2 + i, awayGoals: i,
                    roundIndex: f.RoundIndex, worldDay: 5u));
                s.MarkFixturePlayed(round0[i]);
            }

            s.AdvanceCursorOneRound();
            s.SetBoard(s.Board.WithJobSecurity(742));
            return s;
        }

        private static SeasonState RoundTrip(SeasonState s) =>
            SeasonStateCodec.Decode(SeasonStateCodec.Encode(s));

        // ── Round-trip field identity (FR-SN-022) ──────────────────────────────────

        [Test]
        public void RoundTrip_FreshSeason_IsFieldIdentical()
        {
            SeasonState s = FreshSeason();
            Assert.IsTrue(s.FieldsEqual(RoundTrip(s)));
        }

        [Test]
        public void RoundTrip_MidSeason_IsFieldIdentical()
        {
            SeasonState s = MidSeason();
            Assert.IsTrue(s.FieldsEqual(RoundTrip(s)),
                "FR-SN-022: a part-played season must round-trip field-identically.");
        }

        [Test]
        public void RoundTrip_CompletedSeason_IsFieldIdentical()
        {
            // The cursor at RoundCount (IsSeasonComplete) is the F5 boundary state — the one the
            // boundary roll runs from, and the one an off-by-one cursor encoding would corrupt.
            SeasonState s = FreshSeason();
            int rounds = s.Calendar.RoundCount;
            for (int r = 0; r < rounds; r++)
            {
                foreach (int idx in s.UnplayedFixtureIndicesInRound(r))
                {
                    Fixture f = s.FixtureAt(idx);
                    s.ApplyResult(new MatchResult(
                        f.HomeClubId, f.AwayClubId, 1, 1, f.RoundIndex, s.Calendar.DayOfRound(r)));
                    s.MarkFixturePlayed(idx);
                }

                s.AdvanceCursorOneRound();
            }

            Assert.IsTrue(s.Calendar.IsSeasonComplete, "fixture precondition: the season is complete");
            SeasonState got = RoundTrip(s);
            Assert.IsTrue(s.FieldsEqual(got));
            Assert.IsTrue(got.Calendar.IsSeasonComplete,
                "A completed season's cursor must survive the round-trip at RoundCount (F5).");
        }

        [Test]
        public void RoundTrip_PreservesEveryTableColumn()
        {
            // FieldsEqual delegates the table comparison, so assert the columns directly too: a codec
            // that transposed two of the nine per-row integers would still satisfy a laxer check.
            SeasonState s = MidSeason();
            SeasonState got = RoundTrip(s);
            foreach (int clubId in Clubs)
            {
                LeagueTableRow a = s.TableRow(clubId);
                LeagueTableRow b = got.TableRow(clubId);
                Assert.AreEqual(a.Played, b.Played, "Played (club {0})", clubId);
                Assert.AreEqual(a.Won, b.Won, "Won (club {0})", clubId);
                Assert.AreEqual(a.Drawn, b.Drawn, "Drawn (club {0})", clubId);
                Assert.AreEqual(a.Lost, b.Lost, "Lost (club {0})", clubId);
                Assert.AreEqual(a.GoalsFor, b.GoalsFor, "GoalsFor (club {0})", clubId);
                Assert.AreEqual(a.GoalsAgainst, b.GoalsAgainst, "GoalsAgainst (club {0})", clubId);
                Assert.AreEqual(a.GoalDifference, b.GoalDifference, "GoalDifference (club {0})", clubId);
                Assert.AreEqual(a.Points, b.Points, "Points (club {0})", clubId);
            }
        }

        [Test]
        public void RoundTrip_PreservesScalarsAndBoard()
        {
            SeasonState s = MidSeason();
            SeasonState got = RoundTrip(s);
            Assert.AreEqual(s.Seed, got.Seed, "seed");
            Assert.AreEqual(s.SeasonNumber, got.SeasonNumber, "seasonNumber");
            Assert.AreEqual(s.ManagedClubId, got.ManagedClubId, "managedClubId (Appendix B row 3a)");
            Assert.AreEqual(s.Board.Objective.TargetPositionOrBetter,
                got.Board.Objective.TargetPositionOrBetter, "board objective");
            Assert.AreEqual(s.Board.JobSecurityPerMille, got.Board.JobSecurityPerMille, "job security");
            Assert.AreEqual(s.Calendar.NextRoundIndex, got.Calendar.NextRoundIndex, "cursor");
            CollectionAssert.AreEqual(s.Calendar.RoundDaysCopy(), got.Calendar.RoundDaysCopy(), "round days");
        }

        [Test]
        public void Encode_IsDeterministic_ForEqualStates()
        {
            // Two independently built states with the same history must encode to identical bytes —
            // the property the season blob's contribution to save determinism rests on.
            CollectionAssert.AreEqual(
                SeasonStateCodec.Encode(MidSeason()), SeasonStateCodec.Encode(MidSeason()));
        }

        [Test]
        public void Encode_ChangesWithState()
        {
            // Non-vacuity guard for the determinism test above: a season that differs must not encode
            // identically (otherwise the round-trip assertions could pass over a constant blob).
            SeasonState fresh = FreshSeason();
            CollectionAssert.AreNotEqual(
                SeasonStateCodec.Encode(fresh), SeasonStateCodec.Encode(MidSeason()));
        }

        // ── Fail-loud (FR-SN-023 / F3) ─────────────────────────────────────────────

        [Test]
        public void Encode_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeasonStateCodec.Encode(null));
        }

        [Test]
        public void Decode_NullBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeasonStateCodec.Decode(null));
        }

        [Test]
        public void Decode_WrongFormatVersion_FailsLoud()
        {
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            blob[OffsetVersion] ^= 0xFF;
            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(blob),
                "A SEASON_STATE_FORMAT_VERSION mismatch must fail loud — no Stage-0 migration (KD-4).");
        }

        [Test]
        public void Decode_TrailingBytes_FailsLoud()
        {
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(padded),
                "Trailing bytes after the declared content must fail loud (FR-SN-023).");
        }

        [Test]
        public void Decode_Truncated_FailsLoud()
        {
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            var chopped = new byte[blob.Length - 3];
            Array.Copy(blob, chopped, chopped.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(chopped),
                "A truncated blob must fail loud, not read past the end.");
        }

        [Test]
        public void Decode_OversizeClubCount_FailsLoud()
        {
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int o = OffsetClubCount;
            CanonicalSerializer.WriteU32(blob, ref o, 0xFFFFFFFFu);
            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(blob),
                "A club count exceeding the remaining bytes must fail loud, never over-allocate " +
                "(the overflow-safe ReadCount bound, FR-SN-023).");
        }

        [Test]
        public void Construct_EmptyScheduleWithUnsetCalendar_FailsLoud()
        {
            // #30 T1 AR regression: this state used to be constructible (the calendar-coverage check is
            // vacuous when the schedule is empty), and Encode would write a zero-round calendar that
            // Decode cannot rebuild — an FR-SN-022 round-trip asymmetry. The constructor now refuses it,
            // so every constructible SeasonState is a round-trippable one.
            Assert.Throws<ArgumentException>(
                () => new SeasonState(
                    seed: 1UL, seasonNumber: 0, managedClubId: ManagedClub,
                    clubIds: Clubs, fixtures: Array.Empty<Fixture>(),
                    table: LeagueTable.Empty(Clubs),
                    calendar: default,
                    board: BoardState.Fresh(new BoardObjective(1))),
                "A calendar mapping no rounds must be refused at construction.");
        }

        [Test]
        public void Decode_OversizeTableRowCount_FailsLoud()
        {
            // Locks the element-wise row bound. Note what this does and does not prove: it exercises the
            // bound at ordinary sizes, which the superseded byte-product form also caught. The overflow
            // case that motivated expressing the bound in ELEMENTS (a `count * 36` product wrapping int
            // for a >150 MB blob and a crafted count, landing back on a small positive value) is not
            // reachable in a test — the bound is written to be provably overflow-free instead.
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int rowCountOffset = TableRowOffset(blob, 0) - 4;
            int o = rowCountOffset;
            uint actual = CanonicalSerializer.ReadU32(blob, ref o);
            o = rowCountOffset;
            CanonicalSerializer.WriteU32(blob, ref o, actual + 2);

            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(blob),
                "A table-row count exceeding what the remaining bytes can hold must fail loud.");
        }

        [Test]
        public void Decode_EmptyBlob_FailsLoud()
        {
            Assert.Throws<InvalidOperationException>(
                () => SeasonStateCodec.Decode(Array.Empty<byte>()),
                "An empty blob cannot even carry the version gate and must fail loud.");
        }

        [Test]
        public void Decode_CorruptGoalDifference_FailsLoud()
        {
            // GoalDifference is serialized (Appendix B row 10) but DERIVED on build (§3.2). The decoder
            // must not silently prefer either form — a save whose GD contradicts its goal columns is
            // corrupt, and quietly recomputing it would hide that.
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int gdOffset = GoalDifferenceOffset(blob, rowIndex: 0);
            int o = gdOffset;
            int stored = CanonicalSerializer.ReadI32(blob, ref o);
            o = gdOffset;
            CanonicalSerializer.WriteI32(blob, ref o, stored + 1);

            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(blob),
                "A goal difference disagreeing with the goal columns must fail loud.");
        }

        [Test]
        public void Decode_NegativeTableColumn_FailsLoud()
        {
            // A negative COLUMN inside a table row (here Played), not a count prefix — the sibling
            // Decode_OversizeTableRowCount_FailsLoud covers the prefix. This is the LeagueTableRow.Create
            // gate: the decode path's per-row coherence check on data straight out of the blob.
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int playedOffset = TableRowOffset(blob, rowIndex: 0) + 4; // clubId, then Played
            int o = playedOffset;
            CanonicalSerializer.WriteI32(blob, ref o, -1);

            Assert.Throws<ArgumentOutOfRangeException>(() => SeasonStateCodec.Decode(blob),
                "A negative table count must fail loud at LeagueTableRow.Create.");
        }

        [Test]
        public void Decode_ManagedClubOutsideClubSet_FailsLoud()
        {
            // The SeasonState constructor's cross-field coherence gate: an individually well-formed
            // managed-club id that names no club in the set.
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int o = OffsetManagedClubId;
            CanonicalSerializer.WriteI32(blob, ref o, 999);

            Assert.Throws<ArgumentException>(() => SeasonStateCodec.Decode(blob),
                "A managed club outside the club set must fail loud (F2/F3).");
        }

        [Test]
        public void Decode_FixtureNamingUnknownClub_FailsLoud()
        {
            // Corrupt a CLUB ID rather than a fixture, so the fixture list (unchanged) now names clubs
            // outside the set — the SeasonState constructor's other coherence gate.
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            int o = OffsetFirstClubId;
            CanonicalSerializer.WriteI32(blob, ref o, 777);

            Assert.Throws<ArgumentException>(() => SeasonStateCodec.Decode(blob),
                "A club set that no longer covers the schedule must fail loud (F2/F3).");
        }

        [Test]
        public void Decode_BadPlayedFlag_FailsLoud()
        {
            byte[] blob = SeasonStateCodec.Encode(MidSeason());
            // The first fixture's played flag is the 13th byte of the fixture block (round, home, away,
            // played), which starts right after the club-id array.
            int fixturesStart = OffsetFirstClubId + Clubs.Length * 4 + 4; // + fixtureCount prefix
            blob[fixturesStart + 12] = 2;

            Assert.Throws<InvalidOperationException>(() => SeasonStateCodec.Decode(blob),
                "A played flag other than 0/1 must fail loud.");
        }

        [Test]
        public void Decode_SeedAndSeasonNumberSitAtTheirPinnedOffsets()
        {
            // Locks the Appendix B field ORDER, not just the round-trip: a transposition of two
            // same-width fields would round-trip cleanly but break the pinned layout other tools read.
            SeasonState s = MidSeason();
            byte[] blob = SeasonStateCodec.Encode(s);

            int o = OffsetVersion;
            Assert.AreEqual(SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION,
                CanonicalSerializer.ReadU32(blob, ref o), "row 1: version");
            o = OffsetSeed;
            Assert.AreEqual(s.Seed, CanonicalSerializer.ReadU64(blob, ref o), "row 2: seed");
            o = OffsetSeasonNumber;
            Assert.AreEqual(s.SeasonNumber, CanonicalSerializer.ReadI32(blob, ref o), "row 3: seasonNumber");
            o = OffsetManagedClubId;
            Assert.AreEqual(s.ManagedClubId, CanonicalSerializer.ReadI32(blob, ref o), "row 3a: managedClubId");
            o = OffsetClubCount;
            Assert.AreEqual((uint)Clubs.Length, CanonicalSerializer.ReadU32(blob, ref o), "row 4: clubCount");
        }

        // ── Offset helpers (derived from the encoded blob, not hardcoded) ──────────

        // These mirror SeasonStateCodec's private element widths (Appendix B rows 5/7/8/10). They cannot
        // be shared with the codec without widening its surface, so TableRowOffset_LandsOnTheFirstRow
        // below asserts the walk actually lands on the row it claims — a width that drifts out of sync
        // fails there rather than silently corrupting a different field in every offset-based test.
        private const int BytesPerClubId  = 4;        // i32
        private const int BytesPerFixture = 4 + 4 + 4 + 1;
        private const int BytesPerRoundDay = 4;       // u32
        private const int BytesPerTableRow = 9 * 4;   // 9 x i32

        [Test]
        public void TableRowOffset_LandsOnTheFirstRow()
        {
            // Coherence guard for the helper above: the walked offset must point at row 0's ClubId.
            SeasonState s = MidSeason();
            byte[] blob = SeasonStateCodec.Encode(s);

            int o = TableRowOffset(blob, rowIndex: 0);
            Assert.AreEqual(s.TableRowsInClubIdOrder()[0].ClubId, CanonicalSerializer.ReadI32(blob, ref o),
                "The offset helper must land on table row 0's ClubId, or every offset-based test in " +
                "this fixture is corrupting a field other than the one it names.");
        }

        // The byte offset of table row `rowIndex`'s first field (ClubId), computed by walking the
        // variable-length blocks that precede the table.
        private static int TableRowOffset(byte[] blob, int rowIndex)
        {
            int o = OffsetClubCount;
            int clubCount = (int)CanonicalSerializer.ReadU32(blob, ref o);
            o += clubCount * BytesPerClubId;

            int fixtureCount = (int)CanonicalSerializer.ReadU32(blob, ref o);
            o += fixtureCount * BytesPerFixture;

            o += 4;                                                   // calendar cursor
            int roundCount = (int)CanonicalSerializer.ReadU32(blob, ref o);
            o += roundCount * BytesPerRoundDay;

            o += 4;                                                   // tableRowCount prefix
            return o + rowIndex * BytesPerTableRow;
        }

        private static int GoalDifferenceOffset(byte[] blob, int rowIndex) =>
            TableRowOffset(blob, rowIndex) + 7 * 4;   // clubId,P,W,D,L,GF,GA then GD
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T1): round-trip field identity      |
// |         |            |        | (fresh / mid-season / completed), per-column and scalar locks,  |
// |         |            |        | encode determinism + non-vacuity, the pinned-offset layout      |
// |         |            |        | lock, and every FR-SN-023 fail-loud gate.                       |
// | 1.1     | 2026-07-25 | —      | AR pass 1: negative-COLUMN test renamed off the misleading      |
// |         |            |        | "count" name; offset-helper widths named + a coherence guard    |
// |         |            |        | (TableRowOffset_LandsOnTheFirstRow) so a layout change fails    |
// |         |            |        | loudly instead of walking every probe to the wrong bytes.       |
#endregion
