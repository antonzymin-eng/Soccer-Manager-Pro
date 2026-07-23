// File:     src/decision-tree/Tests/DecisionTreeStateTests.cs
// Created:  2026-06-22
// Modified: 2026-06-22
// Author:   —
// Spec:     Decision Tree #8 §3.7; Match Engine design note §2.6 (Phase D step D0); Code Standards #20
// Purpose:  Locks the Phase D D0 DecisionTree snapshot seam: (1) a DecisionTreeState survives a
//           CanonicalSerializer write/read round-trip byte-for-byte (the serialization the match-engine
//           snapshot layer will perform at the Phase-D snapshot extension), and (2) CaptureState →
//           RestoreState → CaptureState is the identity on the cross-tick state machine.

using System.Reflection;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    public sealed class DecisionTreeStateTests
    {
        private static DecisionTreeState MakePopulatedState()
        {
            var passParams = new PassRequest
            {
                AgentId          = 4,
                PassType         = PassType.Lofted,
                CrossSubType     = CrossSubType.Whipped,
                TargetAgentId    = 7,
                TargetPosition   = new Vector3(62.5f, 40.0f, 0.0f),
                IntendedDistance = 18.0f,
                Urgency          = 0.35f,
                IsWeakFoot       = true,
                TeamId           = 0,
                FrameNumber      = 612
            };

            var shotParams = new ShotRequest
            {
                AgentId         = 4,
                PowerIntent     = 0.7f,
                ContactZone     = ContactZone.BelowCentre,
                SpinIntent      = 0.2f,
                PlacementTarget = new Vector2(0.6f, 0.8f),
                IsWeakFoot      = false,
                DistanceToGoal  = 22.0f,
                TeamId          = 0,
                FrameNumber     = 612
            };

            var lastAction = new AgentAction(
                agentId: 4,
                type: ActionType.PASS,
                targetAgentId: 7,
                targetPosition: new Vector2(62.5f, 40.0f),
                passParams: passParams,
                shotParams: shotParams,
                utilityScore: 0.84f,
                heartbeatTick: 102);

            return new DecisionTreeState(
                state: (int)DtState.EXECUTING,
                lastAction: lastAction,
                hasDispatchedAction: true);
        }

        private static void Serialize(byte[] buf, ref int o, in DecisionTreeState s)
        {
            CanonicalSerializer.WriteI32(buf, ref o, s.State);
            CanonicalSerializer.WriteBool(buf, ref o, s.HasDispatchedAction);

            AgentAction a = s.LastAction;
            CanonicalSerializer.WriteI32(buf, ref o, a.AgentId);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.Type);
            CanonicalSerializer.WriteI32(buf, ref o, a.TargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, a.TargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.TargetPosition.y);

            // PassParams
            CanonicalSerializer.WriteI32(buf, ref o, a.PassParams.AgentId);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.PassParams.PassType);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.PassParams.CrossSubType);
            CanonicalSerializer.WriteI32(buf, ref o, a.PassParams.TargetAgentId);
            CanonicalSerializer.WriteF32(buf, ref o, a.PassParams.TargetPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.PassParams.TargetPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.PassParams.TargetPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, a.PassParams.IntendedDistance);
            CanonicalSerializer.WriteF32(buf, ref o, a.PassParams.Urgency);
            CanonicalSerializer.WriteBool(buf, ref o, a.PassParams.IsWeakFoot);
            CanonicalSerializer.WriteI32(buf, ref o, a.PassParams.TeamId);
            CanonicalSerializer.WriteI32(buf, ref o, a.PassParams.FrameNumber);

            // ShotParams
            CanonicalSerializer.WriteI32(buf, ref o, a.ShotParams.AgentId);
            CanonicalSerializer.WriteF32(buf, ref o, a.ShotParams.PowerIntent);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.ShotParams.ContactZone);
            CanonicalSerializer.WriteF32(buf, ref o, a.ShotParams.SpinIntent);
            CanonicalSerializer.WriteF32(buf, ref o, a.ShotParams.PlacementTarget.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.ShotParams.PlacementTarget.y);
            CanonicalSerializer.WriteBool(buf, ref o, a.ShotParams.IsWeakFoot);
            CanonicalSerializer.WriteF32(buf, ref o, a.ShotParams.DistanceToGoal);
            CanonicalSerializer.WriteI32(buf, ref o, a.ShotParams.TeamId);
            CanonicalSerializer.WriteI32(buf, ref o, a.ShotParams.FrameNumber);

            CanonicalSerializer.WriteF32(buf, ref o, a.UtilityScore);
            CanonicalSerializer.WriteI32(buf, ref o, a.HeartbeatTick);
        }

        private static DecisionTreeState Deserialize(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);
            bool hasDispatched = CanonicalSerializer.ReadBool(buf, ref o);

            int agentId        = CanonicalSerializer.ReadI32(buf, ref o);
            var type           = (ActionType)CanonicalSerializer.ReadI32(buf, ref o);
            int targetAgentId  = CanonicalSerializer.ReadI32(buf, ref o);
            var targetPosition = new Vector2(
                CanonicalSerializer.ReadF32(buf, ref o),
                CanonicalSerializer.ReadF32(buf, ref o));

            var passParams = new PassRequest
            {
                AgentId        = CanonicalSerializer.ReadI32(buf, ref o),
                PassType       = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                CrossSubType   = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o),
                TargetAgentId  = CanonicalSerializer.ReadI32(buf, ref o),
                TargetPosition = new Vector3(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDistance = CanonicalSerializer.ReadF32(buf, ref o),
                Urgency          = CanonicalSerializer.ReadF32(buf, ref o),
                IsWeakFoot       = CanonicalSerializer.ReadBool(buf, ref o),
                TeamId           = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber      = CanonicalSerializer.ReadI32(buf, ref o)
            };

            var shotParams = new ShotRequest
            {
                AgentId         = CanonicalSerializer.ReadI32(buf, ref o),
                PowerIntent     = CanonicalSerializer.ReadF32(buf, ref o),
                ContactZone     = (ContactZone)CanonicalSerializer.ReadI32(buf, ref o),
                SpinIntent      = CanonicalSerializer.ReadF32(buf, ref o),
                PlacementTarget = new Vector2(
                    CanonicalSerializer.ReadF32(buf, ref o),
                    CanonicalSerializer.ReadF32(buf, ref o)),
                IsWeakFoot     = CanonicalSerializer.ReadBool(buf, ref o),
                DistanceToGoal = CanonicalSerializer.ReadF32(buf, ref o),
                TeamId         = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber    = CanonicalSerializer.ReadI32(buf, ref o)
            };

            float utilityScore = CanonicalSerializer.ReadF32(buf, ref o);
            int heartbeatTick  = CanonicalSerializer.ReadI32(buf, ref o);

            var lastAction = new AgentAction(
                agentId, type, targetAgentId, targetPosition,
                passParams, shotParams, utilityScore, heartbeatTick);

            return new DecisionTreeState(state, in lastAction, hasDispatched);
        }

        private static void AssertStateEquals(in DecisionTreeState expected, in DecisionTreeState actual)
        {
            Assert.AreEqual(expected.State, actual.State, "State");
            Assert.AreEqual(expected.HasDispatchedAction, actual.HasDispatchedAction, "HasDispatchedAction");

            AgentAction e = expected.LastAction;
            AgentAction a = actual.LastAction;
            Assert.AreEqual(e.AgentId, a.AgentId, "LastAction.AgentId");
            Assert.AreEqual(e.Type, a.Type, "LastAction.Type");
            Assert.AreEqual(e.TargetAgentId, a.TargetAgentId, "LastAction.TargetAgentId");
            Assert.AreEqual(e.TargetPosition, a.TargetPosition, "LastAction.TargetPosition");
            Assert.AreEqual(e.UtilityScore, a.UtilityScore, "LastAction.UtilityScore");
            Assert.AreEqual(e.HeartbeatTick, a.HeartbeatTick, "LastAction.HeartbeatTick");

            Assert.AreEqual(e.PassParams.AgentId, a.PassParams.AgentId, "PassParams.AgentId");
            Assert.AreEqual(e.PassParams.PassType, a.PassParams.PassType, "PassParams.PassType");
            Assert.AreEqual(e.PassParams.CrossSubType, a.PassParams.CrossSubType, "PassParams.CrossSubType");
            Assert.AreEqual(e.PassParams.TargetAgentId, a.PassParams.TargetAgentId, "PassParams.TargetAgentId");
            Assert.AreEqual(e.PassParams.TargetPosition, a.PassParams.TargetPosition, "PassParams.TargetPosition");
            Assert.AreEqual(e.PassParams.IntendedDistance, a.PassParams.IntendedDistance, "PassParams.IntendedDistance");
            Assert.AreEqual(e.PassParams.Urgency, a.PassParams.Urgency, "PassParams.Urgency");
            Assert.AreEqual(e.PassParams.IsWeakFoot, a.PassParams.IsWeakFoot, "PassParams.IsWeakFoot");
            Assert.AreEqual(e.PassParams.TeamId, a.PassParams.TeamId, "PassParams.TeamId");
            Assert.AreEqual(e.PassParams.FrameNumber, a.PassParams.FrameNumber, "PassParams.FrameNumber");

            Assert.AreEqual(e.ShotParams.AgentId, a.ShotParams.AgentId, "ShotParams.AgentId");
            Assert.AreEqual(e.ShotParams.PowerIntent, a.ShotParams.PowerIntent, "ShotParams.PowerIntent");
            Assert.AreEqual(e.ShotParams.ContactZone, a.ShotParams.ContactZone, "ShotParams.ContactZone");
            Assert.AreEqual(e.ShotParams.SpinIntent, a.ShotParams.SpinIntent, "ShotParams.SpinIntent");
            Assert.AreEqual(e.ShotParams.PlacementTarget, a.ShotParams.PlacementTarget, "ShotParams.PlacementTarget");
            Assert.AreEqual(e.ShotParams.IsWeakFoot, a.ShotParams.IsWeakFoot, "ShotParams.IsWeakFoot");
            Assert.AreEqual(e.ShotParams.DistanceToGoal, a.ShotParams.DistanceToGoal, "ShotParams.DistanceToGoal");
            Assert.AreEqual(e.ShotParams.TeamId, a.ShotParams.TeamId, "ShotParams.TeamId");
            Assert.AreEqual(e.ShotParams.FrameNumber, a.ShotParams.FrameNumber, "ShotParams.FrameNumber");
        }

        [Test]
        public void DecisionTreeState_SurvivesCanonicalSerializerRoundTrip()
        {
            DecisionTreeState original = MakePopulatedState();

            byte[] buf = new byte[512];
            int writeOffset = 0;
            Serialize(buf, ref writeOffset, in original);

            int readOffset = 0;
            DecisionTreeState reconstructed = Deserialize(buf, ref readOffset);

            Assert.AreEqual(writeOffset, readOffset, "read offset must consume exactly the written bytes");
            AssertStateEquals(in original, in reconstructed);
        }

        [Test]
        public void DecisionTree_CaptureRestoreCapture_IsIdentity()
        {
            // Deps null: CaptureState/RestoreState never touch the movement controller or executors.
            var dt = new DecisionTree(agentId: 4, movementController: null, matchSeed: 0x1234UL);
            DecisionTreeState seeded = MakePopulatedState();

            dt.RestoreState(in seeded);
            DecisionTreeState recaptured = dt.CaptureState();

            AssertStateEquals(in seeded, in recaptured);
            Assert.AreEqual(DtState.EXECUTING, dt.State, "RestoreState must drive the live state machine");
        }

        [Test]
        public void DecisionTree_FreshInstance_CapturesIdleDefault()
        {
            var dt = new DecisionTree(agentId: 0, movementController: null, matchSeed: 1UL);
            DecisionTreeState captured = dt.CaptureState();

            Assert.AreEqual((int)DtState.IDLE, captured.State, "fresh DT is IDLE");
            Assert.IsFalse(captured.HasDispatchedAction, "fresh DT has dispatched nothing");
        }

        [Test]
        public void DecisionTree_InstanceFieldCount_MatchesCapturedSet()
        {
            // Mechanical coupling guard, the analogue of the B0 OscillationGuard BufferSize assert and the
            // C0 executor field-count locks: adding cross-tick state without extending DecisionTreeState /
            // CaptureState / RestoreState would silently drop it from the snapshot and diverge replay
            // (§2.6 trap). 10 instance fields = 6 injected/identity (_movementController / _passExecutor /
            // _shotExecutor / _saveDispatch (ERR-008-013) / _agentId / _matchSeed) + 1 scratch
            // (_optionBuffer) + 3 captured cross-tick (_state / _lastAction / _hasDispatchedAction).
            // _saveDispatch is an injected dependency (the GK save sink), NOT cross-tick state — the same
            // legitimately-excluded class as the executors.
            int fieldCount = typeof(DecisionTree)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Length;

            Assert.AreEqual(10, fieldCount,
                "DecisionTree instance field count changed. If you added cross-tick state, extend " +
                "DecisionTreeState + CaptureState + RestoreState, then update this count (and confirm " +
                "_matchSeed / _optionBuffer remain the only legitimately excluded fields).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Initial implementation — Phase D D0 DecisionTree snapshot-seam |
// |         |            |        | round-trip + Capture/Restore identity + fresh-IDLE default +   |
// |         |            |        | reflection field-count lock (silent-omission guard).          |
#endregion
