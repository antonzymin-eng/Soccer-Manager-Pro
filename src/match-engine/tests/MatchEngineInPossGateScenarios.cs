// File:     src/match-engine/tests/MatchEngineInPossGateScenarios.cs
// Created:  2026-08-08
// Modified: 2026-09-17 (PR #416 corrected-baseline floors frozen from exact post-fix head)
// Author:   —
// Spec:     Positioning AI #12 §3.0.1 / §3.0.2 / FR-PA-022 (ERR-012-011);
//           match-engine-wiring-backlog.md §3 C1; Testing Strategy & Framework #19
//           §3.3.1/§3.3.5/Appendix A.1; Code Standards #20
// Purpose:  Acceptance scenario for wiring-backlog C1 — with the ball in the final third, #12 must
//           commit a POSSESSION phase (InPoss or OutOfPoss) rather than a transition phase, because
//           somebody's team almost always has the ball there.
//
//           WHAT IT DELIBERATELY DOES NOT ASSERT. No goal rate, no shot count, no box-occupancy
//           figure and no dribble direction. All four were measured across the six-seed corpus at
//           this landing and the shape metrics moved the WRONG way — deepest composed slot 23.0 →
//           25.7 m from goal, mean attackers in the box 0.04 → 0.02 — exactly as the pre-implementation
//           council predicted from #12's PullFactor table, whose InPoss column is LESS advanced than
//           the TransToAtk column it replaces for every attacking role. Pinning any of them here
//           would either encode a regression as a contract or need its bound lowered to pass, and a
//           predicate whose bound must be lowered to pass is measuring nothing (Acceptance-1's own
//           lesson). What IS asserted is the mechanism's own signature: whether the engine knows who
//           has the ball.
//
//           MIRRORED BY CONSTRUCTION. The share is asserted independently from BOTH teams' committed
//           phases. Possession is a shared fact, so the two views must agree — but #12's phase is
//           computed per team from a MIRRORED snapshot with a hand-written BallVxFiltered sign flip,
//           and three home/away asymmetry defects have shipped in this project because every fixture
//           used the home team (#8 ERR-008-002). A one-sided predicate would not see that class.

