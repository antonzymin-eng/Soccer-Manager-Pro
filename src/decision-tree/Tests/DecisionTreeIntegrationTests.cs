// File:     src/decision-tree/Tests/DecisionTreeIntegrationTests.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.6, §3.7, §4.1–4.3, Code Standards #20
// Purpose:  Integration tests for the full 6-step Decision Tree pipeline via the public
//           DecisionTree class. Verifies state machine transitions (IDLE → EVALUATING →
//           EXECUTING → IDLE), interrupt handling, and that LastAction carries a valid
//           selected action after a complete pipeline run.
//           Uses only public API types — no InternalsVisibleTo dependency.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class DecisionTreeIntegrationTests
    {
        // ── Stub movement controller (public interface) ───────────────────────

        private sealed class RecordingController : IDtMovementController
        {
            public int             LastAgentId = -1;
            public MovementCommand LastCommand;
            public int             CallCount;

            public void SubmitCommand(int agentId, MovementCommand command)
            {
                LastAgentId = agentId;
                LastCommand = command;
                CallCount++;
            }
        }

        // ── UT-24: Initial state is IDLE ──────────────────────────────────────

        [Test]
        public void InitialState_IsIdle()
        {
            DecisionTree dt = new DecisionTree(0, new RecordingController(), 0xABCDUL);
            Assert.AreEqual(DtState.IDLE, dt.State);
        }

        // ── UT-25: Valid off-ball snapshot → EXECUTING after pipeline ─────────

        [Test]
        public void ValidSnapshot_OffBall_TransitionsToExecuting()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);

            Assert.AreEqual(DtState.EXECUTING, dt.State,
                "After a valid snapshot the pipeline must complete and reach EXECUTING");
        }

        // ── UT-26: NotifyInterrupt → INTERRUPTED ──────────────────────────────

        [Test]
        public void NotifyInterrupt_TransitionsToInterrupted()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);
            dt.NotifyInterrupt();

            Assert.AreEqual(DtState.INTERRUPTED, dt.State);
        }

        // ── UT-27: NotifyActionComplete → IDLE ───────────────────────────────

        [Test]
        public void NotifyActionComplete_TransitionsToIdle()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);
            dt.NotifyActionComplete();

            Assert.AreEqual(DtState.IDLE, dt.State);
        }

        // ── UT-28: Invalid snapshot (wrong ObserverId) → stays IDLE ──────────

        [Test]
        public void InvalidSnapshot_WrongObserverId_StaysIdle()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            // Build snapshot with ObserverId=99 != agentId=0
            FilteredView badSnap = new FilteredView
            {
                ObserverId               = 99,
                FrameNumber              = 1,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleOpponents         = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            MatchContext mc2 = BuildMatchContext(possessingAgentId: 5);
            dt.ReceiveSnapshot(
                badSnap,
                mc2,
                TacticalContext.Stage0Default(new Vector2(50f, 34f)),
                DtAgentAttributes.CreateDefault(teamId: 0),
                BuildAgentState(new Vector2(52f, 34f)),
                pressureScalar: 0.0f);

            Assert.AreEqual(DtState.IDLE, dt.State,
                "Invalid snapshot (wrong ObserverId) must leave state as IDLE");
        }

        // ── UT-29: LastAction has valid type after pipeline ───────────────────

        [Test]
        public void AfterValidPipeline_LastActionHasValidType()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);

            ActionType t = dt.LastAction.Type;
            bool isKnownType = t == ActionType.MOVE_TO_POSITION
                            || t == ActionType.PRESS
                            || t == ActionType.INTERCEPT
                            || t == ActionType.HOLD;

            Assert.IsTrue(isKnownType,
                $"Off-ball pipeline must select a movement action; got {t}");
        }

        // ── UT-30: LastAction carries correct AgentId ─────────────────────────

        [Test]
        public void AfterValidPipeline_LastActionCarriesCorrectAgentId()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(5, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 5, frameNumber: 1);

            Assert.AreEqual(5, dt.LastAction.AgentId);
        }

        // ── UT-31: Movement command submitted to controller after pipeline ─────

        [Test]
        public void AfterValidPipeline_ControllerReceivesCommand()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);

            Assert.Greater(mc.CallCount, 0,
                "Movement controller must receive at least one command per pipeline run");
        }

        // ── UT-32: SetMatchSeed accepted without exception ────────────────────

        [Test]
        public void SetMatchSeed_AcceptedWithoutException()
        {
            DecisionTree dt = new DecisionTree(0, new RecordingController(), 0xABCDUL);
            Assert.DoesNotThrow(() => dt.SetMatchSeed(0xDEADBEEFCAFEBABEUL));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void ReceiveMinimalOffBallSnapshot(
            DecisionTree dt,
            int agentId,
            int frameNumber)
        {
            // Off-ball scenario: possessing agent is different from this agent.
            // No opponents visible → PRESS gate fails → no PRESS generated.
            // BallVisible=false → INTERCEPT ball-speed check → likely no INTERCEPT.
            // Only MOVE_TO_POSITION is guaranteed in off-ball branch.
            FilteredView snap = new FilteredView
            {
                ObserverId               = agentId,
                FrameNumber              = frameNumber,
                BallVisible              = false,
                BallPerceivedPosition    = new Vector2(52f, 34f),
                BallStalenessFrames      = 1,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleTeammatesCount    = 0,
                VisibleOpponents         = new PerceivedAgent[0],
                VisibleOpponentsCount    = 0,
                BlindSidePerceivedAgents = new PerceivedAgent[0],
                BlindSidePerceivedAgentsCount = 0
            };

            MatchContext mc2  = BuildMatchContext(possessingAgentId: 15); // away agent → off-ball for agent 0
            TacticalContext tc = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            DtAgentAttributes attr = DtAgentAttributes.CreateDefault(teamId: agentId < 11 ? 0 : 1);
            AgentState state = BuildAgentState(new Vector2(52f, 34f));

            dt.ReceiveSnapshot(snap, mc2, tc, attr, state, pressureScalar: 0.0f);
        }

        private static MatchContext BuildMatchContext(int possessingAgentId)
        {
            return new MatchContext
            {
                HomeScore          = 0,
                AwayScore          = 0,
                MatchTimeSeconds   = 900.0f,
                Possession         = PossessionState.AWAY_TEAM,
                PossessingAgentId  = possessingAgentId,
                Phase              = MatchPhase.OPEN_PLAY,
                BallPosition       = new Vector2(52f, 34f),
                BallZone           = FieldZone.MIDFIELD
            };
        }

        private static AgentState BuildAgentState(Vector2 position)
        {
            return new AgentState
            {
                Position        = position,
                Velocity        = Vector2.zero,
                FacingDirection = Vector2.right,
                AerobicPool     = 0.80f,
                CurrentState    = AgentMovementState.IDLE
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
