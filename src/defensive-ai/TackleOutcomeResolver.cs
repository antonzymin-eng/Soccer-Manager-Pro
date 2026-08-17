// File:     src/defensive-ai/TackleOutcomeResolver.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6.5, Deterministic Simulation #16 §3.4.4, Code Standards #20
// Purpose:  Pure static resolution of one committed tackle into Missed / BallWon / BallLoose / Foul,
//           from the two players' abilities and the geometry of the challenge. No state, no RNG
//           service — the caller supplies the draw.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Resolves one committed tackle (#14 §3.6.5). Pure static, zero allocation, no RNG service: the
    /// caller supplies a single uniform draw and this maps it onto the four outcomes.
    ///
    /// <para><b>Why this lives in #14 and not in the composition root.</b> KD-6 as originally written
    /// said "#14 produces intent; #8 mediates dispatch; #3 owns contact physics", and none of that
    /// survived contact with the code: #8 cannot mediate (its <c>ActionType</c> ordinal space is
    /// exhausted by the 3-bit composure-noise field), and #3 defers slide-tackle collision to Stage 2
    /// while its own KR-3 records that ball-first-versus-player-first is undecidable at 60 Hz discrete
    /// detection. A Stage-0 tackle therefore cannot be a physics contact; it is an abstract duel
    /// between two players' abilities — and by the W1 precedent, which took the keeper's rush-commit
    /// distance back into #11 because "how far he comes out is a property of the keeper", whether a
    /// defender wins a challenge is a property of the defender and the carrier. It belongs to the spec
    /// that owns them. That is the ERR-014-006 back-propagation.</para>
    ///
    /// <para><b>One draw, three decisions</b> — the §5.Z.9 KD-F2 shape. A single uniform is consumed by
    /// nested inverse transforms, so there is no second stream, no second draw site, and no ordering
    /// between sub-decisions to get wrong.</para>
    ///
    /// <para><b>Continuous everywhere (football-judgment doctrine §6 P1).</b> No threshold anywhere
    /// decides an outcome: every input moves the probabilities smoothly, so a millimetre or one
    /// attribute point never flips a challenge from certain success to certain failure. The nearest
    /// thing to a cliff is the caller's contact radius, and even that enters here as a continuous
    /// <c>ReachFraction</c> rather than as an in/out test.</para>
    /// </summary>
    public static class TackleOutcomeResolver
    {
        /// <summary>
        /// Resolves one challenge. <paramref name="uniform"/> must be in [0, 1); values outside are
        /// clamped rather than rejected, because a draw is a derived quantity and failing loud here
        /// would put a throw on the match's hot path for a condition the caller cannot recover from.
        /// </summary>
        public static TackleOutcome Resolve(in TackleDuelInputs inputs, float uniform)
        {
            float u = Mathf.Clamp(uniform, 0f, TackleUniformCeiling);

            float engage = EngageProbability(in inputs);
            if (u >= engage)
            {
                return TackleOutcome.Missed;
            }

            // Conditional on connecting, u/engage is uniform on [0,1) — the input the two shares below
            // are defined against. engage > u >= 0 here, so it cannot be zero.
            float v = u / engage;

            float foulShare = FoulShare(in inputs);
            if (v < foulShare)
            {
                return TackleOutcome.Foul;
            }

            // Same construction one level down. foulShare < 1 is guaranteed by its own clamp, so the
            // denominator is strictly positive.
            float w = (v - foulShare) / (1f - foulShare);

            return w < CleanShare(in inputs) ? TackleOutcome.BallWon : TackleOutcome.BallLoose;
        }

        /// <summary>
        /// P(the challenge connects at all) — the complement of <see cref="TackleOutcome.Missed"/>.
        ///
        /// <para>Driven by how committed the tackler's movement is and how close he is to the ball.
        /// Commitment is <c>cos(approachAngle)</c> clamped at zero: running straight at the man is full
        /// commitment, running across him is none, and running away from him is none rather than
        /// negative — a tackler moving away does not connect LESS than one moving sideways, he simply
        /// does not connect, and the clamp says so without a branch.</para>
        /// </summary>
        public static float EngageProbability(in TackleDuelInputs inputs)
        {
            float commitment = Mathf.Clamp01(Mathf.Cos(inputs.ApproachAngle));
            float proximity = 1f - Mathf.Clamp01(inputs.ReachFraction);

            float p = DefensiveAIConstants.TackleEngageBase
                      + DefensiveAIConstants.TackleEngageCommitmentK * commitment
                      + DefensiveAIConstants.TackleEngageProximityK * proximity;

            return Mathf.Clamp01(p);
        }

        /// <summary>
        /// P(foul | connected). Rises with the tackler's Aggression and falls with his Tackling: the
        /// same challenge is a foul when it is mistimed, and timing is what Tackling measures.
        ///
        /// <para>Clamped strictly below 1 so <see cref="Resolve"/>'s second inverse transform always has
        /// a positive denominator — a ceiling that is a numerical guarantee, not a football judgment,
        /// and is therefore <c>[FIXED]</c> rather than tunable.</para>
        /// </summary>
        public static float FoulShare(in TackleDuelInputs inputs)
        {
            float p = DefensiveAIConstants.TackleFoulShareBase
                      + DefensiveAIConstants.TackleFoulShareAggressionK * Mathf.Clamp01(inputs.TacklerAggression)
                      - DefensiveAIConstants.TackleFoulShareTacklingK * Mathf.Clamp01(inputs.TacklerTackling);

            return Mathf.Clamp(p, 0f, DefensiveAIConstants.TACKLE_FOUL_SHARE_CEILING);
        }

        /// <summary>
        /// P(clean win | connected and not a foul) — the rest being <see cref="TackleOutcome.BallLoose"/>.
        ///
        /// <para>Driven by the tackler's edge over the carrier: his Tackling against the carrier's
        /// ability to keep the ball (Dribbling) and to stay on his feet through contact (Balance). A
        /// defender who merely matches his man mostly knocks the ball loose; taking it cleanly off him
        /// requires being better than him, which is why the base sits well below a half.</para>
        /// </summary>
        public static float CleanShare(in TackleDuelInputs inputs)
        {
            float retain = DefensiveAIConstants.TackleRetainDribblingWeight * Mathf.Clamp01(inputs.CarrierDribbling)
                           + DefensiveAIConstants.TackleRetainBalanceWeight * Mathf.Clamp01(inputs.CarrierBalance);

            float edge = Mathf.Clamp01(inputs.TacklerTackling) - retain;

            return Mathf.Clamp01(
                DefensiveAIConstants.TackleCleanShareBase
                + DefensiveAIConstants.TackleCleanShareEdgeK * edge);
        }

        /// <summary>
        /// The largest value <see cref="Resolve"/> will treat as a draw. A draw of exactly 1.0 would
        /// compare <c>u &gt;= engage</c> true for every reachable <c>engage</c> and make the outcome
        /// unreachable-by-construction rather than merely unlikely; clamping just below keeps the
        /// mapping total.
        /// </summary>
        private const float TackleUniformCeiling = 0.999999f;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-12 | —      | Initial. #14 §3.6.5 (wiring backlog W2, ERR-014-006). One draw,  |
// |         |            |        | three nested inverse transforms (the KD-F2 shape) onto four      |
// |         |            |        | outcomes. Continuous in every input per football-judgment §6 P1. |
// |         |            |        | ApproachAngle is used ONLY as commitment (cos, clamped at 0) —   |
// |         |            |        | it does not encode which side of the carrier the tackler is on.  |
#endregion
