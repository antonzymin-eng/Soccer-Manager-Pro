// File:     src/season-save/tests/FixtureSchedulerTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §5.2 (T-SN-FIX-001..007), §3.1, Appendix C; Testing Strategy #19
// Purpose:  Unit + determinism locks for deterministic double round-robin fixture generation.

using System.Collections.Generic;
using NUnit.Framework;
using TacticalDirector.SeasonSave;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class FixtureSchedulerTests
    {
        private static readonly int[] FourClubs = { 10, 11, 12, 13 };

        /// <summary>
        /// T-SN-FIX-001 — the 4-club worked schedule: rounds, pairings and venues.
        /// <para>
        /// <b>Asserted against the ERR-030-010 CORRECTED table, not the table currently printed in
        /// §3.7 / Appendix C.</b> Those two were hand-derived without applying §3.1's round-parity
        /// venue rule; the pairings agree, only the home/away side of the odd-numbered rounds differs
        /// (round 1 is <c>{12 v 10, 11 v 13}</c> here, <c>{10 v 12, 13 v 11}</c> there; likewise
        /// round 4, its second-leg mirror). See the deviation note in <c>FixtureScheduler.cs</c> for
        /// why the pseudocode wins: without parity a 20-club league gives the pinned club 19
        /// consecutive home fixtures.
        /// </para>
        /// </summary>
        [Test]
        public void Generate_FourClubs_MatchesCorrectedWorkedSchedule()
        {
            Fixture[] actual = FixtureScheduler.Generate(
                FourClubs, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            (int round, int home, int away)[] expected =
            {
                (0, 10, 13), (0, 11, 12),
                (1, 12, 10), (1, 11, 13),
                (2, 10, 11), (2, 12, 13),
                (3, 13, 10), (3, 12, 11),
                (4, 10, 12), (4, 13, 11),
                (5, 11, 10), (5, 13, 12),
            };

            Assert.That(actual.Length, Is.EqualTo(expected.Length), "fixture count");

            // App. C lists rounds in order; compare as a set per round so intra-round ordering is not
            // over-specified, but assert the exact pairing + venue.
            for (int round = 0; round < 6; round++)
            {
                var actualRound = new List<string>();
                foreach (Fixture f in actual)
                {
                    if (f.RoundIndex == round)
                    {
                        actualRound.Add($"{f.HomeClubId}v{f.AwayClubId}");
                    }
                }

                var expectedRound = new List<string>();
                foreach ((int r, int h, int a) in expected)
                {
                    if (r == round)
                    {
                        expectedRound.Add($"{h}v{a}");
                    }
                }

                actualRound.Sort();
                expectedRound.Sort();
                Assert.That(actualRound, Is.EqualTo(expectedRound), $"round {round} fixtures");
            }
        }

        /// <summary>
        /// ERR-030-010 regression lock — the reason §3.1's parity clause is implemented rather than
        /// the worked tables' rule. Over a 20-club league every club's first-leg home count must sit
        /// within one of the 9/10 ideal, and no club may take more than a handful of consecutive home
        /// fixtures. Under the worked-table rule the pinned club scores 19 and 19 here, so this test
        /// fails loudly if the venue rule is ever reverted.
        /// </summary>
        [Test]
        public void Generate_TwentyClubs_FirstLegVenuesAreBalanced()
        {
            const int N = 20;
            int[] ids = MakeClubIds(N);
            Fixture[] fixtures = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            int firstLegRounds = FixtureScheduler.RoundCount(N) / 2;

            var homeCount = new Dictionary<int, int>();
            var sequence = new Dictionary<int, List<bool>>();
            foreach (int id in ids)
            {
                homeCount[id] = 0;
                sequence[id] = new List<bool>();
            }

            for (int round = 0; round < firstLegRounds; round++)
            {
                foreach (Fixture f in fixtures)
                {
                    if (f.RoundIndex != round)
                    {
                        continue;
                    }

                    homeCount[f.HomeClubId]++;
                    sequence[f.HomeClubId].Add(true);
                    sequence[f.AwayClubId].Add(false);
                }
            }

            foreach (int id in ids)
            {
                Assert.That(homeCount[id], Is.InRange(firstLegRounds / 2 - 1, firstLegRounds / 2 + 1),
                    $"club {id} has {homeCount[id]} of {firstLegRounds} first-leg fixtures at home; " +
                    "the first leg must be venue-balanced (ERR-030-010)");

                int run = 0;
                int longest = 0;
                foreach (bool wasHome in sequence[id])
                {
                    run = wasHome ? run + 1 : 0;
                    longest = System.Math.Max(longest, run);
                }

                Assert.That(longest, Is.LessThanOrEqualTo(3),
                    $"club {id} plays {longest} consecutive home fixtures in the first leg");
            }
        }

        /// <summary>
        /// Venue balance must not come at the cost of season balance: over the full double round-robin
        /// every club plays exactly N−1 at home and N−1 away.
        /// </summary>
        [TestCase(4)]
        [TestCase(20)]
        public void Generate_FullSeason_EveryClubHasEqualHomeAndAwayFixtures(int n)
        {
            int[] ids = MakeClubIds(n);
            Fixture[] fixtures = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            var home = new Dictionary<int, int>();
            var away = new Dictionary<int, int>();
            foreach (int id in ids)
            {
                home[id] = 0;
                away[id] = 0;
            }

            foreach (Fixture f in fixtures)
            {
                home[f.HomeClubId]++;
                away[f.AwayClubId]++;
            }

            foreach (int id in ids)
            {
                Assert.That(home[id], Is.EqualTo(n - 1), $"club {id} home fixtures");
                Assert.That(away[id], Is.EqualTo(n - 1), $"club {id} away fixtures");
            }
        }

        /// <summary>T-SN-FIX-002 — two-run determinism (FR-SN-001).</summary>
        [Test]
        public void Generate_TwiceWithSameInputs_IsFieldIdentical()
        {
            Fixture[] a = FixtureScheduler.Generate(FourClubs, 12345UL);
            Fixture[] b = FixtureScheduler.Generate(FourClubs, 12345UL);

            Assert.That(a.Length, Is.EqualTo(b.Length));
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(a[i].RoundIndex, Is.EqualTo(b[i].RoundIndex), $"fixture {i} round");
                Assert.That(a[i].HomeClubId, Is.EqualTo(b[i].HomeClubId), $"fixture {i} home");
                Assert.That(a[i].AwayClubId, Is.EqualTo(b[i].AwayClubId), $"fixture {i} away");
            }
        }

        /// <summary>
        /// T-SN-FIX-003 — double round-robin completeness (FR-SN-002): every ordered pair exactly once,
        /// N·(N−1) fixtures. Checked at several even and odd sizes.
        /// </summary>
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(6)]
        [TestCase(20)]
        public void Generate_EvenClubCount_ContainsEveryOrderedPairExactlyOnce(int n)
        {
            int[] ids = MakeClubIds(n);
            Fixture[] fixtures = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            Assert.That(fixtures.Length, Is.EqualTo(n * (n - 1)), "N*(N-1) fixtures");

            var seen = new HashSet<(int, int)>();
            foreach (Fixture f in fixtures)
            {
                Assert.That(seen.Add((f.HomeClubId, f.AwayClubId)), Is.True,
                    $"ordered pair ({f.HomeClubId},{f.AwayClubId}) appears more than once");
            }

            Assert.That(seen.Count, Is.EqualTo(n * (n - 1)), "distinct ordered pairs");
        }

        /// <summary>T-SN-FIX-004 — no club appears twice in one round (FR-SN-003).</summary>
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(20)]
        public void Generate_NoClubAppearsTwiceInARound(int n)
        {
            int[] ids = MakeClubIds(n);
            Fixture[] fixtures = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            var perRound = new Dictionary<int, HashSet<int>>();
            foreach (Fixture f in fixtures)
            {
                if (!perRound.TryGetValue(f.RoundIndex, out HashSet<int> clubs))
                {
                    clubs = new HashSet<int>();
                    perRound[f.RoundIndex] = clubs;
                }

                Assert.That(clubs.Add(f.HomeClubId), Is.True,
                    $"club {f.HomeClubId} appears twice in round {f.RoundIndex}");
                Assert.That(clubs.Add(f.AwayClubId), Is.True,
                    $"club {f.AwayClubId} appears twice in round {f.RoundIndex}");
            }
        }

        /// <summary>
        /// T-SN-FIX-005 — odd N bye rotation: every real club plays 2·(N−1) fixtures and none against a
        /// phantom (FR-SN-004).
        /// </summary>
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(19)]
        public void Generate_OddClubCount_EveryClubPlaysTwiceNMinusOne_NoPhantom(int n)
        {
            int[] ids = MakeClubIds(n);
            Fixture[] fixtures = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            var appearances = new Dictionary<int, int>();
            foreach (int id in ids)
            {
                appearances[id] = 0;
            }

            foreach (Fixture f in fixtures)
            {
                Assert.That(appearances.ContainsKey(f.HomeClubId), Is.True,
                    $"fixture names non-club {f.HomeClubId} (phantom leaked)");
                Assert.That(appearances.ContainsKey(f.AwayClubId), Is.True,
                    $"fixture names non-club {f.AwayClubId} (phantom leaked)");
                appearances[f.HomeClubId]++;
                appearances[f.AwayClubId]++;
            }

            foreach (int id in ids)
            {
                Assert.That(appearances[id], Is.EqualTo(2 * (n - 1)),
                    $"club {id} should play 2*(N-1) = {2 * (n - 1)} fixtures");
            }

            // Ordered-pair completeness still holds with the bye.
            var seen = new HashSet<(int, int)>();
            foreach (Fixture f in fixtures)
            {
                Assert.That(seen.Add((f.HomeClubId, f.AwayClubId)), Is.True, "duplicate ordered pair");
            }

            Assert.That(seen.Count, Is.EqualTo(n * (n - 1)));
        }

        /// <summary>T-SN-FIX-006 — N &lt; 2 throws (F1 / FR-SN-004).</summary>
        [Test]
        public void Generate_FewerThanTwoClubs_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => FixtureScheduler.Generate(new[] { 7 }, 0UL));
            Assert.Throws<System.ArgumentException>(
                () => FixtureScheduler.Generate(System.Array.Empty<int>(), 0UL));
        }

        /// <summary>T-SN-FIX-007 — seed sensitivity: a distinct permutation seed reorders the schedule.</summary>
        [Test]
        public void Generate_DistinctSeed_YieldsDistinctFixtureOrder()
        {
            Fixture[] identity = FixtureScheduler.Generate(
                MakeClubIds(8), SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);
            Fixture[] shuffled = FixtureScheduler.Generate(MakeClubIds(8), 0xABCDEF01UL);

            Assert.That(identity.Length, Is.EqualTo(shuffled.Length));

            bool anyDifference = false;
            for (int i = 0; i < identity.Length; i++)
            {
                if (identity[i].HomeClubId != shuffled[i].HomeClubId ||
                    identity[i].AwayClubId != shuffled[i].AwayClubId)
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.That(anyDifference, Is.True,
                "a non-identity permutation seed must change the fixture order");

            // ...while still being a valid complete schedule.
            var seen = new HashSet<(int, int)>();
            foreach (Fixture f in shuffled)
            {
                Assert.That(seen.Add((f.HomeClubId, f.AwayClubId)), Is.True, "duplicate ordered pair");
            }

            Assert.That(seen.Count, Is.EqualTo(8 * 7));
        }

        /// <summary>The identity seed performs no shuffle — the §3.1.1 "zero draws" option.</summary>
        [Test]
        public void Generate_IdentitySeed_EqualsRingOrderGeneration()
        {
            int[] ids = MakeClubIds(6);
            Fixture[] viaSeed = FixtureScheduler.Generate(
                ids, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);
            Fixture[] viaRing = FixtureScheduler.GenerateFromRingOrder(ids);

            Assert.That(viaSeed.Length, Is.EqualTo(viaRing.Length));
            for (int i = 0; i < viaSeed.Length; i++)
            {
                Assert.That(viaSeed[i].HomeClubId, Is.EqualTo(viaRing[i].HomeClubId), $"fixture {i} home");
                Assert.That(viaSeed[i].AwayClubId, Is.EqualTo(viaRing[i].AwayClubId), $"fixture {i} away");
                Assert.That(viaSeed[i].RoundIndex, Is.EqualTo(viaRing[i].RoundIndex), $"fixture {i} round");
            }
        }

        /// <summary>Generation must not reorder the caller's array (the live-array aliasing class).</summary>
        [Test]
        public void Generate_DoesNotMutateCallerArray()
        {
            int[] ids = MakeClubIds(8);
            var before = new int[ids.Length];
            System.Array.Copy(ids, before, ids.Length);

            FixtureScheduler.Generate(ids, 0xFEEDUL);

            Assert.That(ids, Is.EqualTo(before), "the caller's club array must be untouched");
        }

        /// <summary>A duplicate ClubId breaks the FR-SN-007 total order, so generation refuses it.</summary>
        [Test]
        public void Generate_DuplicateClubId_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => FixtureScheduler.Generate(new[] { 1, 2, 2, 3 }, 0UL));
        }

        /// <summary>Round count matches the schedule's actual round span.</summary>
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(20)]
        public void RoundCount_MatchesGeneratedSpan(int n)
        {
            Fixture[] fixtures = FixtureScheduler.Generate(
                MakeClubIds(n), SeasonLoopConstants.IDENTITY_PERMUTATION_SEED);

            int maxRound = 0;
            foreach (Fixture f in fixtures)
            {
                if (f.RoundIndex > maxRound)
                {
                    maxRound = f.RoundIndex;
                }
            }

            Assert.That(FixtureScheduler.RoundCount(n), Is.EqualTo(maxRound + 1));
        }

        private static int[] MakeClubIds(int n)
        {
            var ids = new int[n];
            for (int i = 0; i < n; i++)
            {
                ids[i] = 10 + i;
            }

            return ids;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-25 | —      | Initial suite (#30 T0): T-SN-FIX-001..007 + aliasing,     |
// |         |            |        | duplicate-id, identity-seed and RoundCount locks.         |
#endregion
