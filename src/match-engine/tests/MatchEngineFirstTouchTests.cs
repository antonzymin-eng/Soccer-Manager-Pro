// File:     src/match-engine/tests/MatchEngineFirstTouchTests.cs
// Created:  2026-06-22
// Modified: 2026-07-16 (AR-8 M-1: sent-off agents cannot receive — regression lock)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase D (D3), Code Standards #20
// Purpose:  Phase D D3 tests — the Resolve-phase first-touch trigger. A loose, ground-level, moving ball
//           arriving at an approaching agent is received (CONTROLLED → possession); a receding / high /
//           possessed ball is not touched; away-team agents receive identically (first-touch is
//           frame-agnostic); and a scripted receive is deterministic across two same-seed runs.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase D step D3 first-touch tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineFirstTouchTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // A home outfielder (roster 0 is the home GK) and an away outfielder (roster 11 is the away GK).
        private const int HomeAgent = 1;
        private const int AwayAgent = 12;

        // A clear patch of pitch away from both kickoff lines (home x≈26.25, away x≈78.75), so the only
        // agent within the ~1 m acceptance reach of the test ball is the one we place there.
        private static readonly Vector2 OpenSpot = new Vector2(52.5f, 20.0f);

        /// <summary>Places <paramref name="agent"/> at <paramref name="pos"/>, holding (Stop) so the
        /// Physics phase leaves it put for the single test tick (tick 1 — no AI stride runs yet).</summary>
        private static void ParkAgentAt(MatchEngine engine, int agent, Vector2 pos)
        {
            engine.TestOnly_SetAgent(agent, AgentState.CreateAtPosition(pos, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(agent, MovementCommand.Stop(pos));
        }

        /// <summary>A rolling ground ball at <paramref name="pos"/> with planar velocity
        /// <paramref name="vel"/>.</summary>
        private static BallState RollingBall(Vector2 pos, Vector2 vel)
        {
            BallState ball = BallState.CreateAtPosition(
                new Vector3(pos.x, pos.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
            ball.Velocity = new Vector3(vel.x, vel.y, 0f);
            ball.State    = BallStateType.Rolling;
            return ball;
        }

        // ── CONTROLLED receive ────────────────────────────────────────────────────────

        [Test]
        public void LooseBall_ArrivingAtAgent_GainsPossession()
        {
            var engine = new MatchEngine(MatchSeed); // kickoff loose

            ParkAgentAt(engine, HomeAgent, OpenSpot);
            // Slow ball 0.6 m short of the agent, rolling toward it (+X). A 2 m/s ball with neutral
            // attributes and a standing, unpressured receiver yields q≈1 → r≈0.10 m → CONTROLLED.
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f)));

            engine.RunTick();

            Assert.AreEqual(HomeAgent, engine.TestOnly_PossessingAgentId,
                "A controlled first touch must hand possession to the receiving agent.");
            Assert.AreEqual(HomeAgent, engine.TestOnly_MatchContext.PossessingAgentId,
                "MatchContext (authored after first touch) must reflect the gained possession.");
        }

        [Test]
        public void LooseBall_ArrivingAtAwayAgent_GainsPossession()
        {
            // First touch works in world space directly (no home/away frame mapping — unlike Positioning
            // AI), so an away agent receives identically. Closes the away-team symmetry leg of D3.
            var engine = new MatchEngine(MatchSeed);

            ParkAgentAt(engine, AwayAgent, OpenSpot);
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f)));

            engine.RunTick();

            Assert.AreEqual(AwayAgent, engine.TestOnly_PossessingAgentId,
                "An away agent must receive a loose ball identically to a home agent.");
        }

        // ── Non-trigger gates ─────────────────────────────────────────────────────────

        [Test]
        public void RecedingBall_NotTouched()
        {
            // Ball within reach but moving AWAY from the agent (+X past it): the closing-direction gate
            // excludes it — this is the same gate that stops a kicker re-touching the ball it just played.
            var engine = new MatchEngine(MatchSeed);

            ParkAgentAt(engine, HomeAgent, OpenSpot);
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x + 0.6f, OpenSpot.y), new Vector2(2f, 0f)));

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "A ball receding from the agent must not be received.");
        }

        [Test]
        public void HighBall_NotTouched()
        {
            // Ball centre well above the ground-control height → a Heading (#10) event, not first touch.
            var engine = new MatchEngine(MatchSeed);

            ParkAgentAt(engine, HomeAgent, OpenSpot);
            BallState high = RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f));
            high.Position = new Vector3(high.Position.x, high.Position.y, 1.0f); // height ≈ 0.89 m > 0.50 m
            high.State    = BallStateType.Airborne;
            engine.TestOnly_SetBall(high);

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "A ball above ground-control height must not be first-touched.");
        }

        [Test]
        public void PossessedBall_FirstTouchSkipped()
        {
            // The ball is already possessed by another agent: first touch only acts on a LOOSE ball, so a
            // bystander next to a moving ball does not steal it through the D3 path.
            var engine = new MatchEngine(MatchSeed);

            engine.TestOnly_SetPossession(HomeAgent);
            ParkAgentAt(engine, AwayAgent, OpenSpot);
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f)));

            engine.RunTick();

            Assert.AreEqual(HomeAgent, engine.TestOnly_PossessingAgentId,
                "A possessed ball must not be re-acquired by a bystander via first touch.");
        }

        [Test]
        public void SentOffAgent_CannotReceive_BallStaysLoose()
        {
            // AR-8 M-1 regression lock: the receiver scan must exclude sent-off agents — every other
            // participation surface (AI dispatch, snapshot IsActive fills, physics forced-stop,
            // offside line) already did, but pre-fix the gate-4 loop let a frozen red-carded agent
            // receive the ball into possession it could never release (no AI dispatch ⇒ no kick),
            // deadlocking play. Identical geometry to LooseBall_ArrivingAtAgent_GainsPossession
            // (which proves this setup DOES receive when the agent is active).
            var engine = new MatchEngine(MatchSeed);

            ParkAgentAt(engine, HomeAgent, OpenSpot);
            engine.TestOnly_SetIsSentOff(HomeAgent, true);
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f)));

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "A sent-off agent must not receive the ball — it stays loose and rolls on.");
        }

        // ── Determinism ───────────────────────────────────────────────────────────────

        [Test]
        public void FirstTouch_IsDeterministicAcrossTwoRuns()
        {
            byte[] digestA = RunScriptedReceive(out int possessorA);
            byte[] digestB = RunScriptedReceive(out int possessorB);

            Assert.AreEqual(HomeAgent, possessorA, "The scripted receive must fire (CONTROLLED).");
            Assert.AreEqual(possessorA, possessorB, "Both same-seed runs must reach the same possession.");
            CollectionAssert.AreEqual(digestA, digestB,
                "A first touch must leave the snapshot digest identical across two same-seed runs.");
        }

        private static byte[] RunScriptedReceive(out int possessor)
        {
            var engine = new MatchEngine(MatchSeed);
            ParkAgentAt(engine, HomeAgent, OpenSpot);
            engine.TestOnly_SetBall(RollingBall(new Vector2(OpenSpot.x - 0.6f, OpenSpot.y), new Vector2(2f, 0f)));
            engine.RunTick();
            possessor = engine.TestOnly_PossessingAgentId;
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-22 | —      | Phase D D3: Resolve-phase first-touch trigger tests — CONTROLLED |
// |         |            |        | receive gains possession (home + away), receding / high /       |
// |         |            |        | possessed balls are not touched, and a scripted receive is      |
// |         |            |        | deterministic across two same-seed runs.                        |
// | 1.1     | 2026-07-16 | —      | AR-8 M-1 lock: a sent-off agent in the exact receiving geometry |
// |         |            |        | of the CONTROLLED-receive test does NOT gain possession — the   |
// |         |            |        | ball stays loose (pre-fix it deadlocked on the frozen agent).   |
#endregion
