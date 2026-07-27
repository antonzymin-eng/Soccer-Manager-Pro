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

using System.IO;

using NUnit.Framework;

using TacticalDirector.LivingWorld;

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

        // ── The pure §3.5 step (b) arithmetic ──────────────────────────────────────────────

        [Test]
        public void EvaluateJobSecurity_ObjectiveMet_GainsTheFlatAmount()
        {
            int after = SeasonLoop.EvaluateJobSecurity(
                securityBefore: 500, finalPosition: 2, targetPosition: 4);

            Assert.AreEqual(500 + SeasonLoopConstants.BoardJobSecurityMetDeltaPerMille, after);
        }

        [Test]
        public void EvaluateJobSecurity_ExactlyOnTarget_CountsAsMet()
        {
            // BoardObjective is "position or better", so finishing exactly on it is a pass — the
            // off-by-one that would quietly punish a manager who hit the target precisely.
            int after = SeasonLoop.EvaluateJobSecurity(
                securityBefore: 500, finalPosition: 4, targetPosition: 4);

            Assert.AreEqual(500 + SeasonLoopConstants.BoardJobSecurityMetDeltaPerMille, after);
        }

        [Test]
        public void EvaluateJobSecurity_MissedPenalty_ScalesWithPlacesShort()
        {
            int byOne = SeasonLoop.EvaluateJobSecurity(1000, finalPosition: 5, targetPosition: 4);
            int byThree = SeasonLoop.EvaluateJobSecurity(1000, finalPosition: 7, targetPosition: 4);

            Assert.AreEqual(1000 - SeasonLoopConstants.BoardJobSecurityMissedDeltaPerMille, byOne);
            Assert.AreEqual(1000 - (3 * SeasonLoopConstants.BoardJobSecurityMissedDeltaPerMille), byThree);
            Assert.Less(byThree, byOne, "Missing by more must cost more — a flat penalty would tie them.");
        }

        [Test]
        public void EvaluateJobSecurity_ClampsToTheScaleAtBothEnds()
        {
            Assert.AreEqual(
                SeasonLoopConstants.JobSecurityScale,
                SeasonLoop.EvaluateJobSecurity(SeasonLoopConstants.JobSecurityScale, 1, 4),
                "A run of good seasons saturates rather than banking unbounded credit.");

            Assert.AreEqual(
                0,
                SeasonLoop.EvaluateJobSecurity(10, finalPosition: 20, targetPosition: 1),
                "A catastrophic season floors at zero rather than going negative (BoardState would throw).");
        }

        // ── The pure calendar shift ────────────────────────────────────────────────────────

        [Test]
        public void ShiftCalendarToNextSeason_OpensExactlyOneBreakAfterTheOldFinale()
        {
            SeasonCalendar old = SeasonCalendar.Linear(roundCount: 6, firstRoundDay: 7, daysBetweenRounds: 7);
            uint oldLastDay = old.DayOfRound(5);

            SeasonCalendar next = SeasonLoop.ShiftCalendarToNextSeason(old);

            Assert.AreEqual(oldLastDay + SeasonLoopConstants.SeasonBreakDays, next.DayOfRound(0));
            Assert.AreEqual(0, next.NextRoundIndex, "The new season starts at round 0.");
            Assert.AreEqual(old.RoundCount, next.RoundCount);
        }

        [Test]
        public void ShiftCalendarToNextSeason_PreservesANonUniformSchedule()
        {
            // A calendar with a mid-season break. Rebuilding a LINEAR calendar would silently flatten it;
            // shifting the shape keeps it, which is what makes the transform pure in the old season.
            var days = new uint[] { 10, 17, 24, 60, 67 };
            SeasonCalendar old = SeasonCalendar.Create(nextRoundIndex: days.Length, roundToDay: days);

            SeasonCalendar next = SeasonLoop.ShiftCalendarToNextSeason(old);

            uint shift = next.DayOfRound(0) - old.DayOfRound(0);
            for (int i = 0; i < days.Length; i++)
            {
                Assert.AreEqual(days[i] + shift, next.DayOfRound(i),
                    $"Round {i} must keep its offset — the 36-day mid-season gap survives the roll.");
            }
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
                SeasonLoop.EvaluateJobSecurity(securityBefore, finalPosition, outcome.TargetPosition),
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
                SeasonSaveManager.Save(world, interrupted.State, null, path);
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

            bool scheduleOrTableMoved = TableFingerprint(loop) != firstTable;
            for (int i = 0; i < firstSchedule.Length && !scheduleOrTableMoved; i++)
            {
                scheduleOrTableMoved =
                    firstSchedule[i].HomeClubId != secondSchedule[i].HomeClubId ||
                    firstSchedule[i].AwayClubId != secondSchedule[i].AwayClubId;
            }

            Assert.IsTrue(scheduleOrTableMoved,
                "A new season must not reproduce the previous one fixture-for-fixture AND result-for-result.");
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
#endregion
