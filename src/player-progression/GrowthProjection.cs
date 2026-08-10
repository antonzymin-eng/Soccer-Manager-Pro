// File:     src/player-progression/GrowthProjection.cs
// Created:  2026-07-24
// Modified: 2026-08-10
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1 (the daily growth projection); Code Standards #20
// Purpose:  The pure, draw-free per-player daily step (§3.1) — the SOLE attribute-mutation path
//           (FR-PG-008). Integer fixed-point cursor accrual + spend/drain; age derived, no rollover.

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// The §3.1 daily growth projection: a pure function of the player's state + inputs, with no RNG
    /// draw (FR-PG-002). It is the single writer of attribute change (FR-PG-008): age is derived from
    /// <see cref="PlayerLifecycle.BirthWorldDay"/> with no discrete rollover, the signed
    /// <see cref="PlayerLifecycle.GrowthCursor"/> is the only accumulator, and whole attribute-points
    /// are spent/drained at the <c>POINT_COST</c> threshold.
    /// </summary>
    public static class GrowthProjection
    {
        /// <summary>
        /// Advances one player by one world-day (§3.1). Keeps <c>rec.Age</c> current as a derived cache,
        /// accrues the band's daily points to the cursor, spends/drains whole attribute-points at the
        /// threshold, then recomputes the derived CA. Byte-exact and restore-deterministic: every mutated
        /// field is integer and a pure function of serialized state (FR-PG-006).
        /// </summary>
        /// <param name="rec">The career-state record (attributes + the derived Age cache); mutated in place.</param>
        /// <param name="life">The lifecycle overlay (cursor, PA, birth day, retirement); mutated in place.</param>
        /// <param name="worldDay">The current world-day (calendar day).</param>
        /// <param name="training">The #29 training contribution; <see cref="TrainingInput.Neutral"/> is byte-identical to no training.</param>
        /// <param name="curveEnabled">
        /// Reserved for the T3 deep (PA−CA)-scaled curve. In T0 it has no effect — the step always applies
        /// the literal §4.3 band step (the KD-8 identity, FR-PG-007); the curve modulation lands at T3.
        /// </param>
        public static void AdvanceDayForPlayer(
            ref PlayerRecord rec,
            ref PlayerLifecycle life,
            uint worldDay,
            in TrainingInput training,
            bool curveEnabled)
        {
            // 1. Age is DERIVED — no discrete rollover step (§3.1.1); attribute change is the cursor alone.
            long ageDays = (long)worldDay - life.BirthWorldDay;

            // The narrowing is SATURATING, and the predicate is >= 0 rather than > 0 (AR pass 5).
            // §3.5 states never-write-what-Decode-refuses as a MUST, and this is the one site that
            // derives a value the save path range-gates — so it must be structurally incapable of
            // producing one out of range, independently of the anchor gate that now guards the input.
            // Two layers rather than one because they fail differently: the gate refuses bad state at
            // the boundary, this keeps the step total even if a future boundary is added without it.
            // (`>= 0` also makes the boundary the one it names — a player born TODAY is age 0, which
            // took the else-branch before. Same answer today; the right predicate for when DailyPoints
            // becomes sensitive to the anchor day, which the seed-day credit has now made it.)
            long ageYears = ageDays >= 0 ? ageDays / PlayerProgressionConstants.DAYS_PER_YEAR : 0;
            int age = ageYears > PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                ? PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                : (int)ageYears;
            rec.Age = age; // keep the record's Age current (derived cache, FR-PG-005)

            // 2. Per-day point accrual — the ONLY accumulator (FR-PG-002/003).
            AbilityModel.AgeBand band = AbilityModel.ClassifyAgeBand(age);
            life.GrowthCursor += DailyPoints(band, rec.Position, in training, curveEnabled);

            // 3. Spend/drain whole attribute-points at the POINT_COST threshold (deterministic order).
            while (life.GrowthCursor >= PlayerProgressionConstants.POINT_COST)
            {
                if (!AbilityModel.TrySpendOnePoint(ref rec, ref life))
                {
                    // At the PA ceiling. CLAMP, do not bank (M2, ERR-028-018): leaving the cursor to
                    // accumulate across every refused day lets it grow unbounded while PA == CA holds,
                    // and Stable never spends it — so a long PA-bound stretch (an authored player with
                    // no headroom, #47) banks thousands of points that then have to be walked back down
                    // through Decline before a single point can drain, silently cancelling years of
                    // decline the player should have taken. Clamping to POINT_COST - 1 keeps the pending
                    // fraction (still no thrash — the next Growth day's accrual can still cross the
                    // threshold and try again) while bounding the credit to at most one point's worth.
                    life.GrowthCursor = PlayerProgressionConstants.POINT_COST - 1;
                    break;
                }
                life.GrowthCursor -= PlayerProgressionConstants.POINT_COST;
            }
            while (life.GrowthCursor <= -PlayerProgressionConstants.POINT_COST)
            {
                AbilityModel.DrainOnePoint(ref rec, ref life);
                life.GrowthCursor += PlayerProgressionConstants.POINT_COST;
            }

            // 4. Recompute the derived CA summary (never a second accumulator, FR-PG-003).
            life.CurrentAbility = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);
        }

        // Signed integer daily accrual. curveEnabled OFF ⇒ the literal §4.3 band step (KD-8): Growth
        // +GROWTH_DAILY_POINTS, Decline +DECLINE_DAILY_POINTS, Stable 0; TrainingInput.Neutral adds 0.
        // curveEnabled ON (T3) would modulate by (PA−CA) + training — not built in T0, so both params
        // are accepted but unread here (the flat band step is the KD-8 behaviour-neutral identity).
        private static long DailyPoints(
            AbilityModel.AgeBand band,
            PlayerPosition pos,
            in TrainingInput training,
            bool curveEnabled)
        {
            switch (band)
            {
                case AbilityModel.AgeBand.Growth:
                    return PlayerProgressionConstants.GROWTH_DAILY_POINTS;
                case AbilityModel.AgeBand.Decline:
                    return PlayerProgressionConstants.DECLINE_DAILY_POINTS;
                default:
                    return 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation.                                        |
// | 1.1     | 2026-08-10 | —      | AR pass 5 carryforward (ERR-028-018). Age narrowing made       |
// |         |            |        | saturating: predicate `>= 0` (a player born today is age 0,    |
// |         |            |        | not the else-branch) and a MAX_DERIVABLE_AGE_YEARS ceiling     |
// |         |            |        | (L1). M2 fix: a refused spend at the PA ceiling now clamps     |
// |         |            |        | GrowthCursor to POINT_COST - 1 instead of banking unbounded —  |
// |         |            |        | an unbounded bank (2,189 points measured) silently cancelled   |
// |         |            |        | years of Decline once reached, via FromBlocks / #47 authored   |
// |         |            |        | PA. No draw, no format change.                                 |
#endregion
