// File:     src/season-save/tests/SeasonLoopDisciplineTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3/§5 (T-DC-NEU-001, T-DC-BAN-002/003/005, T-DC-VIEW-001,
//           T-DC-SAV-002); Season & Competition Loop #30 §3.4 (the composed seam,
//           ERR-030-009/-016/-029); ERR-044-002 (both resolution paths), ERR-044-003 (removals only);
//           Code Standards #20
// Purpose:  The #44 T2 WIRING locks. Every case here fails if a wiring point is reverted — the fold's
//           seed and its per-tick pump, the filter at the seam on both paths and both clubs, the
//           serving decrement, the off-by-one, the composition with #41, and the boundary sweep.
//           #44's own rules are unit tested in discipline/tests; nothing here re-tests them.

using NUnit.Framework;

using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonLoopDisciplineTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;
        private const int BigLeagueClubCount = 20;

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, ClubCount);

        /// <summary>A state in which exactly <paramref name="playerIds"/> are serving a one-match ban.</summary>
        private static DisciplineState BannedState(params int[] playerIds) => BansOf(1, playerIds);

        private static DisciplineState BansOf(int matches, params int[] playerIds)
        {
            int[] sorted = (int[])playerIds.Clone();
            System.Array.Sort(sorted);

            var entries = new DisciplineEntry[sorted.Length];
            for (int i = 0; i < sorted.Length; i++)
            {
                entries[i] = new DisciplineEntry(
                    sorted[i], DisciplineConstants.LEAGUE_COMPETITION_KEY, 0, matches);
            }

            return DisciplineState.FromEntries(entries);
        }

        private static SeasonLoop LoopOver(
            League league,
            RoundResolutionMode mode,
            out PlayerCareerStates career,
            DisciplineState discipline = null)
        {
            career = PlayerCareerStates.ForLeague(league, league.ClubIds(), injuryOccurrenceEnabled: false);
            return new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(league.ClubIds()[0]),
                mode,
                career,
                league,
                progressionOrNull: null,
                disciplineOrNull: discipline);
        }

        // ── the sentinel agreement ─────────────────────────────────────────────────────────

        [Test]
        public void TheEngineAndTheFoldAgreeOnTheNoPlayerSentinel()
        {
            // MatchEngine.PlayerIdsByAgentId hands its array straight to CardLedgerFold with no
            // translation, and neither assembly can see the other's constant — match-engine cannot
            // reference discipline, and discipline cannot reference match-engine. season-save is the
            // one place both are visible, so the agreement is pinned here or nowhere. If they ever
            // diverge, an unconfigured slot stops looking unmapped and F1 goes quiet: a card would be
            // attributed to whatever player id the stale sentinel happens to name.
            Assert.That(
                MatchEngineConstants.NO_PLAYER_ID, Is.EqualTo(CardLedgerFold.NO_PLAYER),
                "MatchEngineConstants.NO_PLAYER_ID and CardLedgerFold.NO_PLAYER must be numerically "
                + "equal — the occupancy array crosses that boundary untranslated.");
        }

        // ── the fold's seed: the engine reports who it actually fielded ────────────────────

        [Test]
        public void PlayerIdsByAgentId_ReportsTheLineupTheEngineActuallyConfigured()
        {
            League league = FourClubLeague();
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = loop.State.FixtureAt(0);

            TacticalDirector.MatchEngine.MatchEngine engine =
                loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out int[] awayXi);

            int[] byAgent = engine.PlayerIdsByAgentId();

            Assert.That(byAgent.Length, Is.EqualTo(MatchEngineConstants.AGENT_ID_SPACE),
                "The array must span the whole agent-id space, bench ids included — "
                + "SubstitutionEvent.Incoming indexes past SQUAD_SIZE.");

            // The eleven ids the boot handed back ARE the eleven on the pitch, in slot order. This is
            // the assertion that would fail if PlayerIdsByAgentId re-derived a lineup of its own
            // instead of reporting the one ApplySquad assigned.
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                Assert.That(byAgent[k], Is.EqualTo(homeXi[k]),
                    $"Home slot {k} reports player {byAgent[k]} but the boot fielded {homeXi[k]}.");
                Assert.That(
                    byAgent[MatchEngineConstants.PLAYERS_PER_TEAM + k], Is.EqualTo(awayXi[k]),
                    $"Away slot {k} disagrees with the eleven the boot fielded.");
            }

            // Every bench id is populated too — a fold seeded from a half-filled array would fail F1
            // the first time a substitute was booked.
            for (int b = MatchEngineConstants.SQUAD_SIZE; b < MatchEngineConstants.AGENT_ID_SPACE; b++)
            {
                Assert.That(byAgent[b], Is.Not.EqualTo(MatchEngineConstants.NO_PLAYER_ID),
                    $"Bench agent id {b} carries no player identity, so a card shown to a substitute "
                    + "would fail F1 rather than land on his record.");
            }
        }

        [Test]
        public void PlayerIdsByAgentId_IsAllSentinelOnAMatchThatNeverConfiguredSquads()
        {
            // The neutral path — the pre-#27 match every physics suite still boots. It must report no
            // identities at all rather than player 0 twenty-two times.
            var engine = new TacticalDirector.MatchEngine.MatchEngine(WorldSeed);
            int[] byAgent = engine.PlayerIdsByAgentId();

            for (int i = 0; i < byAgent.Length; i++)
            {
                Assert.That(byAgent[i], Is.EqualTo(MatchEngineConstants.NO_PLAYER_ID),
                    $"Agent {i} claims to be player {byAgent[i]} on a match that never called "
                    + "ConfigureSquads.");
            }
        }

        [Test]
        public void PlayerIdsByAgentId_FollowsASubstitution()
        {
            // The occupancy half of KD-2, at the one site that changes it. Nothing in SeasonLoop calls
            // SubstitutePlayer today (Stage 0 fields a fixed eleven), so without this case the identity
            // swap ships unrun — the shape that left BootFixtureEngine itself unexecuted for months.
            League league = FourClubLeague();
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = loop.State.FixtureAt(0);
            TacticalDirector.MatchEngine.MatchEngine engine = loop.BootFixtureEngine(in fixture, league);

            int[] before = engine.PlayerIdsByAgentId();
            int benchAgentId = MatchEngineConstants.SQUAD_SIZE;   // team 0, bench 0
            int incomingPlayerId = before[benchAgentId];

            // Slot 1 rather than 0: slot 0 is the goalkeeper and bench 0 need not be one.
            engine.SubstitutePlayer(teamId: 0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical);

            int[] after = engine.PlayerIdsByAgentId();

            Assert.That(before[1], Is.Not.EqualTo(incomingPlayerId),
                "Precondition: the incoming substitute must be a different player from the starter, "
                + "or the assertion below is satisfied by a swap that never happened.");
            Assert.That(after[1], Is.EqualTo(incomingPlayerId),
                "Slot 1 still reports the outgoing player. A card shown after this substitution would "
                + "be recorded against a player who is no longer on the pitch — which is exactly the "
                + "information the engine's own per-slot yellow count loses at this moment.");
        }

        // ── the filter, at the seam, on both paths and both clubs ─────────────────────────

        [Test]
        public void EnginePath_ExcludesASuspendedManagedPlayerFromTheFieldedEleven()
        {
            League league = FourClubLeague();
            SeasonLoop unfiltered = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = unfiltered.State.FixtureAt(0);
            unfiltered.BootFixtureEngine(in fixture, league, out int[] baselineXi, out _);

            int bannedStarter = baselineXi[5];
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out _, BannedState(bannedStarter));

            loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out _);

            Assert.That(homeXi, Does.Not.Contain(bannedStarter),
                "A suspended home starter was still fielded. The filter is not reaching the engine "
                + "boot's resolve→filter→configure seam (ERR-030-009 / FR-DC-010).");
            Assert.That(homeXi.Length, Is.EqualTo(baselineXi.Length),
                "The eleven must still be eleven — somebody else takes the place.");
        }

        [Test]
        public void EnginePath_ExcludesASuspendedOPPONENTPlayerToo()
        {
            // FR-DC-010's v0.3 history says this in as many words: the unscoped wording let a
            // managed-club-only implementation pass every test while banned opponents played through
            // their bans. Mirrored from the home case deliberately — three home/away asymmetry defects
            // shipped here because every fixture and example used the home side (ERR-008-002).
            League league = FourClubLeague();
            SeasonLoop unfiltered = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = unfiltered.State.FixtureAt(0);
            unfiltered.BootFixtureEngine(in fixture, league, out _, out int[] baselineAwayXi);

            int bannedStarter = baselineAwayXi[5];
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out _, BannedState(bannedStarter));

            loop.BootFixtureEngine(in fixture, league, out _, out int[] awayXi);

            Assert.That(awayXi, Does.Not.Contain(bannedStarter),
                "A suspended AWAY starter was still fielded. The seam must filter each resolved squad "
                + "of the fixture, not only the managed club's.");
        }

        [Test]
        public void QuickSimPath_MakesASuspensionCostSomething()
        {
            // ERR-044-002. FR-DC-010 names only "the engine-resolved fixture"; #30 §3.4 has the seam
            // live on both paths, and FR-DC-011 serves bans on both — so filtering on one path only
            // would let a quick-sim fixture decrement a ban the player had just played through. Nearly
            // every fixture of a career takes this path, so this is where a ban costs anything at all.
            //
            // Measured through the only thing this path exposes: the table. The quick-sim rates a club
            // by the eleven it would field, so a club forced to play its RESERVES all season must
            // finish worse. Asserting on a Squad object instead would test Availability, which
            // discipline/tests already covers — this has to fail when the SelectAvailable call is
            // removed from ResolveFixture's quick-sim branch, and nothing short of an outcome does that.
            League league = LeagueBootstrap.Generate(WorldSeed, BigLeagueClubCount);
            int clubId = league.ClubIds()[0];

            int[] firstChoice = SquadRating.StartingElevenPlayerIds(league.ResolveByClubId(clubId));

            // Long enough to outlast the season — a two-match ban would be served by round 2 and the
            // two tables would then differ by nothing at all.
            DisciplineState suspended = BansOf(10_000, firstChoice);

            int baselinePoints = PlayWholeSeasonAndReadPoints(league, clubId, null);
            int suspendedPoints = PlayWholeSeasonAndReadPoints(league, clubId, suspended);

            Assert.That(suspendedPoints, Is.LessThan(baselinePoints),
                "A club whose entire first-choice eleven is suspended for the whole season finished "
                + $"with {suspendedPoints} points against {baselinePoints} unsuspended — i.e. no worse. "
                + "The quick-sim branch of ResolveFixture is rating the UNFILTERED roster, so a ban "
                + "costs nothing on the path that resolves nearly every fixture of a career.");
        }

        // ── serving, on both paths, for both clubs ────────────────────────────────────────

        [Test]
        public void EveryClubThatPlayedServesOneMatchOfItsBans()
        {
            League league = FourClubLeague();
            SeasonLoop probe = LoopOver(league, RoundResolutionMode.QuickSimAll, out _);
            Fixture fixture = probe.State.FixtureAt(0);
            Fixture otherFixture = probe.State.FixtureAt(1);

            int homeBanned = SquadRating.StartingElevenPlayerIds(
                league.ResolveByClubId(fixture.HomeClubId))[3];
            int awayBanned = SquadRating.StartingElevenPlayerIds(
                league.ResolveByClubId(fixture.AwayClubId))[3];
            int elsewhere = SquadRating.StartingElevenPlayerIds(
                league.ResolveByClubId(otherFixture.HomeClubId))[3];

            DisciplineState state = BansOf(2, homeBanned, awayBanned, elsewhere);
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.QuickSimAll, out _, state);

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(Ban(state, homeBanned), Is.EqualTo(1),
                "The home club played, so its suspended player served one match (FR-DC-011).");
            Assert.That(Ban(state, awayBanned), Is.EqualTo(1),
                "The away club played too — serving is per club, not per managed club.");

            // A round resolves EVERY fixture (FR-SN-012), so the club in the other fixture played as
            // well and its ban serves identically. That is the point of FR-DC-011: serving follows the
            // club's own fixtures, including the ones nobody watched.
            Assert.That(Ban(state, elsewhere), Is.EqualTo(1),
                "A club whose fixture was resolved elsewhere in the same round did not serve — serving "
                + "is being driven from the managed fixture rather than from every played fixture.");
        }

        // ── the off-by-one contract ──────────────────────────────────────────────────────

        [Test]
        public void AOneMatchBan_CostsExactlyTheNextFixtureAndNoMore()
        {
            // FR-DC-010/011's ordering contract, end to end: out for N+1, back for N+2, and the row
            // gone the moment it is discharged.
            League league = FourClubLeague();
            SeasonLoop probe = LoopOver(league, RoundResolutionMode.QuickSimAll, out _);
            Fixture first = probe.State.FixtureAt(0);
            int banned = SquadRating.StartingElevenPlayerIds(league.ResolveByClubId(first.HomeClubId))[4];

            DisciplineState state = BannedState(banned);
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.QuickSimAll, out _, state);

            Assert.That(
                Availability.IsAvailable(state, banned, DisciplineConstants.LEAGUE_COMPETITION_KEY),
                Is.False,
                "Precondition: the player starts the round suspended.");

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(
                Availability.IsAvailable(state, banned, DisciplineConstants.LEAGUE_COMPETITION_KEY),
                Is.True,
                "After serving one fixture a one-match ban is discharged.");
            Assert.That(state.HasEntry(banned, DisciplineConstants.LEAGUE_COMPETITION_KEY), Is.False,
                "A row that reaches (0, 0) is dropped IMMEDIATELY, mid-season — FR-DC-017's canonical "
                + "minimality, not just a boundary sweep.");
        }

        // ── composition with #41, and the ERR-044-003 tiering ─────────────────────────────

        [Test]
        public void ASuspendedAndAnInjuredPlayerAreBothRemoved()
        {
            League league = FourClubLeague();
            SeasonLoop probe = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = probe.State.FixtureAt(0);
            probe.BootFixtureEngine(in fixture, league, out int[] baselineXi, out _);

            int suspended = baselineXi[2];
            int injured = baselineXi[7];

            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out PlayerCareerStates career,
                BannedState(suspended));

            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 12;
            career.SetMedicalState(fixture.HomeClubId, injured, in knock);

            loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out _);

            Assert.That(homeXi, Does.Not.Contain(suspended),
                "The suspension removal was lost when composed with the injury removal.");
            Assert.That(homeXi, Does.Not.Contain(injured),
                "The injury removal was lost when composed with the suspension removal. Both are "
                + "removals and the composition is a union — neither may mask the other.");
        }

        [Test]
        public void TheBackFillPressesAnInjuredPlayerBackBeforeASuspendedOne()
        {
            // ERR-044-003's tier rule, and the one football compromise in the seam. #30 §3.4 requires
            // that the composed filter "can never leave a club worse off than having no filter at all",
            // so a suspended player IS reinstatable in extremis; suspension is simply the stricter
            // tier, reached only when no merely-injured player is left to press back.
            League league = FourClubLeague();
            int clubId = league.ClubIds()[0];
            Squad full = league.ResolveByClubId(clubId);

            var all = new int[full.Count];
            for (int i = 0; i < full.Count; i++)
            {
                all[i] = full.GetPlayer(i).PlayerId;
            }

            // Suspend everyone but the first eleven, then injure one of THOSE eleven. The composed set
            // is ten players, so the back-fill must reinstate at least one — and it has a choice
            // between the injured man and a suspended one.
            var banned = new int[all.Length - 11];
            for (int i = 11; i < all.Length; i++)
            {
                banned[i - 11] = all[i];
            }

            DisciplineState state = BansOf(1, banned);
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out PlayerCareerStates career, state);

            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 3;
            career.SetMedicalState(clubId, all[0], in knock);

            Squad fielded = AvailabilityComposition.Compose(
                full, career, state, DisciplineConstants.LEAGUE_COMPETITION_KEY);

            Assert.That(SquadRating.CanFieldStartingEleven(fielded), Is.True,
                "The composed filter must never stop a club playing (#30 §3.4 / §2.3 F9).");
            Assert.That(Contains(fielded, all[0]), Is.True,
                "The back-fill reinstated a suspended player while an injured one was still available. "
                + "Suspension is the stricter tier — a banned man plays only when the alternative is a "
                + "club that cannot take the field at all (ERR-044-003).");
        }

        [Test]
        public void WithNothingUnavailable_TheSeamHandsBackTheSAMESquadInstance()
        {
            // FR-DC-018's identity floor, and what makes the whole landing behaviour-neutral on a clean
            // career: no removals ⇒ reference-identical squad ⇒ byte-identical match. A composition that
            // rebuilt an equal-but-distinct Squad would pass every other test here and quietly change
            // every fixture's digest.
            League league = FourClubLeague();
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out PlayerCareerStates career,
                new DisciplineState());

            Squad squad = league.ResolveByClubId(league.ClubIds()[0]);
            Squad composed = AvailabilityComposition.Compose(
                squad, career, loop.Discipline, DisciplineConstants.LEAGUE_COMPETITION_KEY);

            Assert.That(composed, Is.SameAs(squad),
                "A fully fit, fully eligible squad must pass through the seam untouched.");
        }

        // ── the fold, against a real match ────────────────────────────────────────────────

        [Test]
        public void ARealEngineFixtureFoldsItsCardsOntoPlayerRecordsAndChangesNothingElse()
        {
            // The load-bearing case of this suite, and the one that costs real 90-minute matches.
            //
            // It is written as ONE test with two halves on purpose. #44 §5's T-DC-NEU-001 asks only
            // that an observed fixture be digest-identical to an unobserved one — and that is green if
            // the tap is never consumed, if the fold is never invoked, or if the fold is a no-op. It
            // cannot tell observer-NEUTRAL from observer-ABSENT, which is ERR-030-014's shape one layer
            // up. So the neutrality assertion is PAIRED with a positive control in the same test: the
            // observed run must actually have folded something.
            League league = LeagueBootstrap.Generate(WorldSeed, BigLeagueClubCount);
            int managedClubId = league.ClubIds()[0];

            var tally = new DisciplineState();
            MatchResult observed = PlayOneEngineRound(league, managedClubId, tally);
            MatchResult unobserved = PlayOneEngineRound(league, managedClubId, null);

            // Positive control. At the engine's measured discipline rate a 90-minute fixture books
            // several players, so an empty tally means the fold never ran, never saw the tap, or was
            // seeded with an occupancy nothing matched.
            Assert.That(tally.Count, Is.GreaterThan(0),
                "A full engine fixture folded ZERO cards. Either ObserveTick is not pumped inside the "
                + "tick loop, or Commit never runs, or the fold's occupancy seed does not match the "
                + "recipient ids the engine emits. Without this assertion the neutrality check below is "
                + "satisfied by a fold that does nothing at all.");

            // Observer neutrality (FR-DC-003): reading the tap must not move the match by one goal.
            Assert.That(observed.HomeGoals, Is.EqualTo(unobserved.HomeGoals),
                "The observed fixture scored differently from the unobserved one — the fold is not "
                + "read-only with respect to the engine.");
            Assert.That(observed.AwayGoals, Is.EqualTo(unobserved.AwayGoals),
                "The observed fixture scored differently from the unobserved one.");

            // Every folded row belongs to a player of one of the two clubs that played the round's ONE
            // engine fixture — the attribution half. A fold that passed recipient agent ids straight
            // through as player ids would produce rows for ids in neither club.
            for (int i = 0; i < tally.Count; i++)
            {
                int playerId = tally.EntryAt(i).PlayerId;
                int club = playerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
                Assert.That(
                    club == observed.HomeClubId || club == observed.AwayClubId, Is.True,
                    $"Folded a card onto player {playerId}, who belongs to club {club} — neither of the "
                    + $"two clubs ({observed.HomeClubId} v {observed.AwayClubId}) that played the only "
                    + "engine fixture of this round.");
            }
        }

        // ── the season boundary ──────────────────────────────────────────────────────────

        [Test]
        public void TheSeasonRollResetsYellowsAndCarriesUnservedBans()
        {
            League league = FourClubLeague();
            int clubId = league.ClubIds()[0];
            int player = league.ResolveByClubId(clubId).GetPlayer(0).PlayerId;

            // A ban long enough to outlive the season's own fixtures, so the boundary genuinely has an
            // UNSERVED ban to carry — a two-match ban would be served out by round 2 and the carry rule
            // would then be asserted on nothing.
            var state = DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(player, DisciplineConstants.LEAGUE_COMPETITION_KEY, 4, 100),
            });

            SeasonLoop loop = LoopOver(league, RoundResolutionMode.QuickSimAll, out _, state);
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(league);
            }

            int carriedIn = Ban(state, player);
            Assert.That(carriedIn, Is.GreaterThan(0),
                "Precondition: the ban must still be outstanding when the boundary arrives.");
            Assert.That(Yellows(state, player), Is.EqualTo(4),
                "Precondition: the yellows must still be there for the roll to clear.");

            loop.RollToNextSeason();

            Assert.That(Yellows(state, player), Is.EqualTo(0),
                "FR-DC-017: every yellow count resets at the boundary.");
            Assert.That(Ban(state, player), Is.EqualTo(carriedIn),
                "FR-DC-017: an UNSERVED ban carries across the boundary UNCHANGED — a red card in the "
                + "final round is still a ban in August, which is the whole reason #44 persists at all. "
                + "The roll must neither forgive it nor serve it.");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Plays round 0 in <see cref="RoundResolutionMode.ManagedThroughEngine"/> — one real match,
        /// the rest quick-simmed — and returns the managed club's own fixture result.
        /// </summary>
        private static MatchResult PlayOneEngineRound(
            League league, int managedClubId, DisciplineState discipline)
        {
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(managedClubId),
                RoundResolutionMode.ManagedThroughEngine,
                careerOrNull: null,
                careerSquadsOrNull: null,
                progressionOrNull: null,
                disciplineOrNull: discipline);

            loop.AdvanceToNextFixtureDay();
            MatchResult[] results = loop.AdvanceAndPlayNextRound(league);

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].HomeClubId == managedClubId || results[i].AwayClubId == managedClubId)
                {
                    return results[i];
                }
            }

            throw new System.InvalidOperationException(
                "Round 0 contains no fixture for the managed club — the scheduler's own invariant.");
        }

        /// <summary>
        /// Plays a whole season through the quick-sim and returns <paramref name="clubId"/>'s points.
        /// A fresh world and season each call, so two runs differ in exactly one thing.
        /// </summary>
        private static int PlayWholeSeasonAndReadPoints(
            League league, int clubId, DisciplineState discipline)
        {
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(clubId),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null,
                careerSquadsOrNull: null,
                progressionOrNull: null,
                disciplineOrNull: discipline);

            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(league);
            }

            return loop.State.Table.Row(clubId).Points;
        }

        private static bool Contains(Squad squad, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Ban(DisciplineState state, int playerId) =>
            state.EntryFor(playerId, DisciplineConstants.LEAGUE_COMPETITION_KEY).BanMatchesRemaining;

        private static int Yellows(DisciplineState state, int playerId) =>
            state.EntryFor(playerId, DisciplineConstants.LEAGUE_COMPETITION_KEY).Yellows;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the wiring locks —   |
// |         |            |        | sentinel agreement, the engine's occupancy report (incl. the      |
// |         |            |        | substitution branch no production caller reaches), the filter on  |
// |         |            |        | both paths and BOTH clubs, serving, the off-by-one, the #41       |
// |         |            |        | composition and its back-fill tiering, the real-match fold with   |
// |         |            |        | its positive control, and the boundary sweep.                     |
#endregion
