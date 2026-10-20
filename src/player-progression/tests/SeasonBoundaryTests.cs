// File:     src/player-progression/tests/SeasonBoundaryTests.cs
// Created:  2026-10-19
// Modified: 2026-10-20 (AR round 3 L7: the regen pin covers every drawn value + the lifecycle; the tautological stream-index assert removed - v1.3)
//           (AR round 2 M8/M10: season-0 seed golden, ClubB stream, regen pin - v1.2)
//           (AR round 1 P-M2: golden seed, non-index club ids, cursor exhaustion - v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.3 / §3.4 (as amended by ERR-028-024 / ERR-028-025),
//           FR-PG-010/011/015/019/020/024, §5 T-PG-RET-002/003; Code Standards #20
// Purpose:  Locks the staged season boundary: preparation writes nothing, a commit replaces every
//           retiree 1:1 in his own position with a fresh monotonic id, the draw and id orders are the
//           normative ones (re-derived independently, not read back from the code under test), the
//           regens differ between boundaries, and a stale or foreign plan is refused. AR round 1 P-M2:
//           the seed derivation is pinned to an independently computed golden value and to the world
//           seed, the fixture's club ids are not the array indices (so a stream keyed on the loop index
//           fails), and an exhausted id cursor is refused.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class SeasonBoundaryTests
    {
        private const ulong WorldSeed = 0xB0DA2028UL;
        private const uint Day = 400;

        // Deliberately NOT 0 and 1: with ids equal to array indices, `entityId: c` and `entityId: clubIds[c]`
        // are the same program, and the draw-order lock below could not tell them apart (AR round 1 P-M2).
        private const int ClubA = 3;
        private const int ClubB = 7;

        // ── staging + commit ─────────────────────────────────────────────────────────

        [Test]
        public void Prepare_WritesNothing()
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            byte[] before = store.Snapshot();

            store.PrepareSeasonBoundary(WorldSeed, 1, Day);

            Assert.That(store.Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void Commit_ReplacesEveryRetiree_OneForOne_InHisPosition_WithFreshIds()
        {
            ProgressionEngine store = StoreWithRetirees(out Dictionary<int, PlayerPosition> retireePositions);
            int cursorBefore = store.NextPlayerId;

            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 1, Day);
            store.CommitSeasonBoundary(plan);

            Assert.That(plan.RetiredPlayerIds.Count, Is.EqualTo(retireePositions.Count));
            Assert.That(store.NextPlayerId, Is.EqualTo(cursorBefore + retireePositions.Count));
            for (int i = 0; i < plan.RetiredPlayerIds.Count; i++)
            {
                int club = plan.RegenClubIds[i];
                Squad squad = store.SquadFor(club);
                Assert.That(IndexOf(squad, plan.RetiredPlayerIds[i]), Is.LessThan(0), "the retiree is gone");
                int r = IndexOf(squad, plan.RegenPlayerIds[i]);
                Assert.That(r, Is.GreaterThanOrEqualTo(0), "the regen joined the retiree's club");
                Assert.That(squad.GetPlayer(r).Position, Is.EqualTo(retireePositions[plan.RetiredPlayerIds[i]]),
                    "ERR-028-024: the regen takes the retiree's position");
                Assert.That(plan.RegenPlayerIds[i], Is.GreaterThanOrEqualTo(cursorBefore), "FR-PG-011: a fresh id");
                LifecycleViewModel view = store.LifecycleView(club, plan.RegenPlayerIds[i]);
                Assert.That(view.RetirementFlag, Is.False);
            }

            Assert.That(store.SquadFor(ClubA).Count, Is.EqualTo(5), "FR-PG-019: the club size is unchanged");
            Assert.That(store.SquadFor(ClubB).Count, Is.EqualTo(5));
        }

        [Test]
        public void Commit_KeepsTheStoreInstance_SoProjectionsSeeTheNewRoster()
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 1, Day);
            Squad planned = plan.PlannedSquadFor(ClubB);

            store.CommitSeasonBoundary(plan);

            Squad live = store.SquadFor(ClubB);
            Assert.That(live.Count, Is.EqualTo(planned.Count));
            for (int i = 0; i < live.Count; i++)
            {
                Assert.That(live.GetPlayer(i).PlayerId, Is.EqualTo(planned.GetPlayer(i).PlayerId),
                    "what #30 staged against is exactly what was installed");
            }
        }

        [Test]
        public void TheIdAndDrawOrders_AreTheNormativeOnes()
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            int cursor = store.NextPlayerId;

            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 3, Day);

            // Id order: clubs ascending, then retirees by ascending PlayerId within the club.
            Assert.That(plan.RetiredPlayerIds, Is.EqualTo(new[] { 76, 78, 175 }));
            Assert.That(plan.RegenPlayerIds, Is.EqualTo(new[] { cursor, cursor + 1, cursor + 2 }));
            Assert.That(plan.RegenClubIds, Is.EqualTo(new[] { ClubA, ClubA, ClubB }));

            // Draw order: a club's k-th retiree consumes its stream's k-th reservation. Re-derived here
            // from the public generator, not read back from PrepareSeasonBoundary.
            var rng = new DeterministicRngService(ProgressionEngine.DeriveRegenSeed(WorldSeed, 3));
            int clubAStream = rng.RegisterStream(
                PlayerProgressionConstants.REGEN_STREAM_SITE_ID,
                SubsystemOrdinals.PlayerProgression,
                entityId: ClubA,
                streamVersion: PlayerProgressionConstants.REGEN_STREAM_VERSION);
            (PlayerRecord first, _) = RegenGenerator.GenerateRegen(
                rng, clubAStream, ClubA, cursor, PlayerPosition.Defender, Day);
            (PlayerRecord second, _) = RegenGenerator.GenerateRegen(
                rng, clubAStream, ClubA, cursor + 1, PlayerPosition.Forward, Day);

            Squad planned = plan.PlannedSquadFor(ClubA);
            AssertSameIdentity(first, planned.GetPlayer(IndexOf(planned, cursor)));
            AssertSameIdentity(second, planned.GetPlayer(IndexOf(planned, cursor + 1)));

            // FR-PG-020 is PER CLUB: ClubB has its own stream (entityId = ClubB), so ClubB's one retiree takes
            // THAT stream's FIRST reservation - not the third reservation of ClubA's stream, which is what a
            // registration hoisted out of the club loop (one stream serving every club) would hand him (AR round 2
            // M10).
            int clubBStream = rng.RegisterStream(
                PlayerProgressionConstants.REGEN_STREAM_SITE_ID,
                SubsystemOrdinals.PlayerProgression,
                entityId: ClubB,
                streamVersion: PlayerProgressionConstants.REGEN_STREAM_VERSION);
            (PlayerRecord third, _) = RegenGenerator.GenerateRegen(
                rng, clubBStream, ClubB, cursor + 2, PlayerPosition.Goalkeeper, Day);

            Squad plannedB = plan.PlannedSquadFor(ClubB);
            AssertSameIdentity(third, plannedB.GetPlayer(IndexOf(plannedB, cursor + 2)));
        }

        [Test]
        public void TheFirstRegen_IsPinnedAsAnAbsoluteGolden_SoADrawOrderChangeMustBumpTheStreamVersion()
        {
            // A CHARACTERIZATION pin, READ BACK FROM THE CODE at REGEN_STREAM_VERSION = 1 (it was not computed
            // independently, unlike the seed goldens above): ClubA's first regen at (0xB0DA2028, season 3,
            // day 400). The tests above re-derive draws from the same generator, so a change to the draw
            // ORDER inside GenerateRegen moves both sides together and passes; this pin fails it. AR round 3
            // L7: the first form pinned only names, age, position and three attributes (7 of ~37 values), so
            // moving DrawPotentialAbility onto the reserved index 3, or swapping any two attribute draw
            // indices outside Pace/Passing/Finishing, passed. It now covers EVERY drawn value: the names,
            // age, position, weak foot, all 31 [1,20] attributes in AttrIdx order, and the lifecycle (PA, CA,
            // birth day, growth cursor, last-advanced day). Changing the draw order is a stream-version bump
            // (#16 SS3.2.5.1) - re-pin it here in the same commit.
            ProgressionEngine store = StoreWithRetirees(out _);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 3, Day);
            int regenId = plan.RegenPlayerIds[0];
            store.CommitSeasonBoundary(plan);

            ClubCareerStates club = default;
            foreach (ClubCareerStates c in store.ToBlocks())
            {
                if (c.ClubId == ClubA)
                {
                    club = c;
                }
            }

            int index = -1;
            for (int p = 0; p < club.Count; p++)
            {
                if (club.Records[p].PlayerId == regenId)
                {
                    index = p;
                }
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0), "the first regen is on ClubA's committed roster");
            PlayerRecord regen = club.Records[index];
            PlayerLifecycle life = club.Lifecycles[index];

            Assert.That(PlayerProgressionConstants.REGEN_STREAM_VERSION, Is.EqualTo((ushort)1),
                "a stream-version bump means the pin below is re-derived on purpose");
            string actual = regen.FirstName + "|" + regen.LastName + "|" + regen.Age + "|" + regen.Position
                + "|wf" + regen.Attributes.WeakFootRating
                + "|" + string.Join(",", regen.Attributes.ToArray())
                + "|pa" + life.PotentialAbility + "|ca" + life.CurrentAbility
                + "|birth" + life.BirthWorldDay + "|cursor" + life.GrowthCursor
                + "|last" + life.LastAdvancedWorldDay;
            Assert.That(
                actual,
                Is.EqualTo("Anthony|Sanchez|19|Defender|wf1|13,6,11,8,14,6,10,6,6,7,10,8,13,12,14,11,9,6,9,10,13,13,10,10,11,13,15,17,8,7,13"
                    + "|pa9403|ca5486|birth-6535|cursor1|last400"));
        }

        [Test]
        public void SuccessiveBoundaries_DrawDifferentRegens()
        {
            PlayerRecord a = FirstRegen(completedSeason: 1);
            PlayerRecord b = FirstRegen(completedSeason: 2);

            bool differs = a.FirstName != b.FirstName || a.LastName != b.LastName || a.Age != b.Age
                || a.Attributes.Pace != b.Attributes.Pace || a.Attributes.Passing != b.Attributes.Passing;
            Assert.That(differs, Is.True,
                "the completed season number is folded into the seed — a boundary-scoped stream that reset "
                + "to the same seed every season would hand every club the same regen forever");
        }

        [Test]
        public void DeriveRegenSeed_MatchesTheIndependentlyComputedGolden()
        {
            // Computed OUTSIDE this codebase with a Python SplitMix64 mirror (no UL suffixes, every
            // intermediate masked with & 0xFFFFFFFFFFFFFFFF):
            //   h    = mix(0x20 ^ 0xB0DA2028)  = 0x0B9A85760C24CF95
            //   seed = mix(h ^ 3)              = 0x443E7491A6459B6A
            // A mutant that drops the tag, the world seed, the season, or reorders the folds moves this.
            Assert.That(ProgressionEngine.DeriveRegenSeed(WorldSeed, 3), Is.EqualTo(0x443E7491A6459B6AUL));
        }

        [Test]
        public void DeriveRegenSeed_AtSeasonZero_MatchesTheIndependentlyComputedGolden()
        {
            // A career's FIRST boundary closes season 0 (SeasonState.CreateNew's default seasonNumber; ERR-028-027
            // corrected the spec's `>= 1`). Same masked Python SplitMix64 mirror as above:
            //   h    = mix(0x20 ^ 0xB0DA2028) = 0x0B9A85760C24CF95   (x ^ 0 == x, so h is the season-3 vector's)
            //   seed = mix(h ^ 0)             = 0xD3E409D7732EBE4F
            Assert.That(ProgressionEngine.DeriveRegenSeed(WorldSeed, 0), Is.EqualTo(0xD3E409D7732EBE4FUL));
            Assert.That(ProgressionEngine.DeriveRegenSeed(WorldSeed, 0),
                Is.Not.EqualTo(ProgressionEngine.DeriveRegenSeed(WorldSeed, 1)));
        }

        [Test]
        public void TwoWorlds_SameSeason_DrawDifferentRegens()
        {
            PlayerRecord a = FirstRegen(completedSeason: 3, worldSeed: WorldSeed);
            PlayerRecord b = FirstRegen(completedSeason: 3, worldSeed: WorldSeed + 1UL);

            Assert.That(ProgressionEngine.DeriveRegenSeed(WorldSeed, 3),
                Is.Not.EqualTo(ProgressionEngine.DeriveRegenSeed(WorldSeed + 1UL, 3)));
            bool differs = a.FirstName != b.FirstName || a.LastName != b.LastName || a.Age != b.Age
                || a.Attributes.Pace != b.Attributes.Pace || a.Attributes.Passing != b.Attributes.Passing;
            Assert.That(differs, Is.True,
                "the world seed is folded into the regen seed - two careers must not hand out the same regens");
        }

        [Test]
        public void TwoStoresFromTheSameSave_StageIdenticalBoundaries()
        {
            byte[] blob = StoreWithRetirees(out _).Snapshot();
            SeasonBoundaryPlan x = ProgressionEngine.Restore(blob).PrepareSeasonBoundary(WorldSeed, 5, Day);
            SeasonBoundaryPlan y = ProgressionEngine.Restore(blob).PrepareSeasonBoundary(WorldSeed, 5, Day);

            foreach (int club in new[] { ClubA, ClubB })
            {
                Squad sx = x.PlannedSquadFor(club);
                Squad sy = y.PlannedSquadFor(club);
                for (int i = 0; i < sx.Count; i++)
                {
                    AssertSameIdentity(sx.GetPlayer(i), sy.GetPlayer(i));
                }
            }
        }

        [Test]
        public void NoRetirees_IsAnEmptyBoundary()
        {
            ProgressionEngine store = ProgressionEngine.SeedFrom(new[] { Club(ClubA), Club(ClubB) }, Day);
            byte[] before = store.Snapshot();

            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 1, Day);
            store.CommitSeasonBoundary(plan);

            Assert.That(plan.RetiredPlayerIds.Count, Is.EqualTo(0));
            Assert.That(store.Snapshot(), Is.EqualTo(before));
        }

        // ── refusals ─────────────────────────────────────────────────────────────────

        [Test]
        public void Commit_AfterTheStoreMutated_IsRefused()
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 1, Day);
            store.AdvanceDay(Day + 1, TrainingInputBatch.Neutral);
            byte[] advanced = store.Snapshot();

            Assert.Throws<ArgumentException>(() => store.CommitSeasonBoundary(plan));
            Assert.That(store.Snapshot(), Is.EqualTo(advanced), "a refused commit writes nothing");
        }

        [Test]
        public void Commit_OfAPlanFromAnotherStore_IsRefused()
        {
            ProgressionEngine a = StoreWithRetirees(out _);
            ProgressionEngine b = StoreWithRetirees(out _);

            Assert.Throws<ArgumentException>(() => b.CommitSeasonBoundary(a.PrepareSeasonBoundary(WorldSeed, 1, Day)));
        }

        [Test]
        public void Commit_Twice_IsRefused_SoABoundaryCannotApplyTwice()
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(WorldSeed, 1, Day);
            store.CommitSeasonBoundary(plan);

            Assert.Throws<ArgumentException>(() => store.CommitSeasonBoundary(plan));
        }

        [Test]
        public void Prepare_WithAnExhaustedIdCursor_IsRefused()
        {
            ProgressionEngine seeded = StoreWithRetirees(out _);
            ProgressionEngine exhausted = ProgressionEngine.FromBlocks(seeded.ToBlocks(), int.MaxValue);

            Assert.Throws<InvalidOperationException>(() => exhausted.PrepareSeasonBoundary(WorldSeed, 1, Day));
        }

        [Test]
        public void Prepare_OnTheSentinelDay_IsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StoreWithRetirees(out _).PrepareSeasonBoundary(
                WorldSeed, 1, PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL));
        }

        // ── fixtures ─────────────────────────────────────────────────────────────────

        // Two five-man clubs: ClubA = 3 (player ids 75..79) and ClubB = 7 (175..179) - club ids that are not
        // their array indices. Retirees: 76 (DF) and 78 (FW) in ClubA, 175 (GK) in ClubB.
        private static ProgressionEngine StoreWithRetirees(out Dictionary<int, PlayerPosition> retirees)
        {
            ProgressionEngine seeded = ProgressionEngine.SeedFrom(new[] { Club(ClubA), Club(ClubB) }, Day);
            ClubCareerStates[] blocks = seeded.ToBlocks();
            retirees = new Dictionary<int, PlayerPosition>();
            Flag(blocks[0], 76, retirees);
            Flag(blocks[0], 78, retirees);
            Flag(blocks[1], 175, retirees);
            return ProgressionEngine.FromBlocks(blocks, seeded.NextPlayerId);
        }

        private static void Flag(ClubCareerStates club, int playerId, Dictionary<int, PlayerPosition> retirees)
        {
            for (int p = 0; p < club.Count; p++)
            {
                if (club.Records[p].PlayerId == playerId)
                {
                    club.Lifecycles[p].RetirementFlag = true;
                    club.Lifecycles[p].RetirementDay = club.Lifecycles[p].LastAdvancedWorldDay;
                    retirees.Add(playerId, club.Records[p].Position);
                }
            }
        }

        private static Squad Club(int clubId)
        {
            PlayerPosition[] positions =
            {
                PlayerPosition.Goalkeeper, PlayerPosition.Defender, PlayerPosition.Midfielder,
                PlayerPosition.Forward, PlayerPosition.Midfielder
            };
            var players = new PlayerRecord[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                players[i] = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + i);
                players[i].Position = positions[i];
            }

            return new Squad(clubId, players);
        }

        private static PlayerRecord FirstRegen(int completedSeason, ulong worldSeed = WorldSeed)
        {
            ProgressionEngine store = StoreWithRetirees(out _);
            SeasonBoundaryPlan plan = store.PrepareSeasonBoundary(worldSeed, completedSeason, Day);
            Squad squad = plan.PlannedSquadFor(plan.RegenClubIds[0]);
            return squad.GetPlayer(IndexOf(squad, plan.RegenPlayerIds[0]));
        }

        private static int IndexOf(Squad squad, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void AssertSameIdentity(PlayerRecord expected, PlayerRecord actual)
        {
            Assert.That(actual.PlayerId, Is.EqualTo(expected.PlayerId));
            Assert.That(actual.FirstName, Is.EqualTo(expected.FirstName));
            Assert.That(actual.LastName, Is.EqualTo(expected.LastName));
            Assert.That(actual.Age, Is.EqualTo(expected.Age));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            Assert.That(actual.Attributes.ToArray(), Is.EqualTo(expected.Attributes.ToArray()));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-19 | —      | Initial implementation (ERR-028-024 / ERR-028-025): staged      |
// |         |            |        | season-boundary locks.                                          |
// | 1.1     | 2026-10-20 | —      | AR round 1 P-M2 (ERR-028-025): + DeriveRegenSeed golden (0x443E7491A6459B6A for (0xB0DA2028, 3), computed with an independent Python SplitMix64 mirror) and TwoWorlds_SameSeason_DrawDifferentRegens, so dropping the world seed fails; fixture clubs are now 3 and 7 (ids were array indices), so keying the regen stream on the loop index fails the draw-order lock; + Prepare_WithAnExhaustedIdCursor_IsRefused (NextPlayerId == int.MaxValue). |
// | 1.2     | 2026-10-20 | —      | AR round 2 (ERR-028-027): M8 + DeriveRegenSeed_AtSeasonZero golden (0xD3E409D7732EBE4F for (0xB0DA2028, 0), independent masked Python mirror) - a career's first boundary closes season 0; M10 TheIdAndDrawOrders now registers a separate ClubB stream and asserts ClubB's regen is that stream's first reservation (a registration hoisted out of the club loop fails it) + TheFirstRegen_IsPinnedAsAnAbsoluteGolden (characterization pin, so a draw-order change without a REGEN_STREAM_VERSION bump fails). |
// | 1.3     | 2026-10-20 | —      | AR round 3 L7: TheFirstRegen_IsPinnedAsAnAbsoluteGolden… pins ALL 31 attributes + weak foot + names/age/position and the lifecycle (PA, CA, BirthWorldDay, GrowthCursor, LastAdvancedWorldDay) as a characterization string read back from the code (was 7 of ~37 values; a PA draw on the reserved index 3 passed). Removed the tautological clubBStream != clubAStream assert. |
#endregion
