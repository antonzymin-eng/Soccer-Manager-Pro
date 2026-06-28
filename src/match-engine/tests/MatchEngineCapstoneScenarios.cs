// File:     src/match-engine/tests/MatchEngineCapstoneScenarios.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase F,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8,
//           Deterministic Simulation #16 §3.1 (7-phase tick) / §4.8, Event System #17 §3.2.1,
//           Code Standards #20
// Purpose:  Phase F capstone — the cross-spec closed-loop scenario on the #19 ScenarioRunner
//           that drives a multi-second kickoff sequence through the FULLY composed MatchEngine
//           host (all 7 phases, all wired subsystems) and records (a) gameplay-invariant
//           envelope predicates and (b) a two-run determinism digest match. This is the
//           composition surface every per-subsystem suite and every per-spec scenario passes in
//           isolation but no test has exercised end-to-end through the host (design note Risk #3).

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the Phase F capstone scenario index (#19 §3.3.5 cross-spec/ layout; KD-8
    /// ownership). The manifest path lives under
    /// <see cref="TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX"/> and declares every
    /// spec the composed run exercises through the host. The scenario boots a real
    /// <see cref="MatchEngine"/> and ticks it for a multi-second kickoff; the body draws no
    /// randomness of its own (the engine self-seeds its own <see cref="DeterministicRngService"/>
    /// from the run seed at boot — KD-7), so the recorded seed is a canonical identifier per
    /// FR-TS-025. Tier classification is B (Simulation layer).
    /// </summary>
    internal static class MatchEngineCapstoneScenarios
    {
        public const string KickoffMultiSecondPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-kickoff-multi-second";

        public const ulong KickoffMultiSecondSeed = 0x0F1E2D3C4B5A6978UL;

        // 600 ticks @ 60 Hz = 10 s of composed match time. Long enough that the 10 Hz AI loop
        // strides exactly NumTicks / AI_PHASE_STRIDE times and the physics/resolve loop runs every
        // tick — the loop-separation invariant the host composes (#16 §3.1, design note §2.4).
        private const int NumTicks = 600;

        // Owning specs: every spec whose behaviour the composed run drives through the host.
        // Ball Physics (#1) + Agent Movement (#2) in Physics; Collision (#3) + First Touch (#4) +
        // Pass (#5) / Shot (#6) executors in Resolve; Decision Tree (#8) + Perception (#7) and the
        // Positioning/Pressing/Defensive/Attacking mechanics (#12–#15) in AI; Deterministic Sim
        // (#16) drives the 7-phase tick + snapshot digest; Event System (#17) carries the Phase E
        // possession-changed event; Testing Strategy (#19) owns the harness.
        private static readonly int[] OwningSpecIds =
            { 1, 2, 3, 4, 5, 6, 7, 8, 12, 13, 14, 15, 16, 17, 19 };

        // Pitch envelope (Ball Physics #1 §1.2: corner origin, X 0–105 m, Y 0–68 m, Z up, ground
        // at 0). Both ball and agents are bounded with a small margin: the invariant exists to catch
        // NaN / gross divergence of the composed loop over a multi-second run, not to police a
        // sub-metre touchline overrun (Stage 0 has no restart/throw-in model). A genuine escape of
        // these bounds is a real Phase F finding, not test fuzz.
        private const float PitchLengthM = 105.0f;
        private const float PitchWidthM  = 68.0f;
        private const float BoundsMarginM = 2.0f;

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "match-engine-kickoff-multi-second",
                owningSpecIds: OwningSpecIds,
                seed: KickoffMultiSecondSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    KickoffMultiSecondPath,
                    manifest,
                    new ClosedLoopScenario(manifest, KickoffMultiSecond)),
            });
        }

        // Multi-second kickoff through the fully composed host. The body runs the engine twice
        // with the same seed: run A supplies the gameplay-invariant observations; run B exists
        // solely to prove the per-tick chained snapshot digest is byte-identical (the design note
        // §5 Phase F "(b) pinned determinism digest across two runs"). Building engine B AFTER
        // engine A has drained ticks also re-locks EventBus.ResetForNewMatch() across two
        // in-process matches (design note Risk #4 — the process-static bus must re-subscribe).
        private static void KickoffMultiSecond(ScenarioContext context)
        {
            RunObservations a = RunMatch(context.RunSeed, NumTicks);
            RunObservations b = RunMatch(context.RunSeed, NumTicks);

            // (a) Gameplay-invariant envelope predicates.

            // The host advanced exactly NumTicks 60 Hz ticks — the snapshot phase ran every tick.
            context.Envelope.CheckEquals("tick-count", a.FinalTick, NumTicks);

            // The 10 Hz AI loop strided exactly NumTicks / AI_PHASE_STRIDE times over the run —
            // proves the loop-separation composition (Intent→AI stride gate; design note §2.4),
            // not just that the AI phase ran at all.
            long expectedAiTicks = NumTicks / DeterministicSimConstants.AI_PHASE_STRIDE;
            context.Envelope.CheckEquals("ai-stride-cadence", a.AiPhaseRunCount, expectedAiTicks);

            // The ball never left the pitch surface and never went non-finite across the whole run
            // — the composed Physics/Resolve loop kept world state sane for 10 s.
            context.Envelope.CheckTrue(
                "ball-stays-in-bounds", a.BallInBounds,
                "ball left the pitch envelope or went non-finite; first-bad-tick=" + a.FirstBadBallTick);

            // Every agent stayed finite and on (or within a small margin of) the pitch across the
            // whole run — the composed AI→Physics→Resolve fan-out over 22 agents did not diverge.
            context.Envelope.CheckTrue(
                "agents-stay-in-bounds", a.AgentsInBounds,
                "an agent left the pitch envelope or went non-finite; first-bad-tick=" + a.FirstBadAgentTick);

            // The chained snapshot digest changed on every tick (each digest hashes the previous
            // digest + the tick payload — a stalled snapshot phase would repeat a digest).
            context.Envelope.CheckTrue(
                "digest-chain-advances", a.DigestChainAdvances,
                "the chained snapshot digest repeated; first-repeat-tick=" + a.FirstDigestRepeatTick);

            // (b) Determinism: two same-seed runs produce byte-identical per-tick digest chains.
            context.Envelope.CheckTrue(
                "determinism-two-run-digest-match", DigestChainsEqual(a, b, out int divergeTick),
                "the snapshot digest chain diverged between two same-seed runs at tick " + divergeTick);
        }

        // Runs one match for the given seed and ticks, capturing the per-tick digest chain plus the
        // gameplay-invariant observations. A fresh MatchEngine self-seeds its own RNG at boot and
        // resets the process-static EventBus (EventBus.ResetForNewMatch in Boot).
        private static RunObservations RunMatch(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);

            var result = new RunObservations(ticks);
            byte[] prevDigest = null;

            for (int t = 1; t <= ticks; t++)
            {
                engine.RunTick();

                byte[] digest = engine.CurrentSnapshotDigest;
                result.Digests[t - 1] = digest;

                if (result.DigestChainAdvances && prevDigest != null && BytesEqual(prevDigest, digest))
                {
                    result.DigestChainAdvances = false;
                    result.FirstDigestRepeatTick = t;
                }
                prevDigest = digest;

                if (result.BallInBounds && !IsBallInBounds(engine.TestOnly_BallSnapshot.Position))
                {
                    result.BallInBounds = false;
                    result.FirstBadBallTick = t;
                }

                if (result.AgentsInBounds && !AllAgentsInBounds(engine))
                {
                    result.AgentsInBounds = false;
                    result.FirstBadAgentTick = t;
                }
            }

            result.FinalTick = (int)engine.CurrentTick;
            result.AiPhaseRunCount = (long)engine.AiPhaseRunCount;
            return result;
        }

        private static bool AllAgentsInBounds(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 p = engine.TestOnly_AgentSnapshot(i).Position;
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y)
                    || p.x < -BoundsMarginM || p.x > PitchLengthM + BoundsMarginM
                    || p.y < -BoundsMarginM || p.y > PitchWidthM + BoundsMarginM)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsBallInBounds(Vector3 p)
        {
            return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)
                && p.x >= -BoundsMarginM && p.x <= PitchLengthM + BoundsMarginM
                && p.y >= -BoundsMarginM && p.y <= PitchWidthM + BoundsMarginM
                && p.z >= -BoundsMarginM;
        }

        private static bool DigestChainsEqual(RunObservations a, RunObservations b, out int divergeTick)
        {
            int n = Math.Min(a.Digests.Length, b.Digests.Length);
            for (int i = 0; i < n; i++)
            {
                if (!BytesEqual(a.Digests[i], b.Digests[i]))
                {
                    divergeTick = i + 1;
                    return false;
                }
            }
            divergeTick = -1;
            return true;
        }

        private static bool BytesEqual(byte[] x, byte[] y)
        {
            if (x == null || y == null || x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        // Per-run observation sink. Test-side bookkeeping (heap allocation is fine off the hot path).
        private sealed class RunObservations
        {
            public readonly byte[][] Digests;
            public int  FinalTick;
            public long AiPhaseRunCount;
            public bool BallInBounds          = true;
            public bool AgentsInBounds        = true;
            public bool DigestChainAdvances   = true;
            public int  FirstBadBallTick      = -1;
            public int  FirstBadAgentTick     = -1;
            public int  FirstDigestRepeatTick = -1;

            public RunObservations(int ticks)
            {
                Digests = new byte[ticks][];
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation (Phase F capstone). Cross-spec closed-loop  |
// |         |            |        | scenario: boots a real MatchEngine and ticks 600× (10 s kickoff).  |
// |         |            |        | Records gameplay invariants (tick count, AI stride cadence, ball   |
// |         |            |        | + agents in bounds, chained digest advances) and a two-run         |
// |         |            |        | same-seed digest-chain match. Owning specs {1,2,3,4,5,6,7,8,       |
// |         |            |        | 12,13,14,15,16,17,19}; path under SCENARIO_PATH_CROSS_SPEC_PREFIX.  |
#endregion
