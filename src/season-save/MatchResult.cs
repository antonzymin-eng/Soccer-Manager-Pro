// File:     src/season-save/MatchResult.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, §3.4, FR-SN-016..018, KD-3; Code Standards #20
// Purpose:  The structured match-outcome payload — the #22 WorldLoop phase-1 PRODUCER event (KD-3).
//           #30 emits it and records it; it deliberately has NO ingest consumer (FR-SN-017): phase-1
//           activation is gated on #33 (FR-LW-032), and building the consumer now would be the
//           phantom-consumer class FR-LW-031 forbids.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One fixture's outcome (#30 §2.2 / FR-SN-016) — the match-outcome producer payload.
    /// <para>
    /// Identical in shape whether the fixture ran through the full <c>MatchEngine</c> or the
    /// round-resolution model (§3.4.1), so the table-apply path is the same for both (FR-SN-012).
    /// </para>
    /// <para>
    /// <b>Producer only (KD-3 / FR-SN-017).</b> #30 must not wire this into #22's phase-1 ingest and
    /// must not add an ingest entry point to #22. The payload SHAPE is co-defined with #33 at its
    /// landing, cross-checked against FR-LW-027 / FR-LW-032 — so treat these fields as #30's own
    /// record, not as a frozen cross-assembly contract.
    /// </para>
    /// </summary>
    public readonly struct MatchResult
    {
        /// <summary>The home club.</summary>
        public readonly int HomeClubId;

        /// <summary>The away club.</summary>
        public readonly int AwayClubId;

        /// <summary>Goals scored by the home club.</summary>
        public readonly int HomeGoals;

        /// <summary>Goals scored by the away club.</summary>
        public readonly int AwayGoals;

        /// <summary>The round this fixture belonged to.</summary>
        public readonly int RoundIndex;

        /// <summary>The world-day the fixture was played on (matches <c>WorldStore.CurrentWorldTick</c>,
        /// hence <c>uint</c>).</summary>
        public readonly uint WorldDay;

        /// <summary>
        /// Constructs a match result.
        /// </summary>
        /// <exception cref="System.ArgumentException">Self-fixture (F2 class).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Negative goals or round index (F2 class).</exception>
        public MatchResult(
            int homeClubId, int awayClubId, int homeGoals, int awayGoals, int roundIndex, uint worldDay)
        {
            if (homeClubId == awayClubId)
            {
                throw new System.ArgumentException(
                    $"A result cannot be a self-fixture; got club {homeClubId} against itself.",
                    nameof(awayClubId));
            }

            if (homeGoals < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(homeGoals), homeGoals, "Goal counts must be non-negative.");
            }

            if (awayGoals < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(awayGoals), awayGoals, "Goal counts must be non-negative.");
            }

            if (roundIndex < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(roundIndex), roundIndex, "roundIndex must be non-negative.");
            }

            HomeClubId = homeClubId;
            AwayClubId = awayClubId;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            RoundIndex = roundIndex;
            WorldDay = worldDay;
        }

        /// <summary>True when the fixture was drawn.</summary>
        public bool IsDraw => HomeGoals == AwayGoals;

        /// <summary>
        /// The winning club's id, or <c>null</c> on a draw.
        /// </summary>
        public int? WinnerClubId
        {
            get
            {
                if (HomeGoals > AwayGoals)
                {
                    return HomeClubId;
                }

                if (AwayGoals > HomeGoals)
                {
                    return AwayClubId;
                }

                return null;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): producer-only payload with F2     |
// |         |            |        | construction gates; no #22 ingest wiring (FR-SN-017).              |
#endregion
