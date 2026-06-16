// File:     src/match-engine/tests/MatchEngineDeterminismTests.cs
// Created:  2026-06-16
// Modified: 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase A / §7, Code Standards #20
// Purpose:  Phase A capstone tests — the determinism spine. Asserts that two same-seed runs
//           produce byte-identical snapshot digest chains, that the chain is real (advances and
//           is non-degenerate), and that the AI phase fires only on AI_PHASE_STRIDE ticks.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase A determinism + AI-stride cadence tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineDeterminismTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const int   TickCount = 120; // two seconds at 60 Hz

        /// <summary>
        /// Runs <paramref name="ticks"/> ticks from the given seed and captures the per-tick
        /// snapshot digest chain (a fresh 32-byte copy per tick).
        /// </summary>
        private static List<byte[]> RunAndCaptureChain(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        [Test]
        public void TwoSameSeedRuns_ProduceIdenticalDigestChains()
        {
            List<byte[]> chainA = RunAndCaptureChain(MatchSeed, TickCount);
            List<byte[]> chainB = RunAndCaptureChain(MatchSeed, TickCount);

            Assert.AreEqual(TickCount, chainA.Count, "Run A must record one digest per tick.");
            Assert.AreEqual(TickCount, chainB.Count, "Run B must record one digest per tick.");

            for (int i = 0; i < TickCount; i++)
            {
                CollectionAssert.AreEqual(
                    chainA[i], chainB[i],
                    $"Snapshot digest chain diverged at tick {i + 1} — the loop is non-deterministic.");
            }
        }

        [Test]
        public void DigestChain_IsNonDegenerate_AndAdvances()
        {
            List<byte[]> chain = RunAndCaptureChain(MatchSeed, TickCount);

            byte[] allZero = new byte[DeterministicSimConstants.SHA256_BYTES];

            // Each digest is a real 32-byte SHA-256 (never the all-zero placeholder)...
            for (int i = 0; i < chain.Count; i++)
            {
                Assert.AreEqual(DeterministicSimConstants.SHA256_BYTES, chain[i].Length,
                    $"Digest at tick {i + 1} must be 32 bytes.");
                CollectionAssert.AreNotEqual(allZero, chain[i],
                    $"Digest at tick {i + 1} must not be the all-zero placeholder.");
            }

            // ...and the chain advances each tick (prevDigest + tick differ ⇒ digests differ).
            for (int i = 1; i < chain.Count; i++)
            {
                CollectionAssert.AreNotEqual(chain[i - 1], chain[i],
                    $"Digest must change from tick {i} to tick {i + 1} (chain did not advance).");
            }
        }

        [Test]
        public void WorldState_FeedsSnapshotDigest()
        {
            // Baseline run vs a run whose ball height is perturbed before the first tick.
            // If world state were not serialized into the digest preimage, both digests would
            // match (the M-1 coverage gap: chain-advance alone is satisfied by the header tick).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetBallHeight(MatchEngineConstants.BALL_REST_HEIGHT_M + 1f);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing world state left the snapshot digest unchanged — world state is not in the digest preimage.");
        }

        [Test]
        public void AiPhase_RunsOnStrideTicks_Only()
        {
            int stride = DeterministicSimConstants.AI_PHASE_STRIDE;
            Assert.Greater(stride, 0, "AI_PHASE_STRIDE must be positive.");

            var engine = new MatchEngine(MatchSeed);
            int observedAiRuns = 0;

            for (int i = 0; i < TickCount; i++)
            {
                engine.RunTick();

                bool expectAi = (engine.CurrentTick % (ulong)stride) == 0UL;
                Assert.AreEqual(expectAi, engine.DidAiPhaseRunLastTick,
                    $"AI-phase cadence mismatch at tick {engine.CurrentTick} (stride {stride}).");

                if (engine.DidAiPhaseRunLastTick)
                {
                    observedAiRuns++;
                }
            }

            int expectedRuns = TickCount / stride;
            Assert.AreEqual(expectedRuns, observedAiRuns,
                "AI phase ran an unexpected number of times over the run.");
            Assert.AreEqual((ulong)expectedRuns, engine.AiPhaseRunCount,
                "AiPhaseRunCount must match the number of stride ticks.");
        }

        [Test]
        public void FirstProcessedTick_IsOne_AndFirstAiTickIsStride()
        {
            int stride = DeterministicSimConstants.AI_PHASE_STRIDE;
            var engine = new MatchEngine(MatchSeed);

            engine.RunTick();
            Assert.AreEqual(1UL, engine.CurrentTick,
                "MatchClock.Advance runs first, so the first processed tick is 1, not 0.");
            Assert.IsFalse(engine.DidAiPhaseRunLastTick,
                "Tick 1 is not a stride tick, so the AI phase must not run.");

            // Advance to the first stride tick.
            for (ulong t = engine.CurrentTick + 1; t <= (ulong)stride; t++)
            {
                engine.RunTick();
            }
            Assert.AreEqual((ulong)stride, engine.CurrentTick);
            Assert.IsTrue(engine.DidAiPhaseRunLastTick,
                $"The first AI evaluation must land on tick {stride} (~100 ms after kickoff).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                   |
// | 1.0     | 2026-06-16 | —      | Initial Phase A determinism + AI-stride cadence tests.  |
#endregion
