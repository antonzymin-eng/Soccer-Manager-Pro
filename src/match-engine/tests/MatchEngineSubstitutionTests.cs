// File:     src/match-engine/tests/MatchEngineSubstitutionTests.cs
// Created:  2026-07-14
// Modified: 2026-07-16 (AR-7 fix pass: M-1 yellow-card-reset + L-2 post-full-time-refusal regression locks)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §6, Code Standards #20
// Purpose:  Locks SubstitutePlayer's guard rejections, applied effect (bench identity swap, bookkeeping),
//           and the AR-5 pending-event-queue fix (safe to call between ticks, before EventBus has ever
//           entered a valid phase).

using System;

using NUnit.Framework;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineSubstitutionTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        [Test]
        public void SubstitutePlayer_CalledImmediatelyAfterConstruction_DoesNotThrow()
        {
            // AR-5 regression lock: EventBus.CurrentPhase is the post-construction boot state here —
            // no RunTick has ever executed — so a naive immediate Publish would trip the stale-Publish
            // guard. The event is queued, not published, until the next RunResolvePhase.
            var engine = new MatchEngine(MatchSeed);
            Assert.DoesNotThrow(() => engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_AppliesBenchIdentity_AndBookkeeping()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetBenchSlot(teamId: 0, benchIndex: 0, isGoalkeeper: true);

            int outSlot = 1; // a home outfield agent
            Assert.IsFalse(engine.TestOnly_IsGoalkeeper(outSlot));
            Assert.AreEqual(-1, engine.TestOnly_ActiveBenchSlot(outSlot));

            engine.SubstitutePlayer(0, outSlot, benchIndex: 0, SubstitutionReason.Tactical);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(outSlot), "Bench identity (GK flag) applied to the on-pitch slot.");
            Assert.AreEqual(0, engine.TestOnly_ActiveBenchSlot(outSlot));
            Assert.AreEqual(1, engine.TestOnly_SubstitutionsUsed(0));
        }

        [Test]
        public void SubstitutePlayer_InvalidTeamId_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.SubstitutePlayer(2, outSlotIndex: 0, benchIndex: 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_SlotNotBelongingToTeam_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            // Slot 0 belongs to team 0, not team 1.
            Assert.Throws<ArgumentException>(() =>
                engine.SubstitutePlayer(1, outSlotIndex: 0, benchIndex: 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_SentOffSlot_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetIsSentOff(1, true);
            Assert.Throws<InvalidOperationException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_AlreadySubstitutedSlot_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical);
            Assert.Throws<InvalidOperationException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 1, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_InvalidBenchIndex_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: MatchEngineConstants.SUBSTITUTES_PER_TEAM, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_ReusedBenchIndex_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical);
            Assert.Throws<InvalidOperationException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: 2, benchIndex: 0, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_CapReached_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM; i++)
            {
                engine.SubstitutePlayer(0, outSlotIndex: i + 1, benchIndex: i, SubstitutionReason.Tactical);
            }
            Assert.AreEqual(MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM, engine.TestOnly_SubstitutionsUsed(0));
            Assert.Throws<InvalidOperationException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM + 1,
                    benchIndex: MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM, SubstitutionReason.Tactical));
        }

        [Test]
        public void SubstitutePlayer_ResetsYellowCards_SubstituteStartsUnbooked()
        {
            // AR-7 M-1 regression lock: discipline attaches to the player, not the slot. Book the
            // outgoing player, substitute, and verify the incoming player starts on zero yellows —
            // pre-fix the slot kept the count, so the substitute's own first yellow promoted to a
            // second-yellow red.
            var engine = new MatchEngine(MatchSeed);
            int outSlot = 1;

            Assert.AreEqual((byte)0, engine.TestOnly_ApplyCardAndCheckSentOff(outSlot, drawnKind: 0),
                "First yellow issues card kind 0 (yellow).");
            Assert.AreEqual((byte)1, engine.TestOnly_YellowCards(outSlot));

            engine.SubstitutePlayer(0, outSlot, benchIndex: 0, SubstitutionReason.Tactical);

            Assert.AreEqual((byte)0, engine.TestOnly_YellowCards(outSlot),
                "The incoming substitute starts unbooked.");

            // The substitute's own first yellow must be a plain yellow (kind 0), not a
            // second-yellow promotion (kind 2) inherited from the outgoing player.
            Assert.AreEqual((byte)0, engine.TestOnly_ApplyCardAndCheckSentOff(outSlot, drawnKind: 0));
            Assert.IsFalse(engine.TestOnly_IsSentOff(outSlot));
        }

        [Test]
        public void SubstitutePlayer_AfterFullTime_Throws()
        {
            // AR-7 L-2 regression lock: after full time the Resolve phase never flushes the pending
            // substitution-event queue (_matchEnded returns early), so a post-full-time call would
            // mutate a frozen match while its SubstitutionEvent silently never published — refuse it.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            Assert.IsTrue(engine.TestOnly_MatchEnded);

            Assert.Throws<InvalidOperationException>(() =>
                engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical));
            Assert.AreEqual(0, engine.TestOnly_SubstitutionsUsed(0), "No state effect from the refused call.");
        }

        [Test]
        public void SubstitutePlayer_ThenRunTick_QueuedEventFlushesWithoutThrowing()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Injury);
            Assert.DoesNotThrow(() => engine.RunTick());
        }

        [Test]
        public void TwoIndependentEngines_SameSubstitution_BitwiseIdenticalDigests()
        {
            byte[] a = RunWithSub();
            byte[] b = RunWithSub();
            CollectionAssert.AreEqual(a, b);
        }

        private static byte[] RunWithSub()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SubstitutePlayer(0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical);
            for (int i = 0; i < 10; i++) engine.RunTick();
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial substitution suite: AR-5 immediate-call regression     |
// |         |            |        |   lock + applied-effect/bookkeeping + all guard rejections +   |
// |         |            |        |   cap enforcement + queued-event flush + two-run determinism.  |
// | 1.1     | 2026-07-16 | —      | AR-7 fix-pass locks: M-1 (substitute starts on zero yellows —  |
// |         |            |        |   a booked slot's count resets at substitution, and the        |
// |         |            |        |   substitute's own first yellow is kind 0, never an inherited  |
// |         |            |        |   second-yellow promotion) + L-2 (SubstitutePlayer refused     |
// |         |            |        |   after full time, with no state effect).                      |
#endregion
