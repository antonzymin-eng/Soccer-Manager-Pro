// File:     src/season-save/tests/SeasonLoopCareerTests.cs
// Created:  2026-08-06
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (KD-2 tick order), §3.5 (the boundary), FR-SN-026;
//           Training System #29 §3.5, FR-TR-004/025; Injuries & Medical #41 §3.5,
//           FR-MD-003/022/023/025; ERR-030-002; path-to-playable D2/D3 (T2); Code Standards #20
// Purpose:  Locks the T2 wiring at the composition root: the career and its provider bind as a pair
//           and a mismatched provider is refused, slots 2 and 4 run exactly once per world day in the
//           KD-2 order, the world tick stays byte-identical to the unwired loop, a whole season plays
//           with the career live, and the season boundary reconciles roster membership.

using NUnit.Framework;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonLoopCareerTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, ClubCount);

        /// <summary>The league's own rosters behind a provider whose contents can be changed later.</summary>
        private static CareerTestRoster.MutableSquadProvider ProviderOver(League league)
        {
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] ids = league.ClubIds();
            for (int i = 0; i < ids.Length; i++)
            {
                provider.Set(league.ResolveByClubId(ids[i]));
            }

            return provider;
        }

        private static SeasonLoop WiredLoop(
            League league,
            out WorldStore world,
            out PlayerCareerStates career,
            out CareerTestRoster.MutableSquadProvider provider)
        {
            provider = ProviderOver(league);
            career = PlayerCareerStates.ForLeague(provider, league.ClubIds());
            world = new WorldStore(ManagerId, WorldSeed);
            return new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);
        }

        // ── binding ────────────────────────────────────────────────────────────────────────

        [Test]
        public void Constructor_RefusesAHalfSuppliedCareerPair()
        {
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds());

            Assert.Throws<System.ArgumentException>(() => new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, career, null));

            Assert.Throws<System.ArgumentException>(() => new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, null, provider));
        }

        [Test]
        public void Constructor_RefusesACareerThatDoesNotCoverTheSeason()
        {
            // A career over a subset constructs, advances days and rolls seasons without complaint —
            // the day steps iterate the CAREER's clubs, not the season's — and then throws from the
            // availability filter part-way through a round, after earlier fixtures in that round have
            // already been applied to the table and marked played. This is the only layer holding both,
            // so it is the only layer that can refuse the pairing.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            int[] allClubs = league.ClubIds();
            var subset = new int[allClubs.Length - 1];
            System.Array.Copy(allClubs, subset, subset.Length);

            PlayerCareerStates partial = PlayerCareerStates.ForLeague(provider, subset);

            Assert.Throws<System.ArgumentException>(() => new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, partial, provider));
        }

        [Test]
        public void AdvanceAndPlayNextRound_WithADifferentProvider_FailsLoud()
        {
            // Two providers would train one league and resolve fixtures against another, and every
            // symptom of that is a plausible-looking table rather than a crash.
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(league, out _, out _, out _);
            loop.AdvanceToNextFixtureDay();

            Assert.Throws<System.ArgumentException>(() => loop.AdvanceAndPlayNextRound(league));
        }

        [Test]
        public void UnwiredLoop_ExposesNoCareer()
        {
            League league = FourClubLeague();
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll);

            Assert.IsNull(loop.Career);
            loop.AdvanceToNextFixtureDay();
            Assert.DoesNotThrow(() => loop.AdvanceAndPlayNextRound(league),
                "An unwired loop must keep accepting any provider — the guard belongs to the career.");
        }

        // ── the day steps ──────────────────────────────────────────────────────────────────

        [Test]
        public void DayAdvance_RunsBothStepsExactlyOncePerWorldDay()
        {
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out WorldStore world, out PlayerCareerStates career,
                out CareerTestRoster.MutableSquadProvider provider);

            loop.AdvanceDays(5);

            Assert.AreEqual(5u, world.CurrentWorldTick);

            // The reference: the same five days driven directly, one call per day. Comparing against it
            // catches an off-by-one in either direction — four days, six days, or two steps per day —
            // without this fixture re-deriving #29's conditioning arithmetic and drifting from it.
            PlayerCareerStates reference =
                PlayerCareerStates.ForLeague(provider, league.ClubIds());
            for (uint day = 0; day < 5; day++)
            {
                reference.AdvanceTrainingDay(day, provider, CoachingModifier.Identity);
                reference.AdvanceMedicalDay(
                    day, world.WorldSeed, provider, MedicalModifier.Identity);
            }

            for (int c = 0; c < career.ClubCount; c++)
            {
                ClubTrainingStates training = career.TrainingBlocks()[c];
                ClubTrainingStates expected = reference.TrainingBlocks()[c];
                ClubInjuryStates medical = career.MedicalBlocks()[c];

                for (int i = 0; i < training.Count; i++)
                {
                    Assert.AreEqual(4u, training.States[i].LastAdvancedWorldDay,
                        "The steps take the day BEFORE the clock's increment, so after five days the "
                        + "last advanced day is 4 — one behind the clock, which is what makes a save "
                        + "taken between ticks restore without a phantom gap.");
                    Assert.AreEqual(4u, medical.States[i].LastAdvancedWorldDay);
                    Assert.AreEqual(expected.States[i].Condition, training.States[i].Condition,
                        "Five days of conditioning, not four and not six.");
                }
            }
        }

        [Test]
        public void DayAdvance_LeavesTheWorldByteIdenticalToABareAdvance()
        {
            // FR-SN-026 / KD-8: neither day step touches the WorldStore — they mutate only the career
            // state, which lives in its own sub-blobs. Wiring T2 must not have moved the world tick.
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(league, out WorldStore throughLoop, out _, out _);
            var bare = new WorldStore(ManagerId, WorldSeed);

            // Seven days is the whole gap to the first fixture — the KD-4 guard refuses to step past
            // it, so this is the longest uninterrupted run of no-fixture days a season has.
            loop.AdvanceDays(7);
            for (int i = 0; i < 7; i++)
            {
                bare.AdvanceDay();
            }

            Assert.AreEqual(bare.Snapshot(), throughLoop.Snapshot(),
                "The career steps must leave the world exactly as the unwired loop left it.");
        }

        [Test]
        public void DayAdvance_OnTheDefaultFocus_ProjectsZeroMatchEntryFatigue()
        {
            // The whole-landing neutrality claim, asserted where it matters: an untouched career hands
            // the engine an all-rested array, so a match booted through T2's wiring is byte-identical
            // to one booted without it.
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out _, out PlayerCareerStates career, out CareerTestRoster.MutableSquadProvider provider);

            for (int round = 0; round < 4; round++)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(provider);
            }

            float[] fatigue = career.MatchEntryFatigue(provider.ResolveByClubId(0));
            for (int i = 0; i < fatigue.Length; i++)
            {
                Assert.AreEqual(0f, fatigue[i], 0f);
            }
        }

        // ── a whole season with the career live ────────────────────────────────────────────

        [Test]
        public void AWholeSeason_PlaysWithTheCareerWired()
        {
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out WorldStore world, out PlayerCareerStates career,
                out CareerTestRoster.MutableSquadProvider provider);

            int rounds = 0;
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(provider);
                rounds++;
            }

            Assert.AreEqual(loop.State.Calendar.RoundCount, rounds);

            // Every player advanced up to the last day the clock passed through — no gaps, no double
            // accrual, across a season's worth of days and a whole league's worth of players.
            uint lastLivedDay = world.CurrentWorldTick - 1u;
            for (int c = 0; c < career.ClubCount; c++)
            {
                ClubTrainingStates block = career.TrainingBlocks()[c];
                for (int i = 0; i < block.Count; i++)
                {
                    Assert.AreEqual(lastLivedDay, block.States[i].LastAdvancedWorldDay);
                }
            }
        }

        // ── the season boundary ────────────────────────────────────────────────────────────

        [Test]
        public void RollToNextSeason_ReconcilesRosterMembership()
        {
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out _, out PlayerCareerStates career,
                out CareerTestRoster.MutableSquadProvider provider);

            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(provider);
            }

            Squad before = provider.ResolveByClubId(0);
            int departedId = before.GetPlayer(before.Count - 1).PlayerId;
            int carriedId = before.GetPlayer(0).PlayerId;
            int carriedCondition = career.TrainingView(0, carriedId).Condition;
            int generationBefore = career.RosterGeneration;

            // Stand in for #28's season-boundary churn, which is unwired (roadmap D1): club 0 loses its
            // last player (a retirement), and club 1 swaps its last player for a fresh id (a retirement
            // AND a regen in one block). Both directions in one roll, because FR-TR-025 / FR-MD-025
            // specify both and only removal was covered here before. A club cannot simply grow — the
            // roster is already at CLUB_SQUAD_SIZE — which is itself why the swap is the honest shape.
            provider.Set(CareerTestRoster.Build(0, before.Count - 1));

            Squad club1 = provider.ResolveByClubId(1);
            var swappedLocals = new int[club1.Count];
            for (int k = 0; k < swappedLocals.Length - 1; k++)
            {
                swappedLocals[k] = k;
            }
            swappedLocals[swappedLocals.Length - 1] = PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 5;
            int retiredFromClub1 = club1.GetPlayer(club1.Count - 1).PlayerId;
            provider.Set(CareerTestRoster.Build(1, club1.Count, swappedLocals));
            int regenId = provider.ResolveByClubId(1).GetPlayer(club1.Count - 1).PlayerId;

            loop.RollToNextSeason();

            Assert.AreEqual(before.Count - 1, career.TrainingBlocks()[0].Count,
                "The boundary must drop the departed player's entry (FR-TR-025) — otherwise the block "
                + "leaks entries unboundedly across seasons.");
            Assert.Throws<System.ArgumentException>(() => career.MedicalView(0, departedId));
            Assert.AreEqual(carriedCondition, career.TrainingView(0, carriedId).Condition,
                "A departure must not disturb anybody else's accrued state.");

            Assert.AreEqual(club1.Count, career.TrainingBlocks()[1].Count,
                "One out, one in — the block's size is unchanged.");
            Assert.Throws<System.ArgumentException>(() => career.MedicalView(1, retiredFromClub1),
                "The retiree's entry must be gone (FR-MD-025).");
            Assert.AreEqual(
                TrainingSystemConstants.ConditionStart,
                career.TrainingView(1, regenId).Condition,
                "A regen must arrive via TrainingState.Create — at ConditionStart, not at the "
                + "season's worth of conditioning his predecessor had accrued (FR-TR-025).");
            Assert.AreNotEqual(
                TrainingSystemConstants.ConditionStart,
                career.TrainingView(1, provider.ResolveByClubId(1).GetPlayer(0).PlayerId).Condition,
                "Precondition for the assertion above: a CARRIED player is well past ConditionStart "
                + "after a full season, so the regen's value is discriminating.");
            Assert.IsTrue(career.IsAvailable(1, regenId));

            Assert.Greater(career.RosterGeneration, generationBefore,
                "The boundary replaced the arrays, so any cached TrainingSchedule is now detached.");
        }

        [Test]
        public void RollToNextSeason_WithAnUnresolvableClub_LeavesTheSeasonUntouched()
        {
            // SyncToRoster runs at the (d′) position, before the commits, so a provider that cannot
            // answer must leave BOTH the season and the career exactly as they were — the roll's own
            // validate-everything-before-writing-anything discipline.
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out _, out PlayerCareerStates career,
                out CareerTestRoster.MutableSquadProvider provider);

            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(provider);
            }

            int seasonNumber = loop.State.SeasonNumber;
            int carried = career.TrainingBlocks()[0].Count;
            provider.Remove(league.ClubIds()[ClubCount - 1]);

            Assert.Throws<System.ArgumentException>(() => loop.RollToNextSeason());
            Assert.AreEqual(seasonNumber, loop.State.SeasonNumber,
                "A refused roll must not have begun the next season.");
            Assert.AreEqual(carried, career.TrainingBlocks()[0].Count,
                "…nor half-synced the career.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial (#29/#41 T2): the pair binding and the same-provider      |
// |         |            |        | guard, one day step per world day taking the pre-increment day,   |
// |         |            |        | the FR-SN-026 world-tick floor still holding, a whole season      |
// |         |            |        | played with the career live, and the (d′) boundary reconciliation |
// |         |            |        | including its refuse-cleanly path.                                |
#endregion
