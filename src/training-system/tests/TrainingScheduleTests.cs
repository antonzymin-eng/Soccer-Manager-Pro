// File:     src/training-system/tests/TrainingScheduleTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 / FR-TR-003 / FR-TR-019 / FR-TR-023 (F2/F4); Code Standards #20
// Purpose:  The schedule is a VIEW, not a stored copy — a focus written through SetFocus is visible
//           through an already-open view — plus the parallel-array guard and T-TR-FAIL-003.

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

            Assert.IsTrue(TrainingStep.SetFocus(ids, states, playerId: 10, focus: TrainingFocus.Physical));

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

            // F2 — an unknown player is refused, not thrown: a stale id from a UI must not crash a
            // career, and the roster is authoritative.
            Assert.IsFalse(TrainingStep.SetFocus(ids, states, playerId: 999, focus: TrainingFocus.Fitness));
            Assert.AreEqual(TrainingFocus.Balanced, states[0].Focus, "a refused command mutates nothing.");

            // F4 — an undefined ordinal IS thrown: clamping it would persist a silently wrong focus.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrainingStep.SetFocus(ids, states, playerId: 10, focus: (TrainingFocus)200));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                            |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0). |
#endregion
