// File:     src/client-app/tests/CareerScreenTextTests.cs
// Created:  2026-11-03
// Modified: 2026-11-04
// Author:   —
// Spec:     Interactive Unity client §5-P7; Localization #49; Code Standards #20
// Purpose:  Validate career display semantics and shared formatter isolation/admission.

using System;

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
            StringAssert.Contains(home.TableClubNames[0], text.Table(home));
            CareerTestSupport.ProgressUntil(holder.Require(), CareerProgressAction.PlayMatchday);
            holder.Require().Progress();
            home = source.Project();
            StringAssert.Contains("Last result:", text.Summary(home));
            Assert.AreEqual("Resolve matchday (on Career Home)", text.Progress(CareerProgressAction.PlayMatchday));
        }
    }
}
#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-03 | — | Career copy isolation, pseudo-locale schemas, save states and real Home/result text. |
// | 1.1 | 2026-11-04 | — | Cover translated slot composition, per-row caching and neutral/release-checkpoint copy. |
#endregion
