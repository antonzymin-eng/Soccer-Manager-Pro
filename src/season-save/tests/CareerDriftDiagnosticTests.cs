// File:     src/season-save/tests/CareerDriftDiagnosticTests.cs
// Created:  2026-11-04
// Modified: 2026-11-04
// Author:   —
// Spec:     ERR-028-026 (open-issues `regen-club-strength`); League Bootstrap KD-5 (club strength ramp);
//           Player Progression #28 FR-PG-019 (1:1 regens); Season & Competition Loop #30 §3.4.1
//           (quick-sim rates squads by SquadRating.StartingElevenMean); Code Standards #20
// Purpose:  Measure how the league's club-strength ramp and regen quality evolve over many seasons of
//           the production career composition (CareerSession.StartNew's wiring, QuickSimAll rounds).
//           ERR-028-026's "about 18 seasons" erosion figure was estimated from constants; this replaces
//           the estimate with a measurement before D-13 decides the fix.
//
//           Env-gated because it plays seeds × seasons whole careers. It is a DIAGNOSTIC, not a rate
//           lock: only non-vacuity (regens arrive, the genesis ramp exists) is asserted.
//
//             TD_CAREER_DRIFT=1 dotnet test --filter CareerDriftDiagnostic
//
//           Optional smoke-run overrides: TD_CAREER_DRIFT_SEEDS (default 5), TD_CAREER_DRIFT_SEASONS
//           (default 20). The catalogued measurement uses the defaults.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal sealed class CareerDriftDiagnosticTests
    {
        private const int DEFAULT_SEEDS = 5;
        private const int DEFAULT_SEASONS = 20;
        private const int MANAGER_ID = 1;          // ClientAppConstants.CAREER_MANAGER_ID; not referenced (layering)
        private const int MANAGED_CLUB_INDEX = 0;  // which club the career manages; irrelevant under QuickSimAll
        private const int RAMP_GROUP = 5;          // top/bottom genesis clubs compared for the ramp gap
        private static readonly ulong[] SeedBase = { 0x5EED0001UL, 0x5EED0002UL, 0x5EED0003UL, 0x5EED0004UL, 0x5EED0005UL };

        // Age bands reported for CA (PlayerRecord.Age, whole years).
        private static readonly int[] BandMin = { 16, 19, 24, 30 };
        private static readonly int[] BandMax = { 18, 23, 29, 99 };
        private static readonly string[] BandName = { "16-18", "19-23", "24-29", "30+" };

        private sealed class SeasonSample
        {
            internal double RatingSd;
            internal double RatingRange;
            internal double RatingMean;
            internal double RampGap;          // mean rating of genesis top-5 minus genesis bottom-5
            internal double SpearmanGenesis;  // rank correlation of current ratings with genesis ratings
            internal double RegenShare;       // share of league players not present at genesis
            internal readonly double[] BandCa = new double[BandMin.Length];
            internal readonly int[] BandCount = new int[BandMin.Length];
            internal double RegenCaMean;      // mean CA of all non-genesis players
            internal int RegenCount;
        }

        [Test]
        [Category("Calibration")]
        public void CareerDriftDiagnostic_ReportsClubStrengthAndRegenQualityOverSeasons()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_CAREER_DRIFT")))
            {
                Assert.Ignore("Set TD_CAREER_DRIFT=1 to run the ERR-028-026 multi-season career drift measurement.");
            }

            int seeds = ReadOverride("TD_CAREER_DRIFT_SEEDS", DEFAULT_SEEDS, 1, SeedBase.Length);
            int seasons = ReadOverride("TD_CAREER_DRIFT_SEASONS", DEFAULT_SEASONS, 1, 100);

            // samples[seed][season], season 0 = genesis (before any round is played).
            var samples = new SeasonSample[seeds][];
            for (int s = 0; s < seeds; s++)
            {
                samples[s] = RunCareer(SeedBase[s], seasons);
            }

            var report = new StringBuilder();
            report.AppendLine("=== Career drift (ERR-028-026) ===");
            report.AppendLine(Invariant($"seeds={seeds} seasons={seasons} mode=QuickSimAll clubs={LeagueBootstrapConstants.DefaultClubCount} spread=±{LeagueBootstrapConstants.LeagueStrengthSpread}"));
            report.AppendLine("rating = SquadRating.StartingElevenMean (the quick-sim's input); values are means over seeds [min..max].");
            report.AppendLine("season | rating SD | ramp gap top5-bot5 | Spearman vs genesis | league mean | regen share | regen CA | CA 16-18 | CA 19-23 | CA 24-29 | CA 30+");
            for (int season = 0; season <= seasons; season++)
            {
                report.Append(Invariant($"{season,6} | "));
                report.Append(Stat(samples, season, x => x.RatingSd, "F3")).Append(" | ");
                report.Append(Stat(samples, season, x => x.RampGap, "F3")).Append(" | ");
                report.Append(Stat(samples, season, x => x.SpearmanGenesis, "F2")).Append(" | ");
                report.Append(Stat(samples, season, x => x.RatingMean, "F3")).Append(" | ");
                report.Append(Stat(samples, season, x => x.RegenShare, "F2")).Append(" | ");
                report.Append(Stat(samples, season, x => x.RegenCount > 0 ? x.RegenCaMean : double.NaN, "F0")).Append(" | ");
                for (int b = 0; b < BandMin.Length; b++)
                {
                    int band = b;
                    report.Append(Stat(samples, season, x => x.BandCount[band] > 0 ? x.BandCa[band] : double.NaN, "F0"));
                    report.Append(b + 1 < BandMin.Length ? " | " : "");
                }
                report.AppendLine();
            }

            TestContext.Out.Write(report.ToString());

            // Non-vacuity only: a genesis ramp exists to erode, and the boundary actually produced regens.
            for (int s = 0; s < seeds; s++)
            {
                Assert.Greater(samples[s][0].RampGap, 0.0, "Genesis must carry a strength ramp (KD-5).");
                Assert.Greater(samples[s][seasons].RegenCount, 0, "The season boundary must have produced regens.");
            }
        }

        private static SeasonSample[] RunCareer(ulong worldSeed, int seasons)
        {
            // CareerSession.StartNew's composition (client-app), restated here because season-save tests
            // cannot reference client-app. Any change there must be mirrored to keep this measurement real.
            League league = LeagueBootstrap.Generate(worldSeed);
            var world = new WorldStore(MANAGER_ID, worldSeed);
            int[] clubIds = league.ClubIds();
            var day0Squads = new Squad[clubIds.Length];
            for (int i = 0; i < clubIds.Length; i++)
            {
                day0Squads[i] = league.ResolveByClubId(clubIds[i]);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(day0Squads, world.CurrentWorldTick);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), clubIds, injuryOccurrenceEnabled: true);
            SeasonLoop loop = league.CreateLoop(
                world, clubIds[MANAGED_CLUB_INDEX], RoundResolutionMode.QuickSimAll, null, career, progression,
                new DisciplineState());

            var genesisIds = new HashSet<int>();
            var genesisRating = new double[clubIds.Length];
            for (int c = 0; c < clubIds.Length; c++)
            {
                Squad squad = progression.SquadFor(clubIds[c]);
                genesisRating[c] = SquadRating.StartingElevenMean(squad);
                for (int p = 0; p < squad.Count; p++)
                {
                    genesisIds.Add(squad.GetPlayer(p).PlayerId);
                }
            }

            int[] byGenesis = OrderByRatingDescending(genesisRating);
            var result = new SeasonSample[seasons + 1];
            result[0] = Sample(progression, clubIds, genesisIds, genesisRating, byGenesis);
            for (int season = 1; season <= seasons; season++)
            {
                while (!loop.IsSeasonComplete)
                {
                    loop.AdvanceToNextFixtureDay();
                    loop.AdvanceAndPlayNextRound();
                }

                loop.AdvanceDays(1);   // ERR-030-053: the final fixture day must be ticked before the roll
                loop.RollToNextSeason();
                result[season] = Sample(progression, clubIds, genesisIds, genesisRating, byGenesis);
            }

            return result;
        }

        private static SeasonSample Sample(
            ProgressionEngine progression, int[] clubIds, HashSet<int> genesisIds, double[] genesisRating, int[] byGenesis)
        {
            var sample = new SeasonSample();
            var rating = new double[clubIds.Length];
            double regenCaSum = 0;
            int players = 0;
            for (int c = 0; c < clubIds.Length; c++)
            {
                Squad squad = progression.SquadFor(clubIds[c]);
                rating[c] = SquadRating.StartingElevenMean(squad);
                for (int p = 0; p < squad.Count; p++)
                {
                    PlayerRecord record = squad.GetPlayer(p);
                    int ca = progression.LifecycleView(clubIds[c], record.PlayerId).CurrentAbility;
                    players++;
                    if (!genesisIds.Contains(record.PlayerId))
                    {
                        sample.RegenCount++;
                        regenCaSum += ca;
                    }

                    for (int b = 0; b < BandMin.Length; b++)
                    {
                        if (record.Age >= BandMin[b] && record.Age <= BandMax[b])
                        {
                            sample.BandCa[b] += ca;
                            sample.BandCount[b]++;
                        }
                    }
                }
            }

            for (int b = 0; b < BandMin.Length; b++)
            {
                if (sample.BandCount[b] > 0)
                {
                    sample.BandCa[b] /= sample.BandCount[b];
                }
            }

            sample.RegenCaMean = sample.RegenCount > 0 ? regenCaSum / sample.RegenCount : 0;
            sample.RegenShare = players > 0 ? (double)sample.RegenCount / players : 0;
            sample.RatingMean = Mean(rating);
            sample.RatingSd = StdDev(rating, sample.RatingMean);
            sample.RatingRange = Max(rating) - Min(rating);

            int group = Math.Min(RAMP_GROUP, clubIds.Length / 2);
            double top = 0, bottom = 0;
            for (int k = 0; k < group; k++)
            {
                top += rating[byGenesis[k]];
                bottom += rating[byGenesis[byGenesis.Length - 1 - k]];
            }

            sample.RampGap = (top - bottom) / group;
            sample.SpearmanGenesis = Spearman(genesisRating, rating);
            return sample;
        }

        // ── statistics ───────────────────────────────────────────────────────────────────

        private static int[] OrderByRatingDescending(double[] values)
        {
            var order = new int[values.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            // Ties broken by club index so the order is deterministic.
            Array.Sort(order, (a, b) => values[a] != values[b] ? values[b].CompareTo(values[a]) : a.CompareTo(b));
            return order;
        }

        private static double Spearman(double[] x, double[] y)
        {
            double[] rx = Ranks(x);
            double[] ry = Ranks(y);
            double mx = Mean(rx), my = Mean(ry), num = 0, dx = 0, dy = 0;
            for (int i = 0; i < x.Length; i++)
            {
                num += (rx[i] - mx) * (ry[i] - my);
                dx += (rx[i] - mx) * (rx[i] - mx);
                dy += (ry[i] - my) * (ry[i] - my);
            }

            return dx > 0 && dy > 0 ? num / Math.Sqrt(dx * dy) : double.NaN;
        }

        private static double[] Ranks(double[] values)
        {
            var order = new int[values.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (a, b) => values[a].CompareTo(values[b]));
            var ranks = new double[values.Length];
            int start = 0;
            while (start < order.Length)
            {
                int end = start;
                while (end + 1 < order.Length && values[order[end + 1]] == values[order[start]])
                {
                    end++;
                }

                double average = (start + end) / 2.0 + 1.0;   // ties share the average rank
                for (int k = start; k <= end; k++)
                {
                    ranks[order[k]] = average;
                }

                start = end + 1;
            }

            return ranks;
        }

        private static double Mean(double[] values)
        {
            double sum = 0;
            foreach (double v in values)
            {
                sum += v;
            }

            return values.Length > 0 ? sum / values.Length : 0;
        }

        private static double StdDev(double[] values, double mean)
        {
            double sum = 0;
            foreach (double v in values)
            {
                sum += (v - mean) * (v - mean);
            }

            return values.Length > 1 ? Math.Sqrt(sum / (values.Length - 1)) : 0;
        }

        private static double Max(double[] values)
        {
            double m = double.MinValue;
            foreach (double v in values)
            {
                m = Math.Max(m, v);
            }

            return m;
        }

        private static double Min(double[] values)
        {
            double m = double.MaxValue;
            foreach (double v in values)
            {
                m = Math.Min(m, v);
            }

            return m;
        }

        private static string Stat(SeasonSample[][] samples, int season, Func<SeasonSample, double> pick, string format)
        {
            double sum = 0, lo = double.MaxValue, hi = double.MinValue;
            int n = 0;
            for (int s = 0; s < samples.Length; s++)
            {
                double v = pick(samples[s][season]);
                if (double.IsNaN(v))
                {
                    continue;
                }

                sum += v;
                lo = Math.Min(lo, v);
                hi = Math.Max(hi, v);
                n++;
            }

            if (n == 0)
            {
                return "n/a";
            }

            return (sum / n).ToString(format, CultureInfo.InvariantCulture)
                + " [" + lo.ToString(format, CultureInfo.InvariantCulture)
                + ".." + hi.ToString(format, CultureInfo.InvariantCulture) + "]";
        }

        private static int ReadOverride(string name, int fallback, int min, int max)
        {
            string raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            int value = int.Parse(raw, CultureInfo.InvariantCulture);
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(name, value, Invariant($"Must be in [{min}, {max}]."));
            }

            return value;
        }

        private static string Invariant(FormattableString text) => FormattableString.Invariant(text);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-04 | —      | ERR-028-026 multi-season career drift measurement (club strength ramp, regen CA by age band). |
#endregion
