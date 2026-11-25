// File:     src/client-app/tests/CareerSessionTests.cs
// Created:  2026-10-18
// Modified: 2026-11-25
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-1/2), roadmap C4,
//           Season & Competition Loop #30 §4 (save/resume), Player Progression #28 KD-4, Code Standards #20
// Purpose:  Locks the career session: the C4 new-game chain produces a full composition and an
//           autosave, a resumed career continues exactly as the uninterrupted one would, autosave
//           follows rounds and rolls only, manual saves cannot target the autosave slot, and a caller
//           error on Resume never invalidates a healthy slot, and a save that no longer matches its
//           sidecar is invalidated so Continue moves on.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerSessionTests
    {
        [Test]
        public void StartNew_ComposesTheFullCareer_AndWritesTheAutosaveSlot()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);

            Assert.AreEqual(CareerTestSupport.MANAGED_CLUB, session.ManagedClubId);
            Assert.IsNotNull(session.Loop.Career, "#29/#41 career state is wired.");
            Assert.IsNotNull(session.Loop.Progression, "#28 is the roster authority (KD-4).");
            Assert.IsNotNull(session.Loop.Discipline, "#44 tally is wired.");
            Assert.IsNotNull(session.Loop.TransfersSnapshot(), "#31 transfers are wired.");
            Assert.AreEqual(CareerChangeKind.Started, session.LastChange.Kind);
            Assert.IsNull(session.LastAutosaveError);

            CareerSlotSummary autosave = temp.Slots.Describe(CareerSaveSlot.Autosave);
            Assert.AreEqual(CareerSlotState.Ready, autosave.State);
            Assert.AreEqual(CareerTestSupport.WORLD_SEED, autosave.WorldSeed);
            Assert.AreEqual(session.ManagedClubName, autosave.ManagedClubName);
            Assert.AreEqual(0, autosave.SeasonNumber);
        }

        [Test]
        public void StartNew_RefusesAClubOutsideTheLeague()
        {
            using var temp = new TempSlots();
            Assert.Throws<ArgumentException>(() =>
                CareerSession.StartNew(CareerTestSupport.WORLD_SEED, 999, temp.Slots, RoundResolutionMode.QuickSimAll));
            Assert.AreEqual(CareerSlotState.Empty, temp.Slots.Describe(CareerSaveSlot.Autosave).State,
                "a refused start writes nothing.");
        }

        [Test]
        public void Autosave_FollowsRoundsAndRolls_NotDayAdvances()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            long afterStart = temp.Slots.Describe(CareerSaveSlot.Autosave).Sequence;

            session.Progress();
            Assert.AreEqual(CareerChangeKind.DayAdvanced, session.LastChange.Kind);
            Assert.AreEqual(afterStart, temp.Slots.Describe(CareerSaveSlot.Autosave).Sequence,
                "a day advance does not autosave.");

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            session.Progress();
            CareerSlotSummary afterRound = temp.Slots.Describe(CareerSaveSlot.Autosave);
            Assert.Greater(afterRound.Sequence, afterStart, "a played round autosaves.");
            Assert.AreEqual(1, afterRound.RoundsPlayed);
            Assert.AreEqual(session.Loop.CurrentWorldDay, afterRound.WorldDay);
        }

        [Test]
        public void Resume_WithAnUndefinedMode_LeavesAHealthySlotReady()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => CareerSession.Resume(temp.Slots, CareerSaveSlot.Manual1, (RoundResolutionMode)99));
            Assert.AreEqual(CareerSlotState.Ready, temp.Slots.Describe(CareerSaveSlot.Manual1).State,
                "a caller error is not the save's fault.");
            Assert.DoesNotThrow(() => CareerSession.Resume(temp.Slots, CareerSaveSlot.Manual1, RoundResolutionMode.QuickSimAll));
        }

        [Test]
        public void Continue_WithASaveThatNoLongerMatchesItsSidecar_MarksItDamaged_AndThenPicksTheNextSlot()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.SaveTo(CareerSaveSlot.Manual1);
            uint manual1Day = temp.Slots.Describe(CareerSaveSlot.Manual1).WorldDay;
            session.Progress();
            session.SaveTo(CareerSaveSlot.Manual2);
            Assert.AreNotEqual(manual1Day, temp.Slots.Describe(CareerSaveSlot.Manual2).WorldDay);

            // Manual2 is the latest slot, but its file now holds Manual1's (valid) save.
            File.Copy(temp.Slots.SavePath(CareerSaveSlot.Manual1), temp.Slots.SavePath(CareerSaveSlot.Manual2), overwrite: true);

            Assert.Throws<InvalidDataException>(() =>
                CareerSession.ContinueLatest(temp.Slots, RoundResolutionMode.QuickSimAll));
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual2).State);
            Assert.IsFalse(temp.Slots.RestoreDamaged(CareerSaveSlot.Manual2),
                "a mismatched save cannot be restored to Ready.");

            CareerSession resumed = CareerSession.ContinueLatest(temp.Slots, RoundResolutionMode.QuickSimAll);
            Assert.AreEqual(manual1Day, resumed.Loop.CurrentWorldDay, "Continue moves on to Manual1.");
        }

        [Test]
        public void ResumedCareer_ContinuesExactlyAsTheUninterruptedOne()
        {
            using var tempA = new TempSlots();
            using var tempB = new TempSlots();
            CareerSession straight = CareerTestSupport.StartQuick(tempA.Slots);
            CareerSession interrupted = CareerTestSupport.StartQuick(tempB.Slots);

            // Play into round 2 on both, save one manually, resume it, and keep both going in lockstep.
            for (int r = 0; r < 2; r++)
            {
                CareerTestSupport.ProgressUntil(straight, CareerProgressAction.PlayMatchday);
                straight.Progress();
                CareerTestSupport.ProgressUntil(interrupted, CareerProgressAction.PlayMatchday);
                interrupted.Progress();
            }

            straight.Progress();
            interrupted.Progress();
            interrupted.SaveTo(CareerSaveSlot.Manual2);
            CareerSession resumed = CareerSession.Resume(tempB.Slots, CareerSaveSlot.Manual2, RoundResolutionMode.QuickSimAll);
            Assert.AreEqual(CareerChangeKind.Resumed, resumed.LastChange.Kind);
            Assert.AreEqual(interrupted.ManagedClubName, resumed.ManagedClubName);
            Assert.IsNotNull(resumed.Loop.TransfersSnapshot(), "resume must retain #31 wiring.");
            CollectionAssert.AreEqual(
                TransfersSaveCodec.Encode(interrupted.Loop.TransfersSnapshot()),
                TransfersSaveCodec.Encode(resumed.Loop.TransfersSnapshot()),
                "resume must preserve the saved contracts, window and committed spend.");

            for (int r = 0; r < 3; r++)
            {
                CareerTestSupport.ProgressUntil(straight, CareerProgressAction.PlayMatchday);
                straight.Progress();
                CareerTestSupport.ProgressUntil(resumed, CareerProgressAction.PlayMatchday);
                resumed.Progress();
            }

            Assert.AreEqual(straight.Loop.CurrentWorldDay, resumed.Loop.CurrentWorldDay);
            Assert.IsTrue(straight.Loop.State.FieldsEqual(resumed.Loop.State),
                "the resumed career's season state diverged from the uninterrupted one.");
            CollectionAssert.AreEqual(straight.Loop.Snapshot(), resumed.Loop.Snapshot());
            Assert.AreEqual(straight.LastChange.ManagedResult.HomeGoals, resumed.LastChange.ManagedResult.HomeGoals);
            Assert.AreEqual(straight.LastChange.ManagedResult.AwayGoals, resumed.LastChange.ManagedResult.AwayGoals);
        }

        [Test]
        public void ContinueLatest_PicksTheMostRecentlyWrittenSlot()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            session.Progress();
            session.SaveTo(CareerSaveSlot.Manual3);
            uint dayOfManual = session.Loop.CurrentWorldDay;

            CareerSession continued = CareerSession.ContinueLatest(temp.Slots, RoundResolutionMode.QuickSimAll);

            Assert.AreEqual(dayOfManual, continued.Loop.CurrentWorldDay,
                "Manual3 was written after the start autosave, so Continue loads it.");
        }

        [Test]
        public void ContinueLatest_WithNoSave_Throws()
        {
            using var temp = new TempSlots();
            Assert.Throws<InvalidOperationException>(() =>
                CareerSession.ContinueLatest(temp.Slots, RoundResolutionMode.QuickSimAll));
        }

        [Test]
        public void SaveTo_TheAutosaveSlot_IsRefused()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            Assert.Throws<ArgumentException>(() => session.SaveTo(CareerSaveSlot.Autosave));
            Assert.IsFalse(session.IsBusy, "a refused save leaves the session usable.");
        }

        [Test]
        public void Revision_AdvancesWithEveryOperation()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            int before = session.Revision;

            session.Progress();
            Assert.Greater(session.Revision, before);
            int afterProgress = session.Revision;
            session.SaveTo(CareerSaveSlot.Manual1);
            Assert.Greater(session.Revision, afterProgress);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 8 session locks incl. resume ≡       |
// |         |            |        | uninterrupted (state + snapshot bytes).                        |
// | 1.1     | 2026-10-27 | —      | PR 519 review 2: +1 lock — an undefined mode on Resume leaves  |
// |         |            |        | the slot Ready (9 locks).                                      |
// | 1.2     | 2026-10-30 | —      | PR 521 merge: new careers wire #31; resume preserves its blob. |
// | 1.3     | 2026-11-25 | —      | +1 lock — Continue marks a digest-mismatched save Damaged and  |
// |         |            |        | the next Continue picks the older intact slot.                 |
#endregion
