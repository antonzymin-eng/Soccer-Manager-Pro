// File:     src/match-analytics/Tests/MatchAnalyticsValueTypeTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 / §2.3 (FR-AN-015/018, F1/F2/F4) + §4.1 KD-4,
//           Code Standards #20
// Purpose:  Locks the T0 view models: they are immutable snapshots rather than windows onto a
//           producer's buffers, they refuse incoherent input at construction rather than in a report,
//           and — mechanically — nothing in the simulation references this assembly.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace TacticalDirector.MatchAnalytics.Tests
{
    [TestFixture]
    public sealed class MatchAnalyticsValueTypeTests
    {
        private static int[] Grid() => AdvancedStatline.NewHeatmapGrid();

        private static MatchStatline Line(byte team) =>
            new MatchStatline(team, 1, 40f, 2, 1, 0, 1, 3, 12, 4, 2);

        private static AdvancedStatline Advanced(byte team) =>
            new AdvancedStatline(team, 55f, false, 0f, Grid());

        // ── FR-AN-015 — value semantics ──────────────────────────────────────────────────────────

        [Test]
        public void AllViewModels_AreValueTypes()
        {
            Assert.IsTrue(typeof(MatchStatline).IsValueType);
            Assert.IsTrue(typeof(AdvancedStatline).IsValueType);
            Assert.IsTrue(typeof(StatPoint).IsValueType);
            Assert.IsTrue(typeof(MatchAnalyticsResult).IsValueType);
        }

        [Test]
        public void MutatingTheProducerHeatmapArray_DoesNotChangeTheStatline()
        {
            int[] bins = Grid();
            bins[5] = 7;
            var line = new AdvancedStatline(0, 50f, false, 0f, bins);

            bins[5] = 999;                                   // the producer keeps accumulating

            Assert.AreEqual(7, line.HeatmapBins[5],
                "the statline copied the bins; it must not alias the aggregator's live buffer");
        }

        [Test]
        public void MutatingTheProducerMapArray_DoesNotChangeTheResult()
        {
            var goals = new[] { new StatPoint(0, 10f, 10f) };
            var result = new MatchAnalyticsResult(
                Line(0), Line(1), Advanced(0), Advanced(1), goals, new StatPoint[0], new StatPoint[0]);

            goals[0] = new StatPoint(1, 99f, 99f);

            Assert.AreEqual(0, result.GoalMap[0].TeamId, "the result copied the map, it did not wrap it");
            Assert.AreEqual(10f, result.GoalMap[0].X);
        }

        [Test]
        public void DefaultConstructedAdvancedStatline_ExposesAnEmptyGrid_NotNull()
        {
            AdvancedStatline empty = default;
            Assert.IsNotNull(empty.HeatmapBins);
            Assert.AreEqual(0, empty.HeatmapBins.Count);
        }

        [Test]
        public void DefaultConstructedResult_ExposesEmptyMaps_NotNull()
        {
            MatchAnalyticsResult empty = default;
            Assert.IsNotNull(empty.GoalMap);
            Assert.IsNotNull(empty.FoulMap);
            Assert.IsNotNull(empty.OffsideMap);
            Assert.AreEqual(0, empty.GoalMap.Count);
        }

        // ── FR-AN-018 / F1 / F2 — fail loud at construction ──────────────────────────────────────

        [Test]
        public void StatPoint_RefusesUnknownTeamAndNonFiniteCoordinates()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatPoint(2, 10f, 10f));
            Assert.Throws<ArgumentException>(() => new StatPoint(0, float.NaN, 10f));
            Assert.Throws<ArgumentException>(() => new StatPoint(0, 10f, float.PositiveInfinity));
        }

        [Test]
        public void MatchStatline_RefusesNegativeCountsAndBadPossessionShare()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchStatline(0, -1, 50f, 0, 0, 0, 0, 0, 0, 0, 0), "negative goals");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchStatline(0, 0, 50f, 0, 0, 0, 0, 0, 0, 0, -3), "negative substitutions");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchStatline(0, 0, 101f, 0, 0, 0, 0, 0, 0, 0, 0), "share above 100");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchStatline(0, 0, float.NaN, 0, 0, 0, 0, 0, 0, 0, 0), "non-finite share");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MatchStatline(2, 0, 50f, 0, 0, 0, 0, 0, 0, 0, 0), "unknown team");
        }

        [Test]
        public void AdvancedStatline_RefusesAWrongSizedOrNegativeGrid()
        {
            Assert.Throws<ArgumentNullException>(() => new AdvancedStatline(0, 50f, false, 0f, null));
            Assert.Throws<ArgumentException>(
                () => new AdvancedStatline(0, 50f, false, 0f, new int[MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM - 1]));

            int[] negative = Grid();
            negative[0] = -1;
            Assert.Throws<ArgumentException>(() => new AdvancedStatline(0, 50f, false, 0f, negative));
        }

        /// <summary>
        /// F4 / KD-2 — the reason <c>LiveXgAvailable</c> exists at all. A non-zero xG total with no
        /// shot-origin producer behind it would render as a real number on a report, indistinguishable
        /// from a measured one. Refusing the combination here means no screen has to know that.
        /// </summary>
        [Test]
        public void AdvancedStatline_RefusesXgWithoutAProducer()
        {
            Assert.Throws<ArgumentException>(() => new AdvancedStatline(0, 50f, liveXgAvailable: false, xgSum: 1.2f, Grid()));
            Assert.DoesNotThrow(() => new AdvancedStatline(0, 50f, liveXgAvailable: false, xgSum: 0f, Grid()));
            Assert.DoesNotThrow(() => new AdvancedStatline(0, 50f, liveXgAvailable: true, xgSum: 1.2f, Grid()));
        }

        [Test]
        public void Result_RefusesMisAttributedStatlines()
        {
            // Two home lines: every number a report showed would be attributed to the wrong side.
            Assert.Throws<ArgumentException>(() => new MatchAnalyticsResult(
                Line(0), Line(0), Advanced(0), Advanced(1), new StatPoint[0], new StatPoint[0], new StatPoint[0]));

            Assert.Throws<ArgumentException>(() => new MatchAnalyticsResult(
                Line(0), Line(1), Advanced(1), Advanced(1), new StatPoint[0], new StatPoint[0], new StatPoint[0]));

            Assert.Throws<ArgumentNullException>(() => new MatchAnalyticsResult(
                Line(0), Line(1), Advanced(0), Advanced(1), null, new StatPoint[0], new StatPoint[0]));
        }

        // ── KD-4 — the one-directional layering invariant, checked mechanically ──────────────────

        /// <summary>
        /// §4.1: no simulation assembly may reference <c>TacticalDirector.MatchAnalytics</c>. The build
        /// enforces it only by the absence of a reference, which is invisible until someone adds one —
        /// so it is scanned, exactly as #38 scans for reverse references to the UI layer (T-UI-LAYER-001).
        /// </summary>
        [Test]
        public void NoOtherAssemblyReferencesMatchAnalytics()
        {
            DirectoryInfo root = FindRepoRoot();
            Assert.IsNotNull(root,
                "could not locate the repo root from " + AppContext.BaseDirectory +
                " — this lock must fail loud rather than scan nothing and pass.");

            FileInfo[] asmdefs = new DirectoryInfo(Path.Combine(root.FullName, "src"))
                .GetFiles("*.asmdef", SearchOption.AllDirectories);

            Assert.Greater(asmdefs.Length, 20, "scanned implausibly few asmdefs — root resolution is wrong");

            // The invariant is "no SIM assembly references analytics", which is not the same as "no
            // assembly does" — #37 §4.1 names the UI layer as a sanctioned consumer, and B6's browser
            // client is the first one to exist. Rather than relax to an allow-list alone (which could
            // later be grown to admit a sim assembly by whoever is fixing a red test), this asserts
            // BOTH halves: only sanctioned consumers may reference it, AND the sim assemblies are
            // named explicitly so adding one to the allow-list still fails.
            var sanctionedConsumers = new HashSet<string>
            {
                "match-analytics", "match-analytics-tests",   // itself
                "match-client-web", "match-client-web-tests", // roadmap B6 — the PM-1 browser client
            };

            var mustNeverReference = new HashSet<string>
            {
                "match-engine", "season-save", "living-world", "player-database", "player-progression",
                "deterministic-sim", "event-system", "decision-tree", "perception-system",
                "positioning-ai", "pressing-ai", "defensive-ai", "attacking-ai",
                "ball-physics", "agent-movement", "collision-system", "first-touch",
                "pass-mechanics", "shot-mechanics", "heading-mechanics", "goalkeeper-mechanics",
                "tactical-instructions",
            };

            var offenders = new List<string>();
            foreach (FileInfo asmdef in asmdefs)
            {
                string name = Path.GetFileNameWithoutExtension(asmdef.Name);
                if (!File.ReadAllText(asmdef.FullName).Contains("TacticalDirector.MatchAnalytics"))
                {
                    continue;
                }

                if (mustNeverReference.Contains(name) || !sanctionedConsumers.Contains(name))
                {
                    offenders.Add(asmdef.FullName.Substring(root.FullName.Length + 1));
                }
            }

            CollectionAssert.IsEmpty(offenders,
                "no simulation assembly may reference the analytics layer, and only a sanctioned " +
                "presentation-layer consumer may (#37 §4.1 KD-4)");

            // Non-vacuity: the two halves above are only meaningful if the names they test against
            // correspond to assemblies that exist.
            var present = new HashSet<string>();
            foreach (FileInfo asmdef in asmdefs) { present.Add(Path.GetFileNameWithoutExtension(asmdef.Name)); }
            CollectionAssert.IsSubsetOf(
                mustNeverReference, present,
                "the never-reference list names an assembly that no longer exists — a renamed " +
                "assembly would silently drop out of this guard.");
        }

        private static DirectoryInfo FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "tools")))
                {
                    return dir;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): value semantics, copy-not-wrap for  |
// |         |            |        | the heatmap and map arrays, every FR-AN-018 construction gate  |
// |         |            |        | incl. the F4 xG-without-a-producer refusal, and the mechanical |
// |         |            |        | KD-4 reverse-reference scan.                                   |
#endregion
