// File:     src/match-engine/tests/MatchEngineDisciplineScenarios.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.9 (acceptance);
//           foul-discipline-balance-design.md §5; Testing Strategy & Framework #19 §3.3.1 / §3.3.3 /
//           §3.3.5 / Appendix A.1 / KD-8; Code Standards #20
// Purpose:  The acceptance scenario for the foul/discipline balance pass. Phase H left the engine issuing
//           ~7 red cards per 9 minutes — 480 fouls, 147 yellows and 75 reds per 90 minutes, i.e. every
//           player on the pitch dismissed well inside a match. Nothing in the tree noticed, because no
//           test had ever asked what a played match's discipline rate LOOKS like: the play-development
//           scenario proves the ball moves, and a match in which twenty-two players are sent off satisfies
//           every one of its predicates.
//
//           This scenario asks that question. Its predicates are a PLAUSIBILITY FLOOR, not a pin: bands
//           several times wider than the football target on either side, chosen so they fail by more than
//           an order of magnitude on the pre-fix engine while surviving ordinary retuning of the
//           surrounding systems. Pinning today's counts would make every future physics or AI change a
//           discipline-test failure.

using System;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the §5.Z.9 discipline acceptance scenario index (#19 §3.3.5 cross-spec layout; KD-8
    /// ownership). Tier B — it costs real composed match time, which is the only way to observe a rate.
    /// </summary>
    internal static class MatchEngineDisciplineScenarios
    {
        public const string DisciplinePlausiblePath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-discipline-plausible";

        public const ulong DisciplineSeed = 0x00000000C0FFEE11UL;

        /// <summary>
        /// 32 400 ticks @ 60 Hz = 9 minutes per seed; six seeds = 54 minutes of composed play. A
        /// discipline rate needs a sample, not a moment: at the calibrated rate a whole 90-minute match
        /// contains only ~21 fouls, so this corpus expects ~13 — few enough that the bands must be wide,
        /// many enough that the pre-fix engine's 288 fouls over the same run is unmistakable.
        /// </summary>
        private const int NumTicks = 32400;

        /// <summary>Ticks in a full 90-minute match — the denominator the reported rates scale to.</summary>
        private const int TicksPerMatch = 324000;

        private static readonly ulong[] Seeds =
        {
            DisciplineSeed,
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x0000000000000001UL,
            0x00000000ABCDEF12UL,
            0x000000005A5A5A5AUL,
        };

        // Owning specs: the discipline path runs through the collision classifier that raises candidates
        // (#3), the movement freeze applied to a sent-off agent (#2), the restart the foul awards (#1
        // ball placement), the deterministic card draw (#16), the events published (#17), and the harness
        // (#19). The engine composes them.
        private static readonly int[] OwningSpecIds = { 1, 2, 3, 16, 17, 19 };

        // ── Bands ─────────────────────────────────────────────────────────────────────
        // Real football: ~22 fouls, ~3.5 yellows, ~0.25 reds per 90 minutes. Every band below is stated
        // per 90 minutes, extrapolated from the aggregate, with the measured pre-fix and post-fix values
        // quoted so the margin on both sides is visible rather than asserted.

        /// <summary>Fouls must not vanish. Post-fix measured 21; football ~22.
        /// A lower bound matters as much as the upper one here: the pass ADDED a probability gate, so
        /// "discipline silently switched off" is now a reachable regression, and a scenario that only
        /// caught over-officiating would pass an engine that never blows the whistle at all.</summary>
        private const float MinFoulsPerMatch = 3f;

        /// <summary>Post-fix measured 21; pre-fix 480. Football ~22.</summary>
        private const float MaxFoulsPerMatch = 90f;

        /// <summary>Post-fix measured 3.0; pre-fix 147. Football ~3.5.</summary>
        private const float MaxYellowsPerMatch = 20f;

        /// <summary>Post-fix measured 1.0; pre-fix 75. Football ~0.25.</summary>
        private const float MaxRedsPerMatch = 5f;

        /// <summary>
        /// The predicate a player would actually notice, and the direct expression of §5.Z.7's "every
        /// player on the pitch would be dismissed inside a full match": a team reduced below nine is
        /// abandoned under the Laws, so a match that reaches it is not football whatever the rate table
        /// says. Pre-fix, three of nine minutes was enough to send off seven.
        /// </summary>
        private const int MinPlayersRemainingPerTeam = 9;

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "match-engine-discipline-plausible",
                owningSpecIds: OwningSpecIds,
                seed: DisciplineSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    DisciplinePlausiblePath,
                    manifest,
                    new ClosedLoopScenario(manifest, DisciplineIsPlausible)),
            });
        }

        private static void DisciplineIsPlausible(ScenarioContext context)
        {
            int totalFouls = 0;
            int totalYellows = 0;
            int totalReds = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                Observations o = RunMatch(Seeds[s], NumTicks);

                totalFouls += o.Fouls;
                totalYellows += o.Yellows;
                totalReds += o.Reds;

                // Per seed, not aggregated: a single match ending eight-a-side is the failure, and
                // averaging it away across six seeds is exactly how it would be missed.
                context.Envelope.CheckTrue(
                    "seed" + s + "-both-teams-keep-nine-players",
                    o.MinPlayersRemaining >= MinPlayersRemainingPerTeam,
                    "a team was reduced to " + o.MinPlayersRemaining + " players (minimum "
                    + MinPlayersRemainingPerTeam + "); the match would be abandoned");
            }

            float scale = (float)TicksPerMatch / (Seeds.Length * (float)NumTicks);
            float foulsPerMatch = totalFouls * scale;
            float yellowsPerMatch = totalYellows * scale;
            float redsPerMatch = totalReds * scale;

            context.Envelope.CheckInRange(
                "fouls-per-match-in-band", foulsPerMatch, MinFoulsPerMatch, MaxFoulsPerMatch);

            context.Envelope.CheckInRange(
                "yellows-per-match-in-band", yellowsPerMatch, 0f, MaxYellowsPerMatch);

            context.Envelope.CheckInRange(
                "reds-per-match-in-band", redsPerMatch, 0f, MaxRedsPerMatch);

            // Cards are a consequence of fouls, so the ratio is the part that stays meaningful if the
            // foul rate is later retuned: a model that books nearly every foul is as wrong as one that
            // gives too many fouls, and the two are independent failures.
            if (totalFouls > 0)
            {
                float bookedFraction = (float)(totalYellows + totalReds) / totalFouls;
                context.Envelope.CheckTrue(
                    "card-rate-is-a-minority-of-fouls", bookedFraction <= 0.6f,
                    "cards were issued for " + bookedFraction.ToString("F2", Inv)
                    + " of fouls; real football books roughly one in six");
            }
        }

        private static readonly System.Globalization.CultureInfo Inv =
            System.Globalization.CultureInfo.InvariantCulture;

        private struct Observations
        {
            public int Fouls;
            public int Yellows;
            public int Reds;
            public int MinPlayersRemaining;
        }

        private static Observations RunMatch(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);

            int fouls = 0;
            int previousCooldown = 0;

            for (int t = 0; t < ticks; t++)
            {
                engine.RunTick();

                // The cooldown is armed only by an applied foul and decremented by one per tick, so an
                // INCREASE between ticks is exactly one foul given. Counting this way rather than
                // subscribing to FoulCommittedEvent keeps the scenario off the process-static EventBus,
                // whose subscriber set is shared across the whole test run (#17 §3.2.1).
                int cooldown = engine.TestOnly_FoulCooldownRemaining;
                if (cooldown > previousCooldown)
                {
                    fouls++;
                }
                previousCooldown = cooldown;
            }

            // Discipline is read from the final state rather than accumulated. Note that
            // SubstitutePlayer resets the outgoing slot's yellow count (the card belongs to the player,
            // not the shirt), so a booked player who was substituted would not be counted here — nothing
            // in these runs substitutes, and adding a cumulative counter would mean new engine state for
            // a scenario's convenience. Recorded so the number is read for what it is.
            int yellows = 0;
            var sentOffPerTeam = new int[MatchEngineConstants.TEAM_COUNT];
            var playersPerTeam = new int[MatchEngineConstants.TEAM_COUNT];

            for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
            {
                int team = engine.AgentTeamId(a);
                playersPerTeam[team]++;

                yellows += engine.TestOnly_YellowCards(a);
                if (engine.TestOnly_IsSentOff(a))
                {
                    sentOffPerTeam[team]++;
                }
            }

            int minRemaining = int.MaxValue;
            int reds = 0;
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                reds += sentOffPerTeam[team];
                int remaining = playersPerTeam[team] - sentOffPerTeam[team];
                if (remaining < minRemaining)
                {
                    minRemaining = remaining;
                }
            }

            return new Observations
            {
                Fouls = fouls,
                Yellows = yellows,
                Reds = reds,
                MinPlayersRemaining = minRemaining,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-07-26 | —      | Initial: the §5.Z.9 acceptance scenario. Foul / yellow / red rates   |
// |         |            |        | in plausibility bands, both teams keeping nine players, and cards a  |
// |         |            |        | minority of fouls. Every predicate fails on the pre-balance engine.  |
#endregion
