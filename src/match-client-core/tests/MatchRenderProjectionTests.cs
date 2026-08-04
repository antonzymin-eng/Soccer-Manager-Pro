// File:     src/match-client-core/tests/MatchRenderProjectionTests.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the frame → draw-state projection: which source each field comes from, the
//           possession ring, the ball's world placement and shadow, and every shape guard.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchRenderProjectionTests
    {
        private const int Roster = 4;
        private const float Tolerance = 1e-4f;

        private static MatchRoster TwoPerTeam() => new MatchRoster(new[] { 0, 0, 1, 1 });

        private static LiveMatchFrame Frame(
            Vector2[] positions = null,
            LiveAgentCue[] cues = null,
            int possessing = -1,
            Vector3 ball = default)
        {
            return new LiveMatchFrame(
                tick: 120,
                ballPosition: ball,
                possessingAgentId: possessing,
                agentPositions: positions ?? new Vector2[Roster],
                agentCues: cues ?? new LiveAgentCue[Roster],
                substitutionsUsed: new int[MatchEngineConstants.TEAM_COUNT],
                score: new Scoreline(0, 0),
                matchEnded: false,
                period: MatchPeriod.FirstHalf,
                restart: RestartBanner.None);
        }

        // ── Agents ──────────────────────────────────────────────────────────────────────

        [Test]
        public void PositionsComeFromTheInterpolatedBuffer_NotTheFrame()
        {
            // The whole reason the method takes two position sources: the frame's positions are the
            // last captured tick, the buffer's are what is actually being drawn this display frame.
            // If projection silently read the frame, motion would step at 60 Hz and P3 would be dead
            // code — which is exactly the "the composition runs but does nothing" shape this repo
            // has paid for once already.
            var framePositions = new Vector2[Roster];
            framePositions[0] = new Vector2(10f, 10f);

            var drawn = new Vector2[Roster];
            drawn[0] = new Vector2(30f, 20f);

            LiveMatchFrame frame = Frame(positions: framePositions);
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(drawn, in frame, TwoPerTeam(), models);

            Vector3 expected = PitchViewProjection.ToWorld(new Vector2(30f, 20f), 0f);
            Assert.AreEqual(expected.x, models[0].WorldPosition.x, Tolerance);
            Assert.AreEqual(expected.z, models[0].WorldPosition.z, Tolerance);
            Assert.AreEqual(0f, models[0].WorldPosition.y, Tolerance, "agents stay on the ground plane");
        }

        [Test]
        public void CuesComeFromTheFrame_AndLandOnTheRightSlot()
        {
            var cues = new LiveAgentCue[Roster];
            cues[1] = new LiveAgentCue(1, false, -1, false);  // booked outfielder, original starter
            cues[2] = new LiveAgentCue(0, true, 3, false);    // sent-off substitute
            cues[3] = new LiveAgentCue(0, false, -1, true);   // the away keeper

            LiveMatchFrame frame = Frame(cues: cues);
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

            Assert.AreEqual(1, models[1].YellowCards);
            Assert.IsFalse(models[1].IsSubstitute);
            Assert.IsFalse(models[1].IsGoalkeeper);

            Assert.IsTrue(models[2].IsSentOff);
            Assert.IsTrue(models[2].IsSubstitute);

            Assert.IsTrue(models[3].IsGoalkeeper);
            Assert.AreEqual(0, models[3].YellowCards);
        }

        [Test]
        public void TheGoalkeeperFlagIsLive_SoASubstitutionMovesIt()
        {
            // The defect this cue was added for: a keeper substituted for an outfield player moves
            // which slot is the goalkeeper. Frame 1 has the keeper on slot 0, frame 2 on slot 1.
            var models = new AgentRenderModel[Roster];
            MatchRoster roster = TwoPerTeam();

            var before = new LiveAgentCue[Roster];
            before[0] = new LiveAgentCue(0, false, -1, true);
            LiveMatchFrame frameBefore = Frame(cues: before);

            var after = new LiveAgentCue[Roster];
            after[0] = new LiveAgentCue(0, false, 2, false);
            after[1] = new LiveAgentCue(0, false, -1, true);
            LiveMatchFrame frameAfter = Frame(cues: after);

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frameBefore, roster, models);
            Assert.IsTrue(models[0].IsGoalkeeper);
            Assert.IsFalse(models[1].IsGoalkeeper);

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frameAfter, roster, models);
            Assert.IsFalse(models[0].IsGoalkeeper, "the keeper flag must follow the substitution");
            Assert.IsTrue(models[1].IsGoalkeeper);
        }

        [Test]
        public void TeamAndShirtNumberComeFromTheRoster()
        {
            LiveMatchFrame frame = Frame();
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

            Assert.AreEqual(new[] { 0, 0, 1, 1 }, new[] { models[0].TeamId, models[1].TeamId, models[2].TeamId, models[3].TeamId });
            Assert.AreEqual(new[] { 1, 2, 1, 2 }, new[] { models[0].ShirtNumber, models[1].ShirtNumber, models[2].ShirtNumber, models[3].ShirtNumber });
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, new[] { models[0].RosterIndex, models[1].RosterIndex, models[2].RosterIndex, models[3].RosterIndex });
        }

        [Test]
        public void ExactlyThePossessingAgentGetsARing_AtBothEnds()
        {
            var models = new AgentRenderModel[Roster];

            // Mirrored to the away side per the root CLAUDE.md trap: a home-only possession test
            // would pass on an implementation that only ever ringed team 0.
            foreach (int possessing in new[] { 1, 3 })
            {
                LiveMatchFrame frame = Frame(possessing: possessing);
                MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

                for (int i = 0; i < Roster; i++)
                {
                    Assert.AreEqual(i == possessing, models[i].HasBall,
                        "slot " + i + " with possession on " + possessing);
                    Assert.AreEqual(
                        i == possessing ? MatchClientConstants.PossessionRingRadiusM : 0f,
                        models[i].PossessionRingRadius, Tolerance);
                }
            }
        }

        [Test]
        public void ALooseBall_RingsNobody()
        {
            LiveMatchFrame frame = Frame(possessing: -1);
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

            for (int i = 0; i < Roster; i++)
            {
                Assert.IsFalse(models[i].HasBall, "slot " + i);
            }
        }

        [Test]
        public void TheMarkerAndRingRadii_AreBothPopulatedAndNotSwapped()
        {
            // Both are floats on the same struct, so a transposition compiles. What makes this able
            // to fail is that the two constants differ and the ring is the larger — asserting the
            // marker radius against its own constant alone would restate the assignment.
            LiveMatchFrame frame = Frame(possessing: 2);
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

            for (int i = 0; i < Roster; i++)
            {
                Assert.AreEqual(MatchClientConstants.AgentMarkerRadiusM, models[i].MarkerRadius, Tolerance, "slot " + i);
                Assert.Greater(models[i].MarkerRadius, 0f, "an unassigned marker radius draws nothing");
            }

            Assert.Greater(models[2].PossessionRingRadius, models[2].MarkerRadius,
                "a ring inside the marker it annotates is invisible");
        }

        [Test]
        public void ThePossessionRingRadius_IsDerivedFromHasBall_NotStoredBesideIt()
        {
            // The ring size is a [GT] presentation value; "who has the ball" is a fact about the
            // simulation. Deriving the radius from the flag keeps a config change from being able to
            // answer that question — inverting the derivation would have let a zero ring radius
            // report that nobody in the match had possession.
            var withBall    = new AgentRenderModel(0, 0, 1, Vector3.zero, 0.7f, true,  false, 0, false, false);
            var withoutBall = new AgentRenderModel(1, 0, 2, Vector3.zero, 0.7f, false, false, 0, false, false);

            Assert.AreEqual(MatchClientConstants.PossessionRingRadiusM, withBall.PossessionRingRadius, Tolerance);
            Assert.AreEqual(0f, withoutBall.PossessionRingRadius, Tolerance);
        }

        [Test]
        public void ANonFiniteAgentPosition_IsRefused_RatherThanDrawn()
        {
            // Nothing upstream gates coordinates: FrameInterpolator treats a non-finite position as
            // a discontinuity and SNAPS to it, propagating the NaN. MatchFrameView refuses one on
            // the screen-facing path; the pitch-facing path must not be quieter, or a NaN reaches
            // transform.position once P4b exists.
            MatchRoster roster = TwoPerTeam();
            var models = new AgentRenderModel[Roster];
            LiveMatchFrame frame = Frame();

            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var drawn = new Vector2[Roster];
                drawn[2] = new Vector2(bad, 34f);
                Assert.Throws<ArgumentException>(
                    () => MatchRenderProjection.ProjectAgents(drawn, in frame, roster, models),
                    "X = " + bad);

                var drawnY = new Vector2[Roster];
                drawnY[2] = new Vector2(52.5f, bad);
                Assert.Throws<ArgumentException>(
                    () => MatchRenderProjection.ProjectAgents(drawnY, in frame, roster, models),
                    "Y = " + bad);
            }
        }

        [Test]
        public void ARefusedFrame_LeavesTheDestinationUntouched_NotHalfWritten()
        {
            // The guard runs as its own pass before any write. Checking inside the write loop would
            // leave the slots before the bad one holding this frame and the rest holding the last —
            // a half-written buffer behind a thrown exception, which is worse than either outcome.
            MatchRoster roster = TwoPerTeam();
            LiveMatchFrame frame = Frame();

            var models = new AgentRenderModel[Roster];
            for (int i = 0; i < Roster; i++)
            {
                models[i] = new AgentRenderModel(90 + i, 1, 9, Vector3.one, 1f, true, true, 2, true, true);
            }

            var drawn = new Vector2[Roster];
            drawn[Roster - 1] = new Vector2(float.NaN, 34f);  // the LAST slot, so a partial write shows

            Assert.Throws<ArgumentException>(
                () => MatchRenderProjection.ProjectAgents(drawn, in frame, roster, models));

            for (int i = 0; i < Roster; i++)
            {
                Assert.AreEqual(90 + i, models[i].RosterIndex,
                    "slot " + i + " was overwritten before the frame was refused");
            }
        }

        [Test]
        public void ANonFiniteBallGroundPosition_IsRefused()
        {
            // Unlike a height, a ground position has no truthful fallback — there is no "where the
            // ball is" left to draw, so it is refused rather than approximated.
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.Throws<ArgumentException>(
                    () => MatchRenderProjection.ProjectBall(new Vector3(bad, 34f, 1f)), "X = " + bad);
                Assert.Throws<ArgumentException>(
                    () => MatchRenderProjection.ProjectBall(new Vector3(52.5f, bad, 1f)), "Y = " + bad);
            }
        }

        [Test]
        public void ProjectAgents_ReturnsTheCount_AndLeavesTrailingBufferSlotsAlone()
        {
            LiveMatchFrame frame = Frame();
            var models = new AgentRenderModel[Roster + 2];
            models[Roster] = new AgentRenderModel(99, 1, 7, Vector3.one, 1f, true, true, 2, true, true);

            int written = MatchRenderProjection.ProjectAgents(new Vector2[Roster + 3], in frame, TwoPerTeam(), models);

            Assert.AreEqual(Roster, written);
            Assert.AreEqual(99, models[Roster].RosterIndex, "slots past the roster are not the projector's to touch");
        }

        [Test]
        public void ShapeMismatchesAreRefused()
        {
            MatchRoster roster = TwoPerTeam();
            var models = new AgentRenderModel[Roster];
            LiveMatchFrame ok = Frame();

            Assert.Throws<ArgumentNullException>(
                () => MatchRenderProjection.ProjectAgents(null, in ok, roster, models));
            Assert.Throws<ArgumentNullException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster], in ok, null, models));
            Assert.Throws<ArgumentNullException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster], in ok, roster, null));

            LiveMatchFrame mismatchedCues = Frame(cues: new LiveAgentCue[Roster - 1]);
            Assert.Throws<ArgumentException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster], in mismatchedCues, roster, models));

            Assert.Throws<ArgumentException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster - 1], in ok, roster, models),
                "a short position buffer must not silently project fewer agents");
            Assert.Throws<ArgumentException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster], in ok, roster, new AgentRenderModel[Roster - 1]),
                "a short destination would leave stale slots on screen");
            Assert.Throws<ArgumentException>(
                () => MatchRenderProjection.ProjectAgents(new Vector2[Roster], in ok, new MatchRoster(new[] { 0, 1 }), models),
                "a roster smaller than the frame would mis-number half the pitch");
        }

        // ── Ball ────────────────────────────────────────────────────────────────────────

        [Test]
        public void AGroundedBall_SitsOnItsOwnShadow()
        {
            BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(52.5f, 34f, 0f));

            Assert.AreEqual(Vector3.zero, ball.ShadowPosition, "the centre spot is the world origin");
            Assert.AreEqual(ball.ShadowPosition, ball.WorldPosition);
            Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.Radius, Tolerance);
            Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.ShadowRadius, Tolerance);
        }

        [Test]
        public void TheShadowStaysOnTheGroundPoint_WhateverTheHeight()
        {
            Vector3 expected = PitchViewProjection.ToWorld(new Vector2(20f, 50f), 0f);

            foreach (float height in new[] { 0f, 2f, 12f, 40f })
            {
                BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, height));

                Assert.AreEqual(expected.x, ball.ShadowPosition.x, Tolerance, "height " + height);
                Assert.AreEqual(0f, ball.ShadowPosition.y, Tolerance, "a shadow is on the ground");
                Assert.AreEqual(expected.z, ball.ShadowPosition.z, Tolerance,
                    "the shadow marks where the ball actually is; height must not move it");
            }
        }

        [Test]
        public void HeightLiftsTheBallOnTheWorldYAxis_WithoutMovingItOverThePitch()
        {
            BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, 4f));

            Assert.AreEqual(ball.ShadowPosition.x, ball.WorldPosition.x, Tolerance,
                "lifting the ball must not slide it across the pitch");
            Assert.AreEqual(ball.ShadowPosition.z, ball.WorldPosition.z, Tolerance);
            Assert.AreEqual(4f, ball.WorldPosition.y, Tolerance, "height IS the world Y axis now");
            Assert.AreEqual(4f, ball.HeightM, Tolerance, "the raw engine height is reported as-is");
        }

        [Test]
        public void TheBallRadiusDoesNotVaryWithHeight()
        {
            // The flat-view model grew the ball with altitude to fake depth, which needed a cap and
            // saturated at 10 m. A tilted camera supplies apparent size itself, so the radius is
            // constant — this asserts the ramp is really gone rather than merely unused.
            foreach (float height in new[] { 0f, 1f, 10f, 25f, 100f })
            {
                BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(30f, 30f, height));

                Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.Radius, Tolerance, "height " + height);
                Assert.AreEqual(ball.Radius, ball.ShadowRadius, Tolerance, "height " + height);
            }
        }

        [Test]
        public void ADegenerateHeight_PutsTheBallOnTheGroundRatherThanNowhere()
        {
            // Deliberately NOT symmetric with the ground-position gate below: a bad height still
            // leaves a true ground position to draw at, so it degrades instead of throwing.
            Vector3 expectedGround = PitchViewProjection.ToWorld(new Vector2(20f, 50f), 0f);

            foreach (float height in new[] { float.NaN, float.PositiveInfinity, -3f })
            {
                BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, height));

                Assert.AreEqual(expectedGround.x, ball.ShadowPosition.x, Tolerance, "height " + height);
                Assert.AreEqual(expectedGround.z, ball.ShadowPosition.z, Tolerance, "height " + height);
                Assert.AreEqual(ball.ShadowPosition, ball.WorldPosition, "height " + height);
                Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.Radius, Tolerance, "height " + height);
            }
        }

    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): source-of-each-field assertions, the    |
