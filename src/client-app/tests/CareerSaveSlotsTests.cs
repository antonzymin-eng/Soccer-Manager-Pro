// File:     src/client-app/tests/CareerSaveSlotsTests.cs
// Created:  2026-10-18
// Modified: 2026-10-19
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2, D-10), Code Standards #20
// Purpose:  Locks the slot store: empty listing, write/describe round trip, sequence-based latest,
//           D-10 incompatibility (listed, never loaded or continued), damage detection, and no stray
//           temporary files after a write.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerSaveSlotsTests
    {
        [Test]
        public void AFreshStore_ListsFourEmptySlots_AndHasNoLatest()
        {
            using var temp = new TempSlots();
            CareerSlotSummary[] rows = temp.Slots.List();

            Assert.AreEqual(1 + ClientAppConstants.CAREER_MANUAL_SLOT_COUNT, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                Assert.AreEqual((CareerSaveSlot)i, rows[i].Slot);
                Assert.AreEqual(CareerSlotState.Empty, rows[i].State);
            }

            Assert.IsFalse(temp.Slots.TryGetLatest(out _));
            Assert.Throws<InvalidOperationException>(() => temp.Slots.Load(CareerSaveSlot.Manual1));
        }

        [Test]
        public void Write_RecordsTheCareerFacts_AndLeavesNoTemporaryFiles()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.Progress();

            CareerSlotSummary written = session.SaveTo(CareerSaveSlot.Manual1);
            CareerSlotSummary read = temp.Slots.Describe(CareerSaveSlot.Manual1);

            Assert.AreEqual(CareerSlotState.Ready, read.State);
            Assert.AreEqual(written.Sequence, read.Sequence);
            Assert.AreEqual(SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION, read.SeasonSaveFormatVersion);
            Assert.AreEqual(CareerTestSupport.WORLD_SEED, read.WorldSeed);
            Assert.AreEqual(CareerTestSupport.MANAGED_CLUB, read.ManagedClubId);
            Assert.AreEqual(session.ManagedClubName, read.ManagedClubName);
            Assert.AreEqual(session.Loop.CurrentWorldDay, read.WorldDay);
            Assert.AreEqual(session.Loop.State.Calendar.RoundCount, read.RoundCount);
            Assert.AreEqual(session.Loop.State.PositionOf(session.ManagedClubId), read.LeaguePosition);
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
        }

        [Test]
        public void Latest_IsTheHighestSequence_NotTheSlotOrder()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual3);
            session.Progress();
            session.SaveTo(CareerSaveSlot.Manual1);

            Assert.IsTrue(temp.Slots.TryGetLatest(out CareerSlotSummary latest));
            Assert.AreEqual(CareerSaveSlot.Manual1, latest.Slot);
            Assert.Greater(latest.Sequence, temp.Slots.Describe(CareerSaveSlot.Manual3).Sequence);
            Assert.Greater(temp.Slots.Describe(CareerSaveSlot.Manual3).Sequence,
                temp.Slots.Describe(CareerSaveSlot.Autosave).Sequence);
        }

        [Test]
        public void ASaveFromAnotherFormatVersion_IsIncompatible_NeverLoadedOrContinued()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual2);

            string sidecar = Path.Combine(temp.Directory, "manual-2.slot");
            string text = File.ReadAllText(sidecar);
            string current = "seasonSaveFormat=" + SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION;
            Assert.IsTrue(text.Contains(current), "precondition: the sidecar records the format version.");
            File.WriteAllText(sidecar, text.Replace(current, "seasonSaveFormat=" + (SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION - 1)));

            CareerSlotSummary row = temp.Slots.Describe(CareerSaveSlot.Manual2);
            Assert.AreEqual(CareerSlotState.Incompatible, row.State);
            Assert.AreEqual(session.ManagedClubName, row.ManagedClubName, "an incompatible slot still shows what it was.");
            Assert.Throws<InvalidOperationException>(() => temp.Slots.Load(CareerSaveSlot.Manual2));
            Assert.IsTrue(temp.Slots.TryGetLatest(out CareerSlotSummary latest));
            Assert.AreEqual(CareerSaveSlot.Autosave, latest.Slot,
                "Continue skips the newer but incompatible slot (D-10).");

            session.SaveTo(CareerSaveSlot.Manual2);
            Assert.AreEqual(CareerSlotState.Ready, temp.Slots.Describe(CareerSaveSlot.Manual2).State,
                "an incompatible slot is freely overwritten.");
        }

        [Test]
        public void AMissingSave_OrAnUnreadableSidecar_IsDamaged()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);
            session.SaveTo(CareerSaveSlot.Manual2);

            File.Delete(temp.Slots.SavePath(CareerSaveSlot.Manual1));
            File.WriteAllText(Path.Combine(temp.Directory, "manual-2.slot"), "not a sidecar");

            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual1).State);
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual2).State);
            Assert.Throws<InvalidOperationException>(() => temp.Slots.Load(CareerSaveSlot.Manual2));
        }

        [Test]
        public void AFailedWrite_LeavesNoTemporaryFile_AndThePreviousSlotStateIntact()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);

            // A directory squatting on the destination makes the swap fail after the temp save is written.
            Directory.CreateDirectory(temp.Slots.SavePath(CareerSaveSlot.Manual1));

            Assert.Catch<IOException>(() => session.SaveTo(CareerSaveSlot.Manual1));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"), "a failed write must clean up its temp file.");
            Assert.IsFalse(session.IsBusy);
            Assert.AreEqual(CareerSlotState.Ready, temp.Slots.Describe(CareerSaveSlot.Autosave).State);
        }

        [Test]
        public void Constructor_RefusesAnEmptyDirectory()
        {
            Assert.Throws<ArgumentException>(() => new CareerSaveSlots(""));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 6 slot-store locks incl. D-10.       |
// | 1.1     | 2026-10-19 | —      | Self-review: a failed write cleans up its temp file (7 locks). |
#endregion
