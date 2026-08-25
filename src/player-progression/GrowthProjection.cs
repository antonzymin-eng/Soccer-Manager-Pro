// File:     src/player-progression/GrowthProjection.cs
// Created:  2026-07-24
// Modified: 2026-08-22 (ERR-028-020 — football-judgment proxy review batch 1 — v1.3)
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

            // M2(a): a NEGATIVE ageDays means BirthWorldDay is AHEAD of worldDay — the anchor claims
            // the player is born after the very day being advanced to, which cannot happen to a real
            // career (SeedFrom anchors at the seed day; AdvanceDay only ever moves worldDay forward).
            // The retired else-branch used to read this as age 0 — corrupt state read as ordinary data,
            // permanently: FromBlocks accepts it (ProgressionSaveCodec.DescribeOutOfRangeValues has no
            // world day to bound the anchor's upper end against; its own ceiling is uint.MaxValue,
            // true of the FORMAT but not of any one clock), so a save carrying the corrupt anchor loads
            // cleanly and every subsequent day step silently re-derives age 0 forever. Fail loud instead
            // of manufacturing a plausible-looking wrong age; the composition boundary (SeasonLoop's
            // per-player walk, M2(b)) now refuses this pairing before a day step can reach it — this
            // guard is the structural half, independent of any caller remembering to run that check.
            if (ageDays < 0)
            {
                throw new System.InvalidOperationException(
                    "Player " + rec.PlayerId + "'s BirthWorldDay (" + life.BirthWorldDay + ") is AHEAD "
                    + "of worldDay (" + worldDay + ") — a future-dated anchor is corrupt state, not an "
                    + "age (M2). Refusing rather than silently deriving age 0.");
            }

            // The narrowing is SATURATING (AR pass 5): both loops leave the derived age at most
            // MAX_DERIVABLE_AGE_YEARS. ageDays is now known non-negative (the guard above), so the old
            // `>= 0 ? … : 0` ternary that gated the retired else-branch collapses to a plain divide.
            long ageYears = ageDays / PlayerProgressionConstants.DAYS_PER_YEAR;
            int age = ageYears > PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                ? PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                : (int)ageYears;
            rec.Age = age; // keep the record's Age current (derived cache, FR-PG-005)

            // 2. Per-day point accrual — the ONLY accumulator (FR-PG-002/003).
            //    ERR-028-020: the rate is a function of ageDAYS, not of a three-way band on ageYEARS.
            //    The retired form asked ClassifyAgeBand(age) and returned one of three constants, so
            //    the day a player turned GROWTH_AGE his development stopped outright — a football
            //    judgment that is continuous everywhere collapsed onto an exact integer birthday.
            life.GrowthCursor += DailyPoints(ageDays, rec.Position, in training, curveEnabled);

            // 3. Spend/drain whole attribute-points at the POINT_COST threshold (deterministic order).
            while (life.GrowthCursor >= PlayerProgressionConstants.POINT_COST)
            {
                if (!AbilityModel.TrySpendOnePoint(ref rec, ref life))
                {
                    // At the PA ceiling: DISCARD the fraction. AR pass 5's M2 clamped to POINT_COST - 1
                    // "to keep the pending fraction — the next Growth day's accrual can still cross the
                    // threshold and try again". AR pass 6 falsified that rationale by execution: in the
                    // only situation reaching this line the retry can NEVER succeed, because PA does not
                    // rise (§3.2) and no attribute falls in the Growth band, so once the spend is refused
                    // it is refused every remaining day of Growth. The retained 364 bought nothing and
                    // cost a full point: a PA-bound player exited Growth carrying +364 and took his
                    // first decline point on day 4743 against an unbound player's 4379 — 364 days, one
                    // whole attribute point of Decline, silently cancelled. Which is the same unit of
                    // harm M2 was filed for, bounded to one point instead of six years' worth.
                    //
                    // Zero also restores the invariant 789ea74 established for everyone else and locked
                    // in ..._GainsExactlyOnePointPerYear_AndLeavesNoResidue: a band traversal ends with
                    // no residue. Neither of those two commits considered the other; this reconciles them.
                    life.GrowthCursor = 0;
                    break;
                }
                life.GrowthCursor -= PlayerProgressionConstants.POINT_COST;
            }
            while (life.GrowthCursor <= -PlayerProgressionConstants.POINT_COST)
            {
                if (!AbilityModel.DrainOnePoint(ref rec, ref life))
                {
                    // Fully drained — every attribute at ATTRIBUTE_MIN. The mirror of the exit above,
                    // and AR pass 6's High: without it this loop had NO failure exit at all. DrainOnePoint
                    // was void and a no-op at the floor, so the loop's only progress was the cursor
                    // creeping up one POINT_COST per iteration — which terminates in principle and not in
                    // practice: a cursor of long.MinValue/2 is ~1.26e13 iterations, about 70 days of CPU,
                    // with no diagnostic and from a save file that loads cleanly. Silent non-termination
                    // of the day step is worse than the refusal its neighbouring gates produce.
                    //
                    // Zero for the same reason as the spend side: within Decline no attribute rises, so
                    // the refusal is permanent and the fraction is unspendable.
                    life.GrowthCursor = 0;
                    break;
                }
                life.GrowthCursor += PlayerProgressionConstants.POINT_COST;
            }

            // 4. Recompute the derived CA summary (never a second accumulator, FR-PG-003).
            life.CurrentAbility = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);
        }

        // Signed integer daily accrual, age-continuous since ERR-028-020: AbilityModel.DailyBandPoints
        // differences an exact integer cumulative, so the per-day step stays in {0, ±1} (the cursor
        // scale is unchanged and nothing about the save block moves) while its DENSITY follows a rate
        // that ramps smoothly across each band edge. At AgeBandRampHalfWidthYears = 0 it returns the
        // literal §4.3 band step for every day, which is the KD-8 / FR-PG-007 identity.
        // curveEnabled ON (T3) would modulate by (PA−CA) + training — not built in T0, so both params
        // are accepted but unread here.
        private static long DailyPoints(
            long ageDays,
            PlayerPosition pos,
            in TrainingInput training,
            bool curveEnabled)
        {
            return AbilityModel.DailyBandPoints(ageDays);
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
// | 1.2     | 2026-08-11 | —      | AR pass 6, M2(a). The `>= 0 ? … : 0` ternary's else-branch      |
// |         |            |        | silently read a future-dated BirthWorldDay (ageDays < 0) as    |
// |         |            |        | age 0 — reachable and PERMANENT, since ProgressionSaveCodec's  |
// |         |            |        | DescribeOutOfRangeValues has no world day to bound the anchor  |
// |         |            |        | against (its own ceiling is uint.MaxValue). Now fails loud on  |
// |         |            |        | ageDays < 0 instead of manufacturing an age; the composition   |
// |         |            |        | boundary (M2(b), PlayerCareerStates.RequireBirthWorldDayWithin |
// |         |            |        | Clock) refuses the pairing before a day step can reach this.   |
// | 1.3     | 2026-08-22 | —      | ERR-028-020. Step 2's accrual takes ageDAYS through
// |         |            |        | AbilityModel.DailyBandPoints instead of a three-way band on ageYEARS —
// |         |            |        | the hard step at an exact integer age the football-judgment proxy review
// |         |            |        | filed. No signature change on AdvanceDayForPlayer, no format change.
#endregion
