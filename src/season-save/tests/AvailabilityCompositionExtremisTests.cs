// File:     src/season-save/tests/AvailabilityCompositionExtremisTests.cs
// Created:  2026-08-16
// Modified: 2026-08-16, later (ERR-030-045 — the multi-reinstatement case stopped asserting the
//           PRE-FIX key on a roster where the choice could not matter, and a k >= 2 lock added)
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 / §2.3 F9 (the depleted-squad back-fill and its
//           within-tier ordering key — ERR-030-044, ERR-030-045); Discipline & Suspensions #44 §2.3 /
//           §7.2 / FR-DC-011 (ERR-044-019, the statement of the extremis compromise); ERR-044-003
//           stage 1 (the fielded-eleven serving exemption); Code Standards #20
// Purpose:  Locks for the tier-2 (suspended) reinstatement CHOICE — that the back-fill presses a
//           suspended player onto the BENCH whenever any candidate choice permits it (including when
//           it takes TWO reinstatements to get there), that his ban then advances because he did not
//           play, and that the forced-start residual is exactly the recorded compromise, no wider.

using NUnit.Framework;

using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// The ERR-030-044 locks. Every fixture here is a HAND-BUILT roster rather than a bootstrapped
    /// league, because the defect these tests exist to catch is a CHOICE between candidates: it is
    /// invisible unless the roster order and the rating order of the suspended candidates disagree,
    /// which a generated squad supplies only by luck.
    /// <para>
    /// <b>Why the trigger is bench depth, not the eleven.</b> #30 §3.4's probe is
    /// <see cref="SquadRating.CanFieldStartingEleven"/>, which is the full selection walk — eleven
    /// position-matched starters PLUS the seven-slot bench (<c>PLAYERS_PER_TEAM</c> +
    /// <c>SUBSTITUTES_PER_TEAM</c> = 18). So a club with seventeen fit, position-complete players
    /// reaches the extremis tier while still able to field a perfectly legal XI, and the reinstated
    /// suspended player belongs on the BENCH: he is needed for the eighteenth slot, not the eleventh.
    /// </para>
    /// <para>
    /// <b>Why two of these cases need a DOUBLE shortfall</b> (ERR-030-045). Fieldability is monotone in
    /// adding players, so while a club is short by more than one no candidate is fieldable and the two
    /// probe-qualified passes are structurally unreachable — the third pass decides alone, and its key
    /// is the only thing standing between a mass-suspension club and its best banned man in the XI. A
    /// single-reinstatement fixture cannot exercise that pass at all, which is how the original
    /// ERR-030-044 suite came to assert the PRE-FIX key on a roster where the choice could not matter.
    /// </para>
    /// </summary>
    [TestFixture]
    internal class AvailabilityCompositionExtremisTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const ulong SeasonSeed = 0x0C0FFEE0B1A5E5EDUL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;
        private const int CraftedClubId = 0;

        // ── 1. the mutant-killer: earliest-roster is NOT the ordering key ─────────────────

        [Test]
        public void BenchDepthExtremis_ReinstatesACandidateTheSelectorBenches_NotTheEarliestOnTheRoster()
        {
            // ERR-030-044(b). Seventeen fit players and two suspended ones: the roster-EARLIER
            // suspended player is the club's best forward (he would walk into the XI), the roster-LATER
            // one its worst (he would bench). One reinstatement takes the club from seventeen to
            // eighteen, so exactly one of the two is pressed back — and which one is the whole
            // question. The pre-fix key ("earliest roster position") picks the best forward and puts a
            // banned man in the starting eleven of a club that could field a legal one without him;
            // post-fix the choice is probe-qualified and lands on the man the selector benches.
            Squad full = BenchDepthRoster();
            DisciplineState state = BansOf(3, IdOf(StartingCandidateLocal), IdOf(BenchCandidateLocal));

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "Precondition: the composed filter must never stop a club playing (#30 §2.3 F9).");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Precondition: exactly one suspended player is pressed back — seventeen fit plus one "
                + "reinstatement is the eighteen the selection walk needs. If this is not 18 the "
                + "fixture no longer exercises a CHOICE and the assertions below prove nothing.");

            Assert.That(Contains(composed, IdOf(BenchCandidateLocal)), Is.True,
                "The back-fill did not press back the suspended player the selector would BENCH. "
                + "Tier 2's ordering key is 'the first candidate, in roster order, the selector would "
                + "bench' — earliest roster position is the fallback for when no choice keeps every "
                + "reinstated-suspended player out of the XI, not the rule (ERR-030-044).");
            Assert.That(Contains(composed, IdOf(StartingCandidateLocal)), Is.False,
                "The back-fill pressed back the roster-EARLIEST suspended player, who is this club's "
                + "best forward — the pre-fix behaviour. A benchable candidate existed.");

            int[] xi = SquadRating.StartingElevenPlayerIds(composed);
            Assert.That(xi, Does.Not.Contain(IdOf(StartingCandidateLocal)),
                "A suspended player is in the starting eleven of a club that can field a legal one "
                + "without him — which is the defect, whatever the reinstatement count.");
            Assert.That(xi, Does.Not.Contain(IdOf(BenchCandidateLocal)),
                "The reinstated suspended player must be on the BENCH. If he starts, the ERR-044-003 "
                + "stage-1 exemption stalls his ban and the extremis appearance is free again.");
        }

        // ── 2. and because he is benched, his ban advances ────────────────────────────────

        [Test]
        public void BenchDepthExtremis_TheReinstatedSuspendedPlayer_StillServesHisBan()
        {
            // The consequence half, driven through a really played round. ERR-044-003 stage 1 exempts
            // anyone in the fielded eleven from FR-DC-011's decrement, so a reinstatee who STARTS never
            // serves — his ban stalls for as long as his club stays depleted. A reinstatee who is
            // BENCHED is not in the eleven, so he serves normally and the suspension costs what the
            // Laws say it costs. That is the whole point of choosing a benchable candidate.
            //
            // The reinstatee is identified from the composed squad rather than hardcoded, so this test
            // asserts about "whoever the back-fill chose" and fails pre-fix for the right reason: there
            // the chosen man is the best forward, he starts, and his ban does not move.
            Squad full = BenchDepthRoster();
            int startingCandidate = IdOf(StartingCandidateLocal);
            int benchCandidate = IdOf(BenchCandidateLocal);
            DisciplineState state = BansOf(3, startingCandidate, benchCandidate);

            League league = LeagueAround(full);
            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            int reinstated = Contains(composed, startingCandidate) ? startingCandidate : -1;
            if (Contains(composed, benchCandidate))
            {
                Assert.That(reinstated, Is.EqualTo(-1),
                    "Precondition: exactly ONE of the two suspended players is pressed back, or the "
                    + "'the reinstatee served' assertion below is ambiguous.");
                reinstated = benchCandidate;
            }

            Assert.That(reinstated, Is.Not.EqualTo(-1),
                "POSITIVE CONTROL: the extremis back-fill did not fire at all, so this test would pass "
                + "vacuously. Seventeen fit players is one short of the eighteen the selection walk "
                + "needs — if that is no longer true, the fixture needs rebuilding, not the assertion.");

            int banBefore = Ban(state, reinstated);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(CraftedClubId),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null,
                careerSquadsOrNull: null,
                progressionOrNull: null,
                disciplineOrNull: state);

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(Ban(state, reinstated), Is.EqualTo(banBefore - 1),
                "The suspended player the back-fill pressed back in did NOT serve a match of his ban. "
                + "He was reinstated for BENCH depth, so he is not in the fielded eleven and the "
                + "ERR-044-003 stage-1 exemption must not reach him — a ban that never advances while "
                + "its holder sits on the bench is a suspension the club serves for free (ERR-030-044).");
        }

        // ── 3. the residual: when no choice avoids the XI, the recorded compromise stands ──

        [Test]
        public void ForcedStartExtremis_TheOnlyGoalkeeper_IsReinstatedIntoTheXi_AndHisBanStalls()
        {
            // The other side of ERR-030-044, locked as INTENDED behaviour rather than left to be
            // rediscovered as a defect. Tier 2's pass 1 asks for a candidate the selector benches;
            // when the club's ONLY goalkeeper is the suspended man, no candidate choice exists — pass
            // 2 presses him back anyway (a club that cannot take the field is what #30 §2.3 F9 refuses
            // to allow), he necessarily starts, and ERR-044-003 stage 1 then exempts him from serving.
            //
            // That stall is the recorded compromise between #30's liveness invariant and the Laws
            // (#44 §7.2, ERR-044-019): it is NOT the bench-depth defect, because here there is nothing
            // else the filter could have done. The fuller answer is the youth / generated-cover ladder
            // recorded in AvailabilityComposition's remarks, both blocked today.
            Squad full = SoleGoalkeeperRoster();
            int keeper = IdOf(SoleGoalkeeperLocal);
            DisciplineState state = BansOf(3, keeper);

            League league = LeagueAround(full);
            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "#30 §3.4's liveness invariant: the composed filter can never leave a club worse off "
                + "than having no filter at all.");
            Assert.That(Contains(composed, keeper), Is.True,
                "The suspended goalkeeper was not reinstated, so this club cannot take the field — the "
                + "F9 terminal refusal instead of the back-fill.");
            Assert.That(SquadRating.StartingElevenPlayerIds(composed), Does.Contain(keeper),
                "Liveness: with no other goalkeeper on the roster the reinstated man MUST be in the "
                + "eleven. If he is not, selection found a keeper somewhere and this fixture no longer "
                + "exercises the forced-start case.");

            int banBefore = Ban(state, keeper);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed),
                league.CreateSeason(CraftedClubId),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null,
                careerSquadsOrNull: null,
                progressionOrNull: null,
                disciplineOrNull: state);

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            Assert.That(Ban(state, keeper), Is.EqualTo(banBefore),
                "A suspended player who was FORCED into the eleven served his ban for the match he "
                + "played in — the free appearance ERR-044-003 stage 1 removed. The stall is the "
                + "recorded compromise (ERR-030-044 / ERR-044-019); the exemption firing here is "
                + "correct and this assertion is what would catch it being narrowed away.");
        }

        // ── 4. multi-reinstatement: the weakest go back first, and it terminates ──────────

        [Test]
        public void MultiReinstatementExtremis_PressesTheWeakestBannedPlayersBack_AndTerminatesDeterministically()
        {
            // Tier 2's pass 3, and the whole reason it is ranked by SELECTOR RATING rather than by
            // roster position (ERR-030-045). With sixteen fit players NO single reinstatement reaches
            // eighteen, so on the first pass neither the "benchable" probe nor the "fieldable at all"
            // probe can succeed for ANY candidate — fieldability is monotone in adding players, so a
            // squad that is two short cannot be made legal by one man. Pass 3 therefore decides that
            // pass outright, and its key is what keeps the club's best banned player out of the pool
            // the rating-greedy selector then draws the eleven from: ascending selector rank, so the
            // WEAKEST banned player is pressed back first and the best is reached only if nothing else
            // can close the gap.
            //
            // This is also the case that would hang or throw if the probe-qualified branch forgot its
            // fallback, and the case where "the answer must not depend on how many times you ask" is a
            // real property rather than a truism.
            Squad full = DoubleShortfallRoster();
            DisciplineState state = BansOf(
                3, IdOf(BestBannedLocal), IdOf(WeakBannedLocal), IdOf(WeakestBannedLocal));

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "The back-fill must terminate at a squad that can actually play (#30 §2.3 F9).");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Sixteen fit players need exactly TWO reinstatements to reach the eighteen the "
                + "selection walk requires — and no more, since each pass presses back exactly one.");

            Assert.That(Contains(composed, IdOf(WeakestBannedLocal)), Is.True,
                "The first reinstatement of a double shortfall must be the WEAKEST banned player by "
                + "the selector's own rating, not the earliest on the roster: no single candidate "
                + "makes the squad fieldable, so pass 1 and pass 2 both come back empty and pass 3 "
                + "decides — and pass 3's key is ascending selector rank (ERR-030-045).");
            Assert.That(Contains(composed, IdOf(BestBannedLocal)), Is.False,
                "The back-fill reached the club's BEST banned player while two weaker banned players "
                + "were still available to close the same gap. This roster is built so the pre-fix "
                + "roster-order key picks exactly him, which is what puts a banned man in the XI.");

            Squad again = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);
            for (int i = 0; i < composed.Count; i++)
            {
                Assert.That(again.GetPlayer(i).PlayerId, Is.EqualTo(composed.GetPlayer(i).PlayerId),
                    $"Roster slot {i} differs between two identical Compose calls. The back-fill draws "
                    + "no RNG and reads nothing outside its arguments; a difference here is state "
                    + "leaking between calls.");
            }
        }

        // ── 5. and with k >= 2 reinstatements, the XI still comes back clean ──────────────

        [Test]
        public void MultiReinstatementExtremis_KeepsEveryBannedPlayerOutOfTheXi_WhenACompletingChoiceExists()
        {
            // ERR-030-045, the finding itself. Under the pre-fix key the k >= 2 case was EXACTLY the
            // pre-ERR-030-044 behaviour: passes 1 and 2 are structurally unreachable on every
            // non-final reinstatement (fieldability is monotone, so nothing is fieldable while the
            // club is still short), pass 3 took the roster-earliest banned player — here the club's
            // best forward — and once he is back in the pool, AnyReinstatedSuspendedStarts sees HIM
            // in every subsequent candidate's eleven, which makes pass 1 unsatisfiable for the final
            // pick too and drops that one to roster order as well. The composed XI then contained a
            // banned man even though the alternative pair {WeakBanned, WeakestBanned} completes the
            // squad and keeps every banned player on the bench.
            //
            // The assertion is about the OUTCOME rather than about which candidate was picked, so it
            // survives any future change to the within-tier key that still achieves it.
            Squad full = DoubleShortfallRoster();
            DisciplineState state = BansOf(
                3, IdOf(BestBannedLocal), IdOf(WeakBannedLocal), IdOf(WeakestBannedLocal));

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Precondition: the back-fill ran to exactly eighteen, so TWO players were pressed "
                + "back and this really is the k >= 2 case.");

            int[] xi = SquadRating.StartingElevenPlayerIds(composed);
            Assert.That(xi, Does.Not.Contain(IdOf(BestBannedLocal)),
                "A banned player is in the starting eleven of a club that could have completed its "
                + "squad without him — {WeakBanned, WeakestBanned} is fieldable and benches both. "
                + "A multi-reinstatement shortfall is not a licence to start a suspended man "
                + "(ERR-030-045).");
            Assert.That(xi, Does.Not.Contain(IdOf(WeakBannedLocal)),
                "A reinstated banned player is starting. Both of the two players the back-fill "
                + "pressed back must sit on the bench here, or the ERR-044-003 stage-1 exemption "
                + "stalls a ban this club never needed to stall.");
            Assert.That(xi, Does.Not.Contain(IdOf(WeakestBannedLocal)),
                "A reinstated banned player is starting. Both of the two players the back-fill "
                + "pressed back must sit on the bench here, or the ERR-044-003 stage-1 exemption "
                + "stalls a ban this club never needed to stall.");
        }

        // ── fixtures ─────────────────────────────────────────────────────────────────────

        /// <summary>Roster slot of the suspended player the selector would START (best forward).</summary>
        private const int StartingCandidateLocal = 3;

        /// <summary>Roster slot of the suspended player the selector would BENCH (worst forward).</summary>
        private const int BenchCandidateLocal = 18;

        /// <summary>Roster slot of the sole goalkeeper in <see cref="SoleGoalkeeperRoster"/>.</summary>
        private const int SoleGoalkeeperLocal = 0;

        /// <summary>
        /// <see cref="DoubleShortfallRoster"/>'s roster-EARLIEST banned player, and the club's best
        /// forward — the pre-ERR-030-045 key's pick, and the one who must not come back.
        /// </summary>
        private const int BestBannedLocal = 16;

        /// <summary>The middle banned player of <see cref="DoubleShortfallRoster"/>.</summary>
        private const int WeakBannedLocal = 17;

        /// <summary>The weakest banned player of <see cref="DoubleShortfallRoster"/> — pass 3's pick.</summary>
        private const int WeakestBannedLocal = 18;

        /// <summary>
        /// Nineteen players: seventeen fit and position-complete (2 GK, 6 DEF, 6 MID, 3 FWD) plus two
        /// forwards whose roster order and rating order DISAGREE — slot 3 is the club's best forward,
        /// slot 18 its worst. Removing both leaves a squad that can field a legal eleven and is one
        /// player short of the eighteen the full selection walk needs.
        /// </summary>
        private static Squad BenchDepthRoster()
        {
            var players = new PlayerRecord[19];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 10);
            players[1]  = Player(1,  PlayerPosition.Defender,   10);
            players[2]  = Player(2,  PlayerPosition.Defender,   10);
            players[3]  = Player(3,  PlayerPosition.Forward,    20);   // suspended, would START
            players[4]  = Player(4,  PlayerPosition.Defender,   10);
            players[5]  = Player(5,  PlayerPosition.Defender,   10);
            players[6]  = Player(6,  PlayerPosition.Midfielder, 10);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 10);
            players[8]  = Player(8,  PlayerPosition.Midfielder, 10);
            players[9]  = Player(9,  PlayerPosition.Midfielder, 10);
            players[10] = Player(10, PlayerPosition.Forward,    12);
            players[11] = Player(11, PlayerPosition.Forward,    11);
            players[12] = Player(12, PlayerPosition.Goalkeeper,  8);
            players[13] = Player(13, PlayerPosition.Defender,    8);
            players[14] = Player(14, PlayerPosition.Defender,    8);
            players[15] = Player(15, PlayerPosition.Midfielder,  8);
            players[16] = Player(16, PlayerPosition.Midfielder,  8);
            players[17] = Player(17, PlayerPosition.Forward,     7);
            players[18] = Player(18, PlayerPosition.Forward,     1);   // suspended, would BENCH
            return new Squad(CraftedClubId, players);
        }

        /// <summary>
        /// Eighteen players whose ONLY goalkeeper is slot 0. Suspend him and no candidate choice can
        /// keep a suspended player out of the eleven — the forced-start residual.
        /// </summary>
        private static Squad SoleGoalkeeperRoster()
        {
            var players = new PlayerRecord[18];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 10);   // suspended, the ONLY keeper
            players[1]  = Player(1,  PlayerPosition.Defender,   12);
            players[2]  = Player(2,  PlayerPosition.Defender,   11);
            players[3]  = Player(3,  PlayerPosition.Defender,   10);
            players[4]  = Player(4,  PlayerPosition.Defender,    9);
            players[5]  = Player(5,  PlayerPosition.Defender,    8);
            players[6]  = Player(6,  PlayerPosition.Defender,    7);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 12);
            players[8]  = Player(8,  PlayerPosition.Midfielder, 11);
            players[9]  = Player(9,  PlayerPosition.Midfielder, 10);
            players[10] = Player(10, PlayerPosition.Midfielder,  9);
            players[11] = Player(11, PlayerPosition.Midfielder,  8);
            players[12] = Player(12, PlayerPosition.Midfielder,  7);
            players[13] = Player(13, PlayerPosition.Forward,    12);
            players[14] = Player(14, PlayerPosition.Forward,    11);
            players[15] = Player(15, PlayerPosition.Forward,    10);
            players[16] = Player(16, PlayerPosition.Forward,     9);
            players[17] = Player(17, PlayerPosition.Forward,     8);
            return new Squad(CraftedClubId, players);
        }

        /// <summary>
        /// Nineteen players of whom sixteen are fit (2 GK, 5 DEF, 5 MID, 4 FWD) — two short of the
        /// selection walk's eighteen, so the back-fill must reinstate twice and its first pass has no
        /// single candidate that reaches fieldability.
        /// <para>
        /// The three banned players' roster order and rating order DISAGREE, which is what makes the
        /// k &gt;= 2 case a real choice (ERR-030-045): slot 16 is the club's best forward by a wide
        /// margin, slots 17 and 18 its two worst players. Reinstating {17, 18} completes the squad and
        /// benches both; reinstating 16 starts him.
        /// </para>
        /// </summary>
        private static Squad DoubleShortfallRoster()
        {
            var players = new PlayerRecord[19];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 10);
            players[1]  = Player(1,  PlayerPosition.Goalkeeper,  9);
            players[2]  = Player(2,  PlayerPosition.Defender,   12);
            players[3]  = Player(3,  PlayerPosition.Defender,   11);
            players[4]  = Player(4,  PlayerPosition.Defender,   10);
            players[5]  = Player(5,  PlayerPosition.Defender,    9);
            players[6]  = Player(6,  PlayerPosition.Defender,    8);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 12);
            players[8]  = Player(8,  PlayerPosition.Midfielder, 11);
            players[9]  = Player(9,  PlayerPosition.Midfielder, 10);
            players[10] = Player(10, PlayerPosition.Midfielder,  9);
            players[11] = Player(11, PlayerPosition.Midfielder,  8);
            players[12] = Player(12, PlayerPosition.Forward,    12);
            players[13] = Player(13, PlayerPosition.Forward,    11);
            players[14] = Player(14, PlayerPosition.Forward,    10);
            players[15] = Player(15, PlayerPosition.Forward,     9);
            players[16] = Player(16, PlayerPosition.Forward,    20);   // suspended, the BEST forward
            players[17] = Player(17, PlayerPosition.Midfielder,  2);   // suspended, weak
            players[18] = Player(18, PlayerPosition.Forward,     1);   // suspended, the weakest
            return new Squad(CraftedClubId, players);
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A player of <paramref name="position"/> whose rating is decided by <paramref name="pace"/>
        /// alone — every other attribute is the mid-range default, so the selector's greedy walk has a
        /// strict, readable preference order (<c>CareerTestRoster.Build</c>'s own technique).
        /// </summary>
        private static PlayerRecord Player(int local, PlayerPosition position, int pace)
        {
            PlayerRecord player = PlayerRecord.CreateDefault(IdOf(local));
            player.Position = position;

            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            attributes.Pace = pace;
            player.Attributes = attributes;

            return player;
        }

        private static int IdOf(int local) =>
            CraftedClubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + local;

        /// <summary>
        /// A four-club league whose club 0 is <paramref name="crafted"/> and whose other three are
        /// ordinary full rosters — enough for a season to schedule and quick-sim around the one club
        /// these tests care about. Built directly rather than through <c>LeagueBootstrap.Generate</c>,
        /// which has no way to install a hand-made squad.
        /// </summary>
        private static League LeagueAround(Squad crafted)
        {
            var clubs = new Club[ClubCount];
            var squads = new Squad[ClubCount];
            for (int c = 0; c < ClubCount; c++)
            {
                clubs[c] = new Club(c, "Club " + c, 0);
                squads[c] = c == CraftedClubId
                    ? crafted
                    : CareerTestRoster.Build(c, PlayerDatabaseConstants.CLUB_SQUAD_SIZE);
            }

            return new League(WorldSeed, SeasonSeed, clubs, squads);
        }

        /// <summary>A tally in which exactly <paramref name="playerIds"/> carry a
        /// <paramref name="matches"/>-match ban in the league competition.</summary>
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-16 | —      | Initial (ERR-030-044, adversarial-review H2): the tier-2         |
// |         |            |        | reinstatement-choice locks. The bench-depth mutant-killer (a     |
// |         |            |        | benchable candidate must be preferred to the roster-earliest     |
// |         |            |        | one, which here would START), the ban-advances-from-the-bench    |
// |         |            |        | consequence over a really played round, the forced-start         |
// |         |            |        | residual locked as the RECORDED compromise rather than left to   |
// |         |            |        | be refound as a defect, and the double-shortfall fallback where  |
// |         |            |        | pass 3 decides. Hand-built rosters throughout: the defect is a   |
// |         |            |        | CHOICE, invisible unless roster order and rating order disagree. |
// | 1.1     | 2026-08-16 | —      | ERR-030-045. v1.0's double-shortfall case asserted                |
// |         |            |        | Contains(composed, banned[0]) — the PRE-FIX ordering key — on a  |
// |         |            |        | roster whose three banned players were identically rated, so     |
// |         |            |        | the choice provably could not matter and the assertion locked    |
// |         |            |        | in the very behaviour ERR-030-044 was filed to remove, for the   |
// |         |            |        | k >= 2 case where it still ran. DoubleShortfallRoster's banned   |
// |         |            |        | trio now disagrees on roster order vs rating (slot 16 the best   |
// |         |            |        | forward, 17 and 18 the two worst), and the case is reshaped to   |
// |         |            |        | assert termination, repeat-call determinism and the NEW key's    |
// |         |            |        | effect — weakest pressed back first, the best banned player      |
// |         |            |        | never reached. A second case adds the k >= 2 lock from the       |
// |         |            |        | finding's own probe: NO banned player in                         |
// |         |            |        | StartingElevenPlayerIds(composed) when an alternative            |
// |         |            |        | completing choice exists. Both observed FAILING at the pre-fix   |
// |         |            |        | tree and green after. The three single-reinstatement cases are   |
// |         |            |        | untouched — they exercise passes 1 and 2, which the finding      |
// |         |            |        | leaves correct.                                                  |
#endregion
