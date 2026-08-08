// File:     src/season-save/tests/AppearanceRecordTests.cs
// Created:  2026-08-07
// Modified: 2026-08-08
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
using TacticalDirector.PlayerDatabase;
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
                // Identity, not just count (AR pass 1): the recorded set must BE the eleven the
                // selector fields from the filtered squad — the same walk both resolution modes use
                // (the recording path does not branch on mode), so this is the mode-independence
                // lock too.
                int[] expectedXi = SquadRating.StartingElevenPlayerIds(
                    career.SelectAvailable(provider.ResolveByClubId(blocks[c].ClubId)));
                var recorded = new System.Collections.Generic.List<int>();
                for (int i = 0; i < blocks[c].Count; i++)
                {
                    int days = AppearanceWindow.AppearanceDaysOn(in blocks[c].States[i], fixtureDay + 1u);
                    Assert.LessOrEqual(days, 1, "one round is one appearance");
                    if (days == 1)
                    {
                        recorded.Add(blocks[c].PlayerIds[i]);
                    }
                }

                CollectionAssert.AreEquivalent(expectedXi, recorded,
                    $"club {blocks[c].ClubId}: exactly the starting eleven the selector fields " +
                    "carries an appearance — not the bench, not the whole squad, not a different XI");
            }
        }

        [Test]
        public void AnInjuredStarter_IsExcludedFromTheRecordedEleven()
        {
            // The filter PARTICIPATES in the recorded identity (AR pass 2): with every player fit,
            // APlayedRound's expected-XI recomputation and the production path could both be reading
            // the unfiltered squad and still agree. An injured first-choice starter forces the two
            // elevens apart, so this fails if either side stops going through SelectAvailable.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = loop.World.CurrentWorldTick;

            int clubId = league.ClubIds()[0];
            int[] fitXi = SquadRating.StartingElevenPlayerIds(provider.ResolveByClubId(clubId));
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Moderate;
            injured.RecoveryRemaining = 12;
            career.SetMedicalState(clubId, fitXi[0], in injured);

            loop.AdvanceAndPlayNextRound(provider);

            ClubAppearanceStates block = BlockFor(career, clubId);
            var recorded = new System.Collections.Generic.List<int>();
            for (int i = 0; i < block.Count; i++)
            {
                if (AppearanceWindow.AppearanceDaysOn(in block.States[i], fixtureDay + 1u) == 1)
                {
                    recorded.Add(block.PlayerIds[i]);
                }
            }

            CollectionAssert.DoesNotContain(recorded, fitXi[0],
                "an injured starter must not carry an appearance — he was not fielded");
            int[] filteredXi = SquadRating.StartingElevenPlayerIds(
                career.SelectAvailable(provider.ResolveByClubId(clubId)));
            CollectionAssert.AreEquivalent(filteredXi, recorded,
                "the recorded eleven is the FILTERED selector's eleven");
            CollectionAssert.AreNotEquivalent(fitXi, filteredXi,
                "precondition: the injury genuinely changed the eleven, or this test proves nothing");
        }

        // ── the record's own refusals (direct) ─────────────────────────────────────────────

        [Test]
        public void RecordAppearances_UnknownIds_RecordNothingAtAll()
        {
            // The validate-all-then-write promise, asserted through the borrowed block arrays: a
            // throw on ANY id must leave every OTHER id of the club unwritten too — half a matchday
            // in the record is worse than none, because the retry then double-counts nobody but
            // still injures off a phantom appearance.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            int clubId = league.ClubIds()[0];
            ClubAppearanceStates block = BlockFor(career, clubId);
            int validId = block.PlayerIds[0];

            Assert.Throws<ArgumentException>(
                () => career.RecordAppearances(clubId, new[] { validId, int.MaxValue }, 10u),
                "an id the career does not carry refuses the write");
            Assert.Throws<ArgumentException>(
                () => career.RecordAppearances(int.MaxValue, new[] { validId }, 10u),
                "a club the career does not carry refuses the write");
            Assert.Throws<ArgumentNullException>(
                () => career.RecordAppearances(clubId, null, 10u));

            for (int i = 0; i < block.Count; i++)
            {
                Assert.AreEqual(0u, block.States[i].RecentBits,
                    "nothing may be recorded when any id fails — validate ALL, then write");
            }
        }

        [Test]
        public void RecordAppearances_DayRegression_RecordsNothingNew()
        {
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            int clubId = league.ClubIds()[0];
            ClubAppearanceStates block = BlockFor(career, clubId);
            int anchored = block.PlayerIds[0];
            int fresh = block.PlayerIds[1];

            career.RecordAppearances(clubId, new[] { anchored }, 20u);

            // `fresh` is listed FIRST: an interleaved validate-and-write would set his bit before
            // reaching the regression on `anchored` — exactly the half-recorded state the pre-check
            // exists to prevent (AR pass 1), so this ordering is the mutant-killer.
            Assert.Throws<ArgumentException>(
                () => career.RecordAppearances(clubId, new[] { fresh, anchored }, 10u),
                "a day before an already-recorded appearance is a wrong-career pairing or clock fault");

            Assert.AreEqual(0u, block.States[1].RecentBits,
                "the player listed before the offender must not have been written");
            Assert.AreEqual(20u, block.States[0].BitsAsOfWorldDay,
                "the anchored player's record is untouched by the refused write");
            Assert.AreEqual(1u, block.States[0].RecentBits);
        }

        [Test]
        public void ARecordingThrow_LeavesTheFixtureUnplayed_SoTheRoundIsRetryable()
        {
            // AR pass 1 moved the recording ABOVE apply/emit/mark because a throw after
            // MarkFixturePlayed strands the round; this is the lock that fails if it moves back.
            // Poison: pre-anchor one of the away side's would-be starters past today, so recording
            // trips the regression refusal AFTER the home club recorded — the worst-ordered throw.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceToNextFixtureDay();
            int round = loop.NextRoundIndex;
            int[] unplayed = loop.State.UnplayedFixtureIndicesInRound(round);
            Fixture first = loop.State.FixtureAt(unplayed[0]);

            int[] awayXi = SquadRating.StartingElevenPlayerIds(
                career.SelectAvailable(provider.ResolveByClubId(first.AwayClubId)));
            career.RecordAppearances(
                first.AwayClubId, new[] { awayXi[0] }, loop.CurrentWorldDay + 100u);

            Assert.Throws<ArgumentException>(() => loop.AdvanceAndPlayNextRound(provider));

            CollectionAssert.Contains(
                loop.State.UnplayedFixtureIndicesInRound(round), unplayed[0],
                "the fixture whose recording threw must remain unplayed — recording runs before " +
                "apply/emit/mark, so nothing was committed for it");
            Assert.AreEqual(round, loop.NextRoundIndex,
                "the round cursor must not advance past a stranded fixture");

            // AR pass 3 (L): the pair form validates BOTH clubs before writing EITHER, so the
            // worst-ordered throw must leave the HOME side unwritten too — no phantom appearance
            // for a match that was never applied.
            ClubAppearanceStates homeBlock = BlockFor(career, first.HomeClubId);
            for (int i = 0; i < homeBlock.Count; i++)
            {
                Assert.AreEqual(0u, homeBlock.States[i].RecentBits,
                    "the home XI must not carry an appearance for the refused fixture");
            }
        }

        [Test]
        public void TheRecordedXi_ComesFromTheResolutionsOwnSquadInstance()
        {
            // The pass-2 Medium's DISCRIMINATING lock (AR pass 3, M4): the two locks recorded at pass
            // 2 compute their expected XI through the same SelectAvailable walk the deleted code used,
            // so both pass against the pre-fix loop. This one does not. Pre-fix, the recording
            // re-resolved the club from the provider AFTER the resolution — so a provider whose
            // roster shifts mid-round recorded an XI the match never fielded. Post-fix the ids come
            // out of ResolveFixture itself, and this test fails at the pre-fix commit.
            League league = FourClubLeague();
            int[] clubIds = league.ClubIds();
            var inner = new CareerTestRoster.MutableSquadProvider();
            for (int i = 0; i < clubIds.Length; i++)
            {
                inner.Set(CareerTestRoster.Build(clubIds[i], PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            }

            int clubId = clubIds[0];
            Squad rosterA = inner.ResolveByClubId(clubId);
            int[] xiA = SquadRating.StartingElevenPlayerIds(rosterA);

            // Roster B: the same 25 ids, with two same-position DEFENDER slots' ids swapped — one
            // selected in A, one not — so B's eleven is a different ID SET over identical ratings.
            int inSlot = -1, outSlot = -1;
            for (int k = 0; k < rosterA.Count; k++)
            {
                if (CareerTestRoster.PosFor(k) != PlayerPosition.Defender)
                {
                    continue;
                }

                bool selected = Array.IndexOf(xiA, rosterA.GetPlayer(k).PlayerId) >= 0;
                if (selected && inSlot < 0)
                {
                    inSlot = k;
                }
                else if (!selected && outSlot < 0)
                {
                    outSlot = k;
                }
            }

            Assert.IsTrue(inSlot >= 0 && outSlot >= 0,
                "precondition: a selected and an unselected defender slot must both exist");

            var suffixes = new int[rosterA.Count];
            for (int k = 0; k < suffixes.Length; k++)
            {
                suffixes[k] = k;
            }

            (suffixes[inSlot], suffixes[outSlot]) = (suffixes[outSlot], suffixes[inSlot]);
            Squad rosterB = CareerTestRoster.Build(clubId, rosterA.Count, suffixes);
            CollectionAssert.AreNotEquivalent(
                xiA, SquadRating.StartingElevenPlayerIds(rosterB),
                "precondition: B's eleven must be a different id set, or the shift is unobservable");

            var provider = new ShiftingSquadProvider(inner, clubId, rosterB);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                provider, clubIds, injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = loop.CurrentWorldDay;

            // Armed, the fixture day resolves the poisoned club exactly three times: slot-2 training,
            // slot-4 medical, then the round's own ResolveFixture — which sees roster A. Any LATER
            // resolve (the deleted re-recording walk was one) sees roster B. If the loop's call shape
            // changes, the identity assertion below fails visibly and this constant is re-fitted.
            provider.Arm(resolvesBeforeShift: 3);
            loop.AdvanceAndPlayNextRound(provider);

            ClubAppearanceStates block = BlockFor(career, clubId);
            var recorded = new System.Collections.Generic.List<int>();
            for (int i = 0; i < block.Count; i++)
            {
                if (AppearanceWindow.AppearanceDaysOn(in block.States[i], fixtureDay + 1u) == 1)
                {
                    recorded.Add(block.PlayerIds[i]);
                }
            }

            CollectionAssert.AreEquivalent(xiA, recorded,
                "the record must carry the eleven the RESOLUTION fielded (roster A) — a re-resolve " +
                "after the fact would have recorded roster B's eleven, who never played");
            Assert.AreEqual(0, provider.PassThroughRemaining,
                "the shift must have gone LIVE during the round — an unconsumed pass-through budget " +
                "means the loop's call shape changed and this lock is passing vacuously; re-fit the " +
                "Arm() constant to the new shape");
        }

        /// <summary>
        /// Delegates to the inner provider until armed; from the (N+1)th armed resolve of the one
        /// poisoned club onward, hands back a different roster. See the test above for why.
        /// </summary>
        private sealed class ShiftingSquadProvider : ISquadProvider
        {
            private readonly CareerTestRoster.MutableSquadProvider _inner;
            private readonly int _club;
            private readonly Squad _later;
            private bool _armed;
            private int _passThroughRemaining;

            internal ShiftingSquadProvider(
                CareerTestRoster.MutableSquadProvider inner, int club, Squad later)
            {
                _inner = inner;
                _club = club;
                _later = later;
            }

            internal void Arm(int resolvesBeforeShift)
            {
                _armed = true;
                _passThroughRemaining = resolvesBeforeShift;
            }

            /// <summary>Pass-through budget left. 0 after the round ⇒ the shift went LIVE — the
            /// vacuous-pass guard (AR pass 4: a call-shape change that REMOVES a resolve would
            /// otherwise leave the shift unfired and the identity assertion satisfied trivially,
            /// including against the pre-fix loop this lock exists to kill).</summary>
            internal int PassThroughRemaining => _passThroughRemaining;

            public Squad ResolveByClubId(int clubId)
            {
                Squad squad = _inner.ResolveByClubId(clubId);
                if (!_armed || clubId != _club)
                {
                    return squad;
                }

                if (_passThroughRemaining > 0)
                {
                    _passThroughRemaining--;
                    return squad;
                }

                return _later;
            }
        }

        private static ClubAppearanceStates BlockFor(PlayerCareerStates career, int clubId)
        {
            ClubAppearanceStates[] blocks = career.AppearanceBlocks();
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].ClubId == clubId)
                {
                    return blocks[i];
                }
            }

            throw new InvalidOperationException($"club {clubId} not carried by the career under test");
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

                // An all-zero record would round-trip green, so prove something WAS recorded first
                // (AR pass 1): the played round must have set a bit for every club's eleven.
                int recordedBeforeSave = 0;
                for (int c = 0; c < before.Length; c++)
                {
                    for (int i = 0; i < before[c].Count; i++)
                    {
                        if (before[c].States[i].RecentBits != 0)
                        {
                            recordedBeforeSave++;
                        }
                    }
                }

                Assert.AreEqual(ClubCount * MatchEngineConstants.PLAYERS_PER_TEAM, recordedBeforeSave,
                    "precondition: every club's eleven carries a recorded appearance before the save");

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
// | 1.1     | 2026-08-07 | —      | Balance-pass AR pass 1 (2L): the round lock asserts XI IDENTITY   |
// |         |            |        | against the selector's own ids (count alone could not see a       |
// |         |            |        | wrong eleven, and this doubles as the mode-independence lock);    |
// |         |            |        | the save round trip proves 44 bits were recorded before asserting |
// |         |            |        | they survive (an all-zero record round-tripped green).            |
// | 1.2     | 2026-08-07 | —      | Balance-pass AR pass 2 (4L): + the direct RecordAppearances       |
// |         |            |        | refusals (unknown id / unknown club / regression, each proving    |
// |         |            |        | NOTHING was written — the fresh-listed-first ordering is the      |
// |         |            |        | interleaved-write mutant-killer); + the recording-throw-leaves-   |
// |         |            |        | the-fixture-unplayed lock on the pass-1 ordering fix; + the       |
// |         |            |        | injured-starter case, which forces the filter to PARTICIPATE in   |
// |         |            |        | the XI-identity assertion (all-fit squads let both sides read the |
// |         |            |        | unfiltered squad and still agree).                                |
// | 1.3     | 2026-08-08 | —      | Balance-pass AR pass 3 (M4): + TheRecordedXi_ComesFromThe-        |
// |         |            |        | ResolutionsOwnSquadInstance — the one lock that FAILS against the |
// |         |            |        | pre-fix loop, via a mid-round roster-shifting provider; the       |
// |         |            |        | unplayed-fixture lock asserts the home side unwritten (the pair   |
// |         |            |        | form). Row added at pass 4 — the pass-3 edit shipped rowless, the |
// |         |            |        | third recurrence of the FR-CS-057 class in this chain.            |
// | 1.4     | 2026-08-08 | —      | Balance-pass AR pass 4 (L4): the shifting provider exposes its    |
// |         |            |        | remaining pass-through budget and the lock asserts it was         |
// |         |            |        | CONSUMED — a call-shape change that removes a resolve would       |
// |         |            |        | otherwise leave the shift unfired and the lock passing vacuously, |
// |         |            |        | including against the pre-fix loop it exists to kill.             |
#endregion
