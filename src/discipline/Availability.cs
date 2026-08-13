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
        /// Writes #44's suspension mask for <paramref name="squad"/> into <paramref name="removed"/> —
        /// #44's contribution to #30's composed availability seam (§3.3, FR-DC-010).
        /// <para>
        /// <b>This method OWNS <paramref name="removed"/>.</b> Every entry is (re)written to reflect
        /// suspension status alone, unconditionally — index <c>local</c> ends the call exactly
        /// <c>true</c> when that player is suspended and exactly <c>false</c> otherwise, regardless of
        /// what the caller passed in. It is <b>NOT additive</b>: a caller composing #44's removals with
        /// another contributor's (e.g. #41's) MUST allocate a separate mask for each contributor and
        /// OR them together explicitly — the shape <c>AvailabilityComposition.Compose</c> already uses,
        /// because passing one shared mask here would let this call silently CLEAR a flag a different
        /// contributor had set for a different reason (M3: the prior additive-and-skip contract made
        /// exactly that mistake look like the safe, documented way to call it).
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
        /// A mask parallel to the squad's roster indices, indexed by roster position. Overwritten in
        /// full by this call — see the remarks above.
        /// </param>
        /// <returns>How many players are suspended — the number of entries this call wrote <c>true</c>,
        /// 0 for the overwhelming majority of fixtures.</returns>
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

            int suspendedCount = 0;
            for (int local = 0; local < squad.Count; local++)
            {
                bool isSuspended = state.EntryFor(squad.GetPlayer(local).PlayerId, competitionId).IsSuspended;
                removed[local] = isSuspended;
                if (isSuspended)
                {
                    suspendedCount++;
                }
            }

            return suspendedCount;
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
        /// <b>Returns <c>null</c> when EVERY player is suspended</b> (M2). <see cref="Squad"/>'s own
        /// constructor refuses a zero-player squad, so there is no reduced value copy left to return —
        /// this mirrors the private helper of the same shape one layer up in
        /// <c>AvailabilityComposition.Compose</c> rather than letting a cross-assembly
        /// <c>ArgumentException</c> naming neither discipline nor the cause escape from
        /// <c>Squad</c>'s constructor. <b>This method — and this all-suspended case — is FR-DC-009's own
        /// surface, exercised directly by this assembly's tests, and is NOT #44's production path:</b>
        /// #30's composed seam (<c>AvailabilityComposition.Compose</c>) never calls it, consuming
        /// <see cref="MarkSuspended"/>'s mask directly instead so it can back-fill before a
        /// too-small-to-field squad is ever constructed.
        /// </para>
        /// <para>
        /// <b>No viability gate otherwise</b> — see the type remarks (ERR-044-003). This can return a
        /// squad too small to field an eleven; #30's seam is what recovers from that, and it is the
        /// only thing that can, because it alone sees every contributor's removals.
        /// </para>
        /// </summary>
        /// <returns>
        /// The reduced squad, the same instance when nothing is suspended, or <c>null</c> when every
        /// player is suspended.
        /// </returns>
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

            if (keptCount == 0)
            {
                return null;
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
// | 1.1     | 2026-08-13 | —      | AR fixes. M2: FilterAvailable now returns null for an            |
// |         |            |        | all-suspended squad instead of letting Squad's constructor throw |
// |         |            |        | a cross-assembly ArgumentException naming neither #44 nor the    |
// |         |            |        | cause; doc records this method is FR-DC-009's own surface, not   |
// |         |            |        | the production path (AvailabilityComposition consumes the mask   |
// |         |            |        | directly). M3: MarkSuspended now OWNS its removed mask — writes  |
// |         |            |        | suspension status unconditionally for every index instead of     |
// |         |            |        | skipping already-true entries — because the additive contract    |
// |         |            |        | was the shape that would put a suspended player on the pitch if  |
// |         |            |        | a caller ever passed it a shared mask; AvailabilityComposition   |
// |         |            |        | already allocates a fresh, separate mask, so it is unaffected.   |
#endregion
