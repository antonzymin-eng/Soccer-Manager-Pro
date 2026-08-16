// File:     src/season-save/tests/SeasonLoopDisciplineTests.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest of all (reviewed-findings pass, M16 — v1.12: comment-string-only fix.
//           TheBackFillPressesAnInjuredPlayerBackBeforeASuspendedOne's failure message quoted the
//           ERR-044-019 sentence ("a banned man plays only when the alternative is a club that cannot
//           take the field at all") that adversarial review declared FALSE of the implementation on
//           both halves; restated to the two-case form (benched whenever any choice permits it; forced
//           into the XI, and only then does the ban stall). No assertion, no test logic, changed.
// Modified: 2026-08-16, latest (reviewed findings pass, findings A/B — v1.11: the groundTruthFold
//           construction in ANewBanEarnedThisFixtureIsNotServedByThisSameFixture updated for
//           CardLedgerFold's new required onPitchAgentIdCount parameter (ERR-044-022),
//           MatchEngineConstants.SQUAD_SIZE. New lock PlayerIdsByAgentId_IsInjectiveAtBoot_
//           ButNotAfterOneSubstitution (ERR-044-023) pins the cross-assembly precondition
//           CardLedgerFold's constructor doc now states explicitly — MatchEngine.PlayerIdsByAgentId()
//           is one-to-one over non-sentinel entries only AT BOOT, and is proven NOT injective after one
//           SubstitutePlayer call, naming the constraint a future caller rebuilding a fold from a
//           restored (necessarily mid-fixture) engine would hit — beside the existing
//           TheEngineAndTheFoldAgreeOnTheNoPlayerSentinel sentinel-agreement lock, the same reasoning
//           for the same reason: neither match-engine nor discipline can see the other's code, so
//           season-save is the one place both are visible.
// Modified: 2026-08-16 (ERR-044-014, adversarial-review H1 — both test IFixtureDisciplineDriver
//           implementations updated for OnClubFixturePlayed's new clubPlayerIds parameter — v1.10)
// Modified: 2026-08-15 (reviewed findings, M1/M2/M3 — v1.9: M1 parameterises the extremis-exemption
//           wiring lock over BOTH clubs of the fixture, the home-only version having been proven
//           unable to catch an away-side argument-swap mutation (ERR-008-002's class, recurring in
//           this file's own newest lock); M2 adds the training-cursor assertion that discriminates
//           SeasonLoop.PlayNextRound's "refused while nothing has been written at all" claim from
//           RunCareerDaySteps running first; M3 adds a new tier-order lock,
//           TheBackFillNeverReinstatesASuspendedPlayerWhileAnyInjuredPlayerRemains, forcing TWO
//           reinstate calls so a defect that only shows up past the first is caught too (ERR-030-042).
//           Prior: 2026-08-15 (AR round 5 fix, ERR-044-006 — v1.8: the Spec: line below drops T-DC-VIEW-001,
//           WITHDRAWN at #44 section-5.md v0.5. This file listed it while implementing no test for it,
//           and neither did discipline/tests/AvailabilityTests.cs, whose only such test was deleted as
//           tautological at AR round 1 — two files claiming one id, neither implementing it. Header
//           comment only; no test logic changed.
//           Prior: 2026-08-15 (AR round 4 fix, M22 — v1.7: ThrowOnFirstServeDriver gains a pass-through
//           RequireCommittableConfig() to match the widened IFixtureDisciplineDriver interface, and a
//           new ThrowOnRequireCommittableConfigDriver + RequireCommittableConfigDriver_RunsBefore-
//           AnyFixtureIsResolved lock the round-level [GT] pre-check's call site itself.
//           Prior: 2026-08-15 (ERR-044-003 stage 1 — ThrowOnFirstServeDriver.OnClubFixturePlayed updated
//           for the new required fieldedPlayerIds parameter, and a new wiring lock proving the extremis
//           back-fill's fielded player is exempt from serving while an unfielded suspended team-mate
//           still serves — v1.6)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3/§5 (T-DC-NEU-001/002, T-DC-FOLD-002, T-DC-BAN-002/003/
//           005/006, T-DC-SAV-002); Season & Competition Loop #30 §3.4/§5 (the composed seam,
//           ERR-030-009/-016/-029/-042, T-SN-DET-004); ERR-044-002 (both resolution paths), ERR-044-003
//           (the fielded-eleven serving exemption, stage 1); ERR-030-037 (the M6/M7 within-fixture
//           serve-before-commit lock); ERR-044-022 (the onPitchAgentIdCount boundary); ERR-044-023
//           (the boot-only PlayerIdsByAgentId precondition); ERR-008-002 (home/away asymmetry); unified
//           season save §4 / KD-6 (restore fidelity — the C1/C2 AR's H2); Code Standards #20
// Purpose:  The #44 T2 WIRING locks. Every case here fails if a wiring point is reverted — the fold's
//           seed and its per-tick pump, the filter at the seam on both paths and both clubs, the
//           serving decrement (including the ERR-044-003 fielded-eleven exemption, now on both clubs
//           of the fixture), the off-by-one (both across fixtures and WITHIN one), the composition with
//           #41 (including the back-fill's tier order across multiple reinstatements), the round-level
//           pre-check's position relative to BOTH the fixture loop and RunCareerDaySteps, and the
//           boundary sweep. #44's own rules are unit tested in discipline/tests; nothing here re-tests
//           them.

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
                    sorted[i], DisciplineConstants.LeagueCompetitionKey, 0, matches);
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

            // M22: pass-through, matching RulesFixtureDisciplineDriver's production behaviour — this
            // driver exists to force a throw INSIDE the serve+commit block (M12/M6), not at the M17
            // round-level pre-check, which ThrowOnRequireCommittableConfigDriver below covers instead.
            public void RequireCommittableConfig() => CardLedgerFold.RequireCommittableConfig();

            public void OnClubFixturePlayed(int clubId, int[] clubPlayerIds, int[] fieldedPlayerIds)
            {
                _rules.OnClubFixturePlayed(clubId, clubPlayerIds, fieldedPlayerIds);
                _serves++;
                if (_serves == 1)
                {
                    throw new System.InvalidOperationException(
                        "M16/M12 forced throw — locks the serve+commit block's position.");
                }
            }

            public void CommitFixtureCards(CardLedgerFold foldOrNull) => foldOrNull?.Commit(_rules);
        }

        /// <summary>
        /// M22's own lock. Throws from <see cref="SeasonLoop.IFixtureDisciplineDriver.RequireCommittableConfig"/>
        /// alone — <see cref="OnClubFixturePlayed"/> and <see cref="CommitFixtureCards"/> must never be
        /// reached if <see cref="PlayNextRound"/> genuinely asks the round-level question BEFORE the
        /// fixture loop, so both throw too (a defensive assertion failure, not the expected exception),
        /// proving this driver is never even asked to serve or commit anything.
        /// </summary>
        private sealed class ThrowOnRequireCommittableConfigDriver : SeasonLoop.IFixtureDisciplineDriver
        {
            public void RequireCommittableConfig() =>
                throw new System.InvalidOperationException(
                    "M22 forced throw — locks the round-level [GT] pre-check's call site.");

            public void OnClubFixturePlayed(int clubId, int[] clubPlayerIds, int[] fieldedPlayerIds) =>
                throw new System.InvalidOperationException(
                    "OnClubFixturePlayed must not run: RequireCommittableConfig should have thrown "
                    + "before the fixture loop was ever entered.");

            public void CommitFixtureCards(CardLedgerFold foldOrNull) =>
                throw new System.InvalidOperationException(
                    "CommitFixtureCards must not run: RequireCommittableConfig should have thrown "
                    + "before the fixture loop was ever entered.");
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

        /// <summary>Counts non-sentinel entries and returns whether every one is unique.</summary>
        private static bool IsInjectiveOverNonSentinelEntries(int[] byAgent)
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < byAgent.Length; i++)
            {
                if (byAgent[i] == MatchEngineConstants.NO_PLAYER_ID)
                {
                    continue;
                }
                if (!seen.Add(byAgent[i]))
                {
                    return false;
                }
            }
            return true;
        }

        [Test]
        public void PlayerIdsByAgentId_IsInjectiveAtBoot_ButNotAfterOneSubstitution()
        {
            // ERR-044-023. CardLedgerFold's constructor doc and its type remarks both now state a
            // BOOT-ONLY precondition on the array MatchEngine.PlayerIdsByAgentId returns: one-to-one
            // over its non-sentinel entries only at that moment. Neither assembly can see the other's
            // code (match-engine cannot reference discipline; discipline cannot reference match-engine),
            // so season-save — the one place both are visible — is where that precondition is pinned or
            // nowhere, exactly like TheEngineAndTheFoldAgreeOnTheNoPlayerSentinel above pins the
            // sentinel agreement. This is deliberately not asserted through CardLedgerFold's own
            // constructor (which would just re-derive the M1 guard, already locked in
            // CardLedgerFoldTests); it is asserted directly against the engine's array, naming the
            // property a future caller who rebuilds a fold from a RESTORED (necessarily mid-fixture,
            // necessarily post-substitution) engine would hit.
            League league = FourClubLeague();
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = loop.State.FixtureAt(0);
            TacticalDirector.MatchEngine.MatchEngine engine = loop.BootFixtureEngine(in fixture, league);

            Assert.That(IsInjectiveOverNonSentinelEntries(engine.PlayerIdsByAgentId()), Is.True,
                "At boot, before any substitution, PlayerIdsByAgentId() must be one-to-one over its "
                + "non-sentinel entries — this is the precondition CardLedgerFold's constructor relies "
                + "on and SeasonLoop.PlayThroughEngine satisfies by seeding immediately after boot.");

            // Slot 1 rather than 0: slot 0 is the goalkeeper and bench 0 need not be one (the same
            // choice PlayerIdsByAgentId_FollowsASubstitution above makes).
            engine.SubstitutePlayer(teamId: 0, outSlotIndex: 1, benchIndex: 0, SubstitutionReason.Tactical);

            // Documenting the known shape (ERR-044-023), not a regression this test can fix: MatchEngine.
            // SubstitutePlayer copies the incoming player's identity onto the outgoing on-pitch slot but
            // never clears his OWN bench-origin entry, so after one substitution he occupies TWO agent
            // ids and the array is no longer injective. A fold seeded from THIS array would correctly
            // throw at construction (CardLedgerFold's own M1 one-to-one check) — which is exactly why a
            // caller must take the seed at boot, before any substitution, and never re-derive it later.
            Assert.That(IsInjectiveOverNonSentinelEntries(engine.PlayerIdsByAgentId()), Is.False,
                "After one substitution, PlayerIdsByAgentId() is expected to be NON-injective — the "
                + "substitute now occupies both his new on-pitch slot and his stale bench id. If this "
                + "ever starts passing, MatchEngine.SubstitutePlayer has started clearing the bench "
                + "entry and the boot-only precondition this test exists to name may no longer be "
                + "necessary — but the doc comments it locks (CardLedgerFold's constructor, MatchEngine."
                + "PlayerIdsByAgentId, and this test's own) would then need revisiting together, not "
                + "just this assertion.");
        }

        // ── the fold's seed: the engine reports who it actually fielded ────────────────────

        [Test]
        public void PlayerIdsByAgentId_ReportsTheLineupTheEngineActuallyConfigured()
        {
            League league = FourClubLeague();
            SeasonLoop loop = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = loop.State.FixtureAt(0);

            TacticalDirector.MatchEngine.MatchEngine engine =
                loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out int[] awayXi, out _, out _);

            int[] byAgent = engine.PlayerIdsByAgentId();

            Assert.That(byAgent.Length, Is.EqualTo(MatchEngineConstants.AgentIdSpace),
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
            for (int b = MatchEngineConstants.SQUAD_SIZE; b < MatchEngineConstants.AgentIdSpace; b++)
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
            unfiltered.BootFixtureEngine(in fixture, league, out int[] baselineXi, out _, out _, out _);

            int bannedStarter = baselineXi[5];
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out _, BannedState(bannedStarter));

            loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out _, out _, out _);

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
            unfiltered.BootFixtureEngine(in fixture, league, out _, out int[] baselineAwayXi, out _, out _);

            int bannedStarter = baselineAwayXi[5];
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out _, BannedState(bannedStarter));

            loop.BootFixtureEngine(in fixture, league, out _, out int[] awayXi, out _, out _);

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
                Availability.IsAvailable(state, banned, DisciplineConstants.LeagueCompetitionKey),
                Is.False,
                "Precondition: the player starts the round suspended.");

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(
                Availability.IsAvailable(state, banned, DisciplineConstants.LeagueCompetitionKey),
                Is.True,
                "After serving one fixture a one-match ban is discharged.");
            Assert.That(state.HasEntry(banned, DisciplineConstants.LeagueCompetitionKey), Is.False,
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
                    groundTruthEngine.PlayerIdsByAgentId(), MatchEngineConstants.SQUAD_SIZE,
                    DisciplineConstants.LeagueCompetitionKey);
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

        [Test]
        public void RequireCommittableConfigDriver_RunsBeforeAnyFixtureIsResolved()
        {
            // M22. PlayNextRound previously asked its round-level [GT] question by calling the STATIC
            // CardLedgerFold.RequireCommittableConfig() directly — nothing in this process can bind a
            // bad DisciplineConstants value (public static readonly, resolved once at boot), so no test
            // could ever prove that call was reached, and deleting it left the whole tree green. Routing
            // it through IFixtureDisciplineDriver.RequireCommittableConfig lets THIS test substitute an
            // implementation that always throws, proving the round-level pre-check is both reached and
            // reached before the fixture loop: no fixture this round would otherwise resolve can have
            // been marked played yet.
            //
            // L1 (adversarial review over AR round 5): this test's ONLY assertion USED TO BE
            // Played == False, which locks the pre-check's position relative to the FIXTURE LOOP alone
            // — NOT relative to RunCareerDaySteps. Moving the pre-check to run after
            // RunCareerDaySteps(worldDay) but still before the fixture loop would leave that assertion
            // green (no fixture would yet be marked played) while breaking "refused while nothing has
            // been written at all", since a career day-step is itself a write. The comment previously
            // recorded that gap as retracted rather than closed; M2 (reviewed findings, 2026-08-15)
            // closes it with the cursor assertion below, since production and test comment had drifted
            // to disagree on the same claim otherwise.
            // VERIFIED by executing (M3, adversarial review over AR round 5): temporarily deleting the
            // `_disciplineDriver.RequireCommittableConfig();` call in SeasonLoop.PlayNextRound does NOT
            // turn this test red at Assert.Throws — ThrowOnRequireCommittableConfigDriver's OTHER two
            // members (OnClubFixturePlayed, CommitFixtureCards) also throw InvalidOperationException,
            // so the fixture loop reaches OnClubFixturePlayed instead and Assert.Throws still sees an
            // exception of the expected type and PASSES. The mutation is caught downstream: by the time
            // OnClubFixturePlayed throws, MarkFixturePlayed has already run, so `Played` reads True and
            // the `Assert.That(..., Is.False, ...)` below FAILS. That assertion is the only thing this
            // test locks; Assert.Throws alone would pass under this exact mutation. Restoring the call
            // turns the test green again (RequireCommittableConfig throws first, before the fixture
            // loop is ever entered, so Played stays False).
            // VERIFIED by executing (M2, reviewed findings, 2026-08-15): moving the
            // `_disciplineDriver.RequireCommittableConfig();` call below `RunCareerDaySteps(worldDay);`
            // in SeasonLoop.PlayNextRound leaves `Played == False` green (still before the fixture loop)
            // but turns the new `LastAdvancedWorldDay` assertion below red — the career day-step has
            // already advanced the cursor by the time the pre-check throws. Restoring the call's
            // position turns it green again. See the CHANGELOG entry for the observed run.
            League league = FourClubLeague();
            var tally = new DisciplineState();
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.QuickSimAll, out PlayerCareerStates career, tally,
                new ThrowOnRequireCommittableConfigDriver());

            loop.AdvanceToNextFixtureDay();

            // Captured AFTER AdvanceToNextFixtureDay, not before: that call runs the KD-2 day steps for
            // every day up to (but not including) the fixture day itself (SeasonLoop.cs's own
            // AdvanceToNextFixtureDay doc — "the fixture day's OWN day steps have not run when this
            // returns"), so the cursor is already off its sentinel by this point on any league whose
            // first fixture day is not day 0. Capturing before that call — as this test did on its
            // first attempt — makes the assertion below fail on UNMODIFIED production code, comparing
            // AdvanceToNextFixtureDay's own advance against a sentinel that was never going to survive it.
            uint cursorBefore = career.TrainingBlocks()[0].States[0].LastAdvancedWorldDay;

            Assert.Throws<System.InvalidOperationException>(
                () => loop.AdvanceAndPlayNextRound(league));

            Assert.That(loop.State.FixtureAt(0).Played, Is.False,
                "The round-level pre-check must fail BEFORE any fixture is touched — if this fixture "
                + "were marked played, RequireCommittableConfig ran after (or was skipped and) the "
                + "fixture loop, not before it.");

            Assert.That(career.TrainingBlocks()[0].States[0].LastAdvancedWorldDay, Is.EqualTo(cursorBefore),
                "The round-level pre-check must fail BEFORE RunCareerDaySteps, not merely before the "
                + "fixture loop — if the training cursor has already advanced, a career day-step ran "
                + "before the config was refused, and SeasonLoop.PlayNextRound's M17 comment (\"the "
                + "config that could throw here is refused while nothing has been written at all\") is "
                + "false.");
        }

        // ── composition with #41, and the ERR-044-003 tiering ─────────────────────────────

        [Test]
        public void ASuspendedAndAnInjuredPlayerAreBothRemoved()
        {
            League league = FourClubLeague();
            SeasonLoop probe = LoopOver(league, RoundResolutionMode.FullEngine, out _);
            Fixture fixture = probe.State.FixtureAt(0);
            probe.BootFixtureEngine(in fixture, league, out int[] baselineXi, out _, out _, out _);

            int suspended = baselineXi[2];
            int injured = baselineXi[7];

            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out PlayerCareerStates career,
                BannedState(suspended));

            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 12;
            career.SetMedicalState(fixture.HomeClubId, injured, in knock);

            loop.BootFixtureEngine(in fixture, league, out int[] homeXi, out _, out _, out _);

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
                full, career, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(fielded), Is.True,
                "The composed filter must never stop a club playing (#30 §3.4 / §2.3 F9).");
            Assert.That(Contains(fielded, all[0]), Is.True,
                "The back-fill reinstated a suspended player while an injured one was still available. "
                + "Suspension is the stricter tier — a reinstated suspended player is benched whenever "
                + "any candidate choice permits it (his ban advances normally), and is forced into the "
                + "XI only when no completing choice avoids it, which is the only case where the ban "
                + "stalls (ERR-044-003 stage 1, ERR-044-019).");
        }

        [Test]
        public void TheBackFillNeverReinstatesASuspendedPlayerWhileAnyInjuredPlayerRemains()
        {
            // M3 (reviewed findings, 2026-08-15). #30 §5's T-SN-DET-004 names two depleted-squad locks
            // (the back-fill and the terminal refusal) but neither one is a lock on the back-fill's TIER
            // ORDER itself — the test above proves the tier holds for a SINGLE injured player, which is
            // exactly the case where the first Reinstate call has only one candidate to choose between.
            // Nothing here proved the order holds across MULTIPLE reinstate calls, which is why
            // ERR-030-042 (an implementer following the spec could press banned players back AHEAD of
            // injured ones) had no detector.
            //
            // Every player but three (SuspendedCount) is injured, so the back-fill must reinstate
            // several times before it could possibly run out of injured candidates — the tier order is
            // exercised across an unknown-but-large number of passes rather than hand-picked to be
            // exactly one, which is what let it silently reach the suspended tier the first time this
            // test was written with a tight margin (CanFieldStartingEleven needs the full EIGHTEEN —
            // 11 starters + 7 bench, MatchEngineConstants.PLAYERS_PER_TEAM / SUBSTITUTES_PER_TEAM — and
            // a "first eighteen by roster index" core was one player short of position-complete on this
            // seed's squad, forcing a THIRD reinstate that landed on the suspended tier and correctly
            // failed the not-yet-corrected version of this test). With twenty-two injured candidates
            // against a formation that needs at most eighteen selectable, there is no roster-position
            // combination that can exhaust the injured pool before the suspended three are needed.
            League league = FourClubLeague();
            int clubId = league.ClubIds()[0];
            Squad full = league.ResolveByClubId(clubId);

            var all = new int[full.Count];
            for (int i = 0; i < full.Count; i++)
            {
                all[i] = full.GetPlayer(i).PlayerId;
            }

            const int SuspendedCount = 3;
            var banned = new int[SuspendedCount];
            for (int i = 0; i < SuspendedCount; i++)
            {
                banned[i] = all[all.Length - SuspendedCount + i];
            }

            DisciplineState state = BansOf(1, banned);
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.FullEngine, out PlayerCareerStates career, state);

            int injuredCount = all.Length - SuspendedCount;
            for (int i = 0; i < injuredCount; i++)
            {
                var knock = InjuryState.Create();
                knock.Severity = InjurySeverity.Moderate;
                knock.RecoveryRemaining = i + 1;   // distinct, ascending — a real tie-break order
                career.SetMedicalState(clubId, all[i], in knock);
            }

            Squad fielded = AvailabilityComposition.Compose(
                full, career, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(fielded), Is.True,
                "The composed filter must never stop a club playing (#30 §3.4 / §2.3 F9).");
            Assert.That(fielded.Count, Is.LessThan(all.Length),
                "Precondition: the back-fill must have actually run (not every player selectable), or "
                + "the assertions below are vacuous.");
            for (int i = 0; i < banned.Length; i++)
            {
                Assert.That(Contains(fielded, banned[i]), Is.False,
                    $"Suspended player {banned[i]} was reinstated while {injuredCount} injured players "
                    + "were available to press back instead — the back-fill's suspended tier fired "
                    + "ahead of the injured tier (ERR-030-042).");
            }
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
                squad, career, loop.Discipline, DisciplineConstants.LeagueCompetitionKey);

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
                full, null, tally, DisciplineConstants.LeagueCompetitionKey);
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

        // ── ERR-044-003 stage 1: the extremis exemption, wired end to end ─────────────────

        // M1 (reviewed findings, 2026-08-15): parameterised over BOTH clubs of the fixture, not just
        // the home one. Proven live by execution: mutating SeasonLoop.PlayNextRound's away call from
        // `OnClubFixturePlayed(fixture.AwayClubId, awayXi)` to `OnClubFixturePlayed(fixture.AwayClubId,
        // homeXi)` left the home-only version of this test at 22/22 green, because every entry it walks
        // belongs to `fixture.HomeClubId` and is skipped by the driver's own club check before
        // `WasFielded` is ever reached with the away array — the root CLAUDE.md's home-team-only trap
        // (ERR-008-002), recurring in this file's own newest lock two tests after it cites that id by
        // name (`EnginePath_ExcludesASuspendedOPPONENTPlayerToo`, above). The `[TestCase(false)]` case
        // below reads the fixture's AWAY club and fails under that exact mutation (see the CHANGELOG
        // entry for the observed run).
        [TestCase(true, TestName = "ASuspendedPlayerPressedInByTheExtremisBackFill_IsExemptFromServing_WhileAnUnfieldedSuspendedTeammateStillServes_Home")]
        [TestCase(false, TestName = "ASuspendedPlayerPressedInByTheExtremisBackFill_IsExemptFromServing_WhileAnUnfieldedSuspendedTeammateStillServes_Away")]
        public void ASuspendedPlayerPressedInByTheExtremisBackFill_IsExemptFromServing_WhileAnUnfieldedSuspendedTeammateStillServes(bool useHomeClub)
        {
            // ERR-044-003 stage 1's own wiring lock. #30 §2.3 F9's back-fill can press a suspended
            // player onto the pitch when too many of his club's players are unavailable to field the
            // Stage-0 formation otherwise (AvailabilityComposition.Reinstate's extremis tier). Before
            // this landing the SAME fixture's OnClubFixturePlayed call then served his ban anyway,
            // making the appearance strictly free. This locks BOTH halves in one played round:
            // (a) POSITIVE CONTROL — a suspended player really is in the fielded eleven, or the rest
            // of this test is vacuous the way ERR-030-014 was (and this file's own
            // ARealEngineFixtureFoldsItsCardsOntoPlayerRecordsAndChangesNothingElse exists to guard
            // against); (b) that player's ban is UNCHANGED after the round while a suspended
            // team-mate who was NOT fielded still serves — so the test cannot pass by the exemption
            // firing for everybody unconditionally.
            League league = FourClubLeague();
            SeasonLoop probe = LoopOver(league, RoundResolutionMode.QuickSimAll, out _);
            Fixture fixture = probe.State.FixtureAt(0);
            int clubId = useHomeClub ? fixture.HomeClubId : fixture.AwayClubId;
            Squad full = league.ResolveByClubId(clubId);

            var all = new int[full.Count];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = full.GetPlayer(i).PlayerId;
            }

            // Ban enough of the roster that fewer than eighteen players (11 starters + 7 bench —
            // MatchEngineConstants.PLAYERS_PER_TEAM / SUBSTITUTES_PER_TEAM) remain directly
            // selectable, forcing AvailabilityComposition's back-fill to press some of the banned
            // players back in. Nothing here is injured, so every reinstatement comes from the
            // suspended tier. CLUB_SQUAD_SIZE is 25 (PlayerDatabaseConstants), so banning the last
            // BannedCount players leaves (25 - BannedCount) directly selectable; determined
            // empirically (dotnet test, iterating this constant) rather than guessed — 10 leaves 15
            // selectable, needing 3 reinstated, comfortably inside the 10 banned so at least one
            // banned player is left over as the "still serves" control.
            const int BannedCount = 10;
            var banned = new int[BannedCount];
            for (int i = 0; i < BannedCount; i++)
            {
                banned[i] = all[all.Length - BannedCount + i];
            }

            DisciplineState state = BansOf(50, banned);   // long enough to survive one served match
            SeasonLoop loop = LoopOver(
                league, RoundResolutionMode.QuickSimAll, out PlayerCareerStates career, state);

            // The fielded eleven the wired seam is about to use — computed the SAME way SeasonLoop's
            // private SelectAvailable/FieldedXi do (AvailabilityComposition.Compose then
            // SquadRating.StartingElevenPlayerIds), over the identical squad/career/discipline
            // instances AdvanceAndPlayNextRound is about to consume. Pure and side-effect-free, so
            // computing it here disturbs nothing the round itself does.
            Squad composed = AvailabilityComposition.Compose(
                full, career, state, DisciplineConstants.LeagueCompetitionKey);
            int[] expectedXi = SquadRating.StartingElevenPlayerIds(composed);

            int fieldedSuspended = -1;
            int unfieldedSuspended = -1;
            for (int i = 0; i < banned.Length; i++)
            {
                bool inXi = System.Array.IndexOf(expectedXi, banned[i]) >= 0;
                if (inXi && fieldedSuspended < 0)
                {
                    fieldedSuspended = banned[i];
                }
                if (!inXi && unfieldedSuspended < 0)
                {
                    unfieldedSuspended = banned[i];
                }
            }

            Assert.That(fieldedSuspended, Is.GreaterThanOrEqualTo(0),
                "POSITIVE CONTROL: no suspended player was pressed into the fielded eleven by this "
                + "fixture. BannedCount must be raised until the extremis back-fill genuinely fires — "
                + "without this the rest of the test is vacuous.");
            Assert.That(unfieldedSuspended, Is.GreaterThanOrEqualTo(0),
                "Precondition: at least one banned player must be left OFF the fielded eleven, or the "
                + "'still serves' half below cannot be distinguished from the exemption firing for "
                + "everybody unconditionally.");

            int banBeforeFielded = Ban(state, fieldedSuspended);
            int banBeforeUnfielded = Ban(state, unfieldedSuspended);

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(Ban(state, fieldedSuspended), Is.EqualTo(banBeforeFielded),
                "A suspended player the extremis back-fill actually fielded must NOT have his ban "
                + "served by the same fixture he played in (ERR-044-003 stage 1).");
            Assert.That(Ban(state, unfieldedSuspended), Is.EqualTo(banBeforeUnfielded - 1),
                "A suspended team-mate who was NOT fielded must still serve one match of his ban — "
                + "otherwise this test would pass even if the exemption served nobody at all.");
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
                new DisciplineEntry(player, DisciplineConstants.LeagueCompetitionKey, 4, 100),
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

        // ── M18: PlayerCareerStates.MarkUnavailable's OWNING contract, driven directly ────
        //
        // #30's composed seam (AvailabilityComposition.Compose) always hands MarkUnavailable a
        // freshly allocated all-false mask, so the M14 "OWNS every index unconditionally" contract
        // and a merely-additive one are indistinguishable through that call site alone — reverting
        // the fix to additive (skip an index already false/0 instead of writing it) leaves the whole
        // tree green. These two tests drive MarkUnavailable directly with a PRE-SET mask, the only
        // way to observe the difference — the same reason Availability.MarkSuspended's own coverage
        // in AvailabilityTests.cs pre-seeds its masks. This file, not PlayerCareerStatesTests.cs
        // (season-save/tests has no such file today), because SeasonLoopDisciplineTests.cs is the
        // #44/#30 wiring suite this session owns.

        [Test]
        public void MarkUnavailable_OwnsTheMask_ClearsAPreSetFlagForAFitPlayer()
        {
            League league = FourClubLeague();
            LoopOver(league, RoundResolutionMode.QuickSimAll, out PlayerCareerStates career);
            int clubId = league.ClubIds()[0];
            Squad squad = league.ResolveByClubId(clubId);

            var removed = new bool[squad.Count];
            removed[0] = true;   // pre-set true for a player who is NOT injured — stale/shared mask
            var recoveryRemaining = new int[squad.Count];
            recoveryRemaining[0] = 99;   // pre-set to a stale nonzero value

            int injuredCount = career.MarkUnavailable(squad, removed, recoveryRemaining);

            Assert.That(removed[0], Is.False,
                "M18/M14: a pre-set true for a player who is NOT injured must be cleared — this "
                + "method owns the whole mask unconditionally, the same contract "
                + "Availability.MarkSuspended carries.");
            Assert.That(recoveryRemaining[0], Is.EqualTo(0),
                "M18/M14: recoveryRemaining for a player this call does not remove must be written 0, "
                + "not left at whatever the caller passed in.");
            Assert.That(injuredCount, Is.EqualTo(0), "nobody in this fresh career carries an injury.");
        }

        [Test]
        public void MarkUnavailable_RecoveryRemaining_IsZeroForAFitPlayer_EvenWhenOthersAreInjured()
        {
            League league = FourClubLeague();
            LoopOver(league, RoundResolutionMode.QuickSimAll, out PlayerCareerStates career);
            int clubId = league.ClubIds()[0];
            Squad squad = league.ResolveByClubId(clubId);

            int injuredPlayerId = squad.GetPlayer(0).PlayerId;
            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 7;
            career.SetMedicalState(clubId, injuredPlayerId, in knock);

            var removed = new bool[squad.Count];
            var recoveryRemaining = new int[squad.Count];
            recoveryRemaining[1] = 42;   // stale value for a player who is NOT injured

            int injuredCount = career.MarkUnavailable(squad, removed, recoveryRemaining);

            Assert.That(injuredCount, Is.EqualTo(1));
            Assert.That(removed[0], Is.True, "the genuinely injured player must be marked.");
            Assert.That(recoveryRemaining[0], Is.EqualTo(7));
            Assert.That(removed[1], Is.False);
            Assert.That(recoveryRemaining[1], Is.EqualTo(0),
                "M18/M14: a fit player's recoveryRemaining must be zeroed, not left at a stale value "
                + "the caller (or a prior contributor) had written.");
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
            state.EntryFor(playerId, DisciplineConstants.LeagueCompetitionKey).BanMatchesRemaining;

        private static int Yellows(DisciplineState state, int playerId) =>
            state.EntryFor(playerId, DisciplineConstants.LeagueCompetitionKey).Yellows;
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
// | 1.5     | 2026-08-13 | —      | AR round 5 fix (M18): MarkUnavailable_OwnsTheMask_ClearsAPreSet-   |
// |         |            |        | FlagForAFitPlayer and MarkUnavailable_RecoveryRemaining_IsZero-    |
// |         |            |        | ForAFitPlayer_EvenWhenOthersAreInjured — PlayerCareerStates.       |
// |         |            |        | MarkUnavailable's M14 owning contract had zero coverage:           |
// |         |            |        | AvailabilityComposition.Compose always hands it a freshly          |
// |         |            |        | allocated all-false mask, so an additive implementation would      |
// |         |            |        | behave identically through the wired seam and the whole tree       |
// |         |            |        | stayed green under a revert. These two drive the method directly   |
// |         |            |        | with a pre-set mask, mirroring Availability.MarkSuspended's own    |
// |         |            |        | AvailabilityTests coverage.                                        |
// | 1.6     | 2026-08-15 | —      | ERR-044-003 stage 1. ThrowOnFirstServeDriver.OnClubFixturePlayed  |
// |         |            |        | updated for DisciplineRules' new required int[] fieldedPlayerIds  |
// |         |            |        | parameter (forwarded straight through — no behaviour change for   |
// |         |            |        | the M12/M6 position lock it drives). New: ASuspendedPlayerPressed-|
// |         |            |        | InByTheExtremisBackFill_IsExemptFromServing_WhileAnUnfielded-      |
// |         |            |        | SuspendedTeammateStillServes — the wiring lock for the exemption   |
// |         |            |        | itself, over a real played round. Positive control computes the   |
// |         |            |        | fielded eleven the same way SelectAvailable/FieldedXi do          |
// |         |            |        | (AvailabilityComposition.Compose then SquadRating.Starting-        |
// |         |            |        | ElevenPlayerIds) to identify a genuinely back-filled suspended     |
// |         |            |        | player without hardcoding which one the tier order picks, then    |
// |         |            |        | asserts his ban is unchanged after the round while an unfielded    |
// |         |            |        | suspended team-mate's decrements by one, in the same call.         |
// | 1.7     | 2026-08-15 | —      | AR round 4 fix (M22). ThrowOnFirstServeDriver gains a pass-through |
// |         |            |        | RequireCommittableConfig() (SeasonLoop.IFixtureDisciplineDriver    |
// |         |            |        | widened by the same fix). New ThrowOnRequireCommittableConfig-     |
// |         |            |        | Driver + RequireCommittableConfigDriver_RunsBeforeAnyFixtureIs-    |
// |         |            |        | Resolved: the round-level [GT] pre-check previously called the     |
// |         |            |        | static CardLedgerFold.RequireCommittableConfig() directly, which   |
// |         |            |        | nothing in this process could ever drive through a failing config  |
// |         |            |        | (DisciplineConstants is public static readonly, resolved once at   |
// |         |            |        | its non-negative defaults) — deleting the call left the whole tree |
// |         |            |        | green. **CORRECTED at v1.9 (M3):** the claim below that deleting   |
// |         |            |        | `_disciplineDriver.RequireCommittableConfig();` turns the new test |
// |         |            |        | red "because Assert.Throws sees no exception" was FALSE — under    |
// |         |            |        | that mutation Assert.Throws still PASSES (ThrowOnRequireCommittable|
// |         |            |        | ConfigDriver's other two members also throw, so the fixture loop   |
// |         |            |        | supplies an exception of the same type); the test only turns red   |
// |         |            |        | because the Played == False assertion fails once MarkFixturePlayed |
// |         |            |        | has already run by the time that later throw fires. Original text, |
// |         |            |        | for the record: "VERIFIED by executing: deleting                   |
// |         |            |        | `_disciplineDriver.RequireCommittableConfig();` from PlayNextRound  |
// |         |            |        | turns the new test red (Assert.Throws sees no exception), restoring|
// |         |            |        | it turns the test green again."                                    |
// | 1.8     | 2026-08-15 | —      | AR round 5 fix (ERR-044-006), header comment ONLY — no test logic  |
// |         |            |        | changed. The Spec: line claimed T-DC-VIEW-001, which this file    |
// |         |            |        | has never implemented; its only test lived in discipline/tests/   |
// |         |            |        | AvailabilityTests.cs and was deleted at AR round 1 as             |
// |         |            |        | tautological (that file's v1.1, L4(a)), so two files claimed the  |
// |         |            |        | id and neither implemented it. The row is now WITHDRAWN at #44    |
// |         |            |        | section-5.md v0.5. Replaced with the ids this file does lock:     |
// |         |            |        | T-DC-NEU-002 (WithNothingUnavailable_TheSeamHandsBackTheSAME-     |
// |         |            |        | SquadInstance), T-DC-FOLD-002's engine-side occupancy half        |
// |         |            |        | (PlayerIdsByAgentId_FollowsASubstitution) and T-DC-BAN-006 (the   |
// |         |            |        | ERR-044-003 stage 1 exemption lock added at v1.6).                |
// | 1.9     | 2026-08-15 | —      | Reviewed findings, M1/M2/M3, all mutation-verified.               |
// |         |            |        | **M1:** ASuspendedPlayerPressedInByTheExtremisBackFill_Is-        |
// |         |            |        | ExemptFromServing_WhileAnUnfieldedSuspendedTeammateStillServes    |
// |         |            |        | parameterised [TestCase(true/false)] over home/away — the         |
// |         |            |        | home-only version could not catch SeasonLoop.PlayNextRound's away |
// |         |            |        | call passing the wrong XI (ERR-008-002's class); the fixture's    |
// |         |            |        | AWAY club is now exercised too. **M2:** RequireCommittableConfig- |
// |         |            |        | Driver_RunsBeforeAnyFixtureIsResolved gains a training-cursor     |
// |         |            |        | assertion (career.TrainingBlocks()[0].States[0].LastAdvanced-     |
// |         |            |        | WorldDay unchanged) discriminating the pre-check's position       |
// |         |            |        | relative to RunCareerDaySteps, not just the fixture loop — closing|
// |         |            |        | the gap the L1 correction at v1.7/v1.8 left open. **M3:** new     |
// |         |            |        | TheBackFillNeverReinstatesASuspendedPlayerWhileAnyInjuredPlayer-  |
// |         |            |        | Remains — the back-fill tier order (ERR-030-042, resolved spec-   |
// |         |            |        | only against already-correct code) had no locking test at all;   |
// |         |            |        | this one forces two reinstate calls with two injured players so a|
// |         |            |        | defect past the first call is caught too.                        |
// | 1.10    | 2026-08-16 | —      | ERR-044-014 (adversarial review, H1). ThrowOnFirstServeDriver    |
// |         |            |        | and ThrowOnRequireCommittableConfigDriver updated for            |
// |         |            |        | IFixtureDisciplineDriver.OnClubFixturePlayed's new clubPlayerIds |
// |         |            |        | parameter — #44 now reads club membership from the roster the    |
// |         |            |        | loop resolved instead of deriving it from #27's id packing. The  |
// |         |            |        | first driver forwards it to DisciplineRules unchanged; every     |
// |         |            |        | test's assertions are untouched, including the extremis-         |
// |         |            |        | exemption pair, whose banned players are all on the roster the   |
// |         |            |        | loop now supplies.                                               |
// | 1.11    | 2026-08-16, latest | — | Reviewed findings pass, findings A/B. groundTruthFold's       |
// |         |            |        | construction updated for CardLedgerFold's new required           |
// |         |            |        | onPitchAgentIdCount parameter (ERR-044-022), passing              |
// |         |            |        | MatchEngineConstants.SQUAD_SIZE. New cross-assembly lock          |
// |         |            |        | PlayerIdsByAgentId_IsInjectiveAtBoot_ButNotAfterOneSubstitution   |
// |         |            |        | (ERR-044-023) proves PlayerIdsByAgentId() is one-to-one over     |
// |         |            |        | non-sentinel entries at boot and NOT after one SubstitutePlayer  |
// |         |            |        | call, pinning the boot-only precondition CardLedgerFold's         |
// |         |            |        | constructor doc now states explicitly, beside the existing        |
// |         |            |        | NO_PLAYER-sentinel-agreement lock this file already carries for   |
// |         |            |        | the same reason (neither match-engine nor discipline can see the |
// |         |            |        | other's code).                                                     |
// | 1.12    | 2026-08-16, latest of all | — | Reviewed-findings pass, M16. Comment-string-only fix: |
// |         |            |        | TheBackFillPressesAnInjuredPlayerBackBeforeASuspendedOne's failure |
// |         |            |        | message quoted the ERR-044-019 sentence declared false of the     |
// |         |            |        | implementation ("a banned man plays only when the alternative is  |
// |         |            |        | a club that cannot take the field at all"); restated to the       |
// |         |            |        | two-case form. No assertion or test logic changed.                |
#endregion
