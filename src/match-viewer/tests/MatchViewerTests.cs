// File:     src/match-viewer/tests/MatchViewerTests.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling), Testing Strategy #19 (unit layer), Code Standards #20
// Purpose:  Contract tests for MatchReplayRecorder + HtmlReplayExporter: frame cadence, on-pitch
//           finite positions, recorder determinism, observer neutrality (recording does not perturb
//           the engine digest), fail-loud guards, and exporter output structure.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.MatchEngine;
using UnityEngine;

namespace TacticalDirector.MatchViewer.Tests
{
    [TestFixture]
    public class MatchViewerTests
    {
        private const ulong Seed = 777UL;

        // 2 s at 60 Hz — long enough to cross several AI strides, short enough for the unit layer.
        private const int Ticks = 120;

        [Test]
        public void Record_CapturesKickoffAndStrideFrames()
        {
            MatchReplay replay = MatchReplayRecorder.Record(Seed, Ticks, sampleStride: 6);

            // Frame 0 (kickoff, tick 0) + one frame per stride tick (stride divides Ticks exactly).
            Assert.AreEqual(Ticks / 6 + 1, replay.Frames.Count);
            Assert.AreEqual(0UL, replay.Frames[0].Tick);
            Assert.AreEqual((ulong)Ticks, replay.Frames[replay.Frames.Count - 1].Tick);
            Assert.AreEqual(6UL, replay.Frames[1].Tick - replay.Frames[0].Tick);
            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, replay.AgentCount);
        }

        [Test]
        public void Record_FinalTickCapturedWhenStrideDoesNotDivide()
        {
            MatchReplay replay = MatchReplayRecorder.Record(Seed, numTicks: 10, sampleStride: 7);

            // Frames at ticks 0, 7, and the forced final capture at 10.
            Assert.AreEqual(3, replay.Frames.Count);
            Assert.AreEqual(10UL, replay.Frames[2].Tick);
        }

        [Test]
        public void Record_PositionsFiniteAndOnPitch()
        {
            MatchReplay replay = MatchReplayRecorder.Record(Seed, Ticks);

            foreach (ReplayFrame frame in replay.Frames)
            {
                Assert.IsTrue(IsFinite(frame.BallPosition.x) && IsFinite(frame.BallPosition.y)
                              && IsFinite(frame.BallPosition.z));
                for (int i = 0; i < frame.AgentPositions.Length; i++)
                {
                    Vector2 p = frame.AgentPositions[i];
                    Assert.IsTrue(IsFinite(p.x) && IsFinite(p.y), $"agent {i} non-finite at tick {frame.Tick}");
                    Assert.That(p.x, Is.InRange(0f, replay.PitchLengthM), $"agent {i} X off pitch at tick {frame.Tick}");
                    Assert.That(p.y, Is.InRange(0f, replay.PitchWidthM), $"agent {i} Y off pitch at tick {frame.Tick}");
                }
            }
        }

        [Test]
        public void Record_DeterministicAcrossRuns()
        {
            MatchReplay a = MatchReplayRecorder.Record(Seed, Ticks);
            MatchReplay b = MatchReplayRecorder.Record(Seed, Ticks);

            Assert.AreEqual(a.Frames.Count, b.Frames.Count);
            for (int f = 0; f < a.Frames.Count; f++)
            {
                Assert.AreEqual(a.Frames[f].Tick, b.Frames[f].Tick);
                Assert.AreEqual(a.Frames[f].PossessingAgentId, b.Frames[f].PossessingAgentId);
                AssertBitwiseEqual(a.Frames[f].BallPosition.x, b.Frames[f].BallPosition.x);
                AssertBitwiseEqual(a.Frames[f].BallPosition.y, b.Frames[f].BallPosition.y);
                AssertBitwiseEqual(a.Frames[f].BallPosition.z, b.Frames[f].BallPosition.z);
                for (int i = 0; i < a.Frames[f].AgentPositions.Length; i++)
                {
                    AssertBitwiseEqual(a.Frames[f].AgentPositions[i].x, b.Frames[f].AgentPositions[i].x);
                    AssertBitwiseEqual(a.Frames[f].AgentPositions[i].y, b.Frames[f].AgentPositions[i].y);
                }
            }
        }

