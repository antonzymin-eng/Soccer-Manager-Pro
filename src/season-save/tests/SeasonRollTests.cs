// File:     src/season-save/tests/SeasonRollTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Season & Competition Loop #30 §3.5 (season-boundary roll), FR-SN-029 (restartable
//           transform), FR-SN-030 (two-run season determinism), FR-SN-031 (insertion points),
//           FR-SN-032 (sole writer); path-to-playable A5; Code Standards #20
// Purpose:  Locks the #30 T3 boundary roll: the pure helpers (job-security evaluation, calendar shift,
//           seed derivation), the fail-loud gates and their atomicity, the restartability contract, and
//           — the acceptance test — that a rolled season is actually PLAYABLE into a second season.
//
// Everything runs in RoundResolutionMode.QuickSimAll so a two-season career resolves in milliseconds
// (the same reasoning as SeasonLoopTests: the real-engine path is proven in the Simulation-layer
// capstone, and putting a 90-minute match in the unit suite would buy nothing this file asserts).

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonRollTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const int ManagerId = 1;

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, 4);

        private static SeasonLoop NewLoop(League league, out WorldStore world, int managedClubId = 0)
        {
            world = new WorldStore(ManagerId, WorldSeed);
            return new SeasonLoop(
                world, league.CreateSeason(managedClubId), RoundResolutionMode.QuickSimAll);
        }

        private static void PlayWholeSeason(SeasonLoop loop, League league)
        {
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(league);
            }
        }

        // ── The pure §3.5 step (b) arithmetic (BoardState.EvaluateAtSeasonEnd) ─────────────

        private static BoardState Board(int target, int security) =>
            new BoardState(new BoardObjective(target), security);

        [Test]
        public void EvaluateAtSeasonEnd_ObjectiveMet_GainsTheFlatAmount()
        {
            BoardState after = Board(target: 4, security: 500).EvaluateAtSeasonEnd(finalPosition: 2);

            Assert.AreEqual(
                500 + SeasonLoopConstants.BoardJobSecurityMetDeltaPerMille, after.JobSecurityPerMille);
        }

        [Test]
        public void EvaluateAtSeasonEnd_ExactlyOnTarget_CountsAsMet()
        {
            // BoardObjective is "position or better", so finishing exactly on it is a pass — the
            // off-by-one that would quietly punish a manager who hit the target precisely.
            BoardState after = Board(target: 4, security: 500).EvaluateAtSeasonEnd(finalPosition: 4);

            Assert.AreEqual(
                500 + SeasonLoopConstants.BoardJobSecurityMetDeltaPerMille, after.JobSecurityPerMille);
        }

        [Test]
        public void EvaluateAtSeasonEnd_MissedPenalty_ScalesWithPlacesShort()
        {
            int byOne = Board(4, 1000).EvaluateAtSeasonEnd(finalPosition: 5).JobSecurityPerMille;
            int byThree = Board(4, 1000).EvaluateAtSeasonEnd(finalPosition: 7).JobSecurityPerMille;

            Assert.AreEqual(1000 - SeasonLoopConstants.BoardJobSecurityMissedDeltaPerMille, byOne);
            Assert.AreEqual(1000 - (3 * SeasonLoopConstants.BoardJobSecurityMissedDeltaPerMille), byThree);
            Assert.Less(byThree, byOne, "Missing by more must cost more — a flat penalty would tie them.");
        }

        [Test]
        public void EvaluateAtSeasonEnd_ClampsToTheScaleAtBothEnds()
        {
            Assert.AreEqual(
                SeasonLoopConstants.JobSecurityScale,
                Board(4, SeasonLoopConstants.JobSecurityScale)
                    .EvaluateAtSeasonEnd(finalPosition: 1).JobSecurityPerMille,
                "A run of good seasons saturates rather than banking unbounded credit.");

            Assert.AreEqual(
                0,
                Board(1, 10).EvaluateAtSeasonEnd(finalPosition: 20).JobSecurityPerMille,
                "A catastrophic season floors at zero rather than going negative (BoardState would throw).");
        }

        [Test]
        public void EvaluateAtSeasonEnd_AgreesWithTheObjectiveItReports()
        {
            // The verdict a career screen shows and the consequence it explains must come from ONE rule.
            // Before this moved onto BoardState the branch was a second copy of IsMetBy, so extending the
            // objective model (#45) would have moved the reported verdict and left the penalty behind.
            BoardState board = Board(target: 4, security: 500);

            for (int position = 1; position <= 8; position++)
            {
                BoardState after = board.EvaluateAtSeasonEnd(position);
                bool gained = after.JobSecurityPerMille > board.JobSecurityPerMille;

                Assert.AreEqual(board.Objective.IsMetBy(position), gained,
                    $"Position {position}: job security must move in the direction IsMetBy reports.");
                Assert.AreEqual(board.IsOnTrack(position), gained,
                    $"Position {position}: the running read and the boundary verdict must agree.");
            }
        }

        // ── The pure calendar shift (SeasonCalendar.ShiftedToNextSeason) ──────────────────

        [Test]
        public void ShiftedToNextSeason_OpensExactlyOneBreakAfterTheOldFinale()
        {
            SeasonCalendar old = SeasonCalendar.Linear(roundCount: 6, firstRoundDay: 7, daysBetweenRounds: 7);
            uint oldLastDay = old.DayOfRound(5);

            SeasonCalendar next = old.ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays);

            Assert.AreEqual(oldLastDay + SeasonLoopConstants.SeasonBreakDays, next.DayOfRound(0));
            Assert.AreEqual(0, next.NextRoundIndex, "The new season starts at round 0.");
            Assert.AreEqual(old.RoundCount, next.RoundCount);
        }

        [Test]
        public void ShiftedToNextSeason_PreservesANonUniformSchedule()
        {
            // A calendar with a mid-season break. Rebuilding a LINEAR calendar would silently flatten it;
            // shifting the shape keeps it, which is what makes the transform pure in the old season.
            var days = new uint[] { 10, 17, 24, 60, 67 };
            SeasonCalendar old = SeasonCalendar.Create(nextRoundIndex: days.Length, roundToDay: days);

            SeasonCalendar next = old.ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays);

            uint shift = next.DayOfRound(0) - old.DayOfRound(0);
            for (int i = 0; i < days.Length; i++)
            {
                Assert.AreEqual(days[i] + shift, next.DayOfRound(i),
                    $"Round {i} must keep its offset — the 36-day mid-season gap survives the roll.");
            }
        }

        [Test]
        public void ShiftedToNextSeason_SingleRoundCalendar_StillMovesForward()
        {
            // The degenerate shape the season-length term contributes nothing to: with one round,
            // (last - first) is zero, so the whole shift IS the close season. If breakDays were allowed
            // to be zero this calendar would reproduce itself and the "next" season would be this one.
            SeasonCalendar old = SeasonCalendar.Create(nextRoundIndex: 1, roundToDay: new uint[] { 40 });

            SeasonCalendar next = old.ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays);

            Assert.AreEqual(40u + SeasonLoopConstants.SeasonBreakDays, next.DayOfRound(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => old.ShiftedToNextSeason(0));
        }

        [Test]
        public void ShiftedToNextSeason_OverflowingTheDayRange_FailsLoud()
        {
            SeasonCalendar old = SeasonCalendar.Create(
                nextRoundIndex: 2, roundToDay: new uint[] { uint.MaxValue - 10, uint.MaxValue - 1 });

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => old.ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays));
        }

        // ── The pure seed derivation ───────────────────────────────────────────────────────

        [Test]
        public void DeriveNextSeasonSeed_IsDeterministicAndSeparatesItsInputs()
        {
            ulong a = SeasonLoop.DeriveNextSeasonSeed(0xABCDEF0123456789UL, 0);

            Assert.AreEqual(a, SeasonLoop.DeriveNextSeasonSeed(0xABCDEF0123456789UL, 0));
            Assert.AreNotEqual(a, SeasonLoop.DeriveNextSeasonSeed(0xABCDEF0123456789UL, 1),
                "The season NUMBER must move the derivation, or a career could cycle.");
            Assert.AreNotEqual(a, SeasonLoop.DeriveNextSeasonSeed(0xABCDEF012345678AUL, 0));
        }

        [Test]
        public void DeriveNextSeasonSeed_IsDomainSeparatedFromTheMatchSeed()
        {
            // Both derive from (seed, seasonNumber); different domain constants must keep them apart, so
            // a season's successor cannot correlate with any fixture played inside it.
            var fixture = new Fixture(roundIndex: 0, homeClubId: 0, awayClubId: 1);

            Assert.AreNotEqual(
                RoundResolutionModel.MatchSeedFor(in fixture, 0xABCDEF0123456789UL, 0),
                SeasonLoop.DeriveNextSeasonSeed(0xABCDEF0123456789UL, 0));
        }

        // ── Fail-loud gates, and their atomicity ───────────────────────────────────────────

        [Test]
        public void RollToNextSeason_MidSeason_FailsLoud()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.Throws<System.InvalidOperationException>(() => loop.RollToNextSeason());
        }

        [Test]
        public void RollToNextSeason_WorldPastTheNewOpeningDay_FailsLoudAndChangesNothing()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            PlayWholeSeason(loop, league);

            SeasonState before = loop.State.Clone();

            // Sit out the whole close season and then some, so the derived calendar would open in the past.
            uint lastRoundDay = loop.State.Calendar.DayOfRound(loop.State.Calendar.RoundCount - 1);
            while (world.CurrentWorldTick <= lastRoundDay + SeasonLoopConstants.SeasonBreakDays)
            {
                world.AdvanceDay();
            }

            Assert.Throws<System.InvalidOperationException>(() => loop.RollToNextSeason());

            // Atomicity: the board verdict must not have landed against a schedule that was then refused.
            Assert.IsTrue(before.FieldsEqual(loop.State),
                "A refused roll must leave the season completely untouched, not half-rolled.");
        }

        // ── The transform itself ───────────────────────────────────────────────────────────

        [Test]
        public void RollToNextSeason_ResetsTheSeasonAndAdvancesItsNumber()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            ulong previousSeed = loop.State.Seed;
            int previousNumber = loop.State.SeasonNumber;

            SeasonRollOutcome outcome = loop.RollToNextSeason();

            Assert.AreEqual(previousNumber + 1, loop.State.SeasonNumber);
            Assert.AreEqual(SeasonLoop.DeriveNextSeasonSeed(previousSeed, previousNumber), loop.State.Seed);
            Assert.AreEqual(0, loop.NextRoundIndex, "The cursor is back at round 0.");
            Assert.IsFalse(loop.IsSeasonComplete);

            foreach (Fixture fixture in loop.State.Fixtures)
            {
                Assert.IsFalse(fixture.Played, "Every fixture in the new season starts unplayed.");
            }

            foreach (LeagueTableRow row in loop.State.TableRowsInClubIdOrder())
            {
                Assert.AreEqual(0, row.Played, $"Club {row.ClubId}'s table row must be reset.");
                Assert.AreEqual(0, row.Points);
                Assert.AreEqual(0, row.GoalsFor);
                Assert.AreEqual(0, row.GoalsAgainst);
            }

            Assert.AreEqual(previousNumber, outcome.CompletedSeasonNumber);
            Assert.AreEqual(loop.State.SeasonNumber, outcome.NextSeasonNumber);
            Assert.AreEqual(loop.State.Seed, outcome.NextSeasonSeed);
            Assert.AreEqual(loop.State.Calendar.NextFixtureDay(), outcome.NextFirstFixtureDay);
        }

        [Test]
        public void RollToNextSeason_AppliesTheBoardVerdictToJobSecurity()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            int securityBefore = loop.State.Board.JobSecurityPerMille;
            PlayWholeSeason(loop, league);

            int finalPosition = loop.State.PositionOf(loop.State.ManagedClubId);
            SeasonRollOutcome outcome = loop.RollToNextSeason();

            Assert.AreEqual(finalPosition, outcome.FinalPosition);
            Assert.AreEqual(securityBefore, outcome.JobSecurityBeforePerMille);
            Assert.AreEqual(
                Board(outcome.TargetPosition, securityBefore)
                    .EvaluateAtSeasonEnd(finalPosition).JobSecurityPerMille,
                outcome.JobSecurityAfterPerMille);
            Assert.AreEqual(
                outcome.JobSecurityAfterPerMille, loop.State.Board.JobSecurityPerMille,
                "The verdict must be committed to the state, not only reported.");

            Assert.AreEqual(outcome.FinalPosition <= outcome.TargetPosition, outcome.ObjectiveMet);
            Assert.AreEqual(
                outcome.ObjectiveMet ? 0 : finalPosition - outcome.TargetPosition, outcome.PlacesShort);
        }

        [Test]
        public void RollToNextSeason_KeepsTheObjectiveAndTheClubSet()
        {
            // #30 owns the objective; re-negotiating it is #45's. The club set is unchanged until #43's
            // promotion/relegation transform occupies insertion point (a').
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            int target = loop.State.Board.Objective.TargetPositionOrBetter;
            int managedBefore = loop.State.ManagedClubId;
            var clubsBefore = new int[loop.State.ClubIds.Count];
            loop.State.ClubIds.CopyTo(clubsBefore, 0);

            PlayWholeSeason(loop, league);
            loop.RollToNextSeason();

            Assert.AreEqual(target, loop.State.Board.Objective.TargetPositionOrBetter);
            CollectionAssert.AreEqual(clubsBefore, loop.State.ClubIds);
            Assert.AreEqual(managedBefore, loop.State.ManagedClubId);
        }

        // ── FR-SN-029 restartability ───────────────────────────────────────────────────────

        [Test]
        public void RollToNextSeason_IsRestartableAcrossASave()
        {
            League league = FourClubLeague();

            // Straight through: play the season, roll.
            SeasonLoop straight = NewLoop(league, out _);
            PlayWholeSeason(straight, league);
            straight.RollToNextSeason();

            // Interrupted at the boundary — saved after the last round, rolled after the restore.
            SeasonLoop interrupted = NewLoop(league, out WorldStore world);
            PlayWholeSeason(interrupted, league);

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tdsave");
            try
            {
                SeasonSaveManager.Save(world, interrupted.State, null,
                    path, Array.Empty<ClubTrainingStates>(), Array.Empty<ClubInjuryStates>());
                SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
                var resumed = new SeasonLoop(
                    contents.World, contents.Season, RoundResolutionMode.QuickSimAll);

                resumed.RollToNextSeason();

                Assert.IsTrue(straight.State.FieldsEqual(resumed.State),
                    "A save taken at the boundary must restore to the same continuation (FR-SN-029).");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void RolledSeason_SurvivesASaveAndKeepsPlaying()
        {
            // The complement of the test above, and the likelier save point of the two: a player who
            // finishes a season, sees the board's verdict, and quits. The roll installs a schedule and a
            // calendar the codec has never been shown — and installing a state Encode writes but Decode
            // refuses is a defect this exact code path has produced once already (the T1 AR pass-1
            // finding on BeginNextSeason's vacuous coverage check). Nothing asserted it round-trips.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            PlayWholeSeason(loop, league);
            loop.RollToNextSeason();

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tdsave");
            try
            {
                SeasonSaveManager.Save(world, loop.State, null,
                    path, Array.Empty<ClubTrainingStates>(), Array.Empty<ClubInjuryStates>());
                SeasonSaveContents contents = SeasonSaveManager.Load(path, league);

                Assert.IsTrue(loop.State.FieldsEqual(contents.Season),
                    "A season saved just after the roll must decode field-identical.");

                var resumed = new SeasonLoop(
                    contents.World, contents.Season, RoundResolutionMode.QuickSimAll);

                // And it must still be playable — the ERR-030-015 property, across a file this time.
                resumed.AdvanceToNextFixtureDay();
                Assert.DoesNotThrow(() => resumed.AdvanceAndPlayNextRound(league));
                PlayWholeSeason(resumed, league);
                Assert.IsTrue(resumed.IsSeasonComplete);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void AdvanceDays_PastTheNextSeasonsOpeningDay_FailsLoudRatherThanStrandingTheCareer()
        {
            // The close season is days the client walks, and RollToNextSeason refuses a calendar that
            // would open in the past. Together those two facts mean stepping past the opening day before
            // rolling reaches a state with no way forward: the season cannot be played (complete) and
            // cannot be rolled (calendar behind the clock), the world clock only moves forward, and the
            // stuck state saves and reloads perfectly happily. AdvanceDays has to refuse the step.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            PlayWholeSeason(loop, league);

            uint before = world.CurrentWorldTick;
            int overshoot = (int)SeasonLoopConstants.SeasonBreakDays + 1;

            Assert.Throws<System.InvalidOperationException>(() => loop.AdvanceDays(overshoot));
            Assert.AreEqual(before, world.CurrentWorldTick,
                "A refused advance must not have moved the clock partway.");

            // The whole close season, to the day, is still allowed — the bound is the opening day itself,
            // matching the roll's own gate (which accepts world-day == opening day).
            loop.AdvanceDays((int)SeasonLoopConstants.SeasonBreakDays);
            SeasonRollOutcome outcome = loop.RollToNextSeason();

            Assert.AreEqual(world.CurrentWorldTick, outcome.NextFirstFixtureDay);
            Assert.DoesNotThrow(() => loop.AdvanceAndPlayNextRound(league),
                "Rolling on the opening day itself leaves a season that is immediately playable.");
        }

        [Test]
        public void RollToNextSeason_FromTheSamePriorState_IsDeterministic()
        {
            League league = FourClubLeague();

            SeasonLoop first = NewLoop(league, out _);
            PlayWholeSeason(first, league);
            first.RollToNextSeason();

            SeasonLoop second = NewLoop(league, out _);
            PlayWholeSeason(second, league);
            second.RollToNextSeason();

            Assert.IsTrue(first.State.FieldsEqual(second.State));
        }

        // ── FR-SN-030 across the boundary, and the acceptance test ────────────────────────

        [Test]
        public void RolledSeason_IsPlayable_AndTwoCareersAgreeOnBothTables()
        {
            // THE ACCEPTANCE TEST for A5. §3.5's pseudocode regenerates Fixtures and resets the Table but
            // never touches the Calendar — and a rolled season whose cursor is still at RoundCount is
            // permanently unplayable: AdvanceToNextFixtureDay throws (F5) and AdvanceAndPlayNextRound
            // throws, forever. Playing a SECOND season to completion is what catches that (ERR-030-015);
            // asserting only on the rolled state's fields would not.
            League league = FourClubLeague();

            SeasonLoop careerA = NewLoop(league, out WorldStore worldA);
            PlayWholeSeason(careerA, league);
            string firstTableA = TableFingerprint(careerA);
            careerA.RollToNextSeason();
            PlayWholeSeason(careerA, league);

            SeasonLoop careerB = NewLoop(league, out WorldStore worldB);
            PlayWholeSeason(careerB, league);
            string firstTableB = TableFingerprint(careerB);
            careerB.RollToNextSeason();
            PlayWholeSeason(careerB, league);

            Assert.AreEqual(firstTableA, firstTableB, "Season 1 must agree (FR-SN-030).");
            Assert.AreEqual(TableFingerprint(careerA), TableFingerprint(careerB),
                "Season 2 must agree too — determinism has to survive the boundary.");
            Assert.IsTrue(careerA.State.FieldsEqual(careerB.State));
            Assert.AreEqual(worldA.CurrentWorldTick, worldB.CurrentWorldTick);
            Assert.AreEqual(1, careerA.State.SeasonNumber);
        }

        [Test]
        public void SecondSeason_DiffersFromTheFirst_SoACareerDoesNotRepeatItself()
        {
            // The derived seed has to actually change the schedule, or every season would be a replay of
            // the last one with the same fixtures in the same order.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);

            var firstSchedule = new Fixture[loop.State.Fixtures.Count];
            loop.State.Fixtures.CopyTo(firstSchedule, 0);
            PlayWholeSeason(loop, league);
            string firstTable = TableFingerprint(loop);

            loop.RollToNextSeason();
            PlayWholeSeason(loop, league);

            var secondSchedule = new Fixture[loop.State.Fixtures.Count];
            loop.State.Fixtures.CopyTo(secondSchedule, 0);
            Assert.AreEqual(firstSchedule.Length, secondSchedule.Length);

            // Asserted SEPARATELY, not as a disjunction. The table always moves — season 2 quick-sims
            // against a different seed and season number — so "table OR schedule changed" is satisfied by
            // the table alone and says nothing about the schedule, which is the half this test is named
            // for. A regression that stopped feeding the derived seed to FixtureScheduler would replay the
            // identical fixture list every season and still pass a disjunction.
            int changedFixtures = 0;
            for (int i = 0; i < firstSchedule.Length; i++)
            {
                if (firstSchedule[i].HomeClubId != secondSchedule[i].HomeClubId ||
                    firstSchedule[i].AwayClubId != secondSchedule[i].AwayClubId)
                {
                    changedFixtures++;
                }
            }

            Assert.Greater(changedFixtures, 0,
                "The derived seed must actually reshape the schedule, or every season replays the last "
                + "one fixture-for-fixture.");
            Assert.AreNotEqual(firstTable, TableFingerprint(loop),
                "And the results must differ too, or the career is a replay regardless of the fixtures.");
        }

        [Test]
        public void RollToNextSeason_ThenAdvance_ReachesTheNewOpeningDay()
        {
            // The world clock and the new calendar have to meet: the close season is days the client
            // walks through, not a jump the roll performs on its behalf.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            PlayWholeSeason(loop, league);

            uint lastRoundDay = loop.State.Calendar.DayOfRound(loop.State.Calendar.RoundCount - 1);
            Assert.AreEqual(lastRoundDay, world.CurrentWorldTick,
                "Precondition: the clock sits on the day the final round was played.");

            SeasonRollOutcome outcome = loop.RollToNextSeason();
            int advanced = loop.AdvanceToNextFixtureDay();

            Assert.AreEqual((int)SeasonLoopConstants.SeasonBreakDays, advanced,
                "The close season is exactly SeasonBreakDays long.");
            Assert.AreEqual(outcome.NextFirstFixtureDay, world.CurrentWorldTick);
            Assert.DoesNotThrow(() => loop.AdvanceAndPlayNextRound(league));
        }

        /// <summary>
        /// A stable string over the whole final table — the comparison the two-career determinism test
        /// makes. Ordered by the FR-SN-007 tie-break so it captures POSITION, not just the row contents.
        /// </summary>
        private static string TableFingerprint(SeasonLoop loop)
        {
            var builder = new System.Text.StringBuilder();
            foreach (LeagueTableRow row in loop.State.TableOrdered())
            {
                builder.Append(row.ClubId).Append(':')
                    .Append(row.Played).Append('/')
                    .Append(row.Won).Append('/')
                    .Append(row.Drawn).Append('/')
                    .Append(row.Lost).Append('/')
                    .Append(row.GoalsFor).Append('/')
                    .Append(row.GoalsAgainst).Append('/')
                    .Append(row.Points).Append('|');
            }

            return builder.ToString();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial suite (#30 T3 / roadmap A5): the pure job-security,        |
// |         |            |        | calendar-shift and seed-derivation helpers; the F5 and cursor      |
// |         |            |        | gates plus the atomicity of a refused roll; FR-SN-029             |
// |         |            |        | restartability across a real save file; and the acceptance test —  |
// |         |            |        | a rolled season played to completion, which is what catches the    |
// |         |            |        | §3.5 calendar omission (ERR-030-015).                              |
// | 1.1     | 2026-07-27 | —      | AR pass: the step (b) tests repointed to BoardState.EvaluateAt-    |
// |         |            |        | SeasonEnd (+ a lock that the verdict, the running IsOnTrack read   |
// |         |            |        | and the penalty direction all come from IsMetBy). SecondSeason-    |
// |         |            |        | DiffersFromTheFirst asserted a DISJUNCTION whose table half is     |
// |         |            |        | always true, so the schedule half — the thing it is named for —    |
// |         |            |        | was unreachable; now asserted separately. Plus two locks for       |
// |         |            |        | uncovered paths: a season saved AFTER the roll (the likelier save  |
// |         |            |        | point, and the codec had never been shown a rolled state), and     |
// |         |            |        | AdvanceDays refusing to strand a career past the opening day.      |
// | 1.2     | 2026-07-27 | —      | AR pass 4: the two calendar-shift tests repointed to               |
// |         |            |        | SeasonCalendar.ShiftedToNextSeason, plus the two gates that move   |
// |         |            |        | with it — a single-round calendar still moves forward (and a zero  |
// |         |            |        | close season is refused, since at one round the season-length term |
// |         |            |        | is zero and the calendar would reproduce itself), and a shift past |
// |         |            |        | uint.MaxValue fails loud rather than wrapping to a plausible-      |
// |         |            |        | looking calendar at the start of time. Each proven non-vacuous by  |
// |         |            |        | disabling its own gate and watching exactly that test fail.        |
#endregion
