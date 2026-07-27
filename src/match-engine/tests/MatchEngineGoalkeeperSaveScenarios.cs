// File:     src/match-engine/tests/MatchEngineGoalkeeperSaveScenarios.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.17 (acceptance);
//           goalkeeper-save-pipeline-design.md §5; Goalkeeper Mechanics #11 §3.1–§3.5;
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8;
//           Code Standards #20
// Purpose:  The acceptance scenario for the §5.Z.17 goalkeeper save-pipeline pass.
//
//           §5.Z.15 flipped Goalkeeper Mechanics #11 on, gave keepers locomotion, and recorded that what
//           remained was "the quality of the save". Measured, that premise was false in a stronger way
//           than it sounds: over three full matches the keepers made **zero** saves — not poor ones,
//           none. Every keeper dived (13–31 times a match) and the reach envelope never once contained
//           the ball, because the dive had no direction. "Save quality" was unmeasurable.
//
//           Nothing in the tree noticed, and that is the point of this file. The play-development
//           scenario proves the ball moves; the discipline scenario proves the referee is plausible;
//           the GK/Heading composition scenario proves the wiring runs and commits an intent. A match in
//           which the goalkeepers never touch the ball satisfies every predicate of all three.
//
//           PATH NOTE: this scenario runs the NEUTRAL path (no ConfigureSquads), while the measured
//           numbers published in §5.Z.17 came from the ConfigureSquads path, because that is the path a
//           league match takes. The predicates below are path-invariant — every one of the three
//           defects was geometric or arithmetic (a dive with no direction, a reaction window that no
//           caller ever opened, a third-of-pitch predicate read from the wrong end), none of them a
//           function of an attribute value — so the neutral path exercises them identically, and the
//           pre-fix failure evidence in the design note §5 was itself executed against THIS scenario.
//           What the neutral path does NOT cover is LineupSelector choosing the goalkeeper: a
//           regression that seeded the GK slot from the wrong record would be invisible here. That is
//           recorded as a named follow-up in the design note §7 rather than fixed by duplicating the
//           roster builder that already exists in GkSaveDiagnosticTests.
//
//           These predicates are STRUCTURAL, not a rate pin. They assert that each stage of the save
//           pipeline is reachable — the keeper is told about shots, wakes for the right end of the
//           pitch, dives, dives in a direction, and makes contact — because every one of those was
//           silently unreachable before §5.Z.17 and would be an invisible regression again. They
//           deliberately do NOT assert a save percentage or a goal rate: the goal rate is dominated by
//           the shot-side defects §5.Z.17 records as NOT fixed (no crossbar, no blocked shots), so
//           pinning one here would pin a number this pass did not earn and cannot defend.