        [Test]
        public void Record_IsObserverNeutral_DigestMatchesUnobservedRun()
        {
            // Recording reads only the public observation surface; the snapshot digest of a
            // recorded run must be byte-identical to an unobserved run of the same seed.
            var observed = new MatchEngine.MatchEngine(Seed);
            MatchReplayRecorder.Record(observed, Seed, Ticks);

            var unobserved = new MatchEngine.MatchEngine(Seed);
            for (int t = 0; t < Ticks; t++) { unobserved.RunTick(); }

            CollectionAssert.AreEqual(unobserved.CurrentSnapshotDigest, observed.CurrentSnapshotDigest);
        }

        [Test]
        public void Record_GuardsFailLoud()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchReplayRecorder.Record(Seed, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MatchReplayRecorder.Record(Seed, Ticks, 0));
            Assert.Throws<ArgumentNullException>(() => MatchReplayRecorder.Record(null, Seed, Ticks));
        }

        [Test]
        public void Export_ProducesSelfContainedDocument()
        {
            MatchReplay replay = MatchReplayRecorder.Record(Seed, Ticks);
            string html = HtmlReplayExporter.Export(replay);

            StringAssert.StartsWith("<!DOCTYPE html>", html);
            StringAssert.EndsWith("</html>\n", html);
            StringAssert.Contains("const FRAMES=[", html);
            StringAssert.Contains("canvas", html);
            // Every frame row present: count '[' openers inside the FRAMES literal.
            int frameRows = CountOccurrences(html, "\n[");
            Assert.AreEqual(replay.Frames.Count, frameRows);
            // InvariantCulture: no comma decimal separators can produce ",NN]" artifacts, and the
            // fail-loud gate means no non-finite tokens survive to the document.
            StringAssert.DoesNotContain("NaN", html);
            StringAssert.DoesNotContain("Infinity", html);
        }

        [Test]
        public void ObservationSurface_GuardsFailLoud()
        {
            var engine = new MatchEngine.MatchEngine(Seed);
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentView(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentTeamId(MatchEngineConstants.SQUAD_SIZE));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentIsGoalkeeper(-1));
        }

        [Test]
        public void Replay_RefusesIncoherentFrames()
        {
            // AR-1 M-1: a frame whose AgentPositions is null or does not match the roster length
            // must be refused at construction, not silently no-op in the canvas renderer.
            var shortFrame = new List<ReplayFrame>
            {
                new ReplayFrame(0UL, Vector3.zero, -1, new Vector2[1]),
            };
            Assert.Throws<ArgumentException>(
                () => new MatchReplay(0UL, 60, 1, 105f, 68f, new[] { 0, 1 }, new[] { false, false }, shortFrame));

            var nullFrame = new List<ReplayFrame> { new ReplayFrame(0UL, Vector3.zero, -1, null) };
            Assert.Throws<ArgumentException>(
                () => new MatchReplay(0UL, 60, 1, 105f, 68f, new[] { 0 }, new[] { false }, nullFrame));
        }

        [Test]
        public void Export_GuardsFailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => HtmlReplayExporter.Export(null));

            // A hand-built replay with a non-finite coordinate must throw, not emit 'NaN'.
            var frames = new List<ReplayFrame>
            {
                new ReplayFrame(0UL, new Vector3(float.NaN, 0f, 0f), -1, new Vector2[1]),
            };
            var corrupt = new MatchReplay(0UL, 60, 1, 105f, 68f, new[] { 0 }, new[] { false }, frames);
            Assert.Throws<InvalidOperationException>(() => HtmlReplayExporter.Export(corrupt));
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        private static void AssertBitwiseEqual(float expected, float actual)
        {
            Assert.AreEqual(BitConverter.SingleToInt32Bits(expected), BitConverter.SingleToInt32Bits(actual));
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: cadence, on-pitch finiteness, bitwise       |
// |         |            |        | determinism, observer-neutrality digest lock, fail-loud       |
// |         |            |        | guards, exporter structure locks.                             |
// | 1.1     | 2026-07-02 | —      | AR-1 locks: observation-surface roster-index guards (M-2) +   |
// |         |            |        | MatchReplay incoherent-frame refusal (M-1).                   |
#endregion
