// File:     src/match-analytics/XgLocationModel.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §3.3 (KD-2), §2.3 failure modes F2, Code Standards #20
// Purpose:  The expected-goals location model: a pure, stateless, deterministic two-term geometric
//           function of where a shot was struck from. No RNG, no engine state, no allocation.

using System;

using UnityEngine;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// The §3.3 xG location model — <c>xG = logistic(INTERCEPT − DIST·d + ANGLE·θ)</c>, where <c>d</c> is
    /// the distance from the shot origin to the goal centre and <c>θ</c> is the angle the goal mouth
    /// subtends from that origin.
    ///
    /// <para><b>What is contracted here is the SHAPE, not the numbers</b> (§3.3): closer and more open
    /// gives a higher value, monotonically in both terms. The three coefficients are <c>[GT]</c> and
    /// explicitly illustrative until a Stage-2 balance pass fits them; the tests lock the shape and the
    /// spec's worked examples, so a refit changes values without invalidating the model.</para>
    ///
    /// <para><b>Stage-1 status (KD-2): this model has no valid live input yet.</b> Scoring it needs a
    /// shot ORIGIN, and the event ledger carries none — <c>GoalAwardedEvent.BallPosition</c> is where the
    /// ball crossed the line, not where it was struck from. So <c>AdvancedStatline.LiveXgAvailable</c> is
    /// false and <c>XgSum</c> is 0 until the deferred <c>ShotAttemptedEvent</c> producer (Appendix D)
    /// exists. Feeding it the crossing point would produce a confidently wrong number, which is worse
    /// than reporting no number at all — hence the explicit availability flag rather than a silent zero.
    /// The function itself is authored and locked now so that producer, when it lands, consumes it
    /// unchanged.</para>
    /// </summary>
    public static class XgLocationModel
    {
        /// <summary>
        /// Scores a shot struck from <paramref name="shotOrigin"/> by <paramref name="attackingTeam"/>.
        /// </summary>
        /// <param name="shotOrigin">Pitch-plane origin (x, y) in metres, corner-origin frame.</param>
        /// <param name="attackingTeam">0 (attacks +x) or 1 (attacks −x).</param>
        /// <returns>Expected goals in [0, 1].</returns>
        /// <exception cref="ArgumentException">A non-finite coordinate (F2).</exception>
        /// <exception cref="ArgumentOutOfRangeException">An unknown team id.</exception>
        public static float Evaluate(Vector2 shotOrigin, byte attackingTeam)
        {
            // F2 — non-finite covers NaN AND ±Infinity. A NaN-only gate would let an infinity through
            // to the exponential below, where it becomes a plausible-looking 0.0 or 1.0.
            RequireFinite(shotOrigin.x, "shotOrigin.x");
            RequireFinite(shotOrigin.y, "shotOrigin.y");

            RequireTeam(attackingTeam);

            Vector2 goalCentre = GoalCentre(attackingTeam);

            float d     = Vector2.Distance(shotOrigin, goalCentre);
            float theta = SubtendedGoalAngleDegrees(shotOrigin, attackingTeam);

            float z = MatchAnalyticsConstants.XG_INTERCEPT
                    - MatchAnalyticsConstants.XG_DIST_COEF  * d
                    + MatchAnalyticsConstants.XG_ANGLE_COEF * theta;

            return Mathf.Clamp01(Logistic(z));
        }

        /// <summary>
        /// Centre of the goal <paramref name="attackingTeam"/> is shooting at. Team 0 attacks +x
        /// (Ball Physics #1 §1.2 corner-origin frame), so its target is the goal at x = PITCH_LENGTH.
        /// </summary>
        public static Vector2 GoalCentre(byte attackingTeam)
        {
            RequireTeam(attackingTeam);
            float x = attackingTeam == 0 ? MatchAnalyticsConstants.PITCH_LENGTH_M : 0f;
            return new Vector2(x, MatchAnalyticsConstants.PITCH_WIDTH_M * 0.5f);
        }

        /// <summary>
        /// The angle (degrees) the goal mouth subtends from <paramref name="shotOrigin"/> — the "how
        /// much goal can I see" term. Computed as the angle between the vectors to the two posts, which
        /// stays correct from any position on the pitch, including from behind the goal line (where it
        /// collapses towards zero) and from the goal centre itself.
        /// </summary>
        public static float SubtendedGoalAngleDegrees(Vector2 shotOrigin, byte attackingTeam)
        {
            RequireTeam(attackingTeam);
            RequireFinite(shotOrigin.x, "shotOrigin.x");
            RequireFinite(shotOrigin.y, "shotOrigin.y");

            Vector2 goalCentre = GoalCentre(attackingTeam);
            float half = MatchAnalyticsConstants.GOAL_WIDTH_M * 0.5f;

            Vector2 toLeftPost  = new Vector2(goalCentre.x, goalCentre.y - half) - shotOrigin;
            Vector2 toRightPost = new Vector2(goalCentre.x, goalCentre.y + half) - shotOrigin;

            // Standing exactly on a post gives a zero-length vector, for which the angle is undefined.
            // Zero is the right answer there and keeps the function total.
            if (toLeftPost.sqrMagnitude <= 0f || toRightPost.sqrMagnitude <= 0f)
            {
                return 0f;
            }

            return Vector2.Angle(toLeftPost, toRightPost);
        }

        /// <summary>
        /// <c>1 / (1 + e^−z)</c>, written so a large-magnitude z cannot overflow the exponential: for
        /// negative z the algebraically equivalent <c>e^z / (1 + e^z)</c> is evaluated instead, so the
        /// argument to <c>Exp</c> is never positive.
        /// </summary>
        private static float Logistic(float z)
        {
            if (z >= 0f)
            {
                return 1f / (1f + Mathf.Exp(-z));
            }
            float ez = Mathf.Exp(z);
            return ez / (1f + ez);
        }

        /// <summary>Shared by all three public entry points (AR-1 M-5): before this, only
        /// <see cref="Evaluate"/> gated the team, so <c>GoalCentre(5)</c> silently answered with team 1's
        /// geometry — two public methods on one model with opposite contracts.</summary>
        private static void RequireTeam(byte attackingTeam)
        {
            if (attackingTeam >= MatchAnalyticsConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackingTeam), attackingTeam, "attackingTeam must be 0 (attacks +x) or 1 (attacks −x).");
            }
        }

        private static void RequireFinite(float value, string what)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("XgLocationModel." + what + " is not finite.", what);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): the §3.3 pure xG location model —   |
// |         |            |        | distance + subtended-goal-angle logit, F2 non-finite gate,     |
// |         |            |        | overflow-safe logistic, [0,1] clamp. No live consumer at       |
// |         |            |        | Stage 1 (KD-2 — the ledger carries no shot origin).            |
// | 1.1     | 2026-07-27 | —      | AR-1 M-5: the team gate extracted to RequireTeam and applied   |
// |         |            |        | at ALL THREE public entry points. Evaluate checked it;         |
// |         |            |        | GoalCentre and SubtendedGoalAngleDegrees did not, so an        |
// |         |            |        | out-of-range team silently scored against the away goal        |
// |         |            |        | instead of failing loud — and both are public, so a caller     |
// |         |            |        | reaching them directly bypassed the only gate that existed.    |
#endregion
