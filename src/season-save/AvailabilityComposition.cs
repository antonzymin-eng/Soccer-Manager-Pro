// File:     src/season-save/AvailabilityComposition.cs
// Created:  2026-08-13
// Modified: 2026-08-16 (ERR-030-044, adversarial-review H2 — tier 2's within-tier ordering key is now
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
    /// <b>So tier 2 is a CHOICE, and the choice is probe-qualified</b> (ERR-030-044). Reinstating by
    /// earliest roster position put whichever suspended player happened to sit first on the roster into
    /// the pool the rating-greedy selector then draws the STARTING eleven from — so a club that was
    /// merely a bench short got its best banned player back into the XI, and ERR-044-003 stage 1's
    /// exemption (he played, so the fixture is not one of his ban) then stalled that ban for as long as
    /// the club stayed depleted. An ordering key that produces the outcome its own rule's rationale
    /// forbids is a defect in the key, so tier 2 now asks the probe per candidate: the first candidate,
    /// in roster order, that the selector would BENCH; earliest roster position only when no candidate
    /// choice keeps every reinstated-suspended player out of the eleven.
    /// </para>
    /// <para>
    /// <b>The compromise therefore has two cases, and only one of them stalls a ban.</b> Benched — the
    /// bench-depth case, and the common one — the suspended player is not in the fielded eleven, so
    /// <c>DisciplineRules.OnClubFixturePlayed</c> does not exempt him and his ban advances normally: the
    /// suspension costs exactly what the Laws say. Forced to start — no candidate choice avoids the XI,
    /// the club's only goalkeeper being the canonical case — he plays, the exemption fires, and the ban
    /// does not advance. That second case is the residual recorded under ERR-044-003 / ERR-044-019, and
    /// it is what the two missing tiers below eventually delete; it is not a licence for the first.
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
        /// once none of those remain, a suspended one — chosen by <see cref="ChooseSuspendedCandidate"/>
        /// rather than by roster position (ERR-030-044).
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
        /// Tier 2's within-tier choice (ERR-030-044): WHICH suspended player is pressed back in, when the
        /// extremis branch has to press one. Returns his roster index, or <c>-1</c> when nobody is removed
        /// at all — which <see cref="Reinstate"/>'s own guard turns into the divergence throw.
        /// <para>
        /// Three passes, in order, over the still-removed candidates in ascending roster order. Every one
        /// of them is suspended: tier 1 above takes anyone who is removed and NOT suspended, so reaching
        /// here means it found none.
        /// </para>
        /// <list type="number">
        /// <item><b>The first candidate the selector would BENCH.</b> The squad is materialised with him
        /// reinstated and probed: it must be fieldable AND its starting eleven must contain no
        /// reinstated-suspended player — neither him nor anyone the loop has already pressed back on an
        /// earlier pass. This is the pass that keeps a bench-depth shortfall from putting a banned man in
        /// the XI, and with him out of the eleven his ban serves normally.</item>
        /// <item><b>Failing that, the first candidate who makes the squad fieldable at all.</b> The club
        /// still has to take the field (§2.3 <b>F9</b>); a forced start is the recorded compromise, and
        /// ERR-044-003 stage 1's exemption then stalls that one ban.</item>
        /// <item><b>Failing that, earliest roster position</b> — exactly the pre-ERR-030-044 behaviour.
        /// Reached when no SINGLE reinstatement reaches fieldability, i.e. the club is short by more than
        /// one; the outer loop presses another player back and asks again, so the probe-qualified passes
        /// get their say on the reinstatement that finally closes the gap.</item>
        /// </list>
        /// <para>
        /// <b>Cost.</b> One squad materialisation plus one selection walk per candidate per reinstatement
        /// — but only on the extremis branch, which needs a club with no injured players left to press
        /// back and at least one suspended one. On every ordinary fixture this method is never called at
        /// all, and the season path pays boot-cadence costs anyway (<c>SeasonLoop.ResolveFixture</c> makes
        /// the same argument for re-rating each club per matchday).
        /// </para>
        /// </summary>
        private static int ChooseSuspendedCandidate(
            Squad squad, bool[] removed, bool[] suspended, int availableCount)
        {
            int firstFieldable = -1;

            for (int c = 0; c < removed.Length; c++)
            {
                if (!removed[c])
                {
                    continue;
                }

                // Tentative: c is put back only for the probe, and restored immediately. The mask is the
                // loop's own state — a candidate that is not chosen must leave no trace in it.
                removed[c] = false;
                Squad candidate = MaterializeSquad(squad, removed, availableCount + 1);
                removed[c] = true;

                if (candidate == null || !SquadRating.CanFieldStartingEleven(candidate))
                {
                    continue;
                }

                if (firstFieldable < 0)
                {
                    firstFieldable = c;
                }

                if (!AnyReinstatedSuspendedStarts(squad, removed, suspended, candidate, c))
                {
                    return c;
                }
            }

            if (firstFieldable >= 0)
            {
                return firstFieldable;
            }

            for (int i = 0; i < removed.Length; i++)
            {
                if (removed[i])
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Whether <paramref name="candidate"/>'s starting eleven contains any suspended player the
        /// back-fill has put back — the ones already reinstated on an earlier pass
        /// (<c>suspended[i] &amp;&amp; !removed[i]</c>) plus the candidate <paramref name="c"/> being
        /// probed. Identity is by <c>PlayerId</c>, because the candidate squad's own indices are
        /// renumbered by the materialisation.
        /// <para>
        /// The already-reinstated half matters on a multi-reinstatement back-fill: a pass that keeps its
        /// OWN candidate on the bench while promoting a previously reinstated one into the eleven has not
        /// avoided anything.
        /// </para>
        /// </summary>
        private static bool AnyReinstatedSuspendedStarts(
            Squad squad, bool[] removed, bool[] suspended, Squad candidate, int c)
        {
            int[] startingEleven = SquadRating.StartingElevenPlayerIds(candidate);

            for (int i = 0; i < suspended.Length; i++)
            {
                if (!suspended[i])
                {
                    continue;
                }
                if (removed[i] && i != c)
                {
                    // Still filtered out, so he is not in this candidate squad at all.
                    continue;
                }

                int playerId = squad.GetPlayer(i).PlayerId;
                for (int s = 0; s < startingEleven.Length; s++)
                {
                    if (startingEleven[s] == playerId)
                    {
                        return true;
                    }
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
#endregion
