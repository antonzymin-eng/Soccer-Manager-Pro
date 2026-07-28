// File:     src/match-engine/tests/MatchEngineKeeperContactScenarios.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.3.6 (ERR-011-007), Positioning AI #12 §3.3.3
//           (ERR-012-010), Testing Strategy & Framework #19 §3.3.1/§3.3.5/Appendix A.1,
//           Code Standards #20
// Purpose:  Acceptance scenario for the gk-contact-rate pass (gk-contact-rate-design.md §5):
//           over a composed ConfigureSquads corpus, the keeper HOLDS a committed dive until the
//           ball arrives (the ERR-011-007 gate observably fired), contacted threat episodes
//           outnumber un-contacted goal-plane crossings, and no crossed episode shows the deep
//           dive-early miss (the envelope over hundreds of ms before the ball) that dominated
//           the pre-fix baseline (9 of 15 crossed episodes, over by 456–2000 ms).

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the gk-contact-rate acceptance scenario index (#19 §3.3.5 cross-spec layout;
    /// KD-8 ownership). Tier B — contact-rate structure over a bounded composed corpus.
    /// </summary>
    internal static class MatchEngineKeeperContactScenarios
    {
        public const string KeeperContactPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-keeper-contact";

        public const ulong KeeperContactSeed = 0x00000000D1A6D05EUL;

        /// <summary>
        /// 162 000 ticks @ 60 Hz = 45 minutes per seed, two seeds = one match-equivalent —
        /// the MatchEngineKeeperConversionScenarios sizing rationale (threat episodes are rare
        /// composed events; a short window starves the floors). The corpus deliberately uses the
        /// two anatomy-instrument seeds with the busiest keeper traffic (0xD1A6D05E measured 6
        /// resolved episodes post-fix in 90 min per keeper; 0x5EED… 4 for gk1).
        /// </summary>
        private const int NumTicks = 162000;

        private static readonly ulong[] Seeds =
        {
            KeeperContactSeed,
            0x5EED000000000003UL,
        };

        /// <summary>
        /// A crossed episode counts as a DEEP dive-early miss when the dive resolved more than
        /// this long before the ball reached the goal plane. Post-fix residual dive-earlies
        /// measure 100–150 ms (quantisation-scale); pre-fix they measured 456–2000 ms, so 350 ms
        /// separates the populations with margin on both sides.
        /// </summary>
        private const float DeepDiveEarlyMs = 350f;

        // #11 (the gate), #12 (the slot), #2 (keeper locomotion), #16 (determinism), #19 (harness).
        private static readonly int[] OwningSpecIds = { 2, 11, 12, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-keeper-contact",
                owningSpecIds: OwningSpecIds,
                seed: KeeperContactSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunKeeperContact);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(KeeperContactPath, manifest, scenario),
            });
        }

        private static void RunKeeperContact(ScenarioContext context)
        {
            int contacts = 0;
            int crossedUncontacted = 0;
            int deepDiveEarly = 0;
            int heldCommits = 0;      // Anticipate + live SaveIntent sustained >= 2 tactical ticks
            int resolvedEpisodes = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                PlayOne(Seeds[s], ref contacts, ref crossedUncontacted,
                        ref deepDiveEarly, ref heldCommits, ref resolvedEpisodes);
            }

            string inv(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Non-vacuity: the corpus actually produced save situations.
            context.Envelope.CheckTrue("threat-episodes-resolve",
                resolvedEpisodes >= 4,
                "resolvedEpisodes=" + inv(resolvedEpisodes));

            // The ERR-011-007 hold is ALIVE: a keeper with a committed SaveIntent stays coiled in
            // Anticipate across tactical ticks until the ball is inside the commit lead. Pre-fix
            // this state is unreachable for more than one tick — hasSaveIntent forced Diving at
            // the very next tactical evaluation.
            context.Envelope.CheckTrue("committed-dive-is-held-until-arrival",
                heldCommits > 0,
                "heldCommits=" + inv(heldCommits));

            // The contact rate: contacted episodes outnumber un-contacted goal-plane crossings.
            // Post-fix full-match corpus measured 26 vs 7; pre-fix 8 vs 15 (inverted).
            context.Envelope.CheckTrue("contacts-outnumber-uncontacted-crossings",
                contacts > crossedUncontacted,
                "contacts=" + inv(contacts) + " crossedUncontacted=" + inv(crossedUncontacted));

            // The dive-early miss class is gone at depth: no crossed episode's dive resolved more
            // than DeepDiveEarlyMs before the ball arrived (pre-fix: over by 456-2000 ms).
            context.Envelope.CheckTrue("no-deep-dive-early-miss",
                deepDiveEarly == 0,
                "deepDiveEarly=" + inv(deepDiveEarly) + " (bound " +
                DeepDiveEarlyMs.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) + " ms)");
        }

        private static void PlayOne(
            ulong seed, ref int contacts, ref int crossedUncontacted,
            ref int deepDiveEarly, ref int heldCommits, ref int resolvedEpisodes)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int gkCount = GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents;
            var prevContactFrame = new int[gkCount];
            var live = new bool[gkCount];
            var episodeContacted = new bool[gkCount];
            var launchTick = new int[gkCount];
            var durTicks = new int[gkCount];
            var prevAirborne = new bool[gkCount];
            var holdTicks = new int[gkCount];       // consecutive tactical ticks in Anticipate+intent
            var prevTacticalTick = new ulong[gkCount];
            UnityEngine.Vector3 prevBall = engine.BallView.Position;

            for (int t = 0; t < gkCount; t++)
            {
                prevContactFrame[t] = -1;
                launchTick[t] = -1;
            }

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;
                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                UnityEngine.Vector3 ballVel = engine.BallView.Velocity;
                bool loose = engine.PossessingAgentId < 0;

                for (int t = 0; t < gkCount; t++)
                {
                    float goalX = t == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
                    float distToGoalLine = Math.Abs(ballPos.x - goalX);
                    float towardGoal = (goalX - ballPos.x) * ballVel.x;
                    float planarSpeed = new UnityEngine.Vector2(ballVel.x, ballVel.y).magnitude;
                    bool armed = loose
                                 && distToGoalLine <= MatchEngineConstants.GkSaveTriggerRangeM
                                 && towardGoal > 0f
                                 && planarSpeed >= MatchEngineConstants.GkSaveTriggerMinBallSpeedMps;

                    // The hold observable: Anticipate with a live SaveIntent, sampled once per
                    // tactical tick. Two consecutive tactical ticks in that state are impossible
                    // pre-fix (the next tactical evaluation forced Diving).
                    ulong tacticalTick = engine.CurrentTick / (ulong)DeterministicSimConstants.AI_PHASE_STRIDE;
                    if (tacticalTick != prevTacticalTick[t])
                    {
                        prevTacticalTick[t] = tacticalTick;
                        bool holding = gk.SaveIntentActive[t]
                                       && gk.States[t] == GoalkeeperMechanics.GoalkeeperState.Anticipate;
                        holdTicks[t] = holding ? holdTicks[t] + 1 : 0;
                        if (holdTicks[t] == 2)
                        {
                            heldCommits++;
                        }
                    }

                    if (!live[t] && armed)
                    {
                        live[t] = true;
                        episodeContacted[t] = false;
                        launchTick[t] = -1;
                    }

                    if (live[t])
                    {
                        bool airborne = gk.States[t] == GoalkeeperMechanics.GoalkeeperState.Airborne;
                        if (airborne && !prevAirborne[t])
                        {
                            launchTick[t] = tick;
                            durTicks[t] = gk.DiveDurationFrames[t];
                        }
                        prevAirborne[t] = airborne;

                        int cf = gk.ContactStates[t].ActualContactFrame;
                        if (cf != prevContactFrame[t] && cf >= 0)
                        {
                            episodeContacted[t] = true;
                            prevContactFrame[t] = cf;
                        }

                        bool crossed = t == 0
                            ? prevBall.x > 0f && ballPos.x <= 0f
                            : prevBall.x < MatchEngineConstants.PITCH_LENGTH_M
                              && ballPos.x >= MatchEngineConstants.PITCH_LENGTH_M;

                        if (crossed || (!armed && episodeContacted[t]))
                        {
                            resolvedEpisodes++;
                            if (episodeContacted[t])
                            {
                                contacts++;
                            }
                            else
                            {
                                crossedUncontacted++;
                                if (launchTick[t] >= 0)
                                {
                                    int landTick = launchTick[t] + durTicks[t];
                                    float earlyByMs = (tick - landTick) * (1000f / 60f);
                                    if (earlyByMs > DeepDiveEarlyMs)
                                    {
                                        deepDiveEarly++;
                                    }
                                }
                            }
                            live[t] = false;
                        }
                        else if (!armed)
                        {
                            live[t] = false;    // faded — not a resolved save situation
                        }
                    }
                }

                prevBall = ballPos;
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the
        /// MatchEngineKeeperConversionScenarios recipe.</summary>
        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "diagnostic.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);

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
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. gk-contact-rate acceptance: the ERR-011-007 hold is alive |
// |         |            |        | (Anticipate + live intent across >= 2 tactical ticks), contacted   |
// |         |            |        | episodes outnumber un-contacted crossings, and the deep dive-early |
// |         |            |        | miss class (over by > 350 ms) is gone. No rate pins.               |
#endregion