using System;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the §5.Z.17 goalkeeper save acceptance scenario index (#19 §3.3.5 cross-spec layout;
    /// KD-8 ownership). Tier B — a save is a rare composed event, so it costs real match time.
    /// </summary>
    internal static class MatchEngineGoalkeeperSaveScenarios
    {
        public const string GoalkeeperSavesPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "match-engine-goalkeeper-saves";

        public const ulong GoalkeeperSaveSeed = 0x000000005A7E5A7EUL;

        /// <summary>
        /// 54 000 ticks @ 60 Hz = 15 minutes per seed; four seeds = one hour of composed play.
        /// Sized by measurement, not by symmetry with the discipline scenario: a keeper faces roughly
        /// 15–55 armed threats per 90 minutes and converts a small fraction of them into hand contact,
        /// so nine minutes per seed leaves the contact predicate riding on single-digit counts. An hour
        /// of play puts it on a large enough sample that the LOWER bound is meaningful rather than flaky.
        /// </summary>
        private const int NumTicks = 54000;

        private static readonly ulong[] Seeds =
        {
            GoalkeeperSaveSeed,
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        /// <summary>
        /// Ceiling on the share of a match a keeper may spend in Anticipate. Anticipate is a coiled,
        /// committed posture entered when the ball threatens; before ERR-011-002 the predicate that
        /// drove it was inverted AND the state had no exit but a dive, so keepers measured 76–92%.
        /// A generous 40% fails that by a wide margin while leaving room for a busy match.
        /// </summary>
        private const float MaxAnticipateShare = 0.40f;

        // Owning specs: the save path runs through #11 (the state machine, dive kinematics and handling
        // model), #6 (the shot whose execution opens the reaction window), #2 (the keeper's locomotion
        // to its slot), #12 (the slot itself), #16 (determinism) and #19 (the harness).
        private static readonly int[] OwningSpecIds = { 2, 6, 11, 12, 16, 19 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                name: "match-engine-goalkeeper-saves",
                owningSpecIds: OwningSpecIds,
                seed: GoalkeeperSaveSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            var scenario = new ClosedLoopScenario(manifest, RunGoalkeeperSaves);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(GoalkeeperSavesPath, manifest, scenario),
            });
        }

        /// <summary>
        /// Plays four independent matches and records the save funnel across them. Each keeper is a
        /// sample, so the corpus is eight keeper-matches.
        /// </summary>
        private static void RunGoalkeeperSaves(ScenarioContext context)
        {
            int totalContacts = 0;
            int totalDirectedDives = 0;
            int totalAirborneEntries = 0;
            int totalShotsNotified = 0;
            int seedsWithAnyDive = 0;

            for (int s = 0; s < Seeds.Length; s++)
            {
                Funnel[] f = PlayOne(Seeds[s]);

                bool anyDiveThisSeed = false;
                for (int t = 0; t < f.Length; t++)
                {
                    totalContacts += f[t].Contacts;
                    totalDirectedDives += f[t].DirectedDives;
                    totalAirborneEntries += f[t].AirborneEntries;
                    totalShotsNotified += f[t].ShotsNotified;
                    anyDiveThisSeed |= f[t].AirborneEntries > 0;

                    // Per-seed, per-keeper: the keeper must not live in Anticipate. Asserted per keeper
                    // rather than aggregated because ERR-011-002 was a per-side predicate defect — an
                    // average over both keepers could hide one side being wrong (the ERR-008-002 class).
                    float share = (float)f[t].AnticipateTicks / NumTicks;
                    context.Envelope.CheckTrue(
                        "keeper-does-not-live-in-anticipate-seed" + s.ToString() + "-gk" + t.ToString(),
                        share <= MaxAnticipateShare,
                        "anticipateShare=" + share.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                }

                if (anyDiveThisSeed)
                {
                    seedsWithAnyDive++;
                }
            }

            // The keeper is told a shot has been struck (ERR-011-004). Before it, OnShotExecutedEvent
            // had no production caller at all, so this was exactly zero.
            context.Envelope.CheckTrue("keeper-is-notified-of-shots", totalShotsNotified > 0,
                "shotsNotified=" + totalShotsNotified.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Dives are launched at all. Asserted over the CORPUS, not per seed: a save is a rare
            // composed event, and measurement showed one of these four fifteen-minute windows contains
            // no armed threat at either goal — a quiet spell is legitimate football, not a defect, and
            // a per-seed floor would make this test flaky for the wrong reason. (This is the opposite
            // call from the discipline scenario's per-seed rule, and deliberately so: there the risk was
            // one abandoned match averaging away, here it is a small count made noisy by bad luck.)
            context.Envelope.CheckTrue("dives-are-launched", totalAirborneEntries > 0,
                "airborneEntries=" + totalAirborneEntries.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " across " + seedsWithAnyDive.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " of " + Seeds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " seeds");

            // Dives are DIRECTED (ERR-011-003). Pre-fix the lateral direction was identically 0 on
            // every dive ever launched, so this was 0 out of hundreds of airborne frames.
            context.Envelope.CheckTrue("dives-are-directed", totalDirectedDives > 0,
                "directedDives=" + totalDirectedDives.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " of airborneEntries=" + totalAirborneEntries.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // The headline: the keeper's hands reach the ball. Aggregated across the whole corpus
            // rather than per seed — unlike the discipline scenario's per-seed rule, where one
            // abandoned match must not average away, here the count is small and the failure mode is
            // a floor of zero, which aggregation detects and per-seed bounds would only make flaky.
            context.Envelope.CheckTrue("keeper-makes-hand-contact", totalContacts > 0,
                "contacts=" + totalContacts.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " over " + Seeds.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " matches (pre-§5.Z.17 this was 0 over three FULL matches)");
        }

        /// <summary>Plays one match and returns the per-keeper funnel.</summary>
        private static Funnel[] PlayOne(ulong seed)
        {
            var engine = new MatchEngine(seed);

            var f = new Funnel[GoalkeeperMechanics.GoalkeeperConstants.MaxGkAgents];
            for (int t = 0; t < f.Length; t++)
            {
                f[t] = new Funnel();
            }

            var prevState = new GoalkeeperMechanics.GoalkeeperState[f.Length];
            var prevContactFrame = new int[f.Length];
            var lastShotMs = new float[f.Length];
            for (int t = 0; t < f.Length; t++)
            {
                prevState[t] = GoalkeeperMechanics.GoalkeeperState.Resting;
                prevContactFrame[t] = -1;
                lastShotMs[t] = -1f;
            }

            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();

                GoalkeeperMechanics.GoalkeeperTickState gk = engine.TestOnly_GoalkeeperState;

                for (int t = 0; t < f.Length; t++)
                {
                    GoalkeeperMechanics.GoalkeeperState s = gk.States[t];

                    if (s == GoalkeeperMechanics.GoalkeeperState.Anticipate)
                    {
                        f[t].AnticipateTicks++;
                    }

                    if (s != prevState[t])
                    {
                        if (s == GoalkeeperMechanics.GoalkeeperState.Airborne)
                        {
                            f[t].AirborneEntries++;

                            // The dive direction is latched at launch, so read it on the entry frame.
                            if (Math.Abs(gk.DiveDirectionLateral[t]) > 0f)
                            {
                                f[t].DirectedDives++;
                            }
                        }
                        prevState[t] = s;
                    }

                    int cf = gk.ContactStates[t].ActualContactFrame;
                    if (cf != prevContactFrame[t] && cf >= 0)
                    {
                        f[t].Contacts++;
                        prevContactFrame[t] = cf;
                    }

                    float shotMs = gk.ShotDetectedTickMs[t];
                    if (shotMs > 0f && shotMs != lastShotMs[t])
                    {
                        f[t].ShotsNotified++;
                        lastShotMs[t] = shotMs;
                    }
                }
            }

            return f;
        }

        private sealed class Funnel
        {
            public int AnticipateTicks;
            public int AirborneEntries;
            public int DirectedDives;
            public int Contacts;
            public int ShotsNotified;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. The §5.Z.17 acceptance scenario. Locks the save        |
// |         |            |        | pipeline's reachability stage by stage — keeper notified of      |
// |         |            |        | shots (ERR-011-004), does not live in Anticipate (ERR-011-002),  |
// |         |            |        | dives, dives DIRECTED (ERR-011-003), and makes hand contact.     |
// |         |            |        | All five predicates fail on the pre-fix engine, the contact and  |
// |         |            |        | direction ones at exactly zero. Deliberately asserts no save     |
// |         |            |        | percentage and no goal rate: the goal rate is dominated by the   |
// |         |            |        | shot-side defects §5.Z.17 records as NOT fixed, so a band here   |
// |         |            |        | would pin a number this pass did not earn.                       |
#endregion
