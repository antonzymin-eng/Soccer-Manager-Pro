// File:     src/decision-tree/Tests/DispatcherTests.cs
// Created:  2026-05-29
// Modified: 2026-11-21 (W13-F5/F6/F7 / ERR-008-029: tracking-command dispatch tests replace the pace-tier tests)
// Modified (prior): 2026-11-12
// Author:   —
// Spec:     Decision Tree #8 §3.5, Code Standards #20
// Purpose:  Unit tests for ActionDispatcher movement routing. Verifies that HOLD,
//           DRIBBLE, MOVE (far/mid/near), PRESS, and INTERCEPT produce the correct
//           MovementCommand DesiredState and FacingMode per §3.5.4–3.5.8.
//           PASS and SHOOT are excluded: their PassExecutor/ShotExecutor instances
//           depend on physics-system seams unavailable in unit-test context (null is
//           passed; the dispatcher drops the request and logs the wiring failure).

using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(AgentMovementState.JOGGING, mc.LastCommand.DesiredState,
                "MOVE target 6–14m must dispatch MoveTo (JOGGING)");
        }

        // ── UT-20: MOVE near (< 6m) → WalkTo (WALKING) per §3.5.6 ─────────────

        [Test]
        public void Move_NearTarget_DispatchesWalkTo()
        {
            // AR-2 M-5 re-derivation: §3.5.6 near band is WALKING toward the slot
            // ("minor adjustment"). The pre-fix expectation (Stop/IDLE) encoded the
            // deviation — agents within 6 m never closed the residual offset.
            RecordingController mc  = new RecordingController();
            Vector2 agentPos        = new Vector2(52f, 34f);
            Vector2 target          = new Vector2(52.5f, 34f); // 0.5m — below 6m jog threshold
            DecisionContext ctx = BuildContext(agentPos: agentPos, ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, ActionType.MOVE_TO_POSITION, -1, target, default, default, 0.4f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(AgentMovementState.WALKING, mc.LastCommand.DesiredState,
                "MOVE target < 6m must dispatch WalkTo (WALKING toward slot, §3.5.6)");
            Assert.AreEqual(target, mc.LastCommand.TargetPosition,
                "Near-band MOVE must still target the slot, not the current position");
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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState,
                "PRESS must dispatch SPRINTING (§3.5.7)");
            Assert.AreEqual(pressTarget, mc.LastCommand.TargetPosition,
                "PRESS target must match action TargetPosition");
            Assert.AreEqual(DecelerationMode.EMERGENCY, mc.LastCommand.DecelerationMode,
                "PRESS must use EMERGENCY braking (§3.5.7 / UT-DP-04; AR-2 M-5)");
            Assert.AreEqual(FacingMode.TARGET_LOCK, mc.LastCommand.FacingMode,
                "PRESS must TARGET_LOCK on the press target (§3.5.7 / UT-DP-04; AR-2 M-5)");
            Assert.AreEqual(pressTarget, mc.LastCommand.FacingTarget,
                "PRESS facing target must be the pressed opponent (§3.5.7)");
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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState,
                "INTERCEPT must dispatch SPRINTING (§3.5.8)");
            Assert.AreEqual(DecelerationMode.CONTROLLED, mc.LastCommand.DecelerationMode,
                "INTERCEPT must use CONTROLLED braking — protects first-touch quality (§3.5.8)");
            Assert.AreEqual(FacingMode.TARGET_LOCK, mc.LastCommand.FacingMode,
                "INTERCEPT must TARGET_LOCK (§3.5.8; AR-2 M-5)");
            Assert.AreEqual(new Vector2(60f, 34f), mc.LastCommand.FacingTarget,
                "INTERCEPT must watch the BALL during the run, not the intercept point (§3.5.8)");
        }

        // ── FM-DT-14: unknown ActionType → HOLD-safe command (§3.5.9) ─────────

        [Test]
        public void UnknownActionType_DispatchesHoldSafeCommand()
        {
            RecordingController mc  = new RecordingController();
            DecisionContext ctx = BuildContext(
                agentPos: new Vector2(52f, 34f), ballPos: new Vector2(55f, 34f));
            AgentAction action = new AgentAction(
                0, (ActionType)99, -1, Vector2.zero, default, default, 0.1f, 1);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogAssert.Expect(LogType.Error, new Regex("FM-DT-14"));
#endif
            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(1, mc.CallCount,
                "FM-DT-14 must produce a HOLD-safe command, not silently drop the agent (§3.5.9)");
            Assert.AreEqual(FacingMode.TARGET_LOCK, mc.LastCommand.FacingMode,
                "FM-DT-14 HOLD-safe command watches the ball");
            Assert.AreEqual(new Vector2(52f, 34f), mc.LastCommand.TargetPosition,
                "FM-DT-14 HOLD-safe command holds at the agent's own position");
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

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.AreEqual(9, mc.LastAgentId,
                "Dispatcher must pass action.AgentId to SubmitCommand");
        }

        // ── W13-F5/F6/F7 (ERR-008-029): the mark-point MOVE is a tracking command ──

        /// <summary>The §3.5.6 distance tier for a non-marking MOVE: Walk below 6 m, Jog from 6 m, Sprint from 15 m (boundaries inclusive).</summary>
        [TestCase(0.0f, ActionDispatcher.MoveTier.Walk)]
        [TestCase(5.99f, ActionDispatcher.MoveTier.Walk)]
        [TestCase(6.0f, ActionDispatcher.MoveTier.Jog)]
        [TestCase(14.99f, ActionDispatcher.MoveTier.Jog)]
        [TestCase(15.0f, ActionDispatcher.MoveTier.Sprint)]
        public void SelectMoveTier_IsTheDistanceTier(float dist, ActionDispatcher.MoveTier expected)
        {
            Assert.AreEqual(expected, ActionDispatcher.SelectMoveTier(dist));
        }

        /// <summary>
        /// A mark-point MOVE is dispatched as one Agent Movement #2 §3.5.3.1 tracking command — to the action's target, with
        /// the context's mark-point velocity as feed-forward and the context's match time as the issue time — whatever the
        /// distance or the man's speed (no walk / jog band is selected here, so nothing can flip stride by stride).
        /// </summary>
        [TestCase(3.0f, 0.0f)]
        [TestCase(3.0f, 2.6f)]
        [TestCase(10.0f, 5.0f)]
        [TestCase(20.0f, 9.0f)]
        public void Move_MarkPoint_IsOneTrackingCommandWithTheMarkVelocity(float distance, float manSpeed)
        {
            RecordingController mc = new RecordingController();
            DecisionContext ctx = BuildContext(agentPos: new Vector2(50f, 34f), ballPos: new Vector2(55f, 34f));
            Vector2 point = new Vector2(50f + distance, 34f);
            ctx.TacticalContext.HasMarkTarget = true;
            ctx.TacticalContext.MarkTarget = point;
            ctx.TacticalContext.MarkTargetVelocity = new Vector2(manSpeed, 0.5f);
            ctx.MatchContext.MatchTimeSeconds = 123.4f;
            AgentAction action = new AgentAction(0, ActionType.MOVE_TO_POSITION, -1, point, default, default, 0.4f, 1);

            ActionDispatcher.Dispatch(action, in ctx, mc, null, null);

            Assert.IsTrue(mc.LastCommand.IsTracking);
            Assert.AreEqual(point, mc.LastCommand.TargetPosition);
            Assert.AreEqual(new Vector2(manSpeed, 0.5f), mc.LastCommand.TargetVelocity);
            Assert.AreEqual(123.4f, mc.LastCommand.IssueTimeS);
            Assert.AreEqual(AgentMovementState.SPRINTING, mc.LastCommand.DesiredState, "the band ceiling only; the tracking law sets the speed");
            Assert.AreEqual(DecelerationMode.CONTROLLED, mc.LastCommand.DecelerationMode);
            Assert.AreEqual(FacingMode.AUTO_ALIGN, mc.LastCommand.FacingMode);
        }

        /// <summary>Dispatcher-level off-identity: with HasMarkTarget false a stale MarkTargetVelocity changes nothing — the command is the non-tracking distance-tier command of a default context.</summary>
        [TestCase(3f, AgentMovementState.WALKING)]
        [TestCase(10f, AgentMovementState.JOGGING)]
        [TestCase(30f, AgentMovementState.SPRINTING)]
        public void Move_NoMarkTarget_IsUnaffectedByMarkTargetVelocity(float distance, AgentMovementState expected)
        {
            Vector2 target = new Vector2(50f + distance, 34f);
            AgentAction action = new AgentAction(0, ActionType.MOVE_TO_POSITION, -1, target, default, default, 0.4f, 1);

            RecordingController plain = new RecordingController();
            DecisionContext c0 = BuildContext(agentPos: new Vector2(50f, 34f), ballPos: new Vector2(55f, 34f));
            ActionDispatcher.Dispatch(action, in c0, plain, null, null);

            RecordingController stale = new RecordingController();
            DecisionContext c1 = BuildContext(agentPos: new Vector2(50f, 34f), ballPos: new Vector2(55f, 34f));
            c1.TacticalContext.MarkTargetVelocity = new Vector2(9.0f, 0.0f);   // HasMarkTarget stays false
            ActionDispatcher.Dispatch(action, in c1, stale, null, null);

            Assert.AreEqual(expected, stale.LastCommand.DesiredState);
            Assert.IsFalse(stale.LastCommand.IsTracking);
            Assert.AreEqual(plain.LastCommand.DesiredState, stale.LastCommand.DesiredState);
            Assert.AreEqual(plain.LastCommand.TargetPosition, stale.LastCommand.TargetPosition);
            Assert.AreEqual(plain.LastCommand.FacingMode, stale.LastCommand.FacingMode);
            Assert.AreEqual(plain.LastCommand.DecelerationMode, stale.LastCommand.DecelerationMode);
        }

        [Test]
        public void MarkTargetVelocity_DefaultsToZero()
        {
            Assert.AreEqual(Vector2.zero, TacticalContext.Stage0Default(new Vector2(50f, 34f)).MarkTargetVelocity);
            Assert.AreEqual(Vector2.zero, default(TacticalContext).MarkTargetVelocity);
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
// | 1.1     | 2026-06-11 | —      | Audit AR-2: Dispatch signature gains executor instances (H-1; null in unit    |
// |         |            |        |   context). M-5 re-derivations — UT-20 near-band MOVE expects WALKING toward  |
// |         |            |        |   the slot (was Stop/IDLE, encoding the deviation); UT-21 asserts the full     |
// |         |            |        |   §3.5.7 PRESS profile (EMERGENCY + TARGET_LOCK); UT-22 asserts the §3.5.8     |
// |         |            |        |   INTERCEPT profile (CONTROLLED + ball-watching TARGET_LOCK). New FM-DT-14     |
// |         |            |        |   unknown-type HOLD-safe lock (§3.5.9).                                        |
// | 1.2     | 2026-11-12 | —      | W13.1c (ERR-008-028): mark-MOVE pace tier locks — boundary table at JogEnter / SprintEnter, no-mark-target     |
// |         |            |        |   identity at tier and dispatcher level, dispatcher paces a near mark point to the man's speed.                  |
// | 1.3     | 2026-11-12 | —      | ERR-008-028 amendment: speed tier only while the marker is slower than his man (strict; equal speeds fall back) — tier table, MoveTier enum, dispatcher brake case; new block moved above the Helpers banner (review L7). |
// | 1.4     | 2026-11-21 | —      | W13-F5/F6/F7 (ERR-008-029): the pace-tier block replaced — SelectMoveTier is distance-only; a mark-point MOVE is one #2 §3.5.3.1 tracking command (target, MarkTargetVelocity, match time); off-identity with a stale MarkTargetVelocity. |
#endregion
