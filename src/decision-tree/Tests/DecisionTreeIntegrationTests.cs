// File:     src/decision-tree/Tests/DecisionTreeIntegrationTests.cs
// Created:  2026-05-29
// Modified: 2026-06-11
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
        // AR-2 M-11: ReceiveSnapshot publishes DecisionMadeEvent (Tier C) on every
        // successful evaluation, and CosmeticChannel.Publish THROWS
        // ERR_EVT_UNREGISTERED_ORDINAL for an unbooted registry — so the fixture must
        // boot it. Initialize() is idempotent (s_registered guard, v1.2), making this
        // safe alongside EventBusWiringSmokeTests in the same test process. The
        // pre-audit suite lacked this boot and would have thrown on first run — it
        // was never caught because the assembly never compiled (H-1).
        [OneTimeSetUp]
        public void BootEventRegistry()
        {
            EventBusRegistrar.Initialize();
        }

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

        // ── UT-33 (AR-2 H-3 lock): HOLD selection lands in IDLE (§3.7.2 row 3) ─

        [Test]
        public void HoldSelected_TransitionsToIdle_NotExecuting()
        {
            // On-ball, GROUNDED (no DRIBBLE), no teammates (no PASS), out of shooting
            // range (no SHOOT) → HOLD is the only candidate. §3.7.2 row 3: HOLD
            // engages no execution system and lands in IDLE. Pre-fix the machine
            // returned EXECUTING for every selection.
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveOnBallSnapshot(dt, frameNumber: 1, withTeammate: false,
                grounded: true, agentX: 20f);

            Assert.AreEqual(ActionType.HOLD, dt.LastAction.Type,
                "Scenario must isolate HOLD as the only candidate");
            Assert.AreEqual(DtState.IDLE, dt.State,
                "HOLD dispatch must land in IDLE (§3.7.2 row 3), not EXECUTING");
            Assert.AreEqual(1, mc.CallCount,
                "HOLD still routes its movement command (§3.5.5)");
        }

        // ── UT-34 (AR-2 H-3 lock): PASS holds EXECUTING between heartbeats ─────

        [Test]
        public void PassInFlight_NotReevaluated_UntilForcedRefresh()
        {
            // §3.7.2: once PASS is dispatched, the DT stays in EXECUTING and the
            // pipeline is NOT re-entered on the normal heartbeat cadence; §3.6.3: a
            // forced refresh re-evaluates but suppresses re-dispatch when the fresh
            // selection has the same ActionType. Pre-fix the full pipeline re-ran and
            // re-dispatched every heartbeat mid-windup.
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);   // null executors: PASS dispatch dropped, state flow unaffected

            ReceiveOnBallSnapshot(dt, frameNumber: 1, withTeammate: true,
                grounded: false, agentX: 52f);

            Assert.AreEqual(ActionType.PASS, dt.LastAction.Type,
                "Scenario must select PASS (clear forward lane dominates)");
            Assert.AreEqual(DtState.EXECUTING, dt.State);
            Assert.AreEqual(1, dt.LastAction.HeartbeatTick);

            // Normal heartbeat: pipeline must be skipped entirely.
            ReceiveOnBallSnapshot(dt, frameNumber: 2, withTeammate: true,
                grounded: false, agentX: 52f);
            Assert.AreEqual(DtState.EXECUTING, dt.State,
                "PASS in flight: normal heartbeat must not re-evaluate (§3.7.2)");
            Assert.AreEqual(1, dt.LastAction.HeartbeatTick,
                "LastAction must be untouched by a skipped heartbeat");

            // Forced refresh with unchanged conditions: re-evaluates, selects PASS
            // again → same-type re-dispatch suppression (§3.6.3).
            ReceiveOnBallSnapshot(dt, frameNumber: 3, withTeammate: true,
                grounded: false, agentX: 52f, forcedRefresh: true);
            Assert.AreEqual(DtState.EXECUTING, dt.State,
                "Same-ActionType forced refresh continues the in-flight execution (§3.6.3)");
            Assert.AreEqual(1, dt.LastAction.HeartbeatTick,
                "Same-type suppression must not replace the in-flight action record");

            // Completion signal returns the agent to IDLE (§3.7.2 row 4).
            dt.NotifyActionComplete();
            Assert.AreEqual(DtState.IDLE, dt.State);
        }

        // ── UT-35 (AR-2 H-3 lock): invalid snapshot preserves EXECUTING ────────

        [Test]
        public void InvalidSnapshot_DoesNotStompExecutingState()
        {
            RecordingController mc = new RecordingController();
            DecisionTree dt = new DecisionTree(0, mc, 0xABCDUL);

            ReceiveMinimalOffBallSnapshot(dt, agentId: 0, frameNumber: 1);
            Assert.AreEqual(DtState.EXECUTING, dt.State);

            // Malformed snapshot (wrong ObserverId) aborts the tick only.
            FilteredView badSnap = new FilteredView
            {
                ObserverId               = 99,
                FrameNumber              = 2,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleOpponents         = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };
            dt.ReceiveSnapshot(badSnap, BuildMatchContext(possessingAgentId: 15),
                TacticalContext.Stage0Default(new Vector2(50f, 34f)),
                DtAgentAttributes.CreateDefault(teamId: 0),
                BuildAgentState(new Vector2(52f, 34f)), pressureScalar: 0.0f);

            Assert.AreEqual(DtState.EXECUTING, dt.State,
                "A malformed snapshot must abort the tick, not destroy the in-flight EXECUTING record (AR-2 H-3)");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void ReceiveOnBallSnapshot(
            DecisionTree dt,
            int frameNumber,
            bool withTeammate,
            bool grounded,
            float agentX,
            bool forcedRefresh = false)
        {
            Vector2 agentPos = new Vector2(agentX, 34f);
            var teammates = withTeammate
                ? new[] { new PerceivedAgent
                    {
                        AgentId = 4,
                        PerceivedPosition = agentPos + new Vector2(8f, 0f),   // clear forward lane
                        PerceivedVelocity = Vector2.zero,
                        ConfidenceScore = 1.0f
                    } }
                : new PerceivedAgent[0];

            FilteredView snap = new FilteredView
            {
                ObserverId               = 0,
                FrameNumber              = frameNumber,
                ForcedRefreshThisTick    = forcedRefresh,
                BallVisible              = true,
                BallPerceivedPosition    = agentPos,
                BallStalenessFrames      = 0,
                VisibleTeammates         = teammates,
                VisibleTeammatesCount    = teammates.Length,
                VisibleOpponents         = new PerceivedAgent[0],
                VisibleOpponentsCount    = 0,
                BlindSidePerceivedAgents = new PerceivedAgent[0],
                BlindSidePerceivedAgentsCount = 0
            };

            MatchContext mc2 = new MatchContext
            {
                Possession         = PossessionState.HOME_TEAM,
                PossessingAgentId  = 0,                       // this agent has the ball
                Phase              = MatchPhase.OPEN_PLAY,
                BallPosition       = agentPos,
                BallZone           = PitchGeometry.ComputeFieldZone(agentPos.x)
            };

            AgentState state = BuildAgentState(agentPos);
            if (grounded) state.CurrentState = AgentMovementState.GROUNDED;

            dt.ReceiveSnapshot(snap, mc2,
                TacticalContext.Stage0Default(new Vector2(50f, 34f)),
                DtAgentAttributes.CreateDefault(teamId: 0),
                state, pressureScalar: 0.0f);
        }


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
// | 1.1     | 2026-06-11 | —      | Audit AR-2 H-3 locks: UT-33 HOLD→IDLE (§3.7.2 row 3); UT-34 PASS holds        |
// |         |            |        |   EXECUTING across heartbeats with §3.6.3 forced-refresh same-type            |
// |         |            |        |   suppression; UT-35 malformed snapshot preserves EXECUTING. New               |
// |         |            |        |   ReceiveOnBallSnapshot helper (possession-branch scenarios).                  |
#endregion
