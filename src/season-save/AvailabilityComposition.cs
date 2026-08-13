// File:     src/season-save/AvailabilityComposition.cs
// Created:  2026-08-13
// Modified: 2026-08-13
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
    /// the least-injured back in until the club can field the formation, and that "in the limit the
    /// back-fill is the whole squad — the unfiltered behaviour — so the composed filter can never leave
    /// a club worse off than having no filter at all." Preserving that invariant literally means a
    /// suspended player is reinstatable in extremis, which the Laws do not allow. So suspension is a
    /// STRICTER TIER: every injured player is pressed back before any suspended one, and a suspended
    /// player plays only when the alternative is a club that cannot take the field at all. That keeps
    /// #30's stated invariant true and never wedges a season; it is recorded under ERR-044-003 as the
    /// one place #44's football and #30's liveness disagree, with the deferral queue (#44 §7.2) as the
    /// designed answer if the owner would rather refuse the fixture than field a banned player.
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

            int removedCount = 0;
            if (career != null)
            {
                removedCount += career.MarkUnavailable(squad, removed, recoveryRemaining);
            }

            if (discipline != null)
            {
                // A separate mask, then OR'd in — MarkSuspended skips entries already true, so passing
                // the shared mask would leave a player who is BOTH injured and suspended untagged as
                // suspended and back-fillable in the cheaper tier.
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
            Squad filtered = Compose(squad, removed, availableCount);

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

                Reinstate(removed, suspended, recoveryRemaining);
                availableCount++;
                filtered = Compose(squad, removed, availableCount);
            }

            return availableCount == total ? squad : filtered;
        }

        /// <summary>
        /// The squad of the currently-selectable players, or <c>null</c> when none are — which
        /// <see cref="Squad"/> itself refuses to represent, and which the back-fill loop then resolves
        /// by selecting someone.
        /// </summary>
        private static Squad Compose(Squad squad, bool[] removed, int availableCount)
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
        /// once none of those remain, a suspended one (earliest roster position).
        /// <para>
        /// The tier split is the whole football content of this method — see the type remarks. Called
        /// only when at least one player is still removed, which the loop's own guard establishes.
        /// </para>
        /// </summary>
        private static void Reinstate(bool[] removed, bool[] suspended, int[] recoveryRemaining)
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
                for (int i = 0; i < removed.Length; i++)
                {
                    if (removed[i])
                    {
                        best = i;
                        break;
                    }
                }
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the removal/        |
// |         |            |        | back-fill split #30 §3.4 asks for, extracted out of              |
// |         |            |        | PlayerCareerStates.SelectAvailable so a second contributor can   |
// |         |            |        | join before the back-fill rather than after it. Suspension is a  |
// |         |            |        | stricter reinstatement tier than injury (ERR-044-003).           |
#endregion
