// File:     src/goalkeeper-mechanics/Tests/GoalkeeperRushTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1.1 / §3.7 (ERR-011-009), FR-GK-018, KD-15,
//           Testing Strategy #19, Code Standards #20
// Purpose:  Orchestrator-level locks for the rush chain, which had no production caller at all until
//           wiring backlog W1 (docs/tracking/gk-rush-trigger-design.md) — so every behaviour below was
//           reachable only from a unit test and none of it had ever run in a match.
//
//           The load-bearing one is RushReachingLockedTarget_EndsTheRush: §3.1.1 gave Rushing exactly
//           three exits (contact, the 1v1 radius, F-08 interception) and for a LOOSE ball none of them
//           can fire — CheckAttackerWithinRadius returns false outright without a possessor, and F-08
//           needs one — so a keeper who swept a loose ball stood over it in Rushing for the rest of the
//           match. Both keepers are exercised: three home/away asymmetry defects have shipped in this
//           project because every fixture used the home team (#8 ERR-008-002).

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    [TestFixture]
    internal class GoalkeeperRushTests
    {
        private const int Gk0 = 0;
        private const int Gk1 = 1;
        private const int AgentCount = 22;

        private static readonly int[] GkAgentIds = { Gk0, Gk1 };

        /// <summary>The Reached completion publishes a GoalkeeperRushEvent, so the #11 ordinals must be
        /// registered and the bus reset per test. <c>Initialize</c> is idempotent (FR-EVT-020); the
        /// Physics phase declaration reproduces the production context (#11's Update runs inside
        /// MatchEngine.RunPhysicsPhase → DriveGkHeadingPhysics).</summary>
        [SetUp]
        public void SetUp()
        {
            EventBusRegistrar.Initialize();
            EventBus.ResetForNewMatch();
            EventBus.BeginPhase(PhaseId.Physics);
        }

        [TearDown]
        public void TearDown() => EventBus.ResetForNewMatch();

        // ── The rush actually launches and moves the keeper ──────────────────────────────

        [TestCase(Gk0)]
        [TestCase(Gk1)]
        public void CommittedRush_LaunchesAndMovesTheKeeperOffHisLine(int gkIndex)
        {
            var gk = new GoalkeeperMechanics(new LooseBallSystem(), new ZeroRng());
            AgentState[] agents = MakeAgents();
            BallState ball = MakeRestingBall(gkIndex);
            float startX = agents[gkIndex].Position.x;

            DriveToAnticipate(gk, agents, ball, gkIndex);
            gk.CommitRushIntent(gkIndex, RushIntentTo(BallXY(ball), tick: 4), Attrs());

            Assert.IsTrue(gk.HasActiveRushIntent(gkIndex), "the commit must arm #11's own latch");

            DriveTicks(gk, agents, ball, firstTick: 5, tickCount: 1);

            Assert.AreEqual(GoalkeeperState.Rushing, gk.GetState(gkIndex),
                "Anticipate + a RushIntent above RushCommitThreshold must enter Rushing");

            DriveTicks(gk, agents, ball, firstTick: 6, tickCount: 2);

            Assert.AreNotEqual(startX, agents[gkIndex].Position.x,
                "UpdateRushFrame must write the advanced position back into the AM #2 array");
            Assert.Less(
                Mathf.Abs(agents[gkIndex].Position.x - ball.Position.x),
                Mathf.Abs(startX - ball.Position.x),
                "the keeper must be closing on the locked target, not drifting");
        }

        // ── ERR-011-009: a rush that ARRIVES ends ────────────────────────────────────────

        [TestCase(Gk0)]
        [TestCase(Gk1)]
        public void RushReachingLockedTarget_EndsTheRush(int gkIndex)
        {
            var gk = new GoalkeeperMechanics(new LooseBallSystem(), new ZeroRng());
            AgentState[] agents = MakeAgents();
            BallState ball = MakeRestingBall(gkIndex);

            DriveToAnticipate(gk, agents, ball, gkIndex);
            gk.CommitRushIntent(gkIndex, RushIntentTo(BallXY(ball), tick: 4), Attrs());

            // Generous budget: ~10 m at ~5.4 m/s is under 2 s, and the ball never moves, so a run that
            // has not finished inside 60 tactical ticks (6 s) has not finished at all.
            DriveTicks(gk, agents, ball, firstTick: 5, tickCount: 60);

            Assert.AreNotEqual(GoalkeeperState.Rushing, gk.GetState(gkIndex),
                "ERR-011-009: a keeper who swept a loose ball had no exit from Rushing and stood over " +
                "it for the rest of the match");
            Assert.AreNotEqual(GoalkeeperState.OneOnOne, gk.GetState(gkIndex),
                "ERR-011-009: OneOnOne inherits the same strand");
            Assert.IsFalse(gk.HasActiveRushIntent(gkIndex),
                "a resolved rush chain must release the latch, or no later rush can ever commit");
        }

        // ── FR-GK-018 / KD-15: the disarm, and its mid-rush no-op ────────────────────────

        [Test]
        public void ClearRushIntent_BeforeLaunch_DisarmsSoTheKeeperNeverSetsOff()
        {
            var gk = new GoalkeeperMechanics(new LooseBallSystem(), new ZeroRng());
            AgentState[] agents = MakeAgents();
            BallState ball = MakeRestingBall(Gk0);

            DriveToAnticipate(gk, agents, ball, Gk0);
            gk.CommitRushIntent(Gk0, RushIntentTo(BallXY(ball), tick: 4), Attrs());
            gk.ClearRushIntent(Gk0);

            Assert.IsFalse(gk.HasActiveRushIntent(Gk0), "the disarm must clear the latch before launch");

            DriveTicks(gk, agents, ball, firstTick: 5, tickCount: 5);

            Assert.AreNotEqual(GoalkeeperState.Rushing, gk.GetState(Gk0),
                "a disarmed intent must not fire at a later Anticipate — that is a sprint at nothing, " +
                "the ERR-011-002 dive-at-nothing one subsystem across");
        }

        [Test]
        public void ClearRushIntent_MidRush_IsANoOp()
        {
            var gk = new GoalkeeperMechanics(new LooseBallSystem(), new ZeroRng());
            AgentState[] agents = MakeAgents();
            BallState ball = MakeRestingBall(Gk0);

            DriveToAnticipate(gk, agents, ball, Gk0);
            gk.CommitRushIntent(Gk0, RushIntentTo(FarTarget(Gk0), tick: 4), Attrs());
            DriveTicks(gk, agents, ball, firstTick: 5, tickCount: 1);

            Assert.AreEqual(GoalkeeperState.Rushing, gk.GetState(Gk0), "fixture must be mid-rush");

            gk.ClearRushIntent(Gk0);

            Assert.IsTrue(gk.HasActiveRushIntent(Gk0),
                "FR-GK-018 / KD-15: a committed rush is not abortable on anything but F-08, so the " +
                "disarm must be inert once the keeper has set off");
        }

        // ── §3.7.0 the commit distance is the KEEPER's decision (ERR-011-010) ────────────

        /// <summary>The whole point of §3.7.0: when a keeper comes out is governed by his attributes,
        /// not by a fixed range. A keeper who is good at one-on-ones comes out from further.</summary>
        [Test]
        public void RushCommitDistance_RisesWithOneVsOne()
        {
            float timid = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 1f, composure: 10f, fatigue: 0f));
            float bold = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 20f, composure: 10f, fatigue: 0f));

            Assert.Greater(bold, timid,
                "OneVsOne is the attribute that names this decision — it must move the commit distance");
        }

        /// <summary>Leaving the goal empty is a nerve decision as much as a technical one.</summary>
        [Test]
        public void RushCommitDistance_RisesWithComposure()
        {
            float nervy = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 10f, composure: 1f, fatigue: 0f));
            float calm = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 10f, composure: 20f, fatigue: 0f));

            Assert.Greater(calm, nervy, "a composed keeper backs himself to come further");
        }

        /// <summary>0 = rested, 1 = spent (the project-wide convention — this has been found inverted
        /// before). A tired keeper backs off rather than gambling on a sprint he cannot finish.</summary>
        [Test]
        public void RushCommitDistance_FallsWithFatigue()
        {
            float fresh = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 14f, composure: 14f, fatigue: 0f));
            float spent = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 14f, composure: 14f, fatigue: 1f));

            Assert.Less(spent, fresh, "fatigue must REDUCE the distance (0 = rested, 1 = fatigued)");
        }

        [Test]
        public void RushCommitDistance_IsClampedBothWays()
        {
            float floor = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 1f, composure: 1f, fatigue: 1f));
            float ceiling = GoalkeeperRushDispatch.ComputeRushCommitDistanceM(
                AttrsWith(oneVsOne: 20f, composure: 20f, fatigue: 0f));

            Assert.GreaterOrEqual(floor, GoalkeeperConstants.RushCommitMinDistanceM,
                "even the most reluctant keeper comes for a ball on his six-yard line");
            Assert.LessOrEqual(ceiling, GoalkeeperConstants.RushCommitMaxDistanceM,
                "no keeper leaves his goal further than the ceiling, whatever his attributes");
        }

        // ── Bounds ───────────────────────────────────────────────────────────────────────

        [Test]
        public void Accessors_OutOfRangeIndex_AnswerTheMachineDefault()
        {
            var gk = new GoalkeeperMechanics(new LooseBallSystem(), new ZeroRng());

            Assert.AreEqual(GoalkeeperState.Resting, gk.GetState(-1));
            Assert.AreEqual(GoalkeeperState.Resting, gk.GetState(GoalkeeperConstants.MaxGkAgents));
            Assert.IsFalse(gk.HasActiveRushIntent(-1));
            Assert.IsFalse(gk.HasActiveRushIntent(GoalkeeperConstants.MaxGkAgents));
        }

        // ── Fixture ──────────────────────────────────────────────────────────────────────

        /// <summary>Ticks the keeper into Anticipate: Resting → Set on the ball entering the third in
        /// front of the goal he defends, then Set → Anticipate on the Stage-0 anticipation score.</summary>
        private static void DriveToAnticipate(
            GoalkeeperMechanics gk, AgentState[] agents, BallState ball, int gkIndex)
        {
            DriveTicks(gk, agents, ball, firstTick: 0, tickCount: 4);
            Assert.AreEqual(GoalkeeperState.Anticipate, gk.GetState(gkIndex),
                "fixture must reach Anticipate before a rush can be evaluated");
        }

        /// <summary>One tactical tick plus its six physics frames, the production cadence.</summary>
        private static void DriveTicks(
            GoalkeeperMechanics gk, AgentState[] agents, BallState ball, int firstTick, int tickCount)
        {
            float frameMs = GoalkeeperConstants.FrameMs;
            int framesPerTick = GoalkeeperConstants.FramesPerTacticalTick;

            for (int t = firstTick; t < firstTick + tickCount; t++)
            {
                gk.TacticalTick(t, agents, ball, GkAgentIds);
                for (int f = 0; f < framesPerTick; f++)
                {
                    int frame = t * framesPerTick + f;
                    gk.Update(frame, frame * frameMs, agents, ball, GkAgentIds);
                }
            }
        }

        private static AgentState[] MakeAgents()
        {
            var agents = new AgentState[AgentCount];
            agents[Gk0] = AgentState.CreateAtPosition(new Vector2(1f, 34f), Vector2.right);
            agents[Gk1] = AgentState.CreateAtPosition(
                new Vector2(GoalkeeperConstants.PitchLengthM - 1f, 34f), Vector2.left);
            return agents;
        }

        /// <summary>A stationary loose ball ~10 m in front of the goal <paramref name="gkIndex"/>
        /// defends — inside the defensive third, so the keeper wakes, and never moving, so the run has
        /// a fixed length. Stationary is the case that matters: the 1v1 and smother triggers are dead
        /// without a possessor, which is exactly the ERR-011-009 strand.</summary>
        private static BallState MakeRestingBall(int gkIndex)
        {
            float x = gkIndex == 0 ? 11f : GoalkeeperConstants.PitchLengthM - 11f;
            return BallState.CreateAtPosition(new Vector3(x, 34f, 0.11f));
        }

        private static Vector3 BallXY(BallState ball) =>
            new Vector3(ball.Position.x, ball.Position.y, 0f);

        /// <summary>A target far enough from the keeper that a single tick cannot reach it, so the
        /// mid-rush no-op is asserted while the run is genuinely still in flight.</summary>
        private static Vector3 FarTarget(int gkIndex) =>
            gkIndex == 0
                ? new Vector3(20f, 34f, 0f)
                : new Vector3(GoalkeeperConstants.PitchLengthM - 20f, 34f, 0f);

        private static RushIntent RushIntentTo(Vector3 target, int tick) => new RushIntent
        {
            RushTarget = target,
            // Above RushCommitThreshold (0.60) or the state machine ignores the intent entirely.
            CommitmentLevel = 0.85f,
            AttemptCommittedTick = tick
        };

        private static GoalkeeperAgentAttributes AttrsWith(float oneVsOne, float composure, float fatigue)
        {
            GoalkeeperAgentAttributes a = Attrs();
            a.OneVsOne = oneVsOne;
            a.Composure = composure;
            a.Fatigue = fatigue;
            return a;
        }

        private static GoalkeeperAgentAttributes Attrs() => new GoalkeeperAgentAttributes
        {
            Reflexes = 12f,
            Handling = 12f,
            Composure = 12f,
            Strength = 12f,
            Aerial = 12f,
            Balance = 12f,
            OneVsOne = 12f,
            Pace = 12f,
            Throwing = 12f,
            Kicking = 12f,
            Fatigue = 0.0f,
            TeamId = 0
        };

        /// <summary>No possessor — the loose-ball case. Returning −1 is what disables the 1v1 and
        /// smother triggers, and is therefore the whole point of the fixture.</summary>
        private sealed class LooseBallSystem : IGoalkeeperBallSystem
        {
            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs) { }

            public void SetPossessor(int agentId) { }

            public void ParkBall() { }

            public int GetBallPossessorId() => -1;
        }

        private sealed class ZeroRng : IGoalkeeperRngService
        {
            public float NextFloat(int drawSiteId, uint domainTag) => 0.5f;
            public float NextGaussian(int drawSiteId, uint domainTag) => 0.0f;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-04 | —      | Initial. Wiring backlog W1 / ERR-011-009: the rush launches and    |
// |         |            |        | moves the keeper (both keepers); a rush that REACHES its locked    |
// |         |            |        | target ends instead of stranding; ClearRushIntent disarms before   |
// |         |            |        | launch and is inert mid-rush (FR-GK-018); accessor bounds.         |
#endregion
