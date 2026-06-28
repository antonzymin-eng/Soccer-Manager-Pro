// File:     src/match-engine/tests/MatchEngineAwayTeamScenarios.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Decision Tree #8 audit-report.md (ERR-008-002 home/away asymmetry root cause),
//           Tactical Instructions #21 §3.1/§3.2/§4.6 (FR-TI-027/031), Match Engine design note §5,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8,
//           Deterministic Simulation #16 §3.1 (7-phase tick), Code Standards #20
// Purpose:  The away-team closed-loop scenario deferred at the Decision Tree #8 comprehensive
//           audit ("an away-team closed-loop scenario on the #19 ScenarioRunner once the DT
//           orchestrator seam exists"). The #21 runtime-activation single-writer is that seam.
//           Every DT spec worked example and every AR-1 fixture used the HOME team; the three
//           H/M-class asymmetry defects (zone inversion, press-urgency, line-depth) all shared one
//           root cause — the away team was served the home perspective. This scenario sets an
//           ASYMMETRIC tactic through the composed host and asserts that every away agent receives
//           the AWAY tactic, distinct from home — the composition-level lock for that bug class.

using System;

using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the away-team scenario index (#19 §3.3.5 cross-spec/ layout; KD-8 ownership). The body
    /// boots a real <see cref="MatchEngine"/>, sets an asymmetric per-team tactic (home defending,
    /// away attacking), ticks a multi-second kickoff, and records envelope predicates that lock the
    /// away team's per-agent DecisionTree input to the away tactic — the Decision Tree #8 audit
    /// home/away asymmetry root cause, verified through the composed host rather than per-function.
    /// Tier classification is B (Simulation layer).
    /// </summary>
    internal static class MatchEngineAwayTeamScenarios
    {
        public const string TacticMirrorPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "away-team-tactic-mirror";

        public const ulong TacticMirrorSeed = 0x1A2B3C4D5E6F7081UL;

        // 300 ticks @ 60 Hz = 5 s of composed match time — multi-second and many AI strides
        // (NumTicks / AI_PHASE_STRIDE), so the per-team tactic is committed and routed repeatedly.
        private const int NumTicks = 300;

        // Owning specs: the predicates depend on the DecisionTree (#8) tactic-input routing, the
        // Tactical Instructions (#21) translation + stride-gated single-writer, Agent Movement (#2)
        // for the bounds invariant, Deterministic Simulation (#16) for the 7-phase tick + digest, and
        // Testing Strategy (#19) for the harness. (The full host runs every phase; these are the
        // specs the asserted behaviour is load-bearing on.)
        private static readonly int[] OwningSpecIds = { 2, 8, 16, 19, 21 };

        // Pitch envelope (Ball Physics #1 §1.2: corner origin, X 0–105 m, Y 0–68 m). Agents are
        // clamped each physics tick to pitch ± Agent Movement SafetyConstants.PitchBoundaryBuffer
        // (5 m); a small epsilon absorbs float fuzz at the clamp edge.
        private const float PitchLengthM   = 105.0f;
        private const float PitchWidthM    = 68.0f;
        private const float AgentBufferM   = 5.0f;
        private const float BoundsEpsilonM = 0.5f;

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "away-team-tactic-mirror",
                owningSpecIds: OwningSpecIds,
                seed: TacticMirrorSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    TacticMirrorPath,
                    manifest,
                    new ClosedLoopScenario(manifest, TacticMirror)),
            });
        }

        // The away team gets an ATTACKING tactic; the home team a DEFENDING one. After a multi-second
        // run the away partition's DecisionTree input must carry the away (attacking) tactic and the
        // home partition the home (defending) one — distinct. A regression that consumed the home
        // perspective for both teams (the Decision Tree #8 H-2 / M-1 / M-2 defect class) would route
        // the home tactic to away agents and fail the "home-and-away-distinct" predicates.
        private static void TacticMirror(ScenarioContext context)
        {
            RunObservations a = RunMatch(context.RunSeed, NumTicks);
            RunObservations b = RunMatch(context.RunSeed, NumTicks);

            // The host advanced exactly NumTicks ticks and strided the AI loop the expected count.
            context.Envelope.CheckEquals("tick-count", a.FinalTick, NumTicks);
            long expectedAiTicks = NumTicks / DeterministicSimConstants.AI_PHASE_STRIDE;
            context.Envelope.CheckEquals("ai-stride-cadence", a.AiPhaseRunCount, expectedAiTicks);

            // Every AWAY agent received the away (attacking) tactic, translated correctly — not a
            // single away agent was left on the home perspective or the boot default.
            context.Envelope.CheckTrue(
                "away-agents-carry-away-tactic", a.AwayAllAttacking,
                "an away agent's DecisionTree input did not carry the away (attacking) tactic; "
                    + "first-bad-away-agent=" + a.FirstBadAwayAgent);

            // Every HOME agent received the home (defending) tactic — the symmetric half of the lock.
            context.Envelope.CheckTrue(
                "home-agents-carry-home-tactic", a.HomeAllDefending,
                "a home agent's DecisionTree input did not carry the home (defending) tactic; "
                    + "first-bad-home-agent=" + a.FirstBadHomeAgent);

            // The two partitions are DISTINCT — the asymmetry the Decision Tree #8 audit found
            // collapsed (away served the home perspective) is held open here at the composed host.
            context.Envelope.CheckTrue(
                "home-and-away-distinct", a.PartitionsDistinct,
                "home and away agents resolved to the same routed tactic — the home/away asymmetry "
                    + "collapsed (Decision Tree #8 ERR-008-002 defect class).");

            // The away team stayed finite and on (or within the buffer of) the pitch for the whole run.
            context.Envelope.CheckTrue(
                "away-agents-stay-in-bounds", a.AwayAgentsInBounds,
                "an away agent left the pitch envelope or went non-finite; first-bad-tick="
                    + a.FirstBadBoundsTick);

            // Determinism: two same-seed runs with the same asymmetric tactics produce byte-identical
            // per-tick digest chains (also re-locks EventBus.ResetForNewMatch across two in-process
            // matches — engine B is built after engine A has drained ticks).
            context.Envelope.CheckTrue(
                "determinism-two-run-digest-match", DigestChainsEqual(a, b, out int divergeTick),
                "the snapshot digest chain diverged between two same-seed runs at tick " + divergeTick);
        }

        // Runs one match with home=defending / away=attacking, capturing the per-tick digest chain,
        // the away-agent bounds invariant, and the final per-agent routed tactic on both partitions.
        private static RunObservations RunMatch(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            engine.SetTeamTactic(0, Defending());
            engine.SetTeamTactic(1, Attacking());

            var result = new RunObservations(ticks);

            for (int t = 1; t <= ticks; t++)
            {
                engine.RunTick();

                result.Digests[t - 1] = engine.CurrentSnapshotDigest;

                if (result.AwayAgentsInBounds && !AwayAgentsInBounds(engine))
                {
                    result.AwayAgentsInBounds = false;
                    result.FirstBadBoundsTick = t;
                }
            }

            result.FinalTick = (int)engine.CurrentTick;
            result.AiPhaseRunCount = (long)engine.AiPhaseRunCount;

            EvaluatePartitions(engine, result);
            return result;
        }

        // Reads the final committed per-agent routed tactic on each partition. Home agents are
        // 0..PLAYERS_PER_TEAM-1 (team 0); away agents are PLAYERS_PER_TEAM..SQUAD_SIZE-1 (team 1).
        private static void EvaluatePartitions(MatchEngine engine, RunObservations result)
        {
            int perTeam = MatchEngineConstants.PLAYERS_PER_TEAM;

            for (int k = 0; k < perTeam; k++)
            {
                int home = k;
                int away = perTeam + k;

                if (result.HomeAllDefending && !IsDefending(engine, home))
                {
                    result.HomeAllDefending = false;
                    result.FirstBadHomeAgent = home;
                }
                if (result.AwayAllAttacking && !IsAttacking(engine, away))
                {
                    result.AwayAllAttacking = false;
                    result.FirstBadAwayAgent = away;
                }
            }

            // Distinctness: the two team tactics translate to different routed values on every shared
            // dimension, so a correctly-routed match has home != away. Comparing agent 0 (home) vs the
            // first away agent is sufficient — the per-agent loops above already proved uniformity.
            result.PartitionsDistinct =
                engine.TestOnly_Mentality(0) != engine.TestOnly_Mentality(perTeam)
                && engine.TestOnly_Pressing(0) != engine.TestOnly_Pressing(perTeam)
                && engine.TestOnly_Passing(0) != engine.TestOnly_Passing(perTeam);
        }

        private static bool IsAttacking(MatchEngine engine, int agentId)
            => engine.TestOnly_Mentality(agentId) == Mentality.VeryAttacking
            && engine.TestOnly_Pressing(agentId) == PressingMode.HIGH
            && engine.TestOnly_Passing(agentId) == PassingStyle.DIRECT;

        private static bool IsDefending(MatchEngine engine, int agentId)
            => engine.TestOnly_Mentality(agentId) == Mentality.VeryDefensive
            && engine.TestOnly_Pressing(agentId) == PressingMode.LOW
            && engine.TestOnly_Passing(agentId) == PassingStyle.SHORT;

        private static bool AwayAgentsInBounds(MatchEngine engine)
        {
            for (int i = MatchEngineConstants.PLAYERS_PER_TEAM; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 p = engine.TestOnly_AgentSnapshot(i).Position;
                float bound = AgentBufferM + BoundsEpsilonM;
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y)
                    || p.x < -bound || p.x > PitchLengthM + bound
                    || p.y < -bound || p.y > PitchWidthM + bound)
                {
                    return false;
                }
            }
            return true;
        }

        // A bold, fully non-Balanced ATTACKING tactic — every translated dimension is the extreme
        // opposite of the defending tactic (so the two partitions are unambiguously distinct).
        private static TeamTactic Attacking() => new TeamTactic(
            Mentality.VeryAttacking, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Direct, TacticPressing.High, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        private static TeamTactic Defending() => new TeamTactic(
            Mentality.VeryDefensive, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Short, TacticPressing.Low, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

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
            public bool AwayAllAttacking    = true;
            public bool HomeAllDefending    = true;
            public bool PartitionsDistinct;
            public bool AwayAgentsInBounds  = true;
            public int  FirstBadAwayAgent   = -1;
            public int  FirstBadHomeAgent   = -1;
            public int  FirstBadBoundsTick  = -1;

            public RunObservations(int ticks)
            {
                Digests = new byte[ticks][];
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. The Decision Tree #8 audit's deferred      |
// |         |            |        | away-team closed-loop scenario on the #19 ScenarioRunner, now      |
// |         |            |        | enabled by the #21 runtime-activation single-writer. Boots a real  |
// |         |            |        | MatchEngine, sets home=defending / away=attacking, ticks 300×      |
// |         |            |        | (5 s), and locks every away agent to the away tactic, every home   |
// |         |            |        | agent to the home tactic, the two partitions distinct, away agents  |
// |         |            |        | in bounds, and a two-run determinism digest match. Owning specs     |
// |         |            |        | {2,8,16,19,21}; path under SCENARIO_PATH_CROSS_SPEC_PREFIX.         |
#endregion