using System;
using System.Globalization;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the C1 `InPoss`-gate acceptance scenario index (#19 §3.3.5 cross-spec layout; KD-8
    /// ownership). Tier B — phase-classification correctness over a bounded composed corpus.
    /// </summary>
    internal static class MatchEngineInPossGateScenarios
    {
        public const string InPossGatePath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-inposs-gate";

        public const ulong InPossGateSeed = 0x0F1E2D3C4B5A6978UL;

        // PR #416 detector hardening preregistration freezes this run length before the corrected
        // W2-active baseline counts are observed. Full matches are required because §5.Z.23 AR-1
        // established that these distributions are not stationary within a match.
        private const int NumTicks = 324000;

        /// <summary>
        /// The two worst separators on this measure, deliberately adversarial. The broader six-seed
        /// W2 evidence corpus belongs to the separate Step-1b workflow; permanent PR CI stays on these
        /// two seeds.
        /// </summary>
        private static readonly ulong[] Seeds =
        {
            InPossGateSeed,
            0x1A2B3C4D5E6F7081UL,
        };

        // PR #416 preregistered rule: floor(0.80 × corrected-baseline samples), per seed.
        // Exact post-perception-fix capture: run 35286928656 on head a7f2b77…
        // 15,830 -> 12,664; 16,423 -> 13,138. Population-stability guards, not
        // football-realism targets; pooled population remains diagnostic only.
        private static readonly int[] MinimumSamplesBySeed =
        {
            12_664,
            13_138,
        };

        /// <summary>Final-third depth from the defended goal line (m) — PITCH_LENGTH / 3.</summary>
        private const float FinalThirdDepthM = MatchEngineConstants.PITCH_LENGTH_M / 3.0f;

        /// <summary>0.1 s sampling — the #12 tactical cadence, so one sample per committed phase.</summary>
        private const int SampleStrideTicks = 6;

        // #12 (the classifier), #16 (determinism), #19 (harness). Not #13/#14/#15: their gates read
        // this phase but this scenario asserts nothing about what they then do with it.
        private static readonly int[] OwningSpecIds = { 12, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-inposs-gate",
                owningSpecIds: OwningSpecIds,
                seed: InPossGateSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunInPossGate);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(InPossGatePath, manifest, scenario),
            });
        }

        private static void RunInPossGate(ScenarioContext context)
        {
            int pooledSamples = 0;
            int pooledHomeViewPossession = 0;
            int pooledAwayViewPossession = 0;

            string f3(double v) => v.ToString("F3", CultureInfo.InvariantCulture);

            for (int s = 0; s < Seeds.Length; s++)
            {
                int samples = 0;
                int homeViewPossession = 0;
                int awayViewPossession = 0;

                PlayOne(Seeds[s], ref samples, ref homeViewPossession, ref awayViewPossession);

                pooledSamples += samples;
                pooledHomeViewPossession += homeViewPossession;
                pooledAwayViewPossession += awayViewPossession;

                float homeShare = samples > 0 ? (float)homeViewPossession / samples : 0f;
                float awayShare = samples > 0 ? (float)awayViewPossession / samples : 0f;
                string seedLabel = "0x" + Seeds[s].ToString("X16", CultureInfo.InvariantCulture);

                // Deliberate capture line for the preregistered PR #416 corrected baseline. Numeric
                // per-seed floors remain unfrozen until a valid corrected-baseline run completes.
                Console.WriteLine(
                    "INPOSS_GATE_SEED_BASELINE seed=" + seedLabel +
                    " samples=" + samples.ToString(CultureInfo.InvariantCulture) +
                    " homeShare=" + homeShare.ToString("F6", CultureInfo.InvariantCulture) +
                    " awayShare=" + awayShare.ToString("F6", CultureInfo.InvariantCulture));

                // PR #416 preregistration requires each seed independently to retain a meaningful
                // final-third population as well as satisfy both mirrored possession predicates.
                // A pooled assertion would allow one healthy seed to mask starvation of the other.
                context.Envelope.CheckTrue(
                    "final-third-sample-population-" + seedLabel,
                    samples >= MinimumSamplesBySeed[s],
                    "seed=" + seedLabel + " samples=" +
                    samples.ToString(CultureInfo.InvariantCulture) +
                    " (minimum " + MinimumSamplesBySeed[s].ToString(CultureInfo.InvariantCulture) +
                    "; 80% of corrected baseline)");

                context.Envelope.CheckTrue(
                    "final-third-play-is-somebodys-possession-home-view-" + seedLabel,
                    homeShare > 0.70f,
                    "seed=" + seedLabel + " homeShare=" + f3(homeShare) +
                    " (bound 0.70; pre-fix corpus ≈ 0.24)");

                context.Envelope.CheckTrue(
                    "final-third-play-is-somebodys-possession-away-view-" + seedLabel,
                    awayShare > 0.70f,
                    "seed=" + seedLabel + " awayShare=" + f3(awayShare) +
                    " (bound 0.70; pre-fix corpus ≈ 0.24)");
            }

            float pooledHomeShare = pooledSamples > 0
                ? (float)pooledHomeViewPossession / pooledSamples
                : 0f;
            float pooledAwayShare = pooledSamples > 0
                ? (float)pooledAwayViewPossession / pooledSamples
                : 0f;

            // Pooled values are diagnostic only under the preregistered rule. The former pooled
            // >=20,000 sample-count assertion was transitional and invalidated a healthy W2-active
            // capture even though both governed seeds independently satisfied the >0.70 criterion.
            Console.WriteLine(
                "INPOSS_GATE_POOLED_DIAGNOSTIC samples=" +
                pooledSamples.ToString(CultureInfo.InvariantCulture) +
                " homeShare=" + pooledHomeShare.ToString("F6", CultureInfo.InvariantCulture) +
                " awayShare=" + pooledAwayShare.ToString("F6", CultureInfo.InvariantCulture));
        }

        private static void PlayOne(
            ulong seed, ref int samples, ref int homeViewPossession, ref int awayViewPossession)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();
                if (tick % SampleStrideTicks != 0) continue;

                UnityEngine.Vector3 ballPos = engine.BallView.Position;

                // Is the ball in SOME team's attacking third? Depth is from the defended goal line.
                float depthHome = MatchEngineConstants.PITCH_LENGTH_M - ballPos.x; // team 0 attacks x = 105
                float depthAway = ballPos.x;                                        // team 1 attacks x = 0
                if (depthHome > FinalThirdDepthM && depthAway > FinalThirdDepthM) continue;

                samples++;
                if (IsPossessionPhase(engine.TestOnly_PositioningPhase(0))) homeViewPossession++;
                if (IsPossessionPhase(engine.TestOnly_PositioningPhase(1))) awayViewPossession++;
            }
        }

        /// <summary>A committed phase that says a team has the ball, as opposed to a transition.</summary>
        private static bool IsPossessionPhase(PositioningAI.Phase phase) =>
            phase == PositioningAI.Phase.InPoss || phase == PositioningAI.Phase.OutOfPoss;

        /// <summary>Position-coherent squad on the ConfigureSquads path — the §5.Z.20–§5.Z.23
        /// measurement recipe, identical to the sibling close-chance scenario so the two read the
        /// same corpus.</summary>
        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "scenario.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);

            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;

            return RosterGenerator.Generate(rng, stream, clubId, template);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.3     | 2026-09-17 | —      | PR #416 post-fix capture frozen: per-seed sample floors 12,664 |
// |         |            |        |   / 13,138 derived before freezing from 15,830 / 16,423 at 80%;|
// |         |            |        |   mirrored >0.70 checks remain independent; pooled diagnostic. |
// | 1.2     | 2026-09-16 | —      | PR #416 baseline-validity correction: each adversarial seed    |
// |         |            |        |   now owns its >0.70 mirrored assertions; pooled values are     |
// |         |            |        |   diagnostic only and the obsolete >=20,000 pooled floor is     |
// |         |            |        |   removed before numeric per-seed floors are frozen.            |
// | 1.1     | 2026-09-16 | —      | PR #416 preregistered corrected-baseline instrumentation:      |
// |         |            |        |   reports each existing adversarial seed independently before   |
// |         |            |        |   numeric non-vacuity floors are derived/frozen.                |
// | 1.0     | 2026-08-08 | —      | ERR-012-011 (wiring backlog C1): with the ball in the final     |
// |         |            |        |   third, #12 must commit a POSSESSION phase rather than a       |
// |         |            |        |   transition. Asserted from BOTH teams' mirrored snapshots.     |
// |         |            |        |   Pins no goal / shot / box / dribble figure — every shape     |
// |         |            |        |   metric moved the wrong way at this landing and pinning one   |
// |         |            |        |   would encode a regression as a contract.                     |
#endregion