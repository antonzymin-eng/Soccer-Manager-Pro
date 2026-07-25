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
    /// <b>Job security is an integer per-mille</b> in <c>[0, JobSecurityScale]</c>, not a float.
    /// Appendix B row 11 leaves the representation open (<c>jobSecurity f32/u8</c>); this resolves it
    /// toward the integer-arithmetic convention every later management spec standardized on (#41's
    /// AR-1 moved that spec's whole model "float arithmetic → integer per-mille"; #40 uses integer
    /// currency; #33 uses per-mille scalars). Integers also make the T1 sub-blob trivially
    /// round-trip-exact with no NaN gate. Recorded as a spec-clarification back-prop candidate — see
    /// this landing's notes.
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): integer per-mille job security    |
// |         |            |        | (resolving Appendix B's open f32/u8 toward the integer convention);|
// |         |            |        | pure IsOnTrack projection (FR-SN-015). Boundary evaluation is T3.  |
#endregion
