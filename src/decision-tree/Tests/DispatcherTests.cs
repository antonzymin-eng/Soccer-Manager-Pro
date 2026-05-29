// File:     src/decision-tree/Tests/DispatcherTests.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.5, Code Standards #20
// Purpose:  Unit tests for ActionDispatcher movement routing. Verifies that HOLD,
//           DRIBBLE, MOVE (far/mid/near), PRESS, and INTERCEPT produce the correct
//           MovementCommand DesiredState and FacingMode per §3.5.4–3.5.8.
//           PASS and SHOOT are excluded: they call static PassExecutor/ShotExecutor
//           which depend on physics systems unavailable in unit-test context.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class DispatcherTests
    {
        // ── Test movement controller stub ─────────────────────────────────────

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

        // ── UT-16: HOLD → StrafeWhileWatching (TARGET_LOCK, JOGGING) ─────────

        [Test]
        public void Hold_DispatchesStrafeWhileWatching()
        {
            RecordingController mc  = new RecordingController();
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.HOLD, -1, Vector2.zero, default, default, 0.3f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(1, mc.CallCount, "Dispatcher must call SubmitCommand exactly once for HOLD");
            Assert.AreEqual(FacingMode.TARGET_LOCK, mc.LastCommand.FacingMode,
                "HOLD must use TARGET_LOCK facing (toward ball)");
            Assert.AreEqual(AgentMovementState.JOGGING, mc.LastCommand.DesiredState,
                "HOLD must request JOGGING state (strafe)");
        }

        // ── UT-17: DRIBBLE → MoveTo (JOGGING, AUTO_ALIGN) ────────────────────

        [Test]
        public void Dribble_DispatchesMoveTo()
        {
            RecordingController mc  = new RecordingController();
            Vector2 dribbleTarget   = new Vector2(55f, 36f);
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(52f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.DRIBBLE, -1, dribbleTarget, default, default, 0.5f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.JOGGING, mc.LastCommand.DesiredState,
                "DRIBBLE must request JOGGING state (MoveTo)");
            Assert.AreEqual(FacingMode.AUTO_ALIGN, mc.LastCommand.FacingMode,
                "DRIBBLE must use AUTO_ALIGN facing");
        }

        // ── UT-18: MOVE far (≥ 15m) → SprintUrgent ───────────────────────────

        [Test]
        public void Move_FarTarget_DispatchesSprintUrgent()
        {
            RecordingController mc  = new RecordingController();
            Vector2 agentPos        = new Vector2(10f, 34f);
            Vector2 target          = new Vector2(40f, 34f);   // 30m — above 15m sprint threshold
            DecisionContext ctx = BuildContext(agentPos: agentPos, ballPos: new Vector2(20f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.MOVE_TO_POSITION, -1, target, default, default, 0.4f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState,
                "MOVE target ≥ 15m must dispatch SprintUrgent (SPRINTING)");
        }

        // ── UT-19: MOVE mid (6–14m) → MoveTo (JOGGING) ───────────────────────

        [Test]
        public void Move_MidTarget_DispatchesMoveTo()
        {
            RecordingController mc  = new RecordingController();
            Vector2 agentPos        = new Vector2(50f, 34f);
            Vector2 target          = new Vector2(60f, 34f);   // 10m — between 6m jog and 15m sprint thresholds
            DecisionContext ctx = BuildContext(agentPos: agentPos, ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.MOVE_TO_POSITION, -1, target, default, default, 0.4f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.JOGGING, mc.LastCommand.DesiredState,
                "MOVE target 6–14m must dispatch MoveTo (JOGGING)");
        }

        // ── UT-20: MOVE near (< 6m) → Stop (IDLE) ────────────────────────────

        [Test]
        public void Move_NearTarget_DispatchesStop()
        {
            RecordingController mc  = new RecordingController();
            Vector2 agentPos        = new Vector2(52f, 34f);
            Vector2 target          = new Vector2(52.5f, 34f); // 0.5m — below 6m jog threshold
            DecisionContext ctx = BuildContext(agentPos: agentPos, ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.MOVE_TO_POSITION, -1, target, default, default, 0.4f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.IDLE, mc.LastCommand.DesiredState,
                "MOVE target < 6m must dispatch Stop (IDLE) — agent is already on slot");
        }

        // ── UT-21: PRESS → SprintUrgent (SPRINTING) ──────────────────────────

        [Test]
        public void Press_DispatchesSprintUrgent()
        {
            RecordingController mc  = new RecordingController();
            Vector2 pressTarget     = new Vector2(55f, 34f);
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.PRESS, 15, pressTarget, default, default, 0.5f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState,
                "PRESS must dispatch SprintUrgent (SPRINTING)");
            Assert.AreEqual(pressTarget, mc.LastCommand.TargetPosition,
                "PRESS SprintUrgent target must match action TargetPosition");
        }

        // ── UT-22: INTERCEPT → SprintUrgent (SPRINTING) ──────────────────────

        [Test]
        public void Intercept_DispatchesSprintUrgent()
        {
            RecordingController mc     = new RecordingController();
            Vector2 interceptPoint     = new Vector2(58f, 35f);
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(60f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.INTERCEPT, -1, interceptPoint, default, default, 0.5f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState,
                "INTERCEPT must dispatch SprintUrgent (SPRINTING)");
        }

        // ── UT-23: Dispatcher routes correct AgentId ──────────────────────────

        [Test]
        public void Dispatch_PassesCorrectAgentIdToController()
        {
            RecordingController mc  = new RecordingController();
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                9, ActionType.HOLD, -1, Vector2.zero, default, default, 0.3f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc);

            Assert.AreEqual(9, mc.LastAgentId,
                "Dispatcher must pass action.AgentId to SubmitCommand");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DecisionContext BuildContext(Vector2 agentPos, Vector2 ballPos)
        {
            var mc = new MatchContext
            {
                Phase              = MatchPhase.OPEN_PLAY,
                BallZone           = FieldZone.MIDFIELD,
                Possession         = PossessionState.HOME_TEAM,
                PossessingAgentId  = 0,
                BallPosition       = ballPos
            };
            var tc   = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            var snap = new FilteredView
            {
                ObserverId               = 0,
                FrameNumber              = 1,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleOpponents         = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId              = 0,
                AgentTeamId          = 0,
                CurrentFrame         = 1,
                AgentHasBall         = true,
                PossessedByTeam      = PossessionState.HOME_TEAM,
                AgentPosition        = agentPos,
                AgentFacingDirection  = Vector2.right,
                AgentState           = default,
                A_Vision             = 0.5f,
                A_Passing            = 0.5f,
                A_Finishing          = 0.5f,
                A_Dribbling          = 0.5f,
                A_LongShots          = 0.5f,
                A_Composure          = 0.5f,
                A_Decisions          = 0.5f,
                A_Anticipation       = 0.5f,
                A_Pace               = 0.5f,
                A_Agility            = 0.5f,
                A_WorkRate           = 0.5f,
                A_Stamina            = 0.5f,
                A_Aggression         = 0.5f,
                A_Positioning        = 0.5f,
                A_Crossing           = 0.5f,
                MatchContext         = mc,
                TacticalContext      = tc,
                PressureScalar       = 0.0f,
                MatchSeed            = 0xCAFEBABEUL,
                Snapshot             = snap,
                OpponentGoalCentre   = new Vector2(105f, 34f),
                OpponentGoalPostL    = new Vector2(105f, 30.34f),
                OpponentGoalPostR    = new Vector2(105f, 37.66f)
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
