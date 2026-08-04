// File:     src/match-client-core/tests/MatchRenderProjectionTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the frame → draw-state projection: which source each field comes from, the
//           possession ring, the ball's height cues, and every shape guard.

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

            Vector2 expected = PitchViewProjection.ToView(new Vector2(30f, 20f));
            Assert.AreEqual(expected.x, models[0].ViewPosition.x, Tolerance);
            Assert.AreEqual(expected.y, models[0].ViewPosition.y, Tolerance);
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
        public void EveryAgentGetsTheConfiguredMarkerRadius()
        {
            LiveMatchFrame frame = Frame();
            var models = new AgentRenderModel[Roster];

            MatchRenderProjection.ProjectAgents(new Vector2[Roster], in frame, TwoPerTeam(), models);

            for (int i = 0; i < Roster; i++)
            {
                Assert.AreEqual(MatchClientConstants.AgentMarkerRadiusM, models[i].MarkerRadius, Tolerance);
            }
        }

        [Test]
        public void TheRingIsLargerThanTheMarkerItAnnotates()
        {
            Assert.Greater(MatchClientConstants.PossessionRingRadiusM, MatchClientConstants.AgentMarkerRadiusM,
                "a ring inside the marker is invisible");
        }

        [Test]
        public void ProjectAgents_ReturnsTheCount_AndLeavesTrailingBufferSlotsAlone()
        {
            LiveMatchFrame frame = Frame();
            var models = new AgentRenderModel[Roster + 2];
            models[Roster] = new AgentRenderModel(99, 1, 7, Vector2.one, 1f, 1f, true, 2, true, true);

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
        public void AGroundedBall_DrawsItsSpriteOnItsShadow_AtUnitScale()
        {
            BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(52.5f, 34f, 0f));

            Assert.AreEqual(Vector2.zero, ball.ShadowPosition);
            Assert.AreEqual(ball.ShadowPosition, ball.SpritePosition);
            Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.SpriteRadius, Tolerance);
            Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.ShadowRadius, Tolerance);
        }

        [Test]
        public void TheShadowStaysOnTheGroundPoint_WhateverTheHeight()
        {
            Vector2 expected = PitchViewProjection.ToView(new Vector2(20f, 50f));

            foreach (float height in new[] { 0f, 2f, 12f, 40f })
            {
                BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, height));

                Assert.AreEqual(expected.x, ball.ShadowPosition.x, Tolerance, "height " + height);
                Assert.AreEqual(expected.y, ball.ShadowPosition.y, Tolerance,
                    "the shadow marks where the ball actually is; height must not move it");
            }
        }

        [Test]
        public void HeightLiftsTheSpriteAlongYOnly_AndGrowsIt()
        {
            BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, 4f));

            Assert.AreEqual(ball.ShadowPosition.x, ball.SpritePosition.x, Tolerance,
                "the lift is vertical in the view plane; it must not shift the ball across the pitch");
            Assert.AreEqual(
                4f * MatchClientConstants.BallHeightViewOffsetPerMetre,
                ball.SpritePosition.y - ball.ShadowPosition.y, Tolerance);
            Assert.Greater(ball.SpriteRadius, ball.ShadowRadius);
            Assert.AreEqual(4f, ball.HeightM, Tolerance, "the raw engine height is reported as-is");
        }

        [Test]
        public void HeightScale_IsMonotonicAndCapped()
        {
            Assert.AreEqual(1f, MatchRenderProjection.HeightScale(0f), Tolerance);

            float previous = 1f;
            for (float h = 1f; h <= 60f; h += 1f)
            {
                float scale = MatchRenderProjection.HeightScale(h);
                Assert.GreaterOrEqual(scale, previous, "scale fell going up at " + h);
                Assert.LessOrEqual(scale, MatchClientConstants.BallMaxHeightScale + Tolerance,
                    "the cap must hold at " + h);
                previous = scale;
            }

            Assert.AreEqual(MatchClientConstants.BallMaxHeightScale, MatchRenderProjection.HeightScale(1000f), Tolerance);
        }

        [Test]
        public void ADegenerateHeight_DrawsTheBallOnTheGroundRatherThanNowhere()
        {
            foreach (float height in new[] { float.NaN, float.PositiveInfinity, -3f })
            {
                BallRenderModel ball = MatchRenderProjection.ProjectBall(new Vector3(20f, 50f, height));

                Assert.AreEqual(ball.ShadowPosition, ball.SpritePosition, "height " + height);
                Assert.AreEqual(MatchClientConstants.BallMarkerRadiusM, ball.SpriteRadius, Tolerance, "height " + height);
                Assert.AreEqual(1f, MatchRenderProjection.HeightScale(height), Tolerance);
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
#endregion
