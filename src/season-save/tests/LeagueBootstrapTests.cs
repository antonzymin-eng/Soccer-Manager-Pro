// File:     src/season-save/tests/LeagueBootstrapTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) §3
//           (determinism contract), §4 (F1..F6), KD-2/KD-3/KD-5/KD-6/KD-9; path-to-playable A3
// Purpose:  Locks for the league bootstrap: byte-level determinism, seed divergence, club identity,
//           the strength ramp, the KD-6 position coherence that keeps every bootstrapped squad
//           fieldable, the F1..F6 fail-loud gates, and the CreateSeason handoff to #30.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;

using MEngine = TacticalDirector.MatchEngine.MatchEngine;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal sealed class LeagueBootstrapTests
    {
        private const ulong WorldSeedA = 0x51A7C0FFEE1234ABUL;
        private const ulong WorldSeedB = 0x0BADC0DE99887766UL;

        // ── Determinism (§3) ────────────────────────────────────────────────────────

        [Test]
        public void Generate_SameSeed_IsFieldIdentical()
        {
            League a = LeagueBootstrap.Generate(WorldSeedA, 20);
            League b = LeagueBootstrap.Generate(WorldSeedA, 20);

            Assert.AreEqual(a.ClubCount, b.ClubCount);
            Assert.AreEqual(a.SeasonSeed, b.SeasonSeed, "The derived season seed must be reproducible.");

            for (int c = 0; c < a.ClubCount; c++)
            {
                Club ca = a.ClubAt(c);
                Club cb = b.ClubAt(c);
                Assert.AreEqual(ca.ClubId, cb.ClubId);
                Assert.AreEqual(ca.Name, cb.Name);
                Assert.AreEqual(ca.StrengthDelta, cb.StrengthDelta);

                AssertSquadsIdentical(a.ResolveByClubId(c), b.ResolveByClubId(c), $"club {c}");
            }
        }

        [Test]
        public void Generate_DifferentSeed_DivergesInRostersAndStrengthOrder()
        {
            League a = LeagueBootstrap.Generate(WorldSeedA, 20);
            League b = LeagueBootstrap.Generate(WorldSeedB, 20);

            bool anyRosterDiffers = false;
            bool anyStrengthDiffers = false;
            for (int c = 0; c < a.ClubCount; c++)
            {
                if (a.ClubAt(c).StrengthDelta != b.ClubAt(c).StrengthDelta)
                {
                    anyStrengthDiffers = true;
                }

                // Compare IDENTITY fields, which no post-generation step touches. An attribute would
                // be contaminated by the strength delta, so the roster assertion could be satisfied by
                // the delta alone — it would pass even if rosterSeed stopped depending on the world seed.
                PlayerRecord pa = a.ResolveByClubId(c).GetPlayer(0);
                PlayerRecord pb = b.ResolveByClubId(c).GetPlayer(0);
                if (pa.FirstName != pb.FirstName || pa.LastName != pb.LastName || pa.Age != pb.Age)
                {
                    anyRosterDiffers = true;
                }
            }

            Assert.IsTrue(anyRosterDiffers, "A different world seed must produce different rosters.");
            Assert.IsTrue(anyStrengthDiffers,
                "A different world seed must reorder which clubs are strong (KD-5 — the seed changes " +
                "who is good, KD-3 keeps who exists fixed).");
        }

        [Test]
        public void Generate_ClubBaseRoster_IsIndependentOfLeagueSize()
        {
            // entityId = clubId scopes each club's stream key (KD-4), so club 3's BASE roster (identity
            // + pre-strength attributes) is a function of (worldSeed, clubId) alone.
            //
            // Scope, deliberately narrow: this does NOT hold for the shipped squad's attributes. The
            // strength delta depends on clubCount twice (the rank permutation's length and the ramp
            // denominator), so growing or shrinking the league DOES rewrite every club's attributes —
            // which matters for #43's promotion/relegation transform. Only the identity fields, which
            // no post-generation step touches, are size-invariant, and only those are asserted here.
            League small = LeagueBootstrap.Generate(WorldSeedA, 4);
            League large = LeagueBootstrap.Generate(WorldSeedA, 20);

            for (int c = 0; c < small.ClubCount; c++)
            {
                Squad s = small.ResolveByClubId(c);
                Squad l = large.ResolveByClubId(c);
                for (int p = 0; p < s.Count; p++)
                {
                    Assert.AreEqual(s.GetPlayer(p).PlayerId, l.GetPlayer(p).PlayerId);
                    Assert.AreEqual(s.GetPlayer(p).FirstName, l.GetPlayer(p).FirstName);
                    Assert.AreEqual(s.GetPlayer(p).LastName, l.GetPlayer(p).LastName);
                    Assert.AreEqual(s.GetPlayer(p).Age, l.GetPlayer(p).Age);
                    Assert.AreEqual(s.GetPlayer(p).Position, l.GetPlayer(p).Position);
                }
            }
        }

        [Test]
        public void Generate_ClubAttributes_DoChangeWithLeagueSize()
        {
            // The other half of the contract above, asserted rather than left implicit: a resize DOES
            // move attributes, because the strength ramp is over league size. Locking this stops the
            // narrowed claim from silently becoming the stronger (false) one again.
            League small = LeagueBootstrap.Generate(WorldSeedA, 4);
            League large = LeagueBootstrap.Generate(WorldSeedA, 20);

            bool anyAttributeDiffers = false;
            for (int c = 0; c < small.ClubCount && !anyAttributeDiffers; c++)
            {
                if (small.ClubAt(c).StrengthDelta != large.ClubAt(c).StrengthDelta)
                {
                    anyAttributeDiffers = true;
                }
            }

            Assert.IsTrue(anyAttributeDiffers,
                "The strength ramp is a function of league size, so at least one club's delta — and " +
                "therefore its attributes — must differ between a 4-club and a 20-club league.");
        }

        [Test]
        public void Generate_DefaultOverload_UsesTheConfiguredClubCount()
        {
            Assert.AreEqual(
                LeagueBootstrapConstants.DefaultClubCount,
                LeagueBootstrap.Generate(WorldSeedA).ClubCount);
        }

        // ── Club identity (KD-2 / KD-3) ─────────────────────────────────────────────

        [Test]
        public void Generate_ClubIds_AreContiguousFromZero_AndPlayerIdsGloballyUnique()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);

            var playerIds = new HashSet<int>();
            for (int c = 0; c < league.ClubCount; c++)
            {
                Assert.AreEqual(c, league.ClubAt(c).ClubId, "Club ids must be contiguous 0..N-1 (KD-2).");

                Squad squad = league.ResolveByClubId(c);
                Assert.AreEqual(c, squad.ClubId);
                for (int p = 0; p < squad.Count; p++)
                {
                    Assert.IsTrue(playerIds.Add(squad.GetPlayer(p).PlayerId),
                        "Contiguous club ids must give globally unique PlayerIds across the league (KD-2).");
                }
            }

            Assert.AreEqual(20 * PlayerDatabaseConstants.CLUB_SQUAD_SIZE, playerIds.Count);
        }

        [Test]
        public void Generate_Names_ComeFromTheCatalogueByIdAndAreStableAcrossSeeds()
        {
            League a = LeagueBootstrap.Generate(WorldSeedA, 20);
            League b = LeagueBootstrap.Generate(WorldSeedB, 20);

            for (int c = 0; c < a.ClubCount; c++)
            {
                Assert.AreEqual(ClubNameCatalogue.Names[c], a.ClubAt(c).Name);
                Assert.AreEqual(a.ClubAt(c).Name, b.ClubAt(c).Name,
                    "KD-3: a new-game seed changes who is good, not who exists.");
            }
        }

        [Test]
        public void ClubNameCatalogue_CoversTheCapAndHasNoDuplicates()
        {
            Assert.GreaterOrEqual(
                ClubNameCatalogue.Names.Count, LeagueBootstrapConstants.MaxClubCount,
                "Raising MaxClubCount past the name catalogue would produce unnamed clubs (F2).");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in ClubNameCatalogue.Names)
            {
                Assert.IsFalse(string.IsNullOrEmpty(name), "Every catalogue entry must be a real name.");
                Assert.IsTrue(seen.Add(name), $"Duplicate club name '{name}' in the catalogue.");
            }
        }

        [Test]
        public void MaxClubCount_FitsTheRngStreamBudget()
        {
            // One roster stream per club (KD-4). Raising MaxClubCount past the registry would fail
            // mid-generation with a generic "registry full", so the coupling is locked here.
            Assert.LessOrEqual(
                LeagueBootstrapConstants.MaxClubCount, DeterministicSimConstants.MaxRngStreams,
                "MaxClubCount must fit MaxRngStreams — the bootstrap registers one stream per club.");
        }

        // ── Strength ramp (KD-5) ────────────────────────────────────────────────────

        [Test]
        public void Generate_StrengthDeltas_SpanTheRamp()
        {
            const int n = 20;
            int spread = LeagueBootstrapConstants.LeagueStrengthSpread;
            League league = LeagueBootstrap.Generate(WorldSeedA, n);

            int min = int.MaxValue;
            int max = int.MinValue;
            for (int c = 0; c < n; c++)
            {
                int d = league.ClubAt(c).StrengthDelta;
                Assert.GreaterOrEqual(d, -spread);
                Assert.LessOrEqual(d, spread);
                min = Math.Min(min, d);
                max = Math.Max(max, d);
            }

            // "At least one", not "exactly one": at N=20 the ramp puts TWO clubs at each extreme
            // (distribution -3:2, -2:3, -1:3, 0:4, 1:3, 2:3, 3:2). The exact multiset is locked by the
            // golden vector's pinned 20-club delta row, not here.
            Assert.AreEqual(-spread, min, "At least one club must sit at the bottom of the ramp.");
            Assert.AreEqual(spread, max, "At least one club must sit at the top of the ramp.");
        }

        [Test]
        public void LeagueStrengthSpread_IsWithinTheAttributeScaleItShifts()
        {
            // A negative spread would invert the ramp (the bottom of the table would get the best
            // players); one past the scale is meaningless and would overflow the integer ramp.
            Assert.GreaterOrEqual(LeagueBootstrapConstants.LeagueStrengthSpread, 0);
            Assert.LessOrEqual(
                LeagueBootstrapConstants.LeagueStrengthSpread,
                PlayerDatabaseConstants.ATTRIBUTE_MAX - PlayerDatabaseConstants.ATTRIBUTE_MIN);
        }

        [Test]
        public void BuildStrengthRanks_IsAPermutation()
        {
            int[] ranks = LeagueBootstrap.BuildStrengthRanks(20, 0xDEADBEEFCAFEUL);
            Array.Sort(ranks);
            for (int i = 0; i < ranks.Length; i++)
            {
                Assert.AreEqual(i, ranks[i], "Ranks must be a permutation of 0..N-1 (no ties, no gaps).");
            }
        }

        [Test]
        public void StrengthDelta_RampEndpointsAndMidpoint()
        {
            int s = LeagueBootstrapConstants.LeagueStrengthSpread;
            Assert.AreEqual(-s, LeagueBootstrap.StrengthDelta(rank: 0, clubCount: 20));
            Assert.AreEqual(s, LeagueBootstrap.StrengthDelta(rank: 19, clubCount: 20));
            // Rank 9 and 10 straddle the midpoint of a 20-club ramp: symmetric about zero.
            Assert.AreEqual(
                -LeagueBootstrap.StrengthDelta(rank: 10, clubCount: 20),
                LeagueBootstrap.StrengthDelta(rank: 9, clubCount: 20),
                "The ramp must be symmetric about the middle of the table.");
            // Two clubs: one at each end.
            Assert.AreEqual(-s, LeagueBootstrap.StrengthDelta(rank: 0, clubCount: 2));
            Assert.AreEqual(s, LeagueBootstrap.StrengthDelta(rank: 1, clubCount: 2));
        }

        [Test]
        public void Generate_AllAttributes_StayWithinBoundsAfterTheShift()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);

            for (int c = 0; c < league.ClubCount; c++)
            {
                Squad squad = league.ResolveByClubId(c);
                for (int p = 0; p < squad.Count; p++)
                {
                    PlayerAttributes attrs = squad.GetPlayer(p).Attributes;
                    foreach (int v in attrs.ToArray())
                    {
                        Assert.GreaterOrEqual(v, PlayerDatabaseConstants.ATTRIBUTE_MIN);
                        Assert.LessOrEqual(v, PlayerDatabaseConstants.ATTRIBUTE_MAX);
                    }

                    Assert.GreaterOrEqual(attrs.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MIN);
                    Assert.LessOrEqual(attrs.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MAX);
                }
            }
        }

        [Test]
        public void ApplyStrength_ShiftsEveryAttribute_ButLeavesWeakFootExactlyAlone()
        {
            // The WeakFoot exclusion is a deliberate KD-5 decision ([1,5] scale would saturate), so it
            // is asserted SEMANTICALLY here rather than left to the golden vector. A bounds check cannot
            // see the difference: a regression that shifted WeakFoot and clamped to [1,5] stays in range.
            League league = LeagueBootstrap.Generate(WorldSeedA, 4);
            Squad baseSquad = league.ResolveByClubId(0);

            const int delta = 3;
            Squad shifted = LeagueBootstrap.ApplyStrength(baseSquad, delta);

            Assert.AreEqual(baseSquad.Count, shifted.Count);
            bool anyAttributeMoved = false;
            for (int p = 0; p < baseSquad.Count; p++)
            {
                PlayerAttributes before = baseSquad.GetPlayer(p).Attributes;
                PlayerAttributes after = shifted.GetPlayer(p).Attributes;

                Assert.AreEqual(before.WeakFootRating, after.WeakFootRating,
                    $"player {p}: WeakFootRating must be untouched by the strength shift (KD-5).");

                int[] b = before.ToArray();
                int[] a = after.ToArray();
                for (int i = 0; i < b.Length; i++)
                {
                    int expected = System.Math.Min(b[i] + delta, PlayerDatabaseConstants.ATTRIBUTE_MAX);
                    Assert.AreEqual(expected, a[i], $"player {p} attribute {i}");
                    if (a[i] != b[i])
                    {
                        anyAttributeMoved = true;
                    }
                }
            }

            Assert.IsTrue(anyAttributeMoved,
                "Non-vacuity: a +3 shift must actually move attributes, or the WeakFoot assertion above " +
                "would hold trivially.");
        }

        [Test]
        public void Generate_StrongerClub_HasHigherMeanAttribute()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);

            int weakest = 0;
            int strongest = 0;
            for (int c = 1; c < league.ClubCount; c++)
            {
                if (league.ClubAt(c).StrengthDelta < league.ClubAt(weakest).StrengthDelta) weakest = c;
                if (league.ClubAt(c).StrengthDelta > league.ClubAt(strongest).StrengthDelta) strongest = c;
            }

            Assert.Greater(
                MeanAttribute(league.ResolveByClubId(strongest)),
                MeanAttribute(league.ResolveByClubId(weakest)),
                "The strength delta must actually reach the rosters — a league of statistically " +
                "identical clubs makes the table meaningless (KD-5).");
        }

        // ── Position coherence (KD-6 / F6) ──────────────────────────────────────────

        [Test]
        public void BuildSquadPositionTemplate_MatchesTheCataloguedCountsAndFillsASquad()
        {
            PlayerPosition[] template = LeagueBootstrapConstants.BuildSquadPositionTemplate();
            Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, template.Length);

            var counts = new int[LeagueBootstrapConstants.PositionCount];
            foreach (PlayerPosition p in template)
            {
                counts[(int)p]++;
            }

            for (int p = 0; p < counts.Length; p++)
            {
                Assert.AreEqual(LeagueBootstrapConstants.SquadPositionCounts[p], counts[p],
                    $"Template count for {(PlayerPosition)p} must match the catalogue.");
            }
        }

        [Test]
        public void Generate_EverySquad_CanFieldEveryShippedFormation()
        {
            // F6 — the lock that would have caught the KD-6 defect. LineupSelector fails loud when a
            // starter slot's required position has no eligible player; the worst case across the three
            // shipped families is GK 1 / DF 4 / MF 5 / FW 3 (F442 1/4/4/2, F433 1/4/3/3, F4231 1/4/5/1).
            League league = LeagueBootstrap.Generate(WorldSeedA, LeagueBootstrapConstants.MaxClubCount);

            for (int c = 0; c < league.ClubCount; c++)
            {
                Squad squad = league.ResolveByClubId(c);
                var counts = new int[LeagueBootstrapConstants.PositionCount];
                for (int p = 0; p < squad.Count; p++)
                {
                    counts[(int)squad.GetPlayer(p).Position]++;
                }

                Assert.GreaterOrEqual(counts[(int)PlayerPosition.Goalkeeper], 1, $"club {c}: goalkeepers");
                Assert.GreaterOrEqual(counts[(int)PlayerPosition.Defender], 4, $"club {c}: defenders");
                Assert.GreaterOrEqual(counts[(int)PlayerPosition.Midfielder], 5, $"club {c}: midfielders");
                Assert.GreaterOrEqual(counts[(int)PlayerPosition.Forward], 3, $"club {c}: forwards");
            }
        }

        [Test]
        public void Generate_Squads_AreAcceptedByTheRealMatchEngine()
        {
            // The end-to-end half of F6: ConfigureSquads runs the real LineupSelector (internal to
            // match-engine) against the Stage-0 formation, so a squad that cannot be fielded throws here.
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);

            for (int c = 0; c + 1 < league.ClubCount; c += 2)
            {
                var engine = new MEngine(0xA11CEUL + (ulong)c);
                Assert.DoesNotThrow(
                    () => engine.ConfigureSquads(league.ResolveByClubId(c), league.ResolveByClubId(c + 1)),
                    $"Bootstrapped squads {c}/{c + 1} must be fieldable by the real engine.");
            }
        }

        // ── Fail-loud gates (§4) ────────────────────────────────────────────────────

        [Test]
        public void Generate_ClubCountOutOfRange_FailsLoud()
        {
            // F1.
            Assert.Throws<ArgumentOutOfRangeException>(() => LeagueBootstrap.Generate(WorldSeedA, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LeagueBootstrap.Generate(WorldSeedA, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => LeagueBootstrap.Generate(WorldSeedA, -3));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LeagueBootstrap.Generate(WorldSeedA, LeagueBootstrapConstants.MaxClubCount + 1));
        }

        [Test]
        public void Generate_AtBothBounds_Succeeds()
        {
            Assert.AreEqual(2, LeagueBootstrap.Generate(WorldSeedA, 2).ClubCount);
            Assert.AreEqual(
                LeagueBootstrapConstants.MaxClubCount,
                LeagueBootstrap.Generate(WorldSeedA, LeagueBootstrapConstants.MaxClubCount).ClubCount);
        }

        [Test]
        public void ResolveByClubId_UnknownClub_ReturnsNull()
        {
            // F4 — the ISquadProvider contract: null, so the caller's own gate fails loud rather than
            // silently substituting a default roster.
            League league = LeagueBootstrap.Generate(WorldSeedA, 4);
            Assert.IsNull(league.ResolveByClubId(4));
            Assert.IsNull(league.ResolveByClubId(-1));
            Assert.IsNotNull(league.ResolveByClubId(0));
            Assert.IsNotNull(league.ResolveByClubId(3));
        }

        [Test]
        public void ClubAt_OutOfRange_Throws()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => league.ClubAt(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => league.ClubAt(-1));
        }

        [Test]
        public void League_RefusesNonContiguousIds()
        {
            Squad squad0 = LeagueBootstrap.Generate(WorldSeedA, 2).ResolveByClubId(0);
            Squad squad1 = LeagueBootstrap.Generate(WorldSeedA, 2).ResolveByClubId(1);

            // Club ids out of index order — the KD-2 invariant ResolveByClubId's O(1) indexing rests on.
            Assert.Throws<ArgumentException>(() => new League(
                1UL, 2UL,
                new[] { new Club(1, "A", 0), new Club(0, "B", 0) },
                new[] { squad0, squad1 }));

            // Mismatched lengths.
            Assert.Throws<ArgumentException>(() => new League(
                1UL, 2UL,
                new[] { new Club(0, "A", 0), new Club(1, "B", 0) },
                new[] { squad0 }));

            // Fewer than two clubs.
            Assert.Throws<ArgumentException>(() => new League(
                1UL, 2UL, new[] { new Club(0, "A", 0) }, new[] { squad0 }));
        }

        [Test]
        public void Club_RefusesNegativeIdOrEmptyName()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Club(-1, "A", 0));
            Assert.Throws<ArgumentException>(() => new Club(0, "", 0));
            Assert.Throws<ArgumentException>(() => new Club(0, null, 0));
        }

        // ── CreateSeason handoff to #30 (KD-9) ──────────────────────────────────────

        [Test]
        public void CreateSeason_ProducesAStartableSeasonOverTheLeague()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);
            SeasonState season = league.CreateSeason(managedClubId: 7);

            Assert.AreEqual(20, season.ClubCount);
            Assert.AreEqual(7, season.ManagedClubId);
            Assert.AreEqual(league.SeasonSeed, season.Seed,
                "The season must be generated from the bootstrap's derived season seed (KD-4).");
            Assert.AreEqual(20 * 19, season.FixtureCount, "A 20-club double round-robin is 380 fixtures.");
            Assert.AreEqual(0, season.SeasonNumber);
            Assert.AreEqual(10, season.Board.Objective.TargetPositionOrBetter,
                "The default objective is a top-half finish (KD-9).");

            for (int c = 0; c < league.ClubCount; c++)
            {
                Assert.AreEqual(0, season.TableRow(c).Played, $"club {c} starts on an empty row.");
            }
        }

        [Test]
        public void CreateSeason_RoundTripsThroughTheSeasonCodec()
        {
            // The bootstrap's output must be persistable by #30 T1 as-is — the A3→A2 join.
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);
            SeasonState season = league.CreateSeason(managedClubId: 3);

            SeasonState restored = SeasonStateCodec.Decode(SeasonStateCodec.Encode(season));
            Assert.IsTrue(season.FieldsEqual(restored),
                "A bootstrapped season must round-trip field-identically through the season codec.");
        }

        [Test]
        public void SavedWorldSeed_RebuildsTheSameLeague()
        {
            // The career-resume path (KD-9 / AR-5 M-1). Squads are not persisted: a save carries club
            // IDs, and the ISquadProvider is rebuilt by re-running the bootstrap. The ONLY value a save
            // file holds that makes that possible is the world seed inside the world blob — so if this
            // breaks, a saved career cannot be reopened at all.
            const ulong worldSeed = 0xC0FFEE5EA50117UL;
            League original = LeagueBootstrap.Generate(worldSeed, 6);
            SeasonState season = original.CreateSeason(managedClubId: 2);

            var world = new WorldStore(managerId: 1, worldSeed);
            WorldStore restoredWorld = WorldStore.Restore(world.Snapshot());

            Assert.AreEqual(worldSeed, restoredWorld.WorldSeed,
                "The world seed must survive the save round-trip — it is the league's only anchor.");

            League rebuilt = LeagueBootstrap.Generate(restoredWorld.WorldSeed, season.ClubCount);
            Assert.AreEqual(original.ClubCount, rebuilt.ClubCount);
            Assert.AreEqual(original.SeasonSeed, rebuilt.SeasonSeed);
            for (int c = 0; c < original.ClubCount; c++)
            {
                Assert.AreEqual(original.ClubAt(c).Name, rebuilt.ClubAt(c).Name);
                Assert.AreEqual(original.ClubAt(c).StrengthDelta, rebuilt.ClubAt(c).StrengthDelta);
                AssertSquadsIdentical(
                    original.ResolveByClubId(c), rebuilt.ResolveByClubId(c), $"rebuilt club {c}");
            }
        }

        [Test]
        public void CreateSeason_UnknownManagedClub_FailsLoud()
        {
            // F5 — delegated to SeasonState's own club-set gate.
            League league = LeagueBootstrap.Generate(WorldSeedA, 4);
            Assert.Throws<ArgumentException>(() => league.CreateSeason(managedClubId: 99));
        }

        [Test]
        public void CreateSeason_ExplicitObjective_Overrides()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 20);
            SeasonState season = league.CreateSeason(0, new BoardObjective(1));
            Assert.AreEqual(1, season.Board.Objective.TargetPositionOrBetter);
        }

        // ── Immutability ────────────────────────────────────────────────────────────

        [Test]
        public void League_HandsOutCopies_NotItsOwnState()
        {
            League league = LeagueBootstrap.Generate(WorldSeedA, 4);

            int[] ids = league.ClubIds();
            ids[0] = 999;
            Assert.AreEqual(0, league.ClubIds()[0], "ClubIds() must hand back a fresh array each call.");

            Assert.AreNotSame(league.Clubs, league.Clubs,
                "Clubs must hand back a fresh read-only copy each call.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private static void AssertSquadsIdentical(Squad a, Squad b, string what)
        {
            Assert.AreEqual(a.Count, b.Count, what);
            for (int p = 0; p < a.Count; p++)
            {
                PlayerRecord pa = a.GetPlayer(p);
                PlayerRecord pb = b.GetPlayer(p);
                Assert.AreEqual(pa.PlayerId, pb.PlayerId, what);
                Assert.AreEqual(pa.FirstName, pb.FirstName, what);
                Assert.AreEqual(pa.LastName, pb.LastName, what);
                Assert.AreEqual(pa.Age, pb.Age, what);
                Assert.AreEqual(pa.Position, pb.Position, what);
                Assert.AreEqual(pa.Attributes.WeakFootRating, pb.Attributes.WeakFootRating, what);

                int[] aa = pa.Attributes.ToArray();
                int[] bb = pb.Attributes.ToArray();
                for (int i = 0; i < aa.Length; i++)
                {
                    Assert.AreEqual(aa[i], bb[i], $"{what} player {p} attribute {i}");
                }
            }
        }

        private static double MeanAttribute(Squad squad)
        {
            long sum = 0;
            long n = 0;
            for (int p = 0; p < squad.Count; p++)
            {
                foreach (int v in squad.GetPlayer(p).Attributes.ToArray())
                {
                    sum += v;
                    n++;
                }
            }

            return (double)sum / n;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial suite (roadmap A3).                                        |
#endregion
