// File:     src/match-client-core/tests/FrameInterpolatorTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client §5-P3 / §7 "Interpolation", Code Standards #20
// Purpose:  Locks the blend math, the speed-awareness of alpha, and the two discontinuities the
//           interpolator must NOT smooth over (restarts and substitutions).

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    /// <summary>P3 frame-interpolation tests (see file header).</summary>
    [TestFixture]
    public sealed class FrameInterpolatorTests
    {
        private const int Roster = MatchEngineConstants.SQUAD_SIZE;

        private static LiveMatchFrame Frame(ulong tick, Vector3 ball, Func<int, Vector2> agentAt)
        {
            var positions = new Vector2[Roster];
            var cues      = new LiveAgentCue[Roster];
            for (int i = 0; i < Roster; i++)
            {
                positions[i] = agentAt(i);
            }

            return new LiveMatchFrame(
                tick, ball, MatchEngineConstants.NO_POSSESSION,
                positions, cues, new int[2],
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);
        }

        private static LiveMatchFrame FlatFrame(ulong tick, Vector3 ball, Vector2 everyAgent) =>
            Frame(tick, ball, _ => everyAgent);

        // ── ComputeAlpha ────────────────────────────────────────────────────────────────────────

        [Test]
        public void Alpha_HalfwayThroughTheInterval_IsAHalf()
        {
            // Six ticks at 60 Hz = 0.1 s; render 0.05 s in.
            Assert.AreEqual(0.5f, FrameInterpolator.ComputeAlpha(100UL, 106UL, 0.05f, 60f), 1e-5f);
        }

        [Test]
        public void Alpha_ScalesWithPlaybackSpeed_SoTheViewKeepsUpAt3x()
        {
            // The SAME 0.05 s of wall clock covers three times as much simulated time at 3×, so the
            // blend must be three times further along. An interpolator handed the unscaled rate would
            // fall further behind the sim every frame.
            float atOne   = FrameInterpolator.ComputeAlpha(100UL, 106UL, 0.05f, 60f);
            float atThree = FrameInterpolator.ComputeAlpha(100UL, 106UL, 0.05f, 180f);

            Assert.AreEqual(0.5f, atOne, 1e-5f);
            Assert.AreEqual(1f, atThree, 1e-5f, "3× consumes the whole interval and clamps.");
        }

        [Test]
        public void Alpha_ClampsIntoZeroOne()
        {
            Assert.AreEqual(0f, FrameInterpolator.ComputeAlpha(100UL, 106UL, 0f, 60f), 1e-6f);
            Assert.AreEqual(1f, FrameInterpolator.ComputeAlpha(100UL, 106UL, 10f, 60f), 1e-6f);
            Assert.AreEqual(0f, FrameInterpolator.ComputeAlpha(100UL, 106UL, -1f, 60f), 1e-6f);
        }

        [Test]
        public void Alpha_DegenerateInputs_FailToTheNewerFrame()
        {
            Assert.AreEqual(1f, FrameInterpolator.ComputeAlpha(106UL, 106UL, 0.01f, 60f), 1e-6f,
                "Non-advancing ticks: there is no interval to blend across.");
            Assert.AreEqual(1f, FrameInterpolator.ComputeAlpha(110UL, 106UL, 0.01f, 60f), 1e-6f,
                "Out-of-order frames.");
            Assert.AreEqual(1f, FrameInterpolator.ComputeAlpha(100UL, 106UL, 0.01f, 0f), 1e-6f,
                "A zero tick rate.");
            Assert.AreEqual(1f, FrameInterpolator.ComputeAlpha(100UL, 106UL, float.NaN, 60f), 1e-6f,
                "A NaN elapsed time must not become a NaN position.");
        }

        // ── Ball ────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Ball_BlendsLinearly()
        {
            var a = FlatFrame(100UL, new Vector3(50f, 34f, 0.11f), Vector2.zero);
            var b = FlatFrame(106UL, new Vector3(54f, 34f, 0.11f), Vector2.zero);

            Assert.AreEqual(52f, FrameInterpolator.BallAt(a, b, 0.5f).x, 1e-4f);
            Assert.AreEqual(50f, FrameInterpolator.BallAt(a, b, 0f).x, 1e-4f);
            Assert.AreEqual(54f, FrameInterpolator.BallAt(a, b, 1f).x, 1e-4f);
        }

        [Test]
        public void Ball_SnapsAcrossARestartTeleport_RatherThanGlidingTheLengthOfThePitch()
        {
            // A goal kick puts the ball on the six-yard line from wherever it left play. Blending that
            // draws the ball flying back up the pitch, which is not what happened.
            var a = FlatFrame(100UL, new Vector3(103f, 60f, 0.11f), Vector2.zero);
            var b = FlatFrame(101UL, new Vector3(6f, 34f, 0.11f), Vector2.zero);

            Vector3 drawn = FrameInterpolator.BallAt(a, b, 0.5f);

            Assert.AreEqual(b.BallPosition.x, drawn.x, 1e-4f, "Snapped to the newer frame.");
            Assert.AreEqual(b.BallPosition.y, drawn.y, 1e-4f);
        }

        [Test]
        public void Ball_MovementJustUnderTheSnapThresholdStillBlends()
        {
            // Non-vacuity for the test above: the snap must be a discontinuity guard, not a blanket
            // "never interpolate the ball".
            float justUnder = MatchClientConstants.BallSnapDistanceM * 0.9f;
            var a = FlatFrame(100UL, new Vector3(50f, 34f, 0.11f), Vector2.zero);
            var b = FlatFrame(101UL, new Vector3(50f + justUnder, 34f, 0.11f), Vector2.zero);

            Assert.AreEqual(50f + justUnder * 0.5f, FrameInterpolator.BallAt(a, b, 0.5f).x, 1e-3f);
        }

        [Test]
        public void Ball_NonFinitePosition_TakesTheNewerFrame()
        {
            var a = FlatFrame(100UL, new Vector3(float.NaN, 34f, 0.11f), Vector2.zero);
            var b = FlatFrame(101UL, new Vector3(50f, 34f, 0.11f), Vector2.zero);

            Assert.AreEqual(50f, FrameInterpolator.BallAt(a, b, 0.5f).x, 1e-4f);
        }

        // ── Agents ──────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Agents_BlendLinearly()
        {
            var a = Frame(100UL, Vector3.zero, i => new Vector2(i, 0f));
            var b = Frame(106UL, Vector3.zero, i => new Vector2(i, 2f));
            var dst = new Vector2[Roster];

            FrameInterpolator.AgentsAt(a, b, 0.25f, dst);

            for (int i = 0; i < Roster; i++)
            {
                Assert.AreEqual(i, dst[i].x, 1e-4f);
                Assert.AreEqual(0.5f, dst[i].y, 1e-4f);
            }
        }

        [Test]
        public void Agents_SnapTheSubstitutedSlotOnly()
        {
            // A substitution swaps who occupies slot 7; every other slot kept moving normally, so only
            // slot 7 should jump.
            var a = Frame(100UL, Vector3.zero, i => new Vector2(10f + i, 10f));
            var b = Frame(101UL, Vector3.zero, i => i == 7 ? new Vector2(90f, 60f) : new Vector2(10f + i, 12f));
            var dst = new Vector2[Roster];

            FrameInterpolator.AgentsAt(a, b, 0.5f, dst);

            Assert.AreEqual(90f, dst[7].x, 1e-4f, "The substituted slot snaps.");
            Assert.AreEqual(60f, dst[7].y, 1e-4f);
            Assert.AreEqual(11f, dst[6].y, 1e-4f, "Its neighbours are untouched and still blend.");
        }

        [Test]
        public void Agents_RosterLengthChange_SnapsEverySlot()
        {
            var a = Frame(100UL, Vector3.zero, i => new Vector2(10f, 10f));
            var b = new LiveMatchFrame(
                101UL, Vector3.zero, MatchEngineConstants.NO_POSSESSION,
                new[] { new Vector2(11f, 11f), new Vector2(12f, 12f) },
                new LiveAgentCue[2], new int[2],
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);
            var dst = new Vector2[Roster];

            FrameInterpolator.AgentsAt(a, b, 0.5f, dst);

            Assert.AreEqual(11f, dst[0].x, 1e-4f, "Slot i in one frame need not be slot i in the other.");
            Assert.AreEqual(12f, dst[1].x, 1e-4f);
        }

        [Test]
        public void Agents_ShortDestination_FailsLoud()
        {
            var a = Frame(100UL, Vector3.zero, _ => Vector2.zero);
            var b = Frame(101UL, Vector3.zero, _ => Vector2.zero);

            Assert.Throws<ArgumentException>(
                () => FrameInterpolator.AgentsAt(a, b, 0.5f, new Vector2[Roster - 1]),
                "A partial write leaves the remaining slots rendering a stale frame.");
        }

        [Test]
        public void Agents_NullBuffers_FailLoud()
        {
            var a = Frame(100UL, Vector3.zero, _ => Vector2.zero);
            var b = Frame(101UL, Vector3.zero, _ => Vector2.zero);

            Assert.Throws<ArgumentNullException>(() => FrameInterpolator.AgentsAt(a, b, 0.5f, null));
        }

        [Test]
        public void Agents_NonFiniteAlpha_TakesTheNewerFrame()
        {
            var a = Frame(100UL, Vector3.zero, _ => new Vector2(1f, 1f));
            var b = Frame(101UL, Vector3.zero, _ => new Vector2(2f, 2f));
            var dst = new Vector2[Roster];

            FrameInterpolator.AgentsAt(a, b, float.NaN, dst);

            Assert.AreEqual(2f, dst[0].x, 1e-4f, "A NaN alpha must never reach a rendered position.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P3): alpha derivation incl. playback-speed   |
// |         |            |        | scaling and every degenerate input, linear ball/agent blending,|
// |         |            |        | and the restart / substitution / roster-change snap paths.     |
// | 1.1     | 2026-07-27 | —      | Builds frames through the 10-parameter LiveMatchFrame ctor     |
// |         |            |        | (Scoreline + RestartBanner). P4 was authored in parallel       |
// |         |            |        | against the 13-parameter loose-field shape the P1 AR-1 M-6     |
// |         |            |        | restructure replaced — the same semantic merge conflict.       |
#endregion
