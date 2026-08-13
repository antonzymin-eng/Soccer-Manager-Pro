// File:     src/discipline/Availability.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.3 / FR-DC-008/009/010/022; ERR-044-003 (F5 vs #30 §2.3 F9
//           — viability is #30's, #44 contributes removals only); Season & Competition Loop #30 §3.4
//           (the composed availability seam, ERR-030-016/ERR-030-029); Code Standards #20
// Purpose:  #44's read-only availability view — the pure suspension predicate, and the removal set it
//           contributes to #30's composed resolve→filter→configure seam.

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// #44's availability view (KD-4). Pure over <see cref="DisciplineState"/> — nothing here writes
    /// #27 <c>Squad</c>/<c>PlayerRecord</c> state, or any state at all (FR-DC-001).
    /// <para>
    /// <b>#44 contributes REMOVALS. It does not adjudicate viability.</b> #44 §2.3 <b>F5</b> requires
    /// this filter to fail loud when it would reduce a squad below the eighteen <c>ConfigureSquads</c>
    /// consumes — but #30 §3.4 (ERR-030-029, approved after #44 was written) settles the same event
    /// differently and holds authority by its own explicit statement: the seam back-fills, probing
    /// <c>SquadRating.CanFieldStartingEleven</c>, and fails loud only if the WHOLE squad cannot field
    /// the formation (#30 §2.3 F9) — "#44/#36 contribute removals only and inherit the rule unchanged
    /// when they join". Two viability rules of opposite posture on one shared method is a
    /// spec-vs-spec contradiction, filed as <b>ERR-044-003</b>; #30 wins, and F5's fail-loud is not
    /// implemented here. Nothing is lost: the eighteen-player floor is still enforced, one layer up,
    /// by the rule that also knows how to recover from it.
    /// </para>
    /// <para>
    /// <b>Removals are what makes the composition order-independent.</b> #30 §3.4 asks that the
    /// commuting property of the composed filters be preserved rather than relied on: set intersection
    /// commutes, so #41's injury removals and #44's suspension removals may be gathered in any order —
    /// but a filter that ADDED a player would need an explicit order and could not simply join the
    /// list. This type only ever removes.
    /// </para>
    /// </summary>
    public static class Availability
    {
        /// <summary>
        /// True unless the player is serving a ban in this competition (FR-DC-008). A player with no
        /// row is available — the absent case is not an error, and under FR-DC-017 it is the only
        /// representation of "clean".
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        public static bool IsAvailable(DisciplineState state, int playerId, int competitionId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return !state.EntryFor(playerId, competitionId).IsSuspended;
        }

        /// <summary>
        /// Marks every suspended member of <paramref name="squad"/> in <paramref name="removed"/> —
        /// #44's contribution to #30's composed availability seam (§3.3, FR-DC-010).
        /// <para>
        /// <b>Additive into a shared mask</b>, never clearing a flag another contributor set: the mask
        /// is the union of every filter's removals, and a player unavailable for one reason is not made
        /// available by being fit for another.
        /// </para>
        /// <para>
        /// Applies to <b>each</b> resolved squad — the managed club's and its opponent's — and on
        /// <b>both</b> resolution paths. FR-DC-010 names only "the engine-resolved fixture", which is
        /// the narrower text and is wrong: #30 §3.4 has the seam LIVE "for both clubs of every fixture
        /// on both resolution paths", and FR-DC-011 already serves bans on both, so filtering on one
        /// would let a quick-sim fixture decrement a ban the banned player just played through. Filed
        /// as <b>ERR-044-002</b>; the caller is #30's single <c>SelectAvailable</c> site, which reaches
        /// both paths.
        /// </para>
        /// </summary>
        /// <param name="squad">The club's unfiltered roster.</param>
        /// <param name="state">The tally to read. Never written.</param>
        /// <param name="competitionId">The competition partition (FR-DC-012).</param>
        /// <param name="removed">
        /// A mask parallel to the squad's roster indices. Suspended players' entries are set true;
        /// entries already true are left alone.
        /// </param>
        /// <returns>How many entries this call newly set — 0 for the overwhelming majority of fixtures.</returns>
        /// <exception cref="ArgumentNullException">Any argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="removed"/> is not the squad's length — it
        /// is indexed by roster position, so a length mismatch means it was built against a different
        /// squad and every flag in it names the wrong player.</exception>
        public static int MarkSuspended(Squad squad, DisciplineState state, int competitionId, bool[] removed)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (removed == null)
            {
                throw new ArgumentNullException(nameof(removed));
            }
            if (removed.Length != squad.Count)
            {
                throw new ArgumentException(
                    "Availability.MarkSuspended: the removal mask has " + removed.Length +
                    " entries for a " + squad.Count + "-player squad. It is indexed by roster position, " +
                    "so a length mismatch means it was built against a different squad.",
                    nameof(removed));
            }

            int newlyRemoved = 0;
            for (int local = 0; local < squad.Count; local++)
            {
                if (removed[local])
                {
                    continue;
                }
                if (state.EntryFor(squad.GetPlayer(local).PlayerId, competitionId).IsSuspended)
                {
                    removed[local] = true;
                    newlyRemoved++;
                }
            }

            return newlyRemoved;
        }

        /// <summary>
        /// The reduced value copy of <paramref name="squad"/> with suspended players removed
        /// (FR-DC-009) — #44's removal set expressed as a squad, for a caller that composes nothing
        /// else. Built through <see cref="MarkSuspended"/> so there is one suspension rule, not two.
        /// <para>
        /// Returns the SAME instance when nothing is suspended: with no active ban the filter passes
        /// the squad through unchanged (FR-DC-009), and the seam #30 owns already distinguishes
        /// "filtered" from "untouched" by reference.
        /// </para>
        /// <para>
        /// <b>No viability gate</b> — see the type remarks (ERR-044-003). This can return a squad too
        /// small to field an eleven; #30's seam is what recovers from that, and it is the only thing
        /// that can, because it alone sees every contributor's removals.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="squad"/> or <paramref name="state"/> is null.</exception>
        public static Squad FilterAvailable(Squad squad, DisciplineState state, int competitionId)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var removed = new bool[squad.Count];
            if (MarkSuspended(squad, state, competitionId, removed) == 0)
            {
                return squad;
            }

            int keptCount = 0;
            for (int local = 0; local < removed.Length; local++)
            {
                if (!removed[local])
                {
                    keptCount++;
                }
            }

            var kept = new PlayerRecord[keptCount];
            int w = 0;
            for (int local = 0; local < removed.Length; local++)
            {
                if (!removed[local])
                {
                    kept[w++] = squad.GetPlayer(local);
                }
            }

            return new Squad(squad.ClubId, kept);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0, roadmap C1): the pure suspension |
// |         |            |        | predicate, the removal-mask contribution #30's composed seam     |
// |         |            |        | consumes, and the reduced-copy filter built on it. F5's          |
// |         |            |        | fail-loud deliberately NOT implemented — ERR-044-003.            |
#endregion
