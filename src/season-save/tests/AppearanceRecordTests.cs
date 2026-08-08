// File:     src/season-save/tests/AppearanceRecordTests.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 / Appendix B (the appearance record, ERR-041-010(b));
//           Injuries & Medical #41 FR-MD-010 (the window unit); ERR-030-027 (current-day exclusion);
//           Code Standards #20
// Purpose:  Locks the ERR-041-010(b) appearance record end to end: the window arithmetic (including
//           the multi-bit path a 7-day fixture calendar can never exercise), the APPR codec's framing
//           gates, the season loop writing both clubs' fielded XIs on a played round, the FromBlocks
//           copy discipline, and the save-file round trip.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class AppearanceRecordTests
    {
        private const int ManagerId = 77;
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const int ClubCount = 4;

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, ClubCount);

        private static CareerTestRoster.MutableSquadProvider ProviderOver(League league)
        {
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] ids = league.ClubIds();
            for (int i = 0; i < ids.Length; i++)
            {
                provider.Set(league.ResolveByClubId(ids[i]));
            }

            return provider;
        }

        // ── the window arithmetic ──────────────────────────────────────────────────────────

        [Test]
        public void FreshState_ReadsZeroOnAnyDay()
        {
            var state = default(AppearanceState);
            Assert.AreEqual(0, AppearanceWindow.AppearanceDaysOn(in state, 0u),
                "default(AppearanceState) is the valid fresh state — no day-0 trap");
            Assert.AreEqual(0, AppearanceWindow.AppearanceDaysOn(in state, 1000u));
        }

        [Test]
        public void TheCurrentDayIsNeverCounted()
        {
            // The ERR-030-027 exclusion, asserted directly: the occurrence draw runs pre-round, so a
            // window that could contain the current day would let a draw see a match not yet played.
            // (In production the write happens post-round, so this read shape cannot even arise on a
            // fixture day — this locks the window's own contract independent of call order.)
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);

            Assert.AreEqual(0, AppearanceWindow.AppearanceDaysOn(in state, 10u),
                "a match on day d must NOT feed a draw on day d (FR-MD-010 / ERR-030-027)");
            Assert.AreEqual(1, AppearanceWindow.AppearanceDaysOn(in state, 11u),
                "…and must feed the draw on day d+1");
        }

        [Test]
        public void AnAppearanceAgesOutOfTheWindowExactly()
        {
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);

            int window = InjuriesMedicalConstants.AppearanceWindowDays;
            Assert.AreEqual(1, AppearanceWindow.AppearanceDaysOn(in state, 10u + (uint)window),
                "age == window: still counted (the window is [day - window, day - 1])");
            Assert.AreEqual(0, AppearanceWindow.AppearanceDaysOn(in state, 11u + (uint)window),
                "age == window + 1: aged out");
        }

        [Test]
        public void RecordingTheSameDayTwice_IsIdempotent()
        {
            // A round records the XI once per fixture; a re-entrant save/restore path must not be able
            // to double-count a matchday.
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);
            AppearanceWindow.Record(ref state, 10u);

            Assert.AreEqual(1, AppearanceWindow.AppearanceDaysOn(in state, 11u));
        }

        [Test]
        public void TheMultiBitPath_CountsEveryAppearanceInTheWindow()
        {
            // The 7-day fixture calendar makes the season-driven count identically 0 or 1, so this path
            // is locked by hand-driving a congested week — a season test structurally cannot fail it.
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);
            AppearanceWindow.Record(ref state, 13u);
            AppearanceWindow.Record(ref state, 16u);

            Assert.AreEqual(3, AppearanceWindow.AppearanceDaysOn(in state, 17u),
                "days 10, 13, 16 all sit inside [10, 16]");
            Assert.AreEqual(2, AppearanceWindow.AppearanceDaysOn(in state, 18u),
                "day 10 has aged out of [11, 17]; 13 and 16 remain");
            Assert.AreEqual(1, AppearanceWindow.AppearanceDaysOn(in state, 21u),
                "only day 16 sits inside [14, 20]");
            Assert.AreEqual(0, AppearanceWindow.AppearanceDaysOn(in state, 24u),
                "everything has aged out of [17, 23]");
        }

        [Test]
        public void TimeGoingBackwards_FailsLoud()
        {
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);

            Assert.Throws<ArgumentException>(() => AppearanceWindow.Record(ref state, 9u),
                "re-anchoring backwards would silently discard newer appearances");
            Assert.Throws<ArgumentException>(
                () => AppearanceWindow.AppearanceDaysOn(in state, 9u),
                "a read from the anchor's past has no well-defined window");
        }

        [Test]
        public void ALongGap_ShiftsEverythingOut_WithoutOverflow()
        {
            // shift >= 32 would be undefined behaviour on a raw C# shift (it masks to shift % 32);
            // Record guards it explicitly. 40 days later the mask must read empty, not wrapped.
            var state = default(AppearanceState);
            AppearanceWindow.Record(ref state, 10u);
            AppearanceWindow.Record(ref state, 50u);

            Assert.AreEqual(1, AppearanceWindow.AppearanceDaysOn(in state, 51u),
                "only the fresh appearance survives a 40-day gap — bit 40 must not wrap to bit 8");
        }

        // ── the APPR codec ─────────────────────────────────────────────────────────────────

        [Test]
        public void Codec_RoundTripsFieldIdentically()
        {
            var clubs = new[]
            {
                new ClubAppearanceStates(
                    3,
                    new[] { 100, 200 },
                    new[]
                    {
                        new AppearanceState { RecentBits = 0b1001u, BitsAsOfWorldDay = 21 },
                        default(AppearanceState),
                    }),
                new ClubAppearanceStates(
                    9,
                    new[] { 55 },
                    new[] { new AppearanceState { RecentBits = 1u, BitsAsOfWorldDay = 7 } }),
            };

            ClubAppearanceStates[] got = AppearanceSaveCodec.Decode(AppearanceSaveCodec.Encode(clubs));

            Assert.AreEqual(2, got.Length);
            Assert.AreEqual(3, got[0].ClubId);
            CollectionAssert.AreEqual(new[] { 100, 200 }, got[0].PlayerIds);
            Assert.AreEqual(0b1001u, got[0].States[0].RecentBits);
            Assert.AreEqual(21u, got[0].States[0].BitsAsOfWorldDay);
            Assert.AreEqual(0u, got[0].States[1].RecentBits);
            Assert.AreEqual(9, got[1].ClubId);
            Assert.AreEqual(7u, got[1].States[0].BitsAsOfWorldDay);
        }

        [Test]
        public void Codec_RefusesAForeignBlock_ByMagicNotShape()
        {
            // The ERR-029-005 class: every sub-blob format sits at version 1, so only the magic can
            // tell one format from another. A real #29 training block must be refused by name.
            byte[] trainingBytes = TrainingSaveCodec.Encode(new[]
            {
                new ClubTrainingStates(
                    1, new[] { 10 }, new[] { TrainingState.Create(TrainingFocus.Balanced) }),
            });

            Assert.Throws<InvalidOperationException>(
                () => AppearanceSaveCodec.Decode(trainingBytes),
                "a training block in the appearance slot must be refused by its magic");
        }

        [Test]
        public void Codec_RefusesAStaleFormatVersion()
        {
            byte[] blob = AppearanceSaveCodec.Encode(Array.Empty<ClubAppearanceStates>());
            int o = 4;   // past the magic
            CanonicalSerializer.WriteU32(blob, ref o, SeasonSaveConstants.APPEARANCE_SAVE_FORMAT_VERSION + 1);

            Assert.Throws<InvalidOperationException>(() => AppearanceSaveCodec.Decode(blob),
                "no cross-version migration at Stage 0");
        }

        [Test]
        public void Codec_RefusesTrailingBytes_AndUnorderedIds()
        {
            byte[] blob = AppearanceSaveCodec.Encode(new[]
            {
                new ClubAppearanceStates(1, new[] { 10 }, new AppearanceState[1]),
            });
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);
            Assert.Throws<InvalidOperationException>(() => AppearanceSaveCodec.Decode(padded));

            // Encode canonicalizes, so an unordered blob can only be hand-built: swap the two player
            // records of a two-player club at the byte level and Decode must refuse.
            byte[] two = AppearanceSaveCodec.Encode(new[]
            {
                new ClubAppearanceStates(1, new[] { 10, 20 }, new AppearanceState[2]),
            });
            const int firstPlayerOffset = 4 + 4 + 4 + 4 + 4;   // magic, version, clubCount, clubId, playerCount
            const int bytesPerPlayer = 12;
            var swapped = (byte[])two.Clone();
            Array.Copy(two, firstPlayerOffset, swapped, firstPlayerOffset + bytesPerPlayer, bytesPerPlayer);
            Array.Copy(two, firstPlayerOffset + bytesPerPlayer, swapped, firstPlayerOffset, bytesPerPlayer);

            Assert.Throws<InvalidOperationException>(() => AppearanceSaveCodec.Decode(swapped),
                "player ids must be strictly ascending in a decoded block");
        }

        // ── the season loop writes it ──────────────────────────────────────────────────────

        [Test]
        public void APlayedRound_RecordsExactlyTheFieldedEleven_ForBothClubsOfEveryFixture()
        {
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = loop.World.CurrentWorldTick;
            loop.AdvanceAndPlayNextRound(provider);

            // A 4-club round is 2 fixtures covering all 4 clubs — the away sides are not a mirror
            // afterthought (ERR-008-002's class), so every club is asserted, not just fixture 0's home.
            ClubAppearanceStates[] blocks = career.AppearanceBlocks();
            Assert.AreEqual(ClubCount, blocks.Length);
            for (int c = 0; c < blocks.Length; c++)
            {
                int fielded = 0;
                for (int i = 0; i < blocks[c].Count; i++)
                {
                    int days = AppearanceWindow.AppearanceDaysOn(in blocks[c].States[i], fixtureDay + 1u);
                    Assert.LessOrEqual(days, 1, "one round is one appearance");
                    fielded += days;
                }

                Assert.AreEqual(MatchEngineConstants.PLAYERS_PER_TEAM, fielded,
                    $"club {blocks[c].ClubId}: exactly the starting eleven carries an appearance — " +
                    "not the bench, not the whole squad");
            }
        }

        [Test]
        public void TheRecordSurvivesTheSaveFile()
        {
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            var world = new WorldStore(ManagerId, WorldSeed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = world.CurrentWorldTick;
            loop.AdvanceAndPlayNextRound(provider);

            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".tdsave");
            try
            {
                SeasonSaveManager.Save(loop, matchOrNull: null, path);
                SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
                PlayerCareerStates restored = PlayerCareerStates.FromBlocks(
                    contents.TrainingClubs, contents.MedicalClubs, contents.AppearanceClubs,
                    injuryOccurrenceEnabled: false);

                ClubAppearanceStates[] before = career.AppearanceBlocks();
                ClubAppearanceStates[] after = restored.AppearanceBlocks();
                for (int c = 0; c < before.Length; c++)
                {
                    for (int i = 0; i < before[c].Count; i++)
                    {
                        Assert.AreEqual(
                            before[c].States[i].RecentBits, after[c].States[i].RecentBits,
                            "an appearance recorded before the save must be an appearance after it");
                        Assert.AreEqual(
                            before[c].States[i].BitsAsOfWorldDay, after[c].States[i].BitsAsOfWorldDay);
                    }
                }

                Assert.Greater(fixtureDay, 0u, "precondition: a round was actually played");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        [Test]
        public void FromBlocks_CopiesTheAppearanceArrays_SoTheBlocksAreNotABackDoor()
        {
            // The AR pass-6 question, asked of the new state set up front: which test fails if the
            // Array.Copy is reverted? This one. ClubAppearanceStates.States is a public array field
            // over a borrowed array, and the restore path hands FromBlocks the arrays Load returns
            // inside the public SeasonSaveContents — sharing them would make any holder a second
            // writer of the record the occurrence risk reads.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates source = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);

            ClubAppearanceStates[] appearance = source.AppearanceBlocks();
            PlayerCareerStates career = PlayerCareerStates.FromBlocks(
                source.TrainingBlocks(), source.MedicalBlocks(), appearance,
                injuryOccurrenceEnabled: false);

            appearance[0].States[0].RecentBits = 0xFFFFFFFFu;
            appearance[0].States[0].BitsAsOfWorldDay = 999u;

            Assert.AreEqual(0u, career.AppearanceBlocks()[0].States[0].RecentBits,
                "FromBlocks must COPY the appearance arrays — a mutation through the handed-in block " +
                "must not reach the career");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2, ERR-041-010(b)): window  |
// |         |            |        | arithmetic incl. the multi-bit path, APPR codec gates, the round  |
// |         |            |        | recording all four clubs' XIs, the save round trip, and the       |
// |         |            |        | FromBlocks copy lock.                                              |
#endregion
