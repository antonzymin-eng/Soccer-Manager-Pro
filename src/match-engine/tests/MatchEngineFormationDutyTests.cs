// File:     src/match-engine/tests/MatchEngineFormationDutyTests.cs
// Created:  2026-11-09
// Modified: 2026-11-10
// Author:   —
// Spec:     #12 FR-PA-039, ERR-012-013; #21 ERR-021-009; #25 ERR-025-001
// Purpose:  Kickoff formation lock, mixed requests, variable rotation rows and Duty replay.

using System;
using System.Reflection;

using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using UnityIs = UnityEngine.TestTools.Constraints.Is;
#endif

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;
using TacticalDirector.AttackingAI;
using TacticalDirector.DefensiveAI;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Outcome tests for the approved kickoff-only formation and Duty landing.</summary>
    [TestFixture]
    public sealed class MatchEngineFormationDutyTests
    {
        private const ulong Seed = 0x0123456789ABCDEFUL;

        [TestCase(0, TacticFormation.F442)]
        [TestCase(1, TacticFormation.F442)]
        [TestCase(0, TacticFormation.F433)]
        [TestCase(1, TacticFormation.F433)]
        [TestCase(0, TacticFormation.F4231)]
        [TestCase(1, TacticFormation.F4231)]
        public void KickoffSelection_FirstUseAllocatesNothing_AndFirstTickKeepsBaseline(int team, TacticFormation formation)
        {
            foreach (TacticFormation warmFamily in new[] { TacticFormation.F442, TacticFormation.F433, TacticFormation.F4231 })
            {
                var warm = new MatchEngine(Seed);
                warm.SetTeamTactic(team, Tactic(warmFamily));
                warm.RunTick();
            }

            var engine = new MatchEngine(Seed);
            engine.SetTeamTactic(team, Tactic(formation));
            var snapshots = (PositioningPerceptionSnapshot[])typeof(MatchEngine)
                .GetField("_posSnapshots", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(engine);
            var positioning = engine.TestOnly_Positioning(team);
            FormationFamily family = PositioningAI.TacticTranslation.ToFormationFamily(formation);
            // Measure the actual cold selection/seeding seam. Construction, reflection and delegate
            // creation are outside the window; no simulation tick has warmed this instance.
            TestDelegate select = () => positioning.InitializeFormation(family, snapshots[team]);
#if UNITY_5_3_OR_NEWER
            TestDelegate control = () => GC.KeepAlive(new byte[1024]);
            Assert.That(control, UnityIs.AllocatingGCMemory(), "The native recorder must detect a known allocation.");
            Assert.That(select, UnityIs.Not.AllocatingGCMemory());
#else
            long controlBefore = GC.GetAllocatedBytesForCurrentThread();
            byte[] control = new byte[1024];
            long controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;
            GC.KeepAlive(control);
            Assert.Greater(controlBytes, 0L, "The counter must detect a known allocation.");
            long before = GC.GetAllocatedBytesForCurrentThread();
            select();
            long selectionBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0L, selectionBytes, "The kickoff formation selection/seeding itself must allocate zero bytes.");
#endif
            Assert.AreEqual(family, positioning.Formation);
            Assert.AreEqual(formation == TacticFormation.F4231 ? 6 : 5, positioning.CaptureRotationState().PairCount);
            engine.RunTick();
            AssertFirstFrame(engine, team, formation);

#if !UNITY_5_3_OR_NEWER
            // The whole first tick has an existing 536-byte cost on the frozen Linux lane, also
            // reproduced on unchanged main. Do not hide it or attribute it to formation routing.
            // Each sample below is a fresh engine's first actual RunTick, with only lazy crypto
            // setup primed. Exact equality rejects any added per-match routing allocation.
            long baseline = FewestFirstTickBytes(team, TacticFormation.F442);
            long selected = FewestFirstTickBytes(team, formation);
            Assert.AreEqual(baseline, selected, "The first full tick must not add allocation beyond the default first-tick baseline.");
            TestContext.WriteLine("First RunTick allocation: default=" + baseline + " selected=" + selected + " bytes; selection=0 bytes.");
#endif
        }

#if !UNITY_5_3_OR_NEWER
        private static long FewestFirstTickBytes(int team, TacticFormation formation)
        {
            long fewest = long.MaxValue;
            // Tiered-JIT bookkeeping can charge one window. A repeatable per-match allocation
            // cannot disappear: all eight windows begin on a different engine at clock zero.
            for (int round = 0; round < 8; round++)
            {
                var engine = NewAllocationProbe(team, formation);
                long before = GC.GetAllocatedBytesForCurrentThread();
                engine.RunTick();
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                fewest = Math.Min(fewest, bytes);
                AssertFirstFrame(engine, team, formation);
            }
            return fewest;
        }
#endif

        private static MatchEngine NewAllocationProbe(int team, TacticFormation formation)
        {
            var engine = new MatchEngine(Seed);
            engine.SetTeamTactic(team, Tactic(formation));
            var orchestrator = (TickOrchestrator)typeof(MatchEngine)
                .GetField("_orchestrator", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(engine);
            var codec = (SnapshotCodec)typeof(TickOrchestrator)
                .GetField("_codec", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(orchestrator);
            // The existing managed SHA backend is lazy on Linux. Warm that dependency without
            // advancing its digest chain, running a physics tick, or initializing positioning.
            codec.ValidateCurrentDigest(orchestrator.CurrentHeader, orchestrator.CurrentPayload);
            Assert.AreEqual(0UL, orchestrator.CurrentHeader.Tick);
            Assert.AreEqual(TacticFormation.F442, engine.GetEffectiveFormation(team));
            return engine;
        }

        private static void AssertFirstFrame(MatchEngine engine, int team, TacticFormation formation)
        {
            var header = engine.CaptureDurableHeader();
            Assert.AreEqual(1UL, header.Tick, "The allocation oracle must execute exactly the first physics tick.");
            Assert.AreEqual(formation, engine.GetEffectiveFormation(team));
            CollectionAssert.AreEqual(new byte[32], header.PrevSnapshotDigest,
                "Hash warm-up must not advance the match's digest chain.");
        }

        internal static TeamTactic Tactic(TacticFormation formation, Mentality mentality = Mentality.Balanced) =>
            new TeamTactic(mentality, formation, Tempo.Standard, TacticWidth.Standard,
                TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f,
                TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
                TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        [TestCase(0, TacticFormation.F433, FormationFamily.F433, 5)]
        [TestCase(1, TacticFormation.F433, FormationFamily.F433, 5)]
        [TestCase(0, TacticFormation.F4231, FormationFamily.F4231, 6)]
        [TestCase(1, TacticFormation.F4231, FormationFamily.F4231, 6)]
        [TestCase(0, (TacticFormation)255, FormationFamily.F442, 5)]
        [TestCase(1, (TacticFormation)255, FormationFamily.F442, 5)]
        public void FinalPreKickoffChoice_LocksBeforeFrameOne_InExistingObject(
            int team, TacticFormation requested, FormationFamily expected, int rows)
        {
            var engine = new MatchEngine(Seed);
            var positioning = engine.TestOnly_Positioning(team);
            engine.SetTeamTactic(team, Tactic(TacticFormation.F433));
            engine.SetTeamTactic(team, Tactic(requested));
            engine.RunTick();
            Assert.AreEqual(requested, engine.GetRequestedFormation(team), "requested value is an unnormalized audit value");
            Assert.AreEqual(requested, team == 0 ? engine.Formations.HomeRequested : engine.Formations.AwayRequested);
            Assert.AreEqual(PositioningAI.TacticTranslation.ToTacticFormation(expected), engine.GetEffectiveFormation(team));
            Assert.AreSame(positioning, engine.TestOnly_Positioning(team), "retained consumer views");
            Assert.AreEqual(expected, positioning.Formation);
            Assert.AreEqual(AnchorCalculator.ComputeAnchor(PositioningAIConstants.GetFormationSlots(expected)[5]),
                positioning.GetFormationSlot(team * 11 + 5), "selected anchor exists before first AI tick");
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
                var pairs = RotationAdjacencyCatalogue.GetPairs(PositioningAI.TacticTranslation.ToFormationFamily(formation));
                binding[pairs[0].SlotA] = pairs[0].SlotB; binding[pairs[0].SlotB] = pairs[0].SlotA;
                rot.RestoreBinding(binding);
                var active = new RotationPairState { Rotated = true, HoldTicksRemaining = 10 };
                rot.RestorePairState(0, in active);
                var state = new RotationPairState { TriggerDwellTicks = 1 };
                rot.RestorePairState(rot.PairCount - 1, in state);
            }
            // Let the real positioning stride apply the restored binding and update its entity
            // lookup/composed cache before saving, rather than saving a pre-compose fixture state.
            for (int i = 0; i < 6; i++) engine.RunTick();
            var header = engine.CaptureDurableHeader();
            var payload = engine.CaptureDurablePayload();
            var expected = new byte[240][];
            for (int i = 0; i < expected.Length; i++) { engine.RunTick(); expected[i] = engine.CurrentSnapshotDigest; }
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            for (int t = 0; t < 2; t++)
            {
                var pairs = RotationAdjacencyCatalogue.GetPairs(PositioningAI.TacticTranslation.ToFormationFamily(formation));
                Assert.AreEqual(pairs[0].SlotB, restored.TestOnly_SlotBinding(t, pairs[0].SlotA));
                Assert.IsTrue(restored.TestOnly_RotationPairState(t, 0).Rotated);
                Assert.AreEqual(10, restored.TestOnly_RotationPairState(t, 0).HoldTicksRemaining);
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

        private static Squad SquadFor(int club)
        {
            var rng = new DeterministicRngService(Seed ^ (ulong)club);
            int stream = rng.RegisterStream("formation-duty.roster", SubsystemOrdinals.PlayerDatabase, club, 1);
            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            for (int i = 0; i < template.Length; i++)
                template[i] = i < 3 ? PlayerPosition.Goalkeeper : i < 11 ? PlayerPosition.Defender
                    : i < 19 ? PlayerPosition.Midfielder : PlayerPosition.Forward;
            return RosterGenerator.Generate(rng, stream, club, template);
        }

        private sealed class TwoSquads : ISquadProvider
        {
            internal readonly Squad Home = SquadFor(1), Away = SquadFor(2);
            public Squad ResolveByClubId(int clubId) => clubId == 1 ? Home : clubId == 2 ? Away
                : throw new ArgumentOutOfRangeException(nameof(clubId));
        }

        [TestCase(TacticFormation.F433)]
        [TestCase(TacticFormation.F4231)]
        public void ConfiguredSquads_KeepSelectedIds_AndRestoreFormationDutyContinuation(TacticFormation family)
        {
            var squads = new TwoSquads();
            var control = new MatchEngine(Seed); control.ConfigureSquads(squads.Home, squads.Away);
            var ids = new int[22]; for (int i = 0; i < ids.Length; i++) ids[i] = control.AgentPlayerId(i);
            var engine = new MatchEngine(Seed);
            for (int t = 0; t < 2; t++) engine.SetTeamTactic(t, Tactic(family));
            engine.ConfigureSquads(squads.Home, squads.Away);
            for (int i = 0; i < ids.Length; i++) Assert.AreEqual(ids[i], engine.AgentPlayerId(i), "F442-selected ID " + i);
            engine.SetPlayerTactic(6, new PlayerTactic(PlayerRole.Default, Duty.Attack, PlayerInstructions.Default));
            engine.SetPlayerTactic(17, new PlayerTactic(PlayerRole.Default, Duty.Defend, PlayerInstructions.Default));
            for (int i = 0; i < 60; i++) engine.RunTick();
            var header = engine.CaptureDurableHeader(); var payload = engine.CaptureDurablePayload();
            var expected = new byte[60][];
            for (int i = 0; i < expected.Length; i++) { engine.RunTick(); expected[i] = engine.CurrentSnapshotDigest; }
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed, squads);
            for (int t = 0; t < 2; t++) Assert.AreEqual(family, restored.GetEffectiveFormation(t));
            for (int i = 0; i < ids.Length; i++) Assert.AreEqual(ids[i], restored.AgentPlayerId(i));
            for (int i = 0; i < expected.Length; i++)
            {
                restored.RunTick(); CollectionAssert.AreEqual(expected[i], restored.CurrentSnapshotDigest);
            }
        }

        [TestCase(0, 11)]
        [TestCase(1, 11)]
        [TestCase(0, 12)]
        [TestCase(1, 12)]
        public void Restore_KeepsKickoffFamily_WhenRequestedAndActiveTacticsDiffer(int team, int saveFrame)
        {
            var engine = new MatchEngine(Seed);
            engine.SetTeamTactic(team, Tactic(TacticFormation.F4231));
            for (int i = 0; i < 6; i++) engine.RunTick();
            engine.SetTeamTactic(team, Tactic(TacticFormation.F442, Mentality.VeryAttacking));
            while (engine.CurrentTick < (ulong)saveFrame) engine.RunTick();
            var header = engine.CaptureDurableHeader(); var payload = engine.CaptureDurablePayload();
            var reference = new byte[60][];
            for (int i = 0; i < reference.Length; i++) { engine.RunTick(); reference[i] = engine.CurrentSnapshotDigest; }
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            Assert.AreEqual(TacticFormation.F4231, restored.GetEffectiveFormation(team));
            Assert.AreEqual(TacticFormation.F442, restored.GetRequestedFormation(team));
            Assert.AreEqual(6, restored.TestOnly_Positioning(team).CaptureRotationState().PairCount);
            for (int i = 0; i < reference.Length; i++)
            {
                restored.RunTick();
                CollectionAssert.AreEqual(reference[i], restored.CurrentSnapshotDigest, "frame " + i);
            }
        }

        [Test]
        public void SyntheticTickZero_UninitializedCanSelectFinalChoice()
        {
            var original = new MatchEngine(Seed);
            var body = original.CapturePostSnapshotWorldState();
            var restored = new MatchEngine(Seed);
            restored.ApplyPostSnapshotWorldState(body, null);
            restored.SetTeamTactic(0, Tactic(TacticFormation.F4231));
            restored.RunTick();
            Assert.AreEqual(TacticFormation.F4231, restored.GetEffectiveFormation(0));
        }

        [Test]
        public void SyntheticTickZero_InitializedDoesNotResamplePendingFormation()
        {
            var original = new MatchEngine(Seed);
            original.SetTeamTactic(0, Tactic(TacticFormation.F4231)); original.RunTick();
            var body = original.CapturePostSnapshotWorldState();
            // Synthetic clock-0 overlay with an explicitly initialized six-row family.
            Array.Clear(body.PayloadBytes, sizeof(uint), sizeof(ulong));
            var restored = new MatchEngine(Seed);
            restored.ApplyPostSnapshotWorldState(body, null);
            restored.SetTeamTactic(0, Tactic(TacticFormation.F433));
            restored.RunTick();
            Assert.AreEqual(TacticFormation.F4231, restored.GetEffectiveFormation(0));
            Assert.AreEqual(6, restored.TestOnly_Positioning(0).CaptureRotationState().PairCount);
        }

        private static object Field(object instance, string name) =>
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        [TestCase(0, TacticFormation.F433)]
        [TestCase(1, TacticFormation.F433)]
        [TestCase(0, TacticFormation.F4231)]
        [TestCase(1, TacticFormation.F4231)]
        public void AllPositioningConsumers_ReadTheSelectedFamily(int team, TacticFormation family)
        {
            var control = new MatchEngine(Seed);
            for (int i = 0; i < 6; i++) control.RunTick();
            var oldSlot = control.TestOnly_Positioning(team).GetFormationSlot(team * 11 + 5);
            var engine = new MatchEngine(Seed); engine.SetTeamTactic(team, Tactic(family));
            for (int i = 0; i < 6; i++) engine.RunTick();
            var expected = engine.TestOnly_Positioning(team).GetFormationSlot(team * 11 + 5);
            Assert.AreNotEqual(oldSlot, expected, "non-default family changes the target");
            var press = ((PressingAITick[])Field(engine, "_pressing"))[team];
            var attack = ((AttackingAITick[])Field(engine, "_attacking"))[team];
            Assert.AreEqual(expected, ((PositioningAIView)Field(press, "_posAI")).GetFormationSlot(team * 11 + 5));
            Assert.AreEqual(expected, ((PositioningAIView)Field(attack, "_posAI")).GetFormationSlot(team * 11 + 5));
            var defensive = ((DefensiveSnapshot[])Field(engine, "_defSnapshots"))[team];
            Assert.AreEqual(expected, defensive.Agents[team * 11 + 5].BaselineSlot);
        }

        [Test]
        public void V28_IsRejectedBeforeAnyFormationStateIsDecoded()
        {
            var engine = new MatchEngine(Seed); engine.RunTick();
            var payload = engine.CaptureDurablePayload();
            Array.Copy(BitConverter.GetBytes(28u), payload.PayloadBytes, 4);
            payload.PayloadBytes[12] = 255; // Must report version mismatch first.
            var error = Assert.Throws<InvalidOperationException>(() => MatchEngine.RestoreFromSnapshot(
                engine.CaptureDurableHeader(), payload, Seed));
            StringAssert.Contains("Snapshot schema version 28", error.Message);
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
        [TestCase(5)]
        public void MalformedRotationBlock_RejectsCountOrTruncation(int invalidCount)
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
            if (invalidCount == 5) payload.BytesWritten = offset + 4;
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
// | 1.1 | 2026-11-10 | — | Strengthens active/pending rotation replay, consumer views, synthetic tick-0 and exact Support byte assertions. |
// | 1.2 | 2026-11-10 | — | Real-squad selected-ID/reprojection coverage and active-binding save after an actual composition stride. |
// | 1.3 | 2026-11-10 | — | Zero-allocation cold selection and full-first-tick baseline comparison for each family/team; existing crypto setup and baseline cost explicitly distinguished. |
#endregion
