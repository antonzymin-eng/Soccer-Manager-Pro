// File:     src/season-save/BoardState.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, §3.5 step (b), Appendix B row 11, FR-SN-014/015, KD-6;
//           Code Standards #20
// Purpose:  The board's objective plus its running job-security reading. Evaluation at the season
//           boundary is #30 T3 (§3.5); T0 provides the value type and the pure "on track?" projection
//           FR-SN-015 requires.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The board's state (#30 §2.2 / FR-SN-014): the season objective and a job-security reading.
    /// <para>
    /// <b>Job security is an integer per-mille</b> in <c>[0, JobSecurityScale]</c>, not a float. #30 T0
    /// adopted this against an Appendix B row 11 that then left the representation open
    /// (<c>jobSecurity f32/u8</c>), following the integer-arithmetic convention every later management
    /// spec standardized on (#41's AR-1 moved that spec's whole model "float arithmetic → integer
    /// per-mille"; #40 uses integer currency; #33 uses per-mille scalars); integers also make the T1
    /// sub-blob round-trip-exact with no NaN gate. The back-prop candidate this row was recorded as is
    /// now CLOSED: T1 pinned Appendix B row 11 to <c>jobSecurityPerMille i32</c> (ERR-030-011), so the
    /// spec and this type agree and the representation is no longer open.
    /// </para>
    /// </summary>
    public readonly struct BoardState
    {
        /// <summary>The season objective (FR-SN-014).</summary>
        public readonly BoardObjective Objective;

        /// <summary>
        /// Job security, per-mille in <c>[0, SeasonLoopConstants.JobSecurityScale]</c>: 0 = about to
        /// be sacked, 1000 = fully secure.
        /// </summary>
        public readonly int JobSecurityPerMille;

        /// <summary>
        /// Constructs a board state.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="jobSecurityPerMille"/>
        /// is outside <c>[0, JobSecurityScale]</c>.</exception>
        public BoardState(BoardObjective objective, int jobSecurityPerMille)
        {
            if (jobSecurityPerMille < 0 || jobSecurityPerMille > SeasonLoopConstants.JobSecurityScale)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(jobSecurityPerMille), jobSecurityPerMille,
                    $"Job security must be in [0, {SeasonLoopConstants.JobSecurityScale}].");
            }

            Objective = objective;
            JobSecurityPerMille = jobSecurityPerMille;
        }

        /// <summary>
        /// A fresh board state for a club starting a season under <paramref name="objective"/>, fully
        /// secure.
        /// </summary>
        public static BoardState Fresh(BoardObjective objective) =>
            new BoardState(objective, SeasonLoopConstants.JobSecurityScale);

        /// <summary>
        /// The running "on track?" read (FR-SN-015): whether the club's CURRENT league position would
        /// satisfy the objective. A pure projection over the live table position — it does not mutate
        /// the objective, and is distinct from the season-boundary pass/fail evaluation (#30 T3, §3.5
        /// step (b)).
        /// </summary>
        public bool IsOnTrack(int currentPosition) => Objective.IsMetBy(currentPosition);

        /// <summary>Returns this state with a new job-security reading.</summary>
        public BoardState WithJobSecurity(int jobSecurityPerMille) =>
            new BoardState(Objective, jobSecurityPerMille);

        /// <summary>
        /// The §3.5 step (b) season-boundary evaluation (#30 T3): judges <paramref name="finalPosition"/>
        /// against this board's objective and returns the state with the resulting job-security reading.
        /// <para>
        /// Meeting the objective adds a flat amount; missing it subtracts per league position short, so
        /// the penalty tracks how badly it was missed — a flat rate would make missing by one identical
        /// to finishing bottom. The result is clamped into <c>[0, JobSecurityScale]</c>, accumulated in
        /// <c>long</c> first so a hostile <c>[GT]</c> value cannot overflow the multiply into a positive
        /// number and read as a reward.
        /// </para>
        /// <para>
        /// <b>Board policy lives here, not on the caller.</b> The pass/fail branch is
        /// <see cref="BoardObjective.IsMetBy"/> — the same predicate <see cref="IsOnTrack"/> uses — so the
        /// running "on track?" read and the boundary verdict can never disagree. When #45 extends the
        /// objective model, changing <c>IsMetBy</c> moves both, with no second copy of the rule to find.
        /// </para>
        /// </summary>
        public BoardState EvaluateAtSeasonEnd(int finalPosition)
        {
            long delta;
            if (Objective.IsMetBy(finalPosition))
            {
                delta = SeasonLoopConstants.BoardJobSecurityMetDeltaPerMille;
            }
            else
            {
                long placesShort = finalPosition - Objective.TargetPositionOrBetter;
                delta = -(long)SeasonLoopConstants.BoardJobSecurityMissedDeltaPerMille * placesShort;
            }

            long result = JobSecurityPerMille + delta;

            if (result < 0)
            {
                result = 0;
            }
            else if (result > SeasonLoopConstants.JobSecurityScale)
            {
                result = SeasonLoopConstants.JobSecurityScale;
            }

            return WithJobSecurity((int)result);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): integer per-mille job security    |
// |         |            |        | (resolving Appendix B's open f32/u8 toward the integer convention);|
// |         |            |        | pure IsOnTrack projection (FR-SN-015). Boundary evaluation is T3.  |
// | 1.1     | 2026-07-25 | —      | AR pass 2 (doc): the Appendix B row 11 back-prop candidate is now  |
// |         |            |        | CLOSED — T1 pinned the row to jobSecurityPerMille i32              |
// |         |            |        | (ERR-030-011), so the representation is no longer open.            |
// | 1.2     | 2026-07-27 | —      | #30 T3 AR: EvaluateAtSeasonEnd — the §3.5 step (b) boundary        |
// |         |            |        | evaluation, moved here from SeasonLoop. It had re-derived the      |
// |         |            |        | pass/fail branch as `finalPosition <= targetPosition` rather than  |
// |         |            |        | calling BoardObjective.IsMetBy, so #45 extending the objective     |
// |         |            |        | model would have moved the reported verdict and IsOnTrack while    |
// |         |            |        | leaving the job-security consequence on the old rule — silently.   |
// |         |            |        | One predicate now drives verdict, running read, and penalty.       |
#endregion
