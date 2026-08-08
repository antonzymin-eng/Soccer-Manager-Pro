// File:     src/season-save/tests/SeasonLoopScenarios.cs
// Created:  2026-07-26
// Modified: 2026-08-08 (retired the coincidence-prone managed-fixture-differs-from-the-model predicate; see the FR-SN-013b note)
// Author:   —
// Spec:     Season & Competition Loop #30 §5.7 (the season-multi-fixture capstone), §3.3/§3.4,
//           FR-SN-012/013b/026/030; Testing Strategy & Framework #19 §3.3.1/§3.3.3/§3.3.5/Appendix A.1/KD-8;
//           League Bootstrap design supplement (the league under test); path-to-playable A4; Code Standards #20
// Purpose:  The #30 T2 capstone: a whole season driven head-lessly through the composed root, plus the ONE
//           predicate the unit suite cannot afford — that a managed fixture really does run through a real
//           90-minute MatchEngine while its round-mate resolves through the model.
//
// RUNTIME: this scenario boots one real MatchEngine match (~2 minutes of compute, roadmap C1). That is the
// deliberate home for it: the Simulation layer is where the project already accepts multi-second-to-minutes
// composed runs (the match-engine kickoff capstone precedent), and it keeps a full 90-minute simulation out
// of the unit suite while still being gate-enforced on every push.

using System;

