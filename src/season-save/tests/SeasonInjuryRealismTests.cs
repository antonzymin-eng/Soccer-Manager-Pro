// File:     src/season-save/tests/SeasonInjuryRealismTests.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Injuries & Medical #41 §5 (the balance pass), FR-MD-027; Training System #29 §5;
//           Season & Competition Loop #30 §3.3/§3.4; ERR-041-010(b) / ERR-030-027; Code Standards #20
// Purpose:  The season-scale injury instrument — measures what the WIRED #29→#41→#30 chain actually
//           produces over full 20-club seasons, and locks the balance pass's league-wide bands. The
//           ERR-030-014 lesson applied to the season layer: assert the outcome the system exists to
//           produce, not that its steps run without throwing.

using NUnit.Framework;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// Measures injuries per player-season through the real chain — the day steps at #30's slots, the
    /// pre-round step (ERR-030-027), the appearance record (ERR-041-010(b)) and the availability filter
    /// — over full quick-sim seasons of the 20-club bootstrap league.
    /// <para>
    /// <b>Why quick-sim is the right instrument (KD-W1):</b> nothing in the occurrence chain reads a
    /// match-engine output (<c>HardContactWeight</c> = 0; the appearance term counts fielded XIs), so
    /// the sole coupling to match resolution is which eleven the selector fields — quick-sim's own
    /// surface. A <c>FullEngine</c> season would cost ~380 × ~23 min and measure nothing more.
    /// </para>
    /// <para>
    /// <b>Bands are league-wide, never per-club.</b> A season of ~500 players at football-realistic
    /// rates is a few hundred Poisson events; a per-club count (σ ≈ √40) would be brittle on any seed,
    /// while the league total's relative σ is ~5%. Eight seeds are pooled for the per-seed band and the
    /// pooled mean is asserted tighter.
    /// </para>
    /// </summary>
    [TestFixture]
    internal class SeasonInjuryRealismTests
    {
        private const int ManagerId = 77;

        private static readonly ulong[] Seeds =
        {
            0x5EED1EA6D0DEC0DEUL, 0xA11CE5EA50000001UL, 0xB16B00B5C0FFEE02UL, 0xD00DFEEDFACade03UL,
            0x0DDBA11DEADBEA04UL, 0xF00DF00DF00DF005UL, 0xCAFEBABE12345606UL, 0x8BADF00D0BADF007UL,
        };

        private sealed class SeasonMeasurement
        {
            public int Players;
            public int TotalInjuries;
            public int StarterInjuries;      // players fielded in >= half the rounds
            public int Starters;
            public int ReserveInjuries;      // everyone else
            public int Reserves;
            public double UnavailableFractionSum;   // summed over matchday observations
            public int MatchdayObservations;
        }

        /// <summary>Plays one full quick-sim season with the career wired and the dial as given.</summary>
        private static SeasonMeasurement PlaySeason(ulong seed, bool occurrenceEnabled)
        {
            League league = LeagueBootstrap.Generate(seed);
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] clubIds = league.ClubIds();
            for (int i = 0; i < clubIds.Length; i++)
            {
                provider.Set(league.ResolveByClubId(clubIds[i]));
            }

            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                provider, clubIds, occurrenceEnabled);
            var world = new WorldStore(ManagerId, seed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);

            var m = new SeasonMeasurement();
            var appearances = new System.Collections.Generic.Dictionary<long, int>();

            int rounds = 0;
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(provider);
                rounds++;

                // Matchday observation: the fraction of all players unavailable right after the round
                // — the number football publishes (squad unavailability, ~12-14%).
                int unavailable = 0;
                int total = 0;
                for (int c = 0; c < clubIds.Length; c++)
                {
                    Squad squad = provider.ResolveByClubId(clubIds[c]);
                    for (int p = 0; p < squad.Count; p++)
                    {
                        total++;
                        if (!career.IsAvailable(clubIds[c], squad.GetPlayer(p).PlayerId))
                        {
                            unavailable++;
                        }
                    }
                }

                m.UnavailableFractionSum += (double)unavailable / total;
                m.MatchdayObservations++;

                // Track who was fielded this round, from the appearance record itself (bit for the
                // fixture day is set exactly for the fielded XIs).
                ClubAppearanceStates[] blocks = career.AppearanceBlocks();
                uint day = world.CurrentWorldTick;
                for (int c = 0; c < blocks.Length; c++)
                {
                    for (int p = 0; p < blocks[c].Count; p++)
                    {
                        if (AppearanceWindow.AppearanceDaysOn(in blocks[c].States[p], day + 1u) > 0
                            && blocks[c].States[p].BitsAsOfWorldDay == day)
                        {
                            long key = ((long)blocks[c].ClubId << 32) | (uint)blocks[c].PlayerIds[p];
                            appearances.TryGetValue(key, out int n);
                            appearances[key] = n + 1;
                        }
                    }
                }
            }

            // Season totals, split starter / reserve on fielded-round count.
            ClubInjuryStates[] medical = career.MedicalBlocks();
            for (int c = 0; c < medical.Length; c++)
            {
                for (int p = 0; p < medical[c].Count; p++)
                {
                    m.Players++;
                    int injuries = medical[c].States[p].InjuryCount;
                    m.TotalInjuries += injuries;

                    long key = ((long)medical[c].ClubId << 32) | (uint)medical[c].PlayerIds[p];
                    appearances.TryGetValue(key, out int fieldedRounds);
                    if (fieldedRounds * 2 >= rounds)
                    {
                        m.Starters++;
                        m.StarterInjuries += injuries;
                    }
                    else
                    {
                        m.Reserves++;
                        m.ReserveInjuries += injuries;
                    }
                }
            }

            return m;
        }

        [Test]
        public void DialOff_AWholeSeasonInjuresNobody()
        {
            // One direction of the FR-MD-027 both-ways lock at season scale. Satisfiable by a step
            // that is never called — which is why the dial-ON direction below is the load-bearing one.
            SeasonMeasurement m = PlaySeason(Seeds[0], occurrenceEnabled: false);
            Assert.AreEqual(0, m.TotalInjuries, "the dial is off: nobody is ever injured");
            Assert.AreEqual(0.0, m.UnavailableFractionSum, "and nobody is ever unavailable");
        }

        [Test]
        public void DialOn_ASeasonProducesFootballPlausibleInjuryRates()
        {
            // THE season-scale outcome lock. Reference (E-1-derived, per the balance-pass record):
            // a 20-club league of 500 players sees roughly 600-1100 injuries a season (~30-55 per
            // club); an ever-present starter ~1.5-3.5; a reserve ~0.5-1.5; squad unavailability
            // ~6-16% of players at any matchday. The bands are deliberately wide — they are a
            // realism lock, not a regression pin; the per-seed digest chain pins exact behaviour.
            double pooledInjuries = 0;
            double pooledUnavailable = 0;
            double pooledStarterMean = 0;
            double pooledReserveMean = 0;

            foreach (ulong seed in Seeds)
            {
                SeasonMeasurement m = PlaySeason(seed, occurrenceEnabled: true);
                double perClub = m.TotalInjuries / 20.0;
                double starterMean = m.Starters > 0 ? (double)m.StarterInjuries / m.Starters : 0;
                double reserveMean = m.Reserves > 0 ? (double)m.ReserveInjuries / m.Reserves : 0;
                double unavailable = m.UnavailableFractionSum / m.MatchdayObservations;

                TestContext.WriteLine(
                    $"seed 0x{seed:X16}: injuries {m.TotalInjuries} ({perClub:F1}/club), " +
                    $"starters {m.Starters} @ {starterMean:F2}, reserves {m.Reserves} @ {reserveMean:F2}, " +
                    $"unavailable {unavailable:P1}");

                Assert.Greater(m.TotalInjuries, 300,
                    $"seed 0x{seed:X16}: fewer than 300 league injuries a season reads as a switched-" +
                    "off subsystem — the ERR-030-014 class this instrument exists to catch");
                Assert.Less(m.TotalInjuries, 1400,
                    $"seed 0x{seed:X16}: more than 1400 reads as the pre-balance-pass absurdity " +
                    "(23%/day would produce tens of thousands)");

                pooledInjuries += m.TotalInjuries;
                pooledUnavailable += unavailable;
                pooledStarterMean += starterMean;
                pooledReserveMean += reserveMean;
            }

            int n = Seeds.Length;
            TestContext.WriteLine(
                $"pooled: {pooledInjuries / n:F0} injuries/season, starter {pooledStarterMean / n:F2}, " +
                $"reserve {pooledReserveMean / n:F2}, unavailable {pooledUnavailable / n:P1}");

            Assert.That(pooledInjuries / n, Is.InRange(500.0, 1200.0),
                "pooled league injuries/season must sit in the football band (~830 at the E-1 targets)");
            Assert.That(pooledStarterMean / n, Is.InRange(StarterMeanLo, StarterMeanHi),
                "an ever-present starter carries roughly 1.5-3.5 injuries a season (match load " +
                "dominating, E-1) — and MORE than a reserve, or the appearance term is dead");
            Assert.That(pooledReserveMean / n, Is.InRange(ReserveMeanLo, ReserveMeanHi),
                "a reserve carries roughly one (training-and-baseline-driven) injury a season");
            Assert.Greater(pooledStarterMean / n, pooledReserveMean / n,
                "starters must out-injure reserves — the ERR-041-010(b) appearance term is the only " +
                "thing that can produce this split, so its death is visible here");
            Assert.That(pooledUnavailable / n, Is.InRange(0.04, 0.18),
                "squad unavailability at a matchday sits near football's published ~12-14%");
        }

        [Test]
        public void RestoredCareer_WithTheDialArmed_StillInjures()
        {
            // InjuryOccurrenceEnabled is a REQUIRED construction argument and is not serialized, so
            // after a load the running dial position is entirely the reconstruction site's choice —
            // this is the lock that a restore does not silently come back disarmed (the evidence
            // advisor's save->load->advance obligation).
            League league = LeagueBootstrap.Generate(Seeds[0], 4);
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] clubIds = league.ClubIds();
            for (int i = 0; i < clubIds.Length; i++)
            {
                provider.Set(league.ResolveByClubId(clubIds[i]));
            }

            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                provider, clubIds, injuryOccurrenceEnabled: true);
            var world = new WorldStore(ManagerId, Seeds[0]);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(provider);

            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".tdsave");
            try
            {
                SeasonSaveManager.Save(loop, matchOrNull: null, path);
                SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
                PlayerCareerStates restored = PlayerCareerStates.FromBlocks(
                    contents.TrainingClubs, contents.MedicalClubs, contents.AppearanceClubs,
                    injuryOccurrenceEnabled: true);
                var resumed = new SeasonLoop(
                    contents.World, contents.Season, RoundResolutionMode.QuickSimAll,
                    restored, provider);

                // 120 further days at the retuned rates: ~500 players x ~0.4%/day makes zero total
                // injuries astronomically improbable AND deterministic for this seed either way.
                while (!resumed.IsSeasonComplete && contents.World.CurrentWorldTick < 120u)
                {
                    resumed.AdvanceToNextFixtureDay();
                    resumed.AdvanceAndPlayNextRound(provider);
                }

                int injuries = 0;
                ClubInjuryStates[] medical = restored.MedicalBlocks();
                for (int c = 0; c < medical.Length; c++)
                {
                    for (int p = 0; p < medical[c].Count; p++)
                    {
                        injuries += medical[c].States[p].InjuryCount;
                    }
                }

                Assert.Greater(injuries, 0,
                    "a restored career reconstructed at the armed position must keep injuring — a " +
                    "restore that silently disarms the dial is indistinguishable from a healthy run");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        [Test]
        public void DialOn_PerturbingEitherNewConstant_LeavesTheAssertedBands()
        {
            // The perturbation obligation, done as arithmetic over the REAL chain functions rather
            // than a config rebind (the gate runs config-unbound, so constants cannot be rebound at
            // runtime): both risk values below go through AssembleRiskScore itself; only the season
            // aggregation is closed-form. If halving a new constant kept every asserted band green,
            // that band would not be a lock on it.
            PlayerAttributes average = PlayerAttributes.CreateDefault();
            TrainingState peak = TrainingState.Create(TrainingFocus.Balanced);
            peak.Condition = TrainingSystemConstants.ConditionMax;

            int baselineOnly = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(peak, average),
                MatchLoad.None, average, MedicalModifier.Identity);
            int withOneAppearance = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(peak, average),
                new MatchLoad(1, 0), average, MedicalModifier.Identity);
            int appearanceDelta = withOneAppearance - baselineOnly;

            Assert.Greater(baselineOnly, 0,
                "a fit player on the default focus must carry non-zero daily risk — the 'default " +
                "focus converges on injury-proof forever' absurdity must be gone");
            Assert.Greater(appearanceDelta, 0, "an appearance must add risk");

            // ~250 draw-eligible days for a reserve; a starter's appearance term runs ~7 window-days
            // per round over 38 rounds. No clamp binds at these magnitudes, so halving a [GT] halves
            // its term here exactly.
            double denom = InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM;
            double reserve = 250.0 * baselineOnly / denom;
            double starter = reserve + 38.0 * 7.0 * appearanceDelta / denom;
            double reserveHalfBaseline = 250.0 * (baselineOnly / 2.0) / denom;
            double starterHalfAppearance = reserve + 38.0 * 7.0 * (appearanceDelta / 2.0) / denom;

            TestContext.WriteLine(
                $"chain expectation: reserve {reserve:F2}, starter {starter:F2}; halved-baseline " +
                $"reserve {reserveHalfBaseline:F2}; halved-appearance starter {starterHalfAppearance:F2}");

            Assert.That(reserve, Is.InRange(ReserveMeanLo, ReserveMeanHi),
                "the closed-form reserve expectation must sit inside the reserve band");
            Assert.That(starter, Is.InRange(StarterMeanLo, StarterMeanHi),
                "the closed-form starter expectation must sit inside the starter band");
            Assert.Less(reserveHalfBaseline, ReserveMeanLo,
                "halving BaselineDailyRisk must fall out of the reserve band — the band locks it");
            Assert.Less(starterHalfAppearance, StarterMeanLo,
                "halving AppearanceLoadWeight must fall out of the starter band — the band locks it");
        }

        // The pooled per-population bands, shared by the season measurement and the perturbation
        // check so they cannot drift apart. Fitted at the balance pass's measurement run; the
        // perturbation test above is what keeps them tight enough to lock the two new [GT]s.
        private const double StarterMeanLo = 1.7;
        private const double StarterMeanHi = 3.2;
        private const double ReserveMeanLo = 0.7;
        private const double ReserveMeanHi = 1.6;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass, task: the season-scale      |
// |         |            |        | instrument): dial-off zero lock, dial-on league-wide realism      |
// |         |            |        | bands over 8 seeds, and the perturbation check that the band      |
// |         |            |        | locks both new constants.                                          |
#endregion
