// File:     src/season-save/tests/RoundResolutionCalibrationHarness.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     League Bootstrap design supplement KD-8 (calibration methodology); Season & Competition Loop
//           #30 §3.4.1; path-to-playable roadmap A4a (+ C1a's compute budget); Code Standards #20
// Purpose:  The roadmap-A4a corpus generator: boot a REAL MatchEngine over two squads shifted to a chosen
//           strength differential, tick to full time, and record (dSquad, homeGoals, awayGoals, matchSeed).
//           This is the ground truth the round-resolution model is fitted against — a model that is
//           internally consistent and empirically wrong passes every unit test and makes the league table
//           feel wrong, which is the whole reason this exists.
//
// HARNESS PLACEMENT (KD-8, corrected during A3's review): this belongs in the SEASON-SAVE test assembly,
// not beside MatchEngineCapstonePerfHarness in match-engine's. Building a controlled rating differential
// needs LeagueBootstrap.ApplyStrength, which is `internal` to season-save; this assembly can reach that
// AND boot a real engine, while the match-engine test assembly can do neither for season-save.
//
// COST (roadmap C1a): one row is one full 90-minute match — roughly two minutes of compute. A full corpus
// is hours, parallelisable across PROCESSES only (the match engine's EventBus is process-static, so two
// engines cannot tick concurrently in one process). Run it, do not discover it.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// One corpus row: everything observable at capture time.
    /// <para>
    /// <b><c>DSquad</c>, never <c>edge</c> (KD-8 / AR-7 H-1).</b> The axis is the MEASURED
    /// <c>Rating(home) − Rating(away)</c>. <c>edge</c> adds <c>HomeAdvantageRating</c>, which is a
    /// parameter this corpus exists to <i>fit</i> — it does not exist until after the fit, and the engine
    /// knows nothing about it, so a harness asked to record it could not.
    /// </para>
    /// </summary>
    internal readonly struct CalibrationRow
    {
        public readonly float DSquad;
        public readonly int HomeGoals;
        public readonly int AwayGoals;
        public readonly ulong MatchSeed;
        public readonly int HomeDelta;
        public readonly int AwayDelta;
        public readonly int HomeBaseClubId;
        public readonly int AwayBaseClubId;

        public CalibrationRow(
            float dSquad, int homeGoals, int awayGoals, ulong matchSeed,
            int homeDelta, int awayDelta, int homeBaseClubId, int awayBaseClubId)
        {
            DSquad = dSquad;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            MatchSeed = matchSeed;
            HomeDelta = homeDelta;
            AwayDelta = awayDelta;
            HomeBaseClubId = homeBaseClubId;
            AwayBaseClubId = awayBaseClubId;
        }

        /// <summary>CSV, one row per match — the raw form the artifact records and the fit reads.</summary>
        public string ToCsv() => string.Join(",", new[]
        {
            DSquad.ToString("R", CultureInfo.InvariantCulture),
            HomeGoals.ToString(CultureInfo.InvariantCulture),
            AwayGoals.ToString(CultureInfo.InvariantCulture),
            MatchSeed.ToString(CultureInfo.InvariantCulture),
            HomeDelta.ToString(CultureInfo.InvariantCulture),
            AwayDelta.ToString(CultureInfo.InvariantCulture),
            HomeBaseClubId.ToString(CultureInfo.InvariantCulture),
            AwayBaseClubId.ToString(CultureInfo.InvariantCulture),
        });

        public static string CsvHeader =>
            "dSquad,homeGoals,awayGoals,matchSeed,homeDelta,awayDelta,homeBaseClubId,awayBaseClubId";
    }

    /// <summary>
    /// One planned match: which base rosters to pair, how far to shift each, and the seed to boot from.
    /// Pure data — nothing here runs an engine, which is what makes the planning logic cheaply testable.
    /// </summary>
    internal readonly struct CalibrationPlanEntry
    {
        public readonly int HomeRosterIndex;
        public readonly int AwayRosterIndex;
        public readonly int HomeDelta;
        public readonly int AwayDelta;
        public readonly ulong MatchSeed;

        public CalibrationPlanEntry(
            int homeRosterIndex, int awayRosterIndex, int homeDelta, int awayDelta, ulong matchSeed)
        {
            HomeRosterIndex = homeRosterIndex;
            AwayRosterIndex = awayRosterIndex;
            HomeDelta = homeDelta;
            AwayDelta = awayDelta;
            MatchSeed = matchSeed;
        }
    }

    /// <summary>
    /// Generates A4a's engine-simulated corpus (league-bootstrap KD-8).
    /// <para>
    /// <b>Pairs are built by direct shift, not by picking league clubs.</b> The harness applies an
    /// ARBITRARY <c>int</c> delta to a fixed base-roster pair, so the reachable grid is not capped at
    /// <c>2·LeagueStrengthSpread</c> — and it does not degenerate into re-seeding the same handful of
    /// ordered pairs at the extremes, which would measure seed variance rather than rating response.
    /// </para>
    /// </summary>
    internal static class RoundResolutionCalibrationHarness
    {
        /// <summary>Domain constant for per-match seeds — keeps corpus seeds off every production stream.</summary>
        private const ulong CalibrationSeedDomain = 0x3F19A7C4E80B2D65UL;

        /// <summary>The seed the base rosters are generated from. Pinned so the corpus is reproducible.</summary>
        internal const ulong BaseRosterSeed = 0x0CA11B8A7E5EED01UL;

        /// <summary>
        /// How many distinct base rosters the harness draws on. Several pairs per bucket keeps a bucket's
        /// mean from being one roster pair's idiosyncrasy.
        /// </summary>
        internal const int BaseRosterCount = 6;

        /// <summary>
        /// Generates the <see cref="BaseRosterCount"/> pre-strength base rosters — position-coherent by the
        /// KD-6 template, so every one of them can field the Stage-0 formation.
        /// </summary>
        internal static Squad[] BuildBaseRosters()
        {
            PlayerPosition[] template = LeagueBootstrapConstants.BuildSquadPositionTemplate();
            var rng = new DeterministicRngService(BaseRosterSeed);
            var rosters = new Squad[BaseRosterCount];

            for (int clubId = 0; clubId < rosters.Length; clubId++)
            {
                int streamIndex = rng.RegisterStream(
                    LeagueBootstrapConstants.ROSTER_STREAM_SITE_ID,
                    SubsystemOrdinals.PlayerDatabase,
                    entityId: clubId,
                    streamVersion: LeagueBootstrapConstants.ROSTER_STREAM_VERSION);
                rosters[clubId] = RosterGenerator.Generate(rng, streamIndex, clubId, template);
            }

            return rosters;
        }

        /// <summary>
        /// Runs ONE match: shift both squads, measure the differential the engine will actually field, boot
        /// a real <c>MatchEngine</c>, and tick to full time.
        /// <para>
        /// Every column of the returned row is observable at capture time — which is the property that made
        /// the first draft of KD-8 unbuildable when it asked for <c>dRating</c>.
        /// </para>
        /// </summary>
        internal static CalibrationRow RunOne(
            Squad homeBase, Squad awayBase, int homeDelta, int awayDelta, ulong matchSeed)
        {
            Squad home = LeagueBootstrap.ApplyStrength(homeBase, homeDelta);
            Squad away = LeagueBootstrap.ApplyStrength(awayBase, awayDelta);

            float dSquad = TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(home)
                - TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(away);

            var engine = new TacticalDirector.MatchEngine.MatchEngine(matchSeed);
            engine.ConfigureSquads(home, away);
            while (!engine.MatchEnded)
            {
                engine.RunTick();
            }

            return new CalibrationRow(
                dSquad, engine.HomeScore, engine.AwayScore, matchSeed,
                homeDelta, awayDelta, homeBase.ClubId, awayBase.ClubId);
        }

        /// <summary>
        /// The per-match seed for sample <paramref name="sampleIndex"/> of the bucket targeting
        /// <paramref name="targetDelta"/> — keyed, so any subset of the grid can be regenerated
        /// independently (which is what lets the corpus run split across processes).
        /// </summary>
        internal static ulong SeedFor(int targetDelta, int sampleIndex)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                ulong z = CalibrationSeedDomain
                    ^ ((ulong)(uint)targetDelta * 0x9E3779B97F4A7C15UL)
                    ^ ((ulong)(uint)sampleIndex * 0xBF58476D1CE4E5B9UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Builds one bucket's PLAN — which base rosters to pair, how to shift them, and which seed to use
        /// — without running anything. Split from execution deliberately: the plan is the part with real
        /// logic worth locking by test, and a test that locked it through <see cref="RunOne"/> would cost a
        /// full 90-minute match per assertion.
        /// <para>
        /// The bucket LABEL is <paramref name="targetDelta"/>; the corpus buckets on each row's own MEASURED
        /// <see cref="CalibrationRow.DSquad"/> (KD-8), because two squads shifted by the same delta do not
        /// have the same rating — their base rosters differ.
        /// </para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Non-positive sample count, or a roster count
        /// below 2 (a bucket needs two distinct base rosters to pair).</exception>
        internal static CalibrationPlanEntry[] BuildPlan(
            int rosterCount, int targetDelta, int samplesPerBucket)
        {
            if (rosterCount < 2)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(rosterCount), rosterCount, "A bucket needs at least two base rosters to pair.");
            }

            if (samplesPerBucket <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(samplesPerBucket), samplesPerBucket, "samplesPerBucket must be positive.");
            }

            // Split the target across the two sides so neither is pushed against the [1,20] clamp harder
            // than necessary (KD-5 notes the ramp is slightly sublinear at the top for exactly that reason).
            // Integer division truncates toward zero, and awayDelta absorbs the remainder, so
            // homeDelta - awayDelta == targetDelta exactly for both signs.
            int homeDelta = targetDelta / 2;
            int awayDelta = homeDelta - targetDelta;

            var plan = new CalibrationPlanEntry[samplesPerBucket];
            for (int s = 0; s < samplesPerBucket; s++)
            {
                int homeIdx = s % rosterCount;

                // Step the partner by a stride that grows every full cycle, so successive cycles pair
                // different rosters instead of repeating (home, home+1) forever. `1 + ...` keeps the stride
                // non-zero, and the modulus keeps it inside the roster set; a stride that lands back on
                // homeIdx is nudged on, which can only happen when the grown stride is a multiple of the
                // roster count.
                int stride = 1 + ((s / rosterCount) % (rosterCount - 1));
                int awayIdx = (homeIdx + stride) % rosterCount;
                if (awayIdx == homeIdx)
                {
                    awayIdx = (homeIdx + 1) % rosterCount;
                }

                plan[s] = new CalibrationPlanEntry(
                    homeIdx, awayIdx, homeDelta, awayDelta, SeedFor(targetDelta, s));
            }

            return plan;
        }

        /// <summary>Executes a bucket's plan — one real match per entry.</summary>
        internal static List<CalibrationRow> RunBucket(
            Squad[] baseRosters, int targetDelta, int samplesPerBucket)
        {
            CalibrationPlanEntry[] plan = BuildPlan(baseRosters.Length, targetDelta, samplesPerBucket);
            var rows = new List<CalibrationRow>(plan.Length);
            for (int i = 0; i < plan.Length; i++)
            {
                CalibrationPlanEntry e = plan[i];
                rows.Add(RunOne(
                    baseRosters[e.HomeRosterIndex],
                    baseRosters[e.AwayRosterIndex],
                    e.HomeDelta,
                    e.AwayDelta,
                    e.MatchSeed));
            }

            return rows;
        }

        /// <summary>Renders rows as CSV with the header — the raw artifact form.</summary>
        internal static string ToCsv(IEnumerable<CalibrationRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CalibrationRow.CsvHeader);
            foreach (CalibrationRow row in rows)
            {
                sb.AppendLine(row.ToCsv());
            }

            return sb.ToString();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (roadmap A4a / KD-8): base-roster generation, |
// |         |            |        | direct-shift pair construction, one-match engine runner recording    |
// |         |            |        | only capture-time-observable columns, keyed per-sample seeds so the  |
// |         |            |        | grid can be split across processes, and CSV rendering.              |
#endregion