using TacticalDirector.LivingWorld;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// Builds the season-loop scenario index (#19 §3.3.5 cross-spec layout; KD-8 ownership). Tier B
    /// (Simulation layer).
    /// </summary>
    internal static class SeasonLoopScenarios
    {
        public const string MultiFixturePath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "season-multi-fixture";

        public const ulong MultiFixtureSeed = 0x5EA50117A0DEC0DEUL;

        /// <summary>Clubs in the scenario league: 4 ⇒ 2 fixtures per round, of which exactly one is the
        /// managed club's, so a single round exercises BOTH resolution paths side by side.</summary>
        private const int ClubCount = 4;

        /// <summary>The human's club.</summary>
        private const int ManagedClubId = 0;

        private const int ManagerId = 1;

        // Owning specs: the predicates are load-bearing on the Season Loop (#30), Deterministic Simulation
        // (#16) for the keyed draws and the engine's own tick pipeline, Living World (#22) for the world
        // clock the day-advance drives, Squad/Player Data (#27) for the rosters and lineup selection, and
        // Testing Strategy (#19) for the harness. The engine-played fixture additionally exercises the whole
        // match-engine spec set; those are not listed because no predicate here asserts their behaviour
        // directly — the scenario asserts that a real match RAN and produced the season's scoreline.
        private static readonly int[] OwningSpecIds = { 16, 19, 22, 27, 30 };

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "season-multi-fixture",
                owningSpecIds: OwningSpecIds,
                seed: MultiFixtureSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);

            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    MultiFixturePath,
                    manifest,
                    new ClosedLoopScenario(manifest, MultiFixture)),
            });
        }

        private static void MultiFixture(ScenarioContext context)
        {
            League league = LeagueBootstrap.Generate(context.RunSeed, ClubCount);

            // ── (a) + (c): a whole season head-lessly, and the KD-8 no-fixture-day floor ──────────
            SeasonRun quick = RunQuickSeason(context.RunSeed, league);

            context.Envelope.CheckEquals(
                "season-fixtures-resolved", quick.Outcomes, ClubCount * (ClubCount - 1));
            context.Envelope.CheckTrue(
                "season-complete", quick.Complete, "the loop did not reach the end of the schedule");
            context.Envelope.CheckTrue(
                "table-reflects-every-result", quick.TableBalances,
                "the table's played/goal totals do not reconcile with the emitted results: "
                    + quick.TableDiagnostic);
            context.Envelope.CheckTrue(
                "no-fixture-days-are-behaviour-neutral", quick.FloorHolds,
                "advancing a no-fixture day through the loop diverged from a bare WorldStore.AdvanceDay "
                    + "(FR-SN-026 / KD-8)");

            // ── (b) two-run same-seed determinism over the full season state ──────────────────────
            SeasonRun replay = RunQuickSeason(context.RunSeed, league);
            context.Envelope.CheckTrue(
                "determinism-two-run-season-blob", BytesEqual(quick.SeasonBlob, replay.SeasonBlob),
                "two same-seed seasons produced different season blobs");
            context.Envelope.CheckTrue(
                "determinism-two-run-world-blob", BytesEqual(quick.WorldBlob, replay.WorldBlob),
                "two same-seed seasons left the world in different states");

            // ── FR-SN-013b: the managed fixture really goes through a real MatchEngine ────────────
            //
            // One round in ManagedThroughEngine mode resolves TWO fixtures: the managed club's and its
            // round-mate. Three predicates pin the split, and between them nothing about the round can be
            // explained by a single producer:
            //   * the engine path executed exactly once (EnginePlayedFixtures — a real observable, not an
            //     inference), while the whole quick-sim season above executed it zero times;
            //   * the round-mate's scoreline equals RoundResolutionModel's prediction EXACTLY, so the model
            //     path is live and correctly keyed.
            //
            // RETIRED July 26, 2026 — a third predicate asserted that the managed fixture's scoreline does
            // NOT equal the model's prediction, as a proxy for "it did not come from the model". The
            // authoring comment already recorded its residual (it false-passes when a real match happens to
            // reproduce the model's exact scoreline), and it has now false-FAILED for exactly that reason:
            // once §5.Z Phase H made matches actually score, agreement on a low scoreline stopped being
            // unlikely and became ordinary. A predicate whose failure rate is set by coincidence rather
            // than by correctness is worse than no predicate — it trains the reader to re-seed around it.
            //
            // What is given up, stated rather than glossed: the retired proxy was the only check on
            // "the engine ran, the counter incremented, and the table then took the model's number
            // anyway". Nothing cheap covers that — SeasonLoop deliberately exposes no engine MatchResult
            // (ERR-030-013: the producer record is loop-scoped), and proving it exactly would mean
            // replaying the managed fixture through a second real MatchEngine, doubling this scenario's
            // ~3.4-minute cost. EnginePlayedFixtures remains a REAL observable, not an inference, and it
            // is what makes the routing split conclusive: a regression that quick-simmed everything
            // leaves it at zero, and one that engine-played everything leaves the round-mate's scoreline
            // no longer matching the model.
            EngineRoundObservations engineRound = RunOneEngineRound(context.RunSeed, league);

            context.Envelope.CheckEquals(
                "quicksim-season-booted-no-engine", quick.EnginePlayedFixtures, 0);
            context.Envelope.CheckEquals(
                "engine-path-ran-exactly-once", engineRound.EnginePlayedFixtures, 1);
            context.Envelope.CheckTrue(
                "managed-fixture-found", engineRound.ManagedFound,
                "the managed club had no fixture in round 0");
            context.Envelope.CheckTrue(
                "unmanaged-fixture-found", engineRound.UnmanagedFound,
                "round 0 had no non-managed fixture, so the routing split is untestable");
            context.Envelope.CheckTrue(
                "unmanaged-fixture-matches-the-model", engineRound.UnmanagedMatchesModel,
                "a non-managed fixture's scoreline did not match RoundResolutionModel's prediction");
            context.Envelope.CheckTrue(
                "active-match-cleared", engineRound.ActiveMatchCleared,
                "SeasonLoop.ActiveMatch was left non-null after the round completed");
            context.Envelope.CheckTrue(
                "world-clock-frozen-during-the-match", engineRound.WorldDayUnchanged,
                "the world day advanced while a match was being played — the world and match clocks must "
                    + "stay disjoint (FR-SN-025)");
            context.Envelope.CheckTrue(
                "engine-injury-changed-the-eleven", engineRound.InjuryChangedTheEleven,
                "precondition: the injured starter's absence must change WHICH eleven is selected, "
                    + "or the two predicates below cannot distinguish a live filter from a dead one");
            context.Envelope.CheckTrue(
                "engine-fixture-records-the-filtered-eleven",
                engineRound.EngineFixtureRecordedTheFilteredEleven,
                "the ENGINE-resolved fixture's appearance record does not match the filtered "
                    + "selector's eleven (ERR-041-010(b) on the FR-SN-013b engine path — the branch "
                    + "no cheaper suite executes)");
            context.Envelope.CheckTrue(
                "engine-fixture-excludes-the-injured-starter",
                engineRound.InjuredStarterUnrecorded,
                "an injured starter carries an appearance for an engine-resolved fixture he did not "
                    + "play (the availability filter did not participate in the recorded identity)");
        }

        /// <summary>
        /// Plays a whole season in <see cref="RoundResolutionMode.QuickSimAll"/>, checking the KD-8 floor on
        /// every no-fixture day against a bare <see cref="WorldStore"/> advanced in lockstep.
        /// </summary>
        private static SeasonRun RunQuickSeason(ulong seed, League league)
        {
            var world = new WorldStore(ManagerId, seed);
            var bare = new WorldStore(ManagerId, seed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(ManagedClubId), RoundResolutionMode.QuickSimAll);

            var run = new SeasonRun();
            int goalsEmitted = 0;

            while (!loop.IsSeasonComplete)
            {
                // Walk to the fixture day one day at a time so each no-fixture day can be compared against
                // the bare world — this is the FR-SN-026 floor asserted per day, not just at the end.
                uint target = loop.State.Calendar.NextFixtureDay();
                while (loop.CurrentWorldDay < target)
                {
                    loop.AdvanceDays(1);
                    bare.AdvanceDay();
                    if (run.FloorHolds && !BytesEqual(world.Snapshot(), bare.Snapshot()))
                    {
                        run.FloorHolds = false;
                    }
                }

                MatchResult[] results = loop.AdvanceAndPlayNextRound(league);
                foreach (MatchResult r in results)
                {
                    goalsEmitted += r.HomeGoals + r.AwayGoals;
                }
            }

            run.Complete = loop.IsSeasonComplete;
            run.Outcomes = loop.MatchOutcomes.Count;

            int played = 0;
            int goalsFor = 0;
            int goalsAgainst = 0;
            foreach (LeagueTableRow row in loop.State.TableRowsInClubIdOrder())
            {
                played += row.Played;
                goalsFor += row.GoalsFor;
                goalsAgainst += row.GoalsAgainst;
            }

            run.TableBalances =
                played == 2 * loop.MatchOutcomes.Count
                && goalsFor == goalsEmitted
                && goalsAgainst == goalsEmitted;
            run.TableDiagnostic =
                $"played={played} goalsFor={goalsFor} goalsAgainst={goalsAgainst} emitted={goalsEmitted} "
                + $"outcomes={loop.MatchOutcomes.Count}";

            run.EnginePlayedFixtures = loop.EnginePlayedFixtures;
            run.SeasonBlob = loop.Snapshot();
            run.WorldBlob = world.Snapshot();
            return run;
        }

        /// <summary>
        /// Plays exactly round 0 in <see cref="RoundResolutionMode.ManagedThroughEngine"/> and compares both
        /// fixtures against the model's predictions.
        /// </summary>
        private static EngineRoundObservations RunOneEngineRound(ulong seed, League league)
        {
            var world = new WorldStore(ManagerId, seed);
            SeasonState season = league.CreateSeason(ManagedClubId);

            // The career is wired HERE and nowhere cheaper (AR pass 4 M2): every other career test
            // runs QuickSimAll, and every engine-mode career test stops at BootFixtureEngine — so the
            // id plumbing BootFixtureEngine → PlayThroughEngine → ResolveFixture →
            // RecordFixtureAppearances had never executed on the engine path (the T2 AR pass-5
            // High's shape, one layer out). This scenario already pays for the one real match, so
            // the appearance record's engine branch is asserted on it, with an injured starter so
            // the filter PARTICIPATES in the recorded identity.
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                league, league.ClubIds(), injuryOccurrenceEnabled: false);
            int[] fitManagedXi = TacticalDirector.MatchEngine.SquadRating.StartingElevenPlayerIds(
                league.ResolveByClubId(ManagedClubId));
            int injuredStarterId = fitManagedXi[0];
            var injuredStarter = InjuriesMedical.InjuryState.Create();
            injuredStarter.Severity = InjuriesMedical.InjurySeverity.Moderate;
            injuredStarter.RecoveryRemaining = 12;
            career.SetMedicalState(ManagedClubId, injuredStarterId, in injuredStarter);

            var loop = new SeasonLoop(
                world, season, RoundResolutionMode.ManagedThroughEngine, career, league);

            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = loop.CurrentWorldDay;

            var obs = new EngineRoundObservations();
            MatchResult[] results = loop.AdvanceAndPlayNextRound(league);

            int[] expectedManagedXi = TacticalDirector.MatchEngine.SquadRating.StartingElevenPlayerIds(
                career.SelectAvailable(league.ResolveByClubId(ManagedClubId)));

            // Precondition (AR pass 5 L7): the injury must have CHANGED the eleven, or the two
            // predicates below are satisfied by a filter that did nothing — the same guard the
            // sibling suite carries. Sensitive to the pre-round day-step count only through
            // RecoveryRemaining = 12 outliving the days to the first fixture; if the calendar
            // moves, this fails loudly here rather than as a confusing exclusion failure.
            bool injuryChangedTheEleven = false;
            for (int i = 0; i < fitManagedXi.Length && !injuryChangedTheEleven; i++)
            {
                injuryChangedTheEleven = System.Array.IndexOf(expectedManagedXi, fitManagedXi[i]) < 0;
            }

            obs.InjuryChangedTheEleven = injuryChangedTheEleven;
            var recordedManagedXi = new System.Collections.Generic.List<int>();
            bool injuredStarterRecorded = false;
            ClubAppearanceStates[] blocks = career.AppearanceBlocks();
            for (int c = 0; c < blocks.Length; c++)
            {
                if (blocks[c].ClubId != ManagedClubId)
                {
                    continue;
                }

                for (int i = 0; i < blocks[c].Count; i++)
                {
                    if (AppearanceWindow.AppearanceDaysOn(in blocks[c].States[i], fixtureDay + 1u) == 1)
                    {
                        recordedManagedXi.Add(blocks[c].PlayerIds[i]);
                        injuredStarterRecorded |= blocks[c].PlayerIds[i] == injuredStarterId;
                    }
                }
            }

            recordedManagedXi.Sort();
            var expectedSorted = new System.Collections.Generic.List<int>(expectedManagedXi);
            expectedSorted.Sort();
            obs.EngineFixtureRecordedTheFilteredEleven =
                recordedManagedXi.Count == expectedSorted.Count;
            for (int i = 0; obs.EngineFixtureRecordedTheFilteredEleven && i < expectedSorted.Count; i++)
            {
                obs.EngineFixtureRecordedTheFilteredEleven = recordedManagedXi[i] == expectedSorted[i];
            }

            obs.InjuredStarterUnrecorded = !injuredStarterRecorded;

            obs.WorldDayUnchanged = loop.CurrentWorldDay == fixtureDay;
            obs.ActiveMatchCleared = loop.ActiveMatch == null;

            foreach (MatchResult r in results)
            {
                var fixture = new Fixture(r.RoundIndex, r.HomeClubId, r.AwayClubId);
                // The prediction reads the UNFILTERED rosters while the loop rates filtered ones —
                // sound here only because the single injury above sits on the MANAGED club, whose
                // scoreline is deliberately not compared (AR pass 5 L7: stated, not assumed).
                MatchResult predicted = RoundResolutionModel.Resolve(
                    in fixture,
                    season.Seed,
                    season.SeasonNumber,
                    TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(
                        league.ResolveByClubId(r.HomeClubId)),
                    TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(
                        league.ResolveByClubId(r.AwayClubId)),
                    fixtureDay);

                if (fixture.Involves(ManagedClubId))
                {
                    obs.ManagedFound = true;
                }
                else
                {
                    // Only the round-mate is compared against the model — the managed fixture's own
                    // comparison was the retired coincidence-prone predicate (see the note above).
                    obs.UnmanagedFound = true;
                    obs.UnmanagedMatchesModel =
                        predicted.HomeGoals == r.HomeGoals && predicted.AwayGoals == r.AwayGoals;
                }
            }

            obs.EnginePlayedFixtures = loop.EnginePlayedFixtures;
            return obs;
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

        private sealed class SeasonRun
        {
            public bool Complete;
            public int Outcomes;
            public bool TableBalances;
            public string TableDiagnostic = string.Empty;
            public bool FloorHolds = true;
            public int EnginePlayedFixtures;
            public byte[] SeasonBlob = Array.Empty<byte>();
            public byte[] WorldBlob = Array.Empty<byte>();
        }

        private sealed class EngineRoundObservations
        {
            public bool ManagedFound;
            public bool UnmanagedFound;
            public bool UnmanagedMatchesModel;
            public int EnginePlayedFixtures;
            public bool ActiveMatchCleared;
            public bool WorldDayUnchanged;
            public bool EngineFixtureRecordedTheFilteredEleven;
            public bool InjuredStarterUnrecorded;
            public bool InjuryChangedTheEleven;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 §5.7 capstone): a full head-less season  |
