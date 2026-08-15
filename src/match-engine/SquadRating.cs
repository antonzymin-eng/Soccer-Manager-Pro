// File:     src/match-engine/SquadRating.cs
// Created:  2026-07-26
// Modified: 2026-08-15 (#44 AR round 5, M5 — the substitution-dependency note now names its
//           SECOND consumer: #44's FR-DC-011 serving exemption, not just #41's appearance record)
// Prior-Modified: 2026-08-07
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

        /// <summary>
        /// Whether <paramref name="squad"/> can field the Stage-0 formation at all — enough players for
        /// the starters and bench, and an eligible player for every starter slot's required position
        /// (KD-L3).
        /// <para>
        /// <b>Why a probe rather than letting the caller catch.</b> #30's availability filter (#41
        /// FR-MD-023) removes injured players before rating or configuring, and an injury list can
        /// leave a club position-incomplete — every goalkeeper out, say — at which point both
        /// <see cref="StartingElevenMean"/> and <c>ConfigureSquads</c> refuse and the season stops.
        /// The filter's answer is to press the least-injured back into service until the club can play,
        /// and to do that it has to be able to ask the question without treating a thrown exception as
        /// control flow.
        /// </para>
        /// <para>
        /// Pure and allocating, like <see cref="StartingElevenMean"/> — boot / season cadence only.
        /// </para>
        /// </summary>
        /// <param name="squad">A club roster.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="squad"/> is null.</exception>
        public static bool CanFieldStartingEleven(Squad squad)
        {
            if (squad == null)
            {
                throw new System.ArgumentNullException(nameof(squad));
            }

            return LineupSelector.CanSelect(squad, MatchEngineConstants.STAGE0_FORMATION);
        }

        /// <summary>
        /// The <c>PlayerId</c>s of the starting eleven <c>LineupSelector</c> selects from
        /// <paramref name="squad"/> under the Stage-0 formation — the same eleven
        /// <see cref="StartingElevenMean"/> rates and <c>ConfigureSquads</c> fields.
        /// <para>
        /// <b>Why this seam exists (ERR-041-010(b)).</b> #30's appearance record (the FR-MD-010
        /// <c>MatchLoad</c> input) needs to know WHO was fielded, on both resolution paths, and the
        /// alternative — a second selection walk in <c>season-save</c> — is exactly the
        /// parallel-surface trap the T2 AR collapsed <c>LineupSelector.CanSelect</c>'s hand-copied
        /// walk to avoid. One selector, three read shapes (mean, viability, ids).
        /// </para>
        /// <para>
        /// Pure, deterministic and allocating, like its siblings — boot / season cadence only. The
        /// returned array is fresh per call; no caller can alias another's.
        /// </para>
        /// <para>
        /// <b>Substitution dependency (recorded, not yet real):</b> this read equals "everyone who
        /// took the pitch" only while Stage 0 fields a fixed eleven. When substitutions exist, the
        /// appearance record's question ("who played?") and this method's ("who started?") diverge,
        /// and the record must widen at its call sites — do not paper over that by redefining THIS
        /// read, which <c>StartingElevenMean</c> and <c>ConfigureSquads</c> also key on.
        /// </para>
        /// <para>
        /// <b>There are TWO such consumers now, not one</b> (#44 AR round 5, M5). #41's FR-MD-010
        /// appearance record was the first. The second, added August 15, 2026, is #44's FR-DC-011
        /// <b>serving exemption</b>: <c>SeasonLoop.FieldedXi</c> feeds this array to
        /// <c>DisciplineRules.OnClubFixturePlayed</c>, which skips the ban decrement for anyone in it.
        /// That consumer is the sharper of the two, because #44 goes to real lengths to be
        /// substitution-CORRECT for card attribution (FR-DC-005, the whole <c>CardLedgerFold</c>
        /// occupancy machinery, the synthetic bench-id derivation) and then serves bans against "who
        /// started". The day <c>SubstitutePlayer</c> gets a production caller, a suspended player
        /// pressed on by #30 §2.3 F9's extremis back-fill and brought on as a substitute has his ban
        /// served for a match he played — exactly the free appearance ERR-044-003 stage 1 exists to
        /// remove, silently reintroduced. Widen at the call sites, as above; not here.
        /// </para>
        /// </summary>
        /// <param name="squad">A full club roster.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="squad"/> is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// The squad cannot field the Stage-0 formation (KD-L3) — same contract as
        /// <see cref="StartingElevenMean"/>; probe with <see cref="CanFieldStartingEleven"/> first.
        /// </exception>
        public static int[] StartingElevenPlayerIds(Squad squad)
        {
            if (squad == null)
            {
                throw new System.ArgumentNullException(nameof(squad));
            }

            LineupPlan plan = LineupSelector.Select(squad, MatchEngineConstants.STAGE0_FORMATION);
            int[] starters = plan.StarterLocalIndices;
            var ids = new int[starters.Length];
            for (int s = 0; s < starters.Length; s++)
            {
                ids[s] = squad.GetPlayer(starters[s]).PlayerId;
            }

            return ids;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 T2 prerequisite): the public XI-mean   |
// |         |            |        | rating seam over the internal LineupSelector, so season-save's     |
// |         |            |        | round-resolution model reuses the engine's own selection rather    |
// |         |            |        | than growing a parallel one (league-bootstrap KD-7 / AR-4 M-1).    |
// | 1.1     | 2026-08-06 | —      | #41 T2: + CanFieldStartingEleven, the viability probe #30's        |
// |         |            |        | availability filter needs. An injury list can leave a club         |
// |         |            |        | position-incomplete, at which point selection refuses and the      |
// |         |            |        | season stops; the filter presses the least-injured back in until   |
// |         |            |        | the club can play, which needs to ask rather than catch.           |
// | 1.2     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): + StartingElevenPlayerIds, the   |
// |         |            |        | id-returning read #30's appearance record needs on both resolution |
// |         |            |        | paths. Same single TrySelect walk — a second selection walk in     |
// |         |            |        | season-save would be the parallel-surface trap CanSelect's own     |
// |         |            |        | history warns about.                                               |
// | 1.3     | 2026-08-07 | —      | Balance-pass AR pass 2 (L, doc only): StartingElevenPlayerIds     |
// |         |            |        | records its substitution dependency — "who started" equals "who   |
// |         |            |        | played" only while Stage 0 fields a fixed eleven; the divergence  |
// |         |            |        | belongs to the appearance record's call sites, not to this read.  |
// | 1.4     | 2026-08-15 | —      | AR round 5 fix (M5), doc only. The substitution-dependency  |
// |         |            |        | note named only #41's appearance record as the consumer that|
// |         |            |        | must widen when substitutions land. Since ERR-044-003 stage |
// |         |            |        | 1 there is a second — #44's serving exemption, which reads  |
// |         |            |        | this array through SeasonLoop.FieldedXi — and it is the     |
// |         |            |        | sharper case: a suspended player brought on as a SUB would  |
// |         |            |        | serve his ban for a match he played, reintroducing exactly  |
// |         |            |        | the free appearance stage 1 removed.                        |
#endregion
