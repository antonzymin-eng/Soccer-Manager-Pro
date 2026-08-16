// File:     src/season-save/tests/AvailabilityCompositionExtremisTests.cs
// Created:  2026-08-16
// Modified: 2026-08-16, round 5 (a reviewed-findings pass over the round-4 landing — CapFallbackExtremis's
//           own comment still carried the exact sentence round 4 proved false ("it self-heals ... the
//           exact search resumes for every later pass"), even though that SAME commit reworded the
//           identical claim at every other site it touched; reworded here to state both halves (the
//           search resumes, the guarantee does not). ALSO: the round-4 sweep's own site count was wrong —
//           it recorded "two sites" fixed in AvailabilityComposition.cs when the same diff touched three
//           (the XML doc at ~152, the XML doc at ~421, and the inline comment inside
//           `ChooseSuspendedCandidate`'s Cap branch at ~528), so the real corrected-site count, this file
//           included, is NINE: #30 `section-3.md` §3.4's Cap bullet and residual (ii), the three
//           `AvailabilityComposition.cs` sites, `SeasonSaveConstants.cs`, #44 `section-2.md`, #44
//           `section-7.md`, and this comment. Annotated in place at each recorded site per the
//           annotate-published-rows convention — v1.4)
// Prior:    2026-08-16, even later still (round-4 reviewed-findings pass over the ERR-030-046 landing —
//           + InCapBoundaryExtremis (m == EXTREMIS_SEARCH_CANDIDATE_CAP exactly, the cap-comparison
//           mutant-killer) and TiedForcedStartExtremis (two candidates tied at the same positive dirty
//           count, the preference-strictness mutant-killer); both observed FAILING against their named
//           mutant and reverted (see the fix's own version-history row in AvailabilityComposition.cs).
//           The eight existing cases are untouched)
//           Prior: 2026-08-16, later still (ERR-030-046 — + the WeakPositionExtremis mutant-killer (a squad
//           thin at ONE position, where a global rating key presses back exactly the banned player it
//           has no room for), the MinimalStallExtremis minimisation lock and the CapFallbackExtremis
//           beyond-cap lock; the five existing cases keep every assertion and had only their retired
//           pass-structure prose updated)
//           Prior: 2026-08-16, later (ERR-030-045 — the multi-reinstatement case stopped asserting the
//           PRE-FIX key on a roster where the choice could not matter, and a k >= 2 lock added)
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 / §2.3 F9 (the depleted-squad back-fill and its
//           within-tier rule — ERR-030-044, ERR-030-045, ERR-030-046); Discipline & Suspensions #44
//           §2.3 / §7.2 / FR-DC-011 (ERR-044-019, the statement of the extremis compromise);
//           ERR-044-003 stage 1 (the fielded-eleven serving exemption); Code Standards #20
// Purpose:  Locks for the tier-2 (suspended) reinstatement CHOICE — that the composed eleven contains
//           the MINIMUM achievable number of reinstated-suspended players (zero whenever any completing
//           choice benches them all), that a benched reinstatee's ban then advances because he did not
//           play, and that the residual is exactly forced starts plus the beyond-cap corner.

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
    /// <b>Why several of these cases need a DOUBLE shortfall</b> (ERR-030-045, ERR-030-046).
    /// Fieldability is monotone in adding players, so while a club is short by more than one NO single
    /// candidate is fieldable: any rule that ranks candidates one at a time decides those passes blind.
    /// That is why the choice is made over SUBSETS, and a single-reinstatement fixture cannot exercise
    /// it at all — which is how the original ERR-030-044 suite came to assert the PRE-FIX key on a
    /// roster where the choice could not matter.
    /// </para>
    /// <para>
    /// <b>And why one of them is thin at a POSITION</b> (ERR-030-046). Selection is per position, so a
    /// squad's ability to absorb a banned player is a positional question and a global rating key
    /// cannot see it. <c>WeakPositionExtremis</c> is the fixture where the globally weakest banned
    /// player is the one the squad has no room for; it is the mutant-killer for the whole search.
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

        // ── 1. earliest-roster position is NOT what decides the choice ───────────────────

        [Test]
        public void BenchDepthExtremis_ReinstatesACandidateTheSelectorBenches_NotTheEarliestOnTheRoster()
        {
            // ERR-030-044(b). Seventeen fit players and two suspended ones: the roster-EARLIER
            // suspended player is the club's best forward (he would walk into the XI), the roster-LATER
            // one its worst (he would bench). One reinstatement takes the club from seventeen to
            // eighteen, so exactly one of the two is pressed back — and which one is the whole
            // question. The original key ("earliest roster position") picks the best forward and puts
            // a banned man in the starting eleven of a club that could field a legal one without him.
            // The search finds its clean completion at SIZE 1 — the bench candidate's own singleton,
            // dirty count 0 — and stops there.
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
                + "One reinstatement closes this gap, so the search finds its clean completion at SIZE "
                + "1 — the bench candidate's own singleton, dirty count 0 — and stops there "
                + "(ERR-030-046).");
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
            // The other side of the extremis compromise, locked as INTENDED behaviour rather than
            // left to be rediscovered as a defect. Here the candidate set has ONE member, so the
            // search's own minimum is his singleton at dirty count 1: the club's only goalkeeper is
            // the suspended man and no completing subset exists that benches him. He is pressed back
            // anyway (a club that cannot take the field is what #30 §2.3 F9 refuses to allow), he
            // necessarily starts, and ERR-044-003 stage 1 then exempts him from serving — residual
            // (i), the probe-verified forced start (ERR-030-046).
            //
            // That stall is the recorded compromise between #30's liveness invariant and the Laws
            // (#44 §7.2, ERR-044-019 as extended at ERR-030-046): it is NOT the bench-depth defect,
            // because the search PROVED there is nothing else the filter could have done. The fuller
            // answer is the youth / generated-cover ladder recorded in AvailabilityComposition's
            // remarks, both blocked today.
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
                + "recorded residual (i) (ERR-030-046 / ERR-044-019); the exemption firing here is "
                + "correct and this assertion is what would catch it being narrowed away.");
        }

        // ── 4. multi-reinstatement: the clean pair is found, and it terminates ───────────

        [Test]
        public void MultiReinstatementExtremis_PressesTheWeakestBannedPlayersBack_AndTerminatesDeterministically()
        {
            // The k >= 2 case, and the one no single-candidate probe can decide. With sixteen fit
            // players NO single reinstatement reaches eighteen — fieldability is monotone in adding
            // players, so a squad that is two short cannot be made legal by one man — which is exactly
            // why the choice is made over SUBSETS rather than over candidates (ERR-030-046). The
            // canonical clean pair here is {17, 18}, the two weakest banned players: it completes the
            // squad and benches both, so the search stops at the first size-2 subset with a dirty
            // count of 0 and commits one of its members per pass.
            //
            // This is also the case that would hang or throw if the search forgot its no-completing-
            // subset fallback, and the case where "the answer must not depend on how many times you
            // ask" is a real property rather than a truism.
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
                "The weakest banned player is not in the composed squad. {17, 18} is the canonical "
                + "clean completing pair — no single candidate makes the squad fieldable, so the "
                + "search's first zero-dirty subset is that pair, and both of its members are "
                + "committed across the two passes (ERR-030-046).");
            Assert.That(Contains(composed, IdOf(BestBannedLocal)), Is.False,
                "The back-fill reached the club's BEST banned player while a clean completing pair "
                + "was available to close the same gap. This roster is built so an ordering key picks "
                + "exactly him, which is what puts a banned man in the XI.");

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
            // ERR-030-045's finding, kept as a standing lock under ERR-030-046's stronger rule. Every
            // single-candidate probe is structurally unreachable on a non-final reinstatement
            // (fieldability is monotone, so nothing is fieldable while the club is still short), so any
            // rule that ranks CANDIDATES decides those passes blind — and a blindly reinstated starter
            // then poisons the probe for every later candidate. The pair {WeakBanned, WeakestBanned}
            // completes the squad and keeps every banned player on the bench, and a search over SUBSETS
            // finds it directly.
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

        // ── 6. the ERR-030-046 mutant-killer: the choice is per POSITION, not by global rank ──

        [Test]
        public void WeakPositionExtremis_KeepsTheGloballyWeakestBannedPlayerOut_WhenACleanPairExists()
        {
            // ERR-030-046, and the case the ascending-global-rank key could not see. The fit squad is
            // THIN AT MIDFIELD (five midfielders rated 5-7) and deep at the back (five defenders rated
            // 10-12), while the banned trio is one midfielder rated 8 — the globally WEAKEST banned
            // player, and still better than every fit midfielder — and two defenders rated 9, each
            // outranked by all five fit defenders.
            //
            // Selection is PER POSITION. A global scalar rank cannot express that, so the blind
            // ascending-rank pass pressed the midfielder back first, he walked into the XI, and the
            // reinstated-suspended count then poisoned the probe for the final pick. The clean pair
            // {D9a, D9b} completes the squad and benches both, and the search must find it.
            Squad full = WeakMidfieldRoster();
            DisciplineState state = BansOf(
                3, IdOf(WeakMidBannedLocal), IdOf(StrongDefBannedALocal), IdOf(StrongDefBannedBLocal));

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "Precondition: the composed filter must never stop a club playing (#30 §2.3 F9).");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Precondition: sixteen fit players need exactly TWO reinstatements to reach the "
                + "eighteen the selection walk requires, so this really is the k >= 2 case.");

            int[] xi = SquadRating.StartingElevenPlayerIds(composed);
            Assert.That(xi, Does.Not.Contain(IdOf(WeakMidBannedLocal)),
                "A banned midfielder is in the starting eleven of a club that could have completed its "
                + "squad without him — {D9a, D9b} is fieldable and benches both. Selection is per "
                + "POSITION, so 'the globally weakest banned player' is not the same question as 'the "
                + "banned player this squad can absorb' (ERR-030-046).");
            Assert.That(xi, Does.Not.Contain(IdOf(StrongDefBannedALocal)),
                "A reinstated banned defender is starting; five fit defenders outrank him and the XI "
                + "needs four, so a clean completion exists and the search must have found it.");
            Assert.That(xi, Does.Not.Contain(IdOf(StrongDefBannedBLocal)),
                "A reinstated banned defender is starting; five fit defenders outrank him and the XI "
                + "needs four, so a clean completion exists and the search must have found it.");

            Assert.That(Contains(composed, IdOf(WeakMidBannedLocal)), Is.False,
                "The back-fill pressed the globally weakest banned player back in even though the pair "
                + "of banned defenders completes the squad cleanly. Committing him is what put a banned "
                + "man in the XI, and it is exactly the ascending-global-rank greedy the exhaustive "
                + "clean-completion search replaces (ERR-030-046).");
        }

        // ── 7. the minimum is a MINIMUM, not zero: one forced start, and only one ban stalls ──

        [Test]
        public void MinimalStallExtremis_StartsExactlyTheForcedPlayer_AndOnlyHisBanStalls()
        {
            // The minimisation half of the ERR-030-046 theorem. No clean set exists at all — the club's
            // ONLY goalkeeper is banned, so every completing subset starts him — but a second banned
            // player (the club's worst forward) is absorbable onto the bench. The search must therefore
            // return the MINIMUM achievable dirty count, which is exactly 1, not "some set that works".
            //
            // The consequence half is driven through a really played round: the forced keeper is in the
            // fielded eleven, so ERR-044-003 stage 1's exemption fires and his ban stalls; the benched
            // forward is not, so his advances. That pairing is what makes "minimum" a football claim
            // rather than an arithmetic one — every avoided start is a ban that actually gets served.
            Squad full = SoleBannedGoalkeeperRoster();
            int keeper = IdOf(ForcedKeeperLocal);
            int benchable = IdOf(BenchableForwardLocal);
            DisciplineState state = BansOf(3, keeper, benchable);

            League league = LeagueAround(full);
            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "#30 §3.4's liveness invariant: the composed filter can never leave a club worse off "
                + "than having no filter at all.");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Precondition: sixteen fit outfielders need BOTH banned players back to reach the "
                + "eighteen the selection walk requires, so both are in the composed squad.");

            int[] xi = SquadRating.StartingElevenPlayerIds(composed);
            int dirty = 0;
            for (int s = 0; s < xi.Length; s++)
            {
                if (xi[s] == keeper || xi[s] == benchable)
                {
                    dirty++;
                }
            }

            Assert.That(dirty, Is.EqualTo(1),
                "The composed eleven must contain EXACTLY the one reinstated-suspended player no "
                + "completing choice can avoid. Zero is unreachable (the only goalkeeper is banned); "
                + "two would mean the search settled for a completing set instead of a MINIMUM one "
                + "(ERR-030-046).");
            Assert.That(xi, Does.Contain(keeper),
                "The forced start must be the goalkeeper — he is the one the formation cannot do "
                + "without. If it is not, this fixture no longer exercises the forced-start case.");
            Assert.That(xi, Does.Not.Contain(benchable),
                "The absorbable banned forward is starting. Four fit forwards outrank him and the XI "
                + "needs two, so benching him costs nothing — and benching him is what lets his ban "
                + "advance.");

            int keeperBanBefore = Ban(state, keeper);
            int benchableBanBefore = Ban(state, benchable);
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

            Assert.That(Ban(state, keeper), Is.EqualTo(keeperBanBefore),
                "The FORCED starter served a match of his ban for a fixture he played in. That is the "
                + "free appearance ERR-044-003 stage 1 removed; the stall is the recorded compromise "
                + "and applies to exactly the players the search could not keep off the pitch.");
            Assert.That(Ban(state, benchable), Is.EqualTo(benchableBanBefore - 1),
                "The BENCHED reinstatee did not serve. He was pressed back for bench depth and never "
                + "took the field, so the stage-1 exemption must not reach him — this is the half of "
                + "the theorem that turns 'minimum starts' into 'maximum bans actually served'.");
        }

        // ── 8. beyond the search cap: the greedy fallback still terminates ────────────────

        [Test]
        public void CapFallbackExtremis_BeyondTheCandidateCap_StillTerminatesAndFieldsASquad()
        {
            // The stated residual (ii). With THIRTEEN concurrent suspended candidates the exhaustive
            // search is refused outright — 2^13 probes is an algorithmic budget, not a football
            // judgement — and the first pass degrades to the ascending-rank greedy with NO minimality
            // claim. The exact search resumes once the candidate count falls back inside the bound, but
            // the guarantee does not: a greedily committed candidate is not revisited, so the
            // composition can remain above the minimum for that fixture.
            //
            // The assertions are deliberately narrow. Termination, fieldability and repeat-call
            // determinism are the only properties the fallback promises, and asserting a clean XI here
            // would be asserting a guarantee this branch explicitly does not make.
            Squad full = ThirteenBannedRoster();
            var bannedIds = new int[BannedCountBeyondCap];
            for (int b = 0; b < BannedCountBeyondCap; b++)
            {
                bannedIds[b] = IdOf(FitCountBeyondCap + b);
            }

            DisciplineState state = BansOf(3, bannedIds);

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "The back-fill must terminate at a squad that can actually play (#30 §2.3 F9), beyond "
                + "the search cap exactly as within it.");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Twelve fit players need exactly six reinstatements to reach eighteen — one per pass, "
                + "the first beyond the cap and the remaining five within it.");

            Squad again = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);
            for (int i = 0; i < composed.Count; i++)
            {
                Assert.That(again.GetPlayer(i).PlayerId, Is.EqualTo(composed.GetPlayer(i).PlayerId),
                    $"Roster slot {i} differs between two identical Compose calls. The cap fallback "
                    + "draws no RNG and reads nothing outside its arguments; a difference here is "
                    + "state leaking between calls.");
            }
        }

        // ── 9. the cap boundary itself: m == CAP must still run the exhaustive search ──────

        [Test]
        public void InCapBoundaryExtremis_AtExactlyTheSearchCap_FindsTheCleanCandidate_NotTheGloballyWeakest()
        {
            // Mutant-killer for the cap comparison (`m > CAP` mutated to `m >= CAP`). Twelve
            // suspended candidates is EXACTLY SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP, so
            // the exhaustive search MUST run on the first pass (12 > 12 is false); a `>=` mutant sends
            // that exact pass to the beyond-cap greedy fallback instead, which blindly commits order[0]
            // — the globally weakest candidate, who STARTS the moment he is reinstated. A completing
            // 5-subset drawn from the eleven decoys always beats any subset containing him (dirty 0 vs
            // dirty 1), so the real search never reinstates him at all; the mutant's blind pass-1
            // commit is never undone, so he ends up a PERMANENT starter for the rest of the back-fill.
            // Passes 2-5 run the real search under EITHER operator (m = 11, 10, 9, 8 are all below the
            // cap either way), so the whole divergence traces to this one pass-1 decision.
            Squad full = InCapBoundaryRoster();
            DisciplineState state = BansOf(3, AllInCapBoundarySuspendedIds());

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "Precondition: the composed filter must never stop a club playing (#30 §2.3 F9).");
            Assert.That(composed.Count, Is.EqualTo(CareerTestRoster.MinimumSquad),
                "Precondition: thirteen fit players need exactly FIVE reinstatements to reach the "
                + "eighteen the selection walk requires, with the first pass landing exactly at "
                + "m == EXTREMIS_SEARCH_CANDIDATE_CAP.");

            Assert.That(Contains(composed, IdOf(InCapWeakLocal)), Is.False,
                "The globally weakest candidate — order[0], and what the beyond-cap greedy fallback "
                + "would blindly commit on pass 1 — was pressed back at all. At m == CAP the exhaustive "
                + "search must run and must have found a completing set drawn from the eleven clean "
                + "decoys instead (ERR-030-046); eleven decoys is more than the five needed.");
        }

        // ── 10. a tie at the same positive dirty count: canonical-first must win ───────────

        [Test]
        public void TiedForcedStartExtremis_CommitsTheCanonicallyFirstCandidate_WhenTwoTieAtTheSameDirtyCount()
        {
            // Mutant-killer for the preference comparison (`dirty < bestDirty` mutated to `<=`). Two
            // suspended goalkeepers are the club's ONLY goalkeepers: either one alone completes the
            // squad and each forces exactly one start (dirty == 1, tied), so the search must prefer
            // the CANONICALLY FIRST of the tied candidates — order[0], the lower-rated keeper — over
            // whichever is found last. A `<=` mutant lets a later-found tie overwrite an earlier one,
            // which here also violates the monotonicity lemma (both singles complete, so the mutated
            // search reaches a |R*| == 2 winner it should never reach) — so under the mutant this
            // fixture either commits the wrong keeper or throws the lemma's fail-loud, and either
            // outcome fails this test.
            Squad full = TiedForcedStartRoster();
            DisciplineState state = BansOf(3, IdOf(TiedForcedGkALocal), IdOf(TiedForcedGkBLocal));

            Squad composed = AvailabilityComposition.Compose(
                full, career: null, state, DisciplineConstants.LeagueCompetitionKey);

            Assert.That(SquadRating.CanFieldStartingEleven(composed), Is.True,
                "#30 §2.3 F9's liveness invariant: the composed filter can never leave a club worse "
                + "off than having no filter at all.");
            Assert.That(Contains(composed, IdOf(TiedForcedGkALocal)), Is.True,
                "The canonically-first (order[0], lower-rated) of the two tied goalkeepers was not "
                + "reinstated (ERR-030-046).");
            Assert.That(Contains(composed, IdOf(TiedForcedGkBLocal)), Is.False,
                "The later-found tied candidate was reinstated instead of the canonically-first one — "
                + "exactly what a `<` weakened to `<=` in the preference comparison would allow.");
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

        /// <summary>The weakest banned player of <see cref="DoubleShortfallRoster"/>, and one half of
        /// its canonical clean completing pair.</summary>
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

        /// <summary>The banned MIDFIELDER of <see cref="WeakMidfieldRoster"/> — rated 8, the globally
        /// weakest banned player, and better than every fit midfielder on the roster.</summary>
        private const int WeakMidBannedLocal = 16;

        /// <summary>The first banned DEFENDER of <see cref="WeakMidfieldRoster"/> — rated 9, outranked
        /// by all five fit defenders, so the selector benches him.</summary>
        private const int StrongDefBannedALocal = 17;

        /// <summary>The second banned DEFENDER of <see cref="WeakMidfieldRoster"/>, rated 9.</summary>
        private const int StrongDefBannedBLocal = 18;

        /// <summary>The sole goalkeeper of <see cref="SoleBannedGoalkeeperRoster"/>, suspended.</summary>
        private const int ForcedKeeperLocal = 16;

        /// <summary>The absorbable banned forward of <see cref="SoleBannedGoalkeeperRoster"/>.</summary>
        private const int BenchableForwardLocal = 17;

        /// <summary>Fit players in <see cref="ThirteenBannedRoster"/> — exactly the eleven the formation
        /// needs plus one spare, so every reinstated player belongs on the bench.</summary>
        private const int FitCountBeyondCap = 12;

        /// <summary>Suspended players in <see cref="ThirteenBannedRoster"/> — one past
        /// <c>SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP</c>.</summary>
        private const int BannedCountBeyondCap = 13;

        /// <summary>
        /// Nineteen players whose fit sixteen are <b>thin at midfield and deep at the back</b> (2 GK,
        /// 5 DEF rated 10-12, 5 MID rated 5-7, 4 FWD rated 10-12), plus a banned trio built so that
        /// GLOBAL rank and POSITIONAL absorbability disagree: the banned midfielder is rated 8 — lowest
        /// of the three — and outranks every fit midfielder, while the two banned defenders are rated 9
        /// and are outranked by all five fit defenders.
        /// <para>
        /// Sixteen fit players is two short of the selection walk's eighteen, so exactly two of the
        /// three come back. {D9a, D9b} completes the squad and benches both; any set containing the
        /// midfielder starts him. That is the ERR-030-046 case: a global scalar key presses the
        /// globally weakest banned player back first and cannot see that the squad has no room for him.
        /// </para>
        /// </summary>
        private static Squad WeakMidfieldRoster()
        {
            var players = new PlayerRecord[19];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 12);
            players[1]  = Player(1,  PlayerPosition.Goalkeeper, 11);
            players[2]  = Player(2,  PlayerPosition.Defender,   12);
            players[3]  = Player(3,  PlayerPosition.Defender,   12);
            players[4]  = Player(4,  PlayerPosition.Defender,   11);
            players[5]  = Player(5,  PlayerPosition.Defender,   11);
            players[6]  = Player(6,  PlayerPosition.Defender,   10);
            players[7]  = Player(7,  PlayerPosition.Midfielder,  7);
            players[8]  = Player(8,  PlayerPosition.Midfielder,  7);
            players[9]  = Player(9,  PlayerPosition.Midfielder,  6);
            players[10] = Player(10, PlayerPosition.Midfielder,  6);
            players[11] = Player(11, PlayerPosition.Midfielder,  5);
            players[12] = Player(12, PlayerPosition.Forward,    12);
            players[13] = Player(13, PlayerPosition.Forward,    11);
            players[14] = Player(14, PlayerPosition.Forward,    10);
            players[15] = Player(15, PlayerPosition.Forward,    10);
            players[16] = Player(16, PlayerPosition.Midfielder,  8);   // suspended, would START
            players[17] = Player(17, PlayerPosition.Defender,    9);   // suspended, would BENCH
            players[18] = Player(18, PlayerPosition.Defender,    9);   // suspended, would BENCH
            return new Squad(CraftedClubId, players);
        }

        /// <summary>
        /// Eighteen players of whom sixteen are fit outfielders (5 DEF, 6 MID, 5 FWD) and two are
        /// suspended: the club's ONLY goalkeeper and its worst forward. Both must come back to reach
        /// eighteen, the keeper necessarily starts, and the forward is absorbed onto the bench — so the
        /// minimum achievable reinstated-suspended count in the XI is exactly one.
        /// </summary>
        private static Squad SoleBannedGoalkeeperRoster()
        {
            var players = new PlayerRecord[18];
            players[0]  = Player(0,  PlayerPosition.Defender,   12);
            players[1]  = Player(1,  PlayerPosition.Defender,   11);
            players[2]  = Player(2,  PlayerPosition.Defender,   10);
            players[3]  = Player(3,  PlayerPosition.Defender,    9);
            players[4]  = Player(4,  PlayerPosition.Defender,    8);
            players[5]  = Player(5,  PlayerPosition.Midfielder, 12);
            players[6]  = Player(6,  PlayerPosition.Midfielder, 11);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 10);
            players[8]  = Player(8,  PlayerPosition.Midfielder,  9);
            players[9]  = Player(9,  PlayerPosition.Midfielder,  8);
            players[10] = Player(10, PlayerPosition.Midfielder,  7);
            players[11] = Player(11, PlayerPosition.Forward,    12);
            players[12] = Player(12, PlayerPosition.Forward,    11);
            players[13] = Player(13, PlayerPosition.Forward,    10);
            players[14] = Player(14, PlayerPosition.Forward,     9);
            players[15] = Player(15, PlayerPosition.Forward,     8);
            players[16] = Player(16, PlayerPosition.Goalkeeper, 10);   // suspended, the ONLY keeper
            players[17] = Player(17, PlayerPosition.Forward,     2);   // suspended, absorbable
            return new Squad(CraftedClubId, players);
        }

        /// <summary>
        /// Twenty-five players — the full <c>CLUB_SQUAD_SIZE</c> — of whom twelve are fit and
        /// <b>thirteen</b> are suspended, one past the exhaustive search's candidate cap. The fit twelve
        /// are exactly the formation's eleven plus one spare and are rated far above every banned
        /// player, so the six reinstatements the club needs all belong on the bench; what the test
        /// asserts of the beyond-cap branch is only that it terminates, fields a squad, and answers the
        /// same way twice.
        /// </summary>
        private static Squad ThirteenBannedRoster()
        {
            var players = new PlayerRecord[FitCountBeyondCap + BannedCountBeyondCap];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 18);
            players[1]  = Player(1,  PlayerPosition.Defender,   18);
            players[2]  = Player(2,  PlayerPosition.Defender,   17);
            players[3]  = Player(3,  PlayerPosition.Defender,   17);
            players[4]  = Player(4,  PlayerPosition.Defender,   16);
            players[5]  = Player(5,  PlayerPosition.Midfielder, 18);
            players[6]  = Player(6,  PlayerPosition.Midfielder, 17);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 17);
            players[8]  = Player(8,  PlayerPosition.Midfielder, 16);
            players[9]  = Player(9,  PlayerPosition.Forward,    18);
            players[10] = Player(10, PlayerPosition.Forward,    17);
            players[11] = Player(11, PlayerPosition.Forward,    16);

            // Thirteen banned players, all far weaker than the fit eleven, spread across the outfield
            // positions so no reinstatement can displace a fit starter.
            PlayerPosition[] cycle =
            {
                PlayerPosition.Defender, PlayerPosition.Midfielder, PlayerPosition.Forward,
            };
            for (int b = 0; b < BannedCountBeyondCap; b++)
            {
                players[FitCountBeyondCap + b] =
                    Player(FitCountBeyondCap + b, cycle[b % cycle.Length], 2 + (b % 5));
            }

            return new Squad(CraftedClubId, players);
        }

        /// <summary>The globally weakest of <see cref="InCapBoundaryRoster"/>'s twelve suspended
        /// candidates (order[0]) — rated below every other, but plays midfield where the fit squad is
        /// THIN, so he STARTS the moment he is reinstated, permanently, for every later pass.</summary>
        private const int InCapWeakLocal = 13;

        /// <summary>Decoy suspended candidates in <see cref="InCapBoundaryRoster"/> — rated above
        /// <see cref="InCapWeakLocal"/> but below the fit squad's deep DEF/FWD, so none of them ever
        /// starts if reinstated. Eleven of them, so <see cref="InCapWeakLocal"/> plus these twelve
        /// candidates land exactly on <c>SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP</c>.</summary>
        private const int InCapDecoyCount = 11;

        /// <summary>
        /// Thirteen fit players (1 GK, 5 DEF, 4 MID, 3 FWD — <c>CLUB_SQUAD_SIZE</c>-bounded, so a
        /// TWELVE-candidate suspended pool needs a THIRTEEN-player fit base, not seventeen: 13 + 12 = 25
        /// is the roster ceiling) plus exactly TWELVE suspended candidates —
        /// <c>SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP</c> itself, the boundary at which the
        /// exhaustive search must still run rather than degrade to the beyond-cap greedy fallback. FIVE
        /// reinstatements reach the selection walk's eighteen (11 starters + 4 fit bench already present
        /// leaves 5 bench slots open) — the multi-reinstatement shape, at the cap boundary rather than
        /// above it. The four fit midfielders are rated 1–4 (THIN); <see cref="InCapWeakLocal"/>, rated
        /// 5, outranks all of them and so STARTS the moment he is reinstated — permanently, since a
        /// reinstatement is never undone — while the eleven decoys (rated 6–16, below the fit DEF/FWD's
        /// 20) never displace a fit starter however many of them come back. The search's first pass
        /// (<c>m == 12</c>) must therefore find a completing 5-subset that excludes
        /// <see cref="InCapWeakLocal"/> (eleven decoys is more than enough); the beyond-cap greedy
        /// fallback a <c>&gt;=</c> mutant would trigger instead blindly commits <c>order[0]</c> —
        /// <see cref="InCapWeakLocal"/> himself — on pass 1, and once committed he is never revisited, so
        /// he ends up a PERMANENT starter for every remaining pass.
        /// </summary>
        private static Squad InCapBoundaryRoster()
        {
            var players = new PlayerRecord[13 + 12];
            players[0]  = Player(0,  PlayerPosition.Goalkeeper, 20);
            players[1]  = Player(1,  PlayerPosition.Defender,   20);
            players[2]  = Player(2,  PlayerPosition.Defender,   20);
            players[3]  = Player(3,  PlayerPosition.Defender,   20);
            players[4]  = Player(4,  PlayerPosition.Defender,   20);
            players[5]  = Player(5,  PlayerPosition.Defender,   20);
            players[6]  = Player(6,  PlayerPosition.Midfielder,  4);   // fit, THIN
            players[7]  = Player(7,  PlayerPosition.Midfielder,  3);   // fit, THIN
            players[8]  = Player(8,  PlayerPosition.Midfielder,  2);   // fit, THIN
            players[9]  = Player(9,  PlayerPosition.Midfielder,  1);   // fit, THIN
            players[10] = Player(10, PlayerPosition.Forward,    20);
            players[11] = Player(11, PlayerPosition.Forward,    20);
            players[12] = Player(12, PlayerPosition.Forward,    20);
            players[13] = Player(13, PlayerPosition.Midfielder,  5);   // suspended, order[0], would START
            PlayerPosition[] cycle = { PlayerPosition.Defender, PlayerPosition.Forward };
            for (int d = 0; d < InCapDecoyCount; d++)
            {
                // Never starts: rated 6..16, below the fit DEF/FWD's 20.
                players[14 + d] = Player(14 + d, cycle[d % cycle.Length], 6 + d);
            }

            return new Squad(CraftedClubId, players);
        }

        private static int[] AllInCapBoundarySuspendedIds()
        {
            var ids = new int[12];
            ids[0] = IdOf(InCapWeakLocal);
            for (int d = 0; d < InCapDecoyCount; d++)
            {
                ids[1 + d] = IdOf(14 + d);
            }

            return ids;
        }

        /// <summary>The lower-rated of <see cref="TiedForcedStartRoster"/>'s two suspended
        /// goalkeepers — order[0], and the one the search must commit.</summary>
        private const int TiedForcedGkALocal = 17;

        /// <summary>The higher-rated of <see cref="TiedForcedStartRoster"/>'s two suspended
        /// goalkeepers — order[1]; committing him instead is the `<=` mutant's signature.</summary>
        private const int TiedForcedGkBLocal = 18;

        /// <summary>
        /// Seventeen fit OUTFIELDERS — no goalkeeper at all — plus two suspended goalkeepers rated 10
        /// and 11. Either alone completes the eighteen the selection walk needs and necessarily starts
        /// (he is the squad's only keeper), so both singletons tie at dirty == 1: the case the strict
        /// <c>&lt;</c> preference comparison exists to resolve deterministically.
        /// </summary>
        private static Squad TiedForcedStartRoster()
        {
            var players = new PlayerRecord[19];
            players[0]  = Player(0,  PlayerPosition.Defender,   12);
            players[1]  = Player(1,  PlayerPosition.Defender,   11);
            players[2]  = Player(2,  PlayerPosition.Defender,   10);
            players[3]  = Player(3,  PlayerPosition.Defender,    9);
            players[4]  = Player(4,  PlayerPosition.Defender,    8);
            players[5]  = Player(5,  PlayerPosition.Defender,    7);
            players[6]  = Player(6,  PlayerPosition.Midfielder, 12);
            players[7]  = Player(7,  PlayerPosition.Midfielder, 11);
            players[8]  = Player(8,  PlayerPosition.Midfielder, 10);
            players[9]  = Player(9,  PlayerPosition.Midfielder,  9);
            players[10] = Player(10, PlayerPosition.Midfielder,  8);
            players[11] = Player(11, PlayerPosition.Midfielder,  7);
            players[12] = Player(12, PlayerPosition.Forward,    12);
            players[13] = Player(13, PlayerPosition.Forward,    11);
            players[14] = Player(14, PlayerPosition.Forward,    10);
            players[15] = Player(15, PlayerPosition.Forward,     9);
            players[16] = Player(16, PlayerPosition.Forward,     8);
            players[17] = Player(17, PlayerPosition.Goalkeeper, 10);   // suspended, order[0]
            players[18] = Player(18, PlayerPosition.Goalkeeper, 11);   // suspended, order[1]
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
// | 1.2     | 2026-08-16, later still | — | ERR-030-046 (escalated High). Three cases added and the retired |
// |         |            |        | pass-structure prose swept out of the five existing ones, whose  |
// |         |            |        | ASSERTIONS are all unchanged — under the search rule test 1's    |
// |         |            |        | clean singleton is found at size 1, test 3's sole goalkeeper is  |
// |         |            |        | the m = 1 dirty minimum, test 4's {17, 18} is the canonical      |
// |         |            |        | clean pair and test 5 is untouched. (a) WeakPositionExtremis,    |
// |         |            |        | THE mutant-killer, written first and observed FAILING against    |
// |         |            |        | the pre-fix tree: a fit sixteen thin at midfield (5 MID rated    |
// |         |            |        | 5-7) and deep at the back (5 DEF rated 10-12), banned trio one   |
// |         |            |        | midfielder rated 8 — globally the WEAKEST banned player, and     |
// |         |            |        | better than every fit midfielder — plus two defenders rated 9    |
// |         |            |        | that five fit defenders outrank. The clean pair {D9a, D9b}       |
// |         |            |        | exists; the ascending-global-rank key committed the midfielder,  |
// |         |            |        | he started, and the observed pre-fix failure was exactly that    |
// |         |            |        | (id 16 in the composed XI). (b) MinimalStallExtremis: no clean   |
// |         |            |        | set exists at all (the sole goalkeeper is banned) but a second   |
// |         |            |        | banned player is absorbable — asserts the XI dirty count is      |
// |         |            |        | EXACTLY 1, that the absorbable one is benched, and through a     |
// |         |            |        | really played AdvanceAndPlayNextRound that the forced keeper's   |
// |         |            |        | ban stalls while the benched man's decrements. That is the       |
// |         |            |        | minimisation half: every avoided start is a ban actually served. |
// |         |            |        | (c) CapFallbackExtremis: thirteen suspended candidates, one past |
// |         |            |        | EXTREMIS_SEARCH_CANDIDATE_CAP — termination, fieldability and    |
// |         |            |        | repeat-call determinism ONLY, since the beyond-cap branch makes  |
// |         |            |        | no minimality claim and asserting one would assert a guarantee   |
// |         |            |        | the code explicitly does not give. The k >= 2 lock (test 5) is   |
// |         |            |        | kept as a standing lock.                                         |
// | 1.3     | 2026-08-16, even later still | — | Round-4 reviewed-findings pass (Fix 4): two new       |
// |         |            |        | mutant-killers for the in-cap search, each verified by actually  |
// |         |            |        | applying its named mutant, running the filter (dotnet test), and |
// |         |            |        | reverting. (a) InCapBoundaryExtremis: THIRTEEN fit players plus  |
// |         |            |        | EXACTLY twelve suspended candidates (m == CAP on pass 1; five    |
// |         |            |        | reinstatements needed — CLUB_SQUAD_SIZE = 25 forbids a           |
// |         |            |        | seventeen-fit/single-reinstatement shape at m = 12, 17+12 = 29). |
// |         |            |        | One candidate (order[0], globally weakest) outranks the fit      |
// |         |            |        | squad's four THIN midfielders and starts the instant he is       |
// |         |            |        | reinstated, permanently; eleven decoy candidates never start.    |
// |         |            |        | The real search always finds a clean 5-subset of decoys on pass  |
// |         |            |        | 1 and never touches order[0] at all. Kills the cap off-by-one    |
// |         |            |        | (`m > CAP` -> `m >= CAP`): mutated, pass 1 (m == 12) is misrouted|
// |         |            |        | to the greedy fallback, which blindly commits order[0]; once     |
// |         |            |        | committed he is never revisited, so he is a PERMANENT starter    |
// |         |            |        | for the rest of the back-fill even though passes 2-5 run the     |
// |         |            |        | real search under either operator (m = 11..8, below the cap      |
// |         |            |        | regardless). (b) TiedForcedStartExtremis: two suspended          |
// |         |            |        | goalkeepers, no fit keeper at all, so both singletons tie at the |
// |         |            |        | SAME positive dirty count (1) — the case the strict `<` in the   |
// |         |            |        | preference comparison exists to resolve. Kills the preference    |
// |         |            |        | mutant (`<` -> `<=`): mutated, the later-found tie overwrites the|
// |         |            |        | earlier one at s == 1 and again at s == 2, reaching a |R*| == 2  |
// |         |            |        | winner whose every member singly completes — which the           |
// |         |            |        | monotonicity lemma (AvailabilityComposition.cs v1.8) proves      |
// |         |            |        | cannot happen, so the mutant is caught either by the wrong       |
// |         |            |        | keeper being committed or by the new fail-loud firing; either    |
// |         |            |        | way the test fails. The third named mutant (the collapsed commit |
// |         |            |        | rule) is moot after v1.8 — the fixed code already behaves        |
// |         |            |        | identically to the collapsed form on every reachable input.      |
// | 1.4     | 2026-08-16, round 5 | — | Round-5 reviewed-findings pass (M1). CapFallbackExtremis's own |
// |         |            |        | comment missed by the round-4 sweep: reworded from "it self-heals|
// |         |            |        | ... the exact search resumes for every later pass" to state both |
// |         |            |        | halves (the search resumes once m falls back inside the bound,   |
// |         |            |        | the guarantee does not, since a greedily committed candidate is  |
// |         |            |        | never revisited). Round 4's own "two sites" count for            |
// |         |            |        | AvailabilityComposition.cs corrected to three (~152, ~421, ~528),|
// |         |            |        | making the real corrected-site total NINE with this one; the     |
// |         |            |        | mismatched counts recorded at #30 section-3.md v2.10's row, #44  |
// |         |            |        | section-2.md v0.18's row, section-7.md v0.11's row, spec-error-  |
// |         |            |        | log.md's ERR-030-046 annotation, and AvailabilityComposition.cs's|
// |         |            |        | own v1.8 header/row are annotated in place, not rewritten.        |
#endregion
