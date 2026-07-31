// File:     src/training-system/tests/ClubTrainingBlockTests.cs
// Created:  2026-07-31
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §5.1a (T-TR-LIFE-001) / §5.5 (T-TR-FAIL-003) / FR-TR-003 / FR-TR-022 /
//           FR-TR-023 / FR-TR-025; F2/F4/F7; Code Standards #20
// Purpose:  Locks the per-club container: the SetFocus command, the regen/retire lifecycle, the derived
//           schedule view, the observer projection, and the deterministic iteration order.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.TrainingSystem.Tests
{
    [TestFixture]
    public sealed class ClubTrainingBlockTests
    {
        private const int ClubId = 7;

        [Test]
        public void Insert_SeedsAFreshStateThroughCreate_NeverTheDefault()
        {
            ClubTrainingBlock block = NewBlock(41);

            TrainingState state = block.GetState(41);

            Assert.AreEqual(TrainingFocus.Balanced, state.Focus, "FR-TR-025: a regen starts Balanced");
            Assert.AreEqual(TrainingSystemConstants.ConditionStart, state.Condition);
            Assert.AreEqual(0, state.TrainingFatigue);
            Assert.AreEqual(
                TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                state.LastAdvancedWorldDay,
                "never default(TrainingState) — a zero cursor would read as 'day 0 already advanced' (F7)");
        }

        [Test]
        public void Insert_RefusesADuplicatePlayer()
        {
            ClubTrainingBlock block = NewBlock(41);
            block.SetFocus(41, TrainingFocus.Fitness);

            Assert.Throws<ArgumentException>(() => block.Insert(41));
            Assert.AreEqual(TrainingFocus.Fitness, block.GetState(41).Focus, "the live state is untouched");
            Assert.AreEqual(1, block.Count);
        }

        [Test]
        public void Remove_RefusesAnUnknownPlayer()
        {
            ClubTrainingBlock block = NewBlock(41);

            Assert.Throws<ArgumentException>(() => block.Remove(42));
            Assert.AreEqual(1, block.Count);
        }

        // T-TR-LIFE-001 — membership tracks the roster in lockstep across a season boundary: regens are
        // inserted, retirees removed, and the count returns to the roster count with no leak.
        [Test]
        public void RegenInsertAndRetireeRemove_LeaveNoLeakAcrossASeasonRoll()
        {
            var block = new ClubTrainingBlock(ClubId);
            for (int playerId = 1; playerId <= 25; playerId++)
            {
                block.Insert(playerId);
            }

            Assert.AreEqual(25, block.Count);

            // Season boundary: three retire, three regens arrive with fresh ids.
            block.Remove(3);
            block.Remove(11);
            block.Remove(25);
            block.Insert(101);
            block.Insert(102);
            block.Insert(103);

            Assert.AreEqual(25, block.Count, "the block equals the roster count — no retired entries leak");
            Assert.IsFalse(block.Contains(3));
            Assert.IsFalse(block.Contains(11));
            Assert.IsFalse(block.Contains(25));
            Assert.IsTrue(block.Contains(101));

            // A regen advances correctly on its FIRST world day — the day-0 trap, checked at day 0.
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            block.AdvanceTrainingDay(101, in attributes, in coach, 0);

            Assert.AreEqual(0u, block.GetState(101).LastAdvancedWorldDay);
            Assert.Greater(block.GetState(101).Condition, TrainingSystemConstants.ConditionStart);
        }

        // Iteration order is a function of CONTENT, not of insert/remove history — the property a
        // Dictionary would not give and #29's determinism requirement (FR-TR-008) needs.
        [Test]
        public void IterationOrder_IsAscendingPlayerId_IndependentOfInsertHistory()
        {
            var forward = new ClubTrainingBlock(ClubId);
            var scrambled = new ClubTrainingBlock(ClubId);

            foreach (int playerId in new[] { 2, 5, 9, 14 })
            {
                forward.Insert(playerId);
            }

            foreach (int playerId in new[] { 14, 9, 2, 33, 5 })
            {
                scrambled.Insert(playerId);
            }

            scrambled.Remove(33);

            Assert.AreEqual(forward.Count, scrambled.Count);
            for (int i = 0; i < forward.Count; i++)
            {
                Assert.AreEqual(forward.PlayerIdAt(i), scrambled.PlayerIdAt(i), $"entry {i}");
            }

            Assert.AreEqual(new[] { 2, 5, 9, 14 }, new[] { forward.PlayerIdAt(0), forward.PlayerIdAt(1), forward.PlayerIdAt(2), forward.PlayerIdAt(3) });
        }

        // T-TR-FAIL-003, F4 half — an out-of-catalogue focus is a caller bug and fails loud, whether or
        // not the target player exists (validated first, deliberately).
        [Test]
        public void SetFocus_OutOfRangeFocus_FailsLoudForKnownAndUnknownPlayersAlike()
        {
            ClubTrainingBlock block = NewBlock(41);

            Assert.Throws<ArgumentOutOfRangeException>(() => block.SetFocus(41, (TrainingFocus)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => block.SetFocus(999, (TrainingFocus)99));
            Assert.AreEqual(TrainingFocus.Balanced, block.GetState(41).Focus, "the refused command changed nothing");
        }

        // T-TR-FAIL-003, F2 half — an unknown player is a bounded refusal (the roster is authoritative),
        // reported through the return value rather than silently swallowed.
        [Test]
        public void SetFocus_UnknownPlayer_IsRefusedAsANoOp()
        {
            ClubTrainingBlock block = NewBlock(41);

            Assert.IsFalse(block.SetFocus(999, TrainingFocus.Rest), "refused (F2)");
            Assert.AreEqual(1, block.Count);
            Assert.AreEqual(TrainingFocus.Balanced, block.GetState(41).Focus);
        }

        [Test]
        public void SetFocus_KnownPlayer_AppliesAndChangesOnlyThatPlayer()
        {
            var block = new ClubTrainingBlock(ClubId);
            block.Insert(41);
            block.Insert(42);

            Assert.IsTrue(block.SetFocus(41, TrainingFocus.Physical));

            Assert.AreEqual(TrainingFocus.Physical, block.GetState(41).Focus);
            Assert.AreEqual(TrainingFocus.Balanced, block.GetState(42).Focus, "the sibling is untouched");
        }

        // FR-TR-003 — the schedule is a VIEW: a SetFocus is visible through it with no separate update,
        // because there is no second copy of focus to update.
        [Test]
        public void Schedule_ReflectsASetFocus_WithoutSeparateStorage()
        {
            var block = new ClubTrainingBlock(ClubId);
            block.Insert(41);
            block.Insert(42);

            TrainingSchedule schedule = block.Schedule; // taken BEFORE the command
            Assert.AreEqual(TrainingFocus.Balanced, schedule.GetFocus(41));

            block.SetFocus(41, TrainingFocus.Tactical);

            Assert.AreEqual(TrainingFocus.Tactical, schedule.GetFocus(41), "the pre-existing view reads through");
            Assert.AreEqual(TrainingFocus.Tactical, block.Schedule.GetFocus(41));
            Assert.AreEqual(2, schedule.Count);
            Assert.AreEqual(ClubId, schedule.ClubId);
        }

        [Test]
        public void Schedule_IteratesEveryPlayerInAscendingIdOrder()
        {
            var block = new ClubTrainingBlock(ClubId);
            block.Insert(9);
            block.Insert(2);
            block.Insert(5);
            block.SetFocus(5, TrainingFocus.Rest);

            int index = 0;
            int[] expectedIds = { 2, 5, 9 };
            foreach (TrainingSchedule.Entry entry in block.Schedule)
            {
                Assert.AreEqual(expectedIds[index], entry.PlayerId, $"entry {index}");
                Assert.AreEqual(block.GetState(entry.PlayerId).Focus, entry.Focus, "the view reads through to the state");
                Assert.AreEqual(block.Schedule.EntryAt(index).PlayerId, entry.PlayerId);
                index++;
            }

            Assert.AreEqual(3, index, "every entry was visited");
        }

        [Test]
        public void Schedule_DefaultValue_FailsLoudRatherThanReportingAnEmptyClub()
        {
            TrainingSchedule schedule = default;

            Assert.Throws<InvalidOperationException>(() => { int _ = schedule.Count; });
        }

        [Test]
        public void Schedule_UnknownPlayer_FailsLoud()
        {
            ClubTrainingBlock block = NewBlock(41);

            Assert.Throws<ArgumentException>(() => block.Schedule.GetFocus(999));
        }

        // FR-TR-022 — the observer surface hands out value copies of the three player-facing fields.
        [Test]
        public void ViewModel_ProjectsValueCopiesOfTheLiveState()
        {
            ClubTrainingBlock block = NewBlock(41);
            block.SetFocus(41, TrainingFocus.Fitness);
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            block.AdvanceTrainingDay(41, in attributes, in coach, 0);

            TrainingViewModel view = block.GetViewModel(41);
            TrainingState state = block.GetState(41);

            Assert.AreEqual(41, view.PlayerId);
            Assert.AreEqual(state.Focus, view.Focus);
            Assert.AreEqual(state.Condition, view.Condition);
            Assert.AreEqual(state.TrainingFatigue, view.TrainingFatigue);
            Assert.AreEqual(view.PlayerId, block.ViewModelAt(0).PlayerId);

            // The projection is a snapshot: advancing afterwards does not retro-change the copy.
            block.AdvanceTrainingDay(41, in attributes, in coach, 1);
            Assert.AreEqual(state.Condition, view.Condition, "the copy does not track later mutation");
            Assert.Greater(block.GetState(41).Condition, view.Condition);
        }

        // The block owns the write: a state handed out by GetState is a copy, so mutating it cannot
        // reach the block (which is what keeps the F6 cursor and the lifecycle un-bypassable).
        [Test]
        public void GetState_ReturnsACopy_SoAWriteCannotReachTheBlock()
        {
            ClubTrainingBlock block = NewBlock(41);

            TrainingState escaped = block.GetState(41);
            escaped.Focus = TrainingFocus.Physical;
            escaped.Condition = 1;

            Assert.AreEqual(TrainingFocus.Balanced, block.GetState(41).Focus);
            Assert.AreEqual(TrainingSystemConstants.ConditionStart, block.GetState(41).Condition);
        }

        [Test]
        public void AdvanceTrainingDay_RoutesThroughTrainingStep_AndPersistsTheResult()
        {
            ClubTrainingBlock block = NewBlock(41);
            block.SetFocus(41, TrainingFocus.Fitness);
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            // The same run performed directly on a free-standing state must agree exactly.
            TrainingState reference = TrainingState.Create(TrainingFocus.Fitness);
            for (uint day = 0; day <= 3; day++)
            {
                block.AdvanceTrainingDay(41, in attributes, in coach, day);
                TrainingStep.AdvanceTrainingDay(ref reference, in attributes, in coach, day);
            }

            TrainingState stored = block.GetState(41);
            Assert.AreEqual(reference.Condition, stored.Condition);
            Assert.AreEqual(reference.TrainingFatigue, stored.TrainingFatigue);
            Assert.AreEqual(reference.LastAdvancedWorldDay, stored.LastAdvancedWorldDay);
            Assert.AreEqual(3u, stored.LastAdvancedWorldDay);
        }

        [Test]
        public void AdvanceTrainingDay_UnknownPlayer_FailsLoud()
        {
            ClubTrainingBlock block = NewBlock(41);
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;

            Assert.Throws<ArgumentException>(() => block.AdvanceTrainingDay(999, in attributes, in coach, 0));
        }

        // A throwing advance must leave the STORED state untouched, not half-written.
        [Test]
        public void AdvanceTrainingDay_RefusedCall_LeavesTheStoredStateUntouched()
        {
            ClubTrainingBlock block = NewBlock(41);
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            CoachingModifier coach = CoachingModifier.Identity;
            block.AdvanceTrainingDay(41, in attributes, in coach, 0);
            TrainingState before = block.GetState(41);

            Assert.Throws<ArgumentException>(() => block.AdvanceTrainingDay(41, in attributes, in coach, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => block.AdvanceTrainingDay(
                41, in attributes, in coach, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL));

            TrainingState after = block.GetState(41);
            Assert.AreEqual(before.Condition, after.Condition);
            Assert.AreEqual(before.TrainingFatigue, after.TrainingFatigue);
            Assert.AreEqual(before.LastAdvancedWorldDay, after.LastAdvancedWorldDay);
        }

        private static ClubTrainingBlock NewBlock(int playerId)
        {
            var block = new ClubTrainingBlock(ClubId);
            block.Insert(playerId);
            return block;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 H-2: locks for the per-club container — FR-TR-025 insert/remove
// |         |            |        | (incl. the T-TR-LIFE-001 no-leak season roll and the regen's first
// |         |            |        | world day), FR-TR-023 SetFocus (F4 fail-loud / F2 refusal), the
// |         |            |        | FR-TR-003 derived schedule view, the FR-TR-022 observer copies, the
// |         |            |        | deterministic ascending-id iteration order, and the
// |         |            |        | refused-call-changes-nothing posture on the routed advance.       |
#endregion
