// File:     src/client-app/tests/CareerSaveSlotsTests.cs
// Created:  2026-10-18
// Modified: 2026-10-27
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2, D-10), Code Standards #20
// Purpose:  Locks the slot store: empty listing, write/describe round trip, sequence-based latest,
//           D-10 incompatibility (listed, never loaded or continued) including nested sub-blob
//           versions, damage detection and invalidation, no stray temporary files after a write, and a
//           failed write never splitting the save from its sidecar, and recovery at open of a write
//           interrupted between its two swaps, which keeps every file when a sidecar is unreadable.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using TacticalDirector.LivingWorld;
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
        public void AFailedSidecarWrite_KeepsThePreviousSaveAndSidecarAsAPair()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            CareerSlotSummary first = session.SaveTo(CareerSaveSlot.Manual1);
            byte[] firstBytes = File.ReadAllBytes(temp.Slots.SavePath(CareerSaveSlot.Manual1));
            session.Progress();
            Assert.AreNotEqual(first.WorldDay, session.Loop.CurrentWorldDay, "precondition: the career moved on.");

            // A directory squatting on the sidecar temp makes the metadata write fail.
            Directory.CreateDirectory(Path.Combine(temp.Directory, "manual-1.slot.tmp"));
            Assert.Catch(() => session.SaveTo(CareerSaveSlot.Manual1));

            CareerSlotSummary row = temp.Slots.Describe(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, row.State);
            Assert.AreEqual(first.Sequence, row.Sequence);
            CollectionAssert.AreEqual(firstBytes, File.ReadAllBytes(temp.Slots.SavePath(CareerSaveSlot.Manual1)),
                "the previous save is not replaced when its sidecar cannot be written.");
            Assert.AreEqual(row.WorldDay, temp.Slots.Load(CareerSaveSlot.Manual1).World.CurrentWorldTick);
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.bak"));
        }

        [Test]
        public void AFailedSidecarSwap_PutsTheBackedUpSaveBack()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);
            string savePath = temp.Slots.SavePath(CareerSaveSlot.Manual1);
            byte[] firstBytes = File.ReadAllBytes(savePath);
            session.Progress();

            // A directory squatting on the sidecar itself: both temps are written and the save is
            // swapped in, then the sidecar swap fails — the rollback path.
            string sidecar = Path.Combine(temp.Directory, "manual-1.slot");
            File.Delete(sidecar);
            Directory.CreateDirectory(sidecar);
            Assert.Catch(() => session.SaveTo(CareerSaveSlot.Manual1));

            CollectionAssert.AreEqual(firstBytes, File.ReadAllBytes(savePath), "the backed-up save is restored.");
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.bak"), "a restored backup is consumed.");
        }

        [Test]
        public void ASaveFromAnotherNestedFormat_IsIncompatible()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual2);

            // What a build with another world-store format writes: the same outer version, another stamp.
            string sidecar = Path.Combine(temp.Directory, "manual-2.slot");
            string text = File.ReadAllText(sidecar);
            string stamp = "nestedFormats=" + CareerSaveSlots.NestedFormatStamp;
            Assert.IsTrue(text.Contains(stamp), "precondition: the sidecar records the nested stamp.");
            string current = "world=" + LivingWorldConstants.WORLD_STORE_FORMAT_VERSION + ",";
            string older = "world=" + (LivingWorldConstants.WORLD_STORE_FORMAT_VERSION - 1) + ",";
            File.WriteAllText(sidecar, text.Replace(stamp, stamp.Replace(current, older)));

            CareerSlotSummary row = temp.Slots.Describe(CareerSaveSlot.Manual2);
            Assert.AreEqual(CareerSlotState.Incompatible, row.State);
            Assert.AreEqual(SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION, row.SeasonSaveFormatVersion,
                "precondition: only the nested version differs.");
            Assert.Throws<InvalidOperationException>(() => temp.Slots.Load(CareerSaveSlot.Manual2));
            Assert.IsTrue(temp.Slots.TryGetLatest(out CareerSlotSummary latest));
            Assert.AreEqual(CareerSaveSlot.Autosave, latest.Slot);
        }

        [Test]
        public void AnOlderSidecarLayout_IsIncompatible_AndACurrentOneWithoutTheStamp_IsDamaged()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);
            session.SaveTo(CareerSaveSlot.Manual2);

            string currentFormat = "format=" + ClientAppConstants.CAREER_SLOT_META_FORMAT_VERSION + "\n";
            string stampLine = "nestedFormats=" + CareerSaveSlots.NestedFormatStamp + "\n";
            string one = Path.Combine(temp.Directory, "manual-1.slot");
            string two = Path.Combine(temp.Directory, "manual-2.slot");
            File.WriteAllText(one, File.ReadAllText(one).Replace(stampLine, "").Replace(currentFormat, "format=1\n"));
            File.WriteAllText(two, File.ReadAllText(two).Replace(stampLine, ""));

            Assert.AreEqual(CareerSlotState.Incompatible, temp.Slots.Describe(CareerSaveSlot.Manual1).State,
                "a sidecar of the earlier layout is a D-10 incompatible save, not a damaged one.");
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual2).State);
        }

        [Test]
        public void TheNestedStamp_CoversEverySubBlobTheClientWrites()
        {
            // Drift guard: a new season-save sub-blob must join CareerSaveSlots.NestedFormatStamp. The
            // stamp is the eight mandatory blobs plus the world snapshot nested in the world blob; the
            // optional match blob is the one the client never writes.
            int blobFields = typeof(SeasonSaveBlobs)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Count(f => f.FieldType == typeof(byte[]));
            Assert.AreEqual(9, blobFields, "SeasonSaveBlobs changed: update NestedFormatStamp and this pin.");
            Assert.AreEqual(blobFields - 1 + 1, CareerSaveSlots.NestedFormatStamp.Split(',').Length);
        }

        [Test]
        public void MarkDamaged_KeepsTheSaveBytes_AndTheSlotIsOverwritable()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);

            Assert.IsTrue(temp.Slots.MarkDamaged(CareerSaveSlot.Manual1));
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual1).State);
            Assert.IsTrue(File.Exists(temp.Slots.SavePath(CareerSaveSlot.Manual1)));

            session.SaveTo(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, temp.Slots.Describe(CareerSaveSlot.Manual1).State);
        }

        [Test]
        public void AnInterruptedFirstSave_IsCompletedWhenTheStoreReopens()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            CareerSlotSummary written = session.SaveTo(CareerSaveSlot.Manual1);

            // Between the two swaps of a first write: the save is in place, its sidecar still staged.
            string sidecar = Path.Combine(temp.Directory, "manual-1.slot");
            File.Move(sidecar, sidecar + ".tmp");
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual1).State,
                "precondition: without recovery the slot is unusable.");

            var reopened = new CareerSaveSlots(temp.Directory);
            CareerSlotSummary row = reopened.Describe(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, row.State);
            Assert.AreEqual(written.Sequence, row.Sequence);
            Assert.IsTrue(reopened.TryGetLatest(out CareerSlotSummary latest));
            Assert.AreEqual(CareerSaveSlot.Manual1, latest.Slot);
            Assert.AreEqual(row.WorldDay, reopened.Load(CareerSaveSlot.Manual1).World.CurrentWorldTick);
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
        }

        [Test]
        public void AnInterruptedOverwrite_WithTheSaveSwappedIn_IsCompletedWhenTheStoreReopens()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);
            session.Progress();
            CareerSlotSummary newer = session.SaveTo(CareerSaveSlot.Manual2);

            // Stage Manual1 as an overwrite interrupted after its save swap: the old save is the
            // backup, the new save is live, the new sidecar is staged, the old sidecar still in place.
            string save1 = temp.Slots.SavePath(CareerSaveSlot.Manual1);
            File.Move(save1, save1 + ".bak");
            File.Copy(temp.Slots.SavePath(CareerSaveSlot.Manual2), save1);
            File.Copy(Path.Combine(temp.Directory, "manual-2.slot"), Path.Combine(temp.Directory, "manual-1.slot.tmp"));

            var reopened = new CareerSaveSlots(temp.Directory);
            CareerSlotSummary row = reopened.Describe(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, row.State);
            Assert.AreEqual(newer.WorldDay, row.WorldDay, "the staged sidecar describing the live save is swapped in.");
            Assert.AreEqual(newer.WorldDay, reopened.Load(CareerSaveSlot.Manual1).World.CurrentWorldTick);
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.bak"));
        }

        [Test]
        public void AnInterruptedOverwrite_WhoseSidecarWasLost_PutsTheBackupBack()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            CareerSlotSummary older = session.SaveTo(CareerSaveSlot.Manual1);
            string save1 = temp.Slots.SavePath(CareerSaveSlot.Manual1);
            byte[] olderBytes = File.ReadAllBytes(save1);
            session.Progress();
            session.SaveTo(CareerSaveSlot.Manual2);

            // The new save is live and the old one is the backup, but the staged sidecar is truncated.
            File.Move(save1, save1 + ".bak");
            File.Copy(temp.Slots.SavePath(CareerSaveSlot.Manual2), save1);
            File.WriteAllText(Path.Combine(temp.Directory, "manual-1.slot.tmp"), "format=2\nseq");

            var reopened = new CareerSaveSlots(temp.Directory);
            CareerSlotSummary row = reopened.Describe(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, row.State);
            Assert.AreEqual(older.Sequence, row.Sequence);
            CollectionAssert.AreEqual(olderBytes, File.ReadAllBytes(save1), "the previous pair is put back.");
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.bak"));
        }

        [Test]
        public void ALockedStagedSidecar_IsKeptByRecovery_AndRecoveredOnceUnlocked()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            CareerSlotSummary written = session.SaveTo(CareerSaveSlot.Manual1);
            string staged = Path.Combine(temp.Directory, "manual-1.slot.tmp");
            File.Move(Path.Combine(temp.Directory, "manual-1.slot"), staged);

            // Unreadable is not invalid: recovery must stop, not judge the sidecar and delete it.
            using (new FileStream(staged, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.DoesNotThrow(() => new CareerSaveSlots(temp.Directory));
            }

            Assert.IsTrue(File.Exists(staged), "the staged sidecar survives a recovery that could not read it.");
            var reopened = new CareerSaveSlots(temp.Directory);
            CareerSlotSummary row = reopened.Describe(CareerSaveSlot.Manual1);
            Assert.AreEqual(CareerSlotState.Ready, row.State);
            Assert.AreEqual(written.Sequence, row.Sequence);
            Assert.IsTrue(reopened.TryGetLatest(out _));
        }

        [Test]
        public void StrayTempsBesideAConsistentPair_AreRemovedAndThePairKept()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            CareerSlotSummary written = session.SaveTo(CareerSaveSlot.Manual1);
            string save1 = temp.Slots.SavePath(CareerSaveSlot.Manual1);
            byte[] bytes = File.ReadAllBytes(save1);

            // A write that stopped before its first swap, plus a backup left after a completed one.
            File.WriteAllText(save1 + ".tmp", "partial");
            File.WriteAllText(save1 + ".tmp.tmp", "partial");
            File.WriteAllText(Path.Combine(temp.Directory, "manual-1.slot.tmp"), "partial");
            File.WriteAllText(save1 + ".bak", "previous");

            var reopened = new CareerSaveSlots(temp.Directory);
            Assert.AreEqual(written.Sequence, reopened.Describe(CareerSaveSlot.Manual1).Sequence);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(save1));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.tmp"));
            Assert.IsEmpty(Directory.GetFiles(temp.Directory, "*.bak"));
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
// | 1.2     | 2026-10-27 | —      | PR 519 review: +6 locks — a failed sidecar write or swap keeps |
// |         |            |        | the previous pair, nested-format and older-layout D-10 cases,  |
// |         |            |        | the stamp drift guard, MarkDamaged (13 locks).                 |
// | 1.3     | 2026-10-27 | —      | PR 519 review 2: +4 locks — recovery at open of an interrupted |
// |         |            |        | first save, an interrupted overwrite (completed, or backup put |
// |         |            |        | back), and stray temps beside a consistent pair (17 locks).    |
// | 1.4     | 2026-10-27 | —      | PR 519 review 3: +1 lock — a locked staged sidecar survives    |
// |         |            |        | recovery and is recovered once unlocked (18 locks).            |
#endregion
