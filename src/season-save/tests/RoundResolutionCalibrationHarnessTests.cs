// File:     src/season-save/tests/RoundResolutionCalibrationHarnessTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Modified: 2026-07-28 (Step 0 re-run PASSED post-§5.Z.20/§5.Z.21; LogAssert wrapper on both env-gated drivers — playing matches emit FM-08/FM-03 as ordinary events)
// Modified: 2026-08-12 (A4a run: TD_CALIBRATION_SAMPLE_FROM + window tiling/slice locks)
// Author:   —
// Spec:     League Bootstrap design supplement KD-8 (calibration methodology + Step 0); path-to-playable
//           roadmap A4a / C1a; Code Standards #20
// Purpose:  Locks the calibration harness's PLANNING logic — which is where the real logic lives — always,
//           and drives the expensive engine corpus only when explicitly asked.
//
// WHY THE CORPUS RUN IS ENV-GATED: one row is a full 90-minute match (~2 minutes of compute), and a corpus
// is hundreds of them. Wiring that into every push would multiply the gate's runtime for a run that is
// by design ONE-OFF (KD-8: the corpus is generated, fitted, and committed as an artifact; it is re-captured
// only when the engine's scoring changes). The engine path itself is not left unverified — the
// season-multi-fixture capstone drives a real MatchEngine through the season loop on every push.
//
//   Step 0 pilot (KD-8):   TD_CALIBRATION_PILOT=1 dotnet test --filter Calibration
//   Corpus slice:          TD_CALIBRATION_SAMPLES=18 TD_CALIBRATION_DELTA_FROM=-5 \
//                          TD_CALIBRATION_DELTA_TO=5 TD_CALIBRATION_OUT=/path/rows.csv \
//                          dotnet test --filter Calibration

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class RoundResolutionCalibrationHarnessTests
    {
        // ── Always-on: the plan, the seeds, and the base rosters ───────────────────────────

        [Test]
        public void BaseRosters_AreFieldableAndDistinct()
        {
            Squad[] rosters = RoundResolutionCalibrationHarness.BuildBaseRosters();

            Assert.AreEqual(RoundResolutionCalibrationHarness.BaseRosterCount, rosters.Length);

            var seenRatings = new List<float>();
            for (int i = 0; i < rosters.Length; i++)
            {
                Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, rosters[i].Count);

                // Fieldable by the KD-6 template — if this threw, every corpus row would fail at
                // ConfigureSquads hours into a run.
                float rating = TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(rosters[i]);
                Assert.Greater(rating, 0f);
                seenRatings.Add(rating);
            }

            // Distinct base rosters are the point of having six: a bucket built from one pair re-seeded
            // measures seed variance, not rating response (KD-8).
            bool anyDifferent = false;
            for (int i = 1; i < seenRatings.Count; i++)
            {
                if (Math.Abs(seenRatings[i] - seenRatings[0]) > 1e-6f)
                {
                    anyDifferent = true;
                }
            }

            Assert.IsTrue(anyDifferent, "The base rosters must not all rate identically.");
        }

        [Test]
        public void BaseRosters_AreDeterministic()
        {
            Squad[] a = RoundResolutionCalibrationHarness.BuildBaseRosters();
            Squad[] b = RoundResolutionCalibrationHarness.BuildBaseRosters();

            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].ClubId, b[i].ClubId);
                for (int p = 0; p < a[i].Count; p++)
                {
                    Assert.AreEqual(a[i].GetPlayer(p).PlayerId, b[i].GetPlayer(p).PlayerId);
                    Assert.AreEqual(
                        a[i].GetPlayer(p).Attributes.Finishing, b[i].GetPlayer(p).Attributes.Finishing);
                }
            }
        }

        [Test]
        public void BuildPlan_SplitsTheTargetDeltaExactly()
        {
            // The differential the plan asks for must be the differential it encodes, for both signs —
            // integer division truncates toward zero, so this is the assertion that catches a sign slip.
            for (int target = -6; target <= 6; target++)
            {
                CalibrationPlanEntry[] plan = RoundResolutionCalibrationHarness.BuildPlan(
                    RoundResolutionCalibrationHarness.BaseRosterCount, target, 4);

                foreach (CalibrationPlanEntry e in plan)
                {
                    Assert.AreEqual(target, e.HomeDelta - e.AwayDelta, $"target {target}");
                }
            }
        }

        [Test]
        public void BuildPlan_NeverPairsARosterWithItself()
        {
            CalibrationPlanEntry[] plan = RoundResolutionCalibrationHarness.BuildPlan(
                RoundResolutionCalibrationHarness.BaseRosterCount, 2, 40);

            foreach (CalibrationPlanEntry e in plan)
            {
                Assert.AreNotEqual(e.HomeRosterIndex, e.AwayRosterIndex);
                Assert.GreaterOrEqual(e.HomeRosterIndex, 0);
                Assert.Less(e.HomeRosterIndex, RoundResolutionCalibrationHarness.BaseRosterCount);
                Assert.GreaterOrEqual(e.AwayRosterIndex, 0);
                Assert.Less(e.AwayRosterIndex, RoundResolutionCalibrationHarness.BaseRosterCount);
            }
        }

        [Test]
        public void BuildPlan_UsesMoreThanOnePairingAcrossABucket()
        {
            // 18 samples over 6 rosters must exercise several distinct pairings, not one pair re-seeded.
            CalibrationPlanEntry[] plan = RoundResolutionCalibrationHarness.BuildPlan(
                RoundResolutionCalibrationHarness.BaseRosterCount, 0, 18);

            var pairs = new HashSet<int>();
            foreach (CalibrationPlanEntry e in plan)
            {
                pairs.Add((e.HomeRosterIndex * 100) + e.AwayRosterIndex);
            }

            Assert.Greater(pairs.Count, RoundResolutionCalibrationHarness.BaseRosterCount,
                "A bucket must span more distinct pairings than it has rosters.");
        }

        [Test]
        public void BuildPlan_SeedsAreDistinctWithinAndAcrossBuckets()
        {
            var seeds = new HashSet<ulong>();
            for (int target = -5; target <= 5; target++)
            {
                CalibrationPlanEntry[] plan = RoundResolutionCalibrationHarness.BuildPlan(
                    RoundResolutionCalibrationHarness.BaseRosterCount, target, 18);
                foreach (CalibrationPlanEntry e in plan)
                {
                    Assert.IsTrue(seeds.Add(e.MatchSeed), "Corpus match seeds must not collide.");
                }
            }

            Assert.AreEqual(11 * 18, seeds.Count);
        }

        [Test]
        public void SeedFor_IsKeyedSoASliceIsReproducibleInIsolation()
        {
            // What makes the corpus run splittable across processes: sample (delta, i)'s seed does not
            // depend on how many samples ran before it.
            Assert.AreEqual(
                RoundResolutionCalibrationHarness.SeedFor(3, 7),
                RoundResolutionCalibrationHarness.SeedFor(3, 7));
            Assert.AreNotEqual(
                RoundResolutionCalibrationHarness.SeedFor(3, 7),
                RoundResolutionCalibrationHarness.SeedFor(4, 7));
            Assert.AreNotEqual(
                RoundResolutionCalibrationHarness.SeedFor(3, 7),
                RoundResolutionCalibrationHarness.SeedFor(3, 8));
        }

        [Test]
        public void BuildPlan_RejectsDegenerateArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RoundResolutionCalibrationHarness.BuildPlan(1, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RoundResolutionCalibrationHarness.BuildPlan(6, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RoundResolutionCalibrationHarness.BuildPlan(6, 0, sampleFrom: -1, sampleCount: 4));
        }

        [Test]
        public void BuildPlan_AWindowIsExactlyTheSliceOfTheContiguousPlan()
        {
            // THE property that makes a bucket splittable across processes: running samples [k, k+m) in
            // their own process must produce the same rows the unsplit run would have produced at those
            // indices. Both the seed AND the roster pairing are derived from the ABSOLUTE index for this
            // reason — pairing off a window-local index would silently re-pair every split slice, and the
            // corpus would still look perfectly well-formed.
            CalibrationPlanEntry[] whole = RoundResolutionCalibrationHarness.BuildPlan(
                RoundResolutionCalibrationHarness.BaseRosterCount, 0, 40);

            for (int start = 0; start + 7 <= whole.Length; start += 7)
            {
                CalibrationPlanEntry[] window = RoundResolutionCalibrationHarness.BuildPlan(
                    RoundResolutionCalibrationHarness.BaseRosterCount, 0, start, 7);

                Assert.AreEqual(7, window.Length);
                for (int i = 0; i < window.Length; i++)
                {
                    Assert.AreEqual(whole[start + i].MatchSeed, window[i].MatchSeed, $"seed at {start + i}");
                    Assert.AreEqual(whole[start + i].HomeRosterIndex, window[i].HomeRosterIndex,
                        $"home roster at {start + i}");
                    Assert.AreEqual(whole[start + i].AwayRosterIndex, window[i].AwayRosterIndex,
                        $"away roster at {start + i}");
                    Assert.AreEqual(whole[start + i].HomeDelta, window[i].HomeDelta, $"homeDelta at {start + i}");
                    Assert.AreEqual(whole[start + i].AwayDelta, window[i].AwayDelta, $"awayDelta at {start + i}");
                }
            }
        }

        [Test]
        public void BuildPlan_AdjacentWindowsTileTheBucketWithoutOverlap()
        {
            // A split corpus must neither duplicate nor drop a sample: four windows that tile [0,80) must
            // carry 80 distinct seeds, which is also what stops a deepened bucket double-counting rows.
            var seeds = new HashSet<ulong>();
            for (int start = 0; start < 80; start += 20)
            {
                foreach (CalibrationPlanEntry e in RoundResolutionCalibrationHarness.BuildPlan(
                             RoundResolutionCalibrationHarness.BaseRosterCount, 0, start, 20))
                {
                    Assert.IsTrue(seeds.Add(e.MatchSeed), $"duplicate seed in window at {start}");
                }
            }

            Assert.AreEqual(80, seeds.Count);
        }

        [Test]
        public void BuildPlan_DefaultOverloadIsTheWindowStartingAtZero()
        {
            CalibrationPlanEntry[] implicitStart = RoundResolutionCalibrationHarness.BuildPlan(
                RoundResolutionCalibrationHarness.BaseRosterCount, 3, 12);
            CalibrationPlanEntry[] explicitStart = RoundResolutionCalibrationHarness.BuildPlan(
                RoundResolutionCalibrationHarness.BaseRosterCount, 3, 0, 12);

            Assert.AreEqual(implicitStart.Length, explicitStart.Length);
            for (int i = 0; i < implicitStart.Length; i++)
            {
                Assert.AreEqual(implicitStart[i].MatchSeed, explicitStart[i].MatchSeed);
                Assert.AreEqual(implicitStart[i].HomeRosterIndex, explicitStart[i].HomeRosterIndex);
                Assert.AreEqual(implicitStart[i].AwayRosterIndex, explicitStart[i].AwayRosterIndex);
            }
        }

        [Test]
        public void Csv_RoundTripsTheObservableColumns()
        {
            var row = new CalibrationRow(1.25f, 3, 1, 0xABCDEF0123456789UL, 1, -1, 0, 2);
            string csv = RoundResolutionCalibrationHarness.ToCsv(new[] { row });

            StringAssert.Contains(CalibrationRow.CsvHeader, csv);
            StringAssert.Contains("3,1,", csv);
            StringAssert.Contains("12379813738877118345", csv);   // the seed, invariant-culture
        }

        // ── Env-gated: the real engine runs ────────────────────────────────────────────────

        [Test]
        [Category("Calibration")]
        public void Pilot_ExtremeDifferentialsAreDistinguishable()
        {
            // KD-8 STEP 0 — the check that must happen BEFORE the full corpus: if a squad at the weak end of
            // the strength ramp and one at the strong end produce indistinguishable goal distributions, the
            // corpus carries no signal to fit, and fitting three parameters to noise would waste the run.
            //
            // Run 2026-07-26: FAILED, correctly — all 20 matches finished 0–0 at a measured dSquad of ±6,
            // because a Stage-0 match never put the ball in motion at all (ERR-030-014 — root cause and
            // evidence in docs/tracking/round-resolution-corpus.md). Step 0 stopped a ~5-hour corpus run
            // that would have fitted three parameters to a table of zeros, which is exactly the cost it
            // was added (league-bootstrap AR-5 M-4) to avoid.
            // Re-run 2026-07-28 (post-§5.Z.20/§5.Z.21): PASSED — strong-at-home margin +7.1, strong-away
            // margin −4.7; both extremes separate IN BOTH DIRECTIONS and the §5.Z.11 home/away asymmetry
            // no longer dominates the signal. Evidence in round-resolution-corpus.md.
            string flag = Environment.GetEnvironmentVariable("TD_CALIBRATION_PILOT");
            if (string.IsNullOrEmpty(flag))
            {
                Assert.Ignore("Set TD_CALIBRATION_PILOT=1 to run the KD-8 Step 0 pilot (~20 real matches).");
            }

            // A playing match emits FM-08/FM-03 possession-race errors as ordinary match events
            // (§5.Z Phase H); without this the shim's log watcher fails the run at teardown even
            // though every assertion passed — this driver predates play developing, so it never
            // needed the wrapper its sibling engine-driving diagnostics carry.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            int spread = LeagueBootstrapConstants.LeagueStrengthSpread;
            Squad[] rosters = RoundResolutionCalibrationHarness.BuildBaseRosters();

            List<CalibrationRow> strongHome =
                RoundResolutionCalibrationHarness.RunBucket(rosters, +2 * spread, 10);
            List<CalibrationRow> strongAway =
                RoundResolutionCalibrationHarness.RunBucket(rosters, -2 * spread, 10);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            float strongHomeMargin = MeanMargin(strongHome);
            float strongAwayMargin = MeanMargin(strongAway);

            TestContext.WriteLine(
                $"KD-8 Step 0 pilot (spread={spread}): strong-at-home mean margin {strongHomeMargin:F3} "
                + $"(dSquad {MeanDSquad(strongHome):F3}), strong-away mean margin {strongAwayMargin:F3} "
                + $"(dSquad {MeanDSquad(strongAway):F3}).");
            TestContext.WriteLine(RoundResolutionCalibrationHarness.ToCsv(strongHome));
            TestContext.WriteLine(RoundResolutionCalibrationHarness.ToCsv(strongAway));

            Assert.Greater(strongHomeMargin, strongAwayMargin,
                "The two ramp extremes must be distinguishable, or the corpus carries no signal (KD-8 Step 0). "
                + "Known open blocker: ERR-030-014 — a Stage-0 match never puts the ball in motion, so every "
                + "engine match is 0-0. See docs/tracking/round-resolution-corpus.md.");
        }

        [Test]
        [Category("Calibration")]
        public void Corpus_GeneratesTheRequestedSlice()
        {
            // The A4a corpus run. Split across processes by delta range; each slice is reproducible in
            // isolation because the seeds are keyed.
            string samplesRaw = Environment.GetEnvironmentVariable("TD_CALIBRATION_SAMPLES");
            if (string.IsNullOrEmpty(samplesRaw))
            {
                Assert.Ignore(
                    "Set TD_CALIBRATION_SAMPLES (plus optional TD_CALIBRATION_DELTA_FROM / _TO / _OUT) "
                    + "to generate an A4a corpus slice. Each sample is a full ~2-minute engine match.");
            }

            int samples = int.Parse(samplesRaw, CultureInfo.InvariantCulture);
            int from = EnvInt("TD_CALIBRATION_DELTA_FROM", -5);
            int to = EnvInt("TD_CALIBRATION_DELTA_TO", 5);

            // The sample WINDOW start, so one bucket can be spread over several processes — which is what
            // makes deepening the dSquad ~ 0 bucket (the one KD-8's W/D/L bar is evaluated at) affordable.
            int sampleFrom = EnvInt("TD_CALIBRATION_SAMPLE_FROM", 0);
            string outPath = Environment.GetEnvironmentVariable("TD_CALIBRATION_OUT");

            // Same wrapper as the pilot: playing matches emit FM-08/FM-03 possession-race errors
            // as ordinary match events (§5.Z Phase H).
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            Squad[] rosters = RoundResolutionCalibrationHarness.BuildBaseRosters();
            var rows = new List<CalibrationRow>();

            for (int target = from; target <= to; target++)
            {
                List<CalibrationRow> bucket =
                    RoundResolutionCalibrationHarness.RunBucket(rosters, target, sampleFrom, samples);
                rows.AddRange(bucket);
                TestContext.WriteLine(
                    $"bucket target={target}: samples [{sampleFrom},{sampleFrom + samples}) "
                    + $"n={bucket.Count} meanDSquad={MeanDSquad(bucket):F3} "
                    + $"meanHome={MeanHome(bucket):F3} meanAway={MeanAway(bucket):F3}");
            }

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            string csv = RoundResolutionCalibrationHarness.ToCsv(rows);
            if (!string.IsNullOrEmpty(outPath))
            {
                File.WriteAllText(outPath, csv);
                TestContext.WriteLine($"wrote {rows.Count} rows to {outPath}");
            }
            else
            {
                TestContext.WriteLine(csv);
            }

            Assert.AreEqual((to - from + 1) * samples, rows.Count);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────────

        private static int EnvInt(string key, int fallback)
        {
            string raw = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(raw) ? fallback : int.Parse(raw, CultureInfo.InvariantCulture);
        }

        private static float MeanMargin(List<CalibrationRow> rows)
        {
            int total = 0;
            foreach (CalibrationRow r in rows)
            {
                total += r.HomeGoals - r.AwayGoals;
            }

            return total / (float)rows.Count;
        }

        private static float MeanDSquad(List<CalibrationRow> rows)
        {
            float total = 0f;
            foreach (CalibrationRow r in rows)
            {
                total += r.DSquad;
            }

            return total / rows.Count;
        }

        private static float MeanHome(List<CalibrationRow> rows)
        {
            int total = 0;
            foreach (CalibrationRow r in rows)
            {
                total += r.HomeGoals;
            }

            return total / (float)rows.Count;
        }

        private static float MeanAway(List<CalibrationRow> rows)
        {
            int total = 0;
            foreach (CalibrationRow r in rows)
            {
                total += r.AwayGoals;
            }

            return total / (float)rows.Count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial suite (roadmap A4a): always-on locks on base-roster          |
// |         |            |        | generation and the bucket plan (target split, no self-pairing, pair  |
// |         |            |        | diversity, keyed non-colliding seeds), plus the env-gated KD-8       |
// |         |            |        | Step 0 pilot and corpus-slice drivers.                               |
// | 1.1     | 2026-07-28 | —      | Step 0 re-run PASSED (strong-home +7.1 / strong-away −4.7; both      |
// |         |            |        | directions separate — the ERR-030-014 gate comment updated). Both    |
// |         |            |        | env-gated drivers gain the LogAssert.ignoreFailingMessages wrapper:  |
// |         |            |        | a PLAYING match emits FM-08/FM-03 possession-race errors as ordinary |
// |         |            |        | match events (§5.Z Phase H), and the shim's log watcher failed the   |
// |         |            |        | first post-play pilot at teardown with every assertion green — this  |
// |         |            |        | driver predates play developing.                                     |
// | 1.2     | 2026-08-12 | —      | A4a corpus run: TD_CALIBRATION_SAMPLE_FROM threads the new sample  |
// |         |            |        | window through the corpus driver, and three locks cover it — a     |
// |         |            |        | window equals the contiguous plan's slice (seeds AND roster pairs),|
// |         |            |        | adjacent windows tile a bucket with no overlap or gap, and the     |
// |         |            |        | 3-arg overload is the window starting at zero. Step 0 re-run on    |
// |         |            |        | this tree PASSED: margins +4.000 / -3.500 (was +7.100 / -4.700 on  |
// |         |            |        | the July-28 tree), and the split-across-processes rows were        |
// |         |            |        | verified byte-identical to this driver's own 20 pilot rows.        |
#endregion
