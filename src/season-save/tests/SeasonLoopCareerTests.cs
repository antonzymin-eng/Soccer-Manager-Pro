// File:     src/season-save/tests/SeasonLoopCareerTests.cs
// Created:  2026-08-06
// Modified: 2026-08-08
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
using TacticalDirector.MatchEngine;
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
            career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
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
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);

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

            PlayerCareerStates partial = PlayerCareerStates.ForLeague(provider, subset, injuryOccurrenceEnabled: false);

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
                PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
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
        public void DayAdvance_StopsBeforeTheFixtureDaysOwnSteps()
        {
            // ERR-030-026 → ERR-030-027. KD-2 pins the order of the nine day-slots but has no slot
            // for "play the round" — a round is a separate command — so where the fixture sits
            // relative to slots 2 and 4 had to be pinned, not left to fall out of a loop condition.
            // The pinned convention: AdvanceToNextFixtureDay still stops on REACHING the fixture day
            // WITHOUT running its steps, and AdvanceAndPlayNextRound runs them itself, PRE-round — so
            // the recovery countdown lands before selection (a player who served his time plays
            // today) and the occurrence draw sits on matchday morning, fed by the FR-MD-010
            // appearance window, which never contains today. The post-round re-run of the same day
            // inside the next advance must be a cursor no-op, or matchday would be lived twice.
            League league = FourClubLeague();
            SeasonLoop loop = WiredLoop(
                league, out WorldStore world, out PlayerCareerStates career,
                out CareerTestRoster.MutableSquadProvider provider);

            uint fixtureDay = loop.State.Calendar.NextFixtureDay();
            Assert.Greater(fixtureDay, 0u, "Precondition: the opening round is not on day 0.");

            loop.AdvanceToNextFixtureDay();

            Assert.AreEqual(fixtureDay, world.CurrentWorldTick,
                "The clock must sit ON the fixture day (§3.3).");

            int playerId = provider.ResolveByClubId(league.ClubIds()[0]).GetPlayer(0).PlayerId;
            Assert.AreEqual(
                fixtureDay - 1u,
                career.TrainingBlocks()[0].States[0].LastAdvancedWorldDay,
                "AdvanceToNextFixtureDay leaves matchday's own steps unrun — they belong to "
                + "AdvanceAndPlayNextRound (ERR-030-027).");
            Assert.AreEqual(
                fixtureDay - 1u,
                career.MedicalBlocks()[0].States[0].LastAdvancedWorldDay,
                "…and the same for #41.");

            // The convention's consequence, made concrete: a player whose recovery expires ON the
            // fixture day serves exactly his tier — the pre-round step heals him and he is available
            // for the round his tier said he would make.
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Minor;
            injured.RecoveryRemaining = 1;
            career.SetMedicalState(league.ClubIds()[0], playerId, in injured);

            Assert.IsFalse(career.IsAvailable(league.ClubIds()[0], playerId),
                "One day of recovery outstanding as the clock reaches matchday: still injured, "
                + "because matchday's own step has not run yet.");

            loop.AdvanceAndPlayNextRound(provider);

            Assert.IsTrue(career.IsAvailable(league.ClubIds()[0], playerId),
                "The pre-round day-step ran his last recovery day before selection — available for "
                + "THIS round, not the next one (ERR-030-027).");
            Assert.AreEqual(
                fixtureDay,
                career.MedicalBlocks()[0].States[0].LastAdvancedWorldDay,
                "Matchday's own steps ran inside AdvanceAndPlayNextRound.");

            // And matchday is lived exactly once: the next advance re-enters the same world day,
            // whose career steps must be a cursor no-op — the training accumulator would otherwise
            // accrue a second day of load for one calendar day.
            int fatigueAfterRound = career.TrainingBlocks()[0].States[0].TrainingFatigue;
            int conditionAfterRound = career.TrainingBlocks()[0].States[0].Condition;
            loop.AdvanceDays(1);

            Assert.AreEqual(fatigueAfterRound, career.TrainingBlocks()[0].States[0].TrainingFatigue,
                "The post-round re-run of the fixture day's steps must be idempotent (F6) — a second "
                + "accrual would live matchday twice.");
            Assert.AreEqual(conditionAfterRound, career.TrainingBlocks()[0].States[0].Condition,
                "…and the same for the conditioning cursor.");
            Assert.AreEqual(fixtureDay + 1u, world.CurrentWorldTick,
                "…while the clock itself still advances past matchday.");
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

        // ── the ENGINE path with a career wired ────────────────────────────────────────────
        //
        // Every other test in this fixture runs QuickSimAll, and every engine-mode test in this
        // assembly constructs the loop without a career — so until these three landed, SeasonLoop's
        // career-wired match boot (the sole production call site of #29's entry-fatigue seam, and the
        // only place the availability filter meets a real MatchEngine) had never executed anywhere.
        // They go through BootFixtureEngine rather than AdvanceAndPlayNextRound so the assertions cost
        // milliseconds instead of a 90-minute match, which is why that seam is internal.

        /// <summary>The first fixture of round 0, and the club playing at home in it.</summary>
        private static Fixture FirstFixture(SeasonLoop loop) => loop.State.FixtureAt(0);

        [Test]
        public void EnginePath_SeedsMatchEntryFatigueOntoTheStartersItSelected()
        {
            League league = FourClubLeague();
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] clubIds = league.ClubIds();
            for (int i = 0; i < clubIds.Length; i++)
            {
                provider.Set(CareerTestRoster.Build(clubIds[i], PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            }

            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, clubIds, injuryOccurrenceEnabled: false);
            var world = new WorldStore(ManagerId, WorldSeed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.FullEngine, career, provider);

            Fixture fixture = FirstFixture(loop);
            Squad home = provider.ResolveByClubId(fixture.HomeClubId);

            // Fatigue only the SECOND half of the roster. The point is the index space: the array
            // ConfigureSquads takes is keyed by squad-local roster index, NOT by team slot, and the
            // selector fills the outfield slots from the highest-rated players — which in this fixture
            // are the high locals, because Pace ramps with the roster slot. So if the seam ever read
            // entryFatigue[slot] instead of entryFatigue[local], every starter would come up rested and
            // this test fails. Nothing here re-derives which eleven is picked; the assertion holds for
            // any selection that reaches past local 10, and a lineup that did not would be a different
            // bug caught by LineupSelectorTests.
            for (int local = 11; local < home.Count; local++)
            {
                Assert.IsTrue(
                    career.TrySetFocus(fixture.HomeClubId, home.GetPlayer(local).PlayerId,
                        TrainingFocus.Physical),
                    "Precondition: the focus command must accept a player on the club's roster.");
            }

            // Seven days is the whole run-up to the first fixture — the KD-4 guard refuses to step past
            // it — so this is the most training a club can bring to the opening round.
            loop.AdvanceDays((int)LeagueBootstrapConstants.FirstRoundDay);

            float[] projected = career.MatchEntryFatigue(home);
            Assert.Greater(projected[home.Count - 1], 0f,
                "Precondition: a week of Physical work must project non-zero match-entry fatigue.");
            Assert.AreEqual(0f, projected[0], 0f,
                "Precondition: a Balanced team-mate must still project zero.");

            MatchEngine.MatchEngine engine = loop.BootFixtureEngine(in fixture, provider);

            int fatiguedStarters = 0;
            for (int slot = 0; slot < MatchEngineConstants.PLAYERS_PER_TEAM; slot++)
            {
                if (engine.AgentView(slot).AerobicPool < 1f)
                {
                    fatiguedStarters++;
                }
            }

            Assert.Greater(fatiguedStarters, 0,
                "No home starter arrived tired. The entry-fatigue array is keyed by squad-local roster "
                + "index and the selected starters are drawn from the high locals, so a seam reading it "
                + "by team slot — or not reading it at all — leaves every reservoir at the rested boot.");

            for (int slot = MatchEngineConstants.PLAYERS_PER_TEAM;
                 slot < 2 * MatchEngineConstants.PLAYERS_PER_TEAM; slot++)
            {
                Assert.AreEqual(1f, engine.AgentView(slot).AerobicPool, 0f,
                    $"Away agent {slot}: the away club trained Balanced and must boot fully rested.");
            }
        }

        [Test]
        public void EnginePath_ConfiguresTheAvailabilityFilteredSquad()
        {
            // The filter's other half. ERR-030-009 puts it at resolve→filter→configure on BOTH
            // resolution paths, and only the quick-sim one was ever executed: an engine fixture booted
            // with the UNFILTERED roster would field injured players with nothing to notice it.
            League league = FourClubLeague();
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] clubIds = league.ClubIds();
            for (int i = 0; i < clubIds.Length; i++)
            {
                provider.Set(CareerTestRoster.Build(clubIds[i], PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            }

            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, clubIds, injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.FullEngine, career, provider);

            Fixture fixture = FirstFixture(loop);
            Squad home = provider.ResolveByClubId(fixture.HomeClubId);

            // The seven best-rated players out, which is what makes the filtered eleven differ from the
            // unfiltered one — the same construction SeasonSaveCareerRestoreTests uses.
            for (int local = home.Count - 1; local >= home.Count - 7; local--)
            {
                var injured = InjuryState.Create();
                injured.Severity = InjurySeverity.Serious;
                injured.RecoveryRemaining = 60;
                career.SetMedicalState(fixture.HomeClubId, home.GetPlayer(local).PlayerId, in injured);
            }

            Squad expected = career.SelectAvailable(home);
            Assert.AreNotSame(home, expected, "Precondition: the filter must have removed somebody.");
            Assert.AreNotEqual(
                SquadRating.StartingElevenMean(home), SquadRating.StartingElevenMean(expected),
                "Precondition: the filter must change WHICH eleven is selected, not merely drop players "
                + "nobody was going to pick — otherwise the divergence assertion below is a coin toss.");

            MatchEngine.MatchEngine booted = loop.BootFixtureEngine(in fixture, provider);

            // The engine exposes no "which locals did you select"; the canonical attribute records are
            // not serialized either, so a PRE-tick digest cannot tell two lineups apart — with no focus
            // set every reservoir is 1.0 and the two boots are byte-identical at tick 0. The eleven only
            // becomes observable by simulating: a different eleven moves differently. Same reason the
            // restore-fidelity lock in SeasonSaveCareerRestoreTests continues 60 ticks.
            const int Ticks = 60;
            Squad away = provider.ResolveByClubId(fixture.AwayClubId);
            ulong seed = RoundResolutionModel.MatchSeedFor(
                in fixture, loop.State.Seed, loop.State.SeasonNumber);

            var reference = new MatchEngine.MatchEngine(seed);
            reference.ConfigureSquads(
                expected, away, career.MatchEntryFatigue(expected), career.MatchEntryFatigue(away));

            var unfiltered = new MatchEngine.MatchEngine(seed);
            unfiltered.ConfigureSquads(home, away);

            bool divergedFromUnfiltered = false;
            for (int t = 0; t < Ticks; t++)
            {
                booted.RunTick();
                reference.RunTick();
                unfiltered.RunTick();

                CollectionAssert.AreEqual(
                    reference.CurrentSnapshotDigest, booted.CurrentSnapshotDigest,
                    $"Tick {t + 1}: the engine path must configure the availability-FILTERED squad "
                    + "(ERR-030-009), so it must track an engine configured with that squad by hand.");

                divergedFromUnfiltered |= !BytesEqual(
                    unfiltered.CurrentSnapshotDigest, booted.CurrentSnapshotDigest);
            }

            Assert.IsTrue(divergedFromUnfiltered,
                "Precondition: filtered and unfiltered must be distinguishable within " + Ticks
                + " ticks, or the assertion above is satisfied by a filter that did nothing.");
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        [Test]
        public void EnginePath_HandsBackTheFilteredElevensIds()
        {
            // The AR pass-2 Medium's lock: the fielded XIs come OUT of the resolution
            // (BootFixtureEngine's id-producing overload), derived from the same filtered squad
            // instances the ConfigureSquads one statement below consumes — the loop's old second
            // SelectAvailable walk was an unenforced agreement with that configuration. An injured
            // starter must therefore be absent from the very ids the appearance record consumes.
            League league = FourClubLeague();
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] clubIds = league.ClubIds();
            for (int i = 0; i < clubIds.Length; i++)
            {
                provider.Set(CareerTestRoster.Build(clubIds[i], PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            }

            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, clubIds, injuryOccurrenceEnabled: false);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.FullEngine, career, provider);

            Fixture fixture = FirstFixture(loop);
            Squad home = provider.ResolveByClubId(fixture.HomeClubId);

            var injuredIds = new System.Collections.Generic.List<int>();
            for (int local = home.Count - 1; local >= home.Count - 7; local--)
            {
                var injured = InjuryState.Create();
                injured.Severity = InjurySeverity.Serious;
                injured.RecoveryRemaining = 60;
                career.SetMedicalState(fixture.HomeClubId, home.GetPlayer(local).PlayerId, in injured);
                injuredIds.Add(home.GetPlayer(local).PlayerId);
            }

            loop.BootFixtureEngine(in fixture, provider, out int[] homeXi, out int[] awayXi);

            int[] expectedHome = SquadRating.StartingElevenPlayerIds(career.SelectAvailable(home));
            int[] expectedAway = SquadRating.StartingElevenPlayerIds(
                career.SelectAvailable(provider.ResolveByClubId(fixture.AwayClubId)));
            CollectionAssert.AreEqual(expectedHome, homeXi,
                "the ids handed back are the filtered selector's eleven, in the selector's own order");
            CollectionAssert.AreEqual(expectedAway, awayXi,
                "…for the away side too (ERR-008-002's class: never home-only)");
            for (int i = 0; i < injuredIds.Count; i++)
            {
                CollectionAssert.DoesNotContain(homeXi, injuredIds[i],
                    "an injured player cannot be in the eleven the record will consume");
            }

            CollectionAssert.AreNotEquivalent(
                SquadRating.StartingElevenPlayerIds(home), homeXi,
                "precondition: the injuries genuinely changed the eleven — otherwise the exclusion "
                + "assertions above are satisfied by a filter that did nothing");
        }

        [Test]
        public void EnginePath_OnAnUnwiredLoop_BootsExactlyAsBefore()
        {
            // The neutrality floor for the branch above: with no career, BootFixtureEngine must pass
            // null for both fatigue arrays and no filter, which is byte-for-byte the pre-T2 boot.
            League league = FourClubLeague();
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.FullEngine);

            Fixture fixture = FirstFixture(loop);
            MatchEngine.MatchEngine booted = loop.BootFixtureEngine(in fixture, league);

            var bare = new MatchEngine.MatchEngine(
                RoundResolutionModel.MatchSeedFor(in fixture, loop.State.Seed, loop.State.SeasonNumber));
            bare.ConfigureSquads(
                league.ResolveByClubId(fixture.HomeClubId), league.ResolveByClubId(fixture.AwayClubId));

            CollectionAssert.AreEqual(bare.CurrentSnapshotDigest, booted.CurrentSnapshotDigest,
                "An unwired loop's engine boot must be identical to the two-argument ConfigureSquads.");
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

            // Every player advanced up to the last day lived — no gaps, no double accrual, across a
            // season's worth of days and a whole league's worth of players. The last command was
            // AdvanceAndPlayNextRound, whose pre-round step lives the fixture day itself
            // (ERR-030-027), so the cursors sit ON the clock rather than one behind it.
            uint lastLivedDay = world.CurrentWorldTick;
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

            // The THIRD state set's carry (AR pass 3): a directly-recorded appearance, because the
            // carried player is not guaranteed a starter's bits from the season itself. Deleting
            // the sync's `appearance[i] = heldAppearance[held]` line left every suite green before
            // this — the pass-6 which-test-fails-if-reverted question, asked of the third set.
            uint appearanceDay = loop.CurrentWorldDay;
            career.RecordAppearances(0, new[] { carriedId }, appearanceDay);

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
            // The regen's suffix must map OUTSIDE every club's id range: this fixture's original
            // CLUB_SQUAD_SIZE + 5 gave club 1 the id 1×N + (N+5) = 2×N + 5 — which IS club 2's
            // local-5 player. The ERR-041-019 guard caught the fixture itself demonstrating the
            // hazard it exists for: the "new allocator" that breaks the generator's accident was
            // this test, and pre-guard it created two players sharing one id — and one occurrence
            // draw — silently.
            swappedLocals[swappedLocals.Length - 1] =
                PlayerDatabaseConstants.CLUB_SQUAD_SIZE * ClubCount + 5;
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

            ClubAppearanceStates club0Appearance = career.AppearanceBlocks()[0];
            int carriedIndex = System.Array.IndexOf(club0Appearance.PlayerIds, carriedId);
            Assert.GreaterOrEqual(carriedIndex, 0);
            Assert.AreEqual(appearanceDay, club0Appearance.States[carriedIndex].BitsAsOfWorldDay,
                "The appearance record rides the boundary with its player (FR-MD-025's carry, third "
                + "state set) — a dropped record silently zeroes the FR-MD-010 match-load term.");
            Assert.AreNotEqual(0u, club0Appearance.States[carriedIndex].RecentBits,
                "…and the recorded bit itself must survive, not just the anchor.");

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
// | 1.1     | 2026-08-06 | —      | T2 AR passes 4-5 (1H + 1M). **H:** three EnginePath_* cases. Every |
// |         |            |        | v1.0 test here ran QuickSimAll and every engine-mode test in this  |
// |         |            |        | assembly builds the loop WITHOUT a career, so SeasonLoop's         |
// |         |            |        | career-wired match boot — the sole production call site of #29's   |
// |         |            |        | entry-fatigue seam, and the only place the ERR-030-009 filter      |
// |         |            |        | meets a real MatchEngine — had never executed anywhere. They go    |
// |         |            |        | through the new internal BootFixtureEngine so the cost is          |
// |         |            |        | milliseconds rather than a 90-minute match, the same reason        |
// |         |            |        | ShouldPlayThroughEngine was extracted. **M:** + DayAdvance_Stops-  |
// |         |            |        | BeforeTheFixtureDaysOwnSteps, pinning ERR-030-026 — where the      |
// |         |            |        | round sits among the KD-2 slots was settled by a loop condition    |
// |         |            |        | and written down nowhere, and it makes every injury one matchday   |
// |         |            |        | longer than its tier.                                             |
// | 1.2     | 2026-08-07 | —      | Balance pass D1 (ERR-030-027): the DayAdvance lock rewritten to    |
// |         |            |        | the pre-round convention — served-his-time plays THIS round, and   |
// |         |            |        | matchday is lived exactly once (the conditioning cursor is the     |
// |         |            |        | discriminating idempotency assertion; Balanced's net fatigue is 0).|
// |         |            |        | ForLeague call sites declare the now-required dial explicitly.     |
// | 1.3     | 2026-08-07 | —      | Balance-pass AR pass 2 (M): + EnginePath_HandsBackTheFiltered-     |
// |         |            |        | ElevensIds — the id-producing BootFixtureEngine overload returns   |
// |         |            |        | exactly the filtered selector's elevens (both sides), with injured |
// |         |            |        | starters absent from the ids the appearance record consumes.       |
// | 1.4     | 2026-08-08 | —      | Balance-pass AR pass 3 (M3 + the ERR-041-019 catch): the roll lock |
// |         |            |        | records an appearance pre-roll and asserts anchor + bits post-roll |
// |         |            |        | — the appearance carry finally has a failing-if-reverted test —    |
// |         |            |        | and the regen fixture's suffix corrected off club 2's id range     |
// |         |            |        | (the guard's first catch: this fixture had been creating a         |
// |         |            |        | cross-club duplicate id since T2). Row added at pass 4 (rowless).  |
#endregion
