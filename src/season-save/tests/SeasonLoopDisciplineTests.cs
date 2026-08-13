// File:     src/season-save/tests/SeasonLoopDisciplineTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13 (#44 C1/C2 adversarial review round 4, M16 — the M12 position lock drives a
//           constructor-injected IFixtureDisciplineDriver instead of SeasonLoop's process-static
//           TestOnly_AfterHomeClubServed hook, which is deleted — v1.4)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3/§5 (T-DC-NEU-001, T-DC-BAN-002/003/005, T-DC-VIEW-001,
//           T-DC-SAV-002); Season & Competition Loop #30 §3.4 (the composed seam,
//           ERR-030-009/-016/-029); ERR-044-002 (both resolution paths), ERR-044-003 (removals only);
//           ERR-030-037 (the M6/M7 within-fixture serve-before-commit lock); unified season save §4 /
//           KD-6 (restore fidelity — the C1/C2 AR's H2); Code Standards #20
// Purpose:  The #44 T2 WIRING locks. Every case here fails if a wiring point is reverted — the fold's
//           seed and its per-tick pump, the filter at the seam on both paths and both clubs, the
//           serving decrement, the off-by-one (both across fixtures and WITHIN one), the composition
//           with #41, and the boundary sweep. #44's own rules are unit tested in discipline/tests;
//           nothing here re-tests them.

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

        // Only the mid-match restore-fidelity lock touches the disk; the rest of this suite is in-memory.
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "td-disc-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { System.IO.Directory.Delete(_tempDir, recursive: true); }
            catch (System.Exception) { }
        }

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
            DisciplineState discipline = null,
            SeasonLoop.IFixtureDisciplineDriver disciplineDriverOrNull = null)
        {
            career = PlayerCareerStates.ForLeague(league, league.ClubIds(), injuryOccurrenceEnabled: false);
            return new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(league.ClubIds()[0]),
                mode,
                career,
                league,
                progressionOrNull: null,
                disciplineOrNull: discipline,
                disciplineDriverOrNull: disciplineDriverOrNull);
        }

        /// <summary>
        /// The M16 seam's test implementation: serves and commits for real through
        /// <see cref="DisciplineRules"/>, and throws on its FIRST serving call so the throw lands
        /// inside <c>PlayNextRound</c>'s serve+commit block, after at least one real decrement.
        /// <para>
        /// Replaces the process-static <c>TestOnly_AfterHomeClubServed</c> hook this suite used at
        /// v1.3: that put an arbitrary-code injection point into the production round loop whose only
        /// safety was one inline <c>finally</c> (M16 / FR-CS-051..054). Instance state, constructed per
        /// test, nothing to clear.
        /// </para>
        /// </summary>
        private sealed class ThrowOnFirstServeDriver : SeasonLoop.IFixtureDisciplineDriver
        {
            private readonly DisciplineRules _rules;
            private int _serves;

            internal ThrowOnFirstServeDriver(DisciplineState state)
            {
                _rules = new DisciplineRules(state);
            }

            public void OnClubFixturePlayed(int clubId)
            {
                _rules.OnClubFixturePlayed(clubId);
                _serves++;
                if (_serves == 1)
                {
                    throw new System.InvalidOperationException(
                        "M16/M12 forced throw — locks the serve+commit block's position.");
                }
            }

            public void CommitFixtureCards(CardLedgerFold foldOrNull) => foldOrNull?.Commit(_rules);
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

        [Test]
        public void ANewBanEarnedThisFixtureIsNotServedByThisSameFixture()
        {
            // M7 (ERR-030-037): the off-by-one contract's WITHIN-fixture half, which
            // AOneMatchBan_CostsExactlyTheNextFixtureAndNoMore above cannot see — that test pre-seeds
            // the ban before the fixture is played, so it is blind to the ORDER of
            // OnClubFixturePlayed vs fold.Commit inside PlayNextRound. Swapping the two calls makes a
            // straight red a ONE-match ban instead of the two FR-DC-006 specifies (or, for a fresh
            // accumulation crossing with no residual yellows, decrements the just-added ban straight to
            // (0, 0) and FR-DC-017 drops the row — the player vanishes from the tally as if never
            // carded at all), and every other test in this suite still passes: QuickSimAll (most of
            // them) never builds a fold at all, and the fixed-tally fixtures above are seeded BEFORE
            // kickoff, so they are blind to commit-vs-serve order within a fixture that EARNS a card.
            //
            // Ground truth for what round 0's cards alone should produce — with no serving anywhere
            // near them — comes from an independent replay of the SAME deterministic round, fixture by
            // fixture: the match is a pure function of (seed, squads), both tallies here start EMPTY so
            // nobody is suspension-filtered, and the replayed engine runs are therefore byte-identical
            // to production's. Committing each replayed fold into one fresh, unrelated
            // DisciplineRules — never touched by a serving call — gives the exact tally PlayNextRound's
            // own commits must reproduce if, and only if, serving never touches a ban a fixture just
            // earned for itself. FullEngine (not ManagedThroughEngine) so BOTH of this 4-club round's
            // fixtures book cards; at WorldSeed, fixture 0 alone happens to book nothing bannable, and
            // a positive control that only checked SOME card existed would be vacuous on that fixture.
            League league = FourClubLeague();

            SeasonLoop groundTruthLoop = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            var groundTruth = new DisciplineState();
            var groundTruthRules = new DisciplineRules(groundTruth);
            for (int f = 0; f < 2; f++)
            {
                Fixture fixture = groundTruthLoop.State.FixtureAt(f);
                TacticalDirector.MatchEngine.MatchEngine groundTruthEngine =
                    groundTruthLoop.BootFixtureEngine(in fixture, league);
                var groundTruthFold = new CardLedgerFold(
                    groundTruthEngine.PlayerIdsByAgentId(), DisciplineConstants.LEAGUE_COMPETITION_KEY);
                var groundTruthTap = new MatchEngineDisciplineTap(groundTruthEngine);
                while (!groundTruthEngine.MatchEnded)
                {
                    groundTruthEngine.RunTick();
                    groundTruthFold.ObserveTick(groundTruthTap);
                }
                groundTruthFold.Commit(groundTruthRules);
            }

            int groundTruthBanEntries = 0;
            for (int i = 0; i < groundTruth.Count; i++)
            {
                if (groundTruth.EntryAt(i).BanMatchesRemaining > 0)
                {
                    groundTruthBanEntries++;
                }
            }

            Assert.That(groundTruthBanEntries, Is.GreaterThan(0),
                "Positive control: round 0 must deterministically book at least one BAN-WORTHY card "
                + "(not just any card — a bare uncrossed yellow has BanMatchesRemaining == 0 and is "
                + "untouched by serving either way, so it would pass this lock vacuously). If this ever "
                + "fails, the fixture/seed pairing needs revisiting, not the ordering this test exists "
                + "to lock.");

            var tally = new DisciplineState();
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.FullEngine, out _, tally);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            for (int i = 0; i < groundTruth.Count; i++)
            {
                DisciplineEntry expected = groundTruth.EntryAt(i);

                Assert.That(
                    tally.HasEntry(expected.PlayerId, expected.CompetitionId), Is.True,
                    $"Player {expected.PlayerId}'s card from this round is MISSING from the production "
                    + "tally entirely. M6/M7: OnClubFixturePlayed ran on this player's newly committed "
                    + "(residual-yellows, ban) entry and decremented it to (0, 0), which FR-DC-017 then "
                    + "drops immediately — a ban his own fixture's card had just added, served (and "
                    + "erased) before it ever counted for a fixture.");

                DisciplineEntry actual = tally.EntryFor(expected.PlayerId, expected.CompetitionId);
                Assert.That(actual.BanMatchesRemaining, Is.EqualTo(expected.BanMatchesRemaining),
                    $"Player {expected.PlayerId} earned a {expected.BanMatchesRemaining}-match ban this "
                    + $"round (ground truth, commit alone) but the production tally shows "
                    + $"{actual.BanMatchesRemaining}. OnClubFixturePlayed must run BEFORE that player's "
                    + "own fixture's fold.Commit, never after — a card shown in fixture N must not have "
                    + "ITS OWN ban served by fixture N.");
            }
        }

        // ── M12: the block's POSITION relative to MarkFixturePlayed ───────────────────────

        [Test]
        public void AThrowInsideTheServeAndCommitBlock_LeavesTheFixturePlayed_AndDoesNotDoubleServeOnRetry()
        {
            // M12: ANewBanEarnedThisFixtureIsNotServedByThisSameFixture (above) locks M7 — the ORDER
            // between OnClubFixturePlayed and fold.Commit — and is blind to M6: moving the WHOLE
            // serve+commit block back above MarkFixturePlayed leaves that test green, since the order
            // between the two calls is unchanged either way. This test locks M6 itself, forcing the
            // throw through a substituted collaborator so it does not depend on DisciplineRules
            // actually being fallible under today's [GT] defaults (it is not, for OnClubFixturePlayed —
            // L9).
            //
            // M16: the collaborator arrives through the CONSTRUCTOR now, not through a process-static
            // delegate the production loop invokes and a `finally` has to clear. Same property locked,
            // same forced throw position — after the home club's real serving decrement, inside the
            // block — with nothing static to leak into another test.
            League league = FourClubLeague();
            Fixture fixture = league.CreateSeason(league.ClubIds()[0]).FixtureAt(0);

            int homeBanned = SquadRating.StartingElevenPlayerIds(
                league.ResolveByClubId(fixture.HomeClubId))[3];

            DisciplineState state = BansOf(5, homeBanned);
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.QuickSimAll, out _, state,
                new ThrowOnFirstServeDriver(state));

            loop.AdvanceToNextFixtureDay();

            Assert.Throws<System.InvalidOperationException>(
                () => loop.AdvanceAndPlayNextRound(league));

            Assert.That(loop.State.FixtureAt(0).Played, Is.True,
                "M12/M6: a throw inside the #44 serve+commit block must not leave the fixture the "
                + "throw happened in UNPLAYED — the block runs AFTER MarkFixturePlayed. Reverting the "
                + "block to run BEFORE MarkFixturePlayed makes this throw before the mark, and this "
                + "assertion is exactly what would then fail.");

            int afterFirstThrow = Ban(state, homeBanned);
            Assert.That(afterFirstThrow, Is.EqualTo(4),
                "Precondition: the home club's OnClubFixturePlayed ran once, before the injected throw.");

            // Retry the SAME round. If the fixture had been left unplayed (M6 reverted), it would be
            // re-resolved and re-served here, decrementing the SAME ban a second time — the exact
            // double-serve hazard M6 exists to prevent.
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(Ban(state, homeBanned), Is.EqualTo(afterFirstThrow),
                "The fixture's ban was served a SECOND time on retry — the serve+commit block ran "
                + "again for an already-played fixture.");
        }

        [Test]
        public void AFixtureDisciplineDriverWithoutATally_IsRefusedAtComposition()
        {
            // M16, the seam's own coherence rule. PlayNextRound gates the whole serve+commit block on
            // the driver, so a driver beside a null DisciplineState would run #44's per-fixture work
            // for a loop whose Discipline property is null and whose save writes an empty DISC block —
            // two answers to "is discipline wired" inside one object, which is precisely the
            // distinction ERR-030-038/-039 exist over. Refused at composition instead.
            League league = FourClubLeague();
            var orphan = new DisciplineState();

            Assert.Throws<System.ArgumentException>(
                () => LoopOver(
                    league, RoundResolutionMode.QuickSimAll, out _, discipline: null,
                    disciplineDriverOrNull: new ThrowOnFirstServeDriver(orphan)),
                "a driver with no tally behind it must be refused where it is composed, not discovered "
                + "when the first fixture serves.");
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

        // ── mid-match restore fidelity (the C1/C2 AR's H2) ───────────────────────────────

        [Test]
        public void AMidMatchSave_WithASuspendedStarter_RestoresTheElevenThatActuallyPlayed()
        {
            // The #29/#41 T2 AR filed and closed this for injuries; #44 reopened it one contributor
            // later. The match is configured through the COMPOSED filter — SeasonLoop.SelectAvailable
            // passes _discipline — but SeasonSaveManager.Load's restore decorator called
            // PlayerCareerStates.SelectAvailable, which composes with `discipline: null`. So a fixture a
            // suspension had touched restored a strictly LARGER candidate set, LineupSelector re-ran
            // over it, and a different eleven's canonical attribute records went onto the pitch: ClubId
            // matching, size gate passing, digest diverging with nothing to announce it.
            //
            // Discriminated by continuing the digest chain, the injury lock's own technique — the
            // canonical attribute records are re-derived from the roster at restore rather than
            // serialized, so simulating on is the only way to see WHICH eleven came back.
            League league = FourClubLeague();
            int clubId = league.ClubIds()[0];
            Squad full = league.ResolveByClubId(clubId);

            // Suspend the seven best-rated players, so an unfiltered re-selection picks several of them
            // and a filtered one cannot. Mirrors the injury lock's fixture exactly.
            int[] firstChoice = SquadRating.StartingElevenPlayerIds(full);
            var banned = new int[7];
            for (int i = 0; i < banned.Length; i++)
            {
                banned[i] = firstChoice[i];
            }

            DisciplineState tally = BansOf(3, banned);

            Squad fielded = AvailabilityComposition.Compose(
                full, null, tally, DisciplineConstants.LEAGUE_COMPETITION_KEY);
            Assert.That(fielded, Is.Not.SameAs(full),
                "Precondition: the filter must have removed somebody.");
            Assert.That(
                SquadRating.StartingElevenMean(fielded),
                Is.Not.EqualTo(SquadRating.StartingElevenMean(full)),
                "Precondition: the suspensions must change WHICH eleven is selected, or this test "
                + "cannot distinguish a correct restore from a broken one.");

            var engine = new TacticalDirector.MatchEngine.MatchEngine(WorldSeed);
            engine.ConfigureSquads(fielded, league.ResolveByClubId(league.ClubIds()[1]));
            for (int t = 0; t < 10; t++)
            {
                engine.RunTick();
            }

            var world = new WorldStore(ManagerId, WorldSeed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(clubId), RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: null,
                disciplineOrNull: tally);

            string path = System.IO.Path.Combine(_tempDir, "midmatch-suspension.save");
            SeasonSaveManager.Save(loop, engine, path);

            // What the un-saved match goes on to do — the chain a correct restore must reproduce.
            const int Continue = 60;
            var reference = new byte[Continue][];
            for (int t = 0; t < Continue; t++)
            {
                engine.RunTick();
                reference[t] = engine.CurrentSnapshotDigest;
            }

            // Loaded with the UNFILTERED league as its provider — which is all any caller has.
            SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
            Assert.That(contents.Match, Is.Not.Null, "Precondition: the save carried a match.");

            for (int t = 0; t < Continue; t++)
            {
                contents.Match.RunTick();
                Assert.That(contents.Match.CurrentSnapshotDigest, Is.EqualTo(reference[t]),
                    $"Digest diverged {t + 1} ticks after restore. The match was configured with the "
                    + "COMPOSED availability filter and the snapshot records only the ClubId, so a "
                    + "restore that re-applies #41's removals alone re-selects from a larger candidate "
                    + "set and puts a different eleven's attribute records on the pitch — silently, "
                    + "with every gate green.");
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
// | 1.1     | 2026-08-13 | —      | #44 C1/C2 adversarial review, H2: the mid-match restore-fidelity  |
// |         |            |        | lock — a save whose eleven was shaped by a SUSPENSION must        |
// |         |            |        | restore that same eleven, proven by a 60-tick digest              |
// |         |            |        | continuation (the #29/#41 T2 AR's technique, on the contributor   |
// |         |            |        | that reopened its defect). + the temp-dir SetUp/TearDown this one |
// |         |            |        | disk-touching case needs.                                          |
// | 1.2     | 2026-08-13 | —      | #44 C1/C2 adversarial review round 2, M7 (ERR-030-037): new       |
// |         |            |        | ANewBanEarnedThisFixtureIsNotServedByThisSameFixture — the         |
// |         |            |        | within-fixture half of the off-by-one contract           |
// |         |            |        | AOneMatchBan_CostsExactlyTheNextFixtureAndNoMore cannot see,       |
// |         |            |        | since that test pre-seeds its ban before kickoff. Ground truth     |
// |         |            |        | for what a fixture's OWN cards should produce comes from an        |
// |         |            |        | independent deterministic replay (same seed, same unfiltered       |
// |         |            |        | squads) committed to a fresh, unrelated DisciplineRules with no    |
// |         |            |        | serving call anywhere near it; the production tally must match     |
// |         |            |        | it exactly. Verified by executing: reverting M6 (swapping          |
// |         |            |        | OnClubFixturePlayed and fold.Commit back to their pre-fix order)   |
// |         |            |        | turns this red, and restoring the fix turns it green again.        |
// | 1.3     | 2026-08-13 | —      | AR round 3 fix (M12). **CORRECTION to the row above:** "reverting  |
// |         |            |        | M6 (swapping OnClubFixturePlayed and fold.Commit back to their     |
// |         |            |        | pre-fix order)" mislabels M7 as M6 — swapping the two calls'       |
// |         |            |        | relative order IS M7 (this test's own subject); the real M6 is    |
// |         |            |        | the whole block's POSITION relative to MarkFixturePlayed, and      |
// |         |            |        | moving the whole block back above MarkFixturePlayed leaves the     |
// |         |            |        | two calls' relative order — and therefore this test — unchanged    |
// |         |            |        | and green. New test                                                |
// |         |            |        | AThrowInsideTheServeAndCommitBlock_LeavesTheFixturePlayed_And-     |
// |         |            |        | DoesNotDoubleServeOnRetry locks M6 itself, via SeasonLoop's new    |
// |         |            |        | TestOnly_AfterHomeClubServed hook (v1.23) rather than a real       |
// |         |            |        | config-driven throw — DisciplineConstants' [GT]s cannot be         |
// |         |            |        | rebound in this process, and OnClubFixturePlayed is not fallible   |
// |         |            |        | under any bound config regardless (L9). VERIFIED by executing:     |
// |         |            |        | temporarily moving the discipline block back above                |
// |         |            |        | MarkFixturePlayed in SeasonLoop.PlayNextRound turns the NEW test   |
// |         |            |        | red (the Played assertion fails) while leaving                     |
// |         |            |        | ANewBanEarnedThisFixtureIsNotServedByThisSameFixture green,        |
// |         |            |        | confirming the two tests lock two different properties; restoring  |
// |         |            |        | the fix turns the new test green again.                            |
// | 1.4     | 2026-08-13 | —      | AR round 4 (M16): the M12 position lock now forces its throw       |
// |         |            |        | through ThrowOnFirstServeDriver, a SeasonLoop.IFixture-            |
// |         |            |        | DisciplineDriver supplied to the constructor, replacing            |
// |         |            |        | SeasonLoop.TestOnly_AfterHomeClubServed — process-static mutable   |
// |         |            |        | state on a production class, invoked from the production round     |
// |         |            |        | loop, whose only safety was this test's own finally (FR-CS-051..   |
// |         |            |        | 054). Same forced-throw position, same assertions, nothing         |
// |         |            |        | static to leak between tests. Plus AFixtureDisciplineDriver-       |
// |         |            |        | WithoutATally_IsRefusedAtComposition for the seam's coherence      |
// |         |            |        | rule. Re-VERIFIED by execution: moving the serve+commit block      |
// |         |            |        | back above MarkFixturePlayed still turns the position lock red.    |
#endregion
