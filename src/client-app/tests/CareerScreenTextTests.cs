// File:     src/client-app/tests/CareerScreenTextTests.cs
// Created:  2026-11-03
// Modified: 2026-11-20
// Author:   —
// Spec:     Interactive Unity client §5-P7; Localization #49; Code Standards #20
// Purpose:  Validate career display semantics and shared formatter isolation/admission.

using System;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.Localization;
using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerScreenTextTests
    {
        [Test]
        public void CareerContentDoesNotChangeTheS0LoadedIdentity()
        {
            string before = new S0TextFormatter().ContentSha256;
            var text = new CareerScreenText();
            Assert.AreEqual("New career", text.Label("new"));
            Assert.AreEqual(before, new S0TextFormatter().ContentSha256);
            Assert.AreNotEqual(before, text.Text.ContentSha256);
            Assert.Throws<ArgumentException>(() => text.Label("action.demo"));
        }

        [Test]
        public void AuthoredCareerExpansionUsesTheSameTypedAdmission()
        {
            var rows = new[] { new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.new"), "[Expanded new career label]"),
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.select"), "[Choose this club: {0} ...]") };
            var text = new CareerScreenText(rows, new LocaleId("qps-ploc"));
            Assert.AreEqual("[Expanded new career label]", text.Label("new"));
            Assert.AreEqual("[Choose this club: Long Club Name ...]", text.Text.Format("club", "select", "Long Club Name"));
            Assert.Throws<ArgumentException>(() => new CareerScreenText(new[] {
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.select"), "Missing required name") }));
            Assert.Throws<ArgumentException>(() => new CareerScreenText(new[] {
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.s0.action.demo"), "Wrong catalogue") }));
        }

        [TestCase(CareerSlotState.Empty, "Empty")]
        [TestCase(CareerSlotState.Damaged, "cannot load")]
        [TestCase(CareerSlotState.Incompatible, "cannot load")]
        public void NonLoadableSlotsStayVisibleWithReasons(CareerSlotState state, string expected)
        {
            var text = new CareerScreenText();
            StringAssert.Contains(expected, text.Slot(CareerSlotSummary.Without(CareerSaveSlot.Manual1, state)));
        }

        [TestCase(CareerSlotState.Ready)]
        [TestCase(CareerSlotState.Incompatible)]
        public void SlotsWithFactsUseTheLocalizedNameAndStatePattern(CareerSlotState state)
        {
            var text = new CareerScreenText(new[] {
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.slot"), "{1} < {0} >") },
                new LocaleId("qps-ploc"));
            var row = new CareerSlotSummary(CareerSaveSlot.Manual2, state, 1, 8, 42, 0,
                "Test Club", 0, 0, 0, 38, 1);
            string rendered = text.Slot(row);
            StringAssert.StartsWith(text.Label(state == CareerSlotState.Ready ? "ready" : "incompatible") +
                " < Manual slot 2 >", rendered);
            StringAssert.Contains("Test Club", rendered);
        }

        [Test]
        public void SlotCachesKeepEachRowsTextAcrossARefresh()
        {
            var text = new CareerScreenText();
            var first = CareerSlotSummary.Without(CareerSaveSlot.Manual1, CareerSlotState.Empty);
            var second = CareerSlotSummary.Without(CareerSaveSlot.Manual2, CareerSlotState.Damaged);
            string before = text.Slot(first);
            text.Slot(second);
            Assert.AreSame(before, text.Slot(first));
        }

        [Test]
        public void NeutralChangeAndSelectionCopyAreSeparateFromTheStartAction()
        {
            var text = new CareerScreenText();
            Assert.AreEqual("No recent action", text.Label("change.None"));
            Assert.AreNotEqual(text.Label("start"), text.Label("selection.ready"));
            StringAssert.Contains("release-readiness checkpoint", text.Label("warning"));
        }

        [Test]
        public void HomeAndResultTextUseTheOwningSnapshots()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            var source = new CareerHomeSource(holder);
            var text = new CareerScreenText();
            CareerHomeViewModel home = source.Project();
            StringAssert.Contains(home.ManagedClubName, text.Summary(home));
            StringAssert.Contains(home.NextOpponentName, text.Summary(home));
            var cells = new string[text.TableHeaders.Count];
            text.WriteTableRow(in home, 0, cells);
            StringAssert.Contains(home.TableClubNames[0], cells[1]);
            Assert.IsEmpty(text.RoundResults(in home));
            CareerTestSupport.ProgressUntil(holder.Require(), CareerProgressAction.PlayMatchday);
            holder.Require().Progress();
            home = source.Project();
            StringAssert.StartsWith("Round 1 results", text.RoundResults(in home));
            Assert.AreEqual("Resolve matchday (on Career Home)", text.Progress(CareerProgressAction.PlayMatchday));

            int differenceColumn = text.TableHeaders.Count - 2;
            Assert.AreEqual("GD", text.TableHeaders[differenceColumn]);
            bool hasNegative = false;
            for (int i = 0; i < home.Table.Count; i++)
            {
                text.WriteTableRow(in home, i, cells);
                Assert.AreEqual(home.Table[i].GoalDifference.ToString(CultureInfo.InvariantCulture), cells[differenceColumn]);
                Assert.AreEqual(home.Table[i].Points.ToString(CultureInfo.InvariantCulture), cells[differenceColumn + 1]);
                hasNegative |= home.Table[i].GoalDifference < 0;
            }
            Assert.IsTrue(hasNegative, "The test must exercise a signed goal difference.");
        }

        [Test]
        public void RoundResultsRenderEveryScorelineThroughTheCareerLocale()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            holder.Set(session);
            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            session.Progress();
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            var text = new CareerScreenText(new[]
            {
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.results"), "[Results {0}]"),
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.results.score"), "{3} {2}:{1} {0}")
            }, new LocaleId("qps-ploc"));

            string[] lines = text.RoundResults(in home).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.AreEqual("[Results 1]", lines[0]);
            Assert.AreEqual(home.LatestRoundResults.Count + 1, lines.Length);
            for (int i = 0; i < home.LatestRoundResults.Count; i++)
            {
                CareerFixtureResult fixture = home.LatestRoundResults[i];
                string homeClub = fixture.HomeClubName + (fixture.Result.HomeClubId == home.ManagedClubId ? " (you)" : "");
                string awayClub = fixture.AwayClubName + (fixture.Result.AwayClubId == home.ManagedClubId ? " (you)" : "");
                Assert.AreEqual($"{awayClub} {fixture.Result.AwayGoals}:{fixture.Result.HomeGoals} {homeClub}", lines[i + 1]);
            }
        }

        /// <summary>The fixture list shows one page under a heading that says where it is in the season.</summary>
        [Test]
        public void FixtureListShowsOnePageUnderItsPositionHeading()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            var text = new CareerScreenText();
            int size = CareerFixturePage.FIXTURES_PER_PAGE;
            CareerFixturePage second = CareerFixturePage.At(in home, size);

            string[] lines = text.FixtureList(home, second).Split('\n');
            Assert.AreEqual(1 + second.Count, lines.Length);
            Assert.AreEqual("Fixtures " + (size + 1) + " to " + (size + second.Count) + " of " + home.Fixtures.Count,
                lines[0].TrimEnd('\r'));
            for (int i = 0; i < second.Count; i++)
            {
                CareerFixtureView fixture = home.Fixtures[size + i];
                StringAssert.StartsWith("Day " + fixture.WorldDay + " | " + fixture.OpponentName + " (", lines[1 + i]);
            }

            Assert.AreEqual("Earlier fixtures", text.Label("schedule.page.previous"));
            Assert.AreEqual("Upcoming fixtures", text.Label("schedule.page.current"));
            Assert.AreEqual("Later fixtures", text.Label("schedule.page.next"));
        }

        /// <summary>The standings heading names the table positions on the page; it is empty without a career.</summary>
        [Test]
        public void StandingsHeadingNamesThePositionsOnThePage()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            var text = new CareerScreenText();
            int size = CareerStandingsPage.ROWS_PER_PAGE;
            int total = home.Table.Count;

            Assert.AreEqual("Positions 1 to " + size + " of " + total,
                text.StandingsHeading(home, CareerStandingsPage.At(in home, 0)));
            CareerStandingsPage last = CareerStandingsPage.At(in home, total - 1);
            Assert.AreEqual("Positions " + (last.First + 1) + " to " + total + " of " + total,
                text.StandingsHeading(home, last));
            Assert.AreEqual("", text.StandingsHeading(default, default));

            Assert.AreEqual("Higher positions", text.Label("table.page.previous"));
            Assert.AreEqual("Your position", text.Label("table.page.current"));
            Assert.AreEqual("Lower positions", text.Label("table.page.next"));
        }

        /// <summary>Both schedule views format fixture data through the same authored locale.</summary>
        [Test]
        public void ScheduleViewsUseLocalizedRowsAndRefreshFixtureStatus()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            holder.Set(session);
            var source = new CareerHomeSource(holder);
            var text = new CareerScreenText(new[]
            {
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.schedule.fixture"), "{1} ({2}) / {3} / {0}"),
                new TemplateCatalogue.StaticRow(new LocalizationKey("ui.career.schedule.upcoming"), "[Upcoming]")
            }, new LocaleId("qps-ploc"));

            CareerHomeViewModel home = source.Project();
            CareerFixtureView fixture = home.Fixtures[0];
            CareerCalendarWeek week = CareerCalendarWeek.At(in home, fixture.WorldDay);
            CareerFixturePage page = CareerFixturePage.At(in home, 0);
            string expected = fixture.OpponentName + " (" + (fixture.IsHome ? "home" : "away") +
                ") / [Upcoming] / Day " + fixture.WorldDay;
            StringAssert.Contains(expected, text.FixtureList(home, page));
            StringAssert.Contains(expected, text.Calendar(home, week));
            StringAssert.Contains("No club fixture", text.Calendar(home, week));

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            home = source.Project();
            StringAssert.Contains("Matchday / Day " + fixture.WorldDay + " (today)", text.Calendar(home, week));
            session.Progress();
            home = source.Project();
            StringAssert.Contains("Played / Day " + fixture.WorldDay, text.FixtureList(home, page));
            StringAssert.Contains("Played / Day " + fixture.WorldDay, text.Calendar(home, week));
        }
    }
}
#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-03 | — | Career copy isolation, pseudo-locale schemas, save states and real Home/result text. |
// | 1.1 | 2026-11-04 | — | Cover translated slot composition, per-row caching and neutral/release-checkpoint copy. |
// | 1.2 | 2026-11-19 | — | Cover localized list and calendar rows and their matchday and played states. |
// | 1.3 | 2026-11-19 | — | Cover the paged fixture list, its position heading and paging labels. |
// | 1.4 | 2026-11-19 | — | Verify localized scorelines and signed goal differences. |
// | 1.5 | 2026-11-20 | — | Cover the standings page heading and the Higher / Your / Lower position labels. |
#endregion