// |         |            |        | live goalkeeper flag under substitution, possession ringing     |
// |         |            |        | mirrored to both teams, the shape guards, and the ball's        |
// |         |            |        | shadow / lift / capped-scale height cues.                       |
// | 1.1     | 2026-08-04 | —      | AR pass M-2/M-3/M-5/L-7: + the non-finite gates for agent and  |
// |         |            |        | ball ground positions (and the deliberate asymmetry with a     |
// |         |            |        | degenerate HEIGHT, which still degrades rather than throwing), |
// |         |            |        | + the ring-derives-from-HasBall lock, + a cap test pinning the |
// |         |            |        | 10 m saturation point its [GT] rationale now states. The       |
// |         |            |        | tautological EveryAgentGetsTheConfiguredMarkerRadius is        |
// |         |            |        | replaced by one that can fail: the two radii are distinct and  |
// |         |            |        | the ring is the larger, so a transposition is visible. + the    |
// |         |            |        | all-or-nothing lock: a refused frame must leave the destination |
// |         |            |        | untouched, not partly this frame and partly the last.           |
// | 1.2     | 2026-08-04 | —      | Tilted-view revision: agent and ball positions are asserted in |
// |         |            |        | world space (engine Y → world Z, height → world Y). The four   |
// |         |            |        | sprite-lift / height-scale / cap tests are GONE with the cues  |
// |         |            |        | they covered, replaced by TheBallRadiusDoesNotVaryWithHeight — |
// |         |            |        | which asserts the ramp is really absent rather than merely     |
// |         |            |        | unused, since an unused ramp would pass every other test here. |
#endregion
