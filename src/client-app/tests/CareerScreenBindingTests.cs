// File:     src/client-app/tests/CareerScreenBindingTests.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     Interactive Unity client §5-P7, UI / Client Framework #38, Code Standards #20
// Purpose:  Verify seven-screen visibility, career root hygiene, typed copy and expanded locale admission.

using System;

using NUnit.Framework;

using TacticalDirector.Localization;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerScreenBindingTests
    {
        [Test]
        public void SevenScreens_ExposeExactlyOneCorrespondingRoot()
        {
            ScreenId[] screens = { ClientScreens.MainMenu, ClientScreens.TacticsSetup, ClientScreens.MatchView,
                ClientScreens.PostMatchReport, ClientScreens.CareerHome, ClientScreens.NewCareer, ClientScreens.LoadCareer };
            for (int i = 0; i < screens.Length; i++)
            {
                ClientScreenVisibility v = ClientScreenVisibility.From(screens[i]);
                bool[] visible = { v.MainMenu, v.TacticsSetup, v.MatchView, v.PostMatchReport, v.CareerHome, v.NewCareer, v.LoadCareer };
                for (int j = 0; j < visible.Length; j++) Assert.AreEqual(i == j, visible[j], "screen/root " + i + "/" + j);
            }
        }

        [Test]
        public void CareerRoots_AreIncludedInAllStructuralGuards()
        {
            var shell = new ClientShellRootSnapshot(100, true, true, new int[0]);
            var roots = new ClientShellRootSnapshot[7];
            for (int i = 0; i < roots.Length; i++) roots[i] = new ClientShellRootSnapshot(i + 1, i == 0, true, new[] { 100 });
            Assert.AreEqual(ClientShellWiringFault.None, ClientShellWiringValidator.Validate(shell, roots));
            for (int i = 4; i < roots.Length; i++)
            {
                ClientShellRootSnapshot saved = roots[i];
                roots[i] = default;
                Assert.AreEqual(ClientShellWiringFault.MissingRoot, ClientShellWiringValidator.Validate(shell, roots));
                roots[i] = roots[0];
                Assert.AreEqual(ClientShellWiringFault.DuplicateRoot, ClientShellWiringValidator.Validate(shell, roots));
                roots[i] = new ClientShellRootSnapshot(i + 1, false, true, new[] { 1 });
                Assert.AreEqual(ClientShellWiringFault.NestedRoot, ClientShellWiringValidator.Validate(shell, roots));
                roots[i] = new ClientShellRootSnapshot(i + 1, false, false, new[] { 100 });
                Assert.AreEqual(ClientShellWiringFault.RootUnderInactiveAncestor, ClientShellWiringValidator.Validate(shell, roots));
                roots[i] = new ClientShellRootSnapshot(i + 1, true, true, new[] { 100 });
                Assert.AreEqual(ClientShellWiringFault.NonMainRootInitiallyActive, ClientShellWiringValidator.Validate(shell, roots));
                roots[i] = saved;
            }
        }

        [Test]
        public void EveryCareerRole_AdmitsExpandedLocaleAndFormatsItsTypedSchema()
        {
            var rows = new TemplateCatalogue.StaticRow[CareerScreenText.All.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                S0TextRole role = CareerScreenText.All[i];
                rows[i] = new TemplateCatalogue.StaticRow(role.Key, "[Expanded career text: " + role.BasePattern + " :expanded]");
            }
            S0TextFormatter text = S0TextFormatter.ForRoles(CareerScreenText.All, rows, new LocaleId("qps-ploc"));
            foreach (S0TextRole role in CareerScreenText.All)
            {
                string suffix = role.Key.Value.Substring("ui.s0.".Length);
                var arguments = new object[role.ArgumentTypes.Length];
                for (int i = 0; i < arguments.Length; i++) arguments[i] = role.ArgumentTypes[i] == 'I' ? (object)3 : "Long Club Name With Several Words";
                string formatted = arguments.Length == 0 ? text.Label(suffix) : text.Format(suffix, suffix, arguments);
                StringAssert.StartsWith("[Expanded career text: ", formatted);
                StringAssert.DoesNotContain("{", formatted);
            }
            Assert.DoesNotThrow(() => new S0PresentationConfiguration(2f));
        }

        [Test]
        public void SlotCopy_ExplainsEmptyDamagedAndIncompatibleStates()
        {
            var text = new CareerScreenText(S0TextFormatter.ForRoles(CareerScreenText.All));
            StringAssert.Contains("Empty", text.Slot(CareerSlotSummary.Without(CareerSaveSlot.Manual1, CareerSlotState.Empty)));
            StringAssert.Contains("cannot be loaded", text.Slot(CareerSlotSummary.Without(CareerSaveSlot.Manual2, CareerSlotState.Damaged)));
            StringAssert.Contains("Incompatible", text.Slot(CareerSlotSummary.Without(CareerSaveSlot.Manual3, CareerSlotState.Incompatible)));
            Assert.AreEqual("Autosave", text.SlotName(CareerSaveSlot.Autosave));
            Assert.AreEqual("Manual slot 3", text.SlotName(CareerSaveSlot.Manual3));
        }

        [Test]
        public void HomeCopy_FormatsRealSnapshotsIncludingProgressAndTable()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            var source = new CareerHomeSource(holder);
            var text = new CareerScreenText(S0TextFormatter.ForRoles(CareerScreenText.All));
            CareerHomeViewModel home = source.Project();
            StringAssert.Contains(home.ManagedClubName, text.Summary(home));
            StringAssert.Contains(home.NextOpponentName, text.Next(home));
            StringAssert.Contains("Injured players", text.Attention(home));
            StringAssert.Contains(home.TableClubNames[0], text.TableRow(home, 0));
            Assert.AreEqual("Your career has started.", text.Changed(home));
            CareerTestSupport.ProgressUntil(holder.Require(), CareerProgressAction.PlayMatchday);
            holder.Require().Progress();
            StringAssert.Contains("Matchday complete:", text.Changed(source.Project()));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-06 | —      | P7b root, copy and pseudo-locale regressions. |
#endregion
