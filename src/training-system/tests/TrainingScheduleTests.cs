// File:     src/training-system/tests/TrainingScheduleTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 / FR-TR-003 / FR-TR-019 / FR-TR-023 (F2/F4); Code Standards #20
// Purpose:  The handle stores no focus of its own — a focus written through TrySetFocus is visible
//           through an already-open view — plus the parallel-array guard, the cross-club write the
//           bound pair makes unreachable, and T-TR-FAIL-003.

using System;

using NUnit.Framework;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class TrainingScheduleTests
    {
        private static (int[] ids, TrainingState[] states) NewClub()
        {
            int[] ids = { 10, 11, 12 };
            TrainingState[] states =
            {
                TrainingState.Create(TrainingFocus.Balanced),
                TrainingState.Create(TrainingFocus.Fitness),
                TrainingState.Create(TrainingFocus.Rest),
            };
            return (ids, states);
        }

        [Test]
        public void View_ReadsFocusLive_NotACopy()
        {
            (int[] ids, TrainingState[] states) = NewClub();
            var schedule = new TrainingSchedule(ids, states);

            Assert.AreEqual(TrainingFocus.Balanced, schedule.FocusAt(0));

            Assert.IsTrue(schedule.TrySetFocus(playerId: 10, focus: TrainingFocus.Physical));

            // FR-TR-003: focus lives ONLY on TrainingState.Focus. If the view had copied it at
            // construction, this read would still say Balanced — and that stale copy is exactly the
            // drift the single-source-of-truth rule exists to prevent.
            Assert.AreEqual(TrainingFocus.Physical, schedule.FocusAt(0));
            Assert.IsTrue(schedule.TryGetFocus(10, out TrainingFocus byId));
            Assert.AreEqual(TrainingFocus.Physical, byId);
        }

        [Test]
        public void View_ExposesRosterOrderAndCount()
        {
            (int[] ids, TrainingState[] states) = NewClub();
            var schedule = new TrainingSchedule(ids, states);

            Assert.AreEqual(3, schedule.Count);
            Assert.AreEqual(10, schedule.PlayerIdAt(0));
            Assert.AreEqual(12, schedule.PlayerIdAt(2));
            Assert.AreEqual(TrainingFocus.Rest, schedule.FocusAt(2));
            Assert.AreEqual(-1, schedule.IndexOf(999));
        }

        [Test]
        public void DefaultView_IsEmpty_NotACrash()
        {
            TrainingSchedule empty = default;

            Assert.AreEqual(0, empty.Count);
            Assert.AreEqual(-1, empty.IndexOf(10));
            Assert.IsFalse(empty.TryGetFocus(10, out TrainingFocus focus));
            Assert.AreEqual(TrainingFocus.Balanced, focus);
        }

        [Test]
        public void MismatchedArrays_FailLoud()
        {
            int[] ids = { 10, 11 };
            TrainingState[] states = { TrainingState.Create(TrainingFocus.Balanced) };

            Assert.Throws<ArgumentException>(() => new TrainingSchedule(ids, states),
                "a length mismatch is a roster-lifecycle bug (FR-TR-025), not something to iterate to " +
                "the shorter length.");
            Assert.Throws<ArgumentNullException>(() => new TrainingSchedule(null, states));
            Assert.Throws<ArgumentNullException>(() => new TrainingSchedule(ids, null));
        }

        [Test]
        public void SetFocus_RefusesUnknownPlayer_AndFailsLoudOnUndefinedFocus_TTRFAIL003()
        {
            (int[] ids, TrainingState[] states) = NewClub();
            var schedule = new TrainingSchedule(ids, states);

            // F2 — an unknown player is refused, not thrown: a stale id from a UI must not crash a
            // career, and the roster is authoritative.
            Assert.IsFalse(schedule.TrySetFocus(playerId: 999, focus: TrainingFocus.Fitness));
            Assert.AreEqual(TrainingFocus.Balanced, states[0].Focus, "a refused command mutates nothing.");

            // F4 — an undefined ordinal IS thrown: clamping it would persist a silently wrong focus.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => schedule.TrySetFocus(playerId: 10, focus: (TrainingFocus)200));
        }

        [Test]
        public void TheCommandLivesOnTheHandle_SoTheWrittenStatesAreTheBoundPair()
        {
            // The defect this shape exists to prevent: two clubs of equal size (every generated club
            // has the same squad size), a command taking the two arrays separately, and a caller that
            // passes club A's ids with club B's states. It resolves the player against A and writes B
            // — a different player, a different club, no exception, and it persists.
            (int[] idsA, TrainingState[] statesA) = NewClub();
            (int[] idsB, TrainingState[] statesB) = NewClub();
            idsB[0] = 20;
            idsB[1] = 21;
            idsB[2] = 22;

            var clubA = new TrainingSchedule(idsA, statesA);
            var clubB = new TrainingSchedule(idsB, statesB);

            Assert.IsTrue(clubA.TrySetFocus(10, TrainingFocus.Physical));

            Assert.AreEqual(TrainingFocus.Physical, statesA[0].Focus);
            Assert.AreEqual(TrainingFocus.Balanced, statesB[0].Focus,
                "club B is untouched — there is no argument a caller could have supplied to reach it.");
            Assert.IsFalse(clubB.TrySetFocus(10, TrainingFocus.Physical),
                "club B does not have player 10; the command refuses rather than writing index 0.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                             |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0).                                  |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (H): retargeted to TrainingSchedule.TrySetFocus, + the  |
// |         |            |        | cross-club test that fails against the old two-array signature.   |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): file header still described a read-only VIEW and a |
// |         |            |        | method named SetFocus, both superseded by v1.1.                   |
#endregion
