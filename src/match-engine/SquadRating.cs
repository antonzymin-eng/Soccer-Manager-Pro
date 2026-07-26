// File:     src/match-engine/SquadRating.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-7 +
//           AR-4 M-1 (the named #30 T2 prerequisite); Season & Competition Loop #30 §3.4.1
//           (FR-SN-013a); Lineup Selection design supplement; Code Standards #20
// Purpose:  The narrow PUBLIC seam onto lineup-derived squad strength — the one number the #30 T2
//           round-resolution model needs, computed by the one selector the engine itself fields.
//           Boot/season-cadence only (never the 10 Hz or 60 Hz loops).

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Squad strength as the match engine sees it: the mean attribute rating of the eleven players
    /// <c>LineupSelector</c> would actually field.
    /// <para>
    /// <b>Why this type exists (league-bootstrap AR-4 M-1).</b> #30's round-resolution model
    /// (<c>RoundResolutionModel</c>, in <c>season-save</c>) is keyed on
    /// <c>Rating(home) − Rating(away)</c>, and KD-7 pins <c>Rating</c> to the
    /// <c>LineupSelector</c> starting-XI mean. But <c>LineupSelector</c> is <c>internal</c> to this
    /// assembly and visible only to its own tests, so <c>SeasonLoop</c> could not reach it — and the
    /// tempting workaround, re-implementing selection in <c>season-save</c>, is explicitly refused:
    /// two selectors would disagree the moment either changed, and the quick-sim's rating would stop
    /// describing the team the engine fields. This is the "small public rating accessor" KD-7 sanctions
    /// instead.
    /// </para>
    /// <para>
    /// <b>No formation parameter.</b> The formation is <c>MatchEngineConstants.STAGE0_FORMATION</c>, the
    /// same one <c>ConfigureSquads</c> selects against, so a caller cannot rate a squad against a
    /// formation the engine would not use. It also keeps <c>positioning-ai</c>'s
    /// <c>FormationFamily</c> off the public surface, so <c>season-save</c> needs no
    /// <c>positioning-ai</c> reference (the formation-parameterized form stays internal on
    /// <c>LineupSelector</c> for this assembly's own tests).
    /// </para>
    /// </summary>
    public static class SquadRating
    {
        /// <summary>
        /// The mean <c>[1,20]</c> attribute rating over the starting eleven
        /// <c>LineupSelector</c> selects from <paramref name="squad"/> under the Stage-0 formation.
        /// <para>
        /// Pure and deterministic: selection draws no RNG and ties break on ascending <c>PlayerId</c>,
        /// so the same squad always yields the same rating. <c>WeakFootRating</c> is excluded (its
        /// <c>[1,5]</c> scale is not on the attribute array).
        /// </para>
        /// </summary>
        /// <param name="squad">A full club roster.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="squad"/> is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// The squad cannot field the Stage-0 formation — a starter slot's required position has no
        /// eligible player (KD-L3). Fail-loud rather than rating a squad that could never take the
        /// pitch; <c>LeagueBootstrap</c>'s position template (KD-6) is what keeps generated squads
        /// clear of this.
        /// </exception>
        public static float StartingElevenMean(Squad squad)
        {
            if (squad == null)
            {
                throw new System.ArgumentNullException(nameof(squad));
            }

            return LineupSelector.StartingElevenMean(squad, MatchEngineConstants.STAGE0_FORMATION);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 T2 prerequisite): the public XI-mean   |
// |         |            |        | rating seam over the internal LineupSelector, so season-save's     |
// |         |            |        | round-resolution model reuses the engine's own selection rather    |
// |         |            |        | than growing a parallel one (league-bootstrap KD-7 / AR-4 M-1).    |
#endregion