// |         |            |        | with the per-day FR-SN-026 floor, two-run season+world blob          |
// |         |            |        | determinism, and the FR-SN-013b routing split proven inside one      |
// |         |            |        | round — the round-mate matching the model exactly while the managed  |
// |         |            |        | fixture, played through a real MatchEngine, does not.                |
// | 1.1     | 2026-07-26 | —      | Retired the `managed-fixture-differs-from-the-model` predicate. It asserted |
// |         |            |        |   that a real match's scoreline differs from RoundResolutionModel's        |
// |         |            |        |   prediction, as a proxy for engine routing. Its own authoring comment      |
// |         |            |        |   recorded the residual (false-passes on coincidental agreement), and once  |
// |         |            |        |   §5.Z Phase H made matches actually score it false-FAILED for exactly that |
// |         |            |        |   reason — agreement on a low scoreline is ordinary, not unlikely. A        |
// |         |            |        |   predicate whose failure rate is set by coincidence rather than by         |
// |         |            |        |   correctness trains readers to re-seed around it. EnginePlayedFixtures     |
// |         |            |        |   (a real counter) plus the round-mate's exact model match keep the routing |
// |         |            |        |   split conclusive; what is given up is documented at the call site.        |
// | 1.2     | 2026-08-08 | —      | Balance-pass AR pass 4 (M2): the engine round wires a CAREER — the |
// |         |            |        | appearance record's engine branch (BootFixtureEngine →            |
// |         |            |        | PlayThroughEngine → ResolveFixture → RecordFixtureAppearances)    |
// |         |            |        | had never executed anywhere; every career suite runs QuickSimAll  |
// |         |            |        | and every engine-mode career test stops at the boot. Two new      |
// |         |            |        | predicates assert the engine fixture records the FILTERED eleven  |
// |         |            |        | and excludes an injured starter, on the one real match this       |
// |         |            |        | scenario already pays for.                                        |
// | 1.3     | 2026-08-08 | —      | Balance-pass AR pass 5 (L7): + the injury-changed-the-eleven   |
// |         |            |        | precondition predicate (the sibling suite's guard), and the    |
// |         |            |        | round-mate model comparison states its unfiltered-roster       |
// |         |            |        | assumption instead of assuming it.                             |
#endregion
