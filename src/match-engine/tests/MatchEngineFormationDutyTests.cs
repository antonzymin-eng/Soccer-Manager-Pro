// File:     src/match-engine/tests/MatchEngineFormationDutyTests.cs
// Created:  2026-11-09
// Modified: 2026-11-09
// Author:   —
// Spec:     #12 FR-PA-039, ERR-012-013; #21 ERR-021-009; #25 ERR-025-001
// Purpose:  Kickoff formation lock, mixed requests, variable rotation rows and Duty replay.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PositioningAI;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Outcome tests for the approved kickoff-only formation and Duty landing.</summary>
    [TestFixture]
    public sealed class MatchEngineFormationDutyTests
    {
        private const ulong Seed = 0x0123456789ABCDEFUL;

        internal static TeamTactic Tactic(TacticFormation formation, Mentality mentality = Mentality.Balanced) =>
            new TeamTactic(mentality, formation, Tempo.Standard, TacticWidth.Standard,
                TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f,
                TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
                TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        [TestCase(0, TacticFormation.F433, FormationFamily.F433, 5)]
        [TestCase(1, TacticFormation.F4231, FormationFamily.F4231, 6)]
        [TestCase(0, (TacticFormation)255, FormationFamily.F442, 5)]
        public void FinalPreKickoffChoice_LocksBeforeFrameOne_InExistingObject(
            int team, TacticFormation requested, FormationFamily expected, int rows)
        {
            var engine = new MatchEngine(Seed);
            var positioning = engine.TestOnly_Positioning(team);
            engine.SetTeamTactic(team, Tactic(TacticFormation.F433));
            engine.SetTeamTactic(team, Tactic(requested));
            engine.RunTick();
            Assert.AreSame(positioning, engine.TestOnly_Positioning(team), "retained consumer views");
            Assert.AreEqual(expected, positioning.Formation);
            Assert.AreEqual(rows, positioning.CaptureRotationState().PairCount);
            Assert.AreEqual(PositioningAI.TacticTranslation.ToTacticFormation(expected), engine.GetEffectiveFormation(team));
            Assert.AreEqual(requested, engine.GetRequestedFormation(team));
            // The early save must already restore the selected controller, before the first AI stride.
            var restored = MatchEngine.RestoreFromSnapshot(engine.CaptureDurableHeader(), engine.CaptureDurablePayload(), Seed);
            Assert.AreEqual(expected, restored.TestOnly_Positioning(team).Formation);
            restored.RunTick();
            var restoredDigest = restored.CurrentSnapshotDigest;
            // EventBus is process-global: run engines sequentially, never interleave two live buses.
            var reference = new MatchEngine(Seed);
            reference.SetTeamTactic(team, Tactic(requested));
            reference.RunTick(); reference.RunTick();
            CollectionAssert.AreEqual(reference.CurrentSnapshotDigest, restoredDigest);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void MixedMidMatchRequest_CommitsMentality_ButKeepsKickoffFormation(int team)
        {
            var engine = new MatchEngine(Seed);
            engine.SetTeamTactic(team, Tactic(TacticFormation.F4231));
            engine.RunTick();
            engine.SetTeamTactic(team, Tactic(TacticFormation.F433, Mentality.VeryAttacking));
            for (int i = 1; i < DeterministicSimConstants.AI_PHASE_STRIDE; i++) engine.RunTick();
            Assert.AreEqual(Mentality.VeryAttacking, engine.TestOnly_Mentality(team * 11 + 5));
            Assert.AreEqual(TacticFormation.F433, engine.GetRequestedFormation(team));
            Assert.AreEqual(TacticFormation.F4231, engine.GetEffectiveFormation(team));
            Assert.AreEqual(PositioningAIConstants.Family4231[5].Role, engine.TestOnly_PositioningRole(team * 11 + 5));
            EventBus.BeginTick((uint)engine.CurrentTick);
            EventBus.BeginPhase(PhaseId.Input);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
            Assert.IsTrue(engine.TestOnly_SecondHalfStarted);
            engine.RunTick();
            Assert.AreEqual(TacticFormation.F4231, engine.GetEffectiveFormation(team), "second kickoff cannot relock");
        }

        [TestCase(TacticFormation.F442)]
        [TestCase(TacticFormation.F433)]
        [TestCase(TacticFormation.F4231)]
        public void NonDefaultDuties_AndEveryRotationRow_RoundTripAndContinue(TacticFormation formation)
        {
            var engine = new MatchEngine(Seed);
            for (int t = 0; t < 2; t++) engine.SetTeamTactic(t, Tactic(formation));
            for (int i = 1; i < 22; i++)
                if (i != 11) engine.SetPlayerTactic(i, new PlayerTactic(PlayerRole.Default,
                    i % 2 == 0 ? Duty.Attack : Duty.Defend, PlayerInstructions.Default));
            for (int i = 0; i < 60; i++) engine.RunTick();
            for (int t = 0; t < 2; t++)
            {
                var rot = engine.TestOnly_Positioning(t).CaptureRotationState();
                var binding = new int[11]; for (int i = 0; i < 11; i++) binding[i] = i;
                // Exercise a non-identity binding and the final catalogue row, including F4231 row 5.
                binding[5] = 6; binding[6] = 5; rot.RestoreBinding(binding);
                var state = new RotationPairState { TriggerDwellTicks = 1 };
                rot.RestorePairState(rot.PairCount - 1, in state);
            }
            // Flush injected state through the real serializer; no AI evaluation at tick 61.
            engine.RunTick();
            var header = engine.CaptureDurableHeader();
            var payload = engine.CaptureDurablePayload();
            var expected = new byte[240][];
            for (int i = 0; i < expected.Length; i++) { engine.RunTick(); expected[i] = engine.CurrentSnapshotDigest; }
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            for (int t = 0; t < 2; t++)
            {
                Assert.AreEqual(6, restored.TestOnly_SlotBinding(t, 5));
                int last = restored.TestOnly_Positioning(t).CaptureRotationState().PairCount - 1;
                Assert.AreEqual(1, restored.TestOnly_RotationPairState(t, last).TriggerDwellTicks);
            }
            for (int i = 0; i < 240; i++)
            {
                restored.RunTick();
                CollectionAssert.AreEqual(expected[i], restored.CurrentSnapshotDigest, "frame " + i);
            }
            Assert.AreEqual(Duty.Defend, restored.TestOnly_PositioningDuty(5));
            Assert.AreEqual(Duty.Attack, restored.TestOnly_PositioningDuty(16));
        }

        [TestCase(0, 2)] // invalid boolean
        [TestCase(1, 3)] // invalid saved formation
        public void CorruptKickoffMetadata_RejectsInsteadOfLiveFallback(int fieldOffset, int value)
        {
            var engine = new MatchEngine(Seed); engine.RunTick();
            var payload = engine.CaptureDurablePayload();
            payload.PayloadBytes[sizeof(uint) + sizeof(ulong) + fieldOffset] = (byte)value;
            Assert.Throws<InvalidOperationException>(() => MatchEngine.RestoreFromSnapshot(
                engine.CaptureDurableHeader(), payload, Seed));
        }

        [Test]
        public void UninitializedFormation_AtPositiveTick_IsRejected()
        {
            var engine = new MatchEngine(Seed); engine.RunTick();
            var payload = engine.CaptureDurablePayload(); payload.PayloadBytes[12] = 0;
            Assert.Throws<InvalidOperationException>(() => MatchEngine.RestoreFromSnapshot(
                engine.CaptureDurableHeader(), payload, Seed));
        }

        [TestCase(0, Duty.Attack, 3f)]
        [TestCase(1, Duty.Attack, -3f)]
        [TestCase(0, Duty.Defend, -3f)]
        [TestCase(1, Duty.Defend, 3f)]
        public void Duty_ReachesPositioning_InBothWorldDirections(int team, Duty duty, float expected)
        {
            var control = new MatchEngine(Seed);
            for (int i = 0; i < 6; i++) control.RunTick();
            float original = control.TestOnly_FormationSlot(team * 11 + 6).x;
            var changed = new MatchEngine(Seed);
            changed.SetPlayerTactic(team * 11 + 6,
                new PlayerTactic(PlayerRole.Default, duty, PlayerInstructions.Default));
            for (int i = 0; i < 6; i++) changed.RunTick();
            // One outfield shift moves the centroid by 0.3 m; the remaining 2.7 m
            // is scaled by the unchanged InPoss vertical context gain (1 + 0.2 × 0.5).
            float composedExpected = expected * (0.1f + 0.9f / 1.1f);
            Assert.AreEqual(composedExpected, changed.TestOnly_FormationSlot(team * 11 + 6).x - original, 0.0001f);
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(int.MaxValue)]
        public void CorruptRotationCount_IsRejectedBeforeReadingRows(int invalidCount)
        {
            var engine = new MatchEngine(Seed); engine.RunTick();
            var payload = engine.CaptureDurablePayload();
            // Locate the real count + identity binding. This signature is unique to each rotation
            // block, independent of earlier snapshot offsets; use its first occurrence (home).
            var signature = new byte[48];
            Array.Copy(BitConverter.GetBytes(5), signature, 4);
            for (int i = 0; i < 11; i++) Array.Copy(BitConverter.GetBytes(i), 0, signature, 4 + i * 4, 4);
            int offset = -1;
            for (int i = 0; i <= payload.BytesWritten - signature.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < signature.Length && match; j++) match = payload.PayloadBytes[i + j] == signature[j];
                if (match) { offset = i; break; }
            }
            Assert.GreaterOrEqual(offset, 0, "must locate a real serialized rotation block");
            Array.Copy(BitConverter.GetBytes(invalidCount), 0, payload.PayloadBytes, offset, 4);
            Assert.Throws<InvalidOperationException>(() => MatchEngine.RestoreFromSnapshot(
                engine.CaptureDurableHeader(), payload, Seed));
        }

        [Test]
        public void ExplicitSupport_IsExactIdentity_ForEveryAgent()
        {
            var a = new MatchEngine(Seed);
            var expected = new byte[120][];
            for (int i = 0; i < expected.Length; i++) { a.RunTick(); expected[i] = a.CurrentSnapshotDigest; }
            var b = new MatchEngine(Seed);
            for (int i = 0; i < 22; i++) b.SetPlayerTactic(i, PlayerTactic.Default(PlayerRole.Default));
            for (int i = 0; i < expected.Length; i++)
            {
                b.RunTick();
                CollectionAssert.AreEqual(expected[i], b.CurrentSnapshotDigest);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-09 | —      | Kickoff lock, mixed requests, replay with six rotation rows, corruption and Support identity. |
#endregion
