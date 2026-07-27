// File:     src/season-save/SeasonRollOutcome.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Season & Competition Loop #30 §3.5 (season-boundary roll), FR-SN-029/FR-SN-031;
//           §2.2 (board state); Code Standards #20
// Purpose:  The value record returned by SeasonLoop.RollToNextSeason — what the board decided at the
//           boundary and what the next season starts from. Read-only value copies; producing one
//           mutates nothing (the roll has already committed by the time it is built).

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The outcome of one season-boundary roll (#30 §3.5).
    /// <para>
    /// <b>Why this exists rather than a void return.</b> The roll's most consequential step is (b), the
    /// board's pass/fail evaluation, and its result is otherwise only inspectable by diffing
    /// <see cref="BoardState.JobSecurityPerMille"/> before and after — which asks every caller to
    /// snapshot state it does not own. A career screen has to say *"you finished 14th, the board wanted
    /// 10th, your job security fell"*, and this is the surface that says it. It is a
    /// <b>session-scoped producer record</b>, not serialized state: everything in it is either derivable
    /// from the season it describes or already carried by <see cref="SeasonState"/> (the ERR-030-013
    /// posture — the durable record is the state, not a parallel log).
    /// </para>
    /// </summary>
    public readonly struct SeasonRollOutcome
    {
        /// <summary>The <c>SeasonNumber</c> of the season that just ENDED.</summary>
        public readonly int CompletedSeasonNumber;

        /// <summary>The managed club's final league position in that season (1 = champions).</summary>
        public readonly int FinalPosition;

        /// <summary>The position the board required — <c>BoardObjective.TargetPositionOrBetter</c>.</summary>
        public readonly int TargetPosition;

        /// <summary>Whether <see cref="FinalPosition"/> satisfied the objective.</summary>
        public readonly bool ObjectiveMet;

        /// <summary>Job security (per-mille) going INTO the evaluation.</summary>
        public readonly int JobSecurityBeforePerMille;

        /// <summary>
        /// Job security (per-mille) after the evaluation, clamped into
        /// <c>[0, SeasonLoopConstants.JobSecurityScale]</c>. Zero does not by itself sack the manager —
        /// #30 owns the reading, #45 owns the consequence.
        /// </summary>
        public readonly int JobSecurityAfterPerMille;

        /// <summary>The <c>SeasonNumber</c> of the season that just BEGAN.</summary>
        public readonly int NextSeasonNumber;

        /// <summary>The derived seed the new season's schedule and quick-sim draws key from.</summary>
        public readonly ulong NextSeasonSeed;

        /// <summary>The world-day the new season's first round is scheduled on.</summary>
        public readonly uint NextFirstFixtureDay;

        /// <summary>Constructs an outcome record. Pure data — no validation beyond the caller's own.</summary>
        public SeasonRollOutcome(
            int completedSeasonNumber,
            int finalPosition,
            int targetPosition,
            bool objectiveMet,
            int jobSecurityBeforePerMille,
            int jobSecurityAfterPerMille,
            int nextSeasonNumber,
            ulong nextSeasonSeed,
            uint nextFirstFixtureDay)
        {
            CompletedSeasonNumber = completedSeasonNumber;
            FinalPosition = finalPosition;
            TargetPosition = targetPosition;
            ObjectiveMet = objectiveMet;
            JobSecurityBeforePerMille = jobSecurityBeforePerMille;
            JobSecurityAfterPerMille = jobSecurityAfterPerMille;
            NextSeasonNumber = nextSeasonNumber;
            NextSeasonSeed = nextSeasonSeed;
            NextFirstFixtureDay = nextFirstFixtureDay;
        }

        /// <summary>
        /// How many league positions short of the objective the club finished; zero when it was met.
        /// The quantity the missed job-security penalty scales by (§3.5 step (b)).
        /// </summary>
        public int PlacesShort => ObjectiveMet ? 0 : FinalPosition - TargetPosition;

        /// <summary>The signed change in job security across the evaluation.</summary>
        public int JobSecurityDeltaPerMille => JobSecurityAfterPerMille - JobSecurityBeforePerMille;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial implementation (#30 T3): the boundary-roll producer record |
// |         |            |        | — board verdict, job-security before/after, and what the next      |
// |         |            |        | season starts from. Session-scoped, deliberately not serialized.   |
#endregion
