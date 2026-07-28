// File:     src/match-engine/tests/MatchEngineKeeperConversionScenarios.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.20 (acceptance);
//           gk-catch-parry-conversion-design.md §5; Goalkeeper Mechanics #11 §3.2 / §3.5;
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8;
//           Code Standards #20
// Purpose:  The acceptance scenario for the gk-catch-parry-conversion pass.
//
//           §5.Z.19 gave shots football pace and goals per shot ROSE to 0.38–0.42, because the
//           keeper contacted shots but converted almost none of the contacts: the §3.2.3 reaction
//           window the §3.5.1 quality blend consumes was re-evaluated per frame, so the value at
//           contact was dated by the ball's whole flight time — and the detection stamp was never
//           cleared, so most dives were dated against shots struck MINUTES earlier (measured mean
//           elapsed-when-airborne 85–349 s). Contact-time windows measured 0.000 and quality
//           0.29–0.60 against a catch threshold of 0.78: one catch in three full matches.
//
//           The §5.Z.17 goalkeeper-saves scenario asserts the save pipeline is REACHABLE
//           (notified → dives → directed → contact); it deliberately says nothing about what a
//           contact CONVERTS to, which is exactly the surface this pass fixes. These predicates
//           lock the conversion's reachability the same structural way: the frozen-at-launch
//           window is alive at contact, and at least one contact converts to the parry band and
//           to a held ball. No save percentage and no goal rate is pinned (rates stay owned by
//           the measured design-doc table, not by a scenario that would flake on the next
//           upstream change).
//
//           PATH NOTE: the ConfigureSquads path (the MatchEngineShotSpeedScenarios BuildSquad
//           recipe), NOT the neutral path — the same call that scenario's AR-4 made, for the
//           same measured reason: the neutral all-10 path samples a DIFFERENT shot population,
//           and the catch conversion calibrated on the configured path did not transfer (a
//           neutral-path draft of this scenario measured heldBalls = 0 over the same corpus
//           while the configured funnel measured 6 catches in 3 full matches). The window and
//           parry-band predicates passed on both paths; the hold predicate is the one that is
//           population-sensitive, so the scenario runs the path the league takes and the
//           calibration was measured on.

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the gk-catch-parry-conversion acceptance scenario index (#19 §3.3.5 cross-spec
    /// layout; KD-8 ownership). Tier B — a converted save is a rare composed event.
    /// </summary>
    internal static class MatchEngineKeeperConversionScenarios
    {
        public const string KeeperConversionPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-keeper-conversion";

        public const ulong KeeperConversionSeed = 0x0F1E2D3C4B5A6978UL;

        /// <summary>
        /// 162 000 ticks @ 60 Hz = 45 minutes per seed, two seeds = one match-equivalent of
        /// composed play. Sized from the funnel instrument's MEASURED per-contact tick
        /// positions, not by symmetry with the sibling scenarios: contacts run ~5 per full match
        /// and the earliest in any calibration seed lands at 16.6 minutes, so a 15-minute-window
        /// corpus (the first draft) contained zero contacts and its floor predicates were
        /// dishonest. The first 45 minutes of these two seeds contain 6 contacts and 3 catches
        /// (0x0F1E… at 26.5/40.8 min q = 0.920/1.000; 0x5EED… at 16.6/19.7/26.0/38.0 min,
        /// q = 0.838 at 38.0), so every floor sits on a deterministic non-trivial count.
        /// </summary>
        private const int NumTicks = 162000;

        private static readonly ulong[] Seeds =
        {
            KeeperConversionSeed,
            0x5EED000000000003UL,
        };

        /// <summary>
        /// Floor on the mean frozen reaction window over all launched dives. Pre-fix the
        /// contact-consumed window measured 0.000 across three full matches (stale stamps +
        /// per-frame re-anchoring), so any healthy positive mean discriminates; 0.15 leaves room
        /// for a corpus of genuinely late dives while failing the pre-fix engine by an order of
        /// magnitude.
        /// </summary>
        private const float MinMeanDiveWindow = 0.15f;

        // The conversion path runs through #11 (window + handling), #6 (the shot), #2/#12 (the
        // keeper's position), #16 (determinism), #19 (the harness).
        private static readonly int[] OwningSpecIds = { 2, 6, 11, 12, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-keeper-conversion",
                owningSpecIds: OwningSpecIds,
                seed: KeeperConversionSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunKeeperConversion);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(KeeperConversionPath, manifest, scenario),
            });
        }

        private static void RunKeeperConversion(ScenarioContext context)
        {
            int dives = 0;
            float diveWindowSum = 0f;
            int contacts = 0;
            int contactsAtParryOrBetter = 0;
            int heldBalls = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                PlayOne(Seeds[s], ref dives, ref diveWindowSum,
                        ref contacts, ref contactsAtParryOrBetter, ref heldBalls);
            }

            float meanDiveWindow = dives > 0 ? diveWindowSum / dives : 0f;

            // The frozen-at-launch window is ALIVE (ERR-011-005 + ERR-011-006). Pre-fix this
            // mean was ~0: stale stamps dated dives against minutes-old shots and the per-frame
            // anchor decayed whatever remained by the flight time.
            context.Envelope.CheckTrue("dive-reaction-window-is-live",
                dives > 0 && meanDiveWindow >= MinMeanDiveWindow,
                "meanDiveWindow=" + meanDiveWindow.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                + " over dives=" + dives.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // A contact CONVERTS: at least one reaches the parry band or better. Pre-fix,
            // contact quality capped in the deflect/spill bands for all but lucky noise draws.
            context.Envelope.CheckTrue("contact-converts-to-parry-band",
                contactsAtParryOrBetter > 0,
                "parryOrBetter=" + contactsAtParryOrBetter.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " of contacts=" + contacts.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // The keeper HOLDS a ball (HandsOnBall — quality >= CatchThreshold). One catch
            // occurred in three FULL pre-fix matches, none in a corpus of this size.
            context.Envelope.CheckTrue("keeper-holds-a-ball",
                heldBalls > 0,
                "heldBalls=" + heldBalls.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " over " + Seeds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " x 45-min windows (measured corpus: 3)");
        }

        private static void PlayOne(
            ulong seed, ref int dives, ref float diveWindowSum,
            ref int contacts, ref int contactsAtParryOrBetter, ref int heldBalls)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int gkCount = GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents;
            var prevState = new GoalkeeperMechanics.GoalkeeperState[gkCount];
            var prevContactFrame = new int[gkCount];
            for (int t = 0; t < gkCount; t++)
            {
                prevState[t] = GoalkeeperMechanics.GoalkeeperState.Resting;
                prevContactFrame[t] = -1;
            }

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;

                for (int t = 0; t < gkCount; t++)
                {
                    GoalkeeperMechanics.GoalkeeperState s = gk.States[t];

                    if (s != prevState[t])
                    {
                        if (s == GoalkeeperMechanics.GoalkeeperState.Airborne)
                        {
                            // The window is frozen at launch, so the Airborne entry frame reads
                            // the value the contact will consume.
                            dives++;
                            diveWindowSum += gk.ContactStates[t].ReactionWindowAchieved;
                        }

                        if (s == GoalkeeperMechanics.GoalkeeperState.HandsOnBall)
                        {
                            heldBalls++;
                        }

                        prevState[t] = s;
                    }

                    int cf = gk.ContactStates[t].ActualContactFrame;
                    if (cf != prevContactFrame[t] && cf >= 0)
                    {
                        contacts++;
                        if (gk.ContactStates[t].HandlingQualityScalar
                            >= GoalkeeperMechanics.GoalkeeperConstants.ParryThreshold)
                        {
                            contactsAtParryOrBetter++;
                        }
                        prevContactFrame[t] = cf;
                    }
                }
            }
        }

        /// <summary>Position-coherent squad on the ConfigureSquads path — the
        /// MatchEngineShotSpeedScenarios / ShotOutcomeDiagnosticTests recipe (a position template
        /// so LineupSelector can field a back four).</summary>
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
// | 1.0     | 2026-07-28 | —      | Initial. gk-catch-parry-conversion acceptance: the frozen dive     |
// |         |            |        | window is alive (ERR-011-005/006), a contact converts to the       |
// |         |            |        | parry band, and the keeper holds a ball — the three quantities     |
// |         |            |        | that were structurally ~0 pre-fix. No rate pins.                   |
#endregion
