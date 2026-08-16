// File:     src/season-save/AvailabilityComposition.cs
// Created:  2026-08-13
// Modified: 2026-08-16, even later still (round-4 reviewed-findings pass over the ERR-030-046 landing —
//           the monotonicity lemma the commit rule's third branch always relied on is now STATED, not
//           assumed: LineupSelector's per-position top-k selection makes dirty(R) monotone
//           non-decreasing under adding candidates, so a completing singleton is already the global
//           minimum and a |R*| >= 2 winner whose every member singly completes cannot arise. The third
//           commit branch is now a fail-loud InvalidOperationException naming the lemma, its re-choice
//           machinery deleted. A hoisted full-candidate-set probe skips the m·2^m enumeration outright
//           when even the full set cannot field the formation. The candidate gather now requires
//           suspended[i], not removed[i] alone. Both "self-heals" residual remarks corrected: the
//           search resumes beyond the cap; the guarantee does not — v1.8)
//           Prior: 2026-08-16, later still (ERR-030-046, escalated High — tier 2's within-tier choice is no
//           longer a greedy element-wise decision at all. Both prior shapes decided ONE player at a
//           time against a scalar key while the constraint they were serving is set-valued and
//           per-position, so a squad thin in the globally-weakest banned player's position pressed HIM
//           back, started him, and poisoned the probe for every later pass. Now a capped EXHAUSTIVE
//           search over subsets of the still-removed candidates: the composed squad fields the MINIMUM
//           achievable number of reinstated-suspended players — v1.7)
//           Prior: 2026-08-16, later (ERR-030-045, adversarial-review H2-continuation — tier 2's FALLBACK key,
//           which ERR-030-044 left at earliest roster position and which therefore decided every
//           reinstatement of a multi-player shortfall except the last, blindly: fieldability is monotone,
//           so passes 1 and 2 are structurally unreachable while the club is short by more than one, and
//           the blind pick then poisoned the final pass too. Now ascending selector rank — the weakest
//           banned player back first — via the new SquadRating.PlayerRating — v1.6)
//           Prior: 2026-08-16 (ERR-030-044, adversarial-review H2 — tier 2's within-tier ordering key is now
//           probe-qualified: the first suspended candidate, in roster order, the selector would BENCH,
//           with earliest roster position kept only as the multi-reinstatement fallback. The trigger is
//           the full selection walk (eleven + seven bench), so tier 2 fires on bench depth alone, and by
//           roster order it was pressing a club's BEST banned player into the starting XI of a club that
//           could field a legal one without him — where ERR-044-003 stage 1's exemption then stalled his
//           ban indefinitely — v1.5)
//           Prior: 2026-08-15 (L2, reviewed-findings pass — the private Squad-materialization helper
//           renamed Compose → MaterializeSquad; it shared its name with the public Compose that calls it,
//           reading as recursion from inside a method named Compose — v1.4)
//           Prior: 2026-08-15 (ERR-044-003 stage 1 — the L7 remark's open owner call is DECIDED: an extremis
//           appearance no longer serves the ban it was fielded through, fixed at the serving site
//           rather than here; the two further FM-style tiers are recorded with what blocks them — v1.3.
//           Prior: 2026-08-13, #44 C1/C2 adversarial review round 3, M14 — both contributors now own the
//           mask they are handed; Compose allocates one per contributor and does all the OR-ing
//           itself, and the stale MarkSuspended-skips-entries-already-true comment (accurate before
//           round 1's M3, false since) is replaced — v1.2. Prior: v1.1 L7 — the type remarks record
//           that the extremis back-fill fields a suspended player AND that same fixture's serving
//           decrement then discharges his ban regardless; extended in spec-error-log.md's ERR-044-003
//           row rather than a new id.)
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4 (the composed availability seam — ERR-030-016 multiple
//           consumers, ERR-030-029 the depleted-squad rule, §2.3 F9) / FR-SN-013 / ERR-030-009;
//           Injuries & Medical #41 FR-MD-023; Discipline & Suspensions #44 §3.3 / FR-DC-009/010;
//           ERR-044-003 (F5 vs F9 — viability is #30's); Code Standards #20
// Purpose:  The one place #30's resolve→filter→configure seam composes its contributors' removals and
//           applies the depleted-squad back-fill. Contributors remove; this decides who actually plays.

using System;

using TacticalDirector.Discipline;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// #30's composed availability filter (§3.4). Gathers every contributor's <b>removals</b> over the
    /// unfiltered squad, intersects them once, then runs the single depleted-squad back-fill.
    /// <para>
    /// <b>Why the composition is its own type.</b> #30 §3.4 records that the contributors "compose
    /// order-independently BECAUSE both are removals — set intersection commutes — and that is stated
    /// as a property to preserve rather than an accident to rely on". Before #44, the removal and the
    /// back-fill lived together inside <see cref="PlayerCareerStates.SelectAvailable"/>, which made the
    /// property impossible to preserve: bolting a second filter after that method runs the back-fill
    /// BEFORE the second contributor's removals, and running it first lets the back-fill press a
    /// suspended player onto the pitch without ever knowing he is suspended. The fix is a split, not an
    /// ordering — every contributor removes into one mask, then one back-fill probes once.
    /// </para>
    /// <para>
    /// <b>#44 does not adjudicate viability</b> (ERR-044-003): #44 §2.3 F5 requires its filter to fail
    /// loud below eighteen players, and #30 §2.3 F9 — approved later, and explicit that "the rule is
    /// #30's because FR-MD-023 puts selection on this side of the seam; #44/#36 contribute removals only
    /// and inherit the rule unchanged when they join" — settles the same event by back-filling instead.
    /// #30 wins. That is also what keeps a mass-suspension season from wedging permanently mid-save.
    /// </para>
    /// <para>
    /// <b>The back-fill's tier order, and the one football compromise in it.</b> #30's rule is to press
    /// the least-injured back in until the club can field WHAT SELECTION REQUIRES, and that "in the
    /// limit the back-fill is the whole squad — the unfiltered behaviour — so the composed filter can
    /// never leave a club worse off than having no filter at all." Preserving that invariant literally
    /// means a suspended player is reinstatable in extremis, which the Laws do not allow. So suspension
    /// is a STRICTER TIER: every injured player is pressed back before any suspended one. That keeps
    /// #30's stated invariant true and never wedges a season; it is recorded under ERR-044-003 as the
    /// one place #44's football and #30's liveness disagree, and the fuller answer the owner chose is the
    /// staged tier ladder below — youth call-ups, then generated low-attribute cover, both ahead of any
    /// suspended player. #44 §7.2's deferral queue was explicitly NOT chosen.
    /// </para>
    /// <para>
    /// <b>The trigger is the whole selection walk, not the eleven — so tier 2 fires on BENCH depth</b>
    /// (ERR-030-044). The probe both #30 §3.4 and #44 §2.3 name is
    /// <see cref="SquadRating.CanFieldStartingEleven"/>, and that is <c>LineupSelector</c>'s full walk:
    /// eleven position-matched starters PLUS the seven-slot bench. A club with seventeen fit,
    /// position-complete players can field a perfectly legal XI and still reach this tier, needing an
    /// eighteenth body for the bench. Both specs' prose said "cannot field the eleven"; the mechanism
    /// they both name has always said eighteen, and the gap between the two hid a real defect rather
    /// than being a wording nicety.
    /// </para>
    /// <para>
    /// <b>So tier 2 is a CHOICE — and it is a choice of a SET, which is why two ordering keys failed at
    /// it</b> (ERR-030-046, superseding ERR-030-044 and ERR-030-045). Reinstating by earliest roster
    /// position put whichever suspended player happened to sit first on the roster into the pool the
    /// rating-greedy selector then draws the STARTING eleven from, and ERR-044-003 stage 1's exemption
    /// (he played, so the fixture is not one of his ban) then stalled that ban for as long as the club
    /// stayed depleted. Ranking instead by ascending <see cref="SquadRating.PlayerRating"/> fixed the
    /// direction and not the shape: <b>the rating is a global scalar and <c>LineupSelector</c> selects
    /// per POSITION</b>, so a club thin in the globally-weakest banned player's position pressed exactly
    /// him back — into a slot no fit player could contest — he started, and the reinstated-suspended
    /// count then made the probe unsatisfiable for every later pass, which dropped those to roster order
    /// in their turn. Both keys were <b>element-wise greedy decisions of a set-valued constraint</b>: a
    /// squad is completed by a SET of reinstatements, whether that set starts a banned man is a property
    /// of the set, and no per-player scalar can express it. The third shape stops guessing.
    /// </para>
    /// <para>
    /// <b>The rule is a capped exhaustive clean-completion search</b>
    /// (<see cref="ChooseSuspendedCandidate"/>). Over the still-removed candidates — ordered ascending by
    /// <see cref="SquadRating.PlayerRating"/>, ties on earliest roster position, a total order so the
    /// enumeration is canonical — every subset is probed, sizes ascending: does the club's squad plus
    /// that subset satisfy selection, and if so how many reinstated-suspended players does the resulting
    /// XI contain. The subset preferred is the one with the fewest such starters, then the smallest, then
    /// the first canonically. Above
    /// <see cref="SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP"/> candidates the search is refused
    /// outright and the pass falls back to the weakest candidate — an algorithmic budget, stated as
    /// residual (ii) below.
    /// </para>
    /// <para>
    /// <b>What that buys — the theorem, stated as a theorem because two weaker claims were falsified in
    /// two days.</b> With the candidate count within the cap, the composed squad fields <b>the minimum
    /// achievable number of reinstated-suspended players in its starting eleven</b>, minimised over every
    /// completing subset of the still-removed candidates — <b>zero whenever any completing choice benches
    /// them all</b>. A reinstated-suspended player therefore starts only in a <i>probe-verified</i> forced
    /// start: every completing choice within the search bound starts at least as many. The induction step
    /// is that committing one member <c>c</c> of the chosen subset <c>R*</c> cannot raise the optimum for
    /// the next pass, because <c>R* \ {c}</c> is still available and the squad
    /// <c>F ∪ {c} ∪ (R* \ {c})</c> is byte-identical to the <c>F ∪ R*</c> already probed — the count sees
    /// <c>c</c> through <c>suspended[c] &amp;&amp; !removed[c]</c> exactly as it saw him through
    /// <c>c ∈ R</c>. Termination is unchanged: <c>availableCount</c> strictly increases per pass and §2.3
    /// F9 fires at the bound. There is no repair loop and nothing is un-committed.
    /// </para>
    /// <para>
    /// <b>The residual, stated exhaustively.</b> <b>(i) Forced starts</b> — the minimum is positive: the
    /// club's sole goalkeeper being banned is the smallest case, and a <c>k ≥ 2</c> shortfall in which
    /// every completing subset starts someone is its generalisation. There the ERR-044-003 stage-1
    /// exemption stalls exactly the bans that were fielded, and no fewer. <b>(ii) The beyond-cap
    /// corner</b> — more than <see cref="SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP"/> concurrent
    /// suspended candidates, which no measured card rate reaches; that pass degrades to ascending-rank
    /// greedy and makes <b>no</b> minimality claim. The exact search resumes once the candidate count
    /// falls back inside the bound, but the guarantee does not: a greedily committed candidate is not
    /// revisited, so the composition can remain above the minimum for that fixture. Those two are the
    /// whole residual; ERR-030-044's two-case and ERR-030-045's three-case statements are
    /// <b>superseded</b>, not amended. What deletes residual (i) is the two missing tiers below, not any
    /// choice rule here.
    /// </para>
    /// <para>
    /// <b>An extremis appearance no longer discharges the ban it was fielded through</b> (ERR-044-003
    /// stage 1, owner decision, August 15, 2026 — this paragraph previously recorded it as an open
    /// call). A player <see cref="Reinstate"/> presses back in via the suspended tier is, by
    /// construction, still carrying <c>BanMatchesRemaining &gt; 0</c> at kickoff: <see cref="Compose"/>
    /// only removes him from the filtered result, and never touches <see cref="DisciplineState"/>.
    /// <c>SeasonLoop</c>'s serving call used to decrement that same fixture's ban anyway — the exact
    /// "decrement a ban the banned player had just played through" hazard ERR-044-002's fix exists to
    /// prevent, reintroduced by this type's own compromise one tier over, and it made the appearance
    /// strictly free. <c>DisciplineRules.OnClubFixturePlayed</c> now takes the fielded eleven and
    /// exempts anyone in it, so the ban still costs a full fixture. The reinstatement tiers below are
    /// unchanged — the fix is at the serving site, because the football rule is about who PLAYED, not
    /// about who was selectable.
    /// </para>
    /// <para>
    /// <b>The two tiers still missing, in order</b> (owner-agreed staging, August 15, 2026): youth
    /// call-ups ahead of any suspended player, then generated low-attribute cover ahead of that — the
    /// Football Manager posture, under which a banned man never takes the field at all and this type's
    /// suspended tier becomes unreachable rather than merely expensive. Neither is buildable yet:
    /// <b>#42 Youth has no <c>src/</c> assembly</b>, and generated cover needs the <c>PlayerId =
    /// clubId × CLUB_SQUAD_SIZE + local</c> id space widened, since it is fully packed at 25 and a
    /// 26th player for club N collides with club N+1's first (#27 FR-SQ-010 as amended by
    /// ERR-027-004). Until then the suspended tier stays, and stays costly.
    /// </para>
    /// </summary>
    internal static class AvailabilityComposition
    {
        /// <summary>
        /// The squad #30 will actually field: <paramref name="squad"/> minus every contributor's
        /// removals, plus whoever the depleted-squad rule has to press back in — which is nobody unless
        /// the removals would otherwise stop the club playing.
        /// <para>
        /// <b>Returns the same instance when nothing is removed</b>, so a fixture with no injuries and
        /// no suspensions — the overwhelming majority — resolves through a reference-identical squad and
        /// is byte-identical to the unfiltered path. That is what makes FR-DC-018's no-trigger identity
        /// testable.
        /// </para>
        /// </summary>
        /// <param name="squad">The resolved, unfiltered roster.</param>
        /// <param name="career">#41's contributor, or null when no career is wired.</param>
        /// <param name="discipline">#44's contributor, or null when discipline is not wired.</param>
        /// <param name="competitionId">The competition partition #44 accrues in (FR-DC-012).</param>
        /// <exception cref="ArgumentNullException"><paramref name="squad"/> is null.</exception>
        /// <exception cref="ArgumentException">The squad's club or one of its players is not carried by the career.</exception>
        /// <exception cref="InvalidOperationException">Even the whole squad cannot field the formation
        /// (§2.3 <b>F9</b>) — a roster problem no filter can repair; the same roster would be refused
        /// identically with nobody unavailable at all.</exception>
        internal static Squad Compose(
            Squad squad, PlayerCareerStates career, DisciplineState discipline, int competitionId)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }
            if (career == null && discipline == null)
            {
                return squad;
            }

            int total = squad.Count;
            var removed = new bool[total];

            // Suspension is tracked separately from removal, not because the removal differs — a
            // removal is a removal — but because the BACK-FILL must be able to tell the tiers apart.
            var suspended = new bool[total];

            // Meaningful only where removed && !suspended: #41's ordering key for the back-fill.
            var recoveryRemaining = new int[total];

            // M14: both contributors OWN the mask they are handed — every entry is (re)written
            // unconditionally (Availability.MarkSuspended and, since M14, PlayerCareerStates.
            // MarkUnavailable). A shared mask would let one contributor's write silently CLEAR a
            // removal the OTHER had already made, so each gets its OWN freshly allocated mask here,
            // and this method does all the OR-ing and counting — one contract, stated once, for both
            // contributors, rather than a per-contributor hazard documented around.
            int removedCount = 0;

            if (career != null)
            {
                var injuredMask = new bool[total];
                career.MarkUnavailable(squad, injuredMask, recoveryRemaining);
                for (int i = 0; i < total; i++)
                {
                    if (!injuredMask[i])
                    {
                        continue;
                    }
                    if (!removed[i])
                    {
                        removed[i] = true;
                        removedCount++;
                    }
                }
            }

            if (discipline != null)
            {
                var suspendedMask = new bool[total];
                Availability.MarkSuspended(squad, discipline, competitionId, suspendedMask);
                for (int i = 0; i < total; i++)
                {
                    if (!suspendedMask[i])
                    {
                        continue;
                    }
                    suspended[i] = true;
                    if (!removed[i])
                    {
                        removed[i] = true;
                        removedCount++;
                    }
                }
            }

            if (removedCount == 0)
            {
                return squad;
            }

            int availableCount = total - removedCount;
            Squad filtered = MaterializeSquad(squad, removed, availableCount);

            // Bounded by the roster: each pass reinstates exactly one more player, so the loop ends at
            // the latest when everybody is selected — at which point the verdict is the roster's own.
            while (filtered == null || !SquadRating.CanFieldStartingEleven(filtered))
            {
                if (availableCount == total)
                {
                    throw new InvalidOperationException(
                        $"Club {squad.ClubId} cannot field the Stage-0 formation even with all "
                        + $"{total} of its players selected. That is a roster problem — too few "
                        + "players, or none of a position the formation requires — and the "
                        + "availability filter cannot repair it (#30 §2.3 F9).");
                }

                Reinstate(squad, removed, suspended, recoveryRemaining, availableCount);
                availableCount++;
                filtered = MaterializeSquad(squad, removed, availableCount);
            }

            return availableCount == total ? squad : filtered;
        }

        /// <summary>
        /// The squad of the currently-selectable players, or <c>null</c> when none are — which
        /// <see cref="Squad"/> itself refuses to represent, and which the back-fill loop then resolves
        /// by selecting someone.
        /// <para>
        /// L2: named <c>MaterializeSquad</c>, not <c>Compose</c> — the public <see cref="Compose"/>
        /// above called this private helper by the SAME name (an overload on two unrelated meanings,
        /// "compose availability" vs "materialise a Squad from a mask"), so the back-fill loop's
        /// <c>filtered = Compose(squad, removed, availableCount);</c> read as recursion from inside a
        /// method named <c>Compose</c>.
        /// </para>
        /// </summary>
        private static Squad MaterializeSquad(Squad squad, bool[] removed, int availableCount)
        {
            if (availableCount == 0)
            {
                return null;
            }

            var selected = new PlayerRecord[availableCount];
            int w = 0;
            for (int i = 0; i < removed.Length; i++)
            {
                if (!removed[i])
                {
                    selected[w++] = squad.GetPlayer(i);
                }
            }

            return new Squad(squad.ClubId, selected);
        }

        /// <summary>
        /// Presses exactly one removed player back into selection: the least-injured of the merely
        /// injured (ascending <c>RecoveryRemaining</c>, ties on earliest roster position), and only
        /// once none of those remain, a suspended one — chosen by <see cref="ChooseSuspendedCandidate"/>,
        /// which searches over SETS of candidates rather than ranking them (ERR-030-046).
        /// <para>
        /// The tier split is the whole football content of this method — see the type remarks. Called
        /// only when at least one player is still removed, which the loop's own guard establishes.
        /// </para>
        /// </summary>
        /// <param name="squad">The unfiltered roster the masks index into. Tier 1 never reads it; tier 2
        /// needs it to materialise each candidate squad for the probe.</param>
        /// <param name="removed">The composed removal mask, mutated: exactly one entry goes false.</param>
        /// <param name="suspended">Which removals came from #44. Never cleared, so it still identifies a
        /// reinstated-suspended player after his <paramref name="removed"/> entry has gone false.</param>
        /// <param name="recoveryRemaining">#41's tier-1 ordering key.</param>
        /// <param name="availableCount">How many players are currently selectable — the count
        /// <paramref name="removed"/> encodes, so a candidate squad has one more.</param>
        private static void Reinstate(
            Squad squad, bool[] removed, bool[] suspended, int[] recoveryRemaining, int availableCount)
        {
            int best = -1;
            int bestRecovery = int.MaxValue;

            for (int i = 0; i < removed.Length; i++)
            {
                if (!removed[i] || suspended[i])
                {
                    continue;
                }
                if (recoveryRemaining[i] < bestRecovery)
                {
                    bestRecovery = recoveryRemaining[i];
                    best = i;
                }
            }

            if (best < 0)
            {
                // Nobody is merely injured; the only players left to reinstate are suspended. This is
                // the extremis branch #30's "never worse off than no filter" invariant requires and the
                // Laws would rather refuse — ERR-044-003.
                best = ChooseSuspendedCandidate(squad, removed, suspended, availableCount);
            }

            if (best < 0)
            {
                throw new InvalidOperationException(
                    "AvailabilityComposition.Reinstate: nobody is removed, so there is nobody to press "
                    + "back in. The back-fill loop must not reach here — its own availableCount == total "
                    + "guard fires first — so this means the mask and the count have diverged.");
            }

            removed[best] = false;
        }

        /// <summary>
        /// Tier 2's within-tier choice (ERR-030-046, superseding ERR-030-044 and ERR-030-045): WHICH
        /// suspended player is pressed back in, when the extremis branch has to press one. Returns his
        /// roster index, or <c>-1</c> when nobody is removed at all — which <see cref="Reinstate"/>'s own
        /// guard turns into the divergence throw. Pure: the mask it is handed is left exactly as it was
        /// found, and the caller commits the single returned index.
        /// <para>
        /// Every candidate here is suspended: tier 1 above takes anyone who is removed and NOT suspended,
        /// so reaching this method means it found none.
        /// </para>
        /// <para>
        /// <b>Why this is a search over SETS and not an ordering</b> — the whole content of ERR-030-046,
        /// and the reason two previous ordering keys were landed and defeated. A club is completed by a
        /// <b>set</b> of reinstatements; whether that set puts a banned man in the eleven is a property
        /// of the set; and <c>LineupSelector</c> decides it <b>per position</b>. Neither earliest roster
        /// position (ERR-030-044's fallback) nor ascending selector rating (ERR-030-045's) can express a
        /// per-position, set-valued constraint — the second failed on a squad thin in the globally
        /// <i>weakest</i> banned player's position, where the "safe" key presses back precisely the one
        /// man no fit player can displace. Ranking candidates at all was the defect class; the fix is to
        /// stop ranking and start enumerating.
        /// </para>
        /// <para>
        /// <b>Setup.</b> The still-removed indices, ordered ascending by
        /// <see cref="SquadRating.PlayerRating"/> with ties on earliest roster position — a <b>total
        /// order</b>, so the enumeration below is canonical and the answer cannot depend on how many
        /// times it is asked. The ordering no longer decides anything on its own; it only fixes what
        /// "first" means.
        /// </para>
        /// <para>
        /// <b>Cap.</b> With more than <see cref="SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP"/>
        /// candidates the search is refused and the weakest candidate returned, with <b>no cleanliness
        /// claim</b> — ERR-030-046 residual (ii). The exact search resumes once the candidate count
        /// falls back inside the bound, but the guarantee does not: a greedily committed candidate is
        /// not revisited, so the composition can remain above the minimum for that fixture.
        /// </para>
        /// <para>
        /// <b>Full-set probe first (Low, ERR-030-046 follow-up).</b> Before enumerating anything, one
        /// probe checks whether <c>F</c> plus <b>every</b> remaining candidate can field the formation at
        /// all. Fieldability is monotone in ADDING candidates — the same premise the lemma below rests
        /// on — so if the full set cannot complete the squad, no smaller subset can either; the whole
        /// <c>m·2^m</c> enumeration would only rediscover that. Skipping straight to the weakest
        /// candidate here costs one extra <see cref="SquadRating.CanFieldStartingEleven"/> walk on every
        /// call and saves the full search on the one call whose answer is "no subset completes" — the
        /// §2.3 F9 roster-integrity case the outer loop's own <c>availableCount == total</c> guard
        /// already exists to report.
        /// </para>
        /// <para>
        /// <b>Search.</b> Enumerate subsets <c>R</c> of the ordered candidates, sizes ascending, and
        /// lexicographically within a size. Materialise the filtered squad plus <c>R</c> (the tentative
        /// mask flip generalised from one index to a subset, restored before anything else runs) and
        /// probe <see cref="SquadRating.CanFieldStartingEleven"/>; a subset that does not complete the
        /// squad is skipped. For one that does, count how many of the resulting starting eleven are
        /// reinstated-suspended players (<see cref="CountReinstatedSuspendedInXi"/>). The preferred
        /// completing subset is (1) the one with the smallest count, then (2) the smallest, then (3) the
        /// first canonically — which a single strict <c>&lt;</c> gives, because sizes ascend and each
        /// size is enumerated lexicographically. The first subset with a count of <b>zero</b> is
        /// globally optimal and stops the search, since every smaller size has already been exhausted.
        /// </para>
        /// <para>
        /// <b>The monotonicity lemma — why the third commit branch is unreachable (Medium, closed this
        /// pass).</b> <c>LineupSelector</c> selects its starting eleven <b>per position class</b>: each
        /// slot draws from the candidates eligible for that class and keeps its own top-<c>k</c>. Adding
        /// a candidate to any squad can therefore only ever DISPLACE a same-class player from that
        /// class's pool — it can never change which OTHER classes are already satisfied. So
        /// <c>dirty(R)</c>, the count of reinstated-suspended starters, is monotone
        /// <b>non-decreasing</b> in <c>R</c> under adding candidates: if some candidate <c>c</c>'s own
        /// singleton <c>{c}</c> completes the squad, <c>dirty({c})</c> is already the GLOBAL minimum over
        /// every completing superset containing <c>c</c> — no larger set built on top of <c>{c}</c> can
        /// do better. The search's own <c>s == 1</c> pass evaluates every singleton before any larger
        /// size, so a completing singleton is found — and, being a minimum, committed at size 1 — before
        /// the search ever reaches a <c>|R*| ≥ 2</c> winner. <b>A completing singleton therefore cannot
        /// coexist with a strictly better <c>|R*| ≥ 2</c> winner</b> while this premise holds.
        /// <b>The premise is <c>LineupSelector</c>'s per-position top-k selection specifically</b> —
        /// verified over 6,858 generated oracle cases (<c>thirdBranchReachable = 0</c>; collapsing the
        /// whole commit rule to <c>return order[bestSubset[0]]</c> is behaviour-identical over 3,966
        /// further cases, 11/11 tests). A Stage-1 role-weighted or cross-class selection rule would
        /// invalidate the lemma and reopen the branch — which is why the implementation below fails loud
        /// there rather than carrying dead re-choice code for a branch nothing can reach today.
        /// </para>
        /// <para>
        /// <b>Commit rule</b> — the search chooses a set, this method must return one member of it.
        /// <c>|R*| == 1</c> ⇒ that member. <c>|R*| ≥ 2</c> ⇒ the monotonicity lemma above guarantees every
        /// member's own singleton was probed and did <b>not</b> complete the squad — a completing
        /// singleton would already have been the global minimum and won at size 1 — so this method
        /// returns the canonically first such member: the outer loop cannot exit on this pass, so the
        /// search simply reruns on the next one with the state <c>R* \ {c}</c> was probed against.
        /// Should the lemma ever be false — a selection rule that is no longer per-position top-k — every
        /// member of <c>R*</c> completing on its own is detected and refused loudly instead of silently
        /// re-choosing among them: the re-choice machinery that used to live here assumed the very
        /// theorem this state contradicts, and guessing at a replacement key is exactly the class of
        /// defect ERR-030-044 and ERR-030-045 were.
        /// </para>
        /// <para>
        /// <b>Guarantee (ERR-030-046).</b> Within the cap the composed squad fields the <b>minimum
        /// achievable</b> number of reinstated-suspended players in its starting eleven, minimised over
        /// every completing subset of the still-removed candidates — zero whenever any completing choice
        /// benches them all — so a reinstated-suspended player starts only in a probe-verified forced
        /// start. The induction step is that committing a non-completing member <c>c</c> of <c>R*</c>
        /// cannot raise the optimum: <c>R* \ {c}</c> remains available next pass, and
        /// <c>F ∪ {c} ∪ (R* \ {c})</c> is byte-identical to the <c>F ∪ R*</c> just probed, because the
        /// count reaches <c>c</c> through <c>suspended[c] &amp;&amp; !removed[c]</c> exactly as it
        /// reached him through <c>c ∈ R</c>. The residual is stated exhaustively in the type remarks:
        /// forced starts, and the beyond-cap corner. Nothing else.
        /// </para>
        /// <para>
        /// <b>Cost.</b> Worst case <c>m·2^(m+1)</c> <c>TrySelect</c> walks — every subset probed for
        /// fieldability, a completing one probed again for its eleven, over at most <c>m</c> passes:
        /// <b>98,304</b> at the cap. The reachable case is far smaller — <c>m ≤ 4</c> gives
        /// <c>≤ 128</c> walks, single-digit milliseconds — and this method is <b>extremis-only</b>: it
        /// needs a club with no injured player left to press back and at least one suspended one, so on
        /// every ordinary fixture it is never called at all. Season cadence, never the 10 Hz or 60 Hz
        /// loops; <c>SeasonLoop.ResolveFixture</c> already re-rates every club per matchday on the same
        /// argument.
        /// </para>
        /// <para>
        /// <b>Allocation, not just walk count (Low, recorded not fixed).</b> Each <c>TrySelect</c> walk
        /// allocates a fresh transient buffer per rating read, so the walk count above understates the
        /// real cost. Measured: <c>m = 12</c> (the cap) is <b>~74.5 ms and ~157 MiB transient</b> (an
        /// <c>int[31]</c> per rating read across roughly 8,000 probes), against <b>~0.22 ms / ~460 KiB</b>
        /// at <c>m = 4</c>. Season cadence, plus the cap's own unreachability at any measured card rate,
        /// keep both acceptable; <see cref="Compose"/>'s reference-identity fast path (FR-DC-018 — the
        /// overwhelming majority of fixtures) is unaffected, since this method is never called on that
        /// path at all.
        /// </para>
        /// </summary>
        private static int ChooseSuspendedCandidate(
            Squad squad, bool[] removed, bool[] suspended, int availableCount)
        {
            int[] order = CandidatesByAscendingRank(squad, removed, suspended);
            int m = order.Length;
            if (m == 0)
            {
                return -1;
            }

            if (m > SeasonSaveConstants.EXTREMIS_SEARCH_CANDIDATE_CAP)
            {
                // Residual (ii): the probe budget, not a football judgement. No minimality is claimed
                // for this pass; the exact search resumes once the candidate count falls back inside
                // the bound, but the guarantee does not — a greedily committed candidate is not
                // revisited, so the composition can remain above the minimum for that fixture.
                return order[0];
            }

            var subset = new int[m];            // positions into `order` — the current combination
            for (int k = 0; k < m; k++)
            {
                subset[k] = k;
            }
            if (ProbeSubset(squad, removed, suspended, order, subset, m, availableCount) < 0)
            {
                // Fieldability is monotone in ADDING candidates (the same premise the commit-rule
                // lemma below rests on): if even every remaining candidate together cannot field the
                // formation, no smaller subset can either, and the m·2^m enumeration below would only
                // rediscover that at full cost. Return the weakest candidate directly; the outer loop's
                // own `availableCount == total` guard is what actually reports the roster problem
                // (§2.3 F9), not this method.
                return order[0];
            }

            var bestSubset = new int[m];
            var singletonCompletes = new bool[m];

            int bestDirty = int.MaxValue;
            int bestSize = 0;

            for (int s = 1; s <= m && bestDirty != 0; s++)
            {
                for (int k = 0; k < s; k++)
                {
                    subset[k] = k;
                }

                while (true)
                {
                    int dirty = ProbeSubset(
                        squad, removed, suspended, order, subset, s, availableCount);
                    if (dirty >= 0)
                    {
                        if (s == 1)
                        {
                            // Recorded for the commit rule below, which has to know whether a member of
                            // a chosen multi-subset completes the squad on its own (case 2's
                            // non-completing check). The dirty count itself is no longer kept per
                            // singleton — the monotonicity lemma below proves it is never needed.
                            singletonCompletes[subset[0]] = true;
                        }

                        if (dirty < bestDirty)
                        {
                            // Sizes ascend and each size is enumerated lexicographically, so a STRICT <
                            // implements all three preference levels at once: fewest reinstated-suspended
                            // starters, then smallest subset, then first canonically.
                            bestDirty = dirty;
                            bestSize = s;
                            Array.Copy(subset, bestSubset, s);
                            if (dirty == 0)
                            {
                                break;
                            }
                        }
                    }

                    int p = s - 1;
                    while (p >= 0 && subset[p] == m - s + p)
                    {
                        p--;
                    }
                    if (p < 0)
                    {
                        break;
                    }
                    subset[p]++;
                    for (int q = p + 1; q < s; q++)
                    {
                        subset[q] = subset[q - 1] + 1;
                    }
                }
            }

            if (bestSize == 0)
            {
                // Defensively unreachable now that the full-set probe above runs first: it already
                // established the full candidate set completes the squad, so the s == m pass of the
                // loop above necessarily assigns bestSize. Kept as a fail-safe, not a live path.
                return order[0];
            }

            if (bestSize == 1)
            {
                return order[bestSubset[0]];
            }

            for (int k = 0; k < bestSize; k++)
            {
                if (!singletonCompletes[bestSubset[k]])
                {
                    // He does not complete the squad alone, so the outer loop cannot exit on this pass
                    // and the search reruns with the rest of R* still available (the induction step).
                    return order[bestSubset[k]];
                }
            }

            // UNREACHABLE under LineupSelector's per-position top-k selection (the monotonicity lemma
            // in this method's doc comment): a completing singleton is already the global minimum, so
            // it would have been found and committed at size 1, and a |R*| >= 2 winner whose every
            // member singly completes could never have been chosen over it. Reaching here means that
            // premise no longer holds — fail loud rather than silently re-choosing among candidates a
            // broken theorem can no longer rank (the re-choice machinery this branch used to run is
            // deleted, not merely dead-coded).
            throw new InvalidOperationException(
                "AvailabilityComposition.ChooseSuspendedCandidate: a completing singleton coexists "
                + "with a strictly better multi-set — the per-position monotonicity lemma is violated; "
                + "the selection rule has changed and the theorem no longer holds.");
        }

        /// <summary>
        /// The still-removed, suspended candidates in the search's canonical order: ascending
        /// <see cref="SquadRating.PlayerRating"/>, ties broken by earliest roster position.
        /// <para>
        /// The order decides nothing by itself since ERR-030-046 — the subset search does — but it must
        /// be a <b>total</b> order, because it is what "first canonically" means in the preference rule
        /// and therefore what makes two identical calls return the same index. Insertion sort: the
        /// candidate count is bounded by the roster and by the search cap, and this runs once per
        /// extremis pass.
        /// </para>
        /// <para>
        /// <b>Gathers on <c>removed[i] &amp;&amp; suspended[i]</c>, not <c>removed[i]</c> alone (Low).</b>
        /// Every candidate this search considers is suspended by construction TODAY — <see cref="Reinstate"/>'s
        /// tier-1 pass takes anyone removed-and-not-suspended first, so nothing else can be left when
        /// <see cref="ChooseSuspendedCandidate"/> calls this — but that was documented, not enforced. A
        /// future tier landing at this seam (the youth call-ups / generated cover ladder the type remarks
        /// name) is exactly the change that could add a THIRD removal reason between tier 1 and tier 2; the
        /// explicit <c>suspended[i]</c> filter keeps this method gathering only real tier-2 candidates
        /// under such a change instead of silently absorbing whatever tier 1 left behind.
        /// </para>
        /// </summary>
        private static int[] CandidatesByAscendingRank(Squad squad, bool[] removed, bool[] suspended)
        {
            int m = 0;
            for (int i = 0; i < removed.Length; i++)
            {
                if (removed[i] && suspended[i])
                {
                    m++;
                }
            }

            var order = new int[m];
            var rating = new float[m];
            int w = 0;
            for (int i = 0; i < removed.Length; i++)
            {
                if (!removed[i] || !suspended[i])
                {
                    continue;
                }

                order[w] = i;
                rating[w] = SquadRating.PlayerRating(squad.GetPlayer(i).Attributes);
                w++;
            }

            for (int a = 1; a < m; a++)
            {
                int index = order[a];
                float r = rating[a];
                int b = a - 1;

                // Strictly-greater, so the sort is stable and equal ratings keep the ascending roster
                // order they were gathered in — the tie-break, without a second comparison.
                while (b >= 0 && rating[b] > r)
                {
                    order[b + 1] = order[b];
                    rating[b + 1] = rating[b];
                    b--;
                }

                order[b + 1] = index;
                rating[b + 1] = r;
            }

            return order;
        }

        /// <summary>
        /// Probes one candidate subset: <c>-1</c> when the filtered squad plus that subset still cannot
        /// satisfy selection, otherwise how many reinstated-suspended players its starting eleven
        /// contains (zero being the clean completion the search is looking for).
        /// <para>
        /// The mask flips are tentative and are restored before anything else runs — the mask is the
        /// back-fill loop's own state, and a subset that is not chosen must leave no trace in it.
        /// </para>
        /// </summary>
        private static int ProbeSubset(
            Squad squad, bool[] removed, bool[] suspended,
            int[] order, int[] subset, int subsetLength, int availableCount)
        {
            for (int k = 0; k < subsetLength; k++)
            {
                removed[order[subset[k]]] = false;
            }
            Squad candidate = MaterializeSquad(squad, removed, availableCount + subsetLength);
            for (int k = 0; k < subsetLength; k++)
            {
                removed[order[subset[k]]] = true;
            }

            // candidate is never null here: MaterializeSquad returns null only for a count of zero, and
            // this site passes availableCount + subsetLength >= 1.
            if (!SquadRating.CanFieldStartingEleven(candidate))
            {
                return -1;
            }

            return CountReinstatedSuspendedInXi(
                squad, removed, suspended, candidate, order, subset, subsetLength);
        }

        /// <summary>
        /// How many players in <paramref name="candidate"/>'s starting eleven are suspended players the
        /// back-fill has put back — the ones an earlier pass already reinstated
        /// (<c>suspended[i] &amp;&amp; !removed[i]</c>) plus the members of the subset being probed.
        /// Identity is by <c>PlayerId</c>, because the candidate squad's own indices are renumbered by
        /// the materialisation.
        /// <para>
        /// A <b>count</b>, not a predicate (ERR-030-046 — this was <c>AnyReinstatedSuspendedStarts</c>).
        /// The search minimises it, and minimising needs to distinguish "starts one" from "starts three":
        /// a boolean can only find a clean completion or give up, which is precisely how the previous
        /// shape ended up with no claim at all whenever no clean completion existed.
        /// </para>
        /// <para>
        /// The already-reinstated half matters on a multi-reinstatement back-fill: a subset that keeps
        /// its OWN members on the bench while promoting a previously reinstated one into the eleven has
        /// not avoided anything.
        /// </para>
        /// </summary>
        private static int CountReinstatedSuspendedInXi(
            Squad squad, bool[] removed, bool[] suspended, Squad candidate,
            int[] order, int[] subset, int subsetLength)
        {
            int[] startingEleven = SquadRating.StartingElevenPlayerIds(candidate);
            int count = 0;

            for (int i = 0; i < suspended.Length; i++)
            {
                if (!suspended[i])
                {
                    continue;
                }
                if (removed[i] && !InSubset(order, subset, subsetLength, i))
                {
                    // Still filtered out, so he is not in this candidate squad at all.
                    continue;
                }

                int playerId = squad.GetPlayer(i).PlayerId;
                for (int s = 0; s < startingEleven.Length; s++)
                {
                    if (startingEleven[s] == playerId)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        /// <summary>Whether roster index <paramref name="rosterIndex"/> is one of the subset's members.</summary>
        private static bool InSubset(int[] order, int[] subset, int subsetLength, int rosterIndex)
        {
            for (int k = 0; k < subsetLength; k++)
            {
                if (order[subset[k]] == rosterIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the removal/        |
// |         |            |        | back-fill split #30 §3.4 asks for, extracted out of              |
// |         |            |        | PlayerCareerStates.SelectAvailable so a second contributor can   |
// |         |            |        | join before the back-fill rather than after it. Suspension is a  |
// |         |            |        | stricter reinstatement tier than injury (ERR-044-003).           |
// | 1.1     | 2026-08-13 | —      | #44 C1/C2 adversarial review round 2 (L7). Type remarks record   |
// |         |            |        | that Reinstate's extremis tier fields a suspended player whose   |
// |         |            |        | ban this same fixture's OnClubFixturePlayed then decrements      |
// |         |            |        | anyway — the ERR-044-002 hazard reintroduced by ERR-044-003's    |
// |         |            |        | compromise. Owner call recorded, not decided; extends the        |
// |         |            |        | existing ERR-044-003 row in spec-error-log.md.                   |
// | 1.2     | 2026-08-13 | —      | AR round 3 fix (M14): the two contributors carried OPPOSITE mask |
// |         |            |        | contracts — round 1's M3 made MarkSuspended an OWNING writer     |
// |         |            |        | while PlayerCareerStates.MarkUnavailable stayed additive — and   |
// |         |            |        | this file's own comment above the discipline block still         |
// |         |            |        | described the retired additive hazard as current. Both           |
// |         |            |        | contributors now OWN the mask they are handed (MarkUnavailable's |
// |         |            |        | own fix is in PlayerCareerStates.cs); Compose allocates a fresh  |
// |         |            |        | mask per contributor and does all the OR-ing and counting        |
// |         |            |        | itself — the shape it already used for #44, now used for #41     |
// |         |            |        | too — and the stale comment is replaced with the real reason a   |
// |         |            |        | separate mask is required: an owning writer would clear the      |
// |         |            |        | other contributor's removals if they shared one.                 |
// | 1.3     | 2026-08-15 | —      | ERR-044-003 stage 1, owner decision. v1.1's L7 remark recorded    |
// |         |            |        | as an OPEN owner call that Reinstate's extremis tier fields a     |
// |         |            |        | suspended player whose ban the same fixture's serving call then   |
// |         |            |        | discharged — making the appearance free. Decided: it does not.    |
// |         |            |        | The fix is in DisciplineRules.OnClubFixturePlayed, which now      |
// |         |            |        | takes the fielded eleven and exempts anyone in it; the rule is    |
// |         |            |        | about who PLAYED, so it belongs at the serving site and this      |
// |         |            |        | type's tier order is untouched. The remark is rewritten from an   |
// |         |            |        | open question to the decision, and the two further tiers the      |
// |         |            |        | owner agreed to (youth call-ups, then generated cover, both       |
// |         |            |        | ahead of any suspended player) are recorded with their blockers:  |
// |         |            |        | #42 has no src/ assembly, and generated cover needs the packed    |
// |         |            |        | clubId x CLUB_SQUAD_SIZE + local id space widened.                |
// | 1.4     | 2026-08-15 | —      | L2 (reviewed-findings pass): the private Squad-materialization    |
// |         |            |        | helper renamed Compose → MaterializeSquad. It overloaded the      |
// |         |            |        | public Compose's name on two unrelated meanings — "compose        |
// |         |            |        | availability" vs "materialise a Squad from a removed-mask" — and  |
// |         |            |        | the back-fill loop's own `filtered = Compose(squad, removed,      |
// |         |            |        | availableCount);`, called from inside a method named Compose,     |
// |         |            |        | read as recursion. No behaviour change; both call sites updated.  |
// | 1.5     | 2026-08-16 | —      | ERR-030-044 (adversarial review, H2): tier 2's within-tier        |
// |         |            |        | ordering key. The extremis trigger is CanFieldStartingEleven —    |
// |         |            |        | the FULL selection walk, eleven starters PLUS the seven-slot      |
// |         |            |        | bench — so it fires on bench depth alone, while both #30 §3.4     |
// |         |            |        | and #44 §2.3 described it in prose as "cannot field the eleven".  |
// |         |            |        | Reinstating by earliest roster position then handed the           |
// |         |            |        | rating-greedy selector whichever suspended player sat first on    |
// |         |            |        | the roster, and it started him: a club seventeen fit and one      |
// |         |            |        | bench short put its best banned man in the XI, and ERR-044-003    |
// |         |            |        | stage 1's exemption stalled his ban for as long as the club       |
// |         |            |        | stayed depleted — the outcome §3.4's own rationale forbids.       |
// |         |            |        | Tier 2 now probes per candidate (new ChooseSuspendedCandidate /   |
// |         |            |        | AnyReinstatedSuspendedStarts): pass 1 the first candidate the     |
// |         |            |        | selector would BENCH (fieldable AND no reinstated-suspended id    |
// |         |            |        | in the XI), pass 2 the first that is fieldable at all (the        |
// |         |            |        | forced-start residual — sole goalkeeper), pass 3 earliest roster  |
// |         |            |        | position, exactly today's behaviour, for the multi-reinstatement  |
// |         |            |        | case where no single candidate reaches fieldability. Tier 1,      |
// |         |            |        | the outer loop, the F9 terminal throw and FR-DC-018's identity    |
// |         |            |        | fast path are untouched; DisciplineRules needed no change, being  |
// |         |            |        | already correct on both sides. Digest invariance is NOT claimed   |
// |         |            |        | for a season whose extremis tier fires: the reinstatee can        |
// |         |            |        | differ, which is the point. Back-props: ERR-030-044 (#30 §3.4,    |
// |         |            |        | trigger clarification + within-tier amendment) and ERR-044-019    |
// |         |            |        | (#44 §2.3 / §7.2, the two-case statement of the compromise).      |
// | 1.6     | 2026-08-16 | —      | ERR-030-045 (adversarial review, H2-continuation): v1.5's fix     |
// |         |            |        | survived intact for k >= 2 reinstatements, and code, spec and     |
// |         |            |        | suite all asserted that it did not. Fieldability is MONOTONE in   |
// |         |            |        | adding players, so on any reinstatement that is not the last no   |
// |         |            |        | candidate is fieldable: passes 1 and 2 are structurally           |
// |         |            |        | unreachable and pass 3 — earliest roster position, the pre-fix    |
// |         |            |        | key — decided alone. Worse, the blindly reinstated man then       |
// |         |            |        | appears in every later candidate's eleven, so                     |
// |         |            |        | AnyReinstatedSuspendedStarts made pass 1 unsatisfiable for the    |
// |         |            |        | final pick too and it fell to roster order as well; net           |
// |         |            |        | behaviour for k >= 2 was exactly pre-ERR-030-044, in the case a   |
// |         |            |        | mass-suspension club reaches most readily. Pass 3's key becomes   |
// |         |            |        | ascending SELECTOR RANK (SquadRating.PlayerRating, the new        |
// |         |            |        | delegation to LineupSelector.MeanAttribute — not a second rating  |
// |         |            |        | formula), ties on earliest roster position: monotone, so it needs |
// |         |            |        | no probe on the blind passes, and the best banned player is       |
// |         |            |        | reached only when nothing weaker closes the gap. The remarks'     |
// |         |            |        | "two cases" claim is corrected to three — benched when any choice |
// |         |            |        | permits; FORCED START when none does, which now names the k >= 2  |
// |         |            |        | every-completing-choice-starts-someone set alongside the          |
// |         |            |        | sole-goalkeeper forcing; the ban stalls only for a reinstatee     |
// |         |            |        | actually fielded. A best-effort MINIMISATION, not a guarantee,    |
// |         |            |        | and it now says so. Two Lows fixed with it: the Cost remark       |
// |         |            |        | undercounted (a fieldable candidate costs TWO TrySelect walks,    |
// |         |            |        | the second inside AnyReinstatedSuspendedStarts), and the dead     |
// |         |            |        | `candidate == null` branch at the probe site — reachable only at  |
// |         |            |        | availableCount 0, where this site passes >= 1 — is replaced by a  |
// |         |            |        | comment saying why it cannot fire.                                |
// | 1.7     | 2026-08-16, later still | — | ERR-030-046 (escalated High — the SAME defect survived two |
// |         |            |        | successive fixes, so the no-third-identical-retry rule applied    |
// |         |            |        | and the shape was ruled rather than iterated). v1.6's pass-3 key  |
// |         |            |        | is an ascending-PlayerRating GLOBAL SCALAR while LineupSelector   |
// |         |            |        | selects PER POSITION: where the fit squad is thin in the          |
// |         |            |        | globally-weakest banned player's position, the blind pass presses |
// |         |            |        | exactly HIM back — into a slot no fit player can contest — he     |
// |         |            |        | starts, the reinstated-suspended probe is then unsatisfiable for  |
// |         |            |        | every later candidate, and pass 2 falls to roster order. Measured |
// |         |            |        | on generated mass-suspension fixtures, >= 476 of 1920 had a clean |
// |         |            |        | completing choice the algorithm missed. Root-cause CLASS, and the |
// |         |            |        | reason a third ordering key was refused: an element-wise greedy   |
// |         |            |        | decision of a set-valued constraint. A squad is completed by a    |
// |         |            |        | SET, whether that set starts a banned man is a property of the    |
// |         |            |        | SET, and no per-player scalar can express it.                     |
// |         |            |        | ChooseSuspendedCandidate is now a CAPPED EXHAUSTIVE               |
// |         |            |        | clean-completion search: candidates ordered ascending             |
// |         |            |        | PlayerRating (ties on roster index — a total order, so the        |
// |         |            |        | enumeration is canonical, and the only thing the order still      |
// |         |            |        | decides); every subset probed, sizes ascending and lexicographic  |
// |         |            |        | within a size; preference is fewest dirty, then the smallest set, |
// |         |            |        | then canonical first, the first dirty == 0 subset being globally  |
// |         |            |        | optimal and stopping the search. AnyReinstatedSuspendedStarts     |
// |         |            |        | becomes the subset-parameterised COUNT                            |
// |         |            |        | CountReinstatedSuspendedInXi, because minimising needs to tell    |
// |         |            |        | "starts one" from "starts three". The commit rule returns ONE     |
// |         |            |        | member: a one-member R* gives that member; a larger R* gives the  |
// |         |            |        | canonical-first member whose own singleton was probed NOT         |
// |         |            |        | completing (the outer loop cannot exit, so the search reruns), or |
// |         |            |        | else a re-choice among the size-1 completing sets by the same     |
// |         |            |        | preference. + [FIXED] SeasonSaveConstants.                        |
// |         |            |        | EXTREMIS_SEARCH_CANDIDATE_CAP = 12 (2^13 probe bound; an          |
// |         |            |        | algorithmic budget, NOT a gameplay dial, so not [GT] and not      |
// |         |            |        | subject to KD-W1) — beyond it the pass degrades to ascending-rank |
// |         |            |        | greedy with no minimality claim, self-healing as each commit      |
// |         |            |        | lowers m. GUARANTEE, now a theorem rather than a best effort:     |
// |         |            |        | within the cap the composed squad fields the MINIMUM achievable   |
// |         |            |        | number of reinstated-suspended players in its XI, minimised over  |
// |         |            |        | every completing subset — zero whenever any completing choice     |
// |         |            |        | benches them all — so one starts only in a probe-verified forced  |
// |         |            |        | start. Induction step: committing a non-completing member c of R* |
// |         |            |        | cannot raise the optimum, since R* \ {c} stays available and      |
// |         |            |        | F u {c} u (R* \ {c}) is byte-identical to the probed F u R*.      |
// |         |            |        | Termination unchanged (availableCount strictly increases; §2.3 F9 |
// |         |            |        | at the bound; no repair loop). RESIDUAL, exhaustive: (i) forced   |
// |         |            |        | starts — the minimum is positive (sole-GK forcings and their k>=2 |
// |         |            |        | generalisations), where the ERR-044-003 stage-1 exemption stalls  |
// |         |            |        | exactly the fielded bans and no fewer; (ii) the beyond-cap corner.|
// |         |            |        | ERR-030-044's two-case and ERR-030-045's three-case guarantee     |
// |         |            |        | statements are SUPERSEDED, not amended, and the falsified claims  |
// |         |            |        | they rested on ("the final pick's pass 1 still has a benchable    |
// |         |            |        | candidate", the two-routes-into-forced-start enumeration) are     |
// |         |            |        | DELETED rather than re-worded — the enumeration form failed       |
// |         |            |        | twice. Compose, the fresh-mask contract, tier-1 precedence, the   |
// |         |            |        | one-reinstatement-per-pass outer loop, the F9 terminal throw and  |
// |         |            |        | FR-DC-018's identity fast paths are UNTOUCHED. No RNG, no draw    |
// |         |            |        | site, no SNAPSHOT_SCHEMA_VERSION / SEASON_SAVE_FORMAT_VERSION     |
// |         |            |        | movement, no change to SquadRating / LineupSelector / MatchEngine;|
// |         |            |        | digest invariance still not claimed for a season whose extremis   |
// |         |            |        | tier fires. Back-props: #30 section-3.md §3.4 (the search rule +  |
// |         |            |        | theorem + residual) and #44 section-2.md / section-7.md           |
// |         |            |        | (ERR-044-019 EXTENDED, not re-filed).                             |
// | 1.8     | 2026-08-16, even later still | — | Round-4 reviewed-findings pass over the ERR-030-046   |
// |         |            |        | landing (six findings, all fixed). (Medium) The commit rule's     |
// |         |            |        | third branch was unreachable, unproven, and would have violated   |
// |         |            |        | the theorem if live — the reason is a lemma this file never       |
// |         |            |        | stated: LineupSelector picks top-k PER POSITION CLASS, so adding  |
// |         |            |        | a candidate to a completing set can only displace a same-class    |
// |         |            |        | player, dirty(R) is monotone non-decreasing under adding          |
// |         |            |        | candidates, and a completing singleton is therefore already the   |
// |         |            |        | global minimum (verified: thirdBranchReachable = 0 over 6,858     |
// |         |            |        | oracle cases; collapsing the whole commit rule to                 |
// |         |            |        | order[bestSubset[0]] is behaviour-identical over 3,966 cases and  |
// |         |            |        | 11/11 tests). ChooseSuspendedCandidate's doc now states the       |
// |         |            |        | lemma, names the per-position-top-k premise as load-bearing, and  |
// |         |            |        | warns a Stage-1 role-weighted or cross-class selector invalidates |
// |         |            |        | it; the third commit branch is now a fail-loud                    |
// |         |            |        | InvalidOperationException naming the lemma, and the re-choice     |
// |         |            |        | machinery (singletonDirty[], the final pick loop) is DELETED, not |
// |         |            |        | dead-coded. (Medium) §3.4 had no defined behaviour for "no subset |
// |         |            |        | completes" beyond the code's own bestSize == 0 fallback; a fourth |
// |         |            |        | Commit case is now normative in section-3.md, and (Low, same      |
// |         |            |        | branch) a single hoisted full-candidate-set probe at the top of   |
// |         |            |        | the search returns order[0] directly — sound by the same          |
// |         |            |        | monotonicity premise — instead of spending the full m·2^m         |
// |         |            |        | enumeration to discover nothing completes; bestSize == 0 is now a |
// |         |            |        | defensive fail-safe rather than a live path. (Medium) Both        |
// |         |            |        | "self-heals" remarks (~v1.7 line 141 and the Cap. paragraph)      |
// |         |            |        | conflated the SEARCH resuming with the GUARANTEE resuming; a      |
// |         |            |        | greedily committed beyond-cap candidate is never revisited, so    |
// |         |            |        | the composition can remain above the minimum for that fixture —   |
// |         |            |        | corrected at both sites here, at SeasonSaveConstants.cs:118, at   |
// |         |            |        | section-3.md §3.4 (the Cap bullet and residual (ii)), and at the  |
// |         |            |        | mirrored residual in discipline-suspensions/{section-2,           |
// |         |            |        | section-7}.md. (Low) CandidatesByAscendingRank gathered on        |
// |         |            |        | removed[i] alone — "every candidate here is suspended" was        |
// |         |            |        | documented, not enforced, and the planned youth/generated-cover   |
// |         |            |        | tiers land exactly at this seam; now gathers on                   |
// |         |            |        | removed[i] && suspended[i], with the reason stated in its own     |
// |         |            |        | doc. (Low) The Cost remark counted walks, not allocation:         |
// |         |            |        | measured m=12 -> ~74.5 ms / ~157 MiB transient (an int[31] per    |
// |         |            |        | rating read across ~8k probes), m=4 -> 0.22 ms / 460 KiB; both    |
// |         |            |        | remarks now say so, noting season cadence + the cap's own         |
// |         |            |        | unreachability keeps it acceptable and Compose's identity fast    |
// |         |            |        | path is unaffected. Tests: AvailabilityCompositionExtremisTests   |
// |         |            |        | v1.3 (+2 cases at m = 12 exactly and at a tied-dirty-count         |
// |         |            |        | scenario, both observed failing against the two named mutants).   |
// |         |            |        | Back-props: #30 section-3.md §3.4 (the lemma, the fourth Commit   |
// |         |            |        | case, the residual wording), #44 section-2.md / section-7.md      |
// |         |            |        | (residual wording only), spec-error-log.md (ERR-030-046           |
// |         |            |        | annotation, dated).                                                |
#endregion
